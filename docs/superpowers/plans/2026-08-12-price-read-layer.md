# Price Read Layer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the layer that answers "what are this card's prices, and what changed?" without ever letting a caller mistake an absence for a value.

**Architecture:** Every rule lives in `CardStock.Domain` as a pure function over hand-buildable records — latest-per-key resolution, gap detection, staleness, grade mapping, the change calculation. `CardStock.Infrastructure` runs two narrow queries and hands the rows to Domain. Nothing about change-only semantics reaches SQL, so the whole layer tests without a database. Absence is expressed in the type system: `PriceAvailable` is the only case carrying a number, so carrying a price across a hole does not compile.

**Tech Stack:** .NET 10, EF Core 10.0.10, Npgsql 10.0.3, PostgreSQL 15, xUnit 2.9.3.

**Spec:** `docs/superpowers/specs/2026-08-12-price-read-layer-design.md`

## Global Constraints

Every task's requirements implicitly include this section.

- **Target framework `net10.0`.** `Nullable=enable`, `ImplicitUsings=enable`, **`TreatWarningsAsErrors=true`** — set repo-wide in `Directory.Build.props`. A warning fails the build, so unused usings and possible-null dereferences must be fixed, not ignored.
- **Reference chain is one-directional:** `Domain ← Application ← Infrastructure`. **Domain references nothing.** It may not use EF Core, Npgsql, or any Infrastructure type. If a Domain rule seems to need a scraper entity, it needs its own input record instead.
- **File-scoped namespaces**, explicit accessibility modifiers, 4-space C# indent, LF endings, final newline. `.editorconfig` enforces the first two at `warning`, which is an error here.
- **Test names are snake_case sentences**, matching `ScraperReadTests.cs` — e.g. `Two_rows_for_one_month_resolve_to_the_later_observation`.
- **Plain xUnit `Assert`.** No FluentAssertions; it is not referenced and must not be added. `Xunit` is a global using in every test project.
- **Commit messages are prose, not conventional commits.** The repo has no `feat:`/`fix:` prefixes — see `git log`. Write what changed and why it matters.
- **Database tests run against the Pi** (D-073) and use `[SkippableFact]` with `Skip.IfNot(Available, ...)`, so an unset `CARDSTOCK_TEST_DB` skips rather than fails. Only `CardStock.Integration.Tests` references `Xunit.SkippableFact`.
- **Never write to a scraper table.** All five are mapped `ToView`; EF throws before Postgres is asked. Nothing in this plan writes.
- **No migrations.** This phase adds no tables and no columns.

## File Structure

```
src/CardStock.Domain/Prices/
  PriceTier.cs                    MOVED from Infrastructure. The ordinal IS the stored value.
  PriceObservation.cs             Domain's input record for one price_months row
  SaleObservation.cs              Domain's input record for one sales row, tier already mapped
  MonthlyPrice.cs                 One resolved month
  TierSeries.cs                   One tier's resolved history + first/last month
  PriceSlot.cs                    ObservedPrice | MissingMonth | OutsideSeries
  TierPrice.cs                    PriceAvailable | PriceStale | NoPriceSeries
  TierChange.cs                   ChangeAvailable | ChangeInsufficient
  CardPriceSnapshot.cs            The whole contract: card + 6 TierSnapshot
  PriceSeriesBuilder.cs           Raw rows -> 6 TierSeries, latest-per-key
  PriceWindow.cs                  TierSeries -> one slot per month
  PriceStaleness.cs               TierSeries -> TierPrice. Holds MaxMonthsBehind
  GradeTierMap.cs                 sales.grade_tier -> PriceTier?, allow-list
  SalesChange.cs                  Sales -> TierChange. Holds MinimumSalesPerWindow
  CardPriceSnapshotBuilder.cs     Assembles everything

src/CardStock.Application/Prices/
  ICardPriceReader.cs             One method, one card

src/CardStock.Infrastructure/
  Persistence/ScraperReadModels/ScraperCard.cs   MODIFY: add LastVisitedAt
  Prices/CardPriceReader.cs                      Two queries, then hand to Domain

tests/CardStock.Domain.Tests/Prices/
  PriceSeriesBuilderTests.cs · PriceWindowTests.cs · PriceStalenessTests.cs
  GradeTierMapTests.cs · SalesChangeTests.cs · CardPriceSnapshotBuilderTests.cs
  PriceTierTests.cs

tests/CardStock.Integration.Tests/
  CardPriceReaderTests.cs         Real Postgres: the queries feed Domain what it expects
```

One file per type, because each is small and a reviewer should be able to hold any one of them entirely in view. `PriceSlot.cs`, `TierPrice.cs` and `TierChange.cs` each hold a closed hierarchy — the abstract base and its cases belong together, since adding a case without reading the others is exactly the bug they exist to prevent.

---

### Task 1: Move `PriceTier` into Domain and pin its ordinals

The pure rules need this enum and Domain references nothing, so it cannot stay in Infrastructure. The ordinal *is* the value stored in `price_months.tier`, so this task also adds the test that makes reordering it a build failure rather than a silent misreading of five years of prices.

**Files:**
- Create: `src/CardStock.Domain/Prices/PriceTier.cs`
- Delete: `src/CardStock.Infrastructure/Persistence/ScraperReadModels/PriceTier.cs`
- Modify: `src/CardStock.Infrastructure/Persistence/ScraperReadModels/ScraperPriceMonth.cs`
- Test: `tests/CardStock.Domain.Tests/Prices/PriceTierTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `CardStock.Domain.Prices.PriceTier` — `Ungraded=0, Grade7=1, Grade8=2, Grade9=3, Grade9Half=4, Psa10=5`. Every later task uses it.

- [x] **Step 1: Write the failing test**

Create `tests/CardStock.Domain.Tests/Prices/PriceTierTests.cs`:

```csharp
using CardStock.Domain.Prices;

namespace CardStock.Domain.Tests.Prices;

public class PriceTierTests
{
    /// <summary>
    /// price_months.tier stores the ordinal as an integer, so these numbers are
    /// data, not implementation detail. Reordering the enum would silently
    /// reinterpret every historical price in the database -- 10.3M rows, with no
    /// error anywhere. This test is the tripwire.
    /// </summary>
    [Fact]
    public void Tier_ordinals_are_the_values_stored_in_the_database()
    {
        Assert.Equal(0, (int)PriceTier.Ungraded);
        Assert.Equal(1, (int)PriceTier.Grade7);
        Assert.Equal(2, (int)PriceTier.Grade8);
        Assert.Equal(3, (int)PriceTier.Grade9);
        Assert.Equal(4, (int)PriceTier.Grade9Half);
        Assert.Equal(5, (int)PriceTier.Psa10);
    }

    [Fact]
    public void There_are_exactly_six_price_tiers()
    {
        Assert.Equal(6, Enum.GetValues<PriceTier>().Length);
    }
}
```

- [x] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/CardStock.Domain.Tests -c Release --filter PriceTierTests`
Expected: FAIL — build error, `CardStock.Domain.Prices` namespace does not exist.

- [x] **Step 3: Create the Domain enum**

Create `src/CardStock.Domain/Prices/PriceTier.cs`:

```csharp
namespace CardStock.Domain.Prices;

/// <summary>
/// Mirrors PokemonInvestBatch.Domain.Parsing.PriceTier exactly. CardStock cannot
/// reference that assembly, so this is a copy and the two must stay in step.
///
/// Stored as <c>integer</c> in price_months.tier -- verified in the crawler's
/// 20260728032826_InitialCreate.cs:134, which is NOT the smallint used for
/// populations.grade. NEVER reorder or insert a member: the ordinal IS the
/// stored value, so a change here silently misreads every historical price.
/// PriceTierTests enforces this.
/// </summary>
public enum PriceTier
{
    Ungraded,
    Grade7,
    Grade8,
    Grade9,
    Grade9Half,
    Psa10,
}
```

- [x] **Step 4: Delete the Infrastructure copy and repoint its user**

Delete `src/CardStock.Infrastructure/Persistence/ScraperReadModels/PriceTier.cs`.

Add the using to `src/CardStock.Infrastructure/Persistence/ScraperReadModels/ScraperPriceMonth.cs` — the first line of the file becomes:

```csharp
using CardStock.Domain.Prices;

namespace CardStock.Infrastructure.Persistence.ScraperReadModels;
```

The `PriceTier Tier { get; init; }` property is unchanged; it now resolves to the Domain type.

- [x] **Step 5: Run the whole suite to verify nothing else referenced the old location**

Run: `dotnet build CardStock.slnx -c Release -m:1`
Expected: build succeeds with zero warnings. If any file fails to resolve `PriceTier`, add the same using there.

Run: `dotnet test CardStock.slnx -c Release --no-build -m:1`
Expected: PASS, including the two new tests.

> **If `dotnet test` hangs before any test runs**, it is MSBuild node contention, not the database (`ops/README.md`). Fix: `dotnet build-server shutdown && pkill -f MSBuild.dll`, then rebuild with `-m:1` and test with `--no-build -m:1`.

- [x] **Step 6: Commit**

```bash
git add -A
git commit -m "Move PriceTier into Domain, and pin its ordinals with a test

The pure price rules need this enum and Domain references nothing, so it
could not stay in Infrastructure.

Added the test while moving it: price_months.tier stores the ordinal as
an integer, so reordering the enum would silently reinterpret 10.3M
historical prices with no error anywhere. The comment warned about it;
now something checks."
```

---

### Task 2: Resolve raw rows into six series, latest observation wins

The change-only rule in one place. Every card returns exactly six series whether or not it has any prices — 11% of cards have none, and a short list would push that special case onto every caller.

