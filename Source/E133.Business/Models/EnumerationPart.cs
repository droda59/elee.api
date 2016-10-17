using System.Collections.Generic;
using System.Linq;

namespace E133.Business.Models
{
    public class EnumerationPart : Part
    {
        public EnumerationPart()
        {
            this.Parts = new List<Part>();
        }

        public override string Type
        {
            get { return "enumeration"; }
            set {}
        }

        public IList<Part> Parts { get; set; }

        internal override string DebuggerDisplay
        {
            get
            {
                return string.Join(" ", this.Parts.Select(x => x.DebuggerDisplay));
            }
        }
    }
}
