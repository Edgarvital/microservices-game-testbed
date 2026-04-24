using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineGame.InventoryService.Api.Contracts.Common;
using OnlineGame.InventoryService.Api.Contracts.Inventory;
using OnlineGame.InventoryService.Core.Application.Repositories;
using OnlineGame.InventoryService.Core.Domain.Entities;

namespace OnlineGame.InventoryService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cards")]
public sealed class CardsController : ControllerBase
{
    private readonly ICardRepository _cardRepository;

    public CardsController(ICardRepository cardRepository)
    {
        _cardRepository = cardRepository;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CardResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CardResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var cards = await _cardRepository.GetAllAsync(cancellationToken);

        var response = cards
            .OrderBy(c => c.Name)
            .Select(c => new CardResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                BaseDamage = c.BaseDamage,
                Rarity = c.Rarity
            })
            .ToArray();

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType<CardResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CardResponse>> Create([FromBody] CreateCardRequest request, CancellationToken cancellationToken)
    {
        var cards = await _cardRepository.GetAllAsync(cancellationToken);
        if (cards.Any(c => string.Equals(c.Name, request.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new ApiErrorResponse
            {
                Code = "CARD_NAME_CONFLICT",
                Message = "A card with this name already exists."
            });
        }

        var card = new Card(Guid.NewGuid(), request.Name.Trim(), request.Description.Trim(), request.BaseDamage, request.Rarity);
        await _cardRepository.AddAsync(card, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new CardResponse
        {
            Id = card.Id,
            Name = card.Name,
            Description = card.Description,
            BaseDamage = card.BaseDamage,
            Rarity = card.Rarity
        });
    }
}
