# Screen spec — Charts

**Extracted from Tier 1:** `CardStock Mockup/Cardstock Charts.dc.html` (928 lines), read in full 2026-08-10.
All `L###` citations in this document refer to that file unless another file is named.
Where a Tier 2/3 document disagrees, **the HTML wins** — see §8.

Seeded sample data (the Umbreon card, the 60 synthetic monthly series, the hard-coded readout literals) is
illustrative only. What is normative here is **structure and state space**.

---

## 1. Identity

| | |
|---|---|
| **Screen name** | Charts (nav label `Charts`, `data-screen-label="Charts"` — L33) |
| **Route** | `/charts` (`HANDOFF.md`:73). The prototype is a single card with no id in the URL. |
| **Deep link** | `#signals` — L340. Only inbound user of it in the mockup set is the Home peek panel's `edit →` (`Cardstock Home.dc.html`:257). |
| **Purpose** | One card, one grade tier: plot its monthly price history, overlay or sub-pane any indicator the data honestly supports, mark every historical rule trigger, and save the resulting indicator set as the card's tracked signals or as a named view. |

**Deep-link behaviour (L339–345).** On mount, if `location.hash === '#signals'`: force `leftOpen: true` and set
`panelGlow: true`; a 2600 ms timer clears the glow. Glow renders as `box-shadow: inset 0 0 0 2px #4A63D0` on the
aside, with `transition: box-shadow 0.7s` (L70, L731). Timer is cleared on unmount (L345). No other hash is handled.

**Not present, despite the spec.** No card-id route segment and no card-swap control. The card is hard-coded
(L72–79). `uploads/CARDSTOCK_UI_SPEC_v1.md`:114 (`/charts/{cardId?}`) and :184 ("Top bar: card search swap")
describe a screen the prototype does not implement — see §8.

**Component props** (L329, the `data-props` block) — two, both overridable by state:
- `defaultRange`: enum `1Y | 3Y | All`, default `3Y`. Consumed at L458 as the fallback when `state.range` is null.
- `compareIndex`: boolean, default `false`. Consumed at L459 as the fallback when `state.cmp` is null.

---

## 2. Layout

**Fixed app frame — the page itself never scrolls vertically.**

Root (L33): `height: 100vh; min-width: 1080px; overflow-x: auto; overflow-y: hidden; display: flex;
flex-direction: column; font-size: 15px`. Below 1080 px the whole frame scrolls **horizontally**; it never reflows.

| Region | Element | Scroll | Size |
|---|---|---|---|
| Nav | `<nav>` L35 | none (`position: sticky; top: 0; z-index: 20`) | 48 px tall, `padding: 0 20px`, `gap: 24px` |
| Body row | `<div>` L67 | none | `flex: 1; display: flex; align-items: stretch; min-height: 0` |
| Indicator panel | `<aside aria-label="Indicators">` L70 | **`overflow-y: auto`** — independent | `width: 272px; flex-shrink: 0; padding: 14px 14px 20px 14px` |
| Chart column | `<main>` L187 | **`overflow-y: auto`** — independent | `flex: 1; min-width: 0; padding: 14px 18px 24px 18px` |

The aside is wrapped in `sc-if leftOpen` (L69/L185) — collapsing it removes it from the flow entirely and `main`
takes the full width. Toggled from the toolbar (L189), glyph `«` when open / `»` when closed (L700).

**Nav contents, left to right** (L36–64): logo + wordmark → Home · five section links (Home, Screener,
**Charts** — active: ink, weight 600, 2 px accent bottom border · Binder, Browse) · flex spacer ·
`<cardstock-search>` (`flex: 0 1 280px; min-width: 110px`) · watchlist button · Views button + dropdown ·
account circle `O` → Profile. **No bell** (alerts were cut wholesale).

**Chart column stacking order** (top to bottom, all in `main`):
1. Toolbar row (L188–204), `flex-wrap: wrap`
2. Stats strip (L206–213), `flex-wrap: wrap`, card + 1 px border + radius 8
3. Price chart card (L215–260)
4. Data table (L262–274) — `sc-if tableOpen`
5. 0–2 indicator panes (L276–323) — `sc-for panes`
6. Static footnote (L324)

**SVG rendering constraints inherited from the DC runtime** (documented at `DESIGN_NOTES.md`:34 and visibly
obeyed by the HTML): every `sc-for` inside an `<svg>` emits exactly one element per iteration, and SVG `<text>`
inside a loop does not render at all. Consequence, which a rebuild is free to drop but must reproduce visually:
**all axis labels are absolutely-positioned HTML `<div>` overlays**, not SVG text (L241–249, L309–320). The left
gutter is 64 px of a 920-unit viewBox = the `left: 6.74%` anchor used by every y-label.

Geometry constants: price chart `viewBox 0 0 920 456`, plot box `L=64 R=14 T=14 B=24`, so the plot spans
x 64→906 and y 14→432 (L220, L466). Panes `viewBox 0 0 920 170`, plot y 14→156 (L288, L775). Bars are clamped to
`x ≥ 66` (L788, L834, L846, L899, L919).

---

## 3. Data contract

### 3.1 Card identity (left panel header, L71–79)

| Field | Render | Source in prototype |
|---|---|---|
| Card image | `<image-slot id="art-umbreon">`, 96×133, radius 6, hover `scale(2.2)` with `transform-origin: left top`, `z-index: 40` | placeholder slot |
| Card name | link → Card page | literal "Umbreon VMAX (Alt Art)" (L76) |
| Set name | link → Set page | literal "Evolving Skies" (L77) |
| Card number | text, ` · ` separated | literal "215/203" (L77) |
| `curPrice` | mono 14 px / 700 | `money(aS[i1])` — anchor tier at the **last visible index**, not "today" (L701) |
| `cur1m` | mono, **hard-coded `--pos2` green** (L78) | `fmtPct(roc(1))` (L701) |

> `cur1m` is styled green in the template regardless of sign. A negative 1M change renders green. Flag for rebuild.

### 3.2 Grade tiers — 19 values, exactly one price series each

Full set with display label, chip row, and series colour (`TIER_COLORS`, L375):

| Tier key | Chip label | Row | Colour |
|---|---|---|---|
| `PSA 10` | PSA 10 | Row 1 (L688) | `PAL.acc` |
| `CGC 10` | CGC | tens sub-panel | `#1F8FA8` |
| `CGC 10 Prist.` | CGC Pristine | tens sub-panel | `#0F6E86` |
| `TAG 10` | TAG | tens sub-panel | `#8646B8` |
| `ACE 10` | ACE | tens sub-panel | `#C24B4B` |
| `SGC 10` | SGC | tens sub-panel | `#5C6B9E` |
| `BGS 10` | BGS | tens sub-panel | `#8A7139` |
| `BGS 10 Black` | BGS Black | tens sub-panel | `#2B2D42` |
| `Grade 9.5` | Grade 9.5 | Row 2 (L689) | `#7A56C9` |
| `Grade 9` | Grade 9 | Row 2 | `PAL.warn` |
| `Grade 8` | Grade 8 | Row 2 | `#4C8F8A` |
| `Grade 7` | Grade 7 | Row 2 | `#A96A4A` |
| `Grade 6` … `Grade 1` | `6` … `1` | lows sub-panel (L694) | `#578AA3 · #5E9490 · #6A9678 · #7F9668 · #97906E · #A08D78` |
| `Raw` | Raw | Row 2, after lows chip (L692) | `PAL.mut2` |

Tens short label rule: `t.replace(' 10','').replace('Prist.','Pristine')` (L671). Lows: `t.replace('Grade ','')`.
Group membership arrays: `TENS` L669, `LOWS` L670.

Per chip (`mkTier`, L652–667): `name`, `tip`, `dot`, `bg`, `bd`, `fg`, `toggle`.
Per group chip (`mkGroup`, L672–682): `name` (active members joined `', '`, else the group label, plus `' ▾'`
open / `' ▸'` closed), `dot`, `bg`, `bd`, `fg`, `toggle`.

