using System.Text.Json;
using Microsoft.Extensions.Configuration;
using OnlineGame.AuthService.Core.Application.Security;

namespace OnlineGame.AuthService.Infrastructure.Security;

public sealed class VaultSecretProvider : ISecretProvider
{
    private readonly HttpClient _httpClient;
    private readonly VaultOptions _options;

    public VaultSecretProvider(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _options = VaultOptions.FromConfiguration(configuration);

        if (!string.IsNullOrWhiteSpace(_options.Address))
        {
            _httpClient.BaseAddress = new Uri(_options.Address, UriKind.Absolute);
        }
    }

    public async Task<string> GetJwtSecretAsync()
    {
        if (string.IsNullOrWhiteSpace(_options.Address))
        {
            throw new InvalidOperationException("Vault:Address is required when Secrets:Provider is Vault.");
        }

        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            throw new InvalidOperationException("Vault:Token is required when Secrets:Provider is Vault.");
        }

        var route = $"v1/{_options.MountPath.Trim('/')}/data/{_options.SecretPath.Trim('/')}";
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Add("X-Vault-Token", _options.Token);

        using var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Vault request failed with status code {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        if (!document.RootElement.TryGetProperty("data", out var dataNode) ||
            !dataNode.TryGetProperty("data", out var secretData))
        {
            throw new InvalidOperationException("Vault response does not contain KV v2 data.data payload.");
        }

        if (!secretData.TryGetProperty(_options.JwtSecretField, out var secretElement))
        {
            throw new InvalidOperationException($"Vault secret field '{_options.JwtSecretField}' was not found.");
        }

        var secret = secretElement.GetString();
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException("Vault returned an empty JWT secret.");
        }

        return secret.Trim();
    }
}