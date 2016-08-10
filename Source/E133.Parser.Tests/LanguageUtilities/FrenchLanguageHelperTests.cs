using E133.Parser.LanguageUtilities;
using E133.Parser.LanguageUtilities.French;

using Xunit;

namespace E133.Parser.Tests.LanguageUtilities
{
    public class FrenchLanguageHelperTests
    {
        private ILanguageHelper _helper;

        public FrenchLanguageHelperTests()
        {
            this._helper = new FrenchLanguageHelper();
        }

        [Theory]
        [InlineData("any")]
        public void TryParseNumber_IsNotANumber_ReturnsResult(string word)
        {
            double number;
            var result = this._helper.TryParseNumber(word, out number);

            Assert.False(result);
        }

        [Theory]
        [InlineData("½", 0.5)]
        [InlineData("¼", 0.25)]
        [InlineData("¾", 0.75)]
        [InlineData("un", 1)]
        [InlineData("une", 1)]
        [InlineData("deux", 2)]
        [InlineData("trois", 3)]
        [InlineData("quatre", 4)]
        [InlineData("cinq", 5)]
        [InlineData("six", 6)]
        [InlineData("sept", 7)]
        [InlineData("huit", 8)]
        [InlineData("neuf", 9)]
        [InlineData("dix", 10)]
        [InlineData("douze", 12)]
        [InlineData("quinze", 15)]
        [InlineData("trente", 30)]
        public void TryParseNumber_IsANumber_ReturnsResult(string word, double number)
        {
            var result = this._helper.TryParseNumber(word, out number);

            Assert.True(result);
        }

        [Theory]
        [InlineData("Un", 1)]
        [InlineData("Une", 1)]
        [InlineData("Deux", 2)]
        [InlineData("Trois", 3)]
        [InlineData("Quatre", 4)]
        [InlineData("Cinq", 5)]
        [InlineData("Six", 6)]
        [InlineData("Sept", 7)]
        [InlineData("Huit", 8)]
        [InlineData("Neuf", 9)]
        [InlineData("Dix", 10)]
        [InlineData("Douze", 12)]
        [InlineData("Quinze", 15)]
        [InlineData("Trente", 30)]
        public void TryParseNumber_IsANumber_Caps_ReturnsResult(string word, double number)
        {
            var result = this._helper.TryParseNumber(word, out number);

            Assert.True(result);
        }
    }
}