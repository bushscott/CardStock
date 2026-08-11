# Screen spec — Binder

**Source of truth:** `CardStock Mockup/Cardstock Binder.dc.html` (622 lines), read in full 2026-08-10.
All `L<n>` citations in this document are line numbers in that file unless another file is named.
Markdown docs are Tier 2/3 and were checked against it; every disagreement is recorded in §8, never averaged.

**Runtime note.** The prototype is a Design Composer component: `<x-dc>` template + a `DCLogic` subclass
in `<script type="text/x-dc">` (L330–L618). `sc-if` renders children only when `value` is truthy, else
returns `null` (`support.js:644–658`); `sc-for` iterates `list` binding each item to `as`
(`support.js:611–643`). **`hint-placeholder-count` / `hint-placeholder-val` are editor/streaming-time
placeholders only** and carry no runtime meaning (`support.js:614, 648`) — do not read them as defaults.
Seeded arrays (`HOLD`, `TX`, `BV`, `IX`, `CARDS`, `ACCENTS`, `PMONTHS`) are illustrative; the structure
and the state space are the specification.

---

## 1. Identity

| | |
|---|---|
| **Name** | Binder |
| **Screen label** | `data-screen-label="Binder"` (L37) |
| **Route** | `/binder` — asserted by `HANDOFF.md:74` (Tier 2). The HTML proves only that Binder is the active nav tab (L48). |
| **Deep link** | `#performance` opens the screen on the Performance tab (`componentDidMount`, L432–434). No other fragment is handled; the fragment is read once on mount and never written back on tab change. |
| **Nav position** | 4th of 5 primary tabs: Home · Screener · Charts · **Binder** · Browse (L45–49). Active styling = `--ink` text, 600 weight, 2px `--acc` bottom border. |
| **Purpose** | Private cost-basis truth and performance proof. The user's collection treated as a portfolio: what is owned, what was paid, what it is worth now, what has been realized, and whether the binder beat the market index. |
| **Privacy** | `PRIVATE` mono badge beside the H1 (L60), tooltip *"Binder data is strictly private — no social features, never shared"*. |
| **Component props** | Exactly one: `emptyState` (boolean, default `false`, editor section "Mode") — L330. Read as `this.props.emptyState ?? false` (L447). This is the only external input to the screen. |
| **Write surface** | The only substantial write surface in the product (transaction create + correct). |

---

## 2. Layout

Vertical stack inside `<main>`: `max-width 1480px`, centered, `padding 14px 20px 28px`, `gap 14px`,
`flex-direction: column` (L56). Page `font-size: 15px` base (L37).

```
nav (48px, sticky, z-index 20)                                   L39–54
main
├── header row                                                   L58–66
│   H1 "Binder" · PRIVATE badge · [spacer] · [Export CSV]* · [+ Add transaction]
├── control row                                                  L68–80
│   segmented tabs: holdings | transactions | performance
│   segmented view toggle: table | gallery          (holdings tab only)
├── ONE of the following panels (mutually exclusive):
│   ├── empty state card                            sc-if isEmpty     L82–88
│   ├── gallery grid                                sc-if isGallery   L90–106
│   ├── holdings table                              sc-if isHold      L108–135
│   ├── transactions ledger                         sc-if isTx        L137–163
│   └── performance stack                           sc-if isPerf      L165–229
│       ├── portfolio-vs-index chart section        L166–204
│       ├── 4-up stat tiles (grid, 4 equal cols)    L205–213
│       └── yearly summary table                    L214–228
├── IRON RULE strip (always rendered, every tab)                  L231–234
└── transaction modal overlay                       sc-if txOpen     L236–325
```

`*` Export CSV renders **only** on the transactions tab (`sc-if isTx`, L62–64).

**Panel gating** (L474–478) — all five panels are mutually exclusive and all four non-empty panels
additionally require `!empty`:

| Flag | Expression | Line |
|---|---|---|
| `isEmpty` | `props.emptyState ?? false` | L447, L474 |
| `isHold` | `!empty && tab==='holdings' && hView==='table'` | L475 |
| `isGallery` | `!empty && tab==='holdings' && hView==='gallery'` | L476 |
| `isHoldTab` | `!empty && tab==='holdings'` (gates the table/gallery toggle) | L477 |
| `isTx` | `!empty && tab==='transactions'` | L478 |
| `isPerf` | `!empty && tab==='performance'` | L478 |

**Consequence to implement deliberately:** the header row, the tab bar, the view toggle's container row,
and the IRON RULE strip are **not** gated on `!empty`. In the empty state the tabs still render and are
still clickable, and switching to Transactions or Performance renders *nothing* — the empty card is also
hidden only by `isEmpty`, which stays true. Empty state therefore shows: header + tabs + empty card +
iron-rule strip; clicking any tab leaves header + tabs + empty card + strip unchanged. Decide whether
production disables the tabs when the binder is empty (§7).

### Table mechanics shared with the rest of the app

- Header cell = centered clickable label + a `│` resize grip (`col-resize`, `onMouseDown`) — L112, L147.
- `startResize(key, bag)` (L435–443): captures `clientX` and the current width, attaches document-level
  `mousemove`/`mouseup`, sets `width = max(52, startW + dx)`. **Minimum column width 52px.**
- Two independent width bags in state: `hColW` (holdings) and `tColW` (transactions) — L411–412.
- Grid templates are recomputed from the bags every render (L492, L512).

---

## 3. Data contract

Every rendered field, by region. "Source" is the `renderVals` key or literal that supplies it.

### 3.1 Seed shapes (the entities the screen consumes)

| Entity | Fields | Line |
|---|---|---|
| **Holding** (`HOLD[]`) | `card` (string), `tier` (string), `qty` (int), `cost` (number, **per unit**), `cur` (number, **per unit current value**), `est` (bool — no recent sales in tier) | L342–351 |
| **Transaction** (`TX[]` + `added[]`) | `d` (ISO `YYYY-MM-DD`), `k` (`'BUY'｜'SELL'`), `card`, `tier`, `q` (int), `p` (number), `note` (string, may be `''`), `v` (bool — superseded/void), `id` (string) | L352–364, L367, L605, L610 |
| **Card corpus** (`CARDS[]`) | `name`, `set` — the BUY typeahead corpus | L378–396 |
| **Accent pair** (`ACCENTS{}`) | `card name → [hex, hex]` for gallery art gradients; fallback `['#D9DDE8','#B9C2D6']` | L397–406, L485 |
| **Index series** | `BV[]` (binder), `IX[]` (market index), `PMONTHS[]` (labels) — all three the same length `N` | L365–366, L407 |

