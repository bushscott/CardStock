# Cardstock — Display vocabularies (every dynamic element's full value space)
For coding: each element below is shown with SOME values in the mockups; this file defines ALL values it can take. Conventions used throughout: green #157A50 bullish · red #D64545 bearish · grey #5B5B57 neutral/informational · amber #8F6614 data-caution. Icon always accompanies color (▲ ▼ – ● ◌ ◆); never color alone.

## 1. Signal chips (Card page header · watchlist rows · peek panel)
One chip grammar everywhere: `icon + short name + evidence number`, tooltip = one-sentence evidence with window and threshold.

**Card page header**: shows only FIRING chips (a signal in a notable state), priority-ordered, cap 4, overflow "+N more" opens all. A signal below its sufficiency floor never chips.
**Watchlist rows**: chips are the user's TRACKED signals for that card — all render regardless of state, including quiet (`–` grey muted) and insufficient (`◌` grey, tooltip = unlock countdown). Glance rule: **colored = hit** (green ▲ bullish · red ▼ bearish · amber – caution/directionless), **grey = nothing to report** (quiet or not yet computable).

Full chip inventory (trigger → chip text):
| Signal | Fires when | Chip | Dir |
|---|---|---|---|
| RS vs index | pct ≥ 90 / ≤ 10 | `RS 94th` | ▲/▼ · amber – on decile exit: 80–89th within 3 mo of ≥90 (`RS 84th`) |
| ROC 3M | ≥ +15% / ≤ −15% | `ROC 3M +18%` | ▲/▼ |
| ROC 1/6/12M | same bands, only if tracked | `ROC 12M +41%` | ▲/▼ |
| MACD (3,6,4) | above/below signal line | `MACD +` / `MACD −` | ▲/▼ |
| EMA 3×9 cross | crossed within last 2 mo | `EMA cross +` / `−` | ▲/▼ |
| RSI (6) | > 80 / < 20 | `RSI 82` | ▲/▼ · amber – in 70–80 (`RSI 71`; tooltip: monthly >70 often continuation) |
| Z vs 6M MA | \|z\| > 1.5 | `z +1.8` | ▲/▼ |
| Trend R² | R² ≥ 0.8 over 6–12M | `clean trend R² .91` | ▲ if slope+, ▼ if slope− |
| Drawdown | ≥ 15% off trailing peak | `−28% off peak` | ▼ (grey-red: state, not signal) |
| Tier-spread | compression trend ≥ threshold | `spread compressing` | ▼ |
| Grading-arb EV | EV > $0 (raw ≥ $40) | `arb EV +$38` | ▲ |
| Churn 30d | ≥ 2× churn 90d | `churn ×2.3` | ▲ |
| Monthly volume | top decile in corpus | `● Most active · 41 sales/30d` | ● grey |
| Monthly volume | ≤ 1 sale/30d | `● thin · 1 sale/30d` | ● grey |
| Amihud | ≥ 90th pct within set | `illiquid · 96th pct` | ● grey |
| Dispersion | σ/μ ≥ 0.20 trailing | `wide pricing ·24` | ● grey |
| Cross-mkt gap | \|gap\| ≥ 10%, venues at depth | `eBay −12% vs auction` | ● grey |
| Pop Δ 60d | ≥ +2% | `Pop Δ +5.1%` | – amber (supply caution; ≥ +5% also arms Supply-Flood composite) |
| Pop Δ 60d | ≤ 1% with churn firing | `pop flat` | ▲ |
| Gem rate drift | \|drift\| ≥ 0.3pp/90d | `gem rate −0.4pp` | ▲ if falling, ▼ if rising |
| Supply overhang | ≥ 3 years | `3.2y overhang` | ▼ |
| Composite match | card in preset/user screen today | `◆ Quiet Accum` | ◆ icon · thesis-colored (green bullish screen · red avoid screen) |

Not chip-eligible: Bollinger (visualization-grade, spec §8.3), beta (descriptive), discount-to-list (4.4% coverage), seasonality (corpus-locked until ~Nov 2028).
Priority when > cap: composites → RS → supply (pop/overhang) → momentum (ROC/MACD/EMA/RSI/z) → liquidity → the rest. Newest crossing wins ties.

