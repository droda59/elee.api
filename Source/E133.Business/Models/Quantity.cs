namespace E133.Business.Models
{
    public class Quantity 
    {
        public double Value { get; set; }
		
        public MeasureUnit Abbreviation { get; set; }

        public string Format { get; set; }

        public string FormatAbbreviation { get; set; }
    }
}