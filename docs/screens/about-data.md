# Screen spec — About our data (`about-data`)

**Source of truth:** `CardStock Mockup/Cardstock About Data.dc.html` (136 lines), read directly 2026-08-10. Every quote carries its line number in that file. Per `CLAUDE.md`, the prototype is Tier 1 and authoritative for **what the page says** — §§1–5 record that faithfully. Whether what it says is **true** is §6, and the answer is: largely not.

> **Amended 2026-08-15 (Catalog phase design, D-110 — build from
> `docs/superpowers/specs/2026-08-15-catalog-phase-design.md`).** The build target is this
> spec's **"Corrected copy — build this"** section, transcribed with exactly three
> adaptations: **(1)** the sufficiency section slims to what exists — the five states, the
> 2026-09-01 floor and its reason, and the locked-controls-name-their-unlock rule; per-signal
> unlock rows return when those signals ship. **(2)** No runtime-computed strings — the
> corrected copy's dates are fixed historical anchors; the authored-countdown class (`≈ Jan
> 2027`) is already gone from it. **(3)** The Card page's freshness footer gains the "About
> our data" link, and the "opening a card page triggers a fresh visit" sentence is
> receipt-verified against the shipped refresh behavior before it prints. Route `/about-data`
> as a public WASM app route (§7.11 answered; tier addendum to D-063). The Apr-'25 pill and
> section die with the rewrite; §7.1's chart-marker question stays open for the chart phases —
> the copy simply stops promising the marking (§7.5 resolved: copy corrected, pipeline not
> built).

> **Read §6 before implementing this page.** It is the most factually wrong page in the prototype set. Its central organising concept — "The April 2025 seam" — describes a boundary that does not exist, on a date that predates the project's first commit by fifteen months. The page also claims a data field (sale counts) the database has never had and the source has never published, and it understates the depth of the one series that is genuinely deep by nearly three years. Shipping this copy unchanged would put false public statements on a site whose entire brand is not making false public statements.

---

## 1. Identity

| Field | Value |
|---|---|
| Screen label | `About our data` — `data-screen-label="About our data"` (line 30) |
| `<h1>` | "About our data" (line 47) |
| Prototype file | `Cardstock About Data.dc.html` |
| Proposed route | `/about-data` |
| In-page anchors | `#sources` (59), `#seam` (68), `#refresh` (76), `#sufficiency` (84), `#honesty` (107), `#disclaimers` (121) |
| Inbound link | `Cardstock Legal.dc.html:65` — "see [About our data]" |
| Nav entry | **None.** Not in the primary nav (lines 34–40) |

**Purpose.** The public data-provenance and methodology page. It is the destination for "where did this number come from", and it is where the product's honesty posture is stated as policy rather than implied by UI. Subtitle, line 48: *"Where every number comes from, how fresh it is, and what we refuse to show."*

**Why accuracy matters more here than anywhere else.** The page's entire persuasive value is that it is checkable. A locked indicator elsewhere in the app is a design choice; a wrong date here is a false public statement about the dataset. D-032 (`DECISIONS.md:342`) already caught this project publishing readiness numbers "wrong in the direction that overstates readiness" — this page does it again, in public-facing copy.

---

## 2. Layout

Single centred column, no sidebar, **no footer**.

```
sticky nav (48px)                                        line 32
container  max-width 820px, margin 0 auto, padding 32px 24px 80px   line 46
├─ h1 "About our data"                                   line 47
├─ subtitle                                              line 48
├─ pill row (flex, wrap, gap 6px, mb 28px)               lines 50–57
│   Sources · The Apr '25 seam · Refresh & closed months · Sufficiency rules · Honesty policy · Disclaimers
├─ section#sources        card, mb 14px    3 paragraphs  lines 59–66
├─ section#seam           card, mb 14px    2 paragraphs  lines 68–74
├─ section#refresh        card, mb 14px    2 paragraphs  lines 76–82
├─ section#sufficiency    card, mb 14px    intro + 2-col grid table  lines 84–105
├─ section#honesty        card, mb 14px    lead-in + 5-item <ul>     lines 107–119
├─ section#disclaimers    card, no mb      2 paragraphs  lines 121–127
└─ closing note (12.5px, --mut2, mt 20px)                lines 129–131
```

**Metrics** (shared with the Legal screen — these two pages are the same document template):

| Property | Value | Line |
|---|---|---|
| Section card | `--card` bg, `1px solid var(--line)`, `border-radius: 8px`, `padding: 20px 22px` | 59, 68, 76, 84, 107, 121 |
| `scroll-margin-top` | `62px` on all six sections | same |
| h1 | `'Inter Tight'`, 27px, 700 | 47 |
| h2 | `'Inter Tight'`, 18.5px, 700, `margin: 0 0 10px` | 60, 69, 77, 85, 108, 122 |
| Body | 14.5px, `line-height: 1.6`, `--mut` | 61, 70, 78, 86, 109, 123 |
| Pill | 13px/600, `border-radius: 99px`, `padding: 4px 12px` | 51–56 |
| Closing note | 12.5px, `--mut2`, `margin-top: 20px` | 129 |

**Sufficiency table** (lines 87–104) — not a `<table>`; a CSS grid inside a bordered, `overflow: hidden` wrapper:

| Property | Value | Line |
|---|---|---|
| Wrapper | `border: 1px solid var(--line)`, `border-radius: 6px`, `overflow: hidden` | 87 |
| Grid | `grid-template-columns: 1.2fr 2fr`, `gap: 0`, `font-size: 13.5px` | 88 |
| Header cells | `--mutbg` bg, 600, 12px, `letter-spacing: 0.06em`, uppercase, `--mut2` | 89–90 |
| Signal cells | `'JetBrains Mono'`, 12.5px, `border-top: 1px solid var(--line4)` | 91, 93, 95, 97, 99, 101 |
| Needs cells | `--mut`, `border-top: 1px solid var(--line4)` | 92, 94, 96, 98, 100, 102 |
| Cell padding | `8px 12px` throughout | all |

**Honesty list** (line 111): `<ul>` as `display: flex; flex-direction: column; gap: 6px`, `padding-left: 20px`.

**Theme tokens — richer than the Legal page.** This page defines gain/loss colours the Legal page does not:

| Selector | Tokens | Line |
|---|---|---|
| `:root[data-theme="dark"]:not([data-cvd="1"])` | `--pos: #4CC08D; --neg: #E57B7B` | 22 |
| `:root[data-theme="dark"][data-cvd="1"]` | `--pos: #58A9E6; --neg: #F5924E` | 23 |
| `:root[data-cvd="1"]` | `--pos: #0B69A8; --neg: #CC5F00` | 24 |
| `:root[data-theme="dark"]` | `--logoTeal: #3FBFAD` | 25 |

These are inherited boilerplate — **no element on this page uses `--pos` or `--neg`.**

---

## 3. Content inventory

Every claim the page makes, quoted exactly.

### 3.1 Header and navigation pills

| Line | Text |
|---|---|
| 47 | "About our data" |
| 48 | "Where every number comes from, how fresh it is, and what we refuse to show." |
| 51 | "Sources" → `#sources` |
| 52 | "The Apr ’25 seam" → `#seam` |
| 53 | "Refresh & closed months" → `#refresh` |
| 54 | "Sufficiency rules" → `#sufficiency` |
| 55 | "Honesty policy" → `#honesty` |
| 56 | "Disclaimers" → `#disclaimers` |

