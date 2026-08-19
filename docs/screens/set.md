# Screen: Set

> **Authority.** Everything below is read from `CardStock Mockup/Cardstock Set.dc.html`
> (242 lines, read directly 2026-08-10). Bare `:N` citations refer to that file. Data
> claims cite `../PokemonInvestBatch/DATA_MODEL.md`. Where a markdown doc disagrees with
> the HTML, the HTML wins and the disagreement is recorded in §8 — never averaged.

> **Amended 2026-08-15 (Catalog phase design, D-110 — build from
> `docs/superpowers/specs/2026-08-15-catalog-phase-design.md`, which supersedes this spec where
> they differ).** Owner rulings of that date: **(a)** §7.1 answered — the code chip renders
> `set_details.code` **uppercase verbatim** (`swsh7` → `SWSH7`, no invented padding), and the
> header **gains an era chip** beside it (superseding this spec's "no era anywhere on the Set
> page"); a `Pending` set renders one **`◌ metadata pending`** chip in place of both.
> **(b)** The index sparkline, 30D/90D deltas, and RS pct column render per the **D-102
> vocabulary** — labels print, dashes in value runs, one ◌ per gated unit, RS pill disabled —
> until the analytics worker (§7.2/§7.3 deferred to it; §7.6's negative rendering is defined
> now, applied at arming). **(c)** §7.4 answered: **full roster, virtualized** — no
> "most-traded" cap; the footer is rewritten to "Showing all {n} tracked cards · prices are
> latest monthly PSA 10". **(d)** §7.5 answered: every count uses `delisted_at IS NULL AND
> not_a_card_at IS NULL`. **(e)** §7.7 answered: **Sales/mo is sortable, with a fifth pill** —
> the pill row covers every sortable key so binder density keeps the full sort set;
> dead-looking unsortable headers (`Card`) lose the pointer/hover. **(f)** The pop banner's dates are
> computed from the excluded cards' first observations (the `Jul 2026` literal does not
> port), the all-pending state is guarded with proper copy — and it is the ship state until
> ~late Sep 2026. **(g)** An empty-roster state is added. **(h)** Negative pop Δ renders a
> real sign (the hard-coded `+` does not port).

> **Amended 2026-08-18 (build).** Sorting on nullable keys (`value`, `ROC 3M`) places rows
> without a value **last in both directions** — a dashed cell never implies a rank (an
> ascending sort must not present a priceless card as "cheapest"). The pop-sort exclusion
> banner terminates with a period when no unlock dates exist among the excluded rows
> (zero-base cards carry a first observation but no unlock date).

> **Amended 2026-08-18 (build, controller ruling R23).** Pop Δ 60d counts only observations
> on/after the D-033 floor (2026-09-01), mirroring the Card page's census mechanic (D-093) and
> the About-data promise — first armings ~2026-10-31, not the design spec §2's
> first-observation-based "~late Sep 2026" estimate, which embedded the omission.

> **Amended 2026-08-18 (owner UAT, D-114).** The bare `loading-strip` is replaced by the shared
> `LoadingRing` — one 48px ring at `inset: 20vh` that the boot indicator fills (real download
> progress) and this page's data fetch then spins in place. Contract: shared-components.md §4.8.

> **Amended 2026-08-19 (owner ruling, D-125).** The `set index · 12M` slot keeps its
> worker-phase arming, and its data contract when it arms is two-source: the monthly price
> history draws the **full 12-month width on day one** (backfilled years deep), a
> **sales-based segment grows month-by-month from the D-033 floor** (2026-09-01), and the
> boundary between them **draws where it falls, never blended** — rule #1 at set grain.
> Page-load computation was probed and rejected (D-124: 46ms, but methodology and
> cross-page consistency are the block).

---

## 1. Identity

| | |
|---|---|
| **Screen label** | `Set` (`data-screen-label="Set"`, `:35`) |
| **Prototype** | `CardStock Mockup/Cardstock Set.dc.html` |
| **Route** | `/set/{id}` — from `HANDOFF.md:78`. The HTML is a static file and asserts no route. |
| **Nav section** | **Browse** is the active tab (`:47` — weight 600, `--ink`, 2px `--acc` bottom border). Set is a leaf of Browse, not its own nav section. |
| **Breadcrumb** | `Browse › Evolving Skies` (`:58`); parent links to `Cardstock Browse.dc.html`. |
| **Component props** | None. `data-props=""` (`:144`) — the screen takes no inputs; the set identity is hard-coded in the prototype. |

**Purpose.** The contents roster for one set: every tracked card in it, ranked by a
market metric the user picks, with a set-level index summary above it. It is a
*reference/browse* page, not an analysis page — there is no filtering, no watchlist
action, no export. Every row and tile is a link into the Card page.

---

## 2. Layout

Single column, top to bottom. `main` is `max-width: 1480px`, `padding: 14px 20px 28px`,
`display:flex; flex-direction:column; gap:14px` (`:56`).

1. **Nav bar** — 48px, sticky, `z-index:20` (`:37`–`:52`). Logo lockup → Home; five section
   links (Home / Screener / Charts / Binder / **Browse**); flex spacer; `<cardstock-search>`
   (`:50`); 28px circular account link → Profile (`:51`). Shared chrome, identical on every
   app page.
2. **Accent bar** — 4px, `linear-gradient(90deg, #2B2D42, #5C6B9E, #7E6BA8)` (`:54`).
   Three stops. (Character's equivalent has two — `Character:54`.)
3. **Breadcrumb** — 13.5px (`:58`).
4. **Header card** — `--card` surface, 1px `--line`, radius 10, padding 16, `display:flex;
   align-items:center; gap:24px` (`:60`). Three zones:
   - *Left* (`:61`–`:67`): h1 set name (Inter Tight 700 / 26px) + mono set-code chip on one
     row (`:62`–`:65`); sub-line underneath (`:66`).
   - *Spacer* (`:68`).
   - *Right* (`:69`–`:80`): a 220px-wide sparkline block with caption (`:70`–`:75`), then a
     right-aligned two-line delta stack (`:76`–`:79`).
5. **Toolbar** — one flex row (`:83`–`:96`): density segmented control (`:84`–`:87`), the
   word `sort` (`:88`), the sort-pill group (`:89`–`:93`), spacer, right-aligned count
   (`:95`).
6. **Exclusion banner** — conditional, amber (`:98`–`:100`).
7. **Roster** — exactly one of two mutually exclusive blocks:
   - *Terminal* (`:102`–`:120`): a card-surface section; one CSS-grid header row (`:104`)
     and N CSS-grid data rows (`:110`), sharing `grid-template-columns: {{ gridCols }}`.
   - *Binder* (`:122`–`:137`): `repeat(auto-fill, minmax(180px, 1fr))`, gap 12 (`:123`).
8. **Footer note** — 12.5px muted (`:139`).

The roster blocks are siblings under `sc-if`, so switching density replaces the block
entirely; nothing is hidden with CSS.

---

## 3. Data contract

Legend for **Backing**: **✔** = queryable from the scraper's eight tables today ·
**⚠** = derivable but needs a defined method or a maturity wait · **✘** = no backing data
exists (needs one of the two non-scraped CardStock tables, `DECISIONS.md:199`).

### 3.1 Set header

| Field | Rendered as | HTML | Backing |
|---|---|---|---|
| `set.name` | h1, Inter Tight 700 26px | `:63` (literal `Evolving Skies`) | ✔ `sets.name` (`DATA_MODEL.md:143`) |
| `set.code` | mono chip, 11.5px, `--mutbg` on `--line`, uppercase tracking | `:64` (literal `SWSH07`) | ✘ **no column exists.** `sets` is `id/slug/name/discovered_at/last_seen_at/last_walked_at` only (`DATA_MODEL.md:139`–`146`). |
| `set.cardsTracked` | mono integer inside the sub-line | `:66` (literal `237`) | ⚠ `count(cards WHERE set_id = …)`; the exclusion policy for `delisted_at` / `not_a_card_at` rows (`DATA_MODEL.md:169`,`:171`) is undefined — see §7. |
| `set.firstSaleObserved` | mono `MMM yyyy`, prefixed by the literal words *first sale observed* | `:66` (literal `Dec 2021`) | ⚠ `min(sales.sold_on)` across the set's cards. Legitimately predates the crawler: a first visit captures whatever the ~30-row bucket windows still held, "months or years" for thin cards (`DATA_MODEL.md:380`–`386`). It is a **bucket-window artifact, not a release date and not a coverage claim.** |
| `idxPts` | SVG `<polyline points>` | `:72`, computed `:197` | ⚠ needs an index definition — see 3.2 |
| sparkline caption | static text `set index · 12M` | `:74` | — |
| 30D delta | mono 15px 700, **`--pos` unconditionally** | `:77` (literal `+4.1%`) | ⚠ see 3.2 |
| 90D delta | mono 15px 700, **`--pos` unconditionally** | `:78` (literal `+9.7%`) | ⚠ see 3.2 |

Every field in this table is a **template literal**, not a binding. The set header is the
only region of this screen with no `{{ }}` interpolation except the sparkline points. An
implementation must parameterise all of it.

### 3.2 Set index sparkline

- Source series `this.IDX` = 12 integers, `[100, 101, 99, 103, 105, 104, 107, 109, 108, 111, 114, 118]` (`:172`).
  Twelve points, matching the `12M` caption — one per month, **rebased to 100 at the
  left edge** (the seed's first value is exactly 100).
- Projection (`:197`): `x = i / (n-1) * 220`; `y = 48 − (v − min) / (max − min) * 42`.
  So the polyline occupies y ∈ [6, 48] in a `viewBox="0 0 220 52"`, min pinned to the
  bottom, max to the top — **the series is auto-scaled to its own range, so the sparkline
  never shows absolute level, only shape.**
- SVG is `width:100%; height:52`, `preserveAspectRatio="none"`, stroke `var(--acc)`,
  `stroke-width:1.8`, `vector-effect="non-scaling-stroke"` (`:71`–`:72`). Non-scaling-stroke
  is what keeps the line 1.8px after the horizontal stretch.
- No axis, no labels, no markers, no fill, no tooltip, no hover, no last-point dot.
- **Backing:** no set-level index exists in the database. It is computable from
  `price_months` (which backfills six tiers monthly to ~Dec 2020 on a card's first visit,
  `DATA_MODEL.md:176`–`179`), but the *method* is undefined and is a product decision:
  constituent membership, weighting, tier (the roster is PSA 10, `:212`), rebase epoch,
  and treatment of cards not yet visited. The 30D / 90D figures are the same series read at
  two lags and inherit the same gap. See §7.

### 3.3 Toolbar

| Field | Type / values | HTML |
|---|---|---|
| `vtBg` / `vtFg` | terminal button colors — active: `PAL.acc` bg / `PAL.card` fg; inactive: `PAL.card` bg / `PAL.mut` fg | `:200` |
| `vbBg` / `vbFg` | binder button, same rule | `:201` |
| `sorts[]` | exactly 4 items; each `{ label, tip, pick, bg, fg, bd }` | `:202`–`:205` |
| `sorts[].label` | `value` · `ROC 3M` · `RS` · `pop Δ` | `:202` |
| `sorts[].tip` | `"Sort by " + label`, and **when that pill is already active** it appends `" — click again to reverse the order"` | `:203` |
| pill colors | `pill(on)` → on: bg `acc`, fg `card`, border `acc`; off: bg `card`, fg `mut`, border `line` | `:189` |
| `shownCount` | `` `${sorted.length} of 237 cards` `` — mono 12.5px | `:206`, rendered `:95` |
| `hasExcluded` | bool — `excluded > 0 && sort === 'pop'` | `:207` |
| `excludedNote` | `` `${n} cards excluded from this sort — pop Δ 60d needs two census observations and their first was Jul 2026. They'll join the sort next census.` `` | `:208` |

`sorts[].label` `value` maps to sort key `value` (`:202`), which the comparator resolves to
`c.price` (`:191`) — the pill named *value* sorts the *PSA 10* column.

### 3.4 Column model (terminal)

`gridCols` = `minmax({colW.name}px, 1.4fr)` followed by `price roc rs pop vol` as fixed
pixel tracks (`:209`). The name column is the only elastic one.

| # | Header | `k` | Sort key | Default width | Tooltip | Backing |
|---|---|---|---|---|---|---|
| 1 | `Card` | `name` | **none** (`s: null`) | 230 | `Card name` | ✔ `cards.name` |
| 2 | `PSA 10` | `price` | `value` | 100 | `Latest monthly PSA 10 price — click to sort` | ✔ latest `price_months` row for tier `Psa10` by `max(observed_at)` — **not** newest month (append-only/change-only, `CLAUDE.md:53`) |
| 3 | `ROC 3M` | `roc` | `roc` | 92 | `3-month rate of change — click to sort` | ✔ from `price_months` |
| 4 | `RS pct` | `rs` | `rs` | 84 | `Relative strength vs market index, percentile — click to sort` | ⚠ needs a defined market index + universe |
| 5 | `Pop Δ 60d` | `pop` | `pop` | 96 | `PSA 10 census growth over 60 days — click to sort` | ⚠ `populations`, but history starts at each card's first visit (`DATA_MODEL.md:120`–`121`) — this is why the pending state exists |
| 6 | `Sales / mo` | `vol` | **none** (`s: null`) | 90 | `Observed sales per month, all tiers` | ⚠ derivable from `sales` **forward of each card's seam**; pre-seam volume is permanently unavailable (`DATA_MODEL.md:391`–`392`) |

Defaults from `state.colW` (`:174`). Each header cell renders `{{ c.name }}{{ c.arrow }}`
centred, plus a `│` resize grip at the right edge (`:106`).

### 3.5 Row model (terminal) — `rows[]`, `:222`–`:230`

| Field | Formatting | Line |
|---|---|---|
| `r.name` | 14px / 500, centred, ellipsised; wrapped in an `<a>` to the Card page | `:111`, `:223` |
| `r.price` | `money()` = `'$' + Math.round(n).toLocaleString('en-US')` — **whole dollars, thousands separator, no cents** | `:187`, `:223` |
| `r.roc` | `pct()` = sign + `abs.toFixed(1)` + `'%'`; positives get `+`, negatives get **U+2212 MINUS SIGN**, not a hyphen | `:188`, `:224` |
| `r.rocFg` | `PAL.pos` when `roc >= 0`, else `PAL.neg2` (zero counts as positive) | `:224` |
| `r.rs` | `` `${rs}th` `` — the ordinal suffix is **always `th`**; 1st/2nd/3rd/21st… are not handled | `:225` |
| `r.pop` | `null → '—'` (U+2014); otherwise `'+' + toFixed(1) + '%'` — **the `+` is hard-coded, so a negative census delta would render as `+-2.0%`** | `:226` |
| `r.popFg` | `null → PAL.mut3` · `>= 5 → PAL.neg2` · else `PAL.mut`. Red means *supply grew fast* | `:227` |
| `r.popTip` | null → `Census too young — first observation Jul 2026, deltas begin next census`; else `+N.N% PSA 10 census growth over 60 days` | `:228`, applied as `title` on the cell `:115` |
| `r.vol` | `String(vol)` — bare integer, `--mut` | `:229` |

Row chrome: `padding: 6px 16px`, `align-items:center`, 1px `--line4` bottom border
(`:110`). All numeric cells are JetBrains Mono and **centre**-aligned (not right-aligned).

### 3.6 Tile model (binder) — `tiles[]`, `:231`–`:236`

| Field | Formatting | Line |
|---|---|---|
| `tl.thumbBg` | `linear-gradient(160deg, acc[0], acc[1])` — a per-card two-stop accent pair carried on the seed (`:157`–`:170`) | `:234` |
| `tl.slotId` | `'art-' + name.toLowerCase().replace(/[^a-z0-9]+/g,'-')` | `:235` |
| `tl.name` | 13.5px / 600, single line, ellipsised | `:129` |
| `tl.price` | same `money()` | `:232` |
| `tl.roc` / `tl.rocFg` | same `pct()` and same threshold | `:233` |

Tile chrome: whole tile is an `<a>` to the Card page (`:125`); art box is
`aspect-ratio: 325/450`, radius 5, painted with `thumbBg` and holding an `<image-slot
placeholder=" ">` (`:126`–`:128`) — the gradient is the backdrop the empty slot sits on.
Hover raises `box-shadow: 0 6px 20px rgba(20,19,26,0.10)`.

**A binder tile drops `rs`, `pop` and `vol` entirely.** Density switching is lossy in one
direction: name / PSA 10 / ROC 3M survive, the other three metrics do not.

### 3.7 Static copy

- Sub-line connectives: `cards tracked · first sale observed` (`:66`).
- Sparkline caption: `set index · 12M` (`:74`).
- Toolbar label: `sort` (`:88`).
- Density tooltips: `Terminal density — more rows, tighter type, every metric column` /
  `Binder density — fewer rows with card art` (`:85`, `:86`).
- Resize grip tooltip: `Drag to resize` (`:106`).
- Footer: `Showing the set's most-traded cards · prices are latest monthly PSA 10 · full
  roster ships with the real corpus` (`:139`).

### 3.8 Seed shape (illustrative, not contract)

`this.CARDS` (`:156`–`:171`) holds **14** rows of
`{ name, price, roc, rs, pop, vol, acc[2] }`. Two rows carry `pop: null` — *Dragonite V
(Alt Art)* (`:165`) and *Glaceon V (Alt Art)* (`:168`). One row carries a negative `roc`
(*Leafeon V*, `−1.4`, `:169`) and one carries both a negative `roc` and the lowest values
(*Duraludon VMAX*, `:170`) — the seed deliberately exercises both colour branches and both
pop branches. `hint-placeholder-count` on the roster loops is 12 (`:109`, `:124`), on `cols`
6 (`:105`), on `sorts` 4 (`:90`) — streaming-skeleton hints, not counts.

---

## 4. States

### 4.1 State variable inventory (`state`, `:174`)

| Key | Domain | Default | Persisted? |
|---|---|---|---|
| `view` | `'terminal'` \| `'binder'` | **`'terminal'`** | No |
| `sort` | `'value'` \| `'roc'` \| `'rs'` \| `'pop'` | `'value'` | No |
| `sortDir` | `'desc'` \| `'asc'` | `'desc'` | No |
| `colW` | 6 independent integers ≥ 52 | `{name:230, price:100, roc:92, rs:84, pop:96, vol:90}` | No |

Nothing on this screen is persisted. `localStorage` is read only for `cardstock-theme` and
`cardstock-cvd` (`:33`, `:146`), which are app-wide chrome, not screen state.

The full state space is the cross product `2 views × 4 sort keys × 2 directions`
(16 combinations) × continuous `colW`. Every combination is legal and reachable, but the
sort controls live **only in the toolbar pills and the table headers**, so in binder view
only the 4 pills are reachable (see §5.2).

### 4.2 Density states

| State | Trigger | Effect |
|---|---|---|
| **Terminal** (default) | initial load; click `terminal` (`:85`) | `isTerm` true (`:198`) → table renders (`:102`); binder grid absent. Button takes accent bg / card fg. |
| **Binder** | click `binder` (`:86`) | `isBind` true → tile grid renders (`:122`); table absent. Metrics reduced to name / price / roc. |

The two are mutually exclusive and jointly exhaustive — `isTerm` and `isBind` are computed
from the same enum, so there is no "both" and no "neither."

### 4.3 Sort states

| State | Trigger | Effect |
|---|---|---|
| **Active pill** | `sort === pill.k` | pill inverts to accent bg (`:189`); its tooltip gains `— click again to reverse the order` (`:203`) |
| **Active column arrow** | `sort === col.s` | ` ▾` (U+25BE) when desc, ` ▴` (U+25B4) when asc, appended to the header label (`:219`) |
| **Direction flip** | click the already-active pill or header | `desc → asc → desc` (`:204`, `:220`) |
| **Key change** | click a different pill or header | direction **resets to `desc`**, never inherited (`:204`, `:220`) |

Pills and column headers write the *same* two state fields, so the pill highlight and the
header arrow can never disagree — the `PSA 10` header and the `value` pill share key
`value` (`:202`, `:212`).

### 4.4 METADATA PENDING / sufficiency states

This screen carries the honesty pattern in **two** places, both driven by
`pop == null`:

1. **Per-cell pending** — a null `Pop Δ 60d` renders `—` in `PAL.mut3` with the tooltip
   `Census too young — first observation Jul 2026, deltas begin next census` (`:226`–`:228`).
   This state is visible in **every** sort except `pop`, because the null rows are only
   filtered out when `pop` is the active key (`:192`).
2. **Exclusion banner** — amber, `rgba(176,127,26,0.06)` fill / `0.25` border / `--warnInk`
   text (`:99`), shown when `hasExcluded` = `excluded > 0 && sort === 'pop'` (`:207`).
   Copy at `:208` states the *reason* (two census observations required) and the
   *resolution* (next census), not just the count.

Consequences worth stating explicitly:

- The banner is **impossible to see** unless the user selects the `pop Δ` sort. Nulls are
  otherwise silently present in the list.
- When `sort === 'pop'`, `shownCount` shrinks (numerator = `sorted.length`, `:206`), and
  the banner explains the shortfall. The two readouts are designed to be read together.
- There is **no `show anyway →` escape hatch** here, unlike the Screener's sufficiency
  pattern (`DISPLAY_VOCABULARY.md:133`). The exclusion is unconditional.
- `Jul 2026` in the copy is a **hard-coded literal**, not derived from the census data.

### 4.5 States that are absent (the state space has holes)

| Missing state | Evidence |
|---|---|
| **Negative index delta** | the 30D/90D readouts are literal text with `color: var(--pos)` baked into the style attribute (`:77`, `:78`). No conditional, no `--neg` branch, no `pct()` call. A falling set has no rendering. |
| **Empty roster** | no `sc-if` guards `rows` / `tiles`; a zero-length list renders a header row over nothing (`:109`, `:124`). |
| **Loading / error** | none. `x-dc` streams a skeleton via `hint-placeholder-count`, which is a prototype affordance, not a product loading state. |
| **All-pop-null set** | reachable and unguarded: sorting by `pop` would yield `0 of 237 cards` plus a banner. |
| **Unknown / missing set metadata** | the set code chip (`:64`) has no pending or absent variant, even though it is the field most likely to be missing (§8). Contrast `DESIGN_NOTES.md:71`, where Browse's set shelves *do* carry a METADATA PENDING badge. |
| **Negative `Pop Δ`** | `'+' + toFixed(1)` is unconditional (`:226`); the census can only be rendered as growing. |

---

## 5. Interactions

### 5.1 Header

| Control | HTML | Consequence |
|---|---|---|
| Breadcrumb `Browse` | `:58` | Navigates to `Cardstock Browse.dc.html`. |
| Set code chip | `:64` | Inert. Not a link, not a filter, no tooltip. |
| Sparkline | `:71`–`:73` | Inert. No hover, no tooltip, no crosshair, no click-through to Charts. |
| 30D / 90D deltas | `:77`, `:78` | Inert. |

### 5.2 Toolbar

| Control | HTML | Consequence |
|---|---|---|
| `terminal` button | `:85` → `viewTerm` (`:199`) | `setState({view:'terminal'})`. Sort state survives the switch. |
| `binder` button | `:86` → `viewBind` (`:199`) | `setState({view:'binder'})`. Same. |
| Sort pill ×4 | `:91` → `so.pick` (`:204`) | Sets `sort` to that key; sets `sortDir` to the flip of the current direction **if the key was already active**, otherwise `desc`. Re-renders both `rows` and `tiles`. Selecting `pop Δ` additionally filters null-pop cards and can raise the banner. |

The sort pills are the **only** sort control available in binder view — the resize grips
and header labels live inside the table, which is unmounted. Column `Sales / mo` therefore
has no sort affordance in either view.

### 5.3 Table

| Control | HTML | Consequence |
|---|---|---|
| Header label ×6 | `:106` → `c.sort` (`:220`) | Four of them set `sort`/`sortDir` exactly as the pills do. **`Card` and `Sales / mo` are bound to a no-op `() => {}`** yet still render `cursor: pointer` and a hover colour change — two dead controls that advertise themselves as live. |
| Resize grip ×6 | `:106` → `c.rs` = `startResize(k)` (`:218`, `:175`–`:184`) | `mousedown` captures `clientX` and the current width, then `document`-level `mousemove` writes `colW[k] = max(52, startW + dx)` live; `mouseup` detaches both listeners. Applies to **all six** columns including `name`. Floor 52px, no ceiling. Not persisted — lost on any remount. |
| Row card name | `:111` | Link to `Cardstock Card.dc.html`. Hover recolours to `--acc`. Only the name is clickable; the rest of the row is inert. |
| `Pop Δ 60d` cell | `:115` | Carries `title="{{ r.popTip }}"` — a native tooltip on a *data cell*, one of the few non-control tooltips in the app. |

### 5.4 Binder grid

| Control | HTML | Consequence |
|---|---|---|
| Tile | `:125` | The entire tile is the link to `Cardstock Card.dc.html`; hover raises a shadow. |
| `image-slot` | `:127` | Rendering surface for card art, `placeholder=" "` so the empty state is invisible (`:22` zeroes the `::part(empty)` opacity) — an absent image reads as the bare accent gradient, never as a broken slot. |

### 5.5 Shared chrome

Five nav links, the logo link, `<cardstock-search>` (`:50`, `/` focuses per
`DESIGN_NOTES.md:123`), and the account circle → Profile (`:51`).

---

## 6. Rules and invariants

1. **One sort, two surfaces.** `rows` and `tiles` are both `sorted.map(...)` off the same
   array (`:222`, `:231`). Binder tile order always equals table row order. There is no
   independent grid ordering.
2. **Direction resets on key change.** A new sort key always starts `desc` (`:204`,
   `:220`) — the user never inherits an ascending order from a previous metric.
3. **Null filtering is sort-scoped.** `included = CARDS.filter(c => sk !== 'pop' || c.pop != null)`
   (`:192`) — null-pop cards are excluded from *only* the pop sort and present everywhere
   else. The comparator is never handed a `null` (which would produce `NaN` and an
   implementation-defined order).
4. **The denominator is the set, the numerator is the view.** `shownCount` is
   `sorted.length of 237` (`:206`) — 237 is the set's tracked-card count from the header
   (`:66`), so the readout compares what is listed against the whole set, not against the
   seeded subset.
5. **The roster is a subset by design.** `:139` — *"Showing the set's most-traded cards …
   full roster ships with the real corpus."* 14 seeded rows against a 237 denominator. The
   selection rule ("most-traded") is stated in copy but **not implemented anywhere** in the
   prototype; the seed is simply short. See §7.
6. **Prices are one tier.** Every price on this screen is *latest monthly PSA 10* — stated
   in the column tooltip (`:212`) and again in the footer (`:139`). The set index and the
   30D/90D deltas carry no tier label at all.
7. **"Latest" is `max(observed_at)`, not the newest month.** Forced by change-only storage
   (`CLAUDE.md:53`); a naive newest-month query returns nothing for most cards.
8. **Column width floor is 52px** (`:179`), applied per column, with no maximum and no
   total-width constraint — the grid can exceed the 1480px `main` and overflow.
9. **Sign glyph is U+2212, not `-`** (`:188`). Mono digit alignment depends on it.
10. **Zero is positive.** `roc >= 0` takes the `--pos` branch (`:224`, `:233`).
11. **Pop red is a warning, not a gain.** `pop >= 5` renders `PAL.neg2` (`:227`) — rising
    PSA 10 population is supply growth, i.e. bearish. The same colour means opposite
    directions in the ROC and Pop columns; the tooltips are what disambiguate.
12. **Both views link to the same destination.** Every card affordance on this screen
    resolves to the Card page (`:111`, `:125`). No modal, no peek panel, no inline expand.
13. **Theme/CVD colours are resolved twice.** Static markup uses `var(--x, <light literal>)`
    so streaming paints light (`:17`–`:30`); logic-computed colours read `this.PAL`, chosen
    once at construction from `localStorage` (`:146`–`:153`). **`PAL` is captured at
    construction and never re-read**, so a theme change made elsewhere in the session does
    not restyle computed cells until remount.
14. **No write actions.** Nothing on this screen mutates anything — no watchlist, no binder
    add, no refresh request, no export. It is read-only.

---

## 7. Open questions

1. **Where does the set code come from?** `SWSH07` (`:64`) has no column, no derivation
   rule, and no pending state. It requires the set metadata table (`DECISIONS.md:199`).
   Does it come from the same curation pass as era/release date? What renders for the ~303
   sets before curation completes — the chip hidden, or a METADATA PENDING badge like
   Browse's Uncategorized shelf (`DESIGN_NOTES.md:71`)?
2. **Define the set index.** Constituents (all tracked cards? the most-traded subset?),
   weighting (equal? value-weighted?), tier, rebase epoch, and what happens to a card with
   no `price_months` row in a given month. 12 monthly points are affordable
   (`price_months` backfills to ~Dec 2020) but the method is a product decision, not a
   query. Same gap covers the 30D and 90D deltas.
3. **Define "RS pct" precisely.** "Relative strength vs market index, percentile" (`:214`) —
   percentile against what universe: this set, the whole corpus, the same era? Over what
   lookback?
4. **What is "most-traded" and how many rows survive it?** `:139` promises a filtered
   roster; the prototype ships an unfiltered short seed. Threshold, metric (`sales/mo`?),
   and whether the user can escape to the full 237 are all unspecified. Note that ranking
   by trade volume requires post-seam sales data, which is ragged per card
   (`DATA_MODEL.md:380`–`392`).
5. **Does `cards tracked` exclude retired rows?** `delisted_at` and `not_a_card_at`
   (`DATA_MODEL.md:169`, `:171`) both mean "the app skips this card everywhere." Whether
   237 counts them determines whether the header number and the roster denominator agree.
6. **Falling sets.** The 30D/90D readouts have no negative rendering (`:77`, `:78`). Adopt
   the row treatment (`pct()` + `PAL.neg2`)? Confirm.
7. **Should `Sales / mo` be sortable?** It is the one metric with a tooltip but no sort
   affordance, while `Card` gets a dead click handler. Both look like oversights rather
   than rulings.
8. **Column widths are ephemeral.** Should `colW` persist per user, like the theme does?
9. **Does the set page need a card-number column at all?** The prototype has none (§8).
   If collation numbering is wanted, it needs a per-card field that does not exist in
   `cards` (`DATA_MODEL.md:154`–`171`).
10. **Set art / accent derivation.** `thumbBg` gradients are seeded per card (`:157`–`:170`).
    `DECISIONS.md:201` flags dominant-colour extraction as conflicting with D-026 because it
    was specified as a new column on the scraper's `cards` table.
11. **Header accent bar** is a fixed three-stop gradient (`:54`); `DESIGN_NOTES.md:72` calls
    it "dominant-accent … (Umbreon dark blues)", implying it is derived per set. Derived how,
    and from what, for a set with no curated art?

---

## 8. Contradictions found

| Claim | Source | What the HTML actually does |
|---|---|---|
| Set header shows "era/release/count" | `DESIGN_NOTES.md:72` | **Only count.** The sub-line is `237 cards tracked · first sale observed Dec 2021` (`:66`). There is **no era and no release date anywhere on the Set page** — not in the header, not in the table, not in a chip. `firstSaleObserved` is a sales-ledger artifact, not a release date, and the two must not be conflated. |
| Set page shows a card-number denominator, e.g. `215/203` | Task brief (no doc carries it — grep across `CardStock Mockup/*.md` and `DECISIONS.md` returns zero hits for `215/203` or `/203`) | **No such display exists.** The only ratio-shaped readout is `shownCount` = `N of 237 cards` (`:206`), which counts *rows currently listed* against *cards tracked in the set*. There is no card-number column, no collector number, and no secret-rare-over-set-size denominator on this screen. |
| Set page shows a release year | Task brief; implied by `DESIGN_NOTES.md:72` | Absent (`:66`). A release **year** does exist — on the **Character** page, per printing (`Character:97`, `Character:118`). It is a card/set attribute surfaced there, not here. |
| Set page shows an era | Task brief; `DESIGN_NOTES.md:72` | Absent. Era exists as a Screener facet with 8 values (`DISPLAY_VOCABULARY.md:123`) and as Browse shelves (`DESIGN_NOTES.md:71`), but the Set page never renders it. |
| Per-sale ledger starts "late Jul 2026 onward — ragged, never a shared date" | `HANDOFF.md:118` | The Set header renders `first sale observed Dec 2021` (`:66`). **Not actually a conflict, but it reads as one** and will be misfiled as a bug: `HANDOFF` describes when *forward-complete* coverage begins, while a first visit also captures whatever the site's ~30-row bucket windows still held — "months or years" for thin cards (`DATA_MODEL.md:380`–`386`). `min(sold_on)` may legitimately be 2021. The banner copy's own `Jul 2026` (`:208`) matches `HANDOFF`. |
| Sorting by pop Δ excludes census-too-young cards "w/ amber count note" | `DESIGN_NOTES.md:72` | **Confirmed.** `hasExcluded` (`:207`) + amber banner (`:99`) + copy (`:208`). One of the few Tier-2 claims about this screen the HTML fully supports. |
| Set page sort pills are `value / ROC 3M / RS / pop Δ` | `DESIGN_NOTES.md:72` | **Confirmed** (`:202`). |
| Table has "app-standard sort arrows + resize pipes" | `DESIGN_NOTES.md:72` | **Confirmed** (`:219`, `:106`), with the caveat that 2 of 6 headers carry a live-looking but no-op click handler (`:220`). |
| `sets` table backs the Set page | implied by any spec treating the page as a straight read | `sets` has **six columns**: `id, slug, name, discovered_at, last_seen_at, last_walked_at` (`DATA_MODEL.md:139`–`146`). Of the header, only `name` is backed. The set code is unbacked; count and first-sale are derivable from `cards` / `sales`; the index, RS and pop-delta are computed and maturity-gated. |
| Prototypes have no props | `DESIGN_NOTES.md:141` ("the other five now have no props") | **Confirmed** — `data-props=""` (`:144`). |

### Fields on this screen requiring the two non-scraped tables (`DECISIONS.md:199`)

| Field | Table needed | Line |
|---|---|---|
| Set code `SWSH07` | **set metadata** (release date + era/series for ~303 sets) | `:64` |
| *(era, release date)* | set metadata | **not rendered on this screen** — needed by Browse shelves and the Screener Era facet, not here |
| — | **character tags** (card → Pokémon) | **not used on this screen at all**; the Set page is set-scoped and never joins to species |
