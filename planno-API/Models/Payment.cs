namespace planno_API.Models
{
    public class Payment
    {
        public int Id { get; set; }
        public int CardId { get; set; }
        public CardDetails Card {  get; set; } = null!;
        public int SubscriptionId { get; set; }
        public Subscription Subscription { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public DateTime PaidAt { get; set; }
        public decimal Amount { get; set; }
    }
}
