namespace CardStock.Domain.Census;

/// <summary>One PSA-10 census cell value at one observation. ObservedOn is the
/// UTC date of populations.observed_at.</summary>
public sealed record PopulationObservation(DateOnly ObservedOn, int Count);

public enum PopulationDeltaState
{
    /// <summary>Both endpoints resolve; Fraction is the 60-day growth.</summary>
    Available,

    /// <summary>First observation younger than the window; dates say when it passes.</summary>
    Pending,

    /// <summary>No PSA 10 population observed, or a zero base — no ratio exists.</summary>
    None,
}

/// <summary>
/// Pop Δ 60d (set.md §3.4 col 5, spec §3.2). Change-only semantics: the census
/// value as of a date is the latest stored row at or before it — flat between
/// rows is the populations contract (which does NOT transfer to price_months).
/// D-033: observations before <see cref="CardCensus.ObservationFloor"/>
/// (2026-09-01) never participate — filtered out before first/pending/
/// available/zero-base resolve, the same read-time floor CensusMetrics
/// applies to the Card page's census metrics (D-093).
/// </summary>
public static class PopulationDelta
{
    public const int WindowDays = 60;

    public sealed record Result(
        PopulationDeltaState State, decimal? Fraction,
        DateOnly? FirstObservedOn, DateOnly? DeltasBeginOn);

    public static Result Evaluate(IReadOnlyList<PopulationObservation> psa10, DateOnly today)
    {
        var eligible = psa10.Where(o => o.ObservedOn >= CardCensus.ObservationFloor).ToList();
        if (eligible.Count == 0)
        {
            return new Result(PopulationDeltaState.None, null, null, null);
        }

        var ordered = eligible.OrderBy(o => o.ObservedOn).ToList();
        var first = ordered[0].ObservedOn;
        var windowStart = today.AddDays(-WindowDays);

        if (first > windowStart)
        {
            return new Result(
                PopulationDeltaState.Pending, null, first, first.AddDays(WindowDays));
        }

        var then = ordered.Last(o => o.ObservedOn <= windowStart).Count;
        var now = ordered[^1].Count;

        return then == 0
            ? new Result(PopulationDeltaState.None, null, first, null)
            : new Result(PopulationDeltaState.Available, (now - then) / (decimal)then, first, null);
    }
}
