using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using E133.Business;
using E133.Business.Bases;
using E133.Business.Models;
using E133.Parser.LanguageUtilities;

using HtmlAgilityPack;

namespace E133.Parser
{
    internal abstract class HtmlDocumentParser<TBase> : IHtmlParser, IDisposable
        where TBase : IBase, new()
    {
        protected const int RequirementsSubrecipeId = -2;
        protected const int PreparationSubrecipeId = -1;

        private readonly IHtmlLoader _htmlLoader;
        private readonly Regex _quantityExpression;
        private readonly Regex _ingredientExpression;
        private readonly Regex _ingredientFullExpression;
        private readonly Regex _ingredientUnitExpression;
        private readonly Regex _wordExpression;

        private readonly Func<CultureInfo, IActionDetector> _actionDetectorFactory;
        private readonly Func<CultureInfo, ITimerDetector> _timerDetectorFactory;
        private readonly Func<CultureInfo, IMeasureUnitDetector> _measureUnitDetectorFactory;
        private readonly Func<CultureInfo, ILanguageHelper> _languageHelperFactory;
        private readonly Func<CultureInfo, ISubrecipeRepository> _subrecipeRepositoryFactory;

        private IActionDetector _actionDetector;
        private ITimerDetector _timerDetector;
        private IMeasureUnitDetector _measureUnitDetector;
        private ILanguageHelper _generalLanguageHelper;
        private ISubrecipeRepository _subrecipeRepository;

        private CultureInfo _recipeCulture;

        protected HtmlDocumentParser(
            IHtmlLoader htmlLoader,
            Func<CultureInfo, IActionDetector> actionDetectorFactory, 
            Func<CultureInfo, ITimerDetector> timerDetectorFactory,
            Func<CultureInfo, IMeasureUnitDetector> measureUnitDetectorFactory,
            Func<CultureInfo, ILanguageHelper> languageHelperFactory,
            Func<CultureInfo, ISubrecipeRepository> subrecipeRepositoryFactory)
        {
            this._htmlLoader = htmlLoader;
            this._actionDetectorFactory = actionDetectorFactory;
            this._timerDetectorFactory = timerDetectorFactory;
            this._measureUnitDetectorFactory = measureUnitDetectorFactory;
            this._languageHelperFactory = languageHelperFactory;
            this._subrecipeRepositoryFactory = subrecipeRepositoryFactory;

            if (this._htmlLoader != null)
            {
                this._htmlLoader.Initialize();
            }
            
            this.Base = new TBase();
            
            // TODO Localize and put somewhere else
            this._wordExpression = new Regex(@"[\w()°]+['’]*|[,]|[\)]\b", RegexOptions.Compiled);
            this._quantityExpression = new Regex(@"[\xbc-\xbe\w]+[\xbc-\xbe\w'’,./]*", RegexOptions.Compiled);
            this._ingredientExpression = new Regex(@"(?<=[a-zA-Z0-9\u00C0-\u017F\s()'’\-\/%] de | d'| d’)([a-zA-Z0-9\u00C0-\u017F\s()'’\-\/%]+)(, [,\w\s]+)*", RegexOptions.Compiled);
            this._ingredientFullExpression = new Regex(@"([a-zA-Z0-9\u00C0-\u017F\s()'’\-\/%]+)(, [,\w\s]+)*", RegexOptions.Compiled);
            this._ingredientUnitExpression = new Regex(@"(?<=[a-zA-Z0-9\u00C0-\u017F\s()'’\-\/%])([a-zA-Z0-9\u00C0-\u017F\s()'’\-\/%]+)(, [,\w\s]+)*", RegexOptions.Compiled);
        }

        public IBase Base { get; }

        public void Dispose()
        {
            this._htmlLoader.Dispose();
        }

        public async Task<QuickRecipe> ParseHtmlAsync(Uri uri)
        {
            var document = await this.LoadDocument(uri);

            // document.OptionDefaultStreamEncoding = Encoding.UTF8;

            var recipe = new QuickRecipe();
            recipe.Language = this.GetRecipeIetfLanguage(document);
            Console.WriteLine("Found language: " + recipe.Language);

            recipe.Note = this.GetNote(document);
            Console.WriteLine("Found note: " + recipe.Note);
            
            recipe.SmallImageUrl = this.GetSmallImageUrl(document);
            Console.WriteLine("Found small image: " + recipe.SmallImageUrl);
            
            recipe.NormalmageUrl = this.GetNormalImageUrl(document);
            Console.WriteLine("Found normal image: " + recipe.NormalmageUrl);
            
            recipe.LargeImageUrl = this.GetLargeImageUrl(document);
            Console.WriteLine("Found large image: " + recipe.LargeImageUrl);

            recipe.OriginalUrl = uri.AbsoluteUri;
            Console.WriteLine("Found uri: " + recipe.OriginalUrl);

            recipe.OriginalServings = this.GetRecipeYield(document);
            Console.WriteLine("Found servings: " + recipe.OriginalServings);

            recipe.Title = this.GetRecipeTitle(document);
            Console.WriteLine("Found title: " + recipe.Title);

            recipe.Durations = this.GetDurations(document).ToList();
            foreach (var duration in recipe.Durations)
            {
                Console.WriteLine("Found duration: " + duration.Title + " for " + duration.Time);  
            }

            recipe.Subrecipes = this._subrecipeRepository.KnownSubrecipes
                .Select(x => new Subrecipe { Id = x.Key, Title = x.Value })
                .ToList();

            var ingredientId = 0;
            var stepId = 0;
            var subrecipeNodes = this.GetSubrecipeNodes(document);

            if (subrecipeNodes != null && subrecipeNodes.Any())
            {
                var subrecipeNodesArray = subrecipeNodes.ToArray();
                for (var subrecipeIndex = 0; subrecipeIndex < subrecipeNodesArray.Count(); subrecipeIndex++)
                {
                    var subrecipeNode = subrecipeNodesArray[subrecipeIndex];
                    var title = this.GetSubrecipeTitle(subrecipeNode);
                    recipe.Subrecipes.Add(
                        new Subrecipe
                        {
                            Id = subrecipeIndex,
                            Title = title
                        });

                    Console.WriteLine("Found subrecipe: " + title);  

                    var subrecipeIngredientNodes = this.GetSubrecipeIngredientNodesFromParent(subrecipeNode);
                    this.ParseIngredients(subrecipeIngredientNodes, recipe, subrecipeIndex, ref ingredientId, ref stepId);
                }
            }
            // TODO Check if recipes exist with subrecipes AND orphans
            else
            {
                var ingredientsSection = this.GetIngredientSection(document);
                var orphanIngredientNodes = this.GetIngredientNodesFromParent(ingredientsSection);
                this.ParseIngredients(orphanIngredientNodes, recipe, PreparationSubrecipeId, ref ingredientId, ref stepId);
            }

            var stepSubrecipeNodes = this.GetStepSections(document);
            foreach (var stepSubrecipeNode in stepSubrecipeNodes)
            {
                // TODO Maybe we should order steps, in case a step subrecipe doesn't have the same text as the ingredient subrecipes
                var subrecipe = recipe.Subrecipes.SingleOrDefault(x => x.Title == stepSubrecipeNode.InnerText.Trim());
                var subrecipeId = subrecipe != null ? subrecipe.Id : PreparationSubrecipeId;

                var stepNodes = this.GetSubrecipeSteps(stepSubrecipeNode);
                foreach (var stepNode in stepNodes)
                {
                    var stepText = stepNode.InnerText.Trim();
                    // TODO Temp fix, localize and do better
                    var splitPhrases = stepText.Replace("c. à", "c à").Split('.').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                    foreach (var splitPhrase in splitPhrases)
                    {
                        var step = new Step { Id = stepId++, SubrecipeId = subrecipeId };
                        var words = this._wordExpression.Matches(splitPhrase);
                        var index = 0;
                        var skippedIndexes = new List<int>();
                        var phraseBuilder = new StringBuilder();
                        Type currentlyReadType = null;
                        while (index < words.Count)
                        {
                            if (skippedIndexes.Contains(index))
                            {
                                skippedIndexes.Remove(index);
                                index++;
                                continue;
                            }

                            var word = words[index].Value;
                            var previouslyReadType = currentlyReadType;
                            var phrase = words.Cast<Match>().Select(m => m.Value).ToArray();

                            if (this.LookAheadIngredientEnumerationStepPart(word, words, index, recipe, subrecipeId, skippedIndexes, ref word))
                            {
                                currentlyReadType = typeof(IngredientEnumerationPart);
                            }
                            else if (this.TryParseIngredientStepPart(word, words, index, recipe, subrecipeId, ref word))
                            {
                                currentlyReadType = typeof(IngredientPart);
                                skippedIndexes.Add(index + 1);
                            }
                            else
                            {
                                var timerPartResult = this.TryParseTimerPart(phrase, index);
                                if (timerPartResult.IsTimerPart)
                                {
                                    currentlyReadType = typeof(TimerPart);
                                    word = string.Concat(timerPartResult.OutputFormat, timerPartResult.OutputValue);
                                    skippedIndexes.AddRange(timerPartResult.SkippedIndexes);
                                    skippedIndexes.Add(index + 1);
                                }
                                else if (this._actionDetector.IsAction(word.Trim()))
                                {
                                    currentlyReadType = typeof(ActionPart);
                                }
                                else
                                {
                                    currentlyReadType = typeof(TextPart);
                                }
                            } 

                            if (previouslyReadType != null && previouslyReadType != currentlyReadType)
                            {
                                this.FlushPhrasePart(recipe, step, phraseBuilder, previouslyReadType);
                            }

                            phraseBuilder.AppendFormat(" {0}", word);

                            index++;
                        }

                        this.FlushPhrasePart(recipe, step, phraseBuilder, currentlyReadType);

                        recipe.Steps.Add(step);
                    }
                }
            }

            foreach (var subrecipe in this._subrecipeRepository.KnownSubrecipes)
            {
                ClearUnusedSubrecipe(recipe, subrecipe.Key);
            }

            return recipe;
        }

        protected string GetMatchingDdValue(HtmlNode dtNode)
        {
            var found = dtNode.SelectSingleNode("following-sibling::*[1][self::dd]");
            return found == null ? "" : found.InnerText;
        }

        protected abstract string GetRecipeIetfLanguage(HtmlDocument document);

        protected abstract string GetRecipeTitle(HtmlDocument document);

        protected abstract string GetSmallImageUrl(HtmlDocument document);

        protected abstract string GetNormalImageUrl(HtmlDocument document);

        protected abstract string GetLargeImageUrl(HtmlDocument document);

        protected abstract string GetNote(HtmlDocument document);

        protected abstract string GetRecipeYield(HtmlDocument document);

        protected abstract IEnumerable<Duration> GetDurations(HtmlDocument document);

        protected abstract HtmlNode GetIngredientSection(HtmlDocument document);

        protected abstract HtmlNodeCollection GetSubrecipeNodes(HtmlDocument document);

        protected abstract string GetSubrecipeTitle(HtmlNode subrecipeNode);

        protected abstract HtmlNodeCollection GetSubrecipeIngredientNodesFromParent(HtmlNode parent);

        protected abstract HtmlNodeCollection GetIngredientNodesFromParent(HtmlNode parent);

        protected abstract HtmlNodeCollection GetStepSections(HtmlDocument document);

        protected abstract HtmlNodeCollection GetSubrecipeSteps(HtmlNode stepSubrecipeNode);

        private async Task<HtmlDocument> LoadDocument(Uri uri)
        {
            var content = await this._htmlLoader.ReadHtmlAsync(uri);
            
            var document = new HtmlDocument();
            
            document.LoadHtml(System.Net.WebUtility.HtmlDecode(content));

            var language = this.GetRecipeIetfLanguage(document);
            this.InitializeCulture(language);

            return document;
        }

        public void InitializeCulture(string language)
        {
            this._recipeCulture = new CultureInfo(language);

            this._actionDetector = this._actionDetectorFactory(this._recipeCulture);
            this._timerDetector = this._timerDetectorFactory(this._recipeCulture);
            this._measureUnitDetector = this._measureUnitDetectorFactory(this._recipeCulture);
            this._generalLanguageHelper = this._languageHelperFactory(this._recipeCulture);
            this._subrecipeRepository = this._subrecipeRepositoryFactory(this._recipeCulture);
        }

        private bool LookAheadIngredientEnumerationStepPart(string word, MatchCollection words, int index, QuickRecipe recipe, int subrecipeId, List<int> skippedIndexes, ref string result)
        {
            var ingredientIds = new List<string>();
            var localSkippedIndexes = new List<int>();

            if (this.TryParseIngredientStepPart(word, words, index, recipe, subrecipeId, ref word))
            {
                ingredientIds.Add(word);
                localSkippedIndexes.Add(index + 1);
                index++;
                while (index < words.Count)
                {
                    if (localSkippedIndexes.Contains(index))
                    {
                        index++;
                        continue;
                    }

                    word = words[index].Value.Trim();
                    // TODO Localize this
                    // TODO Verify future next word, because the rest of the sentence could start with this
                    if (word == "," || word == "et")
                    {
                        localSkippedIndexes.Add(index);
                    }
                    else if (this.TryParseIngredientStepPart(word, words, index, recipe, subrecipeId, ref word))
                    {
                        ingredientIds.Add(word);
                        localSkippedIndexes.Add(index);
                        localSkippedIndexes.Add(index + 1);
                    }
                    else
                    {
                        break;
                    }

                    index++;
                }
            }

            var isEnumeration = ingredientIds.Count > 1;
            if (isEnumeration)
            {
                result = string.Join(",", ingredientIds);
                skippedIndexes.AddRange(localSkippedIndexes);                
            }

            return isEnumeration;
        }

        private bool TryParseIngredientStepPart(string word, MatchCollection words, int index, QuickRecipe recipe, int subrecipeId, ref string result)
        {
            if (this._generalLanguageHelper.IsDeterminant(word) && index + 1 < words.Count)
            {
                var nextWord = words[index + 1].Value.Trim();

                var referencedIngredient = recipe.Ingredients.FirstOrDefault(x => x.Name.Contains(nextWord) && x.SubrecipeId == subrecipeId);
                if (referencedIngredient != null)
                {
                    result = referencedIngredient.Id.ToString();

                    return true;
                }
            }

            return false;
        }

        // TEMP Public
        public ParseTimerPartResult TryParseTimerPart(IList<string> phrase, int index)
        {
            int time;
            var word = phrase[index].Trim();
            if (int.TryParse(word, out time))
            {
                var skippedIndexes = new List<int> { index };
                var durationBuilder = new StringBuilder().AppendFormat("PT{0}", time);
                var isTime = false;
                var rangeDuration = false;

                index++;
                while (index < phrase.Count)
                {
                    word = phrase[index].Trim();

                    // If we find another number, we add it in the duration only if it's not a range duration ("1 to 2 minutes")
                    // We do this because if it's a range duration, we want to keep only the first number for the final duration format
                    if (int.TryParse(word, out time))
                    {
                        if (!rangeDuration)
                        {
                            durationBuilder.Append(time);
                        }
                        skippedIndexes.Add(index);
                    }
                    else if (word == "à")
                    {
                        rangeDuration = true;
                        skippedIndexes.Add(index);
                    }
                    else if (this._timerDetector.IsTimeQualifier(word))
                    {
                        isTime = true;
                        var durationString = durationBuilder.ToString();
                        var qualifier = this._timerDetector.GetTimeQualifier(word);
                        // If qualifier is already in the duration string, do not stack formats.
                        // This may happen in cases like "1 hour 30 to 2 hours", where the second 'hours' will try to stack
                        // If last character is a letter, do not stack other formats. 
                        // This may happen in cases like "1 hour 30 minutes to 2 hours", where the second 'hours' will try to stack after 'minutes'  
                        if (int.TryParse(durationString.Last().ToString(), out time) && !durationString.Contains(qualifier))
                        {
                            durationBuilder.Append(qualifier);
                        }
                        skippedIndexes.Add(index);
                    }
                    // If we find something that is not from a time notation, we end this
                    else
                    {
                        break;
                    }

                    index++;
                }

                if (isTime)
                {
                    var durationString = durationBuilder.ToString();
                    // If this is a time notation, and the last character is an integer, it means we found something like "1 hour 30"
                    // We then have to find the last unit to append the lesser unit
                    if (int.TryParse(durationString.Last().ToString(), out time))
                    {
                        var lastChar = durationString.Last(x => !int.TryParse(x.ToString(), out time));
                        switch (lastChar)
                        {
                            case 'H': 
                                durationBuilder.Append("M");
                                break;
                            case 'M': 
                                durationBuilder.Append("S");
                                break;
                            default:
                                break;
                        }
                    }

                    var outputFormat = "{" + string.Join(" ", skippedIndexes.Select(x => phrase[x])) + "}";
                    var outputValue = durationBuilder.ToString();

                    return new ParseTimerPartResult { IsTimerPart = true, SkippedIndexes = skippedIndexes, OutputFormat = outputFormat, OutputValue = outputValue };
                }
            }

            return new ParseTimerPartResult { IsTimerPart = false };
        }

        private static void ClearUnusedSubrecipe(QuickRecipe recipe, int subrecipeId)
        {
            if (recipe.Steps.All(x => x.SubrecipeId != subrecipeId))
            {
                var subrecipe = recipe.Subrecipes.Single(x => x.Id == subrecipeId);
                recipe.Subrecipes.RemoveAt(recipe.Subrecipes.IndexOf(subrecipe));
            }
        }

        private void ParseIngredients(
            IEnumerable<HtmlNode> ingredientNodes,
            QuickRecipe recipe,
            int subrecipeId,
            ref int ingredientId,
            ref int stepId)
        {
            foreach (var ingredientNode in ingredientNodes)
            {
                var name = ingredientNode.InnerText.Trim();

                var matches = this._quantityExpression.Matches(name);

                var readQuantity = matches[0].Value;
                switch (readQuantity)
                {
                    case "½":
                        readQuantity = 0.5.ToString(this._recipeCulture);
                        break;
                    case "¼":
                        readQuantity = 0.25.ToString(this._recipeCulture);
                        break;
                    case "¾":
                        readQuantity = 0.75.ToString(this._recipeCulture);
                        break;
                }

                double quantity;
                var hasQuantity = double.TryParse(readQuantity, NumberStyles.Any, this._recipeCulture, out quantity);
                if (!hasQuantity)
                {
                    quantity = 1;
                }

                var readMeasureUnit = matches.Count > 1 ? matches[1].Value : string.Empty;
                var measureUnitEnum = this._measureUnitDetector.GetMeasureUnit(readMeasureUnit);

                Match ingredientMatch;
                if (measureUnitEnum == MeasureUnit.Unit)
                {
                    ingredientMatch = hasQuantity
                        ? this._ingredientUnitExpression.Match(name)
                        : this._ingredientFullExpression.Match(name);
                }
                else
                {
                    ingredientMatch = this._ingredientExpression.Match(name);
                }

                var ingredient = new Ingredient
                {
                    Id = ingredientId,
                    Quantity = new Quantity { Value = quantity, Abbreviation = measureUnitEnum },
                    SubrecipeId = subrecipeId
                };

                var ingredientPhrase = ingredientMatch.Value.Trim();
                var firstCommaIndex = ingredientPhrase.IndexOf(",");
                if (firstCommaIndex > -1)
                {
                    var requirements = ingredientPhrase.Substring(firstCommaIndex + 2, ingredientPhrase.Length - firstCommaIndex - 2);

                    ingredient.Name = ingredientPhrase.Substring(0, firstCommaIndex);
                    ingredient.Requirements = requirements;

                    var requirementAction = this._actionDetector.Actionify(requirements);

                    var step = new Step();
                    step.Id = stepId++;
                    step.SubrecipeId = RequirementsSubrecipeId;
                    step.Parts.Add(new TextPart { Value = string.Format("{0}: ", recipe.Subrecipes.Single(x => x.Id == subrecipeId).Title) });
                    step.Parts.Add(new ActionPart { Value = requirementAction });
                    step.Parts.Add(new IngredientPart { Ingredient = ingredient });

                    recipe.Steps.Add(step);
                }
                else
                {
                    ingredient.Name = ingredientPhrase;
                }

                Console.WriteLine("Found ingredient: " + ingredient.Name);

                recipe.Ingredients.Add(ingredient);

                ingredientId++;
            }
        }

        private void FlushPhrasePart(QuickRecipe recipe, Step step, StringBuilder phraseBuilder, Type readType)
        {
            if (phraseBuilder.Length > 0)
            {
                var value = phraseBuilder.ToString().Trim()
                    .Replace(" ,", ",")
                    .Replace(" .", ".")
                    .Replace("’ ", "’")
                    .Replace("' ", "'");

                phraseBuilder.Clear();

                if (readType == typeof(ActionPart))
                {
                    step.Parts.Add(new ActionPart { Value = value });
                }
                else if (readType == typeof(TimerPart))
                {
                    var previousAction = step.Parts.Last(x => x is ActionPart) as ActionPart;
                    var text = value.Split('{', '}')[1];
                    var timerValue = value.Replace(text, string.Empty).Replace("{", string.Empty).Replace("}", string.Empty);
                    step.Parts.Add(new TimerPart { Action = previousAction.Value, Value = timerValue, Text = text });
                }
                else if (readType == typeof(TextPart))
                {
                    step.Parts.Add(new TextPart { Value = value });
                }
                else if (readType == typeof(IngredientPart))
                {
                    var referencedIngredient = recipe.Ingredients.First(x => x.Id == int.Parse(value));
                    step.Parts.Add(new IngredientPart { Ingredient = referencedIngredient });
                }
                else if (readType == typeof(IngredientEnumerationPart))
                {
                    var ingredients = new IngredientEnumerationPart();
                    var ingredientIds = value.Split(',');
                    foreach (var ingredientId in ingredientIds)
                    {
                        var referencedIngredient = recipe.Ingredients.First(x => x.Id == int.Parse(ingredientId));
                        ingredients.Ingredients.Add(referencedIngredient);
                    }

                    step.Parts.Add(ingredients);
                }
            }
        }
    }
}