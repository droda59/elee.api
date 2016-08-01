using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace E133.Parser.Tests
{
    public static class StringExtensions
    {
        public static IList<string> SplitPhrase(this string phrase)
        {
            var parts = new Regex(@"[\w()°]+['’]*|[,]|[\)]\b", RegexOptions.Compiled).Matches(phrase);

            return parts.Cast<Match>().Select(m => m.Value).ToArray();
        }
    }
}