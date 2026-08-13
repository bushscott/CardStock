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
        QualifyingObservations: 42);

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

    private static IReadOnlyList<SignalChip> Chips() =>
    [
        new SignalChip("▲", "ROC 3M +20%", "PSA 10 · 3-month return +20% · fires at ±15% · closed months only", ChipTone.Pos),
        new SignalChip("!", "thin data", "Fewer than 3 sales in the trailing window", ChipTone.Caution),
    ];

    private static CardPageSnapshotDto Map() =>
        CardPageMapper.ToDto(Identity(), Snapshot(), Census(), Chips(), CurrentMonth);

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
    public void Chip_tones_lowercase()
    {
        var dto = Map();

        Assert.Equal(new[] { "pos", "caution" }, dto.Signals.Select(c => c.Tone));
        Assert.Equal(new[] { "ROC 3M +20%", "thin data" }, dto.Signals.Select(c => c.Text));
    }

    [Fact]
    public void SetSize_passes_through_as_null_until_enrichment_lands()
    {
        var dto = CardPageMapper.ToDto(Identity(setSize: null), Snapshot(), Census(), Chips(), CurrentMonth);

        Assert.Null(dto.Identity.SetSize);
    }

    [Fact]
    public void SetSize_passes_through_when_present()
    {
        var dto = CardPageMapper.ToDto(Identity(setSize: 102), Snapshot(), Census(), Chips(), CurrentMonth);

        Assert.Equal(102, dto.Identity.SetSize);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("abc123", true)]
    public void HasImage_derives_from_whether_the_image_hash_is_null(string? imageHash, bool expected)
    {
        var dto = CardPageMapper.ToDto(Identity(imageHash: imageHash), Snapshot(), Census(), Chips(), CurrentMonth);

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
