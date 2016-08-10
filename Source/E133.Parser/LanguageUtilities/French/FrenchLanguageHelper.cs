using System;
using System.Collections.Generic;
using System.Globalization;

namespace E133.Parser.LanguageUtilities.French
{
    internal class FrenchLanguageHelper : ILanguageHelper
    {
        private static readonly IDictionary<string, double> NumberWords = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) 
        {
            { "½", 0.5 },
            { "¼", 0.25 },
            { "¾", 0.75 },
            { "un", 1 },
            { "une", 1 },
            { "deux", 2 },
            { "trois", 3 },
            { "quatre", 4 },
            { "cinq", 5 },
            { "six", 6 },
            { "sept", 7 },
            { "huit", 8 },
            { "neuf", 9 },
            { "dix", 10 },
            { "douze", 12 },
            { "quinze", 15 },
            { "trente", 30 },
        };

        public bool IsDeterminant(string word)
        {
            return word == "le" || word == "la" || word == "les" || word == "l'" || word == "l’";
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