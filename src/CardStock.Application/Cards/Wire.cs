using CardStock.Domain.Signals;

namespace CardStock.Application.Cards;

public sealed record CardPageSnapshotDto(
    long CardId,
    IdentityDto Identity,
    PricesDto Prices,
    CensusDto Census,
    IReadOnlyList<ChipDto> Signals,
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

public sealed record ChipDto(string Glyph, string Text, string Tooltip, string Tone); // "pos" | "neg" | "caution" | "neutral"

public sealed record FreshnessDto(DateTimeOffset? LastVisitedAt);

public sealed record SaleDto(
    DateOnly SoldOn, string GradeTier, int PriceCents, int? ListedPriceCents,
    string Source, string Title);
