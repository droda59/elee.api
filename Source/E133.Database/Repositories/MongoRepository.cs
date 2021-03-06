﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using E133.Business;
using E133.Business.Models;

using MongoDB.Driver;

namespace E133.Database.Repositories
{
    internal class MongoRepository : EntityRepository<QuickRecipe>, IQuickRecipeRepository
    {
        public MongoRepository(IOptions<MongoDBOptions> options)
            : base(options) 
        {
        }
        
        public async Task<IEnumerable<QuickRecipeSearchResult>> GetPaged(int skip = 0, int take = 0)
        {
            var builder = Builders<QuickRecipe>.Filter;
            var filter = builder.Eq(x => x.WasReviewed, true);

            var results = await this.Collection.Find(filter).Skip(skip).Limit(take).ToListAsync();

            return results
                .Select(CreateSearchResult)
                .ToList();
        }
        
        public async Task<IEnumerable<QuickRecipeSearchResult>> GetReviewedAsync(bool wasReviewed)
        {
            var builder = Builders<QuickRecipe>.Filter;
            var filter = builder.Eq(x => x.WasReviewed, wasReviewed);

            var results = await this.Collection.Find(filter).ToListAsync();

            return results
                .Select(CreateSearchResult)
                .ToList();
        }

        public async Task<QuickRecipe> GetAsync(string id)
        {
            return await this.Collection.Find(x => x.Id == id).FirstOrDefaultAsync();
        }

        public async Task<QuickRecipe> GetByUrlAsync(string url)
        {
            return await this.Collection.Find(x => x.OriginalUrl == url).FirstOrDefaultAsync();
        }

        public async Task<QuickRecipe> GetByUniqueNameAsync(string uniqueName)
        {
            return await this.Collection.Find(x => x.UniqueName == uniqueName).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateAsync(string id, QuickRecipe data)
        {
            var result = await this.Collection.FindOneAndReplaceAsync(x => x.Id == id, data);

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
                .Select(CreateSearchResult)
                .ToList();
        }

        private static QuickRecipeSearchResult CreateSearchResult(QuickRecipe document)
        {
            return new QuickRecipeSearchResult 
            { 
                Id = document.Id, 
                Title = document.Title, 
                UniqueName = document.UniqueName,
                Durations = document.Durations, 
                SmallImageUrl = document.SmallImageUrl, 
                Ingredients = document.Ingredients 
            };
        }
    }
}