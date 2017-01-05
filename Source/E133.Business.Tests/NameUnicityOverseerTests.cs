using System;
using System.Threading.Tasks;

using Moq;

using Xunit;

using E133.Business.Models;

namespace E133.Business
{
    public class NameUnicityOverseerTests
    {
        private readonly NameUnicityOverseer _overseer;
        private readonly Mock<IQuickRecipeRepository> _repoMock;

        public NameUnicityOverseerTests()
        {
            this._repoMock = new Mock<IQuickRecipeRepository>();
            this._overseer = new NameUnicityOverseer(this._repoMock.Object);
        }

        [Fact]
        public async Task GenerateUniqueName_NameDoesNotExist_ReturnName()
        {
            var name = "muffins-aux-bleuets";

            this._repoMock.Setup(x => x.GetByUniqueNameAsync(name)).ReturnsAsync(null);

            var result = await this._overseer.GenerateUniqueName(name);

            Assert.Equal(name, result);
        }

        [Fact]
        public async Task GenerateUniqueName_NameDoesNotExist_RepoCalledOnce()
        {
            var name = "muffins-aux-bleuets";

            this._repoMock.Setup(x => x.GetByUniqueNameAsync(name)).ReturnsAsync(null);

            var result = await this._overseer.GenerateUniqueName(name);

            this._repoMock.Verify(x => x.GetByUniqueNameAsync(name), Times.Once());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(50)]
        public async Task GenerateUniqueName_NameDoesExist_IncrementName(int numberOfRetries)
        {
            var name = "muffins-aux-bleuets";
            var existingRecipe = new QuickRecipe();

            this._repoMock.Setup(x => x.GetByUniqueNameAsync(name)).ReturnsAsync(existingRecipe);
            if (numberOfRetries > 1) 
            {
                for (var i = 1; i < numberOfRetries; i++)
                {
                    var newName = $"{name}-{i}";
                    this._repoMock.Setup(x => x.GetByUniqueNameAsync(newName)).ReturnsAsync(existingRecipe);
                }
            }

            var result = await this._overseer.GenerateUniqueName(name);

            Assert.Equal($"{name}-{numberOfRetries}", result);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(50)]
        public async Task GenerateUniqueName_NameDoesExist_RepoCalled(int numberOfRetries)
        {
            var name = "muffins-aux-bleuets";
            var existingRecipe = new QuickRecipe();

            this._repoMock.Setup(x => x.GetByUniqueNameAsync(name)).ReturnsAsync(existingRecipe);
            if (numberOfRetries > 1) 
            {
                for (var i = 1; i < numberOfRetries; i++)
                {
                    var newName = $"{name}-{i}";
                    this._repoMock.Setup(x => x.GetByUniqueNameAsync(newName)).ReturnsAsync(existingRecipe);
                }
            }

            var result = await this._overseer.GenerateUniqueName(name);

            this._repoMock.Verify(x => x.GetByUniqueNameAsync(It.IsAny<string>()), Times.Exactly(numberOfRetries + 1));
        }
    }
}
