namespace E133.Parser.LanguageUtilities
{
    internal interface ITimerDetector
    {
        bool IsTimeQualifier(string part);

        string GetTimeQualifier(string word);
    }
}