### 3.2 Sources & coverage (lines 59–66)

| Line | Claim |
|---|---|
| 60 | "Sources &amp; coverage" |
| 62 | "**Prices** come from realized sales only — completed marketplace listings and major auction results. Asking prices never enter an aggregate; the rare Listed figures you see are labeled as such and cover under 5% of rows." |
| 63 | "**Populations** come from the public census reports the grading companies publish monthly. When a grader restates a past census (it happens), we mark the affected window on charts rather than silently rewriting history." |
| 64 | "**Excluded**: bulk lots, listings with ambiguous grade or damage notes, sales where the card can't be matched to one printing, and marketplaces whose sold data we can't verify. Coverage is deepest for English-language cards graded PSA, BGS, CGC, SGC, ACE, and TAG, plus raw sales." |

### 3.3 The April 2025 seam (lines 68–74)

| Line | Claim |
|---|---|
| 69 | "The April 2025 seam" |
| 71 | "Before April 2025 our archive holds **monthly aggregates** — averages and sale counts, back to August 2023. From April 2025 forward we keep the **per-sale ledger**: every individual transaction with its date, venue, and grade." |
| 72 | "That boundary is drawn as a marker on charts. Indicators that need individual sales — churn, price dispersion, Amihud illiquidity, cross-marketplace gap, discount-to-list — can only be computed after it, which is why their history starts there and why backtests refuse to reach further back (the "honest floor")." |

### 3.4 Refresh & closed months (lines 76–82)

| Line | Claim |
|---|---|
| 77 | "Refresh &amp; closed months" |
| 79 | "Sales data refreshes daily; census data lands when graders publish, roughly monthly. The footer stamp on every page tells you how fresh what you're looking at is." |
| 80 | "Monthly series only include **closed months**. The current month appears as a hollow, dashed point — it aggregates the sales recorded so far and will keep revising until the month closes. We never project or extrapolate it." |

### 3.5 Sufficiency rules (lines 84–105)

| Line | Claim |
|---|---|
| 85 | "Sufficiency rules" |
| 86 | "An indicator that doesn't have enough history to be trustworthy is locked, with its unlock date shown — not rendered anyway with a warning buried in a tooltip. The rules:" |
| 89 / 90 | Column headers "Signal" / "Needs" |

| Signal | Line | Needs | Line |
|---|---|---|---|
| "Churn 30d" | 91 | "30 days of per-sale ledger in that grade bucket; starts LOW CONFIDENCE for its first 30 days" | 92 |
| "Weekly bars" | 93 | "~6 months of ledger (≈ Jan 2027)" | 94 |
| "Daily bars" | 95 | "~12 months of ledger, liquid cards only" | 96 |
| "Oscillators (RSI, z-score)" | 97 | "their full warm-up window of monthly closes before the first value renders" | 98 |
| "Seasonality" | 99 | "one observed cycle so far — labeled illustrative until there are three" | 100 |
| "Composites (G1–G4)" | 101 | "every component signal individually sufficient — a composite never fires on partial inputs" | 102 |

### 3.6 Honesty policy (lines 107–119)

| Line | Claim |
|---|---|
| 108 | "Honesty policy" |
| 110 | "Some things we simply don't show:" |
| 112 | "No projected or extrapolated data points — a partial month renders as partial, never as a forecast." |
| 113 | "Backtests start at each screen's honest floor — the first date every filter in it could actually be computed — not at the start of our archive." |
| 114 | "A backtest that mostly found one set's moment says so, instead of presenting it as a repeatable pattern." |
| 115 | "Missing metadata renders as METADATA PENDING, not as a silent blank or a guess." |
| 116 | "When a grader restates history, the restatement window stays visibly marked." |

### 3.7 Disclaimers (lines 121–127) and closing note

| Line | Claim |
|---|---|
| 122 | "Disclaimers" |
| 124 | "Cardstock is a fan-made analytics project. It is not affiliated with, endorsed by, or sponsored by Nintendo, The Pokémon Company, or any grading company or marketplace. Pokémon names and card references are used for identification only; all trademarks belong to their owners." |
| 125 | "Nothing here is financial advice. Collectible prices are volatile and thinly traded; signals describe the past, not the future. Do your own research before spending money on cardboard." |
| 130 | "Questions about a number? Every stat's tooltip names its source and window." |

---

## 4. States / interactions

Static page. No data binding, no loading or empty states, no conditional rendering in the prototype.

| Interaction | Behaviour | Line |
|---|---|---|
| Six pills | Same-page anchor jumps; every target carries `scroll-margin-top: 62px` to clear the 48px sticky nav | 51–56, section tags |
| Deep links | `/about-data#sources`, `#seam`, `#refresh`, `#sufficiency`, `#honesty`, `#disclaimers` must land on their sections | 59–121 |
| Inbound deep link | `Cardstock Legal.dc.html:65` links here without a fragment | — |
| Nav / wordmark / avatar / search | Standard cross-page navigation | 33–43 |
| Hover / focus | `--accH` + underline; `2px solid var(--acc)` outline, `1px` offset | 19–20 |
| Theme + CVD | Read from `localStorage` pre-paint and stamped on `<html>` | 28 |

**Dynamic-content candidates.** Nothing on the page reads from the database as drawn, but three strings are time-dependent and will rot if hard-coded:
1. "The Apr ’25 seam" / "The April 2025 seam" (52, 69) — a date.
2. "back to August 2023" (71) — a date.
3. "~6 months of ledger (**≈ Jan 2027**)" (94) — a projected unlock date.

Per D-033 (`DECISIONS.md:319`) the correct pattern is "**one anchor date plus denominators**. Numerators are arithmetic against today. **No authored ratios, ever again.**" Line 94's parenthetical is an authored date of exactly the kind D-033 bans, and it is wrong (§6.10).

---

## 5. Rules and invariants

1. **This page is the canonical statement of data provenance.** Any provenance wording elsewhere (tooltips, empty states, lock copy) must agree with it — line 130 promises "Every stat's tooltip names its source and window", making tooltips subordinate to this page.
2. **Six sections, six anchors, fixed order** (59 → 121). The pill row (50–57) must stay in sync with the section ids.
3. **`scroll-margin-top: 62px` on every section** — required by the sticky nav.
4. **Locked, not degraded** (line 86): an insufficient indicator renders locked with an unlock date; it is never rendered anyway with a caveat. Corroborated by D-038 (`DECISIONS.md:237`) — "v1 ships the full UI with locks visible".
5. **Never project** (lines 80, 112): the current month renders as a hollow dashed point; no forecast, no extrapolation, ever.
6. **Composites never fire on partial inputs** (line 102).
7. **Restatement windows stay marked** (lines 63, 116) — history is never silently rewritten. This is the exact commitment `Cardstock Legal.dc.html:68` cross-references ("That's how we treat charts").
8. **Backtests start at the honest floor, not at the start of the archive** (line 113).
9. **Missing metadata renders as `METADATA PENDING`** (line 115) — a literal display token, not a blank.
10. **Dates on this page must be derived, not authored** — D-033's rule (`DECISIONS.md:319`), which the page currently violates three times.
11. **Consistent with D-041** (`DECISIONS.md:224`): the page never mentions candlesticks or news, correctly applying the Tier-1 rule that permanently-impossible features are "omitted from the app entirely, not rendered as disabled controls" (`DECISIONS.md:233`).

