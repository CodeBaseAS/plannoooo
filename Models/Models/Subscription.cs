namespace Models.Models
{
    public class Subscription
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Interval { get; set; }
    }
}
