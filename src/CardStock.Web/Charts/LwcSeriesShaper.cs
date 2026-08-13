using CardStock.Application.Cards;
using CardStock.Web.Services;

namespace CardStock.Web.Charts;

/// <summary>One LWC-ready series bundle for one visible tier.</summary>
public sealed record ShapedSeries(
    string Tier,
    string Color,
    double LineWidth,                        // 2.0 for PSA 10, 1.5 otherwise (card.md R-12)
    IReadOnlyList<ShapedPoint> Points,       // main line: value or whitespace, current month EXCLUDED
    IReadOnlyList<ShapedPoint>? DashedTail,  // [last closed, current] when both present, else null
    IReadOnlyList<ShapedPoint> IsolatedPoints); // present points whose neighbours are both absent

public sealed record ShapedPoint(string Time, decimal? Value); // Time "yyyy-MM-01"; null Value = whitespace

public sealed record ChartShape(
    IReadOnlyList<ShapedSeries> Series,
    string YMaxLabel, string YMinLabel,      // "$30,100" / "$247" over visible present values incl. current month
    string XFirst, string XMiddle, string XLast, // "Sep '25" · "Feb '26" · "Aug '26"
    string? DotSeriesTier, decimal? DotValue);    // first visible tier WITH a current-month value

/// <summary>
/// Pure C# shaper: turns the wire's 12-slot-windowed <see cref="PricesDto"/> into the shape
/// lwc-interop.js needs. card.md §2.4/§2.4.1/§2.4.2. No browser/JS dependency -- unit-testable
/// directly.
/// </summary>
public static class LwcSeriesShaper
{
    /// <summary>
    /// "SER order" -- verified against the frozen prototype (Cardstock Card.dc.html:327, 405, 417),
    /// not the wire DTO's ascending strip order (Raw..PSA10). PSA 10 first, Raw (wire key
    /// "Ungraded") last. Drives legend/series z-order and the hollow-dot fallback chain. Exposed
    /// so PriceChart.razor can render its legend and crosshair tooltip in the same order.
    /// </summary>
    public static readonly IReadOnlyList<string> SerOrder =
        ["Psa10", "Grade9Half", "Grade9", "Grade8", "Grade7", "Ungraded"];

    /// <summary>D-084.3 / brand.md §2.6 TIER_COLORS, per the task-17 brief verbatim. The three
    /// theme-derived entries carry token names ("--acc"); lwc-interop.js resolves those via
    /// getComputedStyle. The other three are fixed hexes, same in both themes.</summary>
    private static readonly IReadOnlyDictionary<string, string> Colors = new Dictionary<string, string>
    {
        ["Psa10"] = "--acc",
        ["Grade9Half"] = "#7A56C9",
        ["Grade9"] = "--warn",
        ["Grade8"] = "#4C8F8A",
        ["Grade7"] = "#A96A4A",
        ["Ungraded"] = "--mut2",
    };

    public static ChartShape Shape(PricesDto prices, IReadOnlySet<string> visibleTiers)
    {
        if (visibleTiers.Count == 0)
        {
            // The component's ≥1-visible guard is what's supposed to prevent this from ever
            // happening at runtime; this is the shaper's own defense if that guard is bypassed.
            throw new ArgumentException("At least one tier must be visible.", nameof(visibleTiers));
        }

        var byTier = prices.Tiers.ToDictionary(t => t.Tier);
        var series = new List<ShapedSeries>();
        var presentCents = new List<int>();
        string? dotTier = null;
        decimal? dotValue = null;

        foreach (var key in SerOrder)
        {
            if (!visibleTiers.Contains(key) || !byTier.TryGetValue(key, out var tier))
            {
                continue;
            }

            var window = tier.Points; // 12-slot window: index 0..10 closed months, 11 = current
            var closed = new List<ShapedPoint>(11);
            for (var k = 0; k < 11; k++)
            {
                var cents = window[k].Cents;
                closed.Add(new ShapedPoint(ToTime(window[k].Month), ToDollars(cents)));
                if (cents is not null)
                {
                    presentCents.Add(cents.Value);
                }
            }

            var currentCents = window[11].Cents;
            if (currentCents is not null)
            {
                presentCents.Add(currentCents.Value);
            }

            IReadOnlyList<ShapedPoint>? tail = null;
            if (closed[10].Value is not null && currentCents is not null)
            {
                tail = new[] { closed[10], new ShapedPoint(ToTime(window[11].Month), ToDollars(currentCents)) };
            }

            var isolated = new List<ShapedPoint>();
            for (var k = 0; k < 11; k++)
            {
                if (closed[k].Value is null)
                {
                    continue;
                }

                var leftPresent = k > 0 && closed[k - 1].Value is not null;
                // Index 10's "right" neighbour is the dashed tail, when one connects it onward --
                // otherwise (like every other index) there is none, by construction of the window.
                var rightPresent = k < 10 ? closed[k + 1].Value is not null : tail is not null;
                if (!leftPresent && !rightPresent)
                {
                    isolated.Add(closed[k]);
                }
            }

            series.Add(new ShapedSeries(key, Colors[key], key == "Psa10" ? 2.0 : 1.5, closed, tail, isolated));

            if (dotTier is null && currentCents is not null)
            {
                dotTier = key;
                dotValue = ToDollars(currentCents);
            }
        }

        string yMax, yMin;
        if (presentCents.Count == 0)
        {
            yMax = yMin = "—";
        }
        else
        {
            yMax = Format.Money(presentCents.Max());
            yMin = Format.Money(presentCents.Min());
        }

        // Derived from CurrentMonth by offset rather than indexed off any one tier's Points --
        // every tier shares the same 12-month window by construction (task-10's CardPageMapper
        // windows every tier from the same CurrentMonth), but Tiers itself can be empty in a
        // minimal test fixture, and this reads the same either way with no such dependency.
        var current = DateOnly.Parse(prices.CurrentMonth + "-01");

        return new ChartShape(
            series, yMax, yMin,
            Format.MonthLabel(current.AddMonths(-11).ToString("yyyy-MM-dd")),
            Format.MonthLabel(current.AddMonths(-6).ToString("yyyy-MM-dd")),
            Format.MonthLabel(prices.CurrentMonth),
            dotTier, dotValue);
    }

    private static decimal? ToDollars(int? cents) => cents is null ? null : cents.Value / 100m;

    private static string ToTime(string month) => month + "-01"; // "yyyy-MM" -> "yyyy-MM-01"
}
