# Screen: Character

> **Authority.** Everything below is read from `CardStock Mockup/Cardstock Character.dc.html`
> (215 lines, read directly 2026-08-10). Bare `:N` citations refer to that file; `Set:N`
> refers to `Cardstock Set.dc.html`. Data claims cite `../PokemonInvestBatch/DATA_MODEL.md`.
> Where a markdown doc disagrees with the HTML, the HTML wins and the disagreement is
> recorded in §8 — never averaged.

> **Amended 2026-08-15 (Catalog phase design, D-110 — build from
> `docs/superpowers/specs/2026-08-15-catalog-phase-design.md`, which supersedes this spec where
> they differ).** Owner rulings of that date, answering §7: **(1)** route is
> `/character/{slug}` on the landed `species.slug` (§7.1). **(2)** The 90d tile renders per
> the **D-102 vocabulary** — label + dash + ◌ — until the analytics worker; no interim species
> index (§7.2, §7.13); no chart is added (§7.3). **(3)** Year and the tile's `set · year` line
> get per-cell pending states with computed tooltips; the dangling `·` bug does not port
> (§7.4, §4.4). **(4)** Chips: one per type, gen tooltip uses the stored region, stage via the
> self-join, color, egg group(s), **habitat only when non-null** (Gen 4+ have none — omitted,
> not pending); region/status keep no chip (§7.5); chips stay inert (§7.7). **(5)** §7.6
> answered: **the Set cell links to `/set/{id}`**. **(6)** §7.8 answered: no co-star markers;
> the footer states the rule — reworded to the named-species form ("a card *naming* multiple
> Pokémon in its title appears under every species it names", per D-106). **(7)** §7.9
> answered: **full roster, virtualized** — no cap. **(8)** §7.10 answered: **sort pills
> arrive** (`value` / `year` / `ROC 3M` / `sales/mo` — every sortable key, so binder view has
> the full sort set), resolving §4.3's default-view trap; the old manual-arrangement binder
> idea is confirmed dropped; **Sales/mo is sortable**. **(9)** §7.11:
> avatar renders the pixel species icon over the stored gradient pair (D-104); accent bar uses
> the same pair. **(10)** §7.12 answered: header tiles abbreviate at ≥$10K; roster cells stay
> full dollars. Total value's tooltip carries the D-061 denominator ("over {n} of {m}
> printings with a PSA 10 price").

> **Amended 2026-08-18 (build).** Sorting on nullable keys (`value`, `year`, `ROC 3M`) places
> rows without a value **last in both directions** — a dashed cell never implies a rank.

---

## 1. Identity

| | |
|---|---|
| **Screen label** | `Character` (`data-screen-label="Character"`, `:35`) |
| **Prototype** | `CardStock Mockup/Cardstock Character.dc.html` |
| **Route** | `/character/{name}` — from `HANDOFF.md:78`. Keyed by **species name**, where Set is keyed by id (`/set/{id}`). The HTML is a static file and asserts no route. |
| **Nav section** | **Browse** is the active tab (`:47`). Character is a leaf of Browse. |
| **Breadcrumb** | `Browse › Umbreon` (`:58`) |
| **Component props** | None. `data-props=""` (`:132`) |

**Purpose.** The **species aggregate**: one Pokémon, every printing of it Cardstock tracks,
across all sets and all eras, with four species-level roll-up stats above the roster. It is
the cross-set counterpart to the Set page's within-set roster, and it exists only because
card → species is a curated many-to-many relation (`DESIGN_NOTES.md:69`).

---

## 2. Layout

Single column. `main` is `max-width: 1480px`, `padding: 14px 20px 28px`, flex column,
`gap: 14px` (`:56`) — identical shell to Set.

1. **Nav bar** — 48px sticky, shared chrome (`:37`–`:52`).
2. **Accent bar** — 4px, `linear-gradient(90deg, #2B2D42, #5C6B9E)` (`:54`). **Two stops**;
   Set's has three (`Set:54`). The two stops match the species avatar gradient (`:61`) and
   the Umbreon seed rows' `acc` pair (`:145`), so the bar reads as species identity colour.
3. **Breadcrumb** (`:58`).
4. **Header card** — `--card`, 1px `--line`, radius 10, padding 16, `display:flex;
   align-items:center; gap:20px` (`:60`). Four zones:
   - 64px circular avatar with the species initial (`:61`);
   - identity block: h1 species name + a wrapping row of Pokédex chips (`:62`–`:69`);
   - flex spacer (`:70`);
   - a right-aligned strip of **four stat tiles**, `gap: 22px` (`:71`–`:76`).
5. **Toolbar** — one flex row (`:79`–`:87`): density segmented control, a static
   descriptive sentence, spacer, right-aligned count. **No sort pills.**
6. **Roster** — exactly one of two mutually exclusive blocks:
   - *Binder* (`:89`–`:105`), listed **first** in source order: `repeat(auto-fill,
     minmax(180px, 1fr))`, gap 12 (`:90`);
   - *Terminal* (`:107`–`:125`): grid header row (`:109`) + N grid data rows (`:115`).
7. **Footer note** — 12.5px muted (`:127`).

**There is no chart on this screen** — no `<svg>`, no sparkline, no polyline, no index
block. The 220px sparkline that occupies the Set header's right side (`Set:70`–`:75`) has
no counterpart here; that space is taken by the four stat tiles. See §8.

---

## 3. Data contract

Legend for **Backing**: **✔** = queryable from the scraper's eight tables today ·
**⚠** = derivable but needs a defined method or a maturity wait · **✘** = no backing data
exists (needs a non-scraped table, `DECISIONS.md:199`, or the external Pokédex schema,
`DESIGN_NOTES.md:70`).

### 3.1 Species header

> **Amended 2026-08-14 (D-104, D-106, Pokédex-phase design).** The avatar row below is
> superseded: the Character page renders an **icon-sized species image** (retro pixel menu
> sprite, `species-icons/{dex}.png`), not an initial-on-gradient — owner ruling D-104, style and
> source ruled in D-106. The ✘ backings in this section also change tier: `species`,
> `card_species`, `species_names`, and the chip attributes become **scraper-owned tables**
> populated in the Pokédex phase (D-106 reversed D-069.10), which CardStock reads. The `Year`
> column's backing (`set_details`) arrives in the same phase with `pending` states for
> non-English sets. Full contract: `docs/superpowers/specs/2026-08-14-pokedex-phase-design.md`.
> This page's build pass (Catalog phase) refreshes the section in full.

> **Amended 2026-08-18 (owner UAT, D-114).** The bare `loading-strip` is replaced by the shared
> `LoadingRing` — one 48px ring at `inset: 20vh` that the boot indicator fills (real download
> progress) and this page's data fetch then spins in place. Contract: shared-components.md §4.8.

| Field | Rendered as | HTML | Backing |
|---|---|---|---|
| avatar initial | 64px circle, `linear-gradient(160deg, #2B2D42, #5C6B9E)`, Inter Tight 700 26px, `rgba(255,255,255,0.92)` | `:61` (literal `U`) | ✘ initial is derivable from the name; the **gradient pair is per-species identity colour** with no source (see §7) |
| `species.name` | h1, Inter Tight 700 26px, `-0.01em` | `:63` (literal `Umbreon`) | ✘ external Pokédex schema |
| `dexChips[]` | wrapping row of mono chips, `gap: 6px` | `:64`–`:67`, data `:174`–`:181` | ✘ external Pokédex schema — see 3.2 |
| **Printings** | uppercase 11px label + mono 19px 700 value | `:72` (literal `34`) | ✘ **character tag table** — `count(distinct card_id)` joined to this species |
| **Sets** | same tile pattern | `:73` (literal `19`) | ✘ character tag table → `count(distinct cards.set_id)` |
| **Total value** | same, abbreviated `$96.4K` | `:74` (literal `$96.4K`) | ⚠ + ✘ — `sum(latest PSA 10)` over the tagged cards; prices are ✔ from `price_months`, membership is ✘ |
| **90d** | same, **`--pos` unconditionally** | `:75` (literal `+6.8%`) | ⚠ + ✘ — a species-level index change; method undefined (see §7) |

All eight values are **template literals, not bindings** — the entire header except the
chip loop is hard-coded. Only `dexChips` is interpolated. An implementation must
parameterise all of it.

Note the value formatting is **inconsistent with the roster below it**: the tile shows
`$96.4K` (abbreviated, one decimal) while every row and tile price uses `money()` =
whole dollars with separators (`:168`). Two money formats on one screen.

### 3.2 `dexChips[]` — Pokédex attribute chips (`:174`–`:181`)

Six items, each `{ label, tip }`, rendered as `title`-bearing mono chips with
`cursor: help` (`:66`):

| # | Label | Tooltip | In the ruled-in Pokédex schema? |
|---|---|---|---|
| 1 | `Dark` | `Pokédex type` | ✔ type(s) |
| 2 | `Gen 2` | `First appeared in Generation 2 (Gold/Silver)` | ✔ generation |
| 3 | `Stage 1` | `Evolution stage — evolves from Eevee` | ~ implied by "evolution line" |
| 4 | `Black` | `Official Pokédex color` | ✘ not enumerated |
| 5 | `Field egg group` | `Pokédex egg group` | ✘ not enumerated |
| 6 | `Urban habitat` | `Pokédex habitat` | ✘ not enumerated |

`DESIGN_NOTES.md:70` enumerates the external schema as *"name, generation, region,
type(s), evolution line, status."* Chips 4–6 are outside that list, and **region** and
**status** — two fields that *are* ruled in, and are Browse facets
(`DESIGN_NOTES.md:71`) — get no chip. See §8.

Chip count is **6**, but the loop's `hint-placeholder-count` is `5` (`:65`) — a skeleton
hint, cosmetic, but it means the streaming skeleton is one chip short.

Chips are **not filters and not links** — inert, tooltip-only.

### 3.3 Toolbar

| Field | Type / values | HTML |
|---|---|---|
| `vbBg` / `vbFg` | binder button — active: `PAL.acc` bg / `PAL.card` fg; inactive: `PAL.card` / `PAL.mut` | `:184` |
| `vtBg` / `vtFg` | terminal button, same rule | `:185` |
| descriptive sentence | static: `every Umbreon printing we track, all eras` — **hard-codes the species name** | `:84` |
| `shownCount` | `` `${sorted.length} of 34 printings` `` — mono 12.5px, denominator hard-coded and equal to the Printings tile | `:186`, rendered `:86` |

**Button order is reversed relative to Set.** Here `binder` is left and `terminal` is right
(`:81`, `:82`); on Set it is `terminal` then `binder` (`Set:85`, `Set:86`). The segmented
control's spatial order differs between two sibling screens.

There are **no sort pills** on this screen. Set has four (`Set:89`–`:93`); Character has
none. Sorting exists only on table headers (3.4).

### 3.4 Column model (terminal)

`gridCols` = `minmax({colW.name}px, 1.4fr)` then `set year price roc vol` as fixed pixel
tracks (`:187`).

| # | Header | `k` | Sort key | Default width | Tooltip | Backing |
|---|---|---|---|---|---|---|
| 1 | `Card` | `name` | **none** | 230 | `Printing name` | ✔ `cards.name` |
| 2 | `Set` | `set` | **none** | 130 | `Set this printing belongs to` | ✔ `sets.name` via `cards.set_id` |
| 3 | `Year` | `year` | `year` | 70 | `Release year — click to sort` | ✘ **set metadata table** (release date for ~303 sets) |
| 4 | `PSA 10` | `price` | `value` | 100 | `Latest monthly PSA 10 price — click to sort` | ✔ latest `price_months` for tier `Psa10` by `max(observed_at)` — **not** newest month (`CLAUDE.md:53`) |
| 5 | `ROC 3M` | `roc` | `roc` | 92 | `3-month rate of change — click to sort` | ✔ from `price_months` |
| 6 | `Sales / mo` | `vol` | **none** | 90 | `Observed sales per month, all tiers` | ⚠ derivable from `sales` **forward of each card's seam**; pre-seam volume permanently unavailable (`DATA_MODEL.md:391`–`392`) |

Defaults from `state.colW` (`:155`). Three of six columns are sortable; `Card`, `Set` and
`Sales / mo` carry a no-op `() => {}` handler (`:198`) while still rendering
`cursor: pointer` and a hover recolour (`:111`) — three dead controls.

**Column-set delta vs Set.** Character swaps Set's `RS pct` and `Pop Δ 60d` for `Set` and
`Year`. Consequence: **the Character page has no census column, therefore no census
pending state and no sufficiency banner** (§4.4).

### 3.5 Row model (terminal) — `rows[]`, `:200`–`:203`

| Field | Formatting | Line |
|---|---|---|
| `r.name` | 14px / 500, centred, ellipsised, wrapped in `<a>` to the Card page | `:116`, `:201` |
| `r.set` | mono 12.5px, `--mut`, centred — **plain text, not a link back to the Set page** | `:117`, `:201` |
| `r.year` | `String(year)`, mono 12.5px, `--mut` | `:118`, `:201` |
| `r.price` | `money()` = `'$' + Math.round(n).toLocaleString('en-US')` — whole dollars | `:168`, `:201` |
| `r.roc` | `pct()` = `+` or **U+2212 MINUS SIGN** + `abs.toFixed(1)` + `%` | `:169`, `:202` |
| `r.rocFg` | `PAL.pos` when `roc >= 0`, else `PAL.neg2` | `:202` |
| `r.vol` | `String(vol)`, `--mut` | `:202` |

Row chrome: `padding: 6px 16px`, 1px `--line4` bottom border, all numerics JetBrains Mono
and **centre**-aligned (`:115`).

**No cell on this screen carries a data tooltip.** Set's `Pop Δ` cell does (`Set:115`);
here every `title` belongs to a control or a chip.

### 3.6 Tile model (binder) — `tiles[]`, `:204`–`:209`

| Field | Formatting | Line |
|---|---|---|
| `tl.thumbBg` | `linear-gradient(160deg, acc[0], acc[1])` — per-printing accent pair from the seed (`:145`–`:152`) | `:207` |
| `tl.slotId` | `'art-' + name.toLowerCase().replace(/[^a-z0-9]+/g,'-')` | `:208` |
| `tl.name` | 13.5px / 600, single line, ellipsised | `:96` |
| **`tl.set` · `tl.year`** | one mono 11.5px `--mut2` line, joined by ` · ` | **`:97`** |
| `tl.price` | `money()` | `:99`, `:205` |
| `tl.roc` / `tl.rocFg` | `pct()` and the same `>= 0` threshold | `:100`, `:206` |

Tile chrome: the entire tile is an `<a>` to `Cardstock Card.dc.html` (`:92`); art box is
`aspect-ratio: 325/450`, radius 5, painted with `thumbBg`, holding
`<image-slot placeholder=" ">` (`:93`–`:95`) so an absent image reads as the bare gradient
(`:22` zeroes `::part(empty)` opacity). Hover raises
`box-shadow: 0 6px 20px rgba(20,19,26,0.10)`.

**Character's binder tile is strictly richer than Set's**: it adds the `set · year` line
(`:97`) that `Set:129`–`:133` has no equivalent for. Density switching here loses only
`Sales / mo`, where on Set it loses three metrics.

### 3.7 Static copy

- Toolbar sentence: `every Umbreon printing we track, all eras` (`:84`).
- Density tooltips: `Binder density — fewer rows with card art` /
  `Terminal density — more rows, tighter type, every metric column` (`:81`, `:82`).
- Resize grip tooltip: `Drag to resize` (`:111`).
- Footer: `Prices are latest monthly PSA 10 · a card picturing multiple Pokémon appears
  under every species it features` (`:127`) — the counting rule, stated in the UI.

### 3.8 Seed shape (illustrative, not contract)

`this.PRINTS` (`:144`–`:153`) holds **8** rows of `{ name, set, year, price, roc, vol,
acc[2] }` spanning **6 distinct sets** (Evolving Skies ×3, Hidden Fates, POP Series 5,
Neo Discovery, Unseen Forces, Undaunted) and years 2001–2021. One row is negative
(`Umbreon ex (Unseen Forces)`, `roc: -0.8`, `:150`), exercising the `--neg2` branch. The
most expensive printing (`$5,150` Gold Star, `:148`) is not the most recent — the seed is
built so that value-sort and year-sort produce visibly different orders.
`hint-placeholder-count` is 8 on both roster loops (`:91`, `:114`) and 6 on `cols` (`:110`).

Seeded totals do **not** reconcile with the header tiles (8 rows / 6 sets / $9,687 vs
34 / 19 / $96.4K) — the seed is a subset, exactly as on Set. Unlike Set, **no footer copy
explains the shortfall** (§8).

---

## 4. States

### 4.1 State variable inventory (`state`, `:155`)

| Key | Domain | Default | Persisted? |
|---|---|---|---|
| `view` | `'binder'` \| `'terminal'` | **`'binder'`** | No |
| `sort` | `'value'` \| `'year'` \| `'roc'` | `'value'` | No |
| `sortDir` | `'desc'` \| `'asc'` | `'desc'` | No |
| `colW` | 6 integers ≥ 52 | `{name:230, set:130, year:70, price:100, roc:92, vol:90}` | No |

**The default density is the opposite of Set's.** Character opens in `binder` (`:155`,
confirmed by `hint-placeholder-val="{{ true }}"` on the binder `sc-if`, `:89`); Set opens
in `terminal` (`Set:174`). This is deliberate — a species page is a visual "look at all the
Umbreons" surface; a set page is a market table.