---

## 6. Factual audit

Checked against `../PokemonInvestBatch/DATA_MODEL.md` (489 lines), the scraper source, and `DECISIONS.md`. All receipts read directly 2026-08-10.

### 6.0 The finding that frames everything else: the page never names its source

**No claim on this page discloses that 100% of CardStock's market data is scraped from a single third-party website.**

- `DATA_MODEL.md:89` — "**All data comes from pricecharting.com** (site facts verified 2026-07-27 against captured pages spanning 2024→live)."
- `CLAUDE.md:47` — the sibling worker "politely crawls pricecharting.com into PostgreSQL".

The page instead uses consistently first-party language:

| Phrase | Line |
|---|---|
| "our archive holds" | 71 |
| "we keep the per-sale ledger" | 71 |
| "we mark the affected window on charts" | 63 |
| "marketplaces whose sold data **we can't verify**" | 64 |
| "**Excluded**: bulk lots…" | 64 |
| "the start of our archive" | 113 |

"Excluded" and "we can't verify" are the sharpest: they describe **editorial decisions about which marketplaces to admit**, implying CardStock evaluates marketplaces and rejects some. It does not. It parses whichever rows pricecharting.com prints, from the five sources that site chooses to show (`DATA_MODEL.md:227` — "ebay, tcgplayer, goldin, heritage, pwcc"). The inclusion decision is pricecharting's, not ours. **Verdict: the page's framing is FALSE by implication throughout**, and this is a bigger reputational exposure than any single wrong date, because a reader who checks will conclude the page was written to obscure a single point of dependency.

It also has downstream consequences the page never states: prices are **not** aggregated by us (§6.1), and there is no editorial exclusion pipeline (§6.4).

### 6.1 Prices come from realized sales only

> **"Prices come from realized sales only — completed marketplace listings and major auction results."** (line 62)

**Verdict: FALSE as applied to the price series the product actually plots.**

- The plotted price series is **not computed from realized sales by CardStock**. It is copied wholesale from the source site's own chart. `DATA_MODEL.md:93–94` — "**`VGPC.chart_data`** — the price chart: **six series of monthly average prices**, in cents, reaching back to ~December 2020." `DATA_MODEL.md:186` — `price_cents` is "site — monthly average price".
- The `price_months` table therefore contains a **third party's precomputed average**, whose methodology is not published and not knowable to us. Whether pricecharting's monthly average is built from realized sales only is an assumption, not a verified fact — nothing in `DATA_MODEL.md` establishes it.
- The `sales` table *is* realized sales (`DATA_MODEL.md:231` — "site — realized sale price"; `:217` — "one immutable row per completed sale we have ever seen"), and the sources are marketplaces plus genuine auction houses — goldin, heritage, pwcc — so "major auction results" is fair **for the ledger**. But per D-001 the ledger begins late Jul 2026, so it is not what any historical price chart is drawn from.
- **Precisely:** "Prices come from realized sales only" would be true of the sales ledger and is unproven for the price series. The page draws no distinction, and the price series is what "prices" means to a reader looking at a chart.

> **"Asking prices never enter an aggregate; the rare Listed figures you see are labeled as such and cover under 5% of rows."** (line 62)

**Verdict: first clause UNVERIFIABLE; "under 5% of rows" UNVERIFIABLE and unsupported.**

- We cannot assert what does or does not enter an aggregate we did not compute (above).
- `listed_price_cents` exists and is genuinely sparse: `DATA_MODEL.md:232` — "site — original listing price when shown; **most rows have none**". That supports "rare". It does **not** support "under 5%": "most rows have none" is consistent with anything from 0% to 49%. No query establishing 5% appears anywhere in either repo. **This is an authored precision number of exactly the class D-032 condemned** (`DECISIONS.md:342`) — a specific figure with no receipt.
- The underlying field is real and its intended use is documented: `DATA_MODEL.md:447` — "Also derivable, currently unused: discount-vs-list (`price_cents` vs `listed_price_cents`".

### 6.2 Populations come from graders' monthly census reports

> **"Populations come from the public census reports the grading companies publish monthly."** (line 63)

**Verdict: FALSE on all three counts — source, publisher, and cadence.**

| Claim | Reality | Receipt |
|---|---|---|
| From "the public census reports" | From the source site's embedded `pop_data` blob, scraped | `DATA_MODEL.md:103` — "**`VGPC.pop_data`** — the graded-population census: `{psa: [10 ints], cgc: [10 ints]}`" |
| "the grading companies publish" | We never touch a grading company. Data is second-hand via pricecharting | `DATA_MODEL.md:89` |
| "monthly" | The site publishes **no** cadence and **no** history — a current snapshot only. Rows appear when *we* visit | `DATA_MODEL.md:104` — "A **current snapshot only** — the site keeps no census history."; `:120–121` — "**Population history.** Only the current census is published; history exists only from the moment *we* started observing." |

- Additionally, the page's coverage list (line 64) names six graders, but **census data exists for exactly two**: `DATA_MODEL.md:204` — "`grader` | string(8), PK part — **only `psa` or `cgc`; any other key is schema drift**". There is no BGS, SGC, ACE, or TAG population data at all.
- Compounding this, D-001 (`DECISIONS.md:22`) makes census history start at each card's first visit in late Jul 2026 — so "monthly reports" describes a series that, at time of writing, has roughly one observation per card.

> **"When a grader restates a past census (it happens), we mark the affected window on charts rather than silently rewriting history."** (line 63)

**Verdict: the premise is VERIFIED and impressively specific; the chart-marking behaviour is UNVERIFIABLE (unbuilt).**

- `DATA_MODEL.md:209–213` — "graders occasionally **restate** their counts (PSA restated ~June 2026; one card's grade cell jumped 397 → 99,246). A >10× jump on an established base, or any decrease, is flagged by metrics/alerts as a *source* change, not a market signal — but the rows are still written."
- Note what the receipt actually covers: detection is an **operational alert**, not a user-facing chart annotation. "we mark the affected window on charts" is a UI promise with no implementation and no design artifact behind it. The honest-history posture ("the rows are still written", never rewritten) is genuinely verified.

### 6.3 Coverage

> **"Coverage is deepest for English-language cards graded PSA, BGS, CGC, SGC, ACE, and TAG, plus raw sales."** (line 64)

**Verdict: UNVERIFIABLE for the language claim; partially supported for graders, and misleading in context.**

- **Language:** no language filter, field, or metric exists anywhere in the scraper. `DATA_MODEL.md` has no language column on `cards`. The claim may be incidentally true of pricecharting's Pokémon catalogue, but CardStock cannot substantiate it.
- **Graders:** the six named do appear in the **sales** grade vocabulary — `CLAUDE.md:93` documents 19 values including "`PSA 10`, `CGC 10`, `CGC 10 Prist.`, `BGS 10`, `BGS 10 Black`, `SGC 10`, `TAG 10`, `ACE 10`", sourced from `GradeTierVocabulary.cs`, and `DATA_MODEL.md:230` confirms `grade_tier` carries "21 distinct labels driven by the page's own selector". So the labels exist on *sales*.
- **The misleading part:** the sentence sits in a paragraph about coverage generally, immediately after a sentence about populations. A reader takes it to mean all six graders are covered across the product. For **populations**, four of the six do not exist (§6.2), and for **price series** the tiers collapse to six with only PSA 10 named — D-003 (`DECISIONS.md:44`): `Ungraded, Grade7, Grade8, Grade9, Grade9Half, Psa10`. D-003's own table makes the split explicit: price series 6 tiers, sales ledger 19. The page flattens three different vocabularies into one coverage claim.
- **Raw sales:** verified — `Ungraded` is both a price tier (D-003) and a grade-tier label.

