using CardStock.Domain.Census;
using Xunit;

namespace CardStock.Domain.Tests;

public class PopulationDeltaTests
{
    private static readonly DateOnly Today = new(2026, 11, 1);

    private static PopulationObservation Obs(int year, int month, int day, int count) =>
        new(new DateOnly(year, month, day), count);

    [Fact]
    public void No_observations_is_None()
    {
        var result = PopulationDelta.Evaluate([], Today);
        Assert.Equal(PopulationDeltaState.None, result.State);
        Assert.Null(result.Fraction);
        Assert.Null(result.FirstObservedOn);
    }

    [Fact]
    public void A_first_observation_younger_than_60_days_is_Pending_with_computed_dates()
    {
        var first = Today.AddDays(-30);
        var result = PopulationDelta.Evaluate([new PopulationObservation(first, 100)], Today);
        Assert.Equal(PopulationDeltaState.Pending, result.State);
        Assert.Equal(first, result.FirstObservedOn);
        Assert.Equal(first.AddDays(PopulationDelta.WindowDays), result.DeltasBeginOn);
        Assert.Null(result.Fraction);
    }

    [Fact]
    public void A_first_observation_exactly_60_days_old_is_Available()
    {
        var result = PopulationDelta.Evaluate(
            [new PopulationObservation(Today.AddDays(-60), 100)], Today);
        Assert.Equal(PopulationDeltaState.Available, result.State);
        // One flat value across the window: zero growth.
        Assert.Equal(0m, result.Fraction);
    }

    [Fact]
    public void Change_only_rows_resolve_as_of_each_date_flat_between_rows()
    {
        // 100 on Sep 1, 110 on Oct 20. As-of Sep 2 (today-60) = 100; now = 110.
        var result = PopulationDelta.Evaluate(
            [Obs(2026, 9, 1, 100), Obs(2026, 10, 20, 110)], Today);
        Assert.Equal(PopulationDeltaState.Available, result.State);
        Assert.Equal(0.10m, result.Fraction);
    }

    [Fact]
    public void A_decrease_is_a_negative_fraction()
    {
        var result = PopulationDelta.Evaluate(
            [Obs(2026, 9, 1, 200), Obs(2026, 10, 20, 150)], Today);
        Assert.Equal(-0.25m, result.Fraction);
    }

    [Fact]
    public void A_zero_base_60_days_ago_is_None_not_a_division()
    {
        // A stored 0 is real: change-only writes a 0 when a cell decreases to zero.
        var result = PopulationDelta.Evaluate(
            [Obs(2026, 9, 1, 0), Obs(2026, 10, 20, 40)], Today);
        Assert.Equal(PopulationDeltaState.None, result.State);
        Assert.Null(result.Fraction);
    }

    [Fact]
    public void Unsorted_input_is_handled()
    {
        var result = PopulationDelta.Evaluate(
            [Obs(2026, 10, 20, 110), Obs(2026, 9, 1, 100)], Today);
        Assert.Equal(0.10m, result.Fraction);
    }

    [Fact]
    public void Pre_floor_observations_are_excluded_first_resolves_to_the_earliest_eligible_row()
    {
        // D-033: the Aug row predates CardCensus.ObservationFloor (2026-09-01) and must never
        // participate -- first resolves to the Oct row, which is younger than the 60-day
        // window (windowStart = Today - 60 = Sep 2), so the state is Pending, not Available.
        var result = PopulationDelta.Evaluate(
            [Obs(2026, 8, 1, 100), Obs(2026, 10, 20, 110)], Today);
        Assert.Equal(PopulationDeltaState.Pending, result.State);
        Assert.Null(result.Fraction);
        Assert.Equal(new DateOnly(2026, 10, 20), result.FirstObservedOn);
        Assert.Equal(new DateOnly(2026, 10, 20).AddDays(PopulationDelta.WindowDays), result.DeltasBeginOn);
    }

    [Fact]
    public void A_card_with_only_pre_floor_rows_is_None()
    {
        // Every row predates the floor -- eligible is empty, same shape as no observations at all.
        var result = PopulationDelta.Evaluate([Obs(2026, 8, 1, 100)], Today);
        Assert.Equal(PopulationDeltaState.None, result.State);
        Assert.Null(result.Fraction);
        Assert.Null(result.FirstObservedOn);
        Assert.Null(result.DeltasBeginOn);
    }
}
