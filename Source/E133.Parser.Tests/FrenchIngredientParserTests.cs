using System.Collections.Generic;

using Xunit;

using E133.Business;
using E133.Business.Models;
using E133.Parser.LanguageUtilities.French;

namespace E133.Parser.Tests
{
    public class FrenchIngredientParserTests
    {
        private RicardoParser _parser;
        private IEnumerable<Ingredient> _ingredients;

        public FrenchIngredientParserTests()
        {
            this._ingredients = new Ingredient[] 
            {
                new Ingredient { IngredientId = 1, SubrecipeId = 0, Name = "carottes" },
                new Ingredient { IngredientId = 2, SubrecipeId = 0, Name = "avocat" },
                new Ingredient { IngredientId = 3, SubrecipeId = 0, Name = "boeuf" },
                new Ingredient { IngredientId = 4, SubrecipeId = 0, Name = "nouille" }
            };

            this._parser = new RicardoParser(null, null, null, x => null, x => null, x => null, x => new FrenchLanguageHelper(), x => null);
            this._parser.InitializeCulture("fr");
        }

        [Theory]
        [InlineData("Ajouter rien du tout")]
        public void TryParseIngredientPart_DoesNotHaveIngredientPart_ReturnsFalse(string phrase) 
        {
            var result = this._parser.TryParseIngredientPart(phrase.SplitPhrase(), 0, this._ingredients, 0);

            Assert.False(result.IsIngredientPart);
        }

        [Theory]
        [InlineData("Ajouter le rien du tout")]
        [InlineData("Ajouter l'absence d'ingrédient")]
        [InlineData("Ajouter les riens du tout")]
        [InlineData("Ajouter la nullité")]
        public void TryParseIngredientPart_DoesNotHaveIngredientPart_WithDeterminant_ReturnsFalse(string phrase) 
        {
            var result = this._parser.TryParseIngredientPart(phrase.SplitPhrase(), 1, this._ingredients, 0);

            Assert.False(result.IsIngredientPart);
        }

        [Theory]
        [InlineData("Ajouter les carottes")]
        public void TryParseIngredientPart_HasIngredientPart_DifferentSubrecipe_ReturnsFalse(string phrase) 
        {
            var result = this._parser.TryParseIngredientPart(phrase.SplitPhrase(), 1, this._ingredients, -1);

            Assert.False(result.IsIngredientPart);
        }

        [Theory]
        [InlineData("Ajouter les carottes", 1)]
        [InlineData("Ajouter la carotte", 1)]
        [InlineData("Ajouter l'avocat", 2)]
        [InlineData("Ajouter l’avocat", 2)]
        [InlineData("Ajouter le boeuf", 3)]
        [InlineData("Ajouter la nouille", 4)]
        public void TryParseIngredientPart_HasIngredientPart_NameContains_ReturnsResult(string phrase, long ingredientId) 
        {
            var result = this._parser.TryParseIngredientPart(phrase.SplitPhrase(), 1, this._ingredients, 0);

            Assert.True(result.IsIngredientPart);
            Assert.Equal(ingredientId, result.IngredientId);
        }
    }
}