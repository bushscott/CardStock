namespace CardStock.Domain.Signals;

public enum ChipTone
{
    Pos,
    Neg,
    Caution,
    Neutral,
}

/// <summary>
/// One firing chip: `icon + short name + evidence number`, tooltip = one
/// sentence of evidence with window and threshold (signals.md chip grammar).
/// </summary>
public sealed record SignalChip(string Glyph, string Text, string Tooltip, ChipTone Tone);
