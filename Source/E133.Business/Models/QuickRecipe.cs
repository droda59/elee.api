using System.Collections.Generic;

using Newtonsoft.Json;

namespace E133.Business.Models
{
    public class QuickRecipe : Document
	{
        public QuickRecipe()
        {
            this.Categories = new List<string>();
            this.Durations = new List<Duration>();
            this.Subrecipes = new List<Subrecipe>();
            this.Ingredients = new List<Ingredient>();
            this.Steps = new List<Step>();
        }

        [JsonIgnore]
        public bool WasReviewed { get; set; }

        [JsonIgnore]
        public bool MarkedForReview { get; set; }

		public string Title { get; set; }

		public string UniqueName { get; set; }

        public string OriginalUrl { get; set; }
        
        public string Language { get; set; }
        
        public string Note { get; set; }
        
        public string SmallImageUrl { get; set; }
        
        public string NormalImageUrl { get; set; }
        
        public string LargeImageUrl { get; set; }
		
		public string Summary { get; set; }
		
		public string OriginalServings { get; set; }

        public IList<string> Categories { get; set; }
		
		public IList<Duration> Durations { get; set; }

        public IList<Subrecipe> Subrecipes { get; set; }

        public IList<Ingredient> Ingredients { get; set; }

        public IList<Step> Steps { get; set; }
	}
}