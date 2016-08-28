using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using E133.Business;

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
