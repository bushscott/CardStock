namespace CardStock.Web.Services;

/// <summary>
/// The card page's number/date-to-copy rules, in one place so no component
/// re-derives them. card.md §3: money is '$' + round, en-US grouping, no cents.
/// Negative percents use U+2212 (true minus), never a hyphen. Month labels use
/// U+2019 (typographic apostrophe): "Sep '26".
/// </summary>
public static class Format
{
    /// <summary>money(): '$' + round, en-US grouping, no cents (card.md §3).</summary>
    public static string Money(int cents) =>
        "$" + Math.Round(cents / 100m, MidpointRounding.AwayFromZero).ToString("N0",
            System.Globalization.CultureInfo.GetCultureInfo("en-US"));

    /// <summary>Signed percent, one decimal, U+2212 for negatives: +6.2% / −0.2%.</summary>
    public static string ChangePercent(decimal fraction)
    {
        var value = Math.Round(fraction * 100, 1, MidpointRounding.AwayFromZero);
        return value < 0 ? $"−{Math.Abs(value):0.0}%" : $"+{value:0.0}%";
    }

    /// <summary>"2026-08" or "2026-08-01" → "Aug ’26" (U+2019), for month labels.</summary>
    public static string MonthLabel(string month)
    {
        var date = DateOnly.Parse(month.Length == 7 ? month + "-01" : month);
        var en = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        return date.ToString("MMM", en) + " ’" + date.ToString("yy", en);
    }

    /// <summary>Header stat tiles abbreviate at ≥$10K (D-110 spec §4): one
    /// decimal, trailing zero dropped. Roster cells always use Money.</summary>
    public static string AbbrevMoney(long cents)
    {
        var dollars = cents / 100m;
        return dollars switch
        {
            >= 1_000_000 => "$" + (dollars / 1_000_000).ToString("0.#",
                System.Globalization.CultureInfo.GetCultureInfo("en-US")) + "M",
            >= 10_000 => "$" + (dollars / 1_000).ToString("0.#",
                System.Globalization.CultureInfo.GetCultureInfo("en-US")) + "K",
            _ => Money((int)cents),
        };
    }

    /// <summary>"2021-12" → "Dec 2021" — the Set header’s first-sale line.</summary>
    public static string MonthYear(string month)
    {
        var date = DateOnly.Parse(month.Length == 7 ? month + "-01" : month,
            System.Globalization.CultureInfo.InvariantCulture);
        return date.ToString("MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
    }
}
