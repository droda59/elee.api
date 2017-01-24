using System.Collections.Generic;
using System.Threading.Tasks;

using E133.Business.Models;

namespace E133.Business
{
    public interface IQuickRecipeRepository
    {
        Task<IEnumerable<QuickRecipeSearchResult>> GetPaged(int skip, int take);

        Task<IEnumerable<QuickRecipeSearchResult>> GetReviewedAsync(bool wasReviewed);
        
        Task<QuickRecipe> GetAsync(string id);

        Task<QuickRecipe> GetByUniqueNameAsync(string uniqueName);

        Task<QuickRecipe> GetByUrlAsync(string url);

        Task<bool> UpdateAsync(string id, QuickRecipe recipe);

        Task<bool> InsertAsync(QuickRecipe recipe);

        Task<IEnumerable<QuickRecipeSearchResult>> SearchAsync(string query);
    }
}