using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using OnlineGame.AuthService.Api.Contracts.Common;
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
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> LoginGuest([FromBody] LoginGuestRequest request, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetByDeviceIdAsync(request.DeviceId, cancellationToken);
        if (player is null)
        {
            return NotFound(Error("GUEST_NOT_FOUND", "Guest player not found."));
        }

        if (!player.IsGuest)
        {
            return Unauthorized(Error("DEVICE_LINKED_TO_REGISTERED", "This deviceId is linked to a registered account."));
        }

        player.RecordLogin();
        await _playerRepository.UpdateAsync(player, cancellationToken);

        var authResponse = await BuildAuthResponseAsync(player);
        return Ok(authResponse);
    }

    [HttpPost("guest/register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> RegisterGuest([FromBody] RegisterGuestRequest request, CancellationToken cancellationToken)
    {
        var existingPlayer = await _playerRepository.GetByDeviceIdAsync(request.DeviceId, cancellationToken);
        if (existingPlayer is not null)
        {
            return Conflict(Error("DEVICE_ALREADY_REGISTERED", "DeviceId is already registered."));
        }

        var player = Player.CreateGuest(request.DeviceId);
        player.RecordLogin();
        await _playerRepository.AddAsync(player, cancellationToken);

        var authResponse = await BuildAuthResponseAsync(player);
        return StatusCode(StatusCodes.Status201Created, authResponse);
    }

    [HttpPost("guest/link-account")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> LinkGuestAccount([FromBody] LinkGuestAccountRequest request, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetByDeviceIdAsync(request.DeviceId, cancellationToken);
        if (player is null)
        {
            return NotFound(Error("GUEST_NOT_FOUND", "Guest player not found for provided deviceId."));
        }

        var existingRegistered = await _playerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingRegistered is not null && existingRegistered.Id != player.Id)
        {
            return Conflict(Error("EMAIL_ALREADY_LINKED", "Email is already linked to another account."));
        }

        player.LinkAccount(request.Email, request.PasswordHash);
        player.RecordLogin();
        await _playerRepository.UpdateAsync(player, cancellationToken);

        var authResponse = await BuildAuthResponseAsync(player);
        return Ok(authResponse);
    }

    [HttpPost("registered/login")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> LoginRegistered([FromBody] LoginRegisteredRequest request, CancellationToken cancellationToken)
    {
        var player = await _playerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (player is null)
        {
            return NotFound(Error("REGISTERED_NOT_FOUND", "Registered player not found."));
        }

        if (!string.Equals(player.PasswordHash, request.PasswordHash, StringComparison.Ordinal))
        {
            return Unauthorized(Error("INVALID_CREDENTIALS", "Invalid email/passwordHash."));
        }

        player.RecordLogin();
        await _playerRepository.UpdateAsync(player, cancellationToken);

        var authResponse = await BuildAuthResponseAsync(player);
        return Ok(authResponse);
    }

    [HttpPost("registered/register")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthResponse>> RegisterRegistered([FromBody] RegisterRegisteredRequest request, CancellationToken cancellationToken)
    {
        var existingPlayer = await _playerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingPlayer is not null)
        {
            return Conflict(Error("EMAIL_ALREADY_REGISTERED", "Email is already registered."));
        }

        var player = Player.CreateRegistered(request.Email, request.PasswordHash);
        player.RecordLogin();
        await _playerRepository.AddAsync(player, cancellationToken);

        var authResponse = await BuildAuthResponseAsync(player);
        return StatusCode(StatusCodes.Status201Created, authResponse);
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

    private static ApiErrorResponse Error(string code, string message)
    {
        return new ApiErrorResponse
        {
            Code = code,
            Message = message
        };
    }
}