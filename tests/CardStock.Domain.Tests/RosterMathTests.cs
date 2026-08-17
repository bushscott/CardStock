using CardStock.Domain.Prices;
using Xunit;

namespace CardStock.Domain.Tests;

public class RosterMathTests
{
    private static readonly DateOnly CurrentMonth = new(2026, 8, 1);

    [Fact]
    public void Roc_compares_last_closed_month_with_three_before_it()
    {
        var cells = new Dictionary<DateOnly, int>
        {
            [new DateOnly(2026, 7, 1)] = 12_000,   // currentMonth − 1
            [new DateOnly(2026, 4, 1)] = 10_000,   // currentMonth − 4
        };
        Assert.Equal(0.2m, RosterMath.Roc3M(cells, CurrentMonth));
    }

    [Fact]
    public void A_missing_endpoint_month_is_null_never_carried_forward()
    {
        var cells = new Dictionary<DateOnly, int>
        {
            [new DateOnly(2026, 7, 1)] = 12_000,
            [new DateOnly(2026, 3, 1)] = 10_000,   // a neighbor, not the anchor
        };
        Assert.Null(RosterMath.Roc3M(cells, CurrentMonth));
    }

    [Fact]
    public void The_current_month_itself_never_participates()
    {
        var cells = new Dictionary<DateOnly, int>
        {
            [new DateOnly(2026, 8, 1)] = 99_000,   // current, partial — must be ignored
            [new DateOnly(2026, 4, 1)] = 10_000,
        };
        Assert.Null(RosterMath.Roc3M(cells, CurrentMonth));
    }
}
