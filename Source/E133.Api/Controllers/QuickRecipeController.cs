using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using E133.Business;
using E133.Business.Models;

namespace E133.Api.Controllers
{
    [Route("api/[controller]")]
    public class QuickRecipeController : Controller
    {
        private readonly IQuickRecipeRepository _repo;

        public QuickRecipeController(IQuickRecipeRepository repo)
        {
            this._repo = repo;
        }

        [HttpGet("{id}")]
        public async Task<QuickRecipe> Get(string id)
        {
            return await this._repo.GetAsync(id);
        }

        [HttpPost]
        public async Task<bool> Post([FromBody]QuickRecipe recipe)
        {
            return await this._repo.InsertAsync(recipe);
        }

        [HttpPut("{id}")]
        public async Task<bool> Put(string id, [FromBody]QuickRecipe recipe)
        {
            recipe.WasReviewed = true;
            recipe.MarkedForReview = false;
            
            return await this._repo.UpdateAsync(id, recipe);
        }
    }
}
