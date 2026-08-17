namespace CardStock.Application.Catalog;

public interface ICharacterPageReader
{
    public Task<CharacterPageSnapshot?> GetAsync(string slug, CancellationToken ct = default);
}

/// <summary>One species page: identity, the three live tiles' inputs, the full
/// roster. Printings is Roster.Count by construction.</summary>
public sealed record CharacterPageSnapshot(
    int SpeciesId,
    string Name,
    string Slug,
    string GradientStart,
    string GradientEnd,
    short Generation,
    string Region,
    string Color,
    string? Habitat,
    short Status,
    short Stage,
    string? EvolvesFrom,
    IReadOnlyList<string> Types,
    IReadOnlyList<string> EggGroups,
    int SetsCount,
    long TotalValueCents,
    int PricedPrintings,
    IReadOnlyList<CharacterRosterCard> Roster);

public sealed record CharacterRosterCard(
    long CardId,
    string Name,
    bool HasImage,
    long SetId,
    string SetName,
    short? Year,
    int? PriceCents,
    decimal? Roc3M,
    int Sales30d);