Full state space: `2 views × 3 sort keys × 2 directions` = 12 combinations, × continuous
`colW`. Nothing is persisted; `localStorage` is read only for `cardstock-theme` /
`cardstock-cvd` (`:33`, `:134`).

### 4.2 Density states

| State | Trigger | Effect |
|---|---|---|
| **Binder** (default) | initial load; click `binder` (`:81`) | `isBind` true (`:182`) → tile grid renders (`:89`); table unmounted. Metrics: name, set, year, price, roc. |
| **Terminal** | click `terminal` (`:82`) | `isTerm` true → table renders (`:107`); grid unmounted. Adds `Sales / mo` and all sort/resize affordances. |

Mutually exclusive and jointly exhaustive — both flags derive from one enum (`:182`).

### 4.3 Sort states — and the default-view trap

| State | Trigger | Effect |
|---|---|---|
| **Active column arrow** | `sort === col.s` | ` ▾` (U+25BE) desc / ` ▴` (U+25B4) asc appended to the header label (`:197`) |
| **Direction flip** | click the already-active header | `desc → asc → desc` (`:198`) |
| **Key change** | click a different sortable header | direction **resets to `desc`** (`:198`) |

**The load-state sort is unreachable and unchangeable without a density switch.** Sort
controls exist *only* as table headers (`:111`), which are unmounted in binder view — the
default view. So on arrival the grid is ordered `value desc` (`:155`, `:171`–`:172`) with
**no visible control, no indicator, and no way to change it** until the user switches to
terminal, sorts, and switches back. Sort state does survive the toggle (`viewBind` /
`viewTerm` write only `view`, `:183`).

