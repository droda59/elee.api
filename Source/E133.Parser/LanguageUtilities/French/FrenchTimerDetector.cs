using System;

namespace E133.Parser.LanguageUtilities.French
{
    internal class FrenchTimerDetector : ITimerDetector
    {
        public bool IsTimeQualifier(string part)
        {
            if (part.StartsWith("seconde", StringComparison.OrdinalIgnoreCase) || part.Equals("s", StringComparison.OrdinalIgnoreCase)
                || part.StartsWith("minute", StringComparison.OrdinalIgnoreCase) || part.Equals("m", StringComparison.OrdinalIgnoreCase) 
                || part.StartsWith("heure", StringComparison.OrdinalIgnoreCase) || part.Equals("h", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public string GetTimeQualifier(string word)
        {
            if (word.StartsWith("seconde", StringComparison.OrdinalIgnoreCase) || word.Equals("s", StringComparison.OrdinalIgnoreCase))
            {
                return "S";
            }
            else if (word.StartsWith("minute", StringComparison.OrdinalIgnoreCase) || word.Equals("m", StringComparison.OrdinalIgnoreCase))
            {
                return "M";
            }
            else if (word.StartsWith("heure", StringComparison.OrdinalIgnoreCase) || word.Equals("h", StringComparison.OrdinalIgnoreCase))
            {
                return "H";
            }

            return string.Empty;
        }
    }
}
