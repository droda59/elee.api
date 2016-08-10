using System;
using System.Linq;

using E133.Business;

namespace E133.Parser.LanguageUtilities.English
{
    internal class EnglishMeasureUnitDetector : IMeasureUnitDetector
    {
        public MeasureUnit GetMeasureUnit(string measureUnit)
        {
            switch (measureUnit)
            {
                case "ml": return MeasureUnit.Millilitre;
                case "cl": return MeasureUnit.Centilitre;
                case "dl": return MeasureUnit.Decilitre;
                case "l": return MeasureUnit.Litre;

                case "teaspoon":
                case "tsp": return MeasureUnit.Teaspoon;

                case "tablespoon":
                case "tbsp": return MeasureUnit.Tablespoon;

                case "oz":
                case "ounce":
                case "ounces": return MeasureUnit.Ounce;

                case "cup":
                case "cups": return MeasureUnit.Cup;

                case "g": return MeasureUnit.Gram;
                case "kg": return MeasureUnit.Kilogram;

                case "pound":
                case "pounds":
                case "lb":
                case "lbs": return MeasureUnit.Pound;

                case "pinch": return MeasureUnit.Pinch;

                default: return MeasureUnit.Unit;
            }
        }

        public ILookup<MeasureUnit, string> MeasureUnitsInString
        {
            get 
            {
                throw new NotImplementedException();
            }
        }
    }
}