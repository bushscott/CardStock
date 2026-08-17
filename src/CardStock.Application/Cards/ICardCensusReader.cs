using CardStock.Domain.Census;

namespace CardStock.Application.Cards;

/// <summary>
/// The census pair's data. Always returns — a card with no rows is an all-zero
/// census (true zeros by the storage contract); existence is the identity
/// reader's question, not this one's.
/// </summary>
public interface ICardCensusReader
{
    public Task<CardCensus> GetAsync(long cardId, CancellationToken cancellationToken = default);
}