### Tracked-pill states (watchlist rows) — the complete pill set
A tracked signal ALWAYS renders exactly one pill, in exactly one of five states; no other pill forms exist:
| State | Render | Example |
|---|---|---|
| Hit bullish | green ▲ · chip text from inventory | `▲ RS 94th` |
| Hit bearish | red ▼ · chip text from inventory | `▼ EMA 3/9` |
| Caution | amber – · evidence number (notable but directionless) | `– RSI 71` |
| Quiet | grey – · short name + `–` (tracked, between bands) | `– MACD –` |
| Pending | grey ◌ · short name + unlock ETA | `◌ Churn — 12d` |

- **Amber bands (complete list)**: RSI 70–80 · RS decile exit (80–89th within 3 mo of ≥90) · Pop Δ 60d ≥ +2%. No other signal has a caution band.
- **Liquidity/state signals** (volume, Amihud, dispersion, cross-market gap) are never directional: notable = ● grey + value, else quiet.
- **Pending ETA format**: days when under 60 (`— 12d`), month beyond (`— Mar ’27`); tooltip = the floor rule + the date history began.
- **Trackable set = the chip-eligible inventory above, exactly.** Bollinger, beta, discount-to-list, seasonality (not chip-eligible) and pure overlays (SMA) cannot be tracked.

## 2. Sufficiency states (any metric, any page)
Every metric on every surface is in exactly one of: **OK** (renders plain) · **LOW DATA** (amber badge `N OBS`, tooltip says floor rule + what improves it) · **LOCKED** (control disabled, countdown copy: "unlocks ~Mar 2027 — needs 60 post-seam days") · **UNDEFINED window** (gaps rendered as gaps, never zeros — Amihud zero-sale months) · **UNSTABLE FIT** (badge, beta on thin history). Rules per metric are in spec §1.5/§8.3; the five states above are the complete render set.

## 3. Screen-activity feed rows (Home)
Row types (complete): screen ENTER (card newly matches a saved/preset screen — ▲ green if screen thesis bullish, ▼ red for avoid-list screens) · screen EXIT (amber; ▼ when the exit is adverse, – otherwise) · product event: sufficiency UNLOCK ("churn now computable for X", ◆ amber). Each row: card · screen name · evidence sentence · timestamp. No other row types exist (tracked-signal crossing rows removed with the screens-only feed ruling, 2026-08-08).

## 4. Sales ledger `source` enum (Card page)
`ebay` · `tcgplayer` · `goldin` · `heritage` · `pwcc`. Render verbatim, lowercase, mono. Realized-price underline marker appears only on rows with `listed_price_cents` (~4.4%); tooltip `listed $X → sold $Y`.

## 5. Grade buckets (everywhere) — canonical 19-value scale (user ruling 2026-08-04)
`Raw` · `Grade 1`–`Grade 9` · `Grade 9.5` · `PSA 10` · `CGC 10` · `CGC 10 Prist.` · `TAG 10` · `ACE 10` · `SGC 10` · `BGS 10` · `BGS 10 Black`. Below 10, buckets are grader-agnostic; each grader's 10 is its own tier. Display order always descending (BGS 10 Black first). User intends to trim after review — "start high."

## 6. Backtest tiles & banners (Screener backtest mode)
Horizon-exit tiles: Buy signals (entries) · Hit rate · Median return · Mean return · Max drawdown · Market index · Best entry · Worst entry. Signal-exit tiles: Buy signals · Hit rate (closed) · Median return · Median hold · Open positions · Market index. Banner set = BACKTEST_WARNINGS.md (15 checks, severity tiers). No other tiles/banners exist.

## 7. Summary sentence templates (Card page census/grading; branch rules in DESIGN_NOTES.md)
Gem rate: 3 branches (falling/rising/flat ±0.1pp). Pace: 3 pace words (rising/steady/slowing) × 2 parentheticals (supply pressure / scarcity intact) + LOW DATA degrade. No free text — every rendered sentence is one of these combinations.

## 8. Tracked-signal states on sparklines (Home watchlist, peek panel)
Marker set: ▲ green above spark (bullish crossing in window) · ▼ red below · hollow ◌ (current month provisional) · amber tick (sufficiency event). Complete.

