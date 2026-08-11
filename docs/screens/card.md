# Screen spec — Card

> **Source of truth:** `CardStock Mockup/Cardstock Card.dc.html` (483 lines), read in full 2026-08-10.
> Every line citation below is `Cardstock Card.dc.html:NNN` unless another file is named.
> Markdown docs are Tier 2/3 and were **not** used to fill gaps. Where they disagree, see §8.
> Seeded values (Umbreon VMAX Alt Art, `$1,486`, 16 sales…) are **illustrative**. What is normative is the
> structure, the derivation rules, and the complete state space.

---

## 1. Identity

| | |
|---|---|
| **Screen name** | Card (`data-screen-label="Card"`, :35) |
| **Prototype file** | `CardStock Mockup/Cardstock Card.dc.html` |
| **Route** | **Not specified in the HTML.** The prototype is a flat file; inbound links from other prototypes are plain filenames. A route must be chosen — see §7 OQ-1. |
| **Purpose** | The single-card market terminal: one card's identity, its current price across the six priced grade tiers, its 12-month price history, its raw per-sale ledger, and its grading-census supply picture — on one page, with a freshness stamp asserting the data is current as of this page view. |
| **Priority** | Highest — thinnest end-to-end vertical slice; built first. |

### Outbound navigation

| Target | Line | Trigger |
|---|---|---|
| `Cardstock Home.dc.html` | :39, :43 | Logo/wordmark; "Home" nav tab |
| `Cardstock Screener.dc.html` | :44 | "Screener" nav tab |
| `Cardstock Charts.dc.html` | :45, :69, :117 | "Charts" nav tab; **"Open in Charts →"** primary button; "open in Charts →" link in the price-chart header |
| `Cardstock Binder.dc.html` | :46 | "Binder" nav tab |
| `Cardstock Browse.dc.html` | :47, :56 | "Browse" nav tab; breadcrumb root |
| `Cardstock Set.dc.html` | :56, :66 | Breadcrumb set crumb; set name in the subline |
| `Cardstock Character.dc.html` | :66 | Character name in the subline |
| `Cardstock Profile.dc.html` | :51 | Avatar chip (initial `O`) |

No nav tab is marked active on this screen — all five carry `border-bottom: 2px solid transparent` (:43–:47). The Card screen is a leaf, not a tab.

### Chrome

- **Nav bar** (:37–:52): sticky, `top: 0`, `z-index: 20`, height 48px, `--card` background, 1px `--line` bottom border, 24px gap, 20px horizontal padding. Logo+wordmark (24px SVG, "Cardstock" Inter 700/18px/-0.03em) → tab group → flex spacer → `<cardstock-search>` custom element → avatar.
- **Main** (:54): `max-width: 1480px`, centred, `padding: 14px 20px 28px`, vertical flex, `gap: 16px`, `font-size: 15px` root (:35).
- **Theme** (:33): `data-theme="dark"` applied when `localStorage['cardstock-theme'] === 'dark'`; `data-cvd="1"` when `localStorage['cardstock-cvd'] === '1'`. Both are read again inside the component as `PAL` (:264–:271) so JS-computed colours track the same two switches. CVD mode swaps the positive/negative pair to blue/orange (:25, :265, :267).
- **Fonts**: Inter (body), Inter Tight (headings, 600/700), JetBrains Mono (all numerics). Loaded from Google Fonts (:14).

---

## 2. Layout

Top-to-bottom, all inside `<main>` (:54). Vertical gap between blocks: 16px.

| # | Block | Lines | Container |
|---|---|---|---|
| 0 | Breadcrumb | :56 | bare div, 13.5px, `--mut2` |
| 1 | **Identity header** — art column + right column | :58–:99 | card panel, radius 10, padding 16, `display:flex; gap:18px` |
| 2 | Art lightbox (conditional overlay) | :101–:108 | `position: fixed`, `z-index: 200` |
| 3 | **Price chart** | :110–:149 | card panel, radius 10, padding `14px 16px` |
| 4 | **Sales ledger** | :151–:215 | card panel, radius 10, **no padding** (rows are full-bleed with internal 16px) |
| 5 | **Census pair** — Population \| Grading activity | :217–:250 | `display:grid; grid-template-columns: minmax(0,1fr) minmax(0,1fr); gap:16px` |
| 6 | **Freshness footer** | :252–:257 | `--mutbg` strip, 1px `--line`, radius 8, padding `9px 14px` |

> "Card panel" throughout = `background: var(--card); border: 1px solid var(--line); border-radius: 10px`.

### 2.1 Identity header — art column (:59–:61)

Fixed **217 × 300 px**, `flex-shrink: 0`, `cursor: zoom-in`, `title="Click to enlarge"`, `onClick → openArt`.
Contains `<image-slot id="art-umbreon" shape="rounded" radius="6" placeholder="card art 325×450">`.
The native art aspect ratio is **325 : 450** (0.7222); the 217×300 box matches it exactly.

### 2.2 Identity header — right column (:62–:98)

`flex: 1; min-width: 0; display:flex; flex-direction:column; justify-content: space-between; gap: 12px`. Three stacked rows:

**Row A — title + actions (:63–:83).** `display:flex; align-items:flex-start; gap:12px`.
- Left: `<h1>` card name (Inter Tight 700, 26px, `-0.01em`, margin 0) and beneath it the subline (:66, 14.5px `--mut`, `margin-top: 3px`): `{set link} · {number} · {character link}`.
- Flex spacer, then, left→right: **"Open in Charts →"** solid button (`--btn` bg, `#FFF` text, height 29, radius 6, 13.5px/600), **watchlist split-button + popover**, **binder button**. All `flex-shrink: 0`, all height 29px.

**Row B — tier strip (:84–:92).** `display: grid; grid-template-columns: repeat(6, 1fr); gap: 8px`. See §2.3.

**Row C — signal chips (:93–:97).** `display:flex; gap:4px; flex-wrap:wrap`, one chip per `sigChips` entry.

### 2.3 Tier strip — SIX cells, exactly (:84–:92, logic :395–:399)

The grid is hard-coded to **`repeat(6, 1fr)`** (:84). The `tierStrip` list is produced by taking the 19-entry
`BUCKETS` vocabulary (:322), **reversing it**, then filtering to a hard-coded allow-list of six
(`['PSA 10','Grade 9.5','Grade 9','Grade 8','Grade 7','Raw']`, :395). The 19-value vocabulary exists on this
screen only as the *ledger* grade vocabulary and as the *sort rank*; it is **never** rendered as a strip.

Resulting order and seeded content (index `i` is the position in `BUCKETS`; price = `TIERS[i]` :323, change = `CHG[i]` :324):

| Cell | Label | `i` | Price | 30d change |
|---|---|---|---|---|
| 1 | PSA 10 | 11 | `$1,486` | `+6.2%` |
| 2 | Grade 9.5 | 10 | `$1,010` | `+2.6%` |
| 3 | Grade 9 | 9 | `$842` | `+3.7%` |
| 4 | Grade 8 | 8 | `$710` | `+0.8%` |
| 5 | Grade 7 | 7 | `$620` | `+1.2%` |
| 6 | Raw | 0 | `$455` | `+3.9%` |

Order is **descending by grade rank, with Raw last** (a consequence of reversing `BUCKETS`, in which `Raw` is index 0).

Each cell (:86–:90): `--bg` background, 1px `--line`, radius 8, padding `9px 11px`, `title = t.tip`. Three stacked lines:

1. **Label** — 11px, weight 600, `letter-spacing: .06em`, `--mut2`, `text-transform: uppercase`, `white-space: nowrap`. (The source strings are mixed-case; CSS uppercases them, so `PSA 10` and `GRADE 9.5` both render upper.)
2. **Price** — JetBrains Mono, 18px, weight 700, `margin-top: 3px`. Formatted by `money()` (:334) = `'$' + Math.round(n).toLocaleString('en-US')` → no decimals, thousands separators.
3. **Change** — JetBrains Mono, 12px, `margin-top: 1px`, text `{chg} 30d` (literal trailing ` 30d`). Colour: `PAL.pos` if the string starts with `+`, otherwise `PAL.neg2` (:397).

Tooltip (:398): `` `${label} latest monthly price · ${chg} over 30 days` ``.

**Invariant:** the seeded tier-strip price equals the last element of that tier's 12-month chart array
(`P10[11]=1486`, `G95[11]=1010`, `G9[11]=842`, `G8[11]=710`, `G7[11]=620`, `RAW[11]=455`, :297–:302).
The strip is "latest monthly price per tier."

### 2.4 Price chart (:110–:149)

Header row (:111–:118): `display:flex; align-items:baseline; gap:14px; flex-wrap:wrap; margin-bottom:8px`.
`<h2>` **"Price · 12M · monthly"** (Inter Tight 600, 17px) → legend buttons (one per series, §5.4) → flex spacer → `open in Charts →` link (13px).

