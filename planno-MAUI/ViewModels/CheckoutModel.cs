using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using static Models.Dtos.BillingDtos;

public sealed partial class CheckoutModel : IValidatableObject
{
    [Required(ErrorMessage = "Please enter your card number.")]
    public string CardNumber { get; set; } = "";

    [Required(ErrorMessage = "Please enter the expiration date.")]
    [RegularExpression(@"^\s*(0[1-9]|1[0-2])\s*/\s*\d{2}\s*$", ErrorMessage = "Format MM/YY, e.g., 08/29.")]
    public string Expiry { get; set; } = "";

    [Required(ErrorMessage = "Please enter the CVC.")]
    [RegularExpression(@"^\d{3,4}$", ErrorMessage = "3 or 4 digits.")]
    public string Cvc { get; set; } = "";

    public CheckoutRequest ToRequest(int subscriptionId)
    {
        Match m = ExpiryPattern().Match(Expiry);
        return new CheckoutRequest(subscriptionId, CardNumber, int.Parse(m.Groups[1].Value), 2000 + int.Parse(m.Groups[2].Value), Cvc);
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        foreach (ValidationResult result in ToRequest(1).Validate(context))
        {
            yield return result.MemberNames.Contains(nameof(CheckoutRequest.CardNumber))
                ? new ValidationResult("Invalid card number.", [nameof(CardNumber)])
                : new ValidationResult("The card has expired.", [nameof(Expiry)]);
        }
    }

    [GeneratedRegex(@"(\d{2})\s*/\s*(\d{2})")]
    private static partial Regex ExpiryPattern();
}