This is the single largest behavioural difference from Set, which exposes four sort pills
outside the table (`Set:89`–`:93`) and therefore keeps sorting reachable in both densities.

### 4.4 METADATA PENDING states

**There are none on this screen.** No `sc-if`, no em-dash fallback, no amber banner, no
null branch anywhere in `renderVals` (`:166`–`:211`). Every field renders unconditionally.

For the Pokédex chips this is *correct by ruling*: `DESIGN_NOTES.md:70` — the Pokédex
schema is external and pre-populated, *"so no METADATA PENDING honesty state is needed on
Pokédex attributes (unlike card/set metadata, which can be missing — spec §4.8)."*
`HANDOFF.md:107` repeats it.

For the **`Year` column and the tile's `set · year` line, it is a gap.** Release year is
card/set metadata, not Pokédex metadata — it comes from the set metadata table that does
not exist yet (`DECISIONS.md:199`), it must be curated for ~303 sets, and it is precisely
the category the ruling carves *out* of the exemption. Yet `r.year` is `String(c.year)`
(`:201`) and the tile renders `{{ tl.set }} · {{ tl.year }}` (`:97`) with no null branch:
an uncurated set yields `undefined` in the cell and a dangling `· ` separator in the tile.
Compare Browse, which does carry a METADATA PENDING badge for exactly this
(`DESIGN_NOTES.md:71`, "Uncategorized shelf w/ METADATA PENDING badge"). See §7.

