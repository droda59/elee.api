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
    internal abstract class HtmlDocumentParser<TBase> : IHtmlParser
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
        private readonly Func<CultureInfo, IIngredientDetector> _ingredientDetectorFactory;
        private readonly Func<CultureInfo, IMeasureUnitDetector> _measureUnitDetectorFactory;
        private readonly Func<CultureInfo, ILanguageHelper> _languageHelperFactory;
        private readonly Func<CultureInfo, ISubrecipeRepository> _subrecipeRepositoryFactory;

        private IActionDetector _actionDetector;
        private ITimerDetector _timerDetector;
        private IIngredientDetector _ingredientDetector;
        private IMeasureUnitDetector _measureUnitDetector;
        private ILanguageHelper _generalLanguageHelper;
        private ISubrecipeRepository _subrecipeRepository;

        private CultureInfo _recipeCulture;

        protected HtmlDocumentParser(
            IHtmlLoader htmlLoader,
            Func<CultureInfo, IActionDetector> actionDetectorFactory, 
            Func<CultureInfo, ITimerDetector> timerDetectorFactory,
            Func<CultureInfo, IIngredientDetector> ingredientDetectorFactory,
            Func<CultureInfo, IMeasureUnitDetector> measureUnitDetectorFactory,
            Func<CultureInfo, ILanguageHelper> languageHelperFactory,
            Func<CultureInfo, ISubrecipeRepository> subrecipeRepositoryFactory)
        {
            this._htmlLoader = htmlLoader;
            this._actionDetectorFactory = actionDetectorFactory;
            this._timerDetectorFactory = timerDetectorFactory;
            this._ingredientDetectorFactory = ingredientDetectorFactory;
            this._measureUnitDetectorFactory = measureUnitDetectorFactory;
            this._languageHelperFactory = languageHelperFactory;
            this._subrecipeRepositoryFactory = subrecipeRepositoryFactory;

            this.Base = new TBase();
            
            // TODO Localize and put somewhere else
            this._wordExpression = new Regex(@"[\w()°]+['’]*|[,]|[\)]\b", RegexOptions.Compiled);
            this._quantityExpression = new Regex(@"[\xbc-\xbe\w]+[\xbc-\xbe\w'’,./]*", RegexOptions.Compiled);
            this._ingredientExpression = new Regex(@"(?<=[a-zA-Z0-9\u00C0-\u017F\s()'’\-\/%] de | d'| d’)([a-zA-Z0-9\u00C0-\u017F\s()'’\-\/%]+)(, [,\w\s]+)*", RegexOptions.Compiled);
            this._ingredientFullExpression = new Regex(@"([a-zA-Z0-9\u00C0-\u017F\s()'’\-\/%]+)(, [,\w\s]+)*", RegexOptions.Compiled);
            this._ingredientUnitExpression = new Regex(@"(?<=[a-zA-Z0-9\u00C0-\u017F\s()'’\-\/%])([a-zA-Z0-9\u00C0-\u017F\s()'’\-\/%]+)(, [,\w\s]+)*", RegexOptions.Compiled);
        }

        public IBase Base { get; }

        public virtual bool IsRecipePage(Uri uri) 
        {
            return true;
        }

        public async Task<QuickRecipe> ParseHtmlAsync(Uri uri)
        {
            var document = await this.LoadDocument(uri);

            // document.OptionDefaultStreamEncoding = Encoding.UTF8;

            var recipe = new QuickRecipe();
            recipe.Language = this.GetRecipeIetfLanguage(document);
            recipe.Note = this.GetNote(document);
            recipe.SmallImageUrl = this.GetImageUrl(document);
            recipe.OriginalUrl = uri.AbsoluteUri;
            recipe.OriginalServings = this.GetRecipeYield(document);
            recipe.Title = this.GetRecipeTitle(document);
            recipe.Durations = this.GetDurations(document).ToList();
            recipe.Subrecipes = this._subrecipeRepository.KnownSubrecipes
                .Select(x => new Subrecipe { Id = x.Key, Title = x.Value })
                .ToList();

            var ingredientId = 0;
            var stepId = 0;
            var subrecipeNodes = this.GetSubrecipeNodes(document);
            if (subrecipeNodes != null && subrecipeNodes.Any())
            {
                for (var subrecipeIndex = 0; subrecipeIndex < subrecipeNodes.Count(); subrecipeIndex++)
                {
                    var subrecipeNode = subrecipeNodes.ToArray()[subrecipeIndex];
                    recipe.Subrecipes.Add(
                        new Subrecipe
                        {
                            Id = subrecipeIndex,
                            Title = this.GetSubrecipeTitle(subrecipeNode)
                        });

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

                            if (this.LookAheadIngredientEnumerationStepPart(word, words, index, recipe, subrecipeId, skippedIndexes, ref word))
                            {
                                currentlyReadType = typeof(IngredientEnumerationPart);
                            }
                            else if (this.TryParseIngredientStepPart(word, words, index, recipe, subrecipeId, ref word))
                            {
                                currentlyReadType = typeof(IngredientPart);
                                skippedIndexes.Add(index + 1);
                            }
                            else if (this.TryParseTimerStepPart(word, words, index, ref word))
                            {
                                currentlyReadType = typeof(TimerPart);
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

                            if (previouslyReadType != null && previouslyReadType != currentlyReadType)
                            {
                                this.FlushPhrasePart(recipe, step, phraseBuilder, previouslyReadType);
                            }

                            phraseBuilder.AppendFormat("{0} ", word);

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

        protected async Task<string> LoadHtmlAsync(Uri uri)
        {
            return await this._htmlLoader.ReadHtmlAsync(uri);
        }

        protected abstract string GetRecipeIetfLanguage(HtmlDocument document);

        protected abstract string GetRecipeTitle(HtmlDocument document);

        protected abstract string GetImageUrl(HtmlDocument document);

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
            var content = await this.LoadHtmlAsync(uri);
            // var content = await this.GetOfflineHtmlContent();
            
            var document = new HtmlDocument();
            
            document.LoadHtml(System.Net.WebUtility.HtmlDecode(content));

            this.InitializeCulture(document);

            return document;
        }

        private void InitializeCulture(HtmlDocument document)
        {
            var language = this.GetRecipeIetfLanguage(document);

            this._recipeCulture = new CultureInfo(language);

            this._actionDetector = this._actionDetectorFactory(this._recipeCulture);
            this._timerDetector = this._timerDetectorFactory(this._recipeCulture);
            this._ingredientDetector = this._ingredientDetectorFactory(this._recipeCulture);
            this._measureUnitDetector = this._measureUnitDetectorFactory(this._recipeCulture);
            this._generalLanguageHelper = this._languageHelperFactory(this._recipeCulture);
            this._subrecipeRepository = this._subrecipeRepositoryFactory(this._recipeCulture);
        }

        private bool LookAheadIngredientEnumerationStepPart(string word, MatchCollection words, int index, QuickRecipe recipe, int subrecipeId, IList<int> skippedIndexes, ref string result)
        {
            var ingredientIds = new List<string>();

            if (this.TryParseIngredientStepPart(word, words, index, recipe, subrecipeId, ref word))
            {
                ingredientIds.Add(word);
                skippedIndexes.Add(index + 1);
                index++;
                while (index < words.Count)
                {
                    if (skippedIndexes.Contains(index))
                    {
                        index++;
                        continue;
                    }

                    word = words[index].Value.Trim();
                    // TODO Localize this
                    // TODO Verify future next word, because the rest of the sentence could start with this
                    if (word == "," || word == "et")
                    {
                        skippedIndexes.Add(index);
                    }
                    else if (this.TryParseIngredientStepPart(word, words, index, recipe, subrecipeId, ref word))
                    {
                        ingredientIds.Add(word);
                        skippedIndexes.Add(index);
                        skippedIndexes.Add(index + 1);
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

        private bool TryParseTimerStepPart(string word, MatchCollection words, int index, ref string result)
        {
            int time;
            if (int.TryParse(word, out time) && index + 1 < words.Count)
            {
                var nextWord = words[index + 1].Value.Trim();

                if (this._timerDetector.IsTimeQualifier(nextWord))
                {
                    result = this._timerDetector.Timerify(time, nextWord);

                    return true;
                }
            }

            return false;
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

                recipe.Ingredients.Add(ingredient);

                ingredientId++;
            }
        }

        private void FlushPhrasePart(QuickRecipe recipe, Step step, StringBuilder phraseBuilder, Type readType)
        {
            if (phraseBuilder.Length > 0)
            {
                var value = phraseBuilder.ToString().Trim();
                phraseBuilder.Clear();

                if (readType == typeof(ActionPart))
                {
                    step.Parts.Add(new ActionPart { Value = value });
                }
                else if (readType == typeof(TimerPart))
                {
                    var previousAction = step.Parts.Last(x => x is ActionPart) as ActionPart;
                    step.Parts.Add(new TimerPart { Action = previousAction.Value, Value = value, Text = "" });
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

        private Task<string> GetOfflineHtmlContent()
        {
            return Task<string>.Factory.StartNew(() => @"
<html lang='fr' class='js rgba multiplebgs borderradius boxshadow textshadow opacity cssgradients csstransforms csstransforms3d video'><!--<![endif]--><head><script type='text/javascript' src='//widgets.pinterest.com/v1/urls/count.json?url=http%3A%2F%2Fwww.ricardocuisine.com%2Frecettes%2F5409-pouding-au-chocolat&amp;callback=window._ate.cbs.rcb_httpwwwricardocuisine5'></script><script type='text/javascript' src='https://cdn.api.twitter.com/1/urls/count.json?url=http%3A%2F%2Fwww.ricardocuisine.com%2Frecettes%2F5409-pouding-au-chocolat&amp;callback=_ate.cbs.rcb_httpwwwricardocuisine4'></script><script type='text/javascript' src='//graph.facebook.com/fql?q=SELECT url, normalized_url, share_count, like_count, comment_count, total_count,commentsbox_count, comments_fbid, click_count FROM link_stat WHERE url=&quot;http%3A%2F%2Fwww.ricardocuisine.com%2Frecettes%2F5409-pouding-au-chocolat&quot;&amp;callback=_ate.cbs.rcb_httpwwwricardocuisine3'></script><script type='text/javascript' src='//widgets.pinterest.com/v1/urls/count.json?url=http%3A%2F%2Fwww.ricardocuisine.com%2Frecettes%2F5409-pouding-au-chocolat&amp;callback=window._ate.cbs.rcb_httpwwwricardocuisine2'></script><script type='text/javascript' src='https://cdn.api.twitter.com/1/urls/count.json?url=http%3A%2F%2Fwww.ricardocuisine.com%2Frecettes%2F5409-pouding-au-chocolat&amp;callback=_ate.cbs.rcb_httpwwwricardocuisine1'></script><script type='text/javascript' src='//graph.facebook.com/fql?q=SELECT url, normalized_url, share_count, like_count, comment_count, total_count,commentsbox_count, comments_fbid, click_count FROM link_stat WHERE url=&quot;http%3A%2F%2Fwww.ricardocuisine.com%2Frecettes%2F5409-pouding-au-chocolat&quot;&amp;callback=_ate.cbs.rcb_httpwwwricardocuisine0'></script><script type='text/javascript' async='' src='http://www.google-analytics.com/plugins/ua/ecommerce.js'></script><script type='text/javascript' src='//s7.addthis.com/static//lang/fr.js'></script>
	<!--rmediaweb3-->
		<base href='http://www.ricardocuisine.com'>
  	<meta charset='utf-8'>
	<title>Pouding au chocolat Recettes | Ricardo</title>
    <meta name='viewport' content='width=1024'>
	<meta http-equiv='X-UA-Compatible' content='IE=7, IE=9, IE=10, IE=edge'>
	<meta name='description' content='Pouding au chocolat'>
	<meta name='author' content='Ricardocuisine'>
	<link rel='alternate' media='only screen and (max-width: 640px)' href='http://m.ricardocuisine.com/recette-detaillee.php?id=5409&amp;lang=fr'><link rel='alternate' href='http://www.ricardocuisine.com/recettes/5409-pouding-au-chocolat' hreflang='fr-ca'><link rel='alternate' href='http://www.ricardocuisine.com/recipes/5409-chocolate-pudding-cake' hreflang='en-ca'>	
	<meta property='og:title' content='Pouding au chocolat'> 
    
	<meta property='og:image' content='http://www.ricardocuisine.com/pictures/cache/f42e29ee36a20b495128fd3f33792edc_w1200_h630_cp_sc.jpg'>
	
	<meta property='og:site_name' content='Ricardocuisine'>
	<meta property='og:url' content='http://www.ricardocuisine.com/recettes/5409-pouding-au-chocolat'>
	<meta property='fb:admins' content='1648309544'>	
		
	<meta name='twitter:card' content='summary_large_image'>
	<meta name='twitter:site' content='@Ricardocuisine'>
	<meta name='twitter:creator' content='@Ricardocuisine'>
	<meta name='twitter:title' content='Pouding au chocolat'>
	<meta name='twitter:description' content='Recette de Ricardo de pouding au chocolat'>
	<meta name='twitter:image:src' content='http://www.ricardocuisine.com/pictures/cache/f42e29ee36a20b495128fd3f33792edc_w1200_h630_cp_sc.jpg'>
	
<body class='skin01 fr recipeDetail'><div id='yass_top_edge_dummy' style='width: 1px; height: 1px; padding: 0px; margin: -17px 0px 0px; border-width: 0px; display: block;'></div><div id='yass_top_edge' style='padding: 0px; margin: 0px; border-width: 0px; height: 0px; display: block; width: 1px; background-image: url(chrome-extension://khpcanbeojalbkpgpmjpdkjnkfcgfkhb/edgebgtop.png); background-attachment: scroll; background-position: 50% 100%;'></div><div id='yass_top_edge_padding' style='padding: 0px 0px 16px; margin: 0px; border-width: 0px; height: 0px; display: block;'></div>

<div class='wrapper' id='top'>
<div class='wrapHeader'>
    <header class='subWrap'>
        <a href='fr' title='Aller à Ricardocuisine.com' class='brand'>
            <img src='global/global_5649/img/lg_ricardocuisine.gif' width='227' height='68' alt='Ricardocuisine.com'>
        </a>
        
        <form action='search_tempo.php' method='post' class='searchForm' id='topFilterForm'>
            <div class='frmElmnt fTxt'>
                <div class='fLbl'><label for='searchInput'>Recherche</label></div>
                <div class='fWdgt'>
                    <input type='text' name='searchInput' id='searchInput' autocomplete='off' placeholder='Recherche' value=''>
                    
					<input type='hidden' name='lang' value='fr'>
                    <button class='btn' type='submit' value='Submit'>Lancer la recherhe</button>
                </div>
				
                <div class='clearer'><!-- --></div>
            </div>
        </form>
		
        <div class='topNav'>
            <ul>
                <li><a href='/magazine' title='Magazine'>Magazine</a></li>
                <li><a href='/vins' title='Vins'>Vins</a></li>
                <li><a href='/boutique' title='Boutique'>Boutique</a></li>
                <li><a href='recipes/5409-chocolate-pudding-cake' title='English'>English</a></li>
            </ul>
        </div>
        
        <div class='userNav'>
        	<p>
        	           <a href='connexion' class='tglTrig popover-trigger' id='login' data-red='mes-recettes' data-redirect='/recettes/5409-pouding-au-chocolat' data-action='login' data-placement='bottom' data-class='unLogin popover-login-header' data-original-title='Connexion'>Mon Espace</a>
                      </p>
        </div>
		
        <nav>
            <ul class='primaryNav'>
                <li><a href='fr' title='Accueil'>Accueil</a></li>
                <li class='active'><a href='recettes' title='Recettes' class='mmTrig hoverselect' id='mmTrigRecipes'>Recettes</a></li>
                <li><a href='themes' title='Thèmes' class='mmTrig hoverselect' id='mmTrigThemes'>Thèmes</a></li>
                <li><a href='chroniques' title='Chroniques'>Chroniques</a></li>
                <li><a href='videos' title='Vidéos'>Vidéos</a></li>
			</ul>
                        <div class='mask'>
            	                
                <div class='secondaryNav' id='mmContRecipes'>
                    <div id='mmSliderRecipes-wrapper' class='liquid-slider-wrapper liquid-responsive' style='width: 1006px; max-width: 1100px;'><div class='liquid-nav'><ul id='mmSliderRecipes-nav-ul' style='float: left;'><li class='tab1'>
       <a href='#1' title='Plats principaux' class='test current'>Plats principaux</a></li><li class='tab2'>
       <a href='#2' title='Entrées' class='test'>Entrées</a></li><li class='tab3'>
       <a href='#3' title='Desserts' class='test'>Desserts</a></li><li class='tab4'>
       <a href='#4' title='Ingrédients' class='test'>Ingrédients</a></li></ul></div><div class='liquid-slider' id='mmSliderRecipes' style='height: 214px; overflow: hidden;'>
                        
                        <div class='panel-container' style='margin-left: -1006px; width: 600%;'><div class='mmSliderRecipes-panel panel' style='width: 16.6666666666667%;'><div class='panel-wrapper'>
                            <span class='lsPanelTitle'>Ingrédients</span>
                            <div class='refLine'>
                            	                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
											                                            <li><a title='' href='recettes/ingredients/agneau/'>Agneau <span></span></a></li>
                                                                                        <li><a title='' href='recettes/ingredients/boeuf/'>Boeuf <span></span></a></li>
                                                                                        <li><a title='' href='recettes/ingredients/canard/'>Canard <span></span></a></li>
                                                                                        <li><a title='' href='recettes/ingredients/chocolat/'>Chocolat <span></span></a></li>
                                                                                        <li><a title='' href='recettes/ingredients/creme/'>Crème <span></span></a></li>
                                                                                        <li><a title='' href='recettes/ingredients/crevettes/'>Crevettes <span></span></a></li>
                                                                                        <li><a title='' href='recettes/ingredients/dinde/'>Dinde <span></span></a></li>
                                                                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
                                            											<li><a title='' href='recettes/ingredients/fromages/'>Fromages <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/fruits/'>Fruits <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/gibier/'>Gibier <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/homard/'>Homard <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/legumes/'>Légumes <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/legumineuses/'>Légumineuses <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/oeufs/'>Oeufs <span></span></a></li>
											                                        </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
                                            											<li><a title='' href='recettes/ingredients/pates/'>Pâtes <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/poisson/'>Poisson <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/porc/'>Porc <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/poulet/'>Poulet <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/saumon/'>Saumon <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/tofu/'>Tofu <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/veau/'>Veau <span></span></a></li>
											                                        </ul>
                                    </div>
                                </div>
                            </div>
                        </div></div><div class='mmSliderRecipes-panel panel' style='width: 16.6666666666667%;'><div class='panel-wrapper'>
                            <span class='lsPanelTitle'>Plats principaux</span>
                            <div class='refLine'>
                            	                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
                                        
                                        	                                            <li><a title='' href='recettes/agneau/'>Agneau <span>55</span></a></li> 
                                                                                        <li><a title='' href='recettes/boeuf/'>Boeuf <span>179</span></a></li> 
                                                                                        <li><a title='' href='recettes/canard/'>Canard <span>34</span></a></li> 
                                                                                        <li><a title='' href='recettes/dejeuners-brunch/'>Déjeuners/brunch <span>87</span></a></li> 
                                                                                        <li><a title='' href='recettes/dinde/'>Dinde <span>39</span></a></li> 
                                                                                        <li><a title='' href='recettes/fondues/'>Fondues <span>13</span></a></li> 
                                                                                        <li><a title='' href='recettes/gibier/'>Gibier <span>29</span></a></li> 
                                                                                        
                                        </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
                                                                                        <li><a title='' href='recettes/legumineuses/'>Légumineuses <span>24</span></a></li>
                                                                                        <li><a title='' href='recettes/oeufs/'>Oeufs <span>48</span></a></li>
                                                                                        <li><a title='' href='recettes/oies-pintades-et-autres-volailles/'>Oies, pintades et autres volailles <span>11</span></a></li>
                                                                                        <li><a title='' href='recettes/pates-alimentaires/'>Pâtes alimentaires <span>238</span></a></li>
                                                                                        <li><a title='' href='recettes/pizzas/'>Pizzas <span>51</span></a></li>
                                                                                        <li><a title='' href='recettes/poissons-et-fruits-de-mer/'>Poissons et fruits de mer <span>293</span></a></li>
                                                                                        <li><a title='' href='recettes/porc/'>Porc <span>220</span></a></li>
                                                                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
                                                                                        <li><a title='' href='recettes/poulet/'>Poulet <span>236</span></a></li>
                                                                                        <li><a title='' href='recettes/quiches-et-tartes-salees/'>Quiches et tartes salées <span>48</span></a></li>
                                                                                        <li><a title='' href='recettes/riz-risotto-et-orge/'>Riz, risotto et orge <span>72</span></a></li>
                                                                                        <li><a title='' href='recettes/sandwichs/'>Sandwichs <span>133</span></a></li>
                                                                                        <li><a title='' href='recettes/tofu-soya-et-cie/'>Tofu, soya et Cie <span>33</span></a></li>
                                                                                        <li><a title='' href='recettes/veau/'>Veau <span>74</span></a></li>
                                                                                        <li><a title='' href='recettes/vegetarien/'>Végétarien <span>161</span></a></li>
                                                                                    </ul>
                                    </div>
                                </div>
                            </div>
                        </div></div><div class='mmSliderRecipes-panel panel' style='width: 16.6666666666667%;'><div class='panel-wrapper'>
                            <span class='lsPanelTitle'>Entrées</span>
                            <div class='refLine'>
                            	                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
                                                                                        <li><a title='' href='recettes/boissons-et-cocktails/'>Boissons et cocktails <span>190</span></a></li>
                                                                                        <li><a title='' href='recettes/conserves-et-ketchups/'>Conserves et ketchups <span>54</span></a></li>
                                                                                        <li><a title='' href='recettes/entrees-et-amuse-gueules/'>Entrées et amuse-gueules <span>381</span></a></li>
                                                                                        <li><a title='' href='recettes/legumes-et-gratins/'>Légumes et gratins <span>296</span></a></li>
                                                                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
                                                                                        <li><a title='' href='recettes/marinades-pour-bbq/'>Marinades pour BBQ <span>28</span></a></li>
                                                                                        <li><a title='' href='recettes/pains-sales/'>Pains salés <span>25</span></a></li>
                                                                                        <li><a title='' href='recettes/salades-vinaigrettes-et-mayonnaises/'>Salades, vinaigrettes et mayonnaises <span>310</span></a></li>
                                                                                        <li><a title='' href='recettes/sauces-beurres-et-pestos/'>Sauces, beurres et pestos <span>112</span></a></li>
                                                                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
                                                                                        <li><a title='' href='recettes/soupes-et-potages/'>Soupes et potages <span>262</span></a></li>
                                                                                        <li><a title='' href='recettes/trempettes-et-tartinades-salees/'>Trempettes et tartinades salées <span>49</span></a></li>
                                                                                    </ul>
                                    </div>
                                </div>
                            </div>
                        </div></div><div class='mmSliderRecipes-panel panel' style='width: 16.6666666666667%;'><div class='panel-wrapper'>
                            <span class='lsPanelTitle'>Desserts</span>
                            <div class='refLine'>
                            	                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                        										<li><a title='' href='recettes/barres-et-carres/'>Barres et carrés <span>33</span></a></li>
																				<li><a title='' href='recettes/biscuits/'>Biscuits <span>113</span></a></li>
																				<li><a title='' href='recettes/bonbons-chocolats-et-friandises/'>Bonbons, chocolat et friandises <span>110</span></a></li>
																				<li><a title='' href='recettes/brownies/'>Brownies <span>19</span></a></li>
																				<li><a title='' href='recettes/confitures-et-tartinades-sucrees/'>Confitures et tartinades sucrées <span>73</span></a></li>
																				<li><a title='' href='recettes/cremes-desserts-mousse-et-meringues/'>Crèmes dessert, mousses et meringues <span>114</span></a></li>
										                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                        										<li><a title='' href='recettes/crepes-et-pancakes/'>Crêpes et pancakes <span>23</span></a></li>
																				<li><a title='' href='recettes/croustades-et-croustillants/'>Croustades et croustillants <span>16</span></a></li>
																				<li><a title='' href='recettes/cupcakes/'>Cupcakes <span>26</span></a></li>
																				<li><a title='' href='recettes/desserts-glaces/'>Desserts glacés <span>87</span></a></li>
																				<li><a title='' href='recettes/fruits/'>Fruits <span>103</span></a></li>
																				<li><a title='' href='recettes/gateaux/'>Gâteaux <span>275</span></a></li>
										                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                        										<li><a title='' href='recettes/muffins-et-pains-desserts/'>Muffins et pains desserts <span>42</span></a></li>
																				<li><a title='' href='recettes/patisseries/'>Pâtisseries <span>82</span></a></li>
																				<li><a title='' href='recettes/poudings-et-tapiocas/'>Poudings et tapiocas <span>69</span></a></li>
																				<li><a title='' href='recettes/tartes/'>Tartes <span>147</span></a></li>
																				<li><a title='' href='recettes/yogourts-et-fromages/'>Yogourts et fromages <span>20</span></a></li>
										                                    </ul>
                                    </div>
                                </div>
                            </div>
                        </div></div><div class='mmSliderRecipes-panel panel' style='width: 16.6666666666667%;'><div class='panel-wrapper'>
                            <span class='lsPanelTitle'>Ingrédients</span>
                            <div class='refLine'>
                            	                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
											                                            <li><a title='' href='recettes/ingredients/agneau/'>Agneau <span></span></a></li>
                                                                                        <li><a title='' href='recettes/ingredients/boeuf/'>Boeuf <span></span></a></li>
                                                                                        <li><a title='' href='recettes/ingredients/canard/'>Canard <span></span></a></li>
                                                                                        <li><a title='' href='recettes/ingredients/chocolat/'>Chocolat <span></span></a></li>
                                                                                        <li><a title='' href='recettes/ingredients/creme/'>Crème <span></span></a></li>
                                                                                        <li><a title='' href='recettes/ingredients/crevettes/'>Crevettes <span></span></a></li>
                                                                                        <li><a title='' href='recettes/ingredients/dinde/'>Dinde <span></span></a></li>
                                                                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
                                            											<li><a title='' href='recettes/ingredients/fromages/'>Fromages <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/fruits/'>Fruits <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/gibier/'>Gibier <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/homard/'>Homard <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/legumes/'>Légumes <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/legumineuses/'>Légumineuses <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/oeufs/'>Oeufs <span></span></a></li>
											                                        </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
                                            											<li><a title='' href='recettes/ingredients/pates/'>Pâtes <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/poisson/'>Poisson <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/porc/'>Porc <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/poulet/'>Poulet <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/saumon/'>Saumon <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/tofu/'>Tofu <span></span></a></li>
																						<li><a title='' href='recettes/ingredients/veau/'>Veau <span></span></a></li>
											                                        </ul>
                                    </div>
                                </div>
                            </div>
                        </div></div><div class='mmSliderRecipes-panel panel' style='width: 16.6666666666667%;'><div class='panel-wrapper'>
                            <span class='lsPanelTitle'>Plats principaux</span>
                            <div class='refLine'>
                            	                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
                                        
                                        	                                            <li><a title='' href='recettes/agneau/'>Agneau <span>55</span></a></li> 
                                                                                        <li><a title='' href='recettes/boeuf/'>Boeuf <span>179</span></a></li> 
                                                                                        <li><a title='' href='recettes/canard/'>Canard <span>34</span></a></li> 
                                                                                        <li><a title='' href='recettes/dejeuners-brunch/'>Déjeuners/brunch <span>87</span></a></li> 
                                                                                        <li><a title='' href='recettes/dinde/'>Dinde <span>39</span></a></li> 
                                                                                        <li><a title='' href='recettes/fondues/'>Fondues <span>13</span></a></li> 
                                                                                        <li><a title='' href='recettes/gibier/'>Gibier <span>29</span></a></li> 
                                                                                        
                                        </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
                                                                                        <li><a title='' href='recettes/legumineuses/'>Légumineuses <span>24</span></a></li>
                                                                                        <li><a title='' href='recettes/oeufs/'>Oeufs <span>48</span></a></li>
                                                                                        <li><a title='' href='recettes/oies-pintades-et-autres-volailles/'>Oies, pintades et autres volailles <span>11</span></a></li>
                                                                                        <li><a title='' href='recettes/pates-alimentaires/'>Pâtes alimentaires <span>238</span></a></li>
                                                                                        <li><a title='' href='recettes/pizzas/'>Pizzas <span>51</span></a></li>
                                                                                        <li><a title='' href='recettes/poissons-et-fruits-de-mer/'>Poissons et fruits de mer <span>293</span></a></li>
                                                                                        <li><a title='' href='recettes/porc/'>Porc <span>220</span></a></li>
                                                                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul>
                                                                                        <li><a title='' href='recettes/poulet/'>Poulet <span>236</span></a></li>
                                                                                        <li><a title='' href='recettes/quiches-et-tartes-salees/'>Quiches et tartes salées <span>48</span></a></li>
                                                                                        <li><a title='' href='recettes/riz-risotto-et-orge/'>Riz, risotto et orge <span>72</span></a></li>
                                                                                        <li><a title='' href='recettes/sandwichs/'>Sandwichs <span>133</span></a></li>
                                                                                        <li><a title='' href='recettes/tofu-soya-et-cie/'>Tofu, soya et Cie <span>33</span></a></li>
                                                                                        <li><a title='' href='recettes/veau/'>Veau <span>74</span></a></li>
                                                                                        <li><a title='' href='recettes/vegetarien/'>Végétarien <span>161</span></a></li>
                                                                                    </ul>
                                    </div>
                                </div>
                            </div>
                        </div></div></div>
                        
                         
                                 
                        
                        
                        
                        
                    </div></div>
                
                </div>
                
                
                <div class='secondaryNav' id='mmContThemes'>
						
                    <div id='mmSliderTheme-wrapper' class='liquid-slider-wrapper liquid-responsive' style='width: 1006px; max-width: 1100px;'><div class='liquid-nav'><ul id='mmSliderTheme-nav-ul' style='float: left;'><li class='tab1'>
       <a href='#1' title='Thèmes' class='test current'>Thèmes</a></li><li class='tab2'>
       <a href='#2' title='Dossiers spéciaux' class='test'>Dossiers spéciaux</a></li><li class='tab3'>
       <a href='#3' title='Le 30' class='test'>Le 30</a></li><li class='tab4'>
       <a href='#4' title='Menus à la carte' class='test'>Menus à la carte</a></li><li class='tab5'>
       <a href='#5' title='Galeries' class='test'>Galeries</a></li></ul></div><div class='liquid-slider' id='mmSliderTheme' style='height: 298px; overflow: hidden;'>
                        
                        <div class='panel-container' style='margin-left: -1006px; width: 700%;'><div class='mmSliderTheme-panel panel' style='width: 14.2857142857143%;'><div class='panel-wrapper'>
                            <span class='lsPanelTitle'>Galeries</span>
                            <div class='refLine'>
								                                
                               <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                                                                <li><a title='' href='galeries/fraises'>Des fraises pour dessert!</a></li> 
                                                                                <li><a title='' href='galeries/5a7'>Jeudis 5 à 7</a></li> 
                                                                                
                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                                                                <li><a title='' href='galeries/homard'>Recevoir au homard</a></li> 
                                                                            </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                                                                <li><a title='' href='galeries/charlevoix'>Saveurs de Charlevoix</a></li> 
                                                                                
                                    </ul>
                                    </div>
                                </div>
                            </div>
						</div></div><div class='mmSliderTheme-panel panel' style='width: 14.2857142857143%;'><div class='panel-wrapper'>
                            <span class='lsPanelTitle'>Thèmes</span>
                            <div class='refLine'>
                            
                            	                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul> 
                                                                                        <li><a title='' href='themes/30-minutes/'>30 minutes<span>310</span></a></li> 
                                                                                        <li><a title='' href='themes/a-congeler/'>À congeler<span>562</span></a></li> 
                                                                                        <li><a title='' href='themes/action-de-grace/'>Action de grâce<span>101</span></a></li> 
                                                                                        <li><a title='' href='themes/BBQ/'>BBQ<span>317</span></a></li> 
                                                                                        <li><a title='' href='themes/cabane-a-sucre/'>Cabane à sucre<span>49</span></a></li> 
                                                                                        <li><a title='' href='themes/cadeaux-gourmands/'>Cadeaux gourmands<span>86</span></a></li> 
                                                                                        <li><a title='' href='themes/camping/'>Camping<span>19</span></a></li> 
                                                                                        <li><a title='' href='themes/comfort-food/'>Comfort food<span>448</span></a></li> 
                                                                                        <li><a title='' href='themes/cuisine-dete/'>Cuisine d'été<span>419</span></a></li> 
                                                                                        <li><a title='' href='themes/cuisine-du-monde/'>Cuisine du monde<span>471</span></a></li> 
                                                                                        
                                        </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                                                                    <li><a title='' href='themes/cuisine-pour-diabetiques/'>Cuisine pour diabétiques  <span>79</span></a></li>
                                                                                        <li><a title='' href='themes/fete-des-meres/'>Fête des Mères  <span>79</span></a></li>
                                                                                        <li><a title='' href='themes/fete-des-peres/'>Fête des Pères  <span>59</span></a></li>
                                                                                        <li><a title='' href='themes/halloween/'>Halloween  <span>36</span></a></li>
                                                                                        <li><a title='' href='themes/le-meilleur/'>Le Meilleur  <span>38</span></a></li>
                                                                                        <li><a title='' href='themes/lunchs/'>Lunchs  <span>173</span></a></li>
                                                                                        <li><a title='' href='themes/mijoteuse/'>Mijoteuse  <span>42</span></a></li>
                                                                                        <li><a title='' href='themes/noel/'>Noël  <span>409</span></a></li>
                                                                                        <li><a title='' href='themes/nouvel-an-chinois/'>Nouvel an chinois  <span>17</span></a></li>
                                                                                        <li><a title='' href='themes/paques/'>Pâques  <span>147</span></a></li>
                                                                                </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                                                                    <li><a title='' href='themes/pour-enfants/'>Pour enfants <span>239</span></a></li>
                                                                                        <li><a title='' href='themes/recettes-de-semaine/'>Recettes de semaine <span>536</span></a></li>
                                                                                        <li><a title='' href='themes/recettes-pour-2/'>Recettes pour 2 <span>54</span></a></li>
                                                                                        <li><a title='' href='themes/recettes-sur-la-plaque/'>Recettes sur la plaque <span>19</span></a></li>
                                                                                        <li><a title='' href='themes/saint-jean-baptiste/'>Saint-Jean-Baptiste <span>5</span></a></li>
                                                                                        <li><a title='' href='themes/saint-patrick/'>Saint-Patrick <span>12</span></a></li>
                                                                                        <li><a title='' href='themes/saint-valentin/'>Saint-Valentin <span>66</span></a></li>
                                                                                        <li><a title='' href='themes/sante/'>Santé / Choix Sain <span>334</span></a></li>
                                                                                    
                                    </ul>
                                    </div>
                                </div>
                             
                         
                            </div>
                        </div></div><div class='mmSliderTheme-panel panel' style='width: 14.2857142857143%;'><div class='panel-wrapper'>
                            <span class='lsPanelTitle'>Dossiers spéciaux</span>
                            <div class='refLine'>
                            	
                                                                
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                        										<li><a title='' href='actiondegrace'>Action de grâce</a></li> 
																				<li><a title='' href='apresski'>Après-ski</a></li> 
																				<li><a title='' href='congelation'>Congélation</a></li> 
																				<li><a title='' href='cuisineitalienne'>Cuisine italienne</a></li> 
																				<li><a title='' href='mieux-manger'>Défi mieux manger</a></li> 
																				<li><a title='' href='laitues'>Duel de laitues</a></li> 
																				<li><a title='' href='estival'>Estival</a></li> 
																				<li><a title='' href='femmes-enceintes'>Femmes enceintes</a></li> 
																				<li><a title='' href='fetedesmeres'>Fête des Mères</a></li> 
										                                        
                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                        										<li><a title='' href='fete-des-peres'>Fête des Pères</a></li> 
																				<li><a title='' href='/halloween'>Halloween</a></li> 
																				<li><a title='' href='japon'>Japon</a></li> 
																				<li><a title='' href='rentree'>La rentrée</a></li> 
																				<li><a title='' href='/mangerlocal'>Manger local</a></li> 
																				<li><a title='' href='http://www.ricardocuisine.com/mariage'>Mariage</a></li> 
																				<li><a title='' href='dossier-martinique'>Martinique</a></li> 
																				<li><a title='' href='noel'>Noël</a></li> 
																				<li><a title='' href='nouvellescuisines'>Nouvelles cuisines</a></li> 
										                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                        										<li><a title='' href='nutrition'>Nutrition sportive </a></li> 
																				<li><a title='' href='pain'>Pain maison</a></li> 
																				<li><a title='' href='paques'>Pâques</a></li> 
																				<li><a title='' href='peche-entre-amis'>Pêche entre amis</a></li> 
																				<li><a title='' href='poireaux'>Poireaux</a></li> 
																				<li><a title='' href='potluckerable'>Potluck à l'érable</a></li> 
																				<li><a title='' href='stvalentin'>Saint-Valentin</a></li> 
																				<li><a title='' href='oscars'>Soirée Oscars</a></li> 
																				<li><a title='' href='norvege'>Voyage gourmand en Norvège</a></li> 
										                                        
                                    </ul>
                                    </div>
                                </div>
                            </div>
                        </div></div><div class='mmSliderTheme-panel panel' style='width: 14.2857142857143%;'><div class='panel-wrapper'>
                            <span class='lsPanelTitle'>Le 30</span>
                            <div class='refLine'>
                            	                            
                               <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                        										<li><a title='' href='le30/30minutes'>Le 30 30 minutes</a></li> 
																				<li><a title='' href='le30/boitealunch'>Le 30 boîte à lunch</a></li> 
																				<li><a title='' href='le30/citrouille-et-courge'>Le 30 citrouille et courge</a></li> 
																				<li><a title='' href='le30/cocktaildinatoire'>Le 30 Cocktail Dînatoire</a></li> 
																				<li><a title='' href='le30/cocos'>Le 30 cocos</a></li> 
																				<li><a title='' href='le30/comfortfood'>Le 30 comfort food</a></li> 
																				<li><a title='' href='le30/dessertsdete'>Le 30 desserts d'été</a></li> 
																				<li><a title='' href='le30/dessertsglaces'>Le 30 desserts glacés</a></li> 
										                                        
                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                        										<li><a title='' href='le30/drolmentcochon'>Le 30 drôlement cochon</a></li> 
																				<li><a title='' href='le30/enunebouchee'>Le 30 en une bouchée</a></li> 
																				<li><a title='' href='le30/fromages'>Le 30 fromages</a></li> 
																				<li><a title='' href='le30/fruitsdemer'>Le 30 fruits de mer</a></li> 
																				<li><a title='' href='le30/grillades'>Le 30 grillades</a></li> 
																				<li><a title='' href='le30/homardetcrabe'>Le 30 homard et crabe</a></li> 
																				<li><a title='' href='le30/lunchs'>Le 30 lunchs</a></li> 
																				<li><a title='' href='le30/moinsdeviande'>Le 30 moins de viande</a></li> 
										                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                        										<li><a title='' href='le30/petitsmarmitons'>Le 30 petits marmitons</a></li> 
																				<li><a title='' href='le30/printanier'>Le 30 printanier</a></li> 
																				<li><a title='' href='le30/recettesdete'>Le 30 recettes d'été</a></li> 
																				<li><a title='' href='le30/salades'>Le 30 salades</a></li> 
																				<li><a title='' href='le30/saladesdete'>Le 30 salades d'été</a></li> 
																				<li><a title='' href='le30/sante'>Le 30 santé</a></li> 
																				<li><a title='' href='le30/soupes-et-potages'>Le 30 soupes et potages</a></li> 
																				<li><a title='' href='le30/toutchocolat'>Le 30 tout chocolat</a></li> 
										                                        
                                    </ul>
                                    </div>
                                </div>
                         
                            </div>
                        </div></div><div class='mmSliderTheme-panel panel' style='width: 14.2857142857143%;'><div class='panel-wrapper'>
                            <span class='lsPanelTitle'>Menus à la carte</span>
                            <div class='refLine'>
                            	                            
                               <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                        										<li><a title='' href='http://www.ricardocuisine.com/menusalacarte/cuisine_asiatique'>Cuisine asiatique</a></li> 
																				<li><a title='' href='menusalacarte/festin_des_fetes'>Festin des fêtes</a></li> 
																				<li><a title='' href='http://www.ricardocuisine.com/menusalacarte/fruits_et_legumes'>Fruits et légumes</a></li> 
																				<li><a title='' href='http://www.ricardocuisine.com/menusalacarte/grillades'>Grillades et recettes d'été</a></li> 
																				<li><a title='' href='menusalacarte/bbq'>La saison du BBQ</a></li> 
										                                        
                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                        										<li><a title='' href='menusalacarte/manger-en-famille'>Manger en famille</a></li> 
																				<li><a title='' href='menusalacarte/mangerlocal'>Manger local</a></li> 
																				<li><a title='' href='menusalacarte/diabete'>Recettes gourmandes pour diabétiques</a></li> 
																				<li><a title='' href='menusalacarte/saveurs_automne'>Saveurs d'automne</a></li> 
																				<li><a title='' href='http://www.ricardocuisine.com/menusalacarte/saveurs_italie'>Saveurs de l'Italie</a></li> 
										                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                        										<li><a title='' href='http://www.ricardocuisine.com/menusalacarte/saveurs_de_saison'>Saveurs de saison</a></li> 
																				<li><a title='' href='menusalacarte/tablee-de-desserts'>Tablée de desserts</a></li> 
																				<li><a title='' href='menusalacarte/hiver'>Un souper en toute simplicité</a></li> 
																				<li><a title='' href='menusalacarte/vins_et_fromages'>Vins et fromages d'ici</a></li> 
																				<li><a title='' href='http://www.ricardocuisine.com/menusalacarte/yogourt_grec'>Yogourt grec en vedette</a></li> 
										                                        
                                    </ul>
                                    </div>
                                </div>
                         
                            </div>
                        </div></div><div class='mmSliderTheme-panel panel' style='width: 14.2857142857143%;'><div class='panel-wrapper'>
                            <span class='lsPanelTitle'>Galeries</span>
                            <div class='refLine'>
								                                
                               <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                                                                <li><a title='' href='galeries/fraises'>Des fraises pour dessert!</a></li> 
                                                                                <li><a title='' href='galeries/5a7'>Jeudis 5 à 7</a></li> 
                                                                                
                                    </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                                                                <li><a title='' href='galeries/homard'>Recevoir au homard</a></li> 
                                                                            </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                                                                <li><a title='' href='galeries/charlevoix'>Saveurs de Charlevoix</a></li> 
                                                                                
                                    </ul>
                                    </div>
                                </div>
                            </div>
						</div></div><div class='mmSliderTheme-panel panel' style='width: 14.2857142857143%;'><div class='panel-wrapper'>
                            <span class='lsPanelTitle'>Thèmes</span>
                            <div class='refLine'>
                            
                            	                                <div class='col4'>
                                    <div class='catNav'>
                                        <ul> 
                                                                                        <li><a title='' href='themes/30-minutes/'>30 minutes<span>310</span></a></li> 
                                                                                        <li><a title='' href='themes/a-congeler/'>À congeler<span>562</span></a></li> 
                                                                                        <li><a title='' href='themes/action-de-grace/'>Action de grâce<span>101</span></a></li> 
                                                                                        <li><a title='' href='themes/BBQ/'>BBQ<span>317</span></a></li> 
                                                                                        <li><a title='' href='themes/cabane-a-sucre/'>Cabane à sucre<span>49</span></a></li> 
                                                                                        <li><a title='' href='themes/cadeaux-gourmands/'>Cadeaux gourmands<span>86</span></a></li> 
                                                                                        <li><a title='' href='themes/camping/'>Camping<span>19</span></a></li> 
                                                                                        <li><a title='' href='themes/comfort-food/'>Comfort food<span>448</span></a></li> 
                                                                                        <li><a title='' href='themes/cuisine-dete/'>Cuisine d'été<span>419</span></a></li> 
                                                                                        <li><a title='' href='themes/cuisine-du-monde/'>Cuisine du monde<span>471</span></a></li> 
                                                                                        
                                        </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                                                                    <li><a title='' href='themes/cuisine-pour-diabetiques/'>Cuisine pour diabétiques  <span>79</span></a></li>
                                                                                        <li><a title='' href='themes/fete-des-meres/'>Fête des Mères  <span>79</span></a></li>
                                                                                        <li><a title='' href='themes/fete-des-peres/'>Fête des Pères  <span>59</span></a></li>
                                                                                        <li><a title='' href='themes/halloween/'>Halloween  <span>36</span></a></li>
                                                                                        <li><a title='' href='themes/le-meilleur/'>Le Meilleur  <span>38</span></a></li>
                                                                                        <li><a title='' href='themes/lunchs/'>Lunchs  <span>173</span></a></li>
                                                                                        <li><a title='' href='themes/mijoteuse/'>Mijoteuse  <span>42</span></a></li>
                                                                                        <li><a title='' href='themes/noel/'>Noël  <span>409</span></a></li>
                                                                                        <li><a title='' href='themes/nouvel-an-chinois/'>Nouvel an chinois  <span>17</span></a></li>
                                                                                        <li><a title='' href='themes/paques/'>Pâques  <span>147</span></a></li>
                                                                                </ul>
                                    </div>
                                </div>
                                <div class='col4'>
                                    <div class='catNav'>
                                    <ul>
                                                                                    <li><a title='' href='themes/pour-enfants/'>Pour enfants <span>239</span></a></li>
                                                                                        <li><a title='' href='themes/recettes-de-semaine/'>Recettes de semaine <span>536</span></a></li>
                                                                                        <li><a title='' href='themes/recettes-pour-2/'>Recettes pour 2 <span>54</span></a></li>
                                                                                        <li><a title='' href='themes/recettes-sur-la-plaque/'>Recettes sur la plaque <span>19</span></a></li>
                                                                                        <li><a title='' href='themes/saint-jean-baptiste/'>Saint-Jean-Baptiste <span>5</span></a></li>
                                                                                        <li><a title='' href='themes/saint-patrick/'>Saint-Patrick <span>12</span></a></li>
                                                                                        <li><a title='' href='themes/saint-valentin/'>Saint-Valentin <span>66</span></a></li>
                                                                                        <li><a title='' href='themes/sante/'>Santé / Choix Sain <span>334</span></a></li>
                                                                                    
                                    </ul>
                                    </div>
                                </div>
                             
                         
                            </div>
                        </div></div></div>
                        
                                            
                         
                         
              
                        
 					      <!-- // Galleries -->
                           

                    </div></div>
                
                </div>
                    
            </div><!-- /.mask -->
                    </nav>
        
        
    </header>
    
</div> <!-- /.wrapHeader-->	<script type='text/javascript'>
    var addthis_config = {
         pubid: 'synchwithus',
         ui_language: 'fr'
    }
    var addthis_share = { 
        email_template: 'ricardo_fr',
        email_vars: { nom: 'Pouding au chocolat' },
		title: 'Pouding au chocolat'
    }
	</script>
<script type='text/javascript' src='//s7.addthis.com/js/300/addthis_widget.js#pubid=synchwithus' async='async'></script>
	<div class='wrapMain' itemscope='' itemtype='http://schema.org/Recipe'>
		
		<div class='subWrap main feature' style='background: url(pictures/cache/f42e29ee36a20b495128fd3f33792edc_w1074.jpg) no-repeat center top transparent;background-size: cover;'>
			
			<div class='innerWrap'>
				<div class='itemDetail'>
                	<div class='pict'>
                        <a title='' href='pictures/cache/cf9e63cbb4b40428e4277afdf36edf5d_w500_h675_sc.jpg' class='fancybox'>
                        <img itemprop='image' width='500' height='675' alt='Pouding au chocolat' src='pictures/cache/cf9e63cbb4b40428e4277afdf36edf5d_w500_h675_sc.jpg'>
                       </a>
					    <a title='' class='zoomInAction fancybox' href='pictures/cache/cf9e63cbb4b40428e4277afdf36edf5d_w500_h675_sc.jpg'>Aggrandir</a>
                    </div>
										
					<div class='desc'>
						
						<h1 itemprop='name'>Pouding au chocolat</h1>                                                
						<div class='meta'>
                                                    
							<a itemprop='aggregateRating' itemscope='' itemtype='http://schema.org/AggregateRating' href='#comments' title='' class='eval eval5 scrollToTarget'>
								<span class='rating' itemprop='ratingValue'>5</span>
								<span class='basedOn'>(<span itemprop='reviewCount'>67</span>)</span>
								<meta itemprop='worstRating' content='1'>
								<meta itemprop='bestRating' content='5'>
							</a>
							<span class='addCommentLinkWrap'><a href='#addComment' title='' class='scrollToTarget'>Évaluer cette recette</a></span>
						</div>
						
                        
                        <div class='notes '>
							<h2>Note personelle</h2>
                                                        <p><a class='addLink popover-trigger' href='#' id='loginAddRecipe' data-redirect='/recettes/5409-pouding-au-chocolat#notes' data-action='login' data-placement='right' data-class='unLogin popover-login-right' data-set-action='add_recipe' data-original-title='Connexion'>Ajouter une note personnelle</a></p>
                            							
						</div>
                        
                        
						<dl>
                        										<dt>Préparation</dt>
									<dd><meta itemprop='prepTime' content='PT30M'>30 min</dd>
								 									<dt>Cuisson</dt>
									<dd><meta itemprop='cookTime' content='PT1H'>1 h</dd>
								                             
                                                        <dt>Portions</dt> 
                            <dd itemprop='recipeYield'>10</dd>
                                                        
						</dl>
						                        
                        <a href='recettes/5409-pouding-au-chocolat/full/' title='Afficher en mode plein écran' class='fullScreenLink' target='_blank'>Afficher en mode plein écran</a>
						
					</div> <!-- /.desc -->
					
					<div class='clearer'></div>
				</div>
					
			</div> <!-- /.innerWrap -->
			
			<div class='featureExtra01'><!--  --></div>
			
		</div> <!-- /.feature -->
		
		<div class='subWrap main'>
			
			<div class='innerWrap'>
				<div class='refLine altDisplay'>
					
					<div class='col8 altDisplay'>
						
						<div class='toolBar'>
                                
                            <div class='actionLinks'>
                            	<ul>
                                	<li>
                                                                        
                                    	<a class='addLink popover-trigger' href='#' id='loginAddRecipe' data-redirect='/recettes/5409-pouding-au-chocolat' data-action='login' data-placement='left' data-class='unLogin popover-login-left' data-set-action='add_recipe' data-original-title='Connexion'>Ajouter à mes recettes</a>
                                    
                                                                        </li>
                                	<li><a class='printLink popover-trigger' data-class='imgPrintDialog' id='printPopUp' href='#' data-action='print_recipe' data-placement='left' data-original-title=''>Imprimer</a></li>
                                	<li><a class='addthis_button_email sendToFriendLink' target='_blank' title='Email' href='#'><img src='css/transparent.gif'>Envoyer par courriel</a></li>
                                    
                                                                        <li><a class='nutritionLink fancybox' href='#nutritionFactsItem'>Valeur nutritive</a></li>
                                                                       
                                    
                                </ul>
                            </div>
                                                        <div class='hide'>
	                            <div class='nutritionFacts' id='nutritionFactsItem'>
						
									<h2>Valeur nutritive</h2>
                                                                       <p class='portion'>Pour 1 portion</p>
									                                    
                                    
                                                                        
                                    <div id='nutrition_0' class='display_nutrition'>
                                    
                                                                        
									<table itemprop='nutrition' itemscope='' itemtype='http://schema.org/NutritionInformation'>
										<tbody><tr>
											<th class='cellAmount'>Teneur</th>
											<th class='cellValue'>% valeur quotidienne</th>
										</tr>
                                        
                                        
                                        										<tr class='first'>
											<td class='cellAmount'>
                                            Calories                                            
																							<span itemprop='calories'>
																						 650  </span>
                                           
                                            </td>
											<td class='cellValue'>&nbsp;</td>
										</tr>
                                        										<tr class=''>
											<td class='cellAmount'>
                                            Lipides (Gras totaux)                                            
																							<span itemprop='fatContent'>
																						 17 g </span>
                                           
                                            </td>
											<td class='cellValue'>&nbsp;</td>
										</tr>
                                        										<tr class=' dataType01 subRow'>
											<td class='cellAmount'>
                                            Saturés                                            
																							<span>
																						 10 g </span>
                                           
                                            </td>
											<td class='cellValue'>&nbsp;</td>
										</tr>
                                        										<tr class=''>
											<td class='cellAmount'>
                                            Sodium (sel)                                            
																							<span>
																						 200 mg </span>
                                           
                                            </td>
											<td class='cellValue'>&nbsp;</td>
										</tr>
                                        										<tr class=''>
											<td class='cellAmount'>
                                            Glucides                                            
																							<span>
																						 123 g </span>
                                           
                                            </td>
											<td class='cellValue'>&nbsp;</td>
										</tr>
                                        										<tr class=' dataType01 subRow'>
											<td class='cellAmount'>
                                            Fibres                                            
																							<span>
																						 4 g </span>
                                           
                                            </td>
											<td class='cellValue'>&nbsp;</td>
										</tr>
                                        										<tr class=''>
											<td class='cellAmount'>
                                            Protéines                                            
																							<span>
																						 7 g </span>
                                           
                                            </td>
											<td class='cellValue'>&nbsp;</td>
										</tr>
                                        										
                                        
                                                                            
                                        
										
									</tbody></table>
                                    <p class='tableFooter'><a href='/a-propos-nutritif'>À propos des valeurs nutritives</a></p>
                                    </div>
                                    									
								</div>
                            </div>
                                                        
                            <div class='tags'>
                                <h2>Catégories</h2>
                                <p>
                                	                                    	
                                	<a href='recettes/poudings-et-tapiocas/' class='keyword'>Poudings et tapiocas</a>
                                    
                                                                        
                                                                        	
                                	<a href='themes/comfort-food/' class='keyword'>Comfort food</a>
                                    
                                                                       
                                	
								</p>
                            </div>
                            
                                                        
							                            <div class='music'>
                                <h2 class='tglTrig tglTrigActive'>Musique en direct</h2>
                                <div class='tglCont tglContActive'>
	                                <a href='javascript:popupcentree('http://www.rythmefm.com/montreal/webradio/',950,600,'menubar=no,scrollbars=no,statusbar=no')' onclick='ga_send({type:'event', category:'externe', action:'rythme fm', text:'Ecoutez la radio en direct'});' title='Ouvrir le lecteur'><img src='global/global_5649/img/ads/rythmfm.png' width='148' height='42' alt='Rythm FM 105.7' title='Rythm FM 105.7'></a>
                                </div>
							</div>
                            
							
							                            
                            							<div class='reference'>
								<h2>Référence</h2>
								<div class='unit hUnit uAlt'>
									<div class='pict'>
										<img src='pictures/cache/681c765fb4f914b8a99f5d7f596d6360_w50_h67_sc.jpg' alt=''>
										<span class='pictExtra'><span></span></span>
									</div>
									<div class='desc'>
                                    	                                        <h3 class='title'>Parce qu'on a tous de la visite</h3>
																					<a href='http://librairie.lapresse.ca/livres/ricardo-935.html' target='_blank' class='seeMoreLink'>Achat en ligne</a>
										
																			</div>
								</div>
							</div>
                                                        
                            
                        </div> <!-- /.toolBar -->
						
						<div class='socialMediaShare largeShareBar'>
                        	<!-- AddThis Button BEGIN -->

<!-- Go to www.addthis.com/dashboard to customize your tools -->
	<div class='addthis_sharing_toolbox' data-url='http://www.ricardocuisine.com/recettes/5409-pouding-au-chocolat' data-title='Pouding au chocolat Recettes | Ricardo'><div id='atstbx' class='at-share-tbx-element addthis_32x32_style addthis-smartlayers addthis-animated at4-show'><a class='at-share-btn at-svc-facebook'><span class='at4-icon aticon-facebook' title='Facebook'></span></a><span class='at_flat_counter'>800</span><a class='at-share-btn at-svc-twitter'><span class='at4-icon aticon-twitter' title='Twitter'></span></a><span class='at_flat_counter'>3</span><a class='at-share-btn at-svc-pinterest_share'><span class='at4-icon aticon-pinterest_share' title='Pinterest'></span></a><a class='at-share-btn at-svc-google_plusone_share'><span class='at4-icon aticon-google_plusone_share' title='Google+'></span></a></div></div>

	<script type='text/javascript'>
		  var addthis_config = {
			   pubid: 'synchwithus',
			   ui_language: 'fr'
		  }
		  var addthis_share = { 
			  email_template: 'ricardo_fr',
			  email_vars: { nom: 'Pouding au chocolat Recettes | Ricardo' },
			  title: 'Pouding au chocolat Recettes | Ricardo'
		  }
	</script>
	

<script type='text/javascript' src='//s7.addthis.com/js/300/addthis_widget.js#pubid=synchwithus' async='async'></script>
<!-- AddThis Button END -->
	
                        </div>
                        	
						<section class='ingredients' style='z-index:0'>
						
							<h2>Ingrédients</h2>
							
							<form name='addIngredients' id='extform' action='/recettes/5409-pouding-au-chocolat' method='post'>
			                    
			                    
			                    <div class='frmInnerWrap'> <!-- Please keep hidden input out of this div -->
                                            <h3>Sauce</h3>
                                                <ul>
                                                        <li>
                                                            <input type='checkbox' id='ing276548' name='idri[]' value='276548'>
                                                            <label itemprop='ingredients' for='ing276548'>
																<span>
																	750 ml (3 tasses) de cassonade																</span>
															</label>
                                                        </li>
                                                                                                                                                                                                         
                                                                                                               <li>
                                                            <input type='checkbox' id='ing276549' name='idri[]' value='276549'>
                                                            <label itemprop='ingredients' for='ing276549'>
																<span>
																	180 ml (3/4 tasse) de cacao, tamisé 																</span>
															</label>
                                                        </li>
                                                                                                                                                                                                         
                                                                                                               <li>
                                                            <input type='checkbox' id='ing276550' name='idri[]' value='276550'>
                                                            <label itemprop='ingredients' for='ing276550'>
																<span>
																	10 ml (2 c. à thé) de fécule de maïs																</span>
															</label>
                                                        </li>
                                                                                                                                                                                                         
                                                                                                               <li>
                                                            <input type='checkbox' id='ing276551' name='idri[]' value='276551'>
                                                            <label itemprop='ingredients' for='ing276551'>
																<span>
																	625 ml (2 &thinsp;1/2 tasses) d’eau																</span>
															</label>
                                                        </li>
                                                                                                                                                                                                         
                                                                                                               <li>
                                                            <input type='checkbox' id='ing276552' name='idri[]' value='276552'>
                                                            <label itemprop='ingredients' for='ing276552'>
																<span>
																	125 ml (1/2 tasse) de crème 35 %																</span>
															</label>
                                                        </li>
                                                                                                                                                                                                         
                                                                                                               <li>
                                                            <input type='checkbox' id='ing276553' name='idri[]' value='276553'>
                                                            <label itemprop='ingredients' for='ing276553'>
																<span>
																	2,5 ml (1/2 c. à thé) d’extrait de vanille																</span>
															</label>
                                                        </li>
                                                                                                                                                   
                                                                                                     </ul>
                                                                                                                                                                                                              <h3>Gâteau</h3>
                                                                                                        <ul>
                                                                                                       
                                                                                                               <li>
                                                            <input type='checkbox' id='ing276554' name='idri[]' value='276554'>
                                                            <label itemprop='ingredients' for='ing276554'>
																<span>
																	250 ml (1 tasse) de lait																</span>
															</label>
                                                        </li>
                                                                                                                                                                                                         
                                                                                                               <li>
                                                            <input type='checkbox' id='ing276555' name='idri[]' value='276555'>
                                                            <label itemprop='ingredients' for='ing276555'>
																<span>
																	125 ml (1/2 tasse) de cacao, tamisé 																</span>
															</label>
                                                        </li>
                                                                                                                                                                                                         
                                                                                                               <li>
                                                            <input type='checkbox' id='ing276556' name='idri[]' value='276556'>
                                                            <label itemprop='ingredients' for='ing276556'>
																<span>
																	375 ml (1 &thinsp;1/2 tasse) de farine tout usage non blanchie																</span>
															</label>
                                                        </li>
                                                                                                                                                                                                         
                                                                                                               <li>
                                                            <input type='checkbox' id='ing276557' name='idri[]' value='276557'>
                                                            <label itemprop='ingredients' for='ing276557'>
																<span>
																	5 ml (1 c. à thé) de bicarbonate de soude																</span>
															</label>
                                                        </li>
                                                                                                                                                                                                         
                                                                                                               <li>
                                                            <input type='checkbox' id='ing276558' name='idri[]' value='276558'>
                                                            <label itemprop='ingredients' for='ing276558'>
																<span>
																	1 pincée de sel																</span>
															</label>
                                                        </li>
                                                                                                                                                                                                         
                                                                                                               <li>
                                                            <input type='checkbox' id='ing276559' name='idri[]' value='276559'>
                                                            <label itemprop='ingredients' for='ing276559'>
																<span>
																	125 ml (1/2 tasse) de beurre non salé, ramolli																</span>
															</label>
                                                        </li>
                                                                                                                                                                                                         
                                                                                                               <li>
                                                            <input type='checkbox' id='ing276560' name='idri[]' value='276560'>
                                                            <label itemprop='ingredients' for='ing276560'>
																<span>
																	375 ml (1 1/2 tasse) de sucre																</span>
															</label>
                                                        </li>
                                                                                                                                                                                                         
                                                                                                               <li>
                                                            <input type='checkbox' id='ing276561' name='idri[]' value='276561'>
                                                            <label itemprop='ingredients' for='ing276561'>
																<span>
																	2 oeufs																</span>
															</label>
                                                        </li>
                                                                                                                                                       
                                                    </ul>    
				                    		                    
				                    <div class='batchAction'>
				                    	<input type='checkbox' onclick='checkedAll(&quot;extform&quot;);' id='cb16'><label for='cb16'>Sélectionner tout</label>
                                                                            
                                    	<a class='btn popover-trigger' href='#' id='loginAddIng' data-redirect='/recettes/5409-pouding-au-chocolat#top' data-action='login' data-placement='right' data-class='unLogin popover-login-right' data-set-action='add_ingredients' data-extform='true' data-original-title='Connexion'>Ajouter à ma liste d'épicerie</a>
                                    
                                    				                    </div>
			                    </div>
			                </form>
						</section>
						
						<section itemprop='recipeInstructions' class='preparation'>
							<h2>Préparation</h2>	
                            
                            								<h3></h3>
                                <ol start='1'>
																<li><span>Placer la grille au centre du four. Préchauffer le four à 180 °C (350 °F). Beurrer un plat de cuisson de 33 x 23 cm (13 x 9 po).</span></li>
							</ol>								<h3>Sauce</h3>
                                <ol start='2'>
																<li><span>Dans une casserole, mélanger la cassonade, le cacao et la fécule. Ajouter l’eau, la crème et porter à ébullition en remuant à l’aide d’un fouet. Incorporer la vanille. Réserver. </span></li>
							</ol>								<h3>Gâteau</h3>
                                <ol start='3'>
																<li><span>Dans une petite casserole, porter à ébullition le lait et le cacao en remuant à l’aide d’un fouet. Laisser tiédir.</span></li>
															<li><span>Dans un bol, mélanger la farine, le bicarbonate et le sel. Réserver.</span></li>
															<li><span>Dans un autre bol, mélanger le beurre et le sucre au batteur électrique jusqu’à ce que le mélange prenne une texture granuleuse. Ajouter les oeufs, un à la fois, et battre jusqu’à ce que la préparation soit homogène. À basse vitesse, incorporer les ingrédients secs en alternant avec le mélange de cacao. Répartir la pâte dans le plat.</span></li>
															<li><span>Verser la sauce chaude délicatement sur la pâte. Cuire au four environ 45 minutes ou jusqu’à ce qu’un cure-dent inséré au centre du gâteau en ressorte propre. Servir chaud ou tempéré.</span></li>
							</ol>							
						</section>
					</div><!---/.col8-->
					
					<div class='col4 altDisplay'>
                        
                        <div class='blankWrap'><span class='title'></span>
    <div style='overflow:visible; position:relative;'>
    </div>
    <div style='clear:both;'></div>
    <div style='overflow:visible; position:relative;'>
    </div>
    <div style='clear:both;'></div>
</div>						
                        <div class='relatedContent rcVideos'>
							<h2>Capsules vidéo</h2>
							<div class='lining carousel'>
                                <div class=''>
                                	<div id='rcVideosSlider-wrapper' class='liquid-slider-wrapper arrows liquid-responsive' style='width: 312px; max-width: 1030px;'><div class='liquid-nav'><ul id='rcVideosSlider-nav-ul' style='float: left;'><li class='tab1'>
       <a href='#1' title='Peler les courges Butternut et poivrée' class='test current'>Peler les courges Butternut et poivrée</a></li><li class='tab2'>
       <a href='#2' title='Nettoyer les champignons' class='test'>Nettoyer les champignons</a></li><li class='tab3'>
       <a href='#3' title='Les variétés de pommes de terre' class='test'>Les variétés de pommes de terre</a></li><li class='tab4'>
       <a href='#4' title='La parfaite purée de pommes de terre' class='test'>La parfaite purée de pommes de terre</a></li><li class='tab5'>
       <a href='#5' title='Manger des artichauts' class='test'>Manger des artichauts</a></li><li class='tab6'>
       <a href='#6' title='L'ail: retirer le germe ou non?' class='test'>L'ail: retirer le germe ou non?</a></li><li class='tab7'>
       <a href='#7' title='Ouvrir une noix de coco' class='test'>Ouvrir une noix de coco</a></li><li class='tab8'>
       <a href='#8' title='Peler, couper et trancher une mangue' class='test'>Peler, couper et trancher une mangue</a></li><li class='tab9'>
       <a href='#9' title='Pour de l'huile parfumée à l'ail sans danger' class='test'>Pour de l'huile parfumée à l'ail sans danger</a></li><li class='tab10'>
       <a href='#10' title='Manipuler des piments en toute sécurité' class='test'>Manipuler des piments en toute sécurité</a></li></ul></div><div class='liquid-nav-left-arrow' data-liquidslider-dir='prev' title='Précédent' style='visibility: visible; opacity: 1;'><a href='#'></a></div><div class='liquid-slider' id='rcVideosSlider' style='height: 170px; overflow: hidden;'>
                                    			                                <div class='panel-container' style='margin-left: -312px; width: 1200%;'><div class='rcVideosSlider-panel panel' style='width: 8.33333333333333%;'><div class='panel-wrapper'>
		                                	<div class='unit cUnit uVideo'>
										        <div class='pict'>
											       	<a href='videos/fruits-et-legumes/91-manipuler-des-piments-en-toute-securite' title='Manipuler des piments en toute sécurité'><img src='pictures/cache/c34efdc3d099f4c24226c1e7b73ab81b_w200_h125_sc.jpg' alt='Manipuler des piments en toute sécurité'></a>
										        </div>
										        <div class='desc'>
											        <h3 class='title'><a href='videos/fruits-et-legumes/91-manipuler-des-piments-en-toute-securite' title='Manipuler des piments en toute sécurité'>Manipuler des piments en toute sécurité</a></h3>
										        </div>
										    </div>
		                                <div class='liquid-slider-preloader'></div></div></div><div class='rcVideosSlider-panel panel' style='width: 8.33333333333333%;'><div class='panel-wrapper'>
		                                	<div class='unit cUnit uVideo'>
										        <div class='pict'>
											       	<a href='videos/fruits-et-legumes/145-peler-les-courges-butternut-et-poivree' title='Peler les courges Butternut et poivrée'><img src='pictures/cache/c85b2b29c2b3d4edbcd2b794b4a3cb40_w200_h125_sc.jpg' alt='Peler les courges Butternut et poivrée'></a>
										        </div>
										        <div class='desc'>
											        <h3 class='title'><a href='videos/fruits-et-legumes/145-peler-les-courges-butternut-et-poivree' title='Peler les courges Butternut et poivrée'>Peler les courges Butternut et poivrée</a></h3>
										        </div>
										    </div>
		                                <div class='liquid-slider-preloader'></div></div></div><div class='rcVideosSlider-panel panel' style='width: 8.33333333333333%;'><div class='panel-wrapper'>
		                                	<div class='unit cUnit uVideo'>
										        <div class='pict'>
											       	<a href='videos/fruits-et-legumes/1-nettoyer-les-champignons' title='Nettoyer les champignons'><img src='pictures/cache/9bf8390ae97f3dd13b4ea4ae81faf156_w200_h125_sc.jpg' alt='Nettoyer les champignons'></a>
										        </div>
										        <div class='desc'>
											        <h3 class='title'><a href='videos/fruits-et-legumes/1-nettoyer-les-champignons' title='Nettoyer les champignons'>Nettoyer les champignons</a></h3>
										        </div>
										    </div>
		                                <div class='liquid-slider-preloader'></div></div></div><div class='rcVideosSlider-panel panel' style='width: 8.33333333333333%;'><div class='panel-wrapper'>
		                                	<div class='unit cUnit uVideo'>
										        <div class='pict'>
											       	<a href='videos/fruits-et-legumes/132-les-varietes-de-pommes-de-terre' title='Les variétés de pommes de terre'><img src='pictures/cache/1c2a612331b83066d0a1c99a947d494b_w200_h125_sc.jpg' alt='Les variétés de pommes de terre'></a>
										        </div>
										        <div class='desc'>
											        <h3 class='title'><a href='videos/fruits-et-legumes/132-les-varietes-de-pommes-de-terre' title='Les variétés de pommes de terre'>Les variétés de pommes de terre</a></h3>
										        </div>
										    </div>
		                                <div class='liquid-slider-preloader'></div></div></div><div class='rcVideosSlider-panel panel' style='width: 8.33333333333333%;'><div class='panel-wrapper'>
		                                	<div class='unit cUnit uVideo'>
										        <div class='pict'>
											       	<a href='videos/fruits-et-legumes/152-la-parfaite-puree-de-pommes-de-terre' title='La parfaite purée de pommes de terre'><img src='pictures/cache/c25fe2f20ce59895a2ccc49e3e7c4bea_w200_h125_sc.jpg' alt='La parfaite purée de pommes de terre'></a>
										        </div>
										        <div class='desc'>
											        <h3 class='title'><a href='videos/fruits-et-legumes/152-la-parfaite-puree-de-pommes-de-terre' title='La parfaite purée de pommes de terre'>La parfaite purée de pommes de terre</a></h3>
										        </div>
										    </div>
		                                <div class='liquid-slider-preloader'></div></div></div><div class='rcVideosSlider-panel panel' style='width: 8.33333333333333%;'><div class='panel-wrapper'>
		                                	<div class='unit cUnit uVideo'>
										        <div class='pict'>
											       	<a href='videos/fruits-et-legumes/3-manger-des-artichauts' title='Manger des artichauts'><img src='pictures/cache/fd2b166da57c879c20d6bf7804a3a46f_w200_h125_sc.jpg' alt='Manger des artichauts'></a>
										        </div>
										        <div class='desc'>
											        <h3 class='title'><a href='videos/fruits-et-legumes/3-manger-des-artichauts' title='Manger des artichauts'>Manger des artichauts</a></h3>
										        </div>
										    </div>
		                                <div class='liquid-slider-preloader'></div></div></div><div class='rcVideosSlider-panel panel' style='width: 8.33333333333333%;'><div class='panel-wrapper'>
		                                	<div class='unit cUnit uVideo'>
										        <div class='pict'>
											       	<a href='videos/fruits-et-legumes/18-l-ail-retirer-le-germe-ou-non' title='L'ail: retirer le germe ou non?'><img src='pictures/cache/12eaf9340708112509994cdda0f15bf0_w200_h125_sc.jpg' alt='L'ail: retirer le germe ou non?'></a>
										        </div>
										        <div class='desc'>
											        <h3 class='title'><a href='videos/fruits-et-legumes/18-l-ail-retirer-le-germe-ou-non' title='L'ail: retirer le germe ou non?'>L'ail: retirer le germe ou non?</a></h3>
										        </div>
										    </div>
		                                <div class='liquid-slider-preloader'></div></div></div><div class='rcVideosSlider-panel panel' style='width: 8.33333333333333%;'><div class='panel-wrapper'>
		                                	<div class='unit cUnit uVideo'>
										        <div class='pict'>
											       	<a href='videos/fruits-et-legumes/66-ouvrir-une-noix-de-coco' title='Ouvrir une noix de coco'><img src='pictures/cache/a839a3e70982fe0bfe376f9871125401_w200_h125_sc.jpg' alt='Ouvrir une noix de coco'></a>
										        </div>
										        <div class='desc'>
											        <h3 class='title'><a href='videos/fruits-et-legumes/66-ouvrir-une-noix-de-coco' title='Ouvrir une noix de coco'>Ouvrir une noix de coco</a></h3>
										        </div>
										    </div>
		                                <div class='liquid-slider-preloader'></div></div></div><div class='rcVideosSlider-panel panel' style='width: 8.33333333333333%;'><div class='panel-wrapper'>
		                                	<div class='unit cUnit uVideo'>
										        <div class='pict'>
											       	<a href='videos/fruits-et-legumes/11-peler-couper-et-trancher-une-mangue' title='Peler, couper et trancher une mangue'><img src='pictures/cache/19ddeeb1ac5f15da9ab3d6fc4bfb0959_w200_h125_sc.jpg' alt='Peler, couper et trancher une mangue'></a>
										        </div>
										        <div class='desc'>
											        <h3 class='title'><a href='videos/fruits-et-legumes/11-peler-couper-et-trancher-une-mangue' title='Peler, couper et trancher une mangue'>Peler, couper et trancher une mangue</a></h3>
										        </div>
										    </div>
		                                <div class='liquid-slider-preloader'></div></div></div><div class='rcVideosSlider-panel panel' style='width: 8.33333333333333%;'><div class='panel-wrapper'>
		                                	<div class='unit cUnit uVideo'>
										        <div class='pict'>
											       	<a href='videos/fruits-et-legumes/84-pour-de-l-huile-parfumee-a-l-ail-sans-danger' title='Pour de l'huile parfumée à l'ail sans danger'><img src='pictures/cache/06e80f9d79d6da92ef37f2868b1315e5_w200_h125_sc.jpg' alt='Pour de l'huile parfumée à l'ail sans danger'></a>
										        </div>
										        <div class='desc'>
											        <h3 class='title'><a href='videos/fruits-et-legumes/84-pour-de-l-huile-parfumee-a-l-ail-sans-danger' title='Pour de l'huile parfumée à l'ail sans danger'>Pour de l'huile parfumée à l'ail sans danger</a></h3>
										        </div>
										    </div>
		                                <div class='liquid-slider-preloader'></div></div></div><div class='rcVideosSlider-panel panel' style='width: 8.33333333333333%;'><div class='panel-wrapper'>
		                                	<div class='unit cUnit uVideo'>
										        <div class='pict'>
											       	<a href='videos/fruits-et-legumes/91-manipuler-des-piments-en-toute-securite' title='Manipuler des piments en toute sécurité'><img src='pictures/cache/c34efdc3d099f4c24226c1e7b73ab81b_w200_h125_sc.jpg' alt='Manipuler des piments en toute sécurité'></a>
										        </div>
										        <div class='desc'>
											        <h3 class='title'><a href='videos/fruits-et-legumes/91-manipuler-des-piments-en-toute-securite' title='Manipuler des piments en toute sécurité'>Manipuler des piments en toute sécurité</a></h3>
										        </div>
										    </div>
		                                <div class='liquid-slider-preloader'></div></div></div><div class='rcVideosSlider-panel panel' style='width: 8.33333333333333%;'><div class='panel-wrapper'>
		                                	<div class='unit cUnit uVideo'>
										        <div class='pict'>
											       	<a href='videos/fruits-et-legumes/145-peler-les-courges-butternut-et-poivree' title='Peler les courges Butternut et poivrée'><img src='pictures/cache/c85b2b29c2b3d4edbcd2b794b4a3cb40_w200_h125_sc.jpg' alt='Peler les courges Butternut et poivrée'></a>
										        </div>
										        <div class='desc'>
											        <h3 class='title'><a href='videos/fruits-et-legumes/145-peler-les-courges-butternut-et-poivree' title='Peler les courges Butternut et poivrée'>Peler les courges Butternut et poivrée</a></h3>
										        </div>
										    </div>
		                                <div class='liquid-slider-preloader'></div></div></div></div>
                                        		                                
                                        		                                
                                        		                                
                                        		                                
                                        		                                
                                        		                                
                                        		                                
                                        		                                
                                        		                                
                                        		                                
		                            </div><div class='liquid-nav-right-arrow' data-liquidslider-dir='next' title='Suivant' style='visibility: visible; opacity: 1;'><a href='#'></a></div></div>
						        </div>
						        
                            </div>
                        </div>
                        
                        <div class='relatedContent rcRecipes'>
                            <h2>Vous aimerez aussi</h2>
                            <div class='lining'>
                            
                            	
<div class='unit hUnit'>
    <div class='pict'>
        <a href='recettes/6772--i-trifle-i-au-chocolat-et-a-l--ananas' title='<i>Trifle</i> au chocolat et à l’ananas'><img src='pictures/cache/ce672de6c8f4645f22e6343b5f1c997b_w80_h108_sc.jpg' alt='<i>Trifle</i> au chocolat et à l’ananas' title='<i>Trifle</i> au chocolat et à l’ananas'></a>
    </div>
    <div class='desc'>
        <h2 class='title'><a href='recettes/6772--i-trifle-i-au-chocolat-et-a-l--ananas' title='<i>Trifle</i> au chocolat et à l’ananas'><i>Trifle</i> au chocolat et à l’ananas</a></h2>
        <a href='recettes/6772--i-trifle-i-au-chocolat-et-a-l--ananas#comments' class='eval eval5'><span class='rating'>5 étoiles</span> <span class='basedOn'>(<span>6</span>)</span></a>
        <ul>
            <li>Préparation : 55 min</li>
            <li>Total : 1 h 25 min</li>
        </ul>
		<ul class='flags'>

		   					</ul>
    </div>
    <div class='clearer'><!-- --></div>
</div>
<div class='unit hUnit'>
    <div class='pict'>
        <a href='recettes/6687-pouding-au-chia' title='Pouding au chia'><img src='pictures/cache/2d5e919fa713731146de625080942925_w80_h108_sc.jpg' alt='Pouding au chia' title='Pouding au chia'></a>
    </div>
    <div class='desc'>
        <h2 class='title'><a href='recettes/6687-pouding-au-chia' title='Pouding au chia'>Pouding au chia</a></h2>
        <a href='recettes/6687-pouding-au-chia#comments' class='eval eval4'><span class='rating'>4 étoiles</span> <span class='basedOn'>(<span>27</span>)</span></a>
        <ul>
            <li>Préparation : 5 min</li>
            <li>Total : 5 min</li>
        </ul>
		<ul class='flags'>

		   					</ul>
    </div>
    <div class='clearer'><!-- --></div>
</div>
<div class='unit hUnit'>
    <div class='pict'>
        <a href='recettes/6643-pouding-aux-fruits-surgeles' title='Pouding aux fruits surgelés'><img src='pictures/cache/b57a359a53c6451b17fd262013dfa10d_w80_h108_sc.jpg' alt='Pouding aux fruits surgelés' title='Pouding aux fruits surgelés'></a>
    </div>
    <div class='desc'>
        <h2 class='title'><a href='recettes/6643-pouding-aux-fruits-surgeles' title='Pouding aux fruits surgelés'>Pouding aux fruits surgelés</a></h2>
        <a href='recettes/6643-pouding-aux-fruits-surgeles#comments' class='eval eval5'><span class='rating'>5 étoiles</span> <span class='basedOn'>(<span>30</span>)</span></a>
        <ul>
            <li>Préparation : 25 min</li>
            <li>Total : 1 h 40 min</li>
        </ul>
		<ul class='flags'>

		   					</ul>
    </div>
    <div class='clearer'><!-- --></div>
</div>                                
                                
                            </div>

                        </div>
						<div class='blankWrap'><span class='title'></span>    <div style='overflow:visible; position:relative;'>
		<script type='text/javascript'>
			sas.render('32177');
		</script>
		
		<noscript>
			&lt;a href='http://diff.smartadserver.com/ac?jump=1&amp;siteid=8587&amp;pgname=standard&amp;fmtid=32177&amp;visit=m&amp;tmstp=12345679&amp;out=nonrich' target='_blank'&gt; 	
				&lt;img src='http://diff.smartadserver.com/ac?out=nonrich&amp;siteid=8587&amp;pgname=standard&amp;fmtid=32177&amp;visit=m&amp;tmstp=12345679' border='0' alt='' /&gt;
			&lt;/a&gt;
		</noscript>
    </div>
    <div style='clear:both;'></div>
    <div style='overflow:visible; position:relative;'>
		<script type='text/javascript'>
			sas.render('32176');
		</script>
		
		<noscript>
			&lt;a href='http://diff.smartadserver.com/ac?jump=1&amp;siteid=8587&amp;pgname=standard&amp;fmtid=32176&amp;visit=m&amp;tmstp=12345679&amp;out=nonrich' target='_blank'&gt; 	
				&lt;img src='http://diff.smartadserver.com/ac?out=nonrich&amp;siteid=8587&amp;pgname=standard&amp;fmtid=32176&amp;visit=m&amp;tmstp=12345679' border='0' alt='' /&gt;
			&lt;/a&gt;
		</noscript>
    </div>
    <div style='clear:both;'></div>
</div>
                    </div> <!---- /.col4 -->
				</div><!-- /.refLine-->
				<div class='clearer'></div>
			
			</div> <!-- /.innerWrap -->
			
		</div> <!-- /.main-->
		
	</div> <!-- /.wrapMain-->

<script type='text/javascript'>

	checked=false;
	vote = 0;
	function checkedAll (frm1) {
		var aa= document.getElementById(frm1);
		 if (checked == false){
			   checked = true
		 }
		 else{
			checked = false
		 }
		for (var i =0; i < aa.elements.length; i++) 
		{
		 aa.elements[i].checked = checked;
		}
	}
	
	function CtrCheck(frm1)
	{
		var aa= document.getElementById(frm1);
		for (var i =0; i < aa.elements.length; i++)
		{ if ( aa.elements[i].checked ) return i; }
		checkedAll('add_ingredients');
		document.getElementById('checkall_btn').checked = checked;
	}
	
	$('.nutritionLink').on('click', function(){
		ga_send({type:'event', action:'click on nutrition', text:'Nutritional values for recipe ' + '5409', category: 'Nutritional value inspection'});
	});
	
	function popupcentree(page,largeur,hauteur,options) { var top=(screen.height-hauteur)/2; var left=(screen.width-largeur)/2; window.open(page,'RythmeFM','top='+top+',left='+left+',width='+largeur+',height='+hauteur+','+options); }

</script>  



        <div class='wrapFooter'>
    
        		
		       <div class='subWrap subFeature carousel carouselItem3'>
			<div class='innerWrap'>
				
					<div id='subFeatureSlider-wrapper' class='liquid-slider-wrapper liquid-responsive' style='width: 1010px; max-width: 1030px;'><div class='liquid-nav'><ul id='subFeatureSlider-nav-ul' style='float: left;'><li class='tab1'>
       <a href='#1' title='Page 1' class='test current'>Page 1</a></li><li class='tab2'>
       <a href='#2' title='Page 2' class='test'>Page 2</a></li><li class='tab3'>
       <a href='#3' title='Page 3' class='test'>Page 3</a></li></ul></div><div class='liquid-slider' id='subFeatureSlider' style='height: 265px; overflow: hidden;'><div class='panel-container' style='margin-left: -1010px; width: 500%;'><div class='subFeatureSlider-panel panel' style='width: 20%;'><div class='panel-wrapper'><span class='lsPanelTitle'>Page 3</span><div class='refLine'>	                	
						<div class='col3'>
							<div class='showcase scAlt'>
								<h2>Dossier</h2>
								<div class='pict'>
									<a href='/nouvellescuisines' title='' data-promo-id='1157' data-page='3'><img src='/pictures/diffuseur/83016882154f9fda7706f3.jpg' alt='Portes ouvertes' width='220' height='138'></a>
								</div>
								<div class='desc'>
									<h3>Portes ouvertes</h3>
									<p>Nouveaux bureaux : on vous fait faire le tour du proprio.</p>
									<a href='/nouvellescuisines' class='seeMoreLink' data-promo-id='1157' data-page='3'>Consulter</a>
								</div>
							</div> <!-- /.showcase -->
						</div>
	                    
	                    </div><div class='liquid-slider-preloader'></div></div></div><div class='subFeatureSlider-panel panel' style='width: 20%;'><div class='panel-wrapper'><span class='lsPanelTitle'>Page 1</span><div class='refLine'>	                	
						<div class='col3'>
							<div class='showcase scAlt'>
								<h2>Dossier spécial</h2>
								<div class='pict'>
									<a href='/estival' title='' data-promo-id='572' data-page='1'><img src='/pictures/diffuseur/6287001045571ad83dcd90.jpg' alt='Le plus bel été de votre vie' width='220' height='138'></a>
								</div>
								<div class='desc'>
									<h3>Le + bel été de votre vie</h3>
									<p>Des recettes et activités, pour un été des plus réussis!</p>
									<a href='/estival' class='seeMoreLink' data-promo-id='572' data-page='1'>Consulter</a>
								</div>
							</div> <!-- /.showcase -->
						</div>
	                    
	                    	                	
						<div class='col3'>
							<div class='showcase scAlt'>
								<h2>Dossier</h2>
								<div class='pict'>
									<a href='/mariage' title='' data-promo-id='1223' data-page='1'><img src='/pictures/diffuseur/1982156645571ab192182c.jpg' alt='Mariage DIY' width='220' height='138'></a>
								</div>
								<div class='desc'>
									<h3>Mariage DIY</h3>
									<p>Des idées économiques et faites à la main pour un mariage <i>Do-it-yourself</i>.</p>
									<a href='/mariage' class='seeMoreLink' data-promo-id='1223' data-page='1'>Consulter</a>
								</div>
							</div> <!-- /.showcase -->
						</div>
	                    
	                    	                	
						<div class='col3'>
							<div class='showcase scAlt'>
								<h2>Dossier</h2>
								<div class='pict'>
									<a href='/mieux-manger' title='' data-promo-id='1058' data-page='1'><img src='/pictures/diffuseur/1834152767557ad77be1c1e.jpg' alt='Le défi mieux manger' width='220' height='138'></a>
								</div>
								<div class='desc'>
									<h3>Le défi mieux manger Ricardo-IGA</h3>
									<p>Découvrez de nouveaux aliments avec nos recettes.</p>
									<a href='/mieux-manger' class='seeMoreLink' data-promo-id='1058' data-page='1'>Consulter</a>
								</div>
							</div> <!-- /.showcase -->
						</div>
	                    
	                    	                	
						<div class='col3'>
							<div class='showcase scAlt'>
								<h2>Le 30</h2>
								<div class='pict'>
									<a href='/le30/grillades' title='' data-promo-id='1171' data-page='1'><img src='/pictures/diffuseur/3955606265593feac76b0b.jpg' alt='Nos meilleures grillades pour l'été!' width='220' height='138'></a>
								</div>
								<div class='desc'>
									<h3>Grillades</h3>
									<p>Nos meilleures grillades pour l'été!</p>
									<a href='/le30/grillades' class='seeMoreLink' data-promo-id='1171' data-page='1'>Consulter</a>
								</div>
							</div> <!-- /.showcase -->
						</div>
	                    
	                    </div><div class='liquid-slider-preloader'></div></div></div><div class='subFeatureSlider-panel panel' style='width: 20%;'><div class='panel-wrapper'><span class='lsPanelTitle'>Page 2</span><div class='refLine'>	                	
						<div class='col3'>
							<div class='showcase scAlt'>
								<h2>Dossier</h2>
								<div class='pict'>
									<a href='/femmes-enceintes' title='' data-promo-id='851' data-page='2'><img src='/pictures/diffuseur/926050472553f79d177a6f.jpg' alt='Guide alimentaire pour la future maman' width='220' height='138'></a>
								</div>
								<div class='desc'>
									<h3>Guide alimentaire pour la future maman</h3>
									<p>Comment s'y retrouver, sans perdre le plaisir de manger?</p>
									<a href='/femmes-enceintes' class='seeMoreLink' data-promo-id='851' data-page='2'>Consulter</a>
								</div>
							</div> <!-- /.showcase -->
						</div>
	                    
	                    	                	
						<div class='col3'>
							<div class='showcase scAlt'>
								<h2>Menus à la carte</h2>
								<div class='pict'>
									<a href='/menusalacarte/bbq' title='' data-promo-id='1235' data-page='2'><img src='/pictures/diffuseur/6969933395559dc8b610e4.jpg' alt='La saison du BBQ' width='220' height='138'></a>
								</div>
								<div class='desc'>
									<h3>La saison du BBQ</h3>
									<p>Trois menus pour savourer l'été</p>
									<a href='/menusalacarte/bbq' class='seeMoreLink' data-promo-id='1235' data-page='2'>Consulter</a>
								</div>
							</div> <!-- /.showcase -->
						</div>
	                    
	                    	                	
						<div class='col3'>
							<div class='showcase scAlt'>
								<h2>Dossier</h2>
								<div class='pict'>
									<a href='/peche-entre-amis' title='' data-promo-id='1208' data-page='2'><img src='/pictures/diffuseur/18633025235531146d5ebd1.jpg' alt='Week-end de pêche entre amis' width='220' height='138'></a>
								</div>
								<div class='desc'>
									<h3>Week-end de pêche entre amis</h3>
									<p>Un plaisir simple de la vie...</p>
									<a href='/peche-entre-amis' class='seeMoreLink' data-promo-id='1208' data-page='2'>Consulter</a>
								</div>
							</div> <!-- /.showcase -->
						</div>
	                    
	                    	                	
						<div class='col3'>
							<div class='showcase scAlt'>
								<h2>Menus à la carte</h2>
								<div class='pict'>
									<a href='/menusalacarte/manger-en-famille' title='' data-promo-id='1205' data-page='2'><img src='/pictures/diffuseur/1691130683552bc80e1efae.jpg' alt='Manger en famille' width='220' height='138'></a>
								</div>
								<div class='desc'>
									<h3>Manger en famille</h3>
									<p>Trois menus conçus sur mesure pour vous inspirer.</p>
									<a href='/menusalacarte/manger-en-famille' class='seeMoreLink' data-promo-id='1205' data-page='2'>Consulter</a>
								</div>
							</div> <!-- /.showcase -->
						</div>
	                    
	                    </div><div class='liquid-slider-preloader'></div></div></div><div class='subFeatureSlider-panel panel' style='width: 20%;'><div class='panel-wrapper'><span class='lsPanelTitle'>Page 3</span><div class='refLine'>	                	
						<div class='col3'>
							<div class='showcase scAlt'>
								<h2>Dossier</h2>
								<div class='pict'>
									<a href='/nouvellescuisines' title='' data-promo-id='1157' data-page='3'><img src='/pictures/diffuseur/83016882154f9fda7706f3.jpg' alt='Portes ouvertes' width='220' height='138'></a>
								</div>
								<div class='desc'>
									<h3>Portes ouvertes</h3>
									<p>Nouveaux bureaux : on vous fait faire le tour du proprio.</p>
									<a href='/nouvellescuisines' class='seeMoreLink' data-promo-id='1157' data-page='3'>Consulter</a>
								</div>
							</div> <!-- /.showcase -->
						</div>
	                    
	                    </div><div class='liquid-slider-preloader'></div></div></div><div class='subFeatureSlider-panel panel' style='width: 20%;'><div class='panel-wrapper'><span class='lsPanelTitle'>Page 1</span><div class='refLine'>	                	
						<div class='col3'>
							<div class='showcase scAlt'>
								<h2>Dossier spécial</h2>
								<div class='pict'>
									<a href='/estival' title='' data-promo-id='572' data-page='1'><img src='/pictures/diffuseur/6287001045571ad83dcd90.jpg' alt='Le plus bel été de votre vie' width='220' height='138'></a>
								</div>
								<div class='desc'>
									<h3>Le + bel été de votre vie</h3>
									<p>Des recettes et activités, pour un été des plus réussis!</p>
									<a href='/estival' class='seeMoreLink' data-promo-id='572' data-page='1'>Consulter</a>
								</div>
							</div> <!-- /.showcase -->
						</div>
	                    
	                    	                	
						<div class='col3'>
							<div class='showcase scAlt'>
								<h2>Dossier</h2>
								<div class='pict'>
									<a href='/mariage' title='' data-promo-id='1223' data-page='1'><img src='/pictures/diffuseur/1982156645571ab192182c.jpg' alt='Mariage DIY' width='220' height='138'></a>
								</div>
								<div class='desc'>
									<h3>Mariage DIY</h3>
									<p>Des idées économiques et faites à la main pour un mariage <i>Do-it-yourself</i>.</p>
									<a href='/mariage' class='seeMoreLink' data-promo-id='1223' data-page='1'>Consulter</a>
								</div>
							</div> <!-- /.showcase -->
						</div>
	                    
	                    	                	
						<div class='col3'>
							<div class='showcase scAlt'>
								<h2>Dossier</h2>
								<div class='pict'>
									<a href='/mieux-manger' title='' data-promo-id='1058' data-page='1'><img src='/pictures/diffuseur/1834152767557ad77be1c1e.jpg' alt='Le défi mieux manger' width='220' height='138'></a>
								</div>
								<div class='desc'>
									<h3>Le défi mieux manger Ricardo-IGA</h3>
									<p>Découvrez de nouveaux aliments avec nos recettes.</p>
									<a href='/mieux-manger' class='seeMoreLink' data-promo-id='1058' data-page='1'>Consulter</a>
								</div>
							</div> <!-- /.showcase -->
						</div>
	                    
	                    	                	
						<div class='col3'>
							<div class='showcase scAlt'>
								<h2>Le 30</h2>
								<div class='pict'>
									<a href='/le30/grillades' title='' data-promo-id='1171' data-page='1'><img src='/pictures/diffuseur/3955606265593feac76b0b.jpg' alt='Nos meilleures grillades pour l'été!' width='220' height='138'></a>
								</div>
								<div class='desc'>
									<h3>Grillades</h3>
									<p>Nos meilleures grillades pour l'été!</p>
									<a href='/le30/grillades' class='seeMoreLink' data-promo-id='1171' data-page='1'>Consulter</a>
								</div>
							</div> <!-- /.showcase -->
						</div>
	                    
	                    </div><div class='liquid-slider-preloader'></div></div></div></div>                    
				
					</div></div>					<!-- /class='liquid-slider' -->
				
				</div>
				</div>
			
                
        <div class='subWrap superFooter '>
            <div class='innerWrap'>
                
                <div class='refLine'>
                    <div class='col3'>
                        <div class='fLinks'>
                            <h2>Liens rapides</h2>
                            <ul>
                                <li><a href='recettes/plats-principaux' title='Plats principaux'>Plats principaux</a></li>
                                <li><a href='recettes/entrees' title='Entrées'>Entrées</a></li>
                                <li><a href='recettes/desserts' title='Desserts'>Desserts</a></li>
                                <li><a href='recettes/ingredients' title='Ingrédients'>Ingrédients</a></li>
				<li><a href='congelation' title='Congélation'>Congélation</a></li>
                                <li><a href='themes' title='Thèmes'>Thèmes</a></li>
                                <li><a href='chroniques' title='Chroniques'>Chroniques</a></li>
                                <li><a href='videos' title='Vidéos '>Vidéos </a></li>
                                <li><a href='convertisseur' title=''>Équivalences</a></li>
				<li><a href='concours' title=''>Concours</a></li>
                                <li><a href='aide' title=''>Aide</a></li>
                               <li><a href='social' title=''>Flux social<span class='sticker newContent' style='top: 2px;width: 50px;'>Nouveau</span></a></li>
                            </ul>
                        </div>
                    </div>
                    
                    <div class='col9'>
                        
                        <div class='refLine'>
                        
                        	
                            <div class='col4'>
                                <div class='fAbout'>
                                    <h2 class='title'><a href='univers-ricardo' title=''>UNIVERS RICARDO</a></h2>
                                    <div class='unit hUnit uAlt'>
                                        <div class='pict'>
                                            <a href='univers-ricardo' title=''><img src='global/global_5649/img/ads/apropos.jpg' alt='A propos de Ricardo' title='A propos de Ricardo'><span class='pictExtra'><span></span></span></a>
                                            
                                        </div>
                                        <div class='desc'>
                                            <h3 class='title'></h3>
                                            <ul>
                                                <li><a href='/univers-ricardo/tout-sur-ricardo/227-biographie-de-ricardo' title='Biographie'>Biographie</a></li>
                                                <li><a href='/univers-ricardo/nouvelles/' title='Nouvelles'>Nouvelles</a></li>
                                                <li><a href='univers-ricardo/en-coulisses/' title='En coulisses'>En coulisses</a></li>                                            </ul>
                                        </div>
                                        <div class='clearer'></div>
                                    </div>
                                </div>
                            </div>
                            
                            
                                                        <div class='col5'>
                                <div class='fMagazine'>
                                    <h2><a href='magazine' title=''>Magazine</a></h2>
                                    <div class='unit hUnit uAlt'>
                                        <div class='pict'>
                                            <a href='magazine' title=''><img src='pictures/cache/ae2c905312eec5e0581c9ef899240666_w80_h108_sc.jpg' alt='Magasine Ricardo' title='Magasine Ricardo'> <span class='pictExtra'><span></span></span></a>
                                           
                                        </div>
                                        <div class='desc'>
                                            <h3 class='title'><a href='magazine' title=''>En kiosque dès maintenant!</a></h3>
                                            
											
											
																						
											<ul>
                                                <li><a href='magazine#enkiosque'>Aperçu</a></li>
                                                <li><a href='magazine'>Abonnement</a></li>
                                                <li><a href='magazine'>Primes</a></li>
                                            </ul>
                                        
																					
										</div>
                                        <div class='clearer'></div>
                                    </div>
                                </div>				
                            </div>
                                                    </div>
                        
                                                <div class='refLine'>
                        	
                            <div class='col4'>
                            	                                <div class='fNewsletter'>
                                    <h2 class='title'>Infolettre</h2>
                                    <p class='intro'>Abonnez-vous pour recevoir des recettes, des chroniques, des concours et plus encore!</p>
                                    <form action='infolettre' method='post' class='singleInputForm' id='signup-newsletter'>
                                        <div class='frmElmnt fTxt frmAlt noLabel'>
                                            <div class='fLbl'><label for='premail'>Adresse courriel</label></div>
                                            <div class='fWdgt'>
                                                <input placeholder='Adresse courriel' type='email' name='premail' id='premail' data-errormessage-value-missing='Veuillez entrer votre adresse courriel' data-errormessage-type-mismatch='Votre adresse courriel est invalide'>
                                                <button class='btn' type='submit' value='Submit'>M'abonner</button>
                                            </div>
                                            <div class='clearer'><!-- --></div>
                                        </div>						
                                    </form>
                                </div>
                                                                <div class='clearer' style='height:15px;'><!-- --></div>
                            </div>
														
							<div class='col5'>
                                <div class='fSocial'>
                                    <h2>SUIVEZ RICARDO</h2>
                                    <p>Restez connecté à la cuisine de Ricardo en tout temps!</p>
                                    
                                    <div class='fbPageLike'>
                                        												<iframe src='https://www.facebook.com/plugins/likebox.php?id=72128548261&amp;width=292&amp;height=62&amp;colorscheme=light&amp;show_faces=false&amp;header=false&amp;stream=false&amp;show_border=false&amp;locale=fr_FR' scrolling='no' frameborder='0' style='border:none; overflow:hidden; width:235px; height:62px;' allowtransparency='true'></iframe>
                                    </div>
                                    
                                    <div class='socialMediaLinks'>
                                        <ul>
                                            <li><a href='https://www.facebook.com/lacuisinedericardo' title='Facebook' class='smlFacebook'>Visitez notre page Facebook</a></li>
                                            <li><a href='https://twitter.com/Ricardocuisine' title='Twitter' class='smlTwitter'>Suivez-nous sur Twitter</a></li>
                                            <li><a href='https://instagram.com/ricardocuisine' title='Instagram' class='smlInstagram'>Suivez-nous sur Instagram</a></li>
                                        </ul>
                                    </div>
                                </div>
                            </div>
							                        </div>
                    </div>
                </div>
            </div>
        </div>
        
        <!--<div class='subWrap footerBanner'>
                    </div>-->
		
		<footer class='subWrap'>
			<div class='lining'>
				
				<ul class='brandShotcuts'>
					<li class='bsItem_ricardocuisine'><a href='http://www.ricardocuisine.com' title=''>Ricardocuisine.com</a></li>
					<li class='bsItem_larriveevinsdumonde'><a href='http://www.ricardocuisine.com/vins' title='' target='_blank'>Larrivée vins du monde</a></li>
					<li class='bsItem_mamachoka'><a href='http://www.mamachoka.com/fr/' title='' target='_blank'>Mama Choka</a></li>
					<li class='bsItem_espacericardo'><a href='http://www.espacericardo.com' title='' target='_blank'>Espace Ricardo</a></li>
				</ul>			
                <ul>
                    <li>Annoncer: <a href='pdf/fr_kit_web2013.pdf' title=''>Web</a>
					/
					<a href='pdf/Ricardo_Mediakit_FR_Aug25.pdf' title=''>Magazine</a></li>
                    <li><a href='contactez-nous' title='Contactez-nous'>Contactez-nous</a></li>
                    <li><a href='politique-de-confidentialite' title='Politique de confidentialité'>Politique de confidentialité</a></li>
                    <li><a href='plan-du-site' title='Plan du site'>Plan du site</a></li>
                </ul>
				
			    <p class='copy'>© 2015 Ricardo Media Inc. Tous droits réservés.</p>
				
			</div>
		</footer>
    </div> <!-- /.wrapFooter-->
	    
</div><div id='_atssh' style='visibility: hidden; height: 1px; width: 1px; position: absolute; top: -9999px; z-index: 100000;'><iframe id='_atssh305' title='AddThis utility frame' src='http://s7.addthis.com/static/sh.e98bf07d.html#iit=1437356474934&amp;tmr=load%3D1437356471417%26core%3D1437356472830%26main%3D1437356474907%26ifr%3D1437356474948&amp;cb=0&amp;cdn=0&amp;kw=&amp;ab=-&amp;dh=www.ricardocuisine.com&amp;dr=&amp;du=http%3A%2F%2Fwww.ricardocuisine.com%2Frecettes%2F5409-pouding-au-chocolat&amp;href=http%3A%2F%2Fwww.ricardocuisine.com%2Frecettes%2F5409-pouding-au-chocolat&amp;dt=Pouding%20au%20chocolat%20Recettes%20%7C%20Ricardo&amp;dbg=0&amp;cap=tc%3D0%26ab%3D0&amp;inst=2&amp;jsl=1&amp;prod=undefined&amp;lng=fr&amp;ogt=url%2Csite_name%2Cimage%2Ctitle&amp;pc=men&amp;pub=synchwithus&amp;ssl=0&amp;sid=55ac51b7376a0505&amp;srpl=1&amp;srcs=1&amp;srd=1&amp;srf=0.01&amp;srx=1&amp;ver=300&amp;xck=0&amp;xtr=0&amp;og=title%3DPouding%2520au%2520chocolat%26image%3Dhttp%253A%252F%252Fwww.ricardocuisine.com%252Fpictures%252Fcache%252Ff42e29ee36a20b495128fd3f33792edc_w1200_h630_cp_sc.jpg%26site_name%3DRicardocuisine%26url%3Dhttp%253A%252F%252Fwww.ricardocuisine.com%252Frecettes%252F5409-pouding-au-chocolat&amp;aa=0&amp;csi=undefined&amp;toLoJson=&amp;rev=v2.2.1-wp&amp;ct=1&amp;xld=1&amp;xd=1' style='height: 1px; width: 1px; position: absolute; top: 0px; z-index: 100000; border: 0px; left: 0px;'></iframe></div> <!-- /.wrapper -->

<div id='popover-container'></div>");
        }
    }
}