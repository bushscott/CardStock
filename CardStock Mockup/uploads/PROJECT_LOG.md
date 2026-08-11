# Project Log — Pokémon Card Investment Platform
**Living document.** Updated after every interview exchange. Source of truth for Stage 2 spec.
Last updated: 2026-07-31 (Stage 1, branch 1 complete)

---

## 00. ORIGINAL BRIEF — standing rules for this engagement (verbatim, do not drift)

**Role:** Senior product designer and UX strategist.
**Project:** A full custom website product (not a simple landing page). Deliverable: a complete UI design/spec document.

**Data-model obligations (standing, every stage):**
- Every screen proposed must account for how data is displayed, collected, or edited.
- **Challenge the user** if they describe a feature the data model doesn't support.
- **Flag any data collected that no screen currently surfaces.** (Running list in §7.)

**STAGE 1 — INTERVIEW FIRST. Do not start designing.**
- Interrogate thoroughly before proposing anything.
- **Ask ONE question at a time** (rule 9) — questions may lead to tangents; maintain the **question tree** and explicitly return to unvisited branches so nothing is lost.
- **Discussion-based, open-ended** (rule 10) — invite expansion and elaboration; not a Q&A checklist.
- Follow up on anything vague or contradictory. **Push back when answers are fuzzy.**
- Minimum coverage: what the product does & core problem · target users (who/context/skill) · 3–7 critical tasks/flows · every page/screen cross-referenced against the data model · content have vs. need · competitors liked/disliked and why · brand (logo, colors, fonts, voice) · constraints (stack, mobile, a11y, timeline, budget) · success criteria for the business.
- Continue until **every design decision could be defended** with something the user said or something in the data model. **Then say so explicitly.**

**STAGE 2 — SPEC DOCUMENT.** Must contain:
1. Product summary and design goals
2. Target users and primary user flows (step by step)
3. Site map (every page, hierarchy, navigation model)
4. Page-by-page spec: purpose, sections, components, **specific data model entities/fields each screen reads or writes**, content, states (empty/loading/error), priority
5. Component inventory (buttons, cards, forms, nav, etc.)
6. Visual direction: typography, color palette, spacing, imagery style — **with rationale**
7. Responsive behavior and accessibility notes
8. Open questions and assumptions

**Then:** ask the user to review and revise. **Only after approval**, export the final spec as a downloadable document suitable for upload to Claude Design.

---

## 00. STANDING INSTRUCTIONS FROM THE USER — do not drift from these
*Captured from the opening brief + running additions. Re-read before every response.*

**My role:** Senior product designer and UX strategist. Building a **full custom website product**, not a landing page. Deliverable is a complete UI design/spec document.

**Data model discipline:**
- Every screen proposed must account for how data is **displayed, collected, or edited**.
- **Challenge the user** if they describe a feature DATA_MODEL.md doesn't support.
- **Flag any data collected that no screen surfaces** (running list in §7).

**STAGE 1 — INTERVIEW FIRST. Do not start designing.**
- ❗ **Ask ONE question at a time** (user override of the original "3–5 batches" — tangents are expected and must not lose the thread).
- ❗ **Maintain the question tree**; explicitly return to unvisited branches (§6).
- ❗ **Discussion-based, open-ended questions** — invite expansion and elaboration. This is a conversation, not a Q&A form.
- Push back when answers are fuzzy. Follow up on vague or contradictory answers.
- Must cover: product/problem · target users · 3–7 critical flows · every page cross-referenced to the data model · content have-vs-need · competitors liked/disliked · brand (logo, colors, fonts, voice) · constraints (stack, mobile, a11y, timeline, budget) · success criteria.
- **Continue until every design decision is defensible** from something the user said or something in the data model. Then say so explicitly.

**STAGE 2 — SPEC DOCUMENT.** Must contain, in order:
1. Product summary and design goals
2. Target users and primary user flows (step by step)
3. Site map (every page, hierarchy, navigation model)
4. Page-by-page spec: purpose · sections · components · **specific data model entities/fields each screen reads or writes** · content · states (empty/loading/error **+ data-maturity**) · priority
5. Component inventory (buttons, cards, forms, nav, etc.)
6. Visual direction: typography, color palette, spacing, imagery style, **with rationale**
7. Responsive behavior and accessibility notes
8. Open questions and assumptions

**Then:** ask the user to review and revise. **Only after approval**, export the final spec as a downloadable document suitable for upload to **Claude Design**.

