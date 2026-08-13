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
    int QualifyingObservations)
{
    /// <summary>D-033: no post-seam metric counts observations before this date.</summary>
    public static readonly DateOnly ObservationFloor = new(2026, 9, 1);

    private static readonly (string Grader, short Grade)[] BarSlots =
        [("psa", 8), ("psa", 9), ("psa", 10), ("cgc", 8), ("cgc", 9), ("cgc", 10)];

    public static CardCensus From(
        IReadOnlyList<CensusObservation> latestCells,
        IReadOnlyList<DateTimeOffset> observationInstants)
    {
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
            observationInstants.Count(at =>
                DateOnly.FromDateTime(at.UtcDateTime) >= ObservationFloor));
    }
}
