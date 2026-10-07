using System.ComponentModel.DataAnnotations;

public enum AuthMode
{
    Login,
    Register
}

public sealed class LoginModel
{
    [Required(ErrorMessage = "Gib deine E-Mail-Adresse ein.")]
    [EmailAddress(ErrorMessage = "Gib eine gültige E-Mail-Adresse ein.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Gib dein Passwort ein.")]
    public string Password { get; set; } = "";

    public bool RememberMe { get; set; }
}

public sealed class RegisterModel
{
    [Required(ErrorMessage = "Gib deinen Namen ein.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Gib deine E-Mail-Adresse ein.")]
    [EmailAddress(ErrorMessage = "Gib eine gültige E-Mail-Adresse ein.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Wähle ein Passwort.")]
    [MinLength(8, ErrorMessage = "Verwende mindestens 8 Zeichen.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Bestätige dein Passwort.")]
    [Compare(nameof(Password), ErrorMessage = "Die Passwörter stimmen nicht überein.")]
    public string ConfirmPassword { get; set; } = "";
}
