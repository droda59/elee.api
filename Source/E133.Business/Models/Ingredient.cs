namespace E133.Business.Models
{
    public class Ingredient 
    {
        public long Id { get; set; }
		
        public int SubrecipeId { get; set; }
		
        public string Name { get; set; }
		
        public Quantity Quantity { get; set; }
		
        public Ingredient Replacement { get; set; }
		
        public string Requirements { get; set; }
    }
}