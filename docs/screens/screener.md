# Screener — Screen Specification

> **Source of truth:** `CardStock Mockup/Cardstock Screener.dc.html` (881 lines), read in full 2026-08-10.
> Every line citation below is `Screener.dc.html:N` unless another file is named.
> Per `CLAUDE.md` "Document authority", this HTML is Tier 1. Where a markdown doc disagrees, the HTML wins — except where `DECISIONS.md` records an owner decision, which overrides all tiers (see §8).

## 1. Identity

**Name:** Screener. `data-screen-label="Screener"` (`:34`); nav tab "Screener" is the active tab (`:40`). DC prop section is `"Screener"` (`:417`).

**Purpose.** A standing question asked of the whole corpus, saved and re-runnable. The user composes an AND-set of filter conditions over card metrics, sees the matching cards as a dense table or an art grid, saves the composition as a named *screen* with a one-line *thesis*, and can flip the same screen into **backtest mode** to replay history and ask "would this question have made money?"

**Routes.** The prototype has **no routing at all** — screen selection, backtest mode, and view density are all component state (`state.screen`, `state.bt`, `state.view`, `:427`). Every route below is a *derived* claim from Tier 2/3 docs, not from the HTML:

| Route | Meaning | Source |
|---|---|---|
| `/screener` | Screener, default screen | `HANDOFF.md:72`, `uploads/CARDSTOCK_UI_SPEC_v1.md:112` |
| `/screener/{id}` | A specific saved screen | `HANDOFF.md:72` only — **absent** from `UI_SPEC_v1.md:112–113` |
| `/screener/{id}/backtest` | Backtest mode of a saved screen | `HANDOFF.md:72`, `uploads/CARDSTOCK_UI_SPEC_v1.md:113` |

Backtest is a **mode, not a page**: the rail, header, and filter chips stay mounted; only the results area swaps (`:202–347` vs `:349–406`). If `/screener/{id}/backtest` is implemented as a real route it must not remount the shell.

**Marketing collision.** `HANDOFF.md:84` assigns `/screener` to `Cardstock Screener Landing.dc.html` as well. Two different pages claim the same path. Unresolved — see §7.

## 2. Layout

Full-viewport, no page scroll: `height: 100vh; overflow: hidden; display: flex; flex-direction: column` (`:34`), base `font-size: 15px`.

```
┌─ nav 48px ─────────────────────────────────────────────────────────┐  :36–48
├──────────────┬─────────────────────────────────────────────────────┤
│ saved-screens│  header:  [«] Screen name   THESIS "…"              │  :79–95
│ rail 232px   │  filter row: [+ filter] chip chip chip … N matches  │  :96–181
│ :52–75       │  results band: [terminal|binder]  … [Save][Backtest→]│ :184–200
│              ├─────────────────────────────────────────────────────┤
│              │  results area — terminal | binder | backtest        │  :202–406
└──────────────┴─────────────────────────────────────────────────────┘
   floating card-art preview (fixed, z-index 100)                        :410–414
```

### 2.1 Global nav (`:36–48`)
48px tall, `--card` background, 1px bottom border, `z-index: 20`, `flex-shrink: 0`. Contents left→right: logo mark + "Cardstock" wordmark linking to Home (`:37`); tabs **Home · Screener · Charts · Binder · Browse** (`:39–43`) where Screener is `href="#"`, weight 600, ink colour, 2px accent bottom border; spacer; `<cardstock-search>` custom element (`:46`); circular 28px account avatar rendering the letter `O`, linking to Profile (`:47`).

### 2.2 Saved-screens rail (`:52–75`)
Wrapped in `sc-if value="{{ railOpen }}"` — hidden entirely when collapsed. `<aside aria-label="Saved screens">`, fixed **232px** wide, `--card` background, right border, own `overflow-y: auto`, padding `14px 10px`.

- Section heading **"Your screens"** — 12.5px, 600, uppercase, `letter-spacing .08em` (`:54`).
- `sc-for` over `myScreens` (`:55`), one row per id in `state.myIds` (`:427`, seeded `['g1','g2','g3','arb','my1','my2']`). Row is `position: relative`, `border-left: 2px solid {{s.bd}}`, `border-radius: 0 5px 5px 0`, background `{{s.bg}}` (`:56`).
- Row body button (`:57–60`): name (14px, weight 600 when active else 500, ellipsised) over sub-line (12px, muted).
- `⋯` actions button (`:61`), `aria-label="Screen actions"`, title "Rename, duplicate, or delete this screen".
- Per-row dropdown (`:62–70`), `position: absolute; right: 4px; top: 34px; z-index: 60`, 160px wide, `data-rail-menu="1"`, closes on `onMouseLeave`. Items: **Rename** (`:64`) · **Edit thesis** (`:65`) · **Duplicate** (`:66`) · 1px divider (`:67`) · **Delete** (`:68`, `--neg2` text, negative hover tint).
- **"+ New screen"** dashed-border full-width button pinned after the list, `margin-top: 14px` (`:73`).

There is **no** "preset vs mine" section split in the rail — the `preset` flag exists on every seeded screen (`:454–465`) but is never read by the renderer. Presets and user screens share one list and one menu (including **Delete**).

### 2.3 Header (`:79–95`)
`padding: 12px 18px 0`, `flex-shrink: 0`. Baseline-aligned row: rail toggle button (26×28, glyph `«`/`»`, `:81`), then screen name (17px/700 Inter Tight, `:86`) or its inline edit input (`:83`, 300px, accent underline, autofocus), then the thesis (`:92`: 11px uppercase label **THESIS** + italic quoted text) or its inline edit input (`:89`, 420px), then a flex spacer.

### 2.4 Filter row (`:96–181`)
`display: flex; gap: 6px; flex-wrap: wrap; margin-top: 8px`. Order is deliberate (`DESIGN_NOTES.md:10` — "+ filter FIRST so popover never drifts off-screen"):

1. **`+ filter`** button and its popover, inside `data-filter-pop="1"` positioning context (`:98–174`). Wrapped in `sc-if notBt` — the entire add-affordance disappears in backtest mode (`:97`, `:175`).
2. **Chips** (`sc-for chips`, `:176–178`) — accent-tinted, mono 12.5px, each with a `✕` remove button that is itself wrapped in `sc-if notBt` (`:177`).
3. Flex spacer, then right-aligned **`N matches`** mono label (`:180`).

**Popover** (`:101`): absolute, `left: 0; top: 31px; z-index: 50`, **300px wide, max-height 380px**, own scroll, 8px radius, drop shadow. Dismisses on `onMouseLeave` (`closeAdd`, which no-ops while an editor is open, `:862`) and on outside click via the document listener (`:685`).

### 2.5 Results band (`:184–200`)
`display: flex; align-items: center; gap: 6px; padding: 10px 18px 0`. Two mutually exclusive contents:

- **`notBt`** (`:185–193`): segmented `terminal | binder` control (single 1px border, `overflow: hidden`, 27px tall, mono 12.5px, `:186–189`); spacer; **Save** button (`:191`); **Backtest →** primary button (`:192`).
- **`isBt`** (`:194–199`): **← Results** button (`:195`); **BACKTEST MODE** badge (mono 11.5px, 600, `letter-spacing .08em`, accent on `--accBg` with a 25%-alpha accent border, `:196`); spacer; muted note **"filters are read-only while backtesting"** (`:198`).

### 2.6 Results area
Exactly one of three renders, gated by `isTerminal` / `isBinder` / `isBt` (`:864`, `:600`):

- **Terminal** (`:349–387`) — `overflow: auto` both axes; table card is `width: max-content; min-width: 100%`, so it scrolls horizontally as columns are widened. Sticky header row (`position: sticky; top: 0; z-index: 5`, `:352`) with a `box-shadow: 0 -12px 0 0 var(--bg)` trick to mask content scrolling under the rounded top. Below the table, attached seamlessly (`border-top: none; border-radius: 0 0 8px 8px`), the amber **hidden-rows** banner (`:380–385`).
- **Binder** (`:389–406`) — `overflow-y: auto`, `display: grid; grid-template-columns: repeat(auto-fill, minmax(150px, 1fr)); gap: 12px` (`:391`). Cards are `--card` panels with a `325/450` aspect-ratio art slot and a hover box-shadow lift (`:393–395`).
- **Backtest** (`:202–347`) — `overflow-y: auto`, vertical flex, `gap: 12px`; anatomy in §4.6.

### 2.7 Floating art preview (`:410–414`)
`position: fixed`, **164×226**, 8px radius, gradient background, `box-shadow: 0 14px 40px rgba(20,19,26,.35)`, `z-index: 100`, `pointer-events: none`. Rendered outside `<main>` at the root of the screen div, so it is never clipped by the results scroller.

## 3. Data contract

Seeded values are illustrative. What follows is the **structure** and the **complete state space** the renderer can express.

### 3.1 Screen (the saved-screen entity) — `SCREENS`, `:453–466`

