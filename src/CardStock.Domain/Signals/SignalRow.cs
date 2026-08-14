namespace CardStock.Domain.Signals;

/// <summary>
/// The five signal-row states (card.md §2.3.2). Every evaluated signal renders in
/// exactly one. The engine emits Firing/Quiet/BelowFloor for the price-computable
/// signals; the composition layer adds Neutral (liquidity — never directional) and
/// Locked (substrate missing product-wide) rows.
/// </summary>
public enum SignalState
{
    Firing,
    Quiet,
    BelowFloor,
    Neutral,
    Locked,
}

/// <summary>
/// One signals-panel row: glyph + name + value + one-sentence tooltip (card.md
/// §2.3.2). The glyph is text in the row's foreground — colour never carries the
/// state alone: ▲/▼ toned firing, – (U+2013) caution firing / quiet / below-floor,
/// ● neutral, ◌ locked.
/// </summary>
public sealed record SignalRow(
    string Glyph, string Name, string Value, string Tooltip, SignalState State, ChipTone Tone);
