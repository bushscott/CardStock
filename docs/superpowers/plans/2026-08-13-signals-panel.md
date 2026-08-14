# Signals Panel + Tier Tiles (Card page rework) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans, task-by-task, TDD
> throughout. The conventions of `2026-08-12-card-page.md` (commit style, gates, integration-test
> invocation, deploy runbook `ops/README.md` §5) all apply. Deploy + live-verify after each
> shippable task, as the 2026-08-13 punch-list session did.

**Goal:** The owner's 2026-08-13 card-page rework built honestly: the tier strip becomes a 3×2
grid of square tiles, the chip row is replaced by an unbounded Signals panel showing **every
evaluated signal** as a row (firing / quiet-with-value / below-floor / neutral / locked), and the
engine + wire grow to carry it.

**Design authority:** `CardStock Mockup/Cardstock Card.rework-2026-08-13.html` (owner-authored,
supersedes the frozen `Cardstock Card.dc.html` for the identity-header region ONLY — everything
else on the page keeps the original + card.md as authority). It is a bundled export: the semantic
content is JS state on its final content line — search anchors `sigChips`, `sigRows`, `tierStrip`.

**Owner rulings captured in conversation (2026-08-13 evening), binding on this plan:**
1. **Unbounded rows.** More than eight signals can fire; every firing row always renders. The
   mockup's eight rows are a sample, not a cap. The auto-fit grid wraps.
2. **Keep D-077.** The rework's tile tooltip ("latest monthly price · +6.2% over 30 days") and the
   missing ◌ are seed-copy regressions — build the new tile geometry but keep the ◌ month-to-date
   glyph + D-077 tooltip on current-month tiles, and the honest cell tooltips from card.md §2.3.1.
3. **No fake values.** Rework rows whose substrate doesn't exist render as **locked/state rows**
   (the rework's own `Churn 30d · unlocks 25 Aug` pattern), never with seed numbers: `RS vs index
   3M` (needs the Phase 3 market index), `Pop Δ 60d` (needs census deltas; observations count from
   2026-09-01). Churn's real unlock derives from the D-033 floor: 60 post-seam days → unlocks
   2026-10-31, `{n} recorded` counts days since 2026-09-01 (0 today).
4. D-087 (slots present), D-084.1 (deferred controls), and the five-state doctrine all apply.

---

### Task 1: Spec re-extraction (card.md)

Re-extract the reworked regions from the rework file into `docs/screens/card.md`, amendment-style
(keep the old text, mark superseded, date it, cite anchors):
- **§2.2 Row B/C**: the lower identity header becomes `flex wrap gap:14 align-items:stretch` of
  two blocks: the tile grid and the Signals panel.
- **§2.3 Tier tiles**: `grid: repeat(3, 100px) / auto rows 100px, gap 8`; each tile `--bg` fill,
  `1px --line` border, radius 8, padding `10px 11px`, flex column space-between; label 11px/600
  caps `--mut2` nowrap; price mono 19px/700; change line mono 12px `{chg} 30d` colored by sign.
  **Plus ruling 2**: ◌ and D-077 tooltips retained (state where the glyph sits: after the label,
  same as today's strip).
- **New §2.3.2 Signals panel**: container `flex:1 1 300px`, `--bg` tile dress, padding
  `10px 12px 9px`, column gap 8. Header row: `SIGNALS` 11px/600 caps `--mut2` + spacer + count
  `"{evaluated} evaluated · {firing} firing"` mono 11px `--mut2` `cursor:help`, tooltip verbatim:
  `"Every chip-eligible signal is evaluated on this card automatically — nothing here is opted
  into. Bollinger, beta, discount-to-list, and seasonality are excluded: visualization-grade,
  descriptive, or below coverage."` Rows grid `repeat(auto-fit, minmax(196px, 1fr))`, `gap 2px
  18px`. Row: `title={tip}`, flex baseline gap 7, `padding 3px 0`, `border-bottom 1px --line4`,
  `cursor:help`; glyph mono 11px width 9px (`▲`/`▼` toned, `●` neutral, `–` quiet, `◌` locked);
  name 12.5px ellipsis (ink when firing/neutral, `--mut` quiet, `--mut2` locked); spacer; value
  mono 11.5px/500 in the row's fg. Footer row: `+{n} quiet` (mono 11px `--mut2`, tooltip `"The
  remaining signals are inside their quiet bands or below their sufficiency floor."`) — rendered
  only when n > 0 — + spacer + `all signals in Charts →` as a **DeferredControl** (Charts doesn't
  exist; tooltip `Charts arrives in a later phase`), 11px mono.
- **§3.3 chip catalog section**: mark the chip-row presentation superseded by the panel; the chip
  ENGINE's firing rules stay (they become the rows' firing states).
