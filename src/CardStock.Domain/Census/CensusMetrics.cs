using CardStock.Domain.Signals;

namespace CardStock.Domain.Census;

/// <summary>
/// The census sentences (card.md §3.8/§3.9), computed read-time from the
/// card's own observation history behind per-metric data checks (owner ruling
/// 2026-08-13, D-093 — build now, unlock when the data qualifies).
///
/// Levels flat-fill from the FULL row history: that is the populations
/// storage contract, and the pre-floor level is real. Measurement WINDOWS are
/// valid only when they start on/after the 2026-09-01 floor — D-033 gates the
/// interval, not the level, so the July–August first-visit backfill can never
/// masquerade as market movement. All day arithmetic is UTC.
/// </summary>
public static class CensusMetrics
{
    private const int GemWindowDays = 90;
    private const int GemSubmissionFloor = 30;
    private const decimal GemFlatBandPp = 0.1m;
    private const int PaceObservationFloor = 2;
    private const int PaceTrendMonths = 6;
    private const decimal PaceSupplyBandPctPerMonth = 2m;

    public static IReadOnlyList<CensusMetric> Evaluate(
        IReadOnlyList<CensusObservation> observations, DateOnly today) =>
        [GemRate(observations, today), Pace(observations, today)];

    // -- gem rate ------------------------------------------------------------

    private static CensusMetric GemRate(IReadOnlyList<CensusObservation> observations, DateOnly today)
    {
        var windowStart = today.AddDays(-GemWindowDays);
        if (windowStart < CardCensus.ObservationFloor)
        {
            var fills = CardCensus.ObservationFloor.AddDays(GemWindowDays);
            return LowData("Gem rate",
                $"needs {GemWindowDays} days of census deltas; observations count from " +
                $"{CardCensus.ObservationFloor:yyyy-MM-dd} — the window fills {fills:yyyy-MM-dd}");
        }

        var deltaAll = PsaAllDelta(observations, windowStart, today);
        if (deltaAll < GemSubmissionFloor)
        {
            return LowData("Gem rate",
                $"fewer than {GemSubmissionFloor} PSA slabs graded in the last {GemWindowDays} days · " +
                $"{Math.Max(0, deltaAll)} of {GemSubmissionFloor} — rate withheld");
        }

        var rate = PsaTenDelta(observations, windowStart, today) / (decimal)deltaAll * 100;
        var definition = "of the last 90 days of PSA submissions, the share that came back 10";

        // Drift needs the prior 90-day window, wholly post-floor and above the
        // same submission floor; below either, the drift clause is omitted
        // entirely (§3.8's gate — never a fabricated comparison).
        var priorStart = windowStart.AddDays(-GemWindowDays);
        var priorAll = priorStart >= CardCensus.ObservationFloor
            ? PsaAllDelta(observations, priorStart, windowStart)
            : 0;
        if (priorStart < CardCensus.ObservationFloor || priorAll < GemSubmissionFloor)
        {
            return new CensusMetric("Gem rate", MetricState.Ok, $"{rate:0.0}%",
                [new MetricSegment(definition, ChipTone.Neutral)]);
        }

        // The band acts on the ROUNDED drift, so the branch always agrees with
        // the number the user is shown (−0.14 displays −0.1 and reads steady).
        var priorRate = PsaTenDelta(observations, priorStart, windowStart) / (decimal)priorAll * 100;
        var drift = Math.Round(rate - priorRate, 1, MidpointRounding.AwayFromZero);
        var driftText = $"{SignedOneDecimal(drift)}pp / 90d";

        if (Math.Abs(drift) <= GemFlatBandPp)
        {
            return new CensusMetric("Gem rate", MetricState.Ok, $"{rate:0.0}%",
            [
                new MetricSegment($"{definition} · drifting ", ChipTone.Neutral),
                new MetricSegment(driftText, ChipTone.Neutral),
                new MetricSegment(" steady", ChipTone.Neutral),
            ]);
        }

        // A falling gem rate is bullish for holders of existing 10s (§3.8).
        var falling = drift < 0;
        return new CensusMetric("Gem rate", MetricState.Ok, $"{rate:0.0}%",
        [
            new MetricSegment($"{definition} · drifting ", ChipTone.Neutral),
            new MetricSegment(driftText, falling ? ChipTone.Pos : ChipTone.Neg),
            new MetricSegment(
                falling
                    ? " (harder to gem = supply of fresh 10s slowing)"
                    : " (easier to gem = fresh 10s arriving faster)",
                ChipTone.Neutral),
        ]);
    }

    // -- pace ----------------------------------------------------------------

