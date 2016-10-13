using System.Collections.Generic;

namespace E133.Parser.LanguageUtilities
{
    public interface IVerbProvider
    {
        HashSet<string> AcceptedVerbs { get; }
    }
}