# Phase 1 — the price read layer

**Status:** design approved 2026-08-12. Implements D-075 Phase 1.
**Ledger:** D-075 (scope), D-076 (express contract), D-077 (Card page treatment), D-002, D-003, D-033, D-057.

## 1. What this is

One layer that answers *"what are this card's prices, and what changed?"* — and never lets a caller
mistake an absence for a value.

It is the prerequisite for everything D-075 puts after it. The Card page renders from it, and the
Binder's `cur` field — which drives portfolio value, unrealized P&L and the vs-index comparison
(`binder.md:196`, D-057 level 1) — is one call into it.

**Scope is one card at a time.** Batch reads for Set, Browse and Screener are deliberately excluded;
they are a different problem with a different constraint (see §9).

## 2. What already exists

Phase 0 shipped (D-071). `CardStockDbContext` maps five crawler tables as EF **views**, so this layer
can read them and cannot write them — `ScraperViews.cs:18` explains why `ToView` rather than
`ExcludeFromMigrations`. `ScraperPriceMonth`, `ScraperSale`, `ScraperCard` and `PriceTier` all exist.

**This phase adds no tables and no migrations.** It reads `public.price_months`, `public.sales` and
`public.cards`, all already mirrored. The only schema-adjacent change is one property added to an
existing read-only mirror (§5.1).

## 3. The data facts this design is built on

Every one was verified against the live database or the crawler's source. They are not assumptions.

| Fact | Consequence | Receipt |
|---|---|---|
| The corpus is fully crawled — 0 of 91,518 live cards unvisited | Sparse series are real, not an artifact of an unfinished crawl | Query, 2026-08-11 |
| Only the current month revises; **17,804 revision rows of 10,357,098 (0.17%)** | `max(observed_at)` is a correctness rule, not a performance concern | Query; `DATA_MODEL.md:110` |
| A missing month means **the source published no point** | Never carry a value across a gap. Gaps are gaps | `CardDetailParser.cs:318–331`; `ChangeOnlyPlanner.cs:22` |
| `price_cents = 0` **never occurs** (0 rows in 10.3M) | Zero is not a sentinel we must disambiguate | Query, 2026-08-11 |
| **95% of in-window months are filled; 33% of series contain ≥1 gap** | Gap handling is the common path, not a corner | 300-card sample |
| Only **19% of cards have all six tiers**; the mode is four; **11% have none** | "No series" is a first-class state, not an error | 300-card sample; query |
| **81% of series are current-month, 15% one month behind, 3.5% two or more** | The staleness threshold is *this month or last*, measured not guessed | 500-card sample |
| `GradeTierVocabulary` **grows** over time | Grade mapping must be an allow-list | `GradeTierVocabulary.cs:16–18` |
| The prototype spells the sixth tier `Raw`; the data spells it `Ungraded` | Translate at the render boundary only | `Card.dc.html:322`; `PriceTier.cs:14` |

## 4. The contract

Callers get one shape per card. Absence is expressed in the **type**, not in a magic value, so
carrying a price across a hole is code that does not compile rather than a rule someone must remember.

```csharp
// CardStock.Domain
public sealed record CardPriceSnapshot(
    long CardId,
    DateTimeOffset? LastVisitedAt,        // drives the 24h refresh decision and the as-of stamp
    IReadOnlyList<TierSnapshot> Tiers);   // ALWAYS exactly 6, in strip order, never a short list

public sealed record TierSnapshot(
    PriceTier  Tier,
    TierSeries Series,   // the full resolved history — the chart draws from this (§4.3)
    TierPrice  Price,    // the strip's top line, derived from Series
    TierChange Change);  // the strip's bottom line, from sales
```

`Price` is derivable from `Series` and is carried anyway, so no caller re-implements "newest point,
unless it is too old." One rule, one place. `Series` is what the chart needs and `Price` is what the
strip and the Binder need, and they must never disagree.

### 4.1 The price — one number per tier

```csharp
public abstract record TierPrice;

/// The newest published month for this tier. IsCurrentMonth drives the ◌ marker.
public sealed record PriceAvailable(int PriceCents, DateOnly Month, bool IsCurrentMonth) : TierPrice;

/// Newest month is 2+ months behind. Renders a dash — a dead grade, not a current price.
public sealed record PriceStale(DateOnly NewestMonth) : TierPrice;

/// This tier has never had a published price for this card. Renders a dash.
public sealed record NoPriceSeries : TierPrice;
```

`PriceAvailable` is the only case carrying `PriceCents`. The other two cannot be rendered as a number
by accident.

**Threshold:** `PriceStaleness.MaxMonthsBehind = 1` — current month or the one just closed. Measured,
not chosen: current-month-only would dash 19% of series, most of them healthy, because early in a
month the source has not yet posted every tier (§3).

### 4.2 The change — the 30-day movement

