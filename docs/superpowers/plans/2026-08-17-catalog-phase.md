# Catalog Phase Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the four Catalog pages — Set, Character, Browse, About-data — on the landed Pokédex substrate, with every not-ready statistic rendered in the D-102 dash-and-◌ vocabulary.

**Architecture:** Vertical slices in the order Set → Character → Browse → About-data, each slice reader → endpoint → page → tests. Read access is five new `ToView` mappings over the scraper's tables; corpus-wide Browse aggregates sit behind a short-TTL in-process cache (the raw query measured 1,427 ms on the Pi). Worker-gated stats have no wire representation — the UI renders their slots unconditionally.

**Tech Stack:** .NET 10, Blazor WebAssembly, EF Core + Npgsql, minimal APIs, xunit + bUnit, per-test Postgres databases on the Pi (`CARDSTOCK_TEST_DB`).

**Spec:** `docs/superpowers/specs/2026-08-15-catalog-phase-design.md` (D-110). The spec travels with this plan; read both. Screen-by-screen detail: `docs/screens/{browse,set,character,about-data}.md`, each carrying a 2026-08-15 amendment banner that supersedes its body where they differ.

## Global Constraints

- `net10.0`, `Nullable=enable`, `TreatWarningsAsErrors=true` — a warning fails the build.
- Before every commit: `dotnet build` clean, the named tests green, `dotnet format --verify-no-changes` clean (run `dotnet format` first if it complains).
- **The D-102 vocabulary** (spec §2): labels always print; value runs below a gate hold `ChipEngine.GlyphDash` (`"–"`, the one definition — never type a literal dash in a gated cell); the ◌ glyph sits beside a gated *statistic's label* with the gate note as a keyboard-reachable tooltip (`tabindex="0" title aria-label`, the `CensusSentence.razor` pattern); per-row maturity (Pop Δ, Year) is a dashed cell with a *computed-date* tooltip, never ◌ and never an authored date.
- **Worker-gated stats never appear in DTOs** — no always-null fields. The UI renders their slots unconditionally.
- **"Active card" everywhere:** `delisted_at IS NULL AND not_a_card_at IS NULL`. Every count, denominator, and roster uses it.
- **Latest PSA 10 per card:** within `(card_id, tier = 5)` take max `month` then max `observed_at` (D-078), excluding `price_cents = 0` (the I2 no-sales rule). Tier 5 is `PriceTier.Psa10` — stored as `integer`, never reorder the enum.
- **Formatting:** percents via `Format.ChangePercent` (U+2212 minus, zero renders `+0.0%`); roster money via `Format.Money` (whole dollars); header stat tiles via `Format.AbbrevMoney` (≥$10K → `$96.4K`). Month labels `Format.MonthLabel` (`Sep ’26`) / `Format.MonthYear` (`Dec 2021`).
- **Copy:** no hype, no exclamation marks, precise numbers over adjectives. Tooltips carry the gate reasons verbatim from this plan.
- Commit messages follow the repo's existing style: lower-case summary of what the change *is*, e.g. `catalog: set page reader resolves latest PSA 10 per card`.
- Tests that need Postgres derive from `CardStock.TestSupport.CardStockDatabaseTest` and skip when `CARDSTOCK_TEST_DB` is unset. Never install a local Postgres; the variable points at the Pi.

## File Structure

```
src/CardStock.Domain/
  Census/PopulationDelta.cs            pop Δ 60d math + states (Task 2)
  Prices/RosterMath.cs                 ROC 3M month-cell rule for rosters (Task 3)
src/CardStock.Application/Catalog/
  SetPageContracts.cs                  ISetPageReader + snapshot records (Task 4)
  CharacterPageContracts.cs            ICharacterPageReader + snapshot records (Task 12)
  BrowseContracts.cs                   IBrowseReader, ICatalogAggregates + tile records (Task 16/17)
  CatalogWire.cs                       every catalog DTO (Tasks 4, 12, 17)
  CatalogMappers.cs                    snapshot → DTO mapping (Tasks 4, 12, 17)
src/CardStock.Infrastructure/
  Persistence/ScraperReadModels/       ScraperSpecies, ScraperSpeciesType, ScraperSpeciesEggGroup,
                                       ScraperCardSpecies, ScraperSetDetail (Task 1)
  Persistence/ScraperViews.cs          + five ToView mappings (Task 1)
  Persistence/CardStockDbContext.cs    + five DbSets (Task 1)
  Catalog/SetPageReader.cs             (Task 5)
  Catalog/CharacterPageReader.cs       (Task 13)
  Catalog/CatalogAggregateCache.cs     (Task 16)
  Catalog/BrowseReader.cs              (Task 17)
src/CardStock.Api/
  Catalog/CatalogEndpoints.cs          /sets/{id}, /characters/{slug}, /browse/*, /species/{id}/icon
                                       (Tasks 6, 14, 18)
  Program.cs                           DI + MapCatalogEndpoints (Tasks 6, 14, 16, 18)
src/CardStock.Web/
  Services/CatalogApiClient.cs         (Task 7)
  Services/Format.cs                   + AbbrevMoney, MonthYear (Task 7)
  Services/SetGradients.cs             deterministic set gradient pairs (Task 20)
  Services/SetShelves.cs               era/date grouping engine (Task 19)
  Services/SpeciesFilters.cs           8-attribute filter engine (Task 19)
  Components/Catalog/PendingGlyph.razor(.css)         ◌ (Task 8)
  Components/Catalog/DeferredIndexBlock.razor(.css)   sparkline mock (Task 8)
  Components/Catalog/SortState.cs                     (Task 8)
  Components/Catalog/DensityToggle.razor(.css)        (Task 8)
  Components/Catalog/SortPills.razor(.css)            (Task 8)
  Components/Catalog/RosterTable.razor(.css)          virtualized sortable table (Task 9)
  Components/Catalog/BinderGrid.razor(.css)           art tile grid (Task 10)
  Components/Catalog/BrowseFilterPopover.razor(.css)  (Task 21)
  Pages/SetPage.razor(.css)            (Task 11)
  Pages/CharacterPage.razor(.css)      (Task 15)
  Pages/BrowsePage.razor(.css)         (Tasks 20, 21)
  Pages/AboutDataPage.razor(.css)      (Task 22)
  Layout/AppChrome.razor               Browse tab arms; search tooltip edit (Task 23)
  Components/Card/FreshnessFooter.razor  + About-data link (Task 23)
  wwwroot/js/catalog.js                pointer-capture helper for column resize (Task 9)
tests/  one test file per component/reader/endpoint, in the matching test project
tests/CardStock.TestSupport/Fixtures/scraper-schema.sql   regenerated with the Pokédex tables (Task 1)
```

---

### Task 1: Pokédex read models, view mappings, and the schema fixture

The five scraper tables this phase reads become `ToView`-mapped read models. The per-test
database fixture predates the sibling's `AddPokedex` migration, so it must be regenerated or
every reader test fails on missing tables.

**Files:**
- Create: `src/CardStock.Infrastructure/Persistence/ScraperReadModels/ScraperSpecies.cs`
- Create: `src/CardStock.Infrastructure/Persistence/ScraperReadModels/ScraperSpeciesType.cs`
- Create: `src/CardStock.Infrastructure/Persistence/ScraperReadModels/ScraperSpeciesEggGroup.cs`
- Create: `src/CardStock.Infrastructure/Persistence/ScraperReadModels/ScraperCardSpecies.cs`
- Create: `src/CardStock.Infrastructure/Persistence/ScraperReadModels/ScraperSetDetail.cs`
- Modify: `src/CardStock.Infrastructure/Persistence/ScraperViews.cs`
- Modify: `src/CardStock.Infrastructure/Persistence/CardStockDbContext.cs`
- Modify: `tests/CardStock.TestSupport/Fixtures/scraper-schema.sql` (regenerate)
- Test: `tests/CardStock.Infrastructure.Tests/PokedexViewTests.cs`

**Interfaces:**
- Consumes: the sibling's landed schema (`../PokemonInvestBatch`, migration `20260815032212_AddPokedex`).
- Produces: `db.ScraperSpecies`, `db.ScraperSpeciesTypes`, `db.ScraperSpeciesEggGroups`, `db.ScraperCardSpecies`, `db.ScraperSetDetails` — queryable, read-only. Enum storage facts every later task relies on: `species.status` smallint 0=Ordinary 1=Legendary 2=Mythical; `card_species.method` smallint 0=TitleMatch 1=Manual; `set_details.match_status` smallint 0=Matched 1=Pending.

- [ ] **Step 1: Regenerate the scraper schema fixture from the sibling**

```bash
cd /Users/scott/RiderProjects/PokemonInvestBatch
dotnet ef migrations script \
  -p src/PokemonInvestBatch.Infrastructure \
  -s src/PokemonInvestBatch.Worker \
  -o /Users/scott/RiderProjects/CardStock/tests/CardStock.TestSupport/Fixtures/scraper-schema.sql
cd /Users/scott/RiderProjects/CardStock
grep -c "CREATE TABLE" tests/CardStock.TestSupport/Fixtures/scraper-schema.sql
grep -n "CREATE TABLE species\b\|CREATE TABLE card_species\|CREATE TABLE set_details" \
  tests/CardStock.TestSupport/Fixtures/scraper-schema.sql
```

Expected: the three greps hit — `species`, `card_species`, `set_details` (plus
`species_types`, `species_egg_groups`, `species_names`, `card_tagging`) now exist in the
fixture. If `dotnet ef` is not installed as a tool there, run
`dotnet tool restore` in the sibling first; if the script command still fails, hand-append the
`Up()` DDL from the sibling's
`src/PokemonInvestBatch.Infrastructure/Persistence/Migrations/20260815032212_AddPokedex.cs`
translated to SQL — but the script route is the intended one. Keep `git diff` limited to
additions plus migration-history rows; the pre-existing tables' DDL must not change.

- [ ] **Step 2: Write the read models**

Five files, one per type. Mirror `ScraperSet.cs`'s style: `IScraperOwned`, `init`-only, doc
comment naming the owner. Only columns CardStock reads are carried.

`ScraperSpecies.cs`:

```csharp
namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>Read-only mirror of public.species (sibling ADR-0011). PK is the
/// national dex number. Owned by PokemonInvestBatch.</summary>
public class ScraperSpecies : IScraperOwned
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public required string Slug { get; init; }

    public short Generation { get; init; }

    public required string Region { get; init; }

    public required string Color { get; init; }

    /// <summary>Null for Generation 4 onward — PokéAPI stopped assigning habitats.</summary>
    public string? Habitat { get; init; }

    /// <summary>0 Ordinary · 1 Legendary · 2 Mythical (sibling's SpeciesStatus).</summary>
    public short Status { get; init; }

    /// <summary>Chain depth from the evolution root; 0 = basic.</summary>
    public short Stage { get; init; }

    public int? EvolvesFromSpeciesId { get; init; }

    public required string GradientStart { get; init; }

    public required string GradientEnd { get; init; }
}
```

`ScraperSpeciesType.cs`:

```csharp
namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>Read-only mirror of public.species_types. 1–2 rows per species, ordered by Slot.</summary>
public class ScraperSpeciesType : IScraperOwned
{
    public int SpeciesId { get; init; }

    public short Slot { get; init; }

    public required string Type { get; init; }
}
```

`ScraperSpeciesEggGroup.cs`:

```csharp
namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>Read-only mirror of public.species_egg_groups. 1–2 display-named rows per species.</summary>
public class ScraperSpeciesEggGroup : IScraperOwned
{
    public int SpeciesId { get; init; }

    public required string EggGroup { get; init; }
}
```

`ScraperCardSpecies.cs`:

```csharp
namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>Read-only mirror of public.card_species — the card ↔ species junction.
/// Current-state, not append-only (sibling ADR-0011 deviation one).</summary>
public class ScraperCardSpecies : IScraperOwned
{
    public long CardId { get; init; }

    public int SpeciesId { get; init; }
}
```

`ScraperSetDetail.cs`:

```csharp
namespace CardStock.Infrastructure.Persistence.ScraperReadModels;

/// <summary>Read-only mirror of public.set_details. One row per set, always;
/// MatchStatus 0 = Matched (code/date/series written), 1 = Pending (all null).</summary>
public class ScraperSetDetail : IScraperOwned
{
    public long SetId { get; init; }

    public short MatchStatus { get; init; }

    /// <summary>TCGdex id verbatim ("swsh7") — display formatting is CardStock's job.</summary>
    public string? Code { get; init; }

    public DateOnly? ReleasedOn { get; init; }

    public string? Series { get; init; }

    public string? Era { get; init; }
}
```

- [ ] **Step 3: Map the views and add the DbSets**

In `ScraperViews.Map`, after the `ScraperSale` block (composite keys mirror the sibling's
`PokemonDbContext` — junction and child tables have composite PKs):

```csharp
        builder.Entity<ScraperSpecies>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.ToView("species", CardStockDbContext.ScraperSchema);
        });

        builder.Entity<ScraperSpeciesType>(entity =>
        {
            entity.HasKey(x => new { x.SpeciesId, x.Slot });
            entity.ToView("species_types", CardStockDbContext.ScraperSchema);
        });

        builder.Entity<ScraperSpeciesEggGroup>(entity =>
        {
            entity.HasKey(x => new { x.SpeciesId, x.EggGroup });
            entity.ToView("species_egg_groups", CardStockDbContext.ScraperSchema);
        });

        builder.Entity<ScraperCardSpecies>(entity =>
        {
            entity.HasKey(x => new { x.CardId, x.SpeciesId });
            entity.ToView("card_species", CardStockDbContext.ScraperSchema);
        });

        builder.Entity<ScraperSetDetail>(entity =>
        {
            entity.HasKey(x => x.SetId);
            entity.ToView("set_details", CardStockDbContext.ScraperSchema);
        });
```

In `CardStockDbContext`, after `ScraperSales`:

```csharp
    public DbSet<ScraperSpecies> ScraperSpecies => Set<ScraperSpecies>();

    public DbSet<ScraperSpeciesType> ScraperSpeciesTypes => Set<ScraperSpeciesType>();

    public DbSet<ScraperSpeciesEggGroup> ScraperSpeciesEggGroups => Set<ScraperSpeciesEggGroup>();

    public DbSet<ScraperCardSpecies> ScraperCardSpecies => Set<ScraperCardSpecies>();

    public DbSet<ScraperSetDetail> ScraperSetDetails => Set<ScraperSetDetail>();
```

Deliberately **not** mapped: `species_names` (nothing reads it until a later phase) and
`card_tagging` (lane bookkeeping). A mapping with no consumer is drift waiting to happen —
say so in a one-line comment at the bottom of `ScraperViews.Map`.

- [ ] **Step 4: Write the failing view test**

`tests/CardStock.Infrastructure.Tests/PokedexViewTests.cs`:

```csharp
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardStock.Infrastructure.Tests;

public class PokedexViewTests : CardStockDatabaseTest
{
    [SkippableFact]
    public async Task The_five_pokedex_views_read_seeded_rows()
    {
        Skip.IfNot(Available);
        await using var db = NewContext();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO public.species
                (id, name, slug, generation, region, color, habitat, status, stage,
                 evolves_from_species_id, gradient_start, gradient_end)
            VALUES (197, 'Umbreon', 'umbreon', 2, 'Johto', 'Black', 'Urban', 0, 1,
                    133, '#2B2D42', '#5C6B9E');
            INSERT INTO public.species_types (species_id, slot, type) VALUES (197, 1, 'Dark');
            INSERT INTO public.species_egg_groups (species_id, egg_group) VALUES (197, 'Field');
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at)
            VALUES (7, 'pokemon-evolving-skies', 'Evolving Skies', now(), now());
            INSERT INTO public.set_details (set_id, match_status, code, released_on, series, era)
            VALUES (7, 0, 'swsh7', '2021-08-27', 'Sword & Shield', 'SWSH');
            INSERT INTO public.cards (id, set_id, name, url)
            VALUES (630001, 7, 'Umbreon VMAX (Alternate Art Secret)',
                    'https://www.pricecharting.com/game/x/y');
            INSERT INTO public.card_species (card_id, species_id, method)
            VALUES (630001, 197, 0);
            """);

        var species = await db.ScraperSpecies.SingleAsync(s => s.Id == 197);
        Assert.Equal("umbreon", species.Slug);
        Assert.Equal("Johto", species.Region);
        Assert.Equal(133, species.EvolvesFromSpeciesId);

        Assert.Equal("Dark",
            (await db.ScraperSpeciesTypes.SingleAsync(t => t.SpeciesId == 197)).Type);
        Assert.Equal("Field",
            (await db.ScraperSpeciesEggGroups.SingleAsync(g => g.SpeciesId == 197)).EggGroup);

        var link = await db.ScraperCardSpecies.SingleAsync();
        Assert.Equal(630001, link.CardId);

        var detail = await db.ScraperSetDetails.SingleAsync(d => d.SetId == 7);
        Assert.Equal("swsh7", detail.Code);
        Assert.Equal(new DateOnly(2021, 8, 27), detail.ReleasedOn);
        Assert.Equal("SWSH", detail.Era);
    }

    [SkippableFact]
    public async Task Writing_a_scraper_view_throws_before_reaching_the_database()
    {
        Skip.IfNot(Available);
        await using var db = NewContext();
        db.Add(new Persistence.ScraperReadModels.ScraperCardSpecies { CardId = 1, SpeciesId = 1 });
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}
```

If the existing suite uses a different skip idiom than `SkippableFact` (check
`CardPriceReaderTests` for the established one), use that idiom — the assertions stay.
If the `cards` or `sets` INSERT trips a NOT NULL column this plan doesn't list, add that
column with a dummy value; the fixture's DDL is the authority.

- [ ] **Step 5: Run the test to verify it fails**

```bash
CARDSTOCK_TEST_DB="<the usual Pi template string>" \
dotnet test tests/CardStock.Infrastructure.Tests --filter PokedexViewTests -v minimal
```

Expected before Steps 2–3 are complete: compile failure (types missing). After schema-only:
fails on missing tables if the fixture was not regenerated. With everything in place, move on.

- [ ] **Step 6: Run the full Infrastructure suite plus SchemaModelTests**

```bash
CARDSTOCK_TEST_DB="..." dotnet test tests/CardStock.Infrastructure.Tests -v minimal
```

Expected: PASS. `SchemaModelTests` asserts every `IScraperOwned` type is `ToView`-mapped —
the five new types must satisfy it automatically.

- [ ] **Step 7: Commit**

```bash
git add src/CardStock.Infrastructure tests/CardStock.TestSupport/Fixtures/scraper-schema.sql \
  tests/CardStock.Infrastructure.Tests/PokedexViewTests.cs
git commit -m "catalog: the five pokedex tables become read-only views"
```

---

### Task 2: Domain — PopulationDelta

The Set roster's Pop Δ 60d: PSA-10 census now vs as-of-60-days-ago under change-only
semantics. Census arithmetic has no Skender analog — the referee is these hand fixtures plus
SQL-predicted live values at phase close.