### 4.5 States that are absent

| Missing state | Evidence |
|---|---|
| **Negative 90d** | the tile's `color: var(--pos)` is baked into the style attribute (`:75`), with no conditional and no `pct()` call. A species in decline has no rendering. Same defect as `Set:77`–`:78`. |
| **Missing release year** | §4.4 |
| **Empty roster** | no `sc-if` guards `rows` / `tiles` (`:91`, `:114`); a species with zero tracked printings renders a header row over nothing, `0 of 34 printings`, and the sentence "every Umbreon printing we track." |
| **Single-printing species** | unguarded; "Sets 1", "Printings 1" render normally, but the aggregate framing (Total value, 90d) becomes a single card's price restated three ways. |
| **Loading / error** | none. |
| **Sort indicator in binder** | §4.3 — the state exists but has no rendering in the default view. |
| **Multi-species card marker** | the footer states cards can appear under several species (`:127`), but **no row or tile carries any badge, co-star name, or indicator** that a given printing is shared. |

---

## 5. Interactions

### 5.1 Header

| Control | HTML | Consequence |
|---|---|---|
| Breadcrumb `Browse` | `:58` | Navigates to `Cardstock Browse.dc.html`. |
| Avatar | `:61` | Inert. |
| Pokédex chip ×6 | `:66` | `cursor: help` + native `title`. **Not filters, not links** — hovering explains the attribute; clicking does nothing. |
| Stat tile ×4 | `:72`–`:75` | Inert. `Sets 19` in particular is not a disclosure or a link to a set list. |