**Housekeeping:** Persist decisions to this file as we go (no cross-conversation memory is enabled; this file is the durable record).

**⚠️ Interview style override (user request):** User self-identifies as *not creative* and wants **options with a recommendation**, not open-ended "what do you want?" questions. Lead every question with 2–4 concrete, named options + a recommendation + rationale. User reacts and selects. Still one question at a time, still discussion-based.

**Reference products (user-named):** Webull (primary), Robinhood. Take inspiration from these.

---

## 0. Naming

| Term | Meaning | Status |
|---|---|---|
| **Binder** | The user's personal collection: what they own, what they paid | ✅ approved (replaces "Heartbeat" — rejected, collides with ops/uptime terminology) |
| **Portfolio** | Analytics/P&L view over the Binder | proposed, not confirmed |
| ~~Heartbeat~~ | — | ❌ killed |
| Product name | — | ⬜ OPEN (working repo name: PokemonInvestBatch) |

---

## 1. Product definition

**Archetype:** A "stock-trading-like" platform for Pokémon cards. Explicit reference: **webull.com** — parameters and charting are *visual and upfront*.

**Three pillars:**
1. **Screener** — rank/filter the corpus (~100k cards, ~303 sets) to surface cards likely to trend up faster than the market average.
2. **Indicator playground** — users compose and tune signals/theories themselves, not just consume presets. Tailored to collectibles data, not copied from equities.
3. **Binder** — private collection tracking with cost basis, gains/losses.

