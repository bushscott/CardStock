using CardStock.Domain.Census;

namespace CardStock.Domain.Tests.Census;

public class CardCensusTests
{
    private static readonly DateTimeOffset July = new(2026, 7, 28, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset October = new(2026, 10, 5, 0, 0, 0, TimeSpan.Zero);

    private static CensusObservation Cell(string grader, short grade, int pop, DateTimeOffset at) =>
        new(grader, grade, pop, at);

    [Fact]
    public void Bars_are_the_fixed_six_with_absent_cells_as_true_zeros()
    {
        var census = CardCensus.From(
            [Cell("psa", 9, 8455, July), Cell("psa", 10, 486, July), Cell("cgc", 10, 4, July)]);

        Assert.Equal(6, census.Bars.Count);
        // Fixed order: PSA 8/9/10, CGC 8/9/10 (D-084.4).
        Assert.Equal(("psa", (short)8, 0), (census.Bars[0].Grader, census.Bars[0].Grade, census.Bars[0].Count));
        Assert.Equal(8455, census.Bars[1].Count);
        Assert.Equal(486, census.Bars[2].Count);
        Assert.Equal(0, census.Bars[3].Count);   // CGC 8 absent -> true zero
        Assert.Equal(0, census.Bars[4].Count);
        Assert.Equal(4, census.Bars[5].Count);
    }

    [Fact]
    public void Totals_sum_every_grade_not_just_the_bars()
    {
        var census = CardCensus.From(
            [Cell("psa", 1, 4096, July), Cell("psa", 6, 17851, July), Cell("psa", 10, 486, July),
             Cell("cgc", 3, 1, July)]);

        Assert.Equal(4096 + 17851 + 486, census.PsaTotal);
        Assert.Equal(1, census.CgcTotal);
    }

    [Fact]
    public void Qualifying_observations_count_only_from_the_floor()
    {
        // One observation in July (pre-floor), one in October (post-floor):
        // exactly one qualifies (D-033: nothing before 2026-09-01 counts). The
        // October row also wins latest-per-cell, and the full history rides
        // the record for the metrics.
        var census = CardCensus.From([Cell("psa", 10, 480, July), Cell("psa", 10, 500, October)]);

        Assert.Equal(1, census.QualifyingObservations);
        Assert.Equal(October, census.ObservedAt);
        Assert.Equal(500, census.Bars[2].Count);
        Assert.Equal(2, census.Observations.Count);
    }

    [Fact]
    public void No_rows_at_all_is_an_all_zero_census()
    {
        var census = CardCensus.From([]);

        Assert.All(census.Bars, bar => Assert.Equal(0, bar.Count));
        Assert.Equal(0, census.PsaTotal);
        Assert.Null(census.ObservedAt);
        Assert.Equal(0, census.QualifyingObservations);
    }
}
