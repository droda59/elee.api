using System.Collections.Generic;
using System.Threading.Tasks;

using E133.Business;
using E133.Business.Models;
using E133.Database;

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

            // TODO Add WasReviewd filter
            var results = new List<QuickRecipeSearchResult>();
            using (var cursor = await this.Collection.FindAsync(Builders<QuickRecipe>.Filter.Text(query)))
            {
                while (await cursor.MoveNextAsync())
                {
                    foreach (var document in cursor.Current)
                    {
                        if (document.WasReviewed)
                        {
                            results.Add(
                                new QuickRecipeSearchResult 
                                { 
                                    Id = document.Id, 
                                    Title = document.Title, 
                                    Durations = document.Durations, 
                                    SmallImageUrl = document.SmallImageUrl, 
                                    Ingredients = document.Ingredients 
                                });
                        }
                    }
                }
            }

            return results;
        }
    }
}