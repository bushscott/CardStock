# Cardstock — Engineering handoff

Everything an engineer needs to build Cardstock, and where to find it. Prototypes are the source of truth for layout, states, and copy; the documents below carry the rules the prototypes can only imply.

Last updated 2026-08-10.

---

> ## ⚠ Provenance — read before relying on any number here
>
> This document was written by Claude Design, which built the prototypes but **never had access to the database**. It is **first-hand about the design** — screens, routes, spec deltas, conventions — and **second-hand about the data**. Every data claim in it was relayed, not observed.
>
> **The rule:** trust this document where it describes itself. Verify it against `../../PokemonInvestBatch` wherever it describes the data. §5 is the section to distrust; §2, §3, §6 and §7 describe the prototypes and are self-verifying — open the HTML.
>
> **Errors confirmed 2026-08-10, every one a data claim:**
>
> | Claim | Verdict |
> |---|---|
> | §1, §5 — per-sale ledger Apr 2025, census Jan 2026 | **False.** Both begin at each card's first visit, late Jul 2026. Corrected in place; see the note under §5 |
> | §5 — listed prices "~12% of rows" | **Contradicted.** `DESIGN_NOTES.md:46` measured 4.4% — 143,062 of 3,265,910 sales |
> | §5 — venue depth "eBay-only today" | **Contradicted.** `DATA_MODEL.md:102,:227` document five sources: ebay, tcgplayer, goldin, heritage, pwcc |
> | §4 — "below 10 the buckets are grader-agnostic" | **Contradicted.** ADR-0005 states the interface must *not* imply the pooled figure is company-neutral; it is PSA-dominated |
> | §5 — "annual cycles 1 of 3" | Unverified |
> | §6 — card imagery is placeholder slots | **Correct.** Describes the prototypes, and its licensing framing is right. Do not "fix" this |
>
> **Downstream consequence.** `DISPLAY_VOCABULARY.md`'s locked-row progress ratios were computed from §5's false dates and overstate readiness by roughly 15 months — "16/24 mo" where the truth is nearer 1/24. **No unlock countdown, progress bar, or LOCKED copy should be implemented until those constants are recalibrated.** Tracked as D-032 in `../DECISIONS.md`, which is the live register for all of this.

---

## 1. What Cardstock is

A market-data application for the Pokémon card aftermarket: price history, a screener, charts with backtesting, and a binder that treats a collection as a portfolio. Fan-made, not affiliated with Nintendo / The Pokémon Company / Creatures Inc.

The product's distinguishing commitment is **honesty about data**. Two rules follow from it and appear everywhere:

1. **Never smooth over a discontinuity.** Two data sources meet at a seam that is **per-card and ragged**: each card's per-sale history begins at its own first crawler visit (late Jul 2026 onward), not on a single shared date. Charts draw each boundary; they do not blend it. The current month is an aggregation of partial data and renders as a dashed line ending in a hollow point — never a projection.
2. **Never compute on insufficient data.** Every metric has a sufficiency floor. Below it the metric does not render a number: it renders a state (LOW DATA / LOCKED / UNDEFINED / UNSTABLE FIT) with the rule it failed and the date it will pass. Locked controls state their unlock condition and progress.

Copy follows the same posture: precise numbers over adjectives, no hype, no exclamation marks.

---

## 2. The document set

| Document | What it carries | Status |
|---|---|---|
| `uploads/CARDSTOCK_UI_SPEC_v1.md` | The approved functional spec: routes, screens, data model, priorities | **Stale in parts** — read with the delta list below |
| `DESIGN_NOTES.md` | Running design log: every ruling, why it was made, and what changed since the spec | Current |
| `DISPLAY_VOCABULARY.md` | Every dynamic element's complete value space — all states, not the sampled ones in the mockups | Current |
| `BACKTEST_WARNINGS.md` | The 15-check backtest warning engine, with severity tiers | Spec only — 3 of 15 wired in the prototype |
| `brand/README.md` + `brand/brand-tokens.css` | Brand system: logo, palette, type, usage rules | Current |
| `BRAND_BRIEF.md` | The brief the brand system was built from | Historical |
| This file | Index, screen inventory, spec deltas, open questions | Current |

