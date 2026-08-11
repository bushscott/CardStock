# Binder — screen specification

**Source of truth:** `CardStock Mockup/Cardstock Binder.dc.html` (622 lines), read in full 2026-08-10.
All line citations below are `Binder:NNN` referring to that file. Per `CLAUDE.md` §"Document authority",
this HTML is Tier 1; where `DISPLAY_VOCABULARY.md` / `DESIGN_NOTES.md` / `HANDOFF.md` disagree, the HTML
wins and the disagreement is recorded in §8.

**Runtime:** Design Composer (`support.js`) — `<x-dc>` root, `sc-for` / `sc-if` conditionals, a
`DCLogic` subclass whose `renderVals()` (Binder:445–617) supplies every `{{ token }}`. Seeded arrays are
illustrative; the structure and state space below are the contract.

---

## 1. Identity

| | |
|---|---|
| **Screen name** | Binder |
| **File** | `CardStock Mockup/Cardstock Binder.dc.html` |
| **`data-screen-label`** | `Binder` (Binder:37) |
| **Route** | `/binder` (`HANDOFF.md:74`) |
| **Deep link** | `/binder#performance` — `componentDidMount` reads `location.hash === '#performance'` and sets `tab: 'performance'` (Binder:432–434). This is the **only** hash handled; `#transactions` / `#holdings` do nothing. |
| **Nav position** | 4th of 5 primary tabs (Home · Screener · Charts · **Binder** · Browse), Binder:44–50. Active styling: `font-weight: 600`, `color: var(--ink)`, 2px `var(--acc)` bottom border. |
| **Component props** | Exactly one: `emptyState` (boolean, default `false`, editor section "Mode") — Binder:330. |

**Purpose.** Treat a card collection as an investment portfolio: what you own (holdings), what you did
(transaction ledger), and how you did against the market (performance vs index). It is the only screen
that renders **user-authored** data rather than scraped market data.

**Privacy stance is part of the identity.** A `PRIVATE` mono badge sits beside the H1 with
`title="Binder data is strictly private — no social features, never shared"` (Binder:60). It is
`cursor: help`, non-interactive.

**Emotional thesis, stated on-screen.** The IRON RULE strip (Binder:231–234) is rendered on *every* tab
and in the empty state — it is outside all `sc-if` blocks:

> `IRON RULE` — Your entered prices never change. Current values are estimates that move with the
> market — badged `EST` when no recent sales support them.

---

## 2. Layout

Vertical stack inside `<main>` (`max-width: 1480px`, `padding: 14px 20px 28px`, `gap: 14px`), Binder:56.

```
nav (48px sticky, z-20)                                              Binder:39–54
└─ logo → Home · Home/Screener/Charts/[Binder]/Browse · <cardstock-search> · avatar "O" → Profile

main
├─ title row                                                         Binder:58–66
│   H1 "Binder" · PRIVATE badge · spacer · [↓ Export CSV]* · [+ Add transaction]
│                                             *only when tab = transactions
├─ control row                                                       Binder:68–80
│   ├─ tab segmented control: holdings | transactions | performance   (ALWAYS rendered)
│   └─ view segmented control: table | gallery      (only when isHoldTab)
│
├─ ⟨sc-if isEmpty⟩   empty-state card                                Binder:82–88
├─ ⟨sc-if isGallery⟩ art-tile grid                                   Binder:90–106
├─ ⟨sc-if isHold⟩    holdings table                                  Binder:108–135
├─ ⟨sc-if isTx⟩      transactions ledger                             Binder:137–163
├─ ⟨sc-if isPerf⟩    perf chart + 4 stat tiles + yearly summary      Binder:165–229
│
├─ IRON RULE strip   (unconditional — shows on all tabs & empty)     Binder:231–234
└─ ⟨sc-if txOpen⟩    transaction modal overlay (z-100, fixed)        Binder:236–325
```

**Structural notes that matter for implementation**

- The **tab strip is unconditional** (Binder:68–80). In the empty state all three tabs still render and
  remain clickable, but every tab body is suppressed (`isHold`/`isTx`/`isPerf` all AND `!empty`), so all
  three tabs show the same empty card.
- The **view toggle is nested** inside `sc-if isHoldTab` (Binder:74–79), so it disappears on the
  transactions and performance tabs and in the empty state.
