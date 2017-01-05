namespace E133.Business.Models
{
    public class ContactForm : Document
    {
        public string Name { get; set; }
        
        public string Email { get; set; }
        
        public string Message { get; set; }
    }
}
