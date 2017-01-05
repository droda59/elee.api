using System;
using System.Linq;

using E133.Business.Models;

namespace E133.Business
{
    internal class RecipeNameGenerator : IRecipeNameGenerator
    {
        public string GenerateName(QuickRecipe recipe)
        {
            var diacriticlessTitle = recipe.Title
                .RemoveDiacritics()
                .Replace("’", string.Empty)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty)
                .ToLowerInvariant();
            var splitTitle = diacriticlessTitle.SplitPhrase();
            var cleanTitle = string.Join("-", splitTitle.Where(x => x.Length > 2).ToList());

            var originalUri = new Uri(recipe.OriginalUrl);
            var authority = originalUri.Authority.Replace("www.", string.Empty).Replace(".com", string.Empty);
            
            return $"{authority}-{cleanTitle}";
        }
    }
}
