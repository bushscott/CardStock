using System.Globalization;
using CardStock.Domain.Census;

namespace CardStock.Application.Catalog;

public static class CatalogMappers
{
    public static SetPageDto ToDto(SetPageSnapshot snapshot) => new(
        snapshot.SetId, snapshot.Name, snapshot.MetadataStatus, snapshot.Code, snapshot.Era,
        snapshot.CardsTracked, snapshot.FirstSale?.ToString("yyyy-MM", CultureInfo.InvariantCulture),
        snapshot.Roster.Select(ToDto).ToArray());

    private static SetRosterRowDto ToDto(RosterCard card) => new(
        card.CardId, card.Name, card.HasImage, card.PriceCents, card.Roc3M,
        ToDto(card.Pop), card.Sales30d);

    private static PopDto ToDto(PopulationDelta.Result pop) => new(
        pop.State switch
        {
            PopulationDeltaState.Available => "available",
            PopulationDeltaState.Pending => "pending",
            _ => "none",
        },
        pop.Fraction,
        pop.FirstObservedOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        pop.DeltasBeginOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

    public static CharacterPageDto ToDto(CharacterPageSnapshot s) => new(
        s.SpeciesId, s.Name, s.GradientStart, s.GradientEnd, Chips(s),
        s.Roster.Count, s.SetsCount, s.TotalValueCents, s.PricedPrintings,
        s.Roster.Select(r => new CharacterRosterRowDto(
            r.CardId, r.Name, r.HasImage, r.SetId, r.SetName, r.Year,
            r.PriceCents, r.Roc3M, r.Sales30d)).ToArray());

    /// <summary>The dex chips (character.md §3.2 as amended by D-110): types,
    /// gen (region in the tooltip — no authored game-pair map), stage, color,
    /// egg group(s), habitat only when it exists. Region and status: no chip.</summary>
    private static IReadOnlyList<ChipDto> Chips(CharacterPageSnapshot s)
    {
        var chips = new List<ChipDto>();
        chips.AddRange(s.Types.Select(t => new ChipDto(t, "Pokédex type")));
        chips.Add(new ChipDto($"Gen {s.Generation}",
            $"First appeared in Generation {s.Generation} ({s.Region})"));
        chips.Add(s.Stage == 0
            ? new ChipDto("Basic", "Evolution stage")
            : new ChipDto($"Stage {s.Stage}",
                s.EvolvesFrom is null
                    ? "Evolution stage"
                    : $"Evolution stage — evolves from {s.EvolvesFrom}"));
        chips.Add(new ChipDto(s.Color, "Official Pokédex color"));
        chips.AddRange(s.EggGroups.Select(g => new ChipDto($"{g} egg group", "Pokédex egg group")));
        if (s.Habitat is { } habitat)
        {
            chips.Add(new ChipDto($"{habitat} habitat", "Pokédex habitat"));
        }

        return chips;
    }
}
