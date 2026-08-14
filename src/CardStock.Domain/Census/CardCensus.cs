namespace CardStock.Domain.Census;

public sealed record CensusCell(string Grader, short Grade, int Count);

/// <summary>
/// The census pair's data. Bars are ALWAYS the fixed six (D-084.4) — census
/// grades are integers 1..10, so a 9.5 cell cannot exist (D-083). A cell with
/// no row was zero at every observation (the storage contract), so absence
/// renders as a true-zero stub, never as missing data.
/// </summary>
public sealed record CardCensus(
    IReadOnlyList<CensusCell> Bars,
    int PsaTotal,
    int CgcTotal,
    DateTimeOffset? ObservedAt,
    int QualifyingObservations,
    IReadOnlyList<CensusObservation> Observations)
{
    /// <summary>D-033: no post-seam metric counts observations before this date.</summary>
    public static readonly DateOnly ObservationFloor = new(2026, 9, 1);

    private static readonly (string Grader, short Grade)[] BarSlots =
        [("psa", 8), ("psa", 9), ("psa", 10), ("cgc", 8), ("cgc", 9), ("cgc", 10)];

    /// <summary>Builds from the card's FULL populations history (change-only rows).
    /// Latest-per-cell and observation instants derive here; the raw rows are kept
    /// on the record so the composition layer can evaluate the census metrics
    /// (CensusMetrics) with the request clock — the same read-time pattern as the
    /// price signals (D-093).</summary>
    public static CardCensus From(IReadOnlyList<CensusObservation> observations)
    {
        var latestCells = observations
            .GroupBy(o => (o.Grader, o.Grade))
            .Select(g => g.OrderByDescending(o => o.ObservedAt).First())
            .ToList();
        var byCell = latestCells.ToDictionary(c => (c.Grader, c.Grade), c => c.Population);

        var bars = BarSlots
            .Select(slot => new CensusCell(
                slot.Grader, slot.Grade, byCell.GetValueOrDefault((slot.Grader, slot.Grade))))
            .ToList();

        return new CardCensus(
            bars,
            latestCells.Where(c => c.Grader == "psa").Sum(c => c.Population),
            latestCells.Where(c => c.Grader == "cgc").Sum(c => c.Population),
            latestCells.Count == 0 ? null : latestCells.Max(c => c.ObservedAt),
            observations
                .Select(o => o.ObservedAt)
                .Distinct()
                .Count(at => DateOnly.FromDateTime(at.UtcDateTime) >= ObservationFloor),
            observations);
    }
}
