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

public sealed record BrowseSetsDto(IReadOnlyList<SetTileDto> Sets);

public sealed record SetTileDto(
    long SetId, string Name, int Cards, long? TopCardId,
    string MetadataStatus, string? Era, DateOnly? ReleasedOn);

public sealed record BrowseSpeciesDto(IReadOnlyList<SpeciesTileDto> Species);

public sealed record SpeciesTileDto(
    int SpeciesId, string Name, string Slug, string GradientStart, string GradientEnd,
    int Printings, long TotalValueCents, IReadOnlyList<string> Types, short Generation,
    string Region, string Status, short Stage, string Color,
    IReadOnlyList<string> EggGroups, string? Habitat);