### 3.3 Series data (60 monthly points, index 0 = Aug 2021 … index 59 = Jul '26)

`N = 60` (L374). `mLabel(i)` → `MON ’YY` where year `= 2021 + floor((7+i)/12)`, month `= (7+i) % 12` (L431).
Date inputs are bounded `min="2021-08-01" max="2026-07-31"` (L198–199) — consistent with the index space.

| Field | Meaning | Line |
|---|---|---|
| `S[tier][i]` | monthly price per tier, all 19 | L379–386 |
| `IDX[i]` | market index | L387 |
| `SETIDX[i]` | set index (Evolving Skies) | L393 |
| `SEAM = 44` | Apr '25 — liquidity seam drawn in the liquidity panes | L388 |
| `RSEAM = 59` | Jul '26 — per-sale-ledger seam drawn on the **price** chart | L389 |
| `CHURN[i]` | sales/day, `null` for `i < SEAM` | L390 |
| `VOLC[i]` | sales/month integer, `null` for `i < SEAM` | L391 |
| `POPD[i]` | census growth %, non-null only for `i ∈ [53, 59]` — 7 observations | L392 |
| `AMIHUD[i]` | price impact %/$1k, `null` for `i < SEAM` | L394 |
| `DISP[i]` | σ/μ, `null` for `i < SEAM` | L395 |
| `DTL[i]` | % below list, `null` for `i < SEAM` | L396 |
| `E4G[i]` | auction premium %, `null` for `i < SEAM` | L397 |
| `OVH[i]` | years of supply, non-null only for `i ∈ [53, 59]` | L398 |
| `SEAS[i]` | seasonal factor | L399 |
| `SPREAD[i]` | `S['PSA 10'][i] / S['Raw'][i]` | L400 |
| `ARBEV[i]` | `0.46·PSA10 + 0.54·G9 − Raw − 45` | L401 |

**Nullability is load-bearing.** Every `pts()` builder skips `null`/`undefined` (L495, L776), so a gap renders as
a gap, never as a zero and never as an interpolated segment. Preserve this.

### 3.4 Toolbar fields

| Field | Value space | Line |
|---|---|---|
| `panelGlyph` | `«` \| `»` | L700 |
| `axisMode` | `("USD · monthly avg" \| "indexed · start = 100") + " · " + (i1−i0+1) + " pts"` | L737 |
| `resBtns[]` | 3 rows: `{label, tip, bg, fg, bd, cur, click}` | L739–743 |
| `fromVal` / `toVal` | `YYYY-MM-01`, always echoing the **computed** window (`i2d(i0)` / `i2d(i1)`) | L738 |
| `rangeBtns[]` | 4 rows: `{label, tip, pick, bg, fg, bd}` for `1Y · 3Y · 5Y · All` | L695–696 |
| `tblBg` / `tblFg` | `accBg`/`acc` when open, `card`/`mut` when closed | L748 |

### 3.5 Stats strip — exactly 6 tiles (L563–570)

| Label | Value | Colour rule | Computed? |
|---|---|---|---|
| `ROC 3M` | `fmtPct(aS[i1]/aS[i1−3] − 1)` | `pos2` if ≥ 0 else `neg2` | yes |
| `ROC 12M` | `fmtPct(… 12)` | same | yes |
| `Drawdown` | `fmtPct(aS[i1]/peak − 1)`, `peak = max(aS[0..i1])` | `neg2` if < −0.001 else `mut` | yes — note peak scans from index **0**, not `i0`, so it is always all-time-to-date |
| `z vs 6M` | `(aS[i1] − sma6[i1]) / sd6[i1]`, 2 dp, `+`/`−` | `warn` if \|z\| > 1.5 else `mut` | yes |
| `Trend R² 12M` | `0.87` | `mut` | **literal** |
| `RS pct 3M` | `94th` | `pos2` | **literal** |

### 3.6 Price chart fields (L215–258)

`chartTitle` = `"Price history · " + (1 tier ? anchor : 0 tiers ? "no tier selected" : N + " tiers")` (L750).
`chartSub` = `mLabel(i0) + " – " + mLabel(i1)` (L751).

| Field | Shape | Line |
|---|---|---|
| `yTicks[]` | 5 evenly spaced: `{y, ty, t, label}`; label = `money(v)` or `v.toFixed(0)` when normalized | L539 |
| `xTicks[]` | year boundaries (`(7+i)%12 === 0 && i > i0`) → `{x, l, label: "2024"}`; **replaced** when `i1−i0 ≤ 14` by month labels at step 1 (span ≤ 6) or 3 | L540–541 |
| `lines[]` | `{pts, color, w, dash}` — one per visible tier (`w = 1.8` for the anchor, `1.3` otherwise, `dash: 'none'`), then EMA fast/slow, then SMA | L496–502 |
| `bollPts` | one closed polygon (upper forward + lower reverse), `fill: rgba(74,99,208,0.06)` | L503–509 |
| `idxPts` | polyline, `mut2`, `stroke-dasharray="4 4"`, width 1.2 | L510, L229 |
| `tris[]` | `{pts, fill, tip}` — trigger triangles | L511–537 |
| `hollows[]` | `{x, y, color}` — one per visible tier at `i1`, radius 3.5, fill `card`, `<title>current month still revising</title>` | L538, L236–238 |
| `rsX`, `rsL`, `rsOn` | price-chart seam position / label position / visibility | L753–755 |
| `hoverX`, `hasHover`, `hoverMonth`, `hoverRows[]` | crosshair + tooltip | L756–757, L542–556 |

`hoverRows[]` composition, in order (L544–555): every visible tier (`{name, dot, val: money}`) → `MKT INDEX`
(only when compare on, value `IDX[i].toFixed(1)`) → `EMA {fast}` and `EMA {slow}` → `SMA {len}` (only when
non-null) → `BOLL ±{m}σ` rendered as a **range** `"$low–$high"`.

**SVG paint order on the price chart** (L221–239), which is the z-order a rebuild must keep:
y gridlines (`line4`) → x gridlines (`hov`) → seam dashed vertical (`warn`, `3 3`) → Bollinger polygon →
index polyline → tier + overlay polylines → trigger triangles → hollow end dots → hover crosshair (`line3`).

### 3.7 Table fields (L262–274)

Header literal: `Last 12 months · visible tiers` (L264). Grid: `90px repeat({{tblColCount}}, 1fr)`.
`tblColCount` = visible tier count (L749). `tblHead[]` = `{name, color}` per visible tier. `tblRows[]` =
`{m: mLabel(i), cells: [{v: money}]}` for `i` in `[i1−11, i1]` (L698) — the last 12 months **of the visible
window**, not of today.

### 3.8 Pane fields (L276–323)

Per pane object: `title`, `sub`, `badge`, `badgeTip`, `hoverTxt`, `close`, `hx`, `regions[]`, `yts[]`,
`hlines[]`, `bars[]`, `lines[]`, `vlines[]`, `xt[]`, `vf(i)` (L572–583).

- `hx` = crosshair x, `-20` when not hovering (L578) — parked offscreen, element always present.
- `hoverTxt` = `mLabel(hoverI) + " · " + vf(hoverI)` when hovering, else `""` (L579).
- `hlines` gain a `t` percentage; `yts` are then **filtered** to drop any tick within 11 % of an hline, so
  reference-line labels never collide with axis labels (L580–581).
- `vlines` gain an `l` percentage for their HTML label overlay (L582).
- `xt` is the **price chart's** `xTicks` — panes share the main x axis exactly (L577).

`paneScaffold` (L769–779): scans all supplied series for min/max ignoring nulls; falls back to `lo=0, hi=1` when
everything is null and to `hi = lo + 1` when flat; pads the range by 12 % each side; `Y(v) = 14 + (1 − (v−lo)/(hi−lo)) · 142`;
emits exactly **2** y-ticks, at 0.88 and 0.12 of the padded range.

---

## 4. States

