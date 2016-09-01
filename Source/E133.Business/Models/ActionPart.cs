namespace E133.Business.Models
{
    public class ActionPart : Part
    {
        public override string Type
        {
            get { return "action"; }
            set {}
        }

        internal override string DebuggerDisplay
        {
            get { return this.Value; }
        }

        public string Value { get; set; }
    }
}