**Read order for a new engineer:** this file → `CARDSTOCK_UI_SPEC_v1.md` §1–3 (product model and routes) → the §3 delta list below → `DISPLAY_VOCABULARY.md` when implementing any specific surface.

---

## 3. Screens

All prototypes are single-file HTML that open directly in a browser. Interactions are real: filters filter, sorts sort, modals validate, backtests run.

| Prototype | Route | Notes |
|---|---|---|
| `Cardstock Home.dc.html` | `/` | Portfolio value, market stat ticker, screen-activity feed, watchlist with peek panel |
| `Cardstock Screener.dc.html` | `/screener` · `/screener/{id}` · `/screener/{id}/backtest` | 27 filter metrics, saved screens, backtest mode |
| `Cardstock Charts.dc.html` | `/charts` · `#signals` deep link | 32 indicator rows, panes, saved views, compare, normalize |
| `Cardstock Binder.dc.html` | `/binder` | Holdings / transactions / performance, transaction modal, CSV export |
| `Cardstock Browse.dc.html` | `/browse` | By set and by Pokémon, attribute filters |
| `Cardstock Card.dc.html` | `/card/{id}` | 19-tier strip, price history, sales ledger, census & grading |
| `Cardstock Set.dc.html` | `/set/{id}` | Set contents, sort pills |
| `Cardstock Character.dc.html` | `/character/{name}` | Species aggregate across printings |
| `Cardstock Profile.dc.html` | `/settings` | Profile, appearance, account management, danger zone |
| `Cardstock Account.dc.html` | `/signin` `/create` `/forgot` `/reset` | 5 logged-out views; `#reset` simulates arriving from the email link |
| `Cardstock About Data.dc.html` | `/about-data` | Methodology, sources, the seam, sufficiency rules |
| `Cardstock Legal.dc.html` | `/legal` (`#privacy` `#terms`) | Privacy and terms |
| `Cardstock Landing.dc.html` | marketing `/` | Product overview |
| `Cardstock Screener Landing` / `Charts Landing` / `Binder Landing` | marketing `/screener` etc. | Per-pillar marketing pages |
| `Cardstock Brand System.dc.html` | — | Brand reference, not a product screen |
| `cardstock-search.js` | — | Shared nav search component used by all app pages |

**Chrome shared by every app page:** 48px nav (logo → Home, five section links, search, account circle → Profile), the search component, theme + colorblind tokens, pre-paint script reading `localStorage`.

---

## 4. Spec deltas — what changed after v1 was approved

The spec was approved before most of the design work. These are the differences; each was a deliberate ruling, recorded with reasoning in `DESIGN_NOTES.md`.

**Cut**
- **Alerts and email delivery, wholesale** — no Alert Center, no `/alerts`, no alert rules, no digests, no unsubscribe flow. The Home feed replaces the in-product surface and is renamed **"Screen activity."** UI remnants (Screener "Email me", nav bell) are stripped.
- **Listed-price column** on the Card page — 4.4% coverage. Replaced by a dotted amber underline plus tooltip on Realized.
- **AsOfStamp component** — removed app-wide; footers say "refreshed just now" instead of per-element staleness stamps.
- **Demo mode** — removed 2026-08-10; the marketing pages carry that story now. Spec §4.16 still lists it as P1.

