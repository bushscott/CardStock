# Worker demand inventory — what every screen asks of the Phase 3 worker

**Working notes for the Phase 3 (worker-only) brainstorm, 2026-08-14.** Built by reading every
`docs/screens/*.md` spec in full plus `docs/signals.md`, extracting what each surface needs
*precomputed* (a prior run to diff against, corpus-wide ranking, or too expensive per-request)
versus what stays request-time. Receipts are spec sections. Uncommitted working file; its content
feeds the Phase 3 design doc.

Substrate shorthand (signals.md): **[S1]** monthly six-tier `price_months` · **[S2]** per-sale
ledger, post-seam · **[S3]** population census, post-first-observation.

---

## signals.md — the computation catalog itself

- 29 rankable signals (25 atomic + 5 composites, minus exclusions); **16 are v1** per the summary
  table: F2 RS-vs-index, A1 ROC, E1 arb EV, D2 pop-vs-price divergence, F1 index, D1 pop Δ,
  A2 EMA cross, C1 churn, C4 volume, G4/G1/G2 composites, E2 spread, A4 trend R², B4 drawdown,
  C2 churn accel, B1 z-score, D3 gem rate.
- **F1 index method guidance** (signals.md F1): chained index of per-card monthly relatives,
  equal- or value-weighted; explicitly NOT Card Ladder's sum-of-last-sold (jump flaw); mitigate
  membership churn with a minimum active-card count per period; per-set indices too (**789 sets**,
  D-086 — not ~303). Repeat-sales regression noted as gold standard but chained-relatives chosen
  as pragmatic. "If a repeat-sales index materially diverges…publish both" (Recommendations).
- **F2 RS** needs the index PLUS percentile ranking within corpus and within set.
- Chip firing rules (signals.md §chip vocabulary) that need **corpus-wide context**: volume
  top-decile (`Most active`), Amihud percentile **within set**, RS percentile. Phase 2 shipped
  `Sales volume {n}/30d` neutral precisely because corpus ranking doesn't exist yet.
- Every signal carries caveats + a sufficiency constraint; product rule: states, never bare nulls.
- Threshold escape hatches (Recommendations): demote S2 signals if ledgers stay thin; global
  restatement banner + auto-suppress D-family during grader restatements.

## Home (home.md) — heaviest single consumer of precomputed rows

**Ticker — 16 stats × 3 windows (7d/30d/90d), fixed order** (§3.2):
SALES (count + Δ% vs prior window) · VOLUME ($ + Δ%) · BREADTH (% advancing) · **INDEX (return —
pinned 30d, window-invariant)** · VINTAGE (% of $ vol) · GRADING (+slabs · gem %) · MEDIAN SALE
($ + Δ%) · VENUE (mix by share) · **NEW 12M HIGHS (count — pinned 30d)** · MEDIAN ROC ·
TOP WINNER (card, %) · TOP LOSER (card, %) · TOP SALE (card, $, venue) · MOST ACTIVE (card,
count) · HOT SET (set, %) · CHARACTER LEADER (character, %).
- Simple window aggregates over `sales` (SALES, VOLUME, MEDIAN SALE, VENUE, TOP SALE) are
  request-time-feasible SQL; everything involving per-card returns or corpus ranking (BREADTH,
  INDEX, NEW 12M HIGHS, MEDIAN ROC, TOP WINNER/LOSER, HOT SET, CHARACTER LEADER) implies
  precomputed per-card return/metric rows.
- §7 open: index undefined (§7.2); why INDEX/HIGHS pin to 30d (§7.3); BREADTH/VINTAGE/
  GRADING-gem/VENUE definitions missing (§7.4). CHARACTER LEADER needs character tags
  (Pokédex phase — not in scraper data).
- Corpus footer stat: `{cards} cards · {sales} sales observed` (§3.12).

