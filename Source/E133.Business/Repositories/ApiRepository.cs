using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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
                using (var content = new StringContent(stringContent, Encoding.UTF8, "application/json"))
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
                
                using (var content = new StringContent(stringContent, Encoding.UTF8, "application/json"))
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

        public async Task<IEnumerable<QuickRecipeSearchResult>> SearchAsync(string query)
        {
            var recipes = new List<QuickRecipeSearchResult>();

            var url = "https://api.mlab.com/api/1/databases/e133/collections/quickrecipe?apiKey=tEW3mV3EqhPQo-IVY2je7cL5Zo0ztYQy";
            using (var client = new HttpClient())
            {
                var data = await client.GetAsync(url);
                if (data.IsSuccessStatusCode)
                {
                    var content = await data.Content.ReadAsStringAsync();
                    
                    recipes = JsonConvert.DeserializeObject<List<QuickRecipe>>(content)
                        .Where(x => string.IsNullOrEmpty(query) || x.Title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                        .Where(x => x.WasReviewed).ToList()
                        .Select(x => 
                            new QuickRecipeSearchResult 
                            { 
                                Id = x.Id, 
                                Title = x.Title, 
                                Durations = x.Durations, 
                                SmallImageUrl = x.SmallImageUrl, 
                                Ingredients = x.Ingredients 
                            })
                        .ToList();
                }
            }
            
            return recipes;
        }
    }
}