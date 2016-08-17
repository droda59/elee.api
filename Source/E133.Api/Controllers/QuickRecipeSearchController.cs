using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using E133.Business;
using E133.Business.Models;

namespace E133.Api.Controllers
{
    public class QuickRecipeSearchController : Controller
    {
        private readonly IQuickRecipeRepository _repo;

        public QuickRecipeSearchController(IQuickRecipeRepository repo)
        {
            this._repo = repo;
        }

        [HttpGet]
        [Route("api/quickrecipe/search")]
        public async Task<IEnumerable<QuickRecipeSearchResult>> Get(string query)
        {
            var results = await this._repo.SearchAsync(query);

            return results;
        }

        [HttpGet]
        [Route("api/quickrecipe/advancedsearch")]
        public async Task<IEnumerable<QuickRecipeSearchResult>> AdvancedSearch(
            IEnumerable<string> includedIngredients, 
            IEnumerable<string> excludedIngredients, 
            IEnumerable<string> categories,
            string query)
        {
            var results = await this._repo.SearchAsync(query);

            return results
                .Where(x => x.Ingredients.Select(y => y.Name).Intersect(includedIngredients).Count() == x.Ingredients.Count())
                .Where(x => x.Ingredients.Select(y => y.Name).Except(excludedIngredients).Count() == x.Ingredients.Count())
                .Select(x => CreateSearchResult(x));
        }

        private QuickRecipeSearchResult CreateSearchResult(QuickRecipe quickRecipe)
        {
            return new QuickRecipeSearchResult 
            { 
                Id = quickRecipe.Id, 
                Title = quickRecipe.Title, 
                SmallImageUrl = quickRecipe.SmallImageUrl 
            };
        }
    }
}