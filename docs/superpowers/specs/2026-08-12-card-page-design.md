# Phase 2 design — the Card page, end to end

**Date:** 2026-08-12 · **Phase:** 2 of the D-075 sequence · **Build reference:** `docs/screens/card.md`
(Tier 1), as amended by the rulings in `DECISIONS.md` D-080–D-084. Where this document and card.md
disagree, this document carries the newer ruling and the corresponding card.md edit is listed in §11.

The page is the product's thinnest end-to-end vertical slice: one card's identity, prices, history,
ledger, census, and freshness — rendered honestly against the data that actually exists on
2026-08-12, with every insufficient metric showing a state rather than a number.

Live-data grounding for every decision here: the corpus is fully crawled and month-axis sparsity is
real (D-080); sale grade labels are exactly the 19-value vocabulary (D-081); the ledger is deep, not
empty — 4.4M sales to 2016, per-grade window starts (D-082); census display data exists today while
deltas are floor-gated to zero (D-083).

## 1. Scope

**In:** route `/card/{id}`; four API endpoints; three new Application readers + DI wiring; the full
page: identity header, tier strip, LWC price chart, sales ledger, census pair, freshness footer,
refresh flow; deferred-disabled chrome; all states in §8; tests; deploy to the Pi.

**Out, with reasons recorded:** auth (D-084.1 — watchlist/binder render deferred-disabled); any card
lookup/search (D-084.2 — URL-only reachability); species anywhere (D-084.10 — future Pokédex phase);
signal chips *content* (worker is Phase 3; the row renders empty because chips are firing-only);
seam markers and ledger captions (D-084.5 — C-7 stands); census gem-rate sentence (inputs
floor-gated until ~late 2026); marketing/SSR tier (Phase 5+).

**Schema changes: none in this repo. No new tables, no migrations.** The one related schema change —
the TCGdex enrichment (collector number + official set size) — happens in `PokemonInvestBatch` per
`docs/superpowers/handoffs/2026-08-12-tcgdex-enrichment-handoff.md`, has **no ordering relationship
with Phase 2** (additive there; consumed here whenever it lands, §9), and is started by a dedicated
subagent after Phase 2 planning completes.

## 2. Architecture

```
Browser (Blazor WASM)
  └─ GET  /api/v1/cards/{id}          snapshot: identity + prices + census + freshness
  └─ GET  /api/v1/cards/{id}/sales    the complete ledger
  └─ GET  /api/v1/cards/{id}/image    card art streamed from the Pi's image store
  └─ POST /api/v1/cards/{id}/refresh  express-visit proxy (per-IP shape limit)
CardStock.Api ──(loopback)── worker's POST /cards/{id}/express-visit
```

- **Hosted-WASM topology:** `CardStock.Api` references `CardStock.Web`, serves its framework files,
  falls back to `index.html`. One process, one origin, no CORS; the existing `cardstock-api.service`
  remains the whole deployment. Tier separation stays at the project level; topology is revisited
  when the marketing SSR tier arrives.
- **Readers (Application):** `ICardIdentityReader`, `ICardCensusReader`, `ICardSalesReader` join
  Phase 1's `ICardPriceReader`. Boundaries follow the must-not-drift invariants: strip and chart
  share the price reader (R-2); census bars and their summary share the census reader (R-26).
- **Concurrent composition (D-084.6):** the snapshot handler runs identity, price, and census
  readers in parallel via `IDbContextFactory` (an EF context is not thread-safe; one context per
  reader, pooled connections). Cross-reader skew mid-refresh is harmless on append-only data and
  heals on the post-refresh refetch.
- **DI:** Phase 2 performs the first real registrations, closing the gap Phase 1 left deliberately.
- **Worker: untouched.**

## 3. API contracts

All responses JSON; errors are RFC 7807 problem details. All endpoints anonymous (D-084.1).

**`GET /api/v1/cards/{id}` → 200 `CardPageSnapshot` | 404**

- `Identity`: `Title` (name with trailing `#num` parsed off; the raw name untouched when the parse
  doesn't match — a failed parse can never invent a number), `CollectorNumber` (string?, from the
  parse today, from enrichment later), `SetSize` (int?, null until enrichment), `SetName`,
  `HasImage`, `DelistedAt?`.
- `Prices`: Phase 1's `CardPriceSnapshot` — six tiers always; per tier the 12-month series (holes as
  holes), `TierPrice`, `TierChange` (two-30-day-sales-windows; floor 3/window), current-month index.
