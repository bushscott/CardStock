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
}
