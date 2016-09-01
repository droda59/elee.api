namespace E133.Business.Models
{
    public class TimerPart : Part
    {
        public override string Type
        {
            get { return "timer"; }
            set {}
        }

        public string Value { get; set; }
        
        public string Text { get; set; }

        public string Action { get; set; }

        internal override string DebuggerDisplay
        {
            get { return this.Value; }
        }
    }
}