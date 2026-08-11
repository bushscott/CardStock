# Screen spec — Home

**Source of truth:** `CardStock Mockup/Cardstock Home.dc.html` (628 lines), read in full 2026-08-10.
All line citations below are into that file unless otherwise stated. Per `CLAUDE.md` §"Document authority",
the prototype is Tier 1; where the markdown docs disagree, the HTML wins — see §8.

**How the prototype works, in one paragraph.** The file is a Design Composer document: markup lives inside
`<x-dc>`, and a `<script type="text/x-dc">` at the bottom defines `class Component extends DCLogic` whose
`renderVals()` return object supplies every `{{ … }}` binding (lines 321–625). `sc-for list="{{ x }}" as="y"`
repeats its children over a list; `sc-if value="{{ x }}"` conditionally renders; `style-hover="…"` is a hover
style rule; `hint-placeholder-count` / `hint-placeholder-val` are design-time-only hints for the composer and
carry no runtime meaning. Seeded arrays (`this.cards`, `this.lists`, `this.feedData`, `tickerItems()`) are
illustrative sample data — the **structure** is the contract, not the values.

---

## 1. Identity

| | |
|---|---|
| **Screen name** | Home (`data-screen-label="Home"`, line 37) |
| **Route** | Application root. In the prototype: `Cardstock Home.dc.html`; the logo (line 41) and the active nav tab (line 45) both point here. **Unresolved:** `HANDOFF.md:71` says `/`, `CARDSTOCK_UI_SPEC_v1.md:111` says `/home` — the HTML cannot settle it (§8 row 24). `/` is the better default on tier grounds |
| **Nav position** | First of five primary tabs — Home · Screener · Charts · Binder · Browse (lines 45–49). Home renders as active: weight 600, `--ink` text, 2px `--acc` bottom border |
| **Purpose** | A market-open dashboard: a scrolling market-wide ticker, what changed on the user's saved screens since last visit, a portfolio snapshot, and the user's watchlists with per-card tracked-signal state — with a peek drawer for inspecting any card without leaving the page |

Home is a **read/monitor surface**. Every mutating action it offers (create list, move to list, remove from
watchlist, add to binder) either navigates away or is inert in the prototype (§5).

---

## 2. Layout

Root is a `min-height: 100vh` flex column at base `font-size: 15px` (line 37). Top to bottom:

| Region | Height / size | Scroll behaviour | Lines |
|---|---|---|---|
| **Top nav** | 48px | `position: sticky; top: 0; z-index: 20` — fixed to viewport top | 39–54 |
| **Market ticker bar** | 36px | Static in flow (scrolls away with the page). Its *content* animates horizontally | 56–81 |
| **`<main>`** | `flex: 1`, `max-width: 1480px`, centred, `padding: 16px 20px`, `gap: 16px` | Normal page scroll | 83–304 |
| **Footer** | auto | Normal page scroll, sits below content | 306–313 |
| **Peek drawer** | fixed overlay | `position: fixed`, own internal scroll | 230–302 |
| **Hover preview** | fixed overlay | `position: fixed`, `pointer-events: none` | 314–318 |

### Visual order vs DOM order — deliberate inversion

`<main>` is a flex column, and the two children carry explicit `order` values:

- **Watchlist `<section>`** — `order: 2`, but appears **first in the DOM** (line 85)
- **Two-column grid** (Screen activity + Binder) — `order: 1`, appears **second in the DOM** (line 147)

So the rendered top-to-bottom order is **Screen activity + Binder, then Watchlist**, while tab order and
screen-reader order visit the **Watchlist first**. A rebuild must preserve both — the reading order is not an
accident of markup, it is set against the visual order.

### The two-column band

`display: grid; grid-template-columns: minmax(0, 3fr) minmax(0, 2fr); gap: 16px; align-items: stretch`
(line 147) — Screen activity at 3fr (left), Binder at 2fr (right), equal height. The peek drawer's `sc-if`
also lives inside this grid element (line 230) but is `position: fixed`, so it does not participate in the grid.

**No responsive breakpoints exist.** There is not a single `@media` query for width anywhere in the file — the
only media query is `prefers-reduced-motion` (line 25). Below roughly 1000px the 3fr/2fr grid and the fixed
watchlist column widths will simply crush. See §7.

### Sticky layering

| Element | `z-index` | Note |
|---|---|---|
| Top nav | 20 | line 39 |
| Watchlist column-header row | 10, `position: sticky; top: 48px` | line 93 — parks directly under the nav; note it does **not** account for the 36px ticker bar, which scrolls away |
| Row actions menu | 40 | line 124 |
| Peek drawer | **10** (declared `50` then `10` in the same `style` attribute — last wins) | line 231, see §7 |
| Hover preview | 100 | line 315 |
| Peek header (internal) | 2, sticky within drawer | line 232 |

---

## 3. Data contract

Notation: **binding** = the `{{ … }}` name in markup; **source** = plausible origin in the
`../PokemonInvestBatch` Postgres schema or a CardStock-owned table. Sources are inferences unless marked.

### 3.1 Top nav (lines 39–54)

| Element | Label / content | What it is | Source |
|---|---|---|---|
| Logo mark + wordmark | `Cardstock` | Static brand. Inline SVG: two card rects + a teal check-line polyline and dot using `--logoTeal` | Static |
| Nav tabs | `Home`, `Screener`, `Charts`, `Binder`, `Browse` | Static primary nav, 5 items in this order | Static |
| Search | `<cardstock-search>` custom element (line 52) | Shared component defined in `CardStock Mockup/cardstock-search.js`. Not specified here — see `docs/screens/shared-components.md` | Card/set catalog |
| Account avatar | Letter `O`; `aria-label="Account"`, `title="Profile & settings"`; links to `Cardstock Profile.dc.html` | Circular 28px initial-avatar | User profile initial |

### 3.2 Market ticker bar (lines 56–81)

**Left cap** (line 57–64), fixed, does not scroll: the literal word `MARKET` (12.5px, letterspaced,
`--mut2`), then the window `<select>`.

**Window select** (lines 59–63): `aria-label="Stats window"`, bound `value="{{ win }}"`,
`onChange="{{ setWin }}"`. Options **in this DOM order**: `30d`, `7d`, `90d`. Default `win: '30d'` (line 331).
Transparent background, no border, JetBrains Mono 14px.

**Ticker items.** Rendered by `sc-for list="{{ ticks }}"` (line 68), from `tickerItems(win)` (lines 434–472).
Each item is a shape of **four optional-ish parts**, rendered inline in this order (line 69):

| Field | Role | Style | Notes |
|---|---|---|---|
| `l` | Stat label | `--mut` | Always present, always uppercase in the data |
| `n` | Subject name | `--ink` | Optional — only on the six "which card/set/character" stats |
| `v` | Value | `font-weight: 500`, colour = `c` | Always present |
| `x` | Suffix / qualifier | `--mut2` | Optional — window override or venue |
| `c` | Colour token | — | `G` = `PAL.pos2`, `L` = `PAL.neg2`, `K` = `PAL.ink` (line 435) |

**The complete stat list — 16 items, identical labels and identical order in all three windows:**

| # | `l` label | Has `n`? | `v` shape | `x` | Colour rule | What it is |
|---|---|---|---|---|---|---|
| 1 | `SALES` | no | count + ▲/▼ + pct (`9,644 ▲ +12%`) | — | pos/neg | Sale count in window, vs prior window |
| 2 | `VOLUME` | no | money + ▲/▼ + pct (`$498K ▲ +8%`) | — | pos/neg | Dollar volume in window, abbreviated K/M |
| 3 | `BREADTH` | no | `54% advancing` | — | pos/neg | Share of tracked cards up over the window |
| 4 | `INDEX` | no | `▲ +2.4%` | **`30d`** | pos/neg | Market index return. **Always labelled 30d** |
| 5 | `VINTAGE` | no | `62% of $ vol` | — | `K` (neutral) | Vintage share of dollar volume |
| 6 | `GRADING` | no | `+1,080 slabs · gem 45%` | — | `K` | New slabs in window + gem (PSA 10) rate |
| 7 | `MEDIAN SALE` | no | `$78 ▼ −2%` | — | pos/neg | Median sale price + change |
| 8 | `VENUE` | no | `ebay 84% · auction 9% · tcgp 7%` | — | `K` | Venue mix by share; 3 venues |
| 9 | `NEW 12M HIGHS` | no | `▲ 214` | **`30d`** | pos | Count of cards at a new 12-month high. **Always labelled 30d** |
| 10 | `MEDIAN ROC` | no | `▲ +0.3%` | — | pos/neg | Median rate of change across cards |
| 11 | `TOP WINNER` | **yes** | `▲ +18%` | — | pos | Best-performing card in window |
| 12 | `TOP LOSER` | **yes** | `▼ −9%` | — | neg | Worst-performing card in window |
| 13 | `TOP SALE` | **yes** | `$18,500` | **venue** (`goldin`, `pwcc`) | `K` | Highest single sale; `x` names the venue |
| 14 | `MOST ACTIVE` | **yes** | `41 sales` | — | `K` | Card with most sales in window |
| 15 | `HOT SET` | **yes** | `▲ +4.1%` | — | pos | Best-performing set |
| 16 | `CHARACTER LEADER` | **yes** | `▲ +3.2%` | — | pos | Best-performing character |

