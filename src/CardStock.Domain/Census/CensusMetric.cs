using CardStock.Domain.Signals;

namespace CardStock.Domain.Census;

public enum MetricState
{
    Ok,
    LowData,
}

/// <summary>One toned run of sentence text. Neutral renders in the note's own
/// muted colour; Pos/Neg tone the market-meaning token (a falling gem rate is
/// green, census growth is red — coloured by meaning, never by arithmetic
/// sign). Mono marks the runs the mockup sets in the mono face: the value and
/// every number token — including the – glyph standing where a value will be.</summary>
public sealed record MetricSegment(string Text, ChipTone Tone, bool Mono = false);

/// <summary>
/// One census sentence (card.md §3.8/§3.9, D-093 gates, D-102 form): Gem rate
/// or Pace, ALWAYS printed. The skeleton is permanent copy; below a gate the
/// value runs hold the – glyph (never a number — the five-state doctrine) and
/// GateNote carries the ◌ tooltip naming the rule that failed and when it
/// passes. Ok fills the runs and appends whichever clauses their own gates
/// allow; GateNote is null.
/// </summary>
public sealed record CensusMetric(
    MetricState State, IReadOnlyList<MetricSegment> Segments, string? GateNote = null);

/// <summary>
/// One slot of the ghost delta chart (D-094): a month that either materialized
/// (Observed, with its delta) or still ghosts — dashed outline, no number, a
/// tooltip naming its unlock. The current month is always the trailing ghost:
/// the outlined partial month the prototype's border plumbing anticipated (OQ-10).
/// </summary>
public sealed record CensusDeltaBar(
    DateOnly Month, string Label, bool Observed, int? Delta, string Tooltip);