- §8: new row recording the supersession and this plan.

Commit: `card.md carries the signals-panel rework before the build starts`

### Task 2: Domain — signal rows engine

**Files:** `src/CardStock.Domain/Signals/SignalRow.cs` (new), `ChipEngine.cs` (extend),
`Indicators.cs` (add RSI), tests beside each.

- `public enum SignalState { Firing, Quiet, BelowFloor, Neutral, Locked }`
- `public sealed record SignalRow(string Glyph, string Name, string Value, string Tooltip,
  SignalState State, ChipTone Tone)`
- `Indicators.Rsi(IReadOnlyList<decimal> closesOldestFirst, int period)` → `decimal?` (Wilder
  smoothing; null when fewer than period+1 values; guard non-positive prices like the other
  indicators).
- `ChipEngine.EvaluateRows(CardPriceSnapshot prices, IReadOnlyList<SaleObservation>?` — **no**:
  sales stay out of Domain purity here; sales-volume row is composed in the mapper (Task 3).
  `EvaluateRows(prices, currentMonth)` returns rows for every price-computable signal, replacing
  none of `Evaluate` (the chips API stays until Task 3 removes its callers):
  - The existing 7, each now three-state: **Firing** (current rules; value = the evidence number,
    e.g. ROC `+18%`, MACD `above signal`, z `+1.8σ`, R² `.91`, drawdown `−28%`, cross `+ cross
    2mo`), **Quiet** (computed, inside bands; value = the live reading: ROC `+13%`, MACD `above
    signal` — quiet MACD shows histogram sign trend? keep it simple: `hist +94` rounded dollars),
    or **BelowFloor** (value `—`, tooltip names the floor and what's present, e.g. `needs 10
    closed months · 6 present` — never a number).
  - **RSI (6)**: fires Caution at ≥ 70 (`overbought`), fires Pos at ≤ 30 (`oversold`), else Quiet
    with the value (`58`); floor 7 closed months.
  - **Tier spread 10/9, redefined reading** (rework): value = current ratio `×{r:0.0}`; fires ▼
    when ratio ≥ 4 **or** the ratio moved ≥ 20% in either direction vs 6 closed months earlier
    (tooltip names both triggers); BelowFloor when endpoints missing. This supersedes the
    compression-only chip; update `docs/signals.md`'s chip-vocabulary rows accordingly (Task 5).
  - Priority order for display: firing first (existing §12 priority), then neutral, then quiet,
    then below-floor, then locked (locked rows appended by the mapper).
- Hand-computed fixtures for RSI and the spread redefinition; three-state tests for at least ROC
  (firing/quiet/below-floor) on the Task-5 pattern from the original plan.

Commit: `The engine reports every signal's state, not only the firing ones`

### Task 3: Wire + composition

**Files:** `Wire.cs`, `CardPageMapper.cs`, `CardsEndpoints.cs`, Application/Api tests.

- `CardPageSnapshotDto.Signals` becomes `SignalsDto(int Evaluated, int Firing,
  IReadOnlyList<SignalRowDto> Rows)`; `SignalRowDto(string Glyph, string Name, string Value,
  string Tooltip, string State, string Tone)` (state/tone lowercase strings). The old `ChipDto`
  list and `SignalChips.razor` are retired in the same commit as their replacement lands (Task 4)
  — the solution must build at every commit, so Tasks 3+4 may be one commit if needed.
- Mapper composes, in order: `ChipEngine.EvaluateRows(...)` + **Sales volume row** (Neutral ●,
  name `Sales volume`, value `{n} / 30d` where n = sales with `SoldOn` in the last 30 days from
  the sales list the endpoint already fetched; tooltip `Sales captured in the last 30 days.
  Liquidity signals are never directional.` — no "most active" superlative until corpus ranking
  exists) + the three **Locked rows** (ruling 3, exact tooltips):
  - `RS vs index 3M` · value `locked` · `Relative strength needs the market index — it arrives
    with the worker phase`
  - `Pop Δ 60d` · value `locked` · `Needs census deltas; observations count from 2026-09-01 —
    deltas need two`
  - `Churn 30d` · value `unlocks 2026-10-31` · `Needs 60+ post-seam days · {n} recorded` (n =
    max(0, days since 2026-09-01), clock from `TimeProvider`)
- `Evaluated` = rows.Count (locked included — they were evaluated and found locked); `Firing` =
  rows with state firing. The endpoint signature: sales must reach the mapper — it already fetches
  sales? **No — the snapshot endpoint doesn't fetch sales today.** Change: the endpoint composes
  `ICardSalesReader.GetAsync` into the parallel `Task.WhenAll` (four readers now) and passes the
  list to the mapper. The `/sales` endpoint is unchanged.
- API tests: counts, a locked row's exact tooltip, sales-volume arithmetic (stub sales straddling
  the 30-day line), state strings lowercase.

