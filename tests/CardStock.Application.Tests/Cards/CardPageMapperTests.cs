using CardStock.Application.Cards;
using CardStock.Domain.Census;
using CardStock.Domain.Prices;
using CardStock.Domain.Signals;

namespace CardStock.Application.Tests.Cards;

public class CardPageMapperTests
{
    private static readonly DateOnly CurrentMonth = new(2026, 8, 1);
    private static readonly DateTimeOffset ObservedAt = new(2026, 8, 10, 0, 0, 0, TimeSpan.Zero);

    private static MonthlyPrice Point(DateOnly month, int cents) => new(month, cents, ObservedAt);

    private static CardIdentity Identity(int? setSize = null, string? imageHash = null) => new(
        CardId: 42,
        Title: "Charizard #4/102",
        CollectorNumber: "4",
        SetSize: setSize,
        SetName: "Base Set",
        ImageHash: imageHash,
        DelistedAt: null,
        NotACardAt: null);

    private static CardCensus Census() => new(
        Bars:
        [
            new CensusCell("psa", 8, 10),
            new CensusCell("psa", 9, 20),
            new CensusCell("psa", 10, 5),
            new CensusCell("cgc", 8, 3),
            new CensusCell("cgc", 9, 7),
            new CensusCell("cgc", 10, 2),
        ],
        PsaTotal: 35,
        CgcTotal: 12,
        ObservedAt: new DateTimeOffset(2026, 8, 11, 0, 0, 0, TimeSpan.Zero),
        QualifyingObservations: 42,
        Observations: []);

    /// <summary>
    /// Six tiers, always. Ungraded carries a hole mid-window (2026-02 absent
    /// with real points either side); Grade7 has no series at all; Grade8 has
    /// a single old point (stale, not current); the rest are minimal.
    /// </summary>
    private static CardPriceSnapshot Snapshot()
    {
        var ungradedPoints = new[]
        {
            Point(new DateOnly(2025, 9, 1), 1000),
            Point(new DateOnly(2025, 10, 1), 1100),
            Point(new DateOnly(2025, 11, 1), 1200),
            Point(new DateOnly(2025, 12, 1), 1300),
            Point(new DateOnly(2026, 1, 1), 1400),
            // 2026-02 deliberately absent: the hole.
            Point(new DateOnly(2026, 3, 1), 1600),
            Point(new DateOnly(2026, 4, 1), 1700),
            Point(new DateOnly(2026, 5, 1), 1800),
            Point(new DateOnly(2026, 6, 1), 1900),
            Point(new DateOnly(2026, 7, 1), 2000),
            Point(new DateOnly(2026, 8, 1), 2100),
        };
        var ungraded = new TierSnapshot(
            PriceTier.Ungraded,
            new TierSeries(PriceTier.Ungraded, ungradedPoints),
            new PriceAvailable(2100, new DateOnly(2026, 8, 1), IsCurrentMonth: true),
            new ChangeAvailable(0.062m, RecentSales: 5, PriorSales: 4));

        var grade7 = new TierSnapshot(
            PriceTier.Grade7,
            new TierSeries(PriceTier.Grade7, []),
            new NoPriceSeries(),
            new ChangeInsufficient(RecentSales: 1, PriorSales: 0));

        var grade8 = new TierSnapshot(
            PriceTier.Grade8,
            new TierSeries(PriceTier.Grade8, [Point(new DateOnly(2026, 6, 1), 4200)]),
            new PriceStale(new DateOnly(2026, 6, 1)),
            new ChangeInsufficient(RecentSales: 2, PriorSales: 1));

        var grade9 = new TierSnapshot(
            PriceTier.Grade9, new TierSeries(PriceTier.Grade9, []),
            new NoPriceSeries(), new ChangeInsufficient(0, 0));
        var grade9Half = new TierSnapshot(
            PriceTier.Grade9Half, new TierSeries(PriceTier.Grade9Half, []),
            new NoPriceSeries(), new ChangeInsufficient(0, 0));
        var psa10 = new TierSnapshot(
            PriceTier.Psa10, new TierSeries(PriceTier.Psa10, []),
            new NoPriceSeries(), new ChangeInsufficient(0, 0));

        return new CardPriceSnapshot(
            CardId: 42,
            LastVisitedAt: new DateTimeOffset(2026, 8, 12, 3, 0, 0, TimeSpan.Zero),
            Tiers: [ungraded, grade7, grade8, grade9, grade9Half, psa10]);
    }

    private static readonly DateOnly Today = new(2026, 8, 13);

