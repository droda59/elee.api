using Xunit;

using E133.Business;
using E133.Parser.LanguageUtilities.French;

namespace E133.Parser.Tests
{
    public class IngredientParserTests
    {
        private RicardoParser _parser;

        public IngredientParserTests()
        {
            this._parser = new RicardoParser(null, x => null, x => null, x => new FrenchMeasureUnitDetector(), x => new FrenchLanguageHelper(), x => null);
            this._parser.InitializeCulture("fr");
        }

        [Theory]
        [InlineData("250 ml de jus de tomate", MeasureUnit.Millilitre)]
        [InlineData("25 cl de jus de tomate", MeasureUnit.Centilitre)]
        [InlineData("2 dl de jus de tomate", MeasureUnit.Decilitre)]
        [InlineData("1 l de jus de tomate", MeasureUnit.Litre)]
        [InlineData("2 c. à thé de jus de tomate", MeasureUnit.Teaspoon)]
        [InlineData("2 tsp de jus de tomate", MeasureUnit.Teaspoon)]
        [InlineData("3 c. à table de jus de tomate", MeasureUnit.Tablespoon)]
        [InlineData("3 c. à café de jus de tomate", MeasureUnit.Tablespoon)]
        [InlineData("3 tbsp de jus de tomate", MeasureUnit.Tablespoon)]
        [InlineData("1 once de jus de tomate", MeasureUnit.Ounce)]
        [InlineData("2 onces de jus de tomate", MeasureUnit.Ounce)]
        [InlineData("1 tasse de jus de tomate", MeasureUnit.Cup)]
        [InlineData("2 tasses de jus de tomate", MeasureUnit.Cup)]
        [InlineData("250 g de boeuf haché", MeasureUnit.Gram)]
        [InlineData("1 kg de boeuf haché", MeasureUnit.Kilogram)]
        [InlineData("1 livre de boeuf haché", MeasureUnit.Pound)]
        [InlineData("2 livres de boeuf haché", MeasureUnit.Pound)]
        [InlineData("1 lb de boeuf haché", MeasureUnit.Pound)]
        [InlineData("2 lbs de boeuf haché", MeasureUnit.Pound)]
        [InlineData("1 pincée de sel", MeasureUnit.Pinch)]
        [InlineData("2 pincées de sel", MeasureUnit.Pinch)]
        [InlineData("2 whatever de whatever", MeasureUnit.Unit)]
        public void ParseIngredientFromString_Units_ReturnsResult(string ingredientString, MeasureUnit unit) 
        {
            var result = this._parser.ParseIngredientFromString(ingredientString);

            Assert.Equal(unit, result.Quantity.Abbreviation);
        }

        [Theory]
        [InlineData("0,25 tasse de jus de tomate", 0.25)]
        [InlineData("¼ tasse de jus de tomate", 0.25)]
        [InlineData("0,5 tasse de jus de tomate", 0.5)]
        [InlineData("½ tasse de jus de tomate", 0.5)]
        [InlineData("0,75 tasse de jus de tomate", 0.75)]
        [InlineData("¾ tasse de jus de tomate", 0.75)]
        public void ParseIngredientFromString_Fraction_ReturnsResult(string ingredientString, double quantity) 
        {
            var result = this._parser.ParseIngredientFromString(ingredientString);

            Assert.Equal(quantity, result.Quantity.Value);
        }

        [Theory]
        [InlineData("3 pommes de terre, pelées et coupées en deux", "pelées et coupées en deux")]
        [InlineData("2 oignons, coupés en 6 quartiers", "coupés en 6 quartiers")]
        public void ParseIngredientFromString_WithRequirements_ReturnsResult(string ingredientString, string requirements) 
        {
            var result = this._parser.ParseIngredientFromString(ingredientString);

            Assert.Equal(requirements, result.Requirements);
        }

        [Theory]
        [InlineData("Sel et poivre", "Sel et poivre", 0, MeasureUnit.Unit)]
        public void ParseIngredientFromString_NoQuantity_ReturnsResult(string ingredientString, string name, double quantity, MeasureUnit unit) 
        {
            var result = this._parser.ParseIngredientFromString(ingredientString);

            Assert.Equal(name, result.Name);
            Assert.Equal(quantity, result.Quantity.Value);
            Assert.Equal(unit, result.Quantity.Abbreviation);
        }

        [Theory]
        [InlineData("1,3 kg (3 lb) de rôti de palette de veau avec os", "rôti de palette de veau avec os", 1.3, MeasureUnit.Kilogram)]
        [InlineData("675 g (1 1/2 lb) de boeuf haché maigre", "boeuf haché maigre", 675, MeasureUnit.Gram)]
        [InlineData("60 ml (1/4 tasse) d’huile d’olive", "huile d’olive", 60, MeasureUnit.Millilitre)]
        [InlineData("250 ml	(1 tasse) d’eau", "eau", 250, MeasureUnit.Millilitre)]
        [InlineData("500 ml (2 tasses) de bouillon de boeuf", "bouillon de boeuf", 500, MeasureUnit.Millilitre)]
        [InlineData("60 ml (¼ tasse) de cassonade", "cassonade", 60, MeasureUnit.Millilitre)]
        [InlineData("3 oignons", "oignons", 3, MeasureUnit.Unit)]
        [InlineData("4 branches de céleri", "branches de céleri", 4, MeasureUnit.Unit)]
        [InlineData("4 gousses d'ail", "gousses d'ail", 4, MeasureUnit.Unit)]
        public void ParseIngredientFromString_ReturnsResult(string ingredientString, string name, double quantity, MeasureUnit unit) 
        {
            var result = this._parser.ParseIngredientFromString(ingredientString);

            Assert.Equal(name, result.Name);
            Assert.Equal(quantity, result.Quantity.Value);
            Assert.Equal(unit, result.Quantity.Abbreviation);
        }
    }
}