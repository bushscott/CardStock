using CardStock.Domain.Census;
using CardStock.Domain.Prices;
using CardStock.Domain.Signals;

namespace CardStock.Application.Cards;

/// <summary>
/// Flattens the domain snapshot into the JSON the card page eats. Every union
/// becomes a "state" string here, once, so no downstream code re-implements
/// the switch. The 12-month window is computed here with PriceWindow.Of, and
/// the signals panel is composed here: the engine's price rows, the sales
/// volume row (sales stay out of Domain purity), and the locked rows whose
/// substrates do not exist yet (card.md §2.3.2 — never seed numbers).
/// </summary>
public static class CardPageMapper
{
    /// <summary>The D-033 floor: post-seam observation counting starts here.</summary>
    private static readonly DateOnly SeamFloor = new(2026, 9, 1);

    /// <summary>Churn 30d needs 60 post-seam days, so the unlock date is derived
    /// from the floor, never authored: 2026-10-31.</summary>
    private static readonly DateOnly ChurnUnlock = SeamFloor.AddDays(60);

    public static CardPageSnapshotDto ToDto(
        CardIdentity identity,
        CardPriceSnapshot prices,
        CardCensus census,
        IReadOnlyList<LedgerSale> sales,
        DateOnly currentMonth,
        DateOnly today) =>
        new(
            identity.CardId,
            ToIdentityDto(identity),
            ToPricesDto(prices, currentMonth),
            ToCensusDto(census, today),
            ToSignalsDto(prices, sales, currentMonth, today),
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

    private static CensusDto ToCensusDto(CardCensus census, DateOnly today) =>
        new(
            [.. census.Bars.Select(bar => new CensusBarDto(bar.Grader, bar.Grade, bar.Count))],
            census.PsaTotal,
            census.CgcTotal,
            census.ObservedAt,
            census.QualifyingObservations,
            [.. CensusMetrics.Evaluate(census.Observations, today).Select(ToMetricDto)],
            [.. CensusMetrics.DeltaBars(census.Observations, today).Select(ToDeltaBarDto)]);

    private static CensusDeltaBarDto ToDeltaBarDto(CensusDeltaBar bar) =>
        new(
            bar.Month.ToString("yyyy-MM"),
            bar.Label,
            bar.Observed ? "observed" : "pending",
            bar.Delta,
            bar.Tooltip);

    private static CensusMetricDto ToMetricDto(CensusMetric metric) =>
        new(
            metric.Name,
            metric.State == MetricState.Ok ? "ok" : "lowdata",
            metric.Value,
            [.. metric.Segments.Select(s => new MetricSegmentDto(s.Text, s.Tone.ToString().ToLowerInvariant()))]);

    private static SignalsDto ToSignalsDto(
        CardPriceSnapshot prices, IReadOnlyList<LedgerSale> sales, DateOnly currentMonth, DateOnly today)
    {
        // Display order (card.md §2.3.2): firing → neutral → quiet → below-floor
        // → locked. The engine hands back firing → quiet → below-floor; splice
        // the volume row after the firing block, append the locked rows.
        var rows = new List<SignalRow>(ChipEngine.EvaluateRows(prices, currentMonth));
        var firstNotFiring = rows.FindIndex(r => r.State != SignalState.Firing);
        rows.Insert(firstNotFiring < 0 ? rows.Count : firstNotFiring, VolumeRow(sales, today));

        rows.Add(new SignalRow("◌", "RS vs index 3M", "locked",
            "Relative strength needs the market index — it arrives with the worker phase",
            SignalState.Locked, ChipTone.Neutral));
        rows.Add(new SignalRow("◌", "Pop Δ 60d", "locked",
            "Needs census deltas; observations count from 2026-09-01 — deltas need two",
            SignalState.Locked, ChipTone.Neutral));
        rows.Add(ChurnRow(today));

        return new SignalsDto(
            rows.Count,
            rows.Count(r => r.State == SignalState.Firing),
            [.. rows.Select(ToRowDto)]);
    }

    /// <summary>Neutral, always: liquidity signals are never directional. The window
    /// is (today − 30, today] — a sale on the boundary day is outside it, and a
    /// future-dated sale (restatements happen) never counts.</summary>
    private static SignalRow VolumeRow(IReadOnlyList<LedgerSale> sales, DateOnly today)
    {
        var floor = today.AddDays(-30);
        var count = sales.Count(s => s.SoldOn > floor && s.SoldOn <= today);
        return new SignalRow("●", "Sales volume", $"{count} / 30d",
            "Sales captured in the last 30 days. Liquidity signals are never directional.",
            SignalState.Neutral, ChipTone.Neutral);
    }

    private static SignalRow ChurnRow(DateOnly today)
    {
        var recorded = Math.Max(0, today.DayNumber - SeamFloor.DayNumber);
        return new SignalRow("◌", "Churn 30d", $"unlocks {ChurnUnlock:yyyy-MM-dd}",
            $"Needs 60+ post-seam days · {recorded} recorded",
            SignalState.Locked, ChipTone.Neutral);
    }

    private static SignalRowDto ToRowDto(SignalRow row) =>
        new(row.Glyph, row.Name, row.Value, row.Tooltip,
            row.State.ToString().ToLowerInvariant(), row.Tone.ToString().ToLowerInvariant());

    private static string FormatMonth(DateOnly month) => month.ToString("yyyy-MM");
}
