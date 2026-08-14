using CardStock.Domain.Signals;

namespace CardStock.Domain.Census;

public enum MetricState
{
    Ok,
    LowData,
}

/// <summary>One toned run of sentence text. Neutral renders in the note's own
/// muted colour; Pos/Neg tone the market-meaning token (a falling gem rate is
/// green, census growth is red — coloured by meaning, never by arithmetic sign).</summary>
public sealed record MetricSegment(string Text, ChipTone Tone);

/// <summary>
/// One census-metric slot (card.md §2.6, D-087/D-093): Gem rate or Pace. Ok
/// carries the headline Value plus the sentence as segments; LowData carries a
/// single segment naming the rule that failed and when it will pass — never a
/// number (the five-state doctrine).
/// </summary>
public sealed record CensusMetric(
    string Name, MetricState State, string? Value, IReadOnlyList<MetricSegment> Segments);

/// <summary>
/// One slot of the ghost delta chart (D-094): a month that either materialized
/// (Observed, with its delta) or still ghosts — dashed outline, no number, a
/// tooltip naming its unlock. The current month is always the trailing ghost:
/// the outlined partial month the prototype's border plumbing anticipated (OQ-10).
/// </summary>
public sealed record CensusDeltaBar(
    DateOnly Month, string Label, bool Observed, int? Delta, string Tooltip);
