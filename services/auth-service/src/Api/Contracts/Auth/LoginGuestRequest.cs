using System.ComponentModel.DataAnnotations;

namespace OnlineGame.AuthService.Api.Contracts.Auth;

public sealed class LoginGuestRequest
{
    [Required]
    [MinLength(3)]
    public string DeviceId { get; set; } = string.Empty;
}