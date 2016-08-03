using System;
using System.Collections.Generic;

namespace E133.Parser.LanguageUtilities.English
{
    internal class EnglishLanguageHelper : ILanguageHelper
    {
        private static readonly IDictionary<string, int> NumberWords = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) 
        {
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

        public bool IsNumber(string word, out int number)
        {
            if (int.TryParse(word, out number))
            {
                return true;
            }
            else if (EnglishLanguageHelper.NumberWords.TryGetValue(word, out number))
            {
                return true;
            }

            return false;
        }
    }
}
