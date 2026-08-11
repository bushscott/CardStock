# Charts — screen specification

> **Source of truth:** `CardStock Mockup/Cardstock Charts.dc.html` (929 lines), read in full 2026-08-10.
> Every line citation below is `Charts:NNN` = that file. Markdown docs are Tier 2/3 and lose to this file wherever they disagree (see §8).
> Seeded arrays (`this.S`, `this.IDX`, `this.CHURN`, …) are illustrative fixtures. What is specified here is **structure and state space**, not the sample numbers.

## 1. Identity

| | |
|---|---|
| Screen label | `Charts` — `data-screen-label="Charts"` (Charts:33) |
| Prototype | `CardStock Mockup/Cardstock Charts.dc.html` |
| Route | `/charts` — asserted by `HANDOFF.md:73` (Tier 2). The prototype has no router; nav marks Charts active with `href="#"` (Charts:40). |
| Deep link | `#signals` (Charts:340). On mount, if `location.hash === '#signals'`: force `leftOpen: true` and `panelGlow: true`, then clear the glow after **2600 ms** (Charts:341–342); timer cleared on unmount (Charts:345). Glow = `box-shadow: inset 0 0 0 2px #4A63D0` on the left aside with a 0.7 s transition (Charts:70, 731). |
| Inbound deep link | `Cardstock Home.dc.html:257` — watchlist peek "edit →" → `Cardstock Charts.dc.html#signals`. Only `#signals` producer in the mockup set. |
| Other inbound | Plain nav links from Home (:47, :296, :589), Card (:45, :69, :117), Set:45, Screener:41, Browse:45, Binder:47, Character:45, Profile:34, Legal:33, About Data:37. |
| Component props | `defaultRange` — enum `["1Y","3Y","All"]`, default `"3Y"`; `compareIndex` — boolean, default `false` (Charts:329). |

**Purpose.** Single-card price workbench: chart 1–19 grade tiers of one card, overlay/enable 24 indicators, open up to 2 indicator panes, compare against a market index, normalize, save/apply named views, and export the underlying monthly closes to a table.

**Second purpose — it is the watchlist tracked-signal editor.** The nav watch button saves *the currently enabled indicators* as the card's tracked chip set (Charts:713–729). There is no separate pin control. Display-only overlays are excluded (`NONTRACK = ['sma','boll','dtl','seas']`, Charts:714).

**Subject card.** Hardcoded fixture: "Umbreon VMAX (Alt Art)", Evolving Skies · 215/203, art slot id `art-umbreon` (Charts:73–77). The prototype carries no card-id parameter — see §7.

---

## 2. Layout

**Fixed app frame — the page itself never scrolls vertically.**

Root (Charts:33): `height: 100vh; min-width: 1080px; overflow-x: auto; overflow-y: hidden; display: flex; flex-direction: column; font-size: 15px`. Below 1080 px the whole frame scrolls **horizontally**; it never scrolls vertically.

| Region | Lines | Sizing | Scroll |
|---|---|---|---|
| Nav bar | 35–65 | `height: 48px`, `position: sticky; top: 0; z-index: 20`, `gap: 24px`, `padding: 0 20px` | none (fixed) |
| Body row | 67 | `flex: 1; display: flex; align-items: stretch; min-height: 0` | none |
| Left aside "Indicators" | 69–185 | `width: 272px; flex-shrink: 0`, `padding: 14px 14px 20px`, `border-right: 1px solid var(--line)` | **`overflow-y: auto` — scrolls independently** |
| Main column | 187–325 | `flex: 1; min-width: 0`, `padding: 14px 18px 24px` | **`overflow-y: auto` — scrolls independently** |

The left aside is wrapped in `sc-if value="{{ leftOpen }}"` (Charts:69, 185) — when collapsed the element is **removed**, not hidden, and main reflows to full width.

**Nav contents, left→right** (Charts:36–64): logo + wordmark → Home · nav links Home / Screener / **Charts** (active: weight 600, 2 px accent bottom-border) / Binder / Browse · flexible spacer · `<cardstock-search>` (`flex: 0 1 280px; min-width: 110px`) · watch button · Views dropdown (menu `position: absolute; right: 0; top: 34px; z-index: 60; min-width: 230px`) · account avatar 28 px circle → Profile.

**Main column stack, top→bottom:**
1. Toolbar row (188–204) — `flex-wrap: wrap`, so it reflows rather than overflowing.
2. Stats strip card (206–213) — `flex-wrap: wrap`, `gap: 14px`.
3. Price chart card (215–260) — `position: relative` (anchor for the HTML label overlays).
4. Table card (262–274) — only when `tableOpen`.
5. Zero, one, or two pane cards (276–323).
6. Static footnote (324): *"Up to 2 indicator panes at a time — enabling a third replaces the oldest. Triangles mark rule triggers on the price series; hover for the rule and forward returns."*

**Chart geometry.** Main SVG `viewBox="0 0 920 456"`, `width: 100%`; plot inset L=64, R=14, T=14, B=24 → `X(i) = 64 + ((i−i0)/(i1−i0)) × 842`, `Y(v) = 14 + (1 − (v−lo)/(hi−lo)) × 418` (Charts:466–467, 494). Pane SVGs `viewBox="0 0 920 170"`, plot band y 14→156, same L/R gutters and the **same `X`** — panes share the price chart's x-scale exactly (Charts:573, 775).

**Axis labels are HTML overlay divs, not SVG text.** Y labels: `position: absolute; left: 6.74%; top: {t}%; transform: translate(-100%,-50%)` (Charts:242, 310, 313). X labels: `left: {l}%; top: 100%; transform: translate(-50%,-100%)` (Charts:248, 319). `6.74%` = the 64 px left gutter as a fraction of 920. Each `sc-for` inside an SVG wraps exactly **one** element (a `<title>` child is permitted — Charts:234, 237, 291). `DESIGN_NOTES.md:34` records why: the DC runtime drops all but the first element in an SVG `sc-for`, and SVG `<text>` in a loop does not render at all. **Any Blazor port is free of this constraint but must reproduce the resulting label positions.**

---

## 3. Data contract

Every bound field, by region. `i0`/`i1` are the inclusive month indices of the visible range; `N = 60` months, index 0 = **Aug 2021**, index 59 = **Jul 2026** (Charts:374, 431).

### 3.1 Nav

| Field | Type | Source | Notes |
|---|---|---|---|
| `watchLabel` | string | 721–722 | 5 values — see §4.11 |
| `watchBg` / `watchFg` / `watchBd` | color | 723 | |
| `watchTip` | string | 724–727 | 4 variants, one per non-flash state; interpolates the tracked-signal name list |
| `addWatch` | action | 728 | |
| `viewsLabel` | string | 703 | `"Views ▾"` or `"View: {activeView} ▾"` |
| `viewsOpen` | bool | 702 | |
| `viewRows[]` | list | 707–712 | `{ name, def (isDefault), active, check ("✓"\|""), apply, del }` |
| `toggleViews`, `closeViews`, `saveCurrent` | actions | 704–706 | |

### 3.2 Left panel — card header (Charts:71–80)

`curPrice` = `money(anchorSeries[i1])` (`$` + rounded, `en-US` grouping — Charts:432); `cur1m` = `fmtPct(roc(1))` = signed, 1 dp, U+2212 minus for negatives (Charts:558). Static: card name → Card page, set name → Set page, collector number, art `image-slot#art-umbreon` in a 96×133 box that scales to 2.2× on hover (`transform-origin: left top; z-index: 40`).

> `cur1m`'s color is **hardcoded** `var(--pos2)` in the template (Charts:78) while the value can be negative. Bug — see §7.

### 3.3 Left panel — tier chips

19 tiers (`state.tiers`, Charts:347) — the canonical grade vocabulary (`GradeTierVocabulary.cs`, 19 values). Default: **`PSA 10` on, all 18 others off.**

| Control | Members | Binding |
|---|---|---|
| Row 1 chip | `PSA 10` | `tierRow1` (688) |
| Row 1 group chip | `other 10s` → CGC 10, CGC 10 Prist., TAG 10, ACE 10, SGC 10, BGS 10, BGS 10 Black | `tensGroupChip` (690), `TENS` (669) |
| Row 2 chips | Grade 9.5, Grade 9, Grade 8, Grade 7 | `tierRow2` (689) |
| Row 2 group chip | `Grade 1–6` → Grade 6…Grade 1 | `lowsGroupChip` (691), `LOWS` (670) |
| Row 2 chip | `Raw` | `rawChip` (692) |
| Sub-chips | `tenSubChips` (7), `lowSubChips` (6) | 693–694, revealed by `tensOpenV` / `lowsOpenV` (733) |

Chip shape (`mkTier`, 652–668): `{ name, tip, dot, bg, bd, fg, toggle }`. `tip` = `"Hide "` / `"Show "` + name + `" on the chart — the y-axis rescales to whatever is visible"`. On: dot/border = `TIER_COLORS[t]`, bg `--card`, fg `--ink`. Off: dot `--line2`, border `--line`, fg `--mut2`.