Plot row (:119–:143): `display:flex; gap:8px`.
- **Y-axis gutter** (:120–:123): 44px fixed, `position: relative`. Two absolutely-positioned mono 10.5px `--mut2` labels: `pcYMax` at `top: 2px`, `pcYMin` at `bottom: 2px`, both right-aligned. **Only two tick labels exist** — no gridlines, no intermediate ticks.
- **Plot area** (:124–:142): `flex: 1; min-width: 0; position: relative; cursor: crosshair`, `onMouseMove → pcMove`, `onMouseLeave → pcOut`.
  - `<svg width="100%" height="230" viewBox="0 0 800 230" preserveAspectRatio="none">` (:125) — the SVG **stretches horizontally** to fill; all strokes use `vector-effect="non-scaling-stroke"` so widths stay true.
  - One decorative horizontal rule at `y=115` in `--line4` (:126) — the geometric midpoint of the viewBox, **not** a data value or zero line.
  - Per visible series, two `<polyline>`s (:127–:130): a solid one and a `stroke-dasharray="4 4"` one.
  - Hollow current-month dot (:132) — see §2.4.1.
  - Hover crosshair + tooltip (:133–:141) — see §5.5.

X-axis labels (:144–:148): a `space-between` flex row, `margin-top: 4px`, `margin-left: 52px` (44px gutter + 8px gap, so the labels align to the plot area). **Three hard-coded mono 10.5px `--mut2` labels**: `Aug '25`, `Jan '26`, `Jul '26` — i.e. first point, midpoint (index 5), last point. There is no per-month tick.

#### 2.4.1 Dashed tail + hollow end dot

`pcSeries` (:414) emits, per visible series:
- `solid` = `pts(arr.slice(0, 11))` → points for indices **0–10** (the eleven closed months).
- `dash` = `pts(arr).split(' ').slice(10).join(' ')` → points **10 and 11** only, a two-point segment.

So the **final segment (month 11) is dashed** and everything before it is solid. On top of that sits one
absolutely-positioned dot (:132): 8×8px, `border-radius: 50%`, `background: var(--card)` (hollow),
`border: 1.5px solid var(--acc)`, `left: 100%`, `top: {{ pcHollowTop }}%`, `transform: translate(-50%,-50%)`,
`box-sizing: border-box`.

`pcHollowTop` = `yPct( (first visible series).arr[11] )` (:417) — the dot tracks the **first visible series in
`SER` order** (PSA 10 when visible; otherwise the next visible one down the list).

Dot tooltip (:132), verbatim:
> `Aug is month-to-date — the point firms up as the month's sales land, and finalizes when the month closes`

**Semantics:** the last of the 12 monthly points is the **current, incomplete month**. Dashed line + hollow dot = "provisional". Closed months are solid with no marker.

#### 2.4.2 Geometry (normative)

Given the visible series' concatenated values, `mn = min`, `mx = max` (:341):

```
x(k) = k / 11 * 800                       # k = 0..11, viewBox units
y(v) = 222 - (v - mn) / (mx - mn) * 212   # viewBox units; y=222 at mn, y=10 at mx
yPct(v) = y(v) / 230 * 100                # percentage of the 230px-tall plot box
```

Stroke width: **2** for PSA 10, **1.5** for every other series (:414).

Y-axis labels are **`'$' + n.toLocaleString('en-US')`** on the raw `mx`/`mn` (:415) — note this is *not* `money()`, so a non-integer bound would print decimals.

### 2.5 Sales ledger (:151–:215)

Four stacked bands inside one card panel:

1. **Toolbar** (:152–:191) — `display:flex; align-items:center; gap:10px; padding: 13px 16px 10px`. `<h2>` "Sales ledger" → filter chip group (`flex-wrap`) → flex spacer → row count (mono 12.5px `--mut`).
2. **Header row** (:192–:196) — `display:grid; grid-template-columns: {{ lgGridCols }}; gap:8px; padding: 7px 16px`, 1px `--line` top **and** bottom borders, `--bg` background, 11.5px/600, `letter-spacing:.05em`, `--mut2`, uppercase.
3. **Body** (:197–:211) — `sc-if hasSales` wrapping `sc-for ledgerRows`. Each row is a wrapper div with `border-bottom: 1px solid var(--line4)` containing an `sc-if r.isSale` grid of the **same** `lgGridCols` template, `gap:8px; padding: 6px 16px; align-items:center`.
4. **Empty state** (:212–:214) — `sc-if noSales`, `padding: 26px 16px`, centred, 13.5px `--mut2`.

**Column template** (:457):
```
lgGridCols = colW.date + 'px ' + colW.bucket + 'px ' + colW.price + 'px ' + colW.src + 'px minmax(160px, 1fr)'
```
Seeded defaults (:272): `date 96px · bucket 108px · price 92px · src 92px · minmax(160px, 1fr)`.
The same template string is applied to the header and every body row, so they stay aligned.

### 2.6 Census pair (:217–:250)

Two equal columns, `minmax(0, 1fr)` each, 16px gap. Both are card panels with `padding: 14px 16px`.

**Left — Population · current census** (:218–:233):
- Header `display:flex; align-items:baseline; gap:10px`: `<h2>` "Population · current census" + a 12.5px `--mut2` span "PSA + CGC · as of 2026-07-30".
- Bar row (:223–:231): `display:flex; align-items:flex-end; gap:14px; height:150px; margin-top:14px`. Each bar column is `flex:1`, full height, `justify-content:flex-end`, `align-items:center`, `gap:4px`, stacking **value label above → bar → grade label below**.
- Summary sentence (:232), 12.5px `--mut2`, `margin-top: 10px`.

**Right — Grading activity · PSA 10 slabs added** (:234–:249):
- Header: `<h2>` + an **observation-count badge** (:237): mono 11px/600, `letter-spacing:.06em`, colour `--warnInk`, background `rgba(176,127,26,0.12)`, radius 3, padding `1px 5px`, `cursor: help`.
- Bar row (:239–:247): same shape as the left panel but `gap: 10px`, and each bar column adds `position: relative` and each bar adds `box-sizing: border-box; border: {{ d.bd }}`.
- Summary sentence (:248), same typography as the left.

### 2.7 Freshness footer (:252–:257)

`display:flex; align-items:center; gap:16px`, `--mutbg` background, 1px `--line`, radius 8, padding `9px 14px`, 12.5px `--mut`. Content: refresh stamp → `·` → census stamp → `flex:1` spacer (content is left-aligned; the right side is deliberately empty).

---

## 3. Data contract

Everything the screen renders. **All monetary values pass through `money()` (:334) unless noted**:
`'$' + Math.round(n).toLocaleString('en-US')` → USD, whole dollars, comma thousands separators, no currency code, no cents.

### 3.1 Card identity

| Field | Line | Type / format | Seeded | Notes |
|---|---|---|---|---|
| Card name | :56, :65 | string | `Umbreon VMAX (Alt Art)` | Rendered twice: breadcrumb leaf and `<h1>`. Must match. |
| Set name | :56, :66 | string + link | `Evolving Skies` | Rendered twice: breadcrumb crumb and subline. Both link to the Set screen. |
| Card number | :66 | string, verbatim | `215/203` | Printed as-is between `·` separators; not zero-padded or reformatted. |
| Character name | :66 | string + link | `Umbreon` | Links to the Character screen. |
| Card art | :60, :104 | image, native 325×450 | `image-slot id="art-umbreon"` | Same asset id in thumbnail and lightbox. Thumbnail radius 6, lightbox radius 10. |

### 3.2 Tier strip — 6 rows

| Field | Line | Format | Notes |
|---|---|---|---|
| `t.label` | :87 | string, CSS-uppercased | One of the six fixed tiers, in the §2.3 order. |
| `t.price` | :88, :396 | `money(TIERS[i])` → `$1,486` | Latest monthly price for that tier. |
| `t.chg` | :89, :396 | pre-formatted signed percent string, 1 decimal, U+2212 minus for negatives (`−0.2%`, :324) | Rendered with a literal trailing ` 30d`. |
| `t.chgFg` | :89, :397 | colour | `PAL.pos` iff `chg[0] === '+'`; else `PAL.neg2`. A zero/flat value has no distinct branch — it depends entirely on the leading character. |
| `t.tip` | :86, :398 | string | `` `{label} latest monthly price · {chg} over 30 days` `` |

**Units:** price = USD. Change = **percent over 30 days** (not 1 month, not since last observation).

### 3.3 Signal chips (:400–:404) — 3 chips, fixed

| # | `i` (glyph) | `t` (text) | Tooltip | Palette |
|---|---|---|---|---|
| 1 | `▲` U+25B2 | `RS 94th` | `Relative strength vs market index, 3M: 94th percentile` | positive: bg `posBg(0.10)`, fg `pos`, border `posBg(0.3)` |
| 2 | `▲` U+25B2 | `MACD +` | `MACD (3,6,4) above signal since May 2026` | positive (same) |
| 3 | `●` U+25CF | `Most active · 41 sales/30d` | `Most-active card on the market in the last 30 days` | neutral: bg `PAL.mutbg`, fg `PAL.mut`, border `PAL.line` |

Rendered as `{{ sg.i }} {{ sg.t }}` (:95) — glyph, space, text. Mono 11.5px/500, padding `1px 6px`, radius 4, 1px border, `cursor: help`.

**Structure, not content:** the list is a variable-length array of `{i, t, tip, bg, fg, bd}`. The seed shows two "positive" chips and one "neutral"; a negative variant is not exercised but `PAL.neg*` exists for it.

