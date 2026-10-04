namespace planno_API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int? ActiveSubscriptionId { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public Subscription? ActiveSubscription { get; set; }
    }
}