**Core user goal (user's words):** find cards that will "trend upwards faster than the average market."

**Design tension to respect:** Webull's visual language assumes daily/intraday OHLC + volume. This data has neither. Indicators must be honestly re-tuned for collectibles, and the UI must make each substrate's epoch visible.

---

## 2. Users & access

- **Multi-user**, open **free signup**, **email + password** auth. ✅ decided
- **Binder is strictly private.** No social layer, no public profiles, no leaderboards, no shared/forkable strategies. ✅ decided

### Primary persona ✅ decided
**The finance-fluent card investor.** Treats Pokémon cards as an investment vehicle; wants help identifying where to invest.
User's own framing: *"The overlap of day trader finance bros and the collectible market is bigger than you think."*
Already fluent in technical analysis. Tuning parameters to compare results is a **finance-fluent behavior** — it is the core desired activity, not an advanced edge case.

**Design consequences (binding):**
- Indicators ship under **real names** (MACD, RSI, EMA, relative strength) — NOT card-native euphemisms.
- Parameters are **numeric inputs exposed by default**, not hidden behind an "advanced" door.
- **Density over hand-holding.** Multi-panel charts, tight tables. No tutorial overlays on primary surfaces.
- **Teaching burden is on OUR DATA'S WEIRDNESS, not on finance concepts.** They know RSI; they don't know about monthly-avg-no-OHLC, per-card seams, or population census as a supply signal with no equity analog. This is what Tier-1 "omit + explain" exists for.
- **Deliberately underserved:** the finance-naive collector. Not blocked, not designed for.

⬜ OPEN (raised, not yet answered): finance-fluent users who tune parameters will want to know *whether the tuning worked* → **backtesting**. Real design question; revisit at branch 3 (flows).
⬜ OPEN: acquisition mindset — already-frustrated card investors vs. collectors converting to investors (asked, not yet answered).

**New entities required (do NOT exist in DATA_MODEL.md):** users, sessions, password-reset tokens, email verification, watchlists, saved screener/indicator configs, binder holdings.
The data model's §Disclosure explicitly anticipates "user-facing entities (accounts, watchlists, valuations, annotations)" — this is sanctioned growth, not a fight with the schema.

**Critical architectural note for the spec:** Binder rows are the *only write surface* in a product that is otherwise read-only over immutable facts. Rule 1 (append-only, no DELETE grant) must NOT be applied to user data — users need full CRUD on their own holdings.

---

## 3. Data Sufficiency Framework ✅ APPROVED — global pattern, referenced by every screen

Three tiers, by *why* a tool falls short:

| Tier | Condition | UI treatment |
|---|---|---|
| **1 — Permanently impossible** | Data never exists at source (candlesticks/OHLC, VWAP, intraday, true volume pre-seam, pre-2026 pop history, order book) | **Omit entirely from the app.** No grayed-out controls. One "about our data" explainer covers the why. Rationale: a disabled control that never enables is a broken promise that erodes trust in every other disabled control. |
| **2 — Not yet sufficient** | Data will mature over time (card 40 days post-seam, <n monthly points, restatement window) | **Locked/grayed with unlock condition as a countdown** ("Churn (30d) — unlocks in ~19 days"). Power-user "show anyway" override → renders with a **persistent** low-confidence badge burned into the chart region (survives screenshots). |
| **3 — Sufficient but weakened** | Thin trading, current revising month, high dispersion | **Full access + confidence badge.** No friction. |

**Screener corollary:** In ranked columns, insufficient-data cards are **excluded by default**, with a visible expandable "N cards hidden: insufficient data" count. Badges don't scale to 500-row tables; exclusion prevents 3-sale cards dominating a churn leaderboard on pure noise.

**Design consequence — the fourth state:** Every data-bearing screen has states empty / loading / error **+ data-maturity state** (day-1 sparse vs. year-2 rich — same layout, different confidence). Sufficiency badges double as *progress indicators*: "the past is frozen, the future compounds."

---

## 4a. Platform constraints ✅ decided
- **Desktop-first web application.** Keyboard + mouse optimized. Dense multi-panel layouts are acceptable and desirable.
- **Mobile is v2, explicitly OUT OF SCOPE for this spec.** (Spec will still note responsive/degradation behavior per the Stage 2 requirement, but v1 designs target desktop viewports.)

---

## 4. Data reality (from DATA_MODEL.md — binding constraints)

**Three substrates:**
- **S1 — Monthly price**, 6 grade tiers (Ungraded, G7, G8, G9, G9.5, PSA10), monthly avg, back to ~Dec 2020 (~68 pts). **Deep and uniform.** Carries most of the undervaluation signal.
- **S2 — Sales ledger**, per-sale immutable rows. **Per-card, per-grade-bucket SEAM.** Reliable only forward of the seam. Source shows only ~30 newest rows/bucket.
- **S3 — Population census**, PSA + CGC, grades 1–10. **Forward-only from our first observation (2026+).** Restatement-prone (flagged).

**Frozen vs. compounding:**
- *Frozen forever:* pre-seam sales volume, rolled-off sales, pre-2026 census, monthly resolution (no OHLC — ever).
- *Compounds daily:* forward sales ledger, forward census, monthly price series.

**Other constraints:** money = integer cents USD. `sales.title` is raw third-party text — **must HTML-encode at render** (XSS is the render layer's job, by design). Slugs are verbatim-encoded, build URLs untouched. Images at `{hash}/1600.jpg`, 325×450.

**Mutable scheduler state (Rule 3) — never display as fact:** `cards.last_visited_at`, `observed_sales_per_day`, `any_bucket_at_cap`, `failure_streak`, `quarantined_until`, `sets.last_*`.

---

## 5. Signal inventory
Full research artifact produced (29 signals, 7 categories, v1/v2/v3 staged). Key v1 backbone:
relative strength vs index · 3M/6M ROC · grading-arbitrage EV · pop-vs-price divergence · corpus/set index · pop delta · short EMA cross · churn & volume (post-seam) · composites (RS breakout, quiet accumulation, supply-flood warning) · tier-spread compression · trend R² · drawdown.
Anchor evidence: Engelberg/Thompson/Williams 2020 (3-month card momentum 5.6%/mo); Amihud 2002 (illiquidity).

---

## 5b. Home page architecture ✅ decided (composite "A + C's signals")
Evaluated three options via wireframes: A watchlist-first (Webull), B screener-first, C discovery dashboard.
**Decision: composite.** User loved A's personal watchlist-first feel + C's plain list of firing signals.
- **Slim market-index strip** across the top (index %, vintage/modern, link to sets) — context, not a module.
- **Watchlist = dominant module**, left, largest (tabs for multiple lists, sparklines, 1M change).
- **Signals firing feed**, right column — cards that crossed the user's saved screens' thresholds. This is the daily-return engine AND the inlet that feeds the watchlist.
- **Binder P&L summary card** below signals (value + unrealized gain).
- **Full screener = nav tab**, not home. Nav constant: Home / Screener / Charts / Binder + search, alerts (bell), account.
- **Zero state:** preset screens power the signals feed from day one; empty watchlist ≠ dead home.
- A's identified weaknesses (no discovery, no return engine, no market context, day-one emptiness) are each patched by a specific composite element — defensible.

## 5c. V1 scope rulings ✅ (branch 3 closed)

**IN v1:**
1. **Email alerts** — screens + per-card tracked signals push to inbox. New machinery: per-user alert rules, evaluation runs, email delivery, unsubscribe. Bell icon = alert center.
2. **Backtesting** — "if I'd bought every card the day it entered this screen, what happened 3/6/12mo later?" Honest per substrate: S1 screens back to ~2021; S2/S3 signals only post-seam/post-2026. Append-only ledger gives true point-in-time correctness (no lookahead bias) — flagship-grade differentiator. Results must footnote "gross of selling fees."
3. **Binder transactions** — buy AND sell rows (not just holdings): realized + unrealized P&L, win rate, hold time, yearly summary, CSV export.
4. **Binder vs. benchmark** — portfolio return vs corpus/set index (uses signal F1).
5. **Comparison charting** — overlay multiple cards + indices on one chart, normalized to 100.
6. **Search as a designed feature** — typo-tolerant autocomplete over 100k cards w/ set+grade context; present on every screen.

**OUT / rejected:**
- Fee-adjusted valuation toggle ("net of fees" on Binder) — ❌ rejected outright (only survives as the backtest footnote above).
- Paper trading / hypothetical binder — v2 at earliest.
- Concentration view — not ruled; default v2. ⬜ confirm later.
- Order execution, real-time quotes, bid/ask depth, news feed — Tier 1 omit (buying happens on eBay etc.; no news source). Closest analog: possible "data events" feed (restatements, cap incidents) — ⬜ undecided.

## 5d. The 7 critical user flows ✅ (satisfies the 3–7 requirement)
1. **Search** → find any card fast, from anywhere.
2. **Research** → Charts playground: tune indicators on a card, overlay comparisons, form a thesis.
3. **Watch** → save card + its enabled indicator set to a watchlist; triage via peek panel on Home.
4. **Discover** → build/run/save screens over the corpus; presets for day one.
5. **Backtest** → validate a screen/strategy against history.
6. **Get alerted** → signals feed at login + email when away.
7. **Track** → log buys/sells in Binder; realized/unrealized P&L vs benchmark.

## 5e. Site map ✅ (branch 4 closed)
**Nav tabs:** Home (§5b) · Screener (filter builder, saved-screens rail, ranked table, backtest as a *mode of a saved screen* — not its own tab) · Charts (indicator playground; save-to-watchlist) · Binder (holdings + buy/sell transactions, realized/unrealized P&L, vs-benchmark, yearly summary, CSV export) · **Browse**.
**Browse** ✅: two modes only — **By set** (shelves grouped by era, tiles show card count + set-index 30d move → Set page) and **By Pokémon** (species picker → character page: all printings as image grid by value + character index chart). "Pick a year" scrapped — era grouping covers it. Browse/character pages are where card images earn their keep; product feels like a collection here, terminal elsewhere.
**Supporting:** Card page (image, six-tier prices, sales table w/ seam markers + `source` + `listed_price` columns, pop census/deltas, honesty badges) · Set page · Character page · Alert center · Search results.
**Shell:** signup/login/verify/reset · account settings · "About our data" explainer · terms + privacy.
**Search:** omnisearch matches cards + sets + characters; character page can be a top hit.
**NEW STATIC DATA REQUIRED (non-scraped, one-time):** (1) set metadata table — release date + era/series for ~303 sets; (2) character tag table — card→Pokémon derived from `cards.name` × Pokédex species list.
**Unsurfaced-data resolutions:** `sales.source` + `listed_price_cents` → columns on card-page sales table ✅; `visits`/`shapes`/`parse_failures` stay ops-only ✅ (user agreed, no user value).

## 5f. Content inventory ✅ (branch 5 closed)
**Nothing exists.** No name, domain, logo, colors, or copy. Non-commercial hobby project ("not selling this"); user unconcerned with legal — spec still includes a bare-bones privacy/ToS page since we collect emails + passwords.
**Division of labor:** Claude authors ALL authorable content in Stage 2 — name candidates, brand direction options, indicator descriptions (research artifact = 80% source), preset screen definitions + theses, About-our-data page, empty states, email templates. User only picks between presented options. Set-metadata curation = dev-time task, semi-automatable.

## 5g. References & visual direction ✅ (branch 6 + branch 7 visual half closed)

**References:** Webull = charting inspiration — specifically applying indicators/theories to historical data *visually*, seeing where rules would have fired. Card Ladder = "lite version of the vision"; steal index-based value estimation for illiquid holdings (badged as estimate), daily-recap instinct; their cost-basis-drift bug (App Store complaints) → our iron rule: **user-entered transactions are immutable; cost basis never drifts. Estimates move, what-you-paid never does.** PriceCharting = anti-reference ("looks like 1997"); thesis = *their data, a terminal Webull users respect.*

**Trigger markers ✅ (core Charts interaction):** every indicator config emits historical buy/sell trigger points rendered as **triangles on the price chart** (no text labels, tooltip on hover). Tune parameters → arrows move → eyeball validity. Backtest = same arrows counted & scored. Bridge between playground and backtesting.

**Visual direction ✅ = "D: dense light terminal"** (A's layout density + B's palette; TradingView/Koyfin genre). Density = professionalism; palette = mood.
- **Light AND dark ship in v1.** Light default; toggle in settings + nav quick-switch + match-system. Mandatory discipline: **zero raw colors — semantic tokens only** (surface, text-primary, gain, loss, warning, accent), palette table has light+dark columns. Per-theme tuned chart ramps (dark ramp ≈ sample C minus pure black). Art accents get dimmed dark variants.
- **NO "LIVE" indicators ever** (data is batch-crawled). Replacement: "data as of Xh ago" stamps (derived from visits) on every data surface.
- **Terminal/Binder view toggle** on listing surfaces (set, character, screener results): dense table ⇄ card-image grid.
- **Dominant-color extraction** from card art → character/set page header accents. NEW derived column on cards (computed from stored images).
- Browse = gallery face (card art at portrait ratio, value-weighted sizing, terminal typography for numbers); analysis surfaces = pure terminal.
- Browse landing stays 2 modes (sets/Pokémon); other slices (blue chips, hot-this-month, under-$100) = preset screens opened in Binder view.

## 5h. Constraints (branch 8, in progress)
- **Frontend: Blazor — LOCKED.** Flavor **LOCKED ✅: Blazor Web App, Interactive Server** (components → services → Postgres directly; no API for own UI).
- **API design: OUT OF SCOPE** for this conversation. Final UI drives API decisions. ✅ user ruling.
- **Charting LOCKED ✅: TradingView Lightweight Charts** via JS interop (series markers = trigger triangles built-in; v5 panes for MACD/RSI). Blazor wrapper component = portfolio centerpiece. Screener grid: QuickGrid + virtualization.
- **Hosting: Pi fleet, accepted** (16GB quad-core, multiple units, ~1 concurrent user — overprovisioned, zero perf concern). Ops checklist (non-UI): Postgres backups now also cover irreplaceable *user* data (binder transactions join sales/pop-history in the can't-recreate category); TLS via Caddy/CF tunnel; **email via transactional service free tier** (residential IP can't send) → shapes alert design: batch digests > per-event sends.
- **This is a PORTFOLIO PIECE** → new success criterion for branch 9: the design must showcase advanced Blazor skill (interop charts, dense virtualized grids), not CRUD.
- Desktop-first ✅ (earlier); mobile app = v2 ✅.
- **Accessibility LOCKED ✅ (the union, ruled by user):** screen-reader support IN — semantic HTML (real buttons/links, no clickable divs), ARIA labels/landmarks, focus management (peek panel + modals trap & restore focus), labeled tables, **every chart gets "view as table" fallback = P1** (it's the SR path AND the copy-data power feature) + one-line text summary. PLUS the free baseline: never color alone (+/− signs, triangle direction), keyboard access to everything, AA contrast enforced in token palette. OUT of scope: exotic per-datapoint chart ARIA.
- **Timeline ✅: "done when done."** No calendar cuts; P1/P2 markers order the build, not deadlines.
- **⚠ LOG MAINTENANCE LESSON:** user sometimes branches the conversation and changes past answers — the file keeps the stale branch. When a logged ruling surprises either party, verify with the user; user is the only non-degrading source of truth.

## 5i. Success criteria ✅ (branch 9 closed) — THE LENS FOR ALL PRIORITIES
Ranked by user: **(1) Interview/portfolio talking point** — hiring manager clicks around 90 seconds and is impressed. **(2) Personal profit** — find cards via own signals, beat the market, Binder proves it. **(3) Someday sell it as a tool** — deferred until proven; "plan accounts upfront."

**Resulting rulings ✅:**
- **Open public signup REVERSED → invite-only.** Registration behind an invite code (friends only); no verification emails; minimal password reset. Schema stays **multi-tenant from day one** (`user_id` on every user-facing table) so "sell it someday" = config change, not rewrite. (Supersedes branch-1b open-signup decision; §2 updated in spirit.)
- **Read-only DEMO MODE — P1, justified by criterion 1.** "View live demo" button → pre-loaded account (real watchlists, seeded binder, saved screens with firing signals), no registration, writes disabled with polite nudge. The hiring-manager path.
- **Backtest + Binder-vs-benchmark = the product's emotional center** — simultaneously the interview wow-moment, the profit instrument, and the "prove it works" evidence. All three criteria converge here.
- **Landing page (logged-out `/`) added to site map — P1.** Hero one-liner, one real screenshot (Charts playground w/ trigger arrows), three-beat feature strip (screener / playground+backtest / binder), primary CTA = View live demo, secondary = login/invite. No pricing/testimonials/hype. Only page with marketing DNA; own mini-section in visual spec (larger type, more air, same brand).

## 6. Question tree — progress

```
1. Product & problem                          ✅ COMPLETE
   1a. What "undervalued" means               ✅ (Webull framing + signal research)
   1b. Multi-user vs personal                 ✅ (multi-user, free signup, email+pw)
   1c. Binder private vs social               ✅ (strictly private)
   1d. Product name                           ⬜ OPEN
2. Target users & skill level                 ✅ COMPLETE (finance-fluent card investor)
3. Critical tasks/flows (3–7)                 ✅ COMPLETE (§5d; home arch §5b pending Claude Design polish only)
4. Pages/screens ↔ data model cross-ref       ✅ COMPLETE (§5e)
5. Content: have vs. need                     ✅ COMPLETE (§5f — all authored by Claude)
6. Competitors & references                   ✅ COMPLETE (§5g)
7. Brand & voice                              ◐ visual direction ✅ (§5g) · naming ⬜ (Stage 2 candidates)
8. Constraints (stack, mobile, a11y, budget)  ✅ COMPLETE (§5h)
9. Success criteria                           ✅ COMPLETE (§5i)

**🏁 STAGE 1 INTERVIEW COMPLETE — all branches closed. Remaining open items are Stage 2 deliverables by design: product name candidates (user picks), concentration view v2 confirm, "data events" feed decision.**
```

---

## 8. Parked features (user: "back pocket" — not in v1 spec, do not lose)
- **On-demand card refresh** (raised during Stage 2 review): visiting a card page queues a priority crawl of that card; "as of" stamp goes live ("updating… → updated just now") over the Blazor circuit. Design constraints already worked out: must be async (politeness gate makes sync refresh a lie), user-request tier slots BELOW burn-window in the scheduler (never risk the zero-missed-sales guarantee), one pending request/card + per-user cap + quarantine respect, freshness threshold can scale with churn. Needs `visit_requests` table + a scheduler tier — a WORKER change, possibly via integrating scraper code into the web app. Revisit after v1 spec approval.

## 7. Flagged for spec: data collected but not yet surfaced by any screen
*(Running list — the "flag data no screen surfaces" obligation)*
- `visits`, `shapes`, `parse_failures` — operational only; may justify an internal-only ops screen (NOT user-facing).
- `sales.listed_price_cents` — discount-to-list signal (v2), mostly sparse.
- `sales.source` (ebay/tcgplayer/goldin/heritage/pwcc) — cross-marketplace divergence (v2).
- `sales.captured_at` — per-visit ingestion batches; likely never user-facing.
- `cards.any_bucket_at_cap` — "sales were missed" flag; candidate for a data-honesty badge on card pages.
- `sets.last_walked_at`, `cards.image_fetched_at` — bookkeeping, likely never user-facing.

---

## 9. STAGE 2 COMPLETE ✅ — spec v1.0 APPROVED (2026-08-01)
- Review revisions incorporated: indicator staging dissolved (ALL honest indicators ship; Sufficiency Framework is the only gate); Tier-1 impossible list confirmed OUT by user.
- Parked during review: on-demand card refresh (§8 above).
- Still open by design (owner decides later): product name (Cardstock = working title/recommendation), Binder "Portfolio" tab label, concentration view, data-events feed, demo seed curation, digest hour.
- **Exported:** CARDSTOCK_UI_SPEC_v1.md (with Claude Design handoff note). Pipeline: Claude Design (visuals) → Claude Code (Blazor build), spec is source of truth.