- The **modal is a sibling of the tab bodies**, not a child — it can be open over any tab, including the
  empty state (the empty card's CTA calls the same `openTx`, Binder:86).
- **Holdings and gallery are mutually exclusive renderings of the same sorted list** (`hs`, Binder:452).
  They are not two sections; they are one section with two templates.

### 2a. Holdings — table (Binder:108–135)

Three-band CSS-grid section: header band, `sc-for hRows` body, totals band. Grid template is computed
(`hGridCols`, Binder:492):

```
minmax(<card>px, 1fr)  <tier>px  <qty>px  <cost>px  <value>px  <pl>px  <pct>px
```

Only the Card column flexes; the other six are fixed pixel widths held in `state.hColW`
(defaults: card 220, tier 92, qty 60, cost 96, value 130, pl 130, pct 90 — Binder:411).

Every header cell is two elements: a clickable sort label (`flex: 1`, centred) and a `│` drag handle
(`cursor: col-resize`) — Binder:112.

### 2b. Holdings — gallery (Binder:90–106)

`display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 14px`.
Each tile: art well (`aspect-ratio: 325 / 450`, accent gradient background, `<image-slot>` overlay) →
name (link) → sub-line → value / P&L % row. Hover raises a box-shadow.

### 2c. Transactions (Binder:137–163)

Header bar (title + `AUDIT LOG` badge + right-aligned count) → column header band → `sc-for txRows`.
Grid template (`tGridCols`, Binder:512):

```
100px 70px minmax(220px,1fr) 92px 56px 92px  minmax(140px,1fr)  72px
date  kind  card              tier  qty  price  note              (edit)
```

The last two tracks are **hard-coded literals** — Note and the action column are not resizable
(see §6 for the aliasing defect in their handles).

### 2d. Performance (Binder:165–229)

Three stacked blocks:

1. **Portfolio vs market index** panel (Binder:166–204) — heading row (title, two legend swatches,
   "both indexed to 100 at first transaction (Jan '25)", right-aligned edge figure), then a 44px y-axis
   gutter beside a `<svg width="100%" height="230" viewBox="0 0 800 230" preserveAspectRatio="none">`,
   then a 3-label x-axis strip indented `margin-left: 52px`.
2. **Stat tiles** — `grid-template-columns: repeat(4, 1fr); gap: 10px` (Binder:205–213). Exactly 4.
3. **Yearly summary** table — `grid-template-columns: 90px 1fr 1fr 1fr 1fr` (Binder:214–228).

### 2e. Transaction modal (Binder:236–325)

Fixed full-screen scrim `rgba(20,19,26,0.45)`, `z-index: 100`, flex-centred. Panel is a **fixed 400px**
column, `padding: 18px 20px`, `gap: 12px`, radius 10, heavy shadow. Field order:

```
title row (modalTitle + ✕)
BUY | SELL segmented control            (always rendered, even in edit mode)
card field                              (three variants — see §5)
[ grader | grade ]  or  [ grade tier ]   |  Qty        grid: 1fr 90px
[ price ] [ date ]                                     grid: 1fr 1fr
[ note ]
helper text about edit ✎
[ Cancel ] [ Save ]                                    right-aligned
```

---

## 3. Data contract

Every rendered field, exhaustively. "Derived" = computed in `renderVals()`; "Literal" = hard-coded in the
prototype and therefore a **real value the application must supply**.

### 3.0 Shared formatters and palette

| Helper | Definition | Line |
|---|---|---|
| `money(n)` | `'$' + Math.round(n).toLocaleString('en-US')` — **whole dollars, no cents, anywhere on this screen** | 413 |
| `pcf(n, d)` | `(n >= 0 ? '+' : '−') + Math.abs(n/d*100).toFixed(1) + '%'` — 1 decimal, U+2212 minus | 465 |
| `seg(on)` | segmented-control colours: on → `{bg: acc, fg: card}`, off → `{bg: card, fg: mut}` | 448 |
| `PAL` | JS mirror of the CSS custom properties; 4 variants (light/dark × normal/CVD) resolved from `localStorage` keys `cardstock-theme` and `cardstock-cvd` | 332–339 |

Sign convention throughout: `+` for ≥ 0, `−` (U+2212) for negative. Positive uses `PAL.pos`, negative
uses `PAL.neg2`.

### 3.1 Chrome and header

| Field | Type | Source | Line |
|---|---|---|---|
| H1 | literal | `Binder` | 59 |
| `PRIVATE` badge | literal + tooltip | tooltip: "Binder data is strictly private — no social features, never shared" | 60 |
| CSV button label | derived | `csvLabel` = `'✓ Exported'` when `csvDone` else `'↓ Export CSV'` | 63, 535 |
| CSV button tooltip | literal | "Download every transaction as a CSV — date, card, grade, quantity, price, and note" | 63 |
| Add-transaction label | literal | `+ Add transaction` | 65 |
| Add-transaction tooltip | literal | "Log a buy or a sell — updates your cost basis and P&L immediately" | 65 |

### 3.2 Tabs

`tabs` (Binder:491) is a 3-element list built from `['holdings','transactions','performance']`.

| Field | Value |
|---|---|
| `tb.label` | the lowercase key itself — rendered verbatim in JetBrains Mono |
| `tb.tip` | holdings → "What you own now — quantity, cost basis, current value, and P&L per position"<br>transactions → "Every buy, sell, and correction, newest first"<br>performance → "Your binder against the market index since your first transaction" |
| `tb.bg` / `tb.fg` | from `seg(state.tab === t)` |
| `tb.pick` | `setState({ tab: t })` |

View toggle (Binder:76–77, values at 480–481): labels `table` / `gallery`; tooltips "Table view" /
"Gallery view — your collection as card art".

### 3.3 Holdings — the source record

> **Storage model (D-067, owner 2026-08-11): a holding is not stored. It is derived from
> transactions.** There is no `holdings` table. The shape below remains the contract the *screen*
> consumes — it is computed per request by aggregating that user's non-superseded transactions,
> grouped by card and tier.
>
> Two consequences for the build:
> - **The foreign key to `public.cards` attaches to `transactions.card_id`** (ADR-0001), at the
>   point data enters, since there is no holdings row to anchor it to.
> - **§7.4 becomes blocking rather than merely open.** A derived holding cannot compute `cost`
>   without a stated cost-lot rule, so FIFO / average cost / specific identification must be ruled
>   before any of this is implemented.
>
> There is also **one binder per user** (D-067) — no `binders` table; `user_id` sits directly on
> transactions.

Seeded shape (Binder:342–351), 8 rows. **This is the holding entity contract:**

| Field | Type | Meaning |
|---|---|---|
| `card` | string | card display name (also the key into `ACCENTS`) |
| `tier` | string | grade-tier label, e.g. `PSA 10`, `Grade 9`, `Grade 7`, `Grade 8`, `Raw` |
| `qty` | integer | units held |
| `cost` | number | **per-unit** cost basis (the column header calls it "Avg cost") |
| `cur` | number | **per-unit** current estimated value |
| `est` | boolean | true → the value is index-estimated, not sale-supported → `EST` badge |

Derived per holding (Binder:452):
- `value = cur * qty`
- `pl = (cur - cost) * qty` — **unrealized** P&L

### 3.4 Holdings table — columns

`hCols` (Binder:493). Seven columns; `name`, `arrow`, `sort`, `rs` per column.

| # | Header text | Resize key | Sort key | Cell content (`hRows`, Binder:498–507) |
|---|---|---|---|---|
| 1 | `Card` | `card` | `card` | `h.card`, rendered as `<a href="Cardstock Card.dc.html">` (Binder:117). 14px/500, ellipsised. |
| 2 | `Tier` | `tier` | `tier` | `h.tier` verbatim, mono 12.5px |
| 3 | `Qty` | `qty` | `qty` | `String(h.qty)`, mono 13px |
| 4 | `Avg cost` | `cost` | `cost` | `money(h.cost)` + `' ea'` **when `qty > 1`**. Cell tooltip: "What you paid never changes" (Binder:120) |
| 5 | `Current value` | `value` | `value` | `money(h.cur * h.qty)`, mono 13.5px/700, followed by the EST badge |
| 6 | `Unrealized ±` | `pl` | `pl` | `±$<abs(pl) toLocaleString> · ±X.X%`, where the % is `pl / (cost*qty)`. Coloured `plFg` |
| 7 | `% of binder` | `pct` | **`value`** | `(value / totV * 100).toFixed(1) + '%'`, muted |

**EST badge** (Binder:121): `display` is `inline-block` when `h.est` else `none`; text `EST`; amber
(`--warnInk` on `rgba(176,127,26,0.12)`); `cursor: help`; tooltip `estTip` =
"No recent sales in this tier — value estimated from index movement since the last observed sale"
(Binder:503).

**Totals band** (Binder:126–133, values 508–510):

| Cell | Content |
|---|---|
| col 1 | `totLabel` = `HOLD.length + ' positions'` (always plural) |
| col 2, 3 | **empty** — Tier and Qty are deliberately not totalled |
| col 4 | `totCost` = `money(Σ cost*qty)` — a **sum of extended cost** under a header that reads "Avg cost" |
| col 5 | `totValue` = `money(Σ cur*qty)` |
| col 6 | `totPl` = `±$abs(unreal) · pcf(unreal, totC)`, coloured `totPlFg` |
| col 7 | literal `100%` |

`unreal = totV − totC` (Binder:471). With the seed: `totV = 4596`, `totC = 3983`, `unreal = +613`.

### 3.5 Holdings — gallery tile

`galleryCards` (Binder:482–490), same order as the table.

| Field | Source |
|---|---|
| `name` | `h.card`, link to the Card page |
| `slotId` | `'art-' + card.toLowerCase().replace(/[^a-z0-9]+/g,'-')` — the `<image-slot id>` |
| `thumbBg` | `linear-gradient(160deg, <c1>, <c2>)` from `ACCENTS[card]`, fallback `['#D9DDE8','#B9C2D6']` |
| `sub` | `h.tier` + `' · ×N'` when `qty > 1` |
| `value` | `money(cur * qty)` |
| `pl` | **percentage only** — `±X.X%`; no dollar figure (unlike the table) |
| `plFg` | pos / neg2 |

`ACCENTS` (Binder:397–406) is a per-card two-stop gradient map; 8 entries seeded.
**The gallery tile has no EST badge** — see §7.

### 3.6 Transactions — the source record

Seeded shape (Binder:352–364), 11 rows, ids assigned `'tx' + i` (Binder:367).

| Field | Type | Meaning |
|---|---|---|
| `id` | string | stable key; `'tx<i>'` seeded, `'a' + Date.now()` for user-added (Binder:610) |
| `d` | string | ISO `YYYY-MM-DD`, **rendered verbatim — no locale formatting** |
| `k` | `'BUY'` \| `'SELL'` | transaction kind |
| `card` | string | card name |
| `tier` | string | grade-tier label at the time of the transaction |
| `q` | integer | quantity |
| `p` | number | unit price (paid for BUY, received for SELL) |
| `note` | string | free text, may be empty |
| `v` | boolean | **voided/superseded flag** — see §4 and §8; nothing in the prototype ever sets it `true` |

Effective list (Binder:472):
`txAll = state.added.concat(TX).map(t => txOverrides[t.id] ? {...t, ...txOverrides[t.id]} : t)`

So: user-added rows first (newest-added first), then the seeded ledger, with per-id overrides applied.

### 3.7 Transactions — columns

`tCols` (Binder:513): `Date · Type · Card · Tier · Qty · Price · Note · (blank)`.
**None of them are sortable** — unlike `hCols`, no `sort` handler is attached (compare Binder:493 vs 513).

`txRows` (Binder:514–533) per-row output:

| Field | Value |
|---|---|
| `date` | `t.d` raw ISO |
| `kind` | `t.v ? 'VOID' : t.k` |
| `card` | `t.card` |
| `tier` | `t.tier` |
| `qty` | `String(t.q)` |
| `price` | `money(t.p)` |
| `note` | `t.note \|\| '—'` |
| `kBg` / `kFg` / `kBd` | chip colours — VOID: amber `rgba(176,127,26,.12)` / `warnInk` / `rgba(176,127,26,.35)`; BUY: `posBg(0.10)` / `pos` / `posBg(0.3)`; SELL: `rgba(74,99,208,.10)` / `acc` / `rgba(74,99,208,.3)` |
| `strike` | `'line-through'` when `t.v` else `'none'` — applied to date, card, tier, qty, price, note but **not** to the kind chip |
| `op` | row opacity `0.62` when `t.v` else `1` |
| `voidTip` | whole-row tooltip. Voided: "Superseded by a correction — kept in the audit log, excluded from your totals". Live: "Hover the row and use ✎ to correct it — the original is kept in the audit log" |
| `voidShow` | **literal `'inline-block'`** — the edit button is always visible on every row |
| `voidIt` | opens the pre-filled edit modal (§5.6) |

Ledger header count: `txCount = txAll.length + ' transactions'` (Binder:511) — always plural.
`AUDIT LOG` badge tooltip (Binder:141): "Every edit is stored as a correction under the hood — the table
shows the current truth, the audit trail is kept".
Edit button: label `edit ✎`, `title="Edit this transaction"` (Binder:159).

### 3.8 Performance — the series

| Field | Value | Line |
|---|---|---|
| `BV` | binder index, 20 monthly points, `[100, 103, 101, … 151, 152]` | 365 |
| `IX` | market index, 20 monthly points, `[100, 101, 103, … 120, 121]` | 366 |
| `PMONTHS` | 20 labels `Jan '25` … `Aug '26` | 407 |
| `N` | `BV.length` = 20 | 468 |

Both series are **indexed to 100 at the first transaction month** (stated in-copy, Binder:171).

**Scale — shared across both series** (Binder:466–467): `mn = min(BV ∪ IX)`, `mx = max(BV ∪ IX)`.
Rendered as `pfYMax` / `pfYMin` in the 44px gutter (Binder:177–178, 537). With the seed: 152 / 100.

**Point projection** (`pts`, Binder:469), viewBox `0 0 800 230`:
```
x = (i / (N-1)) * 800
y = 222 − (v − mn) / (mx − mn) * 212
```
(1 decimal, `preserveAspectRatio="none"` so the SVG stretches horizontally.)

**Four polylines** (Binder:183–186), drawn in this z-order:

| Token | Slice | Stroke | Dash |
|---|---|---|---|
| `pfIxSolid` | `IX[0 … N-2]` | `--mut2`, 1.5 | — |
| `pfIxDash` | `IX[N-2 … N]` | `--mut2`, 1.5 | `4 4` |
| `pfBvSolid` | `BV[0 … N-2]` | `--acc`, 2 | — |
| `pfBvDash` | `BV[N-2 … N]` | `--acc`, 2 | `4 4` |

All use `vector-effect="non-scaling-stroke"`. **The final month's segment is dashed on both series** —
the month-to-date convention. A baseline `<line y=115>` in `--line4` spans the full width (Binder:182);
it is a mid-height rule, *not* the 100 gridline.

**Hollow current point** (Binder:188): 8×8 circle, `--card` fill, 1.5px `--acc` border, at
`left: 100%; top: {{pfHollowTop}}%` with `translate(-50%,-50%)`.
`pfHollowTop = ((222 − (BV[N-1]−mn)/(mx−mn)*212) / 230 * 100)` (Binder:540).
Tooltip: "Aug is month-to-date — the point firms up as the month's sales land, and finalizes when the
month closes".

**Legend** (Binder:169–171): `▬ Binder` (`--btn`) · `▬ Market index` (`--mut2`) · caption
"both indexed to 100 at first transaction (Jan '25)".

**Benchmark comparison figure** — `perfEdge` (Binder:536):
```js
'+' + (BV[N-1] - IX[N-1]) + 'pp vs index'
```
Seed → `+31pp vs index`. Rendered mono 13px/600 in `var(--pos)` (Binder:173).
**The `+` and the positive colour are both hard-coded** — see §7/§8.

**X-axis labels** (Binder:200–202) — three hard-coded literals `Jan '25`, `Oct '25`, `Aug '26` in a
`space-between` flex row, i.e. positioned at 0% / 50% / 100% regardless of where those months fall.

**Hover readout** (Binder:180, 189–196; values 541–545):

| Field | Value |
|---|---|
| `pfMove` | `f = clamp01((clientX − rect.left)/rect.width)`; `idx = Math.round(f * (N-1))`; setState only when changed |
| `pfOut` | `setState({ pfHov: null })` |
| `pfHovShow` | `pfHov != null` |
| `pfHovLeft` | `(pfHov / (N-1) * 100).toFixed(2)` — % offset of the 1px vertical rule |
| `pfHovMonth` | `PMONTHS[pfHov]` |
| `pfHovBv` | `String(BV[pfHov])`, rendered as `Binder <v>` in `--acc`, 700 |
| `pfHovIx` | `String(IX[pfHov])`, rendered as `Index <v>` in `--mut` |

The tooltip card is pinned at `top: 8px; left: 8px` of the plot area — it does **not** follow the cursor.

### 3.9 Performance — stat tiles

`perfStats` (Binder:546–551). Exactly 4, in fixed order. Each tile is `cursor: help` with `ps.tip` as the
whole-tile tooltip; renders `ps.k` (uppercase label) / `ps.v` (21px mono 700, `ps.fg`) / `ps.sub`.

| # | `k` | `v` | `sub` | `fg` | `tip` |
|---|---|---|---|---|---|
| 1 | `Realized P&L` | **literal** `+$612` | **literal** `3 closed sales` | **literal** `pos` | "Profit locked in on sold positions — sale proceeds minus what you paid. Never re-estimated." |
| 2 | `Unrealized ±` | **derived** `±$abs(unreal)` | `'on ' + HOLD.length + ' open positions'` | pos/neg2 by sign | "Current estimated value of holdings minus cost basis. Moves with the market; EST-badged positions are index-estimated." |
| 3 | `Win rate` | **literal** `67%` | **literal** `2 of 3 sales above cost` | `ink` | "Share of closed sales that sold for more than you paid." |
| 4 | `Avg hold` | **literal** `7.2 mo` | **literal** `across closed sales` | `ink` | "Average time between buy and sell on closed positions." |

Tile 2 is the **only** computed tile. Note `const realized = 612` (Binder:471) is declared and never
read — the tile uses the string literal. Definitions the application must implement:

- **Realized P&L** = Σ over closed sales of (proceeds − cost basis of the units sold). Never re-estimated.
- **Unrealized ±** = `Σ(cur·qty) − Σ(cost·qty)` over open positions. Includes EST-badged positions.
- **Win rate** = (# closed sales with proceeds > cost) / (# closed sales), rendered as an integer %.
- **Avg hold** = mean(sell date − buy date) over closed positions, rendered `N.N mo`.

### 3.10 Performance — yearly summary

Columns (Binder:217): `Year · Invested · Proceeds · Realized ± · Year-end value`.
`yearRows` (Binder:552–555) — 2 rows, **every figure a literal except the last cell**:

| `yr` | `inv` | `pro` | `rl` | `rlFg` | `end` |
|---|---|---|---|---|---|
| `2025` | `money(2097)` | `money(1044)` | `+$219` | pos | `money(2381)` |
| `2026 YTD` | `money(1552)` | `money(903)` | `+$393` | pos | **`money(totV)`** (derived) |

Semantics the application must implement per year: **Invested** = Σ BUY (price × qty) dated in the year;
**Proceeds** = Σ SELL (price × qty) dated in the year; **Realized ±** = realized P&L closed in the year
(the two rows sum to 612, matching tile 1); **Year-end value** = mark-to-market of open positions at
year end (current value for the in-progress year).

`rlFg` is per-row and sign-aware in shape (`{{ y.rlFg }}`, Binder:224), so a negative year is
representable here — unlike `perfEdge`.

### 3.11 Card corpus (BUY typeahead)

`CARDS` (Binder:378–396) — 17 entries of `{ name, set }`. This is the "tracked card" corpus the BUY
field must resolve against. Rendered per match: `cm.name` (500 weight) + `cm.set` (12px muted), tooltip
`'Log this transaction against <name> (<set>)'` (Binder:581).

### 3.12 Grade tier vocabulary offered by the modal — **exhaustive**

`GRADERS` (Binder:368–377):

```js
const halves = []; for (let n = 10; n >= 1; n -= 0.5) halves.push(String(n));
// halves = 10, 9.5, 9, 8.5, 8, 7.5, 7, 6.5, 6, 5.5, 5, 4.5, 4, 3.5, 3, 2.5, 2, 1.5, 1  (19 values)
GRADERS = {
  Raw: [],
  PSA: halves.filter(g => g !== '9.5'),      // 18
  CGC: ['10 Pristine'].concat(halves),       // 20
  BGS: ['10 Black Label'].concat(halves),    // 20
  TAG: ['10 Pristine'].concat(halves),       // 20
  ACE: halves.slice(),                       // 19
  SGC: ['10 Pristine'].concat(halves)        // 20
}
```

**7 graders** — `Raw · PSA · CGC · BGS · TAG · ACE · SGC` (`graderOpts`, Binder:589; the placeholder hint
at Binder:284 is likewise 7).

The stored tier label is `grader + ' ' + grade`, or `'Raw'` for Raw (Binder:594–595). Therefore the
**complete set of tier labels this screen can produce is 118**:

| Grader | Grades offered | Labels produced |
|---|---|---|
| Raw | *(none — select disabled)* | `Raw` (1) |
| PSA | 10, 9, 8.5, 8, 7.5, 7, 6.5 … 1 (**no 9.5**) | 18 |
| CGC | `10 Pristine`, then 10 … 1 in 0.5 steps | 20 |
| BGS | `10 Black Label`, then 10 … 1 in 0.5 steps | 20 |
| TAG | `10 Pristine`, then 10 … 1 in 0.5 steps | 20 |
| ACE | 10 … 1 in 0.5 steps | 19 |
| SGC | `10 Pristine`, then 10 … 1 in 0.5 steps | 20 |
| | | **118 total** |

**Coverage against the price series (D-003 six tiers, D-022 pooled-below-10).** Labels with a backing
price series: `Raw`; any grader's `7`, `8`, `9` (18 labels, pooled); any grader's `9.5` (5 labels — PSA
excluded); and `PSA 10`. That is **25 of 118**. The remaining **93 selectable labels have no price
series**:

- every grade from `1` to `6.5` in 0.5 steps, for all 6 graders — **72 labels**
- `7.5` and `8.5` for all 6 graders — **12 labels** (D-012 says "grades 1–6"; the half-steps 7.5 and 8.5
  are unserved too and D-012 does not name them)
- `CGC 10`, `CGC 10 Pristine`, `BGS 10`, `BGS 10 Black Label`, `TAG 10`, `TAG 10 Pristine`, `ACE 10`,
  `SGC 10`, `SGC 10 Pristine` — **9 labels** (D-012 lists 7; the HTML also offers `TAG 10 Pristine` and
  `SGC 10 Pristine`, which are absent from the canonical 19-value scale)

**The prototype declares a bucketing intent that the docs do not record.** `bucketOf` (Binder:414–423),
with the comment *"Raw slab label → internal grade bucket (valuation tier); the slab label is kept on the
transaction"*:

```js
if (label === 'Raw') return 'Raw';
const n = parseFloat(label.replace(/[^0-9.]/g, ''));
if (n >= 10)  return 'PSA 10';
if (n >= 9.5) return 'Grade 9.5';
if (n >= 9)   return 'PSA 9';
if (n >= 8)   return 'PSA 8';
return 'PSA 7';
```

Its 6 outputs map exactly onto the scraper's 6 `PriceTier` values. Consequences it would impose:
`BGS 10 Black Label` → **`PSA 10`**; `CGC 8.5` → `PSA 8`; every grade from 1 to 7.5 → **`PSA 7`**.
**`bucketOf` is never called** — `grep -n bucketOf` returns only its definition. It is a *stated design
intent*, not implemented behaviour, and its ≥10 → PSA 10 rule is precisely the move D-022/D-012 flags as
contentious. Treat as input to D-012, not as a resolution of it.

**Separately, the holdings tier *sort* uses a different vocabulary.** `tierRank` (Binder:451) indexes
into the canonical 19-value scale:

```
Raw · Grade 1..Grade 9 · Grade 9.5 · PSA 10 · CGC 10 · CGC 10 Prist. · TAG 10 · ACE 10 · SGC 10 · BGS 10 · BGS 10 Black
```

Any label the modal produces that is not in that list returns `indexOf → −1`. Since the picker emits
`PSA 9`, `CGC 8`, `BGS 3.5`, `CGC 10 Pristine`, `BGS 10 Black Label`, … and **not** `Grade 9`,
`CGC 10 Prist.`, `BGS 10 Black`, the two vocabularies barely overlap (`Raw`, `PSA 10`, `CGC 10`,
`TAG 10`, `ACE 10`, `SGC 10`, `BGS 10` are the only shared labels). See §8.

### 3.13 Modal fields

| Field | Token(s) | Type | Notes |
|---|---|---|---|
| Title | `modalTitle` | derived | `txEdit ? 'Edit transaction' : 'Add transaction'` (Binder:559) |
| Kind toggle | `buyBg/buyFg`, `sellBg/sellFg` | segmented | BUY active → `PAL.pos2`; SELL active → `PAL.acc` (Binder:574–575). Tooltips at Binder:245–246 |
| Card (BUY) | `txCard`, `cardBd`, `cardHint`, `cardHintFg` | text input | `id="tx-card-input"`, placeholder "Start typing a card name…", `autocomplete="off"`. Hint: `'✓ linked'` (pos) when `txCardOk` else `'(must link to a tracked card)'` (mut2). Border `posBg(0.45)` when linked (Binder:585–587) |
| Typeahead menu | `cardMenuOpen`, `cardMatches`, `cardNoMatch` | popover | absolute `top:56px`, `max-height:190px`, scrollable, ≤6 matches |
| Card (SELL-new) | `sellOpts`, `setTxSell` | `<select>` | placeholder option `Select a holding…` (`value=""`); each option `value=<index>`, label `card · tier · N held` (Binder:567). Label hint: "(from your holdings — you can only sell what the binder holds)" |
| Card (SELL-edit) | `editCardName` | read-only span | `st.txCard \|\| '—'`. Hint: "(from the original sale — correct price, qty, date, or note)" |
| Grader (BUY) | `txGrader`, `graderOpts`, `setTxGrader` | `<select>` | 7 options |
| Grade (BUY) | `txGrade`, `gradeOpts`, `setTxGrade`, `gradeOff`, `gradeBg`, `gradeFg` | `<select>` | disabled when grader = Raw; greyed (`PAL.bg`) and label dimmed to `mut3` |
| Grade tier (SELL) | `sellTier`, `sellTierFg`, `sellTierNote` | read-only span | `txEdit ? st.txTier : (sellIdx !== '' ? HOLD[sellIdx].tier : '—')`. Colour `ink` once resolved, else `mut3`. Note: `'(from the original sale)'` in edit, `'(from holding)'` otherwise (Binder:566, 569–570) |
| Qty | `txQty`, `qtyMax`, `qtyMaxNote`, `qtyBd` | `number` | `min="1"`, `max={{qtyMax}}`. `qtyMax` = held qty **only** for SELL-new with a selection, else `'99'`. `qtyMaxNote` = `'(max N)'` in the same case only. `qtyBd` = `neg2` when SELL-new ∧ selected ∧ qty > held (Binder:571–573) |
| Price | `txPrice`, `priceLabel` | `number` | placeholder `0.00`; label `'Price paid ($)'` for BUY, `'Price received ($)'` for SELL (Binder:576). **No red-border state — the only failure signal is the disabled Save** |
| Date | `txDate` | `date` | no `min`/`max`; state default `'2026-08-04'` (Binder:410) |
| Note | `txNote` | text | placeholder `e.g. auction win`; label `Note (optional)` |
| Helper | literal | — | "Mistakes are fixable — hit `edit ✎` on any row. Corrections are kept in the audit log under the hood." (Binder:318) |
| Cancel | `closeTx` | button | title "Close without saving this transaction" |
| Save | `saveOff`, `saveBg`, `saveCur`, `saveTx` | button | disabled = `!canSave`; bg `acc` enabled / `accMut` disabled; cursor `pointer` / `not-allowed`; title "Save this transaction — it appears in the ledger and updates your totals immediately" |

### 3.14 Component state — complete

`state` (Binder:409–412):

| Key | Initial | Purpose |
|---|---|---|
| `tab` | `'holdings'` | active tab |
| `hView` | `'table'` | holdings rendering |
| `sortKey` | `'value'` | holdings sort column |
| `sortDir` | `'desc'` | holdings sort direction |
| `pfHov` | `null` | hovered performance index, or null |
| `txOpen` | `false` | modal open |
| `csvDone` | `false` | CSV button confirmation flash |
| `txKind` | `'BUY'` | modal kind |
| `txCard` | `''` | modal card name |
| `txTier` | `'PSA 10'` | composed tier label saved on the row |
| `txGrader` | `'PSA'` | grader select |
| `txGrade` | `'10'` | grade select |
| `txQty` | `'1'` | qty (string) |
| `txPrice` | `''` | price (string) |
| `txDate` | `'2026-08-04'` | date |
| `txNote` | `''` | note |
| `added` | `[]` | user-created rows, newest first |
| `sellIdx` | `''` | index into `HOLD` for SELL-new |
| `txEdit` | `null` | id of the row being corrected, or null |
| `txOverrides` | `{}` | `id → replacement row` |
| `txCardOk` | `false` | BUY card resolved to the corpus |
| `hColW` | `{card:220, tier:92, qty:60, cost:96, value:130, pl:130, pct:90}` | holdings column widths |
| `tColW` | `{date:100, kind:70, card:220, tier:92, qty:56, price:92}` | ledger column widths |

---

## 4. States

### 4.1 Top-level view state

Derived flags (Binder:474–478), where `empty = props.emptyState ?? false`:

| Flag | Expression | Renders |
|---|---|---|
| `isEmpty` | `empty` | empty-state card (Binder:82) |
| `isHold` | `!empty ∧ tab='holdings' ∧ hView='table'` | holdings table |
| `isGallery` | `!empty ∧ tab='holdings' ∧ hView='gallery'` | art-tile grid |
| `isHoldTab` | `!empty ∧ tab='holdings'` | the table/gallery toggle |
| `isTx` | `!empty ∧ tab='transactions'` | ledger **and** the CSV button |
| `isPerf` | `!empty ∧ tab='performance'` | chart + tiles + yearly summary |

Five reachable page states: **empty**, **holdings/table**, **holdings/gallery**, **transactions**,
**performance** — times **modal open / closed**, which is orthogonal and available in all five.

**Empty state** (Binder:83–87) — trigger: the `emptyState` prop only. It is **not** data-driven;
`HOLD.length` is never consulted. Copy: "Log your first purchase" / "30 seconds, and your P&L starts
here." / `+ Add transaction`. The tab strip and the IRON RULE strip still render; the view toggle and
CSV button do not.

### 4.2 Holdings sort state

`sortKey ∈ {card, tier, qty, cost, value, pl}` × `sortDir ∈ {asc, desc}` = 12 states.
Arrow glyph: `' ▾'` desc, `' ▴'` asc, shown on the active column **except** `% of binder`, which is
suppressed by `c.k !== 'pct'` (Binder:495). Comparators (Binder:454–464): `card` = raw string compare
(case-sensitive); `tier` = `tierRank` index; `qty`/`cost` = the holding field; `pl` = extended P&L;
default/`value` = extended value.

### 4.3 Column-width state

Continuous. `startResize(key, bag)` (Binder:435–443): `mousedown` captures `clientX` and the current
width, attaches document-level `mousemove`/`mouseup`, and writes `Math.max(52, startW + dx)` — a **52px
floor**, no ceiling. Two independent bags: `hColW` (7 keys) and `tColW` (6 keys).

### 4.4 CSV export state

Binary with a self-clearing timer (Binder:534): `exportCsv` sets `csvDone: true`, then `setTimeout(…, 1800)`
resets it. Label flips `↓ Export CSV` → `✓ Exported` → back after 1.8s. **No file is produced** — no
Blob, no anchor, no download attribute anywhere in the file. This matches `HANDOFF.md:143`.

### 4.5 Performance hover state

`pfHov` is `null` (no crosshair, no tooltip) or an integer `0…N-1`. Entering the plot does not set it;
only `mousemove` does. `mouseleave` clears it.

### 4.6 Modal mode state — the three (really four) modes

Mode is the product of `txKind` and `txEdit`:

| Mode | Condition | Token | Card field | Tier field | Qty cap |
|---|---|---|---|---|---|
| **BUY-new** | `txKind='BUY' ∧ !txEdit` | `isBuy` (Binder:562) | typeahead text input, must resolve | grader + grade selects | `max="99"`, uncapped in `canSave` |
| **SELL-new** | `txKind='SELL' ∧ !txEdit` | `isSellNew` (563) | `<select>` over holdings | read-only, from the holding | `max` = held qty; enforced in `canSave` |
| **SELL-edit** | `txKind='SELL' ∧ txEdit` | `isSellEdit` (564) | read-only span, from the original | read-only, `st.txTier` | `max="99"`, only `≥ 1` enforced |
| **BUY-edit** | `txKind='BUY' ∧ txEdit` | falls through `isBuy` | typeahead text input (pre-filled, pre-linked) | grader + grade selects | uncapped |

The docs name three modes; the HTML has four. **BUY-edit is a real, reachable state** — it is what the
pencil produces on any BUY row (Binder:522–532 sets `txKind: t.k`, which is `'BUY'` for 8 of the 11
seeded rows).

`isBuy` is `txKind === 'BUY'` with **no `txEdit` guard**, so BUY-edit renders the full typeahead + grader
+ grade block. `isSell` (`txKind === 'SELL'`) gates the read-only tier span for **both** sell modes
(Binder:298–302).

### 4.7 Card-resolution state (BUY)

| State | Condition | Rendering |
|---|---|---|
| unresolved, idle | `txCard.trim().length < 2` | no menu; hint "(must link to a tracked card)"; neutral border |
| unresolved, searching | `!txCardOk ∧ length ≥ 2 ∧ matches > 0` | menu open with ≤6 matches (Binder:579–580) |
| unresolved, no match | `length ≥ 2 ∧ matches = 0` | menu open showing "No matching card — binder entries must link to a card we track." (Binder:258, 584) |
| resolved | `txCardOk` | menu closed; hint `✓ linked` in `pos`; border `posBg(0.45)` |

Typing anything (`setTxCard`, Binder:578) sets `txCardOk: false` — **editing a resolved name un-resolves
it**. Only clicking a menu row resolves (Binder:582).

### 4.8 Qty validation state (SELL-new only)

| State | Condition | Rendering |
|---|---|---|
| no holding chosen | `sellIdx === ''` | no `(max N)` note, `max="99"`, tier shows `—` in `mut3`, Save disabled |
| legal | `1 ≤ qty ≤ held` | `(max N)` note, neutral border, Save enabled (if price > 0) |
| over max | `qty > held` | `(max N)` note, border `PAL.neg2`, **Save disabled** |

### 4.9 Save-enabled state

`canSave(st)` (Binder:424–431) — verbatim:

```js
canSave(st) {
  if (!(parseFloat(st.txPrice) > 0)) return false;              // 1
  if (st.txKind === 'BUY') return !!st.txCardOk;                // 2
  if (st.txEdit) return (parseInt(st.txQty, 10) || 0) >= 1;     // 3
  if (st.sellIdx === '') return false;                          // 4
  const q = parseInt(st.txQty, 10) || 0;
  return q >= 1 && q <= this.HOLD[+st.sellIdx].qty;              // 5
}
```

Evaluated in order, the **exact save-blocked conditions** are:

1. **Always:** `parseFloat(txPrice) > 0` must hold. Blank, `0`, negative, or non-numeric → blocked.
   No upper bound. Note `parseFloat('12abc') === 12`, so a trailing-garbage price passes.
2. **BUY (new *or* edit):** additionally `txCardOk === true`. **Qty is not checked at all** in this
   branch — `''`, `0`, `-5`, `abc` all pass, and `saveTx` silently coerces via `parseInt(qty,10) || 1`
   (Binder:605), writing **qty 1**.
3. **SELL-edit:** additionally `parseInt(txQty,10) >= 1`. **No upper bound** — a correction may set a
   quantity larger than was ever held, and `qtyBd` never turns red in this mode.
4. **SELL-new, nothing selected:** blocked outright.
5. **SELL-new, holding selected:** additionally `1 ≤ qty ≤ HOLD[sellIdx].qty`. The over-max case blocks
   Save *and* reddens the border.

**Never validated in any mode:** date (future dates accepted; empty date accepted), note, tier.

### 4.10 Void / superseded row state — specified but unreachable

The ledger has a complete VOID rendering (Binder:151–159, 515–520): kind chip reads `VOID` in amber,
every text cell gets `line-through`, the row drops to `opacity: 0.62`, and the row tooltip becomes
"Superseded by a correction — kept in the audit log, excluded from your totals".

It is gated on `t.v`. **Nothing sets `t.v` to `true`.** All 11 seeded rows carry `v: false`
(Binder:353–363); `saveTx` writes `v: false` on both the new-row and override paths (Binder:605).
So the VOID state is *designed and styled but unreachable in the prototype* — see §8.

### 4.11 Theme / colour-vision state

Set before paint by an inline script (Binder:35) reading `localStorage`: `cardstock-theme === 'dark'` →
`data-theme="dark"`; `cardstock-cvd === '1'` → `data-cvd="1"`. `PAL` (Binder:332–339) recomputes the
same four palettes in JS for the inline-computed colours. Four combinations; the screen has no in-page
control for either (they live on Profile).

---

## 5. Interactions

### 5.1 Header controls

| Control | Line | Consequence |
|---|---|---|
| Logo / wordmark | 41 | → `Cardstock Home.dc.html` |
| Nav links | 45–49 | → Home / Screener / Charts / Binder (self) / Browse |
| `<cardstock-search>` | 52 | shared web component; `/` focuses, Esc clears+blurs |
| Avatar `O` | 53 | → `Cardstock Profile.dc.html` |
| `↓ Export CSV` | 63 | `exportCsv` → label flips to `✓ Exported` for 1800ms. **No file generated.** Visible only on the transactions tab |
| `+ Add transaction` | 65 | `openTx` → `setState({ txOpen: true })`. Opens in whatever mode the state currently holds (see 5.7) |

### 5.2 Tab and view toggles

| Control | Line | Consequence |
|---|---|---|
| `holdings` / `transactions` / `performance` | 71 | `setState({ tab })`. No URL change — the hash is read once at mount and never written |
| `table` | 76 | `setState({ hView: 'table' })` |
| `gallery` | 77 | `setState({ hView: 'gallery' })` |

The view choice persists while switching tabs (it lives in state, not in the tab).

### 5.3 Holdings table

| Control | Line | Consequence |
|---|---|---|
| Column label click | 112, 496 | `sortKey = c.s`; `sortDir` flips desc↔asc if the key was already active, else resets to `desc`. Tooltip on every label: "Click to sort" |
| `% of binder` label click | 493 | sorts by **`value`** (its `s` is `'value'`), and shows **no arrow**. Clicking it toggles the direction of the Current-value sort |
| `│` drag handle | 112, 435 | live column resize, floor 52px; document-level listeners removed on mouseup. Tooltip "Drag to resize" |
| Card name link | 117 | → `Cardstock Card.dc.html` (same href for every row — prototype placeholder) |
| `EST` badge hover | 121 | `cursor: help`, tooltip `estTip` |
| `Avg cost` cell hover | 120 | tooltip "What you paid never changes" |

### 5.4 Gallery

| Control | Line | Consequence |
|---|---|---|
| Tile hover | 93 | `box-shadow: 0 6px 20px rgba(20,19,26,0.10)`, 0.15s ease |
| Card name link | 97 | → `Cardstock Card.dc.html` |

The tile body is not clickable — only the name is a link.

### 5.5 Transactions ledger

| Control | Line | Consequence |
|---|---|---|
| Row hover | 151 | whole-row tooltip `voidTip` |
| `│` drag handles | 147 | resize `tColW`. **The Note and action handles both target `price`** (Binder:513) — dragging either resizes the Price column |
| `edit ✎` | 159, 522 | opens the pre-filled correction modal — see 5.6 |

No sorting, no filtering, no pagination, no row selection, no delete.

### 5.6 The correction flow — what the HTML actually does

`t.voidIt` (Binder:522–532), despite the vestigial name, performs **no void**. It:

1. Splits `t.tier` on spaces and reverse-engineers the pickers:
   `grader = (tier === 'Raw') ? 'Raw' : (GRADERS[parts[0]] ? parts[0] : 'PSA')`;
   `grade = grader === 'Raw' ? '' : (GRADERS[parts[0]] ? parts.slice(1).join(' ') : tier.replace('Grade ',''))`.
2. Sets `txEdit: t.id`, **`txCardOk: true`** (so a BUY-edit is immediately saveable), `txOpen: true`,
   and pre-fills `txKind`, `txCard`, `txTier`, `txGrader`, `txGrade`, `txQty`, `txPrice`, `txDate`,
   `txNote` from the row. `sellIdx` is cleared to `''`.
3. The modal title becomes **"Edit transaction"** (Binder:559).

Saving in edit mode (Binder:606–609) writes `txOverrides[txEdit] = row` — an **in-place replacement keyed
by id**. It does **not** append a row, does **not** mark the original `v: true`, and does **not** change
`txCount`. The tab is not switched.

**Confirmed against the docs:** the pencil does open a pre-filled modal; void-and-re-enter is absent from
the UI; the badge reads `AUDIT LOG` (Binder:141), never `IMMUTABLE` — `grep -i immutable` on the file
returns nothing. What the docs get wrong is the *representation*: no superseded row is produced, so the
struck-through 62%-opacity rendering never appears (§4.10, §8).

**Prefill defects** (real, reproducible):
- `'Grade 9.5'` → grader `PSA`, grade `'9.5'`, but PSA's option list explicitly **excludes** `9.5`.
- `'CGC 10 Prist.'` → grade `'10 Prist.'`; CGC's list has `'10 Pristine'`.
- `'BGS 10 Black'` → grade `'10 Black'`; BGS's list has `'10 Black Label'`.
  In all three the `<select>`'s bound value matches no `<option>`.
- `'Grade 9'`, `'Grade 7'`, `'Grade 8'` → grader is forced to **`PSA`**, silently asserting a grading
  company the original record did not name. This is exactly the neutrality problem ADR-0005 forbids
  (D-022).

### 5.7 Modal controls

| Control | Line | Consequence |
|---|---|---|
| Scrim click | 237 | `closeTx` |
| Panel click | 238 | `stopClick` — `e.stopPropagation()`, so inner clicks do not close |
| `✕` | 242 | `closeTx` |
| `Cancel` | 320 | `closeTx` |
| `BUY` | 245, 561 | `setState({ txKind: 'BUY' })` — **nothing else is reset** |
| `SELL` | 246, 561 | `setState({ txKind: 'SELL', sellIdx: '', txQty: '1' })` |
| Card input | 251, 578 | `setState({ txCard: value, txCardOk: false })` |
| Typeahead row | 255, 582 | `setState({ txCard: c.name, txCardOk: true })` — closes the menu by making `cardMenuOpen` false |
| Holding `<select>` | 266, 568 | `setState({ sellIdx: value, txQty: '1' })` |
| Grader `<select>` | 283, 594 | `txGrader = g`; `txGrade = GRADERS[g][0] ?? ''`; `txTier = (g === 'Raw') ? 'Raw' : g + ' ' + first` |
| Grade `<select>` | 290, 595 | `txGrade = value`; `txTier = txGrader + ' ' + value` |
| Qty / Price / Date / Note | 304–316, 596–599 | plain state writes, no coercion, no validation |
| `Save` | 321, 603 | see below |

**`closeTx`** (Binder:558) resets: `txOpen:false, txEdit:null, txCard:'', txPrice:'', txNote:'', sellIdx:'', txCardOk:false`.
It **does not** reset `txKind`, `txQty`, `txTier`, `txGrader`, `txGrade`, or `txDate`. So closing a SELL
and reopening lands you in SELL mode with the previous qty and date still set.

**`saveTx`** (Binder:603–615):

```js
sell = (txKind === 'SELL' && !txEdit) ? HOLD[+sellIdx] : null
row  = { d: txDate, k: txKind,
         card: sell ? sell.card : txCard.trim(),
         tier: sell ? sell.tier : txTier,
         q: parseInt(txQty,10) || 1,
         p: parseFloat(txPrice),
         note: txNote.trim(), v: false }
```

- **Edit path:** `txOverrides[txEdit] = row`; close and reset. Tab unchanged.
- **New path:** `row.id = 'a' + Date.now()`; `added = [row, ...added]`; close, reset, and
  **`tab: 'transactions'`** — saving always navigates the user to the ledger.

Two consequences worth designing around:

- **New rows are prepended regardless of date.** A back-dated purchase lands at the top of a ledger that
  is otherwise date-descending. There is no re-sort.
- **`HOLD` is never mutated.** A BUY creates no holding; a SELL decrements no quantity. Holdings, totals,
  the performance chart, the stat tiles, and the yearly summary are all static seed data. The
  "updates your cost basis and P&L immediately" copy on both buttons (Binder:65, 321) describes the
  intended application behaviour, not the prototype's.

### 5.8 Mode switching inside an edit — undocumented and live

The BUY/SELL segmented control is rendered **unconditionally** (Binder:244–247) — it is outside every
`sc-if` and is never disabled. In edit mode it remains fully interactive:

- Editing a BUY and clicking `SELL` → `isSellEdit` becomes true (since `txEdit` is set), the card
  collapses to a read-only span showing the original name, and `canSave` switches to the `txEdit` branch.
  Saving overwrites the row with `k: 'SELL'`.
- Editing a SELL and clicking `BUY` → `isBuy` becomes true, the typeahead reappears pre-filled and
  pre-linked (`txCardOk` was set true by `voidIt`), and Save overwrites the row with `k: 'BUY'`.

**A correction can therefore change a transaction's kind.** No document mentions this.

---

## 6. Rules and invariants

1. **Entered prices never change; current values are estimates.** Stated on-screen as the IRON RULE
   (Binder:231–234), reinforced by the Avg-cost cell tooltip "What you paid never changes" (Binder:120)
   and the Realized P&L tooltip "Never re-estimated" (Binder:547).
2. **`EST` marks index-estimated values.** Badge shown iff `h.est` (Binder:121, 502). Meaning is fixed by
   `estTip`: "No recent sales in this tier — value estimated from index movement since the last observed
   sale". Both the IRON RULE strip and the Unrealized tooltip reference it.
3. **Binder data is private.** `PRIVATE` badge, "no social features, never shared" (Binder:60).
4. **A BUY must link to a tracked card.** Free text alone cannot be saved; only a corpus pick sets
   `txCardOk` (Binder:582), and `canSave` requires it (Binder:426). Rejection copy: "No matching card —
   binder entries must link to a card we track."
5. **You cannot sell what you do not hold.** SELL-new's card field is a `<select>` over `HOLD` only
   (Binder:266–271); its tier is inherited read-only; `canSave` caps qty at the held quantity
   (Binder:430). Copy: "(from your holdings — you can only sell what the binder holds)".
6. **Price must be strictly positive in every mode** (Binder:425). This is the only universal gate.
7. **Corrections are edits, not voids, at the UI layer.** Pencil → pre-filled modal → in-place override
   keyed by transaction id. Badge is `AUDIT LOG`; the word "immutable" does not appear in the file.
   Only two kinds are user-selectable: `BUY` and `SELL`.
8. **The ledger row count is invariant under correction.** `txCount` counts `txAll`, and overrides
   replace rather than append (Binder:472, 511).
9. **Both performance series share one y-scale**, computed from the union of `BV` and `IX`
   (Binder:466–467). The comparison is only meaningful because of this.
10. **Both series are indexed to 100 at the first transaction month** (Binder:171); the benchmark figure
    is a **percentage-point difference of index levels**, not a return ratio (Binder:536).
11. **The final month is provisional on both series** — dashed last segment plus a hollow endpoint dot
    on the binder line, with the "month-to-date … finalizes when the month closes" tooltip
    (Binder:184, 186, 188). This is the same convention the Card and Charts screens use.
12. **Holdings and gallery always show the same rows in the same order** — both consume `hs`
    (Binder:452, 482, 498).
    *Challenged and upheld, 2026-08-11.* A drag-to-arrange gallery with a saved arrangement was
    proposed, designed, and withdrawn by the owner the same day — see **D-068** for what the
    exploration established, so it need not be redone. **This invariant stands.**
13. **Whole-dollar display everywhere.** `money()` rounds (Binder:413). Percentages carry exactly one
    decimal (`pcf`, `pct`, `toFixed(1)`).
14. **Column widths have a 52px floor and no ceiling** (Binder:439).
15. **The empty state is prop-driven, not data-driven** — `props.emptyState`, never `HOLD.length`
    (Binder:447).
16. **The screen has exactly one deep link:** `#performance` (Binder:433).

**Known defects in the prototype — do not port these:**

- `tGridCols` (Binder:512) builds the grid by string-joining widths and then calling
  `.replace(tColW.card + 'px', 'minmax(…)')`. `String.replace` hits the **first** match, so if a user
  resizes Date (or Kind, Tier, Qty, Price) to exactly the Card width, the wrong track becomes flexible.
- `tCols` (Binder:513) gives the Note and action columns the resize key `'price'`, aliasing three
  handles onto one width.
- `perfEdge` (Binder:536) hard-codes the `+` sign, and Binder:173 hard-codes `var(--pos)`. An
  under-performing binder renders `+-7pp vs index` in green.
- `voidShow` (Binder:521) is the literal `'inline-block'`, while `voidTip` (Binder:520) tells the user to
  "hover the row" — the pencil is in fact always visible.
- The read-only spans in the modal (Binder:276, 300) hard-code `border: 1px solid #EBEBE7`, and the
  hover crosshair (Binder:190) hard-codes `rgba(28,28,30,0.22)` — neither adapts to dark theme.
- `bucketOf` (Binder:415) and `const realized` (Binder:471) are dead code.
- The x-axis labels (Binder:200–202) are three literals in a `space-between` row, so `Oct '25`
  (index 9 of 19 ≈ 47.4%) is drawn at 50%.
- The `2026 YTD` invested figure (`$1,552`, Binder:554) does not reconcile with the seeded 2026 BUY rows
  (228 + 540 + 88×3 + 232×2 = **1,496**). Confirms the yearly table is literal, not derived.
- Copy is unconditionally plural: `'N transactions'`, `'N positions'`.

---

## 7. Open questions

1. **D-012 — how is a holding valued when its tier has no price series?** Unresolved, and the HTML makes
   it larger than D-012 states: **93 of the 118 selectable tier labels have no series** (§3.12), and the
   picker offers half-grades (`7.5`, `8.5`) and two extra grader-10 variants (`TAG 10 Pristine`,
   `SGC 10 Pristine`) that D-012 does not enumerate. `bucketOf`'s dormant `n >= 10 → 'PSA 10'` rule would
   value a `BGS 10 Black Label` at the PSA 10 series — the move D-022 records the owner rejecting. Needs
   an explicit ruling before any valuation code is written.
2. **Which tier vocabulary is canonical for a binder record?** The modal emits `grader + ' ' + grade`
   (118 labels); `tierRank` sorts the canonical 19; the seeded holdings use the canonical 19; the seeded
   transactions use the canonical 19. Three vocabularies, one screen. Are they one list, or a slab label
   plus a valuation bucket (as `bucketOf`'s comment implies)?
3. **Does the market index exist?** D-004 says there is no index table and no metrics store in the
   scraper DB, yet this screen's entire performance tab and its `EST` semantics depend on one. Blocking.
4. **✅ Resolved by D-074 — the cost-lot model is FIFO.** A SELL consumes open lots oldest-first;
   realized P&L is proceeds minus the FIFO cost of the units sold; remaining lots keep their own
   purchase dates and prices. **Avg hold** (§3.9) is computable because FIFO names the consumed lot
   — average cost would have destroyed the buy date and left that tile undefined, which is what
   decided it. The `Avg cost` column header stays honest: it is the average of the lots still held,
   a display question independent of the accounting method. *Original finding:* all
   three are literals here. A SELL
   realizes against *which* purchase lot — FIFO, average cost, or specific identification? The
   "Avg cost" header hints at average cost; nothing states it. `HOLD` carries a single `cost` per
   card+tier, which is consistent with average cost but does not prove it.
5. **✅ Resolved by D-067 — corrections cascade automatically.** Because holdings and the derived
   figures are computed from transactions rather than stored, editing a historical BUY flows
   through to cost basis, realized P&L, the yearly summary, and the `BV` series with nothing to
   keep in step by hand. This was a significant reason for choosing derivation. *Original finding:*
   editing a historical BUY changes cost basis, therefore realized P&L, the yearly summary, and the
   `BV` series — and the prototype recomputes none of it.
6. **Should a correction be allowed to change a transaction's kind?** It currently can (§5.8).
7. **Should SELL-edit re-validate against holdings?** It currently accepts any qty ≥ 1 with no cap
   (§4.9 rule 3), letting a correction claim a sale larger than the position.
   **Sharpened by D-074:** under FIFO an oversell is a sale with no remaining lots to consume, so it
   is not merely untidy — it is unrepresentable, and the write path must reject it rather than
   record something the cost-basis calculation cannot evaluate.
8. **BUY qty is unvalidated and silently coerced to 1** (§4.9 rule 2). Intentional leniency or a gap?
9. **CSV file generation.** The affordance exists; the payload is specified only by the button tooltip
   ("date, card, grade, quantity, price, and note"). Open: does it export `txAll` (post-override current
   truth) or the full audit trail including superseded rows? The tooltip says "every transaction."
10. **Ordering of user-added rows.** New rows prepend regardless of date (§5.7). Should the ledger sort
    by date, or by entry order?
11. **`EST` in gallery view.** The badge exists only in the table (Binder:121); the gallery tile
    (Binder:99–102) shows a bare value. Same data, one honesty marker missing.
12. **Modal accessibility.** No `role="dialog"`, no `aria-modal`, no focus trap, no Escape handler
    (only scrim click and `✕`), no keyboard navigation in the typeahead menu (no arrow/Enter handling —
    Binder:252–261). Fixed 400px width with no `max-height` or internal scroll.
13. **Persistence scope.** `DISPLAY_VOCABULARY.md:203` says density persists per device via
    localStorage. This screen reads localStorage only for theme and CVD (Binder:35); `hView`, sort, and
    column widths are in-memory and reset on reload.
14. **Currency and locale.** `toLocaleString('en-US')` and `$` are hard-coded throughout; whole dollars
    only. Is multi-currency ever in scope?
15. **Date format.** ISO `YYYY-MM-DD` is rendered raw in the ledger (Binder:152) while the performance
    axis uses `Mon 'YY`. No stated convention.

---

## 8. Contradictions found

Tier-1 HTML vs the derived docs. Per `CLAUDE.md`, the HTML wins; these are recorded, not averaged.

| # | Claim | Source doc:line | What the HTML actually does |
|---|---|---|---|
| 1 | "Save is blocked until: price > 0 **and** (BUY: card resolved · SELL-new: a holding selected · SELL-edit: qty ≥ 1)." | `DISPLAY_VOCABULARY.md:188` | **Incomplete on two counts.** SELL-new also requires `1 ≤ qty ≤ HOLD[sellIdx].qty` — the cap is a save gate, not just a red border (`Binder:430`). And BUY does **not** check qty at all; `''`/`0`/`-5`/`abc` all pass and `saveTx` coerces to 1 (`Binder:426, 605`). The summary also omits **BUY-edit**, a fourth mode governed by the BUY rule. |
| 2 | "Tier \| grader + grade pickers, **full 19-value scale**" | `DISPLAY_VOCABULARY.md:182` | The picker is a **7 × N cross product yielding 118 labels**, not a 19-value list (`Binder:368–377`). It emits half-grades at every 0.5 step from 1.5 to 9.5 and grader-specific 10s (`CGC 10 Pristine`, `BGS 10 Black Label`, `TAG 10 Pristine`, `SGC 10 Pristine`). |
| 3 | Canonical scale = "Raw · Grade 1–9 · Grade 9.5 · PSA 10 · CGC 10 · CGC 10 Prist. · TAG 10 · ACE 10 · SGC 10 · BGS 10 · BGS 10 Black (19 values)", applied to the "Binder tier picker + holdings labels + tier sort rank" | `DESIGN_NOTES.md:77` | True of the **sort rank only** — `tierRank` (`Binder:451`) is exactly that 19-value list. The **picker** emits a different vocabulary (row 2), so most picker output ranks `−1`. Labels also differ in spelling: picker `CGC 10 Pristine` / `BGS 10 Black Label` vs canonical `CGC 10 Prist.` / `BGS 10 Black`. Only 7 labels are shared. |
| 4 | "Superseded rows render struck-through at 62% opacity and are excluded from totals — kept under the `AUDIT LOG` badge, never deleted." | `DISPLAY_VOCABULARY.md:189` | The rendering **exists and is fully styled** (`Binder:151, 515–520`) but is **unreachable**: it is gated on `t.v`, all 11 seeded rows are `v: false` (`Binder:353–363`), and `saveTx` writes `v: false` on both paths (`Binder:605`). Saving a correction writes `txOverrides[id] = row` — an **in-place replacement**, no superseded row, `txCount` unchanged (`Binder:472, 511, 606–609`). |
| 5 | "**No VOID rows in UI.**" | `DESIGN_NOTES.md:63` | True in effect (no reachable VOID row), but the HTML **retains the complete VOID render path** — `VOID` chip text, amber chip colours, `line-through`, `opacity: 0.62`, and the "Superseded by a correction" tooltip (`Binder:515–520`). The handler and visibility props are still named `voidIt` / `voidShow` / `voidTip` (`Binder:520–522`). "Removed from the UI" and "unreachable but fully specified" are different facts. |
| 6 | "Quantity \| BUY: **1–99**" | `DISPLAY_VOCABULARY.md:183` | `max="99"` is on the input (`Binder:304, 571`), but it is **not enforced by `canSave`** for BUY (`Binder:426`) and `saveTx` accepts whatever `parseInt` yields (`Binder:605`). The `1–99` range is an HTML attribute hint, not a validation rule. |
| 7 | "SELL \| **1 … current holding qty** (over-max turns the field's border red and blocks save)" | `DISPLAY_VOCABULARY.md:183` | Correct for **SELL-new only**. In **SELL-edit** `qtyMax` falls back to `'99'`, `qtyMaxNote` is empty, `qtyBd` never reddens, and `canSave` requires only `qty ≥ 1` (`Binder:427, 571–573`). A correction can claim a larger sale than the position. |
| 8 | "SELL is holdings-constrained: … tier read-only from holding, **qty capped (red past max), Save disabled until legal**." | `DESIGN_NOTES.md:64` | Accurate for SELL-new — this note is **more accurate than `DISPLAY_VOCABULARY.md:188`**, which drops the cap. Recorded so the more precise Tier-2 statement is not lost when reconciling row 1. |
| 9 | "Kinds: `BUY` · `SELL`. No other kinds exist at the UI layer (**corrections are edits, not a third kind**)." | `DISPLAY_VOCABULARY.md:177` | Confirmed for what the user can *pick* — the segmented control offers only BUY and SELL (`Binder:245–246`) and `saveTx` writes `k: txKind` (`Binder:605`). But `txRows` can render a **third chip value, `VOID`** (`Binder:515`), and the docs do not mention that the BUY/SELL toggle stays live during a correction, so **an edit can change a row's kind** (`Binder:244–247`, §5.8). |
| 10 | "the ✎ control on any ledger row opens the same modal pre-filled" | `DISPLAY_VOCABULARY.md:189` · `HANDOFF.md:105` | **Confirmed** (`Binder:522–532`), with defects the docs do not record: `Grade 9.5` → PSA (whose list excludes 9.5), `CGC 10 Prist.` → `10 Prist.` (list has `10 Pristine`), `BGS 10 Black` → `10 Black` (list has `10 Black Label`) — all bind a `<select>` to a non-existent option; and `Grade N` labels are silently reassigned grader **`PSA`**, asserting a company the record never named (contra ADR-0005 / D-022). |
| 11 | "the badge reads `AUDIT LOG`, not `IMMUTABLE`" | `HANDOFF.md:105` · `DESIGN_NOTES.md:63, 82` | **Confirmed.** `Binder:141` renders `AUDIT LOG`; `grep -i immutable` on the file returns nothing. |
| 12 | "**Void + re-enter REJECTED** … transactions get a plain edit ✎ per row → modal pre-filled → Save updates in place." | `DESIGN_NOTES.md:63` · `HANDOFF.md:105` | **Confirmed** — `Binder:606–609` literally updates in place via `txOverrides`. The docs' own follow-on sentence ("Superseded rows render struck-through…", row 4) contradicts this; the HTML sides with "updates in place". |
| 13 | "**CSV export.** The control and its affordance exist; the file generation is left to the application." | `HANDOFF.md:143` | **Confirmed.** `exportCsv` only toggles `csvDone` with an 1800ms reset (`Binder:534–535`); no Blob, anchor, or download attribute anywhere in the file. |
| 14 | "**Export CSV** (transactions tab) emits date, card, grade, quantity, price, note for every row." | `DISPLAY_VOCABULARY.md:191` | Tab gating and the field list are **confirmed** by the `sc-if isTx` wrapper and the button tooltip (`Binder:62–63`) — but note the tooltip says "grade" while the ledger column is "Tier", and "every transaction" is ambiguous about superseded rows. |
| 15 | "Tabs: holdings · transactions · performance. Holdings additionally offers table / gallery density." | `DISPLAY_VOCABULARY.md:190` · `DESIGN_NOTES.md:62` | **Confirmed** (`Binder:70–79`), with one structural correction: the **tab strip renders even in the empty state** (it is outside `sc-if isEmpty`), where all three tabs show the same empty card. |
| 16 | "performance (binder vs market index, **both indexed to 100 at first transaction**)" | `DISPLAY_VOCABULARY.md:190` | **Confirmed** in copy (`Binder:171`) and in data (`BV[0] = IX[0] = 100`, `Binder:365–366`). Not recorded anywhere: the two series share **one y-scale** derived from their union (`Binder:466–467`), and the last segment of **both** is dashed. |
| 17 | "Density and theme choices **persist per device (localStorage)**, not per account." | `DISPLAY_VOCABULARY.md:203` | Only **theme and CVD** are read from localStorage (`Binder:35`). The holdings `table`/`gallery` density is plain component state (`Binder:409`) with no persistence — it resets on reload. |
| 18 | "gallery = large card-art tiles w/ accent-gradient image-slots, **value + unrealized %**" | `DESIGN_NOTES.md:62` | **Confirmed** (`Binder:99–102, 488`) — and this is the contradiction with the honesty rule: the gallery shows value with **no `EST` badge**, while the table badges the same holding (`Binder:121`). The IRON RULE strip promises `EST` badging and renders on the gallery view too. |
| 19 | "Binder … `/binder`" (single route) | `HANDOFF.md:74` | **Confirmed**, plus an undocumented deep link: `componentDidMount` routes `#performance` to the performance tab (`Binder:432–434`). Tab changes never write back to the URL. |
| 20 | D-012 enumerates the unvalued tiers as "**grades 1–6**" and "every non-PSA 10 — CGC 10, CGC 10 Pristine, BGS 10, BGS 10 Black, SGC 10, TAG 10, ACE 10" (7 labels). | `DECISIONS.md:399` (D-012) | **Understates the surface.** The picker also offers **`7.5` and `8.5`** for all 6 graders (12 more unserved labels), and **9 non-PSA grade-10 labels**, not 7 — it adds `TAG 10 Pristine` and `SGC 10 Pristine` (`Binder:368–377`). Total unserved: **93 of 118**. Also unrecorded: `bucketOf` (`Binder:415–423`) encodes a dormant answer — `n >= 10 → 'PSA 10'`, `< 8 → 'PSA 7'` — which is the multiplier-style approximation D-022 records the owner rejecting. |
