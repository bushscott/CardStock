# Screen spec — Card

> **Source of truth:** `CardStock Mockup/Cardstock Card.dc.html` (483 lines), read in full 2026-08-10.
> Every line citation below is `Cardstock Card.dc.html:NNN` unless another file is named.
> Markdown docs are Tier 2/3 and were **not** used to fill gaps. Where they disagree, see §8.
> Seeded values (Umbreon VMAX Alt Art, `$1,486`, 16 sales…) are **illustrative**. What is normative is the
> structure, the derivation rules, and the complete state space.
>
> **Amended 2026-08-13 (D-092):** for the lower identity-header region **only** — the tier strip and
> the chip row — the owner rework `CardStock Mockup/Cardstock Card.rework-2026-08-13.html`
> supersedes the frozen prototype. It is a bundled export: the semantic content is JS state on its
> final content line — search anchors `tierStrip`, `sigRows`, `sigCount`, `quietMore`. See §2.2
> (amended), §2.3 (amended), §2.3.2 (new), §3.3 (amended), §8 C-25. Everything else on this page
> keeps `Cardstock Card.dc.html` + this spec as authority.

---

## 1. Identity

| | |
|---|---|
| **Screen name** | Card (`data-screen-label="Card"`, :35) |
| **Prototype file** | `CardStock Mockup/Cardstock Card.dc.html` |
| **Route** | **`/card/{id}`** — `CardStock Mockup/HANDOFF.md:76`, `…/uploads/CARDSTOCK_UI_SPEC_v1.md:119` and `:217`. Tier 2/3 and **not confirmable from the HTML**: the prototype is a flat file and every inbound link is a bare `Cardstock Card.dc.html` with no id. Not contradicted either. See OQ-1. **Confirmed 2026-08-13 (OQ-1 resolved) — Phase 2 spec §1/§2:** `/card/{id}` ships as designed; `{id}` is `cards.id`, PriceCharting's own product id, never locally generated (`../PokemonInvestBatch/DATA_MODEL.md:34`, `:162`, e.g. `630417`). |
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
| `Cardstock Character.dc.html` | :66 | Character name in the subline. **Phase 2: this segment does not render at all** — no species field exists yet; see §3.1.1 (D-079, D-084.10). **Amended 2026-08-13 (D-087): the slot renders a deferred placeholder label — see §3.1.1.** |
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
- Left: `<h1>` card name (Inter Tight 700, 26px, `-0.01em`, margin 0) and beneath it the subline (:66, 14.5px `--mut`, `margin-top: 3px`): `{set link} · {number} · {character link}` in the prototype — **Phase 2 ships two segments, not three; see §3.1.1.**
- Flex spacer, then, left→right: **"Open in Charts →"** solid button (`--btn` bg, `#FFF` text, height 29, radius 6, 13.5px/600), **watchlist split-button + popover**, **binder button**. All `flex-shrink: 0`, all height 29px.

**Row B — tier strip (:84–:92).** `display: grid; grid-template-columns: repeat(6, 1fr); gap: 8px`. See §2.3.

**Row C — signal chips (:93–:97).** `display:flex; gap:4px; flex-wrap:wrap`, one chip per `sigChips` entry.

> **Amended 2026-08-13 (D-092) — Rows B and C are replaced by one wrapping row.** The lower
> identity header becomes a single `display:flex; flex-wrap:wrap; gap:14px; align-items:stretch`
> container of two blocks: the **tier tile grid** (§2.3, amended) and the **Signals panel**
> (§2.3.2, new). Source: the rework file's identity-header section (the flex container preceding
> anchor `tierStrip`). The reserved 28px badge row between Row A and this row (§4.2.1, D-077) is
> unchanged.

### 2.3 Tier strip — SIX cells, exactly (:84–:92, logic :395–:399)

