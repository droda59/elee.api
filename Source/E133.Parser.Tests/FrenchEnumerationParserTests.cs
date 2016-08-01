using System.Collections.Generic;
using System.Linq;

using Xunit;

using E133.Business.Models;
using E133.Parser.LanguageUtilities.French;

namespace E133.Parser.Tests
{
    public class FrenchEnumerationParserTests
    {
        private RicardoParser _parser;
        private IEnumerable<Ingredient> _ingredients;

        public FrenchEnumerationParserTests()
        {
            this._ingredients = new Ingredient[] 
                {
                    new Ingredient { Id = 1, SubrecipeId = 0, Name = "carottes" },
                    new Ingredient { Id = 2, SubrecipeId = 0, Name = "avocat" },
                    new Ingredient { Id = 3, SubrecipeId = 0, Name = "boeuf" },
                    new Ingredient { Id = 4, SubrecipeId = 0, Name = "nouille" }
                };

            this._parser = new RicardoParser(null, x => null, x => null, x => null, x => new FrenchLanguageHelper(), x => null);
            this._parser.InitializeCulture("fr");
        }

        [Theory]
        [InlineData("Ajouter rien du tout")]
        public void TryParseEnumerationPart_DoesNotHaveIngredientPart_ReturnsFalse(string phrase) 
        {
            var result = this._parser.TryParseEnumerationPart(phrase.SplitPhrase(), 0, this._ingredients, 0);

            Assert.False(result.IsEnumerationPart);
        }

        [Theory]
        [InlineData("Ajouter le rien du tout")]
        [InlineData("Ajouter l'absence d'ingrédient")]
        [InlineData("Ajouter les riens du tout")]
        [InlineData("Ajouter la nullité")]
        public void TryParseEnumerationPart_DoesNotHaveIngredientPart_WithDeterminant_ReturnsFalse(string phrase) 
        {
            var result = this._parser.TryParseEnumerationPart(phrase.SplitPhrase(), 1, this._ingredients, 0);

            Assert.False(result.IsEnumerationPart);
        }

        [Theory]
        [InlineData("Ajouter les carottes")]
        public void TryParseEnumerationPart_HasIngredientPart_DifferentSubrecipe_ReturnsFalse(string phrase) 
        {
            var result = this._parser.TryParseEnumerationPart(phrase.SplitPhrase(), 1, this._ingredients, -1);

            Assert.False(result.IsEnumerationPart);
        }

        [Theory]
        [InlineData("Ajouter les carottes")]
        [InlineData("Ajouter la carotte")]
        [InlineData("Ajouter l'avocat")]
        [InlineData("Ajouter l’avocat")]
        [InlineData("Ajouter le boeuf")]
        [InlineData("Ajouter la nouille")]
        public void TryParseEnumerationPart_HasIngredientPart_OnlyOneIngredient_ReturnsFalse(string phrase) 
        {
            var result = this._parser.TryParseEnumerationPart(phrase.SplitPhrase(), 1, this._ingredients, 0);

            Assert.False(result.IsEnumerationPart);
        }

        [Theory]
        [InlineData("Ajouter les carottes et l'avocat puis continuer", new [] { 1, 2 })]
        [InlineData("Ajouter les carottes et l’avocat puis continuer", new [] { 1, 2 })]
        [InlineData("Ajouter les carottes et le boeuf puis continuer", new [] { 1, 3 })]
        [InlineData("Ajouter les carottes et la nouille puis continuer", new [] { 1, 4 })]
        [InlineData("Ajouter l'avocat et le boeuf puis continuer", new [] { 2, 3 })]
        [InlineData("Ajouter l’avocat et le boeuf puis continuer", new [] { 2, 3 })]
        [InlineData("Ajouter l’avocat et la nouille puis continuer", new [] { 2, 4 })]
        [InlineData("Ajouter le boeuf et la nouille puis continuer", new [] { 3, 4 })]
        public void TryParseEnumerationPart_HasIngredientPart_TwoIngredients_WordSeparated_ReturnsResult(string phrase, int[] ingredientIds) 
        {
            var result = this._parser.TryParseEnumerationPart(phrase.SplitPhrase(), 1, this._ingredients, 0);

            Assert.True(result.IsEnumerationPart);
            Assert.Equal(5, result.SkippedIndexes.Count());
            Assert.Collection(result.IngredientIds, 
                item => Assert.Equal(ingredientIds[0], item),
                item => Assert.Equal(ingredientIds[1], item));
        }