### 5.2 Toolbar

| Control | HTML | Consequence |
|---|---|---|
| `binder` button | `:81` → `viewBind` (`:183`) | `setState({view:'binder'})`. Sort state preserved but its controls disappear (§4.3). |
| `terminal` button | `:82` → `viewTerm` (`:183`) | `setState({view:'terminal'})`. Reveals the only sort affordances on the screen. |

No other toolbar control exists — no sort pills, no filter, no set/era selector, no
"group by set" toggle.

### 5.3 Table

| Control | HTML | Consequence |
|---|---|---|
| Header label ×6 | `:111` → `c.sort` (`:198`) | `Year`, `PSA 10`, `ROC 3M` set `sort`/`sortDir`; **`Card`, `Set`, `Sales / mo` are no-ops that still show `cursor: pointer`** and a hover recolour. |
| Resize grip ×6 | `:111` → `c.rs` = `startResize(k)` (`:196`, `:156`–`:165`) | `mousedown` captures `clientX` + current width; `document` `mousemove` writes `colW[k] = max(52, startW + dx)` live; `mouseup` detaches. All six columns, floor 52px, no ceiling, not persisted. |
| Row card name | `:116` | Link to `Cardstock Card.dc.html`; hover recolours to `--acc`. |
| Row `Set` cell | `:117` | **Inert text.** Not a link to `/set/{id}` — a notable omission given every set named here has a Set page in the inventory (`HANDOFF.md:77`). |

