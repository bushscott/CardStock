using System.Text.RegularExpressions;

namespace CardStock.Domain.Cards;

/// <summary>
/// The site embeds the collector number in the card name ("Umbreon VMAX #215").
/// This is the stopgap until the sibling repo's enrichment lands (D-079): parse
/// a TRAILING #token off defensively. A name that doesn't match renders
/// untouched — a failed parse can never invent a number.
/// </summary>
public sealed partial record CardTitle(string Title, string? CollectorNumber)
{
    [GeneratedRegex(@"^(?<title>.+?)\s+#(?<num>[A-Za-z0-9][A-Za-z0-9.-]*)$")]
    private static partial Regex TrailingNumber();

    public static CardTitle Parse(string rawName)
    {
        var trimmed = rawName.Trim();
        var match = TrailingNumber().Match(trimmed);

        return match.Success
            ? new CardTitle(match.Groups["title"].Value, match.Groups["num"].Value)
            : new CardTitle(trimmed, null);
    }
}