```csharp
public abstract record TierChange;

public sealed record ChangeAvailable(decimal Percent, int RecentSales, int PriorSales) : TierChange;

/// Too few sales in one or both windows. Renders a dash. Permanent state, not a phase.
public sealed record ChangeInsufficient(int RecentSales, int PriorSales) : TierChange;
```

Mean sale price over the last 30 days against the mean over the 30 before that (D-075). Both windows
are hardcoded and never widen — today they return a handful of rows, in a year a full window, and the
code is identical.

**Threshold:** `SalesChange.MinimumSalesPerWindow` — **one named constant**, starting at `3`, applied
to both windows. Tuning it is a value change, not a rewrite.

**No countdown, no unlock date, no `LOCKED` state.** Owner, 2026-08-11: *"I don't want logic sticking
around forever that calculates when the data will be full."* And the reasoning outlives the ramp-up —
a quiet card will not have three sales in 30 days in 2028 either, so `ChangeInsufficient` is permanent
regardless. This deliberately overrides `card.md` §4.11's `LOCKED` treatment for this cell.

### 4.3 The series — for the chart

```csharp
public sealed record TierSeries(PriceTier Tier, IReadOnlyList<MonthlyPrice> Points);
public sealed record MonthlyPrice(DateOnly Month, int PriceCents, DateTimeOffset ObservedAt);
```

`Points` is ascending by month, already resolved to one row per month, and holds **only months that
have data** — no padding, no filling. A caller asking for a window gets one slot per month:

```csharp
public abstract record PriceSlot(DateOnly Month);
public sealed record ObservedPrice(DateOnly Month, int PriceCents, DateTimeOffset ObservedAt) : PriceSlot(Month);
public sealed record MissingMonth(DateOnly Month)  : PriceSlot(Month);  // inside the series; source published nothing
public sealed record OutsideSeries(DateOnly Month) : PriceSlot(Month);  // before the first month or after the last
```

The distinction between the last two is not pedantry. `MissingMonth` is a **hole** — real data either
side, and the line must break. `OutsideSeries` is the series not having started, and the axis simply
has nothing there. Drawing them the same way would claim a card's history begins later than it does.

## 5. Where the code lives

```
CardStock.Domain          PriceTier, the records above, and every rule:
                          latest-per-key resolution, windowing, staleness,
                          the grade mapping, the change calculation.
                          Pure. No EF, no SQL, no clock of its own.

CardStock.Application     ICardPriceReader — one method, one card.

CardStock.Infrastructure  CardPriceReader — three narrow queries, then hands
                          the rows to Domain.
```

**Move `PriceTier` from `Infrastructure/Persistence/ScraperReadModels/` to `CardStock.Domain`.** The
pure rules need it and Domain references nothing, so it cannot stay where it is. `ScraperPriceMonth`
keeps using it — Infrastructure already references Domain. Its warning comment travels with it: the
enum ordinal *is* the stored value, so reordering silently misreads every historical price.

### 5.1 The one property added

`ScraperCard` gains `LastVisitedAt`. It is a read-only view mirror, so **no migration, no DDL** — one
C# property against a column that already exists.

`DATA_MODEL.md:163` classifies it as mutable scheduler state under Rule 3, with the durable history in
`visits`, which CardStock does not mirror. That warning is about treating caches as *analytical facts*.
For "when did we last look," this cache is the answer, and mirroring `visits` to re-derive it would be
a sixth mirror to serve one timestamp.

## 6. How it reads

**Fetch narrow, compute pure.** Both queries pull a bounded set and hand it to Domain. Nothing about
change-only semantics lives in SQL, so every rule is testable without a database.

**Prices** — all rows for the card. Uses the primary key `(card_id, tier, month, observed_at)` directly.

```sql
SELECT tier, month, price_cents, observed_at FROM price_months WHERE card_id = @id
```

At 113 rows per card on average and ~410 for a fully-populated one, loading the lot is cheaper than
being clever, and it is what the crawler itself does (`CardPageWriter.cs:61`). Domain then groups by
`(tier, month)` and keeps `max(observed_at)` — which fires on 0.17% of rows but must never be skipped.

**Sales** — 60 days for the card. Uses the existing `sales(card_id, sold_on)` index (D-057).

```sql
SELECT grade_tier, sold_on, price_cents FROM sales
WHERE card_id = @id AND sold_on >= @today - 60
```

Bounded by the source's ~30-rows-per-bucket window, so small in every real case. Domain maps
`grade_tier` to a `PriceTier`, splits into the two windows and computes.

**Card** — `last_visited_at`, `delisted_at`, `not_a_card_at`, joined or fetched alongside.

**The clock is injected.** `TimeProvider`, as the crawler does (`ExpressVisitRunner.cs:38`), so "the
current month" is a test input rather than ambient state. Months are UTC — `price_months.month` is
already a UTC-derived month start (`CardDetailParser.cs:338–341`).

