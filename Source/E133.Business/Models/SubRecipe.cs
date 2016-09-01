using Newtonsoft.Json;

namespace E133.Business.Models
{
    public class Subrecipe
    {
        [JsonProperty("id")]
        public int SubrecipeId { get; set; }
		
        public string Title { get; set; }
    }
}