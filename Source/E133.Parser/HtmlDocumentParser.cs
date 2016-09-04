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
        private readonly Regex _quantityRangeExpression;
        private readonly Regex _ingredientExpression;
        private readonly Regex _ingredientFullExpression;
        private readonly Regex _ingredientUnitExpression;

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
            this._quantityExpression = new Regex(@"[\xbc-\xbe\w]+[\xbc-\xbe\w'’,./]*", RegexOptions.Compiled);
            this._quantityRangeExpression = new Regex(@"((ou|à) [\xbc-\xbe0-9]+[\xbc-\xbe0-9'’,./]* )", RegexOptions.Compiled);
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
            
            recipe.NormalImageUrl = this.GetNormalImageUrl(document);
            Console.WriteLine("Found normal image: " + recipe.NormalImageUrl);
            
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
                .Select(x => new Subrecipe { SubrecipeId = x.Key, Title = x.Value })
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
                            SubrecipeId = subrecipeIndex,
                            Title = title
                        });

                    Console.WriteLine("Found subrecipe: " + title);

                    var subrecipeIngredientNodes = this.GetSubrecipeIngredientNodesFromParent(subrecipeNode);
                    this.CreateIngredientsFromNodes(subrecipeIngredientNodes, recipe, subrecipeIndex, ref ingredientId, ref stepId);
                }
            }
            // TODO Check if recipes exist with subrecipes AND orphans
            else
            {
                var ingredientsSection = this.GetIngredientSection(document);
                var orphanIngredientNodes = this.GetIngredientNodesFromParent(ingredientsSection);
                this.CreateIngredientsFromNodes(orphanIngredientNodes, recipe, PreparationSubrecipeId, ref ingredientId, ref stepId);
            }

            var stepSubrecipeNodes = this.GetStepSections(document);
            foreach (var stepSubrecipeNode in stepSubrecipeNodes)
            {
                // TODO Maybe we should order steps, in case a step subrecipe doesn't have the same text as the ingredient subrecipes
                var subrecipe = recipe.Subrecipes.SingleOrDefault(x => x.Title == stepSubrecipeNode.InnerText.Trim());
                var subrecipeId = subrecipe != null ? subrecipe.SubrecipeId : PreparationSubrecipeId;

                Console.WriteLine("Stepping through subrecipe: " + stepSubrecipeNode.InnerText.Trim());
                var stepNodes = this.GetSubrecipeSteps(stepSubrecipeNode);
                if (stepNodes != null)
                {
                    foreach (var stepNode in stepNodes)
                    {
                        var stepText = stepNode.InnerText.Trim();
                        // TODO Temp fix, localize and do better
                        var splitPhrases = stepText.Replace("c. à", "c à").Split('.').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                        foreach (var splitPhrase in splitPhrases)
                        {
                            var step = new Step { StepId = stepId++, SubrecipeId = subrecipeId };
                            var words = splitPhrase.SplitPhrase();
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

                                var word = words[index];
                                var previouslyReadType = currentlyReadType;

                                var enumerationPartResult = this.TryParseEnumerationPart(words, index, recipe.Ingredients, subrecipeId);
                                if(enumerationPartResult.IsEnumerationPart)
                                {
                                    currentlyReadType = typeof(IngredientEnumerationPart);
                                    word = string.Join(",", enumerationPartResult.IngredientIds);
                                    skippedIndexes.AddRange(enumerationPartResult.SkippedIndexes);
                                }
                                else
                                {
                                    var ingredientPartResult = this.TryParseIngredientPart(words, index, recipe.Ingredients, subrecipeId);
                                    if (ingredientPartResult.IsIngredientPart)
                                    {
                                        currentlyReadType = typeof(IngredientPart);
                                        word = ingredientPartResult.IngredientId.ToString();
                                        skippedIndexes.AddRange(ingredientPartResult.SkippedIndexes);
                                    }
                                    else
                                    {
                                        var timerPartResult = this.TryParseTimerPart(words, index);
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

        internal void InitializeCulture(string language)
        {
            this._recipeCulture = new CultureInfo(language);

            this._actionDetector = this._actionDetectorFactory(this._recipeCulture);
            this._timerDetector = this._timerDetectorFactory(this._recipeCulture);
            this._measureUnitDetector = this._measureUnitDetectorFactory(this._recipeCulture);
            this._generalLanguageHelper = this._languageHelperFactory(this._recipeCulture);
            this._subrecipeRepository = this._subrecipeRepositoryFactory(this._recipeCulture);
        }

        internal Ingredient ParseIngredientFromString(string originalString)
        {
            double quantity;
            var ingredientString = originalString.Replace("\t", " ");
            ingredientString = this._quantityRangeExpression.Replace(ingredientString, string.Empty, 1);

            var matches = this._quantityExpression.Matches(ingredientString);
            var quantityString = matches[0].Value;
            var hasQuantity = this._generalLanguageHelper.TryParseNumber(quantityString, this._recipeCulture, out quantity);

            MeasureUnit measureUnit = MeasureUnit.Unit;
            if (hasQuantity)
            {
                var ingredientStringWithoutQuantity = ingredientString.Replace($"{quantityString} ", string.Empty);
                foreach (var enumValue in Enum.GetValues(typeof(MeasureUnit)).Cast<MeasureUnit>())
                {
                    var possibleStrings = this._measureUnitDetector.MeasureUnitsInString[enumValue];
                    if (possibleStrings.Any(x => ingredientStringWithoutQuantity.StartsWith($"{x} "))) 
                    {
                        measureUnit = enumValue;
                        break;
                    }
                }
            }

            Match ingredientMatch;
            switch (measureUnit)
            {
                case MeasureUnit.Unit: 
                    ingredientMatch = hasQuantity
                        ? this._ingredientUnitExpression.Match(ingredientString)
                        : this._ingredientFullExpression.Match(ingredientString);
                    break;
                default:
                    ingredientMatch = this._ingredientExpression.Match(ingredientString);
                    break; 
            }

            var ingredientPhrase = ingredientMatch.Value.Trim();
            string name = ingredientPhrase;
            string requirements = null;

            // If there is a comma, it's because there is a requirement next to the ingredient.
            var firstCommaIndex = ingredientPhrase.IndexOf(",");
            if (firstCommaIndex > -1)
            {
                name = ingredientPhrase.Substring(0, firstCommaIndex);
                requirements = ingredientPhrase.Substring(firstCommaIndex + 2, ingredientPhrase.Length - firstCommaIndex - 2);
            }

            return new Ingredient
            {
                Name = name,
                Quantity = new Quantity { Value = quantity, Abbreviation = measureUnit },
                Requirements = requirements
            };
        }

        internal ParseTimerPartResult TryParseTimerPart(IList<string> phrase, int index)
        {
            double time;
            var word = phrase[index].Trim();
            if (this._generalLanguageHelper.TryParseNumber(word, out time))
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
                    if (this._generalLanguageHelper.TryParseNumber(word, out time))
                    {
                        if (!rangeDuration)
                        {
                            durationBuilder.Append(time);
                        }
                        skippedIndexes.Add(index);
                    }
                    // TODO Localize this
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
                        if (this._generalLanguageHelper.TryParseNumber(durationString.Last().ToString(), out time) && !durationString.Contains(qualifier))
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
                    if (this._generalLanguageHelper.TryParseNumber(durationString.Last().ToString(), out time))
                    {
                        var lastChar = durationString.Last(x => !this._generalLanguageHelper.TryParseNumber(x.ToString(), out time));
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

                    return new ParseTimerPartResult 
                    { 
                        IsTimerPart = true, 
                        SkippedIndexes = skippedIndexes, 
                        OutputFormat = outputFormat, 
                        OutputValue = outputValue 
                    };
                }
            }

            return ParseTimerPartResult.NegativeResult;
        }

        internal ParseIngredientPartResult TryParseIngredientPart(IList<string> phrase, int index, IEnumerable<Ingredient> ingredients, int subrecipeId)
        {
            var word = phrase[index].Trim();
            if (this._generalLanguageHelper.IsDeterminant(word))
            {
                var skippedIndexes = new List<int> { index };
                index++;
                while (index < phrase.Count)
                {
                    word = phrase[index].Trim();

                    var referencedIngredient = ingredients.FirstOrDefault(x => x.Name.Contains(word) && x.SubrecipeId == subrecipeId);
                    if (referencedIngredient != null)
                    {
                        skippedIndexes.Add(index);

                        return new ParseIngredientPartResult 
                        { 
                            IsIngredientPart = true, 
                            SkippedIndexes = skippedIndexes, 
                            IngredientId = referencedIngredient.IngredientId 
                        };
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return ParseIngredientPartResult.NegativeResult;
        }

        internal ParseEnumerationPartResult TryParseEnumerationPart(IList<string> phrase, int index, IEnumerable<Ingredient> ingredients, int subrecipeId)
        {
            var ingredientIds = new List<long>();
            var skippedIndexes = new List<int>();
            var word = phrase[index].Trim();

            // TODO Localize this
            if (index + 2 < phrase.Count
                && phrase[index].Trim() == "tous"
                && phrase[index + 1].Trim() == "les"
                && phrase[index + 2].Trim() == "ingrédients")
            {
                skippedIndexes.Add(index);
                skippedIndexes.Add(index + 1);
                skippedIndexes.Add(index + 2);

                return new ParseEnumerationPartResult 
                { 
                    IsEnumerationPart = true, 
                    SkippedIndexes = skippedIndexes, 
                    IngredientIds = ingredients.Where(x => x.SubrecipeId == subrecipeId).Select(x => x.IngredientId).ToList() 
                };
            }

            var ingredientPartResult = this.TryParseIngredientPart(phrase, index, ingredients, subrecipeId);
            if (ingredientPartResult.IsIngredientPart)
            {
                ingredientIds.Add(ingredientPartResult.IngredientId);
                skippedIndexes.AddRange(ingredientPartResult.SkippedIndexes);
                index++;
                while (index < phrase.Count)
                {
                    // When we find an ingredient, the future indexes are included in the skippedIndexes, so when the current
                    // index is on this future skipped part, we skip it. 
                    if (skippedIndexes.Contains(index))
                    {
                        index++;
                        continue;
                    }

                    word = phrase[index].Trim();
                    // TODO Localize this
                    if (word == "," || word == "et")
                    {
                        if (index + 1 < phrase.Count)
                        {
                            ingredientPartResult = this.TryParseIngredientPart(phrase, index + 1, ingredients, subrecipeId);
                            if (ingredientPartResult.IsIngredientPart)
                            {
                                skippedIndexes.Add(index);
                            }
                        }
                    }
                    else
                    {
                        ingredientPartResult = this.TryParseIngredientPart(phrase, index, ingredients, subrecipeId);
                        if (ingredientPartResult.IsIngredientPart)
                        {
                            ingredientIds.Add(ingredientPartResult.IngredientId);
                            skippedIndexes.AddRange(ingredientPartResult.SkippedIndexes);
                        }
                        else
                        {
                            break;
                        }
                    }

                    index++;
                }
            }

            if (ingredientIds.Count > 1)
            {
                return new ParseEnumerationPartResult 
                {
                    IsEnumerationPart = true, 
                    SkippedIndexes = skippedIndexes, 
                    IngredientIds = ingredientIds 
                };                
            }

            return ParseEnumerationPartResult.NegativeResult;
        }

        private async Task<HtmlDocument> LoadDocument(Uri uri)
        {
            var content = await this._htmlLoader.ReadHtmlAsync(uri);
            
            var document = new HtmlDocument();
            
            document.LoadHtml(System.Net.WebUtility.HtmlDecode(content));

            var language = this.GetRecipeIetfLanguage(document);
            this.InitializeCulture(language);

            return document;
        }

        private static void ClearUnusedSubrecipe(QuickRecipe recipe, int subrecipeId)
        {
            if (recipe.Steps.All(x => x.SubrecipeId != subrecipeId))
            {
                var subrecipe = recipe.Subrecipes.Single(x => x.SubrecipeId == subrecipeId);
                recipe.Subrecipes.RemoveAt(recipe.Subrecipes.IndexOf(subrecipe));
            }
        }

        private void CreateIngredientsFromNodes(IEnumerable<HtmlNode> ingredientNodes, QuickRecipe recipe, int currentSubrecipeId, ref int currentIngredientId, ref int currentStepId)
        {
            var ingredientStrings = ingredientNodes.Select(x => x.InnerText.Trim()).ToList();
            foreach (var ingredientString in ingredientStrings)
            {
                Console.WriteLine("Found ingredient string: " + ingredientString);  

                var ingredient = this.ParseIngredientFromString(ingredientString);
                ingredient.IngredientId = currentIngredientId++;
                ingredient.SubrecipeId = currentSubrecipeId;

                if (!string.IsNullOrEmpty(ingredient.Requirements))
                {
                    var requirementAction = this._actionDetector.Actionify(ingredient.Requirements);

                    var step = new Step();
                    step.StepId = currentStepId++;
                    step.SubrecipeId = RequirementsSubrecipeId;
                    step.Parts.Add(new TextPart { Value = string.Format("{0}: ", recipe.Subrecipes.Single(x => x.SubrecipeId == currentSubrecipeId).Title) });
                    step.Parts.Add(new ActionPart { Value = requirementAction });
                    step.Parts.Add(new IngredientPart { Ingredient = ingredient });

                    recipe.Steps.Add(step);
                }
                
                recipe.Ingredients.Add(ingredient);
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
                    var referencedIngredient = recipe.Ingredients.First(x => x.IngredientId == int.Parse(value));
                    step.Parts.Add(new IngredientPart { Ingredient = referencedIngredient });
                }
                else if (readType == typeof(IngredientEnumerationPart))
                {
                    var ingredients = new IngredientEnumerationPart();
                    var ingredientIds = value.Split(',');
                    foreach (var ingredientId in ingredientIds)
                    {
                        var referencedIngredient = recipe.Ingredients.First(x => x.IngredientId == int.Parse(ingredientId));
                        ingredients.Ingredients.Add(referencedIngredient);
                    }

                    step.Parts.Add(ingredients);
                }
            }
        }
    }
}