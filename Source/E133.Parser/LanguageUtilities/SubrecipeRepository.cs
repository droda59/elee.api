using System.Collections.Generic;

namespace E133.Parser.LanguageUtilities
{
    internal abstract class SubrecipeRepository : ISubrecipeRepository
    {
        public const int RequirementsSubrecipeId = -2;
        public const int PreparationSubrecipeId = -1;

        public abstract IDictionary<int, string> KnownSubrecipes { get; }
    }
}