### 4.1 The three row forms — and what actually distinguishes them

The template defines three mutually exclusive branches, selected by a boolean flag on the row model:

| Form | Flag | Renders | Template |
|---|---|---|---|
| **toggle** | `isToggle` | 26×15 switch + label (dotted-underline, `cursor: help`) + badge; optionally an amber warning strip; optionally a parameter row — **only while the indicator is on** | L122–138 |
| **readout** | `isReadout` | indented 34 px; label (dotted-underline, `cursor: help`) + right-aligned mono value. **No control at all.** | L139–144 |
| **locked** | `isLocked` | indented 34 px at `opacity: 0.62`; label + `LOCKED` badge + unlock note + optional progress bar (`progPct` fill, `progTxt` caption) + optional `show anyway →` link | L145–162 |

**The distinguishing rule, stated precisely:** a row is a *toggle* if it owns a switch that mutates
`state.inds`; a *readout* if it renders a value and owns no control; a *locked* row if it renders an unlock
condition instead of a value and owns no working switch. Indentation is the visual tell — toggles start at the
switch (x = 0), readouts and locked rows are indented 34 px to line up under the toggle labels.

> ### ⚠ Only two of the three forms are ever instantiated in this prototype.
>
> The `locked(...)` factory exists at **L595** and is **never called** — verified by exhaustive grep of the file.
> Every row `DISPLAY_VOCABULARY.md` §10 calls "locked" is built by **`lockedOr(...)` (L596–598)**, which is:
>
> ```js
> Object.assign(row(id, name, { pane: true, tip, badge: 'LOW DATA', … }), { hasWarn: true, warnTxt: note })
> ```
>
> — i.e. **a working toggle** with a `LOW DATA` badge and a *permanently visible* amber warning strip carrying
> the unlock sentence. Its `prog` and `progTxt` arguments are accepted and **discarded**.
>
> Consequences a rebuild must decide on deliberately:
> - No `LOCKED` badge, no progress bar, and no `show anyway →` link renders anywhere on this screen.
> - `force(id)` (L403–409) and `state.forced` (L349) are unreachable, so `LOW CONFIDENCE · BURNED IN` (L576)
>   never appears at runtime.
> - Even if `locked()` *were* called, the link at L159 binds `onClick="{{ r.force }}"` and `locked()` never sets
>   a `force` key — the override would be inert.
>
> This is the single largest gap between the HTML and every document describing it. See §7 and §8.

### 4.2 Complete indicator row inventory — 7 groups, 31 rows

Legend for **Form**: `toggle` = real switch · `readout` = value only · `lockedOr` = toggle built by the
`lockedOr` factory (what the docs call "locked").
**Opens pane** = `pane: true` was passed, i.e. the row contributes a sub-chart when on.

#### Group 1 — `Trend` (note: none) — L601–608

| Row | id | Form | Opens pane | Params (min–max, default) | Badge | Warning when on (`suff`) |
|---|---|---|---|---|---|---|
| EMA cross | `ema` | toggle | no — price overlay | `fast` 2–12 (**3**), `slow` 3–24 (**9**) | — | only if `emaSlow > 12`: "slow window {n}M leaves few independent observations" (L426) |
| SMA baseline | `sma` | toggle | no — price overlay | `len` 3–24 (**9**) | — | — |
| MACD | `macd` | toggle | **yes** | `f` 2–12 (**3**), `s` 3–24 (**6**), `sig` 2–12 (**4**) | — | only if `macdS > 8`: "warmup consumes {f+s} of 60 months" (L425) |
| ROC 1M | — | readout | — | — | value `fmtPct(roc(1))`, `pos2`/`neg2` | — |
| Trend slope (12M) | — | readout | — | — | value `+2.1%/mo` (**literal**), `pos2` | — |
| Seasonality overlay | `seas` | **lockedOr** | **yes** | — | `LOW DATA` (amber) | always: "unlocks after 3 observed cycles · Nov 2027"; pane badge tooltip "only 1/3 cycles observed — forced early" (L417) |

MACD tooltip: *"Monthly data is fully sufficient — re-tuned (3,6,4) for monthly bars."* — a **tooltip**, not a badge.

#### Group 2 — `Momentum · mean reversion` (note: none) — L609–614

| Row | id | Form | Opens pane | Params | Badge | Warning when on |
|---|---|---|---|---|---|---|
| RSI | `rsi` | toggle | **yes** | `len` 3–12 (**6**) | `SLOW ON MONTHLY` (default grey) | "slow on monthly data — can stay overbought through whole runs" (L412) |
| Bollinger bands | `boll` | toggle | no — price overlay | `k` 4–12 (**6**), `m` 1–3 (**2**) | — | — |
| z-score vs 6M MA | `z` | toggle | **yes** | — | — | — |
| Drawdown from peak | — | readout | — | — | `fmtPct(dd)`, `neg2` if < −0.001 else `mut` | — |

#### Group 3 — `Relative` (note: none) — L615–620

| Row | id | Form | Opens pane | Params | Badge | Warning |
|---|---|---|---|---|---|---|
| RS vs market index | `rs` | toggle | **yes** | — | — | — |
| Set rotation (Evolving Skies) | `f3` | toggle | **yes** | — | `CORPUS` (grey) | — |
| RS percentile (3M) | — | readout | — | — | `94th` (**literal**), `pos2` | — |
| Beta vs index (24M) | — | readout | — | — | `1.31` (**literal**), default ink | — |

> The set name in the row label and the pane title is hard-coded to the seeded card's set. A rebuild must
> parameterise it (`Set rotation ({set.name})`).

#### Group 4 — `Liquidity` (group note: **`post-seam`**) — L621–629

| Row | id | Form | Opens pane | Params | Badge | Always-on note / warning |
|---|---|---|---|---|---|---|
| Churn 30d | `churn` | toggle | **yes** | — | `POST-SEAM` (amber: `warnInk` on `rgba(176,127,26,0.12)`) | when on: "only 16 post-seam months — medium confidence" (L414) |
| Volume & count | `vol` | toggle | **yes** | — | `POST-SEAM` (amber) | when on: "only 16 post-seam months — medium confidence" (L414) |
| Churn acceleration | — | readout | — | — | `×1.6 vs 90d` (**literal**), `pos2` | — |
| Amihud illiquidity | `amihud` | **lockedOr** | **yes** | — | `LOW DATA` (amber) | always: "needs 24 post-seam months · ~Apr 2027"; pane badge tip "16/24 post-seam months — forced early" (L418) |
| Price dispersion | `disp` | **lockedOr** | **yes** | — | `LOW DATA` | always: "needs ≥8 sales/mo in bucket"; pane badge tip "3/8 sales per month — forced early" (L419) |
| Discount-to-list | `dtl` | **lockedOr** | **yes** | — | `LOW DATA` | always: "listed price on 12% of rows"; pane badge tip "12% listed-price coverage — forced early" (L420) |
| Cross-marketplace gap | `e4` | **lockedOr** | **yes** | — | `LOW DATA` | always: "needs ≥5 sales/venue/window — eBay-only depth today"; pane badge tip "1/5 venues with depth — forced early" (L421) |

#### Group 5 — `Supply` (group note: **`2026+`**) — L630–635

| Row | id | Form | Opens pane | Params | Badge | Note / warning |
|---|---|---|---|---|---|---|
| Pop Δ monthly | `popd` | toggle | **yes** | — | `NEW · 7 OBS` (**accent**: `PAL.acc` on `PAL.accBg`) | when on: "only 7 census observations — low confidence" (L415) |
| Pop vs price divergence | `d2` | toggle | **yes** | — | `NOVEL · 2026+` (amber) | when on: "only 7 paired price+census months — low confidence" (L416) |
| Gem rate | — | readout | — | — | `46% · drift −0.8pp` (**literal**), default ink | — |
| Supply overhang | `overhang` | **lockedOr** | **yes** | — | `LOW DATA` | always: "needs 12M of census history"; pane badge tip "7/12 census months — forced early" (L422) |

