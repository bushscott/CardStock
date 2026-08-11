# UI Design Specification — "Cardstock" (working title)
### A technical-analysis terminal for Pokémon card investors

**Version:** 1.0 — APPROVED by the owner, 2026-08-01
**Status:** Final. This document is the single source of truth for visual design (Claude Design) and implementation (Claude Code). Changes go through the owner + the design conversation's PROJECT_LOG.
**Companion documents:** `DATA_MODEL.md` (binding data constraints) · `PROJECT_LOG.md` (interview record & decision rationale) · Signal Inventory research artifact (29-signal reference)
**Downstream consumers:** Claude Design (visual/high-fidelity exploration) → Claude Code (Blazor implementation)


> ### Handoff note for Claude Design
> You are receiving an **approved** UI specification. Do not re-litigate settled decisions (layout architecture, page inventory, visual direction, tokens, priorities) — execute them in high fidelity. Where the spec gives latitude, it says so. Good first prompts against this document: "Build the Home dashboard from §4.3 using the §6 tokens (light theme)" · "High-fidelity Charts playground per §4.6 with trigger triangles per §5 TriggerMarker" · "Explore 3 takes on the peek panel interaction (§4.3) within the approved layout." Honor these invariants everywhere: no LIVE indicators (use AsOfStamp), triangles-only trigger markers, never color alone (+/− and ▲/▼ always), semantic tokens only, density per §6.4, no candlesticks or any Tier-1 impossible element (§1.5).


---

## 1. Product summary and design goals

### 1.1 What this is

A desktop-first web application that applies stock-market-style technical analysis to Pokémon cards. It sits on top of an existing scraping pipeline (PriceCharting → Postgres) holding ~100k cards across ~303 sets: monthly six-tier price history back to ~Dec 2020, an append-only per-sale ledger, and PSA/CGC population census history from 2026 forward.

Three pillars:

1. **Screener** — standing questions asked of the whole corpus ("show me cards entering quiet accumulation"), with saved screens and backtesting.
2. **Indicator playground (Charts)** — pull up a card, toggle real indicators (MACD, RSI, EMA cross, relative strength…), tune parameters numerically, and watch **trigger triangles** appear on the price history everywhere the rule would have fired. Eyeball whether the theory works, then quantify it with a backtest.
3. **Binder** — private collection tracking: immutable buy/sell transactions, realized + unrealized P&L, performance vs. the market index.

Reference point: Webull's charting/screening experience. Anti-reference: PriceCharting's own presentation ("looks like 1997"). Thesis: **their data, a terminal that Webull users respect.**

### 1.2 Success criteria (ranked — these drive every priority in §4)

1. **Portfolio piece.** A hiring manager clicks a link, spends 90 seconds, and is impressed. → Landing page + read-only demo mode are P1; the first-90-seconds path gets polish priority.
2. **Personal profit.** The owner finds cards via his own signals and the Binder proves he beat the market. → Backtest + Binder-vs-benchmark are the product's emotional center.
3. **Someday sellable.** Deferred until proven — but the schema is multi-tenant from day one so commercialization is a config change, not a rewrite.

### 1.3 Design goals

- **Density is professionalism.** Dense light terminal (TradingView/Koyfin genre): tight tables, multi-panel charts, monospace numerals, information pressure. No hand-holding on finance concepts — the persona already knows what RSI is.
- **Honesty is the brand.** The data has hard limits (no OHLC, per-card sales seams, 2026+ census). The UI never fakes around them: impossible things are omitted (with one explainer page), immature things are locked with countdowns, weak things carry badges. No "LIVE" indicator will ever appear; every data surface carries a quiet "data as of Xh ago" stamp instead.
- **Two personalities, one product.** Analysis surfaces are pure terminal. Browse/character/set surfaces let the card art supply warmth (gallery grids, dominant-color accents). A Terminal ⇄ Binder view toggle lets users flip any listing between the two.
- **Teach the data, not the finance.** Explainers exist only where this dataset differs from equities (monthly averages, seams, census-as-supply-signal).
- **Portfolio-grade engineering visible in the UI:** interop-driven charts, virtualized grids, keyboard-first flows, dual themes from a token system.

### 1.4 Hard constraints (binding)

