namespace E133.Business.Models
{
    public class IngredientPart : Part
    {
        public override string Type
        {
            get { return "ingredient"; }
            set {}
        }

        public Ingredient Ingredient { get; set; }

        internal override string DebuggerDisplay
        {
            get { return this.Ingredient.Name; }
        }
    }
}