        [Theory]
        [InlineData("Ajouter les carottes et l'avocat et continuer")]
        [InlineData("Ajouter les carottes et l'avocat et les faire cuire")]
        [InlineData("Ajouter les carottes et l'avocat, puis continuer")]
        [InlineData("Ajouter les carottes et l'avocat, et continuer")]
        public void TryParseEnumerationPart_HasIngredientPart_TwoIngredients_NextWordDivider_ReturnsResult(string phrase) 
        {
            var result = this._parser.TryParseEnumerationPart(phrase.SplitPhrase(), 1, this._ingredients, 0);

            Assert.True(result.IsEnumerationPart);
            Assert.Equal(5, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Ajouter les carottes, l'avocat puis continuer", new [] { 1, 2 })]
        [InlineData("Ajouter les carottes, l’avocat puis continuer", new [] { 1, 2 })]
        [InlineData("Ajouter les carottes, le boeuf puis continuer", new [] { 1, 3 })]
        [InlineData("Ajouter les carottes, la nouille puis continuer", new [] { 1, 4 })]
        [InlineData("Ajouter l'avocat, le boeuf puis continuer", new [] { 2, 3 })]
        [InlineData("Ajouter l’avocat, le boeuf puis continuer", new [] { 2, 3 })]
        [InlineData("Ajouter l’avocat, la nouille puis continuer", new [] { 2, 4 })]
        [InlineData("Ajouter le boeuf, la nouille puis continuer", new [] { 3, 4 })]
        public void TryParseEnumerationPart_HasIngredientPart_TwoIngredients_ComaSeparated_ReturnsResult(string phrase, int[] ingredientIds) 
        {
            var result = this._parser.TryParseEnumerationPart(phrase.SplitPhrase(), 1, this._ingredients, 0);

            Assert.True(result.IsEnumerationPart);
            Assert.Equal(5, result.SkippedIndexes.Count());
            Assert.Collection(result.IngredientIds, 
                item => Assert.Equal(ingredientIds[0], item),
                item => Assert.Equal(ingredientIds[1], item));
        }

        [Theory]
        [InlineData("Ajouter les carottes, l'avocat et le boeuf puis continuer", new [] { 1, 2, 3 })]
        [InlineData("Ajouter les carottes, l’avocat et le boeuf puis continuer", new [] { 1, 2, 3 })]
        [InlineData("Ajouter les carottes, le boeuf et la nouille puis continuer", new [] { 1, 3, 4 })]
        [InlineData("Ajouter l'avocat, le boeuf et la nouille puis continuer", new [] { 2, 3, 4 })]
        [InlineData("Ajouter l’avocat, le boeuf et la nouille puis continuer", new [] { 2, 3, 4 })]
        public void TryParseEnumerationPart_HasIngredientPart_ThreeIngredients_ComaWordSeparated_ReturnsResult(string phrase, int[] ingredientIds) 
        {
            var result = this._parser.TryParseEnumerationPart(phrase.SplitPhrase(), 1, this._ingredients, 0);

            Assert.True(result.IsEnumerationPart);
            Assert.Equal(8, result.SkippedIndexes.Count());
            Assert.Collection(result.IngredientIds, 
                item => Assert.Equal(ingredientIds[0], item),
                item => Assert.Equal(ingredientIds[1], item),
                item => Assert.Equal(ingredientIds[2], item));
        }

        [Theory]
        [InlineData("Ajouter tous les ingrédients et continuer")]
        public void TryParseEnumerationPart_HasIngredientPart_AllIngredients_ReturnsResult(string phrase) 
        {
            var result = this._parser.TryParseEnumerationPart(phrase.SplitPhrase(), 1, this._ingredients, 0);

            Assert.True(result.IsEnumerationPart);
            Assert.Equal(3, result.SkippedIndexes.Count());
            Assert.Equal(this._ingredients.Count(), result.IngredientIds.Count());
            Assert.All(this._ingredients, x => Assert.Contains(x.Id, result.IngredientIds));
        }
    }
}