### 6.1 The grade mapping

`GradeTierMap.ToPriceTier(string) → PriceTier?`, in Domain. An **allow-list** of six:

| `sales.grade_tier` | `PriceTier` |
|---|---|
| `Ungraded` | `Ungraded` (renders as `Raw`) |
| `Grade 7` · `Grade 8` · `Grade 9` · `Grade 9.5` | `Grade7` · `Grade8` · `Grade9` · `Grade9Half` |
| `PSA 10` | `Psa10` |
| everything else — `Grade 1`–`6`, the seven non-PSA tens, **and anything unrecognised** | `null` |

Allow-list, not deny-list, and not as a style preference: `GradeTierVocabulary.cs:16–18` states the
vocabulary grows. A deny-list would silently fold a future grader's 10 into the PSA 10 cell — the
substitution D-022 and D-057 both rejected — with no error, in the cell users read first.

Comparison is case-insensitive on the whitespace-squeezed form, mirroring
`GradeTierVocabulary.Normalize`.

## 7. What this layer refuses to do

The sibling repo owns data integrity; this one owns presentation (owner, 2026-08-11). So the layer
**never** says *"this value looks wrong"* — only *"there is no value here."*

Explicitly excluded: outlier detection, anomaly flagging, quality scoring, backfill requests,
interpolation, carry-forward, and smoothing of any kind. A $299.99 that drops to $40.00 across a gap
renders exactly as stored, with the gap intact.

## 8. Testing

**Domain — no database, and this is where the real coverage lives.** Every rule in §4 and §6.1 is a
pure function over hand-built rows:

- Two rows for one `(tier, month)` → the later `observed_at` wins. Also with the rows supplied in
  reverse order, because "the last one in the list" is the bug this rule exists to prevent.
- A month inside the series with no row → `MissingMonth`, never a carried value.
- A month before the first or after the last → `OutsideSeries`, distinct from `MissingMonth`.
- Newest month = current → `PriceAvailable`, `IsCurrentMonth: true`.
- Newest month one behind → `PriceAvailable`, `IsCurrentMonth: false`. **The price renders; the `◌`
  does not.** This is the 15% case from §3 and the one most likely to be got wrong, because the two
  decisions look like one.
- Newest month two behind → `PriceStale`. No rows at all → `NoPriceSeries`.
- A card whose newest month rolls over midnight on the 1st: same series, one month later, flips from
  `IsCurrentMonth: true` to `false` and then to `PriceStale`. Driven entirely by the injected clock.
- Six tiers always returned, including for a card with zero price rows.
- Grade mapping: all 19 known labels, plus an invented `"PGX 10"` → `null`, plus casing and whitespace
  variants.
- Change: both windows healthy → a percentage; either window below the constant →
  `ChangeInsufficient`; a sale exactly on each window boundary → lands in the window the SQL puts it in.

**Integration — real Postgres, per D-073, on the Pi.** Fewer tests, each proving the query feeds Domain
what it expects:

- A seeded card with a deliberate month gap comes back with the gap intact.
- A seeded `(card, tier, month)` with two `observed_at` values resolves to the later one *through the
  real query*, not just in memory.
- A card with no `price_months` rows returns six `NoPriceSeries`, not an empty list or a null.
- Sales exactly 30 and 60 days old land in the intended windows against real Postgres date arithmetic.

**One test worth naming.** The Charmeleon case from §3 is a real fixture: a series with a hole at
2021-09, `$299.99` before and `$40.00` after. It asserts the gap survives and that no interpolation
smooths the cliff. It is the whole layer's purpose in one row of data.

## 9. Deliberately not in this phase

- **Batch reads** for Set, Browse and Screener. `price_months` has no index but its primary key
  (`20260728032826_InitialCreate.cs:141`) and CardStock holds no DDL rights anywhere (D-071), so any
  query leading with `tier` scans 10.3M rows. Per-card reads ride the key perfectly; the batch case
  needs its own design and probably the analytics tier of D-015.
- **The express-visit call and its rate limit.** The read layer returns `LastVisitedAt`; deciding what
  to do about it belongs with the API and the Card page (D-062, D-037).
- **Any Blazor component.** Phase 2.
- **Populations and the sales ledger.** 2027 (D-001).

## 10. Open, not blocking

- **How far apart are `price_months` and our own sales?** Owner's question, 2026-08-11. One strip cell
  shows a price from PriceCharting's undisclosed average beside a change from our captured sales. Bears
  on `about-data.md:238`, where "built from realized sales" is recorded as an assumption. Worth
  answering before any copy claims the two agree.
- **`SalesChange.MinimumSalesPerWindow` starts at 3** and nobody has ruled on it. It cannot be tuned
  from evidence until ~Nov 2026, when the first real windows exist.
- **What an empty tier cell says.** Settled as a dash with a tooltip; the exact wording is a `card.md`
  question, not a read-layer one.