Transaction ids: seeded rows get `'tx' + index` (L367); rows created in-session get `'a' + Date.now()`
(L610) — **client-generated, collision-prone within one millisecond.** Production must issue server ids.

### 3.2 Derived values

| Value | Formula | Line |
|---|---|---|
| `totV` — total current value | `Σ h.cur × h.qty` | L449 |
| `totC` — total cost basis | `Σ h.cost × h.qty` | L450 |
| per-row `value` | `h.cur × h.qty` | L452 |
| per-row `pl` (unrealized) | `(h.cur − h.cost) × h.qty` | L452 |
| per-row `pct` | `value / totV × 100`, 1 dp | L506 |
| `unreal` (portfolio unrealized) | `totV − totC` | L471 |
| `realized` | literal `612` — **assigned at L471 and never read**; the Realized tile uses the hardcoded string `'+$612'` (L547) | L471, L547 |
| `pcf(n, d)` | `(n≥0 ? '+' : '−') + |n/d×100|.toFixed(1) + '%'` — note **U+2212 MINUS**, not hyphen | L465 |
| `money(n)` | `'$' + Math.round(n).toLocaleString('en-US')` — **whole dollars, no cents, anywhere on this screen** | L413 |
| `tierRank(t)` | index into the canonical 19-value scale; **`-1` for anything not in that list** | L451 |
| `txAll` | `added.concat(TX).map(t => overrides[t.id] ? {...t, ...overrides[t.id]} : t)` | L472 |

### 3.3 Page header (L58–66)

| Field | Content | Source |
|---|---|---|
| Title | `Binder`, Inter Tight 700 / 26px | literal L59 |
| Privacy badge | `PRIVATE` + help tooltip | literal L60 |
| Export CSV label | `↓ Export CSV` ⇄ `✓ Exported` | `csvLabel` L535 |
| Export CSV tooltip | "Download every transaction as a CSV — date, card, grade, quantity, price, and note" | literal L63 |
| Add-transaction button | `+ Add transaction`, primary fill | L65 |
| Add-transaction tooltip | "Log a buy or a sell — updates your cost basis and P&L immediately" | literal L65 |

### 3.4 Tab bar (L68–80)

| Field | Content | Source |
|---|---|---|
| `tabs[]` | three items, labels rendered **lowercase verbatim**: `holdings`, `transactions`, `performance` | L491 |
| `tb.label` / `tb.bg` / `tb.fg` | active = `--acc` bg + `--card` fg; inactive = `--card` bg + `--mut` fg (`seg()`, L448) | L491 |
| `tb.tip` | holdings → "What you own now — quantity, cost basis, current value, and P&L per position"; transactions → "Every buy, sell, and correction, newest first"; performance → "Your binder against the market index since your first transaction" | L491 |
| `vtBg/vtFg`, `vgBg/vgFg` | table/gallery toggle colors, same active/inactive rule | L480–481 |

### 3.5 Holdings — table (L108–135)

Columns, in order (L493). `s` = sort key applied on click.

| # | Header | Key | Sort key `s` | Cell content | Line |
|---|---|---|---|---|---|
| 1 | `Card` | `card` | `card` | card name, links to Card page | L117, L499 |
| 2 | `Tier` | `tier` | `tier` (via `tierRank`) | tier label, mono 12.5px | L118, L499 |
| 3 | `Qty` | `qty` | `qty` | integer as string | L119, L499 |
| 4 | `Avg cost` | `cost` | `cost` | `money(h.cost)` + `' ea'` when `qty > 1` — **per-unit**; tooltip "What you paid never changes" | L120, L500 |
| 5 | `Current value` | `value` | `value` | `money(cur×qty)`, 700 weight, + optional `EST` badge | L121, L501 |
| 6 | `Unrealized ±` | `pl` | `pl` | `±$abs(pl) · ±pct%`, colored `pos`/`neg2` | L122, L504–505 |
| 7 | `% of binder` | `pct` | `value` | `pct.toFixed(1) + '%'`, muted — **no sort arrow ever** (`c.k !== 'pct'` guard) | L123, L495, L506 |

- **EST badge** (L121): rendered via `display: {{h.estShow}}` = `inline-block｜none` (L502) — always in the
  DOM, toggled by CSS. Amber (`--warnInk` on `rgba(176,127,26,0.12)`), 10px mono, `cursor: help`.
  Tooltip (`estTip`, L503): *"No recent sales in this tier — value estimated from index movement since
  the last observed sale."*
- **Sort arrow** (`hc.arrow`, L495): ` ▾` when `desc`, ` ▴` when `asc`, only on the active column.
- **Totals row** (L126–133): `totLabel` = `"{HOLD.length} positions"` (L508); two empty cells (tier, qty);
  `totCost` = `money(totC)`; `totValue` = `money(totV)`; `totPl` = `±$abs(unreal) · ±pct%` colored
  (L509–510); `% of binder` is the **hardcoded literal `100%`** (L132).
  ⚠️ The totals row places a **total** cost under a column headed **"Avg cost"** — L129 vs L493.

### 3.6 Holdings — gallery (L90–106)

`grid-template-columns: repeat(auto-fill, minmax(200px, 1fr))`, gap 14px. **Same sorted order as the
table** (both map over `hs`, L482 / L498). Per tile (`galleryCards`, L482–490):

| Field | Content | Line |
|---|---|---|
| `gc.slotId` | `'art-' + card.toLowerCase().replace(/[^a-z0-9]+/g,'-')` — `<image-slot>` id | L484 |
| `gc.thumbBg` | `linear-gradient(160deg, <accent1>, <accent2>)` behind the slot; art box is `aspect-ratio: 325/450` | L485, L94 |
| `gc.name` | card name, links to Card page | L483, L97 |
| `gc.sub` | `tier` + `' · ×N'` when `qty > 1` | L486 |
| `gc.value` | `money(cur×qty)`, mono 700 | L487 |
| `gc.pl` | **percentage only** (`±X.X%`) — no dollar figure, unlike the table | L488 |
| `gc.plFg` | `pos` / `neg2` | L489 |

No EST badge and no quantity column in gallery — quantity is folded into `sub`, estimate provenance is
**not surfaced at all** in this view (§7).

### 3.7 Transactions (L137–163)

Section header: `Transactions` (h2), `AUDIT LOG` badge, right-aligned count.

| Field | Content | Line |
|---|---|---|
| `AUDIT LOG` badge tooltip | *"Every edit is stored as a correction under the hood — the table shows the current truth, the audit trail is kept"* | L141 |
| `txCount` | `"{txAll.length} transactions"` — counts **every** row including any `v: true` | L511 |