### 5.4 Binder grid

| Control | HTML | Consequence |
|---|---|---|
| Tile | `:92` | Whole tile links to `Cardstock Card.dc.html`; hover raises a shadow. |
| `image-slot` | `:94` | Card-art surface, `placeholder=" "` so empty is invisible. |

**Grid ordering** = `tiles = sorted.map(...)` (`:204`), the identical array backing `rows`
(`:200`). Tile order therefore always equals table row order, and on load that is
**latest PSA 10 price, descending** — not chronological, not by set, not by era, despite
the toolbar sentence advertising "all eras" (`:84`). DOM order is the visual order; the
grid is `auto-fill` left-to-right, top-to-bottom, so the most valuable printing is always
top-left.

### 5.5 Shared chrome

Five nav links, logo link, `<cardstock-search>` (`:50`) whose grouped typeahead includes a
Characters group capped at 4 (`DISPLAY_VOCABULARY.md:195`), and the account circle (`:51`).

---

## 6. Rules and invariants

1. **How a card counts toward a species — the central rule.** Stated in the UI footer
   (`:127`): *"a card picturing multiple Pokémon appears under every species it features."*
   The relation is many-to-many via a join table (`card_characters(card_id, species)`,
   `DESIGN_NOTES.md:69`; `HANDOFF.md:107`), and **a card is counted exactly once per
   featured species**. What the HTML *implies* on top of that ruling:
   - one row / one tile = one **card row** (`cards.id`), not one species-card pair —
     `PRINTS` entries are distinct printings (`:144`–`:153`);
   - therefore within a single species page every card appears **once**, and dedup is not
     a rendering concern — it is a query concern (`DISTINCT card_id` after the join);
   - `Printings` (`:72`) = `count(distinct card_id)` for this species —
     `sorted.length of 34` (`:186`) uses that same number as its denominator, so the roster
     is a subset of exactly the set the tile counts;
   - `Sets` (`:73`) = `count(distinct cards.set_id)` over those same cards — never larger
     than `Printings`, and the seed is consistent with that (6 sets / 8 printings);
   - `Total value` (`:74`) = `sum(latest PSA 10)` over those cards, each added once;
   - **consequence the UI never surfaces:** summing `Total value` across species
     double-counts every multi-Pokémon card. Species aggregates are correct in isolation
     and non-additive across species. Nothing on the screen warns of this.
2. **One sort, two surfaces.** `rows` and `tiles` both map the same `sorted` array
   (`:200`, `:204`). Grid order always equals table order.
3. **Direction resets on key change** (`:198`); a new key always starts `desc`.
4. **No filtering, no exclusion, ever.** `sorted = PRINTS.slice().sort(...)` (`:172`) with
   no `filter` — contrast `Set:192`. There is no sufficiency gate on this screen because
   no column has a maturity floor: `Year` and `Sales / mo` are the two non-`price_months`
   fields and neither is filtered on.
5. **The comparator handles three keys only.** `val()` (`:171`) returns `roc`, `year`, or
   falls through to `price`. Any unknown key silently sorts by price.
6. **Year is a set attribute, not a card attribute.** The seed carries the same year for
   every printing of the same set (Evolving Skies → 2021 across three rows, `:145`,
   `:146`, `:151`). Implementation: join `cards.set_id` → set metadata release date, take
   the year. This is also why `Year` sorting is coarse — printings from one set tie.
7. **Prices are one tier.** Latest monthly PSA 10 (`:192`, restated `:127`). The `90d` and
   `Total value` tiles carry no tier label.