## State-color palettes (theme × colorblind)
Colors are CSS custom properties set at each page root; inline styles use var(--x, <light-standard literal>) so streaming paints light. Colorblind mode swaps HUE only — glyphs (▲▼–◌◆), labels, and the state grammar never change. CVD hues are Okabe-Ito (blue #0072B2, vermillion #D55E00) adjusted for contrast per surface.
| Token | Light std | Light CVD | Dark std | Dark CVD |
|---|---|---|---|---|
| --pos | #157A50 | #0B69A8 | #4CC08D | #58A9E6 |
| --posBg | rgba(24,158,99,.10) | rgba(0,114,178,.10) | rgba(24,158,99,.18) | rgba(0,114,178,.20) |
| --neg | #C13A3A | #CC5F00 | #E57B7B | #F5924E |
| --negBg | rgba(214,69,69,.10) | rgba(213,94,0,.10) | rgba(214,69,69,.18) | rgba(213,94,0,.20) |
| --warn / --warnBg | #8F6614 / rgba(176,127,26,.12) | same | #D6A54A / rgba(176,127,26,.20) | same |

Muted/grey chips use chrome tokens (--mut2 / --mutbg) in every mode. Chrome light→dark: bg #FAFAF7→#161614 · card #FFFFFF→#1E1E1C · line #E4E4E0→#33332F · ink #1C1C1E→#E9E9E5 · mut #5B5B57→#B4B4AE · mut2 #8A8A86→#A8A8A2 (dark greys brightened twice 2026-08-09 per user: mut #B4B4AE · mut3 #9A9A94) · hover #F6F6F2→#282825 · mutbg #F3F3EE→#2A2A27 · accent #3B5BD6→#7290EA · button #3B5BD6→#4A66D8 · input bg #FAFAF7→#262624.
Persistence: localStorage cardstock-theme='dark' · cardstock-cvd='1'. CVD applies app-wide (template var() tokens + :root[data-cvd] helmet overrides; logic colors via this.PAL — pos2 #189E63→#0072B2, neg2 #D64545→#D55E00, neg3 #A93838→#B34E00, bg tints rgba(0,114,178,a)/rgba(213,94,0,a)). Dark theme applies app-wide (chrome tokens via :root[data-theme="dark"]; logic colors via 4-branch this.PAL). Dashboard dark chrome: accBg #252B44, accMut #3A4570, tooltipBg rgba(30,30,28,.95) on top of the Profile palette table.

---

## 9. Screener filter conditions — the complete vocabulary
Chip text is **generated, never authored**. Grammar:
- range → `short [window] operator value` (`Churn accel ≥ ×1.4`), `between` renders `v1–v2` (`ROC 1M between −2% and +2%`)
- enum → `short: selection` (`MACD: Above signal`)
- multi → `short: <none | a, b | N selected>` (`Set: 3 selected`)

Operators are the same three for every range metric: **≥ · ≤ · between**. Units render as prefix (`$` `×`) or suffix (`%` `th` `σ` ` mo`). `signed` metrics accept negative input. Multiple filters **AND** together; there is no OR and no grouping in v1. Removing the last filter shows the unfiltered corpus.

| Metric (editor title) | short | kind | Windows (**default**) | Unit | Default | Data caution shown in editor |
|---|---|---|---|---|---|---|
| Price (tier) | Price | range | Any tier · BGS 10 Black · BGS 10 · SGC 10 · ACE 10 · TAG 10 · CGC 10 Prist. · CGC 10 · **PSA 10** · Grade 9.5 → Grade 1 · Raw *(label: Tier)* | $ | between 50–2000 | — |
| ROC 1/3/6/12M | ROC | range | 1M · **3M** · 6M · 12M | % (signed) | ≥ 5 | — |
| EMA cross state | EMA | enum | — | — | 3/9 bullish · 3/9 bearish · 9/21 bullish · 9/21 bearish · Fresh cross ≤ 1 mo | — |
| MACD state | MACD | enum | — | — | Above signal · Below signal · Histogram rising · Histogram falling · Fresh bullish cross | — |
| Trend R² | Trend R² | range | 6M · **12M** | — | ≥ 0.6 | — |
| Drawdown from peak | Drawdown | range | 12M peak · **24M peak** · All-time *(label: Peak)* | % | ≥ 30 | — |
| RS vs index (percentile) | RS pct | range | 1M · **3M** | th | ≥ 90 | — |
| z-score vs 6M MA | z 6M | range | — | σ (signed) | ≥ 1.5 | — |
| Bollinger %B / bandwidth | Boll | range | **%B** · Bandwidth *(label: Measure)* | — | ≥ 1 | — |
| RSI (6) | RSI 6 | range | — | — | ≤ 30 | — |
| Beta vs index | Beta | range | — | signed | ≥ 1.2 | Monthly data means ~24 usable observations at best — beta estimates carry wide error bars. |
| Churn 30/90d + acceleration | Churn | range | 30d · 90d · **accel** *(label: Measure)* | × | ≥ 1.4 | Post-seam only — cards with under 60 post-seam days are hidden from results. |
| Monthly sales count | Sales/mo | range | — | — | ≥ 8 | Post-seam only — counts before the per-sale ledger begins are estimates and excluded. |
| Amihud percentile | Amihud | range | — | th | ≤ 25 | Needs ~24 post-seam months (Apr '27) for stable baselines — readings today are noisy. |
| Price dispersion | Dispersion | range | — | % | ≤ 12 | Needs ≥8 sales/mo per bucket — most cards are below that today. |
| Discount-to-list | Disc-list | range | — | % | ≥ 8 | Listed price captured on only ~12% of rows so far. |
| Cross-marketplace gap | Mkt gap | range | — | % | ≥ 5 | eBay-only depth today — needs ≥5 sales per venue per window. |
| Pop Δ 30/60/90d | Pop Δ | range | 30d · **60d** · 90d | % (signed) | ≤ 1 | Census history starts Jan '26 — 7 observations so far. |
| Gem rate + drift | Gem rate | range | **Gem rate** · Drift 90d *(label: Measure)* | % | ≥ 40 | Census history starts Jan '26 — 7 observations so far. |
| Supply overhang | Overhang | range | — | mo | ≤ 6 | Needs 12 months of census history — 7/12 so far; treat readings as provisional. |
| Tier-spread ratio | Spread | range | **PSA 10 / 9** · 9 / raw · 10 / raw *(label: Pair)* | × | ≥ 3 | — |
| Grading-arb EV | Arb EV | range | — | $ (signed) | ≥ 50 | — |
| Set | Set | multi (searchable) | — | — | 17 sets, Base Set → Prismatic Evolutions | — |
| Era | Era | multi | — | — | WOTC (1999–03) · EX (2003–07) · DP (2007–11) · BW (2011–14) · XY (2014–17) · SM (2017–20) · SWSH (2020–23) · SV (2023– ) | — |
| Character | Character | multi (searchable) | — | — | 18 named species + "Any alt art" | — |
| Quiet Accumulation (G1) | G1 | enum | — | — | composite states (below) | — |
| Supply Flood Watch (G2) | G2 | enum | — | — | composite states | — |
| RS Breakout (G4) | G4 | enum | — | — | composite states | — |

**Composite membership states (the enum options for G1/G2/G4, identical for all three):** `ACTIVE` · `WATCH` · `ACTIVE or WATCH` · `EXITED ≤ 30d ago`.

**Editor flow:** `+ filter` opens the metric list → picking a metric opens its editor (window pills, operator pills, value input(s), any caution) → `Add` commits the chip and re-runs. `‹` returns without adding. Each chip carries `✕` to remove. Cautions are informational, never blocking.

**Sufficiency interaction:** rows failing a metric's floor are excluded and counted in a banner ("N rows hidden — churn needs 60+ post-seam days") with a `show anyway →` escape that reveals them with their metrics marked unreliable.

## 10. Charts indicator panel — complete row inventory
Rows come in three forms: **toggle** (switch + optional parameter steppers + optional badge), **readout** (label + current value, no control), **locked** (disabled switch + unlock condition + progress ratio). Every row has a hover tooltip stating its data dependency.

| Group | Row | Form | Pane? | Params | Badge / unlock |
|---|---|---|---|---|---|
| Trend | EMA cross | toggle | inline | fast 2–12, slow 3–24 | — |
| Trend | SMA baseline | toggle | inline | len 3–24 | — |
| Trend | MACD | toggle | pane | f 2–12, s 3–24, sig 2–12 | re-tuned (3,6,4) for monthly bars |
| Trend | ROC 1M | readout | — | — | — |
| Trend | Trend slope (12M) | readout | — | — | — |
| Trend | Seasonality overlay | locked | — | — | 3 observed cycles · Nov 2027 (1/3) |
| Momentum · mean reversion | RSI | toggle | pane | len 3–12 | `SLOW ON MONTHLY` |
| Momentum | Bollinger bands | toggle | inline | k 4–12, m 1–3 | — |
| Momentum | z-score vs 6M MA | toggle | pane | — | — |
| Momentum | Drawdown from peak | readout | — | — | — |
| Relative | RS vs market index | toggle | pane | — | — |
| Relative | Set rotation (per set) | toggle | pane | — | `CORPUS` |
| Relative | RS percentile (3M) | readout | — | — | — |
| Relative | Beta vs index (24M) | readout | — | — | — |
| Liquidity | Churn 30d | toggle | pane | — | `POST-SEAM` |
| Liquidity | Volume & count | toggle | pane | — | `POST-SEAM` |
| Liquidity | Churn acceleration | readout | — | — | — |
| Liquidity | Amihud illiquidity | locked | — | — | 24 post-seam months · ~Apr 2027 (16/24 mo) |
| Liquidity | Price dispersion | locked | — | — | ≥8 sales/mo in bucket (3/8) |
| Liquidity | Discount-to-list | locked | — | — | listed price on 12% of rows |
| Liquidity | Cross-marketplace gap | locked | — | — | ≥5 sales/venue/window (1/5 venues) |
| Supply | Pop Δ monthly | toggle | pane | — | `NEW · 7 OBS` (on probation to ~12) |
| Supply | Pop vs price divergence | toggle | pane | — | `NOVEL · 2026+` |
| Supply | Gem rate | readout | — | — | — |
| Supply | Supply overhang | locked | — | — | 12M census history (7/12 mo) |
| Valuation | Tier spread 10/raw | toggle | pane | — | live ratio + `COMPRESSING` |
| Valuation | Grading-arb EV raw→10 | toggle | pane | — | live EV; formula = gem × PSA10 + (1−gem) × G9 − raw − fees |
| Composites | Quiet Accumulation (G1) | toggle | inline | — | `ACTIVE · <month>` |
| Composites | Supply Flood Watch (G2) | toggle | inline | — | `CLEAR` |
| Composites | Breakout Confirmation (G3) | toggle | inline | — | `LAST · <month>` |
| Composites | 3M RS Leaders (G4) | toggle | inline | — | `MEMBER · <month>` |

**Multi-tier block:** indicators analyze exactly one grade tier. Showing a second tier switches every indicator off, stashes the set, and blocks the switches with "Show a single tier to enable indicators." Returning to one visible tier restores the stash.
**Panes:** max 2 indicator panes at a time; each pane has its own close control; pane order is user-reorderable and saved with a view.
**Saved views** capture: visible tiers, enabled indicators, pane order, range, compare-to-index, normalize. Applying a view that changes indicators arms "Update watchlist" (see Watchlist model in DESIGN_NOTES).

## 11. Binder transactions — complete model
**Kinds:** `BUY` · `SELL`. No other kinds exist at the UI layer (corrections are edits, not a third kind).

| Field | BUY | SELL |
|---|---|---|
| Card | free text with typeahead against the corpus; must resolve to a real card | inherited from the chosen holding (not editable) |
| Tier | grader + grade pickers, full 19-value scale | inherited from the holding |
| Quantity | 1–99 | 1 … current holding qty (over-max turns the field's border red and blocks save) |
| Price | "Price paid ($)", > 0 required | "Price received ($)", > 0 required |
| Date | date picker | date picker |
| Note | optional free text | optional free text |

**Save is blocked until:** price > 0 **and** (BUY: card resolved · SELL-new: a holding selected · SELL-edit: qty ≥ 1).
**Correction model (user ruling — void+re-enter rejected):** the ✎ control on any ledger row opens the same modal pre-filled. Saving writes a correction; the table always shows current truth. Superseded rows render struck-through at 62% opacity and are excluded from totals — kept under the `AUDIT LOG` badge, never deleted. Append+void remains the backend representation only.
**Tabs:** holdings (what you own now) · transactions (the ledger) · performance (binder vs market index, both indexed to 100 at first transaction). Holdings additionally offers table / gallery density.
**Export CSV** (transactions tab) emits date, card, grade, quantity, price, note for every row.

## 12. Nav search (all pages)
Shared component; light DOM so theme and colorblind tokens inherit. Trigger: `/` from anywhere (unless focus is in an input), `Esc` clears and blurs, outside click closes. Fires at ≥2 characters.
Result groups render in fixed order with per-group caps: **Characters** (4) → **Sets** (4) → **Cards** (5). Each row is `name + subtitle`, where subtitle is the entity kind for characters/sets and the set name for cards. Empty state: `No matches for "<query>"`. Groups with zero hits are omitted entirely.

## 13. Density & view modes
| Surface | Modes | Meaning |
|---|---|---|
| Screener, Set, Character | terminal / binder | terminal = more rows, tighter type, every metric column; binder = fewer rows with card art |
| Binder holdings | table / gallery | gallery renders the collection as card art |
| Charts | resolution buttons + range (1Y · 3Y · 5Y · All) + custom from/to | "All" begins where the card's data honestly starts |
Density and theme choices persist per device (localStorage), not per account.
