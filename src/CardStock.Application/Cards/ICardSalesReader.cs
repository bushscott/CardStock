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
    /// <summary>D-091: the ledger ships the newest this-many sales per grade bucket,
    /// lifetime — a bucket truncates only once its captured history exceeds the cap,
    /// so rare buckets show their complete lives while fast buckets stay bounded.
    /// One constant shared by the reader's query and the ledger's copy.</summary>
    const int BucketCap = 300;

    Task<IReadOnlyList<LedgerSale>> GetAsync(long cardId, CancellationToken cancellationToken = default);
}
