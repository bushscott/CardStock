using CardStock.Application.Cards;
using CardStock.Web.Charts;

namespace CardStock.Web.Tests;

// card.md §2.4/§2.4.1/§2.4.2, task-17-brief's ChartShape/ShapedSeries contract. The twelve-month
// window always runs Sep '25 (index 0) .. Aug '26 (index 11, the current month) in these fixtures,
// matching the brief's own ChartShape doc-comment example ("Sep '25" / "Feb '26" / "Aug '26").
public class LwcSeriesShaperTests
{
    private static readonly string[] Months =
    [
        "2025-09", "2025-10", "2025-11", "2025-12", "2026-01", "2026-02",
        "2026-03", "2026-04", "2026-05", "2026-06", "2026-07", "2026-08",
    ];

    private static TierDto Tier(string tier, params int?[] cents)
    {
        if (cents.Length != 12)
        {
            throw new ArgumentException("Fixture bug: exactly 12 months required.", nameof(cents));
        }

        var points = Enumerable.Range(0, 12).Select(i => new PointDto(Months[i], cents[i])).ToList();
        return new TierDto(tier, tier, points, new TierPriceDto("none", null, null, null),
            new TierChangeDto("insufficient", null, 0, 0));
    }

    private static PricesDto Prices(params TierDto[] tiers) => new("2026-08", tiers);

    private static readonly int?[] AllNull = new int?[12];

    [Fact]
    public void Whitespace_inserted_exactly_at_null_slots()
    {
        var prices = Prices(Tier("Psa10", 100, 200, 300, null, 500, 600, 700, 800, 900, 1000, 1100, null));

        var shape = LwcSeriesShaper.Shape(prices, new HashSet<string> { "Psa10" });

        var points = shape.Series[0].Points;
        Assert.Equal(11, points.Count);
        Assert.Null(points[3].Value);
        Assert.Equal("2025-12-01", points[3].Time);
        Assert.Equal(1m, points[0].Value);
        Assert.Equal("2025-09-01", points[0].Time);
        Assert.Equal(11m, points[10].Value);
    }

    [Fact]
    public void Current_month_excluded_from_main_points_but_present_in_dashed_tail_when_both_present()
    {
        var cents = (int?[])AllNull.Clone();
        cents[10] = 7000; // last closed (2026-07)
        cents[11] = 7200; // current (2026-08)
        var prices = Prices(Tier("Psa10", cents));

        var shape = LwcSeriesShaper.Shape(prices, new HashSet<string> { "Psa10" });
        var series = shape.Series[0];

        Assert.Equal(11, series.Points.Count); // current month never a Points entry
        Assert.Equal(70m, series.Points[10].Value);
        Assert.NotNull(series.DashedTail);
        Assert.Equal(2, series.DashedTail!.Count);
        Assert.Equal(new ShapedPoint("2026-07-01", 70m), series.DashedTail[0]);
        Assert.Equal(new ShapedPoint("2026-08-01", 72m), series.DashedTail[1]);
    }

    [Theory]
    [InlineData(null, 7200)] // last closed missing
    [InlineData(7000, null)] // current missing
    [InlineData(null, null)] // both missing
    public void No_tail_when_either_last_closed_or_current_is_missing(int? lastClosed, int? current)
    {
        var cents = (int?[])AllNull.Clone();
        cents[10] = lastClosed;
        cents[11] = current;
        var prices = Prices(Tier("Psa10", cents));

        var shape = LwcSeriesShaper.Shape(prices, new HashSet<string> { "Psa10" });

        Assert.Null(shape.Series[0].DashedTail);
    }

    [Fact]
    public void Isolated_point_detected_null_value_null()
    {
        var cents = (int?[])AllNull.Clone();
        cents[5] = 4200; // 2026-02, flanked by null on both sides, no dashed tail (current month null)
        var prices = Prices(Tier("Psa10", cents));

        var shape = LwcSeriesShaper.Shape(prices, new HashSet<string> { "Psa10" });

        var isolated = shape.Series[0].IsolatedPoints;
        Assert.Single(isolated);
        Assert.Equal(new ShapedPoint("2026-02-01", 42m), isolated[0]);
    }