8. **"Latest" is `max(observed_at)`, not the newest month** — change-only storage
   (`CLAUDE.md:53`).
9. **Sign glyph is U+2212** (`:169`); **zero is positive** (`roc >= 0`, `:202`, `:206`).
10. **Column width floor 52px** (`:160`), per column, no ceiling — the grid can overflow
    `main`.
11. **Every card affordance resolves to the Card page** (`:92`, `:116`). No modal, no peek,
    no inline expand, and no outbound link to the Set page.
12. **`PAL` is captured at construction** from `localStorage` (`:134`–`:141`) and never
    re-read; a mid-session theme change does not restyle computed colours until remount.
13. **Read-only.** No watchlist, no binder add, no export, no refresh request.

---

## 7. Open questions

1. **Route key.** `/character/{name}` (`HANDOFF.md:78`) is name-keyed while Set is
   id-keyed. Casing, spacing, and the forms with punctuation (Mr. Mime, Farfetch'd,
   Nidoran♀, Type: Null, Ho-Oh) all need a slug rule, and the species table has ids.
   Name-keying also collides with the many-to-many join, which stores `species`
   (`DESIGN_NOTES.md:69`) — is that a name or an id?
2. **Define the species `90d` figure.** Value-weighted across all printings, equal-weighted,
   or the change in `Total value`? New printings entering the window (a species gains
   cards over time) will otherwise read as price appreciation. Same class of problem as the
   Set index, and this screen has no sparkline to make the series legible.
3. **Should the Character page have an index chart?** Set gets a 12M sparkline
   (`Set:70`–`:75`); Character gets four numbers and no series (§8). If the answer is yes,
   it needs the same method decision as Q2 plus a rebase rule for a constituent basket
   that changes membership across 25 years of printings.
4. **Missing release year rendering.** §4.4. The `Year` column and the `set · year` tile
   line have no pending state, and release date is uncurated for ~303 sets today. Does
   Browse's METADATA PENDING badge (`DESIGN_NOTES.md:71`) apply here, or does the year
   simply blank?
5. **Pokédex attribute scope.** Chips 4–6 (colour, egg group, habitat) exceed the schema
   enumerated in `DESIGN_NOTES.md:70`, while region and status — both ruled in and both
   Browse facets — get no chip (§8). Which set is authoritative, and is the
   no-METADATA-PENDING exemption meant to cover the three extras?
6. **Should the `Set` cell link to the Set page?** (`:117`) It is inert text today.
7. **Should chips be filters?** They are `cursor: help` only (`:66`). Browse already
   filters species by Type/Generation/Region/Status; a chip click could seed that.
8. **Multi-species printings are invisible.** The footer promises cards appear under every
   featured species (`:127`) but no row or tile shows the co-featured species. Is that
   deliberate minimalism or a missing badge?
9. **Which printings, and is "all" true?** The toolbar says *"every Umbreon printing we
   track, all eras"* (`:84`) while the count says `8 of 34` (`:186`). Is the roster capped,
   paginated, or was the seed simply short? Unlike Set (`Set:139`), nothing explains it.
   34 rows fit on a page; 300+ for a heavily printed species (Pikachu, Charizard) do not.
10. **Sortability.** `Set` and `Sales / mo` have dead click handlers (`:198`). And should
    sorting be reachable in binder view — pills like Set's, or an explicit ruling that the
    grid is value-ranked by definition?
11. **Species identity colour.** The avatar gradient (`:61`) and accent bar (`:54`) are
    hard-coded per species. `DESIGN_NOTES.md:71` calls this an "accent initial circle" on
    Browse too, so the palette is shared and must be stored somewhere.
    `DECISIONS.md:201` flags the related dominant-colour-from-art idea as conflicting with
    D-026 because it was specified as a **new column on the scraper's `cards` table**.
12. **`Total value` formatting.** `$96.4K` (`:74`) vs `money()`'s `$1,486` (`:168`) — two
    abbreviations on one screen. Where is the K/M threshold?
13. **Falling species.** The `90d` tile has no negative rendering (`:75`).

---

## 8. Contradictions found

