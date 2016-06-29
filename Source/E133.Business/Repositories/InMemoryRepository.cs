using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using E133.Business.Models;

namespace E133.Business.Repositories
{
    internal class InMemoryRepository : IQuickRecipeRepository
    {
        private readonly static IDictionary<string, QuickRecipe> _knownRecipes = new Dictionary<string, QuickRecipe>();

        public Task<QuickRecipe> GetAsync(string id)
        {
            var task = Task.Run(() => _knownRecipes[id]);
            task.Wait();

            return task;
        }

        public Task<bool> UpdateAsync(QuickRecipe recipe)
        {
            var task = Task.Run(
                () =>
                {
                    _knownRecipes[recipe.Id] = recipe;
                    return true;
                });
            task.Wait();

            return task;
        }

        public Task<bool> InsertAsync(QuickRecipe recipe)
        {
            var task = Task.Run(
                () =>
                {
                    _knownRecipes[recipe.Id] = recipe;
                    return true;
                });
            task.Wait();

            return task;
        }

        public Task<IEnumerable<QuickRecipeSearchResult>> SearchAsync(string query)
        {
            var task = Task.Run(() =>
                _knownRecipes.Values.Select(x => new QuickRecipeSearchResult { Id = x.Id, Title = x.Title, ImageUrl = x.ImageUrl }));
            task.Wait();

            return task;
        }
    }
}