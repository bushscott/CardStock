using CardStock.Domain.Census;
using CardStock.Domain.Prices;
using CardStock.Domain.Signals;

namespace CardStock.Application.Cards;

/// <summary>
/// Flattens the domain snapshot into the JSON the card page eats. Every union
/// becomes a "state" string here, once, so no downstream code re-implements
/// the switch. The 12-month window is computed here with PriceWindow.Of.
/// </summary>
public static class CardPageMapper
{
    public static CardPageSnapshotDto ToDto(
        CardIdentity identity,
        CardPriceSnapshot prices,
        CardCensus census,
        IReadOnlyList<SignalChip> chips,
        DateOnly currentMonth) =>
        new(
            identity.CardId,
            ToIdentityDto(identity),
            ToPricesDto(prices, currentMonth),
            ToCensusDto(census),
            [.. chips.Select(ToChipDto)],
            new FreshnessDto(prices.LastVisitedAt));

    public static SaleDto ToDto(LedgerSale sale) =>
        new(sale.SoldOn, sale.GradeTier, sale.PriceCents, sale.ListedPriceCents, sale.Source, sale.Title);

    private static IdentityDto ToIdentityDto(CardIdentity identity) =>
        new(
            identity.Title,
            identity.CollectorNumber,
            identity.SetSize,
            identity.SetName,
            identity.ImageHash is not null,
            identity.DelistedAt);

    private static PricesDto ToPricesDto(CardPriceSnapshot prices, DateOnly currentMonth) =>
        new(FormatMonth(currentMonth), [.. prices.Tiers.Select(tier => ToTierDto(tier, currentMonth))]);

    private static TierDto ToTierDto(TierSnapshot tier, DateOnly currentMonth) =>
        new(
            tier.Tier.ToString(),
            TierLabels.For(tier.Tier),
            [.. PriceWindow.Of(tier.Series, currentMonth, 12).Select(ToPointDto)],
            ToTierPriceDto(tier.Price),
            ToTierChangeDto(tier.Change));

    private static PointDto ToPointDto(PriceSlot slot) => slot switch
    {
        ObservedPrice observed => new PointDto(FormatMonth(observed.Month), observed.PriceCents),
        MissingMonth or OutsideSeries => new PointDto(FormatMonth(slot.Month), null),
        _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown PriceSlot case."),
    };

    private static TierPriceDto ToTierPriceDto(TierPrice price) => price switch
    {
        PriceAvailable available => new TierPriceDto(
            "available", available.PriceCents, FormatMonth(available.Month), available.IsCurrentMonth),
        PriceStale stale => new TierPriceDto("stale", null, FormatMonth(stale.NewestMonth), null),
        NoPriceSeries => new TierPriceDto("none", null, null, null),
        _ => throw new ArgumentOutOfRangeException(nameof(price), price, "Unknown TierPrice case."),
    };

    private static TierChangeDto ToTierChangeDto(TierChange change) => change switch
    {
        ChangeAvailable available => new TierChangeDto(
            "available", available.Fraction, available.RecentSales, available.PriorSales),
        ChangeInsufficient insufficient => new TierChangeDto(
            "insufficient", null, insufficient.RecentSales, insufficient.PriorSales),
        _ => throw new ArgumentOutOfRangeException(nameof(change), change, "Unknown TierChange case."),
    };

    private static CensusDto ToCensusDto(CardCensus census) =>
        new(
            [.. census.Bars.Select(bar => new CensusBarDto(bar.Grader, bar.Grade, bar.Count))],
            census.PsaTotal,
            census.CgcTotal,
            census.ObservedAt,
            census.QualifyingObservations);

    private static ChipDto ToChipDto(SignalChip chip) =>
        new(chip.Glyph, chip.Text, chip.Tooltip, chip.Tone.ToString().ToLowerInvariant());

    private static string FormatMonth(DateOnly month) => month.ToString("yyyy-MM");
}
