using System.Collections.Generic;

namespace E133.Parser
{
    public class ParseTimerPartResult
    {
        public bool IsTimerPart { get; set; }

        public IEnumerable<int> SkippedIndexes { get; set; }

        public string OutputFormat { get; set; }

        public string OutputValue { get; set; }
    }
}