Group chip (`mkGroup`, 672–682): `name` = active members joined `", "` — or the fallback label when none are active — plus `" ▾"` when expanded, `" ▸"` when collapsed. Dot/border take the **first active member's** color.

Sub-chip labels are shortened: tens drop `" 10"` and expand `Prist.` → `Pristine`; lows drop `"Grade "` leaving the bare number (671, 693–694). Sub-panel headings are static: *"Other graders' 10s"*, *"Grades 1–6"* (Charts:89, 106).

`TIER_COLORS` — 19 fixed hues (Charts:375). `PSA 10` = `PAL.acc`, `Grade 9` = `PAL.warn`, `Raw` = `PAL.mut2`; the other 16 are literals. These are **identity colors, unchanged by CVD mode** (`DESIGN_NOTES.md:104`).

`tierNotice` (734–736) — one of four strings, rendered in a `min-height: 31px` box so the panel does not jump (Charts:115):
- any indicator on → `"Indicators analyze {anchor} — adding another tier switches indicators off."`
- else >1 tier visible → `"Indicators disabled while multiple tiers are shown — keep one tier to enable."`
- else 0 tiers visible → `"No tier selected — pick one to chart."`
- else `""`

### 3.4 Left panel — indicator groups

`groups[]` (Charts:600–651): `{ name, note, open, chev ("▾"|"▸"), toggle, rows[] }`. Seven groups; `note` is a right-aligned qualifier — only **Liquidity** (`post-seam`), **Supply** (`2026+`) and **Composites** (`multi-signal`) have one. Collapse state lives in `state.cg[groupName]`; default all open (648–650).

**Row totals: 31 rows — 24 indicator toggles + 7 readouts. 17 of the 24 are pane-capable.**

#### The three row forms

| Form | Discriminator | Renders (Charts:) | Fields |
|---|---|---|---|
| **Toggle** | `isToggle` | 122–138 | 26×15 switch (`swBg`, knob `translateX(swX)` 0→11 px, 0.12 s), name with dotted-underline help cursor, right-aligned badge chip, optional amber warn strip, optional parameter row |
| **Readout** | `isReadout` | 139–144 | No control. Indented `padding-left: 34px` to align under the toggle labels. Name (dotted underline, `--mut`) + right-aligned mono value in `valFg` |
| **Locked** | `isLocked` | 145–162 | Indented 34 px, `opacity: 0.62`. Name + `LOCKED` chip, `note` line, optional 3 px progress bar (`progPct` width, `--warn` fill) with `progTxt`, optional `show anyway →` link calling `r.force` |

> **The Locked form is declared but never produced.** The factory `locked(name, note, tip, prog, progTxt, anyway)` exists at Charts:595 and has **zero call sites**. Every row that the docs call "locked" is built by `lockedOr` (596–598), which returns a **toggle** row carrying a `LOW DATA` badge and an always-on amber warn strip, with `pane: true`. `lockedOr` accepts `prog` and `progTxt` and **discards both**. Consequently at runtime this screen renders **no `LOCKED` chip, no progress bar, and no "show anyway →" link.** See §8.

Toggle row fields (`row`, 586–593): `{ isToggle, name, tip, toggle, swBg, swX, badge, badgeFg, badgeBg, hasWarn, warnTxt, showParams, params[] }`.
- `blocked = !on && visTiers.length !== 1` → switch track forced to `#EDEDE9` and `tip` replaced by **"Show a single tier to enable indicators."** (589–592).
- `showParams = on && row has params` — steppers appear only while the indicator is enabled.
- Param `{ label, val, set, min, max }` (`np`, 599) → `<input type="number">`, 42×22 px, `min`/`max` attributes bound (Charts:134).
- `hasWarn` = row is on **and** `suff(id)` returns non-null (410–429). Warn strip is amber, prefixed `▲`, indented 34 px (Charts:129).

#### Complete row inventory

Legend — **Form**: `T` toggle · `R` readout · `T*` toggle produced by `lockedOr` (`LOW DATA` badge + permanent amber note). **Pane**: ✓ = `pane: true`, opens a pane and participates in the 2-pane cap.

**Group: Trend** (note: none)

| # | id | Name | Form | Pane | Params (default, min–max) | Badge | Tooltip / permanent note |
|---|---|---|---|---|---|---|---|
| 1 | `ema` | EMA cross | T | — | `fast` = 3 (2–12), `slow` = 9 (3–24) | — | "Monthly data is fully sufficient — unaffected by the per-sale seam." (602) |
| 2 | `sma` | SMA baseline | T | — | `len` = 9 (3–24) | — | same sufficiency text (603) |
| 3 | `macd` | MACD | T | ✓ | `f` = 3 (2–12), `s` = 6 (3–24), `sig` = 4 (2–12) | — | "Monthly data is fully sufficient — re-tuned (3,6,4) for monthly bars." (604) |
| 4 | — | ROC 1M | R | — | — | value = `fmtPct(roc(1))`, pos2/neg2 | "Monthly data is fully sufficient." (605) |
| 5 | — | Trend slope (12M) | R | — | — | value = **`"+2.1%/mo"` hardcoded**, pos2 | (606) |
| 6 | `seas` | Seasonality overlay | T* | ✓ | — | `LOW DATA` | note: *"unlocks after 3 observed cycles · Nov 2027"*; tip: "Needs 3 full annual cycles at any resolution — per-sale data does not speed this up." Discarded ratio `0.33 / "1/3 cycles"` (607) |

**Group: Momentum · mean reversion** (note: none)

| # | id | Name | Form | Pane | Params | Badge | Tooltip / note |
|---|---|---|---|---|---|---|---|
| 7 | `rsi` | RSI | T | ✓ | `len` = 6 (3–12) | `SLOW ON MONTHLY` (mut2 on mutbg) | "Works on monthly; your per-sale ledger will later enable weekly RSI for faster exhaustion reads." (610) |
| 8 | `boll` | Bollinger bands | T | — | `k` = 6 (4–12), `m` = 2 (1–3) | — | "Bands built on monthly averages understate true range — per-sale data will tighten them." (611) |
| 9 | `z` | z-score vs 6M MA | T | ✓ | — | — | "Monthly data is fully sufficient." (612) |
| 10 | — | Drawdown from peak | R | — | — | `fmtPct(dd)`, neg2 when `dd < −0.001` else mut | (613) |

**Group: Relative** (note: none)

| # | id | Name | Form | Pane | Params | Badge | Tooltip / note |
|---|---|---|---|---|---|---|---|
| 11 | `rs` | RS vs market index | T | ✓ | — | — | "Monthly indices are fully sufficient." (616) |
| 12 | `f3` | Set rotation (Evolving Skies) | T | ✓ | — | `CORPUS` | "Corpus-level monthly indices — unaffected by the per-sale seam." (617). **Set name is hardcoded into the row label.** |
| 13 | — | RS percentile (3M) | R | — | — | **`"94th"` hardcoded**, pos2 | (618) |
| 14 | — | Beta vs index (24M) | R | — | — | **`"1.31"` hardcoded**, `--ink` | "Monthly data is fully sufficient once 24 paired months exist." (619) |

**Group: Liquidity** (note: `post-seam`)

| # | id | Name | Form | Pane | Params | Badge | Tooltip / permanent note |
|---|---|---|---|---|---|---|---|
| 15 | `churn` | Churn 30d | T | ✓ | — | `POST-SEAM` (warnInk on `rgba(176,127,26,0.12)`) | "Computed from your per-sale ledger — exists only after this card's seam and sharpens every month it runs." (622) |
| 16 | `vol` | Volume & count | T | ✓ | — | `POST-SEAM` | "Computed from your per-sale ledger — exists only post-seam; more months = better baselines." (623) |
| 17 | — | Churn acceleration | R | — | — | **`"×1.6 vs 90d"` hardcoded**, pos2 | "Per-sale ledger — needs a stable 90d baseline; improves fast post-seam." (624) |
| 18 | `amihud` | Amihud illiquidity | T* | ✓ | — | `LOW DATA` | note: *"needs 24 post-seam months · ~Apr 2027"*; tip: "Pure per-sale signal — impossible on monthly history; your scraper is what unlocks it." Discarded ratio `16/24 / "16/24 mo"` (625) |
| 19 | `disp` | Price dispersion | T* | ✓ | — | `LOW DATA` | note: *"needs ≥8 sales/mo in bucket"*; tip: "Pure per-sale signal — measures spread across individual sales in a month." Discarded ratio `3/8 / "3/8 sales"` (626) |
| 20 | `dtl` | Discount-to-list | T* | ✓ | — | `LOW DATA` | note: *"listed price on 12% of rows"*; tip: "Unlocks as your scraper captures listed prices alongside realized prices." Discarded ratio `0.12 / "12% rows"` (627) |
| 21 | `e4` | Cross-marketplace gap | T* | ✓ | — | `LOW DATA` | note: *"needs ≥5 sales/venue/window — eBay-only depth today"*; tip: "Unlocks as scraped sales accumulate per venue (goldin, heritage, pwcc…)." Discarded ratio `0.2 / "1/5 venues"` (628) |

