using CardStock.Domain.Signals;

namespace CardStock.Application.Cards;

public sealed record CardPageSnapshotDto(
    long CardId,
    IdentityDto Identity,
    PricesDto Prices,
    CensusDto Census,
    SignalsDto Signals,
    FreshnessDto Freshness);

public sealed record IdentityDto(
    string Title, string? CollectorNumber, int? SetSize, string SetName,
    bool HasImage, DateTimeOffset? DelistedAt);

public sealed record PricesDto(string CurrentMonth, IReadOnlyList<TierDto> Tiers);

public sealed record TierDto(
    string Tier, string Label, IReadOnlyList<PointDto> Points,
    TierPriceDto Price, TierChangeDto Change);

/// <summary>Month is "yyyy-MM". Cents null = the source published no point (a hole or outside the series).</summary>
public sealed record PointDto(string Month, int? Cents);

public sealed record TierPriceDto(string State, int? Cents, string? Month, bool? IsCurrentMonth); // "available" | "stale" | "none"

public sealed record TierChangeDto(string State, decimal? Fraction, int RecentSales, int PriorSales); // "available" | "insufficient"

public sealed record CensusDto(
    IReadOnlyList<CensusBarDto> Bars, int PsaTotal, int CgcTotal,
    DateTimeOffset? ObservedAt, int QualifyingObservations);

public sealed record CensusBarDto(string Grader, short Grade, int Count);

/// <summary>The signals panel (card.md §2.3.2). Evaluated counts every row — locked
/// included: they were evaluated and found locked. Firing counts rows in the firing
/// state. Both are computed, never authored.</summary>
public sealed record SignalsDto(int Evaluated, int Firing, IReadOnlyList<SignalRowDto> Rows);

/// <summary>One signals-panel row. State: "firing" | "quiet" | "belowfloor" |
/// "neutral" | "locked". Tone: "pos" | "neg" | "caution" | "neutral".</summary>
public sealed record SignalRowDto(
    string Glyph, string Name, string Value, string Tooltip, string State, string Tone);

public sealed record FreshnessDto(DateTimeOffset? LastVisitedAt);

public sealed record SaleDto(
    DateOnly SoldOn, string GradeTier, int PriceCents, int? ListedPriceCents,
    string Source, string Title);
