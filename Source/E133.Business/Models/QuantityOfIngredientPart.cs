namespace E133.Business.Models
{
    public class QuantityOfIngredientPart : Part
    {
        public override string Type
        {
            get { return "quantity"; }
            set {}
        }

        public Quantity Quantity { get; set; }

        public Ingredient Ingredient { get; set; }

        internal override string DebuggerDisplay
        {
            get { return string.Format("{0} {1} of {2}", this.Quantity.Value, this.Quantity.Abbreviation, this.Ingredient.Name); }
        }
    }
}