**Files:**
- Create: `src/CardStock.Domain/Census/PopulationDelta.cs`
- Test: `tests/CardStock.Domain.Tests/PopulationDeltaTests.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces: `PopulationDelta.Evaluate(IReadOnlyList<PopulationObservation> psa10, DateOnly today)` → `PopulationDelta.Result(PopulationDeltaState State, decimal? Fraction, DateOnly? FirstObservedOn, DateOnly? DeltasBeginOn)`; `enum PopulationDeltaState { Available, Pending, None }`; `record PopulationObservation(DateOnly ObservedOn, int Count)`; `PopulationDelta.WindowDays = 60`.

- [ ] **Step 1: Write the failing tests**

```csharp
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
        // 100 on Aug 1, 110 on Oct 20. As-of Sep 2 (today-60) = 100; now = 110.
        var result = PopulationDelta.Evaluate(
            [Obs(2026, 8, 1, 100), Obs(2026, 10, 20, 110)], Today);
        Assert.Equal(PopulationDeltaState.Available, result.State);
        Assert.Equal(0.10m, result.Fraction);
    }

    [Fact]
    public void A_decrease_is_a_negative_fraction()
    {
        var result = PopulationDelta.Evaluate(
            [Obs(2026, 8, 1, 200), Obs(2026, 10, 20, 150)], Today);
        Assert.Equal(-0.25m, result.Fraction);
    }

    [Fact]
    public void A_zero_base_60_days_ago_is_None_not_a_division()
    {
        // A stored 0 is real: change-only writes a 0 when a cell decreases to zero.
        var result = PopulationDelta.Evaluate(
            [Obs(2026, 8, 1, 0), Obs(2026, 10, 20, 40)], Today);
        Assert.Equal(PopulationDeltaState.None, result.State);
        Assert.Null(result.Fraction);
    }

    [Fact]
    public void Unsorted_input_is_handled()
    {
        var result = PopulationDelta.Evaluate(
            [Obs(2026, 10, 20, 110), Obs(2026, 8, 1, 100)], Today);
        Assert.Equal(0.10m, result.Fraction);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/CardStock.Domain.Tests --filter PopulationDeltaTests -v minimal`
Expected: compile failure — `PopulationDelta` does not exist.

- [ ] **Step 3: Implement**

```csharp
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
/// </summary>
public static class PopulationDelta
{
    public const int WindowDays = 60;

    public sealed record Result(
        PopulationDeltaState State, decimal? Fraction,
        DateOnly? FirstObservedOn, DateOnly? DeltasBeginOn);

    public static Result Evaluate(IReadOnlyList<PopulationObservation> psa10, DateOnly today)
    {
        if (psa10.Count == 0)
        {
            return new Result(PopulationDeltaState.None, null, null, null);
        }

        var ordered = psa10.OrderBy(o => o.ObservedOn).ToList();
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
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/CardStock.Domain.Tests --filter PopulationDeltaTests -v minimal`
Expected: 7 PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Domain/Census/PopulationDelta.cs tests/CardStock.Domain.Tests/PopulationDeltaTests.cs
git commit -m "catalog: pop delta 60d — change-only as-of math with pending and none states"
```

---

### Task 3: Domain — RosterMath (ROC 3M for rosters)

The roster ROC must equal the Card page's: `ChipEngine.At` resolves month `currentMonth − 1 − offset`, so ROC 3M compares the cell at `currentMonth − 1` with `currentMonth − 4`, and an absent month-cell is a gap, never carried forward.

**Files:**
- Create: `src/CardStock.Domain/Prices/RosterMath.cs`
- Test: `tests/CardStock.Domain.Tests/RosterMathTests.cs`

**Interfaces:**
- Consumes: `Indicators.Roc(decimal now, decimal then)` (existing, `Signals/Indicators.cs:29` — returns `now / then − 1`, null when `then == 0`).
- Produces: `RosterMath.Roc3M(IReadOnlyDictionary<DateOnly, int> psa10CentsByMonth, DateOnly currentMonth)` → `decimal?`. Keys are first-of-month dates; values are latest-per-cell cents.

- [ ] **Step 1: Write the failing tests**

```csharp
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
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/CardStock.Domain.Tests --filter RosterMathTests -v minimal`
Expected: compile failure.

- [ ] **Step 3: Implement**

```csharp
using CardStock.Domain.Signals;

namespace CardStock.Domain.Prices;

/// <summary>Per-card roster math. Mirrors ChipEngine's month rule exactly
/// (At: month = currentMonth − 1 − offset), so a roster ROC always agrees
/// with the same card's signals panel.</summary>
public static class RosterMath
{
    public static decimal? Roc3M(
        IReadOnlyDictionary<DateOnly, int> psa10CentsByMonth, DateOnly currentMonth)
    {
        var m1 = currentMonth.AddMonths(-1);
        var m4 = currentMonth.AddMonths(-4);
        if (!psa10CentsByMonth.TryGetValue(m1, out var now) ||
            !psa10CentsByMonth.TryGetValue(m4, out var then))
        {
            return null;
        }

        return Indicators.Roc(now / 100m, then / 100m);
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/CardStock.Domain.Tests --filter RosterMathTests -v minimal`
Expected: 3 PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Domain/Prices/RosterMath.cs tests/CardStock.Domain.Tests/RosterMathTests.cs
git commit -m "catalog: roster ROC 3M rides the chip engine's month rule"
```

---

### Task 4: Application — Set page contracts, wire DTOs, mapper

**Files:**
- Create: `src/CardStock.Application/Catalog/SetPageContracts.cs`
- Create: `src/CardStock.Application/Catalog/CatalogWire.cs`
- Create: `src/CardStock.Application/Catalog/CatalogMappers.cs`
- Test: `tests/CardStock.Application.Tests/SetPageMapperTests.cs`

**Interfaces:**
- Consumes: `PopulationDelta.Result`, `PopulationDeltaState` (Task 2).
- Produces (later tasks depend on these exact names):
  - `ISetPageReader.GetAsync(long setId, CancellationToken ct = default)` → `Task<SetPageSnapshot?>`
  - `SetPageSnapshot(long SetId, string Name, string MetadataStatus, string? Code, string? Era, int CardsTracked, DateOnly? FirstSale, IReadOnlyList<RosterCard> Roster)` — `MetadataStatus` is `"matched"` or `"pending"`; `Code` is raw TCGdex id, uppercasing is the client's job.
  - `RosterCard(long CardId, string Name, bool HasImage, int? PriceCents, decimal? Roc3M, PopulationDelta.Result Pop, int Sales30d)`
  - Wire: `SetPageDto(long SetId, string Name, string MetadataStatus, string? Code, string? Era, int CardsTracked, string? FirstSaleMonth, IReadOnlyList<SetRosterRowDto> Roster)` — `FirstSaleMonth` is `"yyyy-MM"`.
  - `SetRosterRowDto(long CardId, string Name, bool HasImage, int? PriceCents, decimal? Roc3M, PopDto Pop, int Sales30d)`
  - `PopDto(string State, decimal? Fraction, string? FirstObservedOn, string? DeltasBeginOn)` — state `"available" | "pending" | "none"`; dates `"yyyy-MM-dd"`.
  - `CatalogMappers.ToDto(SetPageSnapshot snapshot)` → `SetPageDto`.

- [ ] **Step 1: Write the failing mapper tests**

```csharp
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
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/CardStock.Application.Tests --filter SetPageMapperTests -v minimal`
Expected: compile failure.

- [ ] **Step 3: Implement contracts, wire, mapper**

`SetPageContracts.cs`:

```csharp
using CardStock.Domain.Census;

namespace CardStock.Application.Catalog;

public interface ISetPageReader
{
    Task<SetPageSnapshot?> GetAsync(long setId, CancellationToken ct = default);
}

/// <summary>One set page in one read: header facts plus the full roster
/// (full-roster-virtualized, D-110 — no cap, no "most-traded" fiction).</summary>
public sealed record SetPageSnapshot(
    long SetId,
    string Name,
    string MetadataStatus,
    string? Code,
    string? Era,
    int CardsTracked,
    DateOnly? FirstSale,
    IReadOnlyList<RosterCard> Roster);

public sealed record RosterCard(
    long CardId,
    string Name,
    bool HasImage,
    int? PriceCents,
    decimal? Roc3M,
    PopulationDelta.Result Pop,
    int Sales30d);
```

`CatalogWire.cs` (the Set part; Tasks 12 and 17 append to this file):

```csharp
namespace CardStock.Application.Catalog;

public sealed record SetPageDto(
    long SetId, string Name, string MetadataStatus, string? Code, string? Era,
    int CardsTracked, string? FirstSaleMonth, IReadOnlyList<SetRosterRowDto> Roster);

public sealed record SetRosterRowDto(
    long CardId, string Name, bool HasImage, int? PriceCents, decimal? Roc3M,
    PopDto Pop, int Sales30d);

/// <summary>State: "available" | "pending" | "none". Dates are "yyyy-MM-dd" and
/// computed, never authored (D-061) — the client prints them into the gate
/// tooltips verbatim.</summary>
public sealed record PopDto(
    string State, decimal? Fraction, string? FirstObservedOn, string? DeltasBeginOn);
```

`CatalogMappers.cs`:

```csharp
using CardStock.Domain.Census;

namespace CardStock.Application.Catalog;

public static class CatalogMappers
{
    public static SetPageDto ToDto(SetPageSnapshot snapshot) => new(
        snapshot.SetId, snapshot.Name, snapshot.MetadataStatus, snapshot.Code, snapshot.Era,
        snapshot.CardsTracked, snapshot.FirstSale?.ToString("yyyy-MM"),
        snapshot.Roster.Select(ToDto).ToArray());

    private static SetRosterRowDto ToDto(RosterCard card) => new(
        card.CardId, card.Name, card.HasImage, card.PriceCents, card.Roc3M,
        ToDto(card.Pop), card.Sales30d);

    private static PopDto ToDto(PopulationDelta.Result pop) => new(
        pop.State switch
        {
            PopulationDeltaState.Available => "available",
            PopulationDeltaState.Pending => "pending",
            _ => "none",
        },
        pop.Fraction,
        pop.FirstObservedOn?.ToString("yyyy-MM-dd"),
        pop.DeltasBeginOn?.ToString("yyyy-MM-dd"));
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/CardStock.Application.Tests --filter SetPageMapperTests -v minimal`
Expected: 4 PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Application/Catalog tests/CardStock.Application.Tests/SetPageMapperTests.cs
git commit -m "catalog: set page contracts, wire shapes, and mapper"
```

---

### Task 5: Infrastructure — SetPageReader

Latest-per-key resolves in SQL (`DISTINCT ON`, bounded to one set's cards) — the named
deviation from "Domain does the thinking"; per-card signal math stays in Domain (Tasks 2–3).

**Files:**
- Create: `src/CardStock.Infrastructure/Catalog/SetPageReader.cs`
- Test: `tests/CardStock.Infrastructure.Tests/SetPageReaderTests.cs`

**Interfaces:**
- Consumes: `ISetPageReader`, `SetPageSnapshot`, `RosterCard` (Task 4); `PopulationDelta`, `PopulationObservation` (Task 2); `RosterMath.Roc3M` (Task 3); the five views (Task 1).
- Produces: `SetPageReader : ISetPageReader` (registered in Task 6). Internal row shape for raw SQL: `LatestPsa10Row(long CardId, int PriceCents)` — reused verbatim by Tasks 13 and 16.

- [ ] **Step 1: Write the failing reader tests**

Seed one set with three cards exercising: a priced card with full ROC anchors and mature
census; a card whose price series has a gap at the ROC anchor and whose census is young; a
card with no PSA 10 rows at all. Plus a delisted card that must not appear, and a D-078
revision — two rows for the same closed month where the newer `observed_at` must win.

```csharp
using CardStock.Application.Catalog;
using CardStock.Domain.Census;
using CardStock.Infrastructure.Catalog;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardStock.Infrastructure.Tests;

public class SetPageReaderTests : CardStockDatabaseTest
{
    // Fixed clock: current month = Aug 2026, so ROC anchors are Jul (m−1) and Apr (m−4).
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    private SetPageReader Reader() =>
        new(NewContextFactory(), new FixedTime(Now));

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private async Task SeedAsync()
    {
        await using var db = NewContext();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at)
            VALUES (7, 'pokemon-evolving-skies', 'Evolving Skies', now(), now());
            INSERT INTO public.set_details (set_id, match_status, code, released_on, series, era)
            VALUES (7, 0, 'swsh7', '2021-08-27', 'Sword & Shield', 'SWSH');
            INSERT INTO public.cards (id, set_id, name, url) VALUES
              (1, 7, 'Umbreon VMAX', 'https://x/1'),
              (2, 7, 'Glaceon V',    'https://x/2'),
              (3, 7, 'Leafeon V',    'https://x/3');
            INSERT INTO public.cards (id, set_id, name, url, delisted_at)
            VALUES (4, 7, 'Ghost Card', 'https://x/4', now());

            -- Card 1: Jul revised (12000 then 12500 — newer observed_at must win),
            -- Apr anchor present, plus a current-month row that must not matter.
            INSERT INTO public.price_months (card_id, tier, month, price_cents, observed_at) VALUES
              (1, 5, '2026-07-01', 12000, '2026-08-01T00:00:00Z'),
              (1, 5, '2026-07-01', 12500, '2026-08-10T00:00:00Z'),
              (1, 5, '2026-04-01', 10000, '2026-05-02T00:00:00Z'),
              (1, 5, '2026-08-01', 13000, '2026-08-16T00:00:00Z'),
              (2, 5, '2026-07-01',  5000, '2026-08-01T00:00:00Z'),
              (2, 5, '2026-03-01',  4000, '2026-04-02T00:00:00Z');
            -- Card 3 has no PSA 10 rows at all.

            -- Census: card 1 mature (first obs 1 Jun, grew 100 → 110);
            -- card 2 young (first obs 30 Jul).
            INSERT INTO public.populations (card_id, grader, grade, count, observed_at) VALUES
              (1, 'psa', 10, 100, '2026-06-01T00:00:00Z'),
              (1, 'psa', 10, 110, '2026-08-10T00:00:00Z'),
              (1, 'psa',  9,  40, '2026-06-01T00:00:00Z'),
              (2, 'psa', 10,  20, '2026-07-30T00:00:00Z');

            INSERT INTO public.sales (card_id, source, source_id, grade_tier, title, sold_on, price_cents) VALUES
              (1, 'ebay', 'a1', 'PSA 10', 'Umbreon VMAX', '2026-08-05', 45000),
              (1, 'ebay', 'a2', 'Ungraded', 'Umbreon VMAX', '2026-07-25', 9000),
              (1, 'ebay', 'a3', 'PSA 10', 'Umbreon VMAX', '2026-06-01', 44000),
              (2, 'ebay', 'b1', 'PSA 10', 'Glaceon V', '2021-12-15', 8000);
            """);
    }

    [SkippableFact]
    public async Task The_snapshot_carries_header_facts_and_active_cards_only()
    {
        Skip.IfNot(Available);
        await SeedAsync();

        var snapshot = await Reader().GetAsync(7);

        Assert.NotNull(snapshot);
        Assert.Equal("Evolving Skies", snapshot!.Name);
        Assert.Equal("matched", snapshot.MetadataStatus);
        Assert.Equal("swsh7", snapshot.Code);
        Assert.Equal("SWSH", snapshot.Era);
        Assert.Equal(3, snapshot.CardsTracked);            // the delisted card is out
        Assert.Equal(new DateOnly(2021, 12, 15), snapshot.FirstSale);
        Assert.Equal(3, snapshot.Roster.Count);
        Assert.DoesNotContain(snapshot.Roster, r => r.Name == "Ghost Card");
    }

    [SkippableFact]
    public async Task Latest_price_takes_the_revised_row_and_roc_uses_closed_month_anchors()
    {
        Skip.IfNot(Available);
        await SeedAsync();

        var roster = (await Reader().GetAsync(7))!.Roster;
        var umbreon = roster.Single(r => r.CardId == 1);

        Assert.Equal(13000, umbreon.PriceCents);           // current month IS the latest cell
        Assert.Equal(0.25m, umbreon.Roc3M);                // 12500 (revised Jul) vs 10000 (Apr)
        Assert.Equal(PopulationDeltaState.Available, umbreon.Pop.State);
        Assert.Equal(0.10m, umbreon.Pop.Fraction);
        Assert.Equal(2, umbreon.Sales30d);                 // Aug 5 + Jul 25, all grade labels
    }

    [SkippableFact]
    public async Task Gaps_and_absences_surface_as_nulls_and_states_never_fabrications()
    {
        Skip.IfNot(Available);
        await SeedAsync();

        var roster = (await Reader().GetAsync(7))!.Roster;
        var glaceon = roster.Single(r => r.CardId == 2);
        var leafeon = roster.Single(r => r.CardId == 3);

        Assert.Equal(5000, glaceon.PriceCents);
        Assert.Null(glaceon.Roc3M);                        // Apr cell absent — a real gap
        Assert.Equal(PopulationDeltaState.Pending, glaceon.Pop.State);
        Assert.Equal(new DateOnly(2026, 7, 30), glaceon.Pop.FirstObservedOn);

        Assert.Null(leafeon.PriceCents);                   // no PSA 10 series at all
        Assert.Null(leafeon.Roc3M);
        Assert.Equal(PopulationDeltaState.None, leafeon.Pop.State);
        Assert.Equal(0, leafeon.Sales30d);
    }

    [SkippableFact]
    public async Task An_unknown_set_returns_null()
    {
        Skip.IfNot(Available);
        Assert.Null(await Reader().GetAsync(999));
    }
}
```

(If `sales.id` is NOT NULL without a default in the fixture, add explicit ids to the sales
INSERT. Use the fixture's DDL as the authority, as in Task 1.)

- [ ] **Step 2: Run to verify failure**

Run: `CARDSTOCK_TEST_DB="..." dotnet test tests/CardStock.Infrastructure.Tests --filter SetPageReaderTests -v minimal`
Expected: compile failure — `SetPageReader` missing.

- [ ] **Step 3: Implement the reader**

```csharp
using CardStock.Application.Catalog;
using CardStock.Domain.Census;
using CardStock.Domain.Prices;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Catalog;

/// <summary>Row shape for the DISTINCT ON latest-price query. Shared by every
/// catalog reader (Tasks 5, 13, 16).</summary>
public sealed record LatestPsa10Row(long CardId, int PriceCents);

/// <summary>
/// One set page in bounded queries. Latest-per-key resolves in SQL here — a
/// named deviation from the one-card readers' load-everything shape: 2,531
/// cards × ~113 rows is not the Card page's situation (D-110 spec §3.1).
/// </summary>
public sealed class SetPageReader(
    IDbContextFactory<CardStockDbContext> dbFactory, TimeProvider time) : ISetPageReader
{
    public async Task<SetPageSnapshot?> GetAsync(long setId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var header = await db.ScraperSets.AsNoTracking()
            .Where(s => s.Id == setId)
            .Select(s => new
            {
                s.Id,
                s.Name,
                Detail = db.ScraperSetDetails.SingleOrDefault(d => d.SetId == s.Id),
            })
            .SingleOrDefaultAsync(ct);
        if (header is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        var cards = await db.ScraperCards.AsNoTracking()
            .Where(c => c.SetId == setId && c.DelistedAt == null && c.NotACardAt == null)
            .Select(c => new { c.Id, c.Name, HasImage = c.ImageHash != null })
            .ToListAsync(ct);
        var ids = cards.Select(c => c.Id).ToArray();

        var latest = ids.Length == 0
            ? []
            : await db.Database.SqlQuery<LatestPsa10Row>($"""
                SELECT DISTINCT ON (card_id) card_id AS "CardId", price_cents AS "PriceCents"
                FROM public.price_months
                WHERE tier = 5 AND price_cents > 0 AND card_id = ANY({ids})
                ORDER BY card_id, month DESC, observed_at DESC
                """).ToListAsync(ct);
        var latestByCard = latest.ToDictionary(r => r.CardId, r => r.PriceCents);

        // ROC anchors: the two months the rule reads, latest-per-cell (D-078).
        var m1 = currentMonth.AddMonths(-1);
        var m4 = currentMonth.AddMonths(-4);
        var anchorRows = ids.Length == 0
            ? []
            : await db.Database.SqlQuery<AnchorRow>($"""
                SELECT DISTINCT ON (card_id, month)
                    card_id AS "CardId", month AS "Month", price_cents AS "PriceCents"
                FROM public.price_months
                WHERE tier = 5 AND price_cents > 0 AND card_id = ANY({ids})
                  AND month IN ({m1}, {m4})
                ORDER BY card_id, month, observed_at DESC
                """).ToListAsync(ct);
        var anchorsByCard = anchorRows
            .GroupBy(r => r.CardId)
            .ToDictionary(g => g.Key,
                g => (IReadOnlyDictionary<DateOnly, int>)g.ToDictionary(r => r.Month, r => r.PriceCents));

        var censusRows = await db.ScraperPopulations.AsNoTracking()
            .Where(p => p.Grader == "psa" && p.Grade == 10 && ids.Contains(p.CardId))
            .Select(p => new { p.CardId, p.ObservedAt, p.Count })
            .ToListAsync(ct);
        var censusByCard = censusRows
            .GroupBy(p => p.CardId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PopulationObservation>)g
                .Select(p => new PopulationObservation(
                    DateOnly.FromDateTime(p.ObservedAt.UtcDateTime), p.Count))
                .ToList());

        var salesSince = today.AddDays(-SalesChange.WindowDays);
        var salesCounts = await db.ScraperSales.AsNoTracking()
            .Where(s => ids.Contains(s.CardId) && s.SoldOn >= salesSince)
            .GroupBy(s => s.CardId)
            .Select(g => new { CardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.CardId, g => g.Count, ct);

        var firstSale = await db.ScraperSales.AsNoTracking()
            .Where(s => ids.Contains(s.CardId))
            .Select(s => (DateOnly?)s.SoldOn)
            .MinAsync(ct);

        var roster = cards
            .Select(c => new RosterCard(
                c.Id, c.Name, c.HasImage,
                latestByCard.TryGetValue(c.Id, out var cents) ? cents : null,
                anchorsByCard.TryGetValue(c.Id, out var anchors)
                    ? RosterMath.Roc3M(anchors, currentMonth)
                    : null,
                PopulationDelta.Evaluate(
                    censusByCard.TryGetValue(c.Id, out var census) ? census : [], today),
                salesCounts.GetValueOrDefault(c.Id)))
            .ToList();

        var detail = header.Detail;
        return new SetPageSnapshot(
            header.Id, header.Name,
            detail is { MatchStatus: 0 } ? "matched" : "pending",
            detail?.Code, detail?.Era,
            cards.Count, firstSale, roster);
    }

    private sealed record AnchorRow(long CardId, DateOnly Month, int PriceCents);
}
```

Notes for the implementer: `ids.Contains(...)` translates to `= ANY(@ids)` under Npgsql;
if `SqlQuery` array binding complains, `{ids}` binds a `long[]` parameter natively. The
`month IN ({m1}, {m4})` pair binds two `DateOnly` parameters. If `Min` on an empty set
throws rather than returning null, switch to
`.OrderBy(s => s.SoldOn).Select(...).FirstOrDefaultAsync(ct)` — the test suite decides.

- [ ] **Step 4: Run to verify pass**

Run: `CARDSTOCK_TEST_DB="..." dotnet test tests/CardStock.Infrastructure.Tests --filter SetPageReaderTests -v minimal`
Expected: 4 PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Infrastructure/Catalog/SetPageReader.cs tests/CardStock.Infrastructure.Tests/SetPageReaderTests.cs
git commit -m "catalog: set page reader — latest-per-key in SQL, signal math in Domain"
```

---

### Task 6: API — `GET /api/v1/sets/{id}`

**Files:**
- Create: `src/CardStock.Api/Catalog/CatalogEndpoints.cs`
- Modify: `src/CardStock.Api/Program.cs`
- Modify: `tests/CardStock.Api.Tests/TestApp.cs`
- Test: `tests/CardStock.Api.Tests/SetEndpointTests.cs`

**Interfaces:**
- Consumes: `ISetPageReader`, `SetPageSnapshot`, `CatalogMappers.ToDto` (Tasks 4–5).
- Produces: `GET /api/v1/sets/{id:long}` → 200 `SetPageDto` | 404 ProblemDetails `reason: "unknown"`. `MapCatalogEndpoints()` — Tasks 14 and 18 extend this same file. `TestApp.SetSnapshot` property for endpoint tests.

- [ ] **Step 1: Write the failing endpoint tests**

```csharp
using System.Net;
using System.Net.Http.Json;
using CardStock.Application.Catalog;
using CardStock.Domain.Census;

namespace CardStock.Api.Tests;

public class SetEndpointTests
{
    private static SetPageSnapshot Snapshot() => new(
        7, "Evolving Skies", "matched", "swsh7", "SWSH", 237, new DateOnly(2021, 12, 15),
        [new RosterCard(1, "Umbreon VMAX", true, 45_000, 0.25m,
            new PopulationDelta.Result(PopulationDeltaState.Pending, null,
                new DateOnly(2026, 7, 30), new DateOnly(2026, 9, 28)), 2)]);

    [Fact]
    public async Task A_known_set_serializes_the_dto()
    {
        using var app = new TestApp { SetSnapshot = Snapshot() };
        using var client = app.CreateClient();

        var dto = await client.GetFromJsonAsync<SetPageDto>("/api/v1/sets/7");

        Assert.NotNull(dto);
        Assert.Equal("Evolving Skies", dto!.Name);
        Assert.Equal("2021-12", dto.FirstSaleMonth);
        Assert.Equal("pending", dto.Roster[0].Pop.State);
        Assert.Equal("2026-09-28", dto.Roster[0].Pop.DeltasBeginOn);
    }

    [Fact]
    public async Task An_unknown_set_is_a_404_problem()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/sets/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.Equal("unknown", problem!["reason"].ToString());
    }
}
```

- [ ] **Step 2: Extend TestApp**

In `TestApp`: add the property and stub registration.

```csharp
    public SetPageSnapshot? SetSnapshot { get; set; }
```

In `ConfigureServices`, beside the existing stubs:

```csharp
            services.AddScoped<ISetPageReader>(_ => new StubSetPage(this));
```

And the stub class beside the others:

```csharp
    private sealed class StubSetPage(TestApp app) : ISetPageReader
    {
        public Task<SetPageSnapshot?> GetAsync(long setId, CancellationToken ct = default) =>
            Task.FromResult(app.SetSnapshot?.SetId == setId ? app.SetSnapshot : null);
    }
```

(Add `using CardStock.Application.Catalog;` to TestApp.)

- [ ] **Step 3: Run to verify failure**

Run: `dotnet test tests/CardStock.Api.Tests --filter SetEndpointTests -v minimal`
Expected: FAIL — 404 for both (endpoint unmapped).

- [ ] **Step 4: Implement the endpoint**

`CatalogEndpoints.cs`:

```csharp
using CardStock.Application.Catalog;

namespace CardStock.Api.Catalog;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder routes)
    {
        var api = routes.MapGroup("/api/v1");

        api.MapGet("/sets/{id:long}", async (
            long id, ISetPageReader reader, CancellationToken ct) =>
        {
            var snapshot = await reader.GetAsync(id, ct);
            return snapshot is null ? NotFound() : Results.Ok(CatalogMappers.ToDto(snapshot));
        });

        return routes;
    }

    private static IResult NotFound() => Results.Problem(
        title: "No such entry",
        statusCode: StatusCodes.Status404NotFound,
        extensions: new Dictionary<string, object?> { ["reason"] = "unknown" });
}
```

In `Program.cs`: register the reader beside the card readers and map the group beside
`MapCardEndpoints`:

```csharp
builder.Services.AddScoped<ISetPageReader, SetPageReader>();
```

```csharp
app.MapCatalogEndpoints();
```

(Usings: `CardStock.Api.Catalog`, `CardStock.Application.Catalog`,
`CardStock.Infrastructure.Catalog`.)

- [ ] **Step 5: Run to verify pass, then the whole Api suite**

Run: `dotnet test tests/CardStock.Api.Tests -v minimal`
Expected: PASS, existing suites untouched.

- [ ] **Step 6: Commit**

```bash
git add src/CardStock.Api tests/CardStock.Api.Tests
git commit -m "catalog: GET /api/v1/sets/{id}"
```

---

### Task 7: Web — Format additions and CatalogApiClient

**Files:**
- Modify: `src/CardStock.Web/Services/Format.cs`
- Create: `src/CardStock.Web/Services/CatalogApiClient.cs`
- Modify: `src/CardStock.Web/Program.cs`
- Test: `tests/CardStock.Web.Tests/FormatTests.cs` (extend), `tests/CardStock.Web.Tests/CatalogApiClientTests.cs`

**Interfaces:**
- Consumes: `SetPageDto` (Task 4).
- Produces:
  - `Format.AbbrevMoney(long cents)` — `< $10,000` → `Format.Money` shape; `≥ $10,000` → `$96.4K`; `≥ $1,000,000` → `$1.2M` (one decimal, trailing zero dropped).
  - `Format.MonthYear(string month)` — `"2021-12"` → `"Dec 2021"`.
  - `CatalogResult<T>(T? Value, bool NotFound, bool Failed)` and `CatalogApiClient.GetSetAsync(long id, CancellationToken ct = default)` → `Task<CatalogResult<SetPageDto>>`. Tasks 15/20/21 add `GetCharacterAsync` / `GetBrowseSetsAsync` / `GetBrowseSpeciesAsync` to this same class. `CatalogApiClient.SpeciesIconUrl(int id)` → `"api/v1/species/{id}/icon"`; card art keeps using `CardApiClient.ImageUrl`.

- [ ] **Step 1: Write the failing Format tests** (append to `FormatTests.cs`)

```csharp
    [Theory]
    [InlineData(999_900L, "$9,999")]        // below the 10K floor: full dollars
    [InlineData(1_000_000L, "$10K")]        // exactly $10,000
    [InlineData(9_640_000L, "$96.4K")]
    [InlineData(120_000_000L, "$1.2M")]
    [InlineData(100_000_000L, "$1M")]
    public void AbbrevMoney_abbreviates_at_ten_thousand(long cents, string expected) =>
        Assert.Equal(expected, Format.AbbrevMoney(cents));

    [Fact]
    public void MonthYear_prints_the_full_year() =>
        Assert.Equal("Dec 2021", Format.MonthYear("2021-12"));
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/CardStock.Web.Tests --filter FormatTests -v minimal`
Expected: compile failure.

- [ ] **Step 3: Implement the Format additions**

Append to `Format`:

```csharp
    /// <summary>Header stat tiles abbreviate at ≥$10K (D-110 spec §4): one
    /// decimal, trailing zero dropped. Roster cells always use Money.</summary>
    public static string AbbrevMoney(long cents)
    {
        var dollars = cents / 100m;
        return dollars switch
        {
            >= 1_000_000 => "$" + (dollars / 1_000_000).ToString("0.#",
                System.Globalization.CultureInfo.GetCultureInfo("en-US")) + "M",
            >= 10_000 => "$" + (dollars / 1_000).ToString("0.#",
                System.Globalization.CultureInfo.GetCultureInfo("en-US")) + "K",
            _ => Money((int)cents),
        };
    }

    /// <summary>"2021-12" → "Dec 2021" — the Set header's first-sale line.</summary>
    public static string MonthYear(string month)
    {
        var date = DateOnly.Parse(month.Length == 7 ? month + "-01" : month);
        return date.ToString("MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
    }
```

- [ ] **Step 4: Write the failing client tests**

`CatalogApiClientTests.cs` — mirror `CardApiClientTests`' stub-handler idiom (crib its
`HttpMessageHandler` fake; if it exposes a shared helper, reuse it):

```csharp
using System.Net;
using System.Text;
using CardStock.Application.Catalog;
using CardStock.Web.Services;
using Xunit;

namespace CardStock.Web.Tests;

public class CatalogApiClientTests
{
    private sealed class Stub(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? RequestedPath;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            RequestedPath = request.RequestUri!.PathAndQuery;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    private static CatalogApiClient Client(Stub stub) =>
        new(new HttpClient(stub) { BaseAddress = new Uri("http://localhost/") });

    [Fact]
    public async Task A_set_dto_round_trips()
    {
        var stub = new Stub(HttpStatusCode.OK,
            """{"setId":7,"name":"Evolving Skies","metadataStatus":"matched","code":"swsh7","era":"SWSH","cardsTracked":237,"firstSaleMonth":"2021-12","roster":[]}""");

        var result = await Client(stub).GetSetAsync(7);

        Assert.Equal("/api/v1/sets/7", stub.RequestedPath);
        Assert.False(result.NotFound);
        Assert.False(result.Failed);
        Assert.Equal("Evolving Skies", result.Value!.Name);
    }

    [Fact]
    public async Task A_404_is_NotFound_not_a_failure()
    {
        var result = await Client(new Stub(HttpStatusCode.NotFound,
            """{"reason":"unknown"}""")).GetSetAsync(999);
        Assert.True(result.NotFound);
        Assert.False(result.Failed);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task A_transport_error_is_Failed()
    {
        var throwing = new HttpClient(new ThrowingHandler()) { BaseAddress = new Uri("http://localhost/") };
        var result = await new CatalogApiClient(throwing).GetSetAsync(7);
        Assert.True(result.Failed);
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct) =>
            throw new HttpRequestException("down");
    }
}
```

- [ ] **Step 5: Run to verify failure, implement the client**

Run: `dotnet test tests/CardStock.Web.Tests --filter CatalogApiClientTests -v minimal` — compile failure. Then:

```csharp
using System.Net;
using System.Net.Http.Json;
using CardStock.Application.Catalog;

namespace CardStock.Web.Services;

/// <summary>What any catalog fetch can come back as — the pages' three top states.</summary>
public sealed record CatalogResult<T>(T? Value, bool NotFound, bool Failed) where T : class;

public sealed class CatalogApiClient(HttpClient http)
{
    public Task<CatalogResult<SetPageDto>> GetSetAsync(long id, CancellationToken ct = default) =>
        GetAsync<SetPageDto>($"api/v1/sets/{id}", ct);

    public static string SpeciesIconUrl(int id) => $"api/v1/species/{id}/icon";

    private async Task<CatalogResult<T>> GetAsync<T>(string path, CancellationToken ct)
        where T : class
    {
        try
        {
            using var response = await http.GetAsync(path, ct);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new CatalogResult<T>(null, NotFound: true, Failed: false);
            }

            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<T>(ct);
            return new CatalogResult<T>(dto, NotFound: false, Failed: dto is null);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return new CatalogResult<T>(null, NotFound: false, Failed: true);
        }
    }
}
```

Register in `src/CardStock.Web/Program.cs` beside `CardApiClient`:

```csharp
builder.Services.AddScoped(sp => new CatalogApiClient(sp.GetRequiredService<HttpClient>()));
```

- [ ] **Step 6: Run to verify pass**

Run: `dotnet test tests/CardStock.Web.Tests --filter "CatalogApiClientTests|FormatTests" -v minimal`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add src/CardStock.Web tests/CardStock.Web.Tests
git commit -m "catalog: web client and the tile-abbreviation format rules"
```

---

### Task 8: Web — the D-102 vocabulary components and sort/density controls

Five small pieces of the roster kit. The ◌ markup copies `CensusSentence.razor`'s gate span
exactly (tabindex, title, aria-label) so the vocabulary has one shape everywhere.

**Files:**
- Create: `src/CardStock.Web/Components/Catalog/PendingGlyph.razor` + `.razor.css`
- Create: `src/CardStock.Web/Components/Catalog/DeferredIndexBlock.razor` + `.razor.css`
- Create: `src/CardStock.Web/Components/Catalog/SortState.cs`
- Create: `src/CardStock.Web/Components/Catalog/DensityToggle.razor` + `.razor.css`
- Create: `src/CardStock.Web/Components/Catalog/SortPills.razor` + `.razor.css`
- Test: `tests/CardStock.Web.Tests/CatalogKitTests.cs`

**Interfaces:**
- Consumes: `ChipEngine.GlyphDash` (Domain), app.css tokens (`--card --line --mut --mut2 --acc --mutbg`).
- Produces (Tasks 9, 11, 15, 20 consume these exact shapes):
  - `PendingGlyph` — parameter `Note` (string, required). Renders `<span class="gate-glyph" tabindex="0" title aria-label>◌</span>`.
  - `SortState(string initialKey)` — `Key`, `Descending` (starts true), `Apply(string key)`: same key flips direction, new key resets to descending.
  - `DensityToggle` — parameters `LeftKey/LeftLabel/LeftTooltip`, `RightKey/RightLabel/RightTooltip`, `Value` (string), `ValueChanged` (EventCallback<string>).
  - `SortPill(string Key, string Label, string Tooltip, bool Deferred, string? DeferredTooltip)`; `SortPills` — parameters `Pills` (IReadOnlyList<SortPill>), `Sort` (SortState), `Changed` (EventCallback).
  - `DeferredIndexBlock` — parameter `Caption` (string). Renders the frame, caption + ◌ (note: "Arrives with the analytics worker"), and two delta lines `30D –` / `90D –` with dashes from `GlyphDash`.
  - The worker gate note is the constant `CatalogCopy.WorkerGate = "Arrives with the analytics worker"` — put it in `src/CardStock.Web/Services/CatalogCopy.cs`, created here; every worker-gated tooltip in later tasks uses it.

- [ ] **Step 1: Write the failing bUnit tests**

```csharp
using Bunit;
using CardStock.Domain.Signals;
using CardStock.Web.Components.Catalog;
using CardStock.Web.Services;
using Xunit;

namespace CardStock.Web.Tests;

public class CatalogKitTests : TestContext
{
    [Fact]
    public void PendingGlyph_is_keyboard_reachable_and_carries_its_note()
    {
        var cut = RenderComponent<PendingGlyph>(p => p.Add(x => x.Note, "Arrives with the analytics worker"));
        var span = cut.Find("span.gate-glyph");
        Assert.Equal("◌", span.TextContent);
        Assert.Equal("0", span.GetAttribute("tabindex"));
        Assert.Equal("Arrives with the analytics worker", span.GetAttribute("title"));
        Assert.Equal("Arrives with the analytics worker", span.GetAttribute("aria-label"));
    }

    [Fact]
    public void SortState_flips_on_repeat_and_resets_on_key_change()
    {
        var sort = new SortState("value");
        Assert.True(sort.Descending);
        sort.Apply("value");
        Assert.False(sort.Descending);
        sort.Apply("roc");
        Assert.Equal("roc", sort.Key);
        Assert.True(sort.Descending);
    }

    [Fact]
    public void DensityToggle_marks_the_active_side_with_aria_pressed()
    {
        string value = "terminal";
        var cut = RenderComponent<DensityToggle>(p => p
            .Add(x => x.LeftKey, "terminal").Add(x => x.LeftLabel, "terminal")
            .Add(x => x.LeftTooltip, "Terminal density — more rows, tighter type, every metric column")
            .Add(x => x.RightKey, "binder").Add(x => x.RightLabel, "binder")
            .Add(x => x.RightTooltip, "Binder density — fewer rows with card art")
            .Add(x => x.Value, value)
            .Add(x => x.ValueChanged, v => value = v));

        var buttons = cut.FindAll("button");
        Assert.Equal("true", buttons[0].GetAttribute("aria-pressed"));
        Assert.Equal("false", buttons[1].GetAttribute("aria-pressed"));

        buttons[1].Click();
        Assert.Equal("binder", value);
    }

    [Fact]
    public void A_deferred_pill_is_disabled_with_the_honest_tooltip_and_never_sorts()
    {
        var sort = new SortState("value");
        var pills = new[]
        {
            new SortPill("value", "value", "Sort by value", false, null),
            new SortPill("rs", "RS", "Sort by RS", true, CatalogCopy.WorkerGate),
        };
        var cut = RenderComponent<SortPills>(p => p
            .Add(x => x.Pills, pills).Add(x => x.Sort, sort));

        var rs = cut.FindAll("button")[1];
        Assert.True(rs.HasAttribute("disabled"));
        Assert.Equal(CatalogCopy.WorkerGate, rs.GetAttribute("title"));
        Assert.Equal("value", sort.Key);
    }

    [Fact]
    public void The_deferred_index_block_prints_labels_dashes_and_one_glyph_no_fake_line()
    {
        var cut = RenderComponent<DeferredIndexBlock>(p => p.Add(x => x.Caption, "set index · 12M"));
        Assert.Contains("set index · 12M", cut.Markup);
        Assert.Single(cut.FindAll("span.gate-glyph"));
        Assert.Empty(cut.FindAll("svg polyline"));
        var deltas = cut.FindAll(".dib-delta-value");
        Assert.Equal(2, deltas.Count);
        Assert.All(deltas, d => Assert.Equal(ChipEngine.GlyphDash, d.TextContent));
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/CardStock.Web.Tests --filter CatalogKitTests -v minimal`
Expected: compile failure.

- [ ] **Step 3: Implement the five pieces**

`Services/CatalogCopy.cs`:

```csharp
namespace CardStock.Web.Services;

/// <summary>Gate-note copy, one definition per gate so the vocabulary cannot fork.</summary>
public static class CatalogCopy
{
    public const string WorkerGate = "Arrives with the analytics worker";

    public const string MetadataPending = "Set metadata pending curation";

    public const string YearPending = "Release date pending curation";
}
```

`PendingGlyph.razor`:

```razor
@* The ◌ pending glyph (D-102, brand.md §4): sits beside a gated statistic's
   label, tooltip names the gate, keyboard reachable. Same shape as
   CensusSentence's cs-gate span. *@
<span class="gate-glyph" tabindex="0" title="@Note" aria-label="@Note">◌</span>

@code {
    [Parameter, EditorRequired]
    public string Note { get; set; } = default!;
}
```

`PendingGlyph.razor.css`:

```css
.gate-glyph {
    color: var(--mut2);
    font-size: 12px;
    margin-left: 4px;
    cursor: help;
}
.gate-glyph:focus-visible {
    outline: 2px solid var(--acc);
    outline-offset: 1px;
}
```

`SortState.cs`:

```csharp
namespace CardStock.Web.Components.Catalog;

/// <summary>One sort, two surfaces (set.md §6.1): pills, table headers, and the
/// binder grid all read this single instance, so they can never disagree.</summary>
public sealed class SortState(string initialKey)
{
    public string Key { get; private set; } = initialKey;

    public bool Descending { get; private set; } = true;

    /// <summary>Same key flips direction; a new key always starts descending.</summary>
    public void Apply(string key)
    {
        if (Key == key)
        {
            Descending = !Descending;
        }
        else
        {
            Key = key;
            Descending = true;
        }
    }
}
```

`DensityToggle.razor`:

```razor
<div class="density-toggle" role="group" aria-label="Density">
    <button type="button" title="@LeftTooltip" aria-pressed="@((Value == LeftKey).ToString().ToLowerInvariant())"
            class="@(Value == LeftKey ? "active" : null)"
            @onclick="() => ValueChanged.InvokeAsync(LeftKey)">@LeftLabel</button>
    <button type="button" title="@RightTooltip" aria-pressed="@((Value == RightKey).ToString().ToLowerInvariant())"
            class="@(Value == RightKey ? "active" : null)"
            @onclick="() => ValueChanged.InvokeAsync(RightKey)">@RightLabel</button>
</div>

@code {
    [Parameter, EditorRequired] public string LeftKey { get; set; } = default!;
    [Parameter, EditorRequired] public string LeftLabel { get; set; } = default!;
    [Parameter, EditorRequired] public string LeftTooltip { get; set; } = default!;
    [Parameter, EditorRequired] public string RightKey { get; set; } = default!;
    [Parameter, EditorRequired] public string RightLabel { get; set; } = default!;
    [Parameter, EditorRequired] public string RightTooltip { get; set; } = default!;
    [Parameter] public string Value { get; set; } = "";
    [Parameter] public EventCallback<string> ValueChanged { get; set; }
}
```

`DensityToggle.razor.css` (the prototype's 28px segmented shell):

```css
.density-toggle {
    display: inline-flex;
    border: 1px solid var(--line);
    border-radius: 6px;
    overflow: hidden;
}
.density-toggle button {
    height: 28px;
    padding: 0 12px;
    border: 0;
    background: var(--card);
    color: var(--mut);
    font: 600 13px 'JetBrains Mono', monospace;
    text-transform: lowercase;
    cursor: pointer;
}
.density-toggle button.active {
    background: var(--acc);
    color: var(--card);
}
```

`SortPills.razor`:

```razor
@using CardStock.Web.Services

<div class="sort-pills">
    <span class="sort-label">sort</span>
    @foreach (var pill in Pills)
    {
        var active = !pill.Deferred && Sort.Key == pill.Key;
        if (pill.Deferred)
        {
            <button type="button" class="pill deferred" disabled aria-disabled="true"
                    title="@pill.DeferredTooltip">@pill.Label</button>
        }
        else
        {
            <button type="button" class="pill @(active ? "active" : null)"
                    title="@(active ? pill.Tooltip + " — click again to reverse the order" : pill.Tooltip)"
                    @onclick="() => PickAsync(pill.Key)">@pill.Label</button>
        }
    }
</div>

@code {
    [Parameter, EditorRequired] public IReadOnlyList<SortPill> Pills { get; set; } = default!;
    [Parameter, EditorRequired] public SortState Sort { get; set; } = default!;
    [Parameter] public EventCallback Changed { get; set; }

    private Task PickAsync(string key)
    {
        Sort.Apply(key);
        return Changed.InvokeAsync();
    }
}
```

Add the record at the bottom of `SortState.cs`:

```csharp
/// <summary>A deferred pill renders disabled with its gate tooltip (D-087's
/// control half — controls disable, statistics get the ◌).</summary>
public sealed record SortPill(
    string Key, string Label, string Tooltip, bool Deferred, string? DeferredTooltip);
```

`SortPills.razor.css`:

```css
.sort-pills { display: inline-flex; align-items: center; gap: 6px; }
.sort-label { font: 500 12.5px 'JetBrains Mono', monospace; color: var(--mut2); }
.pill {
    height: 24px;
    padding: 0 10px;
    border: 1px solid var(--line);
    border-radius: 99px;
    background: var(--card);
    color: var(--mut);
    font: 600 12px 'JetBrains Mono', monospace;
    cursor: pointer;
}
.pill.active { background: var(--acc); border-color: var(--acc); color: var(--card); }
.pill.deferred { color: var(--mut3); cursor: not-allowed; }
```

`DeferredIndexBlock.razor`:

```razor
@using CardStock.Domain.Signals
@using CardStock.Web.Services

@* The Set header's index block, fully mocked (D-110 spec §2): frame and caption
   print, one ◌ covers the block (one gate, one glyph), value runs hold the dash.
   No fake polyline — an empty chart area is the honest rendering. *@
<div class="dib">
    <div class="dib-chart" aria-hidden="true"></div>
    <div class="dib-caption">@Caption<PendingGlyph Note="@CatalogCopy.WorkerGate" /></div>
    <div class="dib-deltas">
        <span class="dib-delta"><span class="dib-delta-label">30D</span>
            <span class="dib-delta-value">@ChipEngine.GlyphDash</span></span>
        <span class="dib-delta"><span class="dib-delta-label">90D</span>
            <span class="dib-delta-value">@ChipEngine.GlyphDash</span></span>
    </div>
</div>

@code {
    [Parameter, EditorRequired]
    public string Caption { get; set; } = default!;
}
```

`DeferredIndexBlock.razor.css`:

```css
.dib { width: 220px; }
.dib-chart {
    height: 52px;
    border: 1px dashed var(--line3);
    border-radius: 4px;
    background: var(--mutbg);
}
.dib-caption {
    margin-top: 4px;
    font: 500 11.5px 'JetBrains Mono', monospace;
    color: var(--mut2);
}
.dib-deltas { display: flex; flex-direction: column; align-items: flex-end; gap: 2px; margin-top: 6px; }
.dib-delta-label { font: 500 11px 'JetBrains Mono', monospace; color: var(--mut2); margin-right: 6px; }
.dib-delta-value { font: 700 15px 'JetBrains Mono', monospace; color: var(--mut3); }
```

(`_Imports.razor` in Web already brings component namespaces in via
`@using CardStock.Web.Components.Card` — add `@using CardStock.Web.Components.Catalog`
there now so pages and tests resolve the kit without per-file usings.)

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/CardStock.Web.Tests --filter CatalogKitTests -v minimal`
Expected: 5 PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Web tests/CardStock.Web.Tests/CatalogKitTests.cs
git commit -m "catalog: the dash-and-circle kit — glyph, deferred block, sort state, density, pills"
```

---

### Task 9: Web — RosterTable

The virtualized terminal table both rosters share: column spec in, sorted rows in, grid
markup out. Unsortable columns carry no pointer affordance; deferred columns carry ◌ in the
header and never sort.

**Files:**
- Create: `src/CardStock.Web/Components/Catalog/RosterTable.razor` + `.razor.css`
- Create: `src/CardStock.Web/wwwroot/js/catalog.js`
- Test: `tests/CardStock.Web.Tests/RosterTableTests.cs`

**Interfaces:**
- Consumes: `SortState`, `PendingGlyph` (Task 8).
- Produces (Tasks 11 and 15 consume):
  - `RosterColumn<TRow>(string Key, string Header, int DefaultWidth, string Tooltip, bool Sortable, bool Deferred, string? DeferredTooltip, RenderFragment<TRow> Cell)` — defined in `RosterTable.razor`'s `@code` block? No: **defined in `SortState.cs`'s file? No.** Defined in a new sibling file is overkill; it lives in `RosterTable.razor`'s code-behind section as a top-level generic record — Blazor cannot declare a generic record inside a component, so it lives in `src/CardStock.Web/Components/Catalog/RosterColumn.cs`.
  - `RosterTable<TRow>` — parameters: `Columns` (IReadOnlyList<RosterColumn<TRow>>), `Rows` (IReadOnlyList<TRow>), `Sort` (SortState), `Changed` (EventCallback). Renders header row + `<Virtualize Items="Rows">` data rows; resize grips with the 52px floor.

- [ ] **Step 1: Create `RosterColumn.cs`**

```csharp
using Microsoft.AspNetCore.Components;

namespace CardStock.Web.Components.Catalog;

/// <summary>One terminal-roster column. Deferred ⇒ header carries the ◌ with
/// DeferredTooltip and the column never sorts; Sortable=false ⇒ no pointer, no
/// hover, no dead affordance (D-110 spec §5).</summary>
public sealed record RosterColumn<TRow>(
    string Key,
    string Header,
    int DefaultWidth,
    string Tooltip,
    bool Sortable,
    bool Deferred,
    string? DeferredTooltip,
    RenderFragment<TRow> Cell);
```

- [ ] **Step 2: Write the failing bUnit tests**

```csharp
using Bunit;
using CardStock.Web.Components.Catalog;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace CardStock.Web.Tests;

public class RosterTableTests : TestContext
{
    private sealed record Row(string Name, int Value);

    private static RenderFragment<Row> Text(Func<Row, string> f) =>
        row => builder => builder.AddContent(0, f(row));

    private static IReadOnlyList<RosterColumn<Row>> Columns() =>
    [
        new("name", "Card", 230, "Card name", Sortable: false, Deferred: false, null, Text(r => r.Name)),
        new("value", "PSA 10", 100, "Latest monthly PSA 10 price — click to sort",
            Sortable: true, Deferred: false, null, Text(r => r.Value.ToString())),
        new("rs", "RS pct", 84, "Relative strength", Sortable: false, Deferred: true,
            "Arrives with the analytics worker", Text(_ => "–")),
    ];

    private IRenderedComponent<RosterTable<Row>> Render(SortState sort) =>
        RenderComponent<RosterTable<Row>>(p => p
            .Add(x => x.Columns, Columns())
            .Add(x => x.Rows, new[] { new Row("A", 1), new Row("B", 2) })
            .Add(x => x.Sort, sort));

    [Fact]
    public void A_sortable_header_sorts_and_shows_the_arrow()
    {
        var sort = new SortState("value");
        var cut = Render(sort);
        var header = cut.FindAll(".rt-head-cell")[1];
        Assert.Contains("▾", header.TextContent);

        header.Click();
        Assert.False(sort.Descending);
        Assert.Contains("▴", Render(sort).FindAll(".rt-head-cell")[1].TextContent);
    }

    [Fact]
    public void An_unsortable_header_has_no_pointer_affordance()
    {
        var cut = Render(new SortState("value"));
        var name = cut.FindAll(".rt-head-cell")[0];
        Assert.DoesNotContain("sortable", name.ClassList);
        name.Click();                                   // wired to nothing
        Assert.Equal("value", new SortState("value").Key);
    }

    [Fact]
    public void A_deferred_header_carries_the_glyph_and_never_sorts()
    {
        var sort = new SortState("value");
        var cut = Render(sort);
        var rs = cut.FindAll(".rt-head-cell")[2];
        Assert.Single(rs.QuerySelectorAll("span.gate-glyph"));

        rs.Click();
        Assert.Equal("value", sort.Key);
    }

    [Fact]
    public void The_grid_template_follows_the_column_widths_with_the_name_track_elastic()
    {
        var cut = Render(new SortState("value"));
        var head = cut.Find(".rt-head");
        Assert.Contains("minmax(230px, 1.4fr) 100px 84px", head.GetAttribute("style"));
    }

    [Fact]
    public void Rows_render_through_the_virtualizer()
    {
        var cut = Render(new SortState("value"));
        Assert.Equal(2, cut.FindAll(".rt-row").Count);
        Assert.Contains("A", cut.Markup);
    }
}
```

(bUnit renders `<Virtualize>` items synchronously in its test renderer, so `.rt-row` counts
are directly assertable.)

- [ ] **Step 3: Run to verify failure**

Run: `dotnet test tests/CardStock.Web.Tests --filter RosterTableTests -v minimal`
Expected: compile failure.

- [ ] **Step 4: Implement the table and the JS helper**

`wwwroot/js/catalog.js`:

```javascript
// Column-resize support: pointer capture keeps move events flowing to the grip
// after the cursor leaves it mid-drag. Loaded as a module by RosterTable.
export function capturePointer(element, pointerId) {
    element.setPointerCapture(pointerId);
}
```

`RosterTable.razor`:

```razor
@typeparam TRow
@using Microsoft.AspNetCore.Components.Web.Virtualization
@inject IJSRuntime Js

<div class="roster-table" role="table">
    <div class="rt-head" role="row" style="grid-template-columns: @GridCols()">
        @foreach (var column in Columns)
        {
            <div role="columnheader" class="rt-head-cell @(column.Sortable ? "sortable" : null)"
                 title="@column.Tooltip"
                 tabindex="@(column.Sortable ? "0" : null)"
                 @onclick="() => SortByAsync(column)"
                 @onkeydown="e => HeaderKeyAsync(e, column)">
                @column.Header@(Sort.Key == column.Key && column.Sortable
                    ? Sort.Descending ? " ▾" : " ▴" : "")@if (column.Deferred)
                {<PendingGlyph Note="@(column.DeferredTooltip ?? "")" />}
                <span class="rt-grip" title="Drag to resize"
                      @onpointerdown="e => GripDownAsync(e, column.Key)"
                      @onpointermove="e => GripMove(e, column.Key)"
                      @onpointerup="() => _drag = null"
                      @onclick:stopPropagation>│</span>
            </div>
        }
    </div>
    <div class="rt-body">
        <Virtualize Items="Rows" ItemSize="32" Context="row">
            <div class="rt-row" role="row" style="grid-template-columns: @GridCols()">
                @foreach (var column in Columns)
                {
                    <div role="cell" class="rt-cell rt-cell-@column.Key">@column.Cell(row)</div>
                }
            </div>
        </Virtualize>
    </div>
</div>

@code {
    [Parameter, EditorRequired] public IReadOnlyList<RosterColumn<TRow>> Columns { get; set; } = default!;
    [Parameter, EditorRequired] public IReadOnlyList<TRow> Rows { get; set; } = default!;
    [Parameter, EditorRequired] public SortState Sort { get; set; } = default!;
    [Parameter] public EventCallback Changed { get; set; }

    private const int WidthFloor = 52;
    private Dictionary<string, int> _widths = [];
    private (string Key, double StartX, int StartWidth)? _drag;

    protected override void OnParametersSet()
    {
        foreach (var column in Columns)
        {
            _widths.TryAdd(column.Key, column.DefaultWidth);
        }
    }

    private string GridCols() => string.Join(" ", Columns.Select((c, i) =>
        i == 0 ? $"minmax({_widths[c.Key]}px, 1.4fr)" : $"{_widths[c.Key]}px"));

    private Task SortByAsync(RosterColumn<TRow> column)
    {
        if (!column.Sortable)
        {
            return Task.CompletedTask;
        }

        Sort.Apply(column.Key);
        return Changed.InvokeAsync();
    }

    private Task HeaderKeyAsync(KeyboardEventArgs e, RosterColumn<TRow> column) =>
        e.Key is "Enter" or " " ? SortByAsync(column) : Task.CompletedTask;

    private async Task GripDownAsync(PointerEventArgs e, string key)
    {
        _drag = (key, e.ClientX, _widths[key]);
        try
        {
            await using var module = await Js.InvokeAsync<IJSObjectReference>(
                "import", "./js/catalog.js");
            // Capture on the grip so moves outside it still arrive.
            await module.InvokeVoidAsync("capturePointer",
                (object)e.PointerId is int id ? id : e.PointerId, e.PointerId);
        }
        catch (JSException)
        {
            // No capture (e.g. under test): dragging inside the grip still works.
        }
    }

    private void GripMove(PointerEventArgs e, string key)
    {
        if (_drag is { } drag && drag.Key == key)
        {
            _widths[key] = Math.Max(WidthFloor, drag.StartWidth + (int)(e.ClientX - drag.StartX));
        }
    }
}
```

**Correction to the capture call while implementing:** `setPointerCapture` needs the grip
*element*; pass an `ElementReference` — give the grip `@ref` capture per column via a
`Dictionary<string, ElementReference>` (`@ref="_grips[column.Key]"` fails in a loop — use
`@ref` with a lambda-captured local: assign inside `@onpointerdown` is not possible either).
The workable pattern, and the one to implement: give the grip span
`@ref="_gripRef"` is single — so instead call the module with
`module.InvokeVoidAsync("captureFromEvent", e.PointerId)` and change `catalog.js` to:

```javascript
// The grip that raised the active pointerdown captures its own pointer: the
// event target is still the pressed element when this runs in the same task.
export function captureFromEvent(pointerId) {
    const el = document.querySelector('.rt-grip:hover') ?? document.activeElement;
    if (el && el.setPointerCapture) { el.setPointerCapture(pointerId); }
}
```

That heuristic is fragile — prefer the robust version: attach capture in JS at pointerdown
via one delegated listener installed once per table:

```javascript
export function installGripCapture(tableElement) {
    tableElement.addEventListener('pointerdown', e => {
        const grip = e.target.closest('.rt-grip');
        if (grip) { grip.setPointerCapture(e.pointerId); }
    });
}
```

Use **this** version: `RosterTable` takes `@ref="_root"` on `.roster-table`, and in
`OnAfterRenderAsync(firstRender)` calls
`await module.InvokeVoidAsync("installGripCapture", _root);` (module cached in a field,
disposed in `IAsyncDisposable`). Delete `capturePointer`/`captureFromEvent`; `GripDownAsync`
then only records `_drag`, no JS call. Wrap the interop in `try/catch (JSException)` —
bUnit's JS runtime is strict mode by default; in tests call
`JSInterop.Mode = JSRuntimeMode.Loose;` in the test constructor instead:

```csharp
    public RosterTableTests() => JSInterop.Mode = JSRuntimeMode.Loose;
```

`RosterTable.razor.css`:

```css
.roster-table {
    background: var(--card);
    border: 1px solid var(--line);
    border-radius: 10px;
    overflow-x: auto;
}
.rt-head, .rt-row { display: grid; align-items: center; padding: 6px 16px; }
.rt-head {
    border-bottom: 1px solid var(--line);
    font: 600 12px 'JetBrains Mono', monospace;
    color: var(--mut2);
}
.rt-head-cell { position: relative; text-align: center; user-select: none; }
.rt-head-cell.sortable { cursor: pointer; }
.rt-head-cell.sortable:hover { color: var(--ink); }
.rt-grip {
    position: absolute;
    right: 0;
    top: 0;
    cursor: col-resize;
    color: var(--line3);
    touch-action: none;
}
.rt-row { border-bottom: 1px solid var(--line4); }
.rt-cell { text-align: center; font: 500 12.5px 'JetBrains Mono', monospace; color: var(--mut); }
.rt-cell-name { font: 500 14px 'Inter Tight', sans-serif; color: var(--ink); }
```

- [ ] **Step 5: Run to verify pass**

Run: `dotnet test tests/CardStock.Web.Tests --filter RosterTableTests -v minimal`
Expected: 5 PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CardStock.Web tests/CardStock.Web.Tests/RosterTableTests.cs
git commit -m "catalog: the virtualized roster table — honest headers, 52px floor, one sort"
```

---

### Task 10: Web — BinderGrid

**Files:**
- Create: `src/CardStock.Web/Components/Catalog/BinderGrid.razor` + `.razor.css`
- Test: `tests/CardStock.Web.Tests/BinderGridTests.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces: `BinderGrid<TRow>` — parameters: `Rows` (IReadOnlyList<TRow>), `Href` (Func<TRow, string>), `ArtUrl` (Func<TRow, string?> — null renders no `<img>`, gradient only), `GradientStart`/`GradientEnd` (Func<TRow, string>), `TileBody` (RenderFragment<TRow> — the text under the art), `MinTile` (int, default 180).

- [ ] **Step 1: Write the failing tests**

```csharp
using Bunit;
using CardStock.Web.Components.Catalog;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace CardStock.Web.Tests;

public class BinderGridTests : TestContext
{
    private sealed record Tile(long Id, string Name, bool HasImage);

    private static RenderFragment<Tile> Body() =>
        tile => builder => builder.AddContent(0, tile.Name);

    private IRenderedComponent<BinderGrid<Tile>> Render(params Tile[] tiles) =>
        RenderComponent<BinderGrid<Tile>>(p => p
            .Add(x => x.Rows, tiles)
            .Add(x => x.Href, t => $"card/{t.Id}")
            .Add(x => x.ArtUrl, t => t.HasImage ? $"api/v1/cards/{t.Id}/image" : null)
            .Add(x => x.GradientStart, _ => "#2B2D42")
            .Add(x => x.GradientEnd, _ => "#5C6B9E")
            .Add(x => x.TileBody, Body()));

    [Fact]
    public void The_whole_tile_is_the_link_and_art_lazy_loads()
    {
        var cut = Render(new Tile(630001, "Umbreon VMAX", true));
        var link = cut.Find("a.bg-tile");
        Assert.Equal("card/630001", link.GetAttribute("href"));
        var img = cut.Find(".bg-art img");
        Assert.Equal("lazy", img.GetAttribute("loading"));
        Assert.Contains("api/v1/cards/630001/image", img.GetAttribute("src"));
    }

    [Fact]
    public void A_card_without_art_renders_the_gradient_alone()
    {
        var cut = Render(new Tile(2, "Glaceon V", false));
        Assert.Empty(cut.FindAll(".bg-art img"));
        Assert.Contains("linear-gradient(160deg, #2B2D42, #5C6B9E)",
            cut.Find(".bg-art").GetAttribute("style"));
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/CardStock.Web.Tests --filter BinderGridTests -v minimal`
Expected: compile failure.

- [ ] **Step 3: Implement**

`BinderGrid.razor`:

```razor
@typeparam TRow

@* Binder density (set.md §3.6, character.md §3.6): the whole tile links, art
   sits over the accent gradient so an absent or still-loading image reads as
   the gradient, never a broken slot. onerror hides a 404'd art request the
   same way. *@
<div class="binder-grid" style="grid-template-columns: repeat(auto-fill, minmax(@(MinTile)px, 1fr))">
    @foreach (var row in Rows)
    {
        <a class="bg-tile" href="@Href(row)">
            <div class="bg-art"
                 style="background: linear-gradient(160deg, @GradientStart(row), @GradientEnd(row))">
                @if (ArtUrl(row) is { } art)
                {
                    <img src="@art" alt="" loading="lazy" onerror="this.style.display='none'" />
                }
            </div>
            <div class="bg-body">@TileBody(row)</div>
        </a>
    }
</div>

@code {
    [Parameter, EditorRequired] public IReadOnlyList<TRow> Rows { get; set; } = default!;
    [Parameter, EditorRequired] public Func<TRow, string> Href { get; set; } = default!;
    [Parameter, EditorRequired] public Func<TRow, string?> ArtUrl { get; set; } = default!;
    [Parameter, EditorRequired] public Func<TRow, string> GradientStart { get; set; } = default!;
    [Parameter, EditorRequired] public Func<TRow, string> GradientEnd { get; set; } = default!;
    [Parameter, EditorRequired] public RenderFragment<TRow> TileBody { get; set; } = default!;
    [Parameter] public int MinTile { get; set; } = 180;
}
```

`BinderGrid.razor.css`:

```css
.binder-grid { display: grid; gap: 12px; }
.bg-tile {
    display: block;
    background: var(--card);
    border: 1px solid var(--line);
    border-radius: 10px;
    padding: 10px;
    text-decoration: none;
    color: inherit;
    transition: box-shadow 0.15s;
}
.bg-tile:hover { box-shadow: 0 6px 20px rgba(20, 19, 26, 0.10); }
.bg-art {
    aspect-ratio: 325 / 450;
    border-radius: 5px;
    overflow: hidden;
}
.bg-art img { width: 100%; height: 100%; object-fit: cover; display: block; }
.bg-body { margin-top: 8px; }
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/CardStock.Web.Tests --filter BinderGridTests -v minimal`
Expected: 2 PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Web/Components/Catalog/BinderGrid.razor src/CardStock.Web/Components/Catalog/BinderGrid.razor.css tests/CardStock.Web.Tests/BinderGridTests.cs
git commit -m "catalog: binder grid — art over gradient, whole-tile links"
```

---

### Task 11: Web — the Set page

**Files:**
- Create: `src/CardStock.Web/Pages/SetPage.razor` + `.razor.css`
- Test: `tests/CardStock.Web.Tests/SetPageTests.cs`

**Interfaces:**
- Consumes: `CatalogApiClient.GetSetAsync` (Task 7), `SetPageDto`/`SetRosterRowDto`/`PopDto` (Task 4), the whole kit (Tasks 8–10), `Format`, `ChipEngine.GlyphDash`, `CardApiClient.ImageUrl`, `CatalogCopy`.
- Produces: route `/set/{Id:long}`. Copy constants later tests assert verbatim:
  - Pop pending cell tooltip: `Census too young — first observation {FirstObservedOn}, deltas begin {DeltasBeginOn}` (dates printed through `Dates.Full`-style `MM-DD-YYYY`? **No** — these are wire `yyyy-MM-dd` strings; print them through `Format`-free passthrough is wrong for D-095. Convert: `DateOnly.Parse(...)` then `CardStock.Domain.Dates.Full(...)`).
  - Pop none cell tooltip: `No PSA 10 population observed`.
  - Exclusion banner: `{n} cards excluded from this sort — pop Δ 60d needs two census observations 60 days apart. First observations run {earliest} to {latest}; deltas begin arriving {firstUnlock}.` (all three dates computed from the excluded rows' wire dates, `Dates.Full` formatted).
  - Footer: `Showing all {n} tracked cards · prices are latest monthly PSA 10`.

- [ ] **Step 1: Write the failing page tests**

```csharp
using Bunit;
using CardStock.Application.Catalog;
using CardStock.Web.Pages;
using CardStock.Web.Services;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;   // if the suite's existing idiom differs, use its idiom
using Xunit;

namespace CardStock.Web.Tests;

public class SetPageTests : TestContext
{
    // Follow CardPageTests' established registration idiom for CardApiClient/HttpClient
    // stubs — this file registers CatalogApiClient over a stub handler exactly the same
    // way. The assertions below are the contract; the plumbing mirrors CardPageTests.

    private static SetPageDto Dto(params SetRosterRowDto[] roster) => new(
        7, "Evolving Skies", "matched", "swsh7", "SWSH", 3, "2021-12", roster);

    private static SetRosterRowDto Row(
        long id = 1, string name = "Umbreon VMAX", int? price = 45_000, decimal? roc = 0.25m,
        string popState = "available", decimal? popFraction = 0.10m,
        string? firstObserved = "2026-06-01", string? deltasBegin = null, int sales = 2) =>
        new(id, name, true, price, roc,
            new PopDto(popState, popFraction, firstObserved, deltasBegin), sales);

    [Fact]
    public void The_header_prints_code_uppercase_era_chip_and_first_sale()
    {
        var cut = RenderSetPage(Dto(Row()));
        Assert.Equal("SWSH7", cut.Find(".set-code").TextContent);
        Assert.Equal("SWSH", cut.Find(".set-era").TextContent);
        Assert.Contains("3 cards tracked", cut.Markup);
        Assert.Contains("first sale observed Dec 2021", cut.Markup);
    }

    [Fact]
    public void A_pending_set_renders_one_metadata_chip_with_the_glyph()
    {
        var dto = Dto(Row()) with { MetadataStatus = "pending", Code = null, Era = null };
        var cut = RenderSetPage(dto);
        Assert.Empty(cut.FindAll(".set-code"));
        Assert.Empty(cut.FindAll(".set-era"));
        var chip = cut.Find(".set-meta-pending");
        Assert.Contains("◌", chip.TextContent);
        Assert.Contains("metadata pending", chip.TextContent);
    }

    [Fact]
    public void The_index_block_is_mocked_and_rs_renders_dashes_with_a_header_glyph()
    {
        var cut = RenderSetPage(Dto(Row()));
        Assert.Contains("set index · 12M", cut.Markup);
        var rsHeader = cut.FindAll(".rt-head-cell").Single(h => h.TextContent.Contains("RS pct"));
        Assert.Single(rsHeader.QuerySelectorAll("span.gate-glyph"));
    }

    [Fact]
    public void Pop_states_render_dash_cells_with_computed_tooltips()
    {
        var pending = Row(id: 2, name: "Glaceon V", popState: "pending", popFraction: null,
            firstObserved: "2026-07-30", deltasBegin: "2026-09-28");
        var none = Row(id: 3, name: "Leafeon V", price: null, roc: null,
            popState: "none", popFraction: null, firstObserved: null, sales: 0);
        var cut = RenderSetPage(Dto(Row(), pending, none));

        var pendingCell = cut.FindAll(".rt-cell-pop")[1];
        Assert.Equal("–", pendingCell.TextContent.Trim());
        Assert.Contains("first observation", pendingCell.QuerySelector("[title]")!.GetAttribute("title"));
        var noneCell = cut.FindAll(".rt-cell-pop")[2];
        Assert.Contains("No PSA 10 population observed",
            noneCell.QuerySelector("[title]")!.GetAttribute("title"));
    }

    [Fact]
    public void The_pop_sort_excludes_pending_rows_and_raises_the_banner()
    {
        var pending = Row(id: 2, name: "Glaceon V", popState: "pending", popFraction: null,
            firstObserved: "2026-07-30", deltasBegin: "2026-09-28");
        var cut = RenderSetPage(Dto(Row(), pending));

        cut.FindAll(".sort-pills .pill").Single(p => p.TextContent == "pop Δ").Click();

        Assert.Single(cut.FindAll(".rt-row"));
        var banner = cut.Find(".exclusion-banner");
        Assert.Contains("1 cards excluded", banner.TextContent);
        Assert.Contains("deltas begin arriving", banner.TextContent);
        Assert.Contains("1 of 3 cards", cut.Markup);
    }

    [Fact]
    public void A_negative_pop_delta_renders_a_true_minus_never_a_plus()
    {
        var falling = Row(popState: "available", popFraction: -0.25m);
        var cut = RenderSetPage(Dto(falling));
        Assert.Contains("−25.0%", cut.Find(".rt-cell-pop").TextContent);
    }

    [Fact]
    public void The_footer_owns_the_full_roster_and_the_empty_state_guards()
    {
        var cut = RenderSetPage(Dto(Row()));
        Assert.Contains("Showing all 3 tracked cards", cut.Markup);

        var empty = RenderSetPage(Dto() with { CardsTracked = 0 });
        Assert.Contains("No tracked cards in this set", empty.Markup);
    }
}
```

(`RenderSetPage` is the file's private helper that registers the stubbed `CatalogApiClient`
and renders `SetPage` with `Id=7`, then `cut.WaitForState(...)` until loaded — copy the
exact stub-and-wait plumbing from `CardPageTests`. Pop percent formatting: the pop cell
shows `Fraction` through `Format.ChangePercent` — `-0.25m` → `−25.0%` — with the
supply-warning red applied at `>= +5%` via the `pop-warn` css class, tooltip
`+N.N% PSA 10 census growth over 60 days — rising supply`.)

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/CardStock.Web.Tests --filter SetPageTests -v minimal`
Expected: compile failure.

- [ ] **Step 3: Implement the page**

`SetPage.razor` — the assembled structure; the `@code` block carries the client-side sort
and the copy builders:

```razor
@page "/set/{Id:long}"
@using CardStock.Application.Catalog
@using CardStock.Domain
@using CardStock.Domain.Signals
@using CardStock.Web.Services
@inject CatalogApiClient Api

@if (_result is null)
{
    <p class="loading-strip" aria-busy="true">Loading…</p>
}
else if (_result.Failed)
{
    <div class="card-error">
        <p>Couldn't reach the data service.</p>
        <button type="button" @onclick="LoadAsync">Retry</button>
    </div>
}
else if (_result.NotFound)
{
    <p class="card-not-found">No such set.</p>
}
else
{
    var set = _result.Value!;
    <PageTitle>@set.Name</PageTitle>
    <div class="accent-bar"></div>
    <nav class="breadcrumb"><a href="browse">Browse</a> › @set.Name</nav>

    <header class="set-header">
        <div class="set-identity">
            <div class="set-title-row">
                <h1>@set.Name</h1>
                @if (set.MetadataStatus == "matched")
                {
                    @if (set.Code is { } code)
                    {
                        <span class="set-code">@code.ToUpperInvariant()</span>
                    }
                    @if (set.Era is { } era)
                    {
                        <span class="set-era">@era</span>
                    }
                }
                else
                {
                    <span class="set-meta-pending" tabindex="0"
                          title="@CatalogCopy.MetadataPending">◌ metadata pending</span>
                }
            </div>
            <p class="set-subline"><span class="mono">@set.CardsTracked</span> cards tracked
                @if (set.FirstSaleMonth is { } month)
                {
                    <text> · first sale observed <span class="mono">@Format.MonthYear(month)</span></text>
                }
            </p>
        </div>
        <div class="spacer"></div>
        <DeferredIndexBlock Caption="set index · 12M" />
    </header>

    <div class="set-toolbar">
        <DensityToggle LeftKey="terminal" LeftLabel="terminal"
                       LeftTooltip="Terminal density — more rows, tighter type, every metric column"
                       RightKey="binder" RightLabel="binder"
                       RightTooltip="Binder density — fewer rows with card art"
                       Value="@_view" ValueChanged="v => _view = v" />
        <SortPills Pills="@Pills" Sort="@_sort" Changed="StateHasChanged" />
        <div class="spacer"></div>
        <span class="shown-count mono">@Sorted().Count of @set.CardsTracked cards</span>
    </div>

    @if (_sort.Key == "pop" && Excluded().Count > 0)
    {
        <div class="exclusion-banner">@BannerCopy()</div>
    }

    @if (set.Roster.Count == 0)
    {
        <div class="empty-panel">No tracked cards in this set.</div>
    }
    else if (_view == "terminal")
    {
        <RosterTable TRow="SetRosterRowDto" Columns="@Columns()" Rows="@Sorted()"
                     Sort="@_sort" Changed="StateHasChanged" />
    }
    else
    {
        <BinderGrid TRow="SetRosterRowDto" Rows="@Sorted()"
                    Href="@(r => $"card/{r.CardId}")"
                    ArtUrl="@(r => r.HasImage ? CardApiClient.ImageUrl(r.CardId) : null)"
                    GradientStart="@(_ => "#2B2D42")" GradientEnd="@(_ => "#5C6B9E")">
            <TileBody Context="r">
                <div class="tile-name">@r.Name</div>
                <div class="tile-stats">
                    <span class="mono">@(r.PriceCents is { } c ? Format.Money(c) : ChipEngine.GlyphDash)</span>
                    <span class="mono @RocClass(r.Roc3M)">@(r.Roc3M is { } roc
                        ? Format.ChangePercent(roc) : ChipEngine.GlyphDash)</span>
                </div>
            </TileBody>
        </BinderGrid>
    }

    <p class="set-footer">Showing all @set.Roster.Count tracked cards · prices are latest monthly PSA 10</p>
}
```

The `@code` block:

```csharp
@code {
    [Parameter] public long Id { get; set; }

    private CatalogResult<SetPageDto>? _result;
    private string _view = "terminal";
    private readonly SortState _sort = new("value");

    protected override Task OnParametersSetAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _result = null;
        _result = await Api.GetSetAsync(Id);
    }

    private static readonly IReadOnlyList<SortPill> Pills =
    [
        new("value", "value", "Sort by value", false, null),
        new("roc", "ROC 3M", "Sort by ROC 3M", false, null),
        new("rs", "RS", "Sort by RS", true, CatalogCopy.WorkerGate),
        new("pop", "pop Δ", "Sort by pop Δ", false, null),
        new("sales", "sales/mo", "Sort by observed sales", false, null),
    ];

    private IReadOnlyList<SetRosterRowDto> Included() =>
        _sort.Key == "pop"
            ? _result!.Value!.Roster.Where(r => r.Pop.State == "available").ToList()
            : _result!.Value!.Roster;

    private IReadOnlyList<SetRosterRowDto> Excluded() =>
        _result!.Value!.Roster.Where(r => r.Pop.State != "available").ToList();

    private IReadOnlyList<SetRosterRowDto> Sorted()
    {
        var rows = Included();
        Comparison<SetRosterRowDto> by = _sort.Key switch
        {
            "roc" => (a, b) => Nullable.Compare(a.Roc3M, b.Roc3M),
            "pop" => (a, b) => Nullable.Compare(a.Pop.Fraction, b.Pop.Fraction),
            "sales" => (a, b) => a.Sales30d.CompareTo(b.Sales30d),
            _ => (a, b) => Nullable.Compare(a.PriceCents, b.PriceCents),
        };
        var sorted = rows.ToList();
        sorted.Sort((a, b) => _sort.Descending ? by(b, a) : by(a, b));
        return sorted;
    }

    private string BannerCopy()
    {
        var excluded = Excluded();
        var firsts = excluded
            .Where(r => r.Pop.FirstObservedOn is not null)
            .Select(r => DateOnly.Parse(r.Pop.FirstObservedOn!))
            .ToList();
        var unlocks = excluded
            .Where(r => r.Pop.DeltasBeginOn is not null)
            .Select(r => DateOnly.Parse(r.Pop.DeltasBeginOn!))
            .ToList();
        var range = firsts.Count > 0
            ? $" First observations run {Dates.Full(firsts.Min())} to {Dates.Full(firsts.Max())};" +
              (unlocks.Count > 0 ? $" deltas begin arriving {Dates.Full(unlocks.Min())}." : "")
            : "";
        return $"{excluded.Count} cards excluded from this sort — pop Δ 60d needs two census " +
               $"observations 60 days apart.{range}";
    }

    private static string RocClass(decimal? roc) =>
        roc is null ? "" : roc >= 0 ? "pos" : "neg";

    private IReadOnlyList<RosterColumn<SetRosterRowDto>> Columns() =>
    [
        new("name", "Card", 230, "Card name", false, false, null,
            r => @<a class="row-link" href="card/@r.CardId">@r.Name</a>),
        new("value", "PSA 10", 100, "Latest monthly PSA 10 price — click to sort", true, false, null,
            r => @<text>@(r.PriceCents is { } c ? Format.Money(c) : ChipEngine.GlyphDash)</text>),
        new("roc", "ROC 3M", 92, "3-month rate of change — click to sort", true, false, null,
            r => @<span class="@RocClass(r.Roc3M)">@(r.Roc3M is { } roc
                ? Format.ChangePercent(roc) : ChipEngine.GlyphDash)</span>),
        new("rs", "RS pct", 84, "Relative strength vs market index, percentile", false, true,
            CatalogCopy.WorkerGate, _ => @<text>@ChipEngine.GlyphDash</text>),
        new("pop", "Pop Δ 60d", 96, "PSA 10 census growth over 60 days — click to sort", true, false, null,
            r => @<span title="@PopTip(r.Pop)" class="@PopClass(r.Pop)">@PopText(r.Pop)</span>),
        new("sales", "Sales / mo", 90, "Observed sales in the last 30 days, all grade labels — click to sort",
            true, false, null, r => @<text>@r.Sales30d</text>),
    ];

    private static string PopText(PopDto pop) =>
        pop.State == "available" ? Format.ChangePercent(pop.Fraction!.Value) : ChipEngine.GlyphDash;

    private static string PopClass(PopDto pop) =>
        pop is { State: "available", Fraction: >= 0.05m } ? "pop-warn" : "";

    private static string PopTip(PopDto pop) => pop.State switch
    {
        "available" => $"{Format.ChangePercent(pop.Fraction!.Value)} PSA 10 census growth over " +
                       "60 days — rising supply reads as a warning",
        "pending" => $"Census too young — first observation {FullDate(pop.FirstObservedOn)}, " +
                     $"deltas begin {FullDate(pop.DeltasBeginOn)}",
        _ => "No PSA 10 population observed",
    };

    private static string FullDate(string? iso) =>
        iso is null ? "" : Dates.Full(DateOnly.Parse(iso));
}
```

(`Dates.Full` lives in `CardStock.Domain` — check its parameter type; if it takes
`DateTimeOffset`, add a `DateOnly` overload there rather than formatting locally, one-line
change with a one-line test beside the existing `DatesTests`.)

`SetPage.razor.css` — the header/toolbar/banner/footer chrome:

```css
.accent-bar { height: 4px; border-radius: 2px;
    background: linear-gradient(90deg, #2B2D42, #5C6B9E, #7E6BA8); }
.breadcrumb { font-size: 13.5px; color: var(--mut2); margin: 8px 0; }
.breadcrumb a { color: var(--link); text-decoration: none; }
.set-header { display: flex; align-items: center; gap: 24px; background: var(--card);
    border: 1px solid var(--line); border-radius: 10px; padding: 16px; }
.set-title-row { display: flex; align-items: baseline; gap: 10px; }
.set-title-row h1 { font: 700 26px 'Inter Tight', sans-serif; margin: 0; }
.set-code, .set-era { font: 600 11.5px 'JetBrains Mono', monospace; background: var(--mutbg);
    border: 1px solid var(--line); border-radius: 4px; padding: 2px 7px;
    text-transform: uppercase; letter-spacing: 0.05em; color: var(--mut); }
.set-meta-pending { font: 600 11.5px 'JetBrains Mono', monospace; color: var(--mut2);
    border: 1px dashed var(--line3); border-radius: 4px; padding: 2px 7px; cursor: help; }
.set-subline { color: var(--mut); font-size: 13.5px; margin: 6px 0 0; }
.set-toolbar { display: flex; align-items: center; gap: 14px; margin-top: 4px; }
.shown-count { font-size: 12.5px; color: var(--mut2); }
.exclusion-banner { background: rgba(176, 127, 26, 0.06);
    border: 1px solid rgba(176, 127, 26, 0.25); border-radius: 8px;
    padding: 10px 14px; font-size: 13px; color: var(--warn); }
.empty-panel { background: var(--card); border: 1px solid var(--line); border-radius: 10px;
    padding: 40px; text-align: center; font-size: 14px; color: var(--mut2); }
.set-footer { font-size: 12.5px; color: var(--mut2); }
.spacer { flex: 1; }
.mono { font-family: 'JetBrains Mono', monospace; }
.pos { color: var(--pos); }
.neg { color: var(--neg2); }
.pop-warn { color: var(--neg2); }
.row-link { color: inherit; text-decoration: none; }
.row-link:hover { color: var(--acc); }
.tile-name { font: 600 13.5px 'Inter Tight', sans-serif; white-space: nowrap;
    overflow: hidden; text-overflow: ellipsis; }
.tile-stats { display: flex; justify-content: space-between; margin-top: 4px; font-size: 12.5px; }
```

- [ ] **Step 4: Run to verify pass, then the whole Web suite**

Run: `dotnet test tests/CardStock.Web.Tests -v minimal`
Expected: PASS.

- [ ] **Step 5: Smoke it against the live API**

```bash
cd src/CardStock.Api && dotnet run &
sleep 6
curl -s "http://localhost:5180/api/v1/sets/$(ssh scott@192.168.0.56 "cd /tmp && sudo -u postgres psql -d pokemon -tA -c \"SELECT id FROM sets WHERE name='Evolving Skies' LIMIT 1\"")" | head -c 400
kill %1
```

(Adjust the port to `Properties/launchSettings.json`'s. Expected: a JSON body with the real
roster. This proves the local API against the Pi's database if the local
`appsettings.Development.json` points there; if it does not, skip this step — the deploy
task's receipts cover it.)

- [ ] **Step 6: Commit**

```bash
git add src/CardStock.Web tests/CardStock.Web.Tests/SetPageTests.cs
git commit -m "catalog: the Set page — honest header chips, mocked index, guarded pop sort"
```

---

### Task 12: Application — Character contracts, wire, mapper (chips built server-side)

**Files:**
- Create: `src/CardStock.Application/Catalog/CharacterPageContracts.cs`
- Modify: `src/CardStock.Application/Catalog/CatalogWire.cs`
- Modify: `src/CardStock.Application/Catalog/CatalogMappers.cs`
- Test: `tests/CardStock.Application.Tests/CharacterPageMapperTests.cs`

**Interfaces:**
- Consumes: nothing new.
- Produces:
  - `ICharacterPageReader.GetAsync(string slug, CancellationToken ct = default)` → `Task<CharacterPageSnapshot?>`
  - `CharacterPageSnapshot(int SpeciesId, string Name, string Slug, string GradientStart, string GradientEnd, short Generation, string Region, string Color, string? Habitat, short Status, short Stage, string? EvolvesFrom, IReadOnlyList<string> Types, IReadOnlyList<string> EggGroups, int SetsCount, long TotalValueCents, int PricedPrintings, IReadOnlyList<CharacterRosterCard> Roster)` — `Printings` is `Roster.Count`, not a separate field, so the tile and the roster can never disagree.
  - `CharacterRosterCard(long CardId, string Name, bool HasImage, long SetId, string SetName, short? Year, int? PriceCents, decimal? Roc3M, int Sales30d)`
  - Wire: `CharacterPageDto(int SpeciesId, string Name, string GradientStart, string GradientEnd, IReadOnlyList<ChipDto> Chips, int Printings, int SetsCount, long TotalValueCents, int PricedPrintings, IReadOnlyList<CharacterRosterRowDto> Roster)`; `ChipDto(string Label, string Tooltip)`; `CharacterRosterRowDto(long CardId, string Name, bool HasImage, long SetId, string SetName, short? Year, int? PriceCents, decimal? Roc3M, int Sales30d)`.
  - Chip rules (mapper-owned, the wire never re-words): one chip per type (`Pokédex type`); `Gen {n}` (`First appeared in Generation {n} ({Region})`); stage — `Basic` (`Evolution stage`) or `Stage {n}` (`Evolution stage — evolves from {EvolvesFrom}`); color (`Official Pokédex color`); `{g} egg group` per egg group (`Pokédex egg group`); `{h} habitat` only when Habitat is non-null (`Pokédex habitat`). Region and status get no chip.

- [ ] **Step 1: Write the failing tests**

```csharp
using CardStock.Application.Catalog;
using Xunit;

namespace CardStock.Application.Tests;

public class CharacterPageMapperTests
{
    private static CharacterPageSnapshot Umbreon() => new(
        197, "Umbreon", "umbreon", "#2B2D42", "#5C6B9E",
        Generation: 2, Region: "Johto", Color: "Black", Habitat: "Urban",
        Status: 0, Stage: 1, EvolvesFrom: "Eevee",
        Types: ["Dark"], EggGroups: ["Field"],
        SetsCount: 6, TotalValueCents: 9_640_000, PricedPrintings: 7,
        Roster: [new CharacterRosterCard(1, "Umbreon VMAX", true, 7, "Evolving Skies",
            2021, 45_000, 0.25m, 2)]);

    [Fact]
    public void Six_chip_kinds_in_order_with_the_ruled_tooltips()
    {
        var chips = CatalogMappers.ToDto(Umbreon()).Chips;
        Assert.Equal(
            ["Dark", "Gen 2", "Stage 1", "Black", "Field egg group", "Urban habitat"],
            chips.Select(c => c.Label).ToArray());
        Assert.Equal("First appeared in Generation 2 (Johto)", chips[1].Tooltip);
        Assert.Equal("Evolution stage — evolves from Eevee", chips[2].Tooltip);
    }

    [Fact]
    public void A_null_habitat_omits_the_chip_entirely()
    {
        var gen4 = Umbreon() with { Habitat = null };
        Assert.DoesNotContain(CatalogMappers.ToDto(gen4).Chips, c => c.Label.EndsWith("habitat"));
    }

    [Fact]
    public void Stage_zero_reads_Basic_with_no_parent_clause()
    {
        var basic = Umbreon() with { Stage = 0, EvolvesFrom = null };
        var stage = CatalogMappers.ToDto(basic).Chips.Single(c => c.Label is "Basic");
        Assert.Equal("Evolution stage", stage.Tooltip);
    }

    [Fact]
    public void Dual_types_get_two_chips()
    {
        var dual = Umbreon() with { Types = ["Grass", "Poison"] };
        var labels = CatalogMappers.ToDto(dual).Chips.Select(c => c.Label).ToList();
        Assert.Equal("Grass", labels[0]);
        Assert.Equal("Poison", labels[1]);
    }

    [Fact]
    public void Printings_is_the_roster_count()
    {
        Assert.Equal(1, CatalogMappers.ToDto(Umbreon()).Printings);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/CardStock.Application.Tests --filter CharacterPageMapperTests -v minimal`
Expected: compile failure.

- [ ] **Step 3: Implement**

`CharacterPageContracts.cs`:

```csharp
namespace CardStock.Application.Catalog;

public interface ICharacterPageReader
{
    Task<CharacterPageSnapshot?> GetAsync(string slug, CancellationToken ct = default);
}

/// <summary>One species page: identity, the three live tiles' inputs, the full
/// roster. Printings is Roster.Count by construction.</summary>
public sealed record CharacterPageSnapshot(
    int SpeciesId,
    string Name,
    string Slug,
    string GradientStart,
    string GradientEnd,
    short Generation,
    string Region,
    string Color,
    string? Habitat,
    short Status,
    short Stage,
    string? EvolvesFrom,
    IReadOnlyList<string> Types,
    IReadOnlyList<string> EggGroups,
    int SetsCount,
    long TotalValueCents,
    int PricedPrintings,
    IReadOnlyList<CharacterRosterCard> Roster);

public sealed record CharacterRosterCard(
    long CardId,
    string Name,
    bool HasImage,
    long SetId,
    string SetName,
    short? Year,
    int? PriceCents,
    decimal? Roc3M,
    int Sales30d);
```

Append to `CatalogWire.cs`:

```csharp
public sealed record CharacterPageDto(
    int SpeciesId, string Name, string GradientStart, string GradientEnd,
    IReadOnlyList<ChipDto> Chips, int Printings, int SetsCount,
    long TotalValueCents, int PricedPrintings, IReadOnlyList<CharacterRosterRowDto> Roster);

public sealed record ChipDto(string Label, string Tooltip);

public sealed record CharacterRosterRowDto(
    long CardId, string Name, bool HasImage, long SetId, string SetName, short? Year,
    int? PriceCents, decimal? Roc3M, int Sales30d);
```

Append to `CatalogMappers.cs`:

```csharp
    public static CharacterPageDto ToDto(CharacterPageSnapshot s) => new(
        s.SpeciesId, s.Name, s.GradientStart, s.GradientEnd, Chips(s),
        s.Roster.Count, s.SetsCount, s.TotalValueCents, s.PricedPrintings,
        s.Roster.Select(r => new CharacterRosterRowDto(
            r.CardId, r.Name, r.HasImage, r.SetId, r.SetName, r.Year,
            r.PriceCents, r.Roc3M, r.Sales30d)).ToArray());

    /// <summary>The dex chips (character.md §3.2 as amended by D-110): types,
    /// gen (region in the tooltip — no authored game-pair map), stage, color,
    /// egg group(s), habitat only when it exists. Region and status: no chip.</summary>
    private static IReadOnlyList<ChipDto> Chips(CharacterPageSnapshot s)
    {
        var chips = new List<ChipDto>();
        chips.AddRange(s.Types.Select(t => new ChipDto(t, "Pokédex type")));
        chips.Add(new ChipDto($"Gen {s.Generation}",
            $"First appeared in Generation {s.Generation} ({s.Region})"));
        chips.Add(s.Stage == 0
            ? new ChipDto("Basic", "Evolution stage")
            : new ChipDto($"Stage {s.Stage}",
                s.EvolvesFrom is null
                    ? "Evolution stage"
                    : $"Evolution stage — evolves from {s.EvolvesFrom}"));
        chips.Add(new ChipDto(s.Color, "Official Pokédex color"));
        chips.AddRange(s.EggGroups.Select(g => new ChipDto($"{g} egg group", "Pokédex egg group")));
        if (s.Habitat is { } habitat)
        {
            chips.Add(new ChipDto($"{habitat} habitat", "Pokédex habitat"));
        }

        return chips;
    }
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/CardStock.Application.Tests --filter CharacterPageMapperTests -v minimal`
Expected: 5 PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Application/Catalog tests/CardStock.Application.Tests/CharacterPageMapperTests.cs
git commit -m "catalog: character contracts and the chip rules"
```

---

### Task 13: Infrastructure — CharacterPageReader

**Files:**
- Create: `src/CardStock.Infrastructure/Catalog/CharacterPageReader.cs`
- Test: `tests/CardStock.Infrastructure.Tests/CharacterPageReaderTests.cs`

**Interfaces:**
- Consumes: Task 12's contracts; `LatestPsa10Row`, the same SQL shapes as Task 5; the views (Task 1).
- Produces: `CharacterPageReader : ICharacterPageReader` (registered Task 14). Total value = sum of latest PSA 10 over the species' active cards; `PricedPrintings` = how many of them had a latest price — the D-061 denominator pair. `Year` = `set_details.released_on`'s year where matched, null where pending.

- [ ] **Step 1: Write the failing tests**

```csharp
using CardStock.Application.Catalog;
using CardStock.Infrastructure.Catalog;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardStock.Infrastructure.Tests;

public class CharacterPageReaderTests : CardStockDatabaseTest
{
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    private CharacterPageReader Reader() => new(NewContextFactory(), new FixedTime(Now));

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private async Task SeedAsync()
    {
        await using var db = NewContext();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO public.species
                (id, name, slug, generation, region, color, habitat, status, stage,
                 evolves_from_species_id, gradient_start, gradient_end) VALUES
              (133, 'Eevee', 'eevee', 1, 'Kanto', 'Brown', 'Urban', 0, 0, NULL, '#B98', '#DCA'),
              (197, 'Umbreon', 'umbreon', 2, 'Johto', 'Black', 'Urban', 0, 1, 133, '#2B2D42', '#5C6B9E');
            INSERT INTO public.species_types (species_id, slot, type) VALUES (197, 1, 'Dark');
            INSERT INTO public.species_egg_groups (species_id, egg_group) VALUES (197, 'Field');
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at) VALUES
              (7, 'es', 'Evolving Skies', now(), now()),
              (8, 'jp', 'Pokemon Japanese Promo', now(), now());
            INSERT INTO public.set_details (set_id, match_status, code, released_on, series, era) VALUES
              (7, 0, 'swsh7', '2021-08-27', 'Sword & Shield', 'SWSH'),
              (8, 1, NULL, NULL, NULL, NULL);
            INSERT INTO public.cards (id, set_id, name, url) VALUES
              (1, 7, 'Umbreon VMAX', 'https://x/1'),
              (2, 8, 'Umbreon Promo', 'https://x/2');
            INSERT INTO public.cards (id, set_id, name, url, delisted_at)
            VALUES (3, 7, 'Umbreon Gone', 'https://x/3', now());
            INSERT INTO public.card_species (card_id, species_id, method) VALUES
              (1, 197, 0), (2, 197, 0), (3, 197, 0);
            INSERT INTO public.price_months (card_id, tier, month, price_cents, observed_at) VALUES
              (1, 5, '2026-07-01', 45000, '2026-08-01T00:00:00Z');
            """);
    }

    [SkippableFact]
    public async Task The_species_resolves_by_slug_with_parent_name_and_active_roster()
    {
        Skip.IfNot(Available);
        await SeedAsync();

        var snapshot = await Reader().GetAsync("umbreon");

        Assert.NotNull(snapshot);
        Assert.Equal("Umbreon", snapshot!.Name);
        Assert.Equal("Eevee", snapshot.EvolvesFrom);
        Assert.Equal(["Dark"], snapshot.Types);
        Assert.Equal(2, snapshot.Roster.Count);           // the delisted link is out
        Assert.Equal(2, snapshot.SetsCount);
        Assert.Equal(45_000, snapshot.TotalValueCents);
        Assert.Equal(1, snapshot.PricedPrintings);
    }

    [SkippableFact]
    public async Task Year_comes_from_matched_set_details_and_is_null_when_pending()
    {
        Skip.IfNot(Available);
        await SeedAsync();

        var roster = (await Reader().GetAsync("umbreon"))!.Roster;
        Assert.Equal((short)2021, roster.Single(r => r.CardId == 1).Year);
        Assert.Null(roster.Single(r => r.CardId == 2).Year);
    }

    [SkippableFact]
    public async Task An_unknown_slug_returns_null()
    {
        Skip.IfNot(Available);
        Assert.Null(await Reader().GetAsync("missingno"));
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `CARDSTOCK_TEST_DB="..." dotnet test tests/CardStock.Infrastructure.Tests --filter CharacterPageReaderTests -v minimal`
Expected: compile failure.

- [ ] **Step 3: Implement**

```csharp
using CardStock.Application.Catalog;
using CardStock.Domain.Prices;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Catalog;

/// <summary>One species page. Same bounded-SQL shape as SetPageReader; the
/// junction (card_species) supplies membership, set_details supplies Year.</summary>
public sealed class CharacterPageReader(
    IDbContextFactory<CardStockDbContext> dbFactory, TimeProvider time) : ICharacterPageReader
{
    public async Task<CharacterPageSnapshot?> GetAsync(string slug, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var species = await db.ScraperSpecies.AsNoTracking()
            .SingleOrDefaultAsync(s => s.Slug == slug, ct);
        if (species is null)
        {
            return null;
        }

        var evolvesFrom = species.EvolvesFromSpeciesId is { } parentId
            ? await db.ScraperSpecies.AsNoTracking()
                .Where(s => s.Id == parentId).Select(s => s.Name).SingleOrDefaultAsync(ct)
            : null;
        var types = await db.ScraperSpeciesTypes.AsNoTracking()
            .Where(t => t.SpeciesId == species.Id).OrderBy(t => t.Slot)
            .Select(t => t.Type).ToListAsync(ct);
        var eggGroups = await db.ScraperSpeciesEggGroups.AsNoTracking()
            .Where(g => g.SpeciesId == species.Id).OrderBy(g => g.EggGroup)
            .Select(g => g.EggGroup).ToListAsync(ct);

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        var cards = await db.ScraperCardSpecies.AsNoTracking()
            .Where(link => link.SpeciesId == species.Id)
            .Join(db.ScraperCards.AsNoTracking()
                    .Where(c => c.DelistedAt == null && c.NotACardAt == null),
                link => link.CardId, c => c.Id,
                (_, c) => new { c.Id, c.Name, HasImage = c.ImageHash != null, c.SetId })
            .ToListAsync(ct);
        var ids = cards.Select(c => c.Id).ToArray();

        var setIds = cards.Select(c => c.SetId).Distinct().ToArray();
        var setNames = await db.ScraperSets.AsNoTracking()
            .Where(s => setIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);
        var years = await db.ScraperSetDetails.AsNoTracking()
            .Where(d => setIds.Contains(d.SetId) && d.ReleasedOn != null)
            .ToDictionaryAsync(d => d.SetId, d => (short)d.ReleasedOn!.Value.Year, ct);

        var latestByCard = ids.Length == 0
            ? []
            : (await db.Database.SqlQuery<LatestPsa10Row>($"""
                SELECT DISTINCT ON (card_id) card_id AS "CardId", price_cents AS "PriceCents"
                FROM public.price_months
                WHERE tier = 5 AND price_cents > 0 AND card_id = ANY({ids})
                ORDER BY card_id, month DESC, observed_at DESC
                """).ToListAsync(ct)).ToDictionary(r => r.CardId, r => r.PriceCents);

        var m1 = currentMonth.AddMonths(-1);
        var m4 = currentMonth.AddMonths(-4);
        var anchorsByCard = ids.Length == 0
            ? []
            : (await db.Database.SqlQuery<AnchorRow>($"""
                SELECT DISTINCT ON (card_id, month)
                    card_id AS "CardId", month AS "Month", price_cents AS "PriceCents"
                FROM public.price_months
                WHERE tier = 5 AND price_cents > 0 AND card_id = ANY({ids})
                  AND month IN ({m1}, {m4})
                ORDER BY card_id, month, observed_at DESC
                """).ToListAsync(ct))
            .GroupBy(r => r.CardId)
            .ToDictionary(g => g.Key,
                g => (IReadOnlyDictionary<DateOnly, int>)g.ToDictionary(r => r.Month, r => r.PriceCents));

        var salesSince = today.AddDays(-SalesChange.WindowDays);
        var salesCounts = await db.ScraperSales.AsNoTracking()
            .Where(s => ids.Contains(s.CardId) && s.SoldOn >= salesSince)
            .GroupBy(s => s.CardId)
            .Select(g => new { CardId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.CardId, g => g.Count, ct);

        var roster = cards
            .Select(c => new CharacterRosterCard(
                c.Id, c.Name, c.HasImage, c.SetId, setNames[c.SetId],
                years.TryGetValue(c.SetId, out var year) ? year : null,
                latestByCard.TryGetValue(c.Id, out var cents) ? cents : null,
                anchorsByCard.TryGetValue(c.Id, out var anchors)
                    ? RosterMath.Roc3M(anchors, currentMonth)
                    : null,
                salesCounts.GetValueOrDefault(c.Id)))
            .ToList();

        return new CharacterPageSnapshot(
            species.Id, species.Name, species.Slug, species.GradientStart, species.GradientEnd,
            species.Generation, species.Region, species.Color, species.Habitat,
            species.Status, species.Stage, evolvesFrom, types, eggGroups,
            setIds.Length, roster.Sum(r => (long)(r.PriceCents ?? 0)),
            roster.Count(r => r.PriceCents is not null), roster);
    }

    private sealed record AnchorRow(long CardId, DateOnly Month, int PriceCents);
}
```

- [ ] **Step 4: Run to verify pass**

Run: `CARDSTOCK_TEST_DB="..." dotnet test tests/CardStock.Infrastructure.Tests --filter CharacterPageReaderTests -v minimal`
Expected: 3 PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Infrastructure/Catalog/CharacterPageReader.cs tests/CardStock.Infrastructure.Tests/CharacterPageReaderTests.cs
git commit -m "catalog: character reader — junction membership, set-detail years, D-061 denominators"
```

---

### Task 14: API — `GET /api/v1/characters/{slug}` and `GET /api/v1/species/{id}/icon`

**Files:**
- Modify: `src/CardStock.Api/Catalog/CatalogEndpoints.cs`
- Modify: `src/CardStock.Api/Program.cs`
- Modify: `tests/CardStock.Api.Tests/TestApp.cs`
- Test: `tests/CardStock.Api.Tests/CharacterEndpointTests.cs`, `tests/CardStock.Api.Tests/SpeciesIconEndpointTests.cs`

**Interfaces:**
- Consumes: Tasks 12–13.
- Produces: `GET /api/v1/characters/{slug}` → 200 `CharacterPageDto` | 404 `reason: "unknown"`. `GET /api/v1/species/{id:int}/icon` → 200 `image/png` immutable | 404. Config key `SpeciesIcons:Directory` (throw `InvalidOperationException` when unset, like `ImageStore:Directory`). `TestApp.CharacterSnapshot` and `TestApp.SpeciesIconDirectory`.

- [ ] **Step 1: Write the failing tests**

`CharacterEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using CardStock.Application.Catalog;

namespace CardStock.Api.Tests;

public class CharacterEndpointTests
{
    private static CharacterPageSnapshot Umbreon() => new(
        197, "Umbreon", "umbreon", "#2B2D42", "#5C6B9E", 2, "Johto", "Black", "Urban",
        0, 1, "Eevee", ["Dark"], ["Field"], 6, 9_640_000, 7, []);

    [Fact]
    public async Task A_known_slug_serializes_the_dto_with_chips()
    {
        using var app = new TestApp { CharacterSnapshot = Umbreon() };
        using var client = app.CreateClient();

        var dto = await client.GetFromJsonAsync<CharacterPageDto>("/api/v1/characters/umbreon");

        Assert.Equal("Umbreon", dto!.Name);
        Assert.Equal("Gen 2", dto.Chips[1].Label);
        Assert.Equal(0, dto.Printings);
    }

    [Fact]
    public async Task An_unknown_slug_is_a_404_problem()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        var response = await client.GetAsync("/api/v1/characters/missingno");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
```

`SpeciesIconEndpointTests.cs`:

```csharp
using System.Net;

namespace CardStock.Api.Tests;

public class SpeciesIconEndpointTests
{
    [Fact]
    public async Task A_stored_icon_serves_as_immutable_png()
    {
        using var app = new TestApp();
        File.WriteAllBytes(Path.Combine(app.SpeciesIconDirectory, "197.png"),
            [0x89, 0x50, 0x4E, 0x47]);
        using var client = app.CreateClient();

        var response = await client.GetAsync("/api/v1/species/197/icon");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(response.Headers.CacheControl!.Extensions, e => e.Name == "immutable");
    }

    [Fact]
    public async Task A_missing_icon_is_a_404_the_client_gradient_covers()
    {
        using var app = new TestApp();
        using var client = app.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound,
            (await client.GetAsync("/api/v1/species/9999/icon")).StatusCode);
    }
}
```

- [ ] **Step 2: Extend TestApp**

```csharp
    public CharacterPageSnapshot? CharacterSnapshot { get; set; }

    public string SpeciesIconDirectory { get; } =
        Directory.CreateTempSubdirectory("cardstock-icon-tests-").FullName;
```

`ConfigureWebHost` additions: `builder.UseSetting("SpeciesIcons:Directory", SpeciesIconDirectory);`
plus the stub registration and class:

```csharp
            services.AddScoped<ICharacterPageReader>(_ => new StubCharacter(this));
```

```csharp
    private sealed class StubCharacter(TestApp app) : ICharacterPageReader
    {
        public Task<CharacterPageSnapshot?> GetAsync(string slug, CancellationToken ct = default) =>
            Task.FromResult(app.CharacterSnapshot?.Slug == slug ? app.CharacterSnapshot : null);
    }
```

Also delete `SpeciesIconDirectory` in `Dispose` beside `ImageDirectory`.

- [ ] **Step 3: Run to verify failure**

Run: `dotnet test tests/CardStock.Api.Tests --filter "CharacterEndpointTests|SpeciesIconEndpointTests" -v minimal`
Expected: FAIL (404 everywhere / unset config).

- [ ] **Step 4: Implement the endpoints**

Append inside `MapCatalogEndpoints`, after the sets route:

```csharp
        api.MapGet("/characters/{slug}", async (
            string slug, ICharacterPageReader reader, CancellationToken ct) =>
        {
            var snapshot = await reader.GetAsync(slug, ct);
            return snapshot is null ? NotFound() : Results.Ok(CatalogMappers.ToDto(snapshot));
        });

        // The card-image endpoint's shape (CardsEndpoints.cs): disk is the fact.
        // The id is an int route constraint, so no traversal-shaped value can
        // reach Path.Combine.
        api.MapGet("/species/{id:int}/icon", (
            int id, IConfiguration configuration, HttpContext httpContext) =>
        {
            var directory = configuration["SpeciesIcons:Directory"]
                ?? throw new InvalidOperationException("SpeciesIcons:Directory is not configured.");
            var path = Path.Combine(directory, $"{id}.png");
            if (!File.Exists(path))
            {
                return Results.NotFound();
            }

            httpContext.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            return Results.File(path, "image/png");
        });
```

`Program.cs`: `builder.Services.AddScoped<ICharacterPageReader, CharacterPageReader>();`

- [ ] **Step 5: Run to verify pass, whole Api suite**

Run: `dotnet test tests/CardStock.Api.Tests -v minimal`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/CardStock.Api tests/CardStock.Api.Tests
git commit -m "catalog: character endpoint and the species icon file route"
```

---

### Task 15: Web — the Character page

**Files:**
- Create: `src/CardStock.Web/Pages/CharacterPage.razor` + `.razor.css`
- Modify: `src/CardStock.Web/Services/CatalogApiClient.cs`
- Test: `tests/CardStock.Web.Tests/CharacterPageTests.cs`

**Interfaces:**
- Consumes: `CharacterPageDto` (Task 12), the kit (Tasks 8–10), `CatalogCopy`, `CardApiClient.ImageUrl`, `CatalogApiClient.SpeciesIconUrl`.
- Produces: route `/character/{Slug}`; `CatalogApiClient.GetCharacterAsync(string slug, CancellationToken ct = default)` → `Task<CatalogResult<CharacterPageDto>>` (one line beside `GetSetAsync`: `GetAsync<CharacterPageDto>($"api/v1/characters/{Uri.EscapeDataString(slug)}", ct)`).
- Page rulings this task encodes: binder-first-and-default toggle; four pills `value/year/ROC 3M/sales/mo`; Set cell links `/set/{id}`; Year pending cells; 90d tile deferred; footer copy verbatim: `Prices are latest monthly PSA 10 · a card naming multiple Pokémon in its title appears under every species it names`.

- [ ] **Step 1: Write the failing tests** (same stub plumbing as SetPageTests)

```csharp
using Bunit;
using CardStock.Application.Catalog;
using Xunit;

namespace CardStock.Web.Tests;

public class CharacterPageTests : TestContext
{
    private static CharacterPageDto Dto(params CharacterRosterRowDto[] roster) => new(
        197, "Umbreon", "#2B2D42", "#5C6B9E",
        [new ChipDto("Dark", "Pokédex type"), new ChipDto("Gen 2", "First appeared in Generation 2 (Johto)")],
        roster.Length, 6, 9_640_000, 7, roster);

    private static CharacterRosterRowDto Row(long id = 1, short? year = 2021) => new(
        id, "Umbreon VMAX", true, 7, "Evolving Skies", year, 45_000, 0.25m, 2);

    [Fact]
    public void The_header_carries_the_icon_over_the_gradient_with_initial_fallback()
    {
        var cut = RenderCharacterPage(Dto(Row()));
        var avatar = cut.Find(".char-avatar");
        Assert.Contains("linear-gradient(160deg, #2B2D42, #5C6B9E)", avatar.GetAttribute("style"));
        var img = avatar.QuerySelector("img")!;
        Assert.Contains("api/v1/species/197/icon", img.GetAttribute("src"));
        Assert.Equal("lazy", img.GetAttribute("loading"));
        Assert.Contains("U", avatar.QuerySelector(".char-initial")!.TextContent);
    }

    [Fact]
    public void The_90d_tile_is_deferred_with_dash_and_glyph_and_totals_abbreviate()
    {
        var cut = RenderCharacterPage(Dto(Row()));
        Assert.Contains("$96.4K", cut.Markup);
        var tile = cut.FindAll(".stat-tile").Single(t => t.TextContent.Contains("90D"));
        Assert.Contains("–", tile.TextContent);
        Assert.Single(tile.QuerySelectorAll("span.gate-glyph"));
        var total = cut.FindAll(".stat-tile").Single(t => t.TextContent.Contains("TOTAL VALUE"));
        Assert.Contains("over 7 of 1 printings with a PSA 10 price",
            total.GetAttribute("title") ?? total.QuerySelector("[title]")!.GetAttribute("title"));
    }

    [Fact]
    public void Binder_is_the_default_with_four_pills_reachable()
    {
        var cut = RenderCharacterPage(Dto(Row()));
        Assert.NotEmpty(cut.FindAll(".binder-grid"));
        Assert.Empty(cut.FindAll(".roster-table"));
        var labels = cut.FindAll(".sort-pills .pill").Select(p => p.TextContent).ToList();
        Assert.Equal(["value", "year", "ROC 3M", "sales/mo"], labels);
    }

    [Fact]
    public void The_set_cell_links_and_a_pending_year_renders_the_dash_with_its_tooltip()
    {
        var cut = RenderCharacterPage(Dto(Row(), Row(id: 2, year: null)));
        cut.FindAll(".density-toggle button")[1].Click();   // → terminal

        var setLinks = cut.FindAll(".rt-cell-set a");
        Assert.All(setLinks, a => Assert.Equal("set/7", a.GetAttribute("href")));

        var yearCells = cut.FindAll(".rt-cell-year");
        Assert.Contains("2021", yearCells[0].TextContent);
        Assert.Equal("–", yearCells[1].TextContent.Trim());
        Assert.Equal("Release date pending curation",
            yearCells[1].QuerySelector("[title]")!.GetAttribute("title"));
    }

    [Fact]
    public void The_binder_tile_drops_a_pending_year_without_a_dangling_separator()
    {
        var cut = RenderCharacterPage(Dto(Row(id: 2, year: null)));
        var line = cut.Find(".tile-setline");
        Assert.Equal("Evolving Skies", line.TextContent.Trim());
        Assert.DoesNotContain("·", line.TextContent);
    }

    [Fact]
    public void The_footer_states_the_named_species_rule()
    {
        Assert.Contains("a card naming multiple Pokémon in its title appears under every species it names",
            RenderCharacterPage(Dto(Row())).Markup);
    }
}
```

(`RenderCharacterPage` = the file-local stub helper, same shape as `RenderSetPage`. The
TOTAL VALUE tooltip's denominator uses `Printings` — with one roster row the copy reads
`over 7 of 1 printings…` in this fixture; contrived but pins the format.)

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/CardStock.Web.Tests --filter CharacterPageTests -v minimal`
Expected: compile failure.

- [ ] **Step 3: Implement the page**

Add `GetCharacterAsync` to `CatalogApiClient` (Interfaces block above). `CharacterPage.razor`:

```razor
@page "/character/{Slug}"
@using CardStock.Application.Catalog
@using CardStock.Domain.Signals
@using CardStock.Web.Services
@inject CatalogApiClient Api

@if (_result is null)
{
    <p class="loading-strip" aria-busy="true">Loading…</p>
}
else if (_result.Failed)
{
    <div class="card-error">
        <p>Couldn't reach the data service.</p>
        <button type="button" @onclick="LoadAsync">Retry</button>
    </div>
}
else if (_result.NotFound)
{
    <p class="card-not-found">No such species.</p>
}
else
{
    var character = _result.Value!;
    <PageTitle>@character.Name</PageTitle>
    <div class="accent-bar"
         style="background: linear-gradient(90deg, @character.GradientStart, @character.GradientEnd)"></div>
    <nav class="breadcrumb"><a href="browse">Browse</a> › @character.Name</nav>

    <header class="char-header">
        <div class="char-avatar"
             style="background: linear-gradient(160deg, @character.GradientStart, @character.GradientEnd)">
            <span class="char-initial">@character.Name[..1]</span>
            <img src="@CatalogApiClient.SpeciesIconUrl(character.SpeciesId)" alt=""
                 loading="lazy" onerror="this.style.display='none'" />
        </div>
        <div class="char-identity">
            <h1>@character.Name</h1>
            <div class="char-chips">
                @foreach (var chip in character.Chips)
                {
                    <span class="dex-chip" tabindex="0" title="@chip.Tooltip">@chip.Label</span>
                }
            </div>
        </div>
        <div class="spacer"></div>
        <div class="stat-tiles">
            <div class="stat-tile"><span class="stat-label">PRINTINGS</span>
                <span class="stat-value mono">@character.Printings</span></div>
            <div class="stat-tile"><span class="stat-label">SETS</span>
                <span class="stat-value mono">@character.SetsCount</span></div>
            <div class="stat-tile"
                 title="Sum of latest monthly PSA 10 — over @character.PricedPrintings of @character.Printings printings with a PSA 10 price">
                <span class="stat-label">TOTAL VALUE</span>
                <span class="stat-value mono">@Format.AbbrevMoney(character.TotalValueCents)</span></div>
            <div class="stat-tile"><span class="stat-label">90D<PendingGlyph Note="@CatalogCopy.WorkerGate" /></span>
                <span class="stat-value mono deferred-value">@ChipEngine.GlyphDash</span></div>
        </div>
    </header>

    <div class="set-toolbar">
        <DensityToggle LeftKey="binder" LeftLabel="binder"
                       LeftTooltip="Binder density — fewer rows with card art"
                       RightKey="terminal" RightLabel="terminal"
                       RightTooltip="Terminal density — more rows, tighter type, every metric column"
                       Value="@_view" ValueChanged="v => _view = v" />
        <SortPills Pills="@Pills" Sort="@_sort" Changed="StateHasChanged" />
        <span class="toolbar-sentence">every @character.Name printing we track, all eras</span>
        <div class="spacer"></div>
        <span class="shown-count mono">@character.Roster.Count of @character.Printings printings</span>
    </div>

    @if (character.Roster.Count == 0)
    {
        <div class="empty-panel">No tracked printings for this species.</div>
    }
    else if (_view == "terminal")
    {
        <RosterTable TRow="CharacterRosterRowDto" Columns="@Columns()" Rows="@Sorted()"
                     Sort="@_sort" Changed="StateHasChanged" />
    }
    else
    {
        <BinderGrid TRow="CharacterRosterRowDto" Rows="@Sorted()"
                    Href="@(r => $"card/{r.CardId}")"
                    ArtUrl="@(r => r.HasImage ? CardApiClient.ImageUrl(r.CardId) : null)"
                    GradientStart="@(_ => character.GradientStart)"
                    GradientEnd="@(_ => character.GradientEnd)">
            <TileBody Context="r">
                <div class="tile-name">@r.Name</div>
                <div class="tile-setline mono">@r.SetName@(r.Year is { } year ? $" · {year}" : "")</div>
                <div class="tile-stats">
                    <span class="mono">@(r.PriceCents is { } c ? Format.Money(c) : ChipEngine.GlyphDash)</span>
                    <span class="mono @RocClass(r.Roc3M)">@(r.Roc3M is { } roc
                        ? Format.ChangePercent(roc) : ChipEngine.GlyphDash)</span>
                </div>
            </TileBody>
        </BinderGrid>
    }

    <p class="set-footer">Prices are latest monthly PSA 10 · a card naming multiple Pokémon in its title appears under every species it names</p>
}
```

`@code` block — same skeleton as SetPage with these deltas:

```csharp
@code {
    [Parameter] public string Slug { get; set; } = "";

    private CatalogResult<CharacterPageDto>? _result;
    private string _view = "binder";
    private readonly SortState _sort = new("value");

    protected override Task OnParametersSetAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        _result = null;
        _result = await Api.GetCharacterAsync(Slug);
    }

    private static readonly IReadOnlyList<SortPill> Pills =
    [
        new("value", "value", "Sort by value", false, null),
        new("year", "year", "Sort by release year", false, null),
        new("roc", "ROC 3M", "Sort by ROC 3M", false, null),
        new("sales", "sales/mo", "Sort by observed sales", false, null),
    ];

    private IReadOnlyList<CharacterRosterRowDto> Sorted()
    {
        Comparison<CharacterRosterRowDto> by = _sort.Key switch
        {
            "year" => (a, b) => Nullable.Compare(a.Year, b.Year),
            "roc" => (a, b) => Nullable.Compare(a.Roc3M, b.Roc3M),
            "sales" => (a, b) => a.Sales30d.CompareTo(b.Sales30d),
            _ => (a, b) => Nullable.Compare(a.PriceCents, b.PriceCents),
        };
        var sorted = _result!.Value!.Roster.ToList();
        sorted.Sort((a, b) => _sort.Descending ? by(b, a) : by(a, b));
        return sorted;
    }

    private static string RocClass(decimal? roc) =>
        roc is null ? "" : roc >= 0 ? "pos" : "neg";

    private IReadOnlyList<RosterColumn<CharacterRosterRowDto>> Columns() =>
    [
        new("name", "Card", 230, "Printing name", false, false, null,
            r => @<a class="row-link" href="card/@r.CardId">@r.Name</a>),
        new("set", "Set", 130, "Set this printing belongs to", false, false, null,
            r => @<a class="row-link" href="set/@r.SetId">@r.SetName</a>),
        new("year", "Year", 70, "Release year — click to sort", true, false, null,
            r => r.Year is { } year
                ? @<text>@year</text>
                : @<span title="@CatalogCopy.YearPending">@ChipEngine.GlyphDash</span>),
        new("value", "PSA 10", 100, "Latest monthly PSA 10 price — click to sort", true, false, null,
            r => @<text>@(r.PriceCents is { } c ? Format.Money(c) : ChipEngine.GlyphDash)</text>),
        new("roc", "ROC 3M", 92, "3-month rate of change — click to sort", true, false, null,
            r => @<span class="@RocClass(r.Roc3M)">@(r.Roc3M is { } roc
                ? Format.ChangePercent(roc) : ChipEngine.GlyphDash)</span>),
        new("sales", "Sales / mo", 90, "Observed sales in the last 30 days, all grade labels — click to sort",
            true, false, null, r => @<text>@r.Sales30d</text>),
    ];
}
```

`CharacterPage.razor.css` — avatar, chips, tiles (reuses SetPage's toolbar/footer class
names, which are page-scoped, so copy the shared rules in):

```css
.char-header { display: flex; align-items: center; gap: 20px; background: var(--card);
    border: 1px solid var(--line); border-radius: 10px; padding: 16px; }
.char-avatar { position: relative; width: 64px; height: 64px; border-radius: 50%;
    overflow: hidden; flex-shrink: 0; }
.char-initial { position: absolute; inset: 0; display: grid; place-items: center;
    font: 700 26px 'Inter Tight', sans-serif; color: rgba(255, 255, 255, 0.92); }
.char-avatar img { position: relative; width: 100%; height: 100%; object-fit: contain;
    image-rendering: pixelated; }
.char-identity h1 { font: 700 26px 'Inter Tight', sans-serif; margin: 0 0 6px;
    letter-spacing: -0.01em; }
.char-chips { display: flex; flex-wrap: wrap; gap: 6px; }
.dex-chip { font: 600 11.5px 'JetBrains Mono', monospace; background: var(--mutbg);
    border: 1px solid var(--line); border-radius: 4px; padding: 2px 7px;
    color: var(--mut); cursor: help; }
.stat-tiles { display: flex; gap: 22px; }
.stat-tile { display: flex; flex-direction: column; align-items: flex-end; gap: 2px; }
.stat-label { font: 600 11px 'Inter Tight', sans-serif; letter-spacing: 0.06em;
    color: var(--mut2); }
.stat-value { font: 700 19px 'JetBrains Mono', monospace; }
.deferred-value { color: var(--mut3); }
.toolbar-sentence { font-size: 12.5px; color: var(--mut2); }
/* shared-with-SetPage chrome: copy .accent-bar .breadcrumb .set-toolbar .shown-count
   .empty-panel .set-footer .spacer .mono .pos .neg .row-link .tile-name .tile-stats
   .tile-setline rules here (scoped css does not cross pages). */
.tile-setline { font-size: 11.5px; color: var(--mut2); margin-top: 2px; }
```

- [ ] **Step 4: Run to verify pass, whole Web suite**

Run: `dotnet test tests/CardStock.Web.Tests -v minimal`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Web tests/CardStock.Web.Tests/CharacterPageTests.cs
git commit -m "catalog: the Character page — icon identity, four pills in binder, linking set cells"
```

---

### Task 16: Infrastructure — the aggregate cache

The corpus-wide latest-PSA-10 pass measured 1,427 ms cold on the Pi — far too slow per page
load, so it computes once and serves from memory behind a TTL with single-flight. Corpus
state, not user session state: D-063's "stateless" is intact.

**Files:**
- Create: `src/CardStock.Application/Catalog/BrowseContracts.cs` (the `ICatalogAggregates` half)
- Create: `src/CardStock.Infrastructure/Catalog/CatalogAggregateCache.cs`
- Modify: `src/CardStock.Api/Program.cs`
- Test: `tests/CardStock.Infrastructure.Tests/CatalogAggregateCacheTests.cs`

**Interfaces:**
- Consumes: `LatestPsa10Row` (Task 5).
- Produces:
  - `ICatalogAggregates.LatestPsa10ByCardAsync(CancellationToken ct = default)` → `Task<IReadOnlyDictionary<long, int>>` (in `BrowseContracts.cs`; Task 17 appends `IBrowseReader` to the same file).
  - `CatalogAggregateCache : ICatalogAggregates` — constructor `(IDbContextFactory<CardStockDbContext> dbFactory, TimeProvider time, TimeSpan ttl)`; registered as a **singleton** with TTL from config `Catalog:AggregateCacheMinutes` (default 5).

- [ ] **Step 1: Write the failing tests**

```csharp
using CardStock.Infrastructure.Catalog;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardStock.Infrastructure.Tests;

public class CatalogAggregateCacheTests : CardStockDatabaseTest
{
    private sealed class MutableTime(DateTimeOffset start) : TimeProvider
    {
        public DateTimeOffset Now = start;
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private async Task SeedAsync()
    {
        await using var db = NewContext();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at)
            VALUES (7, 'es', 'Evolving Skies', now(), now());
            INSERT INTO public.cards (id, set_id, name, url) VALUES
              (1, 7, 'A', 'https://x/1'), (2, 7, 'B', 'https://x/2');
            INSERT INTO public.price_months (card_id, tier, month, price_cents, observed_at) VALUES
              (1, 5, '2026-07-01', 12000, '2026-08-01T00:00:00Z'),
              (1, 5, '2026-07-01', 12500, '2026-08-10T00:00:00Z'),
              (2, 0, '2026-07-01',  9999, '2026-08-01T00:00:00Z');
            """);
    }

    [SkippableFact]
    public async Task The_dictionary_holds_the_revised_latest_for_psa10_only()
    {
        Skip.IfNot(Available);
        await SeedAsync();
        var time = new MutableTime(new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.Zero));
        var cache = new CatalogAggregateCache(NewContextFactory(), time, TimeSpan.FromMinutes(5));

        var latest = await cache.LatestPsa10ByCardAsync();

        Assert.Equal(12500, latest[1]);        // the D-078 revision wins
        Assert.False(latest.ContainsKey(2));   // Ungraded tier never enters
    }

    [SkippableFact]
    public async Task Within_the_ttl_the_computation_runs_once_and_after_it_refreshes()
    {
        Skip.IfNot(Available);
        await SeedAsync();
        var time = new MutableTime(new DateTimeOffset(2026, 8, 17, 0, 0, 0, TimeSpan.Zero));
        var cache = new CatalogAggregateCache(NewContextFactory(), time, TimeSpan.FromMinutes(5));

        var first = await cache.LatestPsa10ByCardAsync();
        // A new row lands after the first computation.
        await using (var db = NewContext())
        {
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO public.price_months (card_id, tier, month, price_cents, observed_at)
                VALUES (1, 5, '2026-08-01', 13000, '2026-08-16T00:00:00Z');
                """);
        }

        Assert.Same(first, await cache.LatestPsa10ByCardAsync());   // still cached

        time.Now = time.Now.AddMinutes(6);
        var refreshed = await cache.LatestPsa10ByCardAsync();
        Assert.Equal(13000, refreshed[1]);                          // recomputed
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `CARDSTOCK_TEST_DB="..." dotnet test tests/CardStock.Infrastructure.Tests --filter CatalogAggregateCacheTests -v minimal`
Expected: compile failure.

- [ ] **Step 3: Implement**

`BrowseContracts.cs` (first half):

```csharp
namespace CardStock.Application.Catalog;

/// <summary>Corpus-wide aggregates too slow to compute per request (1,427 ms
/// measured on the Pi, 2026-08-15). Interim until the analytics worker
/// materializes them (D-039); refreshes on a short TTL.</summary>
public interface ICatalogAggregates
{
    Task<IReadOnlyDictionary<long, int>> LatestPsa10ByCardAsync(CancellationToken ct = default);
}
```

`CatalogAggregateCache.cs`:

```csharp
using CardStock.Application.Catalog;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Catalog;

/// <summary>Single-flight, TTL-bound cache of the latest-PSA-10 dictionary.
/// Registered as a singleton; loses nothing on restart but a warm-up.</summary>
public sealed class CatalogAggregateCache(
    IDbContextFactory<CardStockDbContext> dbFactory, TimeProvider time, TimeSpan ttl)
    : ICatalogAggregates
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Task<IReadOnlyDictionary<long, int>>? _current;
    private DateTimeOffset _computedAt;

    public async Task<IReadOnlyDictionary<long, int>> LatestPsa10ByCardAsync(
        CancellationToken ct = default)
    {
        var now = time.GetUtcNow();
        var current = _current;
        if (current is { IsFaulted: false } && now - _computedAt < ttl)
        {
            return await current.WaitAsync(ct);
        }

        await _gate.WaitAsync(ct);
        try
        {
            now = time.GetUtcNow();
            if (_current is { IsFaulted: false } && now - _computedAt < ttl)
            {
                return await _current.WaitAsync(ct);
            }

            // Not the caller's token: one caller's cancellation must not poison
            // the shared computation every other request is waiting on.
            _current = ComputeAsync(CancellationToken.None);
            _computedAt = now;
        }
        finally
        {
            _gate.Release();
        }

        return await _current.WaitAsync(ct);
    }

    private async Task<IReadOnlyDictionary<long, int>> ComputeAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var rows = await db.Database.SqlQuery<LatestPsa10Row>($"""
            SELECT DISTINCT ON (card_id) card_id AS "CardId", price_cents AS "PriceCents"
            FROM public.price_months
            WHERE tier = 5 AND price_cents > 0
            ORDER BY card_id, month DESC, observed_at DESC
            """).ToListAsync(ct);
        return rows.ToDictionary(r => r.CardId, r => r.PriceCents);
    }
}
```

`Program.cs` registration (beside the readers):

```csharp
var aggregateTtl = TimeSpan.FromMinutes(
    builder.Configuration.GetValue("Catalog:AggregateCacheMinutes", 5));
builder.Services.AddSingleton<ICatalogAggregates>(sp => new CatalogAggregateCache(
    sp.GetRequiredService<IDbContextFactory<CardStockDbContext>>(),
    sp.GetRequiredService<TimeProvider>(), aggregateTtl));
```

- [ ] **Step 4: Run to verify pass**

Run: `CARDSTOCK_TEST_DB="..." dotnet test tests/CardStock.Infrastructure.Tests --filter CatalogAggregateCacheTests -v minimal`
Expected: 2 PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Application/Catalog/BrowseContracts.cs src/CardStock.Infrastructure/Catalog/CatalogAggregateCache.cs src/CardStock.Api/Program.cs tests/CardStock.Infrastructure.Tests/CatalogAggregateCacheTests.cs
git commit -m "catalog: the corpus aggregate cache — single flight, five-minute ttl"
```

---

### Task 17: Infrastructure — BrowseReader, tiles, mapper

**Files:**
- Modify: `src/CardStock.Application/Catalog/BrowseContracts.cs`
- Modify: `src/CardStock.Application/Catalog/CatalogWire.cs`
- Modify: `src/CardStock.Application/Catalog/CatalogMappers.cs`
- Create: `src/CardStock.Infrastructure/Catalog/BrowseReader.cs`
- Test: `tests/CardStock.Infrastructure.Tests/BrowseReaderTests.cs`, `tests/CardStock.Application.Tests/BrowseMapperTests.cs`

**Interfaces:**
- Consumes: `ICatalogAggregates` (Task 16), the views (Task 1).
- Produces:
  - `IBrowseReader.GetSetsAsync(CancellationToken ct = default)` → `Task<IReadOnlyList<SetTile>>`; `GetSpeciesAsync(...)` → `Task<IReadOnlyList<SpeciesTile>>`.
  - `SetTile(long SetId, string Name, int Cards, long? TopCardId, string MetadataStatus, string? Era, DateOnly? ReleasedOn)` — `TopCardId` = the set's highest-latest-PSA-10 active card, null when none priced.
  - `SpeciesTile(int SpeciesId, string Name, string Slug, string GradientStart, string GradientEnd, int Printings, long TotalValueCents, IReadOnlyList<string> Types, short Generation, string Region, string Status, short Stage, string Color, IReadOnlyList<string> EggGroups, string? Habitat)` — `Status` already as display text `Ordinary|Legendary|Mythical`; **the species list returns ordered by TotalValueCents descending** (the caption's explicit ORDER BY).
  - Wire: `BrowseSetsDto(IReadOnlyList<SetTileDto> Sets)`, `SetTileDto` mirroring `SetTile` field-for-field (`ReleasedOn` as `DateOnly?` — System.Text.Json handles it); `BrowseSpeciesDto(IReadOnlyList<SpeciesTileDto> Species)`, `SpeciesTileDto` mirroring `SpeciesTile`. `CatalogMappers.ToDto(IReadOnlyList<SetTile>)` / `ToDto(IReadOnlyList<SpeciesTile>)`.

- [ ] **Step 1: Write the failing reader tests**

```csharp
using CardStock.Application.Catalog;
using CardStock.Infrastructure.Catalog;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CardStock.Infrastructure.Tests;

public class BrowseReaderTests : CardStockDatabaseTest
{
    private sealed class FixedAggregates(Dictionary<long, int> latest) : ICatalogAggregates
    {
        public Task<IReadOnlyDictionary<long, int>> LatestPsa10ByCardAsync(
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<long, int>>(latest);
    }

    private async Task SeedAsync()
    {
        await using var db = NewContext();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at) VALUES
              (7, 'es', 'Evolving Skies', now(), now()),
              (8, 'jp', 'Pokemon Japanese Promo', now(), now());
            INSERT INTO public.set_details (set_id, match_status, code, released_on, series, era) VALUES
              (7, 0, 'swsh7', '2021-08-27', 'Sword & Shield', 'SWSH'),
              (8, 1, NULL, NULL, NULL, NULL);
            INSERT INTO public.cards (id, set_id, name, url) VALUES
              (1, 7, 'Umbreon VMAX', 'https://x/1'),
              (2, 7, 'Glaceon V', 'https://x/2'),
              (3, 8, 'Promo', 'https://x/3');
            INSERT INTO public.cards (id, set_id, name, url, not_a_card_at)
            VALUES (4, 7, 'Not A Card', 'https://x/4', now());
            INSERT INTO public.species
                (id, name, slug, generation, region, color, habitat, status, stage,
                 evolves_from_species_id, gradient_start, gradient_end) VALUES
              (197, 'Umbreon', 'umbreon', 2, 'Johto', 'Black', 'Urban', 0, 1, NULL, '#2B2D42', '#5C6B9E'),
              (471, 'Glaceon', 'glaceon', 4, 'Sinnoh', 'Blue', NULL, 0, 1, NULL, '#8AB', '#DEF');
            INSERT INTO public.species_types (species_id, slot, type) VALUES
              (197, 1, 'Dark'), (471, 1, 'Ice');
            INSERT INTO public.species_egg_groups (species_id, egg_group) VALUES
              (197, 'Field'), (471, 'Field');
            INSERT INTO public.card_species (card_id, species_id, method) VALUES
              (1, 197, 0), (2, 471, 0), (3, 197, 0);
            """);
    }

    [SkippableFact]
    public async Task Set_tiles_carry_active_counts_top_card_and_metadata()
    {
        Skip.IfNot(Available);
        await SeedAsync();
        var reader = new BrowseReader(NewContextFactory(),
            new FixedAggregates(new() { [1] = 45000, [2] = 5000 }));

        var sets = await reader.GetSetsAsync();

        var es = sets.Single(s => s.SetId == 7);
        Assert.Equal(2, es.Cards);                 // not_a_card excluded
        Assert.Equal(1, es.TopCardId);             // 45000 beats 5000
        Assert.Equal("SWSH", es.Era);
        Assert.Equal(new DateOnly(2021, 8, 27), es.ReleasedOn);
        var jp = sets.Single(s => s.SetId == 8);
        Assert.Equal("pending", jp.MetadataStatus);
        Assert.Null(jp.TopCardId);                 // its card has no PSA 10 price
    }

    [SkippableFact]
    public async Task Species_tiles_aggregate_the_junction_and_order_by_total_value_desc()
    {
        Skip.IfNot(Available);
        await SeedAsync();
        var reader = new BrowseReader(NewContextFactory(),
            new FixedAggregates(new() { [1] = 45000, [2] = 99000 }));

        var species = await reader.GetSpeciesAsync();

        Assert.Equal([471, 197], species.Select(s => s.SpeciesId).ToArray()); // 99000 > 45000
        var umbreon = species.Single(s => s.SpeciesId == 197);
        Assert.Equal(2, umbreon.Printings);        // cards 1 and 3
        Assert.Equal(45_000, umbreon.TotalValueCents);
        Assert.Equal(["Dark"], umbreon.Types);
        Assert.Equal("Ordinary", umbreon.Status);
        Assert.Null(species.Single(s => s.SpeciesId == 471).Habitat);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `CARDSTOCK_TEST_DB="..." dotnet test tests/CardStock.Infrastructure.Tests --filter BrowseReaderTests -v minimal`
Expected: compile failure.

- [ ] **Step 3: Implement contracts, wire, mapper, reader**

Append to `BrowseContracts.cs`:

```csharp
public interface IBrowseReader
{
    Task<IReadOnlyList<SetTile>> GetSetsAsync(CancellationToken ct = default);

    /// <summary>Ordered by TotalValueCents descending — the Browse caption's
    /// explicit ORDER BY (browse.md §6.3).</summary>
    Task<IReadOnlyList<SpeciesTile>> GetSpeciesAsync(CancellationToken ct = default);
}

public sealed record SetTile(
    long SetId, string Name, int Cards, long? TopCardId,
    string MetadataStatus, string? Era, DateOnly? ReleasedOn);

public sealed record SpeciesTile(
    int SpeciesId, string Name, string Slug, string GradientStart, string GradientEnd,
    int Printings, long TotalValueCents, IReadOnlyList<string> Types, short Generation,
    string Region, string Status, short Stage, string Color,
    IReadOnlyList<string> EggGroups, string? Habitat);
```

Append to `CatalogWire.cs`:

```csharp
public sealed record BrowseSetsDto(IReadOnlyList<SetTileDto> Sets);

public sealed record SetTileDto(
    long SetId, string Name, int Cards, long? TopCardId,
    string MetadataStatus, string? Era, DateOnly? ReleasedOn);

public sealed record BrowseSpeciesDto(IReadOnlyList<SpeciesTileDto> Species);

public sealed record SpeciesTileDto(
    int SpeciesId, string Name, string Slug, string GradientStart, string GradientEnd,
    int Printings, long TotalValueCents, IReadOnlyList<string> Types, short Generation,
    string Region, string Status, short Stage, string Color,
    IReadOnlyList<string> EggGroups, string? Habitat);
```

Append to `CatalogMappers.cs`:

```csharp
    public static BrowseSetsDto ToDto(IReadOnlyList<SetTile> sets) => new(
        sets.Select(s => new SetTileDto(
            s.SetId, s.Name, s.Cards, s.TopCardId, s.MetadataStatus, s.Era, s.ReleasedOn))
        .ToArray());

    public static BrowseSpeciesDto ToDto(IReadOnlyList<SpeciesTile> species) => new(
        species.Select(s => new SpeciesTileDto(
            s.SpeciesId, s.Name, s.Slug, s.GradientStart, s.GradientEnd, s.Printings,
            s.TotalValueCents, s.Types, s.Generation, s.Region, s.Status, s.Stage,
            s.Color, s.EggGroups, s.Habitat))
        .ToArray());
```

`BrowseMapperTests.cs` — two straight-through assertions (one per mapper) that a tile's
fields survive; write them first if working strictly test-first within this step.

`BrowseReader.cs`:

```csharp
using CardStock.Application.Catalog;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Catalog;

/// <summary>Both Browse walls. Counts and joins are cheap GROUP BYs; anything
/// touching latest prices rides the aggregate cache's dictionary in memory.</summary>
public sealed class BrowseReader(
    IDbContextFactory<CardStockDbContext> dbFactory, ICatalogAggregates aggregates) : IBrowseReader
{
    public async Task<IReadOnlyList<SetTile>> GetSetsAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var latest = await aggregates.LatestPsa10ByCardAsync(ct);

        var sets = await db.ScraperSets.AsNoTracking()
            .Select(s => new
            {
                s.Id,
                s.Name,
                Detail = db.ScraperSetDetails.SingleOrDefault(d => d.SetId == s.Id),
            })
            .ToListAsync(ct);

        var activeCards = await db.ScraperCards.AsNoTracking()
            .Where(c => c.DelistedAt == null && c.NotACardAt == null)
            .Select(c => new { c.Id, c.SetId })
            .ToListAsync(ct);

        var bySet = activeCards.ToLookup(c => c.SetId);

        return sets
            .Select(s =>
            {
                var members = bySet[s.Id].ToList();
                var top = members
                    .Where(c => latest.ContainsKey(c.Id))
                    .OrderByDescending(c => latest[c.Id])
                    .Select(c => (long?)c.Id)
                    .FirstOrDefault();
                return new SetTile(
                    s.Id, s.Name, members.Count, top,
                    s.Detail is { MatchStatus: 0 } ? "matched" : "pending",
                    s.Detail?.Era, s.Detail?.ReleasedOn);
            })
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<SpeciesTile>> GetSpeciesAsync(CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var latest = await aggregates.LatestPsa10ByCardAsync(ct);

        var species = await db.ScraperSpecies.AsNoTracking().ToListAsync(ct);
        var types = (await db.ScraperSpeciesTypes.AsNoTracking()
                .OrderBy(t => t.Slot).ToListAsync(ct))
            .ToLookup(t => t.SpeciesId, t => t.Type);
        var eggGroups = (await db.ScraperSpeciesEggGroups.AsNoTracking().ToListAsync(ct))
            .ToLookup(g => g.SpeciesId, g => g.EggGroup);

        var links = await db.ScraperCardSpecies.AsNoTracking()
            .Join(db.ScraperCards.AsNoTracking()
                    .Where(c => c.DelistedAt == null && c.NotACardAt == null),
                link => link.CardId, c => c.Id,
                (link, c) => new { link.SpeciesId, c.Id })
            .ToListAsync(ct);
        var bySpecies = links.ToLookup(l => l.SpeciesId, l => l.Id);

        return species
            .Select(s =>
            {
                var cards = bySpecies[s.Id].ToList();
                return new SpeciesTile(
                    s.Id, s.Name, s.Slug, s.GradientStart, s.GradientEnd,
                    cards.Count,
                    cards.Sum(id => (long)latest.GetValueOrDefault(id)),
                    types[s.Id].ToList(), s.Generation, s.Region,
                    s.Status switch { 1 => "Legendary", 2 => "Mythical", _ => "Ordinary" },
                    s.Stage, s.Color, eggGroups[s.Id].ToList(), s.Habitat);
            })
            .OrderByDescending(s => s.TotalValueCents)
            .ToList();
    }
}
```

- [ ] **Step 4: Run to verify pass**

Run: `CARDSTOCK_TEST_DB="..." dotnet test tests/CardStock.Infrastructure.Tests --filter BrowseReaderTests -v minimal && dotnet test tests/CardStock.Application.Tests --filter BrowseMapperTests -v minimal`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Application/Catalog src/CardStock.Infrastructure/Catalog/BrowseReader.cs tests/CardStock.Infrastructure.Tests/BrowseReaderTests.cs tests/CardStock.Application.Tests/BrowseMapperTests.cs
git commit -m "catalog: browse reader — both walls from cheap joins plus the cached dictionary"
```

---

### Task 18: API — `GET /api/v1/browse/sets` and `/api/v1/browse/species`

**Files:**
- Modify: `src/CardStock.Api/Catalog/CatalogEndpoints.cs`
- Modify: `src/CardStock.Api/Program.cs`
- Modify: `tests/CardStock.Api.Tests/TestApp.cs`
- Test: `tests/CardStock.Api.Tests/BrowseEndpointTests.cs`

**Interfaces:**
- Consumes: Task 17.
- Produces: the two GET routes, both 200-always (empty lists are honest empties, never 404).

- [ ] **Step 1: Tests, stubs, endpoints — one cycle**

Tests:

```csharp
using System.Net.Http.Json;
using CardStock.Application.Catalog;

namespace CardStock.Api.Tests;

public class BrowseEndpointTests
{
    [Fact]
    public async Task Sets_and_species_serialize_their_tiles()
    {
        using var app = new TestApp
        {
            BrowseSets = [new SetTile(7, "Evolving Skies", 237, 1, "matched", "SWSH",
                new DateOnly(2021, 8, 27))],
            BrowseSpecies = [new SpeciesTile(197, "Umbreon", "umbreon", "#2B2D42", "#5C6B9E",
                34, 9_640_000, ["Dark"], 2, "Johto", "Ordinary", 1, "Black", ["Field"], "Urban")],
        };
        using var client = app.CreateClient();

        var sets = await client.GetFromJsonAsync<BrowseSetsDto>("/api/v1/browse/sets");
        Assert.Equal("Evolving Skies", sets!.Sets[0].Name);
        Assert.Equal(new DateOnly(2021, 8, 27), sets.Sets[0].ReleasedOn);

        var species = await client.GetFromJsonAsync<BrowseSpeciesDto>("/api/v1/browse/species");
        Assert.Equal("Umbreon", species!.Species[0].Name);
        Assert.Equal("Urban", species.Species[0].Habitat);
    }
}
```

TestApp: properties `public IReadOnlyList<SetTile> BrowseSets { get; set; } = [];` and
`public IReadOnlyList<SpeciesTile> BrowseSpecies { get; set; } = [];`, stub:

```csharp
    private sealed class StubBrowse(TestApp app) : IBrowseReader
    {
        public Task<IReadOnlyList<SetTile>> GetSetsAsync(CancellationToken ct = default) =>
            Task.FromResult(app.BrowseSets);

        public Task<IReadOnlyList<SpeciesTile>> GetSpeciesAsync(CancellationToken ct = default) =>
            Task.FromResult(app.BrowseSpecies);
    }
```

registered `services.AddScoped<IBrowseReader>(_ => new StubBrowse(this));`. Endpoints:

```csharp
        api.MapGet("/browse/sets", async (IBrowseReader reader, CancellationToken ct) =>
            Results.Ok(CatalogMappers.ToDto(await reader.GetSetsAsync(ct))));

        api.MapGet("/browse/species", async (IBrowseReader reader, CancellationToken ct) =>
            Results.Ok(CatalogMappers.ToDto(await reader.GetSpeciesAsync(ct))));
```

`Program.cs`: `builder.Services.AddScoped<IBrowseReader, BrowseReader>();`

- [ ] **Step 2: Verify red → green, then commit**

Run: `dotnet test tests/CardStock.Api.Tests -v minimal` — PASS.

```bash
git add src/CardStock.Api tests/CardStock.Api.Tests
git commit -m "catalog: the two browse endpoints"
```

---

### Task 19: Web — SetShelves, SpeciesFilters, SetGradients (pure engines)

The Browse page's brains, kept out of razor so they test as plain code.

**Files:**
- Create: `src/CardStock.Web/Services/SetShelves.cs`
- Create: `src/CardStock.Web/Services/SpeciesFilters.cs`
- Create: `src/CardStock.Web/Services/SetGradients.cs`
- Test: `tests/CardStock.Web.Tests/SetShelvesTests.cs`, `tests/CardStock.Web.Tests/SpeciesFiltersTests.cs`

**Interfaces:**
- Consumes: `SetTileDto`, `SpeciesTileDto` (Task 17).
- Produces:
  - `SetShelves.Shelf(string Title, IReadOnlyList<SetTileDto> Sets)`.
  - `SetShelves.Alphabetical(IReadOnlyList<SetTileDto>)` → one unshelved ordered list.
  - `SetShelves.ByReleaseDate(IReadOnlyList<SetTileDto>)` → `IReadOnlyList<Shelf>`: one `"By release date"` shelf (dated sets ascending), then `"{n} sets awaiting metadata — alphabetical"`.
  - `SetShelves.ByEra(IReadOnlyList<SetTileDto>)` → shelves = distinct eras ordered by each era's earliest `ReleasedOn`, sets within a shelf by `ReleasedOn` ascending; then `"no era"` (matched, era null, date-ordered, only when non-empty); then `"metadata pending"` (pending, alphabetical, only when non-empty).
  - `SpeciesFilters.Attributes` — 8 `FilterAttribute(string Key, string DisplayName, ...)` in order `type, gen, region, status, stage, color, egg, habitat`; `SpeciesFilters.Matches(SpeciesTileDto s, IReadOnlyDictionary<string, IReadOnlySet<string>> active)` — AND across attributes, OR within one; `SpeciesFilters.Options(string key, IReadOnlyList<SpeciesTileDto> all)` → ordered `(string Value, string Label)` pairs; `SpeciesFilters.Label(string key, string value)`.
  - `SetGradients.For(long setId)` → `(string Start, string End)` — deterministic pick from the authored 12-pair palette below.

- [ ] **Step 1: Write the failing tests**

`SetShelvesTests.cs`:

```csharp
using CardStock.Application.Catalog;
using CardStock.Web.Services;
using Xunit;

namespace CardStock.Web.Tests;

public class SetShelvesTests
{
    private static SetTileDto Tile(long id, string name, string status = "matched",
        string? era = null, string? released = null) => new(
        id, name, 100, null, status, era,
        released is null ? null : DateOnly.Parse(released));

    private static readonly SetTileDto[] Sets =
    [
        Tile(1, "Base Set", era: "WOTC", released: "1999-01-09"),
        Tile(2, "Evolving Skies", era: "SWSH", released: "2021-08-27"),
        Tile(3, "Brilliant Stars", era: "SWSH", released: "2022-02-25"),
        Tile(4, "POP Series 5", era: null, released: "2006-03-01"),          // matched, no era
        Tile(5, "Pokemon Japanese Promo", status: "pending"),
        Tile(6, "Aquapolis", status: "pending"),
    ];

    [Fact]
    public void Era_shelves_are_data_driven_chronological_with_the_two_tails()
    {
        var shelves = SetShelves.ByEra(Sets);
        Assert.Equal(["WOTC", "SWSH", "no era", "metadata pending"],
            shelves.Select(s => s.Title).ToArray());
        Assert.Equal([2L, 3L], shelves[1].Sets.Select(t => t.SetId).ToArray()); // date order
        Assert.Equal([4L], shelves[2].Sets.Select(t => t.SetId).ToArray());
        Assert.Equal(["Aquapolis", "Pokemon Japanese Promo"],
            shelves[3].Sets.Select(t => t.Name).ToArray());                    // alphabetical
    }

    [Fact]
    public void Empty_tails_do_not_render()
    {
        var shelves = SetShelves.ByEra([Tile(1, "Base Set", era: "WOTC", released: "1999-01-09")]);
        Assert.Equal(["WOTC"], shelves.Select(s => s.Title).ToArray());
    }

    [Fact]
    public void Release_order_puts_dated_first_then_the_labeled_pending_block()
    {
        var shelves = SetShelves.ByReleaseDate(Sets);
        Assert.Equal([1L, 4L, 2L, 3L], shelves[0].Sets.Select(t => t.SetId).ToArray());
        Assert.Equal("2 sets awaiting metadata — alphabetical", shelves[1].Title);
    }

    [Fact]
    public void Alphabetical_is_case_insensitive_and_total()
    {
        var ordered = SetShelves.Alphabetical(Sets);
        Assert.Equal(6, ordered.Count);
        Assert.Equal("Aquapolis", ordered[0].Name);
    }
}
```

`SpeciesFiltersTests.cs`:

```csharp
using CardStock.Application.Catalog;
using CardStock.Web.Services;
using Xunit;

namespace CardStock.Web.Tests;

public class SpeciesFiltersTests
{
    private static SpeciesTileDto Species(int id, string name, string[] types, short gen,
        string region, string status = "Ordinary", short stage = 1, string color = "Black",
        string[]? eggs = null, string? habitat = "Urban") => new(
        id, name, name.ToLowerInvariant(), "#000", "#FFF", 10, 1000, types, gen, region,
        status, stage, color, eggs ?? ["Field"], habitat);

    private static readonly SpeciesTileDto[] All =
    [
        Species(197, "Umbreon", ["Dark"], 2, "Johto"),
        Species(1, "Bulbasaur", ["Grass", "Poison"], 1, "Kanto", stage: 0, color: "Green",
            eggs: ["Monster", "Grass"], habitat: "Grassland"),
        Species(471, "Glaceon", ["Ice"], 4, "Sinnoh", habitat: null),
    ];

    [Fact]
    public void And_across_attributes_or_within_one()
    {
        var active = new Dictionary<string, IReadOnlySet<string>>
        {
            ["type"] = new HashSet<string> { "Grass", "Dark" },
            ["gen"] = new HashSet<string> { "1" },
        };
        Assert.Equal(["Bulbasaur"],
            All.Where(s => SpeciesFilters.Matches(s, active)).Select(s => s.Name).ToArray());
    }

    [Fact]
    public void A_multi_valued_attribute_matches_on_either_value()
    {
        var active = new Dictionary<string, IReadOnlySet<string>>
        {
            ["type"] = new HashSet<string> { "Poison" },
        };
        Assert.Single(All.Where(s => SpeciesFilters.Matches(s, active)));
    }

    [Fact]
    public void Species_without_a_habitat_match_no_habitat_value()
    {
        var active = new Dictionary<string, IReadOnlySet<string>>
        {
            ["habitat"] = new HashSet<string> { "Urban" },
        };
        Assert.DoesNotContain(All.Where(s => SpeciesFilters.Matches(s, active)),
            s => s.Name == "Glaceon");
    }

    [Fact]
    public void Stage_labels_read_Basic_then_Stage_N()
    {
        Assert.Equal("Basic", SpeciesFilters.Label("stage", "0"));
        Assert.Equal("Stage 2", SpeciesFilters.Label("stage", "2"));
        Assert.Equal("Gen 4", SpeciesFilters.Label("gen", "4"));
    }

    [Fact]
    public void The_eight_attributes_come_in_the_prototype_order()
    {
        Assert.Equal(["type", "gen", "region", "status", "stage", "color", "egg", "habitat"],
            SpeciesFilters.Attributes.Select(a => a.Key).ToArray());
    }

    [Fact]
    public void Region_options_order_by_generation_not_alphabet()
    {
        var options = SpeciesFilters.Options("region", All).Select(o => o.Value).ToArray();
        Assert.Equal(["Kanto", "Johto", "Sinnoh"], options);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/CardStock.Web.Tests --filter "SetShelvesTests|SpeciesFiltersTests" -v minimal`
Expected: compile failure.

- [ ] **Step 3: Implement the three engines**

`SetShelves.cs`:

```csharp
using CardStock.Application.Catalog;

namespace CardStock.Web.Services;

/// <summary>Browse set-mode ordering (D-110 spec §6): shelves are data-driven —
/// never a hard-coded era list — with two honest tail shelves.</summary>
public static class SetShelves
{
    public sealed record Shelf(string Title, IReadOnlyList<SetTileDto> Sets);

    public static IReadOnlyList<SetTileDto> Alphabetical(IReadOnlyList<SetTileDto> sets) =>
        sets.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();

    public static IReadOnlyList<Shelf> ByReleaseDate(IReadOnlyList<SetTileDto> sets)
    {
        var dated = sets.Where(s => s.ReleasedOn is not null)
            .OrderBy(s => s.ReleasedOn).ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var undated = sets.Where(s => s.ReleasedOn is null)
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var shelves = new List<Shelf> { new("By release date", dated) };
        if (undated.Count > 0)
        {
            shelves.Add(new($"{undated.Count} sets awaiting metadata — alphabetical", undated));
        }

        return shelves;
    }

    public static IReadOnlyList<Shelf> ByEra(IReadOnlyList<SetTileDto> sets)
    {
        var shelves = sets
            .Where(s => s.Era is not null)
            .GroupBy(s => s.Era!)
            .OrderBy(g => g.Min(s => s.ReleasedOn ?? DateOnly.MaxValue))
            .Select(g => new Shelf(g.Key, g
                .OrderBy(s => s.ReleasedOn ?? DateOnly.MaxValue)
                .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()))
            .ToList();

        var noEra = sets.Where(s => s is { Era: null, MetadataStatus: "matched" })
            .OrderBy(s => s.ReleasedOn ?? DateOnly.MaxValue)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (noEra.Count > 0)
        {
            shelves.Add(new("no era", noEra));
        }

        var pending = sets.Where(s => s.MetadataStatus != "matched")
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (pending.Count > 0)
        {
            shelves.Add(new("metadata pending", pending));
        }

        return shelves;
    }
}
```

`SpeciesFilters.cs`:

```csharp
using CardStock.Application.Catalog;

namespace CardStock.Web.Services;

/// <summary>One Browse filter attribute: raw key (the chips' terminal voice),
/// display name (the popover menu), value extraction, option ordering.</summary>
public sealed record FilterAttribute(
    string Key,
    string DisplayName,
    Func<SpeciesTileDto, IEnumerable<string>> ValuesOf,
    Func<IReadOnlyList<SpeciesTileDto>, IEnumerable<string>> OrderedValues);

/// <summary>The 8-attribute algebra (browse.md §3.4–§3.6, spec §6): AND across
/// attributes, OR within one — a Grass/Poison species matches a type filter on
/// either value. Vocabularies derive from the complete species list, which is
/// the whole table, never from a filtered page.</summary>
public static class SpeciesFilters
{
    private static readonly string[] RegionOrder =
        ["Kanto", "Johto", "Hoenn", "Sinnoh", "Unova", "Kalos", "Alola", "Galar", "Paldea"];

    private static readonly string[] StatusOrder = ["Ordinary", "Legendary", "Mythical"];

    public static readonly IReadOnlyList<FilterAttribute> Attributes =
    [
        new("type", "Type", s => s.Types,
            all => all.SelectMany(s => s.Types).Distinct().Order()),
        new("gen", "Generation", s => [s.Generation.ToString()],
            all => all.Select(s => s.Generation).Distinct().Order().Select(g => g.ToString())),
        new("region", "Region", s => [s.Region],
            all => RegionOrder.Where(r => all.Any(s => s.Region == r))),
        new("status", "Status", s => [s.Status],
            all => StatusOrder.Where(v => all.Any(s => s.Status == v))),
        new("stage", "Evolution stage", s => [s.Stage.ToString()],
            all => all.Select(s => s.Stage).Distinct().Order().Select(v => v.ToString())),
        new("color", "Pokédex color", s => [s.Color],
            all => all.Select(s => s.Color).Distinct().Order()),
        new("egg", "Egg group", s => s.EggGroups,
            all => all.SelectMany(s => s.EggGroups).Distinct().Order()),
        new("habitat", "Habitat", s => s.Habitat is null ? [] : [s.Habitat],
            all => all.Where(s => s.Habitat is not null).Select(s => s.Habitat!).Distinct().Order()),
    ];

    public static bool Matches(
        SpeciesTileDto species, IReadOnlyDictionary<string, IReadOnlySet<string>> active) =>
        active.All(filter => Attributes.Single(a => a.Key == filter.Key)
            .ValuesOf(species).Any(filter.Value.Contains));

    public static IReadOnlyList<(string Value, string Label)> Options(
        string key, IReadOnlyList<SpeciesTileDto> all) =>
        Attributes.Single(a => a.Key == key).OrderedValues(all)
            .Select(v => (v, Label(key, v))).ToList();

    public static string Label(string key, string value) => key switch
    {
        "gen" => $"Gen {value}",
        "stage" => value == "0" ? "Basic" : $"Stage {value}",
        _ => value,
    };
}
```

`SetGradients.cs`:

```csharp
namespace CardStock.Web.Services;

/// <summary>Deterministic accent pairs for set fan tiles — sets carry no stored
/// gradient (species do). Twelve muted pairs in the prototype's palette family;
/// the same set always draws the same pair.</summary>
public static class SetGradients
{
    private static readonly (string Start, string End)[] Palette =
    [
        ("#2B2D42", "#5C6B9E"), ("#3A4A5A", "#7E92A8"), ("#4A3A5A", "#8A7BA8"),
        ("#2D4238", "#5C9E7E"), ("#42352D", "#9E7E5C"), ("#2D3D42", "#5C8E9E"),
        ("#3D2D42", "#8E5C9E"), ("#42402D", "#9E965C"), ("#2D4242", "#5C9E9E"),
        ("#422D33", "#9E5C6B"), ("#33422D", "#6B9E5C"), ("#8A9BB8", "#D6E0EC"),
    ];

    public static (string Start, string End) For(long setId) =>
        Palette[(int)((ulong)setId % (ulong)Palette.Length)];
}
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/CardStock.Web.Tests --filter "SetShelvesTests|SpeciesFiltersTests" -v minimal`
Expected: PASS (10 tests).

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Web/Services tests/CardStock.Web.Tests/SetShelvesTests.cs tests/CardStock.Web.Tests/SpeciesFiltersTests.cs
git commit -m "catalog: browse engines — data-driven shelves, the eight-attribute algebra, set gradients"
```

---

### Task 20: Web — BrowsePage, by-set mode

**Files:**
- Create: `src/CardStock.Web/Pages/BrowsePage.razor` + `.razor.css`
- Modify: `src/CardStock.Web/Services/CatalogApiClient.cs`
- Test: `tests/CardStock.Web.Tests/BrowsePageSetsTests.cs`

**Interfaces:**
- Consumes: Tasks 17–19 + the kit.
- Produces: route `/browse` with `?mode=` (`sets` default, `pokemon`); `CatalogApiClient.GetBrowseSetsAsync()` / `GetBrowseSpeciesAsync()` (one line each over `GetAsync<T>`). Task 21 adds the pokémon mode into this same page.

- [ ] **Step 1: Write the failing tests**

```csharp
using Bunit;
using CardStock.Application.Catalog;
using Xunit;

namespace CardStock.Web.Tests;

public class BrowsePageSetsTests : TestContext
{
    private static SetTileDto Tile(long id, string name, int cards = 100, long? top = null,
        string status = "matched", string? era = "SWSH", string? released = "2021-08-27") => new(
        id, name, cards, top, status, era, released is null ? null : DateOnly.Parse(released));

    [Fact]
    public void Sets_mode_is_the_default_with_the_alphabetical_wall()
    {
        var cut = RenderBrowse(sets: [Tile(2, "Evolving Skies"), Tile(1, "Base Set", era: "WOTC")]);
        var names = cut.FindAll(".fan-tile .fan-name").Select(n => n.TextContent).ToList();
        Assert.Equal(["Base Set", "Evolving Skies"], names);
    }

    [Fact]
    public void A_tile_carries_count_deferred_delta_and_the_top_cards_art()
    {
        var cut = RenderBrowse(sets: [Tile(2, "Evolving Skies", cards: 237, top: 630001)]);
        var tile = cut.Find(".fan-tile");
        Assert.Contains("237 cards", tile.TextContent);
        Assert.Contains("30d", tile.TextContent);
        Assert.Contains("–", tile.TextContent);
        Assert.Single(tile.QuerySelectorAll("span.gate-glyph"));
        Assert.Contains("api/v1/cards/630001/image",
            tile.QuerySelector(".fan-front img")!.GetAttribute("src"));
        Assert.Equal("browse-set-link", tile.QuerySelectorAll("a").First().ClassName);
        Assert.Equal("set/2", tile.QuerySelector("a")!.GetAttribute("href"));
    }

    [Fact]
    public void The_era_order_renders_shelf_headings_with_the_tails()
    {
        var cut = RenderBrowse(sets:
        [
            Tile(1, "Base Set", era: "WOTC", released: "1999-01-09"),
            Tile(4, "POP Series 5", era: null, released: "2006-03-01"),
            Tile(5, "Japanese Promo", status: "pending", era: null, released: null),
        ]);

        cut.FindAll(".order-pills .pill").Single(p => p.TextContent == "era").Click();

        var headings = cut.FindAll(".shelf-title").Select(h => h.TextContent).ToList();
        Assert.Equal(["WOTC", "no era", "metadata pending"], headings);
    }

    [Fact]
    public void A_pending_tile_shows_the_metadata_chip_in_era_view()
    {
        var cut = RenderBrowse(sets: [Tile(5, "Japanese Promo", status: "pending", era: null, released: null)]);
        cut.FindAll(".order-pills .pill").Single(p => p.TextContent == "era").Click();
        Assert.Contains("◌ metadata pending", cut.Find(".shelf-title + .set-grid .fan-tile").TextContent);
    }
}
```

(`RenderBrowse(sets:, species:)` — the file's stub helper; species defaults to an empty
list. Register a `FakeNavigationManager` per bUnit's built-in to assert the mode-URL write
in Task 21.)

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/CardStock.Web.Tests --filter BrowsePageSetsTests -v minimal`
Expected: compile failure.

- [ ] **Step 3: Implement the page (sets half)**

Client additions:

```csharp
    public Task<CatalogResult<BrowseSetsDto>> GetBrowseSetsAsync(CancellationToken ct = default) =>
        GetAsync<BrowseSetsDto>("api/v1/browse/sets", ct);

    public Task<CatalogResult<BrowseSpeciesDto>> GetBrowseSpeciesAsync(CancellationToken ct = default) =>
        GetAsync<BrowseSpeciesDto>("api/v1/browse/species", ct);
```

`BrowsePage.razor` (sets half; the `@* POKEMON MODE — Task 21 *@` marker is where the next
task inserts):

```razor
@page "/browse"
@using CardStock.Application.Catalog
@using CardStock.Domain.Signals
@using CardStock.Web.Services
@inject CatalogApiClient Api
@inject NavigationManager Nav

<PageTitle>Browse</PageTitle>

<div class="browse-head">
    <h1>Browse</h1>
    <div class="mode-switch density-toggle" role="group" aria-label="Browse mode">
        <button type="button" aria-pressed="@((!IsPokemon).ToString().ToLowerInvariant())"
                class="@(!IsPokemon ? "active" : null)"
                title="Browse by set — every release, its size, and its market value"
                @onclick='() => SetMode("sets")'>by set</button>
        <button type="button" aria-pressed="@(IsPokemon.ToString().ToLowerInvariant())"
                class="@(IsPokemon ? "active" : null)"
                title="Browse by Pokémon — every species and all of its printings"
                @onclick='() => SetMode("pokemon")'>by pokémon</button>
    </div>
    @if (!IsPokemon)
    {
        <div class="order-pills sort-pills">
            <span class="sort-label">order</span>
            @foreach (var (key, label) in OrderOptions)
            {
                <button type="button" class="pill @(_setOrder == key ? "active" : null)"
                        title="@OrderTooltip(key)" @onclick="() => _setOrder = key">@label</button>
            }
        </div>
    }
</div>

@if (!IsPokemon)
{
    @if (_sets is null)
    {
        <p class="loading-strip" aria-busy="true">Loading…</p>
    }
    else if (_sets.Failed)
    {
        <div class="card-error"><p>Couldn't reach the data service.</p>
            <button type="button" @onclick="LoadAsync">Retry</button></div>
    }
    else
    {
        @foreach (var shelf in Shelves())
        {
            @if (shelf.Title is not null)
            {
                <h2 class="shelf-title">@shelf.Title</h2>
            }
            <div class="set-grid">
                @foreach (var set in shelf.Sets)
                {
                    <a class="browse-set-link fan-tile" href="set/@set.SetId" title="@set.Name">
                        <div class="fan">
                            <div class="fan-card fan-back-left"
                                 style="background: linear-gradient(160deg, @SetGradients.For(set.SetId + 1).Start, @SetGradients.For(set.SetId + 1).End)"></div>
                            <div class="fan-card fan-back-right"
                                 style="background: linear-gradient(160deg, @SetGradients.For(set.SetId + 2).Start, @SetGradients.For(set.SetId + 2).End)"></div>
                            <div class="fan-card fan-front"
                                 style="background: linear-gradient(160deg, @SetGradients.For(set.SetId).Start, @SetGradients.For(set.SetId).End)">
                                @if (set.TopCardId is { } topCard)
                                {
                                    <img src="@CardApiClient.ImageUrl(topCard)" alt="" loading="lazy"
                                         onerror="this.style.display='none'" />
                                }
                            </div>
                        </div>
                        <div class="fan-name">@set.Name</div>
                        <div class="fan-stats mono">
                            <span class="fan-count">@set.Cards cards</span>
                            <span class="fan-delta">@ChipEngine.GlyphDash 30d<PendingGlyph Note="@CatalogCopy.WorkerGate" /></span>
                        </div>
                        @if (_setOrder == "era" && set.MetadataStatus != "matched")
                        {
                            <div class="fan-pending">◌ metadata pending</div>
                        }
                    </a>
                }
            </div>
        }
    }
}
@* POKEMON MODE — Task 21 *@
```

`@code` (sets half):

```csharp
@code {
    [SupplyParameterFromQuery(Name = "mode")]
    public string? Mode { get; set; }

    private bool IsPokemon => Mode == "pokemon";

    private CatalogResult<BrowseSetsDto>? _sets;
    private CatalogResult<BrowseSpeciesDto>? _species;
    private string _setOrder = "az";

    private static readonly (string Key, string Label)[] OrderOptions =
        [("az", "a–z"), ("date", "release date"), ("era", "era")];

    private static string OrderTooltip(string key) => key switch
    {
        "date" => "Dated sets chronologically; sets awaiting metadata follow alphabetically",
        "era" => "Era shelves in chronological order, with the no-era and metadata-pending shelves last",
        _ => "Every set, alphabetically",
    };

    protected override Task OnParametersSetAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        if (IsPokemon)
        {
            _species ??= await Api.GetBrowseSpeciesAsync();
        }
        else
        {
            _sets ??= await Api.GetBrowseSetsAsync();
        }
    }

    private void SetMode(string mode) =>
        Nav.NavigateTo(Nav.GetUriWithQueryParameter("mode", mode == "sets" ? null : mode));

    private IReadOnlyList<(string? Title, IReadOnlyList<SetTileDto> Sets)> Shelves()
    {
        var sets = _sets!.Value!.Sets;
        return _setOrder switch
        {
            "date" => SetShelves.ByReleaseDate(sets).Select(s => ((string?)s.Title, s.Sets)).ToList(),
            "era" => SetShelves.ByEra(sets).Select(s => ((string?)s.Title, s.Sets)).ToList(),
            _ => [(null, SetShelves.Alphabetical(sets))],
        };
    }
}
```

`BrowsePage.razor.css` — the fan geometry from browse.md §2.2 (74×102 backs at ±rotation,
78×108 front) plus grid/shelf chrome:

```css
.browse-head { display: flex; align-items: center; gap: 14px; }
.browse-head h1 { font: 700 26px 'Inter Tight', sans-serif; margin: 0; }
.set-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(230px, 1fr));
    gap: 12px; margin-top: 12px; }
.shelf-title { font: 600 13px 'JetBrains Mono', monospace; color: var(--mut2);
    text-transform: uppercase; letter-spacing: 0.06em; margin: 18px 0 0; }
.fan-tile { display: block; background: var(--card); border: 1px solid var(--line);
    border-radius: 10px; padding: 14px; text-decoration: none; color: inherit;
    transition: box-shadow 0.15s; }
.fan-tile:hover { box-shadow: 0 6px 20px rgba(20, 19, 26, 0.10); }
.fan { position: relative; height: 118px; margin-bottom: 11px; }
.fan-card { position: absolute; left: 50%; border-radius: 5px; overflow: hidden; }
.fan-back-left { width: 74px; height: 102px; top: 4px;
    transform: translateX(-88%) rotate(-8deg); box-shadow: 0 3px 10px rgba(0,0,0,0.2); }
.fan-back-right { width: 74px; height: 102px; top: 4px;
    transform: translateX(-12%) rotate(8deg); box-shadow: 0 3px 10px rgba(0,0,0,0.2); }
.fan-front { width: 78px; height: 108px; top: 0; transform: translateX(-50%);
    box-shadow: 0 5px 14px rgba(0,0,0,0.25); }
.fan-front img { width: 100%; height: 100%; object-fit: cover; }
.fan-name { font: 600 15.5px 'Inter Tight', sans-serif; text-align: center; }
.fan-stats { display: flex; justify-content: center; gap: 8px; font-size: 12px; margin-top: 4px; }
.fan-count { color: var(--mut2); }
.fan-delta { color: var(--mut3); }
.fan-pending { font: 600 11.5px 'JetBrains Mono', monospace; color: var(--mut2);
    text-align: center; margin-top: 6px; }
.mono { font-family: 'JetBrains Mono', monospace; }
```

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/CardStock.Web.Tests --filter BrowsePageSetsTests -v minimal`
Expected: 4 PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Web tests/CardStock.Web.Tests/BrowsePageSetsTests.cs
git commit -m "catalog: Browse by set — the wall, the ordering pills, the era shelves"
```

---

### Task 21: Web — BrowsePage, by-pokémon mode with the filter popover

**Files:**
- Create: `src/CardStock.Web/Components/Catalog/BrowseFilterPopover.razor` + `.razor.css`
- Modify: `src/CardStock.Web/Pages/BrowsePage.razor` + `.razor.css`
- Test: `tests/CardStock.Web.Tests/BrowsePagePokemonTests.cs`

**Interfaces:**
- Consumes: `SpeciesFilters` (Task 19), `SpeciesTileDto`, `CatalogApiClient.SpeciesIconUrl`.
- Produces: `BrowseFilterPopover` — parameters: `AllSpecies` (IReadOnlyList<SpeciesTileDto>), `Active` (IReadOnlyDictionary<string, IReadOnlySet<string>>), `Committed` (EventCallback<(string Key, IReadOnlySet<string> Values)>), `Closed` (EventCallback). Chip copy in the page: `{key} = {label}` / `{key} ∈ {label}, {label}` — raw keys, the terminal voice.

- [ ] **Step 1: Write the failing tests**

```csharp
using Bunit;
using CardStock.Application.Catalog;
using Xunit;

namespace CardStock.Web.Tests;

public class BrowsePagePokemonTests : TestContext
{
    private static SpeciesTileDto Species(int id, string name, long value, string[] types,
        short gen, string? habitat = "Urban") => new(
        id, name, name.ToLowerInvariant(), "#2B2D42", "#5C6B9E", 10, value, types, gen,
        "Johto", "Ordinary", 1, "Black", ["Field"], habitat);

    private static readonly SpeciesTileDto[] All =
    [
        Species(6, "Charizard", 28_400_000, ["Fire"], 1),
        Species(197, "Umbreon", 9_640_000, ["Dark"], 2),
        Species(471, "Glaceon", 1_190_000, ["Ice"], 4, habitat: null),
    ];

    [Fact]
    public void Pokemon_mode_renders_the_value_ordered_grid_with_icons_and_deferred_deltas()
    {
        var cut = RenderBrowse(species: All, mode: "pokemon");

        var names = cut.FindAll(".species-tile .sp-name").Select(n => n.TextContent).ToList();
        Assert.Equal(["Charizard", "Umbreon", "Glaceon"], names);   // wire order preserved
        Assert.Contains("Ordered by total market value across all printings", cut.Markup);

        var tile = cut.FindAll(".species-tile")[0];
        Assert.Contains("api/v1/species/6/icon", tile.QuerySelector(".sp-avatar img")!.GetAttribute("src"));
        Assert.Contains("$284K", tile.TextContent);
        Assert.Contains("10 printings", tile.TextContent);
        Assert.Single(tile.QuerySelectorAll("span.gate-glyph"));
        Assert.Equal("character/charizard", tile.GetAttribute("href"));
        Assert.Contains("3 of 3 species", cut.Markup);
    }

    [Fact]
    public void Committing_a_filter_narrows_the_grid_chips_it_and_the_counter_follows()
    {
        var cut = RenderBrowse(species: All, mode: "pokemon");

        cut.Find(".add-filter").Click();
        cut.FindAll(".pf-attr").Single(a => a.TextContent.Contains("Type")).Click();
        cut.FindAll(".pf-option").Single(o => o.TextContent.Contains("Dark")).Click();
        cut.Find(".pf-add").Click();

        Assert.Single(cut.FindAll(".species-tile"));
        Assert.Contains("type = Dark", cut.Find(".filter-chip").TextContent);
        Assert.Contains("1 of 3 species", cut.Markup);

        cut.Find(".filter-chip .chip-remove").Click();
        Assert.Equal(3, cut.FindAll(".species-tile").Count);
    }

    [Fact]
    public void The_add_button_disables_until_a_value_is_picked()
    {
        var cut = RenderBrowse(species: All, mode: "pokemon");
        cut.Find(".add-filter").Click();
        cut.FindAll(".pf-attr").Single(a => a.TextContent.Contains("Type")).Click();
        Assert.True(cut.Find(".pf-add").HasAttribute("disabled"));
        Assert.Contains("pick at least one", cut.Markup);
    }

    [Fact]
    public void Zero_matches_render_the_empty_panel_copy()
    {
        var cut = RenderBrowse(species: All, mode: "pokemon");
        cut.Find(".add-filter").Click();
        cut.FindAll(".pf-attr").Single(a => a.TextContent.Contains("Generation")).Click();
        cut.FindAll(".pf-option").Single(o => o.TextContent.Contains("Gen 4")).Click();
        cut.Find(".pf-add").Click();
        cut.Find(".filter-chip .chip-remove").Click();   // reset

        // Now a filter that excludes everything: type Dark + gen 1.
        cut.Find(".add-filter").Click();
        cut.FindAll(".pf-attr").Single(a => a.TextContent.Contains("Type")).Click();
        cut.FindAll(".pf-option").Single(o => o.TextContent.Contains("Dark")).Click();
        cut.Find(".pf-add").Click();
        cut.Find(".add-filter").Click();
        cut.FindAll(".pf-attr").Single(a => a.TextContent.Contains("Generation")).Click();
        cut.FindAll(".pf-option").Single(o => o.TextContent.Contains("Gen 1")).Click();
        cut.Find(".pf-add").Click();

        Assert.Contains("No species match these filters — remove one to widen the net.", cut.Markup);
        Assert.Contains("0 of 3 species", cut.Markup);
    }

    [Fact]
    public void The_habitat_editor_carries_the_gen_explainer()
    {
        var cut = RenderBrowse(species: All, mode: "pokemon");
        cut.Find(".add-filter").Click();
        cut.FindAll(".pf-attr").Single(a => a.TextContent.Contains("Habitat")).Click();
        Assert.Contains("Habitat exists for Gen 1–3 species only", cut.Markup);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/CardStock.Web.Tests --filter BrowsePagePokemonTests -v minimal`
Expected: compile failure / missing markup.

- [ ] **Step 3: Implement the popover and the pokémon half**

`BrowseFilterPopover.razor`:

```razor
@using CardStock.Application.Catalog
@using CardStock.Web.Services

@* browse.md §4.3 as amended: role=dialog, Esc closes, initial focus, overlay
   catches outside clicks (no document listeners needed). Menu → editor; the
   editor pre-checks the attribute's committed values so re-opening is edit-in-
   place; Add replaces that attribute's chip wholesale. *@
<div class="pf-overlay" @onclick="() => Closed.InvokeAsync()"></div>
<div class="pf-pop" role="dialog" aria-modal="true" aria-label="Species filters"
     tabindex="-1" @ref="_root" @onkeydown="KeyDown"
     @onmouseleave="MouseLeft" @onclick:stopPropagation>
    @if (_editing is null)
    {
        <div class="pf-section">Pokédex</div>
        @foreach (var attribute in SpeciesFilters.Attributes)
        {
            <button type="button" class="pf-attr"
                    title="Filter species by @attribute.DisplayName.ToLowerInvariant() — fixed Pokédex data"
                    @onclick="() => OpenEditor(attribute.Key)">@attribute.DisplayName<span>›</span></button>
        }
    }
    else
    {
        var attribute = SpeciesFilters.Attributes.Single(a => a.Key == _editing);
        <div class="pf-editor-head">
            <button type="button" class="pf-back" @onclick="() => _editing = null">‹</button>
            <span>@attribute.DisplayName</span>
        </div>
        @if (_editing == "habitat")
        {
            <p class="pf-note">Habitat exists for Gen 1–3 species only</p>
        }
        <div class="pf-options">
            @foreach (var (value, label) in SpeciesFilters.Options(_editing, AllSpecies))
            {
                var on = _selection.Contains(value);
                <button type="button" class="pf-option @(on ? "on" : null)" role="checkbox"
                        aria-checked="@on.ToString().ToLowerInvariant()"
                        title="@(on ? $"Stop including {label}" : $"Include {label} in the results")"
                        @onclick="() => Toggle(value)">
                    <span class="pf-box">@(on ? "✓" : "")</span>@label
                </button>
            }
        </div>
        <div class="pf-footer">
            <span class="pf-preview mono">@Preview()</span>
            <button type="button" class="pf-add" disabled="@(_selection.Count == 0)"
                    @onclick="CommitAsync">Add</button>
        </div>
    }
</div>

@code {
    [Parameter, EditorRequired] public IReadOnlyList<SpeciesTileDto> AllSpecies { get; set; } = default!;
    [Parameter, EditorRequired] public IReadOnlyDictionary<string, IReadOnlySet<string>> Active { get; set; } = default!;
    [Parameter] public EventCallback<(string Key, IReadOnlySet<string> Values)> Committed { get; set; }
    [Parameter] public EventCallback Closed { get; set; }

    private ElementReference _root;
    private string? _editing;
    private HashSet<string> _selection = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _root.FocusAsync();
        }
    }

    private void OpenEditor(string key)
    {
        _editing = key;
        _selection = Active.TryGetValue(key, out var current) ? [.. current] : [];
    }

    private void Toggle(string value)
    {
        if (!_selection.Remove(value))
        {
            _selection.Add(value);
        }
    }

    private string Preview() => _selection.Count == 0
        ? "pick at least one"
        : $"{_editing} {(_selection.Count > 1 ? "∈" : "=")} " +
          string.Join(", ", _selection.Order().Select(v => SpeciesFilters.Label(_editing!, v)));

    private Task CommitAsync() =>
        Committed.InvokeAsync((_editing!, (IReadOnlySet<string>)_selection));

    private Task KeyDown(KeyboardEventArgs e) =>
        e.Key == "Escape" ? Closed.InvokeAsync() : Task.CompletedTask;

    private Task MouseLeft() => _editing is null ? Closed.InvokeAsync() : Task.CompletedTask;
}
```

`BrowseFilterPopover.razor.css`:

```css
.pf-overlay { position: fixed; inset: 0; z-index: 40; }
.pf-pop { position: absolute; top: 31px; z-index: 50; width: 300px; max-height: 380px;
    overflow-y: auto; background: var(--card); border: 1px solid var(--line);
    border-radius: 8px; box-shadow: 0 10px 30px rgba(20, 19, 26, 0.15); padding: 8px; }
.pf-section { font: 600 11px 'Inter Tight', sans-serif; letter-spacing: 0.06em;
    text-transform: uppercase; color: var(--mut2); padding: 4px 8px; }
.pf-attr { display: flex; justify-content: space-between; width: 100%; border: 0;
    background: none; padding: 7px 8px; font-size: 13px; color: var(--ink);
    cursor: pointer; border-radius: 5px; }
.pf-attr:hover { background: var(--mutbg); }
.pf-editor-head { display: flex; align-items: center; gap: 6px; font-weight: 600;
    padding: 4px 8px; }
.pf-back { border: 0; background: none; font-size: 16px; cursor: pointer; color: var(--mut); }
.pf-note { font-size: 12px; color: var(--mut2); padding: 0 8px; margin: 2px 0 6px; }
.pf-options { max-height: 220px; overflow-y: auto; }
.pf-option { display: flex; align-items: center; gap: 8px; width: 100%; border: 0;
    background: none; padding: 6px 8px; font-size: 13px; cursor: pointer;
    border-radius: 5px; color: var(--ink); }
.pf-option:hover { background: var(--mutbg); }
.pf-box { width: 14px; height: 14px; border: 1px solid var(--line3); border-radius: 3px;
    display: grid; place-items: center; font-size: 10px; }
.pf-option.on .pf-box { background: var(--acc); border-color: var(--acc); color: var(--card); }
.pf-footer { display: flex; align-items: center; justify-content: space-between;
    padding: 8px 8px 4px; border-top: 1px solid var(--line4); margin-top: 6px; }
.pf-preview { font-size: 12px; color: var(--mut2); }
.pf-add { border: 0; border-radius: 6px; padding: 5px 14px; font: 600 12.5px
    'Inter Tight', sans-serif; background: var(--acc); color: var(--card); cursor: pointer; }
.pf-add:disabled { background: var(--mutbg); color: var(--mut3); cursor: not-allowed; }
.mono { font-family: 'JetBrains Mono', monospace; }
```

Replace the `@* POKEMON MODE — Task 21 *@` marker in `BrowsePage.razor`:

```razor
@if (IsPokemon)
{
    @if (_species is null)
    {
        <p class="loading-strip" aria-busy="true">Loading…</p>
    }
    else if (_species.Failed)
    {
        <div class="card-error"><p>Couldn't reach the data service.</p>
            <button type="button" @onclick="LoadAsync">Retry</button></div>
    }
    else
    {
        var all = _species.Value!.Species;
        var matched = all.Where(s => SpeciesFilters.Matches(s, _filters)).ToList();

        <div class="filter-bar">
            <div class="filter-anchor">
                <button type="button" class="add-filter" @onclick="() => _popoverOpen = !_popoverOpen">+ filter</button>
                @if (_popoverOpen)
                {
                    <BrowseFilterPopover AllSpecies="all" Active="_filters"
                                         Committed="OnFilterCommitted"
                                         Closed="() => _popoverOpen = false" />
                }
            </div>
            @foreach (var filter in _filters)
            {
                var key = filter.Key;
                <span class="filter-chip mono">@ChipLabel(key, filter.Value)
                    <button type="button" class="chip-remove" aria-label="Remove @key filter"
                            @onclick="() => RemoveFilter(key)">✕</button></span>
            }
            <div class="spacer"></div>
            <span class="shown-count mono">@matched.Count of @all.Count species</span>
        </div>

        <p class="species-caption">Ordered by total market value across all printings</p>
        <div class="species-grid">
            @foreach (var species in matched)
            {
                <a class="species-tile" href="character/@species.Slug"
                   title="Character page for @species.Name">
                    <div class="sp-avatar"
                         style="background: linear-gradient(160deg, @species.GradientStart, @species.GradientEnd)">
                        <span class="sp-initial">@species.Name[..1]</span>
                        <img src="@CatalogApiClient.SpeciesIconUrl(species.SpeciesId)" alt=""
                             loading="lazy" onerror="this.style.display='none'" />
                    </div>
                    <div class="sp-name">@species.Name</div>
                    <div class="sp-printings mono">@species.Printings printings</div>
                    <div class="sp-footer">
                        <span class="sp-value mono">@Format.AbbrevMoney(species.TotalValueCents)</span>
                        <span class="sp-delta mono">@ChipEngine.GlyphDash 90d<PendingGlyph Note="@CatalogCopy.WorkerGate" /></span>
                    </div>
                </a>
            }
        </div>
        @if (matched.Count == 0)
        {
            <div class="empty-panel">No species match these filters — remove one to widen the net.</div>
        }
    }
}
```

And in `@code`, the pokémon-mode state and handlers:

```csharp
    private bool _popoverOpen;
    private readonly Dictionary<string, IReadOnlySet<string>> _filters = [];

    private void OnFilterCommitted((string Key, IReadOnlySet<string> Values) filter)
    {
        _filters[filter.Key] = filter.Values;
        _popoverOpen = false;
    }

    private void RemoveFilter(string key) => _filters.Remove(key);

    private static string ChipLabel(string key, IReadOnlySet<string> values) =>
        $"{key} {(values.Count > 1 ? "∈" : "=")} " +
        string.Join(", ", values.Order().Select(v => SpeciesFilters.Label(key, v)));
```

Species-grid CSS appended to `BrowsePage.razor.css`:

```css
.filter-bar { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; margin-top: 10px; }
.filter-anchor { position: relative; }
.add-filter { border: 1px dashed var(--line3); border-radius: 6px; background: none;
    padding: 4px 10px; font-size: 12.5px; color: var(--mut); cursor: pointer; }
.filter-chip { display: inline-flex; align-items: center; gap: 6px; font-size: 12px;
    background: color-mix(in srgb, var(--acc) 10%, transparent);
    border: 1px solid var(--acc); border-radius: 99px; padding: 3px 10px; }
.chip-remove { border: 0; background: none; cursor: pointer; color: var(--mut); }
.species-caption { font-size: 12.5px; color: var(--mut2); margin: 6px 0 0; }
.species-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(190px, 1fr));
    gap: 12px; margin-top: 10px; }
.species-tile { display: block; background: var(--card); border: 1px solid var(--line);
    border-radius: 10px; padding: 13px; text-decoration: none; color: inherit;
    transition: box-shadow 0.15s; }
.species-tile:hover { box-shadow: 0 6px 20px rgba(20, 19, 26, 0.10); }
.sp-avatar { position: relative; width: 44px; height: 44px; border-radius: 50%;
    overflow: hidden; }
.sp-initial { position: absolute; inset: 0; display: grid; place-items: center;
    font: 700 17px 'Inter Tight', sans-serif; color: rgba(255, 255, 255, 0.92); }
.sp-avatar img { position: relative; width: 100%; height: 100%; object-fit: contain;
    image-rendering: pixelated; }
.sp-name { font: 600 15.5px 'Inter Tight', sans-serif; white-space: nowrap;
    overflow: hidden; text-overflow: ellipsis; margin-top: 8px; }
.sp-printings { font-size: 11.5px; color: var(--mut2); }
.sp-footer { display: flex; justify-content: space-between; align-items: baseline;
    margin-top: 8px; }
.sp-value { font: 700 14.5px 'JetBrains Mono', monospace; }
.sp-delta { font-size: 12px; color: var(--mut3); }
.shown-count { font-size: 12.5px; color: var(--mut2); }
.spacer { flex: 1; }
.empty-panel { background: var(--card); border: 1px solid var(--line);
    border-radius: 10px; padding: 40px; text-align: center; font-size: 14px;
    color: var(--mut2); margin-top: 10px; }
```

- [ ] **Step 4: Run to verify pass, whole Web suite**

Run: `dotnet test tests/CardStock.Web.Tests -v minimal`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Web tests/CardStock.Web.Tests/BrowsePagePokemonTests.cs
git commit -m "catalog: Browse by pokémon — icon tiles, the eight-filter popover, honest empties"
```

---

### Task 22: Web — the About-data page

Static content page transcribing about-data.md's **"Corrected copy — build this"** section
(every sentence there is receipt-backed) with the three D-110 adaptations. No reader, no
DTOs. The copy below is the page — do not reword it.

**Files:**
- Create: `src/CardStock.Web/Pages/AboutDataPage.razor` + `.razor.css`
- Test: `tests/CardStock.Web.Tests/AboutDataPageTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: route `/about-data`; anchors `#sources`, `#holdings`, `#cannot-know`, `#pooled-grades`, `#freshness`, `#floor`, `#honesty`, `#disclaimers`.

- [ ] **Step 1: Write the failing tests**

```csharp
using Bunit;
using CardStock.Web.Pages;
using Xunit;

namespace CardStock.Web.Tests;

public class AboutDataPageTests : TestContext
{
    private IRenderedComponent<AboutDataPage> Render() => RenderComponent<AboutDataPage>();

    [Fact]
    public void The_source_is_named_and_the_seam_fiction_is_gone()
    {
        var markup = Render().Markup;
        Assert.Contains("pricecharting.com", markup);
        Assert.DoesNotContain("April 2025", markup);
        Assert.DoesNotContain("Apr ’25", markup);
        Assert.DoesNotContain("sale counts", markup);
    }

    [Fact]
    public void The_floor_section_states_the_date_and_the_reason()
    {
        var markup = Render().Markup;
        Assert.Contains("1 September 2026", markup);
        Assert.Contains("deliberate cutoff", markup);
    }

    [Fact]
    public void The_five_sufficiency_states_print_and_no_authored_unlock_dates_do()
    {
        var markup = Render().Markup;
        foreach (var state in new[] { "OK", "LOW DATA", "LOCKED", "UNDEFINED window", "UNSTABLE FIT" })
        {
            Assert.Contains(state, markup);
        }
        Assert.DoesNotContain("Jan 2027", markup);
    }

    [Fact]
    public void Eight_pills_anchor_eight_sections()
    {
        var cut = Render();
        var hrefs = cut.FindAll(".pill-row a").Select(a => a.GetAttribute("href")).ToList();
        Assert.Equal(8, hrefs.Count);
        foreach (var id in new[] { "sources", "holdings", "cannot-know", "pooled-grades",
            "freshness", "floor", "honesty", "disclaimers" })
        {
            Assert.Contains($"about-data#{id}", hrefs);
            Assert.NotNull(cut.Find($"#{id}"));
        }
    }

    [Fact]
    public void The_restatement_promise_stops_at_what_is_built()
    {
        var markup = Render().Markup;
        Assert.Contains("We never rewrite history", markup);
        Assert.DoesNotContain("mark the affected window on charts", markup);
    }
}
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/CardStock.Web.Tests --filter AboutDataPageTests -v minimal`
Expected: compile failure.

- [ ] **Step 3: Implement the page**

`AboutDataPage.razor` (full content — the corrected copy verbatim, structure per the
prototype's document template):

```razor
@page "/about-data"

<PageTitle>About our data</PageTitle>

<div class="about-container">
    <h1>About our data</h1>
    <p class="subtitle">Where every number comes from, how fresh it is, and what cannot be known.</p>

    <div class="pill-row">
        <a href="about-data#sources">Sources</a>
        <a href="about-data#holdings">What we hold</a>
        <a href="about-data#cannot-know">What we cannot know</a>
        <a href="about-data#pooled-grades">Pooled grades</a>
        <a href="about-data#freshness">Freshness</a>
        <a href="about-data#floor">The floor</a>
        <a href="about-data#honesty">Honesty policy</a>
        <a href="about-data#disclaimers">Disclaimers</a>
    </div>

    <section id="sources">
        <h2>Where the numbers come from</h2>
        <p>Every price, sale, and population figure on Cardstock comes from
            <strong>pricecharting.com</strong>. We do not collect from marketplaces ourselves.</p>
        <p>Two things follow from that, and they matter when you read a chart:</p>
        <p>The <strong>individual sales</strong> we list are real completed transactions,
            recorded as PriceCharting reported them — date, venue, grade, and price.</p>
        <p>The <strong>monthly price line is not built from those sales.</strong> It is
            PriceCharting's own monthly average, which we store and chart unaltered. We do not
            recompute it, and its method is theirs, not ours.</p>
    </section>

    <section id="holdings">
        <h2>What we hold, and from when</h2>
        <div class="holdings-table">
            <div class="ht-head">Series</div><div class="ht-head">Covers</div><div class="ht-head">Begins</div>
            <div>Monthly average prices</div><div>6 grade tiers</div>
            <div><strong>~December 2020</strong>, complete for every card</div>
            <div>Individual sales</div><div>19 grade labels</div>
            <div>The first time we visited that card</div>
            <div>Population census</div><div>PSA and CGC only</div>
            <div>The first time we visited that card</div>
        </div>
        <p><strong>Our sales and census history does not start on a single date.</strong> It
            starts the first time we visited each card. We began collecting on 28 July 2026, and
            each card entered the record when its turn came — so the boundary sits in a different
            place for every card, and we draw it where it actually falls rather than pretending
            it is one line.</p>
        <p>Monthly prices are the exception. The first visit to a card retrieves its entire
            price chart at once, so that history is complete back to about December 2020
            regardless of when we first saw it.</p>
    </section>

    <section id="cannot-know">
        <h2>What we cannot know</h2>
        <p>Some things are not missing from Cardstock — they do not exist anywhere, and no
            amount of collecting will produce them.</p>
        <p><strong>How many sales happened before we started watching.</strong> PriceCharting
            publishes no historical volume series. Nobody has this.</p>
        <p><strong>Sales older than roughly the last 30 in each grade bucket.</strong>
            PriceCharting keeps a rolling window and discards what falls off it. Once a sale
            scrolls out, it is gone for everyone.</p>
        <p><strong>Census history before our first visit.</strong> PriceCharting publishes a
            current snapshot with no history attached.</p>
        <p><strong>Which company graded a card below grade 10.</strong> PriceCharting pools
            every grading company into a single figure for grades 1 through 9.5, and splits by
            company only at 10.</p>
    </section>

    <section id="pooled-grades">
        <h2>On pooled grades</h2>
        <p>Below grade 10, a price covers every grading company at once — a "Grade 8" figure
            includes PSA, CGC, BGS and others together.</p>
        <p>About <strong>91%</strong> of the identifiable volume in those buckets is PSA, so
            the pooled figure tracks PSA closely. A CGC card of the same grade typically trades
            below it, by roughly a third at grade 8.</p>
        <p>We show the pooled number and label it as pooled. <strong>We do not apply a
            multiplier to estimate a company-specific price</strong> — that would present a
            guess with the same confidence as an observation.</p>
    </section>

    <section id="freshness">
        <h2>How fresh it is</h2>
        <p>Cards are visited continuously, one at a time, in priority order — not on a fixed
            schedule. A card that is selling quickly is visited sooner. A quiet card may go a
            month or more between visits.</p>
        <p><strong>Opening a card page triggers a fresh visit</strong>, so a card page shows
            sales up to that moment.</p>
        <p>The <strong>current month's price revises.</strong> PriceCharting recalculates it as
            sales land, and we pick up the new figure on our next visit. It renders as a hollow,
            dashed point for exactly that reason. <strong>Closed months never change</strong> —
            once a month closes, its value is fixed permanently.</p>
    </section>

    <section id="floor">
        <h2>The floor</h2>
        <p><strong>No metric on Cardstock counts an observation recorded before
            1 September 2026.</strong></p>
        <p>This is a deliberate cutoff, not the date our data begins. We were still stabilising
            the collector through August 2026, so we discard our own earliest observations
            rather than trust them. September 1st is the first date we are willing to stand
            behind.</p>
        <p>Every unlock countdown on this site is measured from that floor.</p>
        <p>An indicator without enough history renders a <em>state</em>, not a number:
            <span class="mono">OK</span> · <span class="mono">LOW DATA</span> ·
            <span class="mono">LOCKED</span> · <span class="mono">UNDEFINED window</span> ·
            <span class="mono">UNSTABLE FIT</span>. Every locked control names the rule it is
            waiting on and its unlock condition.</p>
    </section>

    <section id="honesty">
        <h2>Honesty policy</h2>
        <ul>
            <li><strong>No projected or extrapolated points.</strong> A partial month renders
                as partial, never as a forecast.</li>
            <li><strong>A metric below its sufficiency floor renders a state, not a
                number.</strong> It will tell you which rule it failed and when it will
                pass.</li>
            <li><strong>When a grader restates a past census, we keep what we already
                recorded.</strong> Restatements happen — PSA restated in June 2026 and one
                card's grade cell moved from 397 to 99,246. We write the new figures alongside
                the old ones. We never rewrite history.</li>
            <li><strong>Backtests start at the first date every filter in them could actually
                be computed</strong>, not at the start of our records.</li>
        </ul>
    </section>

    <section id="disclaimers">
        <h2>Disclaimers</h2>
        <p>Cardstock is a fan-made analytics project. It is not affiliated with, endorsed by,
            or sponsored by Nintendo, The Pokémon Company, Creatures Inc., or any grading
            company or marketplace. Pokémon names and card references are used for
            identification only; all trademarks belong to their owners.</p>
        <p>Nothing here is financial advice. Collectible prices are volatile and thinly traded;
            signals describe the past, not the future. Do your own research before spending
            money on cardboard.</p>
    </section>
</div>
```

`AboutDataPage.razor.css` (the shared document template's metrics):

```css
.about-container { max-width: 820px; margin: 0 auto; padding: 32px 24px 80px; }
.about-container h1 { font: 700 27px 'Inter Tight', sans-serif; margin: 0 0 4px; }
.subtitle { color: var(--mut); margin: 0 0 16px; }
.pill-row { display: flex; flex-wrap: wrap; gap: 6px; margin-bottom: 28px; }
.pill-row a { font: 600 13px 'Inter Tight', sans-serif; border: 1px solid var(--line);
    border-radius: 99px; padding: 4px 12px; color: var(--mut); text-decoration: none; }
.pill-row a:hover { color: var(--acc); border-color: var(--acc); }
section { background: var(--card); border: 1px solid var(--line); border-radius: 8px;
    padding: 20px 22px; margin-bottom: 14px; scroll-margin-top: 62px; }
section h2 { font: 700 18.5px 'Inter Tight', sans-serif; margin: 0 0 10px; }
section p, section li { font-size: 14.5px; line-height: 1.6; color: var(--mut); }
section ul { display: flex; flex-direction: column; gap: 6px; padding-left: 20px; margin: 0; }
.holdings-table { display: grid; grid-template-columns: 1.2fr 1fr 1.6fr; gap: 0;
    border: 1px solid var(--line); border-radius: 6px; overflow: hidden;
    font-size: 13.5px; margin-bottom: 12px; }
.holdings-table > div { padding: 8px 12px; border-top: 1px solid var(--line4); }
.holdings-table > .ht-head { background: var(--mutbg); border-top: 0;
    font: 600 12px 'Inter Tight', sans-serif; letter-spacing: 0.06em;
    text-transform: uppercase; color: var(--mut2); }
.mono { font-family: 'JetBrains Mono', monospace; }
```

**Receipt check before this task's commit** (adaptation 3 of spec §9): confirm the
"Opening a card page triggers a fresh visit" sentence against the shipped behavior —
`grep -n "fresh scrape" src/CardStock.Web/Components/Card/FreshnessFooter.razor` shows the
live tooltip making the same claim, and `CardPage.razor`'s `MaybeRefreshAsync` fires the
express visit on load (D-062/D-077). Both greps in the commit message body are the receipt.

- [ ] **Step 4: Run to verify pass**

Run: `dotnet test tests/CardStock.Web.Tests --filter AboutDataPageTests -v minimal`
Expected: 5 PASS.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Web/Pages/AboutDataPage.razor src/CardStock.Web/Pages/AboutDataPage.razor.css tests/CardStock.Web.Tests/AboutDataPageTests.cs
git commit -m "catalog: About our data — the corrected copy ships, the seam fiction dies

Fresh-visit sentence receipt: FreshnessFooter.razor's live tooltip states the same
behavior; CardPage.razor MaybeRefreshAsync fires the express visit on load (D-062/D-077)."
```

---

### Task 23: Web — chrome arming

**Files:**
- Modify: `src/CardStock.Web/Layout/AppChrome.razor`
- Modify: `src/CardStock.Web/Pages/Home.razor`
- Modify: `src/CardStock.Web/Components/Card/FreshnessFooter.razor` (+ its `.razor.css`)
- Test: `tests/CardStock.Web.Tests/ChromeTests.cs` (update), `tests/CardStock.Web.Tests/FreshnessFooterTests.cs` (extend)

**Interfaces:**
- Consumes: routes from Tasks 11/15/20/22.
- Produces: a live Browse tab, active on `/browse`, `/set/*`, `/character/*`; the search
  tooltip's truthfulness edit; Home placeholder copy pointing at Browse; the Card page's
  freshness footer linking About-data.

- [ ] **Step 1: Update the tests first**

In `ChromeTests.cs`: the deferred-tooltips inventory drops Browse and gains the new search
string. Add:

```csharp
    [Fact]
    public void The_browse_tab_is_a_live_link_active_across_catalog_routes()
    {
        var cut = RenderComponent<AppChrome>();
        var browse = cut.FindAll(".tabs a").Single(a => a.TextContent == "Browse");
        Assert.Equal("browse", browse.GetAttribute("href"));

        foreach (var route in new[] { "browse", "set/7", "character/umbreon" })
        {
            Services.GetRequiredService<Bunit.TestDoubles.FakeNavigationManager>()
                .NavigateTo(route);
            cut.Render();
            Assert.Contains("active",
                cut.FindAll(".tabs a").Single(a => a.TextContent == "Browse").ClassList);
        }
    }

    [Fact]
    public void The_search_tooltip_no_longer_promises_the_browse_phase()
    {
        var cut = RenderComponent<AppChrome>();
        Assert.Equal("Search arrives in a later phase",
            cut.Find("input[type=search]").GetAttribute("title"));
    }
```

(Adjust to the file's existing render/navigation idiom; the assertions are the contract.
Whatever list `DeferredTooltips` asserts must drop the Browse entry.)

In `FreshnessFooterTests.cs` add:

```csharp
    [Fact]
    public void The_footer_links_about_our_data()
    {
        var cut = RenderComponent<FreshnessFooter>(p => p
            .Add(x => x.LastVisitedAt, (DateTimeOffset?)null)
            .Add(x => x.CensusObservedAt, (DateTimeOffset?)null));
        var link = cut.FindAll("a").Single(a => a.TextContent == "About our data");
        Assert.Equal("about-data", link.GetAttribute("href"));
    }
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test tests/CardStock.Web.Tests --filter "ChromeTests|FreshnessFooterTests" -v minimal`
Expected: FAIL.

- [ ] **Step 3: Implement**

`AppChrome.razor` — replace the Browse `DeferredControl` with a live anchor and inject
navigation:

```razor
@implements IDisposable
@inject NavigationManager Nav
```

```razor
        <a class="tab @(IsCatalogRoute() ? "active" : null)" href="browse"
           title="Browse every set and species we track">Browse</a>
```

```csharp
@code {
    protected override void OnInitialized() => Nav.LocationChanged += OnLocationChanged;

    private void OnLocationChanged(object? sender,
        Microsoft.AspNetCore.Components.Routing.LocationChangedEventArgs e) => StateHasChanged();

    private bool IsCatalogRoute()
    {
        var path = Nav.ToBaseRelativePath(Nav.Uri);
        return path.StartsWith("browse", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("set/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("character/", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() => Nav.LocationChanged -= OnLocationChanged;
}
```

The search input's title becomes `Search arrives in a later phase` — the honesty edit, and
the `.tab` anchor needs the `DeferredControl`-matching styles in the chrome CSS if the
class previously only ever styled spans (check `AppChrome.razor.css` / app.css and mirror
the active-tab treatment: weight 600, `--ink`, 2px `--acc` bottom border).

`Home.razor` copy:

```razor
    <p>Home arrives in a later phase. Browse is live — every set and species we track —
        and card pages at <span class="mono">/card/{id}</span>.</p>
```

`FreshnessFooter.razor` — before the attribution span:

```razor
    <a class="freshness-about" href="about-data">About our data</a>
    <span class="freshness-sep">·</span>
```

with `.freshness-about { color: var(--link); text-decoration: none; }` in its scoped CSS.

- [ ] **Step 4: Run the whole Web suite**

Run: `dotnet test tests/CardStock.Web.Tests -v minimal`
Expected: PASS — including the pre-existing chrome/home tests updated, none deleted.

- [ ] **Step 5: Commit**

```bash
git add src/CardStock.Web tests/CardStock.Web.Tests
git commit -m "catalog: the Browse tab goes live and the chrome copy stops overpromising"
```

---

### Task 24: Deploy and the phase receipts

The phase closes on receipts, D-109-style: predicted from SQL first, then read from the
live pages. Browser verification via headless Chrome — the Claude-in-Chrome tab freezes the
WASM app (known quirk, not a product bug).

**Files:**
- Modify: the Pi's `/opt/cardstock/api/appsettings.Production.json` (by hand over ssh)
- No repo code changes except any receipt-driven fixes.

- [ ] **Step 1: Full local gate**

```bash
dotnet build && CARDSTOCK_TEST_DB="..." dotnet test && dotnet format --verify-no-changes
```

Expected: clean, all suites green.

- [ ] **Step 2: Predict the receipts from SQL (record every number before looking at a page)**

```bash
ssh scott@192.168.0.56 "cd /tmp && sudo -u postgres psql -d pokemon -tA -c \"
SELECT 'sets', count(*) FROM sets;
SELECT 'species', count(*) FROM species;
SELECT 'era shelves', era, count(*) FROM set_details WHERE era IS NOT NULL GROUP BY era ORDER BY min(released_on);
SELECT 'no-era', count(*) FROM set_details WHERE match_status=0 AND era IS NULL;
SELECT 'pending', count(*) FROM set_details WHERE match_status=1;
SELECT 'top5 species by value', s.name FROM species s JOIN (
  SELECT cs.species_id, sum(l.price_cents) v FROM card_species cs JOIN (
    SELECT DISTINCT ON (card_id) card_id, price_cents FROM price_months
    WHERE tier=5 AND price_cents>0 ORDER BY card_id, month DESC, observed_at DESC) l
  ON l.card_id=cs.card_id GROUP BY cs.species_id) t ON t.species_id=s.id
  ORDER BY t.v DESC LIMIT 5;
SELECT 'umbreon printings', count(*) FROM card_species cs JOIN cards c ON c.id=cs.card_id
  WHERE cs.species_id=197 AND c.delisted_at IS NULL AND c.not_a_card_at IS NULL;
SELECT 'evolving skies id+count', s.id, count(*) FROM sets s JOIN cards c ON c.set_id=s.id
  WHERE s.name='Evolving Skies' AND c.delisted_at IS NULL AND c.not_a_card_at IS NULL GROUP BY s.id;
\""
```

Write the outputs into the execution notes; the pages must reproduce them exactly.

- [ ] **Step 3: Deploy**

```bash
./ops/publish.sh publish/api
./ops/deploy.sh
```

Then add the icon directory to the Pi's production config (one-time):

```bash
ssh scott@192.168.0.56 "sudo -u pokemon ls /opt/pokemon-invest-batch/species-icons | head -3 && \
  sudo python3 - <<'PY'
import json
p = '/opt/cardstock/api/appsettings.Production.json'
cfg = json.load(open(p))
cfg.setdefault('SpeciesIcons', {})['Directory'] = '/opt/pokemon-invest-batch/species-icons'
json.dump(cfg, open(p, 'w'), indent=2)
PY
sudo systemctl restart cardstock-api"
```

(First confirm the actual icon path — `ls` it; if the scraper's `SpeciesIconDirectory` is
configured elsewhere, use that path. If the `cardstock` user cannot read it, fix with
group-read: `sudo chmod -R g+rX /opt/pokemon-invest-batch/species-icons && sudo chgrp -R
cardstock /opt/pokemon-invest-batch/species-icons` — or the ACL route ops/README prefers.)

- [ ] **Step 4: Read the receipts off the live system**

```bash
curl -sf http://192.168.0.56:5180/healthz/data
curl -s http://192.168.0.56:5180/api/v1/browse/sets | python3 -c "import json,sys; d=json.load(sys.stdin); print(len(d['sets']))"          # expect 789
curl -s http://192.168.0.56:5180/api/v1/browse/species | python3 -c "import json,sys; d=json.load(sys.stdin); s=d['species']; print(len(s), [x['name'] for x in s[:5]])"   # expect 1025 + the SQL top-5 in order
curl -s "http://192.168.0.56:5180/api/v1/sets/<EvolvingSkiesId>" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d['cardsTracked'], len(d['roster']), d['code'], d['era'])"
curl -s http://192.168.0.56:5180/api/v1/characters/umbreon | python3 -c "import json,sys; d=json.load(sys.stdin); print(d['printings'], d['setsCount'], len(d['chips']))"
for dex in 1 197 471 1025; do curl -s -o /dev/null -w "%{http_code} " http://192.168.0.56:5180/api/v1/species/$dex/icon; done; echo
# Pop Δ all-pending at ship:
curl -s "http://192.168.0.56:5180/api/v1/sets/<EvolvingSkiesId>" | python3 -c "import json,sys; d=json.load(sys.stdin); print({r['pop']['state'] for r in d['roster']})"   # expect {'pending'} or {'pending','none'} — never 'available' before ~late Sep
```

Then the rendered pages via headless Chrome:

```bash
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless --disable-gpu \
  --dump-dom "http://192.168.0.56:5180/browse" 2>/dev/null | grep -c "fan-tile"      # expect 789
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless --disable-gpu \
  --dump-dom "http://192.168.0.56:5180/browse?mode=pokemon" 2>/dev/null | grep -c "species-tile"   # expect 1025
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless --disable-gpu \
  --dump-dom "http://192.168.0.56:5180/character/umbreon" 2>/dev/null | grep -o "of [0-9]* printings" | head -1
"/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" --headless --disable-gpu \
  --dump-dom "http://192.168.0.56:5180/about-data" 2>/dev/null | grep -c "April 2025"   # expect 0
```

(WASM needs render time — if `--dump-dom` races the boot, add
`--virtual-time-budget=15000`.) Every number must match Step 2's predictions; any mismatch
is a bug to fix before closing, not a note.

- [ ] **Step 5: Cold-cache timing sanity**

```bash
ssh scott@192.168.0.56 'sudo systemctl restart cardstock-api'; sleep 4
time curl -s -o /dev/null http://192.168.0.56:5180/api/v1/browse/species   # cold: expect ~1.5–3s
time curl -s -o /dev/null http://192.168.0.56:5180/api/v1/browse/species   # warm: expect <300ms
```

If cold exceeds ~5s on the Pi, note it for the sibling-index escalation the spec reserves
(spec §3.4) — do not add the index yourself; it is the scraper repo's migration.

- [ ] **Step 6: Close the phase's paperwork**

Update each screen spec's amendment banner if execution deviated anywhere (the maintenance
rule); then the ledger close entry (D-111 or next free number) recording the receipts —
written with the owner in the loop, not unilaterally. Final commit:

```bash
git add -A && git commit -m "catalog: phase receipts — the four pages live on the Pi"
```

---

## Plan Self-Review (performed at write time)

**Spec coverage.** Spec §1 routes/slices → Tasks 11/15/20–22/23; §2 vocabulary → Tasks 8–11,
15, 20–21 (every gated element has a task encoding it; the gate inventory's Pop Δ/Year rows
land in Tasks 11/15); §3 read layer → Tasks 1–5, 13, 16–17 (five views, both disciplines, the
cache with its measured justification); §4 API/DTOs → Tasks 4, 6, 12, 14, 17–18 (no wire
fields for gated stats — verified: no DTO carries a Δ/RS/sparkline field); §5 kit → Tasks
8–10; §6 Browse → Tasks 19–21 (ordering control, data-driven shelves with both tails, icon
tiles, explicit value order, 8-attribute algebra, habitat explainer, URL mode); §7 Set →
Tasks 5–6, 11 (era chip, uppercase-verbatim code, ◌ chip, five pills, sortable sales, banner
with computed dates, footer rewrite, empty state); §8 Character → Tasks 12–15 (icon header,
chip rules incl. habitat omission, four pills, Set-cell links, year pending, named-species
footer); §9 About-data → Task 22 (corrected copy, slimmed sufficiency, fresh-visit receipt,
footer link in Task 23); §10 receipts → Task 24; §11 deviations — each encoded in its page
task. Gap check: the spec's `metadata pending` **chip inside the era view's pending-shelf
tiles** is covered (Task 20 test 4); the Set page's `◌ metadata pending` chip in Task 11.

**Placeholder scan.** No TBDs. Two deliberate delegations remain and are labeled as such:
Task 11/15's `RenderSetPage`/`RenderCharacterPage` stub plumbing says "copy the exact
stub-and-wait plumbing from CardPageTests" — the established idiom is in-repo and the
assertions are fully written; and Task 1/5 defer to the regenerated fixture's DDL for any
NOT-NULL column this plan did not enumerate. Task 9 records its own mid-step correction
(the pointer-capture design) ending in one concrete implementation — the delegated-listener
version is the one to build.

**Type consistency.** `LatestPsa10Row` defined once (Task 5), consumed by Tasks 13/16.
`PopulationDelta.Result` flows Task 2 → 4 → 5 → 6. `SortPill`/`SortState`/`RosterColumn<TRow>`
match between Tasks 8/9 and their consumers in 11/15. `SetTileDto`/`SpeciesTileDto` field
lists are identical in Tasks 17/19/20/21. `CatalogCopy.WorkerGate` is the single gate-note
string everywhere a worker gate appears. `GetAsync<T>` private helper referenced by Tasks
15/20 exists from Task 7.

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-08-17-catalog-phase.md`. Two
execution options:

1. **Subagent-Driven (recommended)** — fresh subagent per task, review between tasks
   (superpowers:subagent-driven-development).
2. **Inline Execution** — this session, batch execution with checkpoints
   (superpowers:executing-plans).