### 6.4 The exclusion list

> **"Excluded: bulk lots, listings with ambiguous grade or damage notes, sales where the card can't be matched to one printing, and marketplaces whose sold data we can't verify."** (line 64)

**Verdict: FALSE — no such exclusion pipeline exists.**

- `grep -rniE "bulk|damage|exclude|ambiguous" --include="*.cs" src` across the scraper returns **no sale-content filtering whatsoever** (run 2026-08-10). Every "exclude" hit concerns delisted/not-a-card *cards* being excluded from **scheduling** (`VisitCandidatePool.cs:78–80`, `StatsLane.cs:108–109`, `CrawlMetrics.cs:94,121`) or a set `Blacklist.cs:21` the operator configures. None of it touches sale rows.
- The parser ingests every row the page prints. `DATA_MODEL.md:100–102` — "Individual sale rows (`<tr id="{source}-{id}">`), grouped into up to 21 grade buckets"; the only validation is a length assertion on the tier label (`CardDetailParser.cs:251–255`) which **throws as schema drift** rather than silently skipping a sale.
- "sales where the card can't be matched to one printing" cannot arise: sales are parsed from a specific card's detail page, so `card_id` comes from "visit context" (`DATA_MODEL.md:226`), not from matching. The stated risk does not exist, so the stated mitigation does not either.
- "marketplaces whose sold data we can't verify" — the five sources are whatever pricecharting shows (`DATA_MODEL.md:227`); we exercise no admission decision (§6.0).
- **If bulk lots or damaged listings appear in pricecharting's buckets, they are in our `sales` table today.** The page tells users the opposite.

### 6.5 The seam date — the page's central claim

> **"The Apr ’25 seam"** (line 52) · **"The April 2025 seam"** (line 69) · **"Before April 2025 our archive holds…"** / **"From April 2025 forward we keep…"** (line 71) · **"That boundary is drawn as a marker on charts."** (line 72)

**Verdict: FALSE. This is the single largest factual error in the prototype set, and it is repeated four times including in a section heading and a navigation pill.**

D-001 (`DECISIONS.md:22–33`) is titled, verbatim: *"Per-sale and census history begin at each card's first crawler visit (late Jul 2026), **not Apr 2025 / Jan 2026**"*, and states *"The seam is **per-card and ragged**, not a single shared date."*

Receipts, all independent of one another:

| Receipt | What it shows |
|---|---|
| `DATA_MODEL.md:404` (via D-001) | `visits`, `fingerprints`, `parse_failures` "begin at first deployment (**2026-07-28**)" |
| `DATA_MODEL.md:120–121` | Population history "exists only from the moment *we* started observing" |
| `git -C PokemonInvestBatch log --reverse` (via D-001), `CLAUDE.md:47` | First commit **2026-07-27** |
| `DESIGN_NOTES.md:41` (via D-001) | "per-sale scraping started Jul '26. Census data 2026+" |
| Owner, `DECISIONS.md:31` | "That's completely false. It just started this month." |

**April 2025 is fifteen months before the scraper's first commit.** No process was collecting anything then. The date cannot be right by any interpretation.

**Two distinct errors, not one:**
1. **The date is wrong** — late Jul 2026, not Apr 2025.
2. **The concept is wrong** — there is no single shared seam. It is per-card and ragged, because each card's history starts at *its* first visit. Line 72's "That boundary is drawn as a marker on charts" describes drawing one vertical line across all charts; the honest rendering is a different marker position per card.

D-009 (`DECISIONS.md:385`) already tracks the same error in `DESIGN_NOTES.md:35` ("still specifies an 'Apr '25 liquidity seam' that D-001 says cannot exist"). **This prototype is a second, public-facing instance of that defect, and D-009 does not currently mention it.**

**Note on document authority.** `CLAUDE.md:20` makes the mockups authoritative for "layout, states, copy, and behaviour" — not for facts about the data, where `CLAUDE.md:21` gives `../PokemonInvestBatch/` authority, and `CLAUDE.md:30` puts `DECISIONS.md` above all tiers. So there is no authority conflict here: the prototype is authoritative that the page *says* April 2025, and simply wrong that April 2025 *is* the seam.

### 6.6 Pre-seam contents: "averages and sale counts"

> **"Before April 2025 our archive holds monthly aggregates — averages and sale counts, back to August 2023."** (line 71)

**Verdict: FALSE three times over — the date, the "sale counts", and the depth.**

**(a) "sale counts" do not exist and never can.** This is the most serious error, because it advertises a field the source has permanently withheld.

- `price_months` has exactly five columns — `card_id`, `tier`, `month`, `price_cents`, `observed_at` (`DATA_MODEL.md:181–187`). **There is no count column.**
- `DATA_MODEL.md:113–115` — "**Historical sales volume.** No page, in any epoch we've captured, carries a volume-over-time series. The only 'volume' on a detail page is a current-rate text label… a snapshot, no time axis."
- `DATA_MODEL.md:482` (via D-017, `DECISIONS.md:467`) — "**Unavailable from source, permanently:** historical sales volume; sales beyond the bucket windows; pre-observation census history."
- `DATA_MODEL.md:391` — "**Monthly sales volume:** derivable from the ledger (§6) **forward of each card's seam**." Counts exist only *after* the seam, from the ledger. The page places them exclusively *before* it. **The claim is precisely inverted.**
- D-004 (`DECISIONS.md:61`) — no precomputed metric store exists in the database at all.
- `DATA_MODEL.md:123–124` states the general rule the page violates: "Any spec that assumes deep volume history, complete deep sales history, or pre-2026 census history is assuming data that does not exist."

**(b) "back to August 2023" understates the one genuinely deep series by ~32 months.** D-002 (`DECISIONS.md:37`) — "Monthly price history is genuinely deep: **~Dec 2020**, backfilled whole on first visit… **The one data series that is not thin.**" Receipts: `DATA_MODEL.md:93–94` and `:176–177` — "A card's first visit backfills the site's entire chart — **six tiers monthly back to ~Dec 2020**".

This error is doubly unfortunate: it is wrong in the *conservative* direction, and it hides the product's only strong data asset. The page undersells the one thing it could legitimately boast about while overselling four things it cannot.

**(c) "Before April 2025"** — same error as §6.5.

**The corrected sentence** is roughly: *"Monthly average prices go back to about December 2020 for every card — six tiers, one value per month, backfilled in full the first time we see a card. Sale counts are not available before we began observing, and are not available from the source at any depth."*

### 6.7 Post-seam contents: "every individual transaction"

> **"From April 2025 forward we keep the per-sale ledger: every individual transaction with its date, venue, and grade."** (line 71)

**Verdict: FALSE on the date and on "every"; VERIFIED on the three fields.**