**Changed**
- **Backtest gained an exit-rule toggle.** The spec had only timer exits (hold 3/6/12M). Signal exit — sell the month a card stops matching — is now a peer mode. It cascades: horizon pills disable, the histogram retitles to "Closed-trade return distribution," and stat tiles and entry columns switch per mode.
- **Backtest horizons disable when immature**, showing the date the first cohort matures.
- **Binder corrections: void + re-enter was rejected at the UI layer.** Inline ✎ opens a pre-filled modal. Append + void is backend audit plumbing only; the badge reads `AUDIT LOG`, not `IMMUTABLE`.
- **Grade scale: six tiers → the canonical 19 values.** Below 10 the buckets are grader-agnostic; each grader's 10 is its own tier. "Ungraded" is renamed **"Raw"** app-wide.
- **Pokédex model is many-to-many** — card ↔ species via a join table; species aggregates count a card once per featured species. The Pokédex schema is external and pre-populated, so species attributes never show METADATA PENDING.
- **Character page was built in v1** (spec had it P2).
- **Typography +15% throughout**, base 15px.

**Added since the spec**
- Email + password auth with transactional email (verify / reset / email-change only — alert email remains out of scope).
- Light + dark themes and a colorblind-safe palette (Okabe-Ito hue swap), persisted per device.
- Nav search on every page.
- About Data page, Legal page, brand system, and the four marketing pages.

---

## 5. Data dependencies that drive the UI

Much of the interface is a function of data maturity. These are the dependencies engineers will need to model explicitly, because the UI reads them at render time:

| Dependency | Starts | Gates |
|---|---|---|
| Monthly price history | Dec 2020 | ROC 12M, trend fits, beta, backtest honest floor |
| Per-sale ledger (post-seam) | Each card's first visit, late Jul 2026 onward — ragged, never a shared date | Churn, sales count, dispersion, Amihud, cross-market gap |
| Census snapshots | Each card's first visit, late Jul 2026 onward | Pop Δ, gem rate drift, supply overhang |
| Listed prices | ~12% of rows | Discount-to-list |
| Venue depth | eBay-only today | Cross-marketplace gap |
| Annual cycles | 1 of 3 | Seasonality overlay (~Nov 2027) |

Every one of these surfaces to the user as either a locked control with a progress ratio, a LOW DATA badge, or a caution string in the filter editor. The complete inventory is in `DISPLAY_VOCABULARY.md` §2, §9, §10.

> **Corrected 2026-08-10.** The per-sale and census rows previously read "Apr 2025" and "Jan 2026." Both were wrong — no such history exists. The scraper's first deployment was 2026-07-28 (`PokemonInvestBatch/DATA_MODEL.md:404`), and both series begin at each card's own first visit (`:397`). Only monthly price history is genuinely deep: it backfills to ~Dec 2020 on first visit (`:373`). Consequence: every liquidity and supply indicator is LOCKED for 6–12 months of calendar time that no engineering shortens.

---

## 6. Not built, deliberately

- **12 of the 15 backtest warning checks.** `BACKTEST_WARNINGS.md` specifies all 15 with severity tiers; concentration, maturity, and honest-floor are wired in the prototype as the banner pattern. The rest are engine work — the display pattern already exists.
- **Card imagery.** Every card, set, and species image is a placeholder slot. This is the largest open risk, and it is a licensing question rather than a design one.
- **Real backtest computation.** The prototype replays seeded series; the exit semantics, warning triggers, and tile definitions are specified, the math is not implemented.
- **CSV export.** The control and its affordance exist; the file generation is left to the application.
- **Alerts, email delivery, mobile app.** Out of scope by ruling.

---

## 7. Conventions worth not re-litigating

- **Color never carries meaning alone.** Every state pairs a hue with a glyph (▲ ▼ – ● ◌ ◆). Colorblind mode swaps hue only; glyphs, labels, and grammar are identical.
- **Numbers are monospace** (JetBrains Mono), everywhere, including inside prose.
- **Green = bullish, red = bearish, amber = data caution, grey = nothing to report.** Amber never means "bad," it means "the data is thin."
- **Tooltips explain consequence, not identity.** The label already says what a control is; the tooltip says what happens when you use it. Every interactive control on every app page carries one.
- **Filters AND together.** No OR, no grouping, in v1.
- **One row per card + tier** on watchlists; a row can track many signals.
- **Theme, colorblind mode, and density persist per device**, not per account.