Columns (`tCols`, L513): `Date` · `Type` · `Card` · `Tier` · `Qty` · `Price` · `Note` · *(blank action
column)*. Header cells here are **not sortable** — label + resize grip only (L147); the ledger has no
sort control at all. Order is `added` (newest first, prepended) then `TX` in the order seeded.

⚠️ Two resize bugs to fix on port, both at L513: the `Note` column and the blank action column both pass
resize key `'price'`, so dragging either grip resizes **Price**. And `tGridCols` (L512) builds the
template by `String.replace` of `card`'s pixel value — if any other column is resized to exactly the card
column's width, the `minmax()` lands on the wrong column.

Row cells (`txRows`, L514–533):

| Field | Content | Line |
|---|---|---|
| `t.date` | raw `d` string (ISO, unformatted) | L515 |
| `t.kind` | `'VOID'` when `t.v`, else `t.k` (`BUY`/`SELL`) | L515 |
| `t.kBg` / `t.kFg` / `t.kBd` | chip palette: VOID → amber `rgba(176,127,26,·)`; BUY → `posBg(0.10)` / `pos` / `posBg(0.3)`; SELL → `rgba(74,99,208,·)` / `acc` | L516–518 |
| `t.card`, `t.tier`, `t.qty` | verbatim; `qty` stringified | L515 |
| `t.price` | `money(t.p)` — **rounded to whole dollars** | L515 |
| `t.note` | note or `'—'` fallback | L515 |
| `t.strike` / `t.op` | `line-through` + opacity `0.62` when `t.v`, else `none` / `1` | L519 |
| `t.voidTip` | row `title`. Void: *"Superseded by a correction — kept in the audit log, excluded from your totals."* Live: *"Hover the row and use ✎ to correct it — the original is kept in the audit log."* | L520 |
| `t.voidShow` | hardcoded `'inline-block'` — the edit control is **always visible**, not hover-revealed, despite the tooltip's wording | L521 |
| edit button | label `edit ✎`, tooltip "Edit this transaction"; handler `t.voidIt` (legacy name) | L159, L522–532 |

The strike-through/opacity/VOID-chip path is fully implemented but **unreachable in the prototype**: no
seed row has `v: true` (L353–363 all `v: false`) and no handler ever sets it — corrections are in-place
overrides (§5). Treat the void rendering as a real, specified state that the current interaction model
never produces (§8, row 3).

### 3.8 Performance (L165–229)

**Chart section** (L166–204):

| Field | Content | Line |
|---|---|---|
| Title | `Portfolio vs market index` | L168 |
| Legend | `Binder` swatch in `--btn`; `Market index` swatch in `--mut2` | L169–170 |
| Baseline caption | **static copy** "both indexed to 100 at first transaction (Jan '25)" | L171 |
| `perfEdge` | `'+' + (BV[N-1] − IX[N-1]) + 'pp vs index'`, always `--pos` colored | L173, L536 |
| `pfYMax` / `pfYMin` | `max` / `min` over `BV ∪ IX` — a single shared scale for both series | L467, L537 |
| SVG | `800×230` viewBox, `preserveAspectRatio="none"`; horizontal rule at `y=115` | L181–182 |
| `pfIxSolid` / `pfIxDash` | index polyline, `slice(0, N-1)` and `slice(N-2, N)`; `--mut2`, 1.5px, dash `4 4` | L538–539, L183–184 |
| `pfBvSolid` / `pfBvDash` | binder polyline, same split; `--acc`, 2px, dash `4 4` | L538, L185–186 |
| Point mapping | `x = (i/(N−1))×800`, `y = 222 − (v−min)/(max−min)×212` | L469 |
| `pfHollowTop` | vertical % position of the hollow month-to-date dot, pinned at `left: 100%` | L188, L540 |
| Hollow-dot tooltip | *"Aug is month-to-date — the point firms up as the month's sales land, and finalizes when the month closes"* — **month name hardcoded** | L188 |
| `pfHovShow` / `pfHovLeft` | crosshair visibility and % offset | L543, L190 |
| `pfHovMonth` / `pfHovBv` / `pfHovIx` | tooltip rows: month label, `Binder {v}` (accent, 700), `Index {v}` (muted) | L544–545, L192–194 |
| X-axis labels | **static copy**: `Jan '25` · `Oct '25` · `Aug '26`, `margin-left: 52px` to clear the y-axis gutter | L199–203 |

The final segment of **both** series is dashed and the binder line ends in a hollow dot — the app-wide
month-to-date convention (`DESIGN_NOTES.md:49`: aggregated on partial data, never projected).

**Stat tiles** (`perfStats`, L546–551) — fixed `repeat(4, 1fr)` grid, each tile `cursor: help` with a
tooltip:

| # | `k` | `v` | `sub` | `fg` | Computed? |
|---|---|---|---|---|---|
| 1 | `Realized P&L` | `+$612` | `3 closed sales` | `pos` | **hardcoded** |
| 2 | `Unrealized ±` | `±$abs(totV − totC)` | `on {HOLD.length} open positions` | `pos`/`neg2` | computed |
| 3 | `Win rate` | `67%` | `2 of 3 sales above cost` | `ink` | **hardcoded** |
| 4 | `Avg hold` | `7.2 mo` | `across closed sales` | `ink` | **hardcoded** |

Tooltips (L547–550), verbatim and load-bearing for the honesty stance:
1. "Profit locked in on sold positions — sale proceeds minus what you paid. Never re-estimated."
2. "Current estimated value of holdings minus cost basis. Moves with the market; EST-badged positions are index-estimated."
3. "Share of closed sales that sold for more than you paid."
4. "Average time between buy and sell on closed positions."

**Yearly summary** (L214–228, `yearRows` L552–555) — grid `90px 1fr 1fr 1fr 1fr`:

| Column | Field | Notes |
|---|---|---|
| `Year` | `y.yr` | e.g. `2025`, `2026 YTD` — the YTD suffix is part of the label string |
| `Invested` | `y.inv` | `money()` |
| `Proceeds` | `y.pro` | `money()` |
| `Realized ±` | `y.rl` + `y.rlFg` | signed string, `pos`/`neg2` |
| `Year-end value` | `y.end` | `money()`; **only the current year's cell is computed** (`money(totV)`, L554) |

Both rows are otherwise hardcoded. Their realized figures sum to the Realized P&L tile
(219 + 393 = 612), and the win-rate denominator (3) matches the seeded SELL count — the seed is
internally consistent, which is a modelling hint, not a rule.

