using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using E133.Business;
using E133.Business.Models;

namespace E133.Api.Controllers
{
    [Route("api/review")]
    public class ReviewController : Controller
    {
        private readonly IQuickRecipeRepository _repo;

        public ReviewController(IQuickRecipeRepository repo)
        {
            this._repo = repo;
        }

        [HttpGet]
        public async Task<IEnumerable<QuickRecipeSearchResult>> Get()
        {
            var recipes = await this._repo.GetAsync();

            return recipes;
        }

        [HttpPut]
        [Route("flag/{id}")]
        public async Task<bool> MarkForReview(string id)
        {
            var recipe = await this._repo.GetAsync(id);
            recipe.WasReviewed = false;
            recipe.MarkedForReview = true;

            var updateSuccessful = await this._repo.UpdateAsync(recipe);

            return updateSuccessful;
        }
    }
}