**Files:**
- Create: `src/CardStock.Domain/Prices/PriceObservation.cs`, `MonthlyPrice.cs`, `TierSeries.cs`, `PriceSeriesBuilder.cs`
- Test: `tests/CardStock.Domain.Tests/Prices/PriceSeriesBuilderTests.cs`

**Interfaces:**
- Consumes: `PriceTier` (Task 1).
- Produces:
  - `PriceObservation(PriceTier Tier, DateOnly Month, int PriceCents, DateTimeOffset ObservedAt)`
  - `MonthlyPrice(DateOnly Month, int PriceCents, DateTimeOffset ObservedAt)`
  - `TierSeries(PriceTier Tier, IReadOnlyList<MonthlyPrice> Points)` with `IsEmpty`, `FirstMonth`, `LastMonth`
  - `PriceSeriesBuilder.Build(IEnumerable<PriceObservation>) → IReadOnlyList<TierSeries>` — always 6, strip order
  - `PriceSeriesBuilder.StripOrder → IReadOnlyList<PriceTier>`

- [x] **Step 1: Write the failing tests**

Create `tests/CardStock.Domain.Tests/Prices/PriceSeriesBuilderTests.cs`:

```csharp
using CardStock.Domain.Prices;

namespace CardStock.Domain.Tests.Prices;

public class PriceSeriesBuilderTests
{
    private static DateOnly M(int year, int month) => new(year, month, 1);

    private static DateTimeOffset At(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Every_card_returns_six_series_in_strip_order()
    {
        var series = PriceSeriesBuilder.Build([]);

        Assert.Equal(6, series.Count);
        Assert.Equal(
            [PriceTier.Psa10, PriceTier.Grade9Half, PriceTier.Grade9,
             PriceTier.Grade8, PriceTier.Grade7, PriceTier.Ungraded],
            series.Select(s => s.Tier));
    }

    [Fact]
    public void A_card_with_no_prices_returns_six_empty_series_not_an_empty_list()
    {
        var series = PriceSeriesBuilder.Build([]);

        Assert.All(series, s => Assert.True(s.IsEmpty));
        Assert.All(series, s => Assert.Null(s.FirstMonth));
        Assert.All(series, s => Assert.Null(s.LastMonth));
    }

    /// <summary>
    /// The whole reason this layer exists. price_months appends rather than
    /// updates, so the current month legitimately carries several rows -- 17,804
    /// of them across the corpus. Charizard #24 held two for 2026-08-01 on the
    /// day this was written.
    /// </summary>
    [Fact]
    public void Two_rows_for_one_month_resolve_to_the_later_observation()
    {
        var series = PriceSeriesBuilder.Build([
            new PriceObservation(PriceTier.Psa10, M(2026, 8), 2861, At(2026, 8, 3)),
            new PriceObservation(PriceTier.Psa10, M(2026, 8), 2500, At(2026, 8, 11)),
        ]);

        var psa10 = series.Single(s => s.Tier == PriceTier.Psa10);
        var point = Assert.Single(psa10.Points);
        Assert.Equal(2500, point.PriceCents);
    }

    /// <summary>
    /// Same data, reversed. "The last one in the list" is the bug this rule
    /// exists to prevent, and it passes the test above by accident.
    /// </summary>
    [Fact]
    public void Resolution_does_not_depend_on_the_order_rows_arrive_in()
    {
        var series = PriceSeriesBuilder.Build([
            new PriceObservation(PriceTier.Psa10, M(2026, 8), 2500, At(2026, 8, 11)),
            new PriceObservation(PriceTier.Psa10, M(2026, 8), 2861, At(2026, 8, 3)),
        ]);

        var point = Assert.Single(series.Single(s => s.Tier == PriceTier.Psa10).Points);
        Assert.Equal(2500, point.PriceCents);
    }

    [Fact]
    public void Points_come_back_ascending_by_month_whatever_order_they_arrived()
    {
        var series = PriceSeriesBuilder.Build([
            new PriceObservation(PriceTier.Grade9, M(2026, 3), 300, At(2026, 3, 2)),
            new PriceObservation(PriceTier.Grade9, M(2026, 1), 100, At(2026, 1, 2)),
            new PriceObservation(PriceTier.Grade9, M(2026, 2), 200, At(2026, 2, 2)),
        ]);

        var grade9 = series.Single(s => s.Tier == PriceTier.Grade9);
        Assert.Equal([M(2026, 1), M(2026, 2), M(2026, 3)], grade9.Points.Select(p => p.Month));
        Assert.Equal(M(2026, 1), grade9.FirstMonth);
        Assert.Equal(M(2026, 3), grade9.LastMonth);
    }

    [Fact]
    public void Tiers_do_not_bleed_into_each_other()
    {
        var series = PriceSeriesBuilder.Build([
            new PriceObservation(PriceTier.Psa10, M(2026, 6), 1000, At(2026, 6, 2)),
            new PriceObservation(PriceTier.Ungraded, M(2026, 6), 50, At(2026, 6, 2)),
        ]);

        Assert.Equal(1000, series.Single(s => s.Tier == PriceTier.Psa10).Points[0].PriceCents);
        Assert.Equal(50, series.Single(s => s.Tier == PriceTier.Ungraded).Points[0].PriceCents);
        Assert.True(series.Single(s => s.Tier == PriceTier.Grade8).IsEmpty);
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CardStock.Domain.Tests -c Release --filter PriceSeriesBuilderTests`
Expected: FAIL — `PriceObservation`, `MonthlyPrice`, `TierSeries`, `PriceSeriesBuilder` do not exist.

- [x] **Step 3: Write the four types**

Create `src/CardStock.Domain/Prices/PriceObservation.cs`:

```csharp
namespace CardStock.Domain.Prices;

/// <summary>
/// One row of price_months, as Domain sees it. Domain cannot reference the EF
/// mirror, and should not: this is the only shape the rules need.
/// </summary>
public sealed record PriceObservation(
    PriceTier Tier,
    DateOnly Month,
    int PriceCents,
    DateTimeOffset ObservedAt);
```

Create `src/CardStock.Domain/Prices/MonthlyPrice.cs`:

```csharp
namespace CardStock.Domain.Prices;

/// <summary>One month of one tier, already resolved to a single observation.</summary>
public sealed record MonthlyPrice(DateOnly Month, int PriceCents, DateTimeOffset ObservedAt);
```

Create `src/CardStock.Domain/Prices/TierSeries.cs`:

```csharp
namespace CardStock.Domain.Prices;

/// <summary>
/// One tier's published history, ascending by month.
///
/// Holds ONLY the months that have data. It is never padded and never filled:
/// a month absent here means the source published no point for it, and 33% of
/// real series contain at least one such hole.
/// </summary>
public sealed record TierSeries(PriceTier Tier, IReadOnlyList<MonthlyPrice> Points)
{
    public bool IsEmpty => Points.Count == 0;

    public DateOnly? FirstMonth => IsEmpty ? null : Points[0].Month;

    public DateOnly? LastMonth => IsEmpty ? null : Points[^1].Month;
}
```

Create `src/CardStock.Domain/Prices/PriceSeriesBuilder.cs`:

```csharp
namespace CardStock.Domain.Prices;

/// <summary>
/// Turns raw price_months rows into six resolved series.
///
/// This is where the change-only contract is honoured: a (tier, month) cell can
/// carry several rows because the current month revises between visits, so the
/// one with the greatest ObservedAt wins. It fires on roughly 0.17% of rows,
/// which is exactly why it must be encoded once rather than remembered.
/// </summary>
public static class PriceSeriesBuilder
{
    /// <summary>
    /// Descending by grade with Raw last, matching the Card page's fixed
    /// six-cell grid (Cardstock Card.dc.html:395). Callers rely on this order,
    /// so the list is always six long and always in it.
    /// </summary>
    public static IReadOnlyList<PriceTier> StripOrder { get; } =
    [
        PriceTier.Psa10,
        PriceTier.Grade9Half,
        PriceTier.Grade9,
        PriceTier.Grade8,
        PriceTier.Grade7,
        PriceTier.Ungraded,
    ];

    public static IReadOnlyList<TierSeries> Build(IEnumerable<PriceObservation> observations)
    {
        var byTier = observations
            .GroupBy(o => o.Tier)
            .ToDictionary(
                tier => tier.Key,
                tier => (IReadOnlyList<MonthlyPrice>)tier
                    .GroupBy(o => o.Month)
                    // MaxBy cannot tie: observed_at is part of the primary key,
                    // so one (card, tier, month) never holds two identical stamps.
                    .Select(month => month.MaxBy(o => o.ObservedAt)!)
                    .OrderBy(o => o.Month)
                    .Select(o => new MonthlyPrice(o.Month, o.PriceCents, o.ObservedAt))
                    .ToList());

        return [.. StripOrder.Select(tier =>
            new TierSeries(tier, byTier.GetValueOrDefault(tier, [])))];
    }
}
```

- [x] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CardStock.Domain.Tests -c Release --filter PriceSeriesBuilderTests`
Expected: PASS, 6 tests.

- [x] **Step 5: Commit**

```bash
git add -A
git commit -m "Resolve price rows into six series, latest observation wins

The change-only contract in one place: a (tier, month) cell can carry
several rows because the current month revises between visits, so the
greatest observed_at wins. It fires on 0.17% of rows, which is precisely
why it has to be encoded once instead of remembered at each call site.

Six series always, even for the 11% of cards with no prices at all --
returning a short list would push that case onto every caller.

