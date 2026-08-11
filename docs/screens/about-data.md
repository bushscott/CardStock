# Screen spec — About our data

**Source of truth:** `CardStock Mockup/Cardstock About Data.dc.html` (136 lines), read directly 2026-08-10.
All line citations below are to that file unless another path is named.

> ⚠️ **Read §6 before implementing.** This is a public methodology page. Its layout and copy
> are authoritative per D-040, but a large fraction of its *factual claims about the data are
> wrong* — and wrong in the direction that overstates what CardStock has. The seam date, the
> price-history start date, the existence of pre-seam sale counts, and the census provenance
> are all contradicted by `../PokemonInvestBatch/DATA_MODEL.md`. Build the layout from this
> spec; do **not** ship the copy without the owner rulings listed in §7.

---

## 1. Identity

| Field | Value |
|---|---|
| **Screen name** | About our data |
| **`data-screen-label`** | `About our data` (:30) |
| **`<h1>`** | `About our data` (:47) |
| **Route** | `/about-data` — `HANDOFF.md:81` (Tier 2) |
| **Deep-link anchors** | `#sources` (:59), `#seam` (:68), `#refresh` (:76), `#sufficiency` (:84), `#honesty` (:107), `#disclaimers` (:121) |
| **Auth** | Public. Nav chrome shows the account circle (:43) but no state depends on a session |
| **Interactivity** | None. Static content + in-page anchors. Safe as static SSR |

**Purpose**, stated on the page itself (:48):

> "Where every number comes from, how fresh it is, and what we refuse to show."

`DESIGN_NOTES.md:127` describes it as a *"static methodology page, linked from Home footer
'About our data': sources & coverage, the Apr '25 seam, refresh & closed months,
sufficiency-rules table, honesty policy, disclaimers (fan-made, not affiliated, not financial
advice). Pill anchor-nav, 820px column."*

**Why it is load-bearing:** D-038 (`DECISIONS.md:245`) — *"`Cardstock About Data.dc.html` must
carry the floor and its reason"*. With v1 shipping the full UI with locks visible, this page is
the single explanation surface for every locked row in the product. It is not optional content.

### Inbound links (verified by grep across the mockup folder, 2026-08-10)

| From | Target | Line |
|---|---|---|
| `Cardstock Home.dc.html` footer — "About our data" | page root | `:308` |
| `Cardstock Screener.dc.html` — "sufficiency rules ⓘ" | `#sufficiency` | `:384` |
| `Cardstock Charts.dc.html` — **"Why no candlesticks? → About our data"** | page root | `:182` |
| `Cardstock Legal.dc.html` — "(see About our data)" | page root | `:65` |

> ⚠️ **The Charts link is broken as content.** `Cardstock Charts.dc.html:182` sends the user
> here to learn "Why no candlesticks?" and **this page never mentions candlesticks anywhere.**
> See §6-A13 and §7-Q6.

### Outbound links

Nav only (:33–:43): Home, Screener, Charts, Binder, Browse, search component, Profile.
**No link to `Cardstock Legal.dc.html`** — the relationship is one-directional (Legal links
here at `Cardstock Legal.dc.html:65`, not the reverse). See §7-Q9.

---

## 2. Layout

