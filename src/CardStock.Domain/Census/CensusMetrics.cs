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

    private const int DeltaChartSlots = 7;

    /// <summary>The mockup's own sentence body (:232), permanent copy — the
    /// skeleton prints with the – glyph in the value run until the gate passes
    /// (D-102).</summary>
    private static readonly MetricSegment GemDefinition = new(
        " — of the last 90 days of PSA submissions, the share that came back 10.", ChipTone.Neutral);

    /// <summary>
    /// The ghost delta chart (D-094): seven month slots ending at the current
    /// month, never starting before the floor's month. A slot materializes as
    /// an observed bar once its month has closed AND the pace gate holds
    /// (≥ 2 qualifying observations — a closed month on an unobserved card is
    /// not a fabricated zero); otherwise it ghosts with a tooltip naming its
    /// unlock. The current month is always the trailing ghost.
    /// </summary>
    public static IReadOnlyList<CensusDeltaBar> DeltaBars(
        IReadOnlyList<CensusObservation> observations, DateOnly today)
    {
        var firstOfCurrent = new DateOnly(today.Year, today.Month, 1);
        var floorMonth = new DateOnly(
            CardCensus.ObservationFloor.Year, CardCensus.ObservationFloor.Month, 1);
        var sliding = firstOfCurrent.AddMonths(-(DeltaChartSlots - 1));
        var start = sliding > floorMonth ? sliding : floorMonth;

        var qualifying = QualifyingCount(observations);
        var bars = new List<CensusDeltaBar>(DeltaChartSlots);
        for (var i = 0; i < DeltaChartSlots; i++)
        {
            var month = start.AddMonths(i);
            var closes = month.AddMonths(1);
            if (closes > firstOfCurrent)
            {
                bars.Add(new CensusDeltaBar(month, MonthLabel(month), false, null,
                    $"new PSA 10 slabs for {MonthLabel(month)} — closes {Dates.Full(closes)}"));
            }
            else if (qualifying < PaceObservationFloor)
            {
                bars.Add(new CensusDeltaBar(month, MonthLabel(month), false, null,
                    $"new PSA 10 slabs for {MonthLabel(month)} — needs census deltas; observations " +
                    $"count from {Dates.Full(CardCensus.ObservationFloor)}, {qualifying} so far"));
            }
            else
            {
                var delta = PsaTenAt(observations, closes) - PsaTenAt(observations, month);
                bars.Add(new CensusDeltaBar(month, MonthLabel(month), true, delta,
                    $"{DeltaText(delta)} new PSA 10 slabs in {MonthLabel(month)}"));
            }
        }

        return bars;
    }

    // -- gem rate ------------------------------------------------------------

    /// <summary>The population panel's sentence (card.md §3.8, mockup :232),
    /// always printed. Below a gate the value run is the – glyph and GateNote
    /// carries the ◌ tooltip (D-102).</summary>
    public static CensusMetric GemRate(IReadOnlyList<CensusObservation> observations, DateOnly today)
    {
        var windowStart = today.AddDays(-GemWindowDays);
        if (windowStart < CardCensus.ObservationFloor)
        {
            var fills = CardCensus.ObservationFloor.AddDays(GemWindowDays);
            return GemSkeleton(
                $"needs {GemWindowDays} days of census deltas; observations count from " +
                $"{Dates.Full(CardCensus.ObservationFloor)} — the window fills {Dates.Full(fills)}");
        }

        var deltaAll = PsaAllDelta(observations, windowStart, today);
        if (deltaAll < GemSubmissionFloor)
        {
            return GemSkeleton(
                $"fewer than {GemSubmissionFloor} PSA slabs graded in the last {GemWindowDays} days · " +
                $"{Math.Max(0, deltaAll)} of {GemSubmissionFloor} — rate withheld");
        }

        var rate = PsaTenDelta(observations, windowStart, today) / (decimal)deltaAll * 100;
        var rateRun = new MetricSegment($"{rate:0.0}%", ChipTone.Neutral, Mono: true);

        // Drift needs the prior 90-day window, wholly post-floor and above the
        // same submission floor; below either, the drift clause is omitted
        // entirely (§3.8's gate — never a fabricated comparison, and never a
        // dashed one: the clause's words are themselves the data).
        var priorStart = windowStart.AddDays(-GemWindowDays);
        var priorAll = priorStart >= CardCensus.ObservationFloor
            ? PsaAllDelta(observations, priorStart, windowStart)
            : 0;
        if (priorStart < CardCensus.ObservationFloor || priorAll < GemSubmissionFloor)
        {
            return new CensusMetric(MetricState.Ok, [rateRun, GemDefinition]);
        }

        // The band acts on the ROUNDED drift, so the branch always agrees with
        // the number the user is shown (−0.14 displays −0.1 and reads steady).
        var priorRate = PsaTenDelta(observations, priorStart, windowStart) / (decimal)priorAll * 100;
        var drift = Math.Round(rate - priorRate, 1, MidpointRounding.AwayFromZero);
        var driftRun = $"{SignedOneDecimal(drift)}pp / 90d";

        if (Math.Abs(drift) <= GemFlatBandPp)
        {
            return new CensusMetric(MetricState.Ok,
            [
                rateRun, GemDefinition,
                new MetricSegment(" Drifting ", ChipTone.Neutral),
                new MetricSegment(driftRun, ChipTone.Neutral, Mono: true),
                new MetricSegment(" steady.", ChipTone.Neutral),
            ]);
        }

        // A falling gem rate is bullish for holders of existing 10s (§3.8).
        var falling = drift < 0;
        return new CensusMetric(MetricState.Ok,
        [
            rateRun, GemDefinition,
            new MetricSegment(" Drifting ", ChipTone.Neutral),
            new MetricSegment(driftRun, falling ? ChipTone.Pos : ChipTone.Neg, Mono: true),
            new MetricSegment(
                falling
                    ? " (harder to gem = supply of fresh 10s slowing)."
                    : " (easier to gem = fresh 10s arriving faster).",
                ChipTone.Neutral),
        ]);
    }

    private static CensusMetric GemSkeleton(string gateNote) =>
        new(MetricState.LowData,
            [new MetricSegment(ChipEngine.GlyphDash, ChipTone.Neutral, Mono: true), GemDefinition],
            gateNote);

    // -- pace ----------------------------------------------------------------

    /// <summary>The grading-activity panel's sentence (card.md §3.9, mockup
    /// :248), always printed — same D-102 form as the gem rate.</summary>
    public static CensusMetric Pace(IReadOnlyList<CensusObservation> observations, DateOnly today)
    {
        var qualifying = QualifyingCount(observations);
        if (qualifying < PaceObservationFloor)
        {
            return PaceSkeleton(
                $"needs census deltas; observations count from {Dates.Full(CardCensus.ObservationFloor)}, " +
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
            return PaceSkeleton(
                $"first monthly delta closes {Dates.Full(firstClose)} — {qualifying} observations so far");
        }

        var latest = months[^1].Delta;
        var sum = months.Sum(m => m.Delta);
        var segments = new List<MetricSegment>
        {
            new($"{SignedWhole(latest)} / mo", ChipTone.Neutral, Mono: true),
        };

        // The trend word needs two full 3-month windows; below that it is
        // undefined and omitted, not guessed.
        if (months.Count >= PaceTrendMonths)
        {
            var recent = months.TakeLast(3).Sum(m => m.Delta);
            var prior = months.Skip(months.Count - 6).Take(3).Sum(m => m.Delta);
            var word = recent > prior ? "rising" : recent < prior ? "slowing" : "steady";
            segments.Add(new MetricSegment($" and {word} — ", ChipTone.Neutral));
        }
        else
        {
            segments.Add(new MetricSegment(" — ", ChipTone.Neutral));
        }

        segments.Add(new MetricSegment($"{sum}", ChipTone.Neutral, Mono: true));

        // Growth is a share of the census at the window start; a zero start has
        // no honest percentage, so the clause is omitted and the sentence
        // closes on the count.
        var startCount = PsaTenAt(observations, CardCensus.ObservationFloor);
        if (startCount > 0)
        {
            var pct = Math.Round(sum / (decimal)startCount * 100, MidpointRounding.AwayFromZero);
            var supplyPressure = pct / months.Count > PaceSupplyBandPctPerMonth;
            segments.Add(new MetricSegment($" new 10s since {PaceSince}", ChipTone.Neutral));
            segments.Add(new MetricSegment(", growing the census ", ChipTone.Neutral));
            segments.Add(new MetricSegment(
                $"{SignedWhole(pct)}%",
                supplyPressure ? ChipTone.Neg : ChipTone.Pos, Mono: true));
            segments.Add(new MetricSegment($" in {months.Count} months ", ChipTone.Neutral));
            segments.Add(new MetricSegment(
                supplyPressure
                    ? "(fresh supply working against the price)."
                    : "(supply nearly frozen — scarcity intact).",
                ChipTone.Neutral));
        }
        else
        {
            segments.Add(new MetricSegment($" new 10s since {PaceSince}.", ChipTone.Neutral));
        }

        return new CensusMetric(MetricState.Ok, segments);
    }

    /// <summary>`Sep ’26` — the floor month every pace window opens at.</summary>
    private static readonly string PaceSince = MonthLabel(CardCensus.ObservationFloor);

    private static CensusMetric PaceSkeleton(string gateNote) =>
        new(MetricState.LowData,
        [
            new MetricSegment($"{ChipEngine.GlyphDash} / mo", ChipTone.Neutral, Mono: true),
            new MetricSegment(" — ", ChipTone.Neutral),
            new MetricSegment(ChipEngine.GlyphDash, ChipTone.Neutral, Mono: true),
            new MetricSegment($" new 10s since {PaceSince}.", ChipTone.Neutral),
        ], gateNote);

    // -- window plumbing -----------------------------------------------------

    private static int QualifyingCount(IReadOnlyList<CensusObservation> observations) =>
        observations
            .Select(o => o.ObservedAt)
            .Distinct()
            .Count(at => UtcDate(at) >= CardCensus.ObservationFloor);

    /// <summary>The prototype's `'+' + n` value form with the true minus: `+42` / `+0` / `−4`.</summary>
    private static string DeltaText(int delta) =>
        delta >= 0 ? $"+{delta}" : $"−{Math.Abs(delta)}";

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

    /// <summary>`Sep ’26` — explicit en-US like every other MMM site (Format.cs,
    /// PriceChart): the repo deliberately does NOT set InvariantGlobalization
    /// (Directory.Build.props, D-070), and the Pi's own en ICU culture renders
    /// September as "Sept" under a bare format string — caught live, 2026-08-14.</summary>
    private static string MonthLabel(DateOnly month)
    {
        var en = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        return month.ToString("MMM", en) + " ’" + month.ToString("yy", en);
    }
}
