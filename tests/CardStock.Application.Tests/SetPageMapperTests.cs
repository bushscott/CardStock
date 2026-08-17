using CardStock.Application.Catalog;
using CardStock.Domain.Census;
using Xunit;

namespace CardStock.Application.Tests;

public class SetPageMapperTests
{
    private static SetPageSnapshot Snapshot(IReadOnlyList<RosterCard>? roster = null) => new(
        SetId: 7, Name: "Evolving Skies", MetadataStatus: "matched", Code: "swsh7",
        Era: "SWSH", CardsTracked: 237, FirstSale: new DateOnly(2021, 12, 15),
        Roster: roster ?? []);

    [Fact]
    public void First_sale_maps_to_year_month_only()
    {
        var dto = CatalogMappers.ToDto(Snapshot());
        Assert.Equal("2021-12", dto.FirstSaleMonth);
    }

    [Fact]
    public void A_null_first_sale_stays_null()
    {
        var dto = CatalogMappers.ToDto(Snapshot() with { FirstSale = null });
        Assert.Null(dto.FirstSaleMonth);
    }

    [Fact]
    public void Pop_states_map_to_wire_strings_with_iso_dates()
    {
        var pending = new PopulationDelta.Result(
            PopulationDeltaState.Pending, null,
            new DateOnly(2026, 7, 30), new DateOnly(2026, 9, 28));
        var row = new RosterCard(1, "Umbreon VMAX", true, 45_000, 0.031m, pending, 4);

        var dto = CatalogMappers.ToDto(Snapshot([row])).Roster[0];

        Assert.Equal("pending", dto.Pop.State);
        Assert.Equal("2026-07-30", dto.Pop.FirstObservedOn);
        Assert.Equal("2026-09-28", dto.Pop.DeltasBeginOn);
        Assert.Null(dto.Pop.Fraction);
        Assert.Equal(45_000, dto.PriceCents);
        Assert.Equal(0.031m, dto.Roc3M);
    }

    [Fact]
    public void Available_pop_carries_its_fraction()
    {
        var available = new PopulationDelta.Result(
            PopulationDeltaState.Available, 0.10m, new DateOnly(2026, 7, 1), null);
        var dto = CatalogMappers.ToDto(
            Snapshot([new RosterCard(1, "x", false, null, null, available, 0)])).Roster[0];
        Assert.Equal("available", dto.Pop.State);
        Assert.Equal(0.10m, dto.Pop.Fraction);
    }

    [Fact]
    public void None_state_maps_and_passes_dates_through()
    {
        // Empty-history None: all three fields null
        var emptyNone = new PopulationDelta.Result(
            PopulationDeltaState.None, null, null, null);
        var emptyDto = CatalogMappers.ToDto(
            Snapshot([new RosterCard(1, "a", false, null, null, emptyNone, 0)])).Roster[0];
        Assert.Equal("none", emptyDto.Pop.State);
        Assert.Null(emptyDto.Pop.Fraction);
        Assert.Null(emptyDto.Pop.FirstObservedOn);
        Assert.Null(emptyDto.Pop.DeltasBeginOn);

        // Zero-base None: FirstObservedOn set, DeltasBeginOn null
        var zeroBaseNone = new PopulationDelta.Result(
            PopulationDeltaState.None, null, new DateOnly(2026, 8, 1), null);
        var zeroDto = CatalogMappers.ToDto(
            Snapshot([new RosterCard(1, "b", false, null, null, zeroBaseNone, 0)])).Roster[0];
        Assert.Equal("none", zeroDto.Pop.State);
        Assert.Null(zeroDto.Pop.Fraction);
        Assert.Equal("2026-08-01", zeroDto.Pop.FirstObservedOn);
        Assert.Null(zeroDto.Pop.DeltasBeginOn);
    }
}
