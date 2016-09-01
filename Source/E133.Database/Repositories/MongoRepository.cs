using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using E133.Business;
using E133.Business.Models;

using MongoDB.Driver;

namespace E133.Database.Repositories
{
    internal class MongoRepository : EntityRepository<QuickRecipe>, IQuickRecipeRepository
    {
        public async Task<QuickRecipe> GetAsync(string id)
        {
            return await this.Collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateAsync(QuickRecipe data)
        {
            var result = await this.Collection.FindOneAndReplaceAsync(x => x.Id == data.Id, data);

            return result != null;
        }

        public async Task<bool> InsertAsync(QuickRecipe data)
        {
            await this.Collection.InsertOneAsync(data);

            return true;
        }

        public async Task<IEnumerable<QuickRecipeSearchResult>> SearchAsync(string query)
        {
            await this.Collection.Indexes.CreateOneAsync(Builders<QuickRecipe>.IndexKeys.Text(x => x.Title));

            var builder = Builders<QuickRecipe>.Filter;
            var filter = builder.Eq(x => x.WasReviewed, true);
            if (!string.IsNullOrWhiteSpace(query))
            {
                filter &= builder.Text(query);
            }

            var results = await this.Collection.Find(filter).ToListAsync();
            return results
                .Select(document => 
                    new QuickRecipeSearchResult 
                    { 
                        Id = document.Id, 
                        Title = document.Title, 
                        Durations = document.Durations, 
                        SmallImageUrl = document.SmallImageUrl, 
                        Ingredients = document.Ingredients 
                    })
                .ToList();
        }
    }
}