    [Fact]
    public void Last_closed_point_not_isolated_when_a_dashed_tail_connects_it()
    {
        var cents = (int?[])AllNull.Clone();
        cents[10] = 7000; // last closed, left neighbour (index 9) absent
        cents[11] = 7200; // current present -> dashed tail exists, connects index 10 onward
        var prices = Prices(Tier("Psa10", cents));

        var shape = LwcSeriesShaper.Shape(prices, new HashSet<string> { "Psa10" });

        Assert.NotNull(shape.Series[0].DashedTail);
        Assert.Empty(shape.Series[0].IsolatedPoints);
    }

    [Fact]
    public void Last_closed_point_isolated_when_no_dashed_tail_and_left_neighbour_absent()
    {
        var cents = (int?[])AllNull.Clone();
        cents[10] = 7000; // last closed, left neighbour (index 9) absent, no current month value
        var prices = Prices(Tier("Psa10", cents));

        var shape = LwcSeriesShaper.Shape(prices, new HashSet<string> { "Psa10" });

        Assert.Null(shape.Series[0].DashedTail);
        Assert.Single(shape.Series[0].IsolatedPoints);
        Assert.Equal("2026-07-01", shape.Series[0].IsolatedPoints[0].Time);
    }

    [Fact]
    public void Y_labels_reflect_visible_tiers_only_including_the_current_months_values()
    {
        var psa = (int?[])AllNull.Clone();
        psa[0] = 100_00; // $100 min across visible tiers
        psa[11] = 200_00; // current month counts toward the max
        var grade9 = (int?[])AllNull.Clone();
        grade9[3] = 150_00;
        var hiddenGrade8 = (int?[])AllNull.Clone();
        hiddenGrade8[0] = 900_00; // must NOT affect the label -- Grade 8 is not visible

        var prices = Prices(Tier("Psa10", psa), Tier("Grade9", grade9), Tier("Grade8", hiddenGrade8));

        var shape = LwcSeriesShaper.Shape(prices, new HashSet<string> { "Psa10", "Grade9" });

        Assert.Equal("$200", shape.YMaxLabel);
        Assert.Equal("$100", shape.YMinLabel);
    }

    [Fact]
    public void Y_labels_fall_back_to_a_dash_when_no_visible_tier_has_any_value()
    {
        var prices = Prices(Tier("Psa10", AllNull));

        var shape = LwcSeriesShaper.Shape(prices, new HashSet<string> { "Psa10" });

        Assert.Equal("—", shape.YMaxLabel);
        Assert.Equal("—", shape.YMinLabel);
    }

    [Fact]
    public void Dot_tracks_the_first_visible_tier_in_SER_order_when_it_has_a_current_month_value()
    {
        var psa = (int?[])AllNull.Clone();
        psa[11] = 1486_00;
        var grade9 = (int?[])AllNull.Clone();
        grade9[11] = 500_00;
        var prices = Prices(Tier("Psa10", psa), Tier("Grade9", grade9));

        var shape = LwcSeriesShaper.Shape(prices, new HashSet<string> { "Grade9", "Psa10" });

        Assert.Equal("Psa10", shape.DotSeriesTier);
        Assert.Equal(1486.00m, shape.DotValue);
    }

    [Fact]
    public void Dot_falls_back_to_the_next_visible_tier_when_the_first_has_no_current_month_value()
    {
        var psa = (int?[])AllNull.Clone(); // PSA 10 visible but no current-month value
        var grade9 = (int?[])AllNull.Clone();
        grade9[11] = 500_00;
        var prices = Prices(Tier("Psa10", psa), Tier("Grade9", grade9));

        var shape = LwcSeriesShaper.Shape(prices, new HashSet<string> { "Psa10", "Grade9" });

        Assert.Equal("Grade9", shape.DotSeriesTier);
        Assert.Equal(500.00m, shape.DotValue);
    }

    [Fact]
    public void Dot_is_null_when_no_visible_tier_has_a_current_month_value()
    {
        var prices = Prices(Tier("Psa10", AllNull), Tier("Grade9", AllNull));

        var shape = LwcSeriesShaper.Shape(prices, new HashSet<string> { "Psa10", "Grade9" });

        Assert.Null(shape.DotSeriesTier);
        Assert.Null(shape.DotValue);
    }