The order-independence test matters more than it looks: 'take the last
row in the list' passes the straightforward version by accident."
```

---

### Task 3: Window a series into slots, keeping holes as holes

A caller asking for twelve months gets twelve slots. Three of them mean different things and the type system keeps them apart, because a hole in the middle of a series and the series not having started yet must never draw the same way.

**Files:**
- Create: `src/CardStock.Domain/Prices/PriceSlot.cs`, `PriceWindow.cs`
- Test: `tests/CardStock.Domain.Tests/Prices/PriceWindowTests.cs`

**Interfaces:**
- Consumes: `TierSeries`, `MonthlyPrice` (Task 2).
- Produces:
  - `PriceSlot(DateOnly Month)` abstract, with `ObservedPrice(DateOnly, int PriceCents, DateTimeOffset)`, `MissingMonth(DateOnly)`, `OutsideSeries(DateOnly)`
  - `PriceWindow.Of(TierSeries series, DateOnly endMonth, int months) → IReadOnlyList<PriceSlot>` — ascending, oldest first, `endMonth` last

- [x] **Step 1: Write the failing tests**

Create `tests/CardStock.Domain.Tests/Prices/PriceWindowTests.cs`:

```csharp
using CardStock.Domain.Prices;

namespace CardStock.Domain.Tests.Prices;

public class PriceWindowTests
{
    private static DateOnly M(int year, int month) => new(year, month, 1);

    private static MonthlyPrice P(int year, int month, int cents) =>
        new(M(year, month), cents, new DateTimeOffset(year, month, 2, 0, 0, 0, TimeSpan.Zero));

    private static TierSeries Series(params MonthlyPrice[] points) =>
        new(PriceTier.Psa10, points);

    [Fact]
    public void The_window_is_one_slot_per_month_oldest_first_ending_at_the_month_asked_for()
    {
        var slots = PriceWindow.Of(Series(P(2026, 6, 100)), M(2026, 8), 3);

        Assert.Equal([M(2026, 6), M(2026, 7), M(2026, 8)], slots.Select(s => s.Month));
    }

    [Fact]
    public void A_month_with_a_point_is_observed_and_carries_its_price()
    {
        var slots = PriceWindow.Of(Series(P(2026, 8, 1486)), M(2026, 8), 1);

        var observed = Assert.IsType<ObservedPrice>(Assert.Single(slots));
        Assert.Equal(1486, observed.PriceCents);
    }

    /// <summary>
    /// The Charmeleon #24 case, from the live database: Grade 8 runs 2021-05 to
    /// 2026-08 with September 2021 missing, $299.99 before it and $40.00 after.
    /// Carrying the earlier value across the hole would draw an 87% single-month
    /// crash that never happened.
    /// </summary>
    [Fact]
    public void A_hole_inside_the_series_is_a_gap_and_never_a_carried_value()
    {
        var slots = PriceWindow.Of(
            Series(P(2021, 8, 29999), P(2021, 10, 4000)),
            M(2021, 10), 3);

        Assert.IsType<ObservedPrice>(slots[0]);
        Assert.IsType<MissingMonth>(slots[1]);
        Assert.IsType<ObservedPrice>(slots[2]);
        Assert.Equal(M(2021, 9), slots[1].Month);
    }

    [Fact]
    public void Months_before_the_series_starts_are_outside_it_not_gaps()
    {
        var slots = PriceWindow.Of(Series(P(2026, 8, 100)), M(2026, 8), 3);

        Assert.IsType<OutsideSeries>(slots[0]);
        Assert.IsType<OutsideSeries>(slots[1]);
        Assert.IsType<ObservedPrice>(slots[2]);
    }

    [Fact]
    public void Months_after_the_series_ends_are_outside_it_not_gaps()
    {
        var slots = PriceWindow.Of(Series(P(2026, 6, 100)), M(2026, 8), 3);

        Assert.IsType<ObservedPrice>(slots[0]);
        Assert.IsType<OutsideSeries>(slots[1]);
        Assert.IsType<OutsideSeries>(slots[2]);
    }

    [Fact]
    public void An_empty_series_is_outside_everywhere()
    {
        var slots = PriceWindow.Of(new TierSeries(PriceTier.Grade7, []), M(2026, 8), 12);

        Assert.Equal(12, slots.Count);
        Assert.All(slots, s => Assert.IsType<OutsideSeries>(s));
    }

