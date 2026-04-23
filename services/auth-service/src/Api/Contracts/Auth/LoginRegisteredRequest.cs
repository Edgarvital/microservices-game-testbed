using System.ComponentModel.DataAnnotations;

namespace OnlineGame.AuthService.Api.Contracts.Auth;

public sealed class LoginRegisteredRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string PasswordHash { get; set; } = string.Empty;
}