using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Newtonsoft.Json;

namespace E133.Business.Models
{
    [DebuggerDisplay("{DebuggerDisplay}")]
    public class Step  
    {
        public Step()
        {
            this.Parts = new List<Part>();
        }

        [JsonProperty("id")]
        public int StepId { get; set; }

        public int SubrecipeId { get; set; }

        public IList<Part> Parts { get; set; }

        private string DebuggerDisplay
        {
            get
            {
                return string.Join(" ", this.Parts.Select(x => x.DebuggerDisplay));
            }
        }
    }
}