| Field | Type | Rendered where | Notes |
|---|---|---|---|
| `id` | string key | not rendered | Rail order comes from `state.myIds` (`:427`), not from `SCREENS` key order |
| `name` | string | rail row (`:58`), header title (`:86`), backtest **Entry rule** (`:207`) | Editable inline; blank input is discarded (`:702`) |
| `sub` | string | rail sub-line (`:59`) | Free text. Presets: `"default · G1 composite"`; user screens: `"4 filters · edited 2d ago"`; new: `"0 filters · just now"` (`:853`). **Never recomputed** — the prototype does not update it when filters change |
| `thesis` | string | header, rendered wrapped in typographic quotes (`:845`) | Placeholder for new screens: `"click Edit thesis to say why this screen should work"` (`:853`) |
| `preset` | bool | **nothing** | Set on all 6 seeded screens but never read — see §6 |
| `chips` | string[] | chip row (`:177`) | Authored strings in the seed; the live add-path generates them (§3.6) |

Per-screen user overrides live in `state.edits[screenId]` and are merged over the base with `Object.assign` (`:694`, `:708`, `:715`). Added chips live in `state.added[screenId]` (`:835`); removed chips in `state.removed` keyed by **chip label**, and `removed` is reset on screen switch and on new-screen (`:711`, `:854`) but **not** on duplicate/delete.

### 3.2 Card row — terminal view (`CARDS :467–480`, projected `:744–767`)

| Field | Source | Rendering | Colour rule |
|---|---|---|---|
| art | `ACCENTS[id]` two-stop gradient (`:446–452`) + `image-slot id="art-{id}"` | 48×66, radius 4 (`:361–362`) | — |
| `name` | string | 14px/600, ellipsised (`:365`) | — |
| `set` · `tier` | strings | joined `"{set} · {tier}"`, 12.5px muted (`:366`) | — |
| `price` | number (dollars) | `money()` → `$` + rounded + `en-US` thousands (`:565`) | ink, mono 13.5px/700 |
| `roc3` | number (percent) | `fmtPct` → `+`/`−` + 1 dp + `%` (`:737`) | `≥0` → `--pos2`, else `--neg2` (`:760`) |
| `rs` | number | `{n}th` (`:761`) | always `--mut` |
| `z` | number | `+`/`−` + 2 dp (`:761`) | always `--mut` |
| `churn` | number | `×` + 1 dp (`:762`) | `≥1.4` → `--pos`, else `--mut` |
| `pop` | number | **always** `+` + 1 dp + `%` (`:763`) | `≤1` → `--mut`, else `--warnInk` |
| `comp` | enum `ACTIVE` \| `WATCH` \| `EXITED` | pill, mono 11.5px/600 (`:375`) | `CHIPSTATE` (`:732–736`): ACTIVE `--pos` on `posBg(.10)`; WATCH `--warnInk` on `rgba(176,127,26,.12)`; EXITED `--mut2` on `--mutbg` |
| `compSince` | string | pill `title` only | ACTIVE → `"In screen since {compSince}"`; WATCH/EXITED → `compSince` verbatim, which seeds as a *reason phrase* (`"churn ×1.4 not yet sustained"`, `"price drifting below band"`, `"pop reading is 1 observation old"`, `"pop Δ crossed +1% · Jul ’26"`) (`:765`) |

`pop` hard-codes the `+` sign, so a negative census delta would render as `+-1.2%`. All 12 seeded values are positive, so the prototype never exposes it (`:763`).

### 3.3 Card cell — binder view (`:838–842`, rendered `:392–403`)
Four fields only: art (`325/450` aspect), `name` (13.5px/600), `price` (mono 13.5px/700), `roc3` (mono 12px, pos/neg coloured). No set, tier, or metric columns. Uses the same `sorted` array as terminal, so it inherits the terminal sort but offers no sort control of its own.

### 3.4 Filter metric vocabulary — complete

**7 groups, 28 metrics.** `FILTER_MENU` (`:481–501`) defines groups and badges; `EDITORS` (`:534–563`) defines editor shape. The two lists are 1:1 — every menu item has an editor, and `openEditor` bails silently if it ever did not (`:679`).

Every item carries `ok: true` (`:482–500`) and the renderer hard-codes `fg: ink, cur: 'pointer'` (`:787`). **No metric is ever disabled in the menu** — the `ok` flag is dead.

Operators are the same three for every `range` metric — **`≥` · `≤` · `between`** (`:821`); `enum` and `multi` have none.

| # | Metric (menu + editor title) | Group | Badge | `short` | Shape | Windows (**default**) · label | Unit | Default op/value | Editor caution |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Price (tier) | Price & trend | — | `Price` | range | Any tier · BGS 10 Black · BGS 10 · SGC 10 · ACE 10 · TAG 10 · CGC 10 Prist. · CGC 10 · **PSA 10** · Grade 9.5 · Grade 9 · Grade 8 · Grade 7 · Grade 6 · Grade 5 · Grade 4 · Grade 3 · Grade 2 · Grade 1 · Raw *(20 options, label `Tier`)* | `$` prefix | `between` 50–2000 | — |
| 2 | ROC 1/3/6/12M | Price & trend | — | `ROC` | range | 1M · **3M** · 6M · 12M | `%` suffix, signed | `≥` 5 | — |
| 3 | EMA cross state | Price & trend | — | `EMA` | enum | — | — | `3/9 bullish` (opts[0]) | — |
| 4 | MACD state | Price & trend | — | `MACD` | enum | — | — | `Above signal` | — |
| 5 | Trend R² | Price & trend | — | `Trend R²` | range | 6M · **12M** | none | `≥` 0.6 | — |
| 6 | Drawdown from peak | Price & trend | — | `Drawdown` | range | 12M peak · **24M peak** · All-time *(label `Peak`)* | `%` suffix | `≥` 30 | — |
| 7 | RS vs index (percentile) | Momentum & reversion | — | `RS pct` | range | 1M · **3M** | `th` suffix | `≥` 90 | — |
| 8 | z-score vs 6M MA | Momentum & reversion | — | `z 6M` | range | — | `σ` suffix, signed | `≥` 1.5 | — |
| 9 | Bollinger %B / bandwidth | Momentum & reversion | — | `Boll` | range | **%B** · Bandwidth *(label `Measure`)* | none | `≥` 1 | — |
| 10 | RSI (6) | Momentum & reversion | — | `RSI 6` | range | — | none | **`≤`** 30 | — |
| 11 | Beta vs index | Momentum & reversion | `24M MIN` | `Beta` | range | — | none, signed | `≥` 1.2 | "Monthly data means ~24 usable observations at best — beta estimates carry wide error bars." (`:545`) |
| 12 | Churn 30/90d + acceleration | Liquidity | `POST-SEAM` | `Churn` | range | 30d · 90d · **accel** *(label `Measure`)* | `×` prefix | `≥` 1.4 | "Post-seam only — cards with under 60 post-seam days are hidden from results." (`:546`) |
| 13 | Monthly sales count | Liquidity | `POST-SEAM` | `Sales/mo` | range | — | none | `≥` 8 | "Post-seam only — counts before the per-sale ledger begins are estimates and excluded." (`:547`) |
| 14 | Amihud percentile | Liquidity | `LOW DATA` | `Amihud` | range | — | `th` suffix | **`≤`** 25 | "Needs ~24 post-seam months (Apr ’27) for stable baselines — readings today are noisy." (`:548`) ⚠ D-032 |
| 15 | Price dispersion | Liquidity | `LOW DATA` | `Dispersion` | range | — | `%` suffix | **`≤`** 12 | "Needs ≥8 sales/mo per bucket — most cards are below that today." (`:549`) |
| 16 | Discount-to-list | Liquidity | `LOW DATA` | `Disc-list` | range | — | `%` suffix | `≥` 8 | "Listed price captured on only ~12% of rows so far." (`:550`) ⚠ D-031 |
| 17 | Cross-marketplace gap | Liquidity | `LOW DATA` | `Mkt gap` | range | — | `%` suffix | `≥` 5 | "eBay-only depth today — needs ≥5 sales per venue per window." (`:551`) |
| 18 | Pop Δ 30/60/90d | Supply (2026+) | `7 OBS` | `Pop Δ` | range | 30d · **60d** · 90d | `%` suffix, signed | **`≤`** 1 | "Census history starts Jan ’26 — 7 observations so far." (`:552`) ⚠ D-001/D-032 |
| 19 | Gem rate + drift | Supply (2026+) | `7 OBS` | `Gem rate` | range | **Gem rate** · Drift 90d *(label `Measure`)* | `%` suffix | `≥` 40 | "Census history starts Jan ’26 — 7 observations so far." (`:553`) ⚠ D-001/D-032 |
| 20 | Supply overhang | Supply (2026+) | `LOW DATA` | `Overhang` | range | — | `" mo"` suffix (leading space) | **`≤`** 6 | "Needs 12 months of census history — 7/12 so far; treat readings as provisional." (`:554`) ⚠ D-032 |
| 21 | Tier-spread ratio | Valuation | — | `Spread` | range | **PSA 10 / 9** · 9 / raw · 10 / raw *(label `Pair`)* | `×` prefix | `≥` 3 | — |
| 22 | Grading-arb EV | Valuation | — | `Arb EV` | range | — | `$` prefix, signed | `≥` 50 | — |
| 23 | Set | Identity | — | `Set` | multi + **search** | — | — | none selected | — |
| 24 | Era | Identity | — | `Era` | multi | — | — | none selected | — |
| 25 | Character | Identity | — | `Character` | multi + **search** | — | — | none selected | — |
| 26 | Quiet Accumulation (G1) | Composites | — | `G1` | enum | — | — | `ACTIVE` | — |
| 27 | Supply Flood Watch (G2) | Composites | — | `G2` | enum | — | — | `ACTIVE` | — |
| 28 | RS Breakout (G4) | Composites | — | `G4` | enum | — | — | `ACTIVE` | — |

