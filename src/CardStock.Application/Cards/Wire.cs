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
    DateTimeOffset? ObservedAt, int QualifyingObservations,
    IReadOnlyList<CensusMetricDto> Metrics,
    IReadOnlyList<CensusDeltaBarDto> DeltaBars);

public sealed record CensusBarDto(string Grader, short Grade, int Count);

/// <summary>One census-metric slot (D-093): Gem rate or Pace. State: "ok" |
/// "lowdata". Ok carries the headline Value and the sentence as toned segments;
/// lowdata carries a single segment naming the rule and when it passes.</summary>
public sealed record CensusMetricDto(
    string Name, string State, string? Value, IReadOnlyList<MetricSegmentDto> Segments);

/// <summary>Tone: "pos" | "neg" | "caution" | "neutral" — neutral renders in the
/// note's own muted colour.</summary>
public sealed record MetricSegmentDto(string Text, string Tone);

/// <summary>One slot of the ghost delta chart (D-094). Month is "yyyy-MM".
/// State: "observed" (Delta present, bar filled and scaled) | "pending" (dashed
/// ghost, no number; the tooltip names the unlock).</summary>
public sealed record CensusDeltaBarDto(
    string Month, string Label, string State, int? Delta, string Tooltip);

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
