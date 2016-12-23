using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using E133.Business;
using E133.Business.Bases;
using E133.Business.Models;
using E133.Parser.LanguageUtilities;

using HtmlAgilityPack;

namespace E133.Parser
{
    internal class RicardoParser : HtmlDocumentParser<RicardoBase>
    {
        public RicardoParser(
            IHtmlLoader htmlLoader,
            IRecipeNameGenerator nameGenerator, 
            INameUnicityOverseer nameUnicityOverseer,
            Func<CultureInfo, IActionDetector> actionDetectorFactory, 
            Func<CultureInfo, ITimerDetector> timerDetectorFactory,
            Func<CultureInfo, IMeasureUnitDetector> measureUnitDetectorFactory,
            Func<CultureInfo, ILanguageHelper> languageHelperFactory,
            Func<CultureInfo, ISubrecipeRepository> subrecipeRepositoryFactory) 
            : base(htmlLoader, nameGenerator, nameUnicityOverseer, actionDetectorFactory, timerDetectorFactory, measureUnitDetectorFactory, languageHelperFactory, subrecipeRepositoryFactory)
        {
        }

        protected override string GetRecipeIetfLanguage(HtmlDocument document)
        {
            return document.DocumentNode
                .SelectSingleNode("//html").Attributes["lang"].Value.Trim();
        }

        protected override string GetRecipeTitle(HtmlDocument document)
        {
            return document.DocumentNode
                .SelectSingleNode(".//div[@class='recipe-content']")
                .SelectSingleNode(".//h1")
                .InnerText.Trim();
        }

        protected override string GetSmallImageUrl(HtmlDocument document)
        {
            return document.DocumentNode
                .SelectSingleNode(".//div[@class='recipe-picture']")
                .SelectSingleNode(".//a")
                .SelectSingleNode(".//img")
                .Attributes["srcset"].Value.Split(',')[0].Replace("1x", string.Empty).Trim();
        }

        protected override string GetNormalImageUrl(HtmlDocument document)
        {
            return document.DocumentNode
                .SelectSingleNode(".//div[@class='recipe-picture']")
                .SelectSingleNode(".//a")
                .Attributes["href"].Value.Split('?')[0].Trim();
        }

        protected override string GetLargeImageUrl(HtmlDocument document)
        {
            return document.DocumentNode
                .SelectSingleNode(".//div[@class='recipe-bg']")
                .SelectSingleNode(".//img")
                .Attributes["src"].Value.Trim();
        }

        protected override string GetNote(HtmlDocument document)
        {
            var noteNode = document.DocumentNode.SelectSingleNode(".//section[@class='tips']");
            if (noteNode != null)
            {
                var tipsNodes = noteNode
                    .SelectNodes(".//div[@class='content']")
                    .Select(x => x.InnerText)
                    .ToList();
                        
                return string.Join(string.Empty, tipsNodes).Trim();
            }

            return string.Empty;
        }

        protected override string GetRecipeYield(HtmlDocument document)
        {
            var yieldNode = document.DocumentNode
                .SelectSingleNode(".//div[@class='recipe-content']")
                .SelectSingleNode(".//dl")
                .SelectNodes(".//dt")
                .SingleOrDefault(x => x.InnerText == "Portions" || x.InnerText == "Rendement");

            if (yieldNode != null)
            {
                return this.GetMatchingDdValue(yieldNode);
            }

            return string.Empty;
        }

        protected override IEnumerable<Duration> GetDurations(HtmlDocument document)
        {
            var nodes = document.DocumentNode
                .SelectSingleNode(".//div[@class='recipe-content']")
                .SelectSingleNode(".//dl")
                .SelectNodes(".//dd");

            var node = nodes.First();
            while (node != null)
            {
                var valueNode = node.SelectSingleNode(".//meta");
                if (valueNode != null)
                {
                    var descriptionNode = node.SelectSingleNode("preceding-sibling::*[1][self::dt]");
                    var title = descriptionNode.InnerText.Trim();

                    var time = valueNode.Attributes["content"].Value;
                    yield return new Duration
                    {
                        Title = title, 
                        Time = time
                    };
                }

                node = node.NextSibling;
            }
        }

        protected override HtmlNode GetIngredientSection(HtmlDocument document)
        {
            return document.DocumentNode
                .SelectSingleNode("//section[@class='ingredients']");
        }

        protected override HtmlNodeCollection GetSubrecipeNodes(HtmlDocument document)
        {
            return this.GetIngredientSection(document)
                .SelectNodes(".//h3");
        }

        protected override string GetSubrecipeTitle(HtmlNode subrecipeNode)
        {
            return subrecipeNode.InnerText.Trim();
        }

        protected override HtmlNodeCollection GetSubrecipeIngredientNodesFromParent(HtmlNode subrecipeNode)
        {
            var ingredientEnumeration = subrecipeNode
                .SelectSingleNode("following-sibling::*[1][self::ul]");
                
            return this.GetIngredientNodesFromParent(ingredientEnumeration);
        }

        protected override HtmlNodeCollection GetIngredientNodesFromParent(HtmlNode parent)
        {
            return parent
                .SelectNodes(".//li//label//span");
        }

        protected override HtmlNodeCollection GetStepSections(HtmlDocument document)
        {
            return document.DocumentNode
                .SelectNodes("//section[@id='preparation']//h2|//section[@id='preparation']//h3");
        }

        protected override HtmlNodeCollection GetSubrecipeSteps(HtmlNode stepSubrecipeNode)
        {
            return stepSubrecipeNode
                .SelectNodes(".//following-sibling::*[1][self::ol]");
        }
    }
}