Shares the app shell with `Cardstock Legal.dc.html` (`DESIGN_NOTES.md:148` — *"Privacy & Terms
on the About Data shell"*). Build one layout component; both pages consume it.

```
┌─ nav (48px, sticky, top:0, z-index:20) ──────────────────────────────┐
│ logo+wordmark → Home · Home Screener Charts Binder Browse · ⟶ ·      │
│ <cardstock-search> · account circle "O" → Profile                    │
└──────────────────────────────────────────────────────────────────────┘
      ┌─ content column: max-width 820px, margin 0 auto ─┐
      │ padding: 32px 24px 80px; box-sizing: border-box  │
      │                                                  │
      │  h1 "About our data"          27px/700/Inter Tight
      │  subtitle                      14.5px, --mut2
      │  ┌ pill anchor-nav (flex-wrap, gap 6px) ────────┐
      │  │ Sources · The Apr '25 seam · Refresh &       │
      │  │ closed months · Sufficiency rules ·          │
      │  │ Honesty policy · Disclaimers                 │
      │  └───────────────────────────────────────────────┘
      │  ┌ section#sources      ─ card ┐ mb 14px
      │  ┌ section#seam         ─ card ┐ mb 14px
      │  ┌ section#refresh      ─ card ┐ mb 14px
      │  ┌ section#sufficiency  ─ card ┐ mb 14px  (contains 2-col grid table)
      │  ┌ section#honesty      ─ card ┐ mb 14px  (contains <ul>)
      │  ┌ section#disclaimers  ─ card ┐ (no mb — last)
      │  closing note                   12.5px, --mut2, mt 20px
      └──────────────────────────────────────────────────┘
```

### Measured tokens

| Element | Spec | Line |
|---|---|---|
| Page root | `min-height:100vh`, `background: var(--bg,#FAFAF7)`, `color: var(--ink,#1C1C1E)`, `font-size:15px`, flex column | :30 |
| Nav | `height:48px`, `background: var(--card,#FFFFFF)`, `border-bottom:1px solid var(--line,#E4E4E0)`, `gap:24px`, `padding:0 20px`, `position:sticky`, `top:0`, `z-index:20` | :32 |
| Content column | `width:100%`, `max-width:820px`, `margin:0 auto`, `padding:32px 24px 80px` | :46 |
| `h1` | Inter Tight, `27px`, `700`, `margin:0 0 6px` | :47 |
| Subtitle | `14.5px`, `var(--mut2,#6B6B66)`, `margin-bottom:18px` | :48 |
| Pill row | flex, `flex-wrap:wrap`, `gap:6px`, `margin-bottom:28px` | :50 |
| Pill | `13px`/`600`, `1px solid var(--line)`, `border-radius:99px`, `padding:4px 12px`, `background: var(--card)` | :51–:56 |
| Section card | `background: var(--card,#FFFFFF)`, `1px solid var(--line,#E4E4E0)`, `border-radius:8px`, `padding:20px 22px`, `margin-bottom:14px` | :59, :68, :76, :84, :107 |
| Last section card | identical **without** `margin-bottom` | :121 |
| **`scroll-margin-top`** | `62px` on every `<section>` — clears the 48px sticky nav | :59, :68, :76, :84, :107, :121 |
| Section `h2` | Inter Tight, `18.5px`, `700`, `margin:0 0 10px` | :60, :69, :77, :85, :108, :122 |
| Body prose | `14.5px`, `line-height:1.6`, `color: var(--mut,#5B5B57)` | :61, :70, :78, :86, :109, :123 |
| Inline emphasis | `<strong style="color: var(--ink,#1C1C1E)">` — strong is a *color* promotion, not just weight | :62–:64, :71, :80 |
| Closing note | `12.5px`, `var(--mut2,#6B6B66)`, `margin-top:20px` | :129 |

### Sufficiency table (inside `#sufficiency`)

| Property | Value | Line |
|---|---|---|
| Wrapper | `1px solid var(--line)`, `border-radius:6px`, `overflow:hidden` | :87 |
| Grid | `display:grid`, `grid-template-columns: 1.2fr 2fr`, `gap:0`, `font-size:13.5px` | :88 |
| Header cells | `padding:8px 12px`, `background: var(--mutbg,#F3F3EE)`, `600`, `12px`, `letter-spacing:.06em`, `text-transform:uppercase`, `var(--mut2)` | :89–:90 |
| Header labels | `Signal`, `Needs` | :89, :90 |
| Signal cells | `padding:8px 12px`, `border-top:1px solid var(--line4,#F0F0EC)`, **JetBrains Mono `12.5px`** | :91, :93, :95, :97, :99, :101 |
| Needs cells | same padding/border, `color: var(--mut)` | :92, :94, :96, :98, :100, :102 |

Note the grid is a flat cell sequence, not `<table>` markup. For accessibility, implement with
`role="table"`/`role="row"`/`role="cell"` or convert to a real `<table>` with the same visual
metrics — the prototype's flat grid gives screen readers no row association.

### Theming

| Concern | Spec | Line |
|---|---|---|
| Dark tokens | Full `:root[data-theme="dark"]` override block | :21 |
| CVD (colorblind) | `--pos`/`--neg` swapped under `[data-cvd="1"]`, and a dark+CVD combination | :22–:24 |
| Dark logo teal | `--logoTeal: #3FBFAD` set in a **separate** rule (:25) — Legal folds it into the main dark block (`Cardstock Legal.dc.html:21`). Harmless divergence; unify in Blazor |
| Pre-paint script | Reads `localStorage` `cardstock-cvd` and `cardstock-theme`, stamps `data-cvd` / `data-theme` on `<html>` before first paint | :28 |
| Focus ring | `*:focus-visible { outline: 2px solid var(--acc); outline-offset:1px; border-radius:2px }` | :20 |

⚠️ **This page uses no `--pos`/`--neg` tokens** (no numbers are rendered), yet ships the full
CVD palette (:22–:24). That is shell boilerplate, not page requirement — but keep it, because
the shell is shared.

⚠️ **Google Fonts are loaded from a third-party CDN** (:12–:14: `preconnect` to
`fonts.googleapis.com` and `fonts.gstatic.com`, plus a stylesheet link). See
`docs/screens/legal.md` §6 — this directly undercuts the Legal page's "no third-party
trackers" promise. Self-host Inter / Inter Tight / JetBrains Mono.

---

## 3. Content inventory

Every substantive claim on the page, quoted exactly, with its line.

### 3.1 Header

| # | Quote | Line |
|---|---|---|
| C1 | "About our data" | :47 |
| C2 | "Where every number comes from, how fresh it is, and what we refuse to show." | :48 |

### 3.2 Pill anchor-nav (:50–:57)

| Label | Href | Line |
|---|---|---|
| "Sources" | `#sources` | :51 |
| "The Apr ’25 seam" | `#seam` | :52 |
| "Refresh & closed months" | `#refresh` | :53 |
| "Sufficiency rules" | `#sufficiency` | :54 |
| "Honesty policy" | `#honesty` | :55 |
| "Disclaimers" | `#disclaimers` | :56 |

Note the pill (:52) uses a curly apostrophe — `The Apr ’25 seam` — while the section heading
(:69) spells it out as `The April 2025 seam`. Preserve both strings verbatim if the copy
survives §7-Q1.

### 3.3 `#sources` — "Sources & coverage" (:59–:66)

| # | Quote | Line |
|---|---|---|
| C3 | "**Prices** come from realized sales only — completed marketplace listings and major auction results." | :62 |
| C4 | "Asking prices never enter an aggregate; the rare Listed figures you see are labeled as such and cover under 5% of rows." | :62 |
| C5 | "**Populations** come from the public census reports the grading companies publish monthly." | :63 |
| C6 | "When a grader restates a past census (it happens), we mark the affected window on charts rather than silently rewriting history." | :63 |
| C7 | "**Excluded**: bulk lots, listings with ambiguous grade or damage notes, sales where the card can't be matched to one printing, and marketplaces whose sold data we can't verify." | :64 |
| C8 | "Coverage is deepest for English-language cards graded PSA, BGS, CGC, SGC, ACE, and TAG, plus raw sales." | :64 |

### 3.4 `#seam` — "The April 2025 seam" (:68–:74)

| # | Quote | Line |
|---|---|---|
| C9 | "The April 2025 seam" (h2) | :69 |
| C10 | "Before April 2025 our archive holds **monthly aggregates** — averages and sale counts, back to August 2023." | :71 |
| C11 | "From April 2025 forward we keep the **per-sale ledger**: every individual transaction with its date, venue, and grade." | :71 |
| C12 | "That boundary is drawn as a marker on charts." | :72 |
| C13 | "Indicators that need individual sales — churn, price dispersion, Amihud illiquidity, cross-marketplace gap, discount-to-list — can only be computed after it, which is why their history starts there and why backtests refuse to reach further back (the \"honest floor\")." | :72 |

### 3.5 `#refresh` — "Refresh & closed months" (:76–:82)

| # | Quote | Line |
|---|---|---|
| C14 | "Sales data refreshes daily; census data lands when graders publish, roughly monthly." | :79 |
| C15 | "The footer stamp on every page tells you how fresh what you're looking at is." | :79 |
| C16 | "Monthly series only include **closed months**." | :80 |
| C17 | "The current month appears as a hollow, dashed point — it aggregates the sales recorded so far and will keep revising until the month closes." | :80 |
| C18 | "We never project or extrapolate it." | :80 |

### 3.6 `#sufficiency` — "Sufficiency rules" (:84–:105)

| # | Quote | Line |
|---|---|---|
| C19 | "An indicator that doesn't have enough history to be trustworthy is locked, with its unlock date shown — not rendered anyway with a warning buried in a tooltip. The rules:" | :86 |

| Signal (line) | Needs (line) |
|---|---|
| "Churn 30d" (:91) | "30 days of per-sale ledger in that grade bucket; starts LOW CONFIDENCE for its first 30 days" (:92) |
| "Weekly bars" (:93) | "~6 months of ledger (≈ Jan 2027)" (:94) |
| "Daily bars" (:95) | "~12 months of ledger, liquid cards only" (:96) |
| "Oscillators (RSI, z-score)" (:97) | "their full warm-up window of monthly closes before the first value renders" (:98) |
| "Seasonality" (:99) | "one observed cycle so far — labeled illustrative until there are three" (:100) |
| "Composites (G1–G4)" (:101) | "every component signal individually sufficient — a composite never fires on partial inputs" (:102) |

### 3.7 `#honesty` — "Honesty policy" (:107–:119)

| # | Quote | Line |
|---|---|---|
| C20 | "Some things we simply don't show:" | :110 |
| C21 | "No projected or extrapolated data points — a partial month renders as partial, never as a forecast." | :112 |
| C22 | "Backtests start at each screen's honest floor — the first date every filter in it could actually be computed — not at the start of our archive." | :113 |
| C23 | "A backtest that mostly found one set's moment says so, instead of presenting it as a repeatable pattern." | :114 |
| C24 | "Missing metadata renders as METADATA PENDING, not as a silent blank or a guess." | :115 |
| C25 | "When a grader restates history, the restatement window stays visibly marked." | :116 |

### 3.8 `#disclaimers` — "Disclaimers" (:121–:127)

| # | Quote | Line |
|---|---|---|
| C26 | "Cardstock is a fan-made analytics project. It is not affiliated with, endorsed by, or sponsored by Nintendo, The Pokémon Company, or any grading company or marketplace." | :124 |
| C27 | "Pokémon names and card references are used for identification only; all trademarks belong to their owners." | :124 |
| C28 | "Nothing here is financial advice. Collectible prices are volatile and thinly traded; signals describe the past, not the future. Do your own research before spending money on cardboard." | :125 |

### 3.9 Closing note

| # | Quote | Line |
|---|---|---|
| C29 | "Questions about a number? Every stat's tooltip names its source and window." | :130 |

---

## 4. States / Interactions

**There are no dynamic states.** No `state = {}` block, no `onClick`, no `{{ }}` bindings, no
conditional rendering. The page is pure static markup plus the shared shell.

| Interaction | Behaviour | Line |
|---|---|---|
| Six anchor pills | In-page jump to `#sources` / `#seam` / `#refresh` / `#sufficiency` / `#honesty` / `#disclaimers` | :51–:56 |
| Anchor landing offset | `scroll-margin-top:62px` on every target section, so the sticky 48px nav never covers the `h2` | :59, :68, :76, :84, :107, :121 |
| Nav links | Full navigation to Home / Screener / Charts / Binder / Browse / Profile | :33–:43 |
| Search | `<cardstock-search>` web component, shared across all app pages (`HANDOFF.md:86`) | :42 |
| Link hover | `a:hover { color: var(--accH); text-decoration: underline }` — global | :19 |
| Focus | `outline: 2px solid var(--acc)`, offset 1px | :20 |
| Theme / CVD | Applied pre-paint from `localStorage`; **no toggle on this page** (it lives on Profile) | :28 |

### Deep-link contract

`Cardstock Screener.dc.html:384` links to `Cardstock About Data.dc.html#sufficiency`. The
Blazor route must therefore preserve `#sufficiency` as a stable fragment id forever, in
addition to `/about-data` itself. Treat all six ids as a public API.

### Implementation notes

- No `@rendermode` needed. Static SSR is sufficient and correct (relevant to D-013).
- The page performs **zero data reads**. Nothing here touches Postgres.
- Once §7 is resolved, several numbers become *computed* rather than authored — see §5-R6.

---

## 5. Rules and invariants

| # | Rule | Source |
|---|---|---|
| R1 | Content column is exactly `max-width:820px`, centred, `padding:32px 24px 80px` | :46 |
| R2 | Every section is an anchor target with `scroll-margin-top:62px`. Adding a section means adding a pill and an id | :51–:56, :59–:121 |
| R3 | The last section carries no `margin-bottom` | :121 |
| R4 | `<strong>` inside prose promotes color to `--ink`, not merely weight | :62–:64, :71, :80 |
| R5 | Section order is fixed and matches pill order: sources → seam → refresh → sufficiency → honesty → disclaimers | :51–:56 vs :59–:121 |
| R6 | **No authored ratios or dates.** D-033 (`DECISIONS.md:319`): *"Numerators are arithmetic against today. No authored ratios, ever again."* The literal `"≈ Jan 2027"` at :94 violates this and must become a computed value against the 2026-09-01 floor | D-033 |
| R7 | This page is the **only** explanation surface for locks. D-038 (`DECISIONS.md:243–245`): the sufficiency engine is on the critical path and *"`Cardstock About Data.dc.html` must carry the floor and its reason"* | D-038 |
| R8 | Never project or extrapolate — restated three times on the page (:80, :112, and implied at :113) and corroborated by `DESIGN_NOTES.md:49` (*"NO projection/extrapolation to month-end, ever"*) | :80, :112 |
| R9 | The disclaimer paragraph (:124) must survive any rewrite verbatim in substance — it is the trademark shield | :124 |
| R10 | Sufficiency table is the canonical public statement of lock rules; `DISPLAY_VOCABULARY.md` §2/§9/§10 must not contradict it | `HANDOFF.md:132` |
| R11 | The page must name its data source. It currently never does — see §6-A14. `sales.title`-style third-party provenance and D-010's open licensing question both argue for explicit attribution | D-010 |

---

## 6. ⚠ Factual audit

Verdicts are against `../PokemonInvestBatch/DATA_MODEL.md` (Tier 1, authoritative for data) and
`DECISIONS.md`. Every receipt was opened directly on 2026-08-10.

**Summary: 8 FALSE, 9 UNVERIFIABLE, 6 VERIFIED, 1 critical omission.**

---

### A1 — C3 "Prices come from realized sales only" (:62) — ⚠ **UNVERIFIABLE, and misleading about mechanism**

CardStock's price series is not built from sales at all. It is `price_months`, which is a
verbatim copy of pricecharting.com's own chart:

- `DATA_MODEL.md:95` — *"**`VGPC.chart_data`** — the price chart: **six series of monthly average prices**, in cents, reaching back to ~December 2020."*
- `DATA_MODEL.md:186` — `price_cents | int | ✓ | site — monthly average price`
- `DATA_MODEL.md:175–178` — *"A card's first visit backfills the site's entire chart… so deep price history exists for every card from the moment it's first visited."*

Whether pricecharting composes that average from realized sales only is **the third party's
methodology, and nothing in the scraper repo documents it.** The sentence states an upstream
editorial policy as a first-party fact. The two data assets are independent: the chart (:95)
and the completed-sales tables (:100) are parsed separately from the same page and never feed
each other.

**Fix:** "Prices are pricecharting.com's monthly average for each grade tier" — accurate, and
it costs the page nothing.

---

### A2 — C4 "the rare Listed figures… cover under 5% of rows" (:62) — ⚠ **UNVERIFIABLE (but the most credible figure available)**

- `DATA_MODEL.md:232` — `listed_price_cents | int | opt | site — original listing price when shown; **most rows have none**`. Direction confirmed, magnitude not.
- `DESIGN_NOTES.md:46` — *"production coverage is 4.4% (143,062 of 3,265,910 sales)"* — consistent with "under 5%".
- **But** `HANDOFF.md:128` still says *"Listed prices | ~12% of rows"*, which is **not** under 5%.
- D-031 (`DECISIONS.md:375`) rules 4.4% *"the credible one; ~12% looks stale"* — and explicitly adds *"Not yet settled by a live query."*

**Verdict:** the page picked the right number, but the project has two numbers on record and has
never run the query. Do not ship a public percentage until one is run.

---

### A3 — C5 "Populations come from the public census reports the grading companies publish monthly" (:63) — ❌ **FALSE (three ways)**

1. **Wrong source.** Populations are scraped from pricecharting.com, not obtained from graders.
   `DATA_MODEL.md:103` — *"**`VGPC.pop_data`** — the graded-population census: `{psa: [10 ints], cgc: [10 ints]}`… A **current snapshot only** — the site keeps no census history."*
   `DATA_MODEL.md:89` — *"All data comes from pricecharting.com."*
2. **Wrong cadence.** Census is captured on the same detail-page visit as everything else, on
   the crawler's priority schedule — not on a monthly publication rhythm.
   `DATA_MODEL.md:322` — the Detail-crawl lane is *"continuous, one card at a time"* and writes `populations` alongside `price_months` and `sales`.
3. **Wrong plurality.** Only two graders exist in the census.
   `DATA_MODEL.md:204` — `grader | string(8), PK part — **only `psa` or `cgc`; any other key is schema drift**`.
   Corroborated in the prototypes: `Cardstock Card.dc.html:221` reads *"PSA + CGC · as of 2026-07-30"*.

**Systemic, not local.** `Cardstock Card.dc.html:255` carries the same error in a tooltip —
*"Population data comes from PSA/CGC on their own publishing schedule — it can't be scraped on
demand"* — and it traces to `DESIGN_NOTES.md:54` (*"census keeps its as-of date (PSA/CGC publish
on their own schedule)"*). Fixing this page alone will not fix the product.

---

### A4 — C5 (cont.) implied census depth (:63) — ❌ **FALSE by omission — the single most important missing fact**

The sentence describes census as an ongoing monthly feed and never says when *our* history
starts. It starts three weeks ago.

- D-001 (`DECISIONS.md:27`), quoting `DATA_MODEL.md:397` — population history *"begins at each card's first visit (the site publishes no history)"*.
- `DATA_MODEL.md:104` — *"A **current snapshot only** — the site keeps no census history."*
- `DATA_MODEL.md:120–121` — *"**Population history.** Only the current census is published; history exists only from the moment *we* started observing."*
- `DATA_MODEL.md:406` — operational history begins *"at first deployment (2026-07-28)"*.

`CARDSTOCK_UI_SPEC_v1.md:425` shows the page was *supposed* to say this — its outline includes
*"census starts when we started looking"*. The built page dropped it.

---

### A5 — C6 / C25 "we mark the affected window on charts" (:63) and "the restatement window stays visibly marked" (:116) — ⚠ **UNVERIFIABLE, and contradicted by a design ruling**

Restatements are real:
`DATA_MODEL.md:209–213` — *"graders occasionally **restate** their counts (PSA restated ~June 2026; one card's grade cell jumped 397 → 99,246). A >10× jump on an established base, or any decrease, is flagged by metrics/alerts as a *source* change… but the rows are still written."*

Three problems:

1. **No stored flag.** Restatement is an *alert* in the scraper, not a column. CardStock would
   have to re-derive it (>10× jump on an established base, or any decrease). Feasible, but it
   is unbuilt work this page promises as existing behaviour.
2. **It was removed from the Card page.** `DESIGN_NOTES.md:54` — *"Removed from Card page (user decisions): … census restatement hatching (**no census-diff detection planned**; hatching was my invention, not spec)."* `DESIGN_NOTES.md:35` still lists *"pop restatement hatched region"* under Charts seams, so the two notes disagree about whether it survives anywhere.
3. **The one known restatement is unobservable.** PSA restated ~June 2026; our census history
   begins late Jul 2026 (A4). We were not looking, so we hold no before-and-after to mark.

The page makes this promise **twice** (:63, :116). At minimum one of them must go.

---

### A6 — C7 "**Excluded**: bulk lots, listings with ambiguous grade or damage notes…" (:64) — ❌ **FALSE as a first-party claim**

No such filtering exists anywhere in the documented ingest path. Sales are inserted exactly as
the page offers them:

- `DATA_MODEL.md:341–349` — the sale insert is `INSERT … SELECT … FROM unnest(…) ON CONFLICT (source, source_id) DO NOTHING`. Every parsed row goes in. There is no predicate.
- `DATA_MODEL.md:230` — `grade_tier | string(40) | ✓ | site — **bucket label exactly as the page names it** ("PSA 10", "Grade 9.5")`. Labels are taken verbatim; nothing is judged "ambiguous".
- `DATA_MODEL.md:217–221` — *"one immutable row per completed sale we have ever seen… the ledger only ever grows with genuinely new sales."*
- The only rejection mechanism is schema drift, which discards the **whole page**, not individual rows: `DATA_MODEL.md:281–284` (`parse_failures`, *"the crawl writes *nothing* to the fact tables"*).

If pricecharting excludes bulk lots upstream, that is unverified third-party behaviour. As
written the sentence claims an editorial pipeline CardStock does not operate.

---

### A7 — C8 "Coverage is deepest for English-language cards" (:64) — ⚠ **UNVERIFIABLE**

There is **no language field anywhere in the schema.** `cards` (`DATA_MODEL.md:154–171`) carries
`id, set_id, url, name, image_hash, image_fetched_at, first_seen_at, last_seen_at` plus mutable
scheduler state. No locale, no language, no region. The claim cannot be evaluated, and CardStock
cannot filter or badge on it.

---

### A8 — C8 (cont.) "graded PSA, BGS, CGC, SGC, ACE, and TAG, plus raw sales" (:64) — ✅ **VERIFIED as a tier list, ⚠ misleading as a coverage claim**

The six companies are exactly the grade-10 tiers in the vocabulary:
`../PokemonInvestBatch/src/PokemonInvestBatch.Domain/Parsing/GradeTierVocabulary.cs` — `"PSA 10", "CGC 10", "CGC 10 Prist.", "BGS 10", "BGS 10 Black", "SGC 10", "TAG 10", "ACE 10"`, plus `"Ungraded"` (= "raw sales"). Read directly.

**But per-grader coverage only exists at grade 10.** D-022 (`DECISIONS.md:70–79`), from
`../PokemonInvestBatch/docs/adr/0005-pooled-grade-tiers.md`: *"The source reports one 'Grade 8'
figure covering every grading company, splitting by company only at grade 10."* The ADR's
binding UI consequence is quoted in D-022 (`DECISIONS.md:79`): *"The interface must not imply
the pooled figure is company-neutral."*

Saying coverage "is deepest for cards graded PSA, BGS, CGC, SGC, ACE, and TAG" implies
company-resolved coverage across the grade range. Below 10 there is none — one pooled number
serves all six. This is the same wording trap D-022 already flagged in `HANDOFF.md:106`
(*"below 10 the buckets are grader-agnostic"*).

**Also note:** `DATA_MODEL.md:101` and `:230` say the site offers **21** grade buckets while the
vocabulary file lists **19**. See §8-X5.

---

### A9 — C9 / C10 / C11 "The April 2025 seam" (:52, :69, :71) — ❌ **FALSE. The headline error on the page.**

There is no April 2025 seam. There is no shared seam date at all.

- D-001 (`DECISIONS.md:22–23`) — *"Per-sale and census history begin at each card's first crawler visit (late Jul 2026), not Apr 2025 / Jan 2026. The seam is **per-card and ragged**, not a single shared date."*
- `DATA_MODEL.md:382–385` — *"**Epoch boundary is per-card, per-grade-bucket** — *not* the crawler's start date… 'Start of reliable per-sale data' for a bucket = its oldest captured row."*
- `DATA_MODEL.md:406` — *"`visits`, `fingerprints`, `parse_failures` begin at first deployment (2026-07-28)."*
- `git -C ../PokemonInvestBatch log --reverse` — first commit 2026-07-27 (per D-001, `DECISIONS.md:29`).
- `HANDOFF.md:126` (corrected 2026-08-10) — *"Per-sale ledger (post-seam) | **Each card's first visit, late Jul 2026 onward — ragged, never a shared date**"*.
- Owner, quoted in D-001 (`DECISIONS.md:31`): *"That's completely false. It just started this month."*

**Magnitude:** the page claims 16 months of per-sale history that does not exist. Under D-033 the
usable ledger does not begin until **2026-09-01** — three weeks *after* this page's "last
updated" date.

**This settles D-009.** D-009 (`DECISIONS.md:385–390`) asked whether the "Apr '25 liquidity
seam" at `DESIGN_NOTES.md:35` was an error, a lost data source, or deliberate design fiction.
Per D-040's method (*"settleable by opening the HTML"*), the answer is now visible: the
prototype does not merely draw an Apr '25 marker — **it states in prose, on a public methodology
page, that the per-sale ledger begins in April 2025.** That is broader than
`DESIGN_NOTES.md:35`, which scopes the Apr '25 seam to *"(churn/vol panes)"* and assigns
*"per-sale ledger begins"* to the **Jul '26** seam. The prototype and the design note disagree
about what April 2025 even means. Owner ruling required — §7-Q1.

---

### A10 — C10 "monthly aggregates — averages and **sale counts**, back to August 2023" (:71) — ❌ **FALSE twice, in opposite directions**

**(a) "back to August 2023" understates by ~2 years 8 months.**
- D-002 (`DECISIONS.md:37–40`) — *"Monthly price history is genuinely deep: ~Dec 2020, backfilled whole on first visit."*
- `DATA_MODEL.md:375` — *"Backfilled to ~Dec 2020 for every card at its first visit. Monthly resolution, six tiers. This is the only deep history we have, and it carries most of the undervaluation signal."*
- `DATA_MODEL.md:95` — *"reaching back to ~December 2020"*; `:358` — *"6 tiers × ~68 months"*.
- `CARDSTOCK_UI_SPEC_v1.md:20` and `HANDOFF.md:125` both say Dec 2020.

The page is giving away the one genuinely strong asset in the dataset.

**(b) "sale counts" pre-seam do not exist and can never exist.** This is the more serious half.
- `DATA_MODEL.md:113–117` — *"**Historical sales volume.** No page, in any epoch we've captured, carries a volume-over-time series… a snapshot, no time axis."*
- `DATA_MODEL.md:391–393` — *"**No pre-seam volume exists anywhere** — not in our store and not at the source (§2). A spec needing it must mark it *unavailable from source*, not 'pending import'."*
- `DATA_MODEL.md:481–482` — *"**Unavailable from source, permanently:** historical sales volume; sales beyond the bucket windows; pre-observation census history."*
- `DATA_MODEL.md:123–124` — *"Any spec that assumes deep volume history… is assuming data that does not exist."*

`price_months` has exactly one integer per (card, tier, month) — `price_cents`
(`DATA_MODEL.md:186`). There is no count column and no count series.

**The claim is precisely inverted.** Sale counts are derivable *only forward of each card's
seam* (`DATA_MODEL.md:433–438`, the monthly-volume query; `HANDOFF.md:126` gates *"sales count"*
on the per-sale ledger) — the exact era this sentence describes as bare transactions. The page
promises counts where they are permanently impossible and omits them where they exist.

---

### A11 — C10/C11 "**our archive**" / "**we keep**" (:71) — ❌ **FALSE framing. 100% of this data is one third party's.**

The seam section uses first-party possessive language three times — *"our archive holds"*,
*"we keep the per-sale ledger"* (:71), and *"not at the start of our archive"* (:113).

- `DATA_MODEL.md:89` — *"**All data comes from pricecharting.com**"* (site facts verified 2026-07-27).
- `DATA_MODEL.md:8–11` — the worker *"politely scrapes pricecharting.com for Pokemon card price history, individual sales, graded population census, and product images."*
- The monthly series is not even aggregated by CardStock — it is the site's precomputed chart, copied whole on first visit (`DATA_MODEL.md:175–178`). "Our archive holds monthly aggregates" implies an aggregation step that does not occur.

Compounding it, **the page never names pricecharting.com anywhere** (grep across all mockups:
the string appears only in `BRAND_BRIEF.md:18`, `PROJECT_LOG.md:227`, `CARDSTOCK_UI_SPEC_v1.md:20`
and the research upload — never in a shipped page). Meanwhile C29 (:130) promises *"Every stat's
tooltip names its source and window."*

Also note D-017 (`DECISIONS.md:471`): *"no backup exists"* of any kind. "Our archive" asserts a
durability nothing currently provides.

See also `docs/screens/legal.md` §6 — `Cardstock Legal.dc.html:66` forbids users to *"scrape at
volume, resell our data"*, which sits awkwardly beside this.

---

### A12 — C11 "**every** individual transaction" (:71) — ❌ **FALSE (overstated)**

- `DATA_MODEL.md:101` — *"**Each bucket shows only the newest ~30 rows** — the site discards older ones forever."*
- `DATA_MODEL.md:118–119` — *"**Sales older than the ~30-row bucket windows.** Once a row scrolls off, the site shows it to no one."*
- `DATA_MODEL.md:386–390` — *"Completeness is *engineered, not guaranteed* — a card can outsell our visit pace and roll rows off unseen… **a spec should say 'complete except alarmed cap incidents', not 'complete'.**"*

DATA_MODEL states in as many words how this sentence should be phrased, and the page uses the
phrasing it warns against. Note `DESIGN_NOTES.md:48` records the opposite user belief
(*"Missed-sales scraper alert removed (user: capture is complete)"*) — worth an owner
re-confirmation.

**The three named fields are correct:** date = `sales.sold_on`, venue = `sales.source`, grade =
`sales.grade_tier` (`DATA_MODEL.md:227–230`). Only "every" is wrong.

---

### A13 — C13 "backtests refuse to reach further back (the \"honest floor\")" (:72) — ✅ **VERIFIED as mechanism, ❌ FALSE as calibrated**

The mechanism is exactly right and matches `CARDSTOCK_UI_SPEC_v1.md:90` (*"the date picker
*shows* the honest floor and why"*). But a floor computed from an April 2025 seam is **16 months
too permissive**, which turns the product's flagship honesty feature into its largest
overstatement — the identical failure mode D-032 (`DECISIONS.md:356`) describes as *"overstating
data sufficiency inside its own honesty apparatus."*

The five named indicators are individually sound: churn (`DATA_MODEL.md:428–431`), dispersion,
Amihud, cross-marketplace gap, discount-to-list (`:447–449`, *"Also derivable, currently
unused: discount-vs-list… per-bucket seam dates"*). One caveat: **cross-marketplace gap** may
not be as locked as assumed — D-031 (`DECISIONS.md:377`) notes `DATA_MODEL.md:102`/`:227`
document five sources (ebay, tcgplayer, goldin, heritage, pwcc) while `HANDOFF.md:129` says
"eBay-only today", and *"the gate may be locking an indicator that has data behind it."*
Unresolved; needs a query.

---

### A14 — C14 "Sales data refreshes daily" (:79) — ❌ **FALSE**

There is no daily refresh guarantee for any card. The crawl is a continuous priority queue:

- `DATA_MODEL.md:320–322` — Detail crawl lane: *"continuous, one card at a time."*
- `DATA_MODEL.md:329–336` — *"pure priority score, re-computed from Postgres each pick, highest tier wins — *due by burn window* → *refresh requested* → *never visited* → *bucket already at cap* → ***starved past the 30-day floor*** → everyone else by staleness × (1 + churn)."*

A 30-day starvation floor is the *backstop*, meaning a cold card may legitimately go a month
between visits. With ~91k–100k cards on one politely-gated crawler (10 s floor / 300 s ceiling,
`DATA_MODEL.md:315–316`), daily coverage of the corpus is not achievable.

**What *is* true and stronger:** `DESIGN_NOTES.md:54` — *"card page visits trigger a fresh
scrape"* — and `Cardstock Card.dc.html:253` — *"Sales & prices refreshed just now"*. Via the
express-visit intake endpoint (D-024), a viewed card is refreshed **on demand**, which beats
"daily". The page is underselling a real capability while overselling a fake one.

---

### A15 — C14 (cont.) "census data lands when graders publish, roughly monthly" (:79) — ❌ **FALSE**

Same error as A3. Census arrives on the same detail-page visit as prices and sales
(`DATA_MODEL.md:322`), scraped from a site snapshot (`:103–104`). Nothing "lands" from a grader.

---

### A16 — C15 "The footer stamp on **every page**" (:79) — ❌ **FALSE against the prototypes**

- `HANDOFF.md:99` — *"**AsOfStamp component** — removed app-wide; footers say 'refreshed just now' instead of per-element staleness stamps."*
- `DESIGN_NOTES.md:54` — *"Footer staleness stamps replaced by 'Sales & prices refreshed just now'."*
- Grep for freshness stamps across all `.dc.html`: hits only on `Cardstock Card.dc.html:253` (*"Sales & prices refreshed just now"*) and `:255` (*"Census as of 2026-07-30"*). **This page has no stamp. Neither does Home, Screener, Charts, Binder, Browse, or Legal.**

The stamp exists on exactly one screen, and its semantics changed from staleness to
"refreshed just now".

---

### A17 — C16 "Monthly series only include **closed months**" (:80) — ✅ **VERIFIED**

- `DATA_MODEL.md:98–99` — *"Closed months are immutable server-side; only the current month revises between visits."*
- `DATA_MODEL.md:178–179` — *"a typical visit adds 0–2 rows (the current month moved); **closed months carry exactly one row forever**."*
- `DATA_MODEL.md:189–191` — *"The composite PK ends in `observed_at`: the same (card, tier, month) legitimately has multiple rows when the *current* month's average revised between visits. Latest-per-key queries must order by `observed_at`."*

**Implementation invariant:** every price read must be latest-per-key by `observed_at`
(`DATA_MODEL.md:416–420` gives the canonical `DISTINCT ON (tier)` query). A naive query returns
plausible-looking wrong numbers.

---

### A18 — C17 "it aggregates the sales recorded so far" (:80) — ❌ **FALSE mechanism, and it contradicts a design note**

The current-month point is pricecharting's own revising monthly **average**
(`DATA_MODEL.md:186`, `:98–99`), not an aggregate CardStock computes from `sales`. The chart
(`DATA_MODEL.md:95`) and the sales tables (`:100`) are two independent assets parsed from the
same page; neither derives from the other.

`DESIGN_NOTES.md:49` proposes something different again, and internally impossible:

> *"**Current-month point methodology**: the month-to-date point is computed with the SAME aggregation as closed months (median of the tier's sales captured since month start, outlier-trimmed), just on partial data."*

Closed months are **not** computed by CardStock at all, and the site's figure is an *average*,
not an outlier-trimmed *median*. So "the SAME aggregation as closed months" cannot be satisfied
by any implementation. Three options — copy the site's revising point, compute our own from
`sales` (a visibly different series, discontinuous at the seam), or show both — and the choice
must be made before Charts ships. §7-Q4.

**The hollow/dashed presentation is verified design**: `DESIGN_NOTES.md:49` — *"final chart
segment dashed + hollow end dot with tooltip, no text warning. Same treatment to be applied in
Charts."*

---

### A19 — C18 / C21 "We never project or extrapolate" (:80, :112) — ✅ **VERIFIED as policy**

`DESIGN_NOTES.md:49` — *"NO projection/extrapolation to month-end, ever (violates honesty
stance; daily/weekly resolutions are the honest 'right now' view)."* Consistent with D-041's
posture. Keep verbatim.

---

### A20 — "Churn 30d — 30 days of per-sale ledger in that grade bucket" (:91–:92) — ✅ **VERIFIED as mechanism, ⚠ incomplete**

`DATA_MODEL.md:428–431` gives exactly this window: *"churn at any date d: sales in (d-30d, d] / 30.0"*.
Per-bucket framing matches `DATA_MODEL.md:382–383` (*"Epoch boundary is per-card, per-grade-bucket"*).

⚠ Two cautions the row omits:
1. Under D-033 the earliest honest churn value is **~2026-10-01** (30 days after the floor). The
   page implies it is available now.
2. `cards.observed_sales_per_day` must **not** be used — `DATA_MODEL.md:76` and `:423–425` warn
   it is a deliberately hotter, mutable scheduler cache (*"the hottest single bucket's fill
   rate, for scheduling"*), not the card-wide rate. Derive from `sales.sold_on`.

---

### A21 — "Weekly bars — ~6 months of ledger (≈ **Jan 2027**)" (:94) — ❌ **FALSE, and it contradicts this very page**

`(≈ Jan 2027)` implies the ledger began ~Jul 2026 — which is D-001's real answer, **not** this
page's own April 2025 claim at :71. If the ledger truly began April 2025, six months elapsed in
October 2025 and weekly bars would have unlocked ten months ago.

**The seam section and the sufficiency table cannot both be right.** The table was calibrated
against the true Jul '26 seam; the prose was not.

Against D-033's disclosed floor (2026-09-01), six months lands at **~Mar 2027**, so `Jan 2027`
is ~2 months optimistic even on the correct substrate. And an authored literal date is exactly
what D-033 abolished (`DECISIONS.md:319`): *"Numerators are arithmetic against today. No
authored ratios, ever again."* No corroborating source exists — grep finds no weekly/daily bar
entry in `DISPLAY_VOCABULARY.md`.

---

### A22 — "Daily bars — ~12 months of ledger, liquid cards only" (:96) — ⚠ **UNVERIFIABLE**

No date given (good — nothing to be wrong). Under D-033 this lands **~Sep 2027**, matching
D-033's own arithmetic (`DECISIONS.md:321`: *"12 months of census → ~Sept 2027"*).

⚠ "liquid cards only" is in tension with `DATA_MODEL.md:394–395`: *"per-sale history is shortest
exactly where volume matters most (hot cards burn their windows in days)"*. That asymmetry is
pre-seam only — forward of first visit the burn-window scheduler tier protects hot cards
(`DATA_MODEL.md:330–332`) — but "liquid cards only" needs a defined threshold before it can be
rendered as a lock.

---

### A23 — "Oscillators (RSI, z-score) — their full warm-up window of monthly closes" (:98) — ✅ **VERIFIED, and satisfiable today**

Monthly closes run to ~Dec 2020 (D-002), ~68 months (`DATA_MODEL.md:358`). RSI(6) and z-score vs
6M MA (`CARDSTOCK_UI_SPEC_v1.md:184`) warm up in months, not years. **These indicators are
live now** — one of the few rows on this page that is honest and favourable.

---

### A24 — "Seasonality — **one observed cycle so far**" (:100) — ❌ **FALSE on either substrate**

The page never says which substrate seasonality runs on, and both answers contradict "one":

- **Monthly prices** (~Dec 2020 → Aug 2026): ~5.7 annual cycles, not one. D-002 / `DATA_MODEL.md:375`.
- **Per-sale ledger** (late Jul 2026 → Aug 2026): **zero** cycles. D-001.

The figure traces to `HANDOFF.md:130` (*"Annual cycles | 1 of 3"*), which D-031
(`DECISIONS.md:379`) lists as **Unverified**, and `DISPLAY_VOCABULARY.md` contradicts itself on
the unlock date — D-032 (`DECISIONS.md:360`): *"`:36` says 'corpus-locked until ~**Nov 2028**'; `:145` says '3 observed cycles · **Nov 2027** (1/3)'. Same file, one year apart."*

Resolve the substrate first; the count follows arithmetically.

---

### A25 — "Composites (G1–G4) — a composite never fires on partial inputs" (:102) — ✅ **VERIFIED as policy**

Consistent with `CARDSTOCK_UI_SPEC_v1.md:184` and the D-038 posture. No data dependency to check.
⚠ Note the consequence: since composites gate on their weakest input, and liquidity/supply
inputs are locked into 2027–2028 (D-033, `DECISIONS.md:321`), **G1–G4 are dark at launch.**

---

### A26 — C22 "Backtests start at each screen's honest floor" (:113) — ✅ **VERIFIED as design, inherits A9's miscalibration**

`CARDSTOCK_UI_SPEC_v1.md:90` — *"date range limited to what the data honestly supports (S1-only
screens: back to ~2021; screens using sales/census metrics: post-seam/post-2026 — the date picker
*shows* the honest floor and why)"*. Note the spec says **post-2026**, not April 2025 — the spec
was right and the prototype regressed.

Also `CARDSTOCK_UI_SPEC_v1.md:90`: *"signals are computed only from rows whose
`observed_at`/`captured_at` precede the entry date — the append-only ledger makes lookahead bias
structurally impossible."* That is a genuine, verifiable strength (`DATA_MODEL.md:43–48`, Rule 1,
enforced by the absence of a DELETE grant) — and this page does not mention it. Content gap.

---

### A27 — C24 "Missing metadata renders as METADATA PENDING" (:115) — ⚠ **UNVERIFIABLE (product convention)**

No data receipt needed or available. Cross-check against `DISPLAY_VOCABULARY.md` when
implementing. Note `HANDOFF.md:107` carves out an exception: *"The Pokédex schema is external and
pre-populated, so species attributes never show METADATA PENDING."*

---

### A28 — C26 fan-made / unaffiliated disclaimer (:124) — ✅ **VERIFIED as present, ⚠ narrower than Legal's**

Compare `Cardstock Legal.dc.html:67`, which adds **"Creatures Inc."** to the same list. This page
omits it. Two disclaimers on one product should not enumerate different entities.

**Neither page names pricecharting.com**, whose data is 100% of the product
(`DATA_MODEL.md:89`), whose CDN serves every card image (`DATA_MODEL.md:105`, `:292–295` —
~3.6 GB), and whose terms of service have never been read — D-010 (`DECISIONS.md:90`): *"No repo
records reading any terms of service, and storing is a different act from serving."* This is the
page where attribution belongs.

---

### A29 — C28 not-financial-advice (:125) — ✅ **VERIFIED as present and consistent**

*"signals describe the past, not the future"* matches `Cardstock Legal.dc.html:64` (*"Cardstock
describes what the market did, not what it will do"*) and D-041's posture. No data claim. Keep.

---

### A30 — C29 "Every stat's tooltip names its source and window" (:130) — ⚠ **UNVERIFIABLE, and currently self-undermining**

An implementable commitment, but if honoured literally every tooltip on the product says
"pricecharting.com" — a string this page never prints. Either the promise or the omission has to
give.

---

### A31 — ⚠️ **CRITICAL OMISSION: the 2026-09-01 sufficiency floor is absent**

**The page never states the floor, and it was explicitly required to.**

- D-033 (`DECISIONS.md:322`) — *"`Cardstock About Data.dc.html` should carry the floor **and its reason**. 'We discarded our own early data because we didn't trust it' is the same posture as the rest of the design and is a stronger story than an unexplained date."*
- D-038 (`DECISIONS.md:245`) — *"**`Cardstock About Data.dc.html` must carry the floor and its reason** (D-033). With locks everywhere, the explanation page stops being optional."*

Verified by reading all 136 lines: the strings "2026-09-01", "September", "floor" (in the
sufficiency sense), and "stabilis*" appear **nowhere**. The `#sufficiency` section (:84–:105)
runs six rows without once naming the date every one of them is measured from.

**Required copy**, per D-033 (`DECISIONS.md:312–316`): a floor, not a claim about when data
began; the collector was being stabilised through August 2026; earlier observations are
discarded rather than trusted; September 1 is the first day of data the owner will stand behind.

**Note the pointed irony:** the page's most-quoted virtue is *"An indicator that doesn't have
enough history to be trustworthy is locked, with its unlock date shown"* (:86) — while the page
itself omits the one date all those unlock dates are computed from.

---

### A32 — ⚠️ **CONTENT GAP: candlesticks are never explained**

`Cardstock Charts.dc.html:182` renders a link reading **"Why no candlesticks? → About our data"**
that lands on a page with no such section. The explanation was specified twice:

- `CARDSTOCK_UI_SPEC_v1.md:241` — *"Content authored in §8.6: what the six tiers are, **why monthly (no candlesticks)**, the sales seam, why volume can't predate it, census start + restatements, 'as of' stamps, and what we refuse to fake."*
- `CARDSTOCK_UI_SPEC_v1.md:425` — *"…→ **why monthly means no candlesticks (and why we won't fake them)** → the sales seam, **per card**, drawn on your charts → **census starts when we started looking**…"*
- `CARDSTOCK_UI_SPEC_v1.md:184` — *"the indicator panel footer links 'Why no candlesticks? → About our data'."*

D-041 (`DECISIONS.md:224–233`) makes the reason owner-confirmed and rigorous: *"structurally
impossible, not merely unimplemented. `price_months.price_cents` is a single monthly value
(D-003 — six tiers, one integer each); OHLC needs four points per period and intraday sequencing
that does not exist at the source."* Receipt: `DATA_MODEL.md:481` (*"unavailable from source,
permanently"*).

D-041 also confirms **no news**, and the spec outline names *"order books, news, real-time"* as
deliberate omissions — none appear in the Honesty policy list (:112–:116).

**Note how much of the spec's outline the built page dropped or inverted:** "the sales seam, per
card" became a single false global date (A9); "census starts when we started looking" became
"grading companies publish monthly" (A3/A4); "why monthly means no candlesticks" vanished
entirely; "'as of' stamps" became a false claim about every page (A16).

---

## 7. Open questions

| # | Question | Blocks | Owner input needed |
|---|---|---|---|
| **Q1** | **The April 2025 seam (:52, :69, :71–:72) is false (A9). Rewrite to the ragged per-card seam, or to the disclosed 2026-09-01 floor, or both?** D-001 says ragged and per-card; D-033 says one disclosed floor. The page needs one story. Settles **D-009** | Everything on this page; Charts seam markers; every backtest floor | ✅ Yes — this is the ruling |
| **Q2** | Pre-seam "sale counts" (:71) are permanently impossible (A10b). Delete the phrase, or state plainly that volume history is unavailable from source? `CARDSTOCK_UI_SPEC_v1.md:425` wanted *"why volume can't predate it"* | Charts volume pane; Screener volume filters | ✅ Yes |
| **Q3** | The page never names pricecharting.com (A11, A28). Add attribution — and does D-010's unresolved licensing question change the answer? | Legal §IP; image serving; D-010, D-011 | ✅ Yes |
| **Q4** | Current-month point methodology (A18): copy the site's revising average, compute our own median from `sales`, or show both? `DESIGN_NOTES.md:49` specifies an impossible hybrid | Card page chart; Charts | ✅ Yes |
| **Q5** | Restatement marking is promised twice (:63, :116) but `DESIGN_NOTES.md:54` says *"no census-diff detection planned"* while `:35` still lists hatching for Charts (A5). Build it or drop the promise? | Charts pop pane; both sentences | ✅ Yes |
| **Q6** | Add the "why no candlesticks" section (A32)? `Cardstock Charts.dc.html:182` already links to it and D-041 supplies the reasoning | The Charts link is broken until answered | ✅ Yes |
| **Q7** | "Sales data refreshes daily" (:79) is false (A14), while on-demand express-visit refresh is real. Rewrite to the stronger true claim? Ties to **D-025** (which scenarios call which intake endpoint) | Freshness copy across the app | ✅ Yes |
| **Q8** | Which substrate does Seasonality run on (A24)? Determines whether the count is ~5, or 0 | The seasonality row; `DISPLAY_VOCABULARY.md` §2/§10 | ✅ Yes |
| **Q9** | Add a Legal link to this page's footer? Legal links here (`Cardstock Legal.dc.html:65`); the reverse does not exist | Nav completeness | ⬜ Design call |
| **Q10** | "under 5% of rows" (:62) — run the listed-price query and settle 4.4% vs ~12% (A2, D-031) before publishing a number | The sentence; discount-to-list gating | ⬜ Query, then confirm |
| **Q11** | Does "English-language" (:64) survive, given no language field exists (A7)? | The coverage sentence | ⬜ Design call |
| **Q12** | Define "liquid cards" for the daily-bars lock (:96, A22) | Charts D-resolution lock | ⬜ Design call |

---

## 8. Contradictions found

| # | Contradiction | Sources | Resolution |
|---|---|---|---|
| **X1** | **Page vs. reality on the seam.** ":71 — 'Before April 2025 our archive holds monthly aggregates'" vs `DATA_MODEL.md:382–385` (*"Epoch boundary is per-card, per-grade-bucket — not the crawler's start date"*) and D-001 | Tier 1 HTML vs Tier 1 data | Data wins on fact; HTML wins on what the page *says*. Copy must change — Q1 |
| **X2** | **Page vs. itself.** The seam section says the ledger starts Apr 2025 (:71); the sufficiency table's *"(≈ Jan 2027)"* (:94) is arithmetic from a ~Jul 2026 start. Under :71, weekly bars unlocked in Oct 2025 | :71 vs :94 | Internal. Table is closer to truth; prose is the error |
| **X3** | **Page vs. `DESIGN_NOTES.md:35`.** The note scopes Apr '25 to *"liquidity seam… (churn/vol panes)"* and assigns *"per-sale ledger begins"* to the **Jul '26** seam. The page assigns the per-sale ledger to **April 2025** | :71 vs `DESIGN_NOTES.md:35` | Two different meanings for one date. Settles D-009 — Q1 |
| **X4** | **Prices: shallow vs deep.** ":71 — back to August 2023" vs `DATA_MODEL.md:375`, `HANDOFF.md:125`, `CARDSTOCK_UI_SPEC_v1.md:20`, D-002 — all ~Dec 2020 | 1 vs 4 | Dec 2020. Page understates by ~2y 8m |
| **X5** | **19 vs 21 grade buckets.** `GradeTierVocabulary.cs` lists 19 labels (and `CLAUDE.md:93` says 19) while `DATA_MODEL.md:101` says *"up to 21 grade buckets"* and `:230` says *"21 distinct labels"* | Code vs DATA_MODEL | Likely reconcilable — the file is a *detection* vocabulary whose comment says *"this list grows"* — but 19 ≠ 21 is unexplained. Affects A8 |
| **X6** | **Footer stamps.** ":79 — 'The footer stamp on every page'" vs `HANDOFF.md:99` (*"AsOfStamp component — removed app-wide"*) and a grep finding stamps only on `Cardstock Card.dc.html:253,:255` | Page vs HANDOFF + prototypes | Page is wrong — A16 |
| **X7** | **Restatement marking.** Promised at :63 and :116 vs `DESIGN_NOTES.md:54` (*"no census-diff detection planned; hatching was my invention, not spec"*), while `DESIGN_NOTES.md:35` still lists *"pop restatement hatched region"* | Page vs two design notes that also disagree with each other | Unresolved — Q5 |
| **X8** | **Current-month aggregation.** ":80 — 'it aggregates the sales recorded so far'" vs `DATA_MODEL.md:186` (site's monthly **average**), vs `DESIGN_NOTES.md:49` (*"median… outlier-trimmed"*, claimed to be *"the SAME aggregation as closed months"* — which CardStock does not compute) | Three mutually exclusive descriptions | Unresolved — Q4 |
| **X9** | **Listed-price coverage.** ":62 — under 5%" vs `DESIGN_NOTES.md:46` (4.4%) vs `HANDOFF.md:128` (~12%) | D-031 | 4.4% credible, query not yet run — Q10 |
| **X10** | **Seasonality cycle count.** ":100 — one observed cycle" vs D-002 (~5.7 price cycles) vs D-001 (0 ledger cycles); source figure `HANDOFF.md:130` is flagged Unverified by D-031; `DISPLAY_VOCABULARY.md:36` vs `:145` disagree by a year (D-032) | Four sources, no two agreeing | Unresolved — Q8 |
| **X11** | **Disclaimer scope.** ":124" omits *"Creatures Inc."*, present at `Cardstock Legal.dc.html:67`. Neither names pricecharting.com | Page vs Legal | Align both — Q3 |
| **X12** | **Sale counts, inverted.** ":71" puts counts pre-seam (impossible — `DATA_MODEL.md:481`) and omits them post-seam (where `DATA_MODEL.md:433–438` and `HANDOFF.md:126` confirm they are derivable) | Page vs DATA_MODEL | Page is exactly backwards — Q2 |
| **X13** | **Built page vs its own spec.** `CARDSTOCK_UI_SPEC_v1.md:425` outlines *"the sales seam, **per card**"*, *"census starts when we started looking"*, *"why monthly means no candlesticks"*, *"order books, news, real-time"* — the built page inverts the first two and omits the last two | Tier 3 spec vs Tier 1 prototype | Prototype wins on authority, but the spec was factually right. Q1, Q6 |
| **X14** | **Authored constants.** `"(≈ Jan 2027)"` (:94) vs D-033 (`DECISIONS.md:319`): *"Numerators are arithmetic against today. **No authored ratios, ever again.**"* | Page vs D-033 | D-033 overrides — compute it |
| **X15** | **Sourcing language.** *"our archive"* / *"we keep"* (:71, :113) vs `DATA_MODEL.md:89` (*"All data comes from pricecharting.com"*); and `Cardstock Legal.dc.html:66` forbids users to *"scrape at volume, resell our data"* | Page + Legal vs DATA_MODEL | First-party framing over wholly third-party data — Q3 |

---

## Implementation checklist

- [ ] Extract the shared shell (nav + 820px column + theme/CVD pre-paint) — used by this page and `/legal`
- [ ] Self-host Inter, Inter Tight, JetBrains Mono (remove the Google Fonts CDN links at :12–:14) — required for Legal's no-trackers promise
- [ ] Preserve all six fragment ids; `#sufficiency` is linked from `Cardstock Screener.dc.html:384`
- [ ] `scroll-margin-top:62px` on every section
- [ ] Convert the sufficiency grid to a real `<table>` or add ARIA roles (:87–:103)
- [ ] **Do not ship §3 copy** until Q1–Q8 are answered. This is a public factual page
- [ ] Add the 2026-09-01 floor section with its reason (A31 — required by D-033 and D-038)
- [ ] Add the "why no candlesticks" section (A32 — `Cardstock Charts.dc.html:182` links to it today)
- [ ] Replace `"(≈ Jan 2027)"` with a value computed against the floor (R6, X14)
- [ ] Name pricecharting.com (Q3), pending D-010/D-011