    private static IReadOnlyList<LedgerSale> Sales() =>
    [
        new LedgerSale(new DateOnly(2026, 8, 13), "PSA 10", 12345, null, "ebay", "today, counts"),
        new LedgerSale(new DateOnly(2026, 8, 1), "PSA 10", 12345, null, "ebay", "this month, counts"),
        new LedgerSale(new DateOnly(2026, 7, 15), "Grade 9", 9345, null, "ebay", "inside the window, counts"),
        new LedgerSale(new DateOnly(2026, 7, 14), "Grade 9", 9345, null, "ebay", "the 31st day back, does not"),
        new LedgerSale(new DateOnly(2026, 1, 1), "Ungraded", 345, null, "ebay", "ancient, does not"),
        new LedgerSale(new DateOnly(2026, 9, 1), "Ungraded", 345, null, "ebay", "future-dated, does not"),
    ];

    private static CardPageSnapshotDto Map() =>
        CardPageMapper.ToDto(Identity(), Snapshot(), Census(), Sales(), CurrentMonth, Today);

    [Fact]
    public void Windows_twelve_months_oldest_to_newest_with_the_hole_null_in_place()
    {
        var raw = Assert.Single(Map().Prices.Tiers, t => t.Tier == "Ungraded");

        Assert.Equal(12, raw.Points.Count);
        Assert.Equal(
            new[]
            {
                "2025-09", "2025-10", "2025-11", "2025-12", "2026-01", "2026-02",
                "2026-03", "2026-04", "2026-05", "2026-06", "2026-07", "2026-08",
            },
            raw.Points.Select(p => p.Month));
        Assert.Null(raw.Points[5].Cents); // 2026-02, the hole
        Assert.Equal(
            new int?[] { 1000, 1100, 1200, 1300, 1400, null, 1600, 1700, 1800, 1900, 2000, 2100 },
            raw.Points.Select(p => p.Cents));
    }

    [Fact]
    public void A_tier_with_no_series_windows_to_twelve_nulls()
    {
        var grade7 = Assert.Single(Map().Prices.Tiers, t => t.Tier == "Grade7");

        Assert.Equal(12, grade7.Points.Count);
        Assert.All(grade7.Points, p => Assert.Null(p.Cents));
    }

    [Fact]
    public void Six_tiers_stay_six_in_strip_order_with_real_name_labels()
    {
        var dto = Map();

        Assert.Equal(
            new[] { "Ungraded", "Grade7", "Grade8", "Grade9", "Grade9Half", "Psa10" },
            dto.Prices.Tiers.Select(t => t.Tier));
        Assert.Equal(
            new[] { "Raw", "Grade 7", "Grade 8", "Grade 9", "Grade 9.5", "PSA 10" },
            dto.Prices.Tiers.Select(t => t.Label));
    }

    [Fact]
    public void Current_month_formats_as_yyyy_MM()
    {
        Assert.Equal("2026-08", Map().Prices.CurrentMonth);
    }

    [Fact]
    public void PriceAvailable_maps_to_the_available_state_with_cents_month_and_current_flag()
    {
        var raw = Assert.Single(Map().Prices.Tiers, t => t.Tier == "Ungraded");

        Assert.Equal("available", raw.Price.State);
        Assert.Equal(2100, raw.Price.Cents);
        Assert.Equal("2026-08", raw.Price.Month);
        Assert.True(raw.Price.IsCurrentMonth);
    }

    [Fact]
    public void PriceStale_maps_to_the_stale_state_with_only_the_newest_month()
    {
        var grade8 = Assert.Single(Map().Prices.Tiers, t => t.Tier == "Grade8");

        Assert.Equal("stale", grade8.Price.State);
        Assert.Null(grade8.Price.Cents);
        Assert.Equal("2026-06", grade8.Price.Month);
        Assert.Null(grade8.Price.IsCurrentMonth);
    }

    [Fact]
    public void NoPriceSeries_maps_to_the_none_state_with_nothing_else()
    {
        var grade7 = Assert.Single(Map().Prices.Tiers, t => t.Tier == "Grade7");

        Assert.Equal("none", grade7.Price.State);
        Assert.Null(grade7.Price.Cents);
        Assert.Null(grade7.Price.Month);
        Assert.Null(grade7.Price.IsCurrentMonth);
    }

    [Fact]
    public void ChangeAvailable_maps_to_the_available_state_with_the_fraction_and_sale_counts()
    {
        var raw = Assert.Single(Map().Prices.Tiers, t => t.Tier == "Ungraded");

        Assert.Equal("available", raw.Change.State);
        Assert.Equal(0.062m, raw.Change.Fraction);
        Assert.Equal(5, raw.Change.RecentSales);
        Assert.Equal(4, raw.Change.PriorSales);
    }

    [Fact]
    public void ChangeInsufficient_maps_to_the_insufficient_state_with_a_null_fraction()
    {
        var grade7 = Assert.Single(Map().Prices.Tiers, t => t.Tier == "Grade7");

        Assert.Equal("insufficient", grade7.Change.State);
        Assert.Null(grade7.Change.Fraction);
        Assert.Equal(1, grade7.Change.RecentSales);
        Assert.Equal(0, grade7.Change.PriorSales);
    }

