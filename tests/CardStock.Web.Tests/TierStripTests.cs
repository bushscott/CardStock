using Bunit;
using CardStock.Application.Cards;
using CardStock.Web.Components.Card;

namespace CardStock.Web.Tests;

public class TierStripTests : BunitContext
{
    // One cell of each kind card.md §2.3.1/§3.2 distinguish: an available price on the
    // current, still-forming month (carries the ◌); an available price on a closed month
    // (no ◌, still a real number); a stale newest-month price (price dashes); no series at
    // all (price dashes); and a change the sufficiency floor rejects (change dashes).
    private static PricesDto FourTiers => new(
        "2026-08",
        [
            new TierDto("Psa10", "PSA 10", [],
                new TierPriceDto("available", 148600, "2026-08", true),
                new TierChangeDto("available", 0.062m, 12, 9)),
            new TierDto("Grade9Half", "Grade 9.5", [],
                new TierPriceDto("stale", null, "2026-03", null),
                new TierChangeDto("available", 0.026m, 4, 3)),
            new TierDto("Grade9", "Grade 9", [],
                new TierPriceDto("none", null, null, null),
                new TierChangeDto("insufficient", null, 1, 0)),
            new TierDto("Grade8", "Grade 8", [],
                new TierPriceDto("available", 71000, "2026-07", false),
                new TierChangeDto("insufficient", null, 2, 1)),
        ]);

    [Fact]
    public void Renders_the_glyph_once_dashes_for_absence_and_the_formatted_change()
    {
        var cut = Render<TierStrip>(p => p.Add(x => x.Prices, FourTiers));

        var glyphs = cut.FindAll(".tier-glyph");
        Assert.Single(glyphs);
        Assert.Equal("0", glyphs[0].GetAttribute("tabindex"));
        Assert.Contains("Aug ’26's average is still forming", glyphs[0].GetAttribute("title"));
        Assert.Equal(glyphs[0].GetAttribute("title"), glyphs[0].GetAttribute("aria-label"));

        var cells = cut.FindAll(".tier-cell");
        Assert.Equal(4, cells.Count);

        Assert.Equal("$1,486", cells[0].QuerySelector(".tier-price")!.TextContent);
        Assert.Equal("+6.2% 30d", cells[0].QuerySelector(".tier-change")!.TextContent);
        Assert.Equal(
            "PSA 10 — Aug ’26 month-to-date. +6.2% over 30 days.", cells[0].GetAttribute("title"));

        Assert.Equal("—", cells[1].QuerySelector(".tier-price")!.TextContent);
        Assert.Equal("—", cells[2].QuerySelector(".tier-price")!.TextContent);

        Assert.Equal("$710", cells[3].QuerySelector(".tier-price")!.TextContent);
        Assert.Equal("—", cells[3].QuerySelector(".tier-change")!.TextContent);
    }
}
