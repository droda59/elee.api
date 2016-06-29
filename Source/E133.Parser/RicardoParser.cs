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
            Func<CultureInfo, IActionDetector> actionDetectorFactory, 
            Func<CultureInfo, ITimerDetector> timerDetectorFactory,
            Func<CultureInfo, IIngredientDetector> ingredientDetectorFactory,
            Func<CultureInfo, IMeasureUnitDetector> measureUnitDetectorFactory,
            Func<CultureInfo, ILanguageHelper> languageHelperFactory,
            Func<CultureInfo, ISubrecipeRepository> subrecipeRepositoryFactory) 
            : base(htmlLoader, actionDetectorFactory, timerDetectorFactory, ingredientDetectorFactory, measureUnitDetectorFactory, languageHelperFactory, subrecipeRepositoryFactory)
        {
        }
        
        public override bool IsRecipePage(Uri uri)
        {
            return uri.ToString().Contains("recette") || uri.ToString().Contains("recipe");
        }

        protected override string GetRecipeIetfLanguage(HtmlDocument document)
        {
            return document.DocumentNode
                .SelectSingleNode("//html").Attributes["lang"].Value.Trim();
        }

        protected override string GetRecipeTitle(HtmlDocument document)
        {
            return document.DocumentNode
                .SelectSingleNode("//meta[@name='description']").Attributes["content"].Value.Trim();
        }

        protected override string GetImageUrl(HtmlDocument document)
        {
            return document.DocumentNode
                .SelectSingleNode(".//div[@class='itemDetail']")
                .SelectSingleNode(".//div[@class='pict']")
                .SelectSingleNode(".//img")
                .Attributes["src"].Value.Trim();
        }

        protected override string GetNote(HtmlDocument document)
        {
            var tipsNodes = document.DocumentNode
                .SelectSingleNode(".//section[@class='tips']")
                .SelectNodes(".//p")
                .Select(x => x.InnerText)
                .ToList();
                    
            return string.Join(string.Empty, tipsNodes);
        }

        protected override string GetRecipeYield(HtmlDocument document)
        {
            var yieldNode = document.DocumentNode
                .SelectSingleNode(".//div[@class='itemDetail']")
                .SelectSingleNode(".//div[@class='desc']")
                .SelectSingleNode(".//dl")
                .SelectSingleNode(".//dd[@itemprop='recipeYield']");

            if (yieldNode != null)
            {
                return yieldNode.InnerText;
            }

            return string.Empty;
        }

        protected override IEnumerable<Duration> GetDurations(HtmlDocument document)
        {
            var nodes = document.DocumentNode
                .SelectSingleNode(".//div[@class='itemDetail']")
                .SelectSingleNode(".//div[@class='desc']")
                .SelectSingleNode(".//dl")
                .ChildNodes;

            var node = nodes.First();
            while (node != null)
            {
                // TODO Temp fix, localize and do better
                if (node.Name == "dt" && node.InnerText != "Portions")
                {
                    var title = node.InnerText.Trim();
                    var infoContent = node.NextSibling.NextSibling.SelectSingleNode(".//meta[@itemprop]");
                    if (infoContent != null)
                    {
                        var time = infoContent.Attributes["content"].Value;
                        yield return new Duration
                        {
                            Title = title, 
                            Time = time
                        };
                    }
                }

                node = node.NextSibling;
            }
        }

        protected override HtmlNode GetIngredientSection(HtmlDocument document)
        {
            return document.DocumentNode
                .SelectSingleNode("//section[@class='ingredients']")
                .SelectSingleNode(".//div[@class='frmInnerWrap']");
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

        protected override HtmlNodeCollection GetSubrecipeIngredientNodesFromParent(HtmlNode parent)
        {
            return this.GetIngredientNodesFromParent(parent.NextSibling.NextSibling);
        }

        protected override HtmlNodeCollection GetIngredientNodesFromParent(HtmlNode parent)
        {
            return parent
                .SelectNodes(".//li//label[@itemprop='ingredients']//span");
        }

        protected override HtmlNodeCollection GetStepSections(HtmlDocument document)
        {
            return document.DocumentNode
                .SelectNodes("//section[@itemprop='recipeInstructions']//h3");
        }

        protected override HtmlNodeCollection GetSubrecipeSteps(HtmlNode stepSubrecipeNode)
        {
            return stepSubrecipeNode.NextSibling.NextSibling
                .SelectNodes(".//li//span");
        }
    }
}