### 3.9 IRON RULE strip (L231–234)

Always rendered, on every tab **and in the empty state**. Mono `IRON RULE` label + copy:
*"Your entered prices never change. Current values are estimates that move with the market — badged
`EST` when no recent sales support them."* The inline `EST` chip uses the same amber styling as the
table badge.

### 3.10 Transaction modal (L236–325)

Backdrop `rgba(20,19,26,0.45)`, `position: fixed; inset: 0; z-index: 100`, centered; panel 400px wide.

| Field | Content / binding | Line |
|---|---|---|
| `modalTitle` | `'Edit transaction'` when `txEdit` set, else `'Add transaction'` | L559, L240 |
| Close `✕` | `closeTx`, aria-label "Close", tooltip "Close without saving this transaction" | L242 |
| BUY/SELL segment | `buyBg/buyFg` (active = `pos2`), `sellBg/sellFg` (active = `acc`) | L245–246, L574–575 |
| BUY tooltip | "Log a purchase — adds a lot with its own cost basis" | L245 |
| SELL tooltip | "Log a sale — limited to what you currently hold; realizes P&L against your cost basis" | L246 |
| **Card (BUY)** | text input `#tx-card-input`, placeholder "Start typing a card name…", `autocomplete=off`; border `cardBd` (green-tinted `posBg(0.45)` when linked) | L251, L585 |
| `cardHint` / `cardHintFg` | `'✓ linked'` in `pos` when `txCardOk`, else `'(must link to a tracked card)'` in `mut2` | L586–587 |
| `cardMenuOpen` | `txKind==='BUY' && !txCardOk && txCard.trim().length >= 2` | L579 |
| `cardMatches[]` | case-insensitive **substring** match over `CARDS`, `.slice(0, 6)` → **max 6 rows**; each row = `name` + muted `set`, tooltip "Log this transaction against {name} ({set})" | L580–583, L255 |
| `cardNoMatch` | ≥2 chars and zero matches → *"No matching card — binder entries must link to a card we track."* | L584, L257–259 |
| **Card (SELL-new)** | `<select>`; first option `Select a holding…` (value `''`), then `sellOpts` labelled `"{card} · {tier} · {qty} held"`, value = holding index | L266–271, L567 |
| SELL-new label hint | "(from your holdings — you can only sell what the binder holds)" | L265 |
| **Card (SELL-edit)** | read-only span showing `editCardName` (`txCard` or `'—'`) on `--bg` fill | L276, L565 |
| SELL-edit label hint | "(from the original sale — correct price, qty, date, or note)" | L275 |
| **Grader (BUY)** | `<select>` over `graderOpts` = `Object.keys(GRADERS)` → `Raw, PSA, CGC, BGS, TAG, ACE, SGC` (7) | L283–287, L589 |
| **Grade (BUY)** | `<select>` over `gradeOpts` = `GRADERS[txGrader]`; `disabled` when grader is `Raw` (`gradeOff`), fill `gradeBg`, label color `gradeFg` (muted when disabled) | L290–294, L590–593 |
| **Grade tier (SELL)** | read-only span; `sellTier` = `txTier` when editing, else the selected holding's tier, else `'—'`; `sellTierFg` muted (`mut3`) until resolved | L299–301, L569–570 |
| `sellTierNote` | `'(from the original sale)'` when editing, else `'(from holding)'` | L566 |
| **Qty** | `type=number`, `min=1`, `max={{qtyMax}}`; border `qtyBd` turns `neg2` red past max | L303–305, L571–573 |
| `qtyMax` / `qtyMaxNote` | holding qty + `'(max N)'` note **only for SELL-new with a holding selected**; otherwise `'99'` and no note | L571–572 |
| **Price** | `type=number`, placeholder `0.00`; label `priceLabel` = `'Price paid ($)'` for BUY, `'Price received ($)'` for SELL | L308–309, L576 |
| **Date** | `type=date`; no min/max, no validation | L311–312 |
| **Note** | `type=text`, label suffix `(optional)`, placeholder "e.g. auction win" | L315–316 |
| Correction hint | *"Mistakes are fixable — hit `edit ✎` on any row. Corrections are kept in the audit log under the hood."* — shown in **every** modal mode, including a fresh add | L318 |
| Cancel | `closeTx` | L320 |
| Save | `disabled={{saveOff}}`, bg `saveBg` (`acc` enabled / `accMut` disabled), cursor `saveCur` (`pointer`/`not-allowed`); tooltip "Save this transaction — it appears in the ledger and updates your totals immediately" | L321, L600–602 |

**Grade tier vocabulary offered by the BUY picker** (`GRADERS`, L368–377). `halves` = `10, 9.5, 9, 8.5,
… 1` (19 values, L368):

| Grader | Grades offered | Count |
|---|---|---|
| `Raw` | *(none — Grade select disabled)* | 0 |
| `PSA` | `halves` minus `9.5` | 18 |
| `CGC` | `10 Pristine` + `halves` | 20 |
| `BGS` | `10 Black Label` + `halves` | 20 |
| `TAG` | `10 Pristine` + `halves` | 20 |
| `ACE` | `halves` | 19 |
| `SGC` | `10 Pristine` + `halves` | 20 |

The saved tier string is `grader + ' ' + grade` (`setTxGrader` L594 / `setTxGrade` L595), or the literal
`'Raw'`. **That is 118 distinct producible tier labels** — see §6 and §8 for why this matters to D-012.

---

## 4. States

### 4.1 Screen-level (mutually exclusive panels)

| State | Trigger | Renders |
|---|---|---|
| **Empty** | `props.emptyState === true` (L447) | Header + tabs + empty card ("Log your first purchase" / "30 seconds, and your P&L starts here." / `+ Add transaction`, L84–86) + iron-rule strip. **All four data panels suppressed regardless of `tab`.** |
| **Holdings · table** | `tab='holdings'`, `hView='table'` (defaults, L409) | Sortable, resizable positions table + totals row |
| **Holdings · gallery** | `tab='holdings'`, `hView='gallery'` | Art-tile grid, same sort order |
| **Transactions** | `tab='transactions'` (also entered automatically after saving a *new* transaction, L613) | Ledger + AUDIT LOG badge + count; Export CSV appears in the header |
| **Performance** | `tab='performance'`, or `#performance` on mount (L433) | Chart + 4 stat tiles + yearly summary |

### 4.2 Component-level

