using System.Collections.Generic;

namespace E133.Parser
{
    public class ParseIngredientPartResult
    {
        public bool IsIngredientPart { get; set; }

        public IEnumerable<int> SkippedIndexes { get; set; }

        public long IngredientId { get; set; }

        public static ParseIngredientPartResult NegativeResult
        {
            get 
            {
                return new ParseIngredientPartResult { IsIngredientPart = false };
            }
        } 
    }
}