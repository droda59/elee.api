using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

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
        [Route("api/quickrecipe/search/review")]
        public async Task<IEnumerable<QuickRecipeSearchResult>> Reviewed(bool reviewed)
        {
            var recipes = await this._repo.GetReviewedAsync(reviewed);

            return recipes;
        }

        [HttpGet]
        [Route("api/quickrecipe/search")]
        public async Task<IEnumerable<QuickRecipeSearchResult>> Get(string query)
        {
            var results = await this._repo.SearchAsync(query);

            return results.ToList();
        }

        [HttpGet]
        [Route("api/quickrecipe/advancedsearch")]
        public async Task<IEnumerable<QuickRecipeSearchResult>> AdvancedSearch(
            IEnumerable<string> includedIngredients, 
            IEnumerable<string> excludedIngredients, 
            IEnumerable<string> categories,
            string maximumTime,
            string query)
        {
            var results = await this._repo.SearchAsync(query);
            var maximumTimeIsoNotation = (TimeSpan?)null;

            try 
            {
                maximumTimeIsoNotation = XmlConvert.ToTimeSpan(maximumTime);
            }
            catch (FormatException)
            {
                maximumTimeIsoNotation = TimeSpan.MaxValue;
            }

            return results
                // .Where(x => x.Ingredients.Select(y => y.Name).Intersect(includedIngredients).Count() == x.Ingredients.Count())
                // .Where(x => x.Ingredients.Select(y => y.Name).Except(excludedIngredients).Count() == x.Ingredients.Count())
                .Where(x => new TimeSpan(x.Durations.Sum(y => XmlConvert.ToTimeSpan(y.Time).Ticks)) <= maximumTimeIsoNotation.Value)
                .ToList();
        }
    }
}