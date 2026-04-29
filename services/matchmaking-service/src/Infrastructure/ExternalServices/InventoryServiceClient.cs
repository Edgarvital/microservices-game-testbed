using System.Net.Http.Json;

namespace OnlineGame.MatchmakingService.Infrastructure.ExternalServices;

/// <summary>
/// Cliente HTTP para chamar o inventory-service e obter dados do jogador.
/// </summary>
public sealed class InventoryServiceClient
{
    private readonly HttpClient _httpClient;

    public InventoryServiceClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Obt�m o poder atual do jogador do inventory-service.
    /// </summary>
    public async Task<int?> GetPlayerPowerAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/inventory/{playerId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var inventory = await response.Content.ReadFromJsonAsync<InventoryDto>(cancellationToken: cancellationToken);
            return inventory?.Power;
        }
        catch
        {
            return null;
        }
    }
}

public sealed class InventoryDto
{
    public Guid PlayerId { get; set; }
    public int BaseLevel { get; set; }
    public int BaseHp { get; set; }
    public int BaseAttack { get; set; }
    public int Power { get; set; }
    public DateTime LastUpdate { get; set; }
}