- **Stack:** Blazor Web App, Interactive Server rendering; components → services → Postgres directly (no HTTP API for the first-party UI; API design explicitly out of scope). Charting via **TradingView Lightweight Charts** (JS interop; series markers = trigger triangles; v5 panes for indicator sub-charts). Screener grid: QuickGrid + virtualization.
- **Hosting:** Raspberry Pi fleet (16 GB, quad-core), ~1 concurrent user expected. Email via a transactional service free tier (residential IP cannot send) → alert emails favor **batched digests** over per-event sends.
- **Desktop-first; mobile app is v2.** The site must degrade gracefully on phone browsers (§7) but v1 designs target ≥1280 px viewports.
- **Access:** invite-only registration (email + password behind an invite code), plus a no-registration read-only **demo mode**. Binder and all user data strictly private. No social features.
- **Money:** integer cents USD everywhere (per data model convention). `sales.title` is raw third-party text — **must be HTML-encoded at render** (XSS defense is the render layer's job, by design).
- **Data model Rule 3:** mutable scheduler columns (`cards.last_visited_at`, `observed_sales_per_day`, `any_bucket_at_cap`, `failure_streak`, `quarantined_until`, `sets.last_*`) are working memory, never displayed as facts. Where a screen needs the underlying truth, it derives from `visits` / `sales` per data model §6.

### 1.5 The Data Sufficiency Framework (global pattern — every screen in §4 references it)

| Tier | Condition | UI treatment |
|---|---|---|
| **1 — Permanently impossible** (no OHLC/candles, VWAP, intraday, pre-seam volume, pre-2026 census, order book, real-time quotes, news) | Data never exists at the source | **Omitted from the app entirely.** One "About our data" page explains why. No permanently-disabled controls anywhere. |
| **2 — Not yet sufficient** (e.g., churn on a card 40 days post-seam; momentum with < n monthly points; census during a restatement window) | Data matures with time | **Locked control with the unlock condition as a countdown** ("Churn (30d) — unlocks in ~19 days"). Power-user "Show anyway" renders with a persistent low-confidence badge drawn *inside* the chart area (survives screenshots). |
| **3 — Sufficient but weakened** (thin trading, current revising month, high dispersion) | Data valid but noisy | **Full access + confidence badge.** No friction. |

**Screener corollary:** ranked columns exclude insufficient-data cards by default, with a visible expandable count ("1,204 cards hidden: insufficient data — show"). Prevents 3-sale cards topping churn leaderboards on noise.

**The fourth state:** every data-bearing screen defines empty / loading / error **and a data-maturity state** — day-1 sparse vs. year-2 rich, same layout, different confidence. Sufficiency badges double as progress indicators: *the past is frozen, the future compounds.*

---

## 2. Target users and primary user flows

### 2.1 Persona

**The finance-fluent card investor.** Treats Pokémon cards as an asset class; already fluent in technical analysis ("the overlap of day-trader finance bros and the collectible market is bigger than you think" — owner). Wants to *tune parameters and compare results* — the playground is the point, not an advanced mode. Secondary visitor: **the hiring manager** (success criterion 1), who never registers and experiences the product through the landing page and demo mode. **Deliberately underserved:** the finance-naive collector — not blocked, not designed for.

Consequences: indicators use real names; parameters are numeric inputs exposed by default; density over onboarding; explainers only for this dataset's quirks.

### 2.2 The seven critical flows (step by step)

**F1 — Search (from anywhere).**
1. Press `/` or click the search box (present in the nav on every screen). 2. Type ≥2 chars → typo-tolerant autocomplete over cards, sets, and characters; each row shows disambiguating context (set name, current Ungraded + PSA 10 price). Character/set matches can rank above cards ("Charizard — character page · 47 printings"). 3. Enter opens the top hit's page; ↑/↓ + Enter selects; "See all results →" opens Search Results for ambiguous queries. Esc closes.

**F2 — Research (Charts playground).**
1. Open Charts (nav) or "Open full chart →" from any card surface. 2. Card loads with the six-tier monthly price series (default: PSA 10 + Ungraded visible). 3. Toggle indicators from the left panel; each exposes its numeric parameters inline (e.g., EMA fast/slow = 3/6). 4. Indicators draw as overlays or sub-panes; rule-type indicators (crossovers, threshold crossings) additionally plot **trigger triangles** on the price series at every historical firing — ▲ buy below the line, ▼ sell above it; hover explains the rule and the price then/±3/6/12 months after. 5. Adjust a parameter → series and triangles re-render; the user eyeballs whether arrows precede moves. 6. Optional: add comparison series (other cards / set index / market index), normalized to 100 at window start. 7. Save: "Add to watchlist" persists the card **with the currently-enabled indicator set and parameters** as its tracked signals; "Save view" stores the workspace.

**F3 — Watch (watchlists + peek panel).**
1. From Charts (F2 step 7) or any card row's ⋯ menu → Add to watchlist (choose/create list). 2. Home's watchlist module shows each card with price, 1M change, sparkline, and its **tracked-signal badges** (green/amber/red state chips). 3. Click a row (or press Enter with row focus) → **peek panel** slides over the right column: image, mini chart with tracked signals, recent sales, pop Δ, "Open full chart →". ↑/↓ walks rows while open; Esc closes and restores the signals feed beneath. 4. Editing tracked signals happens in Charts; the peek panel links there.

**F4 — Discover (Screener).**
1. Open Screener. Left rail lists saved screens (user's own + built-in presets, §8.4). 2. Build filters as chips: metric + operator + value (e.g., `RS 3M ≥ 90th pct`, `pop Δ 60d ≤ 2%`, `price ≤ $500`). 3. Ranked, virtualized results table; sortable metric columns; sufficiency exclusion row at bottom. 4. Terminal ⇄ Binder toggle switches table to image-grid. 5. Save screen (name + optional one-line thesis). Saved screens power Home's signals feed and can create alert rules (F6) and backtests (F5). 6. Row click → peek panel; "Open" → Card page.

**F5 — Backtest (a mode of a saved screen).**
1. From a saved screen → "Backtest". 2. Configure: entry = card enters the screen; horizons 3/6/12 months; universe = screen's filters; date range limited to what the data honestly supports (S1-only screens: back to ~2021; screens using sales/census metrics: post-seam/post-2026 — the date picker *shows* the honest floor and why). 3. Run → results: equity curve vs. market index, per-entry outcome distribution, hit rate, median return per horizon, and the entry list (every card + entry date + outcome), exportable CSV. 4. Footnote fixed to results: "Returns are gross of selling fees." 5. Point-in-time correctness note: signals are computed only from rows whose `observed_at`/`captured_at` precede the entry date — the append-only ledger makes lookahead bias structurally impossible.

**F6 — Get alerted.**
1. In-product: Home's signals feed lists screen entries/exits and tracked-signal threshold crossings since last visit. 2. Email: from a saved screen or a tracked signal → "Email me"; rules collect into the Alert Center (bell icon). 3. Delivery: batched digest (default daily; per-rule immediate available) via transactional email; every email has one-click unsubscribe. 4. Bell icon badge = unread fired events; Alert Center lists rule status and firing history.

**F7 — Track (Binder).**
1. Binder tab → "Add transaction": type (buy/sell), card (search picker), grade tier, quantity, price paid/received, date, optional note. 2. Transactions are **immutable once saved** (delete-and-re-enter to correct; the ledger keeps both with an audit trail) — cost basis never drifts. 3. Holdings view derives positions from transactions; each shows cost basis, current value (latest tier price; if stale, index-estimated value badged "estimate"), unrealized P&L. 4. Performance view: portfolio value over time vs. market index, normalized; realized P&L, win rate, average hold time; yearly summary. 5. Export CSV.

### 2.3 The hiring-manager flow (criterion 1, explicitly designed)

Landing page → "View live demo" → dropped into a pre-seeded read-only account on **Home** (watchlists populated, signals firing, binder seeded) → most likely clicks: a watchlist row (peek panel wow), "Open full chart →" (trigger triangles wow), Screener preset (density wow), Binder (P&L-vs-index wow). Write actions show a quiet "Demo mode — sign in to save" nudge. Total friction: zero clicks of signup.

---

## 3. Site map

```
/  (logged out)                      Landing  [P1]
├── /login  /register (invite code)  /reset   Auth shell  [P1]
├── /demo → seeds demo session → /home        Demo entry  [P1]
│
/home                                Home dashboard  [P1]
/screener                            Screener  [P1]
│    └── /screener/{screenId}/backtest   Backtest mode of a saved screen  [P1]
/charts/{cardId?}                    Charts playground  [P1]
/binder                              Binder (holdings · transactions · performance)  [P1]
/browse                              Browse landing (By set ⇄ By Pokémon)  [P2]
│    ├── /set/{slug}                 Set page  [P2]
│    └── /pokemon/{species}          Character page  [P2]
/card/{id}                           Card page  [P1]
/alerts                              Alert Center  [P1 rules · P2 history view]
/search?q=                           Search results  [P1]
/settings                            Account settings (theme, email, password)  [P2]
/about-data                          "About our data" explainer  [P1]
/privacy  /terms                     Bare-bones legal  [P2]
```

**Navigation model.** Persistent top nav on all authenticated/demo screens: **Home · Screener · Charts · Binder · Browse** + global search box (`/` focuses), bell (alerts), account menu (theme toggle, settings, About our data, sign out). Logged-out visitors see only Landing/Auth/legal; every other route redirects to Landing with a "view demo" affordance. Breadcrumbs only where hierarchy exists (Browse → Set → Card). The nav never changes shape between pages — it is the product's one fixed landmark.

**Keyboard map (global):** `/` search · `Esc` close overlay · `↑/↓` row navigation in any focused table · `Enter` open peek · `o` open full page from peek · `t` toggle Terminal/Binder view on listing surfaces · `?` shows this map.

---

## 4. Page-by-page specification

Format per page: **Purpose · Priority · Layout & components · Data (reads/writes, exact tables.fields) · Content · States** (empty / loading / error / data-maturity). "New tables" refers to §8.2's user-facing schema additions — sanctioned growth per the data model's Disclosure.

### 4.1 Landing page — `/` (logged out) — **P1**

**Purpose:** the 90-second front door for success criterion 1. Convert a curious stranger (hiring manager) into a demo session with zero friction.
**Layout:** single column, generous air (the only marketing-DNA page). (1) Nav-lite: wordmark + "Sign in". (2) Hero: one-line thesis + subline + primary CTA **"View live demo"**, secondary "Sign in / I have an invite". (3) One real product screenshot: the Charts playground showing trigger triangles (static asset, updated manually). (4) Three-beat feature strip: Screener / Playground + Backtest / Binder — one sentence each. (5) Honesty line: "Built on N cards, M sales observed, updated continuously" (real counts). (6) Footer: About our data · Privacy · Terms · GitHub link (portfolio!).
**Data:** reads aggregate counts only — `count(cards)`, `count(sales)`, `count(price_months)`, latest `visits.fetched_at` for "updated Xh ago". No user data. Writes nothing.
**Content:** hero copy authored in §8.5. No pricing, no testimonials, no fake social proof.
**States:** effectively static; counts degrade to omission on query failure (never show 0). No empty/maturity states.

### 4.2 Auth — `/login`, `/register`, `/reset` — **P1**

**Purpose:** invite-only entry; minimal surface.
**Layout:** centered single card. Register: invite code + email + password (+confirm). Login: email + password. Reset: email → tokened link → new password. No email verification round-trip in v1 (invite code is the gate).
**Data:** new tables `users` (id, email, password_hash, created_at, theme_pref), `invites` (code, created_by, used_by, used_at), `password_resets` (token, user_id, expires_at). Writes on register/login/reset. Multi-tenancy rule: every user-facing table carries `user_id`.
**States:** error = inline field messages ("Invite code not recognized"), never toast-only. Demo users hitting `/register` see the invite explanation ("This is a private beta — registration needs an invite code").

### 4.3 Home — `/home` — **P1**

**Purpose:** the returning investor's first 10 seconds: *what's my stuff doing, and what fired since I left?*
**Layout (approved wireframe, §5b of the log):**
- **Market-index strip** (full width, one line): market index 30d %, Vintage / Modern segment %, "sets →" link. Sparkline optional.
- **Left column (~58%): Watchlist module.** Tabs per list + "new list". Virtualized table: card · tier · current price · 1M % · sparkline · tracked-signal chips (state-colored). Row click/Enter → peek panel. ⋯ menu: open chart, open card, move/remove.
- **Right column: Signals firing feed** — entries since last visit from (a) saved screens (card entered/exited screen) and (b) per-card tracked-signal threshold crossings. Each row: card, rule name, one-line evidence ("churn +140%, price flat"), timestamp. Click → peek panel. "All signals →" → Alert Center history.
- **Binder P&L card** (below feed): total value · unrealized ± · "vs index ±" one-liner → links to Binder performance.
- **Peek panel** (overlay, right column width): image (from `cards.image_hash` local store), name/set, current tier prices, mini chart with tracked signals, last 5 sales, pop Δ 60d, buttons: Open full chart · Open card page · Edit tracked signals (→ Charts).
**Data reads:** watchlists (`watchlists`, `watchlist_cards` incl. tracked-signal config JSON); latest prices per card via data-model §6 latest-per-tier over `price_months(card_id, tier, month, price_cents, observed_at)`; sparklines = trailing 12 `price_months` rows for the card's primary tier; signal states + feed from computed `signal_events` (new derived table, §8.2); binder valuation from `binder_transactions` × latest prices (index-estimate fallback badged); index strip from `indices` (derived); "data as of" from latest `visits.fetched_at` per card (never `cards.last_visited_at` — Rule 3).
**Writes:** watchlist membership edits; peek panel writes nothing.
**States:** *Empty (new user):* watchlist module shows "Add your first card — try the screener →"; signals feed runs on **preset screens** so it is alive on day one; binder card shows "Log your first purchase". *Loading:* skeleton rows, module-by-module (never a full-page spinner). *Error:* per-module inline retry. *Maturity:* signal chips honor Tier 2/3 badges; feed rows from immature signals are suppressed.

### 4.4 Screener — `/screener` — **P1**

**Purpose:** standing questions over the whole corpus; the discovery flagship.
**Layout:** left rail (saved screens: user's, then presets; "+ new screen"); toolbar (filter chips + add-filter combobox + sort + Terminal⇄Binder toggle + Save + Backtest + "Email me" alert button); main = virtualized QuickGrid, ~50 visible of up to thousands: card · set · tier price · chosen metric columns; bottom sufficiency row ("N hidden: insufficient data — show"). Binder view = image grid, same filters, value-weighted card sizing.
**Filter/metric vocabulary (full inventory — sufficiency flags gate per card, §1.5):** price (tier), 1/3/6/12M ROC, RS vs index (percentile), beta, distance-from-MA z-score, Bollinger %B/bandwidth, drawdown from peak, trend R², EMA cross state, MACD state, RSI, tier-spread ratio + compression, grading-arb EV, churn 30/90d + acceleration, monthly sales count, Amihud percentile, price dispersion, discount-to-list, cross-marketplace gap, pop Δ 30/60/90d, gem rate + drift, supply overhang, set, era, character, composite presets (Quiet Accumulation, Supply Flood, RS Breakout). Metrics compute from a nightly **`signal_snapshots`** derived table (§8.2) — the screener never scans raw facts at request time.
**Data reads:** `signal_snapshots` (computed); joins `cards(name, set_id, image_hash)`, `sets(name, slug)`, `card_characters`, `set_metadata(era, released_on)`. Sufficiency flags per metric ride in the snapshot rows. **Writes:** `saved_screens(user_id, name, thesis, filters_json, created_at)`.
**States:** *Empty results:* "No cards match — loosen a filter" + chip highlighting the most restrictive filter. *Loading:* virtualized skeleton. *Error:* inline. *Maturity:* per-column exclusion counts; column headers show a ⓘ linking the sufficiency rule ("churn requires 60+ days post-seam").

### 4.5 Backtest mode — `/screener/{id}/backtest` — **P1**

**Purpose:** "would this screen have made money?" — the interview wow-moment and the proof instrument.
**Layout:** header (screen name + thesis + filter chips, read-only); config row (entry rule = enters screen; horizons 3/6/12M toggle; date range with **honest floor** callout explaining the earliest valid date for *this* screen's metrics); Run. Results: (1) equity curve vs. market index (Lightweight Charts, normalized 100); (2) stat cards: entries count, hit rate per horizon, median/mean return, max drawdown; (3) outcome histogram; (4) entry table (card, entry date, price then, +3/6/12M %, exportable CSV). Fixed footnote: *"Returns are gross of selling fees."* Point-in-time note with a one-line explanation of why lookahead bias is structurally impossible here.
**Data reads:** historical `signal_snapshots` (or on-demand computation over `price_months`/`sales`/`populations` filtered by `observed_at`/`captured_at` ≤ entry date); `indices` history for the benchmark. **Writes:** `backtest_runs(user_id, screen_id, config_json, results_json, ran_at)` — cached, re-runnable.
**States:** *Empty:* screen has metrics whose honest window contains no entries → explain which metric constrains and to when. *Loading:* progress ("scanning 61 months…") — runs may take seconds on the Pi; async with a done-notification is acceptable. *Error:* partial-failure surfaces which month failed. *Maturity:* the date-floor callout IS the maturity state, permanently visible.

### 4.6 Charts playground — `/charts/{cardId?}` — **P1**

**Purpose:** the deepest page: form a thesis on one card, visually.
**Layout:** three zones. **Left panel (collapsible):** card identity (image thumb, name, set, tier selector chips for the six tiers); indicator list grouped (Trend / Momentum / Mean-reversion / Liquidity / Supply / Valuation), each with an on/off switch and inline numeric parameters; comparison section (add card via search picker, add set/market index; normalize toggle). **Main:** Lightweight Charts area — price pane (line/area per selected tiers) + up to 2 indicator sub-panes (MACD, RSI, churn, pop Δ); **trigger triangles** on the price series for rule-indicators; hover tooltip = rule, values, forward returns; range selector (1Y/3Y/All); per-pane "view as table" toggle (a11y P1 + copy-data). **Top bar:** card search swap, "Add to watchlist" (persists enabled indicator set as tracked signals → `watchlist_cards.signals_json`), "Save view", "as of Xh ago" stamp.
**Indicator inventory — ALL indicators ship; the Sufficiency Framework is the only gate** (each defined in §8.3; per-card/per-signal locks, countdowns, and confidence badges replace any staged rollout): *Trend/momentum:* ROC 1/3/6/12M · SMA/EMA 3/6/9 + crossover · MACD (3,6,4) · trend R² + slope. *Mean reversion/risk:* RSI (6) · z-score vs 6M MA · Bollinger (6,2) %B + bandwidth · drawdown from peak. *Relative:* RS vs market/set index · beta vs index (24M, locks until history suffices). *Liquidity (post-seam, per-bucket locks):* churn 30/90d · churn acceleration · monthly volume + count · Amihud illiquidity · within-bucket price dispersion · discount-to-list (rows with `listed_price_cents` only) · cross-marketplace gap (eBay vs auction houses, needs per-venue depth). *Supply (2026+, restatement-gated):* pop Δ 30/60/90d · gem rate + drift · supply overhang (pop ÷ annual sales). *Valuation:* tier-spread ratio + compression trend · grading-arb EV. *Corpus-level locked-until:* seasonality overlay ("unlocks after 3 observed cycles"). Tier-1 impossible indicators (candles, VWAP, OBV-over-full-history…) do not appear; the indicator panel footer links "Why no candlesticks? → About our data".
**Data reads:** `price_months` full history per tier (latest per (tier,month) by `observed_at`); `sales(sold_on, price_cents, grade_tier, source, listed_price_cents, title→encoded)` for sale-dot overlay + churn; per-bucket seam = `min(sold_on)` per `grade_tier` (derived) — **the seam renders as a vertical boundary marker** on sales-derived panes; `populations` + deltas (data-model §6 LAG query) with restatement-flagged spans hatched; `indices` for comparisons; `visits.fetched_at` for the stamp. **Writes:** `watchlist_cards`, `saved_views(user_id, card_id, config_json)`.
**States:** *Empty (no card):* recent cards + search prompt. *Loading:* chart skeleton; indicators compute client-side after series load. *Error:* per-pane. *Maturity:* Tier-2 locks with countdowns on sales/census indicators for young-seam cards; "Show anyway" burns the low-confidence badge into the pane; current (revising) month rendered as a hollow final point with tooltip "current month still revising".

### 4.7 Binder — `/binder` — **P1**

**Purpose:** private cost-basis truth + performance proof. The only substantial write surface.
**Layout:** three tabs. **Holdings:** table — card · tier · qty · avg cost · current value (badge "estimate" when index-estimated) · unrealized ± · % of binder; footer totals. **Transactions:** immutable ledger — date · buy/sell · card · tier · qty · price · note; "Add transaction" opens a focused modal (search picker, tier select, qty, price, date, note); corrections = void + re-enter (both rows visible, void struck through); CSV export. **Performance:** portfolio value chart vs. market index (normalized); stat cards: realized P&L, unrealized, win rate, avg hold time; yearly summary table.
**Iron rule (from Card Ladder's scars):** user-entered numbers never drift. Estimates move; what-you-paid never does. Estimated valuations always badged.
**Data reads:** `binder_transactions(user_id, card_id, kind, grade_tier, qty, price_cents, occurred_on, note, voided_by)`; latest tier prices from `price_months`; staleness check via last sale/`price_months` recency → index-estimate fallback using `indices` relative movement (badged). Benchmark from `indices`. **Writes:** `binder_transactions` inserts only (+ void marker inserts). Full CRUD semantics via append (mirrors data-model Rule 1 by *choice*, giving audit trail).
**States:** *Empty:* "Log your first purchase" + one-field-at-a-time modal. *Loading:* per-tab skeleton. *Error:* transaction save failures keep the modal open with values intact. *Maturity:* valuation badges ("estimate — no recent sales"); performance chart begins at first transaction date.

### 4.8 Browse landing — `/browse` — **P2**

**Purpose:** "show me what exists" — the wandering mode search can't serve.
**Layout:** mode switch **By set ⇄ By Pokémon**. *By set:* era shelves (WOTC, EX, DP, BW, XY, SM, SWSH, SV…), each a row of set tiles — fan of top-3 chase-card images, set name, card count, set-index 30d % → Set page. *By Pokémon:* species picker (search-as-you-type grid ordered by total market value of printings) → Character page.
**Data reads:** `set_metadata(era, released_on)` (new static) joined to `sets(name, slug)`; tile fan images = top cards by latest PSA-10 price (`price_months` + `cards.image_hash`); set indices from `indices`; species list from `card_characters` (new derived) aggregated by value. **Writes:** none.
**States:** *Maturity:* sets missing metadata fall into an "Uncategorized" shelf (curation TODO surfaces honestly). *Loading:* shelf skeletons.

### 4.9 Set page — `/set/{slug}` — **P2**

**Purpose:** one set as an investable universe.
**Layout:** header (name, era, release date, card count, set-index chart + 30/90d %, dominant-accent bar from top card's art); toolbar (sort: value / 3M ROC / RS / pop Δ; Terminal⇄Binder toggle); listing (table or grid) of the set's cards.
**Data reads:** `sets(slug→verbatim URL rule)`, `set_metadata`, `cards(set_id)` roster, `signal_snapshots` for sort metrics, `indices` (set), accent from `card_accents` (new derived). **Writes:** none.
**States:** standard; sufficiency exclusions per sort metric as in the screener. Note data-model TODO: `cards.set_id` can be stale if a card moved sets — accepted, invisible to users.

### 4.10 Character page — `/pokemon/{species}` — **P2**

**Purpose:** every printing of one Pokémon; the page collectors bookmark.
**Layout (approved mock):** header with **dominant-color accent** (bar + sparkline tint) from flagship card art; species name, printings count, sets count, character index 90d %; Terminal⇄Binder toggle (Binder default here); value-weighted image grid (flagship largest), each: art, tier price, 1M %; sort control; footer "as of" stamp.
**Data reads:** `card_characters(species)` → card list; `cards.image_hash` (local image store `{hash}/1600.jpg`, 325×450 portrait); latest prices (`price_months`); `indices` (character); `card_accents`. **Writes:** none.
**States:** species with one printing skip the grid ceremony and link the Card page prominently.

### 4.11 Card page — `/card/{id}` — **P1**

**Purpose:** the permanent record of one card; where the data model is most visible.
**Layout:** header (image at true ratio, name, `sets.name` link, current six-tier price strip, actions: Open in Charts · Add to watchlist · Add to binder). Sections: (1) **Price** — compact multi-tier chart (link to full playground). (2) **Sales ledger** — table: date · grade bucket (`grade_tier` label verbatim) · realized price · listed price (when present) · source (ebay/tcgplayer/goldin/heritage/pwcc) · title (**HTML-encoded**); **per-bucket seam marker row** ("reliable history for PSA 10 begins 2026-03-14"); filter by bucket. (3) **Population** — PSA/CGC grade histogram (current) + grading-activity delta chart (2026+), restatement spans hatched with tooltip. (4) **Data honesty strip** — "as of Xh ago"; if a recent cap incident is derivable ("some sales may have been missed during a hot streak" — from `sales.captured_at` clustering, *not* the Rule-3 flag), show it here.
**Data reads:** `cards(name, set_id, image_hash, url)`, `sets(name, slug)`, `price_months` (all tiers), `sales` (all fields; title encoded at render), `populations` + LAG deltas, seam = min(`sold_on`) per bucket, `visits.fetched_at` latest. **Writes:** watchlist/binder shortcuts only.
**States:** *Maturity is the star here:* young seams, censuses with 1 observation ("first observed 2026-07-30 — deltas begin next observation"), hollow current-month price point. *Empty sales bucket:* "No sales observed in this grade" (true statement — absence means observed-zero per Rule 2 + `visits` proof). *Error:* per-section.

### 4.12 Alert Center — `/alerts` — **P1 (rules) / P2 (history)**

**Purpose:** manage push; audit what fired.
**Layout:** Rules list (source screen/tracked signal · condition summary · delivery: digest/immediate · toggle · delete); delivery settings (digest hour, email); History (fired events, read/unread, deep links).
**Data:** `alert_rules(user_id, source_type, source_id, condition_json, delivery, active)`, `alert_events(rule_id, card_id, fired_at, payload_json, read_at)` (also feeds Home). Evaluation job runs post-crawl batches; emails via transactional service; one-click unsubscribe token per user.
**States:** empty = "Alerts come from saved screens and tracked signals" with links; email-failure banner if the send service errors.

### 4.13 Search results — `/search?q=` — **P1**

Grouped results: Characters · Sets · Cards (virtualized), each row with context + price. Reads a search projection over `cards.name`, `sets.name`, `card_characters` (pg_trgm for typo tolerance). Empty state suggests Browse.

### 4.14 Settings — `/settings` — **P2**

Theme (Light/Dark/System — writes `users.theme_pref`), email change, password change, alert digest hour, data export (binder CSV), sign out everywhere. Demo mode: read-only with nudge.

### 4.15 About our data — `/about-data` — **P1**

**Purpose:** the Tier-1 honesty page; converts limitations into credibility. Content authored in §8.6: what the six tiers are, why monthly (no candlesticks), the sales seam, why volume can't predate it, census start + restatements, "as of" stamps, and what we refuse to fake. Linked from indicator panel footer, sufficiency badges, and the footer everywhere.
**Data reads:** live corpus counts for flavor. Static otherwise.

### 4.16 Demo mode — cross-cutting — **P1**

A seeded read-only session (no registration): demo `user_id` with curated watchlists, saved screens, binder history, fired alerts. All writes intercepted with an inline "Demo mode — sign in with an invite to save" nudge (never a modal wall). Nav shows a slim "DEMO" tag. Seed content is real data chosen to make every module interesting on first paint; refreshed manually.


---

## 5. Component inventory

Blazor components, one directory per family. Every component consumes tokens (§6) only — zero raw colors — and states its keyboard behavior.

**Chrome & navigation**
- `TopNav` — fixed landmark: tabs, search box, bell (unread badge), account menu, theme quick-switch, DEMO tag slot.
- `OmniSearch` — `/`-focusable combobox; grouped async results; full ARIA combobox pattern.
- `PageFooter` — About our data · Privacy · Terms · "data as of" where page-global.
- `Breadcrumbs` — Browse hierarchy only.

**Data display**
- `PriceChart` — **the centerpiece**: Lightweight-Charts JS-interop wrapper. Props: series[] (tier/comparison), indicator configs, trigger-marker sets, seam boundaries, restatement spans, range, normalize flag, theme ramp. Emits hover/click. Includes `ChartTableFallback` ("view as table", P1) + one-line text summary slot.
- `Sparkline` — tiny SVG, 12 monthly points, no axes.
- `DataGrid` — QuickGrid wrapper: virtualization, sortable metric columns, sufficiency-exclusion footer row, row focus/↑↓/Enter contract, Terminal density.
- `CardImage` — local store `{hash}/1600.jpg`, 325×450 intrinsic ratio, lazy, alt = "{name} card art".
- `CardGridTile` — Binder-view unit: art, price, Δ%; value-weighted size variant.
- `StatCard` — label + big monospace number + delta.
- `IndexStrip` — Home's one-line market context.
- `PopHistogram` — PSA/CGC grade bars + delta series pane.
- `SalesTable` — ledger with seam-marker row; **encodes `title` on render** (single choke-point component — the XSS defense lives here and nowhere else).

**Signals & sufficiency**
- `SignalChip` — tracked-signal state: name + state color + icon (▲▼–), tooltip with evidence. Never color-alone.
- `SufficiencyLock` — Tier-2 lock: countdown copy + "Show anyway".
- `ConfidenceBadge` — Tier-3 badge; `burnIn` mode renders inside chart canvas.
- `AsOfStamp` — "data as of Xh ago" from `visits`; the anti-LIVE.
- `TriggerMarker` — triangle glyph spec shared by chart markers and legends (▲ gain-color below line, ▼ loss-color above).

**Input & forms**
- `FilterChipBar` + `FilterEditor` — metric/operator/value chips; add-combobox; the screener's grammar.
- `IndicatorPanel` — grouped switches + inline `NumericParam` fields (keyboard steppers).
- `CardPicker` — search-select for binder/compare flows.
- `TransactionModal` — focused, focus-trapped, values survive errors.
- `Toggle`, `Tabs`, `SegmentedControl` (Terminal⇄Binder, horizons), `Button` (primary/secondary/ghost/destructive), `TextField`, `Select`, `DateField` — standard set, semantic HTML underneath.

**Overlays & feedback**
- `PeekPanel` — slide-over (right column width); focus-trapped; ↑↓ walks source rows; Esc restores focus to origin row.
- `Modal` — rare; transactions and confirmations only.
- `Toast` — action confirmations; never the only error surface.
- `EmptyState` — icon-less, one sentence + one action (copy in §8.7).
- `SkeletonRow` / `SkeletonChart` — loading.
- `DemoNudge` — inline "sign in to save" ribbon.

---

## 6. Visual direction

### 6.1 Concept — "dense light terminal, card-warmed"

Chosen through samples A→D: **A's layout density + B's light palette** (TradingView/Koyfin genre), with the Browse/character surfaces letting card art supply color. Density signals professionalism; light default reads research-grade and lets 325×450 card art present like a gallery. Dark ships day one as an equal theme (the audience's 11pm mode), derived from sample C (charcoal-violet, never pure black). Rejections on record: no "LIVE" affordances, no candlestick cosplay, no consumer-app airiness on analysis surfaces.

### 6.2 Typography

| Role | Face | Usage |
|---|---|---|
| UI & body | **Inter** (variable) | everything conversational; 13px base on analysis surfaces, 14–15px on Browse/Landing; weights 400/500/600 only |
| Numerals & data | **JetBrains Mono** | every number that can be compared: prices, %, table cells, stat cards, axis labels. Tabular figures; the terminal voice |
| Display (Landing + page titles) | **Inter Tight** 600 | restrained; the landing hero is the only place type gets big |

Rationale: one superfamily + one mono keeps the portfolio codebase honest and the terminal feel consistent; the mono *is* the personality. Type scale: 11 / 12 / 13 / 15 / 18 / 24 / 34.

### 6.3 Color tokens (semantic only — zero raw colors in components)

| Token | Light | Dark | Notes |
|---|---|---|---|
| `surface-0` (page) | `#FAFAF7` | `#14131A` | warm white / charcoal-violet |
| `surface-1` (panel) | `#FFFFFF` | `#1B1A22` | |
| `surface-2` (raised) | `#F3F3EE` | `#232130` | |
| `border` | `#E4E4E0` | `#2A2833` | hairlines everywhere |
| `text-primary` | `#1C1C1E` | `#F0EEE9` | AA on all surfaces |
| `text-secondary` | `#5B5B57` | `#A9A6B4` | |
| `text-muted` | `#8A8A86` | `#8F8C9C` | |
| `accent` | `#3B5BD6` | `#8D7BD8` | links, focus, selection |
| `gain` | `#189E63` | `#4FD398` | always with +/▲ |
| `loss` | `#D64545` | `#E8695F` | always with −/▼ |
| `warning` | `#B07F1A` | `#DCAE5A` | sufficiency/estimate badges |
| `chart-grid` | `#F0F0EC` | `#221F2B` | |
| `chart-comparison` | `#7A95D6` | `#8D7BD8` | dashed |

Chart ramps are per-theme (dark ramp ≈ approved sample C). **Art accents** (`card_accents` derived column) tint character/set headers; dark theme applies a −20% saturation/−10% lightness transform so art color never fights legibility. AA contrast is verified at token-definition time for every text/surface pair — the a11y budget spent once.

### 6.4 Spacing, density, shape

4px base grid. Analysis surfaces: 8px cell padding, 28px row height, hairline dividers. Browse/Landing: spacing doubles. Radius: 6px controls / 8px panels / 4px card-art tiles (slab feel). Elevation: borders over shadows (one soft shadow level for overlays only). Focus: 2px `accent` outline, always visible — keyboard is a first-class citizen, and visible focus is part of the terminal aesthetic, not a compliance apology.

### 6.5 Imagery & iconography

The only imagery is **card art** — real product data, never decorative stock. Portrait 325×450 honored; no crops except the tile fan on Browse shelves. Icon set: Tabler (stroke, terminal-appropriate); triangles for direction everywhere (markers, deltas); no emoji in UI chrome.

### 6.6 Landing mini-spec

Same tokens, marketing breathing room: hero 34px Inter Tight, 1.5× section spacing, the one screenshot framed in `surface-1` with a hairline. No gradients, no glow. The product's honesty voice starts here: real counts, real screenshot.

### 6.7 Voice

Plain, precise, unapologetic. Errors say what happened and what to do. Sufficiency copy is factual, never cute ("Churn unlocks in ~19 days — sales history for this grade begins 2026-06-12"). The word "estimate" is never hidden. No exclamation marks anywhere in the terminal.

---

## 7. Responsive behavior and accessibility

### 7.1 Responsive (v1 = graceful degradation, not mobile design)

Breakpoints: **≥1280px** full multi-panel (design target) · **1024–1279** peek panel becomes full-height drawer; Charts left panel collapses to icons · **768–1023** Home stacks (index strip → signals → watchlist → binder); tables drop tertiary columns (sparkline, listed price); Charts becomes chart-first with indicator drawer · **<768** read-mostly: navigation, search, card pages, watchlist, binder *viewing* work; the playground and screener builder show "best on a larger screen" with a read-only snapshot rather than a broken layout. Mobile *app* remains v2.

### 7.2 Accessibility (the "union" ruling — screen-reader support + free baseline)

- **Semantic HTML everywhere:** real buttons/links/tables; no clickable divs. Landmarks: banner/nav/main/contentinfo per page.
- **Focus management:** peek panel and modals trap focus, Esc closes, focus returns to the origin row. Roving tabindex in grids.
- **Charts:** every chart pairs with (a) a one-line text summary ("PSA 10 up 18% over 3 months, above its 6-month average") and (b) the **"view as table" fallback — P1** — the screen-reader path and the copy-data power feature in one control.
- **Tables:** proper th/scope/caption; sort state announced via aria-sort.
- **Live regions:** signals feed and toasts announce politely; alert badge count is text, not color.
- **Color independence:** every gain/loss carries +/− or ▲/▼; sufficiency states pair icon + text; AA contrast enforced in the token table.
- **Keyboard:** the §3 map; `?` overlay documents it; all flows completable mouse-free.
- **Out of scope (declared):** per-datapoint chart ARIA beyond the table fallback; WCAG audit ceremony. Reduced-motion honored (peek slide → fade).

---

## 8. Open questions, assumptions, and authored content

### 8.1 Open questions (user decisions pending)

1. **Product name** — candidates in §8.8; pick or veto.
2. **"Portfolio" as the Binder performance tab's label** — proposed, unconfirmed.
3. **Concentration view** (% of binder in one card) — defaulted v2; confirm.
4. **"Data events" feed** (restatements, cap incidents as a feed) — undecided; v2 default.
5. **Demo seed curation** — which cards/screens make the demo sing (owner's pick, pre-launch).
6. **Digest default hour** for alert emails.

### 8.2 New schema (user-facing growth, sanctioned by the data model's Disclosure)

`users` · `invites` · `password_resets` · `watchlists(user_id, name, position)` · `watchlist_cards(watchlist_id, card_id, signals_json, added_at)` · `saved_screens(user_id, name, thesis, filters_json)` · `saved_views(user_id, card_id, config_json)` · `binder_transactions(user_id, card_id, kind, grade_tier, qty, price_cents, occurred_on, note, voided_by)` · `alert_rules` · `alert_events` · **derived/computed:** `signal_snapshots` (nightly per-card metric values + sufficiency flags) · `signal_events` (crossings; feeds Home + alerts) · `indices` (market/era/set/character, chained per-card monthly relatives, min-active-count guard) · `set_metadata(set_id, released_on, era)` (static, hand-curated) · `card_characters(card_id, species)` (derived from `cards.name` × species list) · `card_accents(card_id, hex)` (derived from stored images) · `backtest_runs`. All fact-table reads remain append-only and untouched — features build **on top of** the ledger, never by mutating it.

### 8.3 Indicator definitions (condensed from the signal research; parameters are defaults, all user-tunable)

**Staging policy (user ruling):** there is no v1/v2 indicator roadmap — **every honest indicator ships**, and the Sufficiency Framework (§1.5) does all gating per card/signal/date with locks, countdowns, and confidence badges. Implementation order is an engineering concern, not a product tier.

- **ROC n** = P_t/P_{t−n} − 1, per tier; n ∈ {1,3,6,12} months. 3M is the headline (card momentum evidence: ~5.6%/mo, Engelberg et al. 2020).
- **SMA/EMA (3/6/9) + crossover** — trend baseline; 3-over-9 EMA cross emits triggers.
- **MACD (3,6,4)** — monthly-tuned; crossover emits triggers.
- **RSI (6)** — wide bands 80/20; monthly RSI>70 often continuation, tooltip says so.
- **Bollinger (6,2)** — %B position + bandwidth as volatility proxy; visualization-grade, weak as a trigger (tooltip says so).
- **Z-score vs 6M MA** — stretch detector; |z|>1.5 emits.
- **Drawdown from trailing peak** — risk/value filter.
- **Trend R² + slope** (6–12M log-price regression) — clean-trend detector.
- **RS vs index** — card return minus market/set index return, percentile-ranked; the product's thesis metric.
- **Beta vs index** — 24M regression; locks until history suffices, unstable-fit badge on thin cards.
- **Tier-spread ratio** — PSA10/Ungraded price ratio + compression trend.
- **Grading-arb EV** — gemrate×P₁₀ + (1−gemrate)×P₉ − P_raw − fees; census gem rate badged as prior.
- **Churn 30/90d + acceleration + monthly volume** — post-seam only, per bucket; seam rendered as a boundary on every sales-derived pane.
- **Amihud illiquidity** — |monthly return| ÷ monthly dollar volume, percentile within set; undefined in zero-sale months (shown as gaps, not zeros).
- **Within-bucket dispersion** — σ/μ of realized prices, trailing window; pricing-uncertainty gauge.
- **Discount-to-list** — 1 − realized/listed on rows carrying `listed_price_cents`; sparse-data badge standard; auction-format rows filtered by `source`.
- **Cross-marketplace gap** — mean realized price eBay vs goldin/heritage/pwcc, same card+bucket; locks until each venue has minimum depth.
- **Pop Δ (30/60/90d) + gem rate + drift** — 2026+ only; restatement spans suppressed and hatched.
- **Supply overhang** — graded pop ÷ trailing-annual sales count = years of supply at current absorption; needs both substrates mature (double lock).
- **Seasonality overlay** — corpus-level lock: "unlocks after 3 observed cycles (~Nov 2028)."
- **Composites (screen presets, not chart overlays):** Quiet Accumulation (churn accel + flat price + flat pop) · Supply Flood (pop growth + spread compression + weak price) · RS Breakout (RS top-decile + high R² + modest drawdown).

### 8.4 Preset screens (day-one content)

1. **3M RS Leaders** — RS-vs-index ≥ 90th pct, 3M. *Thesis: relative momentum persists in cards.*
2. **Quiet Accumulation** — churn 30d ≥ 2× churn 90d · |ROC 1M| < 3% · pop Δ 60d ≤ 1%. *Attention building before price.*
3. **Supply Flood Watch** — pop Δ 60d ≥ 5% · tier-spread compressing · ROC 3M ≤ 0. *Avoid list.*
4. **Grading Arbitrage** — arb EV > $0 · Ungraded ≥ $40 · churn sufficient to exit. *Raw cards worth grading.*
5. **Blue Chips on Sale** — top-decile PSA-10 value · drawdown 15–40% · RS stabilizing. *Quality dips.*
6. **Under $100 Movers** — price ≤ $100 · ROC 3M ≥ 15% · sales count ≥ 5/mo. *Liquid small-caps.*

### 8.5 Landing copy (draft)

**Hero:** "Technical analysis for Pokémon cards." **Sub:** "Five years of price history, every sale we've ever seen, and the grading census — charted, screened, and backtested like the asset class it's become." **CTA:** View live demo · *I have an invite.* **Feature beats:** "Screen 100,000 cards by momentum, supply, and value." / "Tune real indicators and see every trigger they'd have fired." / "Track your binder against the market, to the cent." **Honesty line:** "{cards} cards · {sales} sales observed · updated {x}h ago."

### 8.6 About-our-data page (outline; full draft on approval)

Where the data comes from → the three substrates and their epochs → why monthly means no candlesticks (and why we won't fake them) → the sales seam, per card, drawn on your charts → census starts when we started looking; restatements happen and we flag them → "as of" stamps instead of "LIVE" → what we deliberately leave out (order books, news, real-time) → the append-only promise.

### 8.7 Empty-state copy (selected)

Watchlist: "Nothing watched yet. Find a candidate in the Screener →" · Binder: "Log your first purchase — 30 seconds, and your P&L starts here." · Screener no-results: "No cards match. Your tightest filter is {chip} — try loosening it." · Alerts: "Alerts come from saved screens and tracked signals. Save one and it can email you."

### 8.8 Name candidates

1. **Cardstock** *(recommended — the pun is the pitch: cards as stocks, and the paper they're printed on; clean domainable compounds: cardstock.app, usecardstock.com)*
2. **Holodex** *(holo + index + Pokédex; collision: an existing VTuber site — fine for a hobby, weak for a portfolio)*
3. **Pullback** *(market pullbacks + card pulls; wry, finance-native)*
4. **Slabline** *(graded slabs + trendline; terminal-flavored)*
5. **Basecase** *(Base Set + the analyst's "base case")*

### 8.9 Assumptions of record

- Latest-value semantics follow data-model §6 (`ORDER BY observed_at DESC`); absence of census rows = observed zero, and screens may say so plainly.
- The nightly `signal_snapshots` job is the performance strategy; the UI never scans raw fact tables per request.
- Cap incidents surface via `sales.captured_at` clustering (derived), not the Rule-3 mutable flag; wording stays soft ("some sales may have been missed") because flip history isn't durably stored (data-model §8 known gap).
- Slugs build URLs verbatim (double-encoding bug on record). Images serve from the local store, never hot-linked.
- Backtests are gross of fees (footnoted); demo mode is a real user row with writes intercepted at the service layer.
- Set-metadata curation (era/release) is a dev-time task; "Uncategorized" shelf is the honest fallback until complete.

---

*End of specification v1.0 (approved). Open items in §8.1 are decisions the owner will make during design/build; everything else is settled.*