`NEW · 7 OBS` is the only badge in the panel using the **accent** palette rather than grey or amber — it marks
a *newly unlocked, on-probation* metric, not a caution. Preserve that distinction.

#### Group 6 — `Valuation` (note: none) — L636–639

| Row | id | Form | Opens pane | Badge |
|---|---|---|---|---|
| Tier spread 10/raw | `spread` | toggle | **yes** | `4.8× · COMPRESSING` (**literal string**, amber) |
| Grading-arb EV raw→10 | `arbev` | toggle | **yes** | `+$118` (**literal string**, `PAL.pos` on `PAL.posBg(0.10)`) |

Both badges look computed and are not — they are authored literals at L637–638 while the underlying series
(`SPREAD`, `ARBEV`) are computed at L400–401. A rebuild should drive them from the series.

#### Group 7 — `Composites` (group note: **`multi-signal`**) — L640–645

| Row | id | Form | Opens pane | Badge | Draws |
|---|---|---|---|---|---|
| Quiet Accumulation | `g1` | toggle | **no** | `ACTIVE · JUN ’26` (`PAL.pos` on `posBg(0.10)`) | 1 up-triangle at i = 58 (L532) |
| Supply Flood Watch | `g2` | toggle | **no** | `CLEAR` (grey) | **nothing** — empty event array (L533) |
| Breakout Confirmation | `g3` | toggle | **no** | `LAST · AUG ’25` (grey) | 1 up-triangle at i = 48 (L534) |
| 3M RS Leaders | `g4` | toggle | **no** | `MEMBER · MAR ’26` (`PAL.pos` on `posBg(0.10)`) | 1 up-triangle at i = 55 (L535) |

Composites produce **trigger triangles only** — no overlay, no pane. Their badge encodes membership state
(`ACTIVE` / `CLEAR` / `LAST · <month>` / `MEMBER · <month>`) and its colour encodes whether the state is a hit.
Also carries `suff` warnings when on: `g1` "churn leg has 16 post-seam months — medium confidence" (L423),
`g2` "census leg has 7 observations — low confidence" (L424).

**Totals:** 31 rows = **24 toggles** (17 pane-capable, 3 price overlays, 4 composites) + **7 readouts**.
`state.inds` has exactly 24 keys (L348); `paneModels` has exactly 17 (L571).

#### Row-level visual states

| Element | States |
|---|---|
| Switch background (`swBg`) | on → `PAL.acc` · off → `PAL.line2` · **blocked** → literal `#EDEDE9` (L591) |
| Switch knob (`swX`) | on → `translateX(11px)` · off → `0`, `transition: transform 0.12s` |
| Label | always dotted-underlined (`underline dotted rgba(138,138,134,0.55)`, offset 3 px) with `cursor: help` |
| Badge | absent → `background: transparent`; present → `badgeBg` + `badgeFg` |
| Warning strip | `sc-if hasWarn` → `▲ {warnTxt}` in amber on `rgba(176,127,26,0.08)` with a `0.22` border, indented 34 px |
| Params row | `sc-if showParams` = `on && row has params` — **parameters are hidden until the indicator is enabled** |
| Group header | `chev` `▾` open / `▸` closed; all groups default **open** (`state.cg` starts undefined, L649) |

### 4.3 The one-tier rule

**Invariant: indicators analyse exactly one grade tier — the *anchor*.**

`anchor = state.anchor if still visible, else 'PSA 10' if visible, else visTiers[0], else 'PSA 10'` (L469).
`state.anchor` is read but **never written anywhere in the file** — there is no UI to pick an anchor. In
practice the anchor is PSA 10 when visible, otherwise the first visible tier in the fixed key order of
`state.tiers` (L347). Note the fallback to `'PSA 10'` when **zero** tiers are visible: all readouts, all six
stat tiles, and the left-panel price keep computing PSA 10 numbers while the chart is empty.

**Enabling is blocked when the visible-tier count ≠ 1** (`toggleInd`, L440–455):

```
on = !inds[id]
… compute new inds and paneOrder …
if (on && visibleTierCount !== 1) return;     // silent no-op — nothing is committed
setState({ inds, paneOrder })
```

- Turning an indicator **off** always works, at any tier count.
- The block is a **silent early return**, not a disabled control: the `<button>` has no `disabled` attribute and
  stays focusable and clickable. Its only affordances are the greyed track (`#EDEDE9`) and the tooltip
  *"Show a single tier to enable indicators."* (L592).
- `blocked = !on && visTiers.length !== 1` (L589) — so the grey track appears only on rows that are currently off.

**Stash / restore across tier changes** (`mkTier.toggle`, L655–667):

| Transition | Behaviour |
|---|---|
| new visible count **≠ 1** *and* at least one indicator on | `stash = { inds, paneOrder }` (pre-change), then **all** `inds` set false and `paneOrder = []` |
| new visible count **≠ 1** and no indicator on | nothing stashed; any existing stash survives untouched |
| new visible count **=== 1** and a stash exists | `inds` and `paneOrder` restored from stash; `stash = null` |
| new visible count **=== 1** and no stash | nothing |

Note `≠ 1` includes **zero** tiers, so deselecting the last tier also clears and stashes the indicator set.
`applyView` explicitly clears `stash: null` (L362) — applying a view discards any pending restore.
`state.forced` is **not** cleared by stashing.

**Tier notice** (L734–736), rendered in a `min-height: 31px` block below the chips — four states:

| Condition | Copy |
|---|---|
| any indicator on | `Indicators analyze {anchor} — adding another tier switches indicators off.` |
| none on, > 1 tier visible | `Indicators disabled while multiple tiers are shown — keep one tier to enable.` |
| none on, 0 tiers visible | `No tier selected — pick one to chart.` |
| none on, exactly 1 tier | `""` (empty, block keeps its height) |

### 4.4 Pane states

- **Cap = 2, FIFO.** Enabling a third pane `shift()`s the oldest out of `paneOrder` **and sets that
  indicator's switch off** (L446). Order is enable-order; there is no reorder control.
- **Rendered set** = `paneOrder.filter(id => inds[id])` (L572) — `paneOrder` is authoritative for order,
  `inds` for presence.
- **Pane badge** (L575–576), exactly three states:
  - `state.forced[id]` → `LOW CONFIDENCE · BURNED IN` — *unreachable in this prototype* (§4.1)
  - else `suff(id)` non-null → `LOW CONFIDENCE`, tooltip = the `suff` string
  - else → no badge (`sc-if pn.badge`)
- **Close** (`✕`, L286): sets `inds[id] = false` and drops it from `paneOrder` (L780). Tooltip: *"Remove this
  indicator pane — the indicator stays available in the panel."*
- **Hover text** in the pane header: `mLabel(i) · vf(i)` while hovering, empty otherwise.

Complete pane catalogue (17):

