# Screen spec — Card

**Authority.** Everything below is extracted from `CardStock Mockup/Cardstock Card.dc.html` (483 lines), read directly 2026-08-10. Bare `:NNN` citations refer to that file. Where a Tier-2 document disagrees, the HTML wins and the disagreement is recorded in §8. Runtime semantics verified against `CardStock Mockup/support.js`.

**Reading the prototype.** `x-dc` is a React-backed template runtime. `{{ expr }}` binds to a key returned by `Component.renderVals()` (`:335–479`). `<sc-for list as>` repeats **all** its children per item (`support.js:610–643`). `<sc-if value>` renders children when truthy (`support.js:644–658`). `hint-placeholder-count` / `hint-placeholder-val` are **authoring-time streaming hints only** — they have no runtime meaning and several are wrong (see §6.9). `style-hover` is a hover style. Seeded data is illustrative; structure and state space are normative.

---

## 1. Identity

| | |
|---|---|
| **Name** | Card |
| **Prototype** | `CardStock Mockup/Cardstock Card.dc.html`, `data-screen-label="Card"` (`:35`) |
| **Route** | `/card/{id}` — from `HANDOFF.md:76`. The prototype is a static file with no routing; every inbound link is a bare `Cardstock Card.dc.html` with no id (`Cardstock Set.dc.html:111,125`, `Cardstock Character.dc.html:92,116`, `Cardstock Binder.dc.html:97,117`, `Cardstock Charts.dc.html:76`, `Cardstock Home.dc.html:590`). The `{id}` shape is a Tier-2 claim, not verified in Tier 1. |
| **Purpose** | The single-card terminal: what it is, what each grade tier is worth now, how those prices moved over 12 months, every individual sale we observed, and how many slabs exist. It is the destination of every card link in the product and the only page that triggers an on-demand scrape. |
| **Chrome** | Shared 48px sticky nav (`:37–52`): logo + wordmark → Home, five section links (Home / Screener / Charts / Binder / Browse), flexible spacer, `<cardstock-search>`, 28px avatar circle "O" → Profile. **No nav item is marked active on this page** — all five links render `color: var(--mut)` with a transparent bottom border. Card is not a top-level section. |
| **Page shell** | `min-height: 100vh`, flex column, base `font-size: 15px` (`:35`). `<main>`: `max-width: 1480px`, `margin: 0 auto`, `padding: 14px 20px 28px`, flex column, `gap: 16px` (`:54`). |

---

## 2. Layout

Top to bottom inside `<main>`, six blocks separated by a 16px gap.

### 2.1 Breadcrumb (`:56`)
`13.5px`, `--mut2`. Three segments: `Browse` (link → Browse) › `{setName}` (link → Set) › `{cardName}` (plain, `--ink`). Separator is `›` (U+203A) with spaces.

### 2.2 Hero / identity section (`:58–99`)
Card surface, `1px --line`, `radius 10`, `padding 16`, `display: flex`, `gap: 18`.

**Art column (`:59–61`)** — fixed `217 × 300px`, `flex-shrink: 0`, `cursor: zoom-in`, `title="Click to enlarge"`, `onClick → openArt`. Contains `<image-slot shape="rounded" radius="6">` with a `325×450` source aspect. 217×300 is 325×450 scaled by 2/3, so the box matches the art aspect exactly.

**Right column (`:62`)** — `flex: 1`, `min-width: 0`, flex column, `justify-content: space-between`, `gap: 12`. `space-between` is what implements "right column spreads to fill art height" (`DESIGN_NOTES.md:44`): three rows pinned top / middle / bottom against the 300px art.

1. **Title row (`:63–83`)** — flex, `align-items: flex-start`, `gap: 12`.
   - Title block (`:64–67`): `<h1>` Inter Tight 700 / 26px / `-0.01em`, margin 0; sub-line `14.5px --mut` = `{setName}` link · `{cardNumber}` · `{characterName}` link.
   - Flexible spacer.
   - **Open in Charts →** (`:69`) — solid `--btn` button-styled anchor, white text, `height 29`, `radius 6`, `13.5px/600` → Charts.
   - **Watchlist button + popover** (`:70–81`) — see §5.3.
   - **+ Binder button** (`:82`) — see §5.4.
   All three controls are `height: 29px`, `flex-shrink: 0`.
2. **Tier strip (`:84–92`)** — `display: grid; grid-template-columns: repeat(6, 1fr); gap: 8px`. See §2.3.
3. **Signal chips (`:93–97`)** — `display: flex; gap: 4px; flex-wrap: wrap`. See §3.4.

### 2.3 Tier strip — exactly 6 cells (`:84–92`, data at `:395–399`)

**Six cells. Not nineteen.** The grid is hard-coded `repeat(6, 1fr)` (`:84`), and `tierStrip` is built by taking the canonical 19-value `BUCKETS` array (`:322`), pairing each with its index, **reversing**, then filtering to the six that have a price series:

```
BUCKETS.map((b,i)=>({b,i})).reverse().filter(b ∈ {PSA 10, Grade 9.5, Grade 9, Grade 8, Grade 7, Raw})
```

**Render order, left to right (fixed, descending grade):**

| # | Label | Seeded price | Seeded 30d | Source index into `TIERS`/`CHG` |
|---|---|---|---|---|
| 1 | PSA 10 | `$1,486` | `+6.2%` | 11 |
| 2 | Grade 9.5 | `$1,010` | `+2.6%` | 10 |
| 3 | Grade 9 | `$842` | `+3.7%` | 9 |
| 4 | Grade 8 | `$710` | `+0.8%` | 8 |
| 5 | Grade 7 | `$620` | `+1.2%` | 7 |
| 6 | Raw | `$455` | `+3.9%` | 0 |

This set is exactly the six `price_months` tiers (`DECISIONS.md` D-003: `Ungraded, Grade7, Grade8, Grade9, Grade9Half, Psa10`), with `Ungraded` displayed as **Raw**. The other 13 grade values exist on this page only as *sales* buckets (ledger filters and sort ranks), never as prices.

**Each cell (`:86–90`):** `--bg` fill, `1px --line`, `radius 8`, `padding 9px 11px`; `title` = `"{label} latest monthly price · {chg} over 30 days"`.
- Line 1 — label: `11px / 600`, `letter-spacing .06em`, `--mut2`, `text-transform: uppercase`, `white-space: nowrap`.
- Line 2 — price: JetBrains Mono `18px / 700`, `--ink`, `margin-top 3`.
- Line 3 — change: JetBrains Mono `12px`, text = `"{chg} 30d"`, `margin-top 1`. Colour rule (`:397`): `chg.indexOf('+') === 0 ? PAL.pos : PAL.neg2`. **Anything not starting with `+` renders as negative**, including a literal `0.0%`.

