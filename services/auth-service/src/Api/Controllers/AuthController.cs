using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using OnlineGame.AuthService.Core.Application.Players;
using OnlineGame.AuthService.Api.Contracts.Auth;
using OnlineGame.AuthService.Core.Application.Security;
using OnlineGame.AuthService.Core.Domain;

namespace OnlineGame.AuthService.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IPlayerRepository _playerRepository;
    private readonly ISecretProvider _secretProvider;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthController(
        IPlayerRepository playerRepository,
        ISecretProvider secretProvider,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _playerRepository = playerRepository;
        _secretProvider = secretProvider;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    [HttpPost("guest/login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthResponse>> LoginGuest([FromBody] LoginGuestRequest request, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetByDeviceIdAsync(request.DeviceId, cancellationToken);
        if (player is null)
        {
            player = Player.CreateGuest(request.DeviceId);
            player.RecordLogin();
            await _playerRepository.AddAsync(player, cancellationToken);
        }
        else
        {
            player.RecordLogin();
            await _playerRepository.UpdateAsync(player, cancellationToken);
        }

        var authResponse = await BuildAuthResponseAsync(player);
        return Ok(authResponse);
    }

    [HttpPost("guest/link-account")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> LinkGuestAccount([FromBody] LinkGuestAccountRequest request, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetByDeviceIdAsync(request.DeviceId, cancellationToken);
        if (player is null)
        {
            return NotFound(new { message = "Guest player not found for provided deviceId." });
        }

        var existingRegistered = await _playerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingRegistered is not null && existingRegistered.Id != player.Id)
        {
            return Conflict(new { message = "Email is already linked to another account." });
        }

        player.LinkAccount(request.Email, request.PasswordHash);
        player.RecordLogin();
        await _playerRepository.UpdateAsync(player, cancellationToken);

        var authResponse = await BuildAuthResponseAsync(player);
        return Ok(authResponse);
    }

    [HttpPost("registered/login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> LoginRegistered([FromBody] LoginRegisteredRequest request, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (player is null)
        {
            player = Player.CreateRegistered(request.Email, request.PasswordHash);
            player.RecordLogin();
            await _playerRepository.AddAsync(player, cancellationToken);
        }
        else
        {
            if (!string.Equals(player.PasswordHash, request.PasswordHash, StringComparison.Ordinal))
            {
                return Unauthorized(new { message = "Invalid email/passwordHash." });
            }

            player.RecordLogin();
            await _playerRepository.UpdateAsync(player, cancellationToken);
        }

        var authResponse = await BuildAuthResponseAsync(player);
        return Ok(authResponse);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(Player player)
    {
        var secretKey = await _secretProvider.GetJwtSecretAsync();
        var accessToken = _jwtTokenGenerator.GenerateToken(player, secretKey);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            ExpiresAtUtc = jwt.ValidTo,
            PlayerId = player.Id,
            IsGuest = player.IsGuest,
            BaseLevel = player.BaseLevel,
            Email = player.Email,
            DeviceId = player.DeviceId
        };
    }
}