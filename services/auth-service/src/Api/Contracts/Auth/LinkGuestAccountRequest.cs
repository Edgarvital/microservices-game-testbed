using System.ComponentModel.DataAnnotations;

namespace OnlineGame.AuthService.Api.Contracts.Auth;

public sealed class LinkGuestAccountRequest
{
    [Required]
    [MinLength(3)]
    public string DeviceId { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string PasswordHash { get; set; } = string.Empty;
}