The seeded prices are each tier's month-12 value (`P10[11]=1486`, `G95[11]=1010`, `G9[11]=842`, `G8[11]=710`, `G7[11]=620`, `RAW[11]=455`), confirming *price = latest monthly close for that tier*. The seeded `CHG` values reconcile with the series only for PSA 10 and Raw (see §7).

### 2.4 Price chart section (`:110–149`)
Card surface, `radius 10`, `padding 14px 16px`.
- Header row (`:111–118`), `align-items: baseline`, `gap 14`, wraps: `<h2>` "Price · 12M · monthly" (Inter Tight 600 / 17px) · six legend toggles · spacer · `open in Charts →` link (`13px`).
- Body (`:119–143`), flex `gap 8`:
  - Y-axis gutter: `44px` fixed, `position: relative`; max label pinned `top: 2px right: 0`, min label `bottom: 2px right: 0`, both mono `10.5px --mut2`.
  - Plot: `flex: 1`, `min-width: 0`, `position: relative`, `cursor: crosshair`, `onMouseMove/​onMouseLeave`. Holds the SVG, the hollow end-dot, and the hover overlay.
- X labels (`:144–148`): `margin-left: 52px` (44 gutter + 8 gap), `justify-content: space-between`, three mono `10.5px` labels — first month, mid month, last month (`Aug ’25` / `Jan ’26` / `Jul ’26`).

**SVG geometry (`:125–131`):** `width: 100%`, `height: 230`, `viewBox="0 0 800 230"`, `preserveAspectRatio="none"` — x stretches to the container, y is locked to 230 CSS px. One decorative full-width rule at `y=115` in `--line4` (`:126`); it is a visual midline, **not** a zero axis or a seam.

### 2.5 Sales ledger section (`:151–215`)
Card surface, `radius 10`, no padding on the section (rows are full-bleed to the border).
- Toolbar (`:152–191`): `<h2>` "Sales ledger" · filter chip row (wraps, `gap 4`) · spacer · `{ledgerCount}` right-aligned, mono `12.5px --mut`.
- Header row (`:192–196`): `display: grid`, `grid-template-columns: {lgGridCols}`, `gap 8`, `padding 7px 16px`, rules top and bottom (`1px --line`), `--bg` fill, `11.5px / 600`, `.05em`, `--mut2`, uppercase.
- Body (`:197–211`): one wrapper `<div>` per row with `border-bottom: 1px --line4`; inside it a grid row, `padding 6px 16px`, `align-items: center`.
- Empty state (`:212–214`): replaces the body only.

**Column widths (`:457`, state `:272`):** `lgGridCols = colW.date + 'px ' + colW.bucket + 'px ' + colW.price + 'px ' + colW.src + 'px minmax(160px, 1fr)'`. Defaults `date 96, bucket 108, price 92, src 92`; the last column takes the remainder with a 160px floor. **`colW.listed: 84` is declared in state and never used** — a vestige of the dropped Listed column.

### 2.6 Census / grading pair (`:217–250`)
`display: grid; grid-template-columns: minmax(0,1fr) minmax(0,1fr); gap: 16px` — two equal cards side by side, both `padding 14px 16px`, `radius 10`.
- **Left — Population · current census** (`:218–233`): title row (h2 + `12.5px --mut2` "PSA + CGC · as of {date}"), 150px-tall bar row, then the gem-rate sentence.
- **Right — Grading activity · PSA 10 slabs added** (`:234–249`): title row (h2 + amber `7 OBS` sufficiency badge), 150px-tall bar row, then the pace sentence.

### 2.7 Footer refresh stamp (`:252–257`)
Full-width strip: `--mutbg` fill, `1px --line`, `radius 8`, `padding 9px 14px`, flex, `gap 16`, `12.5px --mut`. Contents: refresh stamp · `·` · census as-of · flexible spacer (nothing right-aligned). See §3.7.

---

## 3. Data contract

Currency is USD throughout; no cents are ever displayed. `money(n)` = `'$' + Math.round(n).toLocaleString('en-US')` (`:334`) → `$1,486`. Source values are cents in `price_months.price_cents` / `sales` and must be divided by 100 before rounding. Dates render ISO `YYYY-MM-DD`. Months render `MMM ’YY` with a typographic apostrophe U+2019. Negative percentages use U+2212 minus, not hyphen (`:324`).

### 3.1 Card identity

| Field | Format / units | Where | Seeded |
|---|---|---|---|
| `cardName` | text, h1 and breadcrumb tail | `:56, :65` | Umbreon VMAX (Alt Art) |
| `setName` | text, links to Set | `:56, :66` | Evolving Skies |
| `cardNumber` | text as printed, `{n}/{total}` | `:66` | 215/203 |
| `characterName` | text, links to Character | `:66` | Umbreon |
| `artUrl` | image, native 325×450, rendered 217×300 and again in the lightbox | `:60, :104` | placeholder |

### 3.2 Tier strip — `tierStrip[6]`

| Field | Format / units | Where |
|---|---|---|
| `label` | one of the 6 price-tier display names, uppercased by CSS | `:87, :396` |
| `price` | `money()`, whole dollars with thousands separator | `:88, :396` |
| `chg` | signed percent, 1 decimal, `+`/U+2212, e.g. `+6.2%`, `−0.2%` | `:89, :396` |
| `chgFg` | derived colour: `PAL.pos` if `chg` starts with `+`, else `PAL.neg2` | `:89, :397` |
| `tip` | `"{label} latest monthly price · {chg} over 30 days"` | `:86, :398` |

### 3.3 Price chart

| Field | Format / units | Where |
|---|---|---|
| `legend[]` — one per **all 6 tiers**, always rendered | | `:113–115, :405–413` |
| ├ `l` | tier display name | `:114` |
| ├ `c` | swatch colour: series colour when visible, `#D8D8D3` when hidden (hard-coded, not themed) | `:406` |
| ├ `op` | button opacity `1` visible / `0.45` hidden | `:114, :406` |
| └ `tip` | `"Show {l}"` when hidden; `"Hide {l} (y-axis rescales to what’s visible)"` when visible | `:407` |
| `pcSeries[]` — **only visible** tiers | | `:127–130, :414` |
| ├ `c` | stroke colour from `TIER_COLORS` (`:325`) | `:414` |
| ├ `w` | stroke width: `2` for PSA 10, `1.5` for every other tier | `:414` |
| ├ `solid` | polyline points for months 0–10 (closed months) | `:128, :414` |
| └ `dash` | polyline points for months 10–11 only, `stroke-dasharray="4 4"` | `:129, :414` |
| `pcYMax` / `pcYMin` | `'$' + int.toLocaleString('en-US')` — max/min across **visible series over all 12 months** | `:121–122, :415` |
| `pcHollowTop` | percent of the 230px plot height, 1 decimal, for the current-month dot | `:132, :417` |
| `pcHovShow` | bool — cursor is over the plot | `:133, :420` |
| `pcHovLeft` | percent, 2 decimals — crosshair x = `hoverIndex / 11 × 100` | `:134, :420` |
| `pcHovMonth` | `MMM ’YY` | `:136, :421` |
| `pcHovRows[]` — one per visible tier, in tier order | | `:137–139, :416` |
| ├ `l` | tier name | `:138` |
| ├ `v` | `money()` at the hovered month | `:138, :416` |
| ├ `c` | tier colour | `:138` |
| └ `w` | font-weight `700` for PSA 10, `400` otherwise | `:138, :416` |
| x-axis labels | 3 static strings: first / middle / last month of the window | `:145–147` |

