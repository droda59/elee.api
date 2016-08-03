using System;
using System.Collections.Generic;

namespace E133.Parser.LanguageUtilities.French
{
    internal class FrenchLanguageHelper : ILanguageHelper
    {
        private static readonly IDictionary<string, int> NumberWords = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) 
        {
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

        public bool IsNumber(string word, out int number)
        {
            if (int.TryParse(word, out number))
            {
                return true;
            }
            else if (FrenchLanguageHelper.NumberWords.TryGetValue(word, out number))
            {
                return true;
            }

            return false;
        }
    }
}