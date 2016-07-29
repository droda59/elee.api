using E133.Business;

namespace E133.Parser.LanguageUtilities.French
{
    internal class FrenchMeasureUnitDetector : IMeasureUnitDetector
    {
        public MeasureUnit GetMeasureUnit(string measureUnit)
        {
            switch (measureUnit)
            {
                case "ml": return MeasureUnit.Millilitre;
                case "cl": return MeasureUnit.Centilitre;
                case "dl": return MeasureUnit.Decilitre;
                case "l": return MeasureUnit.Litre;

                case "c. à thé":
                case "tsp": return MeasureUnit.Teaspoon;

                case "c. à table":
                case "c. à café":
                case "tbsp": return MeasureUnit.Tablespoon;

                case "once":
                case "onces": return MeasureUnit.Ounce;

                case "tasse":
                case "tasses": return MeasureUnit.Cup;

                case "g": return MeasureUnit.Gram;
                case "kg": return MeasureUnit.Kilogram;

                case "livre":
                case "livres":
                case "lb":
                case "lbs": return MeasureUnit.Pound;

                case "pincée":
                case "pincées": return MeasureUnit.Pinch;

                default: return MeasureUnit.Unit;
            }
        }
    }
}