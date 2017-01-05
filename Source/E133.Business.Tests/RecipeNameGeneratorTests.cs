using System.Collections.Generic;
using System.Linq;

using Xunit;

using E133.Business;
using E133.Business.Models;

namespace E133.Business
{
    public class RecipeNameGeneratorTests
    {
        private const string OriginalUrl = "http://www.ricardocuisine.com/recette";
        
        private readonly RecipeNameGenerator _nameGenerator;
        private readonly QuickRecipe _recipe;

        public RecipeNameGeneratorTests()
        {
            this._nameGenerator = new RecipeNameGenerator();
            
            this._recipe = new QuickRecipe();
            this._recipe.OriginalUrl = OriginalUrl;
        }

        [Fact]
        public void GenerateName_StartsWithDomainName()
        {
            this._recipe.Title = "test";

            var result = this._nameGenerator.GenerateName(this._recipe);

            Assert.StartsWith("ricardocuisine", result);
        }

        [Fact]
        public void GenerateName_EndsWithTitle()
        {
            this._recipe.Title = "test";

            var result = this._nameGenerator.GenerateName(this._recipe);

            Assert.EndsWith("test", result);
        }

        [Fact]
        public void GenerateName_LowercaseTitle()
        {
            this._recipe.Title = "TEsT";

            var result = this._nameGenerator.GenerateName(this._recipe);

            Assert.EndsWith("test", result);
        }

        [Fact]
        public void GenerateName_ReplaceSpacesWithDash()
        {
            this._recipe.Title = "muffins aux bleuets";

            var result = this._nameGenerator.GenerateName(this._recipe);

            Assert.Equal(3, result.Count(x => x == '-'));
        }

        [Fact]
        public void GenerateName_SeparateTitleWords()
        {
            this._recipe.Title = "muffins aux bleuets";

            var result = this._nameGenerator.GenerateName(this._recipe);

            Assert.EndsWith("muffins-aux-bleuets", result);
        }

        [Theory]
        [InlineData("èmuffins aux bleuets")]
        [InlineData("êmuffins aux bleuets")]
        [InlineData("émuffins aux bleuets")]
        [InlineData("àmuffins aux bleuets")]
        [InlineData("âmuffins aux bleuets")]
        [InlineData("ùmuffins aux bleuets")]
        [InlineData("ëmuffins aux bleuets")]
        [InlineData("’muffins aux bleuets")]
        [InlineData("(muffins aux bleuets")]
        [InlineData(")muffins aux bleuets")]
        public void GenerateName_RemovedDiacritics(string title) 
        {
            this._recipe.Title = title;

            var result = this._nameGenerator.GenerateName(this._recipe);

            Assert.EndsWith("muffins-aux-bleuets", result);
        }

        [Theory]
        [InlineData("muffins au bleuets")]
        [InlineData("muffins o bleuets")]
        public void GenerateName_RemoveSmallWords(string title) 
        {
            this._recipe.Title = title;

            var result = this._nameGenerator.GenerateName(this._recipe);

            Assert.EndsWith("muffins-bleuets", result);
        }
    }
}
