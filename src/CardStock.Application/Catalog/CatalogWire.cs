namespace CardStock.Application.Catalog;

public sealed record SetPageDto(
    long SetId, string Name, string MetadataStatus, string? Code, string? Era,
    int CardsTracked, string? FirstSaleMonth, IReadOnlyList<SetRosterRowDto> Roster);

public sealed record SetRosterRowDto(
    long CardId, string Name, bool HasImage, int? PriceCents, decimal? Roc3M,
    PopDto Pop, int Sales30d);

/// <summary>State: "available" | "pending" | "none". Dates are "yyyy-MM-dd" and
/// computed, never authored (D-061) — the client prints them into the gate
/// tooltips verbatim.</summary>
public sealed record PopDto(
    string State, decimal? Fraction, string? FirstObservedOn, string? DeltasBeginOn);

public sealed record CharacterPageDto(
    int SpeciesId, string Name, string GradientStart, string GradientEnd,
    IReadOnlyList<ChipDto> Chips, int Printings, int SetsCount,
    long TotalValueCents, int PricedPrintings, IReadOnlyList<CharacterRosterRowDto> Roster);

public sealed record ChipDto(string Label, string Tooltip);

public sealed record CharacterRosterRowDto(
    long CardId, string Name, bool HasImage, long SetId, string SetName, short? Year,
    int? PriceCents, decimal? Roc3M, int Sales30d);
