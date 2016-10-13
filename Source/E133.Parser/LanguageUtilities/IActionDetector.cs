namespace E133.Parser.LanguageUtilities
{
    public interface IActionDetector
    {
        bool IsAction(string part);

        string Actionify(string phrase);
    }
}