    [Fact]
    public void Signals_compose_engine_volume_and_locked_rows_in_display_order_with_true_counts()
    {
        // The fixture's only usable series is Ungraded: rising 1000→2000 over the
        // closed months with the 2026-02 hole. So the engine fires ROC (+18%) and
        // Trend R² (monotone rise), drawdown reads quiet at 0%, and every
        // consecutive-run signal sits below its floor on the hole. The mapper
        // splices Sales volume after the firing block and appends the three
        // locked rows.
        var signals = Map().Signals;

        Assert.Equal(
            new[]
            {
                "ROC 3M", "Trend R²",
                "Sales volume",
                "Drawdown",
                "MACD (3,6,4)", "EMA 3/9 cross", "RSI (6)", "z vs 6M", "Tier spread 10/9",
                "RS vs index 3M", "Pop Δ 60d", "Churn 30d",
            },
            signals.Rows.Select(r => r.Name));
        Assert.Equal(12, signals.Evaluated);
        Assert.Equal(2, signals.Firing);
    }

    [Fact]
    public void Sales_volume_counts_only_the_last_thirty_days()
    {
        // today − 30 = 2026-07-14: strictly-after counts, the boundary day and
        // anything future-dated do not. Fixture: 3 of 6 qualify.
        var volume = Assert.Single(Map().Signals.Rows, r => r.Name == "Sales volume");

        Assert.Equal("●", volume.Glyph);
        Assert.Equal("3 / 30d", volume.Value);
        Assert.Equal("neutral", volume.State);
        Assert.Equal("neutral", volume.Tone);
        Assert.Equal(
            "Sales captured in the last 30 days. Liquidity signals are never directional.",
            volume.Tooltip);
    }

    [Fact]
    public void Locked_rows_carry_their_exact_copy()
    {
        var rows = Map().Signals.Rows;

        var rs = Assert.Single(rows, r => r.Name == "RS vs index 3M");
        Assert.Equal("◌", rs.Glyph);
        Assert.Equal("locked", rs.Value);
        Assert.Equal("locked", rs.State);
        Assert.Equal("Relative strength needs the market index — it arrives with the worker phase", rs.Tooltip);

        var pop = Assert.Single(rows, r => r.Name == "Pop Δ 60d");
        Assert.Equal("locked", pop.Value);
        Assert.Equal("Needs census deltas; observations count from 2026-09-01 — deltas need two", pop.Tooltip);

        var churn = Assert.Single(rows, r => r.Name == "Churn 30d");
        Assert.Equal("unlocks 2026-10-31", churn.Value);
        Assert.Equal("Needs 60+ post-seam days · 0 recorded", churn.Tooltip);
    }

    [Fact]
    public void Churn_counts_days_since_the_seam_floor_never_negative()
    {
        var beforeFloor = CardPageMapper.ToDto(
            Identity(), Snapshot(), Census(), [], CurrentMonth, new DateOnly(2026, 8, 13));
        var afterFloor = CardPageMapper.ToDto(
            Identity(), Snapshot(), Census(), [], new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 11));