    [Fact]
    public void The_window_crosses_a_year_boundary_correctly()
    {
        var slots = PriceWindow.Of(Series(P(2025, 12, 100)), M(2026, 2), 3);

        Assert.Equal([M(2025, 12), M(2026, 1), M(2026, 2)], slots.Select(s => s.Month));
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CardStock.Domain.Tests -c Release --filter PriceWindowTests`
Expected: FAIL — `PriceWindow`, `ObservedPrice`, `MissingMonth`, `OutsideSeries` do not exist.

- [x] **Step 3: Write the slot hierarchy and the windowing function**

Create `src/CardStock.Domain/Prices/PriceSlot.cs`:

```csharp
namespace CardStock.Domain.Prices;

/// <summary>
/// One month of a windowed series. Exactly one of three things, and only one of
/// them carries a number -- so drawing a line across a hole is a compile error
/// rather than a rule somebody has to remember.
/// </summary>
public abstract record PriceSlot(DateOnly Month);

/// <summary>The source published a price for this month.</summary>
public sealed record ObservedPrice(DateOnly Month, int PriceCents, DateTimeOffset ObservedAt)
    : PriceSlot(Month);

/// <summary>
/// Inside the series, but the source published nothing for this month. A HOLE,
/// with real data either side: the line must break here. Distinct from
/// OutsideSeries, because drawing them alike would claim the card's history
/// begins later than it does.
/// </summary>
public sealed record MissingMonth(DateOnly Month) : PriceSlot(Month);

/// <summary>
/// Before the series' first month or after its last. Not a hole -- there is
/// simply no series here yet, or not any more.
/// </summary>
public sealed record OutsideSeries(DateOnly Month) : PriceSlot(Month);
```

Create `src/CardStock.Domain/Prices/PriceWindow.cs`:

```csharp
namespace CardStock.Domain.Prices;

/// <summary>Projects a series onto a fixed run of months, one slot each.</summary>
public static class PriceWindow
{
    /// <param name="endMonth">The newest month in the window, inclusive.</param>
    /// <param name="months">How many months, counting back from endMonth.</param>
    public static IReadOnlyList<PriceSlot> Of(TierSeries series, DateOnly endMonth, int months)
    {
        var points = series.Points.ToDictionary(p => p.Month);

        return [.. Enumerable.Range(0, months)
            .Select(offset => endMonth.AddMonths(offset - months + 1))
            .Select(month => Slot(series, points, month))];
    }

    private static PriceSlot Slot(
        TierSeries series, IReadOnlyDictionary<DateOnly, MonthlyPrice> points, DateOnly month)
    {
        if (points.TryGetValue(month, out var point))
        {
            return new ObservedPrice(month, point.PriceCents, point.ObservedAt);
        }

        // No first/last month means no series at all, so nothing can be "inside" it.
        return series.FirstMonth is { } first && series.LastMonth is { } last
               && month >= first && month <= last
            ? new MissingMonth(month)
            : new OutsideSeries(month);
    }
}
```

- [x] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CardStock.Domain.Tests -c Release --filter PriceWindowTests`
Expected: PASS, 7 tests.

- [x] **Step 5: Commit**

```bash
git add -A
git commit -m "Window a series into slots, keeping holes as holes

Three slot kinds and only one carries a number, so drawing a line across
a hole is a compile error rather than a rule someone has to remember.

MissingMonth and OutsideSeries are deliberately separate. A hole has
real data either side and the line must break; being outside the series
means it has not started, and the axis simply has nothing there. Drawing
them alike would claim a card's history begins later than it does.

The gap test uses Charmeleon #24 from the live database -- Grade 8 with
September 2021 missing, \$299.99 before and \$40.00 after. Carrying the
earlier value across would draw an 87% crash that never happened."
```

---

### Task 4: Decide whether a tier's newest price is worth showing

Three outcomes, measured rather than chosen: 81% of real series are current-month, 15% sit one month behind while perfectly healthy, and 3.5% are two or more behind and genuinely dead.

**Files:**
- Create: `src/CardStock.Domain/Prices/TierPrice.cs`, `PriceStaleness.cs`
- Test: `tests/CardStock.Domain.Tests/Prices/PriceStalenessTests.cs`

**Interfaces:**
- Consumes: `TierSeries` (Task 2).
- Produces:
  - `TierPrice` abstract, with `PriceAvailable(int PriceCents, DateOnly Month, bool IsCurrentMonth)`, `PriceStale(DateOnly NewestMonth)`, `NoPriceSeries()`
  - `PriceStaleness.MaxMonthsBehind` (const int, 1)
  - `PriceStaleness.Evaluate(TierSeries series, DateOnly currentMonth) → TierPrice`

- [x] **Step 1: Write the failing tests**

Create `tests/CardStock.Domain.Tests/Prices/PriceStalenessTests.cs`:

```csharp
using CardStock.Domain.Prices;

namespace CardStock.Domain.Tests.Prices;

public class PriceStalenessTests
{
    private static DateOnly M(int year, int month) => new(year, month, 1);

    private static TierSeries SeriesEndingAt(int year, int month, int cents = 1486) =>
        new(PriceTier.Psa10, [
            new MonthlyPrice(M(year, month), cents,
                new DateTimeOffset(year, month, 2, 0, 0, 0, TimeSpan.Zero)),
        ]);

    [Fact]
    public void A_price_from_the_current_month_is_available_and_marked_as_this_month()
    {
        var price = PriceStaleness.Evaluate(SeriesEndingAt(2026, 8), M(2026, 8));

        var available = Assert.IsType<PriceAvailable>(price);
        Assert.Equal(1486, available.PriceCents);
        Assert.True(available.IsCurrentMonth);
    }

    /// <summary>
    /// The 15% case, and the one most likely to be got wrong, because "does the
    /// price render" and "does the provisional marker show" look like one
    /// decision and are two. Early in a month the source has not yet posted an
    /// average for every tier, so one month behind is healthy, not stale.
    /// </summary>
    [Fact]
    public void A_price_from_last_month_still_renders_but_is_not_this_month()
    {
        var price = PriceStaleness.Evaluate(SeriesEndingAt(2026, 7), M(2026, 8));

        var available = Assert.IsType<PriceAvailable>(price);
        Assert.Equal(1486, available.PriceCents);
        Assert.False(available.IsCurrentMonth);
    }

    [Fact]
    public void A_price_two_months_behind_is_stale_and_carries_no_number()
    {
        var price = PriceStaleness.Evaluate(SeriesEndingAt(2026, 6), M(2026, 8));

        var stale = Assert.IsType<PriceStale>(price);
        Assert.Equal(M(2026, 6), stale.NewestMonth);
    }

    [Fact]
    public void A_grade_that_last_traded_years_ago_is_stale()
    {
        var price = PriceStaleness.Evaluate(SeriesEndingAt(2022, 3), M(2026, 8));

        Assert.IsType<PriceStale>(price);
    }

    [Fact]
    public void A_tier_that_never_had_a_price_says_so()
    {
        var price = PriceStaleness.Evaluate(new TierSeries(PriceTier.Grade7, []), M(2026, 8));

        Assert.IsType<NoPriceSeries>(price);
    }

    [Fact]
    public void Staleness_counts_months_not_days_across_a_year_boundary()
    {
        Assert.IsType<PriceAvailable>(PriceStaleness.Evaluate(SeriesEndingAt(2025, 12), M(2026, 1)));
        Assert.IsType<PriceStale>(PriceStaleness.Evaluate(SeriesEndingAt(2025, 11), M(2026, 1)));
    }

    [Fact]
    public void The_newest_point_is_used_even_when_older_points_exist()
    {
        var series = new TierSeries(PriceTier.Psa10, [
            new MonthlyPrice(M(2026, 6), 100, new DateTimeOffset(2026, 6, 2, 0, 0, 0, TimeSpan.Zero)),
            new MonthlyPrice(M(2026, 8), 999, new DateTimeOffset(2026, 8, 2, 0, 0, 0, TimeSpan.Zero)),
        ]);

        var available = Assert.IsType<PriceAvailable>(PriceStaleness.Evaluate(series, M(2026, 8)));
        Assert.Equal(999, available.PriceCents);
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CardStock.Domain.Tests -c Release --filter PriceStalenessTests`
Expected: FAIL — `PriceStaleness`, `PriceAvailable`, `PriceStale`, `NoPriceSeries` do not exist.

- [x] **Step 3: Write the outcomes and the rule**

Create `src/CardStock.Domain/Prices/TierPrice.cs`:

```csharp
namespace CardStock.Domain.Prices;

/// <summary>
/// What the strip's top line shows for one tier. Only one case carries a
/// number; the other two render a dash.
/// </summary>
public abstract record TierPrice;

/// <summary>
/// A price recent enough to stand as current.
/// <paramref name="IsCurrentMonth"/> drives the provisional marker, and is a
/// separate question from whether the price renders at all -- a price from last
/// month renders without the marker.
/// </summary>
public sealed record PriceAvailable(int PriceCents, DateOnly Month, bool IsCurrentMonth) : TierPrice;

/// <summary>
/// The newest published month is too far back to present as a current price.
/// A grade nobody has traded in a while; 3.5% of real series.
/// </summary>
public sealed record PriceStale(DateOnly NewestMonth) : TierPrice;

/// <summary>
/// The source has never published a price at this grade for this card. Only 19%
/// of cards carry all six tiers, so this is ordinary, not exceptional.
/// </summary>
public sealed record NoPriceSeries : TierPrice;
```

Create `src/CardStock.Domain/Prices/PriceStaleness.cs`:

```csharp
namespace CardStock.Domain.Prices;

public static class PriceStaleness
{
    /// <summary>
    /// How far behind the current month a price may be and still render.
    ///
    /// One, and it was measured rather than chosen. Across a 500-card sample
    /// (1,802 series, 2026-08-11): 81.3% current month, 15.2% one behind, 3.5%
    /// two or more. The 15% are healthy -- early in a month the source has not
    /// yet posted an average for every tier -- so a current-month-only rule
    /// would have dashed 19% of series for no reason.
    /// </summary>
    public const int MaxMonthsBehind = 1;

    public static TierPrice Evaluate(TierSeries series, DateOnly currentMonth)
    {
        if (series.IsEmpty)
        {
            return new NoPriceSeries();
        }

        var newest = series.Points[^1];
        var behind = MonthsBetween(newest.Month, currentMonth);

        return behind > MaxMonthsBehind
            ? new PriceStale(newest.Month)
            : new PriceAvailable(newest.PriceCents, newest.Month, behind == 0);
    }

    /// <summary>Whole months from earlier to later; negative if later precedes earlier.</summary>
    internal static int MonthsBetween(DateOnly earlier, DateOnly later) =>
        ((later.Year - earlier.Year) * 12) + later.Month - earlier.Month;
}
```

- [x] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CardStock.Domain.Tests -c Release --filter PriceStalenessTests`
Expected: PASS, 7 tests.

- [x] **Step 5: Commit**

```bash
git add -A
git commit -m "Decide whether a tier's newest price is worth showing

Threshold is one month, measured rather than chosen. A 500-card sample
put 81% of series at the current month and 15% one behind -- and that
15% is healthy, because early in a month the source has not posted an
average for every tier yet. Current-month-only would have dashed 19% of
series for no reason; this dashes 3.5%, which are grades nobody has
traded in years.

Kept 'does the price render' and 'does the provisional marker show' as
two separate answers on one record. They look like one decision and are
not, which is where a bug would sit."
```

---

### Task 5: Map a sale's grade label onto a price tier

Six of nineteen labels map. The rest have no price series to change against, and an unrecognised label must join them rather than defaulting into the cell users read first.

**Files:**
- Create: `src/CardStock.Domain/Prices/GradeTierMap.cs`
- Test: `tests/CardStock.Domain.Tests/Prices/GradeTierMapTests.cs`

**Interfaces:**
- Consumes: `PriceTier` (Task 1).
- Produces: `GradeTierMap.ToPriceTier(string gradeTier) → PriceTier?`

- [x] **Step 1: Write the failing tests**

Create `tests/CardStock.Domain.Tests/Prices/GradeTierMapTests.cs`:

```csharp
using CardStock.Domain.Prices;

namespace CardStock.Domain.Tests.Prices;

public class GradeTierMapTests
{
    [Theory]
    [InlineData("Ungraded", PriceTier.Ungraded)]
    [InlineData("Grade 7", PriceTier.Grade7)]
    [InlineData("Grade 8", PriceTier.Grade8)]
    [InlineData("Grade 9", PriceTier.Grade9)]
    [InlineData("Grade 9.5", PriceTier.Grade9Half)]
    [InlineData("PSA 10", PriceTier.Psa10)]
    public void The_six_labels_with_a_price_series_map_to_it(string label, PriceTier expected)
    {
        Assert.Equal(expected, GradeTierMap.ToPriceTier(label));
    }

    /// <summary>
    /// price_months carries nothing below Grade 7 (D-012), so these sales have no
    /// price to change against. They still appear in the ledger.
    /// </summary>
    [Theory]
    [InlineData("Grade 1")]
    [InlineData("Grade 2")]
    [InlineData("Grade 3")]
    [InlineData("Grade 4")]
    [InlineData("Grade 5")]
    [InlineData("Grade 6")]
    public void Grades_below_seven_map_to_nothing(string label)
    {
        Assert.Null(GradeTierMap.ToPriceTier(label));
    }

    /// <summary>
    /// The source splits grading companies at 10 and price_months has exactly one
    /// grade-10 tier. Folding these into PSA 10 is the substitution D-022 and
    /// D-057 both rejected as statistically dishonest.
    /// </summary>
    [Theory]
    [InlineData("CGC 10")]
    [InlineData("CGC 10 Prist.")]
    [InlineData("BGS 10")]
    [InlineData("BGS 10 Black")]
    [InlineData("SGC 10")]
    [InlineData("TAG 10")]
    [InlineData("ACE 10")]
    public void Tens_from_other_graders_map_to_nothing(string label)
    {
        Assert.Null(GradeTierMap.ToPriceTier(label));
    }

    /// <summary>
    /// GradeTierVocabulary.cs:16-18 says the list grows -- TAG and ACE are recent.
    /// A deny-list would fold the next new grader's 10 into PSA 10 silently, with
    /// no error, in the cell users read first.
    /// </summary>
    [Fact]
    public void A_grader_that_does_not_exist_yet_maps_to_nothing()
    {
        Assert.Null(GradeTierMap.ToPriceTier("PGX 10"));
        Assert.Null(GradeTierMap.ToPriceTier("Grade 9.7"));
        Assert.Null(GradeTierMap.ToPriceTier(""));
    }

    [Theory]
    [InlineData("psa 10")]
    [InlineData("PSA  10")]
    [InlineData(" PSA 10 ")]
    [InlineData("PSA\n 10")]
    public void Casing_and_ragged_whitespace_still_match(string label)
    {
        Assert.Equal(PriceTier.Psa10, GradeTierMap.ToPriceTier(label));
    }

    /// <summary>The UI says Raw; the data says Ungraded. Only the UI may say Raw.</summary>
    [Fact]
    public void Raw_is_a_display_word_and_is_not_a_stored_value()
    {
        Assert.Null(GradeTierMap.ToPriceTier("Raw"));
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CardStock.Domain.Tests -c Release --filter GradeTierMapTests`
Expected: FAIL — `GradeTierMap` does not exist.

- [x] **Step 3: Write the map**

Create `src/CardStock.Domain/Prices/GradeTierMap.cs`:

```csharp
namespace CardStock.Domain.Prices;

/// <summary>
/// Which price series a sale belongs to, if any.
///
/// An ALLOW-LIST, and not as a matter of taste. The crawler's
/// GradeTierVocabulary.cs:16-18 records that the vocabulary grows -- "TAG and
/// ACE are recent" -- so a deny-list would quietly fold the next grading
/// company's 10 into the PSA 10 cell. That is the substitution D-022 and D-057
/// both rejected, it would happen without an error, and it would happen in the
/// cell users look at first.
///
/// Six of the nineteen labels map. The other thirteen have no price series to
/// change against; they still render in the sales ledger.
/// </summary>
public static class GradeTierMap
{
    private static readonly Dictionary<string, PriceTier> Tiers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Ungraded"] = PriceTier.Ungraded,
            ["Grade 7"] = PriceTier.Grade7,
            ["Grade 8"] = PriceTier.Grade8,
            ["Grade 9"] = PriceTier.Grade9,
            ["Grade 9.5"] = PriceTier.Grade9Half,
            ["PSA 10"] = PriceTier.Psa10,
        };

    public static PriceTier? ToPriceTier(string gradeTier) =>
        Tiers.TryGetValue(Squeeze(gradeTier), out var tier) ? tier : null;

    /// <summary>
    /// The source's option text arrives with nested spans and unclosed tags, so
    /// the same label reaches the database with varying whitespace. Mirrors the
    /// crawler's GradeTierVocabulary.Normalize so both sides agree on equality.
    /// </summary>
    private static string Squeeze(string label) =>
        string.Join(' ', label.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
```

- [x] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CardStock.Domain.Tests -c Release --filter GradeTierMapTests`
Expected: PASS, 25 test cases.

- [x] **Step 5: Commit**

```bash
git add -A
git commit -m "Map a sale's grade label onto a price tier, by allow-list

Six of nineteen labels map. Grades 1-6 have no price series below 7, and
the seven non-PSA tens have no tier of their own -- price_months carries
exactly one grade-10 series.

Allow-list rather than deny-list because GradeTierVocabulary.cs:16-18
says the list grows. A deny-list would fold the next grading company's
10 into the PSA 10 cell silently, in the cell users read first, which is
the substitution D-022 and D-057 both rejected.

Also asserts 'Raw' does not map: it is the display word for Ungraded and
must never reach a query or a mapping key."
```

---

### Task 6: Compute the thirty-day change from two windows of sales

Mean sale price over the last 30 days against the 30 before that. Below the threshold it renders a dash — permanently, not as a phase, because a quiet card will not have three sales in 30 days in 2028 either.

**Files:**
- Create: `src/CardStock.Domain/Prices/SaleObservation.cs`, `TierChange.cs`, `SalesChange.cs`
- Test: `tests/CardStock.Domain.Tests/Prices/SalesChangeTests.cs`

**Interfaces:**
- Consumes: `PriceTier` (Task 1).
- Produces:
  - `SaleObservation(PriceTier Tier, DateOnly SoldOn, int PriceCents)`
  - `TierChange` abstract, with `ChangeAvailable(decimal Fraction, int RecentSales, int PriorSales)` and `ChangeInsufficient(int RecentSales, int PriorSales)`
  - `SalesChange.MinimumSalesPerWindow` (const int, 3), `SalesChange.WindowDays` (const int, 30)
  - `SalesChange.Evaluate(IReadOnlyList<SaleObservation> sales, DateOnly today) → TierChange`

> **`Fraction`, not `Percent`.** `0.062m` means +6.2%. The spec's draft called it `Percent`, which invites a 100× error at the render boundary; the name now says which it is. Update the spec's §4.2 to match when this task lands.

- [x] **Step 1: Write the failing tests**

Create `tests/CardStock.Domain.Tests/Prices/SalesChangeTests.cs`:

```csharp
using CardStock.Domain.Prices;

namespace CardStock.Domain.Tests.Prices;

public class SalesChangeTests
{
    private static readonly DateOnly Today = new(2026, 12, 1);

    private static SaleObservation Sold(int daysAgo, int cents) =>
        new(PriceTier.Psa10, Today.AddDays(-daysAgo), cents);

    [Fact]
    public void Three_sales_in_each_window_produce_a_change()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 1100), Sold(5, 1100), Sold(20, 1100),
            Sold(35, 1000), Sold(40, 1000), Sold(55, 1000),
        ], Today);

        var available = Assert.IsType<ChangeAvailable>(change);
        Assert.Equal(0.10m, available.Fraction, 4);
        Assert.Equal(3, available.RecentSales);
        Assert.Equal(3, available.PriorSales);
    }

    [Fact]
    public void A_fall_produces_a_negative_change()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 900), Sold(5, 900), Sold(20, 900),
            Sold(35, 1000), Sold(40, 1000), Sold(55, 1000),
        ], Today);

        Assert.Equal(-0.10m, Assert.IsType<ChangeAvailable>(change).Fraction, 4);
    }

    [Fact]
    public void Too_few_recent_sales_is_insufficient_and_carries_no_number()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 1100), Sold(5, 1100),
            Sold(35, 1000), Sold(40, 1000), Sold(55, 1000),
        ], Today);

        var insufficient = Assert.IsType<ChangeInsufficient>(change);
        Assert.Equal(2, insufficient.RecentSales);
        Assert.Equal(3, insufficient.PriorSales);
    }

    [Fact]
    public void Too_few_prior_sales_is_insufficient_even_when_recent_is_healthy()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 1100), Sold(5, 1100), Sold(20, 1100),
            Sold(35, 1000),
        ], Today);

        var insufficient = Assert.IsType<ChangeInsufficient>(change);
        Assert.Equal(3, insufficient.RecentSales);
        Assert.Equal(1, insufficient.PriorSales);
    }

    /// <summary>Every card looks like this until roughly November 2026.</summary>
    [Fact]
    public void No_sales_at_all_is_insufficient_with_zero_counts()
    {
        var insufficient = Assert.IsType<ChangeInsufficient>(SalesChange.Evaluate([], Today));

        Assert.Equal(0, insufficient.RecentSales);
        Assert.Equal(0, insufficient.PriorSales);
    }

    [Fact]
    public void A_sale_thirty_days_old_falls_in_the_recent_window()
    {
        var change = SalesChange.Evaluate([
            Sold(30, 1100), Sold(1, 1100), Sold(5, 1100),
            Sold(35, 1000), Sold(40, 1000), Sold(45, 1000),
        ], Today);

        Assert.Equal(3, Assert.IsType<ChangeAvailable>(change).RecentSales);
    }

    [Fact]
    public void A_sale_thirty_one_days_old_falls_in_the_prior_window()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 1100), Sold(5, 1100), Sold(20, 1100),
            Sold(31, 1000), Sold(40, 1000), Sold(45, 1000),
        ], Today);

        var available = Assert.IsType<ChangeAvailable>(change);
        Assert.Equal(3, available.RecentSales);
        Assert.Equal(3, available.PriorSales);
    }

    [Fact]
    public void A_sale_sixty_days_old_is_the_oldest_that_still_counts()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 1100), Sold(5, 1100), Sold(20, 1100),
            Sold(35, 1000), Sold(40, 1000), Sold(60, 1000),
        ], Today);

        Assert.Equal(3, Assert.IsType<ChangeAvailable>(change).PriorSales);
    }

    [Fact]
    public void A_sale_sixty_one_days_old_falls_outside_both_windows()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 1100), Sold(5, 1100), Sold(20, 1100),
            Sold(35, 1000), Sold(40, 1000), Sold(61, 1000),
        ], Today);

        Assert.Equal(2, Assert.IsType<ChangeInsufficient>(change).PriorSales);
    }

    [Fact]
    public void Sales_older_than_sixty_days_are_ignored_entirely()
    {
        var change = SalesChange.Evaluate([
            Sold(1, 1100), Sold(5, 1100), Sold(20, 1100),
            Sold(35, 1000), Sold(40, 1000), Sold(45, 1000),
            Sold(200, 1), Sold(365, 1), Sold(400, 1),
        ], Today);

        var available = Assert.IsType<ChangeAvailable>(change);
        Assert.Equal(3, available.PriorSales);
        Assert.Equal(0.10m, available.Fraction, 4);
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CardStock.Domain.Tests -c Release --filter SalesChangeTests`
Expected: FAIL — `SalesChange`, `SaleObservation`, `ChangeAvailable`, `ChangeInsufficient` do not exist.

- [x] **Step 3: Write the types and the calculation**

Create `src/CardStock.Domain/Prices/SaleObservation.cs`:

```csharp
namespace CardStock.Domain.Prices;

/// <summary>
/// One row of sales, with its grade label already resolved to a price tier by
/// GradeTierMap. Sales whose label maps to nothing never reach Domain.
/// </summary>
public sealed record SaleObservation(PriceTier Tier, DateOnly SoldOn, int PriceCents);
```

Create `src/CardStock.Domain/Prices/TierChange.cs`:

```csharp
namespace CardStock.Domain.Prices;

/// <summary>
/// What the strip's bottom line shows. Only one case carries a number; the
/// other renders a dash.
/// </summary>
public abstract record TierChange;

/// <summary><paramref name="Fraction"/> is a fraction, not a percentage: 0.062m is +6.2%.</summary>
public sealed record ChangeAvailable(decimal Fraction, int RecentSales, int PriorSales) : TierChange;

/// <summary>
/// Too few sales in one or both windows. A PERMANENT possibility, not a phase
/// of the data filling in: a quiet card will not have three sales in 30 days in
/// 2028 either. Renders a dash, with no countdown and no unlock date -- see
/// D-075, where a countdown was proposed and deliberately rejected.
/// </summary>
public sealed record ChangeInsufficient(int RecentSales, int PriorSales) : TierChange;
```

Create `src/CardStock.Domain/Prices/SalesChange.cs`:

```csharp
namespace CardStock.Domain.Prices;

/// <summary>
/// The thirty-day movement: mean sale price over the last 30 days against the
/// mean over the 30 before that.
///
/// Both windows are fixed and never widen. Today they return a handful of rows;
/// in a year they return a full window; the code is identical either way, which
/// is the point -- there is no early-days special case to unpick later.
/// </summary>
public static class SalesChange
{
    public const int WindowDays = 30;

    /// <summary>
    /// How many sales each window needs before a change is worth stating.
    /// Deliberately one number in one place: it cannot be tuned from evidence
    /// until real windows exist (~Nov 2026), and tuning it must stay a value
    /// change rather than a rewrite.
    /// </summary>
    public const int MinimumSalesPerWindow = 3;

    public static TierChange Evaluate(IReadOnlyList<SaleObservation> sales, DateOnly today)
    {
        var recentFrom = today.AddDays(-WindowDays);
        var priorFrom = today.AddDays(-WindowDays * 2);

        var recent = sales.Where(s => s.SoldOn >= recentFrom).ToList();
        var prior = sales.Where(s => s.SoldOn >= priorFrom && s.SoldOn < recentFrom).ToList();

        if (recent.Count < MinimumSalesPerWindow || prior.Count < MinimumSalesPerWindow)
        {
            return new ChangeInsufficient(recent.Count, prior.Count);
        }

        var recentMean = recent.Average(s => (decimal)s.PriceCents);
        var priorMean = prior.Average(s => (decimal)s.PriceCents);

        // Unreachable against today's data -- price_cents = 0 occurs in 0 of
        // 10.3M rows, and a mean of three positive integers cannot be zero. Kept
        // because the alternative to two cheap lines is a divide-by-zero crash
        // if that ever stops being true upstream.
        return priorMean == 0m
            ? new ChangeInsufficient(recent.Count, prior.Count)
            : new ChangeAvailable((recentMean - priorMean) / priorMean, recent.Count, prior.Count);
    }
}
```

- [x] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CardStock.Domain.Tests -c Release --filter SalesChangeTests`
Expected: PASS, 10 tests.

- [x] **Step 5: Update the spec's field name**

In `docs/superpowers/specs/2026-08-12-price-read-layer-design.md` §4.2, change
`ChangeAvailable(decimal Percent, ...)` to `ChangeAvailable(decimal Fraction, ...)` and add after the
record block: *"`Fraction` is a fraction, not a percentage: `0.062m` is +6.2%. Named to remove the
100× ambiguity at the render boundary."*

- [x] **Step 6: Commit**

```bash
git add -A
git commit -m "Compute the thirty-day change from two windows of sales

Mean sale price over the last 30 days against the 30 before that. Both
windows are fixed and never widen -- today they return a handful of
rows, in a year a full window, and the code is identical. No early-days
special case to unpick later.

Below the threshold it renders a dash, permanently rather than as a
phase: a quiet card will not have three sales in 30 days in 2028 either.
No countdown and no unlock date, which D-075 rejected on the grounds
that the logic would outlive the problem by years.

Named the field Fraction rather than Percent. 0.062m is +6.2%, and the
old name invited a 100x error at the render boundary."
```

---

### Task 7: Assemble the snapshot, and publish the Application contract

Everything above, joined into the one shape a caller receives. Six tiers, always, each carrying its series, its price and its change.

**Files:**
- Create: `src/CardStock.Domain/Prices/CardPriceSnapshot.cs`, `CardPriceSnapshotBuilder.cs`
- Create: `src/CardStock.Application/Prices/ICardPriceReader.cs`
- Test: `tests/CardStock.Domain.Tests/Prices/CardPriceSnapshotBuilderTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 2, 4 and 6.
- Produces:
  - `CardPriceSnapshot(long CardId, DateTimeOffset? LastVisitedAt, IReadOnlyList<TierSnapshot> Tiers)`
  - `TierSnapshot(PriceTier Tier, TierSeries Series, TierPrice Price, TierChange Change)`
  - `CardPriceSnapshotBuilder.Build(long cardId, DateTimeOffset? lastVisitedAt, IEnumerable<PriceObservation> prices, IEnumerable<SaleObservation> sales, DateOnly today) → CardPriceSnapshot`
  - `ICardPriceReader.GetAsync(long cardId, CancellationToken) → Task<CardPriceSnapshot?>`

- [x] **Step 1: Write the failing tests**

Create `tests/CardStock.Domain.Tests/Prices/CardPriceSnapshotBuilderTests.cs`:

```csharp
using CardStock.Domain.Prices;

namespace CardStock.Domain.Tests.Prices;

public class CardPriceSnapshotBuilderTests
{
    private static readonly DateOnly Today = new(2026, 8, 12);
    private static readonly DateTimeOffset Visited = new(2026, 8, 9, 14, 0, 0, TimeSpan.Zero);

    private static PriceObservation Price(PriceTier tier, int year, int month, int cents) =>
        new(tier, new DateOnly(year, month, 1), cents,
            new DateTimeOffset(year, month, 2, 0, 0, 0, TimeSpan.Zero));

    private static SaleObservation Sale(PriceTier tier, int daysAgo, int cents) =>
        new(tier, Today.AddDays(-daysAgo), cents);

    [Fact]
    public void The_snapshot_carries_the_card_id_and_when_we_last_looked()
    {
        var snapshot = CardPriceSnapshotBuilder.Build(42, Visited, [], [], Today);

        Assert.Equal(42, snapshot.CardId);
        Assert.Equal(Visited, snapshot.LastVisitedAt);
    }

    [Fact]
    public void A_card_with_nothing_still_returns_six_tiers()
    {
        var snapshot = CardPriceSnapshotBuilder.Build(42, null, [], [], Today);

        Assert.Equal(6, snapshot.Tiers.Count);
        Assert.All(snapshot.Tiers, t => Assert.IsType<NoPriceSeries>(t.Price));
        Assert.All(snapshot.Tiers, t => Assert.IsType<ChangeInsufficient>(t.Change));
        Assert.All(snapshot.Tiers, t => Assert.True(t.Series.IsEmpty));
    }

    [Fact]
    public void Price_and_change_land_on_the_tier_they_belong_to()
    {
        var snapshot = CardPriceSnapshotBuilder.Build(42, Visited,
            [Price(PriceTier.Psa10, 2026, 8, 148600)],
            [
                Sale(PriceTier.Psa10, 1, 1100), Sale(PriceTier.Psa10, 5, 1100), Sale(PriceTier.Psa10, 20, 1100),
                Sale(PriceTier.Psa10, 35, 1000), Sale(PriceTier.Psa10, 40, 1000), Sale(PriceTier.Psa10, 55, 1000),
            ],
            Today);

        var psa10 = snapshot.Tiers.Single(t => t.Tier == PriceTier.Psa10);
        Assert.Equal(148600, Assert.IsType<PriceAvailable>(psa10.Price).PriceCents);
        Assert.Equal(3, Assert.IsType<ChangeAvailable>(psa10.Change).RecentSales);

        var grade9 = snapshot.Tiers.Single(t => t.Tier == PriceTier.Grade9);
        Assert.IsType<NoPriceSeries>(grade9.Price);
        Assert.IsType<ChangeInsufficient>(grade9.Change);
    }

    /// <summary>
    /// The everyday case for four cards in five: a price with no series at some
    /// grades, and no change anywhere because sales are two weeks old.
    /// </summary>
    [Fact]
    public void A_typical_card_has_prices_at_some_tiers_and_no_change_anywhere()
    {
        var snapshot = CardPriceSnapshotBuilder.Build(42, Visited,
            [
                Price(PriceTier.Psa10, 2026, 8, 148600),
                Price(PriceTier.Grade9, 2026, 8, 84200),
                Price(PriceTier.Ungraded, 2026, 8, 45500),
            ],
            [Sale(PriceTier.Psa10, 3, 150000)],
            Today);

        Assert.Equal(3, snapshot.Tiers.Count(t => t.Price is PriceAvailable));
        Assert.Equal(3, snapshot.Tiers.Count(t => t.Price is NoPriceSeries));
        Assert.All(snapshot.Tiers, t => Assert.IsType<ChangeInsufficient>(t.Change));
    }

    [Fact]
    public void The_series_is_carried_so_the_chart_and_the_strip_cannot_disagree()
    {
        var snapshot = CardPriceSnapshotBuilder.Build(42, Visited,
            [Price(PriceTier.Psa10, 2026, 7, 100), Price(PriceTier.Psa10, 2026, 8, 200)],
            [], Today);

        var psa10 = snapshot.Tiers.Single(t => t.Tier == PriceTier.Psa10);
        Assert.Equal(2, psa10.Series.Points.Count);
        Assert.Equal(200, Assert.IsType<PriceAvailable>(psa10.Price).PriceCents);
        Assert.Equal(psa10.Series.Points[^1].PriceCents, ((PriceAvailable)psa10.Price).PriceCents);
    }

    /// <summary>The current month comes from the supplied date, never from the machine clock.</summary>
    [Fact]
    public void Staleness_is_judged_against_the_date_passed_in()
    {
        var prices = new[] { Price(PriceTier.Psa10, 2026, 8, 100) };

        var inAugust = CardPriceSnapshotBuilder.Build(42, null, prices, [], new DateOnly(2026, 8, 31));
        var inNovember = CardPriceSnapshotBuilder.Build(42, null, prices, [], new DateOnly(2026, 11, 1));

        Assert.True(Assert.IsType<PriceAvailable>(
            inAugust.Tiers.Single(t => t.Tier == PriceTier.Psa10).Price).IsCurrentMonth);
        Assert.IsType<PriceStale>(inNovember.Tiers.Single(t => t.Tier == PriceTier.Psa10).Price);
    }
}
```

- [x] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/CardStock.Domain.Tests -c Release --filter CardPriceSnapshotBuilderTests`
Expected: FAIL — `CardPriceSnapshot`, `TierSnapshot`, `CardPriceSnapshotBuilder` do not exist.

- [x] **Step 3: Write the contract and the assembler**

Create `src/CardStock.Domain/Prices/CardPriceSnapshot.cs`:

```csharp
namespace CardStock.Domain.Prices;

/// <summary>
/// Everything the price surfaces need for one card.
///
/// <paramref name="Tiers"/> is ALWAYS six, in strip order, however little the
/// card has. A short list would push "which tiers came back?" onto every caller,
/// and 11% of cards have no prices at all.
/// </summary>
public sealed record CardPriceSnapshot(
    long CardId,
    DateTimeOffset? LastVisitedAt,
    IReadOnlyList<TierSnapshot> Tiers);

/// <summary>
/// One tier. <paramref name="Price"/> is derivable from <paramref name="Series"/>
/// and is carried anyway, so no caller re-implements "newest point, unless it is
/// too old" -- the chart and the strip must never disagree about the same number.
/// </summary>
public sealed record TierSnapshot(
    PriceTier Tier,
    TierSeries Series,
    TierPrice Price,
    TierChange Change);
```

Create `src/CardStock.Domain/Prices/CardPriceSnapshotBuilder.cs`:

```csharp
namespace CardStock.Domain.Prices;

/// <summary>
/// Joins resolved prices and sales into the shape callers receive. Pure: the
/// current date arrives as an argument, so "what month is it" is a test input
/// rather than ambient state.
/// </summary>
public static class CardPriceSnapshotBuilder
{
    public static CardPriceSnapshot Build(
        long cardId,
        DateTimeOffset? lastVisitedAt,
        IEnumerable<PriceObservation> prices,
        IEnumerable<SaleObservation> sales,
        DateOnly today)
    {
        var currentMonth = new DateOnly(today.Year, today.Month, 1);

        var salesByTier = sales
            .GroupBy(s => s.Tier)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<SaleObservation>)g.ToList());

        return new CardPriceSnapshot(cardId, lastVisitedAt, [
            .. PriceSeriesBuilder.Build(prices).Select(series => new TierSnapshot(
                series.Tier,
                series,
                PriceStaleness.Evaluate(series, currentMonth),
                SalesChange.Evaluate(salesByTier.GetValueOrDefault(series.Tier, []), today))),
        ]);
    }
}
```

Create `src/CardStock.Application/Prices/ICardPriceReader.cs`:

```csharp
using CardStock.Domain.Prices;

namespace CardStock.Application.Prices;

/// <summary>
/// One card's prices. Returns null only when the card id is unknown -- a card
/// that exists but has no prices comes back with six empty tiers, because "we
/// have never seen a price for this" and "there is no such card" are different
/// answers and the Card page renders them differently.
/// </summary>
public interface ICardPriceReader
{
    Task<CardPriceSnapshot?> GetAsync(long cardId, CancellationToken cancellationToken = default);
}
```

- [x] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/CardStock.Domain.Tests -c Release --filter CardPriceSnapshotBuilderTests`
Expected: PASS, 6 tests.

- [x] **Step 5: Run the whole Domain suite**

Run: `dotnet test tests/CardStock.Domain.Tests -c Release`
Expected: PASS, all tests from Tasks 1–7.

- [x] **Step 6: Commit**

```bash
git add -A
git commit -m "Assemble the card price snapshot, and publish the reader contract

Six tiers always, each carrying its series, its price and its change.
The series rides along even though the price is derivable from it, so
the chart and the strip cannot disagree about the same number.

The current date is an argument rather than a clock read, which makes
'what month is it' a test input -- the staleness test drives one series
from current to stale by changing only the date.

The reader returns null only for an unknown card id. A card that exists
with no prices comes back with six empty tiers, because that is a
different answer and the page renders it differently."
```

---

### Task 8: Read it from Postgres

Two narrow queries, then Domain does the thinking. This is the first task that needs a database, so it is also where the queries get proven against real Postgres date arithmetic rather than against LINQ-to-objects.

**Files:**
- Modify: `src/CardStock.Infrastructure/Persistence/ScraperReadModels/ScraperCard.cs`
- Create: `src/CardStock.Infrastructure/Prices/CardPriceReader.cs`
- Test: `tests/CardStock.Integration.Tests/CardPriceReaderTests.cs`

**Interfaces:**
- Consumes: `ICardPriceReader` (Task 7), `CardPriceSnapshotBuilder` (Task 7), `GradeTierMap` (Task 5), `SalesChange.WindowDays` (Task 6).
- Produces: `CardPriceReader(CardStockDbContext db, TimeProvider time) : ICardPriceReader`.

- [x] **Step 1: Write the failing tests**

Create `tests/CardStock.Integration.Tests/CardPriceReaderTests.cs`:

```csharp
using CardStock.Domain.Prices;
using CardStock.Infrastructure.Persistence;
using CardStock.Infrastructure.Prices;
using CardStock.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Integration.Tests;

/// <summary>
/// The queries, against real PostgreSQL. Domain already proves the rules; these
/// prove the rows arrive in the shape Domain expects -- including date
/// arithmetic, which LINQ-to-objects cannot vouch for.
/// </summary>
public class CardPriceReaderTests : CardStockDatabaseTest
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 12, 0, 0, TimeSpan.Zero);

    private static CardPriceReader Reader(CardStockDbContext db) => new(db, new FixedClock(Now));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    /// <summary>
    /// Seeded with raw SQL because CardStock's model cannot write these tables --
    /// which is the guarantee ScraperReadTests exists to demonstrate.
    ///
    /// ExecuteSqlInterpolated, not ExecuteSqlRaw with an interpolated string: the
    /// former parameterises each hole, the latter concatenates and trips EF's
    /// raw-SQL analyzer, which TreatWarningsAsErrors turns into a build failure.
    /// </summary>
    private static async Task SeedCardAsync(
        CardStockDbContext db, long cardId, DateTimeOffset? lastVisited)
    {
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public.sets (id, slug, name, discovered_at, last_seen_at)
            VALUES (1, 'base-set', 'Base Set', now(), now())
            ON CONFLICT (id) DO NOTHING;
            """);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO public.cards (id, set_id, url, name, first_seen_at, last_seen_at,
                                       any_bucket_at_cap, failure_streak, last_visited_at)
             VALUES ({cardId}, 1, '/game/pokemon-base-set/test-card', 'Test Card',
                     now(), now(), false, 0, {lastVisited});
             """);
    }

    [SkippableFact]
    public async Task An_unknown_card_id_returns_null()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();

        Assert.Null(await Reader(db).GetAsync(999_999));
    }

    [SkippableFact]
    public async Task A_card_with_no_prices_returns_six_empty_tiers_not_null()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42, Now.AddDays(-3));

        var snapshot = await Reader(db).GetAsync(42);

        Assert.NotNull(snapshot);
        Assert.Equal(6, snapshot.Tiers.Count);
        Assert.All(snapshot.Tiers, t => Assert.IsType<NoPriceSeries>(t.Price));
    }

    [SkippableFact]
    public async Task Last_visited_at_comes_back_so_the_page_can_decide_about_refreshing()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        var visited = Now.AddDays(-3);
        await SeedCardAsync(db, 42, visited);

        var snapshot = await Reader(db).GetAsync(42);

        Assert.NotNull(snapshot);
        Assert.Equal(visited, snapshot.LastVisitedAt);
    }

    /// <summary>
    /// The Charmeleon #24 shape, from the live database: Grade 8 with September
    /// 2021 missing, $299.99 before it and $40.00 after. The gap must survive the
    /// round trip, and nothing may smooth the cliff.
    /// </summary>
    [SkippableFact]
    public async Task A_gap_in_the_month_axis_survives_the_query()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42, Now);
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public.price_months (card_id, tier, month, price_cents, observed_at) VALUES
              (42, 2, DATE '2021-08-01', 29999, now()),
              (42, 2, DATE '2021-10-01',  4000, now());
            """);

        var snapshot = await Reader(db).GetAsync(42);
        var grade8 = snapshot!.Tiers.Single(t => t.Tier == PriceTier.Grade8);
        var window = PriceWindow.Of(grade8.Series, new DateOnly(2021, 10, 1), 3);

        Assert.IsType<ObservedPrice>(window[0]);
        Assert.IsType<MissingMonth>(window[1]);
        Assert.IsType<ObservedPrice>(window[2]);
    }

    /// <summary>
    /// Two rows for one month, resolved through the real query rather than in
    /// memory. Charizard #24 held exactly this for 2026-08-01.
    /// </summary>
    [SkippableFact]
    public async Task The_later_observation_of_a_revised_month_wins()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42, Now);
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public.price_months (card_id, tier, month, price_cents, observed_at) VALUES
              (42, 5, DATE '2026-08-01', 2861, TIMESTAMPTZ '2026-08-03 00:00:00Z'),
              (42, 5, DATE '2026-08-01', 2500, TIMESTAMPTZ '2026-08-11 00:00:00Z');
            """);

        var snapshot = await Reader(db).GetAsync(42);
        var psa10 = snapshot!.Tiers.Single(t => t.Tier == PriceTier.Psa10);

        Assert.Equal(2500, Assert.Single(psa10.Series.Points).PriceCents);
        Assert.Equal(2500, Assert.IsType<PriceAvailable>(psa10.Price).PriceCents);
    }

    [SkippableFact]
    public async Task Sales_land_in_the_right_window_under_real_date_arithmetic()
    {
        Skip.IfNot(Available, "CARDSTOCK_TEST_DB is not set");
        await using var db = NewContext();
        await SeedCardAsync(db, 42, Now);
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO public.sales
              (card_id, source, source_id, sold_on, grade_tier, price_cents, title, captured_at) VALUES
              (42, 'ebay', 'r1', DATE '2026-08-11', 'PSA 10', 1100, 't', now()),
              (42, 'ebay', 'r2', DATE '2026-08-01', 'PSA 10', 1100, 't', now()),
              (42, 'ebay', 'r3', DATE '2026-07-13', 'PSA 10', 1100, 't', now()),
              (42, 'ebay', 'p1', DATE '2026-07-12', 'PSA 10', 1000, 't', now()),
              (42, 'ebay', 'p2', DATE '2026-07-01', 'PSA 10', 1000, 't', now()),
              (42, 'ebay', 'p3', DATE '2026-06-14', 'PSA 10', 1000, 't', now()),
              (42, 'ebay', 'old', DATE '2025-01-01', 'PSA 10', 1, 't', now()),
              (42, 'ebay', 'bgs', DATE '2026-08-10', 'BGS 10 Black', 9999, 't', now());
            """);

        var snapshot = await Reader(db).GetAsync(42);
        var psa10 = snapshot!.Tiers.Single(t => t.Tier == PriceTier.Psa10);

        var change = Assert.IsType<ChangeAvailable>(psa10.Change);
        Assert.Equal(3, change.RecentSales);
        Assert.Equal(3, change.PriorSales);
        Assert.Equal(0.10m, change.Fraction, 4);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail** — ⚠ **NOT DONE AS WRITTEN, 2026-08-12.**

Run: `CARDSTOCK_TEST_DB="Host=192.168.0.56;Database=postgres;Username=cardstock_tester;Password=...;Maximum Pool Size=10" dotnet test tests/CardStock.Integration.Tests -c Release --filter CardPriceReaderTests`
Expected: FAIL — `CardStock.Infrastructure.Prices` does not exist.

> Left unticked deliberately. No database credentials were available at this point, so the red-first
> step was skipped and the implementation was written before the tests had ever run. They passed 6/6
> on their first real execution, which is a *weaker* result than red-then-green: a test that has never
> been seen to fail has not been shown to test anything. Worth re-running with one assertion inverted
> if this layer's behaviour is ever in doubt.

> The password is in `ops/credentials.local`. With the variable unset these skip rather than fail, which is a pass-looking result — read the output and confirm they actually ran.

- [x] **Step 3: Add `LastVisitedAt` to the card mirror**

In `src/CardStock.Infrastructure/Persistence/ScraperReadModels/ScraperCard.cs`, add after `ImageHash`:

```csharp
    /// <summary>
    /// When the crawler last fetched this card's page. Drives the 24h refresh
    /// decision (D-062) and the as-of stamp (D-077).
    ///
    /// DATA_MODEL.md:163 classifies this as mutable scheduler state under Rule 3,
    /// with the durable history in the visits table, which CardStock does not
    /// mirror. That warning is about treating caches as analytical FACTS; for
    /// "when did we last look", this cache is the answer.
    /// </summary>
    public DateTimeOffset? LastVisitedAt { get; init; }
```

No migration: `cards` is mapped `ToView`, so this is one property against a column that already exists.

- [x] **Step 4: Write the reader**

Create `src/CardStock.Infrastructure/Prices/CardPriceReader.cs`:

```csharp
using CardStock.Application.Prices;
using CardStock.Domain.Prices;
using CardStock.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardStock.Infrastructure.Prices;

/// <summary>
/// Two narrow reads, then Domain does the thinking. Nothing about change-only
/// semantics lives in SQL, deliberately: the rules are worth more under test
/// than under a query planner.
/// </summary>
public sealed class CardPriceReader(CardStockDbContext db, TimeProvider time) : ICardPriceReader
{
    public async Task<CardPriceSnapshot?> GetAsync(
        long cardId, CancellationToken cancellationToken = default)
    {
        var card = await db.ScraperCards.AsNoTracking()
            .Where(c => c.Id == cardId)
            .Select(c => new { c.Id, c.LastVisitedAt })
            .SingleOrDefaultAsync(cancellationToken);

        if (card is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(time.GetUtcNow().UtcDateTime);

        // Every row for the card. At ~113 rows on average and ~410 for a fully
        // populated one, loading the lot rides the primary key
        // (card_id, tier, month, observed_at) and is cheaper than being clever.
        // The crawler does the same thing (CardPageWriter.cs:61).
        var prices = await db.ScraperPriceMonths.AsNoTracking()
            .Where(p => p.CardId == cardId)
            .Select(p => new PriceObservation(p.Tier, p.Month, p.PriceCents, p.ObservedAt))
            .ToListAsync(cancellationToken);

        // Two windows' worth, no more. Rides the sales(card_id, sold_on) index,
        // and the source's ~30-rows-per-bucket ceiling keeps it small regardless.
        var since = today.AddDays(-SalesChange.WindowDays * 2);
        var sold = await db.ScraperSales.AsNoTracking()
            .Where(s => s.CardId == cardId && s.SoldOn >= since)
            .Select(s => new { s.GradeTier, s.SoldOn, s.PriceCents })
            .ToListAsync(cancellationToken);

        // Sales at grades with no price series -- 13 of the 19 labels -- have
        // nothing to change against and never reach Domain.
        var sales = sold
            .Select(s => (Tier: GradeTierMap.ToPriceTier(s.GradeTier), s.SoldOn, s.PriceCents))
            .Where(s => s.Tier is not null)
            .Select(s => new SaleObservation(s.Tier!.Value, s.SoldOn, s.PriceCents))
            .ToList();

        return CardPriceSnapshotBuilder.Build(card.Id, card.LastVisitedAt, prices, sales, today);
    }
}
```

- [x] **Step 5: Run tests to verify they pass**

Run: `CARDSTOCK_TEST_DB="..." dotnet test tests/CardStock.Integration.Tests -c Release --filter CardPriceReaderTests`
Expected: PASS, 6 tests. Confirm the output says 6 passed and **not** 6 skipped.

- [x] **Step 6: Run the whole suite**

Run: `dotnet build CardStock.slnx -c Release -m:1` then `CARDSTOCK_TEST_DB="..." dotnet test CardStock.slnx -c Release --no-build -m:1`
Expected: PASS. `SchemaModelTests` and `MigrationContentTests` must still pass — the new property must not have produced a migration or a `public` reference.

- [x] **Step 7: Verify no migration was implied**

Run: `dotnet ef migrations has-pending-model-changes -p src/CardStock.Infrastructure -s src/CardStock.Infrastructure --context CardStockDbContext`
Expected: "No changes have been made to the model since the last migration." `cards` is a view, so adding a property to its mirror must not alter the migration model. **If this reports pending changes, stop** — something mapped the property as a table column.

- [x] **Step 8: Commit**

```bash
git add -A
git commit -m "Read card prices from Postgres

Two narrow queries, then Domain does the thinking. All price rows for
the card, which rides the primary key and averages 113 rows, plus sixty
days of sales on the existing sales(card_id, sold_on) index. Nothing
about change-only semantics lives in SQL, deliberately -- the rules are
worth more under test than under a query planner.

Added LastVisitedAt to the ScraperCard mirror. No migration: cards is
mapped ToView, so it is one property against a column that already
exists, and has-pending-model-changes confirms it.

The integration tests seed the two shapes found in the live database --
Charmeleon's September 2021 hole and Charizard's twice-observed August
-- so the gap and the resolution are proven through real Postgres rather
than in memory."
```

---

## Deliberately not in this plan

Carried from spec §9 so nobody re-adds them as improvements:

- **Batch reads** for Set, Browse and Screener. `price_months` has no index but its primary key and CardStock holds no DDL rights, so any query leading with `tier` scans 10.3M rows.
- **The express-visit call and its rate limit.** This layer returns `LastVisitedAt`; acting on it belongs with the API and the Card page.
- **Any Blazor component**, and any DI registration for `ICardPriceReader` — the API has no endpoint to register it for yet. Wire it up in Phase 2, where a consumer exists to prove it.
- **Validation of any kind** — no outlier detection, no quality scoring, no anomaly flags. The sibling repo owns data integrity; this one owns presentation. The layer says "there is no value here", never "this value looks wrong".