- **Date:** §6.5.
- **"every individual transaction" is not achievable.** `DATA_MODEL.md:101–102` — "**Each bucket shows only the newest ~30 rows** — the site discards older ones forever." `:118–119` — "**Sales older than the ~30-row bucket windows.** Once a row scrolls off, the site shows it to no one."
  - The scraper works hard against this: scheduling prioritises "due by burn window (a selling card approaching the point where its ~30-row bucket will start rolling sales off unseen; visited by 50% of that window — **the zero-missed-sales guarantee**)" (`DATA_MODEL.md:330–332`). That guarantee is real and impressive — but it is a *forward* guarantee, contingent on visit scheduling, on a corpus where cards can be "starved past the 30-day floor" (`DATA_MODEL.md:334`). It does not make "every individual transaction" true retrospectively, and it can be broken by any card whose sales rate outruns its visit rate.
- **Fields VERIFIED.** date → `sold_on` (`DATA_MODEL.md:229`); venue → `source` (`:227`, "ebay, tcgplayer, goldin, heritage, pwcc"); grade → `grade_tier` (`:230`). The ledger is genuinely immutable and dedup-guaranteed (`:217–221`, `UNIQUE (source, source_id)`, `ON CONFLICT DO NOTHING`).

### 6.8 Post-seam indicators

> **"Indicators that need individual sales — churn, price dispersion, Amihud illiquidity, cross-marketplace gap, discount-to-list — can only be computed after it, which is why their history starts there and why backtests refuse to reach further back (the "honest floor")."** (line 72)

**Verdict: VERIFIED in principle (the reasoning is exactly right); the "after it" referent inherits the wrong date from §6.5.**

- The logic matches D-001's stated consequence (`DECISIONS.md:33`): "every liquidity and supply indicator renders LOCKED for 6–12 months of calendar time that no engineering shortens. This is the largest scope fact in the project."
- Each named indicator is supportable from the ledger: churn/volume (`DATA_MODEL.md:391`, `:429`, `:433–435`), discount-to-list (`:447`, explicitly "currently unused"), cross-marketplace gap (five `source` values, `:227`), dispersion and Amihud (derivable from `price_cents` + `sold_on` + volume, forward of the seam).
- **The problem is arithmetic, not logic.** "their history starts there" points at April 2025. It actually starts at each card's first visit (D-001), and for display purposes at **2026-09-01** (D-033). The sentence's reasoning survives a date correction unchanged — this is the one seam claim that only needs its referent fixed.

### 6.9 Refresh cadence

> **"Sales data refreshes daily; census data lands when graders publish, roughly monthly."** (line 79)

**Verdict: FALSE on both halves.**

**Sales — not daily.** The crawl is continuous but per-card scheduling is a priority queue, not a daily sweep:
- `DATA_MODEL.md:322` — Detail crawl cadence is "**continuous, one card at a time**".
- `DATA_MODEL.md:315–316` — one shared politeness gate, "adaptive delay, **10 s floor** / 300 s ceiling".
- `DATA_MODEL.md:329–335` — scheduling is a "pure priority score… *due by burn window* → *refresh requested* → *never visited* → *bucket already at cap* → *starved past the 30-day floor* → everyone else by staleness × (1 + churn)".
- **"starved past the 30-day floor" is decisive**: the system explicitly tolerates a card going ~30 days without a visit. That is the opposite of a daily refresh.
- Arithmetically: at a 10 s floor a full corpus lap takes ~10.5 days for ~91k cards (`DECISIONS.md:320` references "91k cards"), and longer whenever the adaptive delay rises toward the 300 s ceiling. D-033 (`DECISIONS.md:324`) flags the related "~12.4-day corpus lap" figure as **unverified** — so the exact lap time is unknown, but "daily per card" is excluded by the 30-day floor regardless.
- The charitable reading — "the system ingests some sales every day" — is true and useless: a user reading line 79 beside a specific card's chart will conclude *that card* updates daily. It does not.

**Census — does not land when graders publish.** It lands when *we* visit, on the same priority queue, from a snapshot with no history (`DATA_MODEL.md:104`, `:120–121`). Graders' own publication schedules are invisible to this pipeline. Same error as §6.2.

> **"The footer stamp on every page tells you how fresh what you're looking at is."** (line 79)

**Verdict: FALSE as stated — "every page" is contradicted by this very page.**

- A grep for freshness stamps across `CardStock Mockup/*.dc.html` (run 2026-08-10) finds them on `Cardstock Home.dc.html`, `Cardstock Landing.dc.html`, `Cardstock Binder Landing.dc.html`, `Cardstock Charts Landing.dc.html`, `Cardstock Screener Landing.dc.html`, and `Cardstock Brand System.dc.html`.
- **`Cardstock About Data.dc.html` has no footer** — the container closes at line 132 after the note at 129–131; the only match in this file is the sentence making the claim (line 79). **`Cardstock Legal.dc.html` has no footer either.**
- Both omissions are correct — neither page renders market data — but the word "every" makes the sentence self-refuting on the page that says it. Fix the copy ("every page that shows market data"), not the pages.

### 6.10 Closed months and the current-month point

> **"Monthly series only include closed months. The current month appears as a hollow, dashed point — it aggregates the sales recorded so far and will keep revising until the month closes. We never project or extrapolate it."** (line 80)

**Verdict: the behaviour is VERIFIED and well-founded; the stated mechanism ("it aggregates the sales recorded so far") is FALSE.**

- **Closed-month immutability, verified:** `DATA_MODEL.md:98–99` — "**Closed months are immutable server-side; only the current month revises between visits.**" And `:178–179` — "a typical visit adds 0–2 rows (the current month moved); **closed months carry exactly one row forever**."
- **Revision is real and already modelled:** `DATA_MODEL.md:189–191` — "The composite PK ends in `observed_at`: the same (card, tier, month) legitimately has multiple rows when the *current* month's average revised between visits. Latest-per-key queries must order by `observed_at`." The hollow dashed point is an honest rendering of a fact the schema already encodes.
- **The mechanism is wrong.** The current month's value is **the source site's revised monthly average**, refetched on our next visit — not an aggregate CardStock computes from its own sales rows. Two consequences the copy hides: the point revises **only when we visit that card** (not continuously), and its composition is the third party's, not ours (§6.1).
- "We never project or extrapolate it" — VERIFIED as consistent with D-041 (`DECISIONS.md:229`), which establishes that `price_cents` is a single monthly value and that OHLC/intraday structure "does not exist at the source". The product is structurally incapable of the thing it disclaims.

### 6.11 Sufficiency rules — and the missing 2026-09-01 floor

**The page does not state the sufficiency floor, and D-033 says it should. Verdict on the omission: CONFIRMED — the floor is absent.**

D-033 (`DECISIONS.md:322`) says in terms: *"`Cardstock About Data.dc.html` should carry the floor **and its reason**. 'We discarded our own early data because we didn't trust it' is the same posture as the rest of the design and is a stronger story than an unexplained date."*

The string "2026-09-01", "September 2026", and any equivalent **appear nowhere in the file**. The `#sufficiency` section (84–105) lists per-signal requirements and never states the global cutoff they are measured from. The only date in the whole section is "≈ Jan 2027" (line 94), which is wrong *because* the floor is missing.

D-033's substance, for the copy that needs writing (`DECISIONS.md:309–316`): the floor is "not an assertion about when data began — that is per-card and ragged (D-001). It is a deliberate, disclosed cutoff: the collector was still being stabilised through August 2026, so earlier observations are discarded rather than trusted." And on why September: "the owner expects the scraper's bugs resolved by the end of August 2026… An earlier proposal of 'August 2026' was rejected as inconsistent with that same reasoning — if August is the stabilisation month, August data is the suspect data."