### 3.4 Watchlist control (:375–:386)

| Field | Line | Format |
|---|---|---|
| `watchLabel` | :375 | `'+ Watchlist'` when member of zero lists; `'Watching ✓ '` when member of exactly one; `'Watching ✓ (N)'` when N > 1. A literal ` ▾` is appended in markup (:71). |
| `watchBg/Fg/Bd/HoverBg` | :376–:377 | Active (member of ≥1 list): bg `posBg(0.10)`, fg `PAL.pos`, border `posBg(0.35)`, hover unchanged. Inactive: bg `PAL.card`, fg `PAL.ink`, border `PAL.line`, hover `PAL.hov`. |
| `watchLists[].name` | :75, :380 | string |
| `watchLists[].count` | :75, :381 | integer as string, mono 11.5px `--mut2` — the list's card count **with this card's membership applied**. |
| `watchLists[].check` | :75, :382 | `'✓'` (U+2713) or `''` |
| `watchLists[].boxBg/boxBd` | :75, :383 | Checked: both `PAL.pos2`. Unchecked: bg `PAL.card`, border `PAL.line3`. 15×15px, 1.5px border, radius 4. |

Seeded lists: `Alt arts` (12), `Grails` (5), `Grading targets` (8) (:380). Initial membership: `{'Alt arts': true}` (:272).

> The count arithmetic is `base + (member ? 1 : 0) − (name === 'Alt arts' ? 1 : 0)` (:381). The `Alt arts` term is a seed correction — the seeded base already counted this card — not a rule. **The rule is: display the list's card count including this card iff it is a member.**

### 3.5 Binder control (:387–:390)

| Field | Line | Values |
|---|---|---|
| `binderLabel` | :387 | `'+ Binder'` / `'In binder ✓'` |
| `binderBg/Fg/Bd/HoverBg` | :388–:389 | Same positive/neutral pair as the watch button. |

Tooltip (:82): `Log a purchase of this card — opens the binder transaction form`.

### 3.6 Price chart

| Field | Line | Format / derivation |
|---|---|---|
| Series set | :326–:330 | Exactly **6**: PSA 10, Grade 9.5, Grade 9, Grade 8, Grade 7, Raw — in that order (reversed `BUCKETS` filtered to the allow-list). |
| `arr` (per series) | :297–:302, :326 | **12** monthly values, oldest→newest, USD numbers. |
| Months | :296 | 12 labels, `MMM ’YY` with a typographic apostrophe U+2019: `Aug ’25 … Jul ’26`. |
| Series colour `c` | :325, :328 | PSA 10 → `PAL.acc`; Grade 9.5 → `#6E4DB8`; Grade 9 → `PAL.warn`; Grade 8 → `#2E7F78`; Grade 7 → `#B0552E`; Raw → `PAL.mut2`. (Two of the six are theme-derived, four are fixed hexes.) |
| `ps.w` | :414 | `2` for PSA 10, `1.5` otherwise. |
| `ps.solid` | :414 | Polyline points for indices 0–10. |
| `ps.dash` | :414 | Polyline points for indices 10–11 (the provisional segment). |
| `pcYMax` / `pcYMin` | :121–:122, :415 | `'$' + max/min .toLocaleString('en-US')` across **all visible series, all 12 months**. |
| `pcHollowTop` | :132, :417 | `yPct(firstVisibleSeries.arr[11])`, fixed to 1 decimal, used as a `%`. |
| `lg.l` | :114 | Series label (identical to the tier name). |
| `lg.c` | :406 | Series colour when shown; `#D8D8D3` when hidden. |
| `lg.op` | :114, :406 | `1` when shown, `0.45` when hidden. |
| `lg.tip` | :407 | Hidden → `Show {label}`. Shown → `Hide {label} (y-axis rescales to what's visible)`. |
| `pcHovMonth` | :136, :421 | `MONTHS[hov]`, mono 11px `--mut2`. |
| `pcHovRows[]` | :137–:139, :416 | One row **per visible series**: `{l: label, v: money(value), c: series colour, w: 700 for PSA 10 else 400}`. Rendered as `{{ hr.l }} {{ hr.v }}`. |
| `pcHovLeft` | :134, :420 | `(hov / 11 * 100)` to 2 decimals, as a `%`. |

**Units:** USD per grade tier, monthly resolution, 12-month window, most recent month provisional.

### 3.7 Sales ledger

**Columns — FIVE. There is no "Listed" column.** (:458)

| # | Header | Sort key | Resize key | Cell format (line) |
|---|---|---|---|---|
| 1 | `Date` | `date` | `date` | `YYYY-MM-DD` verbatim; mono 12.5px, `--mut`, centred (:202) |
| 2 | `Grade bucket` | `bucket` | `bucket` | One of the 19 `BUCKETS` strings verbatim; mono 12.5px, `--ink`, centred (:203) |
| 3 | `Realized` | `price` | `price` | `money(p)`; mono **13.5px, weight 700**, centred, plus the listed-price affordance below (:204) |
| 4 | `Source` | `src` | `src` | lowercase marketplace slug; mono 12.5px, `--mut`, centred (:205) |
| 5 | `Listing title` | `title` | `src` ⚠ | 13px `--mut`, `white-space:nowrap; overflow:hidden; text-overflow:ellipsis`, `title` = full string (:206) |

⚠ Column 5's resize handle is wired to key `'src'` (:458), so dragging it resizes **Source**. And column 5 is a fluid `minmax(160px, 1fr)` track that the `colW` map cannot address at all. See §7 OQ-6.

**Row fields** (:352):

| Field | Format | Notes |
|---|---|---|
| `isSale` | bool | Always `true` for every emitted row. |
| `isSeam` | bool | Always `false`, and **never read by any markup** — see §4.7 / §6. |
| `date` | `YYYY-MM-DD` | Sorted as a raw string (ISO makes that lexicographically correct). |
| `bucket` | grade string | Must be a member of `BUCKETS` for the rank sort to work. |
| `price` | `money(p)` | Display string. |
| `pRaw` | number | The unformatted value; the numeric sort key. |
| `hasListed` | bool | `!!listed` — **computed but never rendered**; the three fields below carry the behaviour instead. |
| `listedTip` | `` `listed {money(l)} → sold {money(p)}` `` (U+2192 arrow) | `''` when no listed price. |
| `listedLine` | `'2px dotted #8F6614'` \| `'none'` | Applied as `border-bottom` on the Realized value (:204). |
| `listedCur` | `'help'` \| `'default'` | CSS cursor on the Realized value. |
| `src` | lowercase string | Seeded: `ebay`, `tcgplayer`, `goldin`, `pwcc`, `heritage`. |
| `title` | free string | Raw marketplace listing title, incl. emoji and smart quotes. |

**The Realized cell's listed-price affordance (:204) in full.** The span carries a set of *static* CSS
declarations — `text-decoration-line: underline; text-decoration-style: dotted; text-decoration-color:
transparent; text-decoration-thickness: 2px; text-underline-offset: 3px` — which are **inert by design**
(`transparent`). The visible treatment is the interpolated `border-bottom: {{ r.listedLine }}`, i.e.
**a 2px dotted amber (`#8F6614`) bottom border**, present only when a listed price exists, paired with
`cursor: help` and the `listed X → sold Y` tooltip. Confirms the brief: the Listed column was dropped and
folded into the Realized cell.

Note `#8F6614` is **hard-coded**, equal to the *light-theme* `PAL.warn` (:270). It does not follow the dark-theme `--warnInk` (`#D6A54A`). See §7 OQ-7.

**Row count** (:190, :456): `` `${filtered.length} sales shown` `` — mono 12.5px `--mut`. Reflects the **filtered** count, not the total.

**Grade vocabulary — 19 values** (:322), index order (this is also the sort rank, low→high):
`Raw · Grade 1 · Grade 2 · Grade 3 · Grade 4 · Grade 5 · Grade 6 · Grade 7 · Grade 8 · Grade 9 · Grade 9.5 · PSA 10 · CGC 10 · CGC 10 Prist. · TAG 10 · ACE 10 · SGC 10 · BGS 10 · BGS 10 Black`

**Filter chips**, in render order (:154–:188, :446–:453):

| Position | Control | Members |
|---|---|---|
| 1 | `All` chip | — |
| 2 | `PSA 10` chip | — |
| 3 | **`other 10s` group** popover | `CGC 10`, `CGC 10 Prist.`, `TAG 10`, `ACE 10`, `SGC 10`, `BGS 10`, `BGS 10 Black` (7) |
| 4–7 | `Grade 9.5`, `Grade 9`, `Grade 8`, `Grade 7` chips | — |
| 8 | **`Grade 1–6` group** popover | `Grade 6`, `Grade 5`, `Grade 4`, `Grade 3`, `Grade 2`, `Grade 1` (6, descending) |
| 9 | `Raw` chip | — |

7 direct chips + 2 group chips cover all 19 buckets exactly once. Chip pill styling (:346): selected → bg `PAL.acc`, fg `PAL.card`, border `PAL.acc`; unselected → bg `PAL.card`, fg `PAL.mut`, border `PAL.line`. Direct chips: height 24, radius 5, padding `0 9px`, mono 12px/600. Sub-chips: height 22, radius 4, padding `0 7px`, mono 11.5px/600.

