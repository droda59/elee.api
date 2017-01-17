using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
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

        [HttpGet("{uniqueName}")]
        public async Task<QuickRecipe> Get(string uniqueName)
        {
            return await this._repo.GetByUniqueNameAsync(uniqueName);
        }

        [HttpPost]
        [Authorize(Policy = "LocalOnly")]
        public async Task<bool> Post([FromBody]QuickRecipe recipe)
        {
            return await this._repo.InsertAsync(recipe);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "Admin")]
        public async Task<bool> Put(string id, [FromBody]QuickRecipe recipe)
        {
            recipe.WasReviewed = true;
            recipe.MarkedForReview = false;
            
            return await this._repo.UpdateAsync(id, recipe);
        }
    }
}