**Group order is fixed and authored** (`:482–500`): Price & trend (6) → Momentum & reversion (5) → Liquidity (6) → Supply (2026+) (3) → Valuation (2) → Identity (3) → Composites (3).

**Menu-row tooltips.** Only five items carry a `tip` (`:490`, `:494`); every other row gets `''` (`:786`), i.e. no tooltip. The five are Amihud, Price dispersion, Discount-to-list, Cross-marketplace gap, Supply overhang — and each tooltip string is **identical to that metric's editor caution**. So the LOW DATA caution is shown twice: once as a menu hover, once as a panel inside the editor.

**Badge rendering.** Badge chip is mono 10.5px/600, `--warnInk` on `rgba(176,127,26,.12)`, radius 3px (`:108`). Non-badged rows still render the span with `background: transparent` and empty text (`:788`). The same badge repeats in the editor header (`:118–120`, resolved by name lookup at `:811`).

**Enum option sets:**
- EMA cross state: `3/9 bullish` · `3/9 bearish` · `9/21 bullish` · `9/21 bearish` · `Fresh cross ≤ 1 mo` (`:537`)
- MACD state: `Above signal` · `Below signal` · `Histogram rising` · `Histogram falling` · `Fresh bullish cross` (`:538`)
- G1 / G2 / G4 — one shared array `compOpts` (`:533`): `ACTIVE` · `WATCH` · `ACTIVE or WATCH` · `EXITED ≤ 30d ago`

**Multi option sets:**
- Set — 17 values (`:557`): Base Set, Jungle, Fossil, Team Rocket, Neo Genesis, Hidden Fates, Evolving Skies, Fusion Strike, Brilliant Stars, Astral Radiance, Lost Origin, Silver Tempest, Crown Zenith, 151, Obsidian Flames, Paldean Fates, Prismatic Evolutions
- Era — 8 values (`:558`): `WOTC (1999–03)` · `EX (2003–07)` · `DP (2007–11)` · `BW (2011–14)` · `XY (2014–17)` · `SM (2017–20)` · `SWSH (2020–23)` · `SV (2023– )`
- Character — 19 values (`:559`): Charizard, Pikachu, Umbreon, Espeon, Sylveon, Leafeon, Glaceon, Vaporeon, Jolteon, Flareon, Gengar, Mewtwo, Mew, Rayquaza, Lugia, Giratina, Dragonite, Snorlax, **Any alt art** (18 species + one non-species pseudo-option)

**Tier vocabulary.** The Price editor's 20 windows are `Any tier` plus the canonical 19-value grade scale in **descending** order, using **`Raw`** where the scraper domain says `Ungraded` (`CLAUDE.md:93`) — the app-wide rename recorded at `DESIGN_NOTES.md:77`.

### 3.5 Filter editor — field contract (`:114–171`, logic `:792–837`)

Populated by `openEditor` (`:677–681`): `edWin = defWin || windows[0] || ''`, `edOp = defOp || '≥'`, `edV1/edV2 = String(v1/v2)` or `''`, `edSel = opts[0]` for enum, `edMulti = {}`, `edSearch = ''`.

| Element | Gate | Contract |
|---|---|---|
| Header: `‹` back · title · badge | always / `edHasBadge` | `‹` clears `editor` and returns to the menu without adding (`:817`). Title is the full metric name |
| Window pills | `edHasWin` = `!!ed.windows` (`:818`) | Label row uses `ed.winLabel` or `"Window"` (`:818`). Pill tooltip is generic: `"Measure this metric over {w}"` (`:819`) |
| Condition (operator) pills | `edIsRange` (`:820`) | Always `≥ · ≤ · between`. Tooltips: ≥ "Keep cards at or above the value you set"; ≤ "Keep cards at or below the value you set"; between "Keep cards between two values — useful for quiet, mid-range bands" (`:821`) |
| Value input(s) | `edIsRange`; second input on `edIsBetween` (`:822`) | `<input type="number">`, 84×26, mono. Unit renders as a **prefix** span when unit is `$` or `×` (`edHasPre`), as a **suffix** span otherwise when non-empty (`edHasSuf`) (`:823`) |
| Search box | `edSearchable` = `multi && ed.search` (`:826`) | Placeholder "Filter options"; case-insensitive `includes` filter over options (`:812`) |
| Option list | `edHasOpts` = `opts.length > 0` (`:826`) | `max-height: 168px`, own scroll (`:155`). Marker box 14×14, radius **50% for enum / 3px for multi** (`:830`), `✓` when on. Tooltips: enum on → "Currently matching X", off → "Match cards in the state: X"; multi on → "Stop including X", off → "Include X in the results" (`:830`) |
| Caution panel | `edHasCaution` (`:833`) | Amber: `rgba(176,127,26,.07)` fill, `rgba(176,127,26,.2)` border, 12px `--warnInk` (`:165`). Informational — never blocks **Add** |
| Preview + **Add** | always | Mono preview, ellipsised, left; primary Add right (`:167–169`). Add appends the preview string to `added[screen]` and closes both editor and popover (`:835`) |

Pill selected/unselected styling is one shared helper: on → `bg --acc`, `fg --card`, `bd --acc`; off → `bg --card`, `fg --mut`, `bd --line` (`:813`, and the identical `:597` for backtest pills).

### 3.6 Chip generation grammar (`:796–809`)

Chips are **generated from editor state**, never typed. The generated string is both the chip label and the preview text — they are the same value (`:807–809`, `:835`).

Value formatter `fv(v)` (`:796–804`):
- empty/null → `…`
- `signed` metric and `v ≥ 0` → prefix `+`
- unit `$` → `[−]['+' if signed and ≥0]$` + `abs(v).toLocaleString('en-US')`
- unit `×` → `×` + value (after the signed prefix)
- otherwise → value + unit (suffix)

Grammar by shape:

| Shape | Template | Example produced by the defaults |
|---|---|---|
| range, `≥`/`≤` | `{short}[ {window}] {op} {fv(v1)}` | `Price PSA 10 ≥ $50`, `ROC 3M ≥ +5%`, `Amihud ≤ 25th`, `Churn accel ≥ ×1.4`, `Overhang ≤ 6 mo` |
| range, `between` | `{short}[ {window}] {fv(v1)}–{fv(v2)}` — **no operator word, no "and"** | `Price PSA 10 $50–$2,000` |
| enum | `{short}: {selection}` | `MACD: Above signal`, `G1: ACTIVE` |
| multi | `{short}: {none→"any" \| 1–2→"a, b" \| ≥3→"N selected"}` | `Set: any`, `Era: WOTC (1999–03), EX (2003–07)`, `Character: 5 selected` |

The window segment is included **whenever the metric has windows** — there is no "omit the default window" rule.

**The seeded chips do not all obey this grammar.** They are hand-authored illustrative strings (`:455–465`). Non-producible or non-conforming examples: `Spread compressing` and `New 12M high` (no metric emits these — "New 12M high" is not in the vocabulary at all), `EMA 3/9 bullish` (grammar → `EMA: 3/9 bullish`), `Era: WOTC` (grammar → `Era: WOTC (1999–03)`), `Tier: Raw` (tier is a *window* of Price, never its own chip), `Churn 30d ≥ ×1.2 baseline` (trailing word), `ROC 1M between −2% and +2%` (grammar → `ROC 1M −2%–+2%`), `ROC 3M ≤ 0%` (grammar → `≤ +0%`), and `Price ≥ $50` / `RS pct ≥ 90th` / `Gem rate ≥ 40%` / `Price $200–$2,000` (window segment dropped). **Implement the generator, not the seeds.**

### 3.7 Terminal columns (`colDefs :768–776`, header `:352–358`)

Grid template (`:870`): `52px minmax({name}px, 1.4fr)` + 7 fixed pixel tracks. Defaults from `state.colW` (`:427`): name 170, price 78, roc3 78, rs 64, z 64, churn 72, pop 84, comp 96. Resize clamps to **40–420px** (`:434`).

| Order | Header | Sort key | Header tooltip |
|---|---|---|---|
| 1 | *(blank)* — art | not sortable | — |
| 2 | **Card** | not sortable (resize pipe only, `:354`) | — |
| 3 | **Price** | `price` | "Latest monthly price for the row tier" |
| 4 | **ROC 3M** | `roc3` | "Rate of change, 3 months" |
| 5 | **RS pct** | `rs` | "Relative strength vs market index, 3M percentile" |
| 6 | **z 6M** | `z` | "z-score vs 6-month moving average" |
| 7 | **Churn** | `churn` | "Churn acceleration: 30d vs 90d baseline. Requires 60+ post-seam days — 3 cards hidden" |
| 8 | **Pop Δ 60d** | `pop` | "Census growth, 60 days. 2026+ — 7 observations" ⚠ D-001/D-032 |
| 9 | **Screen** | `comp` | "Composite state for this screen: ACTIVE / WATCH / EXITED" |

