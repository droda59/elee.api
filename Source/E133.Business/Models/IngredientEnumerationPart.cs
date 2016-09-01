using System.Collections.Generic;

namespace E133.Business.Models
{
    public class IngredientEnumerationPart : Part
    {
        public IngredientEnumerationPart()
        {
            this.Ingredients = new List<Ingredient>();
        }

        public override string Type
        {
            get { return "enumeration"; }
            set {}
        }

        public IList<Ingredient> Ingredients { get; set; }

        internal override string DebuggerDisplay
        {
            get
            {
                return string.Join(", ", this.Ingredients);
            }
        }
    }
}