| id | Title | Subtitle | Marks | Reference lines | Seam | Hover `vf` / null text |
|---|---|---|---|---|---|---|
| `macd` | MACD | `(f,s,sig) · {anchor}` | histogram bars `posBg(0.4)`/`negBg(0.4)` + MACD line (`acc`) + signal line (`warn`, dashed `4 3` in CVD only) | `0` solid | — | `MACD x · sig y · hist z` |
| `rsi` | RSI | `({len}) · bands 80/20 · {anchor}` | line (`acc`), **fixed 0–100 scale**, no y-ticks | `80`, `20` dashed | — | `RSI x` / `warming up` |
| `z` | z-score vs 6M MA | `stretched beyond ±1.5 · {anchor}` | line (`acc`), no y-ticks | `+1.5`, `0`, `−1.5` | — | `z ±x.xx` / `warming up` |
| `rs` | RS vs market index | `ratio × 100 · above 100 = outperforming` | line (`acc`) | `100` dashed | — | `RS x` |
| `f3` | Set rotation · Evolving Skies | `set index vs market × 100 · above 100 = money rotating in` | line (`acc`) | `100` dashed | — | `ratio x` |
| `churn` | Churn 30d | `sales/day · PSA 10 bucket · post-seam` | line (`acc`) | — | **yes** | `x.xx sales/day` / `pre-seam` |
| `vol` | Volume & count | `observed sales/mo · PSA 10 · post-seam` | bars `rgba(74,99,208,0.45)` grounded at y = 156 | — | **yes** | `N sales` / `pre-seam` |
| `amihud` | Amihud illiquidity | `price impact per $1k volume · post-seam` | line (`acc`) | — | **yes** | `impact x.xx%/$1k` / `pre-seam` |
| `disp` | Price dispersion | `σ/μ of realized prices in bucket · post-seam` | line (`acc`) | — | **yes** | `σ/μ x.xx` / `pre-seam` |
| `dtl` | Discount-to-list | `avg % below listed price · rows with list price only` | line (`acc`) | — | **yes** | `x.x% below list` / `pre-seam` |
| `e4` | Cross-marketplace gap | `auction-house premium vs eBay · post-seam` | line (`acc`) | — | **yes** | `auction +x.x% vs eBay` / `pre-seam` |
| `popd` | Pop Δ monthly | `PSA+CGC census growth % · 2026+` | bars `rgba(176,127,26,0.5)` around 0 | `0` solid | — | `+x.x%` / `pre-census` |
| `d2` | Pop vs price divergence | `price ROC 1M − pop growth · red = supply flooding · 2026+` | signed bars `posBg(0.5)`/`negBg(0.5)` | `0` solid | — | `±x.xpp` / `pre-census` |
| `overhang` | Supply overhang | `pop ÷ annualized sales · 2026+` | bars `rgba(176,127,26,0.5)` grounded at y = 156 | — | — | `x.x yrs of supply` / `pre-census` |
| `seas` | Seasonality | `seasonal factor · 1 observed cycle · illustrative` | line (`acc`) | `1.0` dashed | — | `factor x.xxx` |
| `spread` | Tier spread | `PSA 10 ÷ raw · falling = compressing` | line (**`warn`**) | — | — | `x.xx×` |
| `arbev` | Grading-arb EV | `gem×PSA10 + (1−gem)×G9 − raw − fees · above 0 = grade it` | line (**`pos2`**) | `$0` dashed | — | `+$x EV` / `−$x EV` |