Only columns 3–9 are sortable; each renders `{name} {arrow}` where arrow is `↑`/`↓` on the active key and `''` otherwise, and the active header text turns `--acc` (`:780`, `:356`). Columns 2–9 each carry a `│` drag handle titled "Drag to resize" (`:354`, `:356`).

**The metric columns are fixed** — they do not follow the chosen filters. A screen filtered on Amihud still shows Price/ROC/RS/z/Churn/Pop Δ/Screen.

### 3.8 Backtest dataset contract (`:503–532`)

Three datasets — `short`, `mid`, `long` — selected by `BT_MAP` (`:521`): `g1 → short`, `g2 → short`, `g3 → mid`, **everything else (`arb`, `my1`, `my2`, duplicates, new screens) → `long`** (`:586`).

Horizon-exit dataset (`:503–519`):

| Field | Type | Rendered |
|---|---|---|
| `range` | string `"Mar ’26 → Aug ’26"` | Date range value (`:227`), equity-curve corner (`:251`), "Buy signals" tile sub (`:629`, `:636`), scan line, story line (with `→` replaced by `and`, `:614`) |
| `months` | int | scan text "scanning N months of history" (`:625`) |
| `start` | `[year, monthIndex]` | x-axis month labels (`:569`) |
| `scr`, `idx` | number[] normalized to 100 | polylines, return figures, hover values |
| `entries` | int | story line, "Buy signals (entries)" tile |
| `floor` | string | always-visible amber floor banner (`:232`) |
| `stats["3M"\|"6M"\|"12M"]` | object or **`null`** | `null` ⇒ that horizon pill is disabled |
| `stats[h].aged` | int | **never rendered** — `hitSub` carries the wording |
| `stats[h].hit` / `hitSub` | strings | Hit-rate tile value / sub |
| `stats[h].med` / `mean` | strings | Median / Mean tiles |
| `stats[h].dd` | string | Max-drawdown tile — but **always read from `stats["3M"].dd`** regardless of selected horizon (`:640`) |
| `stats[h].buckets` | int[6] | histogram |
| `ageNote["6M"\|"12M"]` | string | disabled-pill tooltip, story line, grey maturity banner, histogram fallback |
| `rows` | `[name, entryLabel, priceThen, r3, r6, r12]`, nulls allowed | entries table |
| `more` | string | table footer line |
| `warn` | string, **optional** | concentration banner — present on `short` only |

Signal-exit dataset — `BT_EXIT` (`:522–532`), same three keys:

| Field | Rendered |
|---|---|
| `hit` / `hitSub` | Hit rate tile ("4 of 7 closed trades") |
| `med` | Median return tile |
| `mean` | **not rendered** — no Mean tile exists in signal-exit mode (`:628–634`) |
| `medHold` | Median hold tile (`"2 mo"`) |
| `open` | Open positions tile |
| `rows` | `[name, entryLabel, priceThen, exitReturn, heldMonths]`; `null` exit return ⇒ still open |
| `buckets` | int[6] histogram |
| `more` | table footer line |

Histogram bucket labels are shared by both modes and fixed (`:598`): `≤−10%` · `−10–0` · `0–10` · `10–25` · `25–50` · `50%+`. Bars 1–2 use `negBg(0.45)`, bars 3–6 `posBg(0.5)`; height = `round(n/max × 92) + (n>0 ? 4 : 1)` px, so an empty bucket still draws a 1px stub (`:661`).

### 3.9 Backtest stat tiles

Tile shape: uppercase 11px key · mono 21px/700 value · 11.5px muted sub · whole tile is `cursor: help` with a `title` tooltip (`:286–289`). Laid out `repeat(3, 1fr)` inside a `minmax(0,1fr) 440px` two-column grid shared with the histogram (`:283–284`).

**Horizon exit — 6 tiles always, plus 2 conditional (`:635–651`):**

| Key | Value | Sub | Colour |
|---|---|---|---|
| Buy signals (entries) | `ds.entries` | `ds.range` | ink |
| Hit rate **{H}** | `stats[H].hit` or `—` | `hitSub` or "no entries aged yet" | ink |
| Median **{H}** | `stats[H].med` or `—` | "return per entry" | pos if starts `+`, else neg2; `--mut3` when absent |
| Mean **{H}** | `stats[H].mean` or `—` | "return per entry" | same rule |
| Max drawdown | `stats["3M"].dd` | "screen equity" | neg2 |
| Market index | index total return | "same window, buy & hold" | mut |
| Best entry **{H}** *(conditional)* | best non-null row return at H | card name | pos |
| Worst entry **{H}** *(conditional)* | worst non-null row return at H | card name | neg2 |

Best/Worst appear **only when** `stats[H]` exists **and** at least one seeded row has a non-null return at H (`:642–651`). So horizon mode shows **6 or 8** tiles, never a fixed count.

**Signal exit — exactly 6 tiles (`:628–634`):** Buy signals (entries) · Hit rate · Median return · Median hold · Open positions · Market index. No Mean, no Max drawdown, no Best/Worst. "Open positions" is the only accent-coloured value.

All eight tooltips are authored strings and are part of the copy contract (`:629–641`, `:648–649`) — e.g. Mean's tooltip explicitly teaches the mean-vs-median tell: "If mean is well above median, a few big winners are carrying the screen."

### 3.10 Backtest entries table (`:317–344`, columns `:671–674`)

Header: "Buy signals (entries)" + an **export CSV ↓** button (`:322`) — `type="button"` with a title and **no handler**; `DESIGN_NOTES.md:149` records that the real app wires it.

Grid: `minmax({name}px, 1fr)` + 5 fixed tracks from `state.btColW` (name 150, entry 80, price 96, r3 72, r6 72, r12 72; `:427`, `:670`). All six headers carry resize pipes into the `btColW` bucket (`:674`).

| Position | Horizon-exit column | Signal-exit column |
|---|---|---|
| 1 | Card | Card |
| 2 | Entry | Entry |
| 3 | Price then | Price then |
| 4 | **+3M** | **Exit return** — `pc()` formatted, or `unrealized` in `--mut3` when open |
| 5 | **+6M** | **Held** — `"{n} mo"`, or `—` when open |
| 6 | **+12M** | **Status** — `OPEN` in `--acc`, or `closed` in `--mut2` |

Horizon mode highlights the selected horizon in **both** places: the header cell gets an `--accBg` background (`:674`), and the matching body cells get `--accBg` background plus `font-weight: 700` (`:665–666`). Signal-exit mode sets every column's highlight key to `'x'` so nothing ever matches (`:672`) and clears all body highlighting (`isExit` guard, `:665`).

Return cells use `pc()` (`:595`): `null` → `—` in `--mut3`; otherwise sign + 1 dp + `%`, positive `--pos`, negative `--neg2`, zero `--mut`.

Footer line, always shown below the table (`:344`): "Returns are gross of selling fees. Each entry snapshot is computed only from data captured on or before the entry date — lookahead is structurally impossible."

### 3.11 Equity curve contract (`:245–282`, `:566–584`, `:593`)

- Title row: "Equity curve · normalized 100" · `screen {ret}` in accent 700 · `index {ret}` muted · right-aligned `range`.
- Y gutter is a **32px** absolutely-positioned column (`:254–258`) holding max label (top), optional `100` label, min label (bottom). The `100` label renders only when its position sits strictly between 10% and 88% of the plot height (`:578`) — it hides rather than collide with the axis ends.
- Plot: `<svg width="100%" height="200" viewBox="0 0 800 200" preserveAspectRatio="none">`. Scale: `y = 192 − (v − min)/(max − min) × 184`, min/max taken across **both** series concatenated (`:592–593`). Dashed baseline at `v = 100` (`:261`). Index polyline `--mut3` 1.5px; screen polyline `--acc` 2px; both `vector-effect: non-scaling-stroke`.
- X labels: first, middle (`round((n−1)/2)`), last — three only, `margin-left: 40px` (`:277–281`, `:579`).
- Hover: `onMouseMove` maps cursor x to the nearest index (`:575`); renders a full-height 1px crosshair, a 9px dot on each series, and a tooltip pinned **top-left at 8px, 8px** (not following the cursor) showing month label, `screen {value} · {±%}`, `index {value} · {±%}` (`:265–274`, `:573`). `onMouseLeave` clears (`:576`).
- Month labels are computed from `start` + index, formatted `MMM ’YY` (`:567–569`).

## 4. States

Initial state (`:427`): `screen: 'g1'`, `view: 'terminal'`, `railOpen: true`, `bt: false`, `btH: '3M'`, `btExit: 'horizon'`, `btPhase: 'idle'`, `sortKey: 'comp'`, `sortDir: -1`, `addOpen: false`, `editor: null`. `state.saved` is **not declared** in the initial object — it starts `undefined` (falsy) and is only ever set by `doSave` (`:860`).

