using System.Collections.Generic;

namespace E133.Parser.LanguageUtilities.English
{
    internal class EnglishSubrecipeRepository : SubrecipeRepository, ISubrecipeRepository
    {
        public readonly IDictionary<int, string> _knownSubrecipes;

        public EnglishSubrecipeRepository()
        {
            this._knownSubrecipes = new Dictionary<int, string>
            {
                { RequirementsSubrecipeId, "Requirements"},
                { PreparationSubrecipeId, "Preparation"}
            };
        }

        public override IDictionary<int, string> KnownSubrecipes
        {
            get { return this._knownSubrecipes; } 
        }
    }
}