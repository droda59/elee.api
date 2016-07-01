using System.Collections.Generic;
using System.Threading.Tasks;

using E133.Business.Models;

namespace E133.Business
{
    public interface IQuickRecipeRepository
    {
        Task<QuickRecipe> GetAsync(string id);

        Task<bool> UpdateAsync(QuickRecipe recipe);

        Task<bool> InsertAsync(QuickRecipe recipe);

        Task<IEnumerable<QuickRecipe>> SearchAsync(string query);
    }
}