### 4.1 Rail
| State | Trigger | Rendering |
|---|---|---|
| **Open** (default) | `railOpen: true` | 232px aside visible; toggle glyph `«` (`:861`) |
| **Collapsed** | Click the toggle button (`:81`) | Entire `<aside>` unmounted by `sc-if` (`:52`); toggle glyph `»`; main expands |
| **Row menu open** | Click `⋯` on a row (`:718`, `stopPropagation`) | 160px dropdown on that row only; `railMenu` holds one id, so opening a second closes the first |
| **Row menu closed** | `onMouseLeave` on the dropdown (`:63`), or a document click outside `[data-rail-menu]` that is not another "Screen actions" button (`:684`) | — |

### 4.2 Header title / thesis
| State | Trigger | Exit |
|---|---|---|
| **Display** (default) | — | — |
| **Editing name** | "+ New screen" (auto-enters, `:854`), or rail menu → **Rename** (`:719`) | `Enter` blurs → commit; blur → commit (`:697–705`); `Escape` cancels without saving (`:705`) |
| **Editing thesis** | Rail menu → **Edit thesis** (`:720`) | same |

Commit writes into `edits[screen]` and **drops empty input** — trimming to `''` means no patch is applied, so the previous value survives (`:702`). Both editors are `autoFocus`. There is no click-to-edit on the header text itself; editing is reachable **only** through the rail menu or new-screen creation.

### 4.3 Filter popover — two levels
| State | Gate | Trigger in | Trigger out |
|---|---|---|---|
| **Closed** | `addOpen: false` | — | — |
| **Level 1 — metric menu** | `addOpen && showMenu` (`:102`, `:793`) | Click `+ filter` (`:862`) | Click `+ filter` again; `onMouseLeave` **only while no editor is open** (`:862`); document click outside `[data-filter-pop]` (`:685`) |
| **Level 2 — metric editor** | `addOpen && showEditor` (`:114`, `:815`) | Click any metric row (`:789` → `openEditor`) | `‹` back → level 1 (`:817`); **Add** → closes the whole popover (`:835`); outside click closes both (`:685`) |

Level 2 replaces level 1 inside the same 300px panel — it is not a second floating layer. The `onMouseLeave` dismissal is deliberately suppressed at level 2 so a user reaching for the value input cannot lose their work (`:862`).

### 4.4 Results mode
| Mode | Gate | Notes |
|---|---|---|
| **Terminal** | `view === 'terminal' && !bt` (`:864`) | Default |
| **Binder** | `view === 'binder' && !bt` (`:864`) | |
| **Backtest** | `bt === true` (`:600`) | Suppresses `+ filter`, chip `✕` buttons, the view toggle, Save, and Backtest → (`:97`, `:177`, `:185`); replaces them with ← Results, the BACKTEST MODE badge, and the read-only note |

Entering backtest resets `btPhase` to `'idle'` (`:601`); leaving it does **not** reset anything else (`:602`), so `btH`, `btExit`, and column widths persist across mode flips.

### 4.5 Save button
| State | Trigger | Rendering |
|---|---|---|
| **Idle** | default | Label "Save", `--card` bg, `--line` border, ink text (`:857–859`) |
| **Confirmed** | Click Save (`:860`) | Label "✓ Saved", `posBg(.10)` bg, `posBg(.35)` border, `--pos` text; reverts after **1800 ms**; re-clicking restarts the timer (`clearTimeout`) |

### 4.6 Backtest mode — sub-states

**Exit-rule state** (`btExit`, `:653–656`) — two pills, `horizon exit` (default) and `signal exit`. Everything below branches on it.

**Phase state** (`btPhase`):
| Phase | Trigger | Rendering |
|---|---|---|
| **idle** | entering backtest (`:601`) | Centred panel, 14.5px muted, `max-width: 590px` explanatory paragraph (`:233–237`). Text differs per exit mode (`:620–622`) |
| **run** | click **Run backtest** (`:624`) | Centred panel, mono accent line: `"scanning {months} months of history ({range}) · "` + `"holding until signal exit…"` or `"measuring +{H} outcomes…"` (`:625`) |
| **done** | 1100 ms after run (`:624`) | Full results anatomy (`:243–345`) |

Re-running clears the pending timer first, so rapid clicks cannot double-fire (`:624`). Changing horizon or exit rule **does not** reset the phase — results re-render live against the new selection without a re-run.

**Horizon state** (`btH` ∈ `3M`, `6M`, `12M`, `:604–612`):
| Horizon condition | Enabled? | Tooltip |
|---|---|---|
| `stats[h] == null` | **disabled** (`disabled` attr, `--hov` bg, `--mut3` text, `#EBEBE7` border, `cursor: not-allowed`) | `"No entries have aged {N} months yet — "` + the `ageNote` tail. Seeded: 6M → "the earliest cohort matures Sep 2026."; 12M → "the earliest cohort matures Mar 2027." (`:507–508`, `:608`) |
| enabled, horizon-exit mode | yes | `"Hold each entry {N} months, then sell regardless of what the screen says"` + `" (currently selected)"` when active (`:610`) |
| enabled, signal-exit mode | pills stay rendered but the **whole group is inert** | `"Not used with signal exit — the screen decides when to sell"` (`:609`, `:657`) |

In signal-exit mode the horizon group gets `opacity: 0.35; pointer-events: none` with a 0.15s transition (`:217`, `:657`) — dimmed and inert but **not removed**, so the Run button never shifts position (`DESIGN_NOTES.md:12`). Note that `pointer-events: none` also suppresses the group's own `title`, so the "Not used with signal exit" explanation is unreachable by hover in the state it describes.

**Data-dependent banner states** — the complete inventory:

| Banner | Severity tier | Trigger | Line |
|---|---|---|---|
| **Honest floor** | **Amber** — `rgba(176,127,26,.07)` fill, `rgba(176,127,26,.2)` border, `--warnInk` text | **Always visible in backtest mode**, in every phase, above the idle/running/done block. Text = `ds.floor` | `:232`, `:619` |
| **Maturity / age note** | **Grey** — `--mutbg` fill, `--line` border, `--mut` text | `btPhase === 'done'` **and** `stats[btH] == null` (`btHasAgeNote = !s`) | `:311–313`, `:658` |
| **Concentration** | **Red** — `negBg06` fill, `negBg25` border, `--neg3` text, prefixed by a bold uppercase `⚠ Concentration` label | `btPhase === 'done'` **and** the dataset has a `warn` string. Only `short` does, so only screens mapping to `short` (`g1`, `g2`) ever show it | `:314–316`, `:668` |
| **Hidden rows** (terminal only) | **Amber** — same amber trio, welded to the table bottom | Rendered **unconditionally** in terminal view | `:380–385` |
| **Editor caution** | **Amber** | Per-metric, whenever the open editor's metric has a `caution` | `:164–166` |

Banner order in the done state is fixed: story → equity curve → (tiles + histogram) → grey age note → red concentration → entries table → grey footnote (`:243–345`).

The seeded concentration text (`:509`) states the rule the engine must implement: "8 of 14 buy signals are SWSH alt arts" — `DESIGN_NOTES.md:16` fixes the threshold at **>50% of signals from one set**.

**Story-line state** (`:613–618`) — three mutually exclusive templates:
1. *signal exit* — "We found {N} buy signals (entries) between {range}, selling each when its card stopped matching the screen. Of the closed trades, {hit} made money ({hitSub}), median {med} over a typical {medHold} hold — vs {index} for buying and holding the market index. {open} positions are still open."
2. *horizon exit, no aged entries* — "We found {N} buy signals between {range}. None have aged {H} yet — {ageNote}"
3. *horizon exit, aged entries* — "We found {N} buy signals (entries) between {range}. Holding each for {H}, {hit} made money ({hitSub}), with a median return of {med} — vs {index} for buying and holding the market index over the same window."

`range` renders with `→` replaced by "and" inside prose (`:614`), and horizons render as "3 months" not "3M" (`:615`).

**Histogram state** (`:293–309`):
| State | Trigger | Rendering |
|---|---|---|
| Bars | `isExit \|\| stats[btH]` (`btHasBars`, `:660`) | 6 bars; title = `"Closed-trade return distribution"` in signal exit, `"{H} return distribution"` in horizon exit (`:659`) |
| No bars | horizon exit **and** `stats[btH] == null` (`btNoBars`) | Title only, with the `ageNote` text rendered as body copy — the same string that also appears in the grey banner below (`:307`, `:312`) |

### 4.7 Transient / pointer states
| State | Trigger | Behaviour |
|---|---|---|
| **Art preview** | `onMouseEnter` on a terminal row's thumbnail (`:361`, `:748–757`) | 164×226 fixed panel at `x = thumb.right + 10`; `y` centred on the thumb then clamped to `[max(8, headerBottom + 4), min(innerHeight − 234, tableBottom − 230)]`, so it never covers the sticky header or overflows the table. Cleared on `onMouseLeave` (`:875`). **Terminal only** — binder cells have no preview |
| **Chart crosshair** | `onMouseMove` over the plot (`:259`, `:575`) | Snaps to the nearest sample; only re-renders when the index actually changes |
| **Column drag** | `onMouseDown` on a `│` handle (`:428–443`) | Document-level `mousemove`/`mouseup`; sets `body.cursor = 'col-resize'` and `body.userSelect = 'none'` for the duration; width clamped 40–420px. Two independent buckets: `colW` (terminal) and `btColW` (backtest entries) |

