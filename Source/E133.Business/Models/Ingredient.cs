using Newtonsoft.Json;

namespace E133.Business.Models
{
    public class Ingredient 
    {
        [JsonProperty("id")]
        public long IngredientId { get; set; }
		
        public int SubrecipeId { get; set; }
		
        public string Name { get; set; }
		
        public Quantity Quantity { get; set; }
		
        public Ingredient Replacement { get; set; }
		
        public string Requirements { get; set; }
    }
}