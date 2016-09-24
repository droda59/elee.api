using System.Collections.Generic;
using System.Threading.Tasks;

using E133.Business.Models;

namespace E133.Business
{
    public interface IQuickRecipeRepository
    {
        Task<IEnumerable<QuickRecipeSearchResult>> GetAsync();
        
        Task<QuickRecipe> GetAsync(string id);

        Task<bool> UpdateAsync(string id, QuickRecipe recipe);

        Task<bool> InsertAsync(QuickRecipe recipe);

        Task<IEnumerable<QuickRecipeSearchResult>> SearchAsync(string query);
    }
}