Group chip label (:440): `(selectedMembers.length ? selectedMembers.join(', ') : baseLabel) + (open ? ' ▾' : ' ▸')` — U+25BE when open, U+25B8 when closed.
Popover headers (:162, :178): `Other graders' 10s` (typographic apostrophe) and `Grades 1–6` (en dash) — 10.5px/600, `letter-spacing:.07em`, uppercase, `--mut2`, `margin-bottom:5px`. Popover: absolute `top:26px; left:0`, `z-index:50`, `min-width:240px`, padding `8px 9px`, radius 8, `box-shadow: 0 8px 24px rgba(20,19,26,0.13)`.

Chip tooltips: `All` → `Show sales from every grade` (:434). Individual → `` `{Filter the ledger to|Stop filtering the ledger to} {bucket} sales` `` (:435). Group chips → static, on the button: `Filter the ledger to other graders' 10s — click to pick individual graders` (:159) and `Filter the ledger to grades 1–6 — click to pick individual grades` (:175).

### 3.8 Population panel (:367–:370, :465–:470)

Six bars, fixed order — **all PSA grades first, then all CGC grades**:

| # | `label` | `n` | Bar fill |
|---|---|---|---|
| 1 | `PSA 8` | 1,244 | `rgba(74, 99, 208, 0.55)` (accent blue) |
| 2 | `PSA 9` | 3,865 | accent blue |
| 3 | `PSA 10` | 1,479 | accent blue |
| 4 | `CGC 9` | 402 | `rgba(138, 138, 134, 0.45)` (grey) |
| 5 | `CGC 9.5` | 618 | grey |
| 6 | `CGC 10` | 187 | grey |

| Field | Line | Format |
|---|---|---|
| `p.n` | :226, :466 | `n.toLocaleString('en-US')` → `3,865`. Mono 11.5px `--mut`, above the bar. |
| `p.h` | :227, :467 | `Math.round(n / maxPop * 104) + 4` **px**. `maxPop = 4020` (:366) — a fixed headroom constant above the seeded max of 3,865, **not** derived from the data. Range 4–108px inside a 150px row. |
| `p.bg` | :468 | Hard-coded RGBA by grader (see table). Both are literal rgba strings, **not** theme tokens. |
| `p.label` | :228, :466 | Grade string, 11px `--mut2`, `nowrap`, below the bar. |
| `p.tip` | :227, :469 | `` `{grade}: {n} slabs in current census ({PSA|CGC})` `` |

Census date `2026-07-30` appears **twice**: the panel subtitle "PSA + CGC · as of 2026-07-30" (:221) and the footer stamp (:255). Both are hard-coded literals in the markup.

**Summary sentence** (:232) — hard-coded, not templated in the seed. Structure:
> `Gem rate ` **`27.3%`**` — of the last 90 days of PSA submissions, the share that came back 10. Drifting ` **`−0.4pp / 90d`**` (harder to gem = supply of fresh 10s slowing).`

- **Gem rate** — percent, 1 decimal, mono, `--mut`. Definition given inline: *share of PSA submissions in the last 90 days that graded 10*. Window: **90 days**, PSA only (CGC excluded).
- **Drift** — signed **percentage points** per 90 days, 1 decimal, suffix `pp / 90d`, U+2212 minus. Mono, colour `var(--pos, #157A50)`.
- **Branch rule (as rendered):** a *falling* gem rate is coloured **positive/green** and annotated `(harder to gem = supply of fresh 10s slowing)` — falling gem rate is bullish for holders of existing 10s. The inverse branch is not present in the HTML; see §7 OQ-9.

### 3.9 Grading-activity panel (:371–:373, :471–:477)

