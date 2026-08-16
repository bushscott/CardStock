# Catalog phase — design

**Date:** 2026-08-15 · **Status:** approved by the owner section-by-section in the brainstorm of
this date · **Roadmap position:** second phase after the Card page, per D-103 · **Ledger:** D-110
(this design and its rulings). Builds on D-102 (the dash/◌ vocabulary), D-104 (species icon),
D-106/D-109 (the Pokédex substrate, landed and receipt-verified), D-087 (deferred UI is present).

**Terminology** (carried from the Pokédex spec): "the scraper" = the sibling process on the Pi.
"The analytics worker" = `CardStock.Worker`, a later phase (D-103 item 6). No bare "worker."

## 1. Shape

Four pages in the WASM app, delivered as vertical slices in this order — each slice lands
reader → endpoint → page → tests end-to-end, and the shared kit is extracted when its second
consumer arrives:

1. **Set** — `/set/{id:long}` (set slugs are pricecharting-derived and URL-hostile — verified
   sample: `pokemon-japanese-sword-&-shield-premium-trainer-box` — so id-keyed like `/card/{id}`)
2. **Character** — `/character/{slug}` (`species.slug` is purpose-built and clean: `umbreon`,
   `nidoran-f`; unique-indexed)
3. **Browse** — `/browse`, with mode in the query string (`?mode=pokemon`)
4. **About-data** — `/about-data`