**Group: Supply** (note: `2026+`)

| # | id | Name | Form | Pane | Params | Badge | Tooltip / permanent note |
|---|---|---|---|---|---|---|---|
| 22 | `popd` | Pop Δ monthly | T | ✓ | — | `NEW · 7 OBS` (`--acc` on `--accBg`) | "Census snapshots, 2026+ … Newly unlocked — on probation until ~12 observations." (631) |
| 23 | `d2` | Pop vs price divergence | T | ✓ | — | `NOVEL · 2026+` (warn) | "Pairs monthly price with census growth — sharpens as census history accumulates post-2026." (632) |
| 24 | — | Gem rate | R | — | — | **`"46% · drift −0.8pp"` hardcoded**, `--ink` | "Census level readable today; drift needs months of census history." (633) |
| 25 | `overhang` | Supply overhang | T* | ✓ | — | `LOW DATA` | note: *"needs 12M of census history"*; tip: "Pop ÷ annual sales — needs both census history and a year of per-sale ledger." Discarded ratio `7/12 / "7/12 mo"` (634) |

**Group: Valuation** (note: none)

| # | id | Name | Form | Pane | Params | Badge | Tooltip |
|---|---|---|---|---|---|---|---|
| 26 | `spread` | Tier spread 10/raw | T | ✓ | — | **`"4.8× · COMPRESSING"` — a hardcoded string, not derived from the `SPREAD` series** | "Six-tier monthly prices are sufficient; per-sale data adds real-time spread." (637) |
| 27 | `arbev` | Grading-arb EV raw→10 | T | ✓ | — | **`"+$118"` — hardcoded**, `PAL.pos` on `posBg(0.10)` | "gem rate × PSA 10 + (1−gem) × Grade 9 − raw − fees. Monthly tier prices are sufficient." (638) |

**Group: Composites** (note: `multi-signal`) — **none of the four opens a pane**; each contributes trigger triangles to the price chart.