**Screen activity feed** (§3.8): rows are worker-emitted events — `Entered "{screen}"` /
`Exited "{screen}"` / `Indicator unlocked: {name}` with an **evidence line captured at
evaluation time** ("indicator values at evaluation time") + timestamp. Feed is stored ordered;
glyph and state are two independent per-row fields. Header `{N} since your last visit · {M}
unlock` needs per-user last-visit. Unlock rows are a product event the worker must detect
(floor crossings), copy: "…reached 30 days for PSA 10 — starts LOW DATA" (§8 row 2, D-056).

**Watchlist chips** (§3.5): tracked-signal states per (card, tier) row (D-055) — need current
materialized signal values incl. RS percentile, pop Δ, churn ratio, arb EV, composite regime
(`Quiet Accum`), and **pending unlock ETA computed from authored denominators** (§8 "one rule":
compute the countdown, never store it).

**Binder card** (§3.9): `vs market index` in **pp, trailing 12M**; 13-point normalized
portfolio-vs-index chart → needs an index **monthly series**, not just a latest value.

## Screener (screener.md §1–§4) — the widest per-card demand + point-in-time replay

**Filter vocabulary — 7 groups, 28 metrics (§3.4), every one needs a screenable per-card value:**
- Price & trend: Price (per tier — 20 tier windows: Any + the full 19-value canonical scale, so
  screening touches tiers with **no [S1] series**, e.g. BGS 10 Black); ROC 1/3/6/12M; EMA cross
  state (enum incl. **9/21 windows** — beyond Phase 2's 3×9); MACD state; Trend R² (6M/**12M**);
  Drawdown (12M/**24M**/All-time peak).
- Momentum & reversion: RS pct (1M/**3M**); z 6M; Bollinger %B/bandwidth; RSI(6); Beta (24M MIN).
- Liquidity (all POST-SEAM/LOW DATA badged): Churn 30/90/**accel**; Sales/mo; Amihud pctile;
  Dispersion; Discount-to-list; Cross-marketplace gap.
- Supply 2026+: Pop Δ 30/**60**/90d; Gem rate + drift 90d; Overhang.
- Valuation: Tier-spread ratio (**PSA 10/9**, 9/raw, 10/raw); Arb EV.
- Identity: Set; Era (**needs CardStock-owned set-metadata table** — release date + era,
  D-004/DECISIONS:1253); Character (**needs character-tag table** — Pokédex phase).
- Composites: G1/G2/G4 as per-screen membership state.
- Editor cautions carry per-metric sufficiency copy (post-seam hiding, obs counts) — copy is
  authored but **numbers inside must be computed** (D-061 denominator rule).

**Composite/screen state machine** (§3.2, §3.7): per (screen, card): `ACTIVE | WATCH | EXITED`
+ since-date + WATCH/EXITED **reason phrases** ("churn ×1.4 not yet sustained"). Terminal
columns fixed: Price, ROC 3M, RS pct, z 6M, Churn, Pop Δ 60d, Screen-state. Churn column
tooltip: "Requires 60+ post-seam days — {n} cards hidden" → hidden-row counting is a
first-class output.

**Backtest (§3.8–§3.11) — the deep constraint on the worker's storage:**
- Replays a screen over history: entry cohorts, screen equity curve vs **index** (both
  normalized 100), per-horizon stats (3/6/12M: hit rate, median, mean, max drawdown, buckets),
  signal-exit variant (hold until membership exit), maturity gating ("No entries have aged N
  months yet — earliest cohort matures {date}"), concentration warning (**>50% of signals from
  one set**, DESIGN_NOTES:16), always-visible floor banner.
- Footer contract: **"Each entry snapshot is computed only from data captured on or before the
  entry date — lookahead is structurally impossible."** → point-in-time correctness is a
  structural requirement. Either the worker stores per-period metric history (so membership can
  be replayed from stored values) or backtest recomputes from raw history with as-of discipline.
- Export CSV of entries is expected to be wired in the real app (DESIGN_NOTES:149).

## Cross-cutting facts already on the ledger

- **D-033 floor:** no post-seam metric counts observations before 2026-09-01; denominators
  authored, numerators computed (D-061). Earliest census delta ≈ 1 Nov 2026 (DECISIONS:933).
- **D-039:** worker candidate jobs = index · per-card metric materialization + sufficiency
  states · saved-screen evaluation/feed · ticker aggregates · per-card sufficiency vs floor.
  Writes CardStock schema only; reads scraper tables (SELECT-only grants, D-065/ADR-0001).
- **Phase 2 signal engine** (D-069.11): seven signals + RSI(6) + volume row, computed in Domain
  per card on request, closed months only — "the seed of the Phase 3 worker's corpus-wide
  computation."
- **Any month can revise, incl. closed ones** (D-078): reads must resolve latest-per-key for
  every month; a worker snapshot taken before a revision must cope with restatement.
- **price_months month-axis gaps are real gaps** — carrying forward across a missing month
  fabricates a price (CLAUDE.md §related repo). Index design must decide gap handling without
  smoothing over discontinuities (rule 1).
- **Scraper cadence:** continuous polite crawl, no "corpus done" moment; express visits can
  land anytime (CLAUDE.md). Sibling stats interval 5 min, enumeration 7d, canary 6h
  (ScraperOptions.cs).

## Per-page findings — full-spec sweep + phase-chart dependency audit (2026-08-14)

All 14 specs read in full this session, plus D-004 and D-033 ledger entries. Context: the owner
proposed building the worker **all at once, late** (after catalog, accounts/watchlists, Screener-UI,
Binder), and asked whether every other page's non-worker dependencies fall into place under that
ordering. Findings per page:

### Browse — set mode buildable at phase 3; species mode blocked
- Set mode is a flat alphabetical grid (era shelves were deleted from the prototype; shelving is a
  *latent* dep on the set-metadata table, §4.2). Tile wants `{count} cards` + `{chg} 30d` per set —
  the set-level 30d change has **no defined derivation** (§7.6) and is index-flavoured; define a
  request-time method or defer the stat.
- **"Browse-by-Pokémon cannot ship before [the character-tag table] does"** (§7.7). Species
  aggregates (total value, 90d) are species-index methods, also undefined.
- The value-ordering caption requires an explicit `ORDER BY total_value DESC` (§6.3).

### Set — mostly live request-time; index slots lock
- Set code `SWSH07` needs the set-metadata table (§7.1) — curation for **789 sets** (D-086).
- Set-index sparkline + 30D/90D deltas = per-set index (worker F1); RS pct column = market index.
  Both → D-087 locked slots pre-worker. Price/ROC/Pop Δ (with its pending states + exclusion
  banner)/Sales-mo are derivable request-time over one set's cards.
- D-004 receipt: the index is load-bearing on 5 of 10 screens (Home ticker, Set sparkline, Charts
  compare/RS/beta, Binder vs-index, the whole backtest equity curve).

### Character — wholly blocked on the Pokédex substrate
- "The entire page has no rows without [character tags]" (§8 net) + external Pokédex schema + set
  metadata (Year). Its 90d stat is a species index, method undefined. Belongs entirely to a
  Pokédex phase.

### Binder — worker coupling is bigger than "the chart is locked"
- Tables: `transactions` only (D-067 derived holdings, D-074 FIFO, one binder/user; users exist).
- **The EST valuation mechanism itself needs the index** — "value estimated from index movement
  since the last observed sale" — not just the performance tab. Spec §7.3 calls the index
  "Blocking" for "the entire performance tab and its EST semantics."
- Pre-worker Binder ships whole: ledger + corrections, FIFO realized P&L, win rate, avg hold,
  yearly summary, holdings valued at sale-backed latest prices; EST + BV-vs-IX chart + vs-index
  tile lock. D-012 (93/118 unvalued tier labels) must be ruled in this phase; dormant `bucketOf`
  must never be revived.

### Charts — clean at phase 9 given worker (7) + watchlists (4)
- Index-dependent: compare overlay, RS, RS percentile, beta, set rotation, and composite g1–g4
  rule definitions (which exist nowhere — worker design must author them). §7.6 blocks on D-039.
- Charts is the **only tracked-signal editor** (watch button saves enabled-minus-NONTRACK).
  Consequence: watchlist rows created in phases 4–8 carry a default tracked set, uneditable until
  Charts lands.
- Saved views = new per-user table this phase. Per-card seam markers computed (D-061). The locked
  row form + `force()`/burn-in must be wired (D-038, D-049).

### About-data — static, no worker dep, full copy rewrite required
- The most factually wrong prototype page (§6): Apr-'25 seam narrative false four ways, "sale
  counts" never existed, price-history depth understated ~32 months, first-party framing never
  names pricecharting. Three time-dependent strings must be computed, never authored (D-033).
- Linked from Home footer, Card page, Screener's "sufficiency rules ⓘ" → `#sufficiency`, Legal,
  and marketing methodology — so it should exist by/alongside phase 3.

### Account / Profile / Legal — accounts-era, no worker deps
- Account: five logged-out views, open signup, verification/reset links (30-min TTL) → **email
  delivery is a new ops dependency of phase 4**. "Browse the demo →" survives the demo removal and
  is the terminus of all 11 marketing CTAs — owner ruling needed when phase 4 builds this page.
- Profile: inline email-change (third transactional email), password change, typed-DELETE
  deletion; deletion counts become live counts over phase-4/5/6 tables (zeros until then).
  Deletion immediacy already ruled immediate+permanent (D-069; UserSession.cs receipt) — Legal's
  "within 30 days" copy is the side that changes (D-043).
- Legal: static; revisit at phase 4 for the stored-data closed list, New Relic promise (D-037),
  k-anonymity clause, and the pricecharting ToS question (D-010).

### Shared chrome / search / Card-page arming
- `<cardstock-search>`: Cards + Sets groups live at phase 3; Characters group waits for the
  Pokédex phase. Per-entity routes needed. The pre-paint theme script is a hard App.razor-head
  constraint.
- Card page (built) arming inventory: locked rows `RS vs index 3M` (tooltip verbatim: "arrives
  with the worker phase"), `Pop Δ 60d`, `Churn 30d` → phase 7; watchlist/binder controls → phases
  4/6 (D-098); species subline slot → Pokédex phase; "all signals in Charts →" → phase 9;
  census-gated metrics arm with data maturity, not with any phase.

### Marketing — static EXCEPT the ticker; six false seam claims
- The Landing ticker is byte-identical to Home's 30d ticker (16 stats) → a **worker consumer on
  the marketing front door**; §6.4 rates 12 of 16 items high-risk before data maturity + index.
  Options: ship marketing post-worker, hold the ticker, or run an honest reduced set.
- Six Apr-'25 seam claims must become mechanism copy (corrected-copy table already authored in the
  spec); "12 filters" → 28; D-058 moves the family under `/product` (resolves route collisions);
  `/` resolves by auth state (anon → Landing, authed → Home) so marketing naturally pairs with or
  follows the Home phase. Reduced-motion support is a build requirement; 8 image slots need
  licensed art (D-010/D-011).

### D-033 floor mechanics (for the worker design)
- The floor is a **disclosed cutoff** (2026-09-01), not a data-start claim; safe only if later
  than every card's first visit — **verify via the null-`last_visited_at` / max-first-visit query
  before implementing**. Denominators authored, numerators computed. 24 post-seam months →
  ~Sept 2028; 12 census months → ~Sept 2027.

## Audit verdict (delivered 2026-08-14)

The owner's all-at-once-worker chart holds: no circular dependencies, every page's non-worker
inputs are produced by an earlier phase, with three additions — About-data rides with phase 3; a
Pokédex phase (character tags + species schema) must be slotted somewhere, its consumers arming
independently; marketing lands after the worker because of the Landing ticker — and with the
wrinkles listed above (set-metadata curation is real owner work in phase 3; Browse ships set-mode
only; tracked-signal editing waits for Charts; Binder's EST locks pre-worker; phase 7 ends in an
arming pass across six earlier surfaces).
