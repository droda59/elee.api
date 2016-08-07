using System.Globalization;

namespace E133.Parser.LanguageUtilities
{
    internal interface ILanguageHelper
    {
        bool IsDeterminant(string word);

        bool TryParseNumber(string word, out double number);

        bool TryParseNumber(string word, CultureInfo culture, out double number);
    }
}