    private static CensusMetric Pace(IReadOnlyList<CensusObservation> observations, DateOnly today)
    {
        var qualifying = observations
            .Select(o => o.ObservedAt)
            .Distinct()
            .Count(at => UtcDate(at) >= CardCensus.ObservationFloor);
        if (qualifying < PaceObservationFloor)
        {
            return LowData("Pace",
                $"needs census deltas; observations count from {CardCensus.ObservationFloor:yyyy-MM-dd}, " +
                $"{qualifying} so far — deltas need two");
        }

        // Closed calendar months from the floor: month M's delta is
        // count(first of M+1) − count(first of M), flat-filled. The current
        // month is still revising and never renders (rule 1).
        var firstOfCurrent = new DateOnly(today.Year, today.Month, 1);
        var months = new List<(DateOnly First, int Delta)>();
        for (var first = CardCensus.ObservationFloor; first.AddMonths(1) <= firstOfCurrent; first = first.AddMonths(1))
        {
            var delta = PsaTenAt(observations, first.AddMonths(1)) - PsaTenAt(observations, first);
            months.Add((first, delta));
        }

        if (months.Count == 0)
        {
            var firstClose = CardCensus.ObservationFloor.AddMonths(1);
            return LowData("Pace",
                $"first monthly delta closes {firstClose:yyyy-MM-dd} — {qualifying} observations so far");
        }

        var latest = months[^1].Delta;
        var sum = months.Sum(m => m.Delta);
        var segments = new List<MetricSegment>();

        // The trend word needs two full 3-month windows; below that it is
        // undefined and omitted, not guessed.
        if (months.Count >= PaceTrendMonths)
        {
            var recent = months.TakeLast(3).Sum(m => m.Delta);
            var prior = months.Skip(months.Count - 6).Take(3).Sum(m => m.Delta);
            var word = recent > prior ? "rising" : recent < prior ? "slowing" : "steady";
            segments.Add(new MetricSegment($"and {word} — ", ChipTone.Neutral));
        }
        else
        {
            segments.Add(new MetricSegment("— ", ChipTone.Neutral));
        }

        segments.Add(new MetricSegment(
            $"{sum} new 10s since {MonthLabel(CardCensus.ObservationFloor)}", ChipTone.Neutral));

        // Growth is a share of the census at the window start; a zero start has
        // no honest percentage, so the clause is omitted.
        var startCount = PsaTenAt(observations, CardCensus.ObservationFloor);
        if (startCount > 0)
        {
            var pct = Math.Round(sum / (decimal)startCount * 100, MidpointRounding.AwayFromZero);
            var supplyPressure = pct / months.Count > PaceSupplyBandPctPerMonth;
            segments.Add(new MetricSegment(", growing the census ", ChipTone.Neutral));
            segments.Add(new MetricSegment(
                $"{SignedWhole(pct)}%",
                supplyPressure ? ChipTone.Neg : ChipTone.Pos));
            segments.Add(new MetricSegment($" in {months.Count} months ", ChipTone.Neutral));
            segments.Add(new MetricSegment(
                supplyPressure
                    ? "(fresh supply working against the price)"
                    : "(supply nearly frozen — scarcity intact)",
                ChipTone.Neutral));
        }

        return new CensusMetric("Pace", MetricState.Ok, $"{SignedWhole(latest)} / mo", segments);
    }

    // -- window plumbing -----------------------------------------------------

    private static CensusMetric LowData(string name, string note) =>
        new(name, MetricState.LowData, null, [new MetricSegment(note, ChipTone.Neutral)]);

    private static DateOnly UtcDate(DateTimeOffset at) => DateOnly.FromDateTime(at.UtcDateTime);

    /// <summary>Flat-fill: the cell's newest row dated on or before <paramref name="date"/>;
    /// a cell with no row yet was zero at every observation (the storage contract).</summary>
    private static int CountAt(IEnumerable<CensusObservation> cellRows, DateOnly date) =>
        cellRows
            .Where(r => UtcDate(r.ObservedAt) <= date)
            .OrderByDescending(r => r.ObservedAt)
            .Select(r => r.Population)
            .FirstOrDefault();

    private static int PsaTenAt(IReadOnlyList<CensusObservation> observations, DateOnly date) =>
        CountAt(observations.Where(o => o.Grader == "psa" && o.Grade == 10), date);

    private static int PsaTenDelta(IReadOnlyList<CensusObservation> observations, DateOnly from, DateOnly to) =>
        PsaTenAt(observations, to) - PsaTenAt(observations, from);

    /// <summary>New PSA slabs across every grade over (from, to] — the §3.8
    /// operationalization of "submissions": census growth as published.</summary>
    private static int PsaAllDelta(IReadOnlyList<CensusObservation> observations, DateOnly from, DateOnly to) =>
        observations
            .Where(o => o.Grader == "psa")
            .GroupBy(o => o.Grade)
            .Sum(cell => CountAt(cell, to) - CountAt(cell, from));

    private static string SignedOneDecimal(decimal value) =>
        value > 0 ? $"+{value:0.0}" : value < 0 ? $"−{Math.Abs(value):0.0}" : "0.0";

    private static string SignedWhole(decimal value) =>
        value > 0 ? $"+{value:0}" : value < 0 ? $"−{Math.Abs(value):0}" : "0";

    private static string MonthLabel(DateOnly month) => $"{month:MMM} ’{month:yy}";
}
