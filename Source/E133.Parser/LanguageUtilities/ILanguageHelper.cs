namespace E133.Parser.LanguageUtilities
{
    internal interface ILanguageHelper
    {
        bool IsDeterminant(string word);

        bool IsNumber(string word, out int number);
    }
}