| Claim | Source | What the HTML actually does |
|---|---|---|
| The Character page has a **character index chart** | Task brief | **There is no chart.** The file contains no `<svg>`, no `<polyline>`, no sparkline and no index block anywhere in `:35`–`:130`, and `renderVals` (`:166`–`:211`) computes no point series — contrast Set, which has both the `idxPts` binding (`Set:72`) and the `IDX` array (`Set:172`). The header's right side holds **four static stat tiles** instead (`:71`–`:76`): Printings 34, Sets 19, Total value $96.4K, 90d +6.8%. The only trend figure on the screen is that single `90d` number. |
| Species aggregates count a card **once per featured species**; card ↔ species is many-to-many via a join table | `DESIGN_NOTES.md:69`, `:85`; `HANDOFF.md:107` | **Confirmed by the HTML**, and stated in user-facing copy: *"a card picturing multiple Pokémon appears under every species it features"* (`:127`). The roster is one row per card (`:144`–`:153`), so within a species every card appears once. The HTML adds nothing contradicting the ruling — but it also renders **no marker** on shared printings, so the rule is invisible except in footer prose. |
| Pokédex attributes never need a METADATA PENDING state, because the schema is external and pre-populated | `DESIGN_NOTES.md:70`; `HANDOFF.md:107` | **Consistent for the chips** (`:174`–`:181`, no null branch — correct). **But the exemption is being applied too widely on this screen:** `Year` (`:118`, `:201`) and the tile's `set · year` line (`:97`) are *set* metadata, explicitly carved out by that same ruling as data that "can be missing," and they have no pending state either. Release date does not exist for any set today (`DATA_MODEL.md:139`–`146`; `DECISIONS.md:199`). |
| The external Pokédex schema is "name, generation, region, type(s), evolution line, status" | `DESIGN_NOTES.md:70` | The page renders **six** chips (`:174`–`:181`), three of which are outside that list — `Black` (Pokédex colour), `Field egg group`, `Urban habitat` — while **region and status get no chip at all**, despite both being ruled in and both being Browse facets (`DESIGN_NOTES.md:71`). The chip set and the ruled schema are two different lists. |
| Set and Character share a terminal / binder density toggle | `DISPLAY_VOCABULARY.md:200` | **True but not symmetric**, in three ways the HTML makes explicit: (a) **defaults are opposite** — Character opens `binder` (`:155`), Set opens `terminal` (`Set:174`); (b) **button order is reversed** — binder-then-terminal (`:81`–`:82`) vs terminal-then-binder (`Set:85`–`:86`); (c) **Character has no sort pills** (`Set:89`–`:93` has four), so in its *default* view there is no sort control at all (§4.3). |
| Character page shows a release **year** | Task brief; implied by `HANDOFF.md` inventory | **Confirmed** — `Year` column (`:118`) and tile sub-line (`:97`). Note this is the field the Set page was claimed to have and does not (`DESIGN_NOTES.md:72` vs `Set:66`): release year is rendered **only here**, per printing, and is unbacked in both places. |
| Species aggregates include a total market value | `DESIGN_NOTES.md:69` ("printings count, total market value") | **Confirmed** (`:72`, `:74`), plus two figures that ruling does not mention: `Sets` (`:73`) and `90d` (`:75`). |
| Browse species grid is "ordered by total market value" | `DESIGN_NOTES.md:71` | Not this screen, but the analogous ordering **does** hold here: the roster's default sort is `value desc` (`:155`, `:171`). Recorded because it is the only justification available for the grid's unlabelled default order. |
| Character pages are P2 / not built | `DESIGN_NOTES.md:71` ("character pages are P2 (#)") | **Stale.** Superseded by `DESIGN_NOTES.md:86` and `HANDOFF.md:108` ("Character page was built in v1"), and by the file's existence. Cite `:86`, not `:71`, for build status. |
| Prototypes have no props | `DESIGN_NOTES.md:141` | **Confirmed** — `data-props=""` (`:132`). |

### Fields on this screen requiring the two non-scraped tables (`DECISIONS.md:199`)

| Field | HTML | Table needed |
|---|---|---|
| `Printings` (34) | `:72` | **character tags** (card → Pokémon) |
| `Sets` (19) | `:73` | character tags (then `cards.set_id`) |
| `Total value` ($96.4K) | `:74` | character tags (membership); prices are backed |
| `90d` (+6.8%) | `:75` | character tags + an index method |
| `shownCount` denominator | `:186` | character tags |
| roster membership itself | `:144`–`:153` | character tags — **the entire page has no rows without this table** |
| `Year` column | `:118`, `:201` | **set metadata** (release date for ~303 sets) |
| `set · year` tile line | `:97` | set metadata (the `year` half; `set` is `sets.name` ✔) |
| species name, chips, avatar colour | `:63`, `:174`–`:181`, `:61` | external Pokédex schema (`DESIGN_NOTES.md:70`) — a third dependency, distinct from both CardStock tables |

**Net:** Character is the most data-blocked screen of the two. Set loses one header chip
without its non-scraped table; Character loses **every row, all four header stats, and the
species identity itself**.
