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
