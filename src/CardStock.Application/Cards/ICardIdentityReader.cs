using CardStock.Domain.Cards;

namespace CardStock.Application.Cards;

/// <summary>
/// Who this card is. Null only for an unknown id. SetSize is null until the
/// sibling repo's enrichment lands (D-079); CollectorNumber comes from the
/// defensive name parse until then. ImageHash is server-side plumbing for the
/// image endpoint and never reaches the wire.
/// </summary>
public sealed record CardIdentity(
    long CardId,
    string Title,
    string? CollectorNumber,
    int? SetSize,
    string SetName,
    string? ImageHash,
    DateTimeOffset? DelistedAt,
    DateTimeOffset? NotACardAt);

public interface ICardIdentityReader
{
    public Task<CardIdentity?> GetAsync(long cardId, CancellationToken cancellationToken = default);
}