Commit: `The wire carries every signal's row, its counts, and an honest 30-day volume`

### Task 4: Web — tiles + panel

**Files:** `IdentityHeader.razor(.css)` (layout), `TierStrip.razor(.css)` → tile grid,
`SignalsPanel.razor(.css)` (new), delete `SignalChips.razor(.css)` + its tests, `CardPage.razor`
touchpoints, bUnit tests.

- `TierStrip` re-lays out per §2.3 tiles (values from Task 1's spec); keeps `◌` +
  D-077/card.md §2.3.1 tooltips on current-month tiles; dashes unchanged for absent data.
- `SignalsPanel` renders header/count/rows/footer per §2.3.2. Rows are unbounded (ruling 1) —
  render them all; `+{n} quiet` appears only if the component ever folds (Phase 2 folds nothing —
  render the element only when fold count > 0, i.e. not at all yet). `all signals in Charts →` is
  a DeferredControl. Every glyph is text in the row's fg — colour never alone.
- IdentityHeader hosts `flex wrap` of TierStrip + SignalsPanel (stretch), replacing the strip row
  + chip row. bUnit: tile geometry classes, ◌ presence, panel row rendering from a fixture with >8
  rows (ruling 1: assert 10+ rows all render), count line text, locked-row tooltips, no anchors.
- The scoped-stylesheet guard test pattern (b-hash attribute) applies to `SignalsPanel`.

Commit: `Tier tiles and the unbounded signals panel replace the strip row and chips`

### Task 5: Records + ship

- `docs/signals.md`: chip-vocabulary section gains the states model (firing/quiet/below-floor/
  neutral/locked), RSI(6) and Sales-volume rows, the spread-reading redefinition (supersede the
  compression-only row, dated), and the count-line exclusion sentence (Bollinger/beta/
  discount-to-list/seasonality — 25 eligible of 29).
- `DECISIONS.md`: **D-092** — the rework adopted; rulings 1–3 above; D-088 resolved by it (mark
  D-088 with a pointer); the logo-size mystery (no diff found between exports — owner to clarify
  what it was; nothing built).
- Full gates (`dotnet test` all suites with `CARDSTOCK_TEST_DB`, format at CI severity), deploy
  per ops/README §5, live-verify on Charizard: tiles render 3×2 with ◌ on current month, panel
  shows every computable row with real values (hand-check RSI(6) and the ratio against SQL like
  the chip hand-check), locked rows carry their exact tooltips, count line truthful.
- Final commit: `Phase 2.1: the signals panel live — every signal's state on the card`
