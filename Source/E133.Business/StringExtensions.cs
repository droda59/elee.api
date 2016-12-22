using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace E133.Business
{
    public static class StringExtensions
    {
        public static string RemoveDiacritics(this string text) 
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
        
        public static IList<string> SplitPhrase(this string phrase)
        {
            var parts = new Regex(@"[\w()°]+['’]*|[,]|[\)]\b", RegexOptions.Compiled).Matches(phrase);

            return parts.Cast<Match>().Select(m => m.Value).ToArray();
        }
    }
}
