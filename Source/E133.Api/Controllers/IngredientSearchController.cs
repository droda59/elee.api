using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using E133.Business;

namespace E133.Api.Controllers
{
    [Route("api/ingredient/search")]
    public class IngredientSearchController : Controller
    {
        private readonly IQuickRecipeRepository _repo;

        public IngredientSearchController(IQuickRecipeRepository repo)
        {
            this._repo = repo;
        }

        [HttpGet]
        public async Task<IEnumerable<string>> Get(string query)
        {
            var allRecipes = await this._repo.SearchAsync(string.Empty);
            var allIngredients = allRecipes.SelectMany(x => x.Ingredients).Select(x => x.Name).Distinct();

            var ingredients = allIngredients.Where(x => x.Contains(query)).ToList();

            return allIngredients;
        }
    }
}
