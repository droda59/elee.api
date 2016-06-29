using System.Collections.Generic;

namespace E133.Parser.LanguageUtilities
{
    internal interface ISubrecipeRepository
    {
        IDictionary<int, string> KnownSubrecipes { get; }
    }
}