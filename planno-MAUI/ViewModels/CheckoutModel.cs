using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using static Models.Dtos.BillingDtos;

public sealed partial class CheckoutModel : IValidatableObject
{
    [Required(ErrorMessage = "Bitte Kartennummer eingeben.")]
    public string CardNumber { get; set; } = "";

    [Required(ErrorMessage = "Bitte Ablaufdatum eingeben.")]
    [RegularExpression(@"^\s*(0[1-9]|1[0-2])\s*/\s*\d{2}\s*$", ErrorMessage = "Format MM/JJ, z. B. 08/29.")]
    public string Expiry { get; set; } = "";

    [Required(ErrorMessage = "Bitte CVC eingeben.")]
    [RegularExpression(@"^\d{3,4}$", ErrorMessage = "3 oder 4 Ziffern.")]
    public string Cvc { get; set; } = "";

    public CheckoutRequest ToRequest(int subscriptionId)
    {
        Match m = ExpiryPattern().Match(Expiry);
        return new CheckoutRequest(subscriptionId, CardNumber, int.Parse(m.Groups[1].Value), 2000 + int.Parse(m.Groups[2].Value), Cvc);
    }

    // Dieselben Prüfungen wie die API (Luhn, abgelaufen), nur mit deutschen Meldungen und auf die Formularfelder gemappt
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        foreach (ValidationResult result in ToRequest(1).Validate(context))
        {
            yield return result.MemberNames.Contains(nameof(CheckoutRequest.CardNumber))
                ? new ValidationResult("Ungültige Kartennummer.", [nameof(CardNumber)])
                : new ValidationResult("Die Karte ist abgelaufen.", [nameof(Expiry)]);
        }
    }

    [GeneratedRegex(@"(\d{2})\s*/\s*(\d{2})")]
    private static partial Regex ExpiryPattern();
}