Chrome arms last: the nav **Browse** tab goes live (active on `/browse`, `/set/*`,
`/character/*` — Set and Character are Browse's leaves), and the disabled search box's tooltip
gets a truthfulness edit (it promises "arrives with the Browse phase"; Browse is shipping without
it, so the copy changes to a later-phase promise — nothing else).

**In scope:** the four pages; the complete Browse filter popover (8 Pokédex attributes); fully
mocked UI for every stat lacking data or process (§2); species-icon serving; the Browse set-mode
ordering control with era shelves; the About-data copy rewrite from its spec's corrected-copy
section; the Character footer reword (named-species rule, per D-106's non-goal note).

**Out of scope:** the global search box and typeahead (owner: "a separate long conversation");
the Card page's species subline (stays deferred); set-metadata curation tooling (the 622 pending
sets remain D-106's backlog — but see §6: the pending shelf makes the backlog visible); any index
or worker math; localized display (`species_names` stays unread); a static-SSR host.

**About-data's tier** is a small addendum to D-063: the page is neither an authenticated app
screen nor `/product` marketing, no static host exists, so it ships as a public route in the WASM
app and may migrate when Marketing builds the static tier.

## 2. The vocabulary — D-102 corpus-wide

Owner ruling, 2026-08-15, extending D-102 from the Card page's census sentences to every Catalog
surface: *"I wanna mock up all aspects of the UI, even things that we don't have the data or
processes for yet, but I don't want to display fake data. We need to use dash where the number
would go, but still write out the label. And I think we use that circle throughout the page to
indicate we don't have data or processes ready yet for this statistic."* The why, same exchange:
*"I want the web page to be clear on things that we don't have enough data for or that we don't
have a process for yet. That way I can keep an eye on them."* The pages are their own gap
dashboard.

The rules, one vocabulary, no text-placeholder slots anywhere:

- **Every stat's UI prints permanently** — label, position, layout, exactly as it will look armed.
- **The dash glyph** (`ChipEngine.GlyphDash`, the existing single definition) fills value runs.
- **The ◌ pending glyph** (brand.md §4 semantics) sits beside the *label* of any statistic with
  no process or no data yet, carrying the gate note as a keyboard-reachable tooltip.
- **Per-card / per-set maturity is a cell state, not a label state:** a maturing value renders
  `—` in its cell with a *computed* gate tooltip (never an authored date, D-061). ◌ means "this
  statistic isn't computable at all yet"; a bare dashed cell means "this row's data is still
  maturing."
- **Controls stay D-087:** a control whose statistic is gated renders disabled with the honest
  tooltip (the RS sort pill), never removed.
- **Words-as-data omit rather than dash** — a dashed direction word would fake a measurement.
- Both glyphs come from their existing single definitions so the vocabulary cannot fork.

The complete gate inventory:

| Element | Page | Treatment | Tooltip gate note | Arms |
|---|---|---|---|---|
| Set-tile 30d Δ | Browse | `— 30d ◌` on every tile | arrives with the analytics worker | worker |
| Species-tile 90d Δ | Browse | `— 90d ◌` on every tile | arrives with the analytics worker | worker |
| Set index sparkline | Set | frame + `set index · 12M` caption + one ◌ (one gate, one glyph); no fake line | arrives with the analytics worker | worker |
| 30D / 90D deltas | Set | labels print, dashes in value runs (covered by the block's ◌) | — | worker |
| RS pct column | Set | ◌ on the header (whole column is one gate), dash cells; RS pill disabled with tooltip | arrives with the analytics worker | worker |
| 90d tile | Character | label + dash + ◌ | arrives with the analytics worker | worker |
| Pop Δ 60d | Set | per-cell dash + computed tooltip; amber exclusion banner on the pop sort with computed count and dates; **all cells pending at ship** (first census observations are late-Jul 2026; earliest pass ~late Sep 2026) | census too young — first observation {date}, deltas begin {date} | per card, with data maturity |
| Year column / `set · year` tile line | Character | per-cell dash + tooltip; the tile line drops its year segment cleanly (no dangling `·`) | release date pending curation | per set, on curation match |

Every dashed cell and value run uses the one `GlyphDash` definition — the prototype's mix of
dash characters does not port.
| Set code + era chips | Set | one **`◌ metadata pending`** chip replaces both when the set is `Pending` (they are one `set_details` fact — one badge, never two) | set metadata pending curation | per set, on curation match |

Not gated (live at ship): all counts and sums, latest PSA 10, ROC 3M, Sales/mo, era shelves for
matched sets, the filters, and every roster.

## 3. Read layer

**Five new `ToView` mappings** in `ScraperViews.cs` (the one-file rule): `species`,
`species_types`, `species_egg_groups`, `card_species`, `set_details`, with read models in
`ScraperReadModels/`. Deliberately **not** mapped: `species_names` (the sibling itself marks it
unread until a later phase) and `card_tagging` (lane bookkeeping; no page reads it). A mapping
with no consumer is drift waiting to happen.

**Readers** — `SetPageReader`, `CharacterPageReader`, `BrowseReader` — follow `CardPriceReader`
conventions: `IDbContextFactory`, `AsNoTracking`, narrow selects. About-data has no reader; its
corrected copy contains only fixed historical anchors (28 Jul 2026, ~Dec 2020, the 2026-09-01
floor) — the banned authored-countdown class of string is already absent from it.

**The query disciplines, stated once, used everywhere:**

1. **Latest PSA 10 per card** = within `(card, tier=Psa10)`: max `month`, then max `observed_at`
   (D-078 — any month can revise, closed ones included), excluding `price_cents = 0` (the I2
   no-sales rule). At roster scale this resolves **in SQL** (`DISTINCT ON` bounded to one set's
   or one species' cards) — a named deviation from "Domain does the thinking": loading 2,531
   cards × ~113 rows to think in C# is not the Card page's one-card situation. Per-card *signal*
   math stays in Domain: **ROC 3M reuses `Indicators.Roc`** and the existing month-gap rules
   (a missing month-cell is a gap, never carried forward), fed by a bounded per-card fetch.
2. **Pop Δ 60d**: PSA-10 census now vs as-of-60-days-ago under change-only semantics (flat
   between rows — the populations contract, which does NOT transfer to `price_months`). Cards
   with first observation younger than 60 days are pending (§2 table). New Domain math →
   hand-authored fixtures; census arithmetic has no Skender analog, so the referee is fixtures
   plus SQL-predicted live values.
3. **Sales / mo**: identical semantics and copy posture to the Card page's shipped volume row —
   an observed trailing count, neutral framing, no rate extrapolation.
4. **The aggregate cache** (Browse only): one in-process computation — latest PSA 10 per card,
   corpus-wide — measured **1,427 ms cold on the Pi** (EXPLAIN ANALYZE, 2026-08-15), far too
   slow per page load. Short TTL (default 5 min, configurable), single-flight so concurrent cold
   loads don't stampede. Derives per-set active-card count and top-value card (highest latest
   PSA 10 among the set's active cards), and per-species printings and total value. Not user/session state, so D-063's "stateless" is intact; interim
   until the analytics worker materializes aggregates (D-039). If cold cost grows, the measured
   escalation is a scraper-side migration adding a tier-first partial index (their repo, their
   migration — precedent: `ix_card_species` was added for CardStock's read direction).
5. **"Active card" defined once:** `delisted_at IS NULL AND not_a_card_at IS NULL` — every
   count, denominator, and roster on all three data pages uses this rule, so header numbers and
   roster counts can never disagree. `set_details` is one-row-per-set by invariant; `Pending`
   means null code/era/release and the UI renders §2's states, never blanks.

## 4. API

Five endpoints under `/api/v1`, Card-endpoints conventions (route groups, ProblemDetails 404
with `reason`, parallel sub-reads where independent):

- `GET /browse/sets` — all 789 tiles: id, name, active count, gradient fan colors, top-value
  card id. Cache-backed.
- `GET /browse/species` — all 1,025 tiles: id, name, slug, gradient pair, printings, total
  value, **plus each species' full filter attributes** (types, generation, region, status,
  stage, color, egg groups, habitat) so the 8-attribute algebra runs client-side with fixed
  vocabularies riding the payload. Tens of KB gzipped.
- `GET /sets/{id:long}` — header + full roster in one payload (client sort needs every row;
  one wait, D-084.6 precedent).
- `GET /characters/{slug}` — identity (name, dex, gradient, chip data incl. the evolves-from
  parent's name), three live tiles, full roster.
- `GET /species/{id:int}/icon` — streams `{dex}.png` from a configured directory
  (`SpeciesIcons:Directory`), exactly the card-image endpoint's shape: file-exists check,
  404 → client gradient fallback, `immutable` cache headers. Icons live on the Pi already
  (1,025/1,025 per D-109).

**DTO rules:** (1) worker-gated statistics have **no wire representation** — the UI renders §2's
states unconditionally; fields appear when the worker has something true to send. (2) Pending
and maturing data ship as **state, not blank**: nullable values plus `metadataStatus` /
first-observation dates, so every tooltip prints computed facts. Mapping in Application
(`CatalogMappers`, per `CardPageMapper`); money/percent formatting reuses Phase 2 conventions.
One new rule: **header stat tiles abbreviate at ≥$10K** (`$96.4K`, `$1.2M`); roster cells always
render full dollars (the prototype showed two formats on one screen with no rule).

## 5. The shared roster kit

`Components/Catalog/`, extracted when Character (second consumer) lands:

- **`DensityToggle`** — terminal/binder pair, parameterized for the prototypes' deliberate
  asymmetry: Set opens terminal-first-and-default, Character binder-first-and-default; button
  order differs and is kept.
- **`RosterTable`** — CSS-grid columns from a per-page column spec (header, width, sortable,
  tooltip); sort arrows, desc-on-key-change; drag-resize grips, 52px floor, not persisted
  (prototype behavior); centered mono cells; rows through `<Virtualize>`; the `{n} of {total}`
  count line. **Unsortable columns get
  no pointer cursor and no hover** — the prototype's dead-but-live-looking headers don't port.
- **`BinderGrid`** — auto-fill tiles, art via `/cards/{id}/image` over the gradient backdrop,
  lazy-loaded; Character's richer tile line is a parameterized slot.
- **One `SortState` for both densities** — grid order always equals table order.
- **Formatting helpers** reuse Phase 2's money/pct/sign conventions (U+2212, zero-is-positive).
- **Accessibility closes the specs' flagged gaps at kit level:** `aria-pressed` on toggles,
  `role="dialog"` + Esc + focus trap on the filter popover, `role="checkbox"`/`aria-checked` on
  option rows, keyboard-operable sort headers, ◌ tooltips keyboard-reachable (D-102).

Tile walls (789 / 1,025) render in full with lazy images — responsive auto-fill grids don't
virtualize cleanly and the tiles are light; virtualization does its work on the two big roster
tables (2,531 and 1,171 worst cases, verified live).

## 6. Browse

**Two modes, `by set` default, mode in the URL** (`?mode=pokemon` — a recorded deviation; the
prototype kept mode ephemeral). Filter state stays ephemeral.

**By set.** All 789 tiles. Tile: three-card fan — front card renders the set's **top-value
card's art** (from the cache) over its gradient, rear cards gradient-only — set name,
`{count} cards`, `— 30d ◌`. **New: an ordering control** — a pill row in the header area beside
the mode switch, Set-toolbar pill styling (supersedes browse.md §7.1's flat-only holding answer,
on the owner's ruling of this date, now that `set_details` exists):

- **`a–z`** (default while curation coverage is thin) — flat alphabetical, stable comparator.
- **`release date`** — dated sets chronologically, then one labeled block: *"{n} sets awaiting
  metadata — alphabetical"*.
- **`era`** — **shelves are data-driven**: the distinct era values present, ordered
  chronologically by each era's earliest release date — never a hard-coded list. Within a
  shelf, sets order by release date. Two labeled tail shelves: **"no era"** (matched
  side-products — McDonald's Collection 12, Trainer kits 10, POP 9, Call of Legends 1,
  Legendary Collection 1 = 33 sets, date-ordered) and **"metadata pending"** (622,
  alphabetical). The pending shelf shrinking is the curation backlog made visible — deliberate.

Era facts, verified live 2026-08-15: **nine eras** — the ninth, `ME` (Mega Evolution, 7 sets),
was added the same day: the sibling promoted `tcgdex-series-eras.json` to a tracked file (fixing
a deploy hazard: the publish recipe's `--delete` rsync would have silently removed the Pi's
copy), added the mapping, and re-swept; verified from this side:
`SELECT era, count(*) FROM set_details GROUP BY era` → BW 12 · DP 16 · EX 17 · **ME 7** · SM 16 ·
SV 18 · SWSH 17 · WOTC 16 · XY 15 = 134 era-bearing of 167 matched; null 655; match_status
unchanged 167/622. Era codes are display text; the Screener's future "Era facet, 8 values" copy
inherits this correction to 9.

**By pokémon.** All 1,025 species (every one has ≥1 tagged card — verified:
`SELECT count(DISTINCT species_id) FROM card_species` → 1,025), caption *"Ordered by total
market value across all printings"* backed by an **explicit `ORDER BY` total value DESC**
(browse.md §6.3 — the prototype's caption was true only by seed coincidence). Tile: 44px
**pixel-icon avatar** over the species gradient (owner ruling this date — same identity
treatment as the Character header; initial-on-gradient is the loading/404 fallback), name,
`{printings} printings`, total value (≥$10K abbreviation), `— 90d ◌`. The per-species `sets`
count stays off the tile (prototype fidelity).

**The filter popover** — all 8 attributes with the full prototype flow: attribute list → option
editor with pre-checked current values, preview expression, one-chip-per-attribute commit,
**AND across attributes / OR within one** — a Grass/Poison species matches a type filter on
either value (the multi-valued rule from the Pokédex design). Chips keep the raw-key terminal
voice (`gen = Gen 1`, `egg ∈ Field, Monster`). Option vocabularies come **from the species
tables, never from loaded rows** — `Mythical` appears though no prototype seed had it. Habitat's
editor carries the one dataset-quirk explainer the persona rules allow: *"Habitat exists for
Gen 1–3 species only"* — Gen 4+ species match no habitat value, honestly. Counter:
`{n} of 1,025 species`; zero matches render the prototype's empty panel copy.

## 7. Set page

**Header.** Breadcrumb `Browse › {name}`. h1 + **code chip** — uppercase verbatim from
`set_details.code` (`swsh7` → `SWSH7`; the prototype's `SWSH07` zero-padding was illustrative
and inventing padding would fabricate a format) — and **an era chip beside it** (owner ruling
this date; the prototype showed no era, superseded). A `Pending` set renders the single
**`◌ metadata pending`** chip in place of both. Matched side-products with no era simply show no
era chip. Sub-line: `{count} cards tracked · first sale observed {MMM yyyy}` — active-card rule;
first-sale keeps its bucket-window-artifact framing; month-year labels stay per D-095. Right:
the sparkline block per §2 — frame, caption, one ◌, dashes in the 30D/90D value runs, and
sign-colored rendering (including the negative branch the prototype lacked) defined for arming
day.

**Toolbar.** Density toggle (terminal default); **five sort pills** — `value`, `ROC 3M`,
`pop Δ`, `sales/mo` live; **`RS` disabled** with the worker tooltip. The fifth pill follows
from two rulings combined: Sales/mo is sortable, and binder density must offer the same sort
options as terminal — pills are the only sort control binder view has, so **the pill row covers
every sortable key** (a one-pill addition over the prototype's four, recorded in §11). Count
line `{shown} of {tracked} cards`.

**Roster (terminal).** Card (link) · PSA 10 · ROC 3M · RS pct (§2) · Pop Δ 60d · Sales/mo.
**Sales/mo is sortable** (owner ruling this date — set.md §7.7 called its unsortability an
apparent oversight, and sorting by observed sales is the honest "most-traded" view). Pop Δ
mechanics per §2, including the all-pending ship state and the all-excluded banner variant the
prototype left unguarded; negative deltas render with a real sign (the hard-coded `+` bug does
not port). Red-at-≥5% keeps its supply-warning semantics with the disambiguating tooltip.

**Binder density:** art tiles (name, PSA 10, ROC 3M). **Footer, rewritten honest:** *"Showing
all {n} tracked cards · prices are latest monthly PSA 10"* — the "most-traded / full roster
ships with the real corpus" fiction dies. **New empty state** for a zero-active-card set;
unknown id → NotFound.

## 8. Character page

**Header.** Breadcrumb `Browse › {name}`. 64px avatar renders the **pixel species icon** over
the species gradient circle (D-104), initial fallback; the page accent bar uses the stored
gradient pair. h1 is `species.name` verbatim, glyphs included (`Nidoran♀` — D-105's
anglicization is about card titles, not species names).

**Dex chips, all from landed columns:** one chip per type (dual-types get two); `Gen {n}` with
the region in the tooltip (*"First appeared in Generation 2 (Johto)"* — the prototype's
game-pair parenthetical would need an authored map we don't have; region is stored); evolution
stage (*"Stage 1 · evolves from Eevee"* via the self-join; stage 0 reads Basic); Pokédex color;
egg group chip(s); **habitat only when it exists** — Gen 4+ species have none by vocabulary, so
the chip is omitted, not pending. Region and status keep no chip (prototype fidelity; Browse's
filters expose them).

**Stat tiles:** Printings · Sets · Total value (≥$10K abbreviation; tooltip carries the D-061
denominator: *"over {n} of {m} printings with a PSA 10 price"*) · 90d per §2.

**Toolbar.** Binder-first toggle, binder default (kept); **new sort pills** `value` / `year` /
`ROC 3M` / `sales/mo` — every sortable key, so the default binder view has the full sort set
(owner ruling this date — the prototype's default view had no sort control at all; the old
real-binder manual-arrangement idea is confirmed absent from the build reference: "one sort,
two surfaces" in both specs); descriptive sentence; `{n} of {n} printings`.

**Roster (terminal).** Card (link) · **Set (link to `/set/{id}` — owner ruling; the spec called
its inertness a notable omission)** · Year (per-cell pending per §2) · PSA 10 · ROC 3M ·
**Sales/mo (sortable, matching Set)**. Binder tiles carry the `set · year` line, year segment
dropped cleanly when pending.

**Footer, reworded for the named-species rule** (D-106's directive): *"a card naming multiple
Pokémon in its title appears under every species it names"* — "picturing" promised art-cameo
coverage the tagger deliberately does not do. No co-star markers on shared printings (prototype
fidelity; the footer states the rule). Empty-roster state ships guarded even though all 1,025
species have cards today (tagging is current-state); unknown slug → NotFound.

## 9. About-data

Same document template as the Legal prototype (820px column, section cards, pill anchors, no
footer, no nav entry). **The content transcribes about-data.md's receipt-backed "Corrected
copy — build this" section**: pricecharting.com named as the sole source and what follows from
that; the coverage table; the per-card ragged boundary; "What we cannot know"; "On pooled
grades" (ADR-0005, multiplier refused); "How fresh it is"; **"The floor"** (2026-09-01 and its
reason — the section D-033 demanded); the honesty policy with the unbuilt chart-marking promise
deliberately absent; the Legal page's wider disclaimer wording adopted in both places. The
"Apr '25 seam" pill and section die with the rewrite.

Three adaptations, the only ones: (1) **the sufficiency section slims to what exists** — the
five states (`OK · LOW DATA · LOCKED · UNDEFINED window · UNSTABLE FIT`), the floor, and the
locked-controls-name-their-unlock rule, all true of the shipped Card page; per-signal unlock
rows return when those signals ship. (2) **No runtime computation** — the corrected copy's dates
are fixed anchors. (3) **The Card page's freshness footer gains the "About our data" link**
(today no live page links here), and the "opening a card page triggers a fresh visit" sentence
is receipt-verified against the shipped refresh behavior at build time before it prints.

## 10. Edge states, testing, receipts

**Edge states, all guarded:** load/error/loading reuse the Card page's shipped patterns;
unknown id/slug → NotFound; icon 404 → gradient fallback; zero-card set; zero-printing species;
zero-match filter panel; the all-pending pop banner; the cold cache start (~1.5–3 s once per
TTL) behind the normal loading state.

**Testing, TDD throughout:**
- **Domain fixtures (hand-authored):** Pop Δ 60d change-only semantics — flat-between-rows
  as-of resolution, the <60-day pending rule, negative deltas; era grouping — data-driven shelf
  set, chronological shelf order, the two tail shelves.
- **Infrastructure against the Pi test DB** (never local): latest-per-key incl. a D-078
  revised-closed-month case, the `price_cents = 0` exclusion, `DISTINCT ON` correctness,
  pending-null `set_details`, junction directions, the aggregate cache's numbers.
- **bUnit:** kit invariants (one sort two surfaces, desc-on-key-change, 52px floor), §2's
  vocabulary (dashes, ◌ placement, tooltips, chip), pending-state copy, filter algebra, habitat
  omission, sign rendering, a11y roles.
- **API:** DTO shapes, 404 reasons, icon traversal defense + cache headers.

**The phase closes on receipts, D-109-style** — predicted from SQL first, then read from the
live pages: 789/1,025 tile counts; era shelves 9 + no-era(33) + pending(622) with ME's 7 named;
value-order top-N vs SQL; one set page and one character page spot-checked cell-by-cell
(Umbreon's link count recomputed at close); icons sampled across the dex; pop column confirmed
all-pending; headless-Chrome render checks (the Claude-in-Chrome tab freezes the WASM app — a
known verification quirk, not a product bug); suites green; deployed to the Pi.

## 11. Deviations from the frozen prototypes — recorded

Each lands in its screen spec's §8 (or amendment banner) as part of this phase's definition of
done. Rulings are the owner's, 2026-08-15, in the Catalog brainstorm:

1. **Character gains sort pills** (`value`/`year`/`ROC 3M`/`sales/mo`) — the default view had
   no sort control (character.md §4.3's trap, §7.10's question).
2. **Character's Set cell links to `/set/{id}`** (was inert text, §7.6).
3. **Sales/mo is sortable on both rosters, with a pill on both** — Set's pill row grows to
   five so binder density keeps the full sort set (set.md §7.7 + the binder-parity ruling).
4. **Browse set-mode gains the ordering control** with era shelves as an option and two honest
   tail shelves — resurrects the deleted-shelves concept as opt-in, now that the data exists.
5. **Browse mode goes in the URL.**
6. **Browse species tiles render pixel icons** (D-104's open question, settled).
7. **Full rosters, no "most-traded" cap** — Set footer rewritten.
8. **No dead sort affordances** — unsortable headers lose the pointer/hover.
9. **Sign bugs don't port:** pop Δ's hard-coded `+`; the Set/Character delta tiles' baked
   `--pos`; sign-colored rendering defined for arming day.
10. **The dangling `set ·` separator bug doesn't port** (year pending drops the segment).
11. **The D-102 vocabulary is added corpus-wide** (§2) — the prototypes had no pending states at
    all on Browse/Character and only pop's on Set.
12. **The Set header gains an era chip**; code renders uppercase-verbatim, unpadded.
13. **The search tooltip copy edit** (chrome, honesty only).
14. **Kit-level a11y additions** (§5).
15. **Character footer reword** (named-species rule — a correction required by D-106, listed for
    completeness).

## 12. Non-goals, explicit

No search box or typeahead. No Card-page species subline. No curation tooling. No index or
change-percentage methods of any kind — every such number waits for the analytics worker. No
localized names. No static-SSR host. No chart restatement-marking (the About-data copy stops
promising it). No new scraper-side work beyond the already-landed ME era row (§6).

## 13. Delivery

1. This spec + ledger entry D-110 + amendment banners on browse.md / set.md / character.md /
   about-data.md — committed together (this repo, this session).
2. Implementation plan via the writing-plans skill, after the owner reviews this spec.
3. Slices in §1's order; kit extraction at Character; chrome arming last; each slice updates its
   screen spec as it lands (the maintenance rule).
4. Phase closes in the ledger on §10's receipts, verified live from this repo's side.
