using System.ComponentModel.DataAnnotations;

public enum AuthMode
{
    Login,
    Register
}

public sealed class LoginModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password.")]
    public string Password { get; set; } = "";
}

public sealed class RegisterModel
{
    [Required(ErrorMessage = "Please enter your username.")]
    public string Username { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please choose a password.")]
    [MinLength(8, ErrorMessage = "Use at least 8 characters.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";
}