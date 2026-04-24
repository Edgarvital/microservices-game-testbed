using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineGame.InventoryService.Api.Contracts.Common;
using OnlineGame.InventoryService.Api.Contracts.Inventory;
using OnlineGame.InventoryService.Core.Application.Repositories;
using OnlineGame.InventoryService.Core.Domain.Entities;

namespace OnlineGame.InventoryService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/inventory")]
public sealed class InventoryController : ControllerBase
{
    private const int DefaultBaseLevel = 1;
    private const int DefaultBaseHp = 100;
    private const int DefaultBaseAttack = 10;

    private readonly IPlayerInventoryRepository _playerInventoryRepository;
    private readonly IPlayerDeckRepository _playerDeckRepository;
    private readonly ICardRepository _cardRepository;

    public InventoryController(
        IPlayerInventoryRepository playerInventoryRepository,
        IPlayerDeckRepository playerDeckRepository,
        ICardRepository cardRepository)
    {
        _playerInventoryRepository = playerInventoryRepository;
        _playerDeckRepository = playerDeckRepository;
        _cardRepository = cardRepository;
    }

    [HttpGet("{playerId:guid}")]
    [ProducesResponseType<InventoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryResponse>> GetInventory(Guid playerId, CancellationToken cancellationToken)
    {
        var accessError = EnsurePlayerAccess(playerId);
        if (accessError is not null)
        {
            return accessError;
        }

        var inventory = await _playerInventoryRepository.GetByPlayerIdAsync(playerId, cancellationToken);
        if (inventory is null)
        {
            return NotFound(Error("INVENTORY_NOT_FOUND", "Inventory was not found for the player."));
        }

        return Ok(ToResponse(inventory));
    }

    [HttpPost("{playerId:guid}/cards/{cardId:guid}/reward")]
    [ProducesResponseType<InventoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryResponse>> GrantCardReward(
        Guid playerId,
        Guid cardId,
        [FromBody] GrantCardRewardRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = EnsurePlayerAccess(playerId);
        if (accessError is not null)
        {
            return accessError;
        }

        var card = await _cardRepository.GetByIdAsync(cardId, cancellationToken);
        if (card is null)
        {
            return NotFound(Error("CARD_NOT_FOUND", "Card was not found in catalog."));
        }

        var inventory = await EnsureInventoryAsync(playerId, cancellationToken);

        var playerCard = inventory.Cards.FirstOrDefault(c => c.CardId == cardId);
        if (playerCard is null)
        {
            playerCard = new PlayerCard(playerId, cardId, 1, 0, DateTime.UtcNow, null);
            if (request.Amount > 1)
            {
                playerCard.AddDuplicates(request.Amount - 1);
            }

            inventory.Cards.Add(playerCard);
        }
        else
        {
            playerCard.AddDuplicates(request.Amount);
        }

        inventory.Touch(DateTime.UtcNow);
        await _playerInventoryRepository.UpdateAsync(inventory, cancellationToken);

        var updated = await _playerInventoryRepository.GetByPlayerIdAsync(playerId, cancellationToken);
        return Ok(ToResponse(updated!));
    }

    [HttpPost("{playerId:guid}/cards/{cardId:guid}/evolve")]
    [ProducesResponseType<InventoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InventoryResponse>> EvolveCard(
        Guid playerId,
        Guid cardId,
        [FromBody] EvolveCardRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = EnsurePlayerAccess(playerId);
        if (accessError is not null)
        {
            return accessError;
        }

        var inventory = await _playerInventoryRepository.GetByPlayerIdAsync(playerId, cancellationToken);
        if (inventory is null)
        {
            return NotFound(Error("INVENTORY_NOT_FOUND", "Inventory was not found for the player."));
        }

        var playerCard = inventory.Cards.FirstOrDefault(c => c.CardId == cardId);
        if (playerCard is null)
        {
            return NotFound(Error("PLAYER_CARD_NOT_FOUND", "Player does not own this card."));
        }

        if (!playerCard.CanEvolve(request.DuplicatesRequired))
        {
            return Conflict(Error("INSUFFICIENT_DUPLICATES", "Not enough duplicates to evolve this card."));
        }

        playerCard.EvolveUsingDuplicates(request.DuplicatesRequired, DateTime.UtcNow);
        inventory.Touch(DateTime.UtcNow);
        await _playerInventoryRepository.UpdateAsync(inventory, cancellationToken);

        var updated = await _playerInventoryRepository.GetByPlayerIdAsync(playerId, cancellationToken);
        return Ok(ToResponse(updated!));
    }

    [HttpPost("{playerId:guid}/decks")]
    [ProducesResponseType<PlayerDeckResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlayerDeckResponse>> CreateDeck(
        Guid playerId,
        [FromBody] CreateDeckRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = EnsurePlayerAccess(playerId);
        if (accessError is not null)
        {
            return accessError;
        }

        var inventory = await _playerInventoryRepository.GetByPlayerIdAsync(playerId, cancellationToken);
        if (inventory is null)
        {
            return NotFound(Error("INVENTORY_NOT_FOUND", "Inventory was not found for the player."));
        }

        var deck = new PlayerDeck(Guid.NewGuid(), playerId, request.Name.Trim());
        await _playerDeckRepository.AddAsync(deck, cancellationToken);

        var response = new PlayerDeckResponse
        {
            DeckId = deck.Id,
            Name = deck.Name,
            Slots = Array.Empty<DeckSlotResponse>()
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{playerId:guid}/decks/{deckId:guid}/slots")]
    [ProducesResponseType<PlayerDeckResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PlayerDeckResponse>> SetDeckSlots(
        Guid playerId,
        Guid deckId,
        [FromBody] SetDeckSlotsRequest request,
        CancellationToken cancellationToken)
    {
        var accessError = EnsurePlayerAccess(playerId);
        if (accessError is not null)
        {
            return accessError;
        }

        var inventory = await _playerInventoryRepository.GetByPlayerIdAsync(playerId, cancellationToken);
        if (inventory is null)
        {
            return NotFound(Error("INVENTORY_NOT_FOUND", "Inventory was not found for the player."));
        }

        var deck = await _playerDeckRepository.GetByIdAsync(deckId, cancellationToken);
        if (deck is null || deck.PlayerId != playerId)
        {
            return NotFound(Error("DECK_NOT_FOUND", "Deck was not found for the player."));
        }

        var duplicatedSlots = request.Slots
            .GroupBy(s => s.SlotIndex)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicatedSlots.Length > 0)
        {
            return Conflict(Error("DUPLICATE_SLOT_INDEX", "Deck slots must use unique slot indexes."));
        }

        var ownedCardIds = inventory.Cards.Select(c => c.CardId).ToHashSet();
        var invalidCard = request.Slots.FirstOrDefault(slot => !ownedCardIds.Contains(slot.CardId));
        if (invalidCard is not null)
        {
            return Conflict(Error("CARD_NOT_IN_INVENTORY", "Deck contains card(s) not owned by the player."));
        }

        deck.Slots.Clear();
        foreach (var slot in request.Slots.OrderBy(s => s.SlotIndex))
        {
            deck.Slots.Add(new PlayerDeckSlot(deck.Id, slot.SlotIndex, slot.CardId));
        }

        await _playerDeckRepository.UpdateAsync(deck, cancellationToken);
        inventory.Touch(DateTime.UtcNow);
        await _playerInventoryRepository.UpdateAsync(inventory, cancellationToken);

        return Ok(new PlayerDeckResponse
        {
            DeckId = deck.Id,
            Name = deck.Name,
            Slots = deck.Slots
                .OrderBy(s => s.SlotIndex)
                .Select(s => new DeckSlotResponse
                {
                    SlotIndex = s.SlotIndex,
                    CardId = s.CardId
                })
                .ToArray()
        });
    }

    private async Task<PlayerInventory> EnsureInventoryAsync(Guid playerId, CancellationToken cancellationToken)
    {
        var inventory = await _playerInventoryRepository.GetByPlayerIdAsync(playerId, cancellationToken);
        if (inventory is not null)
        {
            return inventory;
        }

        inventory = new PlayerInventory(playerId, DefaultBaseLevel, DefaultBaseHp, DefaultBaseAttack, DateTime.UtcNow);
        await _playerInventoryRepository.AddAsync(inventory, cancellationToken);

        return inventory;
    }

    private static InventoryResponse ToResponse(PlayerInventory inventory)
    {
        return new InventoryResponse
        {
            PlayerId = inventory.PlayerId,
            BaseLevel = inventory.BaseLevel,
            BaseHp = inventory.BaseHp,
            BaseAttack = inventory.BaseAttack,
            LastUpdate = inventory.LastUpdate,
            Cards = inventory.Cards
                .OrderBy(c => c.CardId)
                .Select(c => new PlayerCardResponse
                {
                    CardId = c.CardId,
                    CardName = c.Card?.Name ?? string.Empty,
                    CurrentLevel = c.CurrentLevel,
                    CountDuplicates = c.CountDuplicates,
                    DateObtained = c.DateObtained,
                    LastUpgrade = c.LastUpgrade
                })
                .ToArray(),
            Decks = inventory.Decks
                .OrderBy(d => d.Name)
                .Select(d => new PlayerDeckResponse
                {
                    DeckId = d.Id,
                    Name = d.Name,
                    Slots = d.Slots
                        .OrderBy(s => s.SlotIndex)
                        .Select(s => new DeckSlotResponse
                        {
                            SlotIndex = s.SlotIndex,
                            CardId = s.CardId
                        })
                        .ToArray()
                })
                .ToArray()
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

    private ActionResult? EnsurePlayerAccess(Guid playerId)
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(sub, out var subjectPlayerId))
        {
            return Unauthorized(Error("INVALID_TOKEN_SUB", "Token does not contain a valid player identifier."));
        }

        if (subjectPlayerId != playerId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, Error("PLAYER_ACCESS_DENIED", "Token player does not match requested player."));
        }

        return null;
    }
}
