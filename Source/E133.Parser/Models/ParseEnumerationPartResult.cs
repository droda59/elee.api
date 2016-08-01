using System.Collections.Generic;

namespace E133.Parser
{
    public class ParseEnumerationPartResult
    {
        public bool IsEnumerationPart { get; set; }

        public IEnumerable<int> SkippedIndexes { get; set; }

        public IEnumerable<long> IngredientIds { get; set; }

        public static ParseEnumerationPartResult NegativeResult
        {
            get 
            {
                return new ParseEnumerationPartResult { IsEnumerationPart = false };
            }
        } 
    }
}