using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

using E133.Business.Models;

using Newtonsoft.Json;

namespace E133.Business.Repositories
{
    public class ApiRepository : IQuickRecipeRepository
    {
        public async Task<QuickRecipe> GetAsync(string id)
        {
            QuickRecipe recipe = null;
            var url = "https://api.mlab.com/api/1/databases/e133/collections/quickrecipe/" + id + "?apiKey=tEW3mV3EqhPQo-IVY2je7cL5Zo0ztYQy";
            using (var client = new HttpClient())
            {
                var data = await client.GetAsync(url);
                if (data.IsSuccessStatusCode)
                {
                    var content = await data.Content.ReadAsStringAsync();
                    
                    recipe = JsonConvert.DeserializeObject<QuickRecipe>(content);
                }
            }
            
            return recipe;
        }

        public async Task<bool> InsertAsync(QuickRecipe recipe)
        {
            var result = false;
            var url = "https://api.mlab.com/api/1/databases/e133/collections/quickrecipe?apiKey=tEW3mV3EqhPQo-IVY2je7cL5Zo0ztYQy";
            using (var client = new HttpClient())
            {
                var stringContent = JsonConvert.SerializeObject(recipe);
                using (var content = new StringContent(stringContent))
                {
                    var data = await client.PostAsync(url, content);
                    if (data.IsSuccessStatusCode)
                    {
                        result = true;
                    }
                }
            }
            
            return result;
        }

        public async Task<bool> UpdateAsync(QuickRecipe recipe)
        {
            var result = false;
            var url = "https://api.mlab.com/api/1/databases/e133/collections/quickrecipe?apiKey=tEW3mV3EqhPQo-IVY2je7cL5Zo0ztYQy";
            using (var client = new HttpClient())
            {
                var stringContent = JsonConvert.SerializeObject(recipe);
                using (var content = new StringContent(stringContent))
                {
                    var data = await client.PutAsync(url, content);
                    if (data.IsSuccessStatusCode)
                    {
                        result = true;
                    }
                }
            }
            
            return result;
        }

        public async Task<IEnumerable<QuickRecipe>> SearchAsync(string query)
        {
            var recipes = new List<QuickRecipe>();

            var url = "https://api.mlab.com/api/1/databases/e133/collections/quickrecipe?apiKey=tEW3mV3EqhPQo-IVY2je7cL5Zo0ztYQy";
            using (var client = new HttpClient())
            {
                var data = await client.GetAsync(url);
                if (data.IsSuccessStatusCode)
                {
                    var content = await data.Content.ReadAsStringAsync();
                    
                    recipes = JsonConvert.DeserializeObject<List<QuickRecipe>>(content);
                }
            }
            
            return recipes;
        }
    }
}