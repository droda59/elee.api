namespace E133.Parser.LanguageUtilities
{
    internal interface IActionDetector
    {
        bool IsAction(string part);

        string Actionify(string phrase);
    }
}