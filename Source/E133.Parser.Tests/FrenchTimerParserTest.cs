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
            this._parser = new RicardoParser(null, x => null, x => new FrenchTimerDetector(), x => null, x => null, x => null);
            this._parser.InitializeCulture("fr");
        }

        [Fact]
        public void TryParseTimerPart_DoesNotHaveTimerPart_ReturnsFalse() 
        {
            var phrase = "Brasser pendant longtemps";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 0);

            Assert.False(result.IsTimerPart);
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordNotTimerPart_HasSimilarPart_ReturnsFalse() 
        {
            var phrase = "Brasser 2 fois";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 1);

            Assert.False(result.IsTimerPart);
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordNotTimerPart_HasSimilarRangePart_ReturnsFalse() 
        {
            var phrase = "Brasser 2 à 3 fois";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 1);

            Assert.False(result.IsTimerPart);
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordNotTimerPart_ReturnsFalse()
        {
            var phrase = "Brasser pendant 3 minutes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 0);

            Assert.False(result.IsTimerPart);
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_Hour_ReturnsTrue()
        {
            var phrase = "Brasser pendant 1 heure";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT1H", result.OutputValue);
            Assert.Equal(2, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_Hours_ReturnsTrue()
        {
            var phrase = "Brasser pendant 3 heures";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT3H", result.OutputValue);
            Assert.Equal(2, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_H_ReturnsTrue()
        {
            var phrase = "Brasser pendant 3 h";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT3H", result.OutputValue);
            Assert.Equal(2, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_Minute_ReturnsTrue()
        {
            var phrase = "Brasser pendant 1 minute";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT1M", result.OutputValue);
            Assert.Equal(2, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_Minutes_ReturnsTrue()
        {
            var phrase = "Brasser pendant 3 minutes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT3M", result.OutputValue);
            Assert.Equal(2, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_M_ReturnsTrue()
        {
            var phrase = "Brasser pendant 3 m";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT3M", result.OutputValue);
            Assert.Equal(2, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_Seconde_ReturnsTrue()
        {
            var phrase = "Brasser pendant 1 seconde";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT1S", result.OutputValue);
            Assert.Equal(2, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_Secondes_ReturnsTrue()
        {
            var phrase = "Brasser pendant 3 secondes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT3S", result.OutputValue);
            Assert.Equal(2, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_S_ReturnsTrue()
        {
            var phrase = "Brasser pendant 3 s";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT3S", result.OutputValue);
            Assert.Equal(2, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_RangePart_Hours_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 à 3 heures";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_RangePart_H_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 à 3 h";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_RangePart_Minutes_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 à 3 minutes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2M", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_RangePart_M_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 à 3 m";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2M", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_RangePart_Seconds_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 à 3 secondes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2S", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_RangePart_S_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 à 3 s";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2S", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_HourMinute_ReturnsTrue()
        {
            var phrase = "Brasser pendant 1 heure 1 minute";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT1H1M", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_HoursMinute_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 heures 1 minute";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H1M", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_HourMinutes_ReturnsTrue()
        {
            var phrase = "Brasser pendant 1 heure 2 minutes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT1H2M", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_HoursMinutes_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 heures 2 minutes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H2M", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_HoursM_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 heures 2 m";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H2M", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_HMinutes_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 h 2 minutes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H2M", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_HM_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 h 2 m";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H2M", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_HoursUnspecifiedMinutes_ReturnsHoursWithMinutes()
        {
            var phrase = "Brasser pendant 2 heures 2";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H2M", result.OutputValue);
            Assert.Equal(3, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_HourSecond_ReturnsTrue()
        {
            var phrase = "Brasser pendant 1 heure 1 seconde";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT1H1S", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_HoursSeconds_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 heures 2 secondes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H2S", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_HoursS_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 heures 2 s";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H2S", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_HSeconds_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 h 2 secondes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H2S", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_HS_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 h 2 s";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H2S", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_MinuteSecond_ReturnsTrue()
        {
            var phrase = "Brasser pendant 1 minutes 1 seconde";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT1M1S", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_MinutesSecond_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 minutes 1 seconde";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2M1S", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_MinuteSecondes_ReturnsTrue()
        {
            var phrase = "Brasser pendant 1 minute 2 secondes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT1M2S", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_MinutesSecondes_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 minutes 2 secondes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2M2S", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_MinutesS_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 minutes 2 s";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2M2S", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_MSeconds_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 m 2 secondes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2M2S", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_MS_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 m 2 s";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2M2S", result.OutputValue);
            Assert.Equal(4, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_MinutesUnspecifiedSeconds_ReturnsMinutesWithSeconds()
        {
            var phrase = "Brasser pendant 2 minutes 2";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2M2S", result.OutputValue);
            Assert.Equal(3, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_Range_HourUnspecifiedMinutes_ReturnsTrue()
        {
            var phrase = "Brasser pendant 1 heure 30 à 3 heures";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT1H30M", result.OutputValue);
            Assert.Equal(6, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_Range_HoursUnspecifiedMinutes_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 heures 30 à 3 heures";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H30M", result.OutputValue);
            Assert.Equal(6, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_Range_HoursMinute_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 heures 1 minute à 3 heures";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H1M", result.OutputValue);
            Assert.Equal(7, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_Range_HoursMinutes_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 heures 30 minutes à 3 heures";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2H30M", result.OutputValue);
            Assert.Equal(7, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_Range_MinuteUnspecifiedSeconds_ReturnsTrue()
        {
            var phrase = "Brasser pendant 1 minute 30 à 3 minutes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT1M30S", result.OutputValue);
            Assert.Equal(6, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_Range_MinutesUnspecifiedSeconds_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 minutes 30 à 3 minutes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2M30S", result.OutputValue);
            Assert.Equal(6, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_Range_MinutesSecond_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 minutes 1 seconde à 3 minutes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2M1S", result.OutputValue);
            Assert.Equal(7, result.SkippedIndexes.Count());
        }

        [Fact]
        public void TryParseTimerPart_CurrentWordTimerPart_MultiPart_Range_MinutesSeconds_ReturnsTrue()
        {
            var phrase = "Brasser pendant 2 minutes 30 secondes à 3 minutes";

            var result = this._parser.TryParseTimerPart(phrase.Split(' '), 2);

            Assert.True(result.IsTimerPart);
            Assert.Equal("PT2M30S", result.OutputValue);
            Assert.Equal(7, result.SkippedIndexes.Count());
        }
    }
}