| State | Values | Trigger | Line |
|---|---|---|---|
| Sort | `sortKey ∈ {card, tier, qty, cost, value, pl}` × `sortDir ∈ {asc, desc}`; default `value`/`desc` | header label click; same key → toggle direction, new key → `desc` | L409, L496 |
| Sort arrow | ` ▾` / ` ▴` / none | active column only, never on `% of binder` | L495 |
| Column widths | `hColW{card,tier,qty,cost,value,pl,pct}`, `tColW{date,kind,card,tier,qty,price}`; min 52px | grip drag | L411–412, L439 |
| EST badge | shown / hidden | `h.est` | L502 |
| CSV button | `↓ Export CSV` → `✓ Exported` → back | click; **1800 ms `setTimeout`** | L534–535 |
| Chart hover | `pfHov ∈ {null} ∪ [0, N−1]` | `mousemove` over the plot → nearest index; `mouseleave` → `null` | L541–542 |
| Ledger row | live / **void** (struck, 0.62 opacity, amber `VOID` chip) | `t.v === true` — **no interaction sets this**; unreachable in the prototype | L519 |
| Modal | closed / open | `openTx` / `closeTx` / save | L556–558 |

### 4.3 Modal modes — the four combinations of (`txKind`, `txEdit`)

| Mode | Predicate | Card field | Tier field | Qty cap | Save requires |
|---|---|---|---|---|---|
| **BUY-new** | `isBuy && !txEdit` (L562) | typeahead, must link | Grader + Grade selects | `max=99` attr only | `price > 0 && txCardOk` |
| **BUY-edit** | `isBuy && txEdit` | typeahead, **pre-filled and pre-linked** (`txCardOk: true`, L527) | Grader + Grade, pre-mapped from the row's tier | `max=99` attr only | `price > 0 && txCardOk` |
| **SELL-new** | `isSellNew` (L563) | holdings `<select>` | read-only, from holding | `max = holding qty`, red border past it | `price > 0 && sellIdx !== '' && 1 ≤ qty ≤ holding.qty` |
| **SELL-edit** | `isSellEdit` (L564) | read-only span | read-only, `txTier` | **`max=99`, no red border** | `price > 0 && qty ≥ 1` |

Note the BUY branch in `canSave` is tested **before** the `txEdit` branch (L426 before L427), so BUY-edit
is gated on `txCardOk`, not on the edit rule.

### 4.4 Card-typeahead sub-states (BUY only)

| Sub-state | Condition | Line |
|---|---|---|
| Closed | `<2` chars, or already linked (`txCardOk`) | L579 |
| Open with matches | ≥2 chars, not linked, ≥1 substring match (max 6 shown) | L579–580 |
| Open, no match | ≥2 chars, zero matches → refusal copy | L584 |
| Linked | a suggestion was clicked → `txCardOk: true`, green border, `✓ linked` | L582, L585–587 |
| Re-typing after link | any keystroke sets `txCardOk: false` (L578) → menu reopens, **Save re-blocks** | L578 |

### 4.5 States the prototype does not implement

No loading state, no skeletons, no error state, no save-failure path, no optimistic/pending row, no
network affordance of any kind. `saveTx` (L603) always succeeds synchronously. `CARDSTOCK_UI_SPEC_v1.md:194`
(Tier 3) specifies per-tab skeletons and a save-failure state that keeps the modal open with values
intact; nothing in the HTML corresponds. See §7.

---

## 5. Interactions

| Control | Line | Consequence |
|---|---|---|
| Nav links / brand | L41–49 | Navigate. Binder tab is the active one. |
| `<cardstock-search>` | L52 | Shared web component (`cardstock-search.js`); `/` focuses, Esc clears+blurs. Not implemented in this file. |
| Avatar `O` | L53 | → Profile. |
| **Export CSV** | L63, L534 | `csvDone = true`, label → `✓ Exported`, auto-reverts after 1800 ms. **No file is generated** — the affordance is a confirmation-flash stub. Only visible on the transactions tab. |
| **+ Add transaction** (header) | L65, L557 | `txOpen = true`. ⚠️ `openTx` sets **only** `txOpen` — it does not reset `txKind`, `txQty`, `txDate`, `txGrader`, `txGrade`, or `txTier`, so those leak in from the previous modal session (§6). |
| **+ Add transaction** (empty card) | L86 | Same handler. |
| Tab buttons | L71, L491 | `tab = <label>`. No URL change, no history entry (the `#performance` fragment is read on mount only). |
| table / gallery | L76–77, L479 | `hView = 'table'｜'gallery'`. Sort state is shared — switching views preserves order. |
| Holdings header label | L112, L496 | Sort by that column's `s` key; same key toggles asc/desc, a new key starts at `desc`. `% of binder` sorts by `value`. |
| Resize grip (either table) | L112/L147, L435 | Document-level drag; width clamps at 52px min, no max. |
| Card name links | L97, L117 | → Card page. |
| Chart plot area | L180, L541–542 | `mousemove` → nearest month index → crosshair + readout; `mouseleave` clears. ⚠️ The readout box is pinned at `top:8px; left:8px` (L191) — it does **not** follow the cursor. |
| Stat tiles / badges | L207, L60, L121, L141 | Hover-only, `cursor: help`. No click behaviour. |
| **`edit ✎`** on a ledger row | L159, L522–532 | Opens the modal **pre-filled** from that row: `txEdit = t.id`, `txCardOk = true`, `txKind = t.k`, card, tier, qty, price, date, note; `sellIdx` cleared. The tier is reverse-mapped into grader + grade (L523–525). **No void row is created; nothing is struck through.** |
| Modal backdrop | L237 | `closeTx`. Inner panel calls `stopClick` (`e.stopPropagation()`, L560) so clicks inside do not dismiss. |
| `✕` / `Cancel` | L242, L320, L558 | `closeTx`: clears `txOpen, txEdit, txCard, txPrice, txNote, sellIdx, txCardOk`. **Does not clear** `txKind`, `txQty`, `txDate`, `txGrader`, `txGrade`, `txTier`. |
| **Esc** | — | **Not handled.** The only `addEventListener` calls in the file are the resize drag's `mousemove`/`mouseup` (L441–442). Contradicts the app-wide "Esc closes overlay" keyboard map (`CARDSTOCK_UI_SPEC_v1.md:129`). |
| BUY / SELL segment | L245–246, L561 | `pickBuy` sets `txKind='BUY'` **and nothing else**; `pickSell` sets `txKind='SELL'`, `sellIdx=''`, `txQty='1'`. ⚠️ The segment stays live **while editing**, so a correction can change a transaction's kind (BUY↔SELL) — undocumented anywhere. |
| Card typeahead input | L251, L578 | Every keystroke sets `txCard` and **clears** `txCardOk`. |
| Typeahead suggestion | L255, L582 | Sets `txCard` to the canonical name and `txCardOk = true`. |
| Holdings `<select>` (SELL-new) | L266, L568 | Sets `sellIdx` and resets `txQty = '1'`. |
| Grader `<select>` | L283, L594 | Sets `txGrader`, sets `txGrade` to that grader's **first** option, and recomputes `txTier` (`'Raw'` or `grader + ' ' + first`). |
| Grade `<select>` | L290, L595 | Sets `txGrade` and recomputes `txTier = txGrader + ' ' + grade`. Disabled when grader is `Raw`. |
| Qty / Price / Date / Note | L304/309/312/316 | Plain state writes, **no coercion or validation on input** (L596–599). |
| **Save** | L321, L603–615 | Builds `row = {d, k, card, tier, q: parseInt(qty)｜｜1, p: parseFloat(price), note: note.trim(), v: false}`. For SELL-new, `card` and `tier` come from the **selected holding**, overriding the form. Then: <br>• **Edit** → `txOverrides[txEdit] = row`; modal closes; tab unchanged. <br>• **New** → `row.id = 'a'+Date.now()`; prepended to `added`; modal closes **and `tab` switches to `'transactions'`**. |