Underlying series (`:296–302`): 12 monthly points per tier, `MONTHS = Aug ’25 … Jul ’26`. `pcHovP10` / `pcHovG9` / `pcHovRaw` (`:422`) are computed but bound to nothing — dead.

### 3.4 Signal chips — `sigChips[]` (`:94–96, :400–404`)

Each chip: `{i}` icon glyph + `{t}` text, mono `11.5px/500`, `padding 1px 6px`, `radius 4`, `bg`/`fg`/`bd` triple, `cursor: help`, `title` = `{tip}`.

| `i` | `t` | `tip` | Colour |
|---|---|---|---|
| ▲ | `RS 94th` | "Relative strength vs market index, 3M: 94th percentile" | pos tint (`posBg(.10)` / `pos` / `posBg(.3)`) |
| ▲ | `MACD +` | "MACD (3,6,4) above signal since May 2026" | pos tint |
| ● | `Most active · 41 sales/30d` | "Most-active card on the market in the last 30 days" | neutral (`mutbg` / `mut` / `line`) |

The list is **static in the prototype** — no firing logic, no cap, no overflow control. The full chip vocabulary, trigger thresholds, priority order and the cap-4 "+N more" rule live in `DISPLAY_VOCABULARY.md:11–37` and are unimplemented here (§8).

### 3.5 Sales ledger

**Filter chips** — nine top-level controls covering all 19 buckets, in this render order (`:154–188`, built `:423–455`):

| Order | Control | Members | Where |
|---|---|---|---|
| 1 | `All` | clears all filters | `:446` |
| 2 | `PSA 10` | single bucket | `:446` |
| 3 | `other 10s ▸` (popover) | CGC 10, CGC 10 Prist., TAG 10, ACE 10, SGC 10, BGS 10, BGS 10 Black (7, ascending) | `:158–170, :424, :448` |
| 4–7 | `Grade 9.5`, `Grade 9`, `Grade 8`, `Grade 7` | single buckets | `:447` |
| 8 | `Grade 1–6 ▸` (popover) | Grade 6, 5, 4, 3, 2, 1 (6, descending) | `:174–186, :425, :449` |
| 9 | `Raw` | single bucket | `:450` |

Chip fields: `label`, `tip`, `bg`/`fg`/`bd` (pill state), `pick` handler. Pill styling (`:346`): **on** = `bg: PAL.acc, fg: PAL.card, bd: PAL.acc`; **off** = `bg: PAL.card, fg: PAL.mut, bd: PAL.line`. Height 24px, `radius 5`, mono `12px/600`. Sub-chips are 22px, `radius 4`, mono `11.5px/600` (`:165, :181`).

Group-chip label (`:440`): selected members joined with `", "` when any are selected, otherwise the group name; then `" ▾"` if its popover is open, `" ▸"` if closed. A group chip reads **on** when any member is selected **or** its popover is open (`:438`).

| Field | Format | Where |
|---|---|---|
| `ledgerCount` | `"{n} sales shown"` — count **after** filtering, no separator formatting, never pluralised | `:190, :456` |
| `lgCols[5]` | header cells; `{ name, arrow, sort, rs }` | `:193–195, :458–462` |
| ├ `name` | `Date` · `Grade bucket` · `Realized` · `Source` · `Listing title` | `:458` |
| └ `arrow` | `" ▾"` when this column is the active desc sort, `" ▴"` when asc, `""` otherwise | `:460` |

**Row fields — `ledgerRows[]` (`:198–210`, built `:352`):**

| Column | Field | Format / units | Style |
|---|---|---|---|
| Date | `date` | ISO `YYYY-MM-DD` | mono `12.5px --mut`, centred (`:202`) |
| Grade bucket | `bucket` | one of the 19 canonical values, verbatim | mono `12.5px --ink`, centred (`:203`) |
| Realized | `price` | `money()` — the sale price | mono `13.5px/700 --ink`, centred (`:204`) |
| Realized | `listedLine` | `"2px dotted #8F6614"` when a listed price exists, else `"none"` — applied as `border-bottom` | `:204, :352` |
| Realized | `listedTip` | `"listed {money(l)} → sold {money(p)}"`, else `""` | `:204, :352` |
| Realized | `listedCur` | `"help"` when listed exists, else `"default"` | `:204, :352` |
| Source | `src` | venue enum, lowercase verbatim: `ebay` · `tcgplayer` · `goldin` · `heritage` · `pwcc` (`DISPLAY_VOCABULARY.md:61`; seeded set at `:304–319` uses exactly these five) | mono `12.5px --mut`, centred (`:205`) |
| Listing title | `title` | raw marketplace listing text, may contain emoji and typographic quotes | `13px --mut`, **left**-aligned, `nowrap` + `overflow: hidden` + `text-overflow: ellipsis`, `title` attr = full text (`:206`) |
| — | `pRaw` | numeric sale price, sort key only | `:352` |
| — | `hasListed` | bool, computed but bound to nothing — dead (`:352`) |
| — | `isSale` | always `true`; gates the row template (`:200, :352`) |
| — | `isSeam` | always `false`; **no template branch consumes it** (`:352`) |

**There is no Listed column.** Confirmed three ways: `lgCols` has five entries (`:458`); `lgGridCols` composes widths from `date, bucket, price, src` only (`:457`); and `colW.listed: 84` (`:272`) plus `hint-placeholder-count="6"` on the header loop (`:193`) are orphaned leftovers from when it existed. The listed price survives only as the dotted amber underline + tooltip on Realized — exactly as `DESIGN_NOTES.md:46` specifies. **Verified.**

Note the amber `#8F6614` is hard-coded in the row builder (`:352`) rather than read from `PAL.warnInk`, so it does not lighten in dark theme. Treat that as a bug: bind it to the warn token.

### 3.6 Census and grading

**Population · current census (`:218–233`):**

