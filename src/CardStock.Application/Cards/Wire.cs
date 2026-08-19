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
    string Title, string? CollectorNumber, int? SetSize, long SetId, string SetName,
    IReadOnlyList<SpeciesRefDto> Species, bool HasImage, DateTimeOffset? DelistedAt);

public sealed record SpeciesRefDto(string Name, string Slug);

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
    CensusMetricDto GemRate, CensusMetricDto Pace,
    IReadOnlyList<CensusDeltaBarDto> DeltaBars);

public sealed record CensusBarDto(string Grader, short Grade, int Count);

/// <summary>One census sentence (D-093 gates, D-102 form), always printed —
/// gem rate at the population panel's foot, pace at the grading-activity
/// panel's. State: "ok" | "lowdata". The segments are the whole sentence;
/// below a gate the value runs hold the – glyph and GateNote carries the
/// ◌ tooltip naming the rule and when it passes (null when ok).</summary>
public sealed record CensusMetricDto(
    string State, IReadOnlyList<MetricSegmentDto> Segments, string? GateNote);

/// <summary>Tone: "pos" | "neg" | "caution" | "neutral" — neutral renders in the
/// note's own muted colour. Mono marks the runs the mockup sets in the mono
/// face: the value and every number token, the – placeholders included.</summary>
public sealed record MetricSegmentDto(string Text, string Tone, bool Mono);

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
