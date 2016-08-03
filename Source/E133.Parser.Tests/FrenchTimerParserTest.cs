using System.Linq;

using Xunit;

using E133.Parser.LanguageUtilities.French;

namespace E133.Parser.Tests
{
    public class FrenchTimerParserTest
    {
        private RicardoParser _parser;

        public FrenchTimerParserTest()
        {
            this._parser = new RicardoParser(null, x => null, x => new FrenchTimerDetector(), x => null, x => new FrenchLanguageHelper(), x => null);
            this._parser.InitializeCulture("fr");
        }

        [Theory]
        [InlineData("Brasser pendant longtemps")]
        public void TryParseTimerPart_DoesNotHaveTimerPart_ReturnsFalse(string phrase) 
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 0);

            Assert.False(result.IsTimerPart);
        }

        [Theory]
        [InlineData("Brasser 2 fois")]
        public void TryParseTimerPart_CurrentWordNotTimerPart_HasSimilarPart_ReturnsFalse(string phrase) 
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 1);

            Assert.False(result.IsTimerPart);
        }

        [Theory]
        [InlineData("Brasser 2 à 3 fois")]
        public void TryParseTimerPart_CurrentWordNotTimerPart_HasSimilarRangePart_ReturnsFalse(string phrase) 
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 1);

            Assert.False(result.IsTimerPart);
        }

        [Theory]
        [InlineData("Brasser pendant 3 minutes")]
        public void TryParseTimerPart_CurrentWordNotTimerPart_ReturnsFalse(string phrase)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 0);

            Assert.False(result.IsTimerPart);
        }

        [Theory]
        [InlineData("Brasser pendant une heure", "PT1H")]
        [InlineData("Brasser pendant 1 heure", "PT1H")]
        [InlineData("Brasser pendant 3 heures", "PT3H")]
        [InlineData("Brasser pendant 3 h", "PT3H")]
        public void TryParseTimerPart_Hours_ReturnsResult(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(2, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Brasser pendant une minute", "PT1M")]
        [InlineData("Brasser pendant 1 minute", "PT1M")]
        [InlineData("Brasser pendant 3 minutes", "PT3M")]
        [InlineData("Brasser pendant 3 m", "PT3M")]
        public void TryParseTimerPart_Minutes_ReturnsResult(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(2, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Brasser pendant une seconde", "PT1S")]
        [InlineData("Brasser pendant 1 seconde", "PT1S")]
        [InlineData("Brasser pendant 3 secondes", "PT3S")]
        [InlineData("Brasser pendant 3 s", "PT3S")]
        public void TryParseTimerPart_Seconds_ReturnsResult(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(2, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Brasser pendant deux à trois heures", "PT2H")]
        [InlineData("Brasser pendant 2 à 3 heures", "PT2H")]
        [InlineData("Brasser pendant 2 à 3 h", "PT2H")]
        public void TryParseTimerPart_RangePart_Hours_ReturnsResult(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Brasser pendant deux à trois minutes", "PT2M")]
        [InlineData("Brasser pendant 2 à 3 minutes", "PT2M")]
        [InlineData("Brasser pendant 2 à 3 m", "PT2M")]
        public void TryParseTimerPart_RangePart_Minutes_ReturnsResult(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Brasser pendant deux à trois secondes", "PT2S")]
        [InlineData("Brasser pendant 2 à 3 secondes", "PT2S")]
        [InlineData("Brasser pendant 2 à 3 s", "PT2S")]
        public void TryParseTimerPart_RangePart_Seconds_ReturnsResult(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Brasser pendant une heure une minute", "PT1H1M")]
        [InlineData("Brasser pendant 1 heure 1 minute", "PT1H1M")]
        [InlineData("Brasser pendant 2 heures 1 minute", "PT2H1M")]
        [InlineData("Brasser pendant 1 heure 2 minutes", "PT1H2M")]
        [InlineData("Brasser pendant 2 heures 2 minutes", "PT2H2M")]
        [InlineData("Brasser pendant 2 heures 2 m", "PT2H2M")]
        [InlineData("Brasser pendant 2 h 2 minutes", "PT2H2M")]
        [InlineData("Brasser pendant 2 h 2 m", "PT2H2M")]
        public void TryParseTimerPart_MultiPart_HoursMinutes_ReturnsResult(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Brasser pendant deux heures trente", "PT2H30M")]
        [InlineData("Brasser pendant 2 heures 30", "PT2H30M")]
        public void TryParseTimerPart_MultiPart_HoursUnspecifiedMinutes_ReturnsHoursWithMinutes(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(3, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Brasser pendant une heure une seconde", "PT1H1S")]
        [InlineData("Brasser pendant 1 heure 1 seconde", "PT1H1S")]
        [InlineData("Brasser pendant 1 heure 2 secondes", "PT1H2S")]
        [InlineData("Brasser pendant 2 heures 1 seconde", "PT2H1S")]
        [InlineData("Brasser pendant 2 heures 2 secondes", "PT2H2S")]
        [InlineData("Brasser pendant 2 heures 2 s", "PT2H2S")]
        [InlineData("Brasser pendant 2 h 2 secondes", "PT2H2S")]
        [InlineData("Brasser pendant 2 h 2 s", "PT2H2S")]
        public void TryParseTimerPart_MultiPart_HourSecond_ReturnsResult(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Brasser pendant une minute une seconde", "PT1M1S")]
        [InlineData("Brasser pendant 1 minute 1 seconde", "PT1M1S")]
        [InlineData("Brasser pendant 1 minute 2 secondes", "PT1M2S")]
        [InlineData("Brasser pendant 2 minutes 1 seconde", "PT2M1S")]
        [InlineData("Brasser pendant 2 minutes 2 secondes", "PT2M2S")]
        [InlineData("Brasser pendant 2 minutes 2 s", "PT2M2S")]
        [InlineData("Brasser pendant 2 m 2 secondes", "PT2M2S")]
        [InlineData("Brasser pendant 2 m 2 s", "PT2M2S")]
        public void TryParseTimerPart_MultiPart_MinuteSecond_ReturnsResult(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Brasser pendant deux minutes trente", "PT2M30S")]
        [InlineData("Brasser pendant 2 minutes 30", "PT2M30S")]
        public void TryParseTimerPart_MultiPart_MinutesUnspecifiedSeconds_ReturnsMinutesWithSeconds(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(3, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Brasser pendant une heure trente à trois heures", "PT1H30M")]
        [InlineData("Brasser pendant 1 heure 30 à 3 heures", "PT1H30M")]
        [InlineData("Brasser pendant 2 heures 30 à 3 heures", "PT2H30M")]
        public void TryParseTimerPart_MultiPart_Range_HourUnspecifiedMinutes_ReturnsResult(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(6, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Brasser pendant deux heures une minute à trois heures", "PT2H1M")]
        [InlineData("Brasser pendant 2 heures 1 minute à 3 heures", "PT2H1M")]
        [InlineData("Brasser pendant 2 heures 30 minutes à 3 heures", "PT2H30M")]
        public void TryParseTimerPart_MultiPart_Range_HoursMinute_ReturnsResult(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(7, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Brasser pendant une minute trente à trois minutes", "PT1M30S")]
        [InlineData("Brasser pendant 1 minute 30 à 3 minutes", "PT1M30S")]
        [InlineData("Brasser pendant 2 minutes 30 à 3 minutes", "PT2M30S")]
        public void TryParseTimerPart_MultiPart_Range_MinuteUnspecifiedSeconds_ReturnsResult(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(6, result.SkippedIndexes.Count());
        }

        [Theory]
        [InlineData("Brasser pendant deux minutes une seconde à trois minutes", "PT2M1S")]
        [InlineData("Brasser pendant 2 minutes 1 seconde à 3 minutes", "PT2M1S")]
        [InlineData("Brasser pendant 2 minutes 30 secondes à 3 minutes", "PT2M30S")]
        public void TryParseTimerPart_MultiPart_Range_MinutesSecond_ReturnsResult(string phrase, string output)
        {
            var result = this._parser.TryParseTimerPart(phrase.SplitPhrase(), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal(output, result.OutputValue);
            Assert.Equal(7, result.SkippedIndexes.Count());
        }
    }
}