| Field | Format / units | Where |
|---|---|---|
| header suffix | `"PSA + CGC · as of {censusAsOf}"`, ISO date | `:221` |
| `popBars[]` — 6 bars | | `:224–230, :465–470` |
| ├ `n` | slab count, `toLocaleString('en-US')` (thousands separator), above the bar, mono `11.5px --mut` | `:226, :466` |
| ├ `label` | grade label below the bar, `11px --mut2`, `nowrap` | `:228, :466` |
| ├ `h` | bar height px = `round(n / maxPop × 104) + 4` — the `+4` guarantees a visible stub at n=0 | `:227, :467` |
| ├ `bg` | `rgba(74,99,208,0.55)` for PSA, `rgba(138,138,134,0.45)` for CGC — **hard-coded, not themed** | `:468` |
| └ `tip` | `"{label}: {n} slabs in current census ({PSA\|CGC})"` | `:227, :469` |

Seeded bars, in render order: `PSA 8 1,244` · `PSA 9 3,865` · `PSA 10 1,479` · `CGC 9 402` · `CGC 9.5 618` · `CGC 10 187` (`:368–369`). **PSA group first, then CGC**, each ascending by grade; the split is carried by fill colour, not by a divider or a legend. `maxPop = 4020` is a hard-coded scale ceiling (`:366`), *not* `max(n)` = 3,865 — so the tallest bar reaches ~96% of the track. Bar row: `height 150px`, `align-items: flex-end`, `gap 14`, each column `flex: 1`.

**Gem-rate sentence (`:232`)** — static markup in the prototype; the template and its branches are specified at `DESIGN_NOTES.md:52`.

> Gem rate `27.3%` — of the last 90 days of PSA submissions, the share that came back 10. Drifting `−0.4pp / 90d` (harder to gem = supply of fresh 10s slowing).

- Inputs: `gemRate` (percent, 1 decimal) and `gemDrift` (percentage points per 90 days, 1 decimal, signed with U+2212). Numbers render mono; `gemRate` in `--mut`, `gemDrift` in the branch colour.
- Branch rules (`DESIGN_NOTES.md:52`): rate = trailing-90d PSA submissions; if fewer than ~30 submissions, **omit the drift sentence entirely**. Drift branches — falling → "harder to gem = supply of fresh 10s slowing" (green) · rising → "easier to gem = fresh 10s arriving faster" (red) · flat within ±0.1pp → "steady" (grey).
- **The colour is semantic, not arithmetic**: the seeded `−0.4pp` renders in `--pos` green (`:232`) because a falling gem rate is bullish. Never colour these by sign.
- `DISPLAY_VOCABULARY.md:32` additionally defines a header chip `gem rate −0.4pp` when |drift| ≥ 0.3pp/90d; it is not rendered here.

**Grading activity · PSA 10 slabs added (`:234–249`):**

| Field | Format / units | Where |
|---|---|---|
| `obsBadge` | `"{n} OBS"`, mono `11px/600`, `.06em`, `--warnInk` on `rgba(176,127,26,0.12)`, `radius 3`, `cursor: help` | `:237` |
| `obsBadge` tooltip | `"Census history begins {Mon YYYY} — {n} observations so far; deltas need two"` | `:237` |
| `deltaBars[]` — one per observed month | | `:240–246, :471–477` |
| ├ `n` | `"+{count}"` above the bar, mono `11.5px --mut` | `:242, :472` |
| ├ `label` | short month name below the bar, `11px --mut2` | `:244, :472` |
| ├ `h` | px = `round(n / maxD × 104) + 4` | `:243, :473` |
| ├ `bg` | `rgba(74,99,208,0.55)` — uniform, hard-coded | `:474` |
| ├ `bd` | CSS border shorthand, **always `'none'` in the prototype** — the hook exists for an outlined/provisional bar but no branch sets it | `:243, :475` |
| └ `tip` | `"+{n} new PSA 10 slabs in {Month} {Year}"` | `:243, :476` |

Seeded: `+34 +41 +38 +52 +47 +61 +58` for Jan–Jul (`:371–373`), `maxD = 61` = `max(deltas)` — note this scale ceiling **is** data-derived while the population one is not. Bar row `height 150`, `gap 10`, each column `position: relative` (the hook for a per-bar annotation that is never rendered).

**Pace sentence (`:248`)** — static markup; template and branches at `DESIGN_NOTES.md:53`.

> Pace `+58 / mo` and rising — `331` new 10s since Jan, growing the census `+29%` in 7 months (fresh supply working against the price).

- `+58/mo` = **the latest month's delta**, not a mean (mean is 47.3). `331` = sum of all deltas (34+41+38+52+47+61+58 ✓). `+29%` = `331 ÷ (censusNow − 331)` = 331 ÷ 1,148 = 28.8% ✓, i.e. new slabs ÷ census at window start. `7 months` = observation count. `Jan` = first observation month.
- Pace word branches: rising / steady / slowing, from recent-3mo mean vs prior-3mo mean (seeded: (47+61+58)/3 = 55.3 vs (41+38+52)/3 = 43.7 → **rising** ✓).
- Parenthetical branches: census growth > 2%/mo → "fresh supply working against the price" (red) · else → "supply nearly frozen — scarcity intact" (green).
- Gating: fewer than 2 census observations → the whole line degrades to "census history too young to compute pace" under the LOW DATA convention.
- Again the colour is semantic: `+29%` renders `--neg2` red (`:248`) because supply growth is bearish.

### 3.7 Footer stamps (`:252–257`)

| Element | Text | Tooltip |
|---|---|---|
| Sales/price freshness | `Sales & prices refreshed ` + **`just now`** (mono) | "Opening a card page triggers a fresh scrape — the ledger and prices you see include sales up to right now" |
| Separator | `·` | — |
| Census freshness | `Census as of ` + **`2026-07-30`** (mono, ISO) | "Population data comes from PSA/CGC on their own publishing schedule — it can't be scraped on demand" |

Two different freshness models on one bar, deliberately: sales are pulled on demand, census is not. This replaced the app-wide AsOfStamp component (`DESIGN_NOTES.md:54`, `:84`).

**Implementation note (architectural).** "Card page visits trigger a fresh scrape" is the product behaviour this stamp asserts, and it is the reason this screen is the first vertical slice. The scrape is `POST /cards/{id}/express-visit` (synchronous, 200 parsed · 502 upstream · 422 refused · 504 timeout) or `POST /cards/{id}/refresh-request` (fire-and-forget, 202) on the worker's intake API — **bound to `127.0.0.1`, so the call must be made server-side on the Pi; a browser cannot reach it** (`CLAUDE.md` "The intake API", D-013/D-014). The prototype has no loading, in-flight, retry, or failure presentation for this — it renders "just now" unconditionally (§7).

### 3.8 Theming (`:25–30`, `:264–271`)

