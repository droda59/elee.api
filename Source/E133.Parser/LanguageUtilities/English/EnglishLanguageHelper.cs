using System;
using System.Collections.Generic;
using System.Globalization;

namespace E133.Parser.LanguageUtilities.English
{
    internal class EnglishLanguageHelper : ILanguageHelper
    {
        private static readonly IDictionary<string, double> NumberWords = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) 
        {
            { "½", 0.5 },
            { "¼", 0.25 },
            { "¾", 0.75 },
            { "one", 1 },
            { "two", 2 },
            { "three", 3 },
            { "four", 4 },
            { "five", 5 },
            { "six", 6 },
            { "seven", 7 },
            { "eight", 8 },
            { "nine", 9 },
            { "ten", 10 },
            { "twelve", 12 },
            { "fifteen", 15 },
            { "thirty", 30 },
        };
        public bool IsDeterminant(string word)
        {
            throw new NotImplementedException();
        }

        public bool TryParseNumber(string word, out double number)
        {
            return this.TryParseNumber(word, CultureInfo.InvariantCulture, out number);
        }

        public bool TryParseNumber(string word, CultureInfo culture, out double number)
        {
            if (double.TryParse(word, NumberStyles.Any, culture, out number))
            {
                return true;
            }
            else if (NumberWords.TryGetValue(word, out number))
            {
                return true;
            }

            return false;
        }
    }
}