**The two 30d-pinned stats.** `INDEX` (#4) and `NEW 12M HIGHS` (#9) carry `x: '30d'` in **all three**
window datasets (lines 439/442, 450/453, 461/464), and their values are **byte-identical across windows** —
`▲ +2.4%` and `▲ 214` respectively. They are window-invariant by construction: the dropdown does not
recompute them, and the `30d` suffix exists to tell the user so. Everything else changes with the window.
`TOP SALE`'s dollar value is also identical in 7d and 30d (`$18,500`, `Lugia 1st Ed PSA 10`, `goldin`) and
only differs at 90d — but it carries no `30d` marker, so treat that as sample-data coincidence, not a rule.

**Plausible sources.** All 16 derive from the scraper's append-only `sales`, `price_months`, and
`populations` tables plus the `cards`/`sets` catalog. `GRADING` (slab counts, gem rate) needs
`populations` deltas; `VENUE` and `TOP SALE` need per-sale rows including a venue/source column; `INDEX`
and `BREADTH` need a CardStock-defined market index. **`sales` is append-only and change-only** — window
aggregates must respect `max(observed_at)` semantics (`CLAUDE.md` §"How it stores things").

### 3.3 Watchlist — header and tabs (lines 85–92)

| Element | Content | Notes |
|---|---|---|
| Title | `Watchlist` | `title` tooltip: *"Single cards you follow, each tracking the combination of signals you pinned for it. Chips show each signal's current state. Edit a row's tracked signals in Charts (⋯ → Open full chart)."* (line 87) |
| Tabs | `{{ tb.name }}` + `{{ tb.count }}` | One per watchlist. Count is `ids.length`, mono 12.5px `--mut2` (line 89) |
| Tab tooltip | active: `Showing "<name>" — N card(s)`; inactive: `Switch to "<name>" — N card(s)` | Singular/plural is handled (line 595) |
| New list | `+ new list` | `title`: *"Create another watchlist — rows can be moved between lists"* (line 91) |

Seed lists (line 412–415): `Main` (8 cards), `Grading candidates` (3 cards). **Two is sample data, not a
limit** — the tab strip is a plain `sc-for`.

### 3.4 Watchlist — columns (lines 93–102)

Grid template is data-driven: `gridCols` = `48px {card}px {tier}px {price}px {chg}px {spark}px minmax(0, 1fr) 18px`
(line 616), applied identically to the header row and every data row so they stay aligned.

| # | Header label | Default width | Resizable | Content | Format | Plausible source |
|---|---|---|---|---|---|---|
| 1 | *(blank)* | 48px | no | Card thumbnail | 48×66px, radius 4, gradient placeholder behind an `image-slot` | Card art asset (not in scraper schema — see §7) |
| 2 | `Card` | 220px | **yes** | Two lines: card name (600 weight) over set line | Set line format `<Set name> · <number>/<set size>`, e.g. `Evolving Skies · 215/203`. Both lines ellipsis-truncate | `cards.name`, `sets.name`, card number, set size |
| 3 | `Tier` | 52px | **yes** | Grade tier of the tracked row | Mono 12.5px, centred. Seeded values: `PSA 10`, `PSA 9`, `PSA 8`, `Raw` | Grade tier vocabulary (see §8 — mismatch) |
| 4 | `Price` | 76px | **yes** | Latest price for that tier | Mono 14.5px bold centred. `money()` = `'$' + n.toLocaleString('en-US')` — **whole dollars, no cents** (line 433). Taken from `series[11]`, the last of 12 monthly points (line 556) | `price_months` latest observation for (card, tier) |
| 5 | `1M %` | 52px | **yes** | One-month change | Mono 14px centred, coloured. `chg = series[11] / series[10] − 1` (line 478); text = sign + `abs(pct).toFixed(1)` + `%`. **Negative uses U+2212 MINUS `−`, not a hyphen** (line 479) | Two consecutive `price_months` rows |
| 6 | `12M` | 68px | **yes** | 12-month sparkline | SVG 100%×18, `viewBox 0 0 64 18`, `preserveAspectRatio="none"`, `aria-hidden="true"`. Filled polygon + 1.25px stroke polyline, stroke colour = the 1M % colour | 12 monthly `price_months` points |
| 7 | `Tracked signals` + `· set in Charts` | `minmax(0, 1fr)` | no | Signal chips | Wrapping flex, `overflow: hidden` | CardStock-owned watchlist-row → pinned-signal join |
| 8 | *(blank)* | 18px | no | `⋯` row-actions button | — | — |

Column 7's header has its own tooltip (line 100): *"The signals you pinned for this card in Charts — chips
show each one's current state. Edit via ⋯ → Open full chart."* The `· set in Charts` suffix is a separate
`--mut2` span inside the header.

**Sparkline geometry** (`spark()`, lines 473–476): min/max over the 12 points, `r = max − min || 1`;
x = `i / (n−1) × 63`; y = `16 − ((v − min) / r) × 14 + 1`, i.e. y ranges 3 (max) → 17 (min). The fill polygon
is `0,17 <points> 63,17` (line 569) — closed to the baseline. Fill colour is `posBg(0.12)` when 1M % ≥ 0,
`negBg(0.10)` otherwise (line 570) — note the two alphas differ.

### 3.5 Tracked-signal chips (lines 116–120, chip data on lines 355–411)

Each chip renders `{{ c.i }} {{ c.t }}` with `title="{{ c.tip }}"`. Mono 11.5px, weight 500, padding 1px 6px,
radius 4, `white-space: nowrap`.

| Chip field | Meaning |
|---|---|
| `i` | Leading glyph. Observed: `▲` `▼` `–` `◆` `◌` |
| `t` | Short label, e.g. `RS 94th`, `MACD +`, `RSI 71`, `Churn ×1.6`, `z +1.62`, `Pop Δ +2.4%`, `EMA 3/9 ▼`, `Quiet Accum`, `Arb EV +$62`, `Churn — 12d` |
| `s` | **State key**, one of `gain` / `loss` / `warn` / `muted` — drives colour via `this.CHIP` (lines 349–354) |
| `tip` | Full-sentence explanation shown as a native `title` tooltip |

`CHIP` colour map (lines 349–354): `gain` → fg `PAL.pos`, bg `posBg(0.10)`; `loss` → fg `PAL.neg`,
bg `negBg(0.10)`; `warn` → fg `PAL.warnInk`, bg `rgba(176,127,26,0.12)` (**hard-coded, not a palette token**);
`muted` → fg `PAL.mut2`, bg `PAL.mutbg`.

**Chip colour encodes the signal's current STATE, not its identity** — stated verbatim in the legend
tooltip (line 137): *"Chip color = the signal's current state, not its identity. Colored means it hit; grey
means nothing to report."*

**Signal families visible in the seed data**, each of which a rebuild must be able to render: relative
strength percentile (`RS 94th`), MACD (3,6,4) vs signal, RSI(6), churn ratio 30d vs 90d, z-score vs 6M moving
average, population delta over 60d, EMA 3/9 crossover, a composite named regime (`Quiet Accum`), grading
arbitrage expected value (`Arb EV`), and a not-yet-unlocked indicator countdown (`Churn — 12d`).

### 3.6 Chip legend and keyboard hint (lines 136–144)

Bottom strip of the watchlist card, 12.5px `--mut2`, space-between.

| Legend chip | Colour | Meaning |
|---|---|---|
| `▲ hit bullish` | `--pos` on `--posBg10` | Signal fired, bullish |
| `▼ hit bearish` | `--neg` on `--negBg10` | Signal fired, bearish |
| `– caution` | `--warnInk` on `rgba(176,127,26,0.12)` | Signal fired, cautionary |
| `– quiet · ◌ soon` | `--mut2` on `--mutbg` | **Two states share one grey chip**: `–` = tracked but nothing to report; `◌` = not yet unlocked |

Right-aligned hint (line 143): **`↑↓ rows · Enter peek · / search`**.

### 3.7 Row actions menu (lines 121–133)

Trigger: `⋯` button, `aria-label="Row actions"`, `title="More actions for this card"`.
Menu: `role="menu"`, absolute right-aligned, `min-width: 190px`.

| Item | Tooltip | Behaviour in prototype |
|---|---|---|
| `Open full chart` | *"Opens Charts with this row's tracked signals pinned — any pin changes save back to this row via Update watchlist."* | Navigates to `Cardstock Charts.dc.html` (line 589) |
| `Open card page` | *"Full reference page — every grade tier, the sales ledger, and census data"* | Navigates to `Cardstock Card.dc.html` (line 590) |
| `Add to binder` | *"Log a purchase of this card — opens the binder transaction form"* | **Inert** — only closes the menu (line 127) |
| *(divider)* | | 1px `--line`, 4px margin |
| `Move to list…` | *"Move this row to another watchlist — its tracked signals come with it"* | **Inert** — only closes the menu (line 129) |
| `Remove from watchlist` | *"Stop following this card — its tracked signals are forgotten"* | **Inert** — only closes the menu (line 130). Rendered destructive: `--neg2` text, hover bg `--negBg08` |

### 3.8 Screen activity feed (lines 149–167)

| Element | Content | Notes |
|---|---|---|
| Title | `Screen activity` | Tooltip: *"Cards that entered or exited one of your screens when the data refreshed. Manage screens in the Screener."* (line 151) |
| Header meta | `7 since your last visit · 1 unlock · your screens →` | **Fully hard-coded string** (line 152), not derived from the feed. The link goes to `Cardstock Screener.dc.html`. Shape: `<N> since your last visit · <M> unlock · <link>` |

Row shape (lines 155–165), rendered by `sc-for list="{{ feed }}"`:

| Field | Label | Format | Plausible source |
|---|---|---|---|
| `f.i` | Type glyph | Mono 12.5px in a fixed 12px column, coloured by `f.fg` | Derived from event kind + direction |
| `f.name` | Card name | 500 weight; resolved from the card catalog by `f.id` (line 605) | `cards.name` |
| `f.rule` | Event sentence | 14px, weight 600, coloured `f.fg` | Screen membership diff |
| `f.ev` | Evidence line | 13px `--mut` — the numeric justification | Indicator values at evaluation time |
| `f.t` | Relative time | Mono 12px `--mut2`, right-aligned. Seeded as pre-formatted strings (`2h ago`, `1d ago`, `3d ago`) — **not** timestamps | Event timestamp, humanised |
| `f.s` | State key | `gain` / `warn` / `loss` — maps through the same `CHIP` table for the glyph colour (line 607) | Derived |

**Row types present in the seed (lines 416–424) — three distinct sentence forms:**

1. **Entered a screen** — `Entered "<Screen name>"`, e.g. `Entered "Quiet Accumulation"`,
   `Entered "Alt-art momentum"`, `Entered "RSI overheat watch"`, `Entered "Supply Flood Watch"`,
   `Entered "Stretched above mean"`. Screen names are in **curly quotes** (U+201C/U+201D).
2. **Exited a screen** — `Exited "<Screen name>"`, e.g. `Exited "3M RS Leaders"`.
3. **Indicator unlocked** — `Indicator unlocked: <Indicator name>`, e.g. `Indicator unlocked: Churn 30d`,
   with evidence *"per-sale ledger reached 30 days for PSA 10 — starts LOW CONFIDENCE"*.

**`i` and `s` are independent axes.** The `Exited "3M RS Leaders"` row is `i: '▼'` with `s: 'warn'`
(line 421) — a down glyph in caution amber. Do not derive the glyph from the state key.
Glyphs observed: `▲` (entered, bullish), `▼` (entered bearish / exited), `–` (entered, caution),
`◆` (indicator unlocked).

The feed has **no header count binding, no "load more", no empty state, and no cap** — it renders whatever
`feed` contains (7 rows seeded).

### 3.9 Binder card (lines 169–228)

**Entirely static markup — there is not one `{{ }}` binding in this section.** Every number below is
hard-coded. A rebuild must invent the bindings; the labels and arrangement are the contract.

Header: title `Binder`, right link `Performance →` → `Cardstock Binder.dc.html#performance`.

**Tile row 1** (3 columns, line 174–189) — label 12.5px `--mut2`, value mono **25.5px** bold:

| Label | Value shape | Sub-line | Notes |
|---|---|---|---|
| `Total value` | `$18,432` | none | Whole dollars |
| `Unrealized` | `+$3,108`, coloured `--pos2` | `▲ +20.3%` mono 12.5px, same colour | Signed money |
| `vs market index` | `+8.7` + ` pp` in a 14px span | `trailing 12M` in `--mut2` | **Percentage points**, not percent |

**Tile row 2** (3 columns, top border `--line4`, lines 190–203) — value mono 15px weight 600:

| Label | Value shape | Notes |
|---|---|---|
| `Positions` | `14` + `across 6 sets` in weight 400 `--mut2` | Count + set-diversity qualifier |
| `Cost basis` | `$15,324` | Whole dollars |
| `1M change` | `▲ +$412`, coloured `--pos2` | Glyph + signed money |

**Superlatives row** (3 lines, top border, lines 204–217) — label `--mut2` left, value right:

| Label | Value shape |
|---|---|
| `Best position` | card name + mono coloured `▲ +42%` |
| `Worst position` | card name + mono coloured `▼ −8%` (U+2212) |
| `Largest holding` | card name + mono `31% of value` in `--mut` (**neutral colour — a concentration stat, not a performance stat**) |

**Chart** (lines 218–222): SVG `viewBox 0 0 300 48`, `preserveAspectRatio="none"`, `aria-hidden="true"`.
Three layers: a filled area `rgba(74, 99, 208, 0.07)` under the portfolio line; a **dashed** polyline
(`--mut2`, `stroke-dasharray="3 3"`, width 1) = market index; a **solid** polyline (`--acc`, width 1.5) =
portfolio. 13 x-positions (0…300 step 25) = 13 monthly points.

**Chart legend** (lines 223–227), mono 12px `--mut2`: `— portfolio` (accent dash) · `┄ market index`
(muted dash) · right-aligned `12M · normalized`. "Normalized" is the axis contract: both series are indexed
to a common base, so the chart has **no y-axis and no y labels**.

Plausible sources: CardStock-owned `holdings` / `transactions` tables joined to latest `price_months`,
plus the same market index the ticker uses.

### 3.10 Peek drawer (lines 230–302)

`aside role="dialog" aria-label="Card peek"`. Header bar (sticky within the drawer) then a 12px-padded body
of five stacked blocks.

**Header** (lines 232–238): `{{ peek.name }}` (600, 16px) then `{{ peek.set }}` (14px `--mut2`) then a
`✕` close button, `aria-label="Close peek"`, `title="Close the preview — the watchlist stays as it is"`.
The drawer's 3px top border is `{{ peek.accent }}` — the card's first accent colour (line 508), from the
hard-coded `ACCENTS` map (lines 426–431) which assigns each card a two-stop gradient pair.

**Block 1 — art + Current prices** (lines 240–253):

- Art: `image-slot` 178×246, `placeholder="card art"`.
- `Current prices` (uppercase 12.5px section label), then **six rows** from
  `TIER_LABELS = ['Raw','Grade 7','Grade 8','Grade 9','Grade 9.5','PSA 10']` (line 425) zipped with the
  card's six-element `tiers` array. Label left, `money()` price right, thin `--line4` separator.
- The row whose label **string-equals the card's `tier`** renders in `--ink` at weight 600; all others
  `--mut` at 400 (line 511). See §4 and §8 — for `PSA 9` / `PSA 8` cards nothing matches, so nothing
  highlights.
- The six labels correspond exactly to the scraper's `PriceTier` enum
  (`Ungraded, Grade7, Grade8, Grade9, Grade9Half, Psa10` — `CLAUDE.md` line 92), with `Ungraded` displayed
  as `Raw`.

**Block 2 — 12M chart** (lines 254–279):

| Element | Content | Notes |
|---|---|---|
| Section label | `12M · {{ peek.tier }} · tracked signals` | Uppercase 12.5px |
| Edit link | `edit →` → `Cardstock Charts.dc.html#signals` | |
| Gridlines | 3 horizontal, y = 12 / 61 / 110, x from 34 to 312 | `--line4` |
| Y-axis max label | `{{ peek.yMax }}` at y=15, right-anchored x=30 | `max(series).toLocaleString('en-US')` — **bare number, no `$`** (line 515) |
| Y-axis min label | `{{ peek.yMin }}` at y=113 | `min(series)`, same formatting |
| X-axis left label | **`Aug '25`** (hard-coded, line 265) | |
| X-axis right label | **`Jul '26`** (hard-coded, line 266) | |
| Price line | `{{ peek.line }}` polyline, `--acc`, width 1.5 | 12 points, plot box L=34 R=8 T=12 B=20 in a 320×130 viewBox (line 491) |
| Signal markers | `sc-for {{ peek.tris }}` — triangles with a nested `<title>` tooltip | Up events: triangle **below** the point in `--pos2`; down events: triangle **above** the point in `--neg2` (lines 499–501) |
| Current-month dot | `circle r=3` at the last point, white fill + `--acc` stroke, `<title>current month still revising</title>` | Line 271 — the only in-chart data-freshness affordance |

Marker tooltip copy is per-event free text and carries a **backtest-style outcome**, e.g.
*"MACD (3,6,4) crossed above signal — Dec 2025. Price then $1,240; +3M +2.0%, +6M +12.1%"* (line 359) and
*"EMA 3/9 crossover ▲ — Apr 2026. Price then $1,340; +3M +9.0% so far"*. The `so far` variant is used when
the +6M window has not completed.

**Block 2b — summary + chips** (lines 273–278). Summary is **generated**, not authored (line 505):

> `{tier} {up|down} {N}% over 3 months, {above|below} its 6-month average.`

where `N = |series[11]/series[8] − 1| × 100` rounded to 0 dp, and the average is the mean of
`series.slice(6)` — the **last six** points. Chips below are the identical chip list from the watchlist row.

**Block 3 — Last 5 sales** (lines 280–290): section label `Last 5 sales · {{ peek.tier }}`. Four-column grid
`92px 64px 1fr 70px`:

| Column | Field | Format | Source |
|---|---|---|---|
| Date | `s.date` | Mono, `Jul 28, 2026` (`MMM dd, yyyy`) | `sales.sold_at` |
| Grade | `s.grade` | Mono `--mut2`; always the card's own tier in the generator (line 486) | `sales` grade tier |
| Source | `s.src` | 12.5px `--mut2`, lowercase venue. Observed: `ebay`, `pwcc`, `goldin` | `sales` venue/source |
| Price | `s.price` | Mono, right-aligned, weight 500, `money()` | `sales` price |

The generator (lines 481–487) is pure sample data — fixed dates, fixed venue sequence, and price multipliers
`[0.996, 1.018, 0.972, 1.005, 0.958]` applied to the latest monthly price. **Structure only.**

**Block 4 — Population Δ** (lines 291–294): a `--mutbg` pill, label `Population Δ` left, `{{ peek.popD }}`
mono right, both `--mut`. Two distinct value shapes appear in the seed:
signed pct + window, e.g. `+1.8% (60d)`; and the no-baseline sentence
`first observed 2026-07-30 — deltas begin next observation` (line 393). Source: `populations` deltas.

**Block 5 — actions** (lines 295–299):

| Control | Type | Behaviour |
|---|---|---|
| `Open full chart →` | Primary filled `--btn` link, `flex: 1` | → `Cardstock Charts.dc.html` |
| `Card page` | Outline button | **Inert** (no handler). Tooltip: *"Full reference page — every grade tier, the sales ledger, and census data"* |
| `Edit signals` | Outline button | **Inert** (no handler). Tooltip: *"Open this card in Charts with its tracked signals pinned, ready to change"* |

### 3.11 Hover preview overlay (lines 314–318)

A 164×226 fixed card image, radius 8, heavy shadow, `pointer-events: none`, backed by the row's gradient
with an `image-slot` on top. No text, no labels — pure enlarged art.

### 3.12 Footer (lines 306–313)

| Element | Content | Notes |
|---|---|---|
| Links | `About our data` → `Cardstock About Data.dc.html`; `Privacy` → `Cardstock Legal.dc.html#privacy`; `Terms` → `Cardstock Legal.dc.html#terms` | 13px `--mut` |
| Corpus stat | `101,882 cards · 4.2M sales observed` | Mono 12px, hard-coded. Shape: `<count> cards · <count> sales observed`. Source: `count(cards)` and `count(sales)` |

### 3.13 Card record shape (`this.cards`, lines 355–411)

The seeded card object is the de-facto view model for a watchlist row + its peek:

| Key | Type | Used by |
|---|---|---|
| `id` | string | Row identity, `image-slot` id (`'art-' + id`), accent lookup |
| `name` | string | Row, peek header, feed row |
| `set` | string | Row sub-line, peek header — pre-composed `<set> · <num>/<total>` |
| `tier` | string | Tier column, peek highlight, peek section labels, sales grade column |
| `series` | number[12] | Price, 1M %, sparkline, peek chart, peek summary, sales generator |
| `tiers` | number[6] | Peek "Current prices" |
| `popD` | string | Peek "Population Δ" — **pre-formatted prose, not a number** |
| `tris` | `{i, d, tip}[]` | Peek chart markers. `i` = index into `series`; `d` = `'up'`/`'down'`; `tip` = free text |
| `chips` | `{t, s, i, tip}[]` | Row + peek chips |

---

## 4. States

### 4.1 Watchlist row

| State | Trigger | Visual |
|---|---|---|
| Default | — | Transparent background |
| Hover | Pointer over row | `background: var(--hov)` (line 104, `style-hover`) |
| Focused (keyboard) | `state.focusIdx === row index` | `background: PAL.accBg` (`#EEF1FB` light / `#252B44` dark) (line 571) |
| Being dragged | `state.dragIdx === row index` | `opacity: 0.35` (line 575) |
| Drop target | `state.overIdx === ix && dragIdx !== null && dragIdx !== ix` | 2px accent bar via `box-shadow: inset 0 2px 0 <acc>` on the **top** edge (line 576) |
| Menu open | `state.menuIdx === row index` | Actions menu rendered (line 586) |

Focus background and hover background are independent and can co-occur; hover wins visually because
`style-hover` is applied as a hover rule.

### 4.2 Chip states — the complete space

Only **four** state keys exist (`this.CHIP`, lines 349–354), but the **legend advertises five meanings**
because `muted` covers two:

| State key | Legend meaning | Glyph(s) seen | Colour | Semantics |
|---|---|---|---|---|
| `gain` | `▲ hit bullish` | `▲`, `◆` | `--pos` / `posBg(0.10)` | Signal fired bullish |
| `loss` | `▼ hit bearish` | `▼` | `--neg` / `negBg(0.10)` | Signal fired bearish |
| `warn` | `– caution` | `–` | `--warnInk` / `rgba(176,127,26,0.12)` | Signal fired, cautionary |
| `muted` | `– quiet` | `–` | `--mut2` / `--mutbg` | Tracked, between crossings, nothing to report (line 370) |
| `muted` | `◌ soon` | `◌` | `--mut2` / `--mutbg` | **Not yet unlocked** — countdown (line 395) |

**The "soon" / not-yet-unlocked state is the prototype's LOCKED equivalent.** It renders as a normal chip
with a countdown in the label (`Churn — 12d`) and the reason in the tooltip:
*"Unlocks in ~12 days — sales history for this grade begins 2026-06-12"*. There is **no lock icon, no
disabled styling, and no `LOCKED` string** anywhere in this file.

`gain` is used for the composite-regime chip `Quiet Accum` with a `◆` glyph (line 380) — so glyph is
per-chip data, not derived from state key.

### 4.3 Data-quality / confidence states present on Home

| Concept | How it surfaces | Line |
|---|---|---|
| **Not yet unlocked** ("soon") | Grey `◌` chip with a day countdown + tooltip naming the ledger start date | 395, legend 141 |
| **Just unlocked, LOW CONFIDENCE** | A screen-activity row: `Indicator unlocked: Churn 30d` / *"per-sale ledger reached 30 days for PSA 10 — starts LOW CONFIDENCE"*. State key `warn`, glyph `◆` | 423 |
| **No population baseline** | `Population Δ` reads `first observed 2026-07-30 — deltas begin next observation` instead of a percentage | 393 |
| **Current month still revising** | The last point of the peek chart is drawn as a hollow accent-stroked circle with `<title>current month still revising</title>` | 271 |
| **Quiet / nothing to report** | Grey `–` chip; tooltip *"between crossings — tracked, currently quiet"* | 370 |
| **Incomplete backtest window** | Marker tooltips read `+3M +9.0% so far` where the +6M window hasn't elapsed | 359 |

**Absent from this screen:** there is no `LOW DATA` string, no `LOCKED` string, no `UNDEFINED` state, and no
`UNSTABLE FIT` state anywhere in the file (grepped 2026-08-10). The nearest analogues are the five rows
above. `LOW CONFIDENCE` appears exactly once, as prose inside a feed row's evidence line — it is **not**
rendered as a badge on Home. See §7 and §8.

### 4.4 Loading / empty / error

**None of the three exist.** There is no spinner, no skeleton, no empty-state copy, no error copy, and no
`sc-if` guarding any list against being empty. `sc-for` over an empty list simply renders nothing, which
would produce: a watchlist with headers and legend but no rows; an empty Screen activity card with only its
header and its hard-coded `7 since your last visit · 1 unlock` line; and an empty ticker track that still
animates. Every one of these is an unhandled state a rebuild must design — see §7.

`hint-placeholder-count` / `hint-placeholder-val` attributes (lines 68, 88, 103, 117, 123, 154, 230, 246,
275, 282, 314) are **design-time authoring hints for the Design Composer canvas**, not loading states.

### 4.5 Peek drawer

| State | Trigger |
|---|---|
| Closed | `state.peekId === null` — the initial state (line 331) |
| Open | `peekId` set by: row click (line 573), feed-row click (line 608), `Enter` with a focused row (line 535), or arrow-key navigation **while already open** (line 530) |
| Closed by | `✕` button (line 615), `Escape` when no menu is open (line 523), switching watchlist tab (line 599), starting a row drag (line 577) |
| Follows selection | While open, `↑`/`↓` re-points it at the newly focused row (line 530) |
| Price-row highlight **missing** | Card tier is `PSA 9` or `PSA 8` — no `TIER_LABELS` entry matches, so **no** row is emphasised (line 511) |

There is **no backdrop/scrim and no click-outside-to-close** for the peek. It coexists with the page; the
page remains scrollable and interactive behind it.

### 4.6 Row actions menu

| State | Trigger |
|---|---|
| Closed | `state.menuIdx === null` (initial) |
| Open | `⋯` clicked (line 587) — toggles; clicking the same `⋯` again closes it |
| Closed by | Same `⋯` again, `Escape` (takes priority over closing the peek, line 523), a document click outside the menu (lines 539–544), `onMouseLeave` off the menu (line 124), any menu item (all five call `closeMenu` or navigate), opening a peek via row click (line 573), or starting a drag (line 577) |
| Mutually exclusive | Only one row's menu can be open — `menuIdx` is a single index |

### 4.7 Ticker

| State | Trigger | Effect |
|---|---|---|
| Scrolling | Default | `animation: ticker 45s linear infinite`, `translateX(0) → translateX(-50%)` (lines 24, 66) |
| Paused | Pointer over the track | `style-hover="animation-play-state: paused"` (line 66) |
| Window changed | `<select>` change | `setWin` replaces `state.win`; `ticks` recomputes (lines 619, 622). The CSS animation is **not** restarted — the belt keeps its phase |
| Reduced motion | `prefers-reduced-motion: reduce` | `* { animation-duration: 0.01ms !important }` (line 25) — the ticker effectively stops presenting readable motion; only the first ~half of the belt is ever legible in the viewport. See §7 |

The track is rendered **twice** — the second copy is `aria-hidden="true"` (line 72) — so the `-50%`
translate loops seamlessly. Screen readers see the stat list once. 24px linear-gradient fade masks sit over
the left and right edges, `pointer-events: none` (lines 78–79).

### 4.8 Theme / colour-vision states (global, read on Home)

| State | Trigger | Effect |
|---|---|---|
| Light (default) | No `localStorage` keys set | Default palette |
| Dark | `localStorage['cardstock-theme'] === 'dark'` | `data-theme="dark"` set on `<html>` **before paint** by an inline script (line 35); CSS custom properties swapped (line 29) and the JS `PAL` object mirrors them (lines 323–330) |
| CVD-safe | `localStorage['cardstock-cvd'] === '1'` | `data-cvd="1"`; positive/negative hues become blue/orange (lines 27, 31) |
| Dark + CVD | Both keys | A third distinct palette (line 326 / line 31) |

Home only **reads** these keys; it never writes them (they are set on Profile). Critically, the palette is
resolved **once at construction** into `this.PAL` (line 323) — the JS-computed colours (chip backgrounds,
sparkline strokes, focus background) will not react to a theme change without a remount.

### 4.9 Hover preview

| State | Trigger |
|---|---|
| Hidden | `state.pv === null` (initial) |
| Shown | `mouseenter` on a row's 48×66 thumbnail cell (line 105 → `pvIn`, lines 559–568) |
| Hidden again | `mouseleave` on that same cell (line 105 → `pvOut`, line 621) |

Position: x = thumbnail's `right + 10`; y = thumbnail centre − 113, then **clamped** to
`[max(8, headerBottom + 4), min(innerHeight − 234, tableBottom − 230)]` so the preview stays inside the
watchlist card and inside the viewport (lines 564–566).

---

## 5. Interactions

### 5.1 Market ticker

| Control | Action | Result | Guards |
|---|---|---|---|
| Window `<select>` | Change | `state.win` ← value; all 16 ticker items re-render from `tickerItems(win)` | `D[win] \|\| D['30d']` — unknown value falls back to 30d (line 471) |
| Ticker track | Hover | Animation pauses | Pointer only; no keyboard or touch equivalent |
| Ticker items | — | **Not clickable.** No links, no handlers, despite naming specific cards/sets/characters | — |

### 5.2 Watchlist tabs

| Control | Action | Result |
|---|---|---|
| Tab button | Click | `tab` ← index, **`focusIdx` ← −1, `peekId` ← null** (line 599). Rows, counts and tab styling all recompute |
| `+ new list` | Click | **Nothing** — no `onClick` binding (line 91) |

### 5.3 Watchlist rows

| Control | Action | Result | Guards |
|---|---|---|---|
| Row body | Click | Opens the peek for that card, sets `focusIdx` to it, **closes any open menu** (line 573) | — |
| Row | `role="button" tabindex="0"` | Reachable by Tab and focusable | **No `onKeyDown`** — pressing Enter on a DOM-focused row does nothing. The `Enter` shortcut works off the app's own `focusIdx`, which Tab does not update. See §7 |
| Thumbnail cell | `mouseenter` / `mouseleave` | Shows / hides the large hover preview | Position clamped to the watchlist bounds |
| Column grip `│` | `mousedown` + drag | Live-resizes that column; grid template updates on every `mousemove` for header and all rows together | Width clamped to **36–420px** (line 337). `preventDefault` + `stopPropagation` so the drag does not open a peek (line 334) |
| Row | Drag and drop | Reorders within the current list | See below |
| `⋯` | Click | Toggles that row's menu; `stopPropagation` prevents the row click from firing (line 587) | — |

**Drag-to-reorder** (lines 574–585):

1. `dragstart` — `effectAllowed = 'move'`, the source index is put on the dataTransfer as `text/plain`, and
   state is set to `{dragIdx: ix, menuIdx: null, peekId: null}` — **starting a drag closes both the menu and
   the peek**.
2. `dragover` — `preventDefault()`, `dropEffect = 'move'`, and `overIdx` is updated only when it actually
   changes (avoids a setState per mousemove).
3. `drop` — `preventDefault` + `stopPropagation`; if `from !== null && from !== ix`, the id is spliced out
   and re-inserted at the target index. **This mutates `this.lists[tab].ids` in place**, not a state copy.
   Then `{dragIdx: null, overIdx: null, focusIdx: -1}` — **the drop clears keyboard focus**.
4. `dragend` — clears `dragIdx`/`overIdx` (covers a cancelled drag).

Every row is `draggable` unconditionally (`drag: true`, line 574). Rows can be reordered but **not dragged
between tabs** — the drop handler only ever touches the active list. Cross-list movement is the (inert)
`Move to list…` menu item.

### 5.4 Row actions menu

| Control | Action | Result |
|---|---|---|
| `Open full chart` | Click | `stopPropagation`, then `location.href = 'Cardstock Charts.dc.html'` |
| `Open card page` | Click | `stopPropagation`, then `location.href = 'Cardstock Card.dc.html'` |
| `Add to binder` / `Move to list…` / `Remove from watchlist` | Click | Close the menu only — **no confirmation dialog, and no action** |
| Menu | `mouseleave` | Closes the menu (line 124) — note this fires even if the pointer merely passes through |
| Document | Click outside | Closes the menu, unless the click is inside `[role="menu"]` or on an element whose `aria-label` is `Row actions` (lines 539–544) |

The click-outside test uses `e.target.closest('[role="menu"]')` **and** an `aria-label` check on the target
itself. Because `⋯` is excluded by `aria-label`, its own toggle handler is what closes it — the two do not
fight. But the `aria-label` check only inspects `e.target`, not its ancestors, so a click on a child node
inside the `⋯` button would close and immediately re-open. (In practice the button has only a text glyph.)

### 5.5 Feed rows

| Control | Action | Result |
|---|---|---|
| Feed row | Click | Opens the peek for that card — **`focusIdx` is not set** (line 608), so the watchlist selection is untouched |
| Feed row | `role="button" tabindex="0"` | Focusable; **no key handler**, same gap as watchlist rows |
| `your screens →` | Click | → `Cardstock Screener.dc.html` |

A feed row can open a peek for a card that is **not in the active watchlist tab** — nothing checks. Arrowing
afterwards jumps the peek to `ids[focusIdx]` in the active list, which may be a different card entirely.

### 5.6 Peek drawer

| Control | Action | Result |
|---|---|---|
| `✕` | Click | `peekId` ← null |
| `edit →` | Click | → `Cardstock Charts.dc.html#signals` |
| `Open full chart →` | Click | → `Cardstock Charts.dc.html` |
| `Card page`, `Edit signals` | Click | **Nothing** — no handlers |
| Chart marker triangles | Hover | Native SVG `<title>` tooltip with the event description and backtest outcome |
| Current-month dot | Hover | Native `<title>`: `current month still revising` |
| Chips | Hover | Native `title` tooltip |
| Drawer body | Scroll | `overflow: auto` on the aside; the header is `position: sticky; top: 0` inside it |
| Outside the drawer | Click | **Nothing** — no click-outside-to-close, no scrim |

### 5.7 Keyboard (global `keydown` on `document`, lines 519–545)

| Key | Behaviour | Guards |
|---|---|---|
| Any key while focus is in an `input`/`textarea` | **Ignored**, except `Escape` which blurs the field (line 522) | Tag check is `input`/`textarea` only — a `select` or `contenteditable` is **not** excluded |
| `Escape` | If a row menu is open → close the menu. Otherwise → close the peek (line 523) | Strictly one level per press; never does both |
| `ArrowDown` | `preventDefault`; `focusIdx + 1`, clamped to `[0, len − 1]` of the **active tab's** ids | From the initial −1, one press lands on row 0 |
| `ArrowUp` | `preventDefault`; `focusIdx − 1`, clamped to `[0, len − 1]` | From the initial −1 this computes −2 and clamps to **0** — so `ArrowUp` with nothing selected also selects row 0 |
| `ArrowUp`/`ArrowDown` while the peek is open | Additionally re-points the peek at the newly focused row (line 530) | Only when `peekId` is truthy |
| `Enter` | Opens the peek for `ids[focusIdx]` | Only when `focusIdx >= 0`; does nothing otherwise (line 535) |
| `/` | Advertised by the hint `↑↓ rows · Enter peek · / search` (line 143) | **Not implemented in this file** — handled inside the shared `cardstock-search.js` component. Its documented contract: *"`/` from anywhere (unless focus is in an input), `Esc` clears and blurs, outside click closes. Fires at ≥2 characters"* (`DISPLAY_VOCABULARY.md:194`, `DESIGN_NOTES.md:123`). Note Home's own handler already blurs an input on `Escape` (line 522), which is consistent with that contract |

Arrow navigation **does not scroll the focused row into view** — a focused row below the fold stays there.

Listeners are added in `componentDidMount` and removed in `componentWillUnmount` (line 546).

### 5.8 Navigation map

| Control | Destination |
|---|---|
| Logo, `Home` tab | `Cardstock Home.dc.html` |
| Nav tabs | `Cardstock Screener.dc.html`, `Cardstock Charts.dc.html`, `Cardstock Binder.dc.html`, `Cardstock Browse.dc.html` |
| Avatar | `Cardstock Profile.dc.html` |
| `your screens →` | `Cardstock Screener.dc.html` |
| `Performance →` | `Cardstock Binder.dc.html#performance` |
| Peek `edit →` | `Cardstock Charts.dc.html#signals` |
| Peek `Open full chart →`, menu `Open full chart` | `Cardstock Charts.dc.html` |
| Menu `Open card page` | `Cardstock Card.dc.html` |
| Footer | `Cardstock About Data.dc.html`, `Cardstock Legal.dc.html#privacy`, `Cardstock Legal.dc.html#terms` |

None of the cross-screen links carry a card id or any query parameter — every "open this card in Charts"
path goes to the bare screen. Deep-linking is unresolved (§7).

---

## 6. Rules and invariants

A rebuild must preserve these. Each is enforced by the prototype's code, not merely implied.

### Layout and ordering

1. **Visual order is Screen activity + Binder, then Watchlist; DOM order is Watchlist first.** Set with
   `order: 2` / `order: 1` (lines 85, 147). Do not "fix" this by reordering the markup.
2. Watchlist rows render in the **stored list order** — no sort is applied anywhere. The user's drag order
   *is* the sort.
3. Feed rows render in the **stored feed order**, which in the seed is newest-first
   (`2h → 6h → 14h → 1d → 2d → 2d → 3d`, lines 417–423). No sort is applied in code — the API must return
   them ordered.
4. Ticker items render in **fixed dataset order** — the same 16 labels in the same sequence in every window
   (lines 437–469). Order is part of the design; do not sort by value.
5. The peek's Current-prices block renders **all six** `TIER_LABELS` in ascending-grade order, always, even
   where the card has no meaningful price at that tier.
6. The column header row and every data row **share one `gridCols` string** (line 616) — alignment is
   structural, not coincidental.

### Defaults

7. Ticker window default: **30d** (`state.win: '30d'`, line 331), and `30d` is also the **first** `<option>`
   (line 60) — the list is ordered `30d, 7d, 90d`, not chronologically.
8. Active tab default: index 0 (line 331).
9. `focusIdx` default: **−1** (nothing focused) — and it is reset to −1 on tab switch (line 599) and after a
   drop (line 583).
10. `peekId`, `menuIdx`, `pv` all default to `null` — Home opens with no overlay of any kind.
11. Default column widths: `card 220, tier 52, price 76, chg 52, spark 68` px (line 331).

### Constraints and guards

12. **Column width clamp: 36px minimum, 420px maximum** (line 337). Non-negotiable — below 36 the header
    grips overlap.
13. **`focusIdx` clamp: `[0, ids.length − 1]`** — arrow keys can never move focus off the list.
14. Only **one** row menu open at a time (`menuIdx` is a scalar).
15. Starting a drag force-closes both the menu and the peek (line 577).
16. `Escape` resolves **one** overlay per press, menu before peek (line 523).
17. Keystrokes are suppressed while typing in `input`/`textarea` (line 522).
18. `⋯` and menu-item clicks all `stopPropagation` so the row's own click handler never fires (lines 587–590).
19. Column-resize `mousedown` calls both `preventDefault` and `stopPropagation` (line 334), so a resize drag
    cannot open a peek or start a row drag.
20. During a resize drag, `document.body.style.cursor = 'col-resize'` and `userSelect = 'none'` are set and
    **must be reset on mouseup** (lines 340–342) — otherwise the whole page is left unselectable.
21. Unknown ticker window falls back to 30d (line 471).

### Formatting

22. Money is **whole dollars with thousands separators, no cents**: `'$' + n.toLocaleString('en-US')`
    (line 433). Applies to Price, peek tier prices, and peek sale prices.
23. Percentage change is **one decimal place**, sign-prefixed, and negatives use **U+2212 MINUS `−`**, never
    a hyphen (line 479). The same character is used in the static Binder copy (`▼ −8%`, line 211).
24. The peek summary percentage is **zero decimal places** (line 505) — deliberately coarser than the column.
25. Peek y-axis labels are **bare numbers with separators, no currency symbol** (line 515), while every other
    price on the screen carries `$`.
26. **Exact glyph inventory** (verified by codepoint scan of the whole file, 2026-08-10). These are specific
    characters, not lookalikes — a rebuild that substitutes a hyphen or an ASCII `x` is wrong:

    | Glyph | Codepoint | Count | Used for |
    |---|---|---|---|
    | `▲` | U+25B2 | 43 | Up / bullish |
    | `▼` | U+25BC | 13 | Down / bearish |
    | `–` | U+2013 EN DASH | 9 | Caution **and** quiet chip glyph |
    | `—` | U+2014 EM DASH | 31 | Prose dashes and the pending countdown `Churn — 12d` |
    | `−` | U+2212 MINUS SIGN | 20 | Negative numbers only |
    | `◌` | U+25CC | 2 | Pending / not yet unlocked |
    | `◆` | U+25C6 | 2 | Composite regime / unlock event |
    | `⋯` | U+22EF | 3 | Row-actions button |
    | `│` | U+2502 | 5 | Column resize grip |
    | `✕` | U+2715 | 1 | Peek close |
    | `┄` | U+2504 | 1 | Binder legend "market index" dash |
    | `↑` `↓` `→` | U+2191/2193/2192 | 1/1/7 | Keyboard hint, link affordances |
    | `“` `”` | U+201C/201D | 6/6 | Screen names in feed rows |
    | `’` | U+2019 | 2 | Peek x-axis year labels `Aug ’25` |
27. Screen names in feed rows are wrapped in **curly quotes** `“ ”` (lines 417–423).

### Persistence

28. **`localStorage['cardstock-theme']`** (`'dark'`) and **`localStorage['cardstock-cvd']`** (`'1'`) are read
    twice: by a blocking inline script in `<helmet>` **before first paint** to set `data-theme` / `data-cvd`
    on `<html>` (line 35), and again by the `PAL` initialiser (line 323). Home never writes them. The
    pre-paint read exists to prevent a light-theme flash — a Blazor rebuild must reproduce that timing.
29. **Nothing else persists.** Column widths, active tab, row order, focus, and open overlays are all
    in-memory component state. Reloading the page discards a user's drag reorder and any column resize.
30. Row reorder **mutates `this.lists[tab].ids` in place** (line 582) rather than replacing it — so the new
    order does survive tab switches within a session, but nothing writes it to a server. A rebuild must
    decide the persistence contract (§7).

### Accessibility affordances the prototype ships

31. `aria-label` on: the ticker window select (`Stats window`), the two `<section>`s (`Watchlist`,
    `Screen activity`, `Binder P&L`), the peek (`Card peek`), the `⋯` button (`Row actions`), the close
    button (`Close peek`), the logo link (`Cardstock home`), the avatar (`Account`).
32. `role="button" tabindex="0"` on watchlist rows and feed rows; `role="menu"` / `role="menuitem"` on the
    actions menu; `role="dialog"` on the peek.
33. `aria-hidden="true"` on the duplicated ticker track (line 72) so the stats are announced once, and on all
    decorative SVGs — sparklines (line 115), the binder chart (line 218), the logo (line 41).
34. `*:focus-visible { outline: 2px solid var(--acc); outline-offset: 1px; border-radius: 2px }` (line 21) —
    a single global focus ring; do not remove it per-control.
35. `@media (prefers-reduced-motion: reduce)` neutralises the ticker and the peek entrance animation
    (line 25).
36. Colour is never the sole carrier of direction — every coloured value is paired with a `▲`/`▼`/`–`/`◌`
    glyph, and the CVD palette exists as a second layer of defence.
37. Native `title` tooltips are used pervasively as the explanation layer (section titles, chips, legend,
    every menu item, column grips, both peek buttons). The tooltip *copy* is part of the spec — it is where
    most of the product's teaching happens.
38. The peek's 3px top border is the **only** thing tying the drawer to the row's card identity — it is
    decorative colour, so an accessible rebuild needs the name in the header to carry that job (it does).

---

## 7. Open questions

**Data and sourcing**

1. **Card art has no source.** Every image is an `image-slot` (a Design Composer placeholder) over a
   two-stop gradient from a hard-coded per-card `ACCENTS` map (lines 426–431). The scraper's eight tables
   (`CLAUDE.md` line 55) include no image table. `CARDSTOCK_UI_SPEC_v1.md:160` (Tier 3) says the peek image
   comes *"from `cards.image_hash` local store"* — **unverified against `../PokemonInvestBatch`; check
   `DATA_MODEL.md` before relying on it.** Two conventions are worth preserving regardless: slot ids are
   `art-<cardid>` and are **shared across the watchlist row, the hover preview and the peek** (lines 507,
   558; `DESIGN_NOTES.md:29` adds Charts to that list), and the gradient is a per-card two-stop fallback.
   Open: where does art come from, and what is the gradient keyed on — card id hash, set, or character?
2. **The market index is undefined.** `INDEX` in the ticker, `vs market index` on the Binder card, and the
   dashed line on the Binder chart all reference "the market index". Nothing says what it contains, how it
   is weighted, or what its base period is.
3. **Why are `INDEX` and `NEW 12M HIGHS` pinned to 30d?** The `30d` suffix tells the user, but not why —
   is it a computation cost, a stability floor, or simply not yet built for other windows?
4. **`BREADTH`, `VINTAGE`, `GRADING` gem rate, and `VENUE` all need definitions.** What counts as vintage?
   Is gem rate PSA 10 only, or PSA 10 + equivalents from the 19-value grade vocabulary? Is the venue mix by
   count or by dollars?
5. **Relative time strings are pre-baked** (`2h ago`). Server-rendered or client-computed? Timezone?
6. **`popD` is pre-formatted prose**, including the no-baseline sentence. Should the API return a formatted
   string, or a structured `{delta, window, firstObservedAt}` the UI formats?
7. **`7 since your last visit · 1 unlock`** implies a per-user "last visit" timestamp and a rule for what
   resets it. Neither is defined, and the string is hard-coded (line 152) rather than derived from the feed.
8. **Footer corpus stats** (`101,882 cards · 4.2M sales observed`) — live counts, cached, or as-of a
   refresh? The `4.2M` abbreviation implies a rounding rule that is not stated.

**States the prototype never draws**

9. **No loading state** of any kind. Skeleton rows? Shimmer? Nothing exists to copy.
10. **No empty states.** A user with zero watchlists, an empty list, no screens, no screen activity, or an
    empty binder all render as bare headers. All four need copy.
11. **No error state.** Nothing covers a failed fetch, a stale data warning, or a partial failure (e.g.
    ticker loads, watchlist doesn't).
12. **`LOW DATA`, `LOCKED`, `UNDEFINED`, `UNSTABLE FIT` do not appear on Home.** The nearest analogues are
    the `◌ soon` chip, the "first observed" population sentence, and the `LOW CONFIDENCE` prose inside one
    feed row. Do these vocabulary terms apply to Home at all, and if so where — as chip states, as a badge
    on the price, or as row-level treatment? See §8.
13. **No overflow rule for chips.** The chips container is `flex-wrap: wrap` inside `overflow: hidden` with
    a `minmax(0, 1fr)` column (line 116). With many chips or a narrowed layout, chips are silently clipped —
    no "+2 more" affordance. What is the intended cap?
14. **No pagination or cap** on the watchlist or the feed.

**Behaviour that is implied but not built**

15. **`+ new list` is inert.** What is the creation flow — inline rename, modal, navigation? Is there a limit
    on list count or a name-uniqueness rule?
16. **`Move to list…`, `Remove from watchlist`, `Add to binder` are all inert.** Remove is styled destructive
    but has no confirmation — is one required? Is there undo?
17. **Reorder is not persisted and not saved to a server** (line 582 mutates an in-memory array). Is order a
    per-user server-side property of the watchlist, and does drag fire a save immediately or on drop-settle?
18. **Column widths are not persisted.** They reset on reload. Should they be per-user, per-device, or
    session-only?
19. **No deep links carry a card id.** "Open full chart" navigates to bare `Charts`. The menu tooltip
    promises Charts opens *"with this row's tracked signals pinned"* and that *"any pin changes save back to
    this row via Update watchlist"* — a real round-trip contract that is entirely unimplemented here.
    What is the URL shape, and what is the "Update watchlist" handshake?
20. **`/` to focus search** is advertised in the hint (line 143) but not implemented in this file. Confirm
    it lives in `cardstock-search.js`.
21. **Rows are `role="button" tabindex="0"` with no key handler.** DOM focus (Tab) and the app's `focusIdx`
    are two separate, unsynchronised notions of "current row". Tabbing to a row and pressing Enter does
    nothing; the arrow keys move a highlight that Tab cannot see. This needs one model, plus
    `aria-activedescendant` or roving tabindex, and `scrollIntoView` on arrow navigation.
22. **`Escape` when a peek is open but focus is inside the peek** still hits the global handler — but focus
    is never trapped in the dialog, and it is never restored to the originating row on close. A
    `role="dialog"` needs both.
23. **Feed-row peek and watchlist arrow navigation are inconsistent** — a feed click can peek a card outside
    the active list, and the next arrow press silently jumps to a watchlist card. Intended?
24. **No responsive design exists.** Zero width media queries. The 3fr/2fr grid, the 480px fixed drawer, and
    the fixed-pixel watchlist columns have no defined behaviour below ~1000px, and no mobile design.
    Column resize and drag-to-reorder have no touch equivalents.

**Prototype defects a rebuild should fix rather than copy**

25. **The peek drawer's `z-index` is declared twice in one `style` attribute — `50` then `10`** (line 231).
    The later wins, so the drawer sits at 10, *below* the row-actions menu (40) and the hover preview (100).
    Almost certainly a bug; 50 looks like the intent.
26. **The peek tier highlight is broken for `PSA 9` and `PSA 8` cards.** The match is a raw string equality
    against `TIER_LABELS` (`'Grade 9'`, `'Grade 8'`), so a card whose `tier` is `'PSA 9'` highlights nothing
    (line 511). Either the row tier vocabulary or the peek label vocabulary is wrong — see §8.
27. **Peek chart x-axis labels are hard-coded** `Aug '25` / `Jul '26` (lines 265–266) while the y-axis
    labels are computed. They will be wrong the moment the window moves.
28. **The `prefers-reduced-motion` rule sets `animation-duration: 0.01ms` on an infinite ticker** (line 25),
    which does not degrade to "static and readable" — it degrades to "the belt is effectively frozen at an
    arbitrary offset". Reduced motion needs a real alternative presentation (e.g. a static, wrapped stat
    list).
29. **The sticky column-header offset is `top: 48px`** (line 93) — the nav height only. The 36px ticker bar
    is in normal flow, so during the scroll transition the header and the ticker overlap.
30. **The `title` tooltip is doing far too much work.** Native tooltips do not appear on touch, cannot be
    keyboard-triggered reliably, and cannot be styled — yet they carry the entire explanation layer,
    including multi-sentence signal definitions. This needs a real tooltip component.
31. **Positive and negative sparkline fills use different alphas** — `posBg(0.12)` vs `negBg(0.10)`
    (line 570). Probably an optical adjustment; confirm before normalising.
32. **`this.PAL` is resolved once at construction** (line 323), so JS-driven colours will not follow a live
    theme change.

---

## 8. Contradictions found

Every doc line below was opened and read directly on 2026-08-10, not taken second-hand. Per `CLAUDE.md`
§"Document authority", the HTML column is the answer in every row; the doc line is recorded as stale, not
averaged. `DN` = `CardStock Mockup/DESIGN_NOTES.md`, `DV` = `CardStock Mockup/DISPLAY_VOCABULARY.md`,
`HO` = `CardStock Mockup/HANDOFF.md`, `SP` = `CardStock Mockup/uploads/CARDSTOCK_UI_SPEC_v1.md` (Tier 3).

### Substantive contradictions

| # | Claim | Source doc:line | What the HTML actually does |
|---|---|---|---|
| 1 | *"the five states above are the complete render set"* — **OK · LOW DATA · LOCKED · UNDEFINED window · UNSTABLE FIT**, "every metric on every surface" | DV:55 | **Home renders none of the five.** Grepped for all five names: zero hits. The only sufficiency surfaces on Home are the grey `◌ Churn — 12d` chip (line 395), the "first observed … deltas begin next observation" population string (line 393), and the phrase `LOW CONFIDENCE` inside one feed row's evidence text (line 423). No `N OBS` badge, no disabled control, no gap rendering, no fit badge |
| 2 | `LOW CONFIDENCE` is a **Charts-only** badge for burned-in / show-anyway rows, distinct from the `LOW DATA` sufficiency state | DN:33, DN:131, DN:146 | Home's feed uses `LOW CONFIDENCE` as the name of the state a **newly unlocked indicator starts in** — *"per-sale ledger reached 30 days for PSA 10 — starts LOW CONFIDENCE"* (line 423). Either the term has a second meaning the docs never define, or the feed copy should say `LOW DATA`. **Unresolved — needs an owner ruling** |
| 3 | Sparkline markers on the **Home watchlist**: *"▲ green **above** spark … ▼ red **below** … hollow ◌ (current month provisional) · amber tick (sufficiency event). Complete."* | DV:73 (section header DV:72 names "Home watchlist, peek panel") | **The Home watchlist sparkline has no markers at all** — it is a bare polygon + polyline, `aria-hidden="true"` (line 115). Markers exist only in the **peek** chart, and their placement is **inverted**: up events draw a green triangle **below** the line, down events a red triangle **above** it (lines 499–501). There is no amber tick. The provisional current month is a hollow **circle stroked in `--acc`** (line 271), not a `◌` glyph |
| 4 | Grade tier vocabulary is the **canonical 19-value scale** — `Raw · Grade 1–Grade 9 · Grade 9.5 · PSA 10 · CGC 10 · …`; below 10 the buckets are **grader-agnostic** | DV:64, DN:77, HO:106 | Home uses **three incompatible vocabularies at once.** The peek "Current prices" block uses the **legacy six** (`Raw, Grade 7, Grade 8, Grade 9, Grade 9.5, PSA 10`, line 425). The watchlist Tier column uses **`PSA 9` and `PSA 8`** (lines 371, 381) — grader-*specific* labels below 10, which the canonical scale forbids. `DN:77`'s "Applied to" list does not mention the Home watchlist at all |
| 5 | *(consequence of #4)* — implied: the peek highlights the row matching the card's tier | — | The match is raw string equality against the six labels (line 511). For the two `PSA 9` / `PSA 8` cards **nothing highlights**, because `'PSA 9' !== 'Grade 9'`. A live defect caused directly by the vocabulary split |
| 6 | Ticker dropdown *"changes all stats"* | DN:28 | Two of the sixteen do not change. `INDEX` (`▲ +2.4%`) and `NEW 12M HIGHS` (`▲ 214`) are **byte-identical in all three window datasets** (lines 439/442, 450/453, 461/464). The same doc line says they are "always 30d-labeled", so DN:28 contradicts itself; the HTML resolves it — they are window-invariant, and the `30d` suffix says so |
| 7 | ⋯ menu = *"open chart, open card, move/remove"* (SP), narrowed to "Open full chart" + removal (DN) | SP:157, DN:113 | **Five items**, in this order: `Open full chart`, `Open card page`, `Add to binder`, divider, `Move to list…`, `Remove from watchlist` (lines 125–130). **`Add to binder` appears in no document.** DN:113 is correct that "Edit tracked signals" is gone; it is wrong that the menu is down to two items |
| 8 | Peek panel is *"focus-trapped"*, *"Esc … focus returns to the origin row"*, with *"roving tabindex in grids"* | SP:287, SP:359 | **No focus trap, no focus restore, no roving tabindex.** The peek is a `role="dialog"` with a global `document` keydown handler; `Escape` clears `peekId` (line 523) and nothing touches `document.activeElement`. Watchlist rows are plain `tabindex="0"` — every row is a tab stop |
| 9 | Peek panel width = *"right column width"* / *"overlay, right column width"* | SP:160, SP:287 | A **fixed 480px** panel anchored to the **viewport**, not the column: `position: fixed; top: 96px; right: 20px; bottom: 16px; width: 480px; max-width: calc(100vw − 40px)` (line 231). At the 1480px max content width it does not align with the 2fr column |
| 10 | *"Esc closes and restores the signals feed beneath"* | SP:84 | The peek is anchored to the viewport's right edge and overlays the **Binder** card, not the feed (which is the 3fr left column). Nothing is "restored"; the panel simply unmounts |
| 11 | Global keyboard map includes `o` (open full page from peek), `t` (toggle Terminal/Binder), `?` (show map) | SP:129 | **None of the three exist.** The handler (lines 519–537) implements exactly four keys: `Escape`, `ArrowUp`, `ArrowDown`, `Enter`. `/` is advertised in the on-screen hint (line 143) but is not in this file |
| 12 | *"Every interactive control on every app page carries [a `title` tooltip]"* — ~110 controls | HO:153, DN:152 | Contradicted on Home by at least: the ticker window `<select>` (line 59 — has `aria-label`, **no `title`**), the four inactive nav links (lines 46–49), the peek's primary `Open full chart →` link (line 296), the `Performance →` link (line 172), the `edit →` link (line 257), every feed row (line 155), and every footer link (lines 308–310). The rule holds for the watchlist and the ⋯ menu; it is not universal |
| 13 | Binder card = *"stats row (Positions/Cost basis/1M change) + Best/Worst position/Largest holding + portfolio-vs-index sparkline"* | DN:30 | Correct as far as it goes, but **omits an entire first row**: three large tiles `Total value` / `Unrealized` / `vs market index` at 25.5px (lines 174–189). DN:30 describes rows 2–4 only |
| 14 | Binder card = *"total value · unrealized ± · 'vs index ±' **one-liner**"* | SP:159 | Not a one-liner — a four-block card: 3 large tiles, 3 small tiles, 3 superlative lines, and a dual-series 12M chart with legend (lines 169–228). SP:159 also omits Positions, Cost basis, 1M change, Best/Worst/Largest, and the chart |
| 15 | Footers say *"refreshed just now"*; `AsOfStamp` removed app-wide | HO:99, DN:27 | Home's footer says **neither**. It carries `About our data · Privacy · Terms` and a corpus stat `101,882 cards · 4.2M sales observed` (line 312). No freshness stamp of any kind exists on Home |
| 16 | The *"{cards} cards · {sales} sales observed · updated {x}h ago"* honesty line belongs to the **Landing/marketing** page | SP:421, SP:140 | Home's footer carries that line (minus the `updated {x}h ago` clause) — line 312. No doc assigns it to Home |
| 17 | Feed *"'All signals →' → Alert Center history"* | SP:158 | The header meta link is **`your screens →` → `Cardstock Screener.dc.html`** (line 152), matching DN:92 and the v2 deferral of Alert Center at DN:120. SP:158 is stale, as `HANDOFF.md` §4 predicts |
| 18 | Feed rows include *"per-card tracked-signal threshold crossings"* | SP:158 | Removed. All seven seeded rows are screen entries/exits or the unlock product event (lines 416–424), matching the screens-only ruling at DN:80/DN:91–93 |
| 19 | Feed row types (complete): ENTER is *"▲ green if screen thesis bullish, ▼ red for avoid-list screens"*; EXIT is *"amber"* | DV:58 | The two-way ENTER taxonomy does not cover the data. `Entered "RSI overheat watch"` is **amber `–`** (`s:'warn', i:'–'`, line 419) — neither green ▲ nor red ▼. Confirmed correct: EXIT is amber (line 421), UNLOCK is `◆` amber (line 423). The HTML treats glyph and colour as **two independent per-row fields**, so a rebuild must store both, not derive one from the other |
| 20 | *"A tracked signal ALWAYS renders exactly one pill, in exactly one of **five** states"* (Hit bullish / Hit bearish / Caution / Quiet / **Pending**) | DV:40, DV:47 | The code defines **four** state keys — `gain`, `loss`, `warn`, `muted` (lines 349–354). Quiet and Pending share the `muted` key and are distinguished **only by the per-chip glyph** in the data (`–` vs `◌`, lines 370 vs 395). The rendered result matches the doc; the state model does not. A rebuild that types this as a five-value enum will not round-trip the prototype's data |
| 21 | *"**One row per card + tier** on watchlists"* | HO:155, DN:110 | The prototype's row key is the **card id alone** — `lists[].ids` holds bare ids (lines 412–415) and the tier is read off the card record (line 556). The (card, tier) pair is **not representable**: the same card cannot appear twice at two tiers. The rule is satisfied only because the model cannot express a violation |
| 22 | Watchlist table is *"virtualized"* | SP:157 | A plain `sc-for` over the full list (line 103). No virtualization, no windowing, no pagination, no row cap |
| 23 | Responsive breakpoints: *"≥1280 full · 1024–1279 peek becomes full-height drawer · 768–1023 Home stacks (index strip → signals → watchlist → binder) · <768 read-mostly"* | SP:354 | **Zero width media queries exist in the file.** The only `@media` is `prefers-reduced-motion` (line 25). The 3fr/2fr grid, the 480px drawer and the fixed-pixel columns have no defined sub-desktop behaviour |
| 24 | Route is `/` (HO) vs `/home` (SP) | HO:71 vs SP:111 | The HTML cannot settle this — the prototype is a static file (`Cardstock Home.dc.html`) and every self-link uses the filename (lines 41, 45). **Unresolved.** HO:71 is Tier 2 and SP:111 is Tier 3, so `/` is the better default, but this is a decision, not a finding |

### Colour-token drift (verified, low severity but real)

| # | Claim | Source doc:line | What the HTML actually does |
|---|---|---|---|
| 25 | Warn gold text was migrated `#B07F1A → #8F6614` in the contrast pass | DN:136–137 | Only the **text** migrated. The caution chip's **background is still hard-coded `rgba(176,127,26,0.12)`** = the old `#B07F1A` — in `this.CHIP.warn` (line 352) and again in the legend (line 140). It is the only chip background not drawn from a palette token, and it does not change in dark or CVD mode |
| 26 | *"grey #5B5B57 neutral/informational"* | DV:2 | The quiet/pending chip uses `PAL.mut2` = **`#6B6B66`** light / `#A8A8A2` dark (lines 328–329, 353), not `#5B5B57`. `#5B5B57` is `--mut`, used for body-secondary text. DV:2 names the wrong token for chips |
| 27 | *"accent #3B5BD6 (hover #2E49B8)"* | DN:26 | Superseded by DN:131 (`#4A63D0` / `#3A4FB8`) — and the HTML uses the **new** values throughout (lines 19–21, 329). DN:26 is stale but DN:131 already records the fix; no action beyond not citing DN:26 |

### Doc claims the HTML confirms (recorded so they are not re-litigated)

| Claim | Source | HTML |
|---|---|---|
| Visual order = signals+binder row first, then watchlist | DN:5, DN:25 | Confirmed — `order: 1` / `order: 2` (lines 147, 85) |
| Ticker: `MARKET` label + 7d/30d/90d dropdown; `INDEX` + `NEW 12M HIGHS` always 30d-labeled | DN:28 | Confirmed (lines 58–63; `x: '30d'` at 439/442, 450/453, 461/464) |
| 48×66 image slots, id `art-<cardid>`, shared with peek; hover scale 3.4× | DN:29 | Confirmed — slot ids `'art-' + id` (lines 507, 558); hover preview 164×226 vs 48×66 = **3.42×** (lines 105, 315) |
| Drag-to-reorder; resizable columns via header pipes `│`; ⋯ menu closes on outside click **and** mouse-leave | DN:29 | Confirmed (lines 577–585, 95–99, 124, 539–544) |
| Feed renamed "Screen activity"; header tooltip copy; `your screens →` link to Screener | DN:92 | Confirmed verbatim (lines 151–152) |
| Feed includes an unlock product event `Indicator unlocked: Churn 30d`, `◆` | DN:36 | Confirmed (line 423) |
| No `"view all"` history on the feed; no nav bell; no DEMO chip; no peek "sign in with an invite" notice | DN:120, DN:141 | Confirmed absent |
| Pending ETA format: days when under 60 (`— 12d`); tooltip = floor rule + date history began | DV:47, DV:51 | Confirmed — `Churn — 12d` (em dash) with tooltip *"Unlocks in ~12 days — sales history for this grade begins 2026-06-12"* (line 395) |
| Chip glance rule: coloured = hit, grey = nothing to report | DV:8 | Confirmed verbatim in the legend tooltip (line 137) |
| `peekIn` must animate **transform only**, never opacity | DN:37 | Confirmed — `@keyframes peekIn` moves `translateX` only (line 23) |
| "Worst position", not "Laggard" | DN:30 | Confirmed (line 210) |
| Nav: 48px, logo→Home, five links, search component, account circle→Profile; pre-paint `localStorage` script | HO:88 | Confirmed (lines 35, 39–54) |
| Numbers are JetBrains Mono everywhere, including inside prose | HO:151 | Confirmed throughout |
| Colour never alone — every state pairs a hue with a glyph; CVD swaps hue only | HO:150 | Confirmed (lines 27, 31, 324–327; every coloured value carries `▲▼–◌◆`) |
| Theme + colourblind mode persist per device | HO:156 | Confirmed for both keys (line 35). **Density does not exist on Home** — no third key is read and no density control is present |
| Footer links to About our data / Privacy#privacy / Terms#terms | DN:127, DN:148 | Confirmed (lines 308–310) |

### One rule that applies to the rebuild, not to the prototype

`HANDOFF.md:30` bans hand-written progress ratios and unlock dates: *"author the **denominator** … Numerators
are computed against the floor and today's date. No hand-written progress ratios or unlock dates."* The
prototype hard-codes exactly that — `Churn — 12d` and `sales history for this grade begins 2026-06-12`
(line 395) — because it is seeded sample data. **A rebuild must compute the countdown**, not store it. Note
also that `HANDOFF.md:26` says `DISPLAY_VOCABULARY.md`'s locked-row progress ratios *"overstate readiness by
roughly 15 months"* (D-032), so any sufficiency copy inherited from that file needs re-derivation before use.

