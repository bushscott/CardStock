namespace CardStock.Application.Cards;

/// <summary>One ledger row, verbatim from the source. Title is hostile text —
/// render-encode it, never MarkupString (D-029).</summary>
public sealed record LedgerSale(
    DateOnly SoldOn,
    string GradeTier,
    int PriceCents,
    int? ListedPriceCents,
    string Source,
    string Title);

public interface ICardSalesReader
{
    Task<IReadOnlyList<LedgerSale>> GetAsync(long cardId, CancellationToken cancellationToken = default);
}
