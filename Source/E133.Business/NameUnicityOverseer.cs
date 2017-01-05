using System.Threading.Tasks;

namespace E133.Business
{
    internal class NameUnicityOverseer : INameUnicityOverseer
    {
        private readonly IQuickRecipeRepository _repo;

        public NameUnicityOverseer(IQuickRecipeRepository repo)
        {
            this._repo = repo;
        }

        public async Task<string> GenerateUniqueName(string name)
        {
            var generatedName = name;
            var recipeWithName = await this._repo.GetByUniqueNameAsync(generatedName);
            var uniqueId = 0;

            while (recipeWithName != null && uniqueId < 99)
            {
                uniqueId++;
                generatedName = $"{name}-{uniqueId}";
                recipeWithName = await this._repo.GetByUniqueNameAsync(generatedName);
            }
            
            return generatedName;
        }
    }
}