Two orthogonal switches read from `localStorage` before paint (`:33`): `cardstock-theme = 'dark'` sets `data-theme="dark"`, `cardstock-cvd = '1'` sets `data-cvd="1"`. The JS `PAL` object mirrors the CSS custom properties for values computed in script (chip fills, bar fills, series colours). Four palettes: light, light-CVD (Okabe-Ito), dark, dark-CVD. Semantic tokens: `pos`, `pos2`, `neg`, `neg2`, `neg3`, `posBg(a)`, `negBg(a)` plus the neutral ramp. Colour is never the only channel — every signal chip carries a glyph (`▲ ● `), and the change/tooltip text carries the sign.

---

## 4. States

### 4.1 Page-level
| State | Trigger | Render |
|---|---|---|
| Loaded | default | All six blocks render. |
| Theme | `localStorage` at load | Light / dark × normal / CVD (§3.8). |

### 4.2 Card art
| State | Trigger | Render |
|---|---|---|
| Thumbnail | default (`artOpen` falsy, `:391`) | 217×300, `cursor: zoom-in` (`:59`). |
| Lightbox open | click the art → `openArt` (`:392`) | Fixed full-viewport backdrop `rgba(20,19,26,0.55)`, `z-index 200`, `cursor: zoom-out`; centred figure sized `min(62vh, 78vw)` at `aspect-ratio: 325/450`, `radius 10`; 30px circular ✕ at `top:-14px right:-14px` (`:101–108`). |
| Lightbox closed | click backdrop, or click ✕ → `closeArt` (`:393`); clicks **inside** the figure are swallowed by `stopClick` (`:394`) | Returns to thumbnail. No Escape handler exists (§7). |

### 4.3 Watchlist control (`:70–81`, `:375–386`)
| State | Trigger | Render |
|---|---|---|
| Not watching | no truthy entry in `watchIn` | Label `+ Watchlist ▾`; neutral (`card` bg, `ink` fg, `line` border), hover → `hov`. |
| Watching, 1 list | exactly one truthy | Label `Watching ✓ ▾` (the count parenthetical is suppressed at n=1, leaving a trailing space — `:375`); green tint: `posBg(.10)` bg, `pos` fg, `posBg(.35)` border, hover unchanged. |
| Watching, n>1 | ≥2 truthy | Label `Watching ✓ (n) ▾`; same green tint. |
| Popover open | click the button → `toggleWatch` (`:378`) | Absolute panel below-right (`top 33, right 0`), `z-index 60`, `min-width 190`, card bg, `radius 8`, shadow. |
| Popover closed | click the button again, or `mousedown` anywhere outside `[data-watch-pop]` (`:274–279`) | — |

Row per list (`:75`): 15px checkbox square (fill+border `pos2` and `✓` when member, else `card` + `line3` and empty), name, right-aligned count (mono `11.5px --mut2`). Clicking a row toggles membership and **leaves the popover open** (`:384`). Below a divider, `+ New list…` in accent (`:78`) → `window.prompt('New list name')`; a non-empty answer marks that name watched (`:386`).

Count arithmetic (`:381`): displayed = `baseCount + (isMember ? 1 : 0) − (listAlreadyIncludedThisCard ? 1 : 0)`. In the seed, `Alt arts` is pre-joined so its base 12 already counts this card; the correction keeps the displayed number live as membership toggles. **Rule for the build: show each list's size with this card's current, unsaved membership applied.**

### 4.4 Binder control (`:82`, `:387–390`)
Two states only: `+ Binder` (neutral) and `In binder ✓` (green tint, identical token set to the watch button). Click toggles. Tooltip: "Log a purchase of this card — opens the binder transaction form" — the tooltip promises a transaction form the prototype does not open (§7).

### 4.5 Price chart
| State | Trigger | Render |
|---|---|---|
| Default visibility | `DEF_OFF` (`:331–332`) hides every tier except **PSA 10, Grade 9, Raw** → **Grade 9.5, Grade 8 and Grade 7 are hidden on load** | 3 polyline pairs drawn; 3 legend entries dimmed to `0.45` with a `#D8D8D3` swatch. |
| Series hidden | click a lit legend entry (`:408–412`) | Series removed from the SVG, from the hover readout, and from the y-axis min/max — the axis rescales. |
| Series shown | click a dimmed legend entry | Reverse. |
| Last-series guard | attempt to hide when 5 of 6 are already hidden | **No-op.** Guard: `Object.keys(off).length < SER.length − 1` (`:410`). At least one series is always drawn. |
| Hover | `mousemove` over the plot (`:418`) | Vertical crosshair at `left: {pcHovLeft}%`, `1px`, `rgba(28,28,30,0.22)`, `pointer-events: none`; readout box anchored at `top: 8px; left: 8px` of the plot (**fixed corner, does not follow the cursor**) with the month and one line per visible series. |
| Hover cleared | `mouseleave` (`:419`) | Crosshair and readout removed. |
| Current month | always | Last segment (month 11 → 12) dashed `4 4` in the series colour; a hollow 8px dot sits at `left: 100%`, `top: {pcHollowTop}%`, `background: --card`, `1.5px solid --acc`, centred by `translate(-50%,-50%)`. Tooltip: "Aug is month-to-date — the point firms up as the month's sales land, and finalizes when the month closes" (`:132`). |

