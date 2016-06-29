namespace E133.Business.Models
{
    public class TextPart : Part
    {
        public override string Type
        {
            get { return "text"; }
        }

        public string Value { get; set; }

        internal override string DebuggerDisplay
        {
            get { return this.Value; }
        }
    }
}