        Assert.Equal(
            "Needs 60+ post-seam days · 0 recorded",
            Assert.Single(beforeFloor.Signals.Rows, r => r.Name == "Churn 30d").Tooltip);
        Assert.Equal(
            "Needs 60+ post-seam days · 10 recorded",
            Assert.Single(afterFloor.Signals.Rows, r => r.Name == "Churn 30d").Tooltip);
    }

    [Fact]
    public void Row_states_and_tones_serialize_lowercase()
    {
        var rows = Map().Signals.Rows;

        Assert.Equal("firing", Assert.Single(rows, r => r.Name == "ROC 3M").State);
        Assert.Equal("pos", Assert.Single(rows, r => r.Name == "ROC 3M").Tone);
        Assert.Equal("quiet", Assert.Single(rows, r => r.Name == "Drawdown").State);
        Assert.Equal("belowfloor", Assert.Single(rows, r => r.Name == "MACD (3,6,4)").State);
        Assert.Equal("locked", Assert.Single(rows, r => r.Name == "Churn 30d").State);
    }

    [Fact]
    public void SetSize_passes_through_as_null_until_enrichment_lands()
    {
        var dto = CardPageMapper.ToDto(Identity(setSize: null), Snapshot(), Census(), Sales(), CurrentMonth, Today);

        Assert.Null(dto.Identity.SetSize);
    }

    [Fact]
    public void SetSize_passes_through_when_present()
    {
        var dto = CardPageMapper.ToDto(Identity(setSize: 102), Snapshot(), Census(), Sales(), CurrentMonth, Today);

        Assert.Equal(102, dto.Identity.SetSize);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("abc123", true)]
    public void HasImage_derives_from_whether_the_image_hash_is_null(string? imageHash, bool expected)
    {
        var dto = CardPageMapper.ToDto(Identity(imageHash: imageHash), Snapshot(), Census(), Sales(), CurrentMonth, Today);

        Assert.Equal(expected, dto.Identity.HasImage);
    }

    [Fact]
    public void Census_bars_and_totals_pass_through_verbatim()
    {
        var dto = Map();

        Assert.Equal(6, dto.Census.Bars.Count);
        var psa10Bar = dto.Census.Bars[2];
        Assert.Equal("psa", psa10Bar.Grader);
        Assert.Equal((short)10, psa10Bar.Grade);
        Assert.Equal(5, psa10Bar.Count);
        Assert.Equal(35, dto.Census.PsaTotal);
        Assert.Equal(12, dto.Census.CgcTotal);
        Assert.Equal(42, dto.Census.QualifyingObservations);
    }

    [Fact]
    public void Census_metrics_render_low_data_with_their_own_unlock_notes_today()
    {
        // Today = 2026-08-13: gem rate's 90-day window reaches pre-floor, pace
        // has no qualifying deltas. Each slot names its OWN rule (D-093).
        var metrics = Map().Census.Metrics;

        Assert.Equal(new[] { "Gem rate", "Pace" }, metrics.Select(m => m.Name));
        Assert.All(metrics, m => Assert.Equal("lowdata", m.State));
        Assert.All(metrics, m => Assert.Null(m.Value));
        Assert.Contains("the window fills 2026-11-30", metrics[0].Segments.Single().Text);
        Assert.Equal(
            "needs census deltas; observations count from 2026-09-01, 0 so far — deltas need two",
            metrics[1].Segments.Single().Text);
    }

    [Fact]
    public void The_ghost_chart_ships_seven_pending_slots_before_the_floor()
    {
        // Today = 2026-08-13: the D-094 window is the first seven post-floor
        // months, all pending, each naming its close date.
        var bars = Map().Census.DeltaBars;

        Assert.Equal(7, bars.Count);
        Assert.All(bars, b => Assert.Equal("pending", b.State));
        Assert.All(bars, b => Assert.Null(b.Delta));
        Assert.Equal("2026-09", bars[0].Month);
        Assert.Equal("Sep ’26", bars[0].Label);
        Assert.Equal("new PSA 10 slabs for Sep ’26 — closes 2026-10-01", bars[0].Tooltip);
        Assert.Equal("2027-03", bars[6].Month);
    }

    [Fact]
    public void Census_metric_states_and_tones_serialize_lowercase_when_computed()
    {
        // Two closed post-floor months (Sep +10, Oct +42) on a 1,000-slab
        // census: pace computes, +5% over 2 months → supply-pressure red.
        var census = CardCensus.From(
        [
            new CensusObservation("psa", 10, 1000, new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero)),
            new CensusObservation("psa", 10, 1010, new DateTimeOffset(2026, 9, 20, 12, 0, 0, TimeSpan.Zero)),
            new CensusObservation("psa", 10, 1052, new DateTimeOffset(2026, 10, 15, 12, 0, 0, TimeSpan.Zero)),
        ]);

        var dto = CardPageMapper.ToDto(
            Identity(), Snapshot(), census, [], new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 5));

        var pace = Assert.Single(dto.Census.Metrics, m => m.Name == "Pace");
        Assert.Equal("ok", pace.State);
        Assert.Equal("+42 / mo", pace.Value);
        var growth = Assert.Single(pace.Segments, s => s.Text == "+5%");
        Assert.Equal("neg", growth.Tone);
    }

    [Fact]
    public void CardId_and_freshness_come_from_identity_and_the_snapshot()
    {
        var dto = Map();

        Assert.Equal(42, dto.CardId);
        Assert.Equal(new DateTimeOffset(2026, 8, 12, 3, 0, 0, TimeSpan.Zero), dto.Freshness.LastVisitedAt);
    }

    [Fact]
    public void Sale_maps_every_field_verbatim()
    {
        var sale = new LedgerSale(
            new DateOnly(2026, 7, 15), "PSA 10", 12345, 12999, "eBay", "Charizard <script>alert(1)</script>");

        var dto = CardPageMapper.ToDto(sale);

        Assert.Equal(sale.SoldOn, dto.SoldOn);
        Assert.Equal(sale.GradeTier, dto.GradeTier);
        Assert.Equal(sale.PriceCents, dto.PriceCents);
        Assert.Equal(sale.ListedPriceCents, dto.ListedPriceCents);
        Assert.Equal(sale.Source, dto.Source);
        Assert.Equal(sale.Title, dto.Title);
    }
}