Per-row audit:

| Signal | Line | Verdict |
|---|---|---|
| "Churn 30d" — "30 days of per-sale ledger in that grade bucket; starts LOW CONFIDENCE for its first 30 days" | 91–92 | **VERIFIED as a rule.** Churn is derivable — `DATA_MODEL.md:429`, `SELECT count(*) / 30.0 FROM sales`. The 30-day window must count from 2026-09-01 (D-033), which the page never says, so a reader cannot compute the unlock date |
| "Weekly bars" — "~6 months of ledger (≈ Jan 2027)" | 93–94 | **FALSE.** 2026-09-01 + 6 months = **~Mar 2027**, not Jan 2027 — roughly two months optimistic. Jan 2027 is only reachable from a ~Jul 2026 start, i.e. by ignoring the D-033 floor. Also violates D-033's "no authored ratios, ever again" (`DECISIONS.md:319`) |
| "Daily bars" — "~12 months of ledger, liquid cards only" | 95–96 | **VERIFIED as a rule**; no date authored, so nothing to be wrong. Implies ~Sept 2027, consistent with D-033's "12 months of census → ~Sept 2027" (`DECISIONS.md:321`) |
| "Oscillators (RSI, z-score)" — "their full warm-up window of monthly closes before the first value renders" | 97–98 | **VERIFIED and available now.** These need monthly closes, which reach back to ~Dec 2020 (D-002) — so oscillators are the one family unaffected by the seam. The page never tells the reader this, which is a missed opportunity given §6.6(b) |
| "Seasonality" — "one observed cycle so far — labeled illustrative until there are three" | 99–100 | **FALSE or incoherent, depending on the input series.** From monthly prices (D-002, ~Dec 2020) there are ~5 complete cycles, so "three" was passed years ago. From the per-sale ledger (D-001, late Jul 2026) there is **not yet one** cycle. Either way "one observed cycle so far" is wrong. The three-cycle rule itself is a sound policy |
| "Composites (G1–G4)" — "every component signal individually sufficient — a composite never fires on partial inputs" | 101–102 | **UNVERIFIABLE** — G1–G4 are a CardStock construct with no counterpart in the scraper. The rule is sound and consistent with D-038 |

> **"An indicator that doesn't have enough history to be trustworthy is locked, with its unlock date shown."** (line 86)

**Verdict: VERIFIED as policy** — D-038 (`DECISIONS.md:237`) "v1 ships the full UI with locks visible". **But the page cannot honour "its unlock date shown" while omitting the anchor date** those unlock dates are computed from, and its one worked example (line 94) is wrong.

### 6.12 Honesty policy

| Claim | Line | Verdict |
|---|---|---|
| "No projected or extrapolated data points — a partial month renders as partial, never as a forecast." | 112 | **VERIFIED** — consistent with line 80, and structurally guaranteed by D-041 |
| "Backtests start at each screen's honest floor — the first date every filter in it could actually be computed — not at the start of our archive." | 113 | **VERIFIED as policy, mis-anchored in practice.** The rule is exactly right and matches D-033's intent; but "the start of our archive" is presented on this page as Aug 2023 (§6.6) and the honest floor should be 2026-09-01 (D-033). Right principle, wrong numbers feeding it |
| "A backtest that mostly found one set's moment says so, instead of presenting it as a repeatable pattern." | 114 | **UNVERIFIABLE** — a product behaviour with no data-side counterpart. Sound, unusually candid, no receipt either way |
| "Missing metadata renders as METADATA PENDING, not as a silent blank or a guess." | 115 | **UNVERIFIABLE from the data model; well-founded as a need.** Real gaps exist — `cards` carries nullable identity fields, and `parse_failures` is a first-class table (`PokemonDbContext.cs:8–22`, D-030). The literal token is a display-vocabulary decision |
| "When a grader restates history, the restatement window stays visibly marked." | 116 | **Premise VERIFIED** (`DATA_MODEL.md:209–213`, PSA ~June 2026, 397 → 99,246); **marking UNVERIFIABLE** (unbuilt). Duplicate of line 63 — see §8 |

### 6.13 Disclaimers and closing note

| Claim | Line | Verdict |
|---|---|---|
| "fan-made analytics project… not affiliated with… Nintendo, The Pokémon Company, or any grading company or marketplace" | 124 | **VERIFIED as consistent.** Entities named correctly (marketplaces `DATA_MODEL.md:227`; graders `:204`). **Narrower than the Legal page's version** (`Cardstock Legal.dc.html:67` adds "Creatures Inc." and "or marketplace"). Neither version addresses **image copyright**, the genuinely open risk in D-010 (`DECISIONS.md:90` — "No repo records reading any terms of service") |
| "Nothing here is financial advice… signals describe the past, not the future." | 125 | **VERIFIED** — matches `Cardstock Legal.dc.html:64` and is structurally guaranteed by D-041 |
| "Collectible prices are volatile and thinly traded" | 125 | **VERIFIED by the data's own shape** — the ~30-row bucket windows (`DATA_MODEL.md:101`) and the burn-window scheduler exist precisely because per-card sale flow is low |
| "Every stat's tooltip names its source and window." | 130 | **UNVERIFIABLE and a large commitment.** No tooltip inventory exists. Note the trap: if tooltips name sources honestly they must say pricecharting.com, which this page never does (§6.0) — the two claims cannot both be satisfied without changing this page |
| "Where every number comes from, how fresh it is, and what we refuse to show." | 48 | **FALSE as a summary of the page delivered.** "Where every number comes from" omits the single source (§6.0); "how fresh it is" is wrong (§6.9); "what we refuse to show" describes an exclusion pipeline that does not exist (§6.4) |

### 6.14 Audit summary — every FALSE and UNVERIFIABLE claim