**Current-month treatment, normative** (`DESIGN_NOTES.md:49`): the month-to-date point uses the *same* aggregation as closed months (outlier-trimmed median of that tier's sales since month start) on partial data, recomputed as each sale lands and frozen at month close. **Never projected or extrapolated to month-end.** The dashed segment + hollow dot is the entire warning; no text banner.

The hollow dot is drawn **once**, not per series: it follows `SER.find(g => !off[g.k])` — the first *visible* tier in descending order, i.e. PSA 10 unless PSA 10 is hidden (`:417`). Its border colour is the fixed `--acc`, not the series colour.

### 4.6 Sales ledger — filters
| State | Trigger | Render |
|---|---|---|
| Unfiltered | `bucketSel` empty (default) | `All` chip lit; every sale shown; `ledgerCount` = total. |
| Filtered | any bucket chip or sub-chip clicked (`:428–432`) | Multi-select **OR**: a sale is shown if its bucket is in the selected set (`:349–350`). `All` goes unlit. |
| Cleared | click `All` (`:434`) | `bucketSel = {}` and both group popovers close. |
| Group popover open | click `other 10s` or `Grade 1–6` (`:441`) | Panel below-left (`top 26, left 0`), `z-index 50`, `min-width 240`, with an uppercase `10.5px` caption ("Other graders' 10s" / "Grades 1–6") and a wrapped sub-chip grid. Opening one **closes the other** (`:441`). |
| Group popover closed | click the group chip again · `mouseleave` the wrapper (`:158, :174, :452`) · `mousedown` outside `[data-lg-pop]` (`:277`) | — |

### 4.7 Sales ledger — sort (`:353–365`, `:458–462`)
| Aspect | Behaviour |
|---|---|
| Default | `sortKey = 'date'`, `sortDir = 'desc'` — newest first. |
| Click a **different** column | `sortDir = 'desc'`. **Desc-first, always.** |
| Click the **active** column | toggles `desc → asc → desc`. |
| Indicator | `" ▾"` desc / `" ▴"` asc, appended to the active header's text; other headers show nothing. |
| Date | string comparison of ISO dates — equivalent to chronological. |
| Realized | numeric on `pRaw`. |
| Grade bucket | **rank, not alphabetical**: `BUCKETS.indexOf(bucket)` (`:354`), so `Raw`=0 … `PSA 10`=11 … `BGS 10 Black`=18. Desc puts BGS 10 Black first, Raw last. |
| Source | string comparison, lowercase. |
| Listing title | string comparison, case-sensitive (raw marketplace text). |
| **Ties** | fall to `date` (`:362`) — the tiebreak is embedded *before* the direction flip, so ties resolve newest-first under desc and oldest-first under asc, matching the primary direction. |

Sortable columns: all five. Every header is `cursor: pointer`, `title="Click to sort"`, hover → `--acc`.

### 4.8 Sales ledger — column resize (`:194`, `:282–293`)
Each header carries a `│` grip (`--line3`, `cursor: col-resize`, `title="Drag to resize"`, hover `--acc`). `mousedown` captures `clientX` and the current width, then `mousemove` on `window` sets `width = clamp(40, 420, startW + dx)`; `mouseup` detaches both listeners. `preventDefault` + `stopPropagation` on mousedown keeps a drag from firing the sort. The **Listing title** grip is wired to key `'src'` (`:458`) — dragging it resizes the Source column (§7, bug).

### 4.9 Sales ledger — rows
| State | Trigger | Render |
|---|---|---|
| Has sales | `filtered.length > 0` (`:197, :464`) | Row list. |
| Realized without listed | `sale.listed == null` | Plain bold mono price, `cursor: default`, no tooltip. |
| Realized with listed | `sale.listed != null` (~4.4% of production rows, `DESIGN_NOTES.md:46`) | `border-bottom: 2px dotted #8F6614` under the price, `cursor: help`, tooltip `listed $X → sold $Y`. |
| **True zero** | `filtered.length === 0` (`:212, :464`) | Body replaced by centred `13.5px --mut2` text at `padding 26px 16px`: *"No sales observed in this grade — that's a true zero: our scrapers visited and found none, not \"no data\"."* The toolbar, filter chips, `ledgerCount` ("0 sales shown") and the column header row all **still render**. |

**The true-zero distinction is the point of this state.** It asserts a *scraped negative* — the crawler visited and the market produced no sales in that bucket — which is a different claim from "we have no data here". With the seeded set (`:304–319`: PSA 10 ×7, Grade 9 ×3, Raw ×3, Grade 9.5 ×2, Grade 8 ×1) selecting `Grade 7`, any other-10s bucket, or any of Grades 1–6 reaches it. The copy is only correct for a single-bucket selection (§7).

### 4.10 Sufficiency
The `7 OBS` badge (`:237`) is the LOW DATA render from `DISPLAY_VOCABULARY.md:56`: amber badge `N OBS` + tooltip stating the floor rule and what improves it. It is the **only** sufficiency state the Card prototype exercises; OK, LOCKED, UNDEFINED window and UNSTABLE FIT do not appear here.

**Reality check for the first build (`DECISIONS.md` D-001, D-033).** The seeded ledger reaches back to 2026-03-28 and the census shows 7 monthly observations from Jan 2026 — both impossible. Real per-sale and census history begin at each card's *first crawler visit*, late Jul 2026, per-card and ragged; D-033 further discards observations before 2026-09-01. On day one the ledger will hold days of sales or none, the grading panel will sit below its 2-observation floor and must degrade to "census history too young to compute pace", and the true-zero state will be the **common** case, not the exception. Build the empty and degraded paths first; the seeded density is fiction.

---

## 5. Interactions

| # | Control | Where | Consequence |
|---|---|---|---|
| 1 | Breadcrumb `Browse` | `:56` | → Browse. |
| 2 | Breadcrumb `{setName}` / sub-line set link | `:56, :66` | → Set. |
| 3 | Sub-line `{characterName}` | `:66` | → Character. |
| 4 | **Open in Charts →** | `:69` | → Charts, for this card. |
| 5 | **Watchlist ▾** | `:71` | Toggles the multi-list popover. Does not itself join a list. |
| 6 | Watchlist row | `:75` | Toggles membership of that list; count updates live; popover stays open. |
| 7 | **+ New list…** | `:78` | `prompt()` for a name; non-empty → card joins the new list. |
| 8 | Click outside the watch popover | `:274–279` | Closes it. |
| 9 | **+ Binder / In binder ✓** | `:82` | Toggles binder membership (prototype); intended to open the binder transaction form. |
| 10 | Signal chip hover | `:95` | Native tooltip with the evidence sentence; `cursor: help`. Not clickable. |
| 11 | Tier-strip cell hover | `:86` | Native tooltip "{tier} latest monthly price · {chg} over 30 days". **Cells are not clickable** — they do not filter the ledger or the chart. |
| 12 | Card art click | `:59` | Opens the lightbox. |
| 13 | Lightbox backdrop click / ✕ click | `:102, :105` | Closes. Clicks on the art itself do nothing (`:103`). |
| 14 | Legend entry click | `:114` | Shows/hides that tier; **y-axis rescales to the visible set**; the hover readout gains/loses that row; the hollow dot may move to a different series. Blocked when it would hide the last series. |
| 15 | Chart mousemove | `:124` | `idx = round(fraction × 11)`, clamped 0–11 → crosshair + readout. State only changes when the index changes. |
| 16 | Chart mouseleave | `:124` | Clears the readout. |
| 17 | `open in Charts →` (chart header) | `:117` | → Charts. Same destination as #4. |
| 18 | Bucket chip click | `:156, :172, :187` | Toggles that bucket in the filter set. |
| 19 | `All` chip click | `:434` | Clears the filter set; closes both group popovers. |
| 20 | Group chip click | `:159, :175` | Toggles its popover; closes the sibling popover. |
| 21 | Sub-chip click | `:165, :181` | Toggles that bucket; popover stays open; the group chip's label becomes the joined member list. |
| 22 | Group popover mouseleave / outside mousedown | `:158, :174, :277` | Closes both popovers. |
| 23 | Column header text click | `:194` | Sorts (§4.7). |
| 24 | Column grip drag | `:194, :282–293` | Resizes, clamped 40–420px. |
| 25 | Row title hover | `:206` | Native tooltip with the untruncated listing title. |
| 26 | Realized-price hover (listed rows only) | `:204` | Native tooltip `listed $X → sold $Y`. |
| 27 | Population bar hover | `:227` | Native tooltip "{grade}: {n} slabs in current census (PSA\|CGC)". |
| 28 | Delta bar hover | `:243` | Native tooltip "+{n} new PSA 10 slabs in {Month} {Year}". |
| 29 | `7 OBS` badge hover | `:237` | Native tooltip with the sufficiency floor rule. |
| 30 | Footer stamp hover | `:253, :255` | Native tooltips explaining the two freshness models. |

Every tooltip on this page is a native `title` attribute. Nothing here is a custom tooltip component; the only bespoke overlay is the chart readout.

---

## 6. Rules and invariants

1. **Six price tiers, nineteen sale buckets.** The tier strip and the chart legend show exactly the six tiers that exist in `price_months` — PSA 10, Grade 9.5, Grade 9, Grade 8, Grade 7, Raw (`:395, :327`; `DECISIONS.md` D-003). The ledger's grade buckets, filter chips and sort ranks use the full canonical 19 (`:322`). Never plot or price-filter on a value outside the six; never restrict a *sale* to the six.
2. **Descending grade order everywhere it is a scale.** `BUCKETS` is stored ascending (Raw first) and reversed at every display site (`:327, :395`). The tier strip, chart legend, and grade-desc sort all read high-to-low. (The ledger *chip row* deliberately breaks this — see §8.)
3. **"Raw" is the display name for `Ungraded`, app-wide** (`HANDOFF.md:106`).
4. **At least one chart series is always visible** (`:410`). The y-axis has no defined range otherwise.
5. **The y-axis is the min and max of the visible series across all 12 months** (`:339–341`), recomputed on every legend toggle. It is not zero-based and does not pad.
6. **The current month is always dashed with a hollow end dot, and is never projected** (`:129, :132`; `DESIGN_NOTES.md:49`). Same aggregation as a closed month, on partial data.
7. **Grade sorting is by rank, never alphabetical** (`:354`).
8. **Ties always fall to date**, in the direction of the active sort (`:362`).
9. **Sorting a new column starts descending** (`:461`).
10. **Bucket filtering is OR across selections; an empty selection means everything** (`:349–350`).
11. **Empty ledger means true zero, not missing data** (`:213`). The scrapers visited. Copy must not be softened into "no data available".
12. **No Listed column.** Listed price is a modifier on Realized: dotted amber underline + tooltip, and nothing else (`:204`; `DESIGN_NOTES.md:46` — 4.4% production coverage; a 95.6%-blank column reads as broken).
13. **Money is whole dollars with thousands separators, in JetBrains Mono** (`:334`). Every numeric on this page is mono (`DESIGN_NOTES.md:87`).
14. **Colour is semantic, never arithmetic, in the summary sentences.** A falling gem rate is green; growing supply is red (`:232, :248`).
15. **Colour is never the only channel.** Signal chips carry a glyph; changes carry a sign; census bars carry a tooltip naming the grader (`DISPLAY_VOCABULARY.md:2`).
16. **Two freshness models coexist.** Sales/prices are refreshed by the visit; census is as-of whatever PSA/CGC last published (`:253, :255`). Never merge them into one stamp.
17. **A card page visit triggers a scrape** (`:253`; `DESIGN_NOTES.md:54`). Server-side only — the intake API is loopback-bound (`CLAUDE.md`).
18. **Bar heights always add a 4px floor** so a zero-count bar is still visible (`:467, :473`).
19. **No seam markers on this page.** Not in the ledger, not on the chart (§8).
20. **Every tooltip is a plain `title` attribute** — accessible by default, no JS.

### 6.9 Prototype-only artefacts — do not port
`state.bucket: 'All'` (`:272`, superseded by `bucketSel`) · `colW.listed` (`:272`) · `this.SEAMS` (`:321`) · `isSeam`/`hasListed` on rows (`:352`) · `counts` and the `sum()` helper (`:347, :426`, computed and never read) · `pcHovP10`/`pcHovG9`/`pcHovRaw` (`:422`) · the synthetic-series fallback in `SER` (`:329`, unreachable because all six tiers have real arrays) · `d.bd` always `'none'` (`:475`). Misleading `hint-placeholder-count` values: `lgCols` says 6 but there are 5 (`:193`), `popBars` says 5 but there are 6 (`:224`), `sigChips` says 2 but there are 3 (`:94`), `legend`/`pcSeries` say 3 (`:113, :127`), `ledgerRows` says 10 (`:198`). Hints are authoring artefacts with no runtime effect (`support.js:613, :625`).

---

## 7. Open questions

1. **Which month is "current"?** `MONTHS` ends `Jul ’26`, the x-axis right label is `Jul ’26`, and the dashed segment is Jun→Jul — but the hollow dot's tooltip says *"Aug is month-to-date"* (`:132, :296, :147`), and the newest seeded sale is `2026-08-01` (`:304`). The intent is unambiguous (the last point is the in-progress month, dashed + hollow) but the seeded window is one month short. **Build rule: the last point is always the current calendar month, and the tooltip must name it dynamically.** Confirm whether the window is "trailing 12 months ending this month" (12 points, last one partial) or "12 closed months + current".
2. **Does the tier strip's 30d change come from the price series or somewhere else?** Only PSA 10 (`1486/1399−1 = +6.2%`) and Raw (`455/438−1 = +3.9%`) reconcile with the monthly arrays; Grade 9 shows `+3.7%` where the series implies `+6.6%`, Grade 8 `+0.8%` where the series implies `+6.0%` (`:297–302, :323–324`). Either the seed is careless or "30d" is a genuine trailing-30-day window rather than a month-over-month step. Decide and define.
3. **Where does `gemRate` come from?** 27.3% is not derivable from the six census bars (PSA 10 ÷ all PSA = 22.4%). `DESIGN_NOTES.md:52` says trailing-90d PSA *submissions* — a distinct series from the population snapshot. Confirm the scraper exposes it; if not, the sentence cannot render.
4. **Is the population bar set fixed or data-driven?** The seed hard-codes PSA 8/9/10 + CGC 9/9.5/10 (`:368–369`). Real cards will have PSA 1–10 and CGC/BGS/SGC/TAG/ACE rows. Which grades appear, in what order, and what happens at 10+ bars in a half-width card?
5. **What is `maxPop`?** Hard-coded 4020 (`:366`) rather than `max(n)`, while the grading panel uses `max(deltas)` (`:372`). Is the census bar scale meant to be shared across cards, or was 4020 arbitrary?
6. **Ledger volume.** All 16 seeded rows render at once, unpaged and unvirtualised (`:198`). Production has millions of sales; a hot card will have thousands. Paging, infinite scroll, a window cap, or a server-side limit — undecided, and it changes the query.
7. **Scrape-in-flight presentation.** "Sales & prices refreshed just now" renders unconditionally (`:253`). Nothing specifies the pending, timeout (504), refused (422), or upstream-failure (502) states of the express visit, or whether the page blocks on it or renders stale-then-updates.
8. **Signal chips are static.** No firing evaluation, no cap-4 truncation, no "+N more" overflow control exists (`:400–404` vs `DISPLAY_VOCABULARY.md:7, :37`). Does v1 ship real chips, or a fixed subset?
9. **The seeded chip says "41 sales/30d"** while the ledger shows 16 sales across five months (`:403, :304–319`). Different windows or an inconsistent seed?
10. **`Grade 9.5` label vs reality.** The bucket is grader-agnostic below 10 (ADR-0005 pooling, `DECISIONS.md` D-012/D-022) and PSA does not issue 9.5s, yet the strip labels it plainly. Does it need a qualifier?
11. **Chart period is fixed** at "12M · monthly" with no control (`:112`). Confirmed intentional (that is what Charts is for)?
12. **Route parameter.** `/card/{id}` is a Tier-2 claim (`HANDOFF.md:76`); no prototype link carries an id. Slug or numeric id? What renders on unknown/delisted (the intake API's 404/409 cases)?
13. **Accessibility gaps to close in Blazor:** the lightbox has no Escape handler, no focus trap, no `role="dialog"`/`aria-modal` (`:101–108`); sortable headers are `<span onClick>` with no `aria-sort` and no keyboard affordance (`:194`); popovers are plain divs with no `aria-expanded` on their triggers.
14. **Bugs to fix rather than reproduce:** the Listing-title resize grip is wired to key `'src'` and resizes the Source column (`:458`); `ledgerCount` never pluralises ("1 sales shown", `:456`); the true-zero copy says "in this grade" even for multi-bucket selections (`:213`); the listed-price underline hard-codes `#8F6614` instead of `PAL.warnInk`, so it does not adapt in dark theme (`:352`); the hidden-legend swatch hard-codes `#D8D8D3` (`:406`); `Watching ✓ ` carries a trailing space at n=1 (`:375`); `+ New list…` marks a card as watching a list that never appears in the popover, because `watchLists` is a fixed literal (`:380, :386`).

---

## 8. Contradictions found

| Claim | Source doc:line | What the HTML actually does |
|---|---|---|
| Card page has a **"19-tier strip"** | `HANDOFF.md:76` | Renders **6** cells. The grid is hard-coded `repeat(6, 1fr)` (`:84`) and `tierStrip` filters the 19-value `BUCKETS` down to PSA 10 / Grade 9.5 / Grade 9 / Grade 8 / Grade 7 / Raw (`:395–399`). The 19 values appear only as ledger buckets, filter chips and sort ranks (`:322, :354, :446–453`). Agrees with `DECISIONS.md` D-003 (6 price tiers in `price_months`). |
| **"Tier strip = PSA-only, 5 tiles (PSA 10/9/8/7 + Ungraded) — Grade 9.5 tile DROPPED… Chart legend matches. Card page signed off 2026-08-04."** | `DESIGN_NOTES.md:59` | **6 tiles including Grade 9.5**, and the chart legend also includes Grade 9.5 (`:395–399, :327`). This is the *later* of two conflicting notes in the same file and it is the one the signed-off prototype contradicts. `DESIGN_NOTES.md:55` (six tiers, PSA 10/9.5/9/8/7/Ungraded) is the line that matches. **Build the 6.** |
| **"Seam markers only render in date sort — they're chronological annotations."** | `DESIGN_NOTES.md:47`, repeated `:83` | **No seam marker is ever rendered, in any sort.** `this.SEAMS` is defined (`:321`) and never read; every row is constructed with `isSeam: false` (`:352`); the row template has an `isSale` branch and no seam branch (`:200–208`). The wrapper div that would host one exists (`:199`), which is why the scaffolding looks live. This matches `DESIGN_NOTES.md:54` ("Removed from Card page… seam markers in sales ledger (no seam recognition planned)") — `:47` and `:83` are stale. |
| **"resolution seam Jul '26 amber dashed line on price chart ('per-sale ledger begins')"** | `DESIGN_NOTES.md:35` | The Card price chart draws no amber seam line. The only dashed stroke is each series' final segment, in the series' own colour (`:129, :414`); the only horizontal rule is a decorative `--line4` midline at `y=115` (`:126`). (`DECISIONS.md` D-009 already flags the companion "Apr '25" seam in this same line as unsupported by data.) |
| **"six-tier strip → canonical 19-value grade scale"** listed as a Card-page spec delta | `DESIGN_NOTES.md:83` | The 19-value scale replaced the 6-value scale for **sales buckets and ledger filters** (`:322, :446–453`), not for the tier strip or the price chart, which remain 6 (`:395, :327`). Read as written, the delta is what produced `HANDOFF.md:76`'s "19-tier strip". |
| Grade buckets: **"Display order always descending (BGS 10 Black first)"** | `DISPLAY_VOCABULARY.md:64` | True for the tier strip, the chart legend and grade-desc sort (`:395, :327, :354`). **False for the ledger filter chips**, which promote `PSA 10` ahead of the other 10s (`:446–450`), and for the "other 10s" sub-chips, which render **ascending** CGC 10 → BGS 10 Black (`:424`) while the "Grade 1–6" sub-chips render descending (`:425`). Three different orders on one toolbar. |
| Card-page chips: **"shows only FIRING chips… priority-ordered, cap 4, overflow '+N more' opens all"** | `DISPLAY_VOCABULARY.md:7`, `:37` | Three chips from a hard-coded literal (`:400–404`). No firing evaluation, no priority sort, no cap, no overflow control anywhere on the page. |
| **"the DC runtime drops ALL elements after the first inside an sc-for loop in SVG — one element per loop"** | `DESIGN_NOTES.md:34` | Not supported by the runtime source: `walkFor` maps **every** child builder per item (`support.js:610–643`). The Card chart depends on this — each `pcSeries` item emits two `<polyline>` elements, solid and dashed, inside one `sc-for` (`:127–130`). The note may describe a sandbox-specific artefact; it is not a rule to design around, and it has no analogue in Blazor. |
| Per-sale ledger back to **Mar 2026** and 7 census observations from **Jan 2026** (the seeded data) | `Cardstock Card.dc.html:304–319, :371–373`, and `HANDOFF.md` §5 as originally written | Contradicted by `DECISIONS.md` **D-001** (per-sale and census history begin at each card's first crawler visit, **late Jul 2026**, per-card and ragged; owner: "It just started this month") and **D-033** (nothing before 2026-09-01 counts). The seeded density is illustrative fiction. At launch this page's ledger is near-empty and the grading panel is below its floor. |