| # | id | Name | Form | Pane | Badge | Tooltip | Triangles (`COMP`, 531–537) |
|---|---|---|---|---|---|---|---|
| 28 | `g1` | Quiet Accumulation | T | — | `ACTIVE · JUN '26` (pos) | "Churn leg needs the per-sale ledger — this composite only fires post-seam and strengthens with ledger depth." (641) | 1 up-marker at i=58 (Jun '26) |
| 29 | `g2` | Supply Flood Watch | T | — | `CLEAR` (mut2) | "Census + spread legs — 2026+ only; low confidence until census history builds." (642) | **none** — `COMP.g2 = []` |
| 30 | `g3` | Breakout Confirmation | T | — | `LAST · AUG '25` (mut2) | "Price leg is monthly; the volume-confirmation leg uses your per-sale ledger post-seam." (643) | 1 up-marker at i=48 (Aug '25) |
| 31 | `g4` | 3M RS Leaders | T | — | `MEMBER · MAR '26` (pos) | "Monthly indices are fully sufficient." (644) | 1 up-marker at i=55 (Mar '26) |

#### Data-sufficiency warn strips (`suff(id)`, Charts:410–429)

Rendered as the amber `▲` strip **only while the row is enabled**, except where `lockedOr` overrides it with a permanent note (rows 6, 18–21, 25). The same string becomes a pane's `LOW CONFIDENCE` badge tooltip (Charts:574–576).

| id | Text | Condition |
|---|---|---|
| `rsi` | "slow on monthly data — can stay overbought through whole runs" | always when on |
| `churn`, `vol` | "only 16 post-seam months — medium confidence" | always when on |
| `popd` | "only 7 census observations — low confidence" | always when on |
| `d2` | "only 7 paired price+census months — low confidence" | always when on |
| `seas` | "only 1/3 cycles observed — forced early" | (pane badge only) |
| `amihud` | "16/24 post-seam months — forced early" | (pane badge only) |
| `disp` | "3/8 sales per month — forced early" | (pane badge only) |
| `dtl` | "12% listed-price coverage — forced early" | (pane badge only) |
| `e4` | "1/5 venues with depth — forced early" | (pane badge only) |
| `overhang` | "7/12 census months — forced early" | (pane badge only) |
| `g1` | "churn leg has 16 post-seam months — medium confidence" | always when on |
| `g2` | "census leg has 7 observations — low confidence" | always when on |
| `macd` | "warmup consumes {macdF+macdS} of 60 months" | **only when `macdS > 8`** — parameter-aware |
| `ema` | "slow window {emaSlow}M leaves few independent observations" | **only when `emaSlow > 12`** — parameter-aware |

### 3.5 Left panel — Compare section (Charts:169–180)

Static heading "Compare", outside `groups[]`, therefore **not collapsible and not part of any group**.

| Field | Source | Notes |
|---|---|---|
| `cmpToggle` / `cmpBg` / `cmpX` | 746 | Label "Market index"; static legend `┄ grey`; tip: "Draw the whole-market index alongside this card, so you can see whether a move is the card or the market" |
| `normToggle` / `normBg` / `normX` | 747 | Label "Normalize (start = 100)"; tip: "Rebase every visible series to 100 at the start of the range — compares percentage moves instead of dollar prices" |

Panel footer (Charts:181–183): static link **"Why no candlesticks? → About our data"** → `Cardstock About Data.dc.html`.

### 3.6 Toolbar (Charts:188–204)

| Field | Source | Contract |
|---|---|---|
| `togglePanel` / `panelGlyph` | 700 | `«` when open, `»` when closed |
| `axisMode` | 737 | `("indexed · start = 100" \| "USD · monthly avg") + " · " + (i1−i0+1) + " pts"` |
| `resBtns[3]` | 739–743 | `{ label, tip, bg, fg, bd, cur, click }` — see §5.6 |
| `fromVal` / `toVal` | 738 | `i2d(i0)` / `i2d(i1)` → `YYYY-MM-01` (462) |
| `setFrom` / `setTo` | 744–745 | Empty string → `null` (clears the custom bound) |
| date input attrs | 198–199 | `min="2021-08-01"`, `max="2026-07-31"` — the fixture's exact 60-month window |
| `rangeBtns[4]` | 695–696 | `1Y`, `3Y`, `5Y`, `All` |
| `tableToggle` / `tblBg` / `tblFg` | 748 | Active = `accBg` background, `acc` text |

Static toolbar text: title **"Price"**.

### 3.7 Stats strip (Charts:206–213, 563–570)

Six cells, `{ label, val, fg }`:

| Label | Value | Color rule |
|---|---|---|
| `ROC 3M` | `fmtPct(aS[i1]/aS[i1−3] − 1)` | pos2 / neg2 |
| `ROC 12M` | `fmtPct(… i1−12 …)` | pos2 / neg2 |
| `Drawdown` | `fmtPct(aS[i1]/peak − 1)`, `peak = max(aS[0..i1])` | neg2 when `< −0.001`, else mut |
| `z vs 6M` | `(aS[i1] − sma6[i1]) / sd6[i1]`, signed, 2 dp | warn when `abs > 1.5`, else mut |
| `Trend R² 12M` | **`"0.87"` hardcoded** | mut |
| `RS pct 3M` | **`"94th"` hardcoded** | pos2 |

### 3.8 Price chart (Charts:215–260)

| Field | Source | Contract |
|---|---|---|
| `chartTitle` | 750 | `"Price history · "` + anchor name (1 tier) \| `"no tier selected"` (0) \| `"{n} tiers"` (>1) |
| `chartSub` | 751 | `"{mLabel(i0)} – {mLabel(i1)}"` |
| `yTicks[5]` | 539 | `{ y, ty, t (% of 456), label }`. Label = `money(v)` normally, `v.toFixed(0)` when normalized. Even spacing lo→hi |
| `xTicks[]` | 540–541 | Span > 14 months: one tick per **January** (`m === 0 && i > i0`), label = year. Span ≤ 14: replaced by month labels every 1 month (span ≤ 6) or every 3 |
| `lines[]` | 496–502 | One polyline per visible tier: `{ pts, color: TIER_COLORS[t], w: 1.8 for anchor / 1.3 others, dash: "none" }`. Then EMA fast (`pos2`) and slow (`neg2`), then SMA (`warn`, dash `5 4`) |
| `bollPts` | 503–509 | Single closed polygon (upper band forward + lower band reversed), fill `rgba(74,99,208,0.06)`. `""` when off |
| `idxPts` | 510 | Market index polyline, `--mut2`, width 1.2, dash `4 4`. `""` when compare off |
| `tris[]` | 511–537 | `{ pts, fill, tip }` — see §5.9 |
| `hollows[]` | 538 | One per visible tier at `i1`: `{ x, y, color }`, r = 3.5, fill `--card`, stroke tier color, static `<title>` **"current month still revising"** |
| `rsX` / `rsL` / `rsOn` | 753–755 | Seam marker — see §3.11 |
| `hoverX`, `hasHover`, `hoverMonth`, `hoverRows[]` | 756–757 | See §3.9 |
| `chartMove` / `chartLeave` | 758–765 | |

### 3.9 Hover model

`chartMove` (758–764) maps pointer x → `i = round(i0 + (px − 64)/842 × (i1 − i0))`, clamped to `[i0, i1]`, and stores `hoverI`. Both the main SVG **and every pane SVG** bind the same handlers (Charts:220, 288), so the crosshair is **shared across all stacked charts** — hovering a pane moves the price chart crosshair and vice versa.

Tooltip (Charts:250–257) is positioned **statically** at `top: 3.1%; left: 72px` — it does not follow the cursor. Rows (`hoverRows`, 544–555), in order:
1. One per visible tier: `{ name: tier, dot: TIER_COLORS[t], val: money(S[t][hoverI]) }` — **raw dollars even when normalized** (see §7).
2. `MKT INDEX` — `IDX[hoverI].toFixed(1)`, dot `--mut2` — only when compare is on.
3. `EMA {fast}` (pos2) and `EMA {slow}` (neg2) — when `ema` is on.
4. `SMA {len}` (warn) — when `sma` is on and the value is non-null.
5. `BOLL ±{m}σ` (acc) — value is a `"$lo–$hi"` range string.

### 3.10 Table (Charts:262–274, 698)

Heading **"Last 12 months · visible tiers"**. Grid `90px repeat({tblColCount}, 1fr)`.
- `tblColCount` = number of visible tiers.
- `tblHead[]` = `{ name, color }` per visible tier.
- `tblRows[]` = `{ m: mLabel(i), cells: [{ v: money(S[t][i]) }] }` for `i = i1−11 … i1` — **always 12 rows anchored to the range end, ignoring `i0`** (Charts:698). Computed only when `tableOpen`.

### 3.11 Seams

Two distinct, hardcoded seam constants (Charts:388–389):

| Constant | Value | Month | Where it renders |
|---|---|---|---|
| `SEAM` | 44 | **Apr '25** | Per-sale-derived series start here (`CHURN`, `VOLC`, `AMIHUD`, `DISP`, `DTL`, `E4G` are `null` before it — 390–397). Drawn as a dashed `--warn` vertical in the churn / volume / Amihud / dispersion / discount-to-list / cross-market panes, labelled **"seam · Apr '25"** (829, 839, 870, 878, 886, 894) |
| `RSEAM` | 59 | **Jul '26** | Dashed `--warn` vertical on the **price chart** (Charts:227) with the HTML label **"per-sale ledger begins · Jul '26 →"** (245), right-aligned to the line. Rendered only when `RSEAM` falls inside `[i0, i1]`; otherwise `rsX = −20` (off-canvas) and `rsOn = false` |

A third annotation: the Pop Δ pane draws a **restatement region** — a `rgba(176,127,26,0.10)` rect spanning ±1 bar width around i=55, `<title>` = *"restatement window — grader republished Mar 2026 census"* (Charts:920–922).

**Both seam dates are fixtures that contradict the verified data model** — see §8.

### 3.12 Panes (Charts:276–323, 571–584)

Envelope per pane: `{ title, sub, badge, badgeTip, hoverTxt, close, hx, regions[], yts[], hlines[], bars[], lines[], vlines[], xt }`.

- `badge` (576): `"LOW CONFIDENCE · BURNED IN"` when `state.forced[id]`, else `"LOW CONFIDENCE"` when `suff(id)` is non-null, else `""`. `badgeTip` = the `suff` text.
- `hoverTxt` (579): `"{mLabel(hoverI)} · {pane.vf(hoverI)}"`, blank when not hovering.
- `hx` = crosshair x, or −20 when not hovering.
- `xt` = **the main chart's `xTicks`** (577) — panes never compute their own x labels.
- `yts` are auto-ticks at the 0.88 and 0.12 fractions of the padded range (777); any auto-tick within **11 %** of an explicit `hlines` entry is dropped (581).
- `close` tooltip: "Remove this indicator pane — the indicator stays available in the panel".

| id | Title | Sub | Marks | Y format | Hover (`vf`) | Data window |
|---|---|---|---|---|---|---|
| `macd` | MACD | `({f},{s},{sig}) · {anchor}` | histogram bars (pos/neg @0.4α), MACD line (acc), signal (warn; dashed `4 3` in CVD), zero line | `$n` | `MACD x · sig y · hist z` | full |
| `rsi` | RSI | `({len}) · bands 80/20 · {anchor}` | RSI line (acc), dashed 80 and 20 guides | fixed 0–100 scale | `RSI n` / `warming up` | from index `len` |
| `z` | z-score vs 6M MA | `stretched beyond ±1.5 · {anchor}` | z line (acc), guides at +1.5 / 0 / −1.5 | auto | `z ±n.nn` / `warming up` | from index 5 |
| `rs` | RS vs market index | `ratio × 100 · above 100 = outperforming` | RS line (acc), dashed 100 guide | integer | `RS n` | full |
| `f3` | Set rotation · Evolving Skies | `set index vs market × 100 · above 100 = money rotating in` | ratio line (acc), dashed 100 guide | integer | `ratio n` | full |
| `churn` | Churn 30d | `sales/day · PSA 10 bucket · post-seam` | line (acc), seam vline | `n.nn/d` | `n.nn sales/day` / `pre-seam` | ≥ SEAM |
| `vol` | Volume & count | `observed sales/mo · PSA 10 · post-seam` | bars `rgba(74,99,208,0.45)` grounded at y=156, seam vline | integer | `n sales` / `pre-seam` | ≥ SEAM |
| `popd` | Pop Δ monthly | `PSA+CGC census growth % · 2026+` | bars `rgba(176,127,26,0.5)`, zero line, **restatement region** | `n.n%` | `+n.n%` / `pre-census` | i ≥ 53 (7 obs) |
| `d2` | Pop vs price divergence | `price ROC 1M − pop growth · red = supply flooding · 2026+` | signed bars (pos/neg @0.5α), zero line | `n.npp` | `±n.npp` / `pre-census` | i ≥ 54 (**6 bars**) |
| `seas` | Seasonality | `seasonal factor · 1 observed cycle · illustrative` | line (acc), dashed 1.0 guide | 2 dp | `factor n.nnn` | full |
| `amihud` | Amihud illiquidity | `price impact per $1k volume · post-seam` | line (acc), seam vline | 2 dp | `impact n.nn%/$1k` / `pre-seam` | ≥ SEAM |
| `disp` | Price dispersion | `σ/μ of realized prices in bucket · post-seam` | line (acc), seam vline | 2 dp | `σ/μ n.nn` / `pre-seam` | ≥ SEAM |
| `dtl` | Discount-to-list | `avg % below listed price · rows with list price only` | line (acc), seam vline | `n.n%` | `n.n% below list` / `pre-seam` | ≥ SEAM |
| `e4` | Cross-marketplace gap | `auction-house premium vs eBay · post-seam` | line (acc), seam vline | `n.n%` | `auction +n.n% vs eBay` / `pre-seam` | ≥ SEAM |
| `overhang` | Supply overhang | `pop ÷ annualized sales · 2026+` | bars `rgba(176,127,26,0.5)` grounded at y=156 | `n.ny` | `n.n yrs of supply` / `pre-census` | i ≥ 53 |
| `spread` | Tier spread | `PSA 10 ÷ raw · falling = compressing` | line (**warn**) | `n.n×` | `n.nn×` | full |
| `arbev` | Grading-arb EV | `gem×PSA10 + (1−gem)×G9 − raw − fees · above 0 = grade it` | line (**pos2**), dashed `$0` guide | `$n` / `−$n` | `+$n EV` / `−$n EV` | full |

---

## 4. States

### 4.1 Left panel visibility
`leftOpen` (default `true`, Charts:352). `true` → aside rendered, glyph `«`. `false` → aside **unmounted**, glyph `»`, main takes full width. Triggers: toolbar toggle button (Charts:189, 700); `#signals` deep link forces `true`.

### 4.2 Deep-link attention glow
`panelGlow` — `false` normally. Trigger: `location.hash === '#signals'` at mount. Effect: `panelShadow = "inset 0 0 0 2px #4A63D0"` on the aside (731), 0.7 s transition in and out; auto-clears after 2600 ms. No other trigger sets it.

### 4.3 Tier visibility — three regimes

| `visTiers.length` | `tierNotice` | `chartTitle` | Indicators |
|---|---|---|---|
| 0 | "No tier selected — pick one to chart." | "Price history · no tier selected" | Cannot be enabled. Chart draws no tier lines; y-scale collapses to `lo=0, hi=1` (492) |
| 1 | `""` (or the "analyze" notice if any indicator is on) | "Price history · {tier}" | Enabled |
| >1 | "Indicators disabled while multiple tiers are shown — keep one tier to enable." | "Price history · {n} tiers" | Blocked; all switches greyed |

**Anchor (the analysis tier).** `anchor` = `state.anchor` if still visible → else `PSA 10` if visible → else `visTiers[0]` → else the literal `'PSA 10'` (Charts:469). **`state.anchor` is never written by any handler** — the first branch is dead code and there is no UI to pick an anchor. Consequence: with zero tiers charted, all readouts, stats and the price header still compute against `PSA 10`.

`visTiers` order is the key order of `state.tiers` (347), **not** the chip display order: BGS 10 Black → BGS 10 → SGC 10 → ACE 10 → TAG 10 → CGC 10 Prist. → CGC 10 → PSA 10 → Grade 9.5 → 9 → 8 → 7 → 6 → 5 → 4 → 3 → 2 → 1 → Raw. This order governs line draw order, tooltip row order and table column order.

### 4.4 Indicator row states

| State | Condition | Rendering |
|---|---|---|
| Off, available | `!on`, exactly 1 tier visible | Switch grey (`--line2`), knob at 0, its own tooltip |
| Off, **blocked** | `!on`, tier count ≠ 1 | Switch track forced to `#EDEDE9`, tooltip replaced with **"Show a single tier to enable indicators."** (589–592). Clicking does nothing (449–452) |
| On | `inds[id] === true` | Switch `--acc`, knob `translateX(11px)`; parameter row appears if the row has params; warn strip appears if `suff(id)` is non-null |
| Permanent-note (`lockedOr` rows) | rows 6, 18–21, 25 | Always show the amber note strip regardless of on/off, plus a `LOW DATA` badge |

### 4.5 Pane stack
`paneOrder` (default `['macd']`, Charts:350) — 0, 1 or 2 entries. Rendered set = `paneOrder.filter(id => inds[id])` (572). Enabling a third pane indicator pushes it, **shifts the oldest out, and sets that indicator's toggle to `false`** (446). Non-pane indicators (`ema`, `sma`, `boll`, `g1`–`g4`) never touch `paneOrder`.

### 4.6 Pane confidence badge — three states (Charts:574–576)
1. **No badge** — `suff(id)` returns null and the pane was never forced.
2. **`LOW DATA`** — `suff(id)` non-null; tooltip = the sufficiency text, stating the floor rule and what improves it. **Changed 2026-08-10 by D-056**: the prototype emits plain `LOW CONFIDENCE` here (Charts:576), which was `LOW DATA` under a second name. Two amber badges meaning "the data is thin" is vocabulary drift, and `DISPLAY_VOCABULARY.md:55` names five states as "the complete render set" with no `LOW CONFIDENCE` among them. **Build `LOW DATA`**, carrying its `N OBS` count where available.
3. **`LOW CONFIDENCE · BURNED IN`** — `state.forced[id]` is true. **Persistent**: `forced` is never cleared by any code path — not by closing the pane, not by toggling the indicator off and on, not by applying a view. It survives every interaction in the screen's lifetime, so it is present in any screenshot of that pane.

The link that sets it (Charts:159) carries: *"Force-enables this pane now and burns a permanent LOW CONFIDENCE badge into it — the badge does not clear until the data threshold is truly met."*

### 4.7 Locked-row state — declared, currently unreachable
The `isLocked` branch (Charts:145–162) supports: `LOCKED` chip, `note`, an optional progress bar (`hasProg`, `progPct` = `(prog×100).toFixed(0)+"%"`, `progTxt`), and an optional `show anyway →` link bound to `r.force`. The factory is at Charts:595 and `force` at Charts:403–408. **Neither is invoked** — every candidate row goes through `lockedOr` instead (596–598, used at 607, 625–628, 634). So today no `LOCKED` chip, no progress ratio and no override link render, and `state.forced` can never become non-empty through the UI.

### 4.8 Range state

| State | Condition | Effect |
|---|---|---|
| Preset | `!custom` (`from` and `to` both null) | `i1 = 59`; `i0` = `i1−11` (1Y) / `i1−35` (3Y) / `max(0, i1−59)` (5Y) / `0` (All). Matching range button is highlighted (695) |
| Custom | `from` or `to` set | `i1 = d2i(to)` or 59; `i0 = d2i(from)` or **0**. No range button is highlighted. Setting only `to` therefore silently expands the start to all history (464) |
| Degenerate → clamped | `i1 ≤ i0 + 2` | Forced to a 4-point window: `i0 = max(0, i1−3)`, `i1 = min(59, i0+3)` (465) |

`d2i` clamps any date into `[0, 59]` (461). Date inputs are bounded `2021-08-01 … 2026-07-31` (198–199).

### 4.9 Axis mode
`norm = state.norm && cmp` (471). Two states: **USD** (`"USD · monthly avg · {n} pts"`, y labels `money()`, index rebased to the anchor's price at `i0`) and **indexed** (`"indexed · start = 100 · {n} pts"`, every series divided by its own `i0` value ×100, y labels bare integers).

### 4.10 Hover
`hoverI` null → no crosshair (`hoverX = −20`), no tooltip, panes show empty `hoverTxt`. Non-null **and inside `[i0, i1]`** → `hasHover` true. Trigger: `onMouseMove` on the price SVG or any pane SVG; cleared by `onMouseLeave` on either.

### 4.11 Watch button — five states (Charts:713–729)
Tracked set = enabled indicators minus `NONTRACK` (`sma`, `boll`, `dtl`, `seas`), sorted. `dirty` = watching and the tracked set differs from `wlSaved`. Seed: `watch: true`, `wlSaved: { rs, macd }`, and `inds.macd`/`inds.rs` both true → opens **clean**.

| # | Condition | Label | Style |
|---|---|---|---|
| 1 | `!watch` | `+ Add to watchlist` | accent fill |
| 2 | `watch` and tracked set is **empty** | `Remove from watchlist` | **red** (`neg2`) fill |
| 3 | `watch` and `dirty` | `Update watchlist` | accent fill |
| 4 | `watch`, clean, `wlFlash` | `✓ Watchlist updated` | card bg, `pos2` text — auto-clears after **2200 ms** |
| 5 | `watch`, clean | `✓ On watchlist` | card bg, muted text |

State 2 wins over state 3 (an empty tracked set is a delist, not an update). Each state has its own tooltip (724–727); the dirty and clean tooltips interpolate the uppercased tracked-signal id list.

### 4.12 Views
`viewsOpen` (menu), `activeView` (null or a name), `savedViews[]` (3 seeded: **Trend workspace** `isDefault: true`, **Liquidity check**, **vs Market** — Charts:353–357). Applying sets `activeView` and the label becomes `View: {name} ▾`; the applied row shows `✓`. `isDefault` renders a `DEFAULT` chip and is **never recomputed** — saving or deleting cannot move it.

### 4.13 Group and sub-panel collapse
`state.cg[groupName]` — absent/false = open (`▾`), true = collapsed (`▸`), rows unmounted (Charts:119). `tensOpen` / `lowsOpen` — sub-chip panels, default closed; a group chip's caret label reflects it.

### 4.14 Theme and colour-vision mode
`PAL` is resolved **once, at component construction**, from `localStorage['cardstock-theme']` and `localStorage['cardstock-cvd']` (Charts:331–338); a pre-paint helmet script mirrors them onto `data-theme` / `data-cvd` (Charts:31). Four palettes: light, light-CVD, dark, dark-CVD. CVD also changes **line grammar**, not just hue: EMA overlays go solid w1 → dashed w1.6 (`2.5 3.5` fast, `9 4` slow) (498–500), and the MACD signal line goes solid → `4 3` (791). `TIER_COLORS` are identity colours and do not change under CVD.

### 4.15 Table
`tableOpen` (default false). Toggle button turns `accBg`/`acc` when open. `tblRows` are only computed while open (698).

### 4.16 Stash
`state.stash` — `null`/undefined normally; `{ inds, paneOrder }` while the tier count is ≠ 1 and indicators had been on. Cleared on restore (664) and on applying a view (362).

---

## 5. Interactions

### 5.1 Nav
- **Logo / Home / Screener / Binder / Browse** — plain navigation. **Charts** is `href="#"` (self, active).
- **Search** — `<cardstock-search>` custom element (`cardstock-search.js`), `flex: 0 1 280px; min-width: 110px`.
- **Watch button** — single click does one of: delist (`watch: false, wlSaved: {}`) when the tracked set is empty; otherwise save the current tracked set (`watch: true`, `wlSaved` = enabled ∖ NONTRACK) and flash for 2200 ms (728).
- **Avatar** — → Profile.

### 5.2 Views dropdown
- **Views button** — toggles the menu. Tooltip: "Saved views — a view remembers the grade tiers, indicators, resolution, and date range you have set. Applying one changes which signals are tracked."
- **Menu row click** → `applyView(v)` (359–363): overwrites **all 19 tier flags** (anything not in `cfg.tiers` becomes false), **all 24 indicator flags**, `paneOrder`, `range`, `cmp`, `norm`; clears `from`, `to` and `stash`; sets `activeView`; closes the menu.
- **✕ on a row** → `deleteView` (369–371), `stopPropagation` so it does not also apply. Clears `activeView` if that view was active. No confirmation, and the `DEFAULT` view is deletable.
- **"+ Save current as new view"** → `saveCurrentView` (364–368): snapshots tiers, inds, paneOrder, `range ?? '3Y'`, cmp, norm under the auto-name `"View " + (savedViews.length + 1)`. Names are **not** deduped after deletions.
- **Mouse leave the menu** → closes (Charts:50).

### 5.3 Tier chips
Click toggles that tier and runs the one-tier reconciliation (655–667) — see §6.2. Sub-chips do the same **and close their sub-panel** (683–687). Group chips do **not** toggle membership; they only expand/collapse the sub-panel (680) — despite tooltips that say "Show or hide every grader's 10 at once" / "Show or hide grades 1–6 at once" (Charts:85, 101). See §8.

### 5.4 Indicator switch
`toggleInd(id, isPane)` (440–455). Turning **on** is refused outright (early `return`, no state change) unless exactly one tier is visible. Turning **off** is always permitted. When `isPane`, on → push to `paneOrder` with FIFO eviction at >2; off → filter out of `paneOrder`.

### 5.5 Parameter steppers
`<input type="number">` with bound `min`/`max`. `setP` (439) parses base-10 and **only commits integers > 0** — non-numeric or ≤ 0 input is silently ignored and the field keeps its typed value while state does not change. `min`/`max` are advisory HTML attributes; `setP` does **not** clamp to them. Changing a parameter immediately re-derives the overlay/pane, the trigger triangles, the hover rows, the pane subtitle, and — for `macd`/`ema` — the parameter-aware warn strip.

### 5.6 Resolution toggle (M / W / D)
Three buttons in a bordered segmented group (Charts:193–197, 739–743).

| Button | State | `cursor` | Tooltip | `click` |
|---|---|---|---|---|
| **M** | Active — `--acc` fill, card-coloured glyph | `pointer` | "Monthly bars — current resolution" | **`() => {}` no-op** |
| **W** | Locked — `--hov` fill, `--mut3` glyph | `not-allowed` | "Weekly bars — unlocks after ~6 months of per-sale ledger (~Jan 2027)" | no-op |
| **D** | Locked — same | `not-allowed` | "Daily bars — unlocks after ~12 months of per-sale ledger on liquid cards" | no-op |

There is no unlocked alternative to M; the control is a promise, not a switch. All three handlers are empty.

### 5.7 Date pickers and range presets
- `from` / `to` native date inputs, bounded to the data window. Clearing a field sets that bound to `null`.
- Range buttons `1Y` `3Y` `5Y` `All` set `range` and **clear both custom dates** (695). A button is highlighted only when `!custom && range === label`. Tooltips: "Show the last 1 year / 3 years / 5 years"; **All** → "Show all available history — begins where this card's data honestly starts".

### 5.8 Table toggle
Opens/closes the "Last 12 months · visible tiers" grid. Tooltip: "Show the underlying monthly closes as a table".

### 5.9 Trigger markers (triangles)
`tris[]` (511–537). Geometry relative to the anchor's plotted point `(x, yv)`:
- **Up** (`ev.up`): apex `(x, yv+6)`, base `(x±5, yv+15)` — sits **below** the line, fill `PAL.pos2`.
- **Down**: apex `(x, yv−6)`, base `(x±5, yv−15)` — sits **above** the line, fill `PAL.neg2`.
Any event with `ev.i < i0 + 1` is dropped (513). Each polygon carries an SVG `<title>` — a native browser tooltip, not a custom one (Charts:234).

Four producers:
1. **`ema` on** → every crossover of EMA(fast) − EMA(slow), scanned from `max(emaSlow+2, i0)` (520–524). Tip: `EMA {f}/{s} crossover ▲|▼ — {month} · {price} · +3M {fwd} · +6M {fwd}`.
2. **`macd` on** → every crossover of MACD − signal, scanned from `max(macdF+macdS+1, i0)` (525–530). Tip: `MACD (f,s,sig) crossed above|below signal — {month} · {price} · +3M · +6M`.
3. **Composites `g1`, `g3`, `g4`** → fixed single events with authored tips (531–537). `g2` contributes none.
4. Forward returns come from `fwd(a, i, k)` (438) — `null` (rendered `"n/a"`) when `i + k` runs past the data.

`crosses` (437) fires on **both** directions and treats a touch of zero as a cross (`<= 0 && > 0`, `>= 0 && < 0`).

### 5.10 Compare and normalize
- **Market index** toggle → draws `IDX` as a grey dashed polyline. When not normalized the index is rebased to the anchor's price at `i0` so it shares the dollar axis (`idxScale`, 475); when normalized both go to 100. Compare also adds a `MKT INDEX` tooltip row and extends the y-scan (479).
- **Normalize** toggle → `state.norm`, but the effective flag is `norm = state.norm && cmp` (471). **With compare off, toggling Normalize changes nothing and the switch does not even move**, because `normBg`/`normX` are bound to the derived `norm`, not to `state.norm` (747). Turning compare on afterwards makes the previously invisible normalize state take effect.

### 5.11 Pane close
`✕` → `closePane(id)` (780): sets `inds[id] = false` and removes it from `paneOrder`. Equivalent to switching the row off. `state.forced[id]` is **not** cleared.

### 5.12 Chart hover
See §3.9 and §4.10. Every SVG on the screen shares one `hoverI`.

### 5.13 Static links
Card name → Card · set name → Set · "Why no candlesticks? → About our data" → About Data.

---

## 6. Rules and invariants

1. **One analysis tier.** Every indicator, overlay, readout, stat, trigger marker and pane computes against a single `anchor` series. `anchor` is `PSA 10` when visible, otherwise the first visible tier in canonical order, otherwise the literal `PSA 10` (Charts:469). It is never user-selectable in this prototype.
2. **The one-tier rule (both directions).**
   - *Enable side:* `toggleInd` refuses to turn any indicator **on** unless exactly one tier is visible (449–452); the switch is visually inert and its tooltip is replaced (589–592).
   - *Tier side:* toggling tiers to a count ≠ 1 **while any indicator is on** stashes `{inds, paneOrder}`, sets every indicator false and empties `paneOrder` (659–662).
   - *Restore:* returning to exactly one visible tier with a stash present restores the whole set and clears the stash (663–665). The restored indicators then analyze whatever the new single tier is.
   - Going multi-tier with nothing enabled does not overwrite an existing stash, so 1→2→3→1 tiers still restores correctly.
   - `applyView` discards the stash unconditionally (362).
3. **Pane cap = 2, FIFO.** Enabling a third pane indicator evicts the oldest *and turns its toggle off* (446). Non-pane indicators are uncapped. Restated to the user in the footnote (Charts:324).
4. **Minimum window = 4 points** (465).
5. **Presets are anchored to the end of the data, not to "today"** — `i1` is always 59 for a preset (463–464).
6. **Normalize requires compare.** `norm = state.norm && cmp` (471). Normalize alone is inert and does not even render as on.
7. **Under normalize, every series is rebased to its own value at `i0`** (472) — including each tier independently, so tiers converge at the left edge.
8. **The y-axis rescales to everything drawn**: all visible tiers, the compare index, EMA/SMA overlays, and Bollinger bands, then ±7 % padding (476–493). Tier chip tooltips state this explicitly.
9. **Every visible tier gets a hollow end point at `i1`** with the tooltip "current month still revising" (538, 236–238) — the month-to-date honesty marker.
10. **Drawdown is measured from the all-time peak `max(aS[0..i1])`, not the peak within the visible range** (561). This applies to both the stats strip and the "Drawdown from peak" readout.
11. **The table is always 12 rows ending at `i1`**, regardless of `i0` (698) — its heading claims exactly that ("Last 12 months").
12. **Panes are strictly slaved to the price chart's x-axis**: same `X()`, same `xTicks`, same crosshair index (573, 577, 578).
13. **Pane auto y-ticks yield to explicit guide lines** — any auto-tick within 11 % of an `hlines` entry is dropped (581).
14. **Tracked ≠ displayed.** The watchlist tracked set is the enabled indicators minus `NONTRACK = [sma, boll, dtl, seas]` (714) — display-only overlays never arm the button. Applying a view that changes indicators therefore *does* arm "Update watchlist".
15. **`forced` is write-once and never cleared** (406). Burning in a low-confidence badge is irreversible for the session.
16. **Monthly is the only resolution.** W and D are permanently inert; every unlock story is told in tooltips (741–742).
17. **Two seams, drawn not blended** — `SEAM` = Apr '25 gates the per-sale panes; `RSEAM` = Jul '26 is annotated on the price chart. Series are `null` before their seam and simply do not plot (`pts` skips nulls, 495, 776); nothing is interpolated across a seam.
18. **Axis labels are never SVG text** — they are HTML overlays anchored at `left: 6.74%` (the 64 px gutter) and `top: 100%` (Charts:242, 248).
19. **Badges in the indicator panel are strings, not derived values.** `4.8× · COMPRESSING`, `+$118`, `NEW · 7 OBS`, `ACTIVE · JUN '26`, `LAST · AUG '25`, `MEMBER · MAR '26` are all literals in the row definitions, even where the underlying series is computed (637–644).
20. **Every group row has a tooltip, and every tooltip is about data sufficiency** — a design invariant held across all 31 rows (602–644). Dotted underline + `cursor: help` marks it.

---

## Corrected values — build these

Written 2026-08-10 (D-061). Charts is the worst instance of the seam error, because here it is **not just copy — it is wired into rendering logic.**

### The hardcoded seams

`Charts:388–398` defines two constants:

| Constant | Value | Verdict |
|---|---|---|
| `SEAM` | Apr '25 — drives the liquidity panes | **FALSE.** Fifteen months before the collector's first commit (D-001). Remove |
| `RSEAM` | Jul '26 — the price chart's resolution marker | Roughly right by accident, but still a single shared date |

**Neither survives.** The seam is per-card and ragged — each card's sales history begins at its own first visit. A chart drawing one vertical line across all cards is drawing a boundary that does not exist.

**Build instead:** the seam marker is computed per card as `min(sold_on)` per `grade_tier` (`DATA_MODEL.md:449` names this derivation as already available), and the analysis floor is 2026-09-01 (D-033). A card visited in October has its marker in October.

### Sufficiency notes to replace

| Row | Prototype note | Build instead |
|---|---|---|
| `amihud` (`:170`) | "needs 24 post-seam months · ~Apr 2027" | "needs 24 post-seam months · **~Sept 2028**" — computed from the floor, not authored |
| `dtl` (`:172`) | "listed price on 12% of rows" | "listed price on **4.4%** of rows" (`DESIGN_NOTES.md:46`, D-031) |
| `e4` (`:173`) | "needs ≥5 sales/venue/window — eBay-only depth today" | Keep the requirement, **drop "eBay-only"** — five sources are documented (`DATA_MODEL.md:102`, `:227`); the observed distribution has never been queried |
| `popd` (`:179`) | `NEW · 7 OBS`, "Census snapshots, 2026+" | `NEW · **N** OBS` computed; census begins at each card's first visit |
| `overhang` (`:182`) | "needs 12M of census history" | Unchanged — it is a denominator, which is the correct form |
| `seasonality` (`:607`) | "Nov 2027" | `DISPLAY_VOCABULARY.md:36` says Nov **2028** for the same row. Neither is authored — compute from the floor |

### Pane badges

`:211` shows `"16/24 post-seam months — forced early"`. Per **D-056**, the badge vocabulary is now:

- **`LOW DATA`** — automatic, carrying `N OBS` where available. Replaces plain `LOW CONFIDENCE` at `:576`.
- **`LOW CONFIDENCE · BURNED IN`** — user override only. Currently unreachable (D-049); must be wired when the LOCKED row form is built.

The `16/24` ratio is authored and wrong — the true figure is nearer `1/24` (D-032). **Compute it.**

### The rule, stated once

**Author the denominator. Never author the ratio, the numerator, or the unlock date.** Every authored number in this file was wrong in the direction that overstates readiness, which is the one direction this product cannot afford. D-033 records the reasoning.

---

## 7. Open questions

1. **Card identity is not in the route.** The prototype hardcodes Umbreon VMAX (Alt Art) / Evolving Skies / 215/203 and the art slot id `art-umbreon` (Charts:73–77). `HANDOFF.md:73` gives the route as bare `/charts`. Undecided: `/charts/{cardId}`, `/charts?card=…`, or a card-less landing that requires a search. `DESIGN_NOTES.md:29` says the `art-<cardid>` slot ids are shared with the Home peek and watchlist, which implies a card id is available.
2. **Anchor selection.** `state.anchor` is read (Charts:469) and never written. Is an explicit "analyze this tier" control intended, or is "PSA 10 if visible, else first visible in canonical order" the final rule? Note the fallback returns `'PSA 10'` even when nothing is charted, so the readouts keep computing against an invisible series.
3. **Wiring the locked form.** `D-038` (`DECISIONS.md:237–238`) says v1 ships locked rows "with real countdowns and progress computed against the 2026-09-01 floor" — which requires calling the currently-dead `locked()` (Charts:595) and `force()` (403). Which of the six `lockedOr` rows become genuinely locked, and which stay as enabled `LOW DATA` toggles?
4. **`force()` toggles instead of forcing on.** It calls `toggleInd(id, true)()` (Charts:407), so if the indicator were already enabled, "show anyway" would switch it **off** while marking it burned-in. Must be fixed when the path is wired.
5. **Nine authored constants need real derivations**: Trend slope (12M) `+2.1%/mo`, RS percentile (3M) `94th`, Beta vs index (24M) `1.31`, Churn acceleration `×1.6 vs 90d`, Gem rate `46% · drift −0.8pp`, stat cells `Trend R² 12M 0.87` and `RS pct 3M 94th`, and the two Valuation badges `4.8× · COMPRESSING` / `+$118` (Charts:606, 618–619, 624, 633, 568–569, 637–638). All are literals today.
6. **What backs `IDX` and `SETIDX`?** The market index and the per-set index are fixtures (Charts:387, 393). `D-039` (`DECISIONS.md:438–452`) puts "index values" in the companion worker's write scope but no index definition exists. Blocks `rs`, `f3`, Beta, RS percentile and the compare overlay.
7. **Composite rule definitions are absent.** `g1`–`g4` render authored badges and fixed trigger indices with authored tips (Charts:531–537, 641–644). The actual entry/exit rules exist nowhere in the prototype.
8. **Hover tooltip under normalize** shows raw dollars (Charts:544) while the axis shows index points. Which is correct?
9. **Table scope.** The grid is always the last 12 months ending at `i1`, ignoring `i0` (Charts:698). Intended, or should it follow the visible range?
10. **Mixed measurement scopes.** Drawdown scans from index 0 (Charts:561) while ROC and z are range-relative. Intended?
11. **Group chip tooltips promise bulk toggling** that the handler does not implement (Charts:85/101 vs 680). Decide: implement the bulk toggle, or rewrite the copy.
12. **Persistence.** `savedViews`, `cg` (group collapse), `leftOpen`, `tableOpen` and `forced` are all in-memory. Which survive a reload, and in which CardStock-owned table do views live?
13. **Theme/CVD is resolved once at construction** (Charts:331). A Blazor port needs either a live subscription or an explicit "theme change reloads the chart" contract.
14. **Accessibility gaps.** Indicator switches are `<button aria-label>` with no `role="switch"` / `aria-pressed`; group headers are `role="button" tabindex="0"` with a click handler and no key handler (Charts:118, 124); locked W/D buttons are styled `not-allowed` but are not `disabled`/`aria-disabled` (Charts:741–742).
15. **Parameter bounds are advisory.** `setP` accepts any integer > 0 and ignores the `min`/`max` attributes (Charts:439). Should it clamp?
16. **`d2` off-by-one.** The pane plots from `i ≥ 54` — six bars (Charts:843) — while its sufficiency text claims "7 paired price+census months" (Charts:416).
17. **Is the 2-pane cap a product rule?** It is asserted to the user in the footnote (Charts:324) but is otherwise an arbitrary constant (Charts:446).

---

## 8. Contradictions found

Doc paths are relative to the repo root. `CardStock Mockup/Cardstock Charts.dc.html` is Tier 1 and wins on every row below — **except** where `DECISIONS.md` records an owner ruling, which overrides all tiers (rows 16–19), and there the HTML string is what must change.

| # | Claim | Source `doc:line` | What the HTML actually does |
|---|---|---|---|
| 1 | Rows come in three forms, one of which is **locked** = "disabled switch + unlock condition + progress ratio" | `CardStock Mockup/DISPLAY_VOCABULARY.md:136` | The `isLocked` template exists (Charts:145–162) and its factory `locked()` at Charts:595 has **zero call sites**. All six candidate rows go through `lockedOr` (596–598) which returns an ordinary **toggle** with a `LOW DATA` badge and a permanent amber note. **No `LOCKED` chip, no progress bar, no override link renders.** |
| 2 | Seasonality, Amihud, Price dispersion, Discount-to-list, Cross-marketplace gap, Supply overhang are `locked` with Pane? = `—` | `…/DISPLAY_VOCABULARY.md:145,157,158,159,160,164` | All six are `pane: true` (Charts:607, 625–628, 634) and have working pane renderers (`paneSeas`, `paneAmihud`, `paneDisp`, `paneDtl`, `paneE4`, `paneOvh` — Charts:571, 858–902). They are enabled toggles that open panes. |
| 3 | Locked rows show progress ratios `1/3 cycles`, `16/24 mo`, `3/8`, `1/5 venues`, `7/12 mo` | `…/DISPLAY_VOCABULARY.md:145,157,158,160,164` | Exactly those values are passed to `lockedOr` as arguments 5 and 6 (Charts:607, 625, 626, 628, 634) and **discarded** — `lockedOr`'s body ignores them. No progress ratio renders anywhere on this screen. |
| 4 | LOCKED rows have "progress bars ('16/24 mo') and **working** 'show anyway →' (burns LOW CONFIDENCE · BURNED IN badge, converts row to toggle with **BURNED** badge)" | `CardStock Mockup/DESIGN_NOTES.md:33` | Unreachable. `force()` (Charts:403–408) has no call site; the only reference to `r.force` is inside the never-emitted locked template (Charts:159). The burn-in machinery is real but orphaned: `state.forced` gates the pane badge string `LOW CONFIDENCE · BURNED IN` (Charts:575–576) and is **never cleared**, so if wired the badge is genuinely persistent. There is **no "BURNED" row badge** anywhere in the file. |
| 5 | "Charts: per-indicator track pin (◉ tracked blue / ○ untracked grey, right of each toggle row) … Locked rows have no pin" | `…/DESIGN_NOTES.md:117` | No pin markup exists. The row's right slot is the badge chip (Charts:126). Superseded by the same document at `:112` ("The ◉ pin column is REMOVED"), which the HTML matches — `:117` is stale text left in place. |
| 6 | Watch button clean state reads `"✓ On watchlist · N tracked"` | `…/DESIGN_NOTES.md:112` | Label is `"✓ On watchlist"` with **no count** (Charts:722). The tracked-signal names appear only in the tooltip (Charts:727). |
| 7 | `"+ Add to watchlist" toggles to "Watching ✓"` | `…/DESIGN_NOTES.md:32` | Five labels, none of which is "Watching ✓": `+ Add to watchlist` · `Remove from watchlist` · `Update watchlist` · `✓ Watchlist updated` · `✓ On watchlist` (Charts:722). Also superseded by `:112`. |
| 8 | Current-month presentation = "final chart segment **dashed** + hollow end dot … Same treatment to be applied in Charts" | `…/DESIGN_NOTES.md:49` | Only half is implemented. Hollow end dots exist for every visible tier with the tooltip "current month still revising" (Charts:236–238, 538). Tier polylines are drawn with `dash: 'none'` for their whole length (Charts:496) — **there is no dashed final segment.** |
| 9 | "pane order is **user-reorderable** and saved with a view" | `…/DISPLAY_VOCABULARY.md:173` | No reorder affordance exists. `paneOrder` is append-on-enable, filter-on-disable, shift-oldest at >2 (Charts:444–448, 780). It *is* saved with a view (Charts:366) — that half is correct. |
| 10 | MACD's badge is "re-tuned (3,6,4) for monthly bars" | `…/DISPLAY_VOCABULARY.md:142` | That string is the row **tooltip**; MACD passes no `badge`, so the chip slot is empty (Charts:604). |
| 11 | Relative row is "Set rotation (**per set**)" | `…/DISPLAY_VOCABULARY.md:151` | The set name is hardcoded into the label: `'Set rotation (Evolving Skies)'` (Charts:617) and the pane title `'Set rotation · Evolving Skies'` (Charts:853). |
| 12 | Valuation badges are "**live** ratio + `COMPRESSING`" and "**live** EV" | `…/DISPLAY_VOCABULARY.md:165,166` | Both are literal strings — `'4.8× · COMPRESSING'` and `'+$118'` (Charts:637–638) — even though `SPREAD` and `ARBEV` are computed series (Charts:400–401). Nothing recomputes the badges. |
| 13 | Charts has **32 indicator rows** | `CardStock Mockup/HANDOFF.md:73` | **31** group rows: 24 indicator toggles (`state.inds`, Charts:348) + 7 readouts (Charts:605–606, 613, 618–619, 624, 633). `DISPLAY_VOCABULARY.md`'s own §10 table also lists 31, so `HANDOFF:73` is the outlier. |
| 14 | "All **29** indicators/signals present" | `…/DESIGN_NOTES.md:6` | 24 indicator ids exist (Charts:348), 17 of them pane-capable (Charts:571). |
| 15 | §10 documents "all **32** rows with form/pane/params/badges" | `…/DESIGN_NOTES.md:159` | §10's table has 31 data rows, which matches the HTML. The self-description is off by one. |
| 16 | The per-sale ledger begins at **each card's own first visit, late Jul 2026 onward — never a shared date**; "Apr 2025" and "Jan 2026" were wrong | `…/HANDOFF.md:126,134`; `DECISIONS.md` D-001 | The HTML hardcodes **two shared seam constants**: `SEAM = 44` → **Apr '25**, gating churn/volume/Amihud/dispersion/discount-to-list/cross-market and rendered as "seam · Apr '25" in six panes (Charts:388, 390–397, 829); and `RSEAM = 59` → **Jul '26**, the price-chart marker "per-sale ledger begins · Jul '26 →" (Charts:389, 227, 245). Census series start at a third hardcoded index, 53 → Jan '26 (Charts:392, 398). `DESIGN_NOTES.md:35` documents both seams as intentional. **Per D-001 all three are fiction and must become per-card values.** |
| 17 | Unlock dates land **~Sept 2027** (12 months census) and **~Sept 2028** (24 months liquidity) under the 2026-09-01 floor | `DECISIONS.md:321` (D-033, owner ruling — overrides all tiers) | The HTML tells users **`~Apr 2027`** for Amihud (Charts:625) and **`Nov 2027`** for seasonality (Charts:607), and its sufficiency strings carry the same D-032 numerators (`16/24`, `7/12`, `3/8`, `1/5`, `1/3`, "only 7 census observations") in the pane badge tooltips and warn strips (Charts:410–429). These strings understate the wait by ~15 months and must be recalibrated before Charts ships (`DECISIONS.md:244`). |
| 18 | Discount-to-list coverage is **4.4 %**, not 12 % | `DECISIONS.md:359` (D-032/D-031); `…/DISPLAY_VOCABULARY.md:36` | The HTML carries the rejected figure twice: the row note "listed price on 12% of rows" (Charts:627) and the pane badge tooltip "12% listed-price coverage — forced early" (Charts:420). |
| 19 | Seasonality is corpus-locked until **~Nov 2028** | `…/DISPLAY_VOCABULARY.md:36` (vs its own `:145` "Nov 2027", flagged at `DECISIONS.md:360`) | The HTML says **Nov 2027** (Charts:607), agreeing with `:145` and disagreeing with `:36`. Neither is reconciled with D-033's floor. |
| 20 | Cross-marketplace gap assumes **eBay-only** depth (`1/5 venues`) while `DATA_MODEL.md:102,:227` document five sources | `DECISIONS.md:362` (D-032) | The HTML states "eBay-only depth today" in the row note and "1/5 venues with depth" in the badge tooltip (Charts:628, 421). Needs the query D-032 asks for. |
| 21 | *(internal to the HTML)* Group chip tooltips: "Show or hide **every grader's 10 at once** — click the caret to pick individual graders" / "Show or hide **grades 1–6 at once**" | Charts:85, 101 | The group chip's handler only flips `tensOpen` / `lowsOpen` — it expands the sub-panel and toggles **no** tiers (Charts:672–682). There is also no separate caret; the whole chip is the caret. |
| 22 | *(internal to the HTML)* `defaultRange` prop enum is `["1Y","3Y","All"]` | Charts:329 | The toolbar offers **four** presets including `5Y`, and `renderVals` handles `'5Y'` (Charts:695, 464). The prop enum cannot express the toolbar's own state space. |
| 23 | *(internal to the HTML)* `d2` warn text: "only **7** paired price+census months" | Charts:416 | The pane plots from `i ≥ 54` — **6** bars (Charts:843), because the divergence needs `aS[i−1]` and `POPD` starts at 53. |

| 24 | Not chip-eligible: "Bollinger …, **beta** (descriptive), discount-to-list …, seasonality" | `…/DISPLAY_VOCABULARY.md:36` | The HTML's exclusion list is `NONTRACK = ['sma','boll','dtl','seas']` (Charts:714) — **`sma`, not beta**. Beta is a readout with no toggle, so it could never enter the tracked set anyway; SMA can, and is excluded. `DESIGN_NOTES.md:112` states the list correctly. |

**Verified consistent** (checked, no contradiction): the one-tier block and stash/restore text (`DISPLAY_VOCABULARY.md:172`) matches Charts:589–592, 655–667; what a saved view captures (`:174`) matches Charts:366; the SVG `sc-for` / HTML-overlay-label rendering constraints (`DESIGN_NOTES.md:34`) match Charts:220–249; the toolbar inventory (`DESIGN_NOTES.md:32`) matches Charts:188–204 apart from the watch label; CVD line-grammar rules (`DESIGN_NOTES.md:104`) match Charts:498–500, 791; the nav account circle (`DESIGN_NOTES.md:107`) is present at Charts:64; search shrink sizing (`DESIGN_NOTES.md:124`) matches Charts:45; the 19-tier vocabulary (`DESIGN_NOTES.md:77`, `GradeTierVocabulary.cs`) matches Charts:347 exactly.
