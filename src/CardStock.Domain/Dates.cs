using System.Globalization;

namespace CardStock.Domain;

/// <summary>
/// Full calendar dates render MM-DD-YYYY app-wide (owner ruling 2026-08-14,
/// D-095; separator corrected to dashes same day) — display only; wire
/// payloads, chart time keys, and sort keys stay ISO. Explicit en-US per the
/// deliberate no-InvariantGlobalization stance (Directory.Build.props, D-070).
/// The DateTimeOffset overload formats the instant's own offset components,
/// exactly as the yyyy-MM-dd stamps it replaced did — a format change, never
/// a day-arithmetic change.
/// </summary>
public static class Dates
{
    private static readonly CultureInfo EnUs = CultureInfo.GetCultureInfo("en-US");

    public static string Full(DateOnly date) => date.ToString("MM-dd-yyyy", EnUs);

    public static string Full(DateTimeOffset instant) => instant.ToString("MM-dd-yyyy", EnUs);
}