> **Amended 2026-08-13 (D-092) — the strip becomes a 3×2 grid of square tiles; the six-cell
> selection and order stand.** Geometry from the rework (the grid container preceding anchor
> `tierStrip`):
> - **Container:** `display:grid; grid-template-columns: repeat(3, 100px); grid-auto-rows: 100px;
>   gap: 8px; flex: 0 0 auto` — six square tiles, PSA 10 → Raw in the order below, reading
>   left-to-right, wrapping after the third.
> - **Tile:** `--bg` fill, `1px solid --line`, radius 8, padding `10px 11px`, `display:flex;
>   flex-direction:column; justify-content:space-between; box-sizing:border-box`.
> - **Label:** 11px/600, letter-spacing .06em, `--mut2`, uppercase, `white-space:nowrap` —
>   unchanged from the strip. **The ◌ month-to-date glyph stays**, seated after the label exactly
>   as in the strip (owner ruling, 2026-08-13): the rework's own tile tooltip (*"latest monthly
>   price · +6.2% over 30 days"*) and its missing ◌ are seed-copy regressions — build the new tile
>   geometry with §2.3.1's glyph and two-tooltip table, which stand in full.
> - **Price:** JetBrains Mono **19px**/700, `line-height: 1` (up from the strip's 18px).
> - **Change:** JetBrains Mono 12px, text `{chg} 30d`, colour by the formatted leading sign
>   exactly as §3.2.
> - **Absence unchanged:** `—` for price and change (D-075) — a dash, never a countdown.
>
> The paragraphs below stand as the frozen prototype's record (and remain normative for the
> six-cell selection, order, derivation, and §3.2's data contract).

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

#### 2.3.1 Corrected — the six prices are month-to-date and must say so (D-077)

**Build this, not the prototype's version.** The invariant above is the finding: index 11 is the
**current, incomplete month** (R-8), so every strip price *is* a month-to-date figure. The chart marks
that same number with a dashed segment and a hollow dot (§2.4.1); the strip marks it with nothing and
calls it *"latest monthly price"* — the phrasing a finished number would use. Same value, two honesty
treatments, one screen. Logged as **C-22** in §8.

**Add a `◌` to each cell's label row.** `brand.md` §4.2 already assigns `◌` the meaning *"current month
provisional on sparklines"* — this is that meaning, not a new glyph. It is text, so it survives
colourblind mode (which swaps hue only) and is read aloud.

- Placement: label row, right-aligned against the tier name. `--mut2` grey, ~12px.
- **It belongs to the row, not the layout.** Present only while the rendered month is the current one;
  gone once that month closes. Never a permanent decoration.
- **Keyboard-reachable.** A `title` on a bare `<span>` shows on mouse hover only. It needs
  `tabindex="0"` and an `aria-label` carrying the same sentence, or keyboard users never see it.

**Two tooltips, two questions.** The cell explains the value; the glyph explains the symbol.

| Target | Copy |
|---|---|
| The cell | `{label} — {Month} month-to-date. {chg} over 30 days.` |
| The `◌` | `Month-to-date. {Month}'s average is still forming — it firms up as the month's sales land, and finalizes when the month closes.` |

**`{Month}` is computed from the rendered row, never authored.** Hard-coding it is the mistake OQ-17
already caught once, where the prototype's tooltip says "Aug" while its axis ends at Jul '26.

#### 2.3.2 Signals panel (new 2026-08-13, D-092 — replaces Row C's chip row)

Source: the rework file, the panel container between anchors `tierStrip` and `quietMore`. Sits
beside the tier tile grid in §2.2's wrapping row; wraps beneath it when narrow.

**Container:** `flex: 1 1 300px; min-width: 0; box-sizing: border-box`, tile dress (`--bg` fill,
`1px solid --line`, radius 8), padding `10px 12px 9px`, `display:flex; flex-direction:column;
gap: 8px`.

**Header row** — `display:flex; align-items:baseline; gap:8px`:
- `SIGNALS` — 11px/600, letter-spacing .06em, `--mut2`, uppercase.
- Flex spacer.
- **Count** — JetBrains Mono 11px, `--mut2`, `cursor:help`, text
  `` `{evaluated} evaluated · {firing} firing` `` — both numbers computed, never authored:
  `evaluated` = every row the engine emitted (locked rows included — they were evaluated and found
  locked), `firing` = rows in the firing state. Tooltip verbatim:
  > Every chip-eligible signal is evaluated on this card automatically — nothing here is opted
  > into. Bollinger, beta, discount-to-list, and seasonality are excluded: visualization-grade,
  > descriptive, or below coverage.

**Rows grid** — `display:grid; grid-template-columns: repeat(auto-fit, minmax(196px, 1fr));
gap: 2px 18px; flex: 1; align-content: start`. **Unbounded** (owner ruling, 2026-08-13): every
evaluated signal renders as a row — the rework's eight rows are a sample, not a cap. More than
eight signals can fire; every firing row always renders; the auto-fit grid wraps into columns as
width allows.

**Row** — `title = {tip}`, `display:flex; align-items:baseline; gap:7px; padding: 3px 0;
border-bottom: 1px solid --line4; cursor:help`:
- **Glyph** — JetBrains Mono 11px, fixed `width: 9px`, `flex-shrink: 0`, in the row's fg. The
  glyph is text in the row's foreground colour — colour never carries the state alone.
- **Name** — 12.5px, `white-space:nowrap; overflow:hidden; text-overflow:ellipsis`. Ink when
  firing or neutral; `--mut` when quiet or below-floor; `--mut2` when locked.
- Flex spacer.
- **Value** — JetBrains Mono 11.5px/500, `white-space:nowrap`, in the row's fg.

**Row states — the five-state doctrine mapped onto signals.** Every evaluated signal renders in
exactly one:

| State | Glyph | Row fg | Name | Value |
|---|---|---|---|---|
| **Firing** | `▲`/`▼` in the tone colour; a caution band fires `–` in amber | tone colour (`--pos` / `--neg2` / warn) | ink | the evidence number (e.g. `+18%`, `above signal`, `+1.8σ`, `.91`, `−28%`, `+ cross 2mo`) |
| **Quiet** | `–` (U+2013) | `--mut2` | `--mut` | the live reading (e.g. `+13%`, `58`, `×3.1`) — computed, inside bands |
| **Below floor** | `–` (U+2013) | `--mut2` | `--mut` | `—` — tooltip names the floor and what is present, never a number |
| **Neutral** | `●` | `--mut` | ink | the reading (liquidity/state signals are never directional) |
| **Locked** | `◌` | `--mut2` | `--mut2` | names the unlock (`locked` / `unlocks {date}`) — tooltip names the substrate |

**Substrate-less rows render as locked states, never seed numbers** (owner ruling, 2026-08-13).
The rework seeds `RS 94th` percentiles, `Pop Δ +0.4%`, and `churn · 48 recorded` — none of those
substrates exist. Phase 2's locked rows and their exact copy:

| Row | Value | Tooltip |
|---|---|---|
| `RS vs index 3M` | `locked` | `Relative strength needs the market index — it arrives with the worker phase` |
| `Pop Δ 60d` | `locked` | `Needs census deltas; observations count from 2026-09-01 — deltas need two` |
| `Churn 30d` | `unlocks 2026-10-31` | `Needs 60+ post-seam days · {n} recorded` — n = max(0, days since 2026-09-01), computed from the clock, 0 until the floor |

Churn's unlock derives from the D-033 floor: 60 post-seam days from 2026-09-01 → first satisfied
2026-10-31. The rework's `unlocks 25 Aug` / `48 recorded` seed pair assumes a pre-floor history
that D-033 excludes.

**Footer row** — `display:flex; align-items:baseline; gap:10px`:
- `` `+{n} quiet` `` — JetBrains Mono 11px, `--mut2`, `cursor:help`, tooltip verbatim: `The
  remaining signals are inside their quiet bands or below their sufficiency floor.` **Rendered
  only when n > 0**, i.e. only if the panel ever folds rows away. Phase 2 folds nothing — the
  element does not render at all yet.
- Flex spacer.
- `all signals in Charts →` — a **DeferredControl** (Charts doesn't exist; tooltip `Charts
  arrives in a later phase`), JetBrains Mono 11px. The rework's live link
  (`Cardstock Charts.dc.html#signals`) is the eventual destination, recorded for the Charts
  phase.

### 2.4 Price chart (:110–:149)

> **Engine ruling (D-084.7/9):** built on TradingView Lightweight Charts via the project wrapper.
> Palette = brand.md §2.6 `TIER_COLORS` (C-20 → resolved, Charts values win). Axes stay
> mockup-minimal via wrapper overlay. Gaps render as native whitespace breaks; an isolated month
> renders a point marker (OQ-4 → resolved by LWC autoscale; OQ-14 → the hollow dot tracks the first
> visible series *with a current-month value* and wears that series' colour; OQ-17 → the window is
> 12 months ending at the current month).

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

> **Amended 2026-08-13 (D-090) — a twelve-month window, paged on the client.**
> **⚠ The time window was superseded the same day by D-091 (next box) — the client paging and the
> pager stand.** Kept for the audit trail:
> - **The query is capped at a rolling twelve months** (`sold_on >= today − 12 months`, cutoff
>   inclusive), enforced in `CardSalesReader` so older rows never leave the database. No server
>   pagination — measured corpus max is 717 rows/card lifetime, and the window bounds growth to a
>   card's annual sales rate.
> - **The display pages at 50 rows client-side** ("looking at 600 rows is dumb"); filters and
>   sorts still act instantly on the complete window, and any filter/sort change snaps back to
>   page one. Pager renders only when the set overflows a page: `‹ Prev · Rows 1–50 of N · Next ›`,
>   ends honestly disabled.
> - **Copy scopes to the window:** panel title `Sales ledger · 12M` (the chart's idiom); count
>   line `{n} sales · last 12 months` with tooltip "Shows the last 12 months of captured sales.
>   Each grade is complete from its own first captured sale; nothing earlier was observable.";
>   every empty state gains "in the last 12 months" so the true-zero claim never denies older
>   sales beyond the window.
> - **Tripwire, recorded not built:** keyset pagination becomes its own designed task only if a
>   card's twelve-month window ever crosses ~5,000 rows or real sustained multi-user load arrives.

> **Amended again 2026-08-13 (D-091) — newest 300 per grade bucket, lifetime; the time window is
> retired.** The twelve-month cap broke the rare buckets: PSA 10 Charizard holds exactly 30
> lifetime sales reaching to Dec 2023, and a flat window hid most of a slow bucket's life while
> fast buckets are what actually grow without bound. So the cap moved onto the axis the ledger is
> already organized around:
> - **The query ships the newest `BucketCap = 300` rows per `grade_tier`, lifetime, no time
>   window** (`ICardSalesReader.BucketCap`; a correlated `SelectMany`/LATERAL in
>   `CardSalesReader`). A bucket truncates only once its captured history exceeds 300 — rare
>   buckets show their complete lives; the hard ceiling is 19 × 300 rows however long the crawler
>   runs. The database keeps everything; this is a read window.
> - **Copy:** the panel title reverts to `Sales ledger`; the count line reads
>   `{n} sales · newest 300 per grade` with tooltip "Grades with 300 or fewer captured sales show
>   their complete history; busier grades show their newest 300. Each grade is complete from its
>   own first captured sale; nothing earlier was observable." The true-zero empty states revert to
>   their unscoped form — under a per-bucket cap, an empty selection means zero captured sales
>   ever.
> - The client paging (D-090) stands, resized to **25 rows per page** (owner, same session). Revisit conditions for the query shape and
>   deep-history-on-demand were deliberately **not** authored yet (owner: "don't do the tripwires
>   yet").

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

**Amended 2026-08-13 (D-087 applied) — the metric slots render as states.** The single degrade line
("census history too young to compute pace") is replaced by the mockup's two metric rows rendered
as slots: `Gem rate` and `Pace`, each carrying a `LOW DATA` chip (the OBS badge's warn recipe) and
the note `needs census deltas; observations count from 2026-09-01, {n} so far — deltas need two`.
States, never placeholder numbers — the computed sentences return with the worker phase's delta
substrate.

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
| Card number | :66 | string, verbatim | `215/203` | Printed as-is between `·` separators; not zero-padded or reformatted. **Phase 2: ships `#num` until enrichment lands, then `215/203` — see §3.1.1.** |
| Character name | :66 | string + link | `Umbreon` | Links to the Character screen. **Phase 2: this segment does not render at all** — no species field exists yet; see §3.1.1 (D-079, D-084.10). **Amended 2026-08-13 (D-087): the slot renders the deferred placeholder `Pokémon name` — see §3.1.1.** |
| Card art | :60, :104 | image, native 325×450 | `image-slot id="art-umbreon"` | Same asset id in thumbnail and lightbox. Thumbnail radius 6, lightbox radius 10. |

#### 3.1.1 Corrected — the subline drops species in Phase 2 (D-079, D-084.10)

**Build this, not the prototype's version.** The prototype's subline is three segments —
`{set link} · {number} · {character link}` (:66) — but the scraper's own columns carry no species
field, and the TCGdex enrichment researched to fill the identity gap (D-079) was scoped to
**collector number and official set size only**; species is out of Phase 2 and out of that
enrichment entirely, reserved for a future Pokédex phase (D-084.10: *"in another phase, we will have
to create a Pokedex, and it will belong in there"*). The Phase 2 identity DTO carries no `Species`
field, so the subline is **two** segments, not three:

| When | Subline |
|---|---|
| Today (number parsed from the title; no `SetSize` yet) | `{set} · #num` |
| After the TCGdex enrichment lands (§9) | `{set} · 215/203` |

The Character-name segment and its link return with the Pokédex phase, alongside the species data
itself (D-084.10). This is a different case from the deferred-disabled nav tabs/search/watchlist/
binder controls (D-084.1) — those defer a *feature* that already has data; this defers a *field*
that does not exist yet, so there is nothing to render, disabled or otherwise. See §8 C-24 for the
route-id, delisted-chip, and not-found additions from the same design pass.

**Amended 2026-08-13 (D-087) — the slot ships, holding a placeholder.** The owner extended the
deferred-UI ruling to data slots: *"Even if you don't have the functionality wired up, put the UI in
with placeholder controls or labels."* The subline therefore renders **three** segments again —
`{set} · #4 · Pokémon name` — where the third is a deferred control with label `Pokémon name` and
tooltip `The Pokémon's name arrives with the Pokédex phase`, wearing the pending tone (`--mut2`,
the `card art pending` precedent) rather than the accent, so it can never be read as a real name.
The paragraph above stands as the pre-amendment record; D-084.10 is unchanged on sourcing — real
names and the Character link still arrive with the Pokédex phase's tag table, and a name is never
guessed from the title string.

### 3.2 Tier strip — 6 rows

| Field | Line | Format | Notes |
|---|---|---|---|
| `t.label` | :87 | string, CSS-uppercased | One of the six fixed tiers, in the §2.3 order. |
| `t.price` | :88, :396 | `money(TIERS[i])` → `$1,486` | Latest monthly price for that tier. |
| `t.chg` | :89, :396 | pre-formatted signed percent string, 1 decimal, U+2212 minus for negatives (`−0.2%`, :324) | Rendered with a literal trailing ` 30d`. |
| `t.chgFg` | :89, :397 | colour | `PAL.pos` iff `chg[0] === '+'`; else `PAL.neg2`. A zero/flat value has no distinct branch — it depends entirely on the leading character. |
| `t.tip` | :86, :398 | string | `` `{label} latest monthly price · {chg} over 30 days` `` |

**Units:** price = USD. Change = **percent over 30 days** (not 1 month, not since last observation).

**Absence renders a dash:** a tier with no series (`NoPriceSeries`) or a stale newest month
(`PriceStale`) shows `—` for price; `ChangeInsufficient` shows `—` for change (Phase 1 domain types;
D-075: a dash, never a countdown).

### 3.3 Signal chips (:400–:404) — 3 chips, fixed

> **Amended 2026-08-13 (D-092) — the chip-row *presentation* is superseded by the Signals panel
> (§2.3.2).** The chip ENGINE survives in full: the firing rules, floors, anchor-tier rule and
> priority order of §3.3.1 stand, and a firing chip becomes a firing row. What retires with the
> row: the cap-4 / `+N more` machinery (the panel is unbounded and folds nothing in Phase 2) and
> the chip pill dress. Two roster changes land with the panel, recorded in `docs/signals.md`
> (D-092): **RSI (6)** joins the computed set (caution fires at ≥ 70, positive at ≤ 30, else quiet
> with the value; floor 7 closed months), and **tier spread 10/9 is redefined** — the row always
> shows the current ratio `×{r:0.0}` and fires ▼ when the ratio ≥ 4 **or** it moved ≥ 20% in
> either direction vs 6 closed months earlier, superseding §3.3.1's compression-only trigger.
> This ships D-088's parked full-status surface. The paragraphs below stand as the frozen
> prototype's record.

| # | `i` (glyph) | `t` (text) | Tooltip | Palette |
|---|---|---|---|---|
| 1 | `▲` U+25B2 | `RS 94th` | `Relative strength vs market index, 3M: 94th percentile` | positive: bg `posBg(0.10)`, fg `pos`, border `posBg(0.3)` |
| 2 | `▲` U+25B2 | `MACD +` | `MACD (3,6,4) above signal since May 2026` | positive (same) |
| 3 | `●` U+25CF | `Most active · 41 sales/30d` | `Most-active card on the market in the last 30 days` | neutral: bg `PAL.mutbg`, fg `PAL.mut`, border `PAL.line` |

Rendered as `{{ sg.i }} {{ sg.t }}` (:95) — glyph, space, text. Mono 11.5px/500, padding `1px 6px`, radius 4, 1px border, `cursor: help`.

**Structure, not content:** the list is a variable-length array of `{i, t, tip, bg, fg, bd}`. The seed shows two "positive" chips and one "neutral"; a negative variant is not exercised but `PAL.neg*` exists for it.

**The selection logic is absent from the HTML but specified in Tier 2.** `sigChips` is a static literal — no firing test, no priority sort, no cap, no overflow control. The documented rules, which the three seeded chips match exactly, are:

- **Firing-only, cap 4, overflow `+N more` opens all**; a signal below its sufficiency floor never chips (`DISPLAY_VOCABULARY.md:7`).
- **Priority when over cap:** composites → RS → supply (pop/overhang) → momentum (ROC/MACD/EMA/RSI/z) → liquidity → the rest; newest crossing wins ties (`DISPLAY_VOCABULARY.md:37`, `DESIGN_NOTES.md:57`).
- **Triggers for the three seeded chips:** RS fires at percentile ≥ 90 or ≤ 10, label `RS 94th` (`:13`); MACD (3,6,4) fires on either side of the signal line, label `MACD +` / `MACD −` (`:16`); monthly volume fires in the corpus top decile, label `● Most active · 41 sales/30d` (`:25`).

Build the chip **rendering** from the HTML and the chip **selection** from those rules — see C-13.

#### 3.3.1 Phase 2 chip catalog (D-084.11)

**Ships in Phase 2 — the seven signals honestly computable from the monthly six-tier price series
alone** (`[S1]` in `docs/signals.md`'s substrate notation). Computed in **Domain, on request**,
inside the snapshot — the price reader already loads every row, so no new data access is needed.
Every window is **closed months only** (the revising current month is excluded, `docs/signals.md`'s
standing caveat); below a signal's floor it never chips, and there are no pending/quiet pills on
this page (those belong to watchlist rows, later phases).

| Signal | Fires when | Chip text | Tone | Floor (closed months) |
|---|---|---|---|---|
| ROC 3M (A1) | \|3-mo return\| ≥ 15% | `ROC 3M +18%` | ▲ pos / ▼ neg | 4 |
| MACD 3,6,4 (A3) | MACD above/below signal line | `MACD +` / `MACD −` | ▲ / ▼ | 10 |
| EMA 3×9 cross (A2) | crossed within last 2 closed months | `EMA cross +` / `−` | ▲ / ▼ | 12 |
| z vs 6M MA (B1) | \|z\| > 1.5 | `z +1.8` | ▲ / ▼ | 7 |
| Tier-spread compression (E2) | PSA 10/PSA 9 ratio at the last closed month ≤ 0.8 × the ratio 6 closed months earlier | `spread compressing` | ▼ | 6 paired months, both tiers observed at both endpoints |
| Trend R² (A4) | R² ≥ 0.8 over the trailing 6–12 closed months | `clean trend R² .91` | ▲ if slope +, ▼ if slope − | 6 |
| Drawdown (B4) | ≥ 15% below trailing 12-mo peak | `−28% off peak` | ▼ | 3 |

**Anchor-tier rule:** card-level chips read PSA 10's series when it clears the signal's floor,
otherwise the highest-ranked tier (strip order) that does; the tooltip names the tier read
(`"PSA 10 · 3-mo window · fires at ±15%"`). Tier-spread compression is inherently two-tier and
exempt. A card with no tier clearing a floor simply doesn't chip that signal.

**Compression threshold:** the 0.8× / 6-closed-months rule in the table above — the retired
`DISPLAY_VOCABULARY.md` inventory said only "≥ threshold" and never pinned a number; this spec does.

**Priority order within Phase 2** (the restored family order — composites → RS → supply → momentum →
liquidity → the rest — applied to this roster): ROC 3M → MACD → EMA cross → z → tier-spread →
trend R² → drawdown. Newest crossing wins ties. Cap 4, overflow `+N more` opens all — unchanged from
the general rule above.

**Silently absent until their substrates exist** (firing-only makes absence honest, not a bug): RS
(needs the Phase 3 index); volume, churn, Amihud, dispersion, cross-market gap (post-seam floor +
corpus ranking); Pop Δ, gem-rate drift, overhang, grading-arb EV (census deltas and gem rate);
composite matches (need screens).

**Full inventory:** `docs/signals.md` § "Chip vocabulary" (restored 2026-08-12, D-085) lists every
chip the product can ever show, across every surface — this table is Phase 2's honestly-computable
subset of it, for the Card page header only.

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
| Series colour `c` | :325, :328 | PSA 10 → `PAL.acc`; Grade 9.5 → `#6E4DB8`; Grade 9 → `PAL.warn`; Grade 8 → `#2E7F78`; Grade 7 → `#B0552E`; Raw → `PAL.mut2`. (Two of the six are theme-derived, four are fixed hexes.) **Superseded 2026-08-13 — D-084.3 (C-20/OQ-21 resolved):** brand.md §2.6 `TIER_COLORS` wins — Grade 9.5 → `#7A56C9`, Grade 8 → `#4C8F8A`, Grade 7 → `#A96A4A`; PSA 10/Grade 9/Raw unchanged. See the §2.4 boxed note. |
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

**Resolved 2026-08-13 (OQ-6/I-3) — Phase 2 spec §7:** rather than fixing the routing, Phase 2 removes
column 5's grip entirely — the fluid `minmax(160px, 1fr)` track has no fixed width for a grip to
clamp against in the first place. Grips ship on columns 1–4 only; the shared grid template still
updates both header and rows from one `colW` map (R-6, R-7).

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
| `src` | lowercase string, **closed enum** | `ebay · tcgplayer · goldin · heritage · pwcc` — exactly the five seeded values, and confirmed as the enum at `DISPLAY_VOCABULARY.md:61`: *"Render verbatim, lowercase, mono."* Do not title-case or prettify. |
| `title` | free string | Raw marketplace listing title, incl. emoji and smart quotes. |

**The Realized cell's listed-price affordance (:204) in full.** The span carries a set of *static* CSS
declarations — `text-decoration-line: underline; text-decoration-style: dotted; text-decoration-color:
transparent; text-decoration-thickness: 2px; text-underline-offset: 3px` — which are **inert by design**
(`transparent`). The visible treatment is the interpolated `border-bottom: {{ r.listedLine }}`, i.e.
**a 2px dotted amber (`#8F6614`) bottom border**, present only when a listed price exists, paired with
`cursor: help` and the `listed X → sold Y` tooltip. Confirms the brief: the Listed column was dropped and
folded into the Realized cell.

Note `#8F6614` is **hard-coded**, equal to the *light-theme* `PAL.warn` (:270). It does not follow the dark-theme `--warnInk` (`#D6A54A`). See §7 OQ-7.

**Resolved 2026-08-13, this element (OQ-7 partial) — Phase 2 spec §7:** build against the **theme
token `var(--warnInk)`**, not the literal hex, so the underline follows dark mode (`#D6A54A`)
correctly. The other three theme-blind elements OQ-7 names — the hidden-legend grey and the two
census bar fills — are unresolved by this edit; see §4.10.

**Row count** (:190, :456): `` `${filtered.length} sales shown` `` — mono 12.5px `--mut`. Reflects the **filtered** count, not the total. **Gains a help tooltip in Phase 2 (D-084.5):**
`Each grade is complete from its own first captured sale; nothing earlier was observable.` This is
the honesty mechanism that replaces in-ledger seam markers and captions — C-7 stands, no seam rows,
no captions.

**Grade vocabulary — 19 values** (:322), index order (this is also the sort rank, low→high):
`Raw · Grade 1 · Grade 2 · Grade 3 · Grade 4 · Grade 5 · Grade 6 · Grade 7 · Grade 8 · Grade 9 · Grade 9.5 · PSA 10 · CGC 10 · CGC 10 Prist. · TAG 10 · ACE 10 · SGC 10 · BGS 10 · BGS 10 Black`

**DB mapping (D-081):** the vocabulary above is the *display* label set. The database's
`grade_tier` column stores `Ungraded`, not `Raw` — confirmed by the live label census (D-081:
`Ungraded` 2,635,173 rows, the largest of the 19). **The `Raw` chip and the `Raw` tier-strip cell
both filter/read `grade_tier = 'Ungraded'`**; every other of the 18 labels is verbatim between DB
and display.

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

| # | `label` | `n` (prototype seed) | Bar fill |
|---|---|---|---|
| 1 | `PSA 8` | 1,244 | `rgba(74, 99, 208, 0.55)` (accent blue) |
| 2 | `PSA 9` | 3,865 | accent blue |
| 3 | `PSA 10` | 1,479 | accent blue |
| 4 | `CGC 9` | 402 | `rgba(138, 138, 134, 0.45)` (grey) |
| 5 | `CGC 9.5` | 618 | grey |
| 6 | `CGC 10` | 187 | grey |

**Corrected 2026-08-13 — D-084.4.** `CGC 9.5` cannot exist: `populations.grade` is a `short 1–10`
column (`../PokemonInvestBatch/DATA_MODEL.md` §3.4, D-083), so half-grade census cells are
structurally impossible — the prototype seeded a value for a cell the schema can never produce.
**Build the fixed six as `PSA 8 · PSA 9 · PSA 10 · CGC 8 · CGC 9 · CGC 10`** — `CGC 8` takes row 4's
position in place of the impossible `9.5`; the prototype never seeded a `CGC 8` count, so no
illustrative number is given for it here. The mid-grade census mass this reshuffle displaces is
carried by a **total-slabs count in the summary line** instead of a bar (below), preserving R-20's
grader grouping and keeping the "share of census these six bars show" framing honest.

| Field | Line | Format |
|---|---|---|
| `p.n` | :226, :466 | `n.toLocaleString('en-US')` → `3,865`. Mono 11.5px `--mut`, above the bar. |
| `p.h` | :227, :467 | `Math.round(n / maxPop * 104) + 4` **px** in the prototype, against a fixed `maxPop = 4020` (:366) — headroom above the seeded max of 3,865, **not** derived from the data. Range 4–108px inside a 150px row. **Amended 2026-08-13 — D-084.4/D-084.8, R-21 amended:** the fixed constant is retired as seed fiction (real census counts exceed it — Charizard #4's PSA 8 alone is 15,931). Build **per-card max** scaling instead: `max` = the largest of the card's own six rendered bars, so the tallest always reaches 108px, matching the grading-activity panel's existing rule (§3.9's `maxD`). The `+4` floor stub is unchanged. |
| `p.bg` | :468 | Hard-coded RGBA by grader (see table). Both are literal rgba strings, **not** theme tokens. |
| `p.label` | :228, :466 | Grade string, 11px `--mut2`, `nowrap`, below the bar. |
| `p.tip` | :227, :469 | `` `{grade}: {n} slabs in current census ({PSA|CGC})` `` |

Census date `2026-07-30` appears **twice**: the panel subtitle "PSA + CGC · as of 2026-07-30" (:221) and the footer stamp (:255). Both are hard-coded literals in the markup.

**Corrected 2026-08-13 — D-084.4, Phase 2 spec §4.** The summary line gains a **totals segment**
ahead of the gem-rate sentence: both graders' all-grade totals — `PsaTotal`, `CgcTotal` (every
grade, not just the six displayed bars) — so the "here's a slice of the census" framing of six fixed
bars stays honest against the full population. **The gem-rate sentence itself is omitted until its
inputs qualify** (restating the existing Gate rule below): today, under the 2026-09-01 floor
(D-033), no card has 90 days of qualifying PSA submissions yet, so every card renders the totals
segment with the gem-rate sentence absent — not zeroed, not estimated. See §4.11.

**Summary sentence** (:232) — hard-coded, not templated in the seed. Structure:
> `Gem rate ` **`27.3%`**` — of the last 90 days of PSA submissions, the share that came back 10. Drifting ` **`−0.4pp / 90d`**` (harder to gem = supply of fresh 10s slowing).`

- **Gem rate** — percent, 1 decimal, mono, `--mut`. Definition given inline: *share of PSA submissions in the last 90 days that graded 10*. Window: **90 days**, PSA only (CGC excluded).
- **Drift** — signed **percentage points** per 90 days, 1 decimal, suffix `pp / 90d`, U+2212 minus. Mono, colour `var(--pos, #157A50)`.
- **Branch rule (as rendered):** a *falling* gem rate is coloured **positive/green** and annotated `(harder to gem = supply of fresh 10s slowing)` — falling gem rate is bullish for holders of existing 10s.

**The other branches are not in the HTML but are fully specified in Tier 2, and the rendered branch matches word for word** (`DESIGN_NOTES.md:52`; value space at `DISPLAY_VOCABULARY.md:70`). Build all three:

| Condition | Parenthetical | Colour |
|---|---|---|
| Drift **falling** | `(harder to gem = supply of fresh 10s slowing)` | green / `pos` |
| Drift **rising** | `(easier to gem = fresh 10s arriving faster)` | red / `neg` |
| Drift **flat**, `|drift| ≤ 0.1pp` | `steady` | grey / `mut` |

**Gate:** rate = trailing 90d PSA submissions with a **minimum of ~30 submissions**; below that, **omit the drift sentence entirely** (`DESIGN_NOTES.md:52`). The gem-rate *chip* on other surfaces fires only at `|drift| ≥ 0.3pp/90d` (`DISPLAY_VOCABULARY.md:32`) — a different, higher threshold than the ±0.1pp flat band here. There is **no free text**: every rendered sentence is one of these three combinations.

### 3.9 Grading-activity panel (:371–:373, :471–:477)

| Field | Line | Format |
|---|---|---|
| Observation badge text | :237 | `` `{N} OBS` `` — seeded `7 OBS`. **Shipped 2026-08-13 — D-033/D-084:** the seeded `7 OBS` is prototype fiction (C-21) — Phase 2's badge counts only *qualifying* observations, those at or after the D-033 floor (2026-09-01), against that one fixed anchor rather than the prototype's per-card Jan 2026 start. `GradingActivityPanel.razor` / `CardCensus.QualifyingObservations`. |
| Badge tooltip | :237 | `Census history begins {Mon YYYY} — {N} observations so far; deltas need two` — seeded `Census history begins Jan 2026 — 7 observations so far; deltas need two`. **Shipped 2026-08-13 — D-033/D-084:** the `{Mon YYYY}` per-card anchor is retired along with the fictional count (C-21) — Phase 2 ships one fixed floor date, not a per-card start month: `Census observations counted from 2026-09-01 — {n} so far; deltas need two.` `GradingActivityPanel.razor`. |
| `deltas` | :371 | `[34, 41, 38, 52, 47, 61, 58]` — new PSA 10 slabs per month. |
| `dLabels` | :373 | `['Jan','Feb','Mar','Apr','May','Jun','Jul']` — 3-letter month, no year in the label. |
| `d.n` | :242, :472 | `'+' + n` → `+34`. Mono 11.5px `--mut`, above the bar. |
| `d.h` | :243, :473 | `Math.round(n / maxD * 104) + 4` px; `maxD = 61` (:372) = the **actual max** of `deltas`, so the tallest bar is always 108px. (Contrast with the population panel's fixed 4020.) **Phase 2: the population panel now scales the same way — see the amended R-21 (§6).** |
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
- **`and rising`** — trend qualifier.
- **`+29%`** is coloured `var(--neg2, #D64545)` — **red**, because supply growth is bearish. Parenthetical `(fresh supply working against the price)`.

**Branch rules — `DESIGN_NOTES.md:53`, and every one of them reproduces the seeded output exactly** (value space at `DISPLAY_VOCABULARY.md:70`). Build all of them:

| Element | Rule | Check against the seed |
|---|---|---|
| `+N / mo` | **Latest month's delta** | `deltas[6] = 58` ✓ |
| Pace word | **recent 3-month average vs prior 3-month average** → `rising` / `steady` / `slowing` | recent `(47+61+58)/3 = 55.3` > prior `(41+38+52)/3 = 43.7` → **rising** ✓ |
| `+X% in Y months` | **new slabs ÷ census at window start**; `Y` = number of months in the window | `331 / (1479 − 331) = 28.8%` → `+29%`, `Y = 7` ✓ |
| Parenthetical | census growth **> 2%/mo** → `(fresh supply working against the price)`, red; **else** → `(supply nearly frozen — scarcity intact)`, green | `29% / 7 = 4.1%/mo` > 2% → **red, supply pressure** ✓ |
| **LOW DATA degrade** | **< 2 census observations** → the whole line degrades to `census history too young to compute pace` | not exercised in the seed; the `N OBS` badge tooltip states the same rule ("deltas need two", :237) |

3 pace words × 2 parentheticals + the LOW DATA degrade = **7 possible sentences**. No free text.

**Sign convention across the two panels is deliberately inverted relative to the number's sign**: a negative gem-rate drift is green, a positive census-growth number is red. Both are coloured by *market meaning*, not by arithmetic sign.

### 3.10 Freshness footer (:252–:257)

| Field | Line | Value / format |
|---|---|---|
| Refresh label | :253 | `Sales & prices refreshed ` + **`just now`** (mono). |
| Refresh tooltip | :253 | `Opening a card page triggers a fresh scrape — the ledger and prices you see include sales up to right now` |
| Census label | :255 | `Census as of ` + **`2026-07-30`** (mono, `YYYY-MM-DD`). |
| Census tooltip | :255 | `Population data comes from PSA/CGC on their own publishing schedule — it can't be scraped on demand` — **prototype text; superseded, see below.** |

**Corrected 2026-08-13 — C-17/OQ-13 resolved, Phase 2 spec §6.** The prototype's census tooltip is a
false claim about the data, not just stale copy: `populations` is one of the crawler's own eight
tables, written from the **same detail-page visit** as `price_months` and `sales`, not a separate
PSA/CGC publishing feed (`../PokemonInvestBatch/DATA_MODEL.md:10`, `:343` — the
`GET /game/{set}/{card}` detail crawl writes all three tables together). Verified directly against
the sibling repo; this upgrades C-17 from a recorded Claim to Verified. **Build this tooltip
instead:** `Census updates when the graders publish; we capture it on the same visits as prices.`

**New in Phase 2 (D-084.7):** the footer gains a **TradingView attribution notice and link**,
satisfying the Lightweight Charts Apache-2.0 licence's attribution requirement for the price chart
(§2.4).

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

### 4.2.1 Freshness and refresh states (D-077) — **not in the prototype; build from here**

The prototype asserts a completed refresh (*"refreshed just now"*, :253) and renders no in-flight and no
failure state at all (C-16, OQ-19). This is that missing design.

**The trigger** (D-062): on load, read `cards.last_visited_at`. Older than 24 h → call `express-visit`
through `CardStock.Api`. Fresh → make no call, so the second viewer of a card costs the site nothing.

**The paint never waits.** `express-visit` has no timeout of its own (ADR-0008); a hung upstream returns
502 only after `HttpClient`'s 60 s cap (`Worker/Program.cs:80`, D-076). Blocking the render on it would
put a one-minute blank screen on the path success criterion #1 is measured on.

**Stored prices render immediately at full strength — never skeletoned, never dimmed.** The reason is
arithmetic, not taste: the price block shows 6 tiers × 12 months = 72 values, and a refresh can move at
most the 6 current-month ones, typically 0–2 (`DATA_MODEL.md:110`, `:179`). Hiding 72 real values to
wait on 2 tells the visitor that eleven-twelfths of a chart which is as true as it will ever be should
be distrusted.

**The badge slot.** A fixed **28 px** row beneath the card title, present whether or not it holds
anything. Without the reservation the six price cells jump a moment after paint, which undoes the point
of showing real data immediately.

| State | Slot contents | Elsewhere |
|---|---|---|
| **Fetching** | Neutral badge: **18 px animated logo mark** + `Checking for a newer price` | Prices at full strength; as-of shows the stored date |
| **Landed** (200) | Empty — the slot keeps its height | Changed figures update in place; as-of reads today |
| **Failed** (404/409/422/500/502) | Amber badge: `– as of {date} · {n}d old` | Prices unchanged. They were never wrong, only old |
| **Fresh** (no call) | Empty | As-of reads today |

**The logo loader** is `Cardstock Logo.dc.html:196–208` — `csLoop` 1600 ms + `csDotLoop` 1600 ms, cards
static, sparkline drawing and clearing. Binding rules:

- **18 px minimum in the badge.** `Logo:145` floors the mark at 16 px, so the badge is sized to the
  logo, not the reverse. Both badge variants are 26 px tall so they swap with no movement.
- **The logo appears only while a fetch is genuinely in flight** — not on success, not on failure,
  never as decoration. It is the one place in the product where motion means work is happening.
- **The nav logo stays static.** The mark is now on screen twice; exactly one may move.
- The amber failure badge uses `brand.md` §4.2's en dash `–` ("caution, directionless") in `--warn`
  `#8F6614`, which colourblind mode leaves unchanged.

**This is separate from the `◌`** (§2.3.1), which is about the *month* being unfinished and is present
in every state above, including when nothing is refreshing.

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
>
> **Resolved 2026-08-13 (OQ-5) — Phase 2 spec §7.** The wording now scopes to the selection instead
> of always saying "in this grade": **one bucket selected** → `No sales observed in this grade —
> that's a true zero: our scrapers visited and found none.`; **multiple buckets selected** →
> `No sales observed in these grades — that's a true zero: our scrapers visited and found none.`;
> **`All` / no filter** → `No sales observed for this card — that's a true zero: our scrapers
> visited and found none.` All three drop the prototype's trailing `, not "no data"` clause (:213)
> as redundant with the sentence that follows it. Build these three; the single grade-scoped string
> above (:213) is the prototype fact, not the target.

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

**Four elements are theme-blind in the prototype** — they use hard-coded colours that do not switch: the Realized-cell amber `#8F6614` (:352), the hidden-legend grey `#D8D8D3` (:406), the population/delta bar fills `rgba(74,99,208,.55)` and `rgba(138,138,134,.45)` (:468, :474), and the chart's four fixed tier hexes (:325). See §7 OQ-7.

**Two are resolved for Phase 2 (2026-08-13, OQ-7 partial / C-20):** the Realized-cell underline now
reads the `var(--warnInk)` token (§3.7), and the chart moves to LWC + `brand.md` §2.6 `TIER_COLORS`
entirely (§2.4, §3.6) — theme-aware tokens and CVD-stable hexes, not the prototype's four fixed
literals. **Still hard-coded:** the hidden-legend grey and both bar fills.

### 4.11 Data-sufficiency states (app-wide vocabulary; only one is exercised here)

`DISPLAY_VOCABULARY.md:55` defines the complete render set — **every metric on every surface is in exactly one of five states**:

| State | Presentation | On this screen |
|---|---|---|
| **OK** | renders plain | Everything except the grading-activity panel. |
| **LOW DATA** | amber badge `N OBS`; tooltip states the floor rule and what improves it | **The only one implemented** — the `7 OBS` badge (:237). |
| **LOCKED** | control disabled, countdown copy ("unlocks ~Mar 2027 — needs 60 post-seam days") | **Not present.** No control on this screen is disabled. |
| **UNDEFINED window** | gaps render as **gaps, never zeros** | **Not present**, and the chart has no gap handling at all — `pts()` (:342) maps every index unconditionally. |
| **UNSTABLE FIT** | badge; beta on thin history | **Not present.** |

**This matters more than it looks.** `DECISIONS.md:22` (D-001) establishes that per-sale and census history begin at each card's own first crawler visit in **late Jul 2026**, ragged, never a shared date; `DECISIONS.md:33` calls the consequence *"the largest scope fact in the project"* — every liquidity and supply indicator is LOCKED for 6–12 months of calendar time. `DECISIONS.md:309` (D-033) adds a floor: **no post-seam metric counts observations before 2026-09-01.**

So the realistic launch-day Card page is **not** the seeded one. It shows: a full 12-month price chart and tier strip (monthly history backfills to ~Dec 2020 on first visit — `DECISIONS.md:37`, D-002), a **very short or empty** sales ledger, and a census panel with **one observation** and no deltas at all. **Build the degraded paths first; the seeded density is fiction.** The three states this screen does not implement — LOCKED, UNDEFINED, UNSTABLE FIT — are the ones a real card will spend its first year in. See OQ-20.

**Corrected 2026-08-13 — D-082/D-083.** The paragraph above understates launch-day readiness in two
of its three claims, per live queries against the Pi on 2026-08-12. **The sales ledger is deep, not
near-empty:** `sales` holds 4,406,142 rows over 79,336 cards, `sold_on` running 2016-11-17 →
2026-08-12; the busiest dev card (1958438, Ancient Mew) carries 715 rows (D-082). Build the ledger —
sorting, filtering, resize — for hundreds of rows as the common case, not a handful. **The
population panel has real current-census bars for ~63% of the corpus:** 57,464 of 91,596 active
cards carry at least one `populations` observation (D-083) — the six bars and their totals line
render real data today, not a single-observation placeholder. **What stays degraded, and is still
the thing to build first:** grading-activity *deltas* (need two observations; zero cards qualify
under the 2026-09-01 floor today, D-033/D-083) and every liquidity/momentum metric gated on the
post-seam window (D-001). The LOCKED / UNDEFINED-window / UNSTABLE-FIT states OQ-20 asks for are
still unbuilt and still real — they now belong to the census-delta and signal-chip surfaces
specifically, not to the ledger display or the population bars.

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
| 5 | Breadcrumb `Browse` / set crumb | :56 | → Browse / Set. Leaf crumb is plain `--ink` text, not a link. **Glyphs and colors, from :56 verbatim:** separator `›` (U+203A) in the container's `--mut2`; crumb links `--mut`; leaf `--ink`. *(Corrected 2026-08-13: the build briefly shipped `/` separators and accent crumbs — a plan-prose shorthand read as spec; mockup values restored.)* |

### 5.2 Identity header

| # | Element | Line | Consequence |
|---|---|---|---|
| 6 | **Card art thumbnail** | :59 | `openArt()` → `artOpen = true` → lightbox mounts. `cursor: zoom-in`, `title="Click to enlarge"`. The whole 217×300 box is the hit target. |
| 7 | Set link / character link (subline) | :66 | → Set / Character. **Phase 2: the set link renders deferred-disabled** (Set is a later-phase screen, per the deferred-chrome ruling); **the character segment does not render at all** — see §3.1.1. |
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

Missing in the prototype: Escape key, focus trap, `role="dialog"`, scroll lock, focus return.

**Resolved 2026-08-13 (OQ-8) — Phase 2 spec §4, plan.** Phase 2 builds three of the five gaps above:
the lightbox carries `role="dialog"` + `aria-modal="true"`; an `@onkeydown` handler closes it on
Escape; and on close, focus returns to the art thumbnail via JS interop. **Not built in Phase 2: a
focus trap and a scroll lock** — the ruling above is the complete set of what OQ-8 resolves; those
two remain unaddressed if they matter later.

### 5.4 Price chart

| # | Element | Line | Consequence |
|---|---|---|---|
| 18 | **Legend button** (×6, one per series) | :114 | `lg.toggle()` (:408–:412). Shows a hidden series unconditionally. Hides a visible series **only if fewer than 5 are already hidden** — otherwise silently no-ops, guaranteeing ≥1 visible line. Every toggle re-derives `mn`/`mx`, so **the y-axis and all line geometry rescale on every toggle** (the tooltip says so explicitly). Hover `color: var(--ink)`. |
| 19 | Plot area | :124 | `pcMove` (:418) → snaps the pointer to the nearest of the 12 month indices and sets `hov`; only calls `setState` when the index actually changes. `cursor: crosshair`. |
| 20 | Plot area (leave) | :124 | `pcOut` (:419) → `hov = null`. |
| 21 | Hollow current-month dot | :132 | **Not interactive** — tooltip only (`Aug is month-to-date …`). It sits at `left: 100%`, i.e. outside the plot area's right edge, and is not covered by the hover handler. |
| 22 | `open in Charts →` | :117 | → Charts. |

The hover tooltip and crosshair have `pointer-events: none` (:134) / are absolutely positioned out of the way (:135), so they never interfere with tracking.

**Amended 2026-08-13 (D-089) — the tooltip follows the crosshair horizontally.** Owner ruling,
deviating from the prototype's pinned top-left box (:135) after weighing occlusion against eye
travel: the box rides 12px right of the cursor's x, `top: 8px` fixed, clamped to the pane (8px
left inset; right clamp at `width − 150`, the estimated box width, so it parks at the edge while
the crosshair continues). With no pointer x — keyboard, a missing `param.point` — it falls back to
the pinned corner. `pointer-events: none` unchanged.

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
| 31 | **Header resize grip `│`** (×5) | :194 | `onMouseDown → lc.rs` = `startResize(key)` (:282–:293). `preventDefault` + `stopPropagation` (so **resizing never triggers a sort**), then `mousemove`/`mouseup` listeners on `window`. New width = `clamp(startW + Δx, 40, 420)` px, written into `colW[key]`, which regenerates `lgGridCols` for the header **and every row simultaneously**. `cursor: col-resize`, colour `--line3` → `--acc` on hover, `margin-right: -6px`. ⚠ Grip #5 (`Listing title`) is keyed `'src'` and therefore resizes column 4 in the prototype. **Phase 2 (OQ-6/I-3 resolved, 2026-08-13):** grip #5 is removed rather than rerouted — four grips, not five; see §3.7. |
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
- **R-21.** ~~Population bar heights scale against a **fixed constant** `maxPop = 4020` (:366), not the data max — so bars are comparable across cards but never fill the row.~~ **Amended 2026-08-13 — D-084.4/D-084.8.** The fixed `maxPop = 4020` is seed fiction — real census counts exceed it (Charizard #4's PSA 8 alone is 15,931) — and is retired. Population bars now scale the **same way** as grading-activity bars: **per-card max**, the largest of the six rendered bars, so the tallest always reaches 108px in both panels. Grading-activity bars still scale against `maxD` = the **actual** series max (:372). **The two panels share one scaling rule now; R-20's grader-colour split is the only remaining difference between them.**
- **R-22.** Bar height = `round(n / max × 104) + 4` px in both panels — the `+4` guarantees a visible stub for a zero or near-zero value inside the 150px row.
- **R-23.** The grading-activity panel tracks **PSA 10 only** (heading :236, tooltip :476). CGC contributes to the population census but not to the activity deltas.
- **R-24.** The `N OBS` badge is a **data-sufficiency warning**, styled in the warn palette, and its tooltip states the rule: *deltas need two observations* (:237). It exists because census history is short.
- **R-25.** Summary-sentence colour follows **market meaning, not arithmetic sign**: a falling gem rate is green (:232), a rising census is red (:248).
- **R-26.** `331` and `+29%` in the activity sentence are derivable from rendered data — `sum(deltas)` and `sum(deltas) / (psa10Pop − sum(deltas))` — so the sentence and the two charts must be computed from one source or they will disagree.

**Freshness**

- **R-27.** **Opening a card page triggers a fresh scrape.** Stated in the footer tooltip (:253) and reflected in the stamp reading `just now`. Sales and prices are on-demand fresh.
- **R-28.** ~~**Census is not on-demand.** The tooltip states it comes from PSA/CGC on their own publishing schedule and "can't be scraped on demand" (:255).~~ **Corrected 2026-08-13 — C-17/OQ-13.** The prototype's *reason* was wrong — census **is** captured by the same on-demand scrape as prices, from the same crawler visit (§3.10) — but the *consequence* still holds: the census **number** only moves when PSA/CGC publish new totals upstream, on a cadence CardStock doesn't control, so a fresh visit can re-confirm today's count without changing it. It carries an explicit as-of date, shown in two places (:221, :255), which must agree.
- **R-29.** Consequently the page carries **two different freshness clocks** and must never present census numbers as being as fresh as the ledger.

**Formatting**

- **R-30.** All money uses `money()` (:334): `$`, `Math.round`, `en-US` grouping, no cents — **except** the two y-axis labels, which use `toLocaleString` on the raw bound (:415).
- **R-31.** All numerics render in **JetBrains Mono**; all prose in Inter; all headings in Inter Tight 600/700.
- **R-32.** Negative percentages use **U+2212 MINUS** (`−`), not a hyphen (:324, :232). Month labels use **U+2019** (`’`) (:296). Arrows are U+25B2/U+25CF (chips), U+25BE/U+25B4 (sort), U+25B8/U+25BE (group chips), U+2192 (listed tooltip), U+2713 (check).
- **R-33.** Dates render as **`YYYY-MM-DD`** everywhere (ledger rows :202, census as-of :221/:255) — never localised.
- **R-36. The `Listing title` cell must be HTML-encoded.** `DECISIONS.md:119` (D-029) names this exact column as where it bites. The seeded titles already contain smart quotes, an em dash, an arrow, and an emoji (:304–:319); they are raw marketplace text and are hostile input. It renders in two places — the cell text and the `title` attribute (:206) — and both need encoding. `CARDSTOCK_UI_SPEC_v1.md:220` flags the same requirement (*"title (**HTML-encoded**)"*).

**Runtime**

- **R-34.** `hint-placeholder-count` / `hint-placeholder-val` are **design-time only** — `support.js:614`, `support.js:648` read them solely to render placeholders when the bound value is unavailable. They are **not** counts to implement. Several are already wrong against the seed: `sigChips` hints 2 but renders 3 (:94), `popBars` hints 5 but renders 6 (:224), `lgCols` hints 6 but renders 5 (:193).
- **R-35.** Dead code that must **not** be carried into the Blazor build: `state.bucket`, `state.watch`, `colW.listed` (:272); `hasListed` (:352); `counts` + `sum` (:347–:348, :426 — per-bucket sale counts are computed and never displayed); `pcHovP10`/`pcHovG9`/`pcHovRaw` (:422 — superseded by `pcHovRows`); the `CORE[b] || …` synthetic-series fallback (:329 — unreachable, all six tiers are in `CORE`); `SEAMS` and `isSeam` (:321, :352).

---

## 7. Open questions

| # | Question | Why it is open |
|---|---|---|
| **OQ-1** | ✅ **RESOLVED 2026-08-13 — Phase 2 spec §1/§2.** *Confirm the route is `/card/{id}`.* | Documented at `HANDOFF.md:76` and `CARDSTOCK_UI_SPEC_v1.md:119`/`:217`, but **Tier 2/3 and unverifiable from the HTML** — the prototype has no routing and no inbound link carries an id. Also undecided: what `{id}` is (the DB key? a slug?), and how the breadcrumb, set link, and character link derive their targets. **Answer: `{id}` is `cards.id` — PriceCharting's own product id, never locally generated** (`../PokemonInvestBatch/DATA_MODEL.md:34`, `:162`, e.g. `630417`). The route is confirmed `/card/{id}`. **Still separately unresolved:** how the breadcrumb and set link derive live targets, since Browse and Set are later-phase screens (deferred-disabled meanwhile) — the character link is a different case and does not render in Phase 2 at all; see §3.1.1. |
| **OQ-2** | **What does the Binder button actually do?** | Its tooltip says "opens the binder transaction form" (:82) but the handler only flips a boolean (:390). The real behaviour — navigate to Binder, open a modal form, capture quantity/price/date — is unspecified here. Check `Cardstock Binder.dc.html`. |
| **OQ-3** | **What are seam markers supposed to look like, and when do they render?** | `SEAMS` holds a date per grade (:321) and rows carry `isSeam` (:352), but nothing renders them and no sort-mode gate exists. The brief says "only in date sort" — **the HTML does not implement that, or anything else**. Another prototype (Charts?) may show the intended treatment; otherwise this needs a design decision before build. |
| **OQ-4** | ✅ **RESOLVED 2026-08-13 — D-084.7, Phase 2 spec §5.** *What happens when a chart's visible series are all flat?* | `mx === mn` makes `(v−mn)/(mx−mn)` produce `NaN` and the polylines vanish (:342) **in the prototype's own hand-rolled SVG geometry.** Phase 2 does not port that geometry — it builds on TradingView Lightweight Charts (§2.4 boxed note), whose **autoscale** handles a flat/degenerate series natively. No padding hack needed. |
| **OQ-5** | ✅ **RESOLVED 2026-08-13 — Phase 2 spec §7.** *Should the true-zero copy change under `All` or a multi-bucket filter?* | The string is grade-scoped — "No sales observed **in this grade**" (:213) — but it also renders when `All` is active on a card with no sales at all, and when several buckets are selected. **Answer: yes, three variants scoped to the selection** — see §4.6 for the resolved copy (in this grade / in these grades / for this card). |
| **OQ-6** | ✅ **RESOLVED 2026-08-13 — Phase 2 spec §7.** *Is the `Listing title` column meant to be resizable?* | Its grip is keyed `'src'` (:458) so it resizes Source, and its track is `minmax(160px, 1fr)` which `colW` cannot address. Either the grip should be removed from column 5 or the track should become fixed-width. **Answer: no — the grip is removed from column 5** rather than the track becoming fixed-width; see §3.7. Also resolves I-3 (§8.2). |
| **OQ-7** | ⚠ **PARTIALLY RESOLVED 2026-08-13 — Phase 2 spec §7.** *Should the hard-coded colours be theme-aware?* | The Realized amber `#8F6614` (:352) is the *light* `--warnInk`; dark theme uses `#D6A54A` (:27). Also hard-coded: the hidden-legend grey `#D8D8D3` (:406) and both bar fills (:468, :474). Contrast in dark mode is unverified. **Resolved for one of the four:** the Realized-cell underline now builds against the theme token `var(--warnInk)` (§3.7). The tier-hex question is separately resolved by D-084.3 (§2.4, §3.6), but that was the chart series colours, not this list's four. **Still open:** the hidden-legend grey and both population/activity bar fills remain hard-coded literals — see §4.10. |
| **OQ-8** | ✅ **RESOLVED 2026-08-13 — Phase 2 spec §4, plan.** *Lightbox accessibility.* | No Escape handler, no focus trap, no `role="dialog"`/`aria-modal`, no scroll lock, and focus is not restored to the thumbnail on close (:101–:108). Add or accept? **Answer: add three of the five** — `role="dialog"` + `aria-modal="true"`, an Escape handler, and focus return to the thumbnail on close (see §5.3). Focus trap and scroll lock are **not** added in Phase 2. |
| **OQ-9** | ~~What are the other branches of the two summary sentences?~~ **ANSWERED** — `DESIGN_NOTES.md:52` and `:53`, transcribed into §3.8 and §3.9. Every threshold, branch, and degrade string is specified there and reproduces the seeded output exactly. **Remaining sliver:** the gem-rate *flat* band is `±0.1pp` (`:52`) while the gem-rate *chip* elsewhere fires at `≥0.3pp` (`DISPLAY_VOCABULARY.md:32`) — confirm those are deliberately different thresholds. |
| **OQ-10** | **What was `d.bd` for?** | Every activity bar sets `border: 'none'` yet the markup keeps `box-sizing: border-box; border: {{ d.bd }}` (:243, :475). Most plausibly an outlined treatment for the current partial month, mirroring the chart's hollow dot — unconfirmed. |
| **OQ-11** | **How does the watchlist picker produce a card+tier row, and what is the create-list UI?** | `HANDOFF.md:155` and `DESIGN_NOTES.md:110` say watchlists are **one row per card + tier**, but the picker has no tier selector (:73–:79) — see C-14. Separately, `+ New list…` uses a native `prompt()` (:386) and the created list never appears as a row because `watchLists` is a fixed array (:380). |
| **OQ-12** | ⚠ **PARTIALLY RESOLVED 2026-08-13 — D-084.11, Phase 2 spec §12.** *How are the signal chips computed?* | The *presentation* and *selection rules* are now settled (§3.3, C-13). What is not: the source of each signal. RS is "vs market index, 3M"; MACD is "(3,6,4)"; "Most active" is a corpus-wide top-decile ranking. **None of these exist in the scraper's eight tables** — all are derived, all need a computation owner, and each needs a sufficiency floor (`DISPLAY_VOCABULARY.md:7`: a signal below its floor never chips). **Resolved for seven of them:** ROC 3M, MACD, EMA cross, z vs 6M MA, tier-spread compression, trend R², and drawdown are computed in **Domain, on request**, purely from the price reader's own series (`[S1]` in `docs/signals.md`'s notation) — no new computation owner needed; see §3.3.1. **Still open:** RS, liquidity chips (volume/churn/Amihud/dispersion/cross-market), census-based chips (Pop Δ/gem-rate/overhang), and composites all need substrates — an index, the post-seam ledger at scale, and census deltas respectively — that arrive in later phases; each stays silently absent until then. |
| **OQ-13** | ✅ **RESOLVED 2026-08-13 — C-17, Phase 2 spec §6.** *Is the census tooltip's factual claim correct, and is the as-of date one value or two?* | The date is hard-coded twice (:221, :255) — presumably one field; confirm PSA and CGC can never publish on different days. Separately, the tooltip asserts census *"comes from PSA/CGC on their own publishing schedule — it can't be scraped on demand"* (:255, tracing to `DESIGN_NOTES.md:54`). Sibling analysis in this batch says that is false — census rows come from the scraper's own visits. **Unverified by me against `../PokemonInvestBatch`; verify before shipping the copy.** See C-17. **Now verified directly against `../PokemonInvestBatch`** (`DATA_MODEL.md:10`, `:343`): the claim is false, and the corrected copy is in §3.10. **The as-of-date-is-one-field half stays open** — nothing in this pass confirmed or denied whether PSA and CGC could report on different days; the single `ObservedAt?` field in the Phase 2 API contract (spec §3) treats census as one as-of value across both graders, which is a working assumption baked into the contract, not an independently re-verified fact. |
| **OQ-14** | ✅ **RESOLVED 2026-08-13 — D-084.7, Phase 2 spec §5.** *Which series should the hollow dot track when PSA 10 is hidden?* | It follows the *first visible* series (:417) but its ring is always `var(--acc)` (:132) — PSA 10's colour — so with PSA 10 hidden the dot sits on another series' line wearing the wrong colour **in the prototype.** Phase 2's custom LWC primitive draws the dot at the first visible series *with a current-month value*, in **that series' own colour** — fixing both the wrong-colour bug and the case where the first visible series has no current-month point. |
| **OQ-15** | **Should the ledger link out to the source listing?** | The listing title is truncated text with a `title` tooltip and no link (:206). The scraper knows the marketplace; whether it retains a URL is a data question for `../PokemonInvestBatch`. |
| **OQ-16** | **What is the ledger's time window and page size?** | 16 seeded sales span 2026-03-28 → 2026-08-01 with no pagination, no "load more", no date-range control, and no windowing copy anywhere (:151–:215). Unbounded is unlikely to be the intent. |
| **OQ-17** | ✅ **RESOLVED 2026-08-13 — D-084.9, Phase 2 spec §5.** *Off-by-one in the seeded chart months.* | The x-axis and `MONTHS` end at `Jul ’26` (:147, :296) but the hollow-dot tooltip says "**Aug** is month-to-date" (:132), the ledger's newest sale is `2026-08-01` (:304), and the census is as of `2026-07-30`. If "today" is Aug 2026 the 12-month window should be Sep ’25–Aug ’26. Almost certainly stale seed data — but confirm the rule is "12 months ending at the current, incomplete month". **Confirmed: the rule is 12 months ending at the current, incomplete calendar month** — build the window computed from the clock, never hard-coded, exactly as §2.3.1 already required for the tier-strip `◌` glyph's `{Month}`. |
| **OQ-18** | **Tier-strip change window.** | Labelled `30d` (:89) and the tooltip says "over 30 days" (:398), but the underlying series is *monthly* (:112). 30 days ≠ one calendar month. Confirm which the number really is. |
| **OQ-19** | ✅ **RESOLVED 2026-08-11 (D-077) — see §4.2.1.** *What does the page show while the on-demand scrape is in flight, or when it fails?* | The footer rendered a completed past tense — `refreshed just now` (:253) — and **no other state existed**: no spinner, no "updating…", no error, no stale fallback. **Answer:** stored prices paint immediately at full strength; an 18 px animated logo badge in a reserved 28 px slot carries the in-flight state; failure swaps it for an amber `– as of {date} · {n}d old`; prices never change, because they were never wrong. Two corrections landed with it: the endpoint returns **500, not 504** (D-076), and it is `express-visit`'s **60 s `HttpClient` cap** that bounds the wait, which is why the paint may never block on it. `CLAUDE.md`'s loopback constraint still holds — the call is proxied through `CardStock.Api`, settled by D-063. |
| **OQ-20** | **Design the LOCKED, UNDEFINED-window, and UNSTABLE-FIT states for this screen.** | Only LOW DATA is implemented (§4.11). Given D-001/D-033, a real card at launch has a near-empty ledger and a single census observation — so the states this prototype skips are the ones users will actually see. Specifically: what does the census pair render at `N OBS < 2`? What does the chart do with a month that has no observed sales (gap, not zero)? |
| **OQ-21** | ✅ **RESOLVED 2026-08-13 — D-084.3.** *Do the Card and Charts tier palettes get reconciled?* | Three of six tier colours differ between `Cardstock Card.dc.html:325` and `Cardstock Charts.dc.html:375` (C-20). Both are Tier 1, so this needs an owner ruling, not an inference — and the Card page links directly into Charts twice (:69, :117). **Owner ruling: the Charts values win** — `brand.md` §2.6 `TIER_COLORS` is now the single palette both screens read; the Card prototype's three variant hexes are superseded. See the §2.4 boxed note and §3.6. |
| **OQ-22** | **Does the grade vocabulary imply grader-neutrality it should not?** | `DECISIONS.md:70` (D-022) records a binding ADR consequence: *"The interface must not imply the pooled figure is company-neutral."* The ledger renders bare `Grade 9`, `Grade 9.5`, `Grade 8` labels (:203) with no grader qualification, and the tier strip does the same (:87). Whether that reads as neutral — and whether a disclosure is owed — is unresolved; `HANDOFF.md:22` already flags the related "grader-agnostic" wording as contradicted. |

---

## 8. Contradictions found

Every row below was checked against the HTML directly. **The HTML wins in all of them.**

Paths are relative to the repo root. `MOCK/` = `CardStock Mockup/`. All doc quotes below were read directly.

| # | Claim | Source (doc:line) | What the HTML actually does |
|---|---|---|---|
| **C-1** | *"19-tier strip, price history, sales ledger, census & grading"* | `MOCK/HANDOFF.md:76` | **Six cells, not nineteen.** The grid is literally `repeat(6, 1fr)` (:84); `tierStrip` reverses the 19-value `BUCKETS` and filters to a hard-coded allow-list of six — PSA 10, Grade 9.5, Grade 9, Grade 8, Grade 7, Raw (:395). The 19 values appear **only** as the ledger's grade vocabulary, the filter-chip set, and the sort rank (:322). Note `HANDOFF.md:13` declares §3 (which contains this line) *"self-verifying — open the HTML"*. Opened. It is wrong. |
| **C-2** | *"all 6 tiers as clickable legend toggles (PSA 10/9.5/9/8/7/Ungraded; 9.5/8/7 default-hidden), y-axis rescales to visible, ≥1 series always on, hover readout follows visibility"* | `MOCK/DESIGN_NOTES.md:55` | **Correct in every particular except the sixth tier's name.** Six series (:327); `DEF_OFF` hides exactly Grade 9.5 / Grade 8 / Grade 7 (:331–:332); y-axis recomputes from visible series only (:339–:341); the hide branch is gated so ≥1 stays on (:410); `pcHovRows` is built from visible series only (:416). The sixth tier renders as **`Raw`**, not "Ungraded" — consistent with the later app-wide rename at `DESIGN_NOTES.md:77`. |
| **C-3** | *"Tier strip = PSA-only, 5 tiles (PSA 10/9/8/7 + Ungraded) — Grade 9.5 tile DROPPED (PSA has no 9.5…)"*, dated the same signoff day the Card page was declared done | `MOCK/DESIGN_NOTES.md:59` | **Six tiles, and Grade 9.5 is present** (:395, :323 index 10 → `$1,010`). This ruling was reversed by `DESIGN_NOTES.md:77` (*"SUPERSEDES the earlier PSA-only tier-strip ruling"*) — but :77 then over-corrects; see C-4. The HTML sits between the two: 6 tiles, 9.5 included. **`DESIGN_NOTES.md:59` is stale and must not be built.** |
| **C-4** | *"Applied to: Card tier strip (**auto-wrap grid**) + chart legend/series + ledger chips…"* — i.e. the 19-value scale drives the strip and the chart | `MOCK/DESIGN_NOTES.md:77` | **Two-thirds wrong.** The strip is a **fixed `repeat(6, 1fr)` grid, not auto-wrap** (:84), and carries 6 values. The chart legend/series is **6**, not 19 (:327). Only the third clause holds: the **ledger chips do cover all 19** buckets exactly once (:446–:453 vs :322). |
| **C-5** | *"§4.11 Card: six-tier strip → canonical 19-value grade scale"* | `MOCK/DESIGN_NOTES.md:83` | **The migration never happened for the strip.** Still six (:84, :395). The same line's *"Listed column DROPPED → dotted amber underline + tooltip"* and *"current-month point … dashed + hollow dot"* clauses **are** implemented (see C-8, C-11); its *"seam markers render only in date sort"* clause is **not** (see C-6). |
| **C-6** | *"Seam markers only render in date sort — they're chronological annotations."* | `MOCK/DESIGN_NOTES.md:47` | **No seam marker renders at all, in any sort mode.** `SEAMS` (:321) is populated and **never read**; every row is stamped `isSeam: false` (:352); the row template has exactly one child, `sc-if r.isSale`, and **no seam branch** (:199–:209). Grep confirms only two occurrences of the token in the entire file. No date-sort gate is expressed anywhere. |
| **C-7** | *"Removed from Card page (user decisions): … seam markers in sales ledger (**no seam recognition planned**)"* | `MOCK/DESIGN_NOTES.md:54` | **The HTML sides with this line, not with :47.** :47 and :54 are in the **same file** and directly contradict each other. The rendering matches :54 (nothing renders); the residual `SEAMS` map and `isSeam` flag (:321, :352) are dead scaffolding from before the removal. **Build to :54 — no seam markers.** See OQ-3. |
| **C-8** | *"Listed column DROPPED from sales ledger — production coverage is 4.4% … the Realized cell gets a dotted amber underline + tooltip `listed $X → sold $Y`. A 95.6%-blank column reads as broken, not sparse."* | `MOCK/DESIGN_NOTES.md:46` (cut list restated at `MOCK/HANDOFF.md:98`; marker rule at `MOCK/DISPLAY_VOCABULARY.md:61`) | **Fully verified, down to the tooltip string.** `lgCols` has five columns, no Listed (:458); `lgGridCols` consumes only `date/bucket/price/src` (:457); the vestigial `colW.listed: 84` survives unread (:272). The Realized cell gets `border-bottom: 2px dotted #8F6614`, `cursor: help`, and `title = "listed $X → sold $Y"` (:204, :352). |
| **C-9** | The ledger includes a *"listed price (when present)"* column **and** a *"per-bucket seam marker row"* (*"reliable history for PSA 10 begins 2026-03-14"*) | `MOCK/uploads/CARDSTOCK_UI_SPEC_v1.md:220` (Tier 3); seam+listed columns also at `MOCK/uploads/PROJECT_LOG.md:215` | **Neither exists.** Five columns, no Listed (:458); no seam row (§4.7). Note the spec's example date `2026-03-14` **is** the seeded `SEAMS['PSA 10']` value (:321) — the data outlived the feature. Tier 3, superseded; do not build. |
| **C-10** | Listed prices cover *"~12% of rows"* | `MOCK/HANDOFF.md:128` | **Contradicted by `DESIGN_NOTES.md:46` (4.4%), and HANDOFF self-corrects at `:20`.** `DECISIONS.md:375` (D-031) rules 4.4% credible. The HTML cannot settle a coverage rate — the seed shows 5 of 16 rows with a listed price (:304–:319), which is ~31% and purely illustrative. **Do not treat the seed density as real.** |
| **C-11** | *"the month-to-date point is computed with the SAME aggregation as closed months … NO projection … final chart segment dashed + hollow end dot with tooltip, no text warning"* | `MOCK/DESIGN_NOTES.md:49` | **Verified.** Solid polyline for indices 0–10, dashed `4 4` for the 10→11 segment (:414); hollow `--card`-filled, accent-ringed 8px dot at `left: 100%` (:132); tooltip only, no on-canvas text warning. |
| **C-12** | *"Seams: … resolution seam Jul '26 **amber dashed line on price chart** ('per-sale ledger begins')"* | `MOCK/DESIGN_NOTES.md:35` | **No amber seam line on this chart.** The only non-data line is a `--line4` rule at the viewBox midpoint `y=115` (:126), which is decorative, not a seam. The only dashing is the current-month tail (:414). (`DECISIONS.md:385`, D-009, separately disputes the "Apr '25 liquidity seam" on the same doc line.) |
| **C-13** | Card-header signal chips show **only firing** signals, *"priority-ordered, cap 4, overflow '+N more' opens all"* | `MOCK/DISPLAY_VOCABULARY.md:7`, priority order at `:37`, restated `MOCK/DESIGN_NOTES.md:57` | **None of that machinery exists.** `sigChips` is a static 3-element literal (:400–:404) with no firing test, no priority sort, no cap, and **no `+N more` control**. The container merely `flex-wrap`s (:93). The three seeded chips *do* match the documented triggers exactly (`RS 94th` ← `:13`; `MACD +` ← `:16`; `● Most active · 41 sales/30d` ← `:25`), so the vocabulary is right and only the selection logic is missing. **Resolved 2026-08-13 — D-084.11, Phase 2 spec §12.** The selection machinery is now specced and built: firing test, priority sort, cap-4-with-`+N more`, and a concrete seven-signal Phase 2 roster with floors, an anchor-tier rule, and chip-text formatting — all pure Domain code, under test. See §3.3.1. |
| **C-14** | Watchlists are *"one row per card + tier"*; *"Charts IS the editor, the nav watch button is the save"* | `MOCK/HANDOFF.md:155`, `MOCK/DESIGN_NOTES.md:110`, `:112` | **The Card page's picker has no tier selector** (:73–:79) — it toggles card↔list membership only. The button tooltip does defer signal choice to Charts (*"you pick which signals it tracks in Charts"*, :71), consistent with :112. But if a watchlist row is keyed by card **+ tier**, this control cannot produce one unambiguously. See OQ-11. |
| **C-15** | *"Data honesty strip — 'as of Xh ago'"*; and app-wide, *"every data surface carries a quiet 'data as of Xh ago' stamp"* | `MOCK/uploads/CARDSTOCK_UI_SPEC_v1.md:220`, `:39` (Tier 3) | **Removed.** The footer reads *"Sales & prices refreshed just now"* (:253) with a per-source split, and there is no `AsOfStamp` anywhere. Superseded by `DESIGN_NOTES.md:54` and `:84`, and by `HANDOFF.md:99`. |
| **C-16** | On-demand card refresh *"**must be async** (politeness gate makes sync refresh a lie)"* | `MOCK/uploads/PROJECT_LOG.md:282` (Tier 3) | **The HTML asserts a synchronous, already-complete refresh** — *"refreshed **just now**"* with the tooltip *"Opening a card page triggers a fresh scrape — the ledger and prices you see include sales up to right now"* (:253). The Tier-3 objection has since been overtaken: `CLAUDE.md:74` records a **synchronous** `POST /cards/{id}/express-visit` that bypasses the polite gate, and `DECISIONS.md:429` (D-025) maps this exact stamp to it. **Build to the HTML** — but note it renders no in-flight or failure state (OQ-19). **Update 2026-08-11:** that gap is now filled by §4.2.1 (D-077), and the `504` this row's chain of sources implies no longer exists (D-076). |
| **C-17** | Census tooltip: *"Population data comes from PSA/CGC on their own publishing schedule — it can't be scraped on demand"* (:255), traced to `MOCK/DESIGN_NOTES.md:54` | HTML :255 vs the data repo | **Flagged as factually wrong about the data** by sibling analysis in this same `docs/screens/` batch: census rows come from the scraper's own visits, not a PSA/CGC publication feed. **Not verified by me against `../PokemonInvestBatch`** — recorded here as a Claim, not a finding. It does not change the layout; it may change the tooltip copy. See OQ-13. **Resolved 2026-08-13 — verified, not just flagged.** `../PokemonInvestBatch/DATA_MODEL.md:10` and `:343`: `populations` is written from the same `GET /game/{set}/{card}` detail-page visit as `price_months` and `sales` — one crawl, three tables, no separate PSA/CGC feed. The Claim is now Verified and false; corrected tooltip copy is in §3.10. |
| **C-18** | Per-sale ledger begins **Apr 2025** | `MOCK/HANDOFF.md` §5, now corrected in place — see `:19`, `:126`, `:134` | **Superseded.** `HANDOFF.md:126` now reads *"Each card's first visit, late Jul 2026 onward — ragged, never a shared date"*, and `DECISIONS.md:22` (D-001) rules the same, with the scraper's first deployment at 2026-07-28. `DESIGN_NOTES.md:41` (*"per-sale scraping started Jul '26"*) was right all along. **The HTML's seed contradicts the settled answer**: sales run 2026-03-28 → 2026-08-01 (:304–:319) and `SEAMS` puts per-grade seams in Mar–Jun 2026 (:321) — all before the scraper existed. Seed fiction; ignore the dates, keep the structure. |
| **C-19** | `price_months` carries **exactly 6** price tiers (`Ungraded, Grade7, Grade8, Grade9, Grade9Half, Psa10`); the 19-value scale is legitimate only for *sales* and *holdings* | `DECISIONS.md:44`–`:57` (D-003), reinforced by `:403`–`:404` (D-012) and `CLAUDE.md:92` | **This is the reconciliation of C-1/C-4, and the HTML implements it exactly.** Six tiers wherever a **price series** is plotted (strip :395, chart :327); nineteen wherever a **sale** is described (bucket column :203, filter chips :446–:453, sort rank :354). `DECISIONS.md` overrides all doc tiers, and `:246` (D-038) independently describes the Card page as having a *"six-tier strip"*. **Settled: six.** |
| **C-20** | Tier colours | `MOCK/Cardstock Card.dc.html:325` vs `MOCK/Cardstock Charts.dc.html:375` | **Code vs code — both Tier 1, and they disagree.** Verified by reading both lines: Card uses `Grade 9.5 #6E4DB8`, `Grade 8 #2E7F78`, `Grade 7 #B0552E`; Charts uses `#7A56C9`, `#4C8F8A`, `#A96A4A`. PSA 10 (`PAL.acc`), Grade 9 (`PAL.warn`) and Raw (`PAL.mut2`) match. Needs an owner ruling before either is built — the Card page links straight into Charts (:69, :117), so a user will see both. **Resolved 2026-08-13 — D-084.3.** Charts' values win: `brand.md` §2.6 `TIER_COLORS` — Grade 9.5 `#7A56C9`, Grade 8 `#4C8F8A`, Grade 7 `#A96A4A`. Also resolves OQ-21. See §2.4, §3.6. |
| **C-21** | `7 OBS` / *"Census history begins Jan 2026 — 7 observations so far"* (:237); `Pop Δ` rows citing *"Census history starts Jan '26 — 7 observations"* and *"12M census history (7/12 mo)"* | HTML :237; `MOCK/DISPLAY_VOCABULARY.md:117`, `:118`, `:161`, `:164` | **The badge is structurally right and numerically fiction.** `DECISIONS.md:342`–`:350` (D-032) rules every such ratio wrong *in the direction that overstates readiness*: census starts **late Jul 2026**, so the true figure is ~**1/12**, unlocking ~Jul 2027. `DECISIONS.md:309` (D-033) adds a floor — no post-seam metric counts observations before **2026-09-01**. **Build the badge; do not build the number.** **Resolved 2026-08-13 — D-033/D-084.** Built exactly that: the badge renders `{N} OBS` where `N` is `CardCensus.QualifyingObservations` — observations at or after the D-033 floor (2026-09-01), not the prototype's fictional `7`/Jan-2026 anchor — with tooltip `Census observations counted from 2026-09-01 — {n} so far; deltas need two.` **See §3.9; build that, not the seeded `Census history begins Jan 2026 …` copy at :237.** |

| **C-22** | The tier strip labels its six prices *"latest monthly price"* (:398) and gives them no provisional marker, while the chart marks the identical number with a dashed segment and a hollow dot (:414, :132) | HTML :398 vs :414/:132, reconciled through the §2.3 invariant (:107) | **Resolved 2026-08-11 — D-077.** The invariant is the finding: each strip price equals index 11 of that tier's chart array, and R-8 establishes index 11 as the current, incomplete month. So the strip has been showing six month-to-date figures with finished-number phrasing. Fixed by adding `◌` — `brand.md` §4.2's existing *"current month provisional"* glyph — plus corrected tooltip copy. **See §2.3.1; build that, not :398.** Nothing in the prototype's layout changes. |
| **C-23** | The page asserts a completed refresh (*"refreshed just now"*, :253) and implements no in-flight state and no failure state | HTML :253; C-16; OQ-19 | **Resolved 2026-08-11 — D-077.** `express-visit` returns 200/404/409/422/500/502 with **no timeout** (D-076), so a hung upstream costs 60 s before answering. The page therefore never blocks on it: stored prices paint at full strength, a badge carries the in-flight and failure states, and a reserved 28 px slot keeps the strip from jumping. **See §4.2.1.** |
| **C-24** | The prototype defines the page's complete state space — no HTML claims otherwise | Phase 2 spec §4, §8, §11.7 | **Not a contradiction — a deliberate post-prototype addition.** The frozen prototype has no delisted state (no chip, no styling — `cards.delisted_at` is a scraper-schema fact the mockup predates) and no not-found treatment (`Cardstock Card.dc.html` is a single static file with no routing, so a missing-id case cannot occur in it). Phase 2 adds both: a muted `delisted {date}` chip beside the subline when `DelistedAt` is set (the page otherwise renders in full, and refresh still fires — the worker deliberately permits express-visits on delisted cards, `IntakeApi`/`ExpressVisitRunner`); and a 404 page for unknown ids and `not_a_card_at` cards — `No card with id {id}.`, chrome stays, no fake suggestions. Day arithmetic is UTC throughout. |
| **C-25** | The frozen prototype's lower identity header is a 6×1 tier strip (Row B, :84–:92) and a firing-only chip row (Row C, :93–:97) | `Cardstock Card.dc.html:84–:97` vs the owner rework `Cardstock Card.rework-2026-08-13.html` (anchors `tierStrip`, `sigRows`, `sigCount`, `quietMore`) | **Superseded by design 2026-08-13 — D-092, plan `docs/superpowers/plans/2026-08-13-signals-panel.md`.** The owner reworked the region: a 3×2 grid of square tier tiles (§2.3 amended) and an unbounded Signals panel showing every evaluated signal's state (§2.3.2). The rework supersedes the frozen prototype **for this region only**. Its seed copy is not data-authoritative, per the same session's rulings: the tile tooltip drops D-077's month-to-date honesty (kept — §2.3.1 stands); `RS 94th` / `Pop Δ +0.4%` / `churn 48 recorded` assume substrates that don't exist (locked rows instead — §2.3.2); and its eight rows are a sample, not a cap (the panel is unbounded). |

### 8.1 Corroborations (doc and HTML agree — recorded so they are not re-litigated)

| Claim | Source | HTML |
|---|---|---|
| *"Tier strip runs PSA 10 first (descending grade); right column spreads to fill art height."* | `MOCK/DESIGN_NOTES.md:44` | :395 order; `justify-content: space-between` on the right column (:62) |
| *"Card art click-to-enlarge lightbox (zoom-in cursor; ✕ top-right, backdrop click closes)."* | `MOCK/DESIGN_NOTES.md:45` | :59, :102, :105 — all three, exactly |
| *"sortable by any header (click, ▾/▴, desc first; grade sorts by rank, ties fall to date)"* | `MOCK/DESIGN_NOTES.md:47` | :461 desc-first; :460 ▾/▴; :354 rank; :362 tie→date |
| Source enum `ebay · tcgplayer · goldin · heritage · pwcc`, *"verbatim, lowercase, mono"* | `MOCK/DISPLAY_VOCABULARY.md:61` | :304–:319 seeds exactly those five; :205 renders mono, lowercase, verbatim |
| Gem-rate and pace **branch rules** | `MOCK/DESIGN_NOTES.md:52`, `:53`; value space at `MOCK/DISPLAY_VOCABULARY.md:70` | Every rendered token checks out — see §3.8/§3.9. This is the single most valuable doc find for the build. |
| *"Footer staleness stamps replaced by 'Sales & prices refreshed just now' — card page visits trigger a fresh scrape"* | `MOCK/DESIGN_NOTES.md:54` | :253 verbatim |
| *"Missed-sales scraper alert removed"* | `MOCK/DESIGN_NOTES.md:48` | Absent |
| *"+ Watchlist ▾ opens multi-list picker (checkboxes, counts, + New list; card can be in several lists; click-outside dismiss)"* | `MOCK/DESIGN_NOTES.md:59` | :71–:79, :276 — all five behaviours |
| *"full playground →" renamed "open in Charts →"* | `MOCK/DESIGN_NOTES.md:55` | :117 (and the button at :69) |
| Empty bucket copy *"No sales observed in this grade"* | `MOCK/uploads/CARDSTOCK_UI_SPEC_v1.md:222` | :213, extended with the explicit true-zero clause |
| Route `/card/{id}` | `MOCK/HANDOFF.md:76`, `…/CARDSTOCK_UI_SPEC_v1.md:119`, `:217` | Not contradicted; not confirmable — the prototype has no routing (OQ-1) |

### 8.2 Internal inconsistencies within the prototype itself

Not doc-vs-HTML, but HTML-vs-HTML — flag them so they are not faithfully reproduced as bugs:

| # | Inconsistency | Lines |
|---|---|---|
| I-1 | The hollow-dot tooltip says "**Aug** is month-to-date" but the last chart month is `Jul ’26`. | :132 vs :147, :296 |
| I-2 | The badge says **7 observations** and its tooltip says "deltas need two", yet **7 delta bars** are drawn — 7 observations yield 6 deltas. | :237 vs :371 |
| I-3 | Column 5's resize grip is keyed `'src'`, so it resizes column 4. **Resolved 2026-08-13 (OQ-6) — Phase 2 spec §7:** the grip is removed from column 5 in Phase 2, not rerouted; see §3.7. | :458 |
| I-4 | `+ New list…` adds a membership that no popover row can display. | :380 vs :386 |
| I-5 | The hollow dot follows the first *visible* series but is always drawn in the PSA 10 accent colour. | :132 vs :417 |
| I-6 | Per-bucket sale `counts` are computed and a `sum()` helper is defined, but no chip ever shows a count. | :347–:348, :426 |
| I-7 | `hint-placeholder-count` values disagree with the seeded lists in three places (2 vs 3 chips, 5 vs 6 bars, 6 vs 5 columns). | :94, :224, :193 |
| I-8 | The Realized cell sets five `text-decoration-*` properties that are inert (`text-decoration-color: transparent`); the visible rule is `border-bottom`. | :204 |