### 4.8 States that do **not** exist in the prototype
No loading skeleton, no error state, no empty/no-results state, no "0 screens" rail empty state, no pagination, and no virtualization — the results list is the full 12-card seed array every time. `uploads/CARDSTOCK_UI_SPEC_v1.md:429` authors a no-results string ("No cards match. Your tightest filter is {chip} — try loosening it.") that the prototype never renders.

## 5. Interactions

### 5.1 Rail
| Control | Line | Consequence |
|---|---|---|
| Screen row (body button) | `:57`, `:711` | `screen = id`; **resets** `removed = {}`, `sortKey = 'comp'`, `sortDir = -1`. Does **not** reset `bt`, `btH`, `btExit`, `btPhase`, `view`, or `added`. Tooltip: active → `Currently showing "{name}"`; inactive → `Run "{name}" — {sub}` |
| `⋯` Screen actions | `:61`, `:718` | Toggles that row's menu; `stopPropagation` so the document handler does not immediately re-close it |
| Rename | `:64`, `:719` | Closes menu, **switches to that screen**, enters name-edit with the current name preloaded |
| Edit thesis | `:65`, `:720` | Closes menu, switches to that screen, enters thesis-edit |
| Duplicate | `:66`, `:721–725` | Creates id `{id}-copy{Date.now()%1000}`, copies the *effective* (edited) screen, appends `" (copy)"` to the name, keeps `sub`, forces `preset: false`, appends to `myIds`, and **switches to the copy**. The original is untouched. `removed` is **not** cleared, so a chip hidden on the source stays hidden on the copy |
| Delete | `:68`, `:726` | Removes the id from `myIds`; if it was the active screen, falls back to `myIds[0]`. **No confirmation dialog.** Deletes presets as readily as user screens. Tooltip warns "its filters and backtest history go with it" |
| + New screen | `:73`, `:851–855` | Creates `new{Date.now()%100000}` with name "Untitled screen", sub "0 filters · just now", the placeholder thesis, `chips: []`; switches to it, clears `removed`, and immediately enters name-edit |

### 5.2 Header
| Control | Line | Consequence |
|---|---|---|
| Rail toggle `«`/`»` | `:81`, `:861` | Mounts/unmounts the rail |
| Name input | `:83`, `:697–706` | `onChange` → `editVal`; `Enter` → blur → commit; blur → commit; `Escape` → discard. Empty/whitespace commits are ignored |
| Thesis input | `:89` | Same handlers (`chg`/`commit`/`keyH` are shared) |

### 5.3 Filter row
| Control | Line | Consequence |
|---|---|---|
| `+ filter` | `:99`, `:862` | Toggles `addOpen` and always clears `editor`, so reopening returns to level 1 |
| Metric row | `:106`, `:789` | `openEditor(name)` — seeds every editor field from the metric definition (§3.5) |
| `‹` back | `:116`, `:817` | `editor = null`; popover stays open at level 1; nothing is added |
| Window pill | `:127`, `:819` | `edWin = w`; live-updates the preview |
| Operator pill | `:137`, `:821` | `edOp`; choosing `between` reveals the second value input |
| Value inputs | `:142`, `:145` | `edV1` / `edV2` as raw strings; empty renders `…` in the preview |
| Options search | `:152`, `:827` | Case-insensitive substring filter over the option list. Only Set and Character have it |
| Option row | `:157`, `:831` | enum → single-select (`edSel`); multi → toggle in the `edMulti` map |
| **Add** | `:169`, `:835` | Appends the current preview string to `added[screen]`, clears `editor`, closes `addOpen`. **No validation** — an empty value ships a chip reading `Price PSA 10 ≥ …` |
| Chip `✕` | `:177`, `:730` | Marks the chip's **label** in `state.removed`. Because the key is the label, identical labels on different screens are hidden together, and re-adding the same chip text stays hidden |

### 5.4 Results band
| Control | Line | Consequence |
|---|---|---|
| `terminal` | `:187`, `:866` | `view = 'terminal'`; active segment gets `--acc` bg + `--card` text. Tooltip: "Terminal density — more rows, tighter type, every metric column" |
| `binder` | `:188`, `:866` | `view = 'binder'`. Tooltip: "Binder density — fewer rows with card art" |
| **Save** | `:191`, `:860` | 1800 ms "✓ Saved" confirm. **Nothing is persisted** in the prototype |
| **Backtest →** | `:192`, `:601` | `bt = true`, `btPhase = 'idle'`. Carries the long educational tooltip that defines the whole feature ("Would this screen have made money? …") |
| **← Results** | `:195`, `:602` | `bt = false` |

### 5.5 Terminal results
| Control | Line | Consequence |
|---|---|---|
| Sortable header | `:356`, `:781` | Sets `sortKey`; if already the active key, flips `sortDir`, otherwise resets to `-1` (descending). Sorting is a plain comparator; `comp` is ranked ACTIVE 2 > WATCH 1 > EXITED 0 (`:740–741`) |
| Resize pipe (8 handles) | `:354`, `:356` | Live column resize, 40–420px |
| Row | `:360` | `role="button" tabindex="0"` with a pointer cursor and hover tint — but **no `onClick` and no key handler**. Rows are announced as buttons and do nothing |
| Row thumbnail hover | `:361` | Opens the floating art preview |
| `show anyway →` | `:382`, `:873` | Toggles `state.revealHidden` — **which nothing reads**. The button is inert in the prototype |
| `sufficiency rules ⓘ` | `:384` | Link to `Cardstock About Data.dc.html#sufficiency` |

### 5.6 Binder results
| Control | Line | Consequence |
|---|---|---|
| Card cell | `:393` | `role="button" tabindex="0"`, hover box-shadow lift — **no handler** |

### 5.7 Backtest mode
| Control | Line | Consequence |
|---|---|---|
| Exit-rule pill (`horizon exit` / `signal exit`) | `:213`, `:656` | Sets `btExit`. Re-renders story, tiles, histogram, table columns, and idle copy **without** re-running. Tooltips are the whole plain-English explanation: horizon = "Sell on a timer… Like cooking for exactly 30 minutes"; signal = "Sell when it stops matching… Like cooking until it's done" (`:654–655`) |
| Horizon pill `3M`/`6M`/`12M` | `:221`, `:607` | Sets `btH` → changes the story line, the Hit rate / Median / Mean / Best / Worst tile keys and values, the histogram title and bars, the highlighted entries-table column, and the scan text. Disabled when that horizon has no aged entries |
| **Run backtest** | `:230`, `:624` | `btPhase = 'run'` → 1100 ms → `'done'` |
| `export CSV ↓` | `:322` | No handler — real app wires it (`DESIGN_NOTES.md:149`) |
| Entries-table resize pipes (6) | `:326`, `:674` | Resize into the `btColW` bucket |
| Equity-curve hover | `:259` | Crosshair + dual dots + fixed-position tooltip |
| Stat tile hover | `:286` | `cursor: help` + authored `title` |

### 5.8 Document-level
`componentDidMount` installs one document `click` listener (`:682–688`) that (a) closes the rail menu unless the click is inside `[data-rail-menu]` or on another "Screen actions" button, and (b) closes the filter popover and clears its editor unless the click is inside `[data-filter-pop]`. The popover branch guards on `e.target.isConnected` — `DESIGN_NOTES.md:11` records why: a re-render race was swallowing menu clicks. Removed on unmount (`:689`).

## 6. Rules and invariants

