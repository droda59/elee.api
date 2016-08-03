using E133.Parser.LanguageUtilities;
using E133.Parser.LanguageUtilities.English;

using Xunit;

namespace E133.Parser.Tests.LanguageUtilities
{
    public class EnglishLanguageHelperTests
    {
        private ILanguageHelper _helper;

        public EnglishLanguageHelperTests()
        {
            this._helper = new EnglishLanguageHelper();
        }

        [Theory]
        [InlineData("any")]
        public void IsNumber_IsNotANumber_ReturnsResult(string word)
        {
            int number;
            var result = this._helper.IsNumber(word, out number);

            Assert.False(result);
        }

        [Theory]
        [InlineData("one", 1)]
        [InlineData("two", 2)]
        [InlineData("three", 3)]
        [InlineData("four", 4)]
        [InlineData("five", 5)]
        [InlineData("six", 6)]
        [InlineData("seven", 7)]
        [InlineData("eight", 8)]
        [InlineData("nine", 9)]
        [InlineData("ten", 10)]
        [InlineData("twelve", 12)]
        [InlineData("fifteen", 15)]
        [InlineData("thirty", 30)]
        public void IsNumber_IsANumber_ReturnsResult(string word, int number)
        {
            var result = this._helper.IsNumber(word, out number);

            Assert.True(result);
        }

        [Theory]
        [InlineData("One", 1)]
        [InlineData("Two", 2)]
        [InlineData("Three", 3)]
        [InlineData("Four", 4)]
        [InlineData("Five", 5)]
        [InlineData("Six", 6)]
        [InlineData("Seven", 7)]
        [InlineData("Eight", 8)]
        [InlineData("Nine", 9)]
        [InlineData("Ten", 10)]
        [InlineData("Twelve", 12)]
        [InlineData("Fifteen", 15)]
        [InlineData("Thirty", 30)]
        public void IsNumber_IsANumber_Caps_ReturnsResult(string word, int number)
        {
            var result = this._helper.IsNumber(word, out number);

            Assert.True(result);
        }
    }
}