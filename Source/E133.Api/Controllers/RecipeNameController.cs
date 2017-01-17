using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using E133.Business;

namespace E133.Api.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Policy = "LocalOnly")]
    public class RecipeNameController : Controller
    {
        private readonly IQuickRecipeRepository _repo;
        private readonly IRecipeNameGenerator _nameGenerator;
        private readonly INameUnicityOverseer _nameUnicityOverseer;

        public RecipeNameController(IQuickRecipeRepository repo, IRecipeNameGenerator nameGenerator, INameUnicityOverseer nameUnicityOverseer)
        {
            this._repo = repo;
            this._nameGenerator = nameGenerator;
            this._nameUnicityOverseer = nameUnicityOverseer;
        }

        [HttpGet("{id}")]
        public async Task<string> Get(string id)
        {
            var recipe = await this._repo.GetAsync(id);

            var generatedName = this._nameGenerator.GenerateName(recipe);
            var uniqueName = await this._nameUnicityOverseer.GenerateUniqueName(generatedName);
            
            return uniqueName;
        }

        [HttpPost("{id}")]
        public async Task<bool> Post(string id)
        {
            var recipe = await this._repo.GetAsync(id);

            var generatedName = this._nameGenerator.GenerateName(recipe);
            var uniqueName = await this._nameUnicityOverseer.GenerateUniqueName(generatedName);
            recipe.UniqueName = uniqueName;

            return await this._repo.UpdateAsync(id, recipe);
        }
    }
}