1. **Filters AND together.** The `+ filter` tooltip is explicit: "Add another condition — results must satisfy every filter" (`:99`). There is no OR, no grouping, no nesting anywhere in the markup or logic.
2. **Chips are generated, never authored.** The chip label and the editor preview are the same string, produced by one formatter (`:807–809`, `:835`). The seeded chip arrays are illustrative and several do not conform — see §3.6.
3. **Backtest is a mode of a screen, not a page.** Rail, header, and chips stay mounted; only the results area swaps (`:202`, `:349`, `:389`).
4. **Filters are read-only while backtesting.** Enforced structurally: `+ filter` and every chip `✕` are inside `sc-if notBt` (`:97`, `:177`), and the band states it in words (`:198`).
5. **The entry rule is fixed and not configurable.** It renders as static text `card enters "{screen name}"` (`:207`) — the screen *is* the entry rule.
6. **Horizon and exit rule are orthogonal but only one is live at a time.** Horizon pills stay mounted and keep their selection in signal-exit mode; they are dimmed and inert rather than removed so the Run button does not move (`:657`).
7. **Unmeasurable horizons are disabled, never blank.** A horizon with no aged entries is a disabled pill carrying its maturity date, never a row of dashes (`:605`, `:608`) — `DESIGN_NOTES.md:16`, `BACKTEST_WARNINGS.md` check 2.
8. **The honest floor is always visible.** The floor banner renders in idle, running, and done alike (`:232`), and every seeded `floor` string states the *reason* — "a screen is only as old as its youngest metric" (`:505`). The date-range value carries the matching tooltip "Bounded by the honest floor below" (`:227`).
9. **Entries ≥ today's matches.** The Buy-signals tooltip explains the gap: entries also count cards that entered and later exited, plus re-entries, and "Every card matching today appears here once" (`:636`). The seeded footer reconciles it explicitly — 14 entries vs 12 matches (`:508`).
10. **No lookahead.** Asserted in three places: the idle copy (`:621–622`), the entries-table footnote (`:344`), and the Run tooltip (`:230`).
11. **Max drawdown is a property of the equity curve, not the horizon.** The tile always reads `stats['3M'].dd` (`:640`) and every seeded dataset repeats one `dd` across all three horizons. Implication for implementation: compute it once per screen/window, not per horizon. Structural caveat — this expression would throw if a dataset ever had a null `3M` bucket.
12. **Best/Worst entry tiles are conditional**, derived from the seeded rows rather than from the stats block (`:642–651`), so they reflect only the rows the table shows, not the full entry population.
13. **All three horizon columns are always shown in the entries table**, with the selected one tinted and bolded (`:665–666`, `:674`) — a deliberate reversal recorded at `DESIGN_NOTES.md:14`.
14. **Sufficiency exclusion is a floor with an escape hatch, not a hard block.** Rows below the floor are hidden and counted, with `show anyway →` and a link to the sufficiency rules (`:380–385`). Editor cautions are informational and never disable **Add**.
15. **Presets and user screens are indistinguishable at runtime.** `preset` is written on all six seeded screens (`:454–465`) and read nowhere. Every screen is renameable, duplicable, and deletable.
16. **Unit rendering is positional, not per-metric.** `$` and `×` are prefixes; `%`, `th`, `σ`, and `mo` are suffixes; the rule is derived from the unit string itself (`:823`).
17. **`signed` metrics get an explicit `+`.** Applied to ROC, z 6M, Beta, Pop Δ, and Grading-arb EV (`:800`).
18. **One backtest dataset per screen, with a default.** `BT_MAP` covers only `g1`/`g2`/`g3`; every other screen — including anything the user creates — falls through to `long` (`:586`). A real implementation must compute per screen; the map is scaffolding.
19. **Theme and CVD are read once at construction.** `this.PAL` is built from `localStorage` at class-field initialisation (`:419–426`) and the helmet script sets the root attributes before paint (`:32`). Logic-driven colours therefore do **not** react to a theme change without a reload, while CSS-token colours do. Four palette branches: light/dark × standard/CVD.

## Corrected values — build these

Written 2026-08-10 (D-061). The filter caution strings in §3 carry authored numbers that are wrong. **The rule from D-033 is: author the denominator, never the ratio.** Numerators and unlock dates are computed against the 2026-09-01 floor and today's date.

### Caution strings to replace

| Metric | Prototype string | Build instead | Why |
|---|---|---|---|
| **Discount-to-list** (`:491`) | "Listed price captured on only ~12% of rows" | "Listed price captured on only **4.4%** of rows" | `DESIGN_NOTES.md:46` measured 143,062 of 3,265,910 sales. D-031 rules 4.4% credible; the 12% figure has no receipt anywhere |
| **Cross-marketplace gap** (`:492`) | "eBay-only depth today" | "**Needs ≥5 sales per venue in the window**" — drop the eBay-only assertion pending a query | `DATA_MODEL.md:102`, `:227` document **five** sources: ebay, tcgplayer, goldin, heritage, pwcc. Whether observed volume is effectively eBay-only is a distribution question no one has run |
| **Pop Δ** (`:505`) | "Census history starts Jan '26 — 7 observations" | "Census history starts when we first visited this card. **N observations so far**" — N computed | D-001: census begins at each card's first visit, late Jul 2026. Jan 2026 is 7 months before that |
| **Gem rate** (`:506`) | "Census history starts Jan '26" | Same substitution | Same |
| **Supply overhang** (`:507`) | "Needs 12 months of census history — 7/12 so far" | "Needs 12 months of census history — **N/12**" — N computed from the floor | The 7 was derived from the false Jan 2026 start (D-032) |
| **Amihud / churn** (`:511`) | "ledger begins Apr 2025" | "**the ledger begins when we first visited this card**" | D-001 |

### Badge values

`:487–494` uses four badge strings: `LOW DATA`, `POST-SEAM`, `7 OBS`, `24M MIN`.

- **`7 OBS` becomes `N OBS`**, computed. It is `LOW DATA` carrying its count (D-056), not a separate state.
- **`LOW CONFIDENCE` must not appear** — collapsed into `LOW DATA` per D-056.
- `POST-SEAM` and `24M MIN` are denominators, not ratios, and stand as written.

### Unlock dates, recomputed from the 2026-09-01 floor

| Metric | Prototype | Corrected |
|---|---|---|
| 24-month liquidity (Amihud) | "~Apr 2027" | **~Sept 2028** |
| 12-month census (overhang, gem drift) | implied ~Jan 2027 | **~Sept 2027** |

**Do not author these as strings either** — compute them, so they stay right as time passes.

### Filter count

The Screener has **28** filter metrics (`:481–501`, `:534–563`), not the 27 in `HANDOFF.md:72` or the 29 in `DESIGN_NOTES.md:7`. `DISPLAY_VOCABULARY.md`'s own table already lists 28. The Screener landing page advertises "12 filters" and is also wrong — see `marketing.md`.

### Seeded chips

11 seeded chips (`:455–465`) violate the "chips are generated, never authored" rule, and at least one — `New 12M high` — is not producible by the generator at all. **Implement the generator; discard the seeds.** They are illustrative, not a specification.

---

## 7. Open questions

**Routing and persistence**
1. Does the screen id belong in the URL? `HANDOFF.md:72` says yes (`/screener/{id}`), `uploads/CARDSTOCK_UI_SPEC_v1.md:112–113` says no. The prototype is silent.
2. `/screener` is claimed by both the app and the marketing Screener Landing page (`HANDOFF.md:72` vs `:84`). Which one owns the path?
3. What does **Save** actually do when the active screen is a preset — overwrite, or fork? No new-vs-overwrite affordance exists (`:191`).
4. Who computes the rail sub-line (`"4 filters · edited 2d ago"`)? It is authored per screen and never recomputed (`:462`).
5. The DC prop `defaultView` (enum terminal|binder, default terminal, `:417`) is declared but never read — `state.view` is a literal (`:427`). Is view density a persisted user preference?

**Results behaviour**
6. What happens on **row click**? Rows are `role="button" tabindex="0"` with no handler (`:360`, `:393`). `uploads/CARDSTOCK_UI_SPEC_v1.md:87` says peek panel, then "Open" → Card page. Unimplemented, and the accessibility role is currently a lie.
7. Are the terminal metric columns fixed, or should they follow the active filters? The prototype hard-codes seven (`:768–776`).
8. What does `show anyway →` reveal, and how are the revealed rows marked? `state.revealHidden` is toggled and read by nothing (`:873`). `DISPLAY_VOCABULARY.md:133` says "marked unreliable" without specifying a treatment.
9. The hidden-rows banner names a **single** metric (`:872`). With several floor-bearing filters active, which one is named — the tightest, or all of them?
10. No empty, loading, or error state exists. `uploads/CARDSTOCK_UI_SPEC_v1.md:429` authors a no-results string that nothing renders.
11. Chip removal is keyed by **label text** (`:730`). The real model needs stable filter ids; otherwise two identical chips on different screens are removed together.
12. Binder view offers no sort control and inherits the terminal sort (`:838`). Intended?

**Composite / screen-state semantics**
13. How is the `Screen` column's `ACTIVE` / `WATCH` / `EXITED` state defined? The seeded `compSince` reasons imply a near-miss rule ("churn ×1.4 not yet sustained", "price drifting below band", "pop reading is 1 observation old", `:473–479`) but no threshold is written down.
14. `EXITED ≤ 30d ago` as a composite filter option (`:533`) implies a 30-day exit memory. Where is that stored, and does it apply to user screens or only to G1/G2/G4?

**Backtest**
15. Is the **date range** user-editable? `DESIGN_NOTES.md:12` lists it as backtest config, but the prototype renders it as static text bounded by the honest floor (`:226–228`).
16. `BT_EXIT` carries a `mean` value that no tile renders (`:523`). Missing tile, or a deliberate exclusion because mean is misleading over variable holds?
17. Signal-exit mode drops **Max drawdown** entirely (`:628–634`). Intentional, or an omission?
18. In signal-exit mode the horizon group's explanatory tooltip is unreachable because the group is `pointer-events: none` (`:217`, `:657`). Needs a reachable affordance.
19. In signal-exit mode the grey maturity banner still keys off `stats[btH]` (`:658`), so it can appear while the horizon is irrelevant. Suppress it in signal exit?
20. How is the honest floor computed in general — "a screen is only as old as its youngest metric" (`:505`) is the stated rule, but the mapping from metric → data-start is per-metric and, per D-001, per-card and ragged.
21. Concentration is set-keyed only; character clustering is listed as "to add" (`BACKTEST_WARNINGS.md:9`). Which of the 15 checks ship in v1?
22. Is the badge vocabulary closed? The prototype uses exactly four strings — `LOW DATA`, `POST-SEAM`, `7 OBS`, `24M MIN` (`:487–494`).