| # | Claim | Line | Verdict |
|---|---|---|---|
| 1 | Subtitle: "Where every number comes from, how fresh it is…" | 48 | FALSE as a summary — source undisclosed, freshness wrong |
| 2 | "The Apr ’25 seam" (pill) | 52 | FALSE — D-001, late Jul 2026, and per-card not global |
| 3 | "Prices come from realized sales only" | 62 | FALSE for the plotted series — it is the source site's own monthly average |
| 4 | "Asking prices never enter an aggregate" | 62 | UNVERIFIABLE — we do not compute the aggregate |
| 5 | "cover under 5% of rows" | 62 | UNVERIFIABLE — only "most rows have none" (`DATA_MODEL.md:232`) |
| 6 | "Populations come from the public census reports the grading companies publish monthly" | 63 | FALSE ×3 — scraped from `pop_data`, not from graders, no monthly cadence |
| 7 | "we mark the affected window on charts" | 63 | UNVERIFIABLE — detection is an ops alert; chart marking unbuilt |
| 8 | "Excluded: bulk lots, ambiguous grade or damage notes, unmatched printings, unverified marketplaces" | 64 | FALSE — no sale-content filtering exists in the parser |
| 9 | "Coverage is deepest for English-language cards graded PSA, BGS, CGC, SGC, ACE, and TAG" | 64 | UNVERIFIABLE — no language data; census exists for psa/cgc only |
| 10 | "The April 2025 seam" (heading) | 69 | FALSE — D-001 |
| 11 | "Before April 2025 our archive holds monthly aggregates" | 71 | FALSE — D-001 |
| 12 | "averages and **sale counts**" | 71 | FALSE — no count column; volume history "unavailable from source, permanently" |
| 13 | "back to August 2023" | 71 | FALSE — ~Dec 2020 (D-002); understates depth by ~32 months |
| 14 | "From April 2025 forward we keep the per-sale ledger" | 71 | FALSE — late Jul 2026, per-card (D-001) |
| 15 | "every individual transaction" | 71 | FALSE — ~30-row buckets; older rows discarded by the source forever |
| 16 | "That boundary is drawn as a marker on charts" | 72 | FALSE as stated — the seam is ragged, one marker per card |
| 17 | "their history starts there" | 72 | FALSE — starts per-card (D-001), displayed from 2026-09-01 (D-033) |
| 18 | "Sales data refreshes daily" | 79 | FALSE — priority queue with a 30-day starvation floor |
| 19 | "census data lands when graders publish, roughly monthly" | 79 | FALSE — lands on our visit; source keeps no history |
| 20 | "The footer stamp on **every page**" | 79 | FALSE — this page and the Legal page have no footer |
| 21 | "it aggregates the sales recorded so far" | 80 | FALSE mechanism — it is the source's revised monthly average |
| 22 | "~6 months of ledger (≈ Jan 2027)" | 94 | FALSE — ~Mar 2027 from the D-033 floor; also an authored date D-033 bans |
| 23 | "one observed cycle so far" | 100 | FALSE either way — ~5 cycles of monthly prices, or 0 of ledger |
| 24 | "Composites (G1–G4)… never fires on partial inputs" | 102 | UNVERIFIABLE — CardStock construct, no data-side counterpart |
| 25 | "Backtests start at each screen's honest floor… not at the start of our archive" | 113 | Policy VERIFIED; mis-anchored by the wrong archive start (line 71) |
| 26 | "A backtest that mostly found one set's moment says so" | 114 | UNVERIFIABLE — unbuilt product behaviour |
| 27 | "Missing metadata renders as METADATA PENDING" | 115 | UNVERIFIABLE — display-vocabulary decision, unbuilt |
| 28 | "When a grader restates history, the restatement window stays visibly marked" | 116 | Premise VERIFIED; marking UNVERIFIABLE |
| 29 | "Every stat's tooltip names its source and window" | 130 | UNVERIFIABLE — and in tension with §6.0 |
| 30 | **The 2026-09-01 sufficiency floor** | *absent* | **MISSING** — D-033 (`DECISIONS.md:322`) says this page should carry the floor and its reason. It does not |

**Verified claims, for balance:** closed-month immutability (80), never-project (80, 112), the ledger's date/venue/grade fields (71), the post-seam indicator reasoning (72), the lock-don't-degrade policy (86), oscillator warm-up (98), the restatement premise (63, 116), both disclaimers (124, 125), and "thinly traded" (125).

---

## Corrected copy — build this

Written 2026-08-10 to resolve the 22 FALSE and 13 UNVERIFIABLE claims in §6. **Every statement below is traceable to a receipt**; the receipt is given in the margin note after each block. The prototype's copy is superseded — build from here.

Voice follows `HANDOFF.md` §1: precise numbers over adjectives, no hype, no exclamation marks.

---

### Where the numbers come from

> Every price, sale, and population figure on Cardstock comes from **pricecharting.com**. We do not collect from marketplaces ourselves.
>
> Two things follow from that, and they matter when you read a chart:
>
> The **individual sales** we list are real completed transactions, recorded as PriceCharting reported them — date, venue, grade, and price.
>
> The **monthly price line is not built from those sales.** It is PriceCharting's own monthly average, which we store and chart unaltered. We do not recompute it, and its method is theirs, not ours.