    [Fact]
    public void Empty_visible_set_throws_ArgumentException()
    {
        var prices = Prices(Tier("Psa10", AllNull));

        Assert.Throws<ArgumentException>(() => LwcSeriesShaper.Shape(prices, new HashSet<string>()));
    }

    [Fact]
    public void Series_are_emitted_in_SER_order_with_the_D_084_3_colors_and_line_widths()
    {
        var prices = Prices(
            Tier("Ungraded", AllNull), Tier("Grade7", AllNull), Tier("Grade8", AllNull),
            Tier("Grade9", AllNull), Tier("Grade9Half", AllNull), Tier("Psa10", AllNull));
        var visible = new HashSet<string> { "Ungraded", "Grade7", "Grade8", "Grade9", "Grade9Half", "Psa10" };

        var shape = LwcSeriesShaper.Shape(prices, visible);

        Assert.Equal(["Psa10", "Grade9Half", "Grade9", "Grade8", "Grade7", "Ungraded"],
            shape.Series.Select(s => s.Tier));
        Assert.Equal(["--acc", "#7A56C9", "--warn", "#4C8F8A", "#A96A4A", "--mut2"],
            shape.Series.Select(s => s.Color));
        Assert.Equal([2.0, 1.5, 1.5, 1.5, 1.5, 1.5], shape.Series.Select(s => s.LineWidth));
    }

    [Fact]
    public void X_labels_are_the_first_middle_and_last_of_the_twelve_month_window()
    {
        var prices = Prices(Tier("Psa10", AllNull));

        var shape = LwcSeriesShaper.Shape(prices, new HashSet<string> { "Psa10" });

        Assert.Equal("Sep ’25", shape.XFirst);
        Assert.Equal("Feb ’26", shape.XMiddle);
        Assert.Equal("Aug ’26", shape.XLast);
    }

    // C1 regression: lwc-interop.js's setData() builds state.monthIndex by walking each visible
    // series' main Points, then its DashedTail, calling noteTime(time) on every point in order.
    // The bug was noteTime advancing an incrementing counter even when a time was already
    // present -- and the dashed tail's FIRST point always duplicates the last closed month
    // (ShapedSeries.DashedTail's own doc comment: "[last closed, current]"), so the current
    // month (the tail's second point) landed one slot past its true index. The fix is indexing
    // by state.monthIndex.size instead, i.e. first-seen insertion order. This mirrors that exact
    // rule so a regression in lwc-interop.js's noteTime shows up here even though nothing in
    // this repo executes the .js file directly (verified empirically against node too).
    private static IReadOnlyDictionary<string, int> IndexTimesLikeLwcInterop(ChartShape shape)
    {
        var index = new Dictionary<string, int>();
        void Note(string time)
        {
            if (!index.ContainsKey(time))
            {
                index[time] = index.Count;
            }
        }

        foreach (var series in shape.Series)
        {
            foreach (var point in series.Points)
            {
                Note(point.Time);
            }

            if (series.DashedTail is not null)
            {
                foreach (var point in series.DashedTail)
                {
                    Note(point.Time);
                }
            }
        }

        return index;
    }

    [Fact]
    public void Index_rule_maps_the_current_month_to_eleven_when_a_dashed_tail_duplicates_the_last_closed_month()
    {
        var cents = (int?[])AllNull.Clone();
        cents[10] = 7000; // last closed (2026-07) -- re-noted by the dashed tail's first point
        cents[11] = 7200; // current (2026-08)
        var prices = Prices(Tier("Psa10", cents));

        var shape = LwcSeriesShaper.Shape(prices, new HashSet<string> { "Psa10" });
        Assert.NotNull(shape.Series[0].DashedTail); // fixture sanity: this run must exercise the tail

        var index = IndexTimesLikeLwcInterop(shape);

        Assert.Equal(11, index["2026-08-01"]); // the current month -- NOT 12
        Assert.Equal(12, index.Count); // 11 closed months (0..10) + the current month (11)
    }
}