`popd` additionally draws a **restatement region**: a `rgba(176,127,26,0.10)` rect spanning ±1 bar width around
index 55 (Mar '26), `<title>restatement window — grader republished Mar 2026 census</title>` (L922). This is the
only `regions[]` user in the file.

`churn` and `vol` hard-code `PSA 10` into their subtitles even though the anchor may be another tier —
parameterise on rebuild.

### 4.5 Seam rendering

Two distinct seams, drawn differently, on different surfaces:

| Seam | Where | Index | Render |
|---|---|---|---|
| **Per-sale ledger / resolution seam** | **price chart only** | `RSEAM = 59` → Jul '26 | amber (`--warn`) vertical, `stroke-dasharray="3 3"`, spanning y 14→432 (L227). HTML label overlay `per-sale ledger begins · Jul ’26 →` at `left: rsL%; top: 4%; transform: translateX(-100%); padding-right: 6px` — i.e. right-aligned *into* the line (L244–246). Label is wrapped in `sc-if rsOn`, so it hides when the seam is outside the window; **the line element itself always renders**, parked at `x = -20` when out of range (L753). |
| **Liquidity seam** | six panes: `churn`, `vol`, `amihud`, `disp`, `dtl`, `e4` | `SEAM = 44` → Apr '25 | amber vertical `3 3` spanning y 14→156, plus an HTML label `seam · Apr ’25` at `left: l%; top: 14%; margin-left: 5px` in the line's colour (L829, 839, 870, 878, 886, 894 + L305–307, L315–317). Emitted only when `SEAM ≥ i0`. |

Both seams are **boundaries, not blends** — the series on either side are the same polyline with `null` gaps
where data does not exist (§3.3). Nothing interpolates across a seam.

The Apr '25 liquidity seam is the subject of open decision **D-009**: per **D-001** no per-sale data exists
before late Jul 2026, so this seam has no data behind it. The HTML draws it in **six** panes, not the two named
in `DESIGN_NOTES.md`:35.

### 4.6 Current-month marker

`hollows[]` (L538) places one hollow circle (r 3.5, fill `card`, stroke = tier colour, width 1.6) at index
**`i1`** for every visible tier, with `<title>current month still revising</title>` (L236–238).

`i1` is the **last index of the visible window**, not the current month. With a custom `to` date, or any range
that ends before Jul '26 — which cannot happen here since `i1` defaults to `N−1`, but *can* via the `to`
picker — the "still revising" marker lands on a closed month. Flagged in §7.

There is **no dashed final segment**: every tier polyline is emitted with `dash: 'none'` (L496).

### 4.7 Watchlist button state machine (L713–730)

`NONTRACK = ['sma', 'boll', 'dtl', 'seas']` (L714) — display-only overlays are excluded from the tracked set
and never arm the button. `tkOn` = enabled indicator ids minus `NONTRACK`, sorted; `wlOn` = same over
`state.wlSaved`.

| Condition | Label | `bg` / `fg` / `bd` | Click |
|---|---|---|---|
| `!watch` | `+ Add to watchlist` | `acc` / `card` / `acc` | saves current tracked set, `watch: true`, flash |
| `watch && tkOn.length === 0` | `Remove from watchlist` | **`neg2`** / `card` / `neg2` | `watch: false`, `wlSaved: {}` |
| `watch && tkOn ≠ wlOn` | `Update watchlist` | `acc` / `card` / `acc` | overwrites `wlSaved`, flash |
| `watch`, clean, `wlFlash` | `✓ Watchlist updated` | `card` / `pos2` / `line` | (transient, 2200 ms — L728) |
| `watch`, clean | `✓ On watchlist` | `card` / `mut` / `line` | no-op |

Each state has its own tooltip (L724–727); the clean-state tooltip names the tracked set:
*"On your watchlist, tracking {names}. The enabled indicators ARE the tracked set — toggle in the left panel,
then save here."*

### 4.8 Views dropdown state (L47–63, L702–712)

`viewsLabel` = `"View: {activeView} ▾"` when a view is active, else `"Views ▾"` (L703).
Menu is `sc-if viewsOpen`, `position: absolute; right: 0; top: 34px; z-index: 60`, min-width 230 px.
Per row: check glyph `✓` when active (else empty, width reserved 12 px), name, `DEFAULT` badge when
`isDefault`, and a `✕` delete button. Footer action: `+ Save current as new view`.

Seeded views (L353–357) — structure only, values illustrative:

| Name | Default | tiers | inds | paneOrder | range | cmp | norm |
|---|---|---|---|---|---|---|---|
| Trend workspace | **yes** | PSA 10 | ema, macd | [macd] | 3Y | false | false |
| Liquidity check | no | PSA 10 | churn, vol | [churn, vol] | 1Y | false | false |
| vs Market | no | PSA 10 | rs | [rs] | 3Y | **true** | **true** |

### 4.9 Resolution toggle state (L739–743)

| Button | Background | Foreground | Cursor | Tooltip | Handler |
|---|---|---|---|---|---|
| `M` | `PAL.acc` | `PAL.card` | `pointer` | `Monthly bars — current resolution` | `() => {}` |
| `W` | `PAL.hov` | `PAL.mut3` | `not-allowed` | `Weekly bars — unlocks after ~6 months of per-sale ledger (~Jan 2027)` | `() => {}` |
| `D` | `PAL.hov` | `PAL.mut3` | `not-allowed` | `Daily bars — unlocks after ~12 months of per-sale ledger on liquid cards` | `() => {}` |

All three handlers are no-ops — the entire control is inert; only M is styled active. No `disabled` attribute is
set on any of them, so all three remain focusable and clickable. The segmented container puts `border-right` on
every button including the last (L195), so a stray 1 px divider sits inside the right edge.

### 4.10 Range / date window resolution (L458–465)

```
range  = state.range ?? props.defaultRange ?? '3Y'
custom = !!(state.from || state.to)
i1 = state.to   ? d2i(state.to)   : N−1
i0 = state.from ? d2i(state.from) : (custom ? 0
                                   : range === '1Y' ? i1−11
                                   : range === '3Y' ? i1−35
                                   : range === '5Y' ? max(0, i1−59)
                                   : 0)                      // 'All'
if (i1 <= i0 + 2) { i0 = max(0, i1−3); i1 = min(N−1, i0+3) }  // minimum 4-point window
```

`d2i` clamps to `[0, N−1]` (L461). A range button sets `range` and clears both `from` and `to` (L695).
A range button renders active only when `!custom` (L695) — typing a date deactivates all four.
`fromVal`/`toVal` always echo the resolved `i0`/`i1`, so the pickers are never blank.

---

## 5. Interactions

| # | Control | Line | Consequence |
|---|---|---|---|
| 1 | Nav section links | L38–42 | Navigate. Charts is the active item. |
| 2 | Nav search | L45 | Shared `<cardstock-search>` component; behaviour per `DISPLAY_VOCABULARY.md` §12. |
| 3 | Watchlist button | L46 | Add / update / remove per §4.7. On save, sets `wlFlash` for 2200 ms. |
| 4 | `Views ▾` | L48 | Toggles `viewsOpen`. |
| 5 | View row | L52 | `applyView` — overwrites tiers, inds, paneOrder, range; **clears `from`/`to`**, sets cmp, norm, activeView; closes the menu; **clears `stash`**. Does **not** restore indicator `params` (L362 vs L351). |
| 6 | View `✕` | L56 | `stopPropagation` then delete by name; if it was active, `activeView → null` (L369–371). |
| 7 | `+ Save current as new view` | L60 | Snapshots tiers, inds, paneOrder, `range ?? '3Y'`, cmp, norm under the auto-name `View {n+1}`; makes it active; closes the menu (L364–368). Params are **not** captured. Names can collide. |
| 8 | Menu dismissal | L50 | `onMouseLeave` only. **No outside-click and no `Esc` handler.** |
| 9 | Account circle | L64 | → Profile. |
| 10 | Card art hover | L72 | `scale(2.2)`, `z-index: 40`, deeper shadow, 0.15 s ease. |
| 11 | Card name / set name | L76–77 | → Card page / Set page. |
| 12 | Tier chip | L83, L99, L102 | Toggle that tier's visibility; y-axis rescales; may trigger stash/restore (§4.3). |
| 13 | Group chip (`other 10s`, `Grade 1–6`) | L85, L101 | **Opens/closes the sub-panel only** — despite a tooltip promising bulk show/hide (§8). |
| 14 | Sub-chip (tens / lows) | L92, L109 | Toggles that tier **and collapses the sub-panel** (L685). |
| 15 | Group header | L118 | Collapse/expand the group (`state.cg[name]`). `role="button" tabindex="0"` but **`onClick` only — no key handler**. |
| 16 | Indicator switch | L124 | `toggleInd(id, isPane)` — §4.3, §4.4. |
| 17 | Parameter input | L134 | `setP(key)` on **change**: `parseInt`; ignored unless `!isNaN && > 0`. `min`/`max` are advisory attributes only — the handler does **not** clamp. Everything downstream (series, triangles, panes, hover rows, badges) recomputes. |
| 18 | `show anyway →` | L159 | Would call `r.force`. **Never rendered** (§4.1). Intended: force-enable the pane and burn a permanent `LOW CONFIDENCE · BURNED IN` badge into the chart region — see `DECISIONS.md`:207. |
| 19 | Market index toggle | L172 | Flips `cmp`: draws the dashed grey index polyline, adds a `MKT INDEX` hover row, and includes the index in the y-axis scan (L479). |
| 20 | Normalize toggle | L177 | Flips `state.norm`. **Effective only when compare is on** — `norm = state.norm && cmp` (L471), and the switch renders from the *effective* value (L747), so with compare off the click has no visible effect. |
| 21 | `Why no candlesticks?` | L182 | → About Data page. |
| 22 | Panel toggle `« / »` | L189 | Show/hide the 272 px aside. |
| 23 | M / W / D | L195 | All no-ops (§4.9). |
| 24 | `from` / `to` date | L198–199 | Sets `from`/`to`; empty string → `null`. Enters custom mode, deactivating the range buttons. |
| 25 | Range button | L201 | Sets `range`, clears `from`/`to`. |
| 26 | `table` | L203 | Toggles the data table. Tooltip reads *"Show the underlying monthly closes as a table"* — see §8, D-006. |
| 27 | Chart hover (price chart **and** every pane) | L220, L288 | `chartMove` maps clientX → index via `i0 + (px−64)/(920−64−14)·(i1−i0)`, clamped to `[i0, i1]`, and sets `hoverI`. Because all charts share `hoverI`, hovering a pane moves the crosshair on the price chart and vice versa, and every pane header updates. |
| 28 | Chart leave | L220, L288 | `hoverI = null`; all crosshairs park at `x = -20`. |
| 29 | Trigger triangle hover | L234 | Native SVG `<title>` tooltip carrying rule, month, price, and +3M / +6M forward returns. |
| 30 | Hollow end dot hover | L237 | Native `<title>`: `current month still revising`. |
| 31 | Pane restatement region hover | L291 | Native `<title>`: the restatement sentence. |
| 32 | Pane `✕` | L286 | Removes the pane and switches the indicator off. |

### Trigger markers

`addTris` (L512–519) emits, per event:
- **up** → triangle **below** the price point: apex at `(x, y+6)`, base `(x±5, y+15)`, fill `PAL.pos2`
- **down** → triangle **above**: apex at `(x, y−6)`, base `(x±5, y−15)`, fill `PAL.neg2`

Events with `i < i0 + 1` are skipped (L513). Triangles are anchored to the **anchor tier's** transformed
y-value, so they follow normalization.

Sources of triangles, and only these:
1. **EMA cross** (`ema` on) — zero-crossings of `ema(fast) − ema(slow)`, starting at `max(emaSlow+2, i0)`.
   Tooltip: `EMA {f}/{s} crossover ▲|▼ — {month} · {price} · +3M {pct} · +6M {pct}` (L520–524).
2. **MACD cross** (`macd` on) — zero-crossings of `macd − signal`, starting at `max(f+s+1, i0)`.
   Tooltip: `MACD ({f},{s},{sig}) crossed above|below signal — {month} · {price} · +3M … · +6M …` (L525–530).
3. **Composites** `g1`, `g3`, `g4` — one hard-coded event each, with an authored evidence tooltip (L531–537).
   `g2` has an empty array, so enabling Supply Flood Watch draws nothing.

`fwd(a,i,k)` returns `null` → rendered `n/a` when `i+k` exceeds the series (L438).

### Compare and normalize

- `cmp` on → `idxPts` drawn as a dashed grey polyline; index values participate in the y-range scan (L479);
  a `MKT INDEX` row is appended to the hover tooltip with a raw 1-dp value (L545).
- Index scaling has **two modes** (L475): normalized → `IDX[i]/IDX[i0]·100`; not normalized →
  `IDX[i]/IDX[i0] · S[anchor][i0]`, i.e. the index is rebased onto the anchor's starting price so both fit one
  dollar axis.
- `norm` on (and cmp on) → every tier is rebased `v / S[tier][i0] · 100` (L472–474); y-tick labels switch from
  `money()` to integers (L539); `axisMode` switches to `indexed · start = 100` (L737). Hover tooltip values are
  **not** normalized — they stay in dollars (L544).

---

## 6. Rules and invariants

1. **The frame is fixed.** The page never scrolls vertically; the indicator panel and the chart column scroll
   independently; below 1080 px the frame scrolls horizontally rather than reflowing (L33).
2. **One tier drives all analysis.** Indicators cannot be enabled unless exactly one tier is visible; the block
   is a silent no-op with an explanatory tooltip, not a disabled control (L449–452, L592).
3. **Tier changes stash and restore the whole indicator set**, including pane order, and only when leaving/
   entering the single-tier state with something enabled (L659–665).
4. **Turning an indicator off is never blocked**, at any tier count.
5. **At most two panes; the third evicts the oldest and switches it off** (L446). Order is enable-order.
6. **Parameters are hidden until the indicator is on** (`showParams: on && !!o.params`, L592).
7. **Every row label carries a tooltip** and is dotted-underlined with `cursor: help` — the dotted underline is
   the "has an explanation" affordance (L125, L141, L148).
8. **Tooltips explain consequence, not identity** — verified across all 30+ `title` attributes in the file.
9. **Colour never carries meaning alone.** Warning strips lead with `▲`; triangles are shape-coded up/down;
   badges carry words (`LOW DATA`, `POST-SEAM`, `CLEAR`). CVD mode additionally dashes the EMA overlays
   (fast `2.5 3.5`, slow `9 4`, width 1.6) and the MACD signal line (`4 3`), because both hues would otherwise
   collide (L498–500, L791).
10. **Gaps are gaps.** Null series values are skipped by every point builder — never zero, never interpolated
    (L495, L776).
11. **Seams are drawn, not blended** (§4.5), and the current month renders as a hollow point (§4.6).
12. **All axis labels are HTML overlays**, positioned in percentages against the SVG viewBox; the left gutter
    anchor is `left: 6.74%` and every y-label is `translate(-100%, -50%)` (L242, L310, L313).
13. **Pane y-ticks yield to reference lines** — any tick within 11 % of an hline is dropped (L581).
14. **Panes share the price chart's x axis and hover index exactly** (L577, L578).
15. **Reference lines are semantic per pane:** `0` for signed measures, `100` for ratio-vs-index measures,
    `±1.5` for z, `80/20` for RSI, `1.0` for seasonality, `$0` for arb EV.
16. **Display-only overlays never arm the watchlist** — `NONTRACK = sma, boll, dtl, seas` (L714). This exactly
    matches the "not chip-eligible" set in `DISPLAY_VOCABULARY.md`:36/52.
17. **The enabled indicator set *is* the tracked set.** There is no separate pin control; the nav button is the
    save (L713–730, matching `DESIGN_NOTES.md`:112).
18. **A saved view captures six things** — tiers, inds, paneOrder, range, cmp, norm — and **not** indicator
    parameters and **not** custom from/to (L366). Applying one resets from/to to null (L362).
19. **Minimum window is 4 points** (L465).
20. **Range presets and custom dates are mutually exclusive**: picking a preset clears the dates; typing a date
    deactivates every preset (L695, L460).
21. **A rebuild must not label these values "closes."** `price_months.price_cents` is PriceCharting's monthly
    *average* — `DECISIONS.md`:181 (D-006) makes correct labelling a requirement. `axisMode` gets this right
    ("USD · monthly avg", L737); the table tooltip does not (L203).
22. **All progress ratios and unlock dates in this file are wrong** per D-032/D-033 and must be recomputed
    against the 2026-09-01 floor, with authored denominators and computed numerators. Affected strings:
    L417–422 (`suff`), L607, L625–628, L634 (`lockedOr` notes), L741–742 (W/D unlock tooltips).

---

## 7. Open questions

1. **Are locked rows meant to exist on this screen at all?** The `locked()` factory, the `isLocked` template
   branch, the progress bar, and `show anyway →` are complete and unreachable (§4.1). Three readings: (a) the
   HTML is the final answer and these six rows are simply `LOW DATA` toggles; (b) `lockedOr` was a temporary
   escape so the prototype could demo the panes, and the intended v1 is the locked form; (c) both forms exist
   and a runtime condition (per-card sufficiency) picks between them — which is what the factory name
   `lockedOr` suggests. **Needs an owner ruling before this screen is built.** D-038 ("locked rows render with
   real countdowns") argues for (b) or (c).
2. **If locked rows do ship, what does `show anyway →` do exactly?** `force()` sets `forced[id]` and enables the
   indicator (L403–409), and the badge text `LOW CONFIDENCE · BURNED IN` and its tooltip are authored — but
   nothing specifies whether `forced` persists across sessions, across cards, or is cleared by anything.
   `DECISIONS.md`:207 says the badge "does not clear until the data threshold is truly met"; the prototype
   never clears it, and stashing on a tier change does not clear it either.
3. **What sets the anchor tier?** `state.anchor` is read at L469 and never written. Is a user-selectable anchor
   intended, or is "the one visible tier" the whole rule and `state.anchor` vestigial?
4. **Zero visible tiers.** The chart empties but the stats strip, all seven readouts, and the left-panel price
   keep rendering PSA 10 numbers via the anchor fallback. Should those blank out, or is the fallback intended?
5. **Should the hollow "still revising" dot follow the current month or the window edge?** It currently sits at
   `i1` for every visible tier (L538), so a `to` date in the past marks a closed month as revising.
6. **Should the final segment be dashed?** `DESIGN_NOTES.md`:49 says yes ("same treatment to be applied in
   Charts"); the HTML draws every tier solid.
7. **Does `5Y` exist?** `rangeBtns` renders it (L695) but the `defaultRange` prop enum is `1Y | 3Y | All`
   (L329), so it cannot be a default. Intentional or an oversight?
8. **Route shape.** `/charts` with no card, or `/charts/{cardId?}` per the spec? The prototype has no card
   switcher of any kind, and every inbound "Open in Charts" link across the mockups is parameterless.
9. **Views persistence and naming.** Auto-names (`View 4`) can collide with existing names, and
   `deleteView`/`applyView` both key on `name` (L369, L708). Needs stable ids. Also: should applying a view
   restore indicator parameters?
10. **Is normalize meant to require compare?** Coupling them (L471) is defensible — rebasing a single series to
    100 tells you nothing you cannot read off the ROC tiles — but the toggle's own tooltip describes it as
    independent, and clicking it with compare off silently does nothing.
11. **Do the group chips bulk-toggle?** Their tooltips promise it; the handler only opens a sub-panel, and there
    is no separate caret element to click (§8).
12. **`W`/`D` unlock dates** (L741–742) predate D-033. Recompute against the 2026-09-01 floor, and decide
    whether "on liquid cards" makes the `D` unlock per-card rather than global.
13. **Table edge case:** `tblRows` iterates `i1−11 … i1` unguarded (L698). With the minimum 4-point window and
    an early `to` date, `i` goes negative and cells render `$NaN`. Same class of gap in `roc(k)` (L557) when
    `i1 < k`.
14. **Composite `g2` draws nothing** (L533) — is "Supply Flood Watch, currently CLEAR" meant to have zero
    historical triggers, or is the event list simply unwritten?
15. **Group chip labels use full tier names** while sub-chips use short ones (L676 vs L671), so selecting all
    seven tens produces a very long chip. Truncation rule needed.

---

## 8. Contradictions found

**HTML vs. derived documents.** Every "what the HTML does" cell was read directly from
`Cardstock Charts.dc.html` on 2026-08-10.

| # | Claim | Source doc:line | What the HTML actually does |
|---|---|---|---|
| 1 | Six rows (seasonality, Amihud, dispersion, discount-to-list, cross-market gap, supply overhang) have form **"locked"** — "disabled switch + unlock condition + progress ratio" | `DISPLAY_VOCABULARY.md`:136, :145, :157–:160, :164 | All six are built by `lockedOr()` (L596–598) = **working toggles** with a `LOW DATA` badge and a permanent amber note. The `locked()` factory at L595 is **never called**; `isLocked` (L145–162) never renders. Only **two** of the three declared forms are instantiated. |
| 2 | Locked rows show **progress bars** with ratios (`1/3`, `16/24 mo`, `3/8`, `12% rows`, `1/5 venues`, `7/12 mo`) | `DISPLAY_VOCABULARY.md`:145, :157–:160, :164; `DESIGN_NOTES.md`:33 | No progress bar renders. The ratios are passed to `lockedOr` as `prog`/`progTxt` and **discarded** (L607, L625–628, L634). The same numbers survive only as `suff()` badge-tooltip prose (L417–422). |
| 3 | *"Working `show anyway →` (burns `LOW CONFIDENCE · BURNED IN` badge, converts row to toggle with BURNED badge)"* | `DESIGN_NOTES.md`:33 | Unreachable. `force()` (L403) and `state.forced` (L349) exist and the badge string is computed (L576), but the only caller would be `r.force` inside the never-rendered `isLocked` branch — and `locked()` never sets a `force` key. **The override cannot be triggered.** |
| 4 | Seasonality is *"corpus-locked until ~**Nov 2028**"* | `DISPLAY_VOCABULARY.md`:36 | L607 says **Nov 2027** — agreeing with the same file's :145 and contradicting its own :36. (Self-contradiction already logged as D-032.) |
| 5 | Discount-to-list coverage is **4.4%** (143,062 of 3,265,910 sales) | `DISPLAY_VOCABULARY.md`:36; `DESIGN_NOTES.md`:46 | L627 and L420 both say **12%** — the figure D-031 calls stale. |
| 6 | Amihud `16/24 mo · ~Apr 2027`; overhang `7/12 mo`; dispersion `3/8`; gap `1/5 venues` | `DISPLAY_VOCABULARY.md`:157–:160, :164 | The HTML carries the **identical wrong ratios** (L418–422, L625–628, L634). D-032's finding therefore applies to Tier 1, not only to the doc: these must be recomputed against the D-033 floor before Charts ships. |
| 7 | *"Pane order is user-reorderable"* | `DISPLAY_VOCABULARY.md`:173 | No reorder control exists. Order is strictly enable-order (`paneOrder.push`, L446). ("Saved with a view" **is** true — L354–356, L362, L366.) |
| 8 | MACD's badge is *"re-tuned (3,6,4) for monthly bars"* | `DISPLAY_VOCABULARY.md`:142 | MACD has **no badge** (L604). That string is its hover **tooltip**. The doc's Badge column conflates the two. |
| 9 | Composites are `Pane? inline` | `DISPLAY_VOCABULARY.md`:167–:170 | Composites draw **neither** pane nor overlay — trigger triangles only (L537), and `g2` draws nothing at all (L533). |
| 10 | Tier spread badge is a *"live ratio + COMPRESSING"*; arb EV badge is a *"live EV"* | `DISPLAY_VOCABULARY.md`:165–:166 | Both are **authored literals** — `'4.8× · COMPRESSING'` and `'+$118'` (L637–638) — while the underlying series are computed at L400–401. |
| 11 | *"Showing a **second tier** switches every indicator off"* | `DISPLAY_VOCABULARY.md`:172 | The trigger is `visibleCount !== 1` (L659), which includes **zero** tiers. Deselecting the last tier also clears and stashes. |
| 12 | Indicators are *"disabled (inert, greyed)"* when ≠ 1 tier | `DESIGN_NOTES.md`:31 | Greyed only (track forced to the literal `#EDEDE9`, L591). No `disabled` attribute — the button stays focusable and clickable and the click is a **silent no-op** (L451). |
| 13 | **32** indicator rows | `HANDOFF.md`:73; `DESIGN_NOTES.md`:159 | **31** rows: 24 toggles + 7 readouts, across 7 groups. (`DISPLAY_VOCABULARY.md` §10's own table also lists 31, at :140–:170.) |
| 14 | *"All **29** indicators/signals present"* | `DESIGN_NOTES.md`:6 | 31 rows / 24 toggles / 17 pane-capable. |
| 15 | *"'+ Add to watchlist' toggles to **'Watching ✓'**"* | `DESIGN_NOTES.md`:32 | The clean-state label is **`✓ On watchlist`** (L722). |
| 16 | Clean state reads **`✓ On watchlist · N tracked`** | `DESIGN_NOTES.md`:112 | No count is rendered — the label is exactly `✓ On watchlist` (L722). The count appears only in the tooltip, and as names rather than a number (L727). |
| 17 | Current month = *"final chart segment **dashed** + hollow end dot… Same treatment to be applied in Charts"* | `DESIGN_NOTES.md`:49 | Hollow end dot **yes** (L236–238, L538); dashed final segment **no** — every tier polyline is `dash: 'none'` (L496). |
| 18 | Liquidity seam Apr '25 appears in *"churn/vol panes"* (two panes) | `DESIGN_NOTES.md`:35 | Drawn in **six** panes: churn, vol, amihud, disp, dtl, e4 (L829, 839, 870, 878, 886, 894). Also confirms the Apr '25 seam is real in Tier 1 — the open half of **D-009** is now only *why*, not *whether*. |
| 19 | *"The UI must not call these 'monthly closes'"* | `DECISIONS.md`:181 (D-006) | The table button's tooltip says **"Show the underlying monthly closes as a table"** (L203). `axisMode` gets it right ("USD · monthly avg", L737). A Tier-1 violation of an owner-accepted labelling rule. |
| 20 | Route is `/charts/{cardId?}`; top bar has a *"card search swap"*; *"per-pane 'view as table' toggle"* | `uploads/CARDSTOCK_UI_SPEC_v1.md`:114, :184 | No card id, no card switcher (card hard-coded at L72–79), and **one global** table toggle for the price series only (L203, L262–274) — no per-pane table. |
| 21 | Left panel has *"tier selector chips for the **six** tiers"*; range selector is *"1Y/3Y/All"* | `uploads/CARDSTOCK_UI_SPEC_v1.md`:183–:184 | **19** tier chips in two rows plus two expandable groups (L688–694) — matching the 19-value ruling at `DESIGN_NOTES.md`:77. Range is **1Y/3Y/5Y/All** (L695). |
| 22 | Indicator groups are *"Trend / Momentum / Mean-reversion / Liquidity / Supply / Valuation"* (6) | `uploads/CARDSTOCK_UI_SPEC_v1.md`:183 | **7** groups: Trend · Momentum · mean reversion · Relative · Liquidity · Supply · Valuation · Composites — momentum and mean-reversion are one group, and Relative and Composites are additions (L600–646). |

**Contradictions internal to the HTML** (no document involved — the prototype disagrees with itself):

| # | Where | Copy says | Code does |
|---|---|---|---|
| A | Group chip tooltips, L85 / L101 | *"Show or hide every grader's 10 at once — **click the caret** to pick individual graders"* / *"Show or hide grades 1–6 at once — click the caret…"* | `mkGroup.toggle` (L680) **only** opens/closes the sub-panel. There is no bulk toggle and **no separate caret element** — the chevron is part of the chip's own label string (L676). |
| B | Normalize tooltip, L177 | *"Rebase every visible series to 100 at the start of the range"* — describes an independent control | `norm = state.norm && cmp` (L471), and the switch renders from the **effective** value (L747). With compare off, clicking it changes state but produces no visible change at all. |
| C | Left panel price line, L78 | 1M change is a signed value | The span is hard-styled `color: var(--pos2)` — a negative change renders **green**. |
| D | Table header, L264 | *"Last 12 months"* | Last 12 months **of the visible window** ending at `i1` (L698), not of today; unguarded, so an early `to` date yields negative indices and `$NaN` cells. |
| E | Pane subtitles, L826 / L836 | *"PSA 10 bucket"* / *"PSA 10"* | Hard-coded, while the pane's anchor may be any of the 19 tiers. |
| F | Stat tiles and readouts, L568–569, L606, L618–619, L624, L633 | Presented identically to computed values | `Trend R² 12M`, `RS pct 3M`, `Trend slope (12M)`, `RS percentile (3M)`, `Beta vs index (24M)`, `Churn acceleration`, `Gem rate` are **authored literals**. Only ROC, drawdown and z are computed. |