**Data honesty (blocked on D-032/D-033)**
23. Every hard-coded sufficiency number in this screen's copy is known-wrong (see §8, rows 19–21). What replaces `7 OBS`, `7/12`, `Apr ’27`, `Jan ’26`, `Apr 2025`, and `~12%` once the 2026-09-01 floor is applied? These are *copy* decisions, not just arithmetic — the badges and cautions are user-facing.

## 8. Contradictions found

Tier 1 (this HTML) wins over Tier 2/3 docs, **except** where `DECISIONS.md` records an owner decision, which overrides all tiers (`CLAUDE.md:30`). Rows 19–21 are that exception: the HTML copy is the thing that is wrong.

| # | Claim | Source doc:line | What the HTML actually does |
|---|---|---|---|
| 1 | "27 filter metrics" | `HANDOFF.md:72` | **28.** `FILTER_MENU` (`:481–501`) and `EDITORS` (`:534–563`) each define 28 entries across 7 groups: 6 + 5 + 6 + 3 + 2 + 3 + 3 |
| 2 | "filter editors for all **29** signals" | `DESIGN_NOTES.md:7` | 28 |
| 3 | "§9 … all **27** metrics" | `DESIGN_NOTES.md:159` | 28 — and `DISPLAY_VOCABULARY.md`'s own §9 table already lists 28 rows, so the summary miscounts its own table |
| 4 | Route `/screener/{id}` exists | `HANDOFF.md:72` | No routing of any kind; screen selection is `state.screen` (`:427`, `:711`). `uploads/CARDSTOCK_UI_SPEC_v1.md:112–113` lists only `/screener` and `/screener/{screenId}/backtest`, so the two docs also disagree with each other |
| 5 | `/screener` is the marketing Screener Landing page | `HANDOFF.md:84` | Same doc assigns `/screener` to the app at `:72`. Direct collision; the HTML cannot arbitrate |
| 6 | Results band actions are "Save / Backtest→ / **Alert me**" | `DESIGN_NOTES.md:10` | Only **Save** and **Backtest →** (`:191–192`). The alert affordance was stripped 2026-08-08 (`DESIGN_NOTES.md:80`, `:120`); line 10 is a stale 2026-08-03 record |
| 7 | `between` chip renders as "ROC 1M **between** −2% **and** +2%" | `DISPLAY_VOCABULARY.md:92`; seeded chip `:455` | The generator emits `{short} {window} {fv(v1)}–{fv(v2)}` — no operator word, no "and" (`:807`). The same doc line states the correct rule (`v1–v2`) and then gives an example that violates it |
| 8 | Exit-mode tile is labelled "Hit rate (closed)" / "Hit rate (closed trades)" | `DISPLAY_VOCABULARY.md:67`; `DESIGN_NOTES.md:15` | Tile key is plain **`Hit rate`**; "N of M closed trades" is the sub-line, not the label (`:630`) |
| 9 | Horizon tiles are "Hit rate · Median return · Mean return · Best entry · Worst entry" | `DISPLAY_VOCABULARY.md:67`; `DESIGN_NOTES.md:15` | Keys carry the selected horizon: **`Hit rate 3M`**, **`Median 3M`**, **`Mean 3M`**, **`Best entry 3M`**, **`Worst entry 3M`** (`:637–649`). Subs are "return per entry", not "return" |
| 10 | Best/Worst entry are part of the standard horizon tile set | `DISPLAY_VOCABULARY.md:67`; `DESIGN_NOTES.md:15` | **Conditional.** Rendered only when `stats[btH]` exists *and* at least one row has a non-null return at that horizon (`:642–651`). Horizon mode shows 6 or 8 tiles |
| 11 | Exit mode "swaps to" 4 tiles: Hit rate · Median return · Median hold · Open positions | `DESIGN_NOTES.md:15` | Exit mode renders **6** tiles — it keeps Buy signals (entries) and Market index, and drops Mean and Max drawdown (`:628–634`) |
| 12 | Honest floor is a **Grey (mechanics)** banner | `BACKTEST_WARNINGS.md:27` | Rendered **amber**: `rgba(176,127,26,.07)` fill, `rgba(176,127,26,.2)` border, `--warnInk` text (`:232`). Only the *age note* is grey (`--mutbg`/`--mut`, `:312`). The same file's intro (`BACKTEST_WARNINGS.md:3`) describes the amber/grey split correctly, so it contradicts itself |
| 13 | Hidden-rows banner reads "N **rows** hidden — churn needs 60+ post-seam days" | `DISPLAY_VOCABULARY.md:133` | "**3 cards hidden — insufficient data for churn (needs 60+ post-seam days)**" (`:381`), with both the count (`3`) and the metric string hard-coded (`:872`) |
| 14 | Hidden-rows banner reads "1,204 cards hidden: insufficient data — show" | `uploads/CARDSTOCK_UI_SPEC_v1.md:61` | Third distinct wording; HTML wording as row 13, and the escape link is "**show anyway →**" |
| 15 | "Removing the last filter shows the unfiltered corpus" | `DISPLAY_VOCABULARY.md:96` | No filter ever affects results. `rows` is the full 12-card seed array and `matchLabel` is `rows.length` regardless of chips (`:738–744`, `:856`). Unverifiable in the prototype — treat as a design intent, not a Tier 1 fact |
| 16 | "Row click → peek panel; 'Open' → Card page" | `uploads/CARDSTOCK_UI_SPEC_v1.md:87` | Rows are `role="button" tabindex="0"` with **no** click or key handler (`:360`, `:393`). The only row interaction is a hover-triggered floating art preview (`:361`, `:748`) |
| 17 | "Ranked, **virtualized** results table" / "QuickGrid + virtualization" | `uploads/CARDSTOCK_UI_SPEC_v1.md:87`, `:46` | Plain `sc-for` over the full array; no virtualization, no pagination, no "load more" (`:359`) |
| 18 | Beta vs index has unit "signed" | `DISPLAY_VOCABULARY.md:110` | Beta has **no unit**; `signed: true` is a separate flag that only controls the `+` prefix (`:545`, `:800`). Same conflation would mislead an implementer building the unit renderer |
| 19 | **⚠ D-032 / D-033.** "Amihud … needs ~24 post-seam months (**Apr ’27**)"; "Supply overhang … **7/12** so far"; "Census history starts **Jan ’26** — **7 observations**"; `7 OBS` badges; Pop Δ column tip "2026+ — 7 observations" | HTML `:490`, `:494`, `:548`, `:552–554`, `:493`, `:774` — repeated verbatim into `DISPLAY_VOCABULARY.md:113`, `:117–119` | **The HTML is the wrong source here.** `DECISIONS.md:342–356` (D-032) proves these ratios were derived from dates D-001 disproved; `DECISIONS.md:309` (D-033) sets a disclosed floor of **2026-09-01**, giving ~1/24 (unlock ~Sept 2028) for Amihud and ~1/12 (unlock ~Sept 2027) for census-backed metrics. Every one of these strings must be recomputed before the Screener ships (`DECISIONS.md:244`) |
| 20 | **⚠ D-001.** Backtest floor notes: "the grading census, which begins **Jan 2026**"; "the per-sale ledger begins **Apr 2025**" | HTML `:505`, `:511` | Contradicted by `DECISIONS.md:22` (D-001): both histories begin at each card's **first crawler visit, late Jul 2026**, and the seam is per-card and ragged, not a single shared date. The prototype's floor arithmetic (Mar ’26, Jun ’25 windows) is therefore illustrative only |
| 21 | **⚠ D-031.** Discount-to-list: "Listed price captured on only **~12%** of rows so far" | HTML `:491`, `:550` | `DISPLAY_VOCABULARY.md:36` and `:61` say **4.4%**; `DECISIONS.md:359` (finding) and D-031 at `DECISIONS.md:416` record 4.4% as the credible figure. `DISPLAY_VOCABULARY.md:115` copies the HTML's 12% straight through, so the same file disagrees with itself. The Screener editor caution overstates coverage by ~3× |
| 22 | Concentration fires "when >50% of signals from one set" | `DESIGN_NOTES.md:16` | The prototype computes nothing — `warn` is an authored string present only on the `short` dataset, gated on `btPhase === 'done'` (`:509`, `:668`). The threshold is doc-only and unverified by the HTML; the seeded text ("8 of 14") is consistent with it |
| 23 | Backtest config includes a **date range** control | `DESIGN_NOTES.md:12` | Date range is static read-only text with the tooltip "Bounded by the honest floor below" — no picker, no handler (`:226–228`) |
| 24 | Chips are "generated, never authored" | `DISPLAY_VOCABULARY.md:91` (rule is correct) | The rule holds for the live add-path (`:807–809`, `:835`), but **the seeded chips violate it**: `Spread compressing` and `New 12M high` (`:457`, `:459`) name conditions no metric can emit — "New 12M high" is not in the 28-metric vocabulary at all — and `EMA 3/9 bullish`, `Era: WOTC`, `Tier: Raw`, `Churn 30d ≥ ×1.2 baseline`, `Price ≥ $50`, `RS pct ≥ 90th`, `Gem rate ≥ 40%`, `Price $200–$2,000`, `ROC 3M ≤ 0%` all deviate from the generator's output (`:455–465`). Implement the generator; discard the seeds |
