using System;
using System.Collections.Generic;
using System.Linq;

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

        public ILookup<MeasureUnit, string> MeasureUnitsInString
        {
            get 
            {
                var units = new List<Tuple<MeasureUnit, string>>();
                units.Add(Tuple.Create(MeasureUnit.Millilitre, "ml"));
                units.Add(Tuple.Create(MeasureUnit.Centilitre, "cl"));
                units.Add(Tuple.Create(MeasureUnit.Decilitre, "dl"));
                units.Add(Tuple.Create(MeasureUnit.Litre, "l"));
                units.Add(Tuple.Create(MeasureUnit.Teaspoon, "c. à thé"));
                units.Add(Tuple.Create(MeasureUnit.Teaspoon, "tsp"));
                units.Add(Tuple.Create(MeasureUnit.Tablespoon, "c. à table"));
                units.Add(Tuple.Create(MeasureUnit.Tablespoon, "c. à café"));
                units.Add(Tuple.Create(MeasureUnit.Tablespoon, "tbsp"));
                units.Add(Tuple.Create(MeasureUnit.Ounce, "once"));
                units.Add(Tuple.Create(MeasureUnit.Ounce, "onces"));
                units.Add(Tuple.Create(MeasureUnit.Cup, "tasse"));
                units.Add(Tuple.Create(MeasureUnit.Cup, "tasses"));
                units.Add(Tuple.Create(MeasureUnit.Gram, "g"));
                units.Add(Tuple.Create(MeasureUnit.Kilogram, "kg"));
                units.Add(Tuple.Create(MeasureUnit.Pound, "lb"));
                units.Add(Tuple.Create(MeasureUnit.Pound, "lbs"));
                units.Add(Tuple.Create(MeasureUnit.Pound, "livre"));
                units.Add(Tuple.Create(MeasureUnit.Pound, "livres"));
                units.Add(Tuple.Create(MeasureUnit.Pinch, "pincée"));
                units.Add(Tuple.Create(MeasureUnit.Pinch, "pincées"));

                return units.ToLookup(x => x.Item1, x => x.Item2);
            }
        }
    }
}