using System;

using E133.Business.Models;

namespace E133.Business
{
    public interface IRecipeNameGenerator
    {
        string GenerateName(QuickRecipe recipe);
    }
}