- `Census`: six cells (PSA 8/9/10, CGC 8/9/10 — D-084.4), each `{grader, grade, count}` with
  absent-cell = 0 (true zero by the storage contract); `PsaTotal`, `CgcTotal` (all grades);
  `ObservedAt?`; `QualifyingObservations` (counted under the 2026-09-01 floor, D-033 — 0 today).
- `Freshness`: `LastVisitedAt?`.
- 404 for unknown ids and for `not_a_card_at` cards (problem detail carries which).

**`GET /api/v1/cards/{id}/sales` → 200 rows | 404** — complete ledger, newest first:
`{soldOn, gradeTier, priceCents, listedPriceCents?, source, title}`. Raw `title` is data; encoding
is the render layer's job (D-029: never `MarkupString`).

**`GET /api/v1/cards/{id}/image` → 200 image/jpeg | 404** — streams
`{ImageDirectory}/{hash}/1600.jpg` with long-lived cache headers; 404 when hash is null or the
image fetch is still owed.

**`POST /api/v1/cards/{id}/refresh` → passthrough** — calls the worker's loopback `express-visit`
with a ~65s client timeout (the worker's own 60s upstream cap always answers first) and returns the
worker's status untouched: 200 · 404 · 409 · 422 · 500 · 502 (no 504 exists — D-076). Carries the
**per-IP token bucket**: generous enough that a person browsing hard never notices (order of a few
hundred per hour — exact numbers are an implementation-plan parameter), tripped in minutes by id
enumeration; over-limit → 429. D-062's rationale; per-IP per D-084.1 until accounts exist.

## 4. The page

```
CardPage.razor (/card/{id})
├─ AppChrome          logo · 5 nav tabs · search · avatar — all deferred-disabled
├─ Breadcrumb         Browse / {set} / {name} — inert crumbs until those screens exist
├─ IdentityHeader     art · title · subline · actions · badge slot · chip row
├─ TierStrip          6 cells
├─ PriceChart         LwcChart wrapper, 12M × 6 series
├─ SalesLedger        §7
├─ CensusPair         PopulationPanel · GradingActivityPanel
└─ FreshnessFooter    two stamps + attribution
```

Every panel is a values-only component (`<TierStrip Tiers=…>`, `<CensusBars Cells=…>`,
`<LwcChart Series=…>`); presentation math lives inside components, written and tested once.

- **Deferred chrome, one uniform treatment (owner ruling):** every control whose target screen or
  subsystem is a later phase renders present-but-disabled with an honest tooltip naming what
  unlocks it — nav tabs, search, avatar, Open in Charts, watchlist, binder, breadcrumb and subline
  links. Never stripped, never a dead link.
- **Identity:** title/subline per §3; a `delisted {date}` muted chip beside the subline when
  `DelistedAt` is set. Art thumbnail 217×300 → lightbox with Escape, `role="dialog"`, focus return
  (resolves OQ-8); placeholder slot when imageless. The 28px badge slot is always reserved (§6).
  **Chip row renders empty** — firing-only, nothing computable before the worker.
- **Tier strip:** six cells; absence is a dash (missing tier → `—` price; below-floor → `—`
  change); `◌` per cell only when that cell's price is current-month, keyboard-reachable, D-077's
  two tooltips.
- **Census pair, Phase 2 reality:** the population panel renders the six bars (real data for ~63%
  of the corpus; absent cells are 4px true-zero stubs) with the totals summary line — the gem-rate
  sentence is omitted until its inputs qualify. The grading-activity panel renders its designed
  degrade — `census history too young to compute pace` — with the `{N} OBS` badge counting
  **qualifying** observations under the 2026-09-01 floor (0 for every card today), its tooltip
  naming the floor and that deltas need two.

## 5. The price chart (D-084.7–9)

- **Engine:** TradingView Lightweight Charts, vendored + version-pinned, via the project's own
  Blazor wrapper — Phase 2 builds the minimal slice (line series, whitespace, crosshair, theming).
- **Data mapping:** value → point; hole → **whitespace point** (line breaks; gaps are gaps by
  construction); an isolated month (both neighbours missing) renders a small marker so it can't
  vanish; the current-month segment is a two-point dashed overlay series; a custom primitive draws
  the hollow dot at the first visible series' current-month value, in that series' colour
  (resolves OQ-14); series with no current-month value simply end — no tail, no dot.
- **Axes: mockup-minimal (D-084.9).** LWC scales hidden; the wrapper overlays two y-labels (visible
  max/min) and three month labels (first/middle/last) as HTML. Window: 12 months ending at the
  current month (resolves OQ-17).
- **Palette:** brand.md §2.6 `TIER_COLORS` (D-084.3). Default visibility PSA 10 + Grade 9 + Raw;
  legend chips are Blazor buttons with the ≥1-visible guard; a tier with no data at all renders its
  chip muted with a "no {tier} prices observed" tooltip.
- **Hover:** month-snapped vertical crosshair (horizontal off, magnet mode); tooltip pinned
  top-left, month label + one row per visible series, PSA 10 bold — built from
  `subscribeCrosshairMove`. The hollow dot's tooltip rides a focusable HTML hotspot.
- **Theming:** canvas can't inherit CSS variables — the wrapper's shim reads the brand tokens and
  calls `applyOptions` on every theme/CVD toggle.
- **Flat series** (OQ-4's NaN): resolved by LWC's autoscale; the prototype's divide-by-zero is not
  ported. **Rescale-on-toggle (R-10) is essential, not cosmetic:** real vintage spread is two
  orders of magnitude ($247 Raw vs $30,100 PSA 10 on card 630417).

## 6. Freshness and refresh (D-077, mechanics settled this session)

Trigger: snapshot arrives with `LastVisitedAt`; null or >24h → exactly one `POST /refresh`; fresher
→ no call. The paint never waits.

| Badge state | Slot (28px, always reserved) | Elsewhere |
|---|---|---|
| Fetching | 18px animated logo mark + `Checking for a newer price` | Stored prices full strength; as-of shows stored date |
| Landed (200) | Empties, height kept | Refetch snapshot + sales; figures update in place |
| Failed (404/409/422/429/500/502, network) | Amber `– as of {date} · {n}d old` | Stored numbers stand |
| Fresh (no call) | Empty | As-of reads today |

The loader is an **inline Razor SVG component** carrying the Logo prototype's animation
(`Logo:196–208`) — no missing asset files needed; honors `prefers-reduced-motion`; the nav logo
stays static (motion means work, exactly once, only while true).

Footer: prices/sales stamp from `LastVisitedAt`; census stamp from census `ObservedAt` with
corrected copy — census is captured on the same visits (C-17's false "can't be scraped on demand"
claim dies here); the graders publish upstream on their own cadence. TradingView attribution notice
+ link render in the footer (license obligation, D-084.7).

## 7. The sales ledger

- Complete ledger client-side; `<Virtualize>` renders visible rows only. Filters and sorts are
  in-memory over the full set.
- **Display mapping defined once:** DB `Ungraded` renders `Raw`; the `Raw` chip filters
  `grade_tier = 'Ungraded'`. All other 18 labels verbatim (D-081: the data has exactly these 19).
- Five columns (Date `YYYY-MM-DD` · Grade bucket · Realized · Source lowercase mono · Listing
  title). Listed price = 2px dotted underline on Realized + `listed $X → sold $Y` tooltip, colour
  from the **theme token** `--warnInk` (OQ-7, this element). Title cell ellipsis + full-string
  tooltip, Razor-encoded, never `MarkupString`.
- Sorting: desc-first, flip on re-click, arrow on active column only; grade sorts by vocabulary
  rank; ties fall to date inheriting direction — total, deterministic order.
- Resize: grips clamp 40–420px, one shared grid template for header + rows; **the fluid title
  column has no grip** (fixes OQ-6/I-3).
- Count line `{n} sales shown` (filtered count) carries the D-084.5 help tooltip: each grade is
  complete from its own first captured sale; nothing earlier was observable. **No seam rows, no
  captions** (C-7 stands). The About Data screen's spec inherits the full rolling-window story.
- Empty state, copy scoped to the selection (resolves OQ-5): "No sales observed in this grade /
  in these grades / for this card — that's a true zero: our scrapers visited and found none."

## 8. Errors and edge cases

| Condition | Renders |
|---|---|
| Unknown id | 404 page: "No card with id {id}." Chrome stays; no fake suggestions |
| Not-a-card | Same, with the honest reason |
| Delisted | Full page + muted `delisted {date}` chip; refresh still fires (worker permits deliberately) |
| Never visited (1 card today) | All-absent dress; `LastVisitedAt` null counts as stale → the express visit builds the page live |
| Tier with no data | Strip dash; muted legend chip |
| Flat visible series | LWC autoscale; no NaN |
| All hidden | Impossible (≥1 guard) |
| No image | Placeholder, lightbox disabled |
| Snapshot call fails | "Couldn't reach the data service" + retry |
| Sales call fails alone | Ledger panel error + retry; other panels live (independent by construction) |
| Refresh fails/429 | Amber badge; stored data stands |
| No current-month value for a tier | Line ends at last real month; no tail/dot/`◌` for it |

Day arithmetic in UTC. Long names wrap in the title, ellipsize in the breadcrumb. The delisted chip
and the not-found page are deliberate post-prototype additions, recorded in card.md §8 (§11 below).

## 9. The enrichment seam (consumes `PokemonInvestBatch`'s future schema change)

No ordering dependency in either direction. The sibling's enrichment is additive there (its own
table; the mirrored eight untouched), so it cannot break Phase 2 whenever it lands. On landing:
`SchemaDriftTests` surfaces the new migrations; one additive change in `CardStock.Infrastructure`
teaches the identity reader to prefer the enrichment columns where `match_status` is CONFIRMED
(parse-fallback otherwise — unmatched cards never show invented data); the subline extends to
`215/203`. No API contract change, no component change beyond rendering `SetSize` when non-null.
Inside Phase 2 if the timing allows; the first follow-up otherwise.

## 10. Testing and definition of done

One test project per source project; TDD during implementation.

- **Domain:** `CardTitle.Parse` exhaustively (trailing `#215`, bracket tags, `TG23`, no-number,
  hostile strings); floor-aware observation counting (D-033).
- **Infrastructure:** the three readers against real Postgres on the Pi's test databases (D-073):
  census latest-per-cell under change-only semantics; sales ordering/completeness; identity mapping.
- **Api:** parallel snapshot composition; status codes + problem details; image streaming from a
  temp store; refresh proxy against a stubbed worker (all passthrough codes + 429 bucket).
- **Web (bUnit):** strip states (dash/`◌`); `CensusBars` math (heights, 4px stubs, totals, per-card
  max scaling — D-084.8); ledger comparators and the 19-exactly-once chip partition; badge state
  machine; LWC wrapper data shaping (whitespace insertion, dashed-tail split, theme mapping) with
  interop stubbed. The canvas itself is the trusted library.
- **Done:** live on the Pi; dev cards 1958438 · 630417 · 5834844 · 844898 · 630415 render real
  data; degraded states verified against a thin card, the never-visited card, and a delisted card;
  one full refresh cycle observed; CI green including format; §11 applied; ledger consolidated.

## 11. card.md changes this design obligates (the maintenance rule)

Applied as the implementation plan's first task, so the screen spec never trails the ledger:

1. §2.3/§3.2 — strip absence-as-dash states; `◌` rules already present (D-077).
2. §2.4/§3.6 — chart engine = LWC wrapper; palette = brand.md TIER_COLORS (**C-20 → resolved**,
   D-084.3); axes mockup-minimal via overlay; whitespace gaps; dot colour rule (**OQ-14 →
   resolved**); flat-series behavior (**OQ-4 → resolved**); 12-months-ending-current (**OQ-17 →
   resolved**).
3. §3.8 — six-bar rule PSA 8/9/10 · CGC 8/9/10 (CGC 9.5 impossible — census grades are integers);
   per-card max scaling (**R-21 amended**, fixed 4020 retired); totals summary line; gem-rate
   sentence gated off until qualifying.
4. §3.7/§4.4–4.6 — Ungraded→Raw mapping; title-column grip removed (**OQ-6/I-3 → resolved**);
   `--warnInk` token for the listed underline (OQ-7 part); empty-state copy variants (**OQ-5 →
   resolved**); count-line tooltip (D-084.5).
5. §3.10 — census stamp copy corrected (**C-17/OQ-13 → resolved**: census rides the same visits);
   footer gains the TradingView attribution.
6. §5.3 — lightbox a11y additions (**OQ-8 → resolved**).
7. §8 — new rows: delisted chip; not-found page; route id = PriceCharting product id (**OQ-1 →
   resolved**); subline ships set + `#num` (D-079/D-084.10 — species to the Pokédex phase).
8. §4.11 — the launch-reality paragraph corrected per D-082/D-083 (ledger deep, census bars real).

Rows update in place per the audit-trail rule; reasoning survives.
