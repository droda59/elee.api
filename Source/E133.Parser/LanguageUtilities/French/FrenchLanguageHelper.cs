namespace E133.Parser.LanguageUtilities.French
{
    internal class FrenchLanguageHelper : ILanguageHelper
    {
        public bool IsDeterminant(string word)
        {
            return word == "le" || word == "la" || word == "les" || word == "l'" || word == "l’";
        }
    }
}