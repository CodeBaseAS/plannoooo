using System.ComponentModel.DataAnnotations;

namespace planno_API.Dtos
{
    public class BillingDtos
    {
        public record SubscriptionResponse(int Id, string Description, decimal Price, int Interval);
        public record SubscriptionStatusResponse(bool IsPremium, int? SubscriptionId, string? Description, DateTime? ExpiresAt);
        public record CheckoutRequest(
            [Range(1, int.MaxValue)] int SubscriptionId,
            [Required] string CardNumber,
            [Range(1, 12)] int ExpiryMonth,
            [Range(2000, 2100)] int ExpiryYear,
            [Required, RegularExpression(@"^\d{3,4}$")] string Cvc) : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext context)
            {
                string digits = CardNumber.Replace(" ", "");

                if (digits.Length is < 13 or > 19 || !digits.All(char.IsDigit) || !PassesLuhn(digits))
                {
                    yield return new ValidationResult("Invalid cardnumber.", [nameof(CardNumber)]);
                }

                if (ExpiryMonth is >= 1 and <= 12 && ExpiryYear is >= 2000 and <= 2100)
                {
                    DateTime validUntil = new DateTime(ExpiryYear, ExpiryMonth, 1).AddMonths(1);
                    if (validUntil <= DateTime.UtcNow) 
                    { 
                        yield return new ValidationResult("Die Karte ist abgelaufen.", [nameof(ExpiryYear)]);
                
                    }
            
                }
            }

            private static bool PassesLuhn(string digits)
            {
                int sum = 0;
                bool doubleIt = false;

                for (int i = digits.Length - 1; i >= 0; i--)
                {
                    int d = digits[i] - '0';
                    if (doubleIt)
                    {
                        d *= 2;
                        if (d > 9) d -= 9;
                    }
                    sum += d;
                    doubleIt = !doubleIt;
                }

                return sum % 10 == 0;
            }
        }
        public record CheckoutResponse(int PaymentId, decimal Amount, DateTime PaidAt, DateTime SubscriptionExpiresAt, string InvoiceNumber, string InvoiceUrl);
        public record InvoiceResponse(int Id, string InvoiceNumber, DateTime IssuedAt, decimal Amount, string Description, string PdfUrl);
    }
}
