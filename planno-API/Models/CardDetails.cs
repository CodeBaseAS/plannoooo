namespace planno_API.Models
{
    public class CardDetails
    {
        public int Id { get; set; }
        public string CardBrand { get; set; } = string.Empty;
        public string CardLast4 { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