*Receipts: `DATA_MODEL.md:89` (all data from pricecharting.com); `:93–94`, `:186` (`price_cents` is the site's monthly average); `:217` (sales are one immutable row per completed sale). Source named per D-059.*

---

### What we hold, and from when

> | | Covers | Begins |
> |---|---|---|
> | Monthly average prices | 6 grade tiers | **~December 2020**, complete for every card |
> | Individual sales | 19 grade labels | The first time we visited that card |
> | Population census | PSA and CGC only | The first time we visited that card |
>
> **Our sales and census history does not start on a single date.** It starts the first time we visited each card. We began collecting on 28 July 2026, and each card entered the record when its turn came — so the boundary sits in a different place for every card, and we draw it where it actually falls rather than pretending it is one line.
>
> Monthly prices are the exception. The first visit to a card retrieves its entire price chart at once, so that history is complete back to about December 2020 regardless of when we first saw it.

*Receipts: `DATA_MODEL.md:373`, `:176–177` (backfilled to ~Dec 2020 on first visit); `:404` (first deployment 2026-07-28); `:397` (population history begins at first visit); `:204` (census is `psa` or `cgc` only); D-001; D-002; D-003.*

---

### What we cannot know

> Some things are not missing from Cardstock — they do not exist anywhere, and no amount of collecting will produce them.
>
> **Sale counts before we started watching.** PriceCharting publishes no historical volume series. Nobody has this.
>
> **Sales older than roughly the last 30 in each grade bucket.** PriceCharting keeps a rolling window and discards what falls off it. Once a sale scrolls out, it is gone for everyone.
>
> **Census history before our first visit.** PriceCharting publishes a current snapshot with no history attached.
>
> **Which company graded a card below grade 10.** PriceCharting pools every grading company into a single figure for grades 1 through 9.5, and splits by company only at 10.

*Receipts: `DATA_MODEL.md:113–115`, `:481–482` (historical volume permanently unavailable); `:101–102`, `:118–119` (~30-row bucket windows, discarded forever); `:104`, `:120–121` (current census snapshot only); ADR-0005.*

---

### On pooled grades

> Below grade 10, a price covers every grading company at once — a "Grade 8" figure includes PSA, CGC, BGS and others together.
>
> About **91%** of the identifiable volume in those buckets is PSA, so the pooled figure tracks PSA closely. A CGC card of the same grade typically trades below it, by roughly a third at grade 8.
>
> We show the pooled number and label it as pooled. **We do not apply a multiplier to estimate a company-specific price** — that would present a guess with the same confidence as an observation.

*Receipts: ADR-0005 (74.7% PSA / 6.0% CGC in Grade 8; ~91% of identifiable volume; CGC ≈ 0.68× PSA; multiplier rejected as statistically dishonest); D-022.*

---

### How fresh it is

> Cards are visited continuously, one at a time, in priority order — not on a fixed schedule. A card that is selling quickly is visited sooner. A quiet card may go a month or more between visits.
>
> **Opening a card page triggers a fresh visit**, so a card page shows sales up to that moment.
>
> The **current month's price revises.** PriceCharting recalculates it as sales land, and we pick up the new figure on our next visit. It renders as a hollow, dashed point for exactly that reason. **Closed months never change** — once a month closes, its value is fixed permanently.

*Receipts: `DATA_MODEL.md:322` (continuous, one card at a time); `:329–335` (priority queue, 30-day starvation floor); `:98–99` (closed months immutable server-side); `:189–191` (composite PK ends in `observed_at` because the current month revises); D-024 (`express-visit` on card open).*

---

### The floor

> **No metric on Cardstock counts an observation recorded before 1 September 2026.**
>
> This is a deliberate cutoff, not the date our data begins. We were still stabilising the collector through August 2026, so we discard our own earliest observations rather than trust them. September 1st is the first date we are willing to stand behind.
>
> Every unlock countdown on this site is measured from that floor.

*Receipt: D-033. **This section does not exist in the prototype and must be added** — D-046 §6.11 confirmed the floor appears nowhere in the file.*

---

### Honesty policy

> **No projected or extrapolated points.** A partial month renders as partial, never as a forecast.
>
> **A metric below its sufficiency floor renders a state, not a number.** It will tell you which rule it failed and when it will pass.
>
> **When a grader restates a past census, we keep what we already recorded.** Restatements happen — PSA restated in June 2026 and one card's grade cell moved from 397 to 99,246. We write the new figures alongside the old ones. We never rewrite history.
>
> **Backtests start at the first date every filter in them could actually be computed**, not at the start of our records.

*Receipts: `DATA_MODEL.md:209–213` (the PSA restatement and its magnitude, verbatim); Rule 1, append-only (ADR-0001); D-038; D-041.*

> ⚠ **Build note, not copy:** the prototype also promised "we mark the affected window on charts." D-046 §6.2 confirmed that marking is **unbuilt** — detection exists only as an operational alert. Either build the chart annotation or do not make the promise. It is omitted above deliberately.

---

### Disclaimers

Unchanged from the prototype, and verified accurate in §6.13 — fan-made, not affiliated with Nintendo, The Pokémon Company, Creatures Inc., any grading company or marketplace; nothing here is financial advice; signals describe the past, not the future.

*One fix: the prototype's disclaimer at `:124` is narrower than the Legal page's at `Cardstock Legal.dc.html:67`. Use the Legal page's wording in both places.*

---

### Claims deliberately removed

Not corrected — **deleted**, because no true version exists:

| Removed | Why |
|---|---|
| "sale counts, back to August 2023" | The data has never existed (`DATA_MODEL.md:481`) |
| "Excluded: bulk lots, ambiguous grade or damage notes…" | No exclusion pipeline exists — grep over the scraper returns no sale-content filtering |
| "Sales data refreshes daily" | Contradicted by the 30-day starvation floor |
| "the footer stamp on every page" | Neither this page nor Legal has a footer |
| "Asking prices never enter an aggregate… under 5% of rows" | We did not compute the aggregate, and 5% has no receipt |
| "English-language cards" | No language field exists anywhere in the scraper |
| "the April 2025 seam" (×4) | Fifteen months before the collector's first commit |

---

## 7. Open questions

1. **What replaces "the April 2025 seam" in the UI?** D-001 makes the seam per-card. One marker per card is honest but complicates every chart; a single global marker at the D-033 floor (2026-09-01) is simpler and safe because it errs toward LOCKED (`DECISIONS.md:314`). Not decided — this is a design decision that must precede the copy rewrite.
2. **Does the page disclose pricecharting.com by name?** §6.0. Naming a single upstream is a product-strategy call as well as an honesty call, and line 130's tooltip promise arguably forces it.
3. **Are sale counts removed from the copy entirely, or reframed as post-seam-only?** They are derivable forward of each card's seam (`DATA_MODEL.md:391`) — the true statement is nearly the opposite of line 71's.
4. **Where does the "under 5%" figure come from?** No receipt exists. Either run the query and cite it, or cut the number.
5. **Does the exclusion pipeline (line 64) get built, or does the copy get corrected?** It describes real quality work that would improve the product. Right now it describes work that does not exist.
6. **How is "unlock date shown" computed and rendered?** Requires the D-033 anchor plus per-signal denominators. Depends on D-015 (`DECISIONS.md:460`) for where that computation lives.
7. **What is the true corpus lap time?** D-033 (`DECISIONS.md:324`) flags "~12.4-day corpus lap" as unverified. Whatever replaces "refreshes daily" needs a real number.
8. **Which input series feeds Seasonality?** Determines whether line 100 becomes "five cycles" or "not yet one".
9. **What are G1–G4?** Referenced as if the reader knows. No definition on this page and no counterpart in the scraper.
10. **Do the two unaffiliated disclaimers converge?** Line 124 here vs `Cardstock Legal.dc.html:67`. And should either mention image licensing (D-010)?
11. **Route and inbound links.** `/about-data` is proposed, not decided; only the Legal page currently links here, which is thin for a page this load-bearing.
12. **Who owns re-auditing this page?** Every date on it is a claim that decays. D-033's "one anchor date plus denominators" rule should govern the implementation so the page cannot silently rot.

---

## 8. Contradictions found

1. **The page vs D-001 — four times.** "Apr ’25" (52), "April 2025" (69), and twice in line 71. D-001 (`DECISIONS.md:22`) is titled with the exact negation: "not Apr 2025 / Jan 2026". The owner's words, `DECISIONS.md:31`: "That's completely false. It just started this month."
2. **The page vs D-002.** "back to August 2023" (71) vs "~Dec 2020" — and the page understates rather than overstates, hiding its own strongest asset.
3. **The page vs `DATA_MODEL.md:113–115` and `:482`.** "sale counts" (71) vs "**Unavailable from source, permanently:** historical sales volume". The page advertises data that has never existed and cannot be obtained.
4. **The page vs itself, on sale counts.** Line 71 puts counts *before* the seam; `DATA_MODEL.md:391` makes them derivable only *forward* of it. The page's own line 72 explains that individual sales exist only after the seam — which is incompatible with line 71's pre-seam counts.
5. **The page vs itself, on the footer.** Line 79 says "The footer stamp on **every page**"; this page has no footer, and neither does `Cardstock Legal.dc.html`.
6. **The page vs D-033 — omission plus arithmetic.** `DECISIONS.md:322` says this page "should carry the floor **and its reason**"; it carries neither. And "≈ Jan 2027" (94) is ~2 months earlier than the floor allows, i.e. wrong "in the direction that overstates readiness" — the exact failure D-032 (`DECISIONS.md:342`) was raised to stop, now recurring in public copy.
7. **The page vs D-033's no-authored-dates rule.** `DECISIONS.md:319` — "**No authored ratios, ever again.**" Line 94 authors a date.
8. **The page vs D-003 on coverage.** Line 64 implies six graders throughout; D-003 (`DECISIONS.md:44–57`) documents three separate vocabularies — 6 price tiers, 19 sales labels, arbitrary user entry — and `DATA_MODEL.md:204` limits census to psa/cgc.
9. **Sources section vs the honesty policy.** "we mark the affected window on charts" (63) and "the restatement window stays visibly marked" (116) are the same promise stated twice. Not contradictory, but two copies of an unimplemented commitment that will drift.
10. **D-009 is incomplete.** `DECISIONS.md:385` tracks the Apr '25 seam surviving in `DESIGN_NOTES.md:35`. It does not record that the same error is in this **public-facing prototype**, in a section heading and a nav pill. D-009 should be amended to cover `Cardstock About Data.dc.html:52, 69, 71, 72`.
11. **This page vs the Legal page's data-accuracy claim.** `Cardstock Legal.dc.html:65` — "sales records and census reports contain errors upstream of us" — implies a first-party/third-party boundary that §6.0 shows does not exist. The two pages reinforce each other's misframing.