| Field | Line | Format |
|---|---|---|
| Observation badge text | :237 | `` `{N} OBS` `` — seeded `7 OBS`. |
| Badge tooltip | :237 | `Census history begins {Mon YYYY} — {N} observations so far; deltas need two` — seeded `Census history begins Jan 2026 — 7 observations so far; deltas need two`. |
| `deltas` | :371 | `[34, 41, 38, 52, 47, 61, 58]` — new PSA 10 slabs per month. |
| `dLabels` | :373 | `['Jan','Feb','Mar','Apr','May','Jun','Jul']` — 3-letter month, no year in the label. |
| `d.n` | :242, :472 | `'+' + n` → `+34`. Mono 11.5px `--mut`, above the bar. |
| `d.h` | :243, :473 | `Math.round(n / maxD * 104) + 4` px; `maxD = 61` (:372) = the **actual max** of `deltas`, so the tallest bar is always 108px. (Contrast with the population panel's fixed 4020.) |
| `d.bg` | :474 | `rgba(74, 99, 208, 0.55)` for **every** bar. |
| `d.bd` | :243, :475 | `'none'` for every bar. The `border` + `box-sizing: border-box` plumbing exists for a variant that never fires — most plausibly an outlined current/partial month. See §7 OQ-10. |
| `d.tip` | :243, :476 | `` `+{n} new PSA 10 slabs in {label} 2026` `` — year is hard-coded into the tooltip. |

**Summary sentence** (:248) — hard-coded. Structure:
> `Pace ` **`+58 / mo`**` and rising — ` **`331`**` new 10s since Jan, growing the census ` **`+29%`**` in 7 months (fresh supply working against the price).`

Derivations that check out against the seed:
- **`331`** = `sum(deltas)` = 34+41+38+52+47+61+58 = **331** ✓ — total new PSA 10s over the window.
- **`+29%`** = `331 / (currentPsa10Pop − 331)` = `331 / (1479 − 331)` = 28.8% → **+29%** ✓ — growth of the PSA 10 census over the window, rounded to a whole percent. Depends on the population panel's PSA 10 count (:368).
- **`7 months`** = `deltas.length` (and equals the `7 OBS` badge).
- **`+58 / mo`** = `deltas[last]` = 58, **not** the mean (which is 47.3). "Pace" = the most recent month's delta.
- **`and rising`** — a trend qualifier; the branch condition is not expressed in code. See §7 OQ-9.
- **`+29%`** is coloured `var(--neg2, #D64545)` — **red**, because supply growth is bearish. Parenthetical `(fresh supply working against the price)`.

**Sign convention across the two panels is deliberately inverted relative to the number's sign**: a negative gem-rate drift is green, a positive census-growth number is red. Both are coloured by *market meaning*, not by arithmetic sign.

### 3.10 Freshness footer (:252–:257)

| Field | Line | Value / format |
|---|---|---|
| Refresh label | :253 | `Sales & prices refreshed ` + **`just now`** (mono). |
| Refresh tooltip | :253 | `Opening a card page triggers a fresh scrape — the ledger and prices you see include sales up to right now` |
| Census label | :255 | `Census as of ` + **`2026-07-30`** (mono, `YYYY-MM-DD`). |
| Census tooltip | :255 | `Population data comes from PSA/CGC on their own publishing schedule — it can't be scraped on demand` |

The two stamps encode the product's central data-freshness claim: **sales and prices are on-demand fresh; census is not.** See §6 R-14.

---

## 4. States

### 4.1 Component state shape (:272)

```js
state = {
  bucket: 'All',            // vestigial — never read (superseded by bucketSel)
  watch: false,             // vestigial — never read (superseded by watchIn)
  binder: false,            // binder membership
  hov: null,                // hovered chart month index 0..11, or null
  watchOpen: false,         // watchlist popover
  watchIn: { 'Alt arts': true },  // map listName -> true
  colW: { date: 96, bucket: 108, price: 92, listed: 84, src: 92 }  // 'listed' is vestigial
}
```
Keys created lazily by `setState` and read with defaults: `artOpen` (:391), `pcOff` (:336), `bucketSel` (:349), `sortKey` (:353, default `'date'`), `sortDir` (:353, default `'desc'`), `lgTensOpen` / `lgLowsOpen` (:451).

**Three vestigial fields** — `state.bucket`, `state.watch`, and `colW.listed` — are written in the initialiser and never read. `colW.listed: 84` is the residue of the removed Listed column (§3.7).

### 4.2 Default / loaded state

Tier strip: 6 cells populated. Chart: PSA 10, Grade 9, Raw visible; Grade 9.5, Grade 8, Grade 7 hidden (`DEF_OFF`, :331–:332). Y-axis `$1,486` / `$368`. No hover. Ledger: `All` chip active, 16 rows, sorted Date ▾. Watch button active (`Watching ✓`, one list). Binder inactive. Lightbox closed. Both ledger popovers closed.

### 4.3 Chart states

| State | Trigger | Rendering |
|---|---|---|
| **Default visibility** | Page load, `state.pcOff` unset → falls back to `DEF_OFF` (:336) | PSA 10 + Grade 9 + Raw drawn; Grade 9.5 / Grade 8 / Grade 7 hidden. All six legend buttons render regardless. |
| **Series hidden** | Legend click on a visible series, when fewer than 5 are already hidden (:410) | Its polylines disappear; its legend swatch becomes `#D8D8D3` at `opacity: 0.45`; tooltip flips to `Show {label}`; y-axis recomputes. |
| **Series shown** | Legend click on a hidden series (:410, always allowed) | Reverse of the above. |
| **Last-series guard** | Legend click on the **only** visible series | **No-op.** `Object.keys(off).length < SER.length - 1` (i.e. `< 5`) fails, so the hide is silently refused. At least one series is always drawn. |
| **Y-axis rescaled** | Any visibility change (:339–:341) | `mn`/`mx` recomputed across the union of all visible series' 12 values; both axis labels and all polyline geometry change. |
| **Hover** | `mousemove` over the plot area (:418) | `hov = round(clamp((clientX − left)/width, 0, 1) × 11)` ∈ 0..11. Crosshair line at `hov/11 × 100%`; tooltip pinned at `top:8px; left:8px` of the plot area (**it does not follow the cursor**); one row per visible series. State only changes when the snapped index changes. |
| **Hover cleared** | `mouseleave` (:419) | `hov = null`; crosshair and tooltip unmount. |
| **Current month** | Always | Final segment dashed `4 4`; hollow accent-ringed dot at `left: 100%`, vertically tracking the first visible series' month-11 value. Present in every chart state. |
| **Degenerate y-range** | All visible values identical (`mx === mn`) | `(v−mn)/(mx−mn)` → `0/0` = `NaN`; polyline points become `NaN` and the line vanishes. **Unhandled.** See §7 OQ-4. |

### 4.4 Ledger filter states

| State | Trigger | Effect |
|---|---|---|
| **All** | `bucketSel` empty (default, or `All` chip clicked, :434) | `filtered = SALES`. `All` chip renders selected; every other chip unselected. Clicking `All` also force-closes both popovers. |
| **One or more buckets selected** | Any bucket chip or sub-chip clicked (:428–:432) | `filtered = SALES.filter(s => activeBuckets.includes(s.b))` — **OR across selections**, never AND. `All` chip de-selects automatically (it is selected iff `activeBuckets.length === 0`). |
| **Bucket de-selected** | Same chip clicked again (:430) | Key deleted from `bucketSel`. Removing the last one returns to the All state implicitly. |
| **Group collapsed, no members selected** | Default | Chip shows `other 10s ▸` / `Grade 1–6 ▸`, unselected pill. |
| **Group open** | Group chip clicked (:441) | Own `open` flag flips **and the sibling group is force-closed**. Chip label suffix becomes ` ▾`; the chip renders as **selected** while open even with no members chosen (:438). Popover lists all members as sub-chips. |
| **Group with members selected** | Sub-chip(s) picked | Chip label becomes the comma-joined selected member names (:440), e.g. `CGC 10, BGS 10 ▸`. Pill stays selected after the popover closes. |
| **Group closed** | `mouseleave` of the group wrapper (:158, :174, :452) **or** `mousedown` anywhere outside `[data-lg-pop]` (:277) | Both group flags set false together. |

### 4.5 Ledger sort states

| State | Trigger | Effect |
|---|---|---|
| **Default** | Load (`sortKey`/`sortDir` unset, :353) | `date` / `desc` — newest first. `Date ▾`. |
| **New column** | Click a different header label (:461) | `sortKey = column`, `sortDir = 'desc'`. **Desc-first, always** — the first click on any column sorts descending. |
| **Same column** | Click the active header again (:461) | Direction flips `desc → asc → desc`. |
| **Arrow** | Derived (:460) | `' ▾'` (U+25BE) when desc, `' ▴'` (U+25B4) when asc, appended to the header text. **Only the active column shows an arrow**; inactive columns show none, so there is no "sortable" affordance in the resting state beyond the `Click to sort` tooltip and hover colour. |

Comparators (:355–:364): `date` → string compare of the ISO date; `price` → numeric `pRaw`; `bucket` → `BUCKETS.indexOf(bucket)` (**grade rank**, Raw = 0 … BGS 10 Black = 18); `src` → string; anything else (`title`) → string. String comparisons use raw JS relational operators — **ordinal, case-sensitive**, not `localeCompare`.

**Tie-break** (:362): when the primary keys are equal, `a.date < b.date ? -1 : 1` — then the whole result is negated if `sortDir === 'desc'`. So ties fall to date, but the tie-break *inherits the active direction*: under `desc` ties resolve newest-first, under `asc` oldest-first. The comparator never returns `0`.

### 4.6 Ledger population states

| State | Condition | Rendering |
|---|---|---|
| **Has rows** | `hasSales` = `filtered.length > 0` (:197, :464) | Row list renders. |
| **True zero** | `noSales` = `filtered.length === 0` (:212, :464) | The row list is not rendered; a single centred block replaces it (:213), verbatim:<br>`No sales observed in this grade — that's a true zero: our scrapers visited and found none, not "no data".`<br>Padding `26px 16px`, 13.5px, `--mut2`. |

`hasSales` and `noSales` are exact complements, so exactly one branch always renders.

**Reachability.** In the prototype this state is only reachable by **filtering** to a bucket with no observed sales (e.g. selecting `BGS 10 Black`). In production it also covers a card with zero observed sales overall. **The copy is the same in both cases** and is an affirmative claim about scraper coverage — not a "data missing" message. The header row, chip toolbar, and `0 sales shown` count all still render above it.

> **Note the wording is grade-scoped** ("in this grade"). It reads correctly under a single-bucket filter and awkwardly under `All` or a multi-bucket selection. See §7 OQ-5.

### 4.7 Seam markers — DATA PRESENT, RENDERING ABSENT

`this.SEAMS` (:321) exists and is fully populated:
`{'PSA 10': '2026-03-14', 'Grade 9.5': '2026-05-11', 'Grade 9': '2026-04-02', 'Grade 8': '2026-06-02', 'Raw': '2026-03-20'}` — one per-grade date, presumably the coverage-start boundary for that grade's per-sale history.

Every row also carries an explicit `isSeam: false` flag (:352).

**But:** `SEAMS` is never read anywhere in `renderVals`, no row with `isSeam: true` is ever pushed, and the markup contains **no `sc-if r.isSeam` branch** — the row wrapper (:199–:209) has exactly one child, `sc-if r.isSale`. Verified by grep: the only three occurrences of `seam`/`SEAMS`/`isSeam` in the file are :321, :352, and nothing else.

**Conclusion: this prototype renders no seam markers, in any sort mode.** The scaffolding (a per-row discriminated union of sale-row vs seam-row, and the per-grade seam dates) is laid in and wired to nothing. Sort-mode gating is not expressed anywhere in the file. See §8 and §7 OQ-3.

### 4.8 Overlay / popover states

| State | Open trigger | Close triggers |
|---|---|---|
| **Lightbox** (`artOpen`, :101) | Click the art thumbnail (:59, :392) | Click the backdrop (:102, :393); click the ✕ button (:105, :393). **Clicks inside the image are swallowed** by `stopClick` (:103, :394). **No Escape-key handler exists.** |
| **Watchlist popover** (`watchOpen`, :72) | Click the watch button (:71, :378) — toggles | Click the watch button again; `mousedown` anywhere outside `[data-watch-pop]` (:276). Clicking a list row does **not** close it. |
| **`other 10s` popover** (`lgTensOpen`, :160) | Click its group chip (:159, :441) | Click it again; open the other group; `mouseleave` the wrapper (:158, :452); `mousedown` outside `[data-lg-pop]` (:277). |
| **`Grade 1–6` popover** (`lgLowsOpen`, :176) | Click its group chip (:175, :441) | Same set as above. |

The two group popovers are **mutually exclusive** (:441); the watchlist popover is independent of them.

### 4.9 Button binary states

| Control | Inactive | Active | Line |
|---|---|---|---|
| Watchlist | `+ Watchlist ▾`, card bg, ink fg, line border | `Watching ✓ ▾` / `Watching ✓ (N) ▾`, `posBg(0.10)` bg, `pos` fg, `posBg(0.35)` border | :375–:377 |
| Binder | `+ Binder`, card bg, ink fg, line border | `In binder ✓`, same positive palette | :387–:389 |
| Legend item | hidden: `#D8D8D3` swatch, `opacity .45` | shown: series colour, `opacity 1` | :406 |
| Filter chip | card bg / `mut` fg / `line` border | `acc` bg / `card` fg / `acc` border | :346 |

Both toggle buttons animate with `transition: background 0.15s, color 0.15s` (:71, :82).

### 4.10 Theme / accessibility states

| State | Trigger | Effect |
|---|---|---|
| **Light** (default) | no `data-theme` | Light `PAL` branch (:270). |
| **Dark** | `localStorage['cardstock-theme'] === 'dark'` (:33) | `data-theme="dark"` on `<html>`; CSS vars overridden (:27–:30) and the JS `PAL` takes the dark branch (:269). |
| **CVD off** (default) | — | Green/red positive/negative pair. |
| **CVD on** | `localStorage['cardstock-cvd'] === '1'` (:33) | `data-cvd="1"`; positive/negative become blue/orange (:25, :265, :267). Applies in both themes. |
| **Reduced motion** | `prefers-reduced-motion: reduce` (:23) | `animation-duration: 0.01ms !important` globally. Note this does **not** disable the `transition` properties on the toggle buttons. |
| **Focus visible** | Keyboard focus (:21) | `outline: 2px solid var(--acc); outline-offset: 1px; border-radius: 2px`. |

**Four elements are theme-blind** — they use hard-coded colours that do not switch: the Realized-cell amber `#8F6614` (:352), the hidden-legend grey `#D8D8D3` (:406), the population/delta bar fills `rgba(74,99,208,.55)` and `rgba(138,138,134,.45)` (:468, :474), and the chart's four fixed tier hexes (:325). See §7 OQ-7.

---

## 5. Interactions

Exhaustive list of every interactive element, in document order.

### 5.1 Nav and breadcrumb

| # | Element | Line | Consequence |
|---|---|---|---|
| 1 | Logo + wordmark link | :39 | → Home. `aria-label="Cardstock home"`. |
| 2 | Home / Screener / Charts / Binder / Browse tabs | :43–:47 | Navigate. None marked active here. |
| 3 | `<cardstock-search>` | :50 | Custom element from `cardstock-search.js` (:32). Out of scope for this screen — treat as the shared global search component. |
| 4 | Avatar `O` | :51 | → Profile. `aria-label="Account"`, `title="Profile & settings"`. |
| 5 | Breadcrumb `Browse` / set crumb | :56 | → Browse / Set. Leaf crumb is plain `--ink` text, not a link. |

### 5.2 Identity header

| # | Element | Line | Consequence |
|---|---|---|---|
| 6 | **Card art thumbnail** | :59 | `openArt()` → `artOpen = true` → lightbox mounts. `cursor: zoom-in`, `title="Click to enlarge"`. The whole 217×300 box is the hit target. |
| 7 | Set link / character link (subline) | :66 | → Set / Character. |
| 8 | **Open in Charts →** | :69 | → Charts. Primary solid button; hover `background: var(--accH); color: #FFFFFF; text-decoration: none`. |
| 9 | **Watchlist button** | :71 | `toggleWatch()` → flips `watchOpen`. **It does not add the card** — it only opens the picker. Label carries a ` ▾` affordance. Tooltip: `Follow this card on a watchlist — you pick which signals it tracks in Charts`. |
| 10 | Watchlist row (per list) | :75 | `wl.pick()` → toggles this card's membership in that list (:384). The check glyph, box fill, and the row's count all update immediately; the button label and colour update too. **The popover stays open**, so several lists can be toggled in one pass. Row hover `background: var(--hov)`. Tooltip: `Add this card to this watchlist`. |
| 11 | **+ New list…** | :78 | `newList()` (:386) → native `prompt('New list name')`. On a non-empty answer, adds `{name: true}` to `watchIn`, which increments the button's `(N)` count. ⚠ The new list does **not** appear as a row — `watchLists` is a hard-coded three-item array (:380). Tooltip: `Create another watchlist`. |
| 12 | **Binder button** | :82 | `addBinder()` (:390) → flips `state.binder`, swapping label and palette. The tooltip promises `opens the binder transaction form`; **the prototype only toggles a boolean** — no form, no navigation, no quantity/price capture. See §7 OQ-2. |
| 13 | Tier strip cells ×6 | :86 | **Not interactive.** No `onClick`, no cursor change — hover tooltip only (`t.tip`). |
| 14 | Signal chips ×3 | :95 | **Not interactive.** `cursor: help`, tooltip only. |

### 5.3 Lightbox

| # | Element | Line | Consequence |
|---|---|---|---|
| 15 | Backdrop | :102 | `closeArt()` → `artOpen = false`. `cursor: zoom-out`. Covers the viewport at `z-index: 200`. |
| 16 | Image container | :103 | `stopClick(e)` → `e.stopPropagation()` (:394). Clicking the art itself does **not** close. `cursor: default`. Sized `width: min(62vh, 78vw)` with `aspect-ratio: 325/450`. |
| 17 | ✕ close button | :105 | `closeArt()`. 30px circle at `top:-14px; right:-14px` (deliberately overlapping the art's corner), `aria-label="Close"`, `title="Close the full-size art"`, hover `background: var(--mutbg)`. |

Missing: Escape key, focus trap, `role="dialog"`, scroll lock. See §7 OQ-8.

### 5.4 Price chart

| # | Element | Line | Consequence |
|---|---|---|---|
| 18 | **Legend button** (×6, one per series) | :114 | `lg.toggle()` (:408–:412). Shows a hidden series unconditionally. Hides a visible series **only if fewer than 5 are already hidden** — otherwise silently no-ops, guaranteeing ≥1 visible line. Every toggle re-derives `mn`/`mx`, so **the y-axis and all line geometry rescale on every toggle** (the tooltip says so explicitly). Hover `color: var(--ink)`. |
| 19 | Plot area | :124 | `pcMove` (:418) → snaps the pointer to the nearest of the 12 month indices and sets `hov`; only calls `setState` when the index actually changes. `cursor: crosshair`. |
| 20 | Plot area (leave) | :124 | `pcOut` (:419) → `hov = null`. |
| 21 | Hollow current-month dot | :132 | **Not interactive** — tooltip only (`Aug is month-to-date …`). It sits at `left: 100%`, i.e. outside the plot area's right edge, and is not covered by the hover handler. |
| 22 | `open in Charts →` | :117 | → Charts. |

The hover tooltip and crosshair have `pointer-events: none` (:134) / are absolutely positioned out of the way (:135), so they never interfere with tracking.

### 5.5 Sales ledger

| # | Element | Line | Consequence |
|---|---|---|---|
| 23 | **`All` chip** | :156 | `setState({ bucketSel: {}, lgTensOpen: false, lgLowsOpen: false })` (:434) — clears every filter **and** closes both group popovers. |
| 24 | **Bucket chips** (`PSA 10`, `Grade 9.5`, `Grade 9`, `Grade 8`, `Grade 7`, `Raw`) | :156, :172, :187 | `togSel(bucket)` (:428) — toggles that bucket in `bucketSel`. Multi-select, OR semantics. |
| 25 | **`other 10s` group chip** | :159 | Toggles `lgTensOpen`, force-closes `lgLowsOpen` (:441). |
| 26 | **`Grade 1–6` group chip** | :175 | Toggles `lgLowsOpen`, force-closes `lgTensOpen` (:441). |
| 27 | Group sub-chips (7 + 6) | :165, :181 | `togSel(bucket)` (:444) — identical semantics to a top-level chip. The popover **stays open**, so multiple graders can be picked in one pass. |
| 28 | Group wrapper `mouseleave` | :158, :174 | `closeLgPops()` (:452) — closes both. |
| 29 | Document `mousedown` outside `[data-lg-pop]` | :277 | Closes both group popovers. |
| 30 | **Header label** (×5) | :194 | `lc.sort()` (:461) — desc-first on a new column, flip on the active one. `title="Click to sort"`, hover `color: var(--acc)`, `cursor: pointer`. The label span is `flex: 1; text-align: center`, so the whole cell width minus the grip is the hit target. |
| 31 | **Header resize grip `│`** (×5) | :194 | `onMouseDown → lc.rs` = `startResize(key)` (:282–:293). `preventDefault` + `stopPropagation` (so **resizing never triggers a sort**), then `mousemove`/`mouseup` listeners on `window`. New width = `clamp(startW + Δx, 40, 420)` px, written into `colW[key]`, which regenerates `lgGridCols` for the header **and every row simultaneously**. `cursor: col-resize`, colour `--line3` → `--acc` on hover, `margin-right: -6px`. ⚠ Grip #5 (`Listing title`) is keyed `'src'` and therefore resizes column 4. |
| 32 | Realized value with a listed price | :204 | **Not clickable** — `cursor: help` + `title="listed $X → sold $Y"`, marked by the 2px dotted amber bottom border. Rows without a listed price get `cursor: default`, `border-bottom: none`, and an empty tooltip. |
| 33 | Listing title cell | :206 | **Not clickable** — truncated with an ellipsis; `title` carries the full string. **There is no outbound link to the marketplace listing.** |
| 34 | Ledger rows | :199 | **Not clickable.** No row hover style, no selection, no expansion. |

### 5.6 Census panels

| # | Element | Line | Consequence |
|---|---|---|---|
| 35 | Population bars ×6 | :227 | **Not interactive** — tooltip only (`p.tip`). |
| 36 | `N OBS` badge | :237 | **Not interactive** — `cursor: help`, tooltip only. |
| 37 | Grading-activity bars ×7 | :243 | **Not interactive** — tooltip only (`d.tip`). |

Neither summary sentence contains a link or control.

### 5.7 Footer

| # | Element | Line | Consequence |
|---|---|---|---|
| 38 | Refresh stamp | :253 | **Not interactive** — tooltip only. There is **no manual "refresh now" button**; the refresh is implicit in the page visit. |
| 39 | Census stamp | :255 | **Not interactive** — tooltip only. |

### 5.8 Global listeners

Registered in `componentDidMount` (:273–:280), removed in `componentWillUnmount` (:281): a single `document` `mousedown` handler that (a) ignores events whose target has been detached (`!e.target.isConnected`), (b) closes the watchlist popover on a click outside `[data-watch-pop]`, (c) closes both ledger group popovers on a click outside `[data-lg-pop]`.

`startResize` registers `mousemove`/`mouseup` on `window` per drag and removes both on `mouseup` (:290).

---

## 6. Rules and invariants

**Structure**

- **R-1.** The tier strip is **exactly six cells**, in a hard-coded `repeat(6, 1fr)` grid (:84), in the order **PSA 10 → Grade 9.5 → Grade 9 → Grade 8 → Grade 7 → Raw** (:395). This is descending grade rank with Raw last. Never 19.
- **R-2.** The **same six tiers** drive both the tier strip (:395) and the price-chart series set (:327). The allow-list literal is duplicated verbatim in the two places; they must not drift.
- **R-3.** The **19-value grade vocabulary** (:322) governs only (a) the ledger's `Grade bucket` values, (b) the ledger's filter-chip coverage, and (c) the grade sort rank. It is never rendered as a strip or as chart series.
- **R-4.** The nine ledger filter controls partition all 19 buckets **exactly once**: 7 direct chips + 7 `other 10s` members + 6 `Grade 1–6` members = 20 slots, of which one is the non-bucket `All` chip → 19 buckets covered, no duplicates, no gaps (:322 vs :424–:425, :446–:450).
- **R-5.** The ledger has **five columns** — `Date`, `Grade bucket`, `Realized`, `Source`, `Listing title` (:458). There is **no `Listed` column**. Its removal is confirmed by the vestigial `colW.listed: 84` (:272), which `lgGridCols` (:457) does not consume.
- **R-6.** Header and body rows share one `grid-template-columns` string, `lgGridCols` (:192, :201), so a resize applies to both atomically.
- **R-7.** Column widths clamp to **40–420 px** (:287). The final `Listing title` track is `minmax(160px, 1fr)` and always absorbs the remainder.

**Chart**

- **R-8.** The chart window is **12 monthly points**, oldest→newest; index 11 is the **current, incomplete** month. Points 0–10 render solid; the 10→11 segment renders dashed `4 4` (:414); a hollow, accent-ringed dot marks point 11 (:132).
- **R-9.** **At least one series is always visible.** The hide branch is gated on `Object.keys(off).length < SER.length - 1` (:410), so the last visible series cannot be hidden. There is no all-hidden empty state.
- **R-10.** **The y-axis is derived, never fixed.** `mn`/`mx` are the min/max across the union of *visible* series over all 12 months (:339–:341), so every legend toggle rescales the axis labels and re-lays-out every line. This is stated to the user in the legend tooltip (:407).
- **R-11.** Default visibility is **PSA 10, Grade 9, Raw**; Grade 9.5, Grade 8, Grade 7 are **default-hidden** (`DEF_OFF`, :331–:332). The default is *not* stored in `state` — `state.pcOff` is undefined until the first toggle and `renderVals` falls back to `DEF_OFF` (:336).
- **R-12.** PSA 10 is visually privileged throughout: stroke width 2 vs 1.5 (:414), `font-weight: 700` in the hover tooltip (:416), first in every ordering, and the accent colour.

**Ledger**

- **R-13.** Sorting is **desc-first**: clicking a new column always starts descending; clicking the active column flips (:461). Default is `date`/`desc`.
- **R-14.** The **grade sort is by rank, not alphabetically** — `BUCKETS.indexOf(bucket)` (:354, :359), so `Grade 9.5` sorts above `Grade 9` and below `PSA 10`, and `Raw` is the floor.
- **R-15.** **Ties fall to date** (:362), but the tie-break is negated along with the primary comparison, so it follows the active direction. The comparator never returns 0 → the sort is deterministic and total.
- **R-16.** Bucket filters are **OR**, and an empty selection means "all" (:349–:350). `All` is a derived state (`activeBuckets.length === 0`), not a stored mode.
- **R-17.** The row count reads `{n} sales shown` and always reflects the **filtered** set (:456).
- **R-18.** A listed price never gets its own column; it becomes a **2px dotted amber bottom border + `cursor: help` + `listed X → sold Y` tooltip** on the Realized value (:204, :352). Rows without one render an identical cell minus all three.
- **R-19.** The empty state is a **true zero**, and the copy says so explicitly (:213): scrapers visited and found none. It is not a loading, error, or missing-data state — none of which exist on this screen.

**Census**

- **R-20.** Population bars are **PSA-first, then CGC**, and colour-coded by grader: accent blue `rgba(74,99,208,.55)` for PSA, grey `rgba(138,138,134,.45)` for CGC (:367–:369, :468). The split is by grading company, not by grade.
- **R-21.** Population bar heights scale against a **fixed constant** `maxPop = 4020` (:366), not the data max — so bars are comparable across cards but never fill the row. Grading-activity bars scale against `maxD` = the **actual** series max (:372), so the tallest always reaches 108px. **The two panels use different scaling rules.**
- **R-22.** Bar height = `round(n / max × 104) + 4` px in both panels — the `+4` guarantees a visible stub for a zero or near-zero value inside the 150px row.
- **R-23.** The grading-activity panel tracks **PSA 10 only** (heading :236, tooltip :476). CGC contributes to the population census but not to the activity deltas.
- **R-24.** The `N OBS` badge is a **data-sufficiency warning**, styled in the warn palette, and its tooltip states the rule: *deltas need two observations* (:237). It exists because census history is short.
- **R-25.** Summary-sentence colour follows **market meaning, not arithmetic sign**: a falling gem rate is green (:232), a rising census is red (:248).
- **R-26.** `331` and `+29%` in the activity sentence are derivable from rendered data — `sum(deltas)` and `sum(deltas) / (psa10Pop − sum(deltas))` — so the sentence and the two charts must be computed from one source or they will disagree.

**Freshness**

- **R-27.** **Opening a card page triggers a fresh scrape.** Stated in the footer tooltip (:253) and reflected in the stamp reading `just now`. Sales and prices are on-demand fresh.
- **R-28.** **Census is not on-demand.** The tooltip states it comes from PSA/CGC on their own publishing schedule and "can't be scraped on demand" (:255). It carries an explicit as-of date, shown in two places (:221, :255), which must agree.
- **R-29.** Consequently the page carries **two different freshness clocks** and must never present census numbers as being as fresh as the ledger.

**Formatting**

- **R-30.** All money uses `money()` (:334): `$`, `Math.round`, `en-US` grouping, no cents — **except** the two y-axis labels, which use `toLocaleString` on the raw bound (:415).
- **R-31.** All numerics render in **JetBrains Mono**; all prose in Inter; all headings in Inter Tight 600/700.
- **R-32.** Negative percentages use **U+2212 MINUS** (`−`), not a hyphen (:324, :232). Month labels use **U+2019** (`’`) (:296). Arrows are U+25B2/U+25CF (chips), U+25BE/U+25B4 (sort), U+25B8/U+25BE (group chips), U+2192 (listed tooltip), U+2713 (check).
- **R-33.** Dates render as **`YYYY-MM-DD`** everywhere (ledger rows :202, census as-of :221/:255) — never localised.

**Runtime**

- **R-34.** `hint-placeholder-count` / `hint-placeholder-val` are **design-time only** — `support.js:614`, `support.js:648` read them solely to render placeholders when the bound value is unavailable. They are **not** counts to implement. Several are already wrong against the seed: `sigChips` hints 2 but renders 3 (:94), `popBars` hints 5 but renders 6 (:224), `lgCols` hints 6 but renders 5 (:193).
- **R-35.** Dead code that must **not** be carried into the Blazor build: `state.bucket`, `state.watch`, `colW.listed` (:272); `hasListed` (:352); `counts` + `sum` (:347–:348, :426 — per-bucket sale counts are computed and never displayed); `pcHovP10`/`pcHovG9`/`pcHovRaw` (:422 — superseded by `pcHovRows`); the `CORE[b] || …` synthetic-series fallback (:329 — unreachable, all six tiers are in `CORE`); `SEAMS` and `isSeam` (:321, :352).

---

## 7. Open questions

| # | Question | Why it is open |
|---|---|---|
| **OQ-1** | **What is the route?** | The prototype is a flat file with no routing. Inbound links are filenames. Needs a decision — e.g. `/card/{id}` vs `/{set}/{number}` vs a slug — plus how the breadcrumb, set link, and character link derive their targets. Nothing in the HTML constrains this. |
| **OQ-2** | **What does the Binder button actually do?** | Its tooltip says "opens the binder transaction form" (:82) but the handler only flips a boolean (:390). The real behaviour — navigate to Binder, open a modal form, capture quantity/price/date — is unspecified here. Check `Cardstock Binder.dc.html`. |
| **OQ-3** | **What are seam markers supposed to look like, and when do they render?** | `SEAMS` holds a date per grade (:321) and rows carry `isSeam` (:352), but nothing renders them and no sort-mode gate exists. The brief says "only in date sort" — **the HTML does not implement that, or anything else**. Another prototype (Charts?) may show the intended treatment; otherwise this needs a design decision before build. |
| **OQ-4** | **What happens when a chart's visible series are all flat?** | `mx === mn` makes `(v−mn)/(mx−mn)` produce `NaN` and the polylines vanish (:342). Real single-tier views of a stable card will hit this. Needs a defined fallback (e.g. pad the range). |
| **OQ-5** | **Should the true-zero copy change under `All` or a multi-bucket filter?** | The string is grade-scoped — "No sales observed **in this grade**" (:213) — but it also renders when `All` is active on a card with no sales at all, and when several buckets are selected. |
| **OQ-6** | **Is the `Listing title` column meant to be resizable?** | Its grip is keyed `'src'` (:458) so it resizes Source, and its track is `minmax(160px, 1fr)` which `colW` cannot address. Either the grip should be removed from column 5 or the track should become fixed-width. |
| **OQ-7** | **Should the hard-coded colours be theme-aware?** | The Realized amber `#8F6614` (:352) is the *light* `--warnInk`; dark theme uses `#D6A54A` (:27). Also hard-coded: the hidden-legend grey `#D8D8D3` (:406) and both bar fills (:468, :474). Contrast in dark mode is unverified. |
| **OQ-8** | **Lightbox accessibility.** | No Escape handler, no focus trap, no `role="dialog"`/`aria-modal`, no scroll lock, and focus is not restored to the thumbnail on close (:101–:108). Add or accept? |
| **OQ-9** | **What are the other branches of the two summary sentences?** | Both are hard-coded prose (:232, :248). The HTML shows exactly one branch each: *falling* gem rate (green, "harder to gem = supply of fresh 10s slowing") and *rising* census (red, "fresh supply working against the price"). The rising-gem-rate, flat, and shrinking-census wordings do not exist anywhere in this file. Also unspecified: the threshold for the qualifier "and rising", and what renders when `N OBS < 2` (the badge tooltip implies deltas are then impossible). |
| **OQ-10** | **What was `d.bd` for?** | Every activity bar sets `border: 'none'` yet the markup keeps `box-sizing: border-box; border: {{ d.bd }}` (:243, :475). Most plausibly an outlined treatment for the current partial month, mirroring the chart's hollow dot — unconfirmed. |
| **OQ-11** | **Does the "+ New list…" flow belong here?** | It uses a native `prompt()` (:386) and the created list never appears in the popover because `watchLists` is a fixed array (:380). Real behaviour and the real create-list UI are undefined. |
| **OQ-12** | **Where do the signal chips come from, and how many can there be?** | Three are hard-coded (:400–:404) with no source, window, or overflow rule. RS is "vs market index, 3M"; MACD is "(3,6,4)"; "Most active" is a market-wide ranking. None of these are in the scraper's eight tables as such — all are derived. Wrapping is allowed (:93) but there is no cap. |
| **OQ-13** | **Is the census as-of date one value or two?** | It is hard-coded twice (:221, :255). Presumably one field; confirm they can never diverge (e.g. PSA and CGC published on different days). |
| **OQ-14** | **Which series should the hollow dot track when PSA 10 is hidden?** | It follows the *first visible* series (:417) but its ring is always `var(--acc)` (:132) — PSA 10's colour — so with PSA 10 hidden the dot sits on another series' line wearing the wrong colour. |
| **OQ-15** | **Should the ledger link out to the source listing?** | The listing title is truncated text with a `title` tooltip and no link (:206). The scraper knows the marketplace; whether it retains a URL is a data question for `../PokemonInvestBatch`. |
| **OQ-16** | **What is the ledger's time window and page size?** | 16 seeded sales span 2026-03-28 → 2026-08-01 with no pagination, no "load more", no date-range control, and no windowing copy anywhere (:151–:215). Unbounded is unlikely to be the intent. |
| **OQ-17** | **Off-by-one in the seeded chart months.** | The x-axis and `MONTHS` end at `Jul ’26` (:147, :296) but the hollow-dot tooltip says "**Aug** is month-to-date" (:132), the ledger's newest sale is `2026-08-01` (:304), and the census is as of `2026-07-30`. If "today" is Aug 2026 the 12-month window should be Sep ’25–Aug ’26. Almost certainly stale seed data — but confirm the rule is "12 months ending at the current, incomplete month". |
| **OQ-18** | **Tier-strip change window.** | Labelled `30d` (:89) and the tooltip says "over 30 days" (:398), but the underlying series is *monthly* (:112). 30 days ≠ one calendar month. Confirm which the number really is. |

---

## 8. Contradictions found

Every row below was checked against the HTML directly. **The HTML wins in all of them.**

| # | Claim | Source | What the HTML actually does |
|---|---|---|---|
| C-1 | The card page has a **"19-tier strip"** | `CardStock Mockup/HANDOFF.md` — see §8.1 | **Six cells.** The grid is literally `repeat(6, 1fr)` (:84) and `tierStrip` filters the 19-value `BUCKETS` down to a hard-coded six: PSA 10, Grade 9.5, Grade 9, Grade 8, Grade 7, Raw (:395). The 19 values exist only as the ledger's grade vocabulary and sort rank (:322). |
| C-2 | A **6-tier price preview chart** with PSA 10 / 9.5 / 9 / 8 / 7 / Ungraded, 9.5 / 8 / 7 default-hidden | `CardStock Mockup/DESIGN_NOTES.md:55` | **Confirmed on structure, one naming delta.** Six series in exactly that order (:327), and `DEF_OFF` hides exactly Grade 9.5, Grade 8, Grade 7 (:331–:332). But the sixth tier is labelled **`Raw`** on screen (:322, :395), not "Ungraded". "Ungraded" is the scraper's `PriceTier` enum name (`CLAUDE.md:92`); `Raw` is the display term. |
| C-3 | Only **6 price tiers** exist in the database | `DECISIONS.md` D-003 | **Consistent with the HTML.** Both the strip and the chart are limited to those six (:327, :395), and the 19-value list is used only where per-sale grade data exists — the ledger. This is the reconciliation of C-1: 19 *grade buckets* for sales, 6 *price tiers* for prices. |
| C-4 | The ledger has a **`Listed`** column | (implied by the dropped-column note) | **Dropped, and the residue proves it.** `lgCols` defines five columns with no Listed entry (:458); `lgGridCols` builds four fixed tracks from `date/bucket/price/src` plus a fluid last track (:457); yet `state.colW` still carries an unread `listed: 84` (:272). The listed price now surfaces as a **2px dotted amber `#8F6614` bottom border + `cursor: help` + `listed $X → sold $Y` tooltip** on the Realized cell (:204, :352). **Verified.** |
| C-5 | **Seam markers render only in date sort** | (task brief; `SEAMS` data at :321) | **No seam marker renders at all, in any sort mode.** `SEAMS` (:321) is never read; every row is stamped `isSeam: false` (:352); the row template has only an `sc-if r.isSale` branch and no seam branch (:199–:209); grep finds exactly two occurrences of the token in the file. The date-sort gate is not expressed anywhere. **Unimplemented in this prototype** — see OQ-3. |
| C-6 | Per-sale ledger begins **Apr 2025** | `HANDOFF.md` (per `CLAUDE.md:115`) | vs **Jul '26** per `DESIGN_NOTES.md:41` (per `CLAUDE.md:115`). **This screen settles neither** — it has no coverage-start copy. The closest artefact is `SEAMS` (:321), which puts per-grade seams in **Mar–Jun 2026**, and the seeded sales run 2026-03-28 → 2026-08-01 (:304–:319). Consistent with a 2026 start, not with Apr 2025 — but the seed is illustrative and this is a data question for `../PokemonInvestBatch`. Not settleable from this file. |

### 8.1 Note on doc citations

The claims in C-1, C-2, C-3, and C-6 are stated in the task brief with those attributions; C-2's line
(`DESIGN_NOTES.md:55`) and C-6's (`DESIGN_NOTES.md:41`, quoted at `CLAUDE.md:115`) are the ones given. The
`HANDOFF.md` "19-tier strip" and `DECISIONS.md` D-003 wordings should be re-quoted with exact line numbers
when those files are next touched. **None of that changes the verdicts** — every one of them was decided by
reading `Cardstock Card.dc.html`, which is Tier 1, and the line citations above are the receipts.

### 8.2 Internal inconsistencies within the prototype itself

Not doc-vs-HTML, but HTML-vs-HTML — flag them so they are not faithfully reproduced as bugs:

| # | Inconsistency | Lines |
|---|---|---|
| I-1 | The hollow-dot tooltip says "**Aug** is month-to-date" but the last chart month is `Jul ’26`. | :132 vs :147, :296 |
| I-2 | The badge says **7 observations** and its tooltip says "deltas need two", yet **7 delta bars** are drawn — 7 observations yield 6 deltas. | :237 vs :371 |
| I-3 | Column 5's resize grip is keyed `'src'`, so it resizes column 4. | :458 |
| I-4 | `+ New list…` adds a membership that no popover row can display. | :380 vs :386 |
| I-5 | The hollow dot follows the first *visible* series but is always drawn in the PSA 10 accent colour. | :132 vs :417 |
| I-6 | Per-bucket sale `counts` are computed and a `sum()` helper is defined, but no chip ever shows a count. | :347–:348, :426 |
| I-7 | `hint-placeholder-count` values disagree with the seeded lists in three places (2 vs 3 chips, 5 vs 6 bars, 6 vs 5 columns). | :94, :224, :193 |
| I-8 | The Realized cell sets five `text-decoration-*` properties that are inert (`text-decoration-color: transparent`); the visible rule is `border-bottom`. | :204 |
