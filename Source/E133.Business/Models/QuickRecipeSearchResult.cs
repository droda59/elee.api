using System.Collections.Generic;

namespace E133.Business.Models
{
    public class QuickRecipeSearchResult : Document
    {
        public QuickRecipeSearchResult ()
        {
            this.Ingredients = new List<Ingredient>();
            this.Durations = new List<Duration>();
        }

        public string Title { get; set; }

        public string SmallImageUrl { get; set; }
		
		public IList<Duration> Durations { get; set; }

        public IEnumerable<Ingredient> Ingredients { get; set; }
    }
}