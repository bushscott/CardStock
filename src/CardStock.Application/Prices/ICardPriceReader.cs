using CardStock.Domain.Prices;

namespace CardStock.Application.Prices;

/// <summary>
/// One card's prices. Returns null only when the card id is unknown -- a card
/// that exists but has no prices comes back with six empty tiers, because "we
/// have never seen a price for this" and "there is no such card" are different
/// answers and the Card page renders them differently.
/// </summary>
public interface ICardPriceReader
{
    public Task<CardPriceSnapshot?> GetAsync(long cardId, CancellationToken cancellationToken = default);
}