**What Save does not do:** it never touches `HOLD`. Holdings rows, totals, `% of binder`, the
unrealized tile, the chart, and the yearly summary are all unaffected by adding or correcting a
transaction — despite two tooltips promising otherwise (L65 "updates your cost basis and P&L
immediately", L321 "updates your totals immediately"). See §8 row 12.

---

## 6. Rules and invariants

1. **Iron rule.** Entered prices never drift. Current values are estimates and must be badged `EST`
   when no recent sales support them (L231–234, L120 tooltip "What you paid never changes").
2. **`money()` renders whole dollars only** (L413) — every currency figure on this screen, including
   ledger prices the user typed with cents. An entered `228.50` renders as `$229`.
3. **Signed numbers use U+2212 MINUS (−), not hyphen**, and always carry an explicit `+` when
   non-negative (L465, L488, L504, L509, L548).
4. **Positive/negative colouring** is `PAL.pos` / `PAL.neg2`, resolved at construction from the
   theme × colour-blind matrix in `localStorage` (`cardstock-theme`, `cardstock-cvd`) — L332–339.
   Logic-computed colours must never use `var()` (illegal in SVG presentation attributes); the
   palette object is the mechanism.
5. **You cannot sell what you do not hold.** SELL-new sources card and tier from the holdings list and
   caps quantity at the held quantity, red-bordering and blocking Save past it (L266, L430, L571–573).
6. **A BUY must link to a tracked card.** Free text alone never satisfies Save; only clicking a
   suggestion sets `txCardOk` (L426, L582). Refusal copy: "binder entries must link to a card we track."
7. **Save-blocked conditions, exactly** (`canSave`, L424–431), in evaluation order:
   - `parseFloat(txPrice) > 0` — else blocked (covers empty, `0`, negative, `NaN`);
   - `txKind === 'BUY'` → `txCardOk` (applies to BUY-new **and** BUY-edit);
   - else `txEdit` → `parseInt(txQty) >= 1`;
   - else `sellIdx === ''` → blocked;
   - else `1 <= parseInt(txQty) <= HOLD[sellIdx].qty`.
   **No date validation, no note validation, and no quantity check on any BUY path.**
8. **Corrections are in-place overrides, not void-and-re-enter.** `txOverrides[id]` replaces the row's
   displayed values; the row count does not change and nothing renders struck through (L472, L606–609).
   The `AUDIT LOG` badge (L141) asserts that the append/void trail exists **under the hood**; the UI's
   contract is "the table shows the current truth."
9. **Transactions are never deleted.** No delete control exists anywhere on the screen.
10. **Tier sort rank** uses the canonical 19-value scale (L451) and returns `-1` for anything outside it.
    Combined with rule 11, most tier labels the BUY picker can produce sort to `-1`.
11. **The tier label saved on a transaction is a slab label, not a valuation bucket.** L414 states the
    intent explicitly: *"Raw slab label → internal grade bucket (valuation tier); the slab label is kept
    on the transaction."* The mapping function `bucketOf` (L415–423) exists — `Raw`→`Raw`, `≥10`→`PSA 10`,
    `≥9.5`→`Grade 9.5`, `≥9`→`PSA 9`, `≥8`→`PSA 8`, else `PSA 7` — and is **never called anywhere in the
    file** (verified by grep: one hit, the definition). The prototype therefore *declares* a
    slab-label/valuation-tier split and *implements* neither half of the valuation side.
12. **Holdings valuation is seeded, not derived.** `HOLD` carries `cur` per unit as a given (L342–351);
    no code derives it from a price series, and no code derives holdings from transactions.
13. **Both performance series share one scale** anchored to `min`/`max` over `BV ∪ IX` (L467), and both
    are indexed to 100 at the first transaction (static caption, L171).
14. **The final chart segment is dashed for both series and the binder line ends in a hollow dot** —
    month-to-date, aggregated on partial data, never projected (L184, L186, L188).
15. **Minimum column width is 52px**, enforced in the drag handler (L439).
16. **Every panel is exclusive**; the empty-state prop overrides tab selection entirely (§2).
17. **State leaks between modal sessions.** Neither `openTx` nor `closeTx` resets `txKind`, `txQty`,
    `txDate`, `txGrader`, `txGrade`, or `txTier` (L557–558). Concretely: edit a SELL row, cancel, then
    click `+ Add transaction` → the modal opens in **SELL-new** mode carrying the edited quantity and
    date. Production must decide the reset policy (§7).
18. **`txDate` defaults to a hardcoded `'2026-08-04'`** (L410), not to today.

### Grade tier selection when logging a holding (D-012 relevance)

The Grade-tier control appears **only in BUY mode** (`sc-if isBuy`, L280–297). SELL inherits its tier
read-only from the holding or the original sale (L299–301). So the entire tier vocabulary question is
decided at the BUY step, and the UI offers, precisely:

- **7 graders** — `Raw`, `PSA`, `CGC`, `BGS`, `TAG`, `ACE`, `SGC` (L369–377, L589).
- **118 producible tier labels** — `Raw` (1) + PSA 18 + CGC 20 + BGS 20 + TAG 20 + ACE 19 + SGC 20.
- Every non-PSA grade-10 variant: `CGC 10`, `CGC 10 Pristine`, `BGS 10`, `BGS 10 Black Label`,
  `SGC 10`, `SGC 10 Pristine`, `TAG 10`, `TAG 10 Pristine`, `ACE 10`.
- Every low grade for every grader: `PSA 1` … `ACE 6.5` … `SGC 3.5`, in 0.5 steps.

`DECISIONS.md:399` (D-012) identifies exactly two families with **no price series**: grades 1–6, and
every grade-10 that is not `Psa10`. **The BUY picker offers every member of both families.** The user can
log a `BGS 10 Black Label` or a `PSA 3` in three clicks, and the prototype supplies no valuation for
either — `bucketOf`, which would have collapsed them (any `≥10` → `PSA 10`, anything below 8 → `PSA 7`),
is dead code. That collapse is precisely the move D-022 rejected as statistically dishonest when it took
the form of a multiplier; here it would take the form of straight equality.

**The HTML does not settle D-012. It proves the UI is designed to let the user create the case, at
118-label granularity, and that the one written-down valuation mapping was never wired up.**

---

## 7. Open questions

1. **D-012 — valuation for tiers with no price series.** Unresolved and unavoidable: the BUY picker
   offers every affected tier (§6). Options on the table remain: value at `Psa10`, value at `Psa10`
   minus a haircut, or leave unvalued and exclude from totals. If "exclude", the holdings table, the
   `% of binder` column, the totals row, the unrealized tile, and the vs-index series all need a defined
   behaviour for excluded positions — the prototype has none.
2. **Is the 19-value canonical scale or the 118-label grader×grade picker the intended vocabulary?**
   The HTML implements the latter for input and the former for sorting (§8 rows 1–2). One must give.
3. **Slab label vs valuation bucket.** L414's comment declares the split and `bucketOf` sketches it, but
   nothing calls it. Does a holding key on `(card, slab_label)` or `(card, valuation_tier)`? The seeded
   `HOLD` uses grader-agnostic labels the picker cannot produce, implying bucketing happens somewhere.
4. **Does correcting a transaction re-derive holdings and P&L?** The tooltips promise it (L65, L321);
   the prototype does none of it. Confirm the recompute scope and whether it is synchronous.
5. **Can a correction change a transaction's kind (BUY↔SELL)?** The HTML permits it (§5). Probably
   unintended; needs a ruling.
6. **Reset policy for the modal.** Which fields persist between openings (§6 rule 17)? Should
   `+ Add transaction` always open in BUY mode with today's date?
7. **Loading and error states.** Absent entirely. `CARDSTOCK_UI_SPEC_v1.md:194` specifies per-tab
   skeletons and "save failures keep the modal open with values intact"; both need designing.
8. **CSV export.** File generation, filename, whether the export honours corrections (current truth) or
   emits the full audit trail, and whether it covers voided rows. The tooltip says "grade" where the
   column says "Tier" (L63 vs L513).
9. **Void rows.** The render path is complete but unreachable (§3.7). Does production ever surface a
   struck-through superseded row — e.g. an "show audit trail" affordance — or is the path dead?
10. **Currency precision.** `money()` rounds to whole dollars everywhere while the price inputs accept
    decimals. Confirm whether the ledger should show cents (the marketing copy says "to the cent",
    `CARDSTOCK_UI_SPEC_v1.md:421`).
11. **Empty state and the tab bar.** Should tabs be disabled/hidden when the binder is empty, given all
    four panels are suppressed (§2)?
12. **Esc to dismiss the modal**, and focus management/focus trap — neither is implemented.
13. **Chart hover readout position** is pinned rather than cursor-following (L191). Intentional?
14. **`perfEdge` sign handling**: hardcoded `'+'` prefix (L536) renders `+-5pp vs index` when the binder
    trails, and is always coloured `--pos`. Needs a signed formatter.
15. **Are `Realized P&L`, `Win rate`, `Avg hold` and the yearly summary computed from the ledger?**
    All are hardcoded here (L546–555); only the definitions in their tooltips are authoritative.
16. **Transactions ledger has no sort and no pagination** — deliberate for a small ledger, but the
    behaviour at hundreds of rows is undefined.
17. **Deep link `#performance` is read once on mount** and never updated as tabs change; should tab
    state be URL-addressable?
18. **`% of binder` denominator** when a holding is excluded or a value is unknown (follows from Q1).

---

## 8. Contradictions found

Tier 1 (the HTML) wins in every row. "Source" cites the derived document making the claim.

| # | Claim | Source `doc:line` | What the HTML actually does |
|---|---|---|---|
| 1 | Binder tier picker uses the canonical **19-value** scale (`Raw`, `Grade 1–9`, `Grade 9.5`, `PSA 10`, `CGC 10`, `CGC 10 Prist.`, `TAG 10`, `ACE 10`, `SGC 10`, `BGS 10`, `BGS 10 Black`) | `DESIGN_NOTES.md:77`; `DISPLAY_VOCABULARY.md:65`, `:182` | Two dependent selects producing a **grader × grade cross-product of 118 labels** (`GRADERS`, L368–377; `txTier = grader + ' ' + grade`, L594–595). Label text differs too: `CGC 10 Pristine` not `CGC 10 Prist.`, `BGS 10 Black Label` not `BGS 10 Black`, and `SGC 10 Pristine` / `TAG 10 Pristine` exist with no canonical counterpart. |
| 2 | "Below 10, buckets are **grader-agnostic**" | `DISPLAY_VOCABULARY.md:65`; `DESIGN_NOTES.md:77` | Below 10 the picker emits grader-prefixed labels (`PSA 8`, `CGC 8`, `TAG 3.5` are distinct strings, L594–595). The picker **cannot produce** the grader-agnostic `Grade 7/8/9` labels the seeded holdings actually use (L345, L347–350). The only collapsing code, `bucketOf` (L415–423), is never called (grep: definition only) and collapses to `PSA 9/8/7` — names absent from the canonical 19 and from `tierRank` (L451), so they'd sort to rank `-1`. |
| 3 | "**No VOID rows in UI**" | `DESIGN_NOTES.md:63` | A complete VOID render path is present: kind label `'VOID'` (L515), amber chip palette (L516–518), `line-through` + `opacity 0.62` (L519), a dedicated void tooltip (L520). It is unreachable — every seed row is `v:false` (L353–363) and no handler sets `v:true` — but it is in the code, and the handler/flag names are still `voidIt`/`voidShow`/`voidTip` (L521–522). |
| 4 | Superseded rows "are **excluded from your totals**" | `DISPLAY_VOCABULARY.md:189`; row tooltip L520 | Nothing is ever excluded. `txAll` does not filter on `v` (L472) and `txCount` counts every row (L511). Holdings, totals and performance are computed from the static `HOLD` array (L449–450), never from transactions at all — so no transaction, void or live, affects any total. |
| 5 | "Corrections = **void + re-enter** (both rows visible, void struck through)"; "Transactions are **immutable once saved** (delete-and-re-enter to correct)" | `CARDSTOCK_UI_SPEC_v1.md:191`, `:96` (Tier 3) | In-place override: the pencil opens a pre-filled modal (L522–532) and Save writes `txOverrides[id]` (L606–609). One row, updated in place; no second row, no strike-through. Confirms the reversal recorded at `DESIGN_NOTES.md:63` and `HANDOFF.md:105`. |
| 6 | Badge reads `IMMUTABLE` | `CARDSTOCK_UI_SPEC_v1.md:191` (Tier 3) | Badge reads **`AUDIT LOG`** (L141), tooltip "Every edit is stored as a correction under the hood — the table shows the current truth, the audit trail is kept". Docs at `DESIGN_NOTES.md:63` / `HANDOFF.md:105` already record this; confirmed against the HTML. |
| 7 | Quantity, BUY: "**1–99**" | `DISPLAY_VOCABULARY.md:183` | `min="1" max="99"` are HTML attributes only (L304, L571). `canSave` applies **no quantity check on any BUY path** (L426), and `saveTx` coerces with `parseInt(qty) \|\| 1` (L605). An empty, zero or negative quantity does not block Save and silently becomes 1. |
| 8 | SELL: "over-max turns the field's border red and blocks save" | `DISPLAY_VOCABULARY.md:183` | True for **SELL-new only**. `qtyMax`, `qtyMaxNote` and the red border all require `!txEdit` (L571–573), and `canSave` short-circuits SELL-edit to `qty >= 1` with no cap (L427). SELL-edit gets `max=99`, no note, no red border. (`DISPLAY_VOCABULARY.md:188` states this correctly; `:183` does not.) |
| 9 | "Save is blocked until price > 0 **and** (BUY: card resolved · SELL-new: a holding selected · SELL-edit: qty ≥ 1)" | `DISPLAY_VOCABULARY.md:188` | **Verified accurate**, with one omission: SELL-new additionally requires `1 ≤ qty ≤ HOLD[sellIdx].qty` (L430), not merely that a holding is selected. Also unstated: the BUY branch is tested before the edit branch (L426 before L427), so **BUY-edit** is gated on `txCardOk`, not on the SELL-edit rule. |
| 10 | "Export CSV (transactions tab) **emits** date, card, grade, quantity, price, note for every row" | `DISPLAY_VOCABULARY.md:191` | No file is produced. `exportCsv` sets `csvDone = true` and reverts after 1800 ms; the label swaps `↓ Export CSV` → `✓ Exported` (L534–535). The field list exists only in the button's `title` (L63), and it names "grade" where the ledger column is "Tier" (L513). Placement claim is correct — the button is inside `sc-if isTx` (L62–64). |
| 11 | "Holdings view **derives positions from transactions**"; "cost basis, current value … unrealized P&L" derived | `CARDSTOCK_UI_SPEC_v1.md:96` (Tier 3) | `HOLD` is an independent seeded array (L342–351) carrying `cur` per unit as a given. No derivation exists in either direction. *(Prototype-fidelity limit, not a design reversal — the requirement stands, but the HTML supplies no evidence of the derivation rules.)* |
| 12 | Saving "updates your cost basis and P&L immediately" / "updates your totals immediately" | in-HTML tooltips, L65 and L321 | The HTML contradicts **itself**: `saveTx` (L603–615) writes only to `added` / `txOverrides` and never touches `HOLD`, so no total, tile, chart point or percentage moves after a save. Recorded here because both strings are Tier-1 copy that the implementation must either honour or reword. |
| 13 | States: "*Loading:* per-tab skeleton. *Error:* transaction save failures keep the modal open with values intact." | `CARDSTOCK_UI_SPEC_v1.md:194` (Tier 3) | Neither exists. There is no loading, pending, or error state anywhere in the file; `saveTx` always succeeds and closes the modal. |
| 14 | Empty state: "'Log your first purchase' + **one-field-at-a-time modal**" | `CARDSTOCK_UI_SPEC_v1.md:194` (Tier 3) | The copy matches (L84–85) but the modal is the same single full form in every entry path (L236–325). No progressive/one-field variant exists. |
| 15 | Global keyboard map: "`Esc` close overlay" | `CARDSTOCK_UI_SPEC_v1.md:129` (Tier 3) | No key handling on this screen. The only `addEventListener` calls are the column-resize `mousemove`/`mouseup` (L441–442). The modal is dismissible by backdrop click, `✕`, and Cancel only. |
| 16 | "Track your binder against the market, **to the cent**" | `CARDSTOCK_UI_SPEC_v1.md:421` (Tier 3) | `money()` rounds to whole dollars (L413) and is used for every currency figure including user-entered ledger prices (L515). No cents are rendered anywhere on this screen. |
| 17 | Inline correction control is "**✉**" | `DESIGN_NOTES.md:82` | The control is `edit ✎` (pencil) — L159. `DESIGN_NOTES.md:63` uses ✎ correctly; `:82` is a typo in the spec-delta summary. Minor, but recorded so the ported label is unambiguous. |
| 18 | Transactions tab shows "Every buy, sell, and **correction**, newest first" | in-HTML tab tooltip, L491 | Corrections are not shown as rows — they replace the original in place (L472, L606–609). The ledger shows current truth only, so the tooltip over-promises what the table displays. Recorded as Tier-1 copy needing a decision alongside §7 Q9. |

**Not contradictions — verified consistent:** three tabs + table/gallery toggle
(`DESIGN_NOTES.md:62` ✓ L491, L74–79); SELL holdings-constrained with read-only tier and capped qty
(`DESIGN_NOTES.md:64` ✓ L266–271, L299–301, L571–573); number inputs with no spinners
(`DESIGN_NOTES.md:65` ✓ L19–20); EST badge and iron-rule strip (`DESIGN_NOTES.md:66` ✓ L121, L231–234);
BUY card field as a must-link typeahead over the corpus (`DESIGN_NOTES.md:73` ✓ L579–587); performance
tab indexed to 100 at first transaction (`DISPLAY_VOCABULARY.md:190` ✓ L171); the three route-level
features listed in `HANDOFF.md:74` (✓).
