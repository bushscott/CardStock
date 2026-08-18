using CardStock.Web.Services;
using Xunit;

namespace CardStock.Web.Tests;

public class SpriteScaleTests
{
    // Measured art boxes from sprite-art.json (D-113): the factor is the largest of
    // ½/1/2/3 whose scaled art still fits the 68×56 slot; overflow art takes the
    // uniform half-sample.
    [Theory]
    [InlineData(21, 20, 2)]     // Pikachu — the ruling's motivating case
    [InlineData(44, 39, 1)]     // Charizard — already fills the slot
    [InlineData(32, 35, 1)]     // Gengar — 2× would overflow the slot's height
    [InlineData(31, 27, 2)]     // Rayquaza — 62×54 fits exactly under the lids
    [InlineData(34, 28, 2)]     // boundary: 2× lands exactly on 68×56
    [InlineData(22, 18, 3)]     // 3× fits: 66×54
    [InlineData(14, 11, 3)]     // Tynamo — 4× would fit but the rule caps at 3
    [InlineData(68, 56, 1)]     // art already the full slot
    [InlineData(88, 94, 0.5)]   // Koraidon — 96×96-canvas art, clean half-sample
    [InlineData(69, 10, 0.5)]   // width overflow alone forces the half-sample
    public void The_factor_is_the_largest_clean_scale_that_fits(int w, int h, double expected)
        => Assert.Equal(expected, SpriteScale.Factor(w, h));
}
