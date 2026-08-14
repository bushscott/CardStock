# CardStock — Decision & Fact Ledger

The running register of what is **true** about this project and what has been **decided**. Started 2026-08-10.

Entries keep their ID forever and move between status sections as they settle. See `CLAUDE.md` for the rules — the important one is that **"Verified" requires a receipt someone else can re-run.**

**Status meanings**

| Status | Means |
|---|---|
| **Verified** | Checked against code, data, or a document — with a `file:line` or query you can re-run |
| **Claimed** | Someone asserted it. Could be a doc, could be an agent. Not yet checked |
| **Disputed** | Contested, or two sources contradict each other. Unresolved |
| **Open** | A question that needs an answer before something else can proceed |
| **Decided** | The owner's call. Needs a reason and a date, not proof |
| **Superseded** | Was true or was decided; no longer |

---

## Verified

### D-085 — 🚩 The chip inventory existed in no current document; the retirement over-claimed. Restored
Found 2026-08-12 when the owner asked "did we define anywhere all the possible chips that could
display?" The answer was yes — the retired `DISPLAY_VOCABULARY.md` §1 held the complete inventory:
a 21-row trigger→chip table, the chip grammar (`icon + short name + evidence number`), firing-only /
cap 4 / priority / `+N more` rules, the five tracked-pill states, the complete amber-band list, and
the not-chip-eligible list. The retirement commit (`d54b40b`, 2026-08-10) claimed the file was
*"verified fully superseded"* — **false for §1**: card.md §3.3 carries only the machinery summary
plus three seeded triggers, and grep finds the inventory nowhere else; `design-rationale.md:71`
still points at the deleted file. **Restored verbatim into `docs/signals.md` ("Chip vocabulary")**
with provenance; receipt: `git show d54b40b^:"CardStock Mockup/DISPLAY_VOCABULARY.md"` is
byte-identical to the copy a prior session recovered into tmp. Lesson for future retirements: a
"fully superseded" claim needs a per-section receipt, same as any other Verified.

### D-080 — The corpus is fully crawled; month-axis sparsity is real. CLAUDE.md's open query, run
Live read-only queries against the Pi, 2026-08-12 (`ssh scott@192.168.0.56 "sudo -u postgres psql -d pokemon"`).
`SELECT count(*) FILTER (WHERE last_visited_at IS NULL), count(*) FROM cards WHERE delisted_at IS NULL AND not_a_card_at IS NULL;`
→ **1 never-visited of 91,596 active cards.** So the 113-rows-per-card average (D-071) is genuine series
sparsity, not an uncrawled corpus: per-card `price_months` counts run **p25 36 · median 103 · p75 213 ·
max 437** against ~408 for a dense six-tier backfill to Dec 2020. `DATA_MODEL.md` §5's "deep and uniform"
means uniform *depth* (~Dec 2020 for every card), not filled cells. **Consequence for Phase 2: an empty
(tier, month) cell is the common case — chart gap handling (gaps render as gaps, never bridged, D-061/
CLAUDE.md) is the main path, not an edge case — and a never-visited card page is a genuine rarity.**

### D-081 — Sale grade labels observed: exactly the 19-value vocabulary, no others
`SELECT grade_tier, count(*) FROM sales GROUP BY 1 ORDER BY 2 DESC;` (live, 2026-08-12) → **19 rows,
matching `GradeTierVocabulary` exactly**: Ungraded 2,635,173 · Grade 9 443,496 · PSA 10 441,223 ·
Grade 8 266,092 · CGC 10 141,894 · Grade 7 118,444 · Grade 6 70,356 · Grade 9.5 68,071 · CGC 10 Prist.
67,758 · Grade 5 42,303 · Grade 4 21,010 · TAG 10 19,783 · Grade 1 18,309 · ACE 10 14,045 · BGS 10
11,347 · Grade 3 11,171 · Grade 2 6,697 · SGC 10 6,329 · BGS 10 Black 2,636. `DATA_MODEL.md:236`'s
"21 distinct labels driven by the page's own selector" does not match the observed ledger (it may
describe the selector's option count). The Card page's filter-chip partition (card.md R-4 — 19 buckets
exactly once) holds with no holes.

### D-082 — 🚩 The launch-day ledger is deep, not empty: sales reach back to 2016
Live, 2026-08-12: `sales` holds **4,406,142 rows over 79,336 cards**, `sold_on` **2016-11-17 →
2026-08-12**; the most-active card (1958438, Ancient Mew) has **715 rows**. Mechanism, per
`DATA_MODEL.md` §5: a card's first visit captures whatever its per-bucket windows still held — years of
real history for thin cards, days for hot ones; forward of first visit is effectively complete; **no
pre-seam volume exists anywhere**. Three corrections follow. (1) `card.md` §4.11's "realistic launch-day
page shows a very short or empty sales ledger" is **wrong in the direction that understates readiness**
— the ledger must be built for hundreds of rows, and OQ-16 (windowing) has real stakes. (2) D-075's
"Card sales ledger … blocked until 2027" applies to liquidity *metrics* (floor-gated), not the ledger
*display*, which is rich today. (3) The per-bucket epoch seam inside the displayed ledger is a real
density discontinuity, which reopens the seam-marker question (C-6/C-7) for the owner during Phase 2
design. Top-5 live dev cards: 1958438 Ancient Mew (715) · 630417 Charizard #4 (615) · 5834844 Pikachu
with Grey Felt Hat #85 (604) · 844898 Moltres & Zapdos & Articuno GX #SM210 (599) · 630415 Blastoise #2 (588).

### D-083 — Census display data exists today; census deltas do not, and half grades cannot
Live, 2026-08-12: **57,464 cards** carry `populations` rows; observation depth is **1 for 54,562 cards,
2 for 2,902, none deeper**. So the population panel renders real current-census bars for ~63% of the
corpus now, while grading-activity deltas — which need two observations — count **zero qualifying
observations under D-033's 2026-09-01 floor** for every card today. Schema fact with design impact:
`populations.grade` is `short 1–10`, "array index + 1" (`DATA_MODEL.md` §3.4), so **half grades cannot
exist in census data** — the prototype's seeded `CGC 9.5` population bar (card.md §3.8) is unfillable,
and the panel's six-bar selection needs a rule written against real columns. Also confirmed from the
read models: `sales` stores **no listing URL**, settling OQ-15 — the ledger can never link out.

### D-078 — 🚩 Closed months DO revise. `DATA_MODEL.md`'s immutability claim is false
Found 2026-08-12 while verifying the price read layer's query plans against live data.

**The claim.** `DATA_MODEL.md:110` — *"Closed months are immutable server-side; only the current month
revises between visits."* And `:179` — *"closed months carry exactly one row forever."* Both are
repeated in `CLAUDE.md` and were used as reasoning in this repo.

**The counter-example**, card 630437 (`Charmeleon #24`, Base Set), read directly:

| tier | month | price_cents | observed_at |
|---|---|---|---|
| 0 `Ungraded` | **2026-07-01** | 222 | 2026-07-29 |
| 0 `Ungraded` | **2026-07-01** | **220** | **2026-08-04** |
| 4 `Grade9Half` | 2026-07-01 | 5850 | 2026-07-29 |
| 4 `Grade9Half` | **5832** | | **2026-08-04** |
| 5 `Psa10` | 2026-07-01 | 25000 | 2026-07-29 |
| 5 `Psa10` | **24635** | | **2026-08-04** |

On 29 July, July was the live month. On **4 August — after July closed** — all three restated. The
source revises a month after it ends, so "exactly one row forever" is not a property of closed months.

**Re-runnable receipt:**

```sql
SELECT tier, month, price_cents, observed_at,
       row_number() OVER (PARTITION BY tier, month ORDER BY observed_at DESC) AS newest_first
FROM price_months
WHERE card_id = 630437
  AND (tier, month) IN (SELECT tier, month FROM price_months
                        WHERE card_id = 630437 GROUP BY tier, month HAVING count(*) > 1)
ORDER BY tier, month, observed_at DESC;
```

**What it does not change.** The read layer resolves `max(observed_at)` per `(tier, month)` regardless
of which month it is, so `PriceSeriesBuilder` is correct as written and needs no edit. This is a
documentation error, not a defect.

**What it does change.** Any optimisation that special-cases "only the current month can have two
rows" is unsafe — including the obvious one of resolving only the newest month and trusting the rest.
Nobody has written that yet. This entry exists so nobody writes it later.

**Not established:** how often this happens corpus-wide, or how long after a month closes it can still
move. 17,804 of 10,357,098 rows are revisions (D-075 receipt), but that figure was never broken down
by whether the revised month was open or closed at the time. One card is proof the invariant is false;
it is not a measurement of the effect.

---

### D-076 — 🚩 The express-visit contract is wrong in three places, in `CLAUDE.md` and in D-062 itself
Found 2026-08-11 while designing the Card page's refresh. All three errors are ADR-0006 facts that
survived **ADR-0008**, which superseded them. Every claim below was read from source, not inferred.

**1. There is no single-flight. Express visits run fully parallel.**

`CLAUDE.md:151` states *"What remains in the worker: **single-flight** (one outbound fetch at a
time)"*. D-062 goes further and cites `SemaphoreSlim(1,1)` at three specific lines.

*Receipt:* `grep -rn "SemaphoreSlim" ../PokemonInvestBatch/src/` returns **exactly one hit** —
`Application/Crawling/PoliteGate.cs:13`, the turnstile express deliberately bypasses.
`ExpressVisitRunner.cs:42` is `private readonly Lock _sync = new()`, which guards the *in-flight
dictionary*, not a fetch gate. `ExpressVisitRunner.cs:26` says it plainly: *"in parallel with any
other express visit, with no floor, no queue, and no timeout."* ADR-0008's Consequences agree:
*"neither is its single-flight promise that express 'never opens parallel connections'… express
fetches run concurrently and unbounded."*

**Consequence, and it matters:** D-062 estimates the ceiling at *"roughly 30–60 requests/minute"* on
the strength of one-fetch-at-a-time. **That is not a ceiling.** The bound is whatever CardStock sends
concurrently, which makes D-037's abuse-shape limit more load-bearing than either entry assumed.

**2. 504 is dead.** `CLAUDE.md:145` and D-062's closing line both list a 504 timeout on
`express-visit`. ADR-0008:37 removed `Scraper:ExpressTimeoutSeconds` and its 504 response; ADR-0008:106
— *"504 leaves the express contract."* No timeout exists in the worker at all.

**3. Express refuses not-a-card only — delisted cards ARE visitable.** `CLAUDE.md:145` lists express's
409 as "delisted/not-a-card". `ExpressVisitRunner.cs:115–121` checks `NotACardAt` alone, with the
comment *"Delisted and benched cards ARE visitable here: express is exactly how an operator asks 'is
it back?'"* The delisted-409 belongs to `refresh-request` (`IntakeApi.cs:49`), not to express.

**The real contract**, read from `Intake/IntakeApi.cs:52–74`:

| Status | Cause |
|---|---|
| **200** | Parsed and committed |
| **404** | Unknown card |
| **409** | Not a card (**not** delisted) |
| **422** | Page fetched and refused — parse drift, or proved not a card |
| **500** | `ExpressErrored`, carrying a reason string |
| **502** | Upstream site failed |

**The number that constrains the frontend:** `Worker/Program.cs:80` — `http.Timeout =
TimeSpan.FromSeconds(60)`. With the worker's own deadline gone, a hung pricecharting.com returns 502
only after a **full 60 seconds**. Any CardStock code awaiting an express visit must therefore never
block a render — see D-077.

---

### D-001 — Per-sale and census history begin at each card's first crawler visit (late Jul 2026), not Apr 2025 / Jan 2026
The seam is **per-card and ragged**, not a single shared date.

**Receipts:**
- `PokemonInvestBatch/DATA_MODEL.md:404` — `visits`, `fingerprints`, `parse_failures` "begin at first deployment (2026-07-28)"
- `PokemonInvestBatch/DATA_MODEL.md:397` — population history "begins at each card's first visit (the site publishes no history)"
- `CardStock Mockup/DESIGN_NOTES.md:41` — "per-sale scraping started Jul '26. Census data 2026+"
- `git -C PokemonInvestBatch log --reverse` — first commit 2026-07-27

**Notes:** `HANDOFF.md` §1 and §5 said Apr 2025 / Jan 2026. Corrected 2026-08-10 with a dated note in §5. Owner confirmed: "That's completely false. It just started this month."

**Consequence:** every liquidity and supply indicator renders LOCKED for 6–12 months of calendar time that no engineering shortens. This is the largest scope fact in the project.

---

### D-002 — Monthly price history is genuinely deep: ~Dec 2020, backfilled whole on first visit
The one data series that is not thin.

**Receipts:** `PokemonInvestBatch/DATA_MODEL.md:373`, `:176–177` — "Backfilled to ~Dec 2020 for every card at its first visit. Monthly resolution, six tiers."

---

### D-003 — `price_months` carries exactly 6 price tiers
`Ungraded, Grade7, Grade8, Grade9, Grade9Half, Psa10`.

**Receipt:** `PokemonInvestBatch/src/PokemonInvestBatch.Domain/Parsing/PriceTier.cs:10–18`. Opened and read directly, 2026-08-10.

**Notes:** This is **not** the whole picture, per owner. There are three separate grade vocabularies and only the price series is limited to six:

| Domain | Granularity | Authority |
|---|---|---|
| Price series — Card tier strip, Charts, Screener price filters | **6** | `price_months.tier` (verified, `PriceTier.cs:10–18`) |
| Sales ledger — Card page ledger, grade chips, sort rank | **19** | `sales.grade_tier` (verified, `GradeTierVocabulary.cs`) |
| Binder — transactions and personal collection | **118** | the user (verified, `Cardstock Binder.dc.html:368–377`) |

**Granularity increasing toward the personal domain is correct, not a defect.** Owner, 2026-08-10: "we go into more detail in the binder page with transactions and personal collection." A user who owns a BGS 10 Black Label should record exactly that; forcing a coarser entry would make the product lie about their own collection. Each vocabulary is right for its domain.

**So the problem is a mapping function, not a data gap** — and it runs one direction only: a 118-label holding must map *down* to one of 6 price series to be valued. Nothing needs to map upward. See D-012, which is that function's specification.

---

### D-004 — There is no market index and no precomputed metric store in the scraper database
Eight DbSets: `Sets, Cards, PriceMonths, Populations, Sales, Visits, Fingerprints, ParseFailures`. No index table, no metrics table.

**Receipts:** `PokemonInvestBatch/src/…/PokemonDbContext.cs:8–22`; `grep -rniE "class (Index|MarketIndex|CardMetric|Metric)" src` returns nothing. Both run directly, 2026-08-10.

**Consequence:** the market index is load-bearing on 5 of 10 screens (Home ticker, Set sparkline, Charts compare/RS/beta, Binder "vs index", the entire backtest equity curve) and does not exist in any form. Confirms the need for D-010.

---

### D-022 — Grade tiers are pooled below 10 and split by company only at 10, and no multiplier may approximate the rest
The source reports one "Grade 8" figure covering every grading company, splitting by company only at grade 10.

**Receipt:** `../PokemonInvestBatch/docs/adr/0005-pooled-grade-tiers.md`, Accepted 2026-08-04. Read directly 2026-08-10.

**The evidence it records:** of "Grade 8" sales, 74.7% name PSA and 6.0% name CGC — ~91% of identifiable volume is PSA. CGC sells at ~0.68× PSA, dragging the blend down only 2–3%. Fewer than 3% of cards have two sales from each of two companies, so a per-card "CGC 8 price" would rest on zero or one sale.

**The rejected alternative matters more than the decision.** A global multiplier ("CGC ≈ 0.68× pooled") was rejected *by the owner* as statistically dishonest: "it projects a corpus-wide median onto individual cards where it may not hold, and presents an estimate with the same confidence as an observation."

**Binding consequence on the UI, stated in the ADR:** "The interface must not imply the pooled figure is company-neutral." `HANDOFF.md`:79 currently says "below 10 the buckets are grader-agnostic" — that phrasing asserts the neutrality the ADR forbids. Needs a wording fix; raised as part of D-012.

---

### D-010 — Card images exist on disk (~3.6 GB), and `HANDOFF.md` was right about them all along
~3.6 GB of real photos at `{ImageDirectory}/{hash}/1600.jpg`, joined to cards via `cards.image_hash`, refreshed hourly at 50/sweep.

**Receipts, read directly 2026-08-10:** `DATA_MODEL.md:292–295` ("Images — filesystem `{ImageDirectory}/{hash}/1600.jpg`… **~3.6 GB at full corpus**"), `:160` (`image_hash` column), `:325` (refresh cadence), `:105` (source CDN path). `DATA_MODEL.md:464` anticipates this app serving them.

**Correction, 2026-08-10 — my error, twice.** I twice reported that `HANDOFF.md` was "wrong that images exist." Re-reading it directly: line 114 sits under **"§6 Not built, deliberately"** and says *"Every card, set, and species image is a placeholder slot. This is the largest open risk, and it is a licensing question rather than a design one."* That describes the **prototypes**, where placeholder slots genuinely are what's there — and its second sentence is exactly right: the open question is licensing, not data. The document made no claim about the database. I read a section about unbuilt UI as a claim about storage and then repeated it. Nothing in `HANDOFF.md` §6 needs changing.

**What is genuinely open:** licensing. No repo records reading any terms of service, and storing is a different act from serving. Tracked under D-011.

---

### D-027 — The scraper states a single-writer rule for its own tables
"They still never write the scraper's tables directly — **the worker is the only SQL writer.**"

**Receipt:** `DATA_MODEL.md:459–462`, read directly 2026-08-10. Corroborates ADR-0006 from a second document.

**This is evidence, not a CardStock obligation** — see D-026. It is the scraper author's stated invariant, and stronger than I first characterised it, but adopting it as binding is still an open decision.

---

### D-028 — The read API is an explicit TODO in the scraper repo, not a prohibition
Directly corroborates the owner's reading of S-002.

**Receipts, read directly 2026-08-10:**
- `DATA_MODEL.md:463–466` — "**Nobody else.** The market data — the entire point of the system — has no consumer yet. The web app that will read `sales`, `price_months`, `populations`, and serve the images does not exist; **its read API is undesigned.**"
- `DATA_MODEL.md:472` — "**TODO (design): web app read API** — undesigned."

So the sibling repo does not forbid a read API. It has one filed as outstanding design work and names this app as its consumer.

---

### D-029 — `sales.title` is stored raw and must be HTML-encoded at render
A security requirement CardStock inherits, not an optional nicety. Listing titles come from marketplace sellers and are stored unescaped by design.

**Receipt:** `DATA_MODEL.md:472–473` — "must HTML-encode `sales.title` at render (stored raw by design)." Read directly 2026-08-10.

**Where it bites:** the Card page sales ledger renders a Listing-title column (`HANDOFF.md`:49, `DESIGN_NOTES.md`:47). Razor `@` output encodes by default, so the default path is safe — the risk is any `MarkupString`, `innerHTML` via JS interop, or tooltip/attribute injection. Worth a test, given D-005 makes Blazor certain.

---

### D-030 — There are eight tables, not nine. Thread resolved
`DATA_MODEL.md` §3.1–3.8 document exactly eight tables, matching the eight `DbSet`s. §3.9 is "Non-database data" (the filesystem image store) and §3.10 is relationships.

**Receipt:** `grep -nE "^### " DATA_MODEL.md`, run directly 2026-08-10.

**Origin of the discrepancy:** the initial survey agent reported "nine tables in three groups," counting §3.9 — which is explicitly *not* a database table — as one. Both its count and its "three groups" taxonomy were its own phrasing, not the document's.

---

### D-024 — A loopback intake API already exists, and it was built for CardStock
**Receipts:** `../PokemonInvestBatch/docs/adr/0006-localhost-intake-api-and-express-visits.md` (Accepted 2026-08-09), read in full; `src/PokemonInvestBatch.Worker/Intake/IntakeApi.cs:19–30` (three routes); `src/PokemonInvestBatch.Worker/ScraperOptions.cs:65` (`IntakeAddress = "127.0.0.1"`). Both read directly 2026-08-10.

Routes: `POST /cards/{id}/refresh-request` (202, fire-and-forget, takes the next crawl slot unless a burn-window-due card owns it), `POST /cards/{id}/express-visit` (synchronous, bypasses the polite gate, ~~200/502/422/504~~ — **corrected 2026-08-11: 200/404/409/422/500/502, no 504, see D-076**), `GET /healthz`.

**The ADR names this product explicitly** — "The product this scraper feeds is a trading website. Its web application… will live on the same Raspberry Pi, read the same Postgres." The integration was designed for, not retrofitted.

**Two hard consequences:**

1. **The ownership rule.** Each codebase migrates and writes only its own tables. CardStock's own tables (users, binders, holdings, watchlists, saved screens) are CardStock's to write normally. The scraper's eight tables are **read-only** to CardStock — there is no write path into them, not SQL and not HTTP.

   **Corrected 2026-08-10.** This entry originally read "Mutations go over HTTP," which was my generalization sitting next to a real quote, not something the quote said. The intake API is **not a write channel and not a CRUD surface** — both endpoints take a card id, accept no data, and exist for two specific scenarios. Owner: "those two endpoints exist for two very specific scenarios. They do not exist for normal CRUD operations for the database at large." The originating error is recorded in `CLAUDE.md` under the verify-everything rule, as the worked example of a receipt being stretched past what it covers.
2. **Loopback binding constrains the frontend.** A browser cannot reach `127.0.0.1` on the Pi. Any code calling these endpoints must run server-side on that machine. This bears directly on D-013 and D-014.

~~**Corrects an earlier claim.** The initial survey flagged express-visit as "an unthrottled outbound amplifier… up to 8,640 fetches/day if scripted." That overstated it: single-flight (at most one express visit in flight, ever), a 10 s spacing floor, same-card coalescing, and `PoliteGate.RecordFetchNow()` are all in place, and the ADR bounds worst case at "one request per spacing floor." The residual concern the ADR itself names is narrower — express can still poke the site once per spacing floor *during a three-strike pause*, with a "refuse express during the pause" toggle noted as the follow-up.~~

> **⚠ Struck 2026-08-11 — the correction was itself wrong, and it corrected in the wrong direction.**
> ADR-0008 removed **both** guardrails this paragraph rests on: the 10 s spacing floor (D-062) and
> single-flight (D-076). Of the four mechanisms listed, only same-card coalescing and
> `RecordFetchNow` survive, and neither bounds volume. **The original survey's "unthrottled outbound
> amplifier" reading is the accurate one** — the worker enforces no ceiling at all, and ADR-0008 says
> so in as many words: *"Rate limiting moves to the calling app… The worker no longer bounds express
> volume at all."* This is the second time a reassuring number about express load turned out to be
> unsupported; treat any such figure in this ledger as suspect until re-read from
> `ExpressVisitRunner.cs`.

---

### D-023 — The sibling repo's engineering conventions, verified
Read directly 2026-08-10, to be mirrored per D-018–D-021.

| Concern | Convention | Receipt |
|---|---|---|
| Target / language | `net10.0`, nullable enabled, implicit usings, **warnings as errors**, invariant globalization | `Directory.Build.props` |
| Style | 4-space C#, 2-space config, LF, file-scoped namespaces (warning), explicit accessibility (warning) | `.editorconfig` |
| CI | restore → build → test → `dotnet format --verify-no-changes`, Postgres 15 service container pinned to the Pi's version | `.github/workflows/ci.yml` |
| ADRs | Nygard format, numbered, **never edited after the fact** — reversals get a superseding ADR; index table in `docs/adr/README.md` | `docs/adr/README.md` |
| Layout | `.slnx`, `src/` + `tests/`, strict `Domain → Application → Infrastructure → Worker`, one test project per source project | `PokemonInvestBatch.slnx` |

**Note on the CI comment worth preserving:** the Postgres version is pinned because the tests assert on SQL the Npgsql provider generates — "a version drift here would pass in CI and fail in production, which is the one failure a test suite must not have."

---

## Decided

### D-095 — Full dates display MM-DD-YYYY, app-wide
Owner, 2026-08-14, after seeing the footer's ISO stamps: *"going forward and in general, if
we're using the full date, use month month day day, year year year year."* Supersedes the
`YYYY-MM-DD` display convention (the card-page plan's global constraint) wherever a full
month-day-year date RENDERS: freshness footer, refresh badge, census "as of" stamps, the
ledger's Date column, the delisted chip, and every date-bearing tooltip/note (locked unlocks,
census-metric gates, ghost-chart closes). Separator: dashes — `08-14-2026` — first built with slashes as the default US reading, corrected
by the owner to dashes the same day. One helper enforces it: `CardStock.Domain.Dates.Full`,
explicit en-US per D-070's deliberate no-InvariantGlobalization stance. **Display only**: wire
payloads (`yyyy-MM`), chart time keys, and ledger sort keys stay ISO (sorting is on the
`DateOnly`, so the display change cannot break date order), and month-only labels (`Sep ’26`)
are untouched. Tradeoff noted once, not relitigated: ISO is internationally unambiguous;
MM/DD/YYYY matches the product's US audience — the owner's call. Swept 8 display sites + every
test expectation; 362 green; live-verified on Charizard 630417 the same day. Spec: card.md §3
convention box + §3.7 Date column row.

### D-094 — The ghost delta chart: the grading-activity bar row ships as its own placeholder
Owner, 2026-08-14: *"I would like a 'ghost' chart show up under Grading activity · PSA 10 slabs
added"* (metric-slot copy free to rework; it wasn't needed — both notes stand). The prototype's
seeded 7-bar delta row, absent since D-087 reduced the panel to metric slots, returns as a
**ghost chart**: seven month slots ending at the current month, window start clamped to the
D-033 floor's month (`max(Sep ’26, currentMonth − 6)`) so a month that can never fill is never
promised. Each slot materializes as a real bar — prototype fill, `+N` mono value, `maxD` scaling,
restated months as 4px stubs with a true `−N` — the moment its month closes with the pace gate
met (≥ 2 qualifying observations); until then it renders a dashed `--line3` outline at uniform
44px with **no number** (rule 2: a ghost may never read as data) and a tooltip naming its own
unlock (`closes {date}`, or the gate copy on under-observed cards). The current month always
rides as the trailing ghost — giving purpose to the prototype's vestigial `d.bd` border plumbing
(§3.9/OQ-10's suspected "outlined current/partial month"). Wire: `CensusDto.DeltaBars`
(`{month, label, state: observed|pending, delta, tooltip}`), computed read-time beside D-093's
metrics in `CensusMetrics.DeltaBars`. At launch every card shows Sep ’26 – Mar ’27 as seven
ghosts; the first bar materializes 2026-10-01 with no deploy. Spec: card.md §2.6 (D-094 box).
Suites 362 green; live-verified on Charizard 630417 the same day.

### D-093 — The census sentences: implemented now, unlocked by data checks
Owner, 2026-08-13, immediately after confirming the collection side is already right (*"if you
have the ability to collect the data, and we're just waiting for the data to fill in, let's write
the code and implement the calculations and hide it behind a data check"*). This supersedes the
§2.6 amendment's worker-phase deferral: the gem-rate and pace sentences (card.md §3.8/§3.9) are
computed **read-time in Domain** (`CensusMetrics.Evaluate`) from the card's own `populations`
history — which `CardCensusReader` already fetched in full — behind per-metric sufficiency gates.
Each grading-activity slot flips from `LOW DATA` to its computed sentence the moment its own data
qualifies, with no further deploy: pace's first monthly delta closes **2026-10-01**, gem rate's
90-day window fills **2026-11-30**, its drift clause follows ~**2027-02-28**.

The honesty mechanics: levels flat-fill from the full history (the populations storage contract —
pre-floor levels are real) while measurement windows must start on/after the D-033 floor, so the
first-visit backfill can never masquerade as movement; UTC day arithmetic; boundary rows close
the earlier period; restatement-negative deltas clamp to `0 of 30`, never negative progress; the
trend word waits for six closed months; a zero starting census omits the growth clause rather
than divide by nothing. Referee: the §3.9 seed derivations the spec hand-checked against the
prototype (331 new 10s · +29% · +58/mo · rising · supply-pressure red) are reproduced exactly by
`CensusMetricsTests.Pace_reproduces_the_spec_seed_arithmetic_exactly` — 18 domain facts in all,
353 tests green across the six suites. Live-verified on Charizard 630417 same night: both slots
render LOW DATA with their own unlock copy (gem: *"the window fills 2026-11-30"*; pace: *"0 so
far — deltas need two"*), truthful against its single pre-floor observation (2026-07-28).
Wire: `CensusDto` gains `Metrics` (state + headline value + toned sentence segments);
`CardCensus.From` now takes the full observation list and carries it.

### D-092 — The signals panel: the owner's card-page rework, adopted and built
Owner, 2026-08-13 (evening), by authored rework file — `CardStock Mockup/Cardstock
Card.rework-2026-08-13.html`, committed beside the frozen prototype as the authority for the
lower identity-header region **only**. The tier strip became a 3×2 grid of square tiles; the chip
row became an unbounded **Signals panel**: every evaluated signal as a row in exactly one of five
states (firing · quiet · below-floor · neutral · locked), a computed
`{evaluated} evaluated · {firing} firing` count line, locked rows naming their unlock. Ships
D-088's parked full-status surface (its entry moved to this section with a pointer). Three owner
rulings from the same session bound the build:
1. **Unbounded rows.** The mockup's eight rows are a sample, not a cap; more than eight signals
   can fire and every firing row always renders; the auto-fit grid wraps.
2. **Keep D-077.** The rework's tile tooltip (*"latest monthly price · +6.2% over 30 days"*) and
   its missing ◌ are seed-copy regressions — the new tile geometry keeps the ◌ month-to-date
   glyph and card.md §2.3.1's two-tooltip table.
3. **No fake values.** Substrate-less rows render as locked states, never seed numbers:
   `RS vs index 3M` (needs the Phase 3 market index) · `Pop Δ 60d` (needs census deltas;
   observations count from 2026-09-01) · `Churn 30d` (`unlocks 2026-10-31` — derived as the
   D-033 floor + 60 days, `{n} recorded` counting days since the floor, 0 until it arrives).

Built and shipped the same night (plan `docs/superpowers/plans/2026-08-13-signals-panel.md`):
`ChipEngine.EvaluateRows` — the 8 price signals three-state, **RSI (6)** new (caution ≥ 70,
positive ≤ 30, floor 7 closed months), **tier spread 10/9 redefined** (ratio always shown; fires
at ×4 or a ≥ 20% move vs 6 closed months earlier); `SignalsDto {evaluated, firing, rows}` with
counts computed from the rows; mapper composition (volume row over `(today−30, today]`, the three
locked rows, display order firing → neutral → quiet → below-floor → locked); web (3×2 `TierStrip`
tiles, `SignalsPanel`, `SignalChips` retired). Spec updated with the build: card.md §2.2/§2.3
amended, §2.3.2 new, §3.3 amended, §8 C-25. Commits `95bde9b` · `8a6cfb7` · `e5f1f3c`.

**The math carries an external referee (owner challenge, mid-build).** The owner questioned
hand-written indicator formulas — *"just because … it compiles and spits out a number doesn't
mean they're accurate."* Resolved with receipts, not reassurance: **`Skender.Stock.Indicators`
2.7.3** (test-only dependency, itself validated against TA-Lib) cross-validates the arithmetic on
every run — RSI matches the hand fixtures to 1e-6 (`82.05128…` = 100·32/39 by hand fraction),
EMA(3/6/9) and MACD(3,6,4) converge on a 120-month series where seeding influence has decayed
(agreement proves the recursion coefficients; a wrong α diverges), ROC and the log-trend
regression (slope and R²) match directly. Receipt:
`tests/CardStock.Domain.Tests/Signals/IndicatorsCrossValidationTests.cs` — 8 facts, green. The
z-score keeps its hand fixture plus a stdlib receipt (`python3 statistics.stdev` → `2.0412`,
sample n−1) because Skender's z uses population stddev — a convention difference, not an error.
Two deliberate deviations, documented in code: our EMA seeds with the first value (pandas
`ewm(adjust=False)`) so the 7–12-month authored windows yield full-length values, where the
SMA-seeded classic would push most of the corpus below-floor; and a flat RSI window reads **50**,
not the no-loss convention's 100 — a dead-flat card is not "overbought." Production stays
hand-written decimal for the floors and honesty semantics; the library referees it.

**Live-verified on Charizard 630417, 2026-08-13, predictions locked before reading the API.**
From the Pi's `price_months` (latest-per-`(tier, month)`, closed months Jan–Jul 2026): PSA 10
closes 14072.29 → 16358.00 → 18055.00 → 26698.31 → 29871.57 → 30100.00 → 30100.00 — six deltas,
zero losses → predicted RSI 100 → live row `– RSI (6) · overbought`, tooltip `RSI(6) 100` ✓.
Spread: 30100.00/3275.00 = ×9.2 (≥ ×4, and +55% vs Jan's 14072.29/2380.64 = ×5.9) → live
`▼ ×9.2 … ×5.9 six closed months ago` ✓. ROC: 30100/26698.31 − 1 = +12.7% → live quiet `+13%` ✓.
Corroboration: the live MACD tooltip's `histogram +94` equals the +94 this ledger's D-088 entry
recorded for Charizard independently, a day earlier, from the chips era. Count line
`12 evaluated · 4 firing` = 4 firing + 1 neutral + 4 quiet + 3 locked rows ✓ — two firings are
old rules preserved (MACD, R²), two are tonight's roster changes (RSI new, spread redefined).
Headless-Chrome screenshots (per the browser-quirks memory) confirmed the 3×2 tiles with ◌, the
panel's five states, and the deferred Charts link. One rendering bug found and fixed live: the ◌
inherited Inter, which lacks U+25CC — the system-fallback glyph is ~12px wide vs JetBrains Mono's
7.1px (canvas-measured), overflowing the 100px tile's 76px content box and ellipsizing
`GRADE 9.5` to `GRADE …`. Fix: the glyph is explicitly JetBrains Mono (`TierStrip.razor.css`);
label 61.5px + gap 4 + glyph 7.1 = 72.6px fits with margin.

### D-091 — The ledger cap moves to the bucket: newest 300 per grade, lifetime
Owner, 2026-08-13, minutes after D-090 shipped — seeing PSA 10 Charizard hold exactly 30 lifetime
sales reaching to Dec 2023 exposed the twelve-month window as backwards: it hid a slow bucket's
whole life while doing nothing about the fast buckets that actually grow without bound (the
source's own ~30-row-per-bucket display means slow buckets arrive deep and fast buckets accumulate
forever). Ruling: **the query ships the newest 300 rows per grade bucket, lifetime, no time
window** (`ICardSalesReader.BucketCap`, one constant shared by query and copy). A bucket truncates
only past 300 captured sales, so every rare bucket shows its complete history — the owner’s stated
requirement — while the ceiling is 19 × 300 however long the crawler runs. Proven at the boundary
against real Postgres (a 302-row bucket drops exactly its two oldest; a 3-row 2016 bucket ships
whole). D-090’s client paging stands at an owner-tuned **25 rows per page** (down from 50); its time window is superseded. Revisit conditions
deliberately not authored yet (owner: “don’t do the tripwires yet”).

### D-090 — The sales ledger: twelve-month window, 50-row client pages, no server pagination
**⚠ The twelve-month window was superseded the same day by D-091; the client paging stands.**
Owner, 2026-08-13, after seeing the live 615-row ledger and weighing multi-user database load.
Claude's measurements framed the call: corpus max 717 sales/card lifetime (p99 288, median 46), the
615-row query serving in 2ms off `sales(card_id, sold_on)`, so server pagination would trade the
ledger's instant complete-set filters/sorts for a payload problem that doesn't exist. Rulings:
**(1)** the query caps at a rolling twelve months, cutoff inclusive, in `CardSalesReader` — older
rows never leave the DB, and growth is bounded by a card's annual sales rate; **(2)** the display
pages at 50 rows client-side over the fully filtered/sorted window (*"looking at 600 rows is
dumb"*), resetting to page one on any filter/sort change; **(3)** no server pagination, with the
revisit condition authored: it becomes its own designed task (keyset cursors, server-side
sort/filter, honest counts) only if a card's window crosses ~5,000 rows or sustained multi-user
load arrives. All window-scoped copy amended so the true-zero empty states never deny sales older
than the window. card.md §2.5 carries the build detail.

### D-089 — The chart tooltip follows the crosshair horizontally (owner, over the mockup's pin)
2026-08-13, on the live page. The prototype pins the hover box top-left (:135); the owner asked for
cursor-following, Claude recommended keeping the pin (occlusion, jitter, TradingView precedent),
and the owner chose the offered compromise: **horizontal-follow** — the box rides 12px right of the
cursor, top fixed at 8px, clamped to the pane so it parks at the edges, falling back to the pinned
corner when no pointer x exists. Recorded because it deliberately deviates from the frozen mockup;
card.md §5.4 carries the amendment.

### D-088 — The chip row as a full signal-status surface (held, then shipped by D-092)
**Resolved 2026-08-13, the same day it was parked — the owner returned with the rework D-092
adopts, and the panel shipped that night.** What shipped differs from the sketch below in two
ways: nothing collapses (the panel is unbounded, so there is no `locked signals (22) ▾` fold — the
three currently-evaluable locked rows render inline), and a fifth state exists (neutral, for
never-directional liquidity rows). The MACD-tooltip note at the bottom shipped too: firing MACD
tooltips carry the histogram in rounded dollars (`· histogram +94 ·`); the "and direction" half
was deliberately kept simple — magnitude only, no trend. Entry moved from Open; original text
kept below for the record.

Owner, 2026-08-13, reviewing the live chips: show **every** signal, graying out the ones that
aren't firing — then held off on learning the catalog is 29 signals, not 7. Parked, not rejected.
The shape sketched in conversation, kept so it isn't re-derived: the 7 computed signals always
render with four states — firing (toned) · quiet (grayed, **showing its actual value**) ·
below-floor (grayed dash, tooltip naming the floor — never a number) · locked (deferred, tooltip
naming the substrate) — and the 22 substrate-locked signals collapse behind one deferred
`locked signals (22) ▾` chip reusing the `+N more` popover. Adopting this would supersede the
firing-only carve-out recorded alongside D-084.1 (DISPLAY_VOCABULARY.md:7 — "no placeholder
chips"), and changes a chip's meaning from "signal detected" to "signal status."

Independently worth doing when this reopens: the MACD chip's tooltip should carry the histogram's
value and direction — Charizard's `MACD +` today sits on a histogram that collapsed +1,424 → +94
over four months, and "barely positive, falling" is the answer to the owner's actual question
("should this worry me?") that the bare `+` cannot give.

### D-087 — Placeholder-first UI: every slot ships, labeled honestly, before its data or feature
Owner, 2026-08-13, reviewing the live card page: *"Even if you don't have the functionality wired
up, put the UI in with placeholder controls or labels."* A standing rule for all remaining
development. It extends D-084.1 (later-phase **controls** render deferred-disabled with honest
tooltips) to **data-shaped slots**: where a field's data does not exist yet, the slot still
renders, holding a placeholder label that is self-evidently not data — pending tone (`--mut2`),
deferred treatment, tooltip naming the phase that fills it. This supersedes card.md §3.1.1's
pre-amendment distinction ("a field that does not exist… there is nothing to render, disabled or
otherwise"); that spec section carries the dated amendment.

**Boundary — the two rules are untouched.** A placeholder *label* is UI scaffolding; a
computed-looking *value* is data. Metrics below their floors still render states (`LOW DATA`,
`LOCKED`, …), never placeholder numbers, and identity fields render placeholder labels, never
guessed values — the species name is still never derived from the title string (D-084.10 unchanged
on sourcing).

**First applications, same day:** the subline's character segment (deferred label `Pokémon name`,
tooltip `The Pokémon's name arrives with the Pokédex phase`; real names arrive with the Pokédex
phase's tag table) and the root page (the Home feed's slot renders `Home arrives in a later phase`,
retiring the Blazor template's "Hello, world!" filler the Phase 2 plan's deletion list missed).

**Known omission left un-ruled:** the census gem-rate sentence (card.md §3.8) is omitted until its
inputs qualify; under this rule it arguably becomes a visible state line instead. Not changed —
needs an owner call when it next comes up. **Ruled later the same day:** the owner chose the state
lines — Gem rate and Pace render as slots with `LOW DATA` chips naming the unlock condition; built
2026-08-13, card.md §2.6 carries the copy.

### D-084 — Phase 2 scope rulings: no auth, URL-only reachability, brand.md tier palette
Owner, 2026-08-12, at the start of the (restarted) Phase 2 brainstorming:
1. **No auth in Phase 2.** The Card page ships anonymous. Watchlist and binder render
   deferred-disabled per the present-not-omitted ruling; the D-062 abuse-shape limit binds **per-IP**
   until accounts exist; auth arrives with its first real consumer (Binder, Phase 4, or a public URL —
   D-011).
2. **URL-only reachability.** No card lookup ships in Phase 2; the nav search element renders
   deferred-disabled; navigation arrives with its own screens (D-075 ordering).
3. **Tier palette: brand.md §2.6 `TIER_COLORS` — the Charts values.** Grade 9.5 `#7A56C9`, Grade 8
   `#4C8F8A`, Grade 7 `#A96A4A`. **Resolves C-20/OQ-21**; the Card prototype's three variant hexes
   (`Cardstock Card.dc.html:325`: `#6E4DB8`, `#2E7F78`, `#B0552E`) are superseded. card.md §8 row
   update lands with the Phase 2 spec.
4. **Census six-bar rule: fixed premium six — PSA 8 · PSA 9 · PSA 10 · CGC 8 · CGC 9 · CGC 10.**
   Chosen in the visual companion against Charizard #4's live census after seeing the mocked
   `CGC 9.5` bar is unfillable (D-083: census grades are integers 1–10). CGC 8 substitutes for the
   impossible 9.5; the mid-grade mass is carried by a **total-slabs count in the panel's summary
   line** rather than bars, preserving R-20/R-21 (grader grouping, fixed cross-card scaling).
   Sub-line names both graders' totals so 25%-of-census framing stays honest. card.md §3.8 update
   lands with the Phase 2 spec.
5. **Ledger epoch boundary: no in-table markers and no caption — C-7 stands.** Owner's argument,
   accepted after the live-data review (D-082): within its range each grade's record is complete —
   a sale newer than the list's oldest entry is by definition on the list — and this persona reads
   marketplace sold-lists as windowed by default, so a permanent caption defends against a misread
   the audience largely doesn't make. Honesty lands in zero-chrome places instead: the ledger
   row-count carries a help tooltip ("each grade is complete from its own first captured sale;
   nothing earlier was observable"), and the About Data screen's spec gets the rolling-window story
   when its phase comes. The marketable fact is forward completeness, not the window. Resolves the
   reopened half of OQ-3 for this screen.
6. **API shape: snapshot + sales split, per-panel readers composed concurrently inside the
   snapshot.** Owner's synthesis: keep the two GETs (small snapshot for immediate paint, full ledger
   separately — ≤715 rows known max, D-082, rendered with `<Virtualize>`), but organize the snapshot
   internally by panel — independent readers (identity, prices, census) run in parallel via
   `IDbContextFactory`, since one EF context is not thread-safe. Grouping follows the must-not-drift
   invariants: strip+chart share the price reader (R-2); census bars+sentence share the census
   reader (R-26). Cross-reader skew mid-refresh is harmless on append-only data and heals on the
   post-refresh refetch. Owner adds the standing expectation (not a constraint): this app will call
   for highly parallelized workloads even where computation lives in the database — the per-reader
   connection is the unit that scales into that; corpus-scale work still prefers set-based SQL and
   worker lanes over connection fan-out on the four-core Pi.
7. **Charting: TradingView Lightweight Charts, reaffirmed.** The owner recalled the harvested
   ruling my keyword grep missed — D-042's `PROJECT_LOG.md:242` harvest: *"charting locked.
   TradingView Lightweight Charts via JS interop, 'Blazor wrapper component = portfolio
   centerpiece.'"* Phase 2 builds the wrapper's minimal slice for the Card page's price chart:
   self-hosted bundle, line series with **whitespace points for gaps**, dashed provisional tail as a
   two-point overlay series, hollow dot via custom primitive, month-snapped crosshair, and an
   `applyOptions` theming shim reading the brand tokens (canvas cannot inherit CSS variables).
   License verified 2026-08-12: Apache 2.0 **plus required attribution** — TradingView notice and
   link on the page; goes in the app footer. Panes/markers for MACD etc. wait for the Charts phase.
8. **Census bars: styled divs inside a values-only `<CensusBars>` component — LWC is reserved for
   time axes.** Chosen in the companion against a best-case LWC histogram mock (grades need fake
   timestamps, value labels need a custom plugin, grade labels end up as HTML under canvas anyway).
   All presentation math lives in the component. Its scaling rule becomes **per-card max** (tallest
   of the six bars fills the row; 4px stub floor stays): the prototype's fixed `maxPop = 4020`
   cannot survive real data — Charizard #4's PSA 8 is 15,931 — so R-21's cross-card-comparability
   half is retired as seed fiction. card.md §3.8/R-21 updates land with the spec.
9. **Chart axes: mockup-minimal.** Chosen in the companion against an LWC-native-axes rendering of
   the same real series. The wrapper hides LWC's price and time scales and overlays the frozen
   prototype's five labels as HTML — visible max/min on the left, first/middle/last month below.
   TradingView attribution is satisfied by the notice + link in the app footer (whether the
   in-canvas logo mark also stays is resolved at build time against the shipped LWC version; either
   placement satisfies the license). Hover model unchanged from the spec: month-snapped vertical
   crosshair, tooltip pinned top-left, one row per visible series, built from LWC's crosshair
   events.
10. **Species is out of Phase 2 and out of the TCGdex enrichment entirely.** Owner: *"in another
   phase, we will have to create a Pokedex, and it will belong in there."* The enrichment's required
   scope is collector number + official set size only (handoff amended same day); the Phase 2
   identity DTO carries no Species field; the subline's finished Phase 2 form is `{set} · 215/203`.
   The Character screen's subline link waits for the Pokédex phase alongside the species data
   itself.
11. **The signal-chip engine ships in Phase 2 — the seven S1-computable signals plus the full chip
   machinery.** Owner, choosing completeness over ease (*"not looking for the easiest. I'm looking
   for the best"*), after the completeness sweep surfaced that chips had never been designed for
   this phase and D-085 restored the chip inventory. Roster: ROC 3M · MACD 3,6,4 · EMA 3×9 cross ·
   z vs 6M MA · tier-spread compression · trend R² · drawdown — computed in Domain, on request,
   closed months only, firing-only with cap 4 and `+N more`. Two details authored in spec §12: the
   **anchor-tier rule** (PSA 10 when it clears the floor, else the highest tier that does; tooltip
   names the tier) and the **compression threshold** (PSA 10/PSA 9 ratio ≤ 0.8× its value 6 closed
   months earlier). RS, liquidity, census, and composite chips stay silently absent until their
   substrates exist. The engine is the seed of the Phase 3 worker's corpus-wide computation.

### D-077 — Stale prices are shown, never hidden. The Card page's freshness treatment, settled
Owner, 2026-08-11, across a mockup session. Implements the call pattern D-062 already recorded: on
card page load, if `cards.last_visited_at` is older than 24 hours, call `express-visit`.

**The question was whether the page waits.** The owner's opening instinct was a skeleton loader, and
his stated reason for it was that *making people wait for fresh data is more on brand.* Both were
reversed by one fact about the data.

**The fact.** `DATA_MODEL.md:110` — *"Closed months are immutable server-side; only the current month
revises between visits."* And `:179` — *"a typical visit adds 0–2 rows (the current month moved);
closed months carry exactly one row forever."* The price block renders 6 tiers × 12 months = **72
values**. A refresh can move **at most 6** of them, and typically moves 0 to 2. A skeleton hides 72
real values to wait on 2, and tells the visitor that eleven-twelfths of a chart which is as true as it
will ever be should not be trusted. **That is a false statement about our own data** — the one thing
this brand cannot make.

**And "make them wait" was never the rule.** The five states disclose; they do not conceal. `LOW DATA`
renders the number *and* names the rule it failed. A skeleton is the only treatment with no
disclosure in it at all.

**So, decided:**

| | |
|---|---|
| Stored prices | Render **immediately, at full strength.** Never skeletoned, never dimmed |
| The as-of date | Always shown — `cards.last_visited_at`, not "now" |
| A refresh in flight | Never blocks the paint. The page renders, then updates in place |
| Unknown card id | A 404 page, not a loading state |
| Never-crawled card | Rare (the crawler front-runs new releases). Identity paints, price block empty, express fills it |

**The tier strip gets a provisional marker, and this resolves a contradiction nobody had caught.**
`card.md:107` records the invariant that each strip price equals index 11 of that tier's chart array —
i.e. **the six strip prices are the current, unfinished month.** The chart marks that same number with
a dashed final segment and a hollow dot (`card.md:144`). The strip marks it with nothing, and its
tooltip calls it *"latest monthly price"* — what a finished number would say. Same value, two honesty
treatments, one screen. Logged as **C-22** in `card.md` §8.

Resolved with `◌`, which `brand.md` §4.2 already defines as *"current month provisional"* — an existing
glyph for this exact meaning, not a new invention. It is text, so it survives colourblind mode and is
read aloud. It carries its own tooltip explaining the symbol, separate from the cell's tooltip
explaining the value. **It belongs to the row, not the layout** — it disappears when the month closes.

**The refreshing indicator uses the logo loader** (`Cardstock Logo.dc.html:196–208`, moved into
`CardStock Mockup/` on 2026-08-11). Owner chose the badge placement over a global nav-logo indicator,
which was the recommendation. Consequences that follow from that choice and are binding:

- **An 18 px mark** — `Logo:145` floors the mark at 16 px, so the badge is sized to the logo, not the
  reverse.
- **A fixed 28 px slot** that exists whether or not a badge is in it. Without it the six price boxes
  jump a moment after paint, undoing the reason for showing real data immediately.
- **The logo appears only while a fetch is genuinely in flight** — not on success, not on failure,
  never as decoration.
- **The nav logo stays static.** The mark is now on screen twice; exactly one may move.
- **On failure the badge becomes amber `– as of 8 Aug · 3d old`** — `brand.md` §4.2's "caution,
  directionless" en dash, in the one hue colourblind mode leaves alone. The prices do not change,
  because they were never wrong.

**What this pins down for Phase 1** (D-075), which is the whole reason it was worth settling now:
the read layer must return the card's **`last_visited_at`** beside every price, and must tell callers
**which month is the current, unfinished one.** Neither was obvious before the screen was drawn.

**Still open:** four brand files named at `Logo:250–251` do not exist — `logo-animated.svg`,
`logo-loader.svg` and both `-dark` variants — and `Logo:257` requires them in **SMIL, not CSS**, so
the motion survives being embedded as an `<img>`. `Cardstock Loading States.dc.html`, linked at
`Logo:211` for "full usage, sizes, and placement rules", does not exist either.

**Spec updated:** `docs/screens/card.md` — new **§2.3.1** (the strip's provisional marker and both
tooltips), new **§4.2.1** (the whole freshness and refresh flow, which the spec never had), and §8 rows
**C-22** and **C-23**.

---

### D-075 — The build sequence, and why it is ordered by data rather than by screen
Owner approved 2026-08-11 (*"that sounds good"*). Recorded because it was agreed in conversation and
existed nowhere else.

**The ordering principle:** sort features by what data actually exists today, not by screen. Doing
that produces a spine that cuts across six screens:

| Works on today's data | Blocked until 2027 |
|---|---|
| Browse, Set, Card price chart | Card sales ledger, census |
| Charts price and trend indicators | Charts liquidity and supply rows |
| **Binder — holdings, cost basis, P&L** | Binder vs benchmark *(needs the index)* |
| Home watchlist | Home feed *(needs the worker)* |
| Screener price filters | Screener liquidity filters |

Everything valuable that works **today** rides `price_months` (deep to ~Dec 2020, D-002) plus
CardStock's own tables. `sales` and `populations` are a 2027 story no engineering shortens.

| Phase | What | Status |
|---|---|---|
| 0 | Walking skeleton: solution, CI, schema, deployed to the Pi | ✅ **Done** 2026-08-11 |
| 1 | `price_months` read layer — change-only as-of semantics, encoded once, tested hard. **Plus the 30-day sales read** — see below | ✅ **Done** 2026-08-12 |
| 2 | Card page end to end, with real `LOCKED` / `LOW DATA` states | ✅ **Done** 2026-08-13 |
| 3 | Worker: index construction first — nothing comparative exists without it | |
| 4 | Binder — the owner's stated emotional centre, and it works on today's data | |
| 5+ | Screener, Charts, Home feed, demo mode (D-064), marketing and landing **last** | |

**Two orderings that were argued and rejected:**
- *Landing page first.* It has zero technical risk and zero data dependency, so it can be built at
  any point — including last. It is the frame around a story that cannot be told yet, so building it
  early means building it twice.
- *Depth-first by screen.* The data does the sequencing; a screen-at-a-time order would repeatedly
  hit the same 2027 wall in five different places.

**Phase 1 shipped 2026-08-12.** Spec at `docs/superpowers/specs/2026-08-12-price-read-layer-design.md`,
plan at `docs/superpowers/plans/2026-08-12-price-read-layer.md`, merged to `main` as `1874f32..c7a336a`.
63 Domain tests plus 6 integration tests against the Pi. It added no tables and no migrations.

**What Phase 2 inherits:** `ICardPriceReader.GetAsync(cardId)` returns a `CardPriceSnapshot` — six
tiers always, each with its full series, a `TierPrice` and a `TierChange`, plus the card's
`LastVisitedAt`. Absence is in the type, so the page cannot accidentally render a hole as a number.
**Nothing is registered in DI yet** — deliberately, because no consumer existed to prove the wiring.

**Three things Phase 1 turned up that outlive it:** D-076 (the express contract was wrong in three
places), D-078 (closed months revise, contradicting `DATA_MODEL.md` twice over), and the fact that the
crawler's schema drifts under us — `SchemaDriftTests` caught `AddCardNearMissAt` the morning it landed.

**Still open, and neither blocks Phase 2:** `SalesChange.MinimumSalesPerWindow` is 3 on no evidence and
cannot be tuned until real windows exist (~Nov 2026); and nobody has measured how far
PriceCharting's monthly average sits from the mean of our own captured sales.

**Phase 2 shipped 2026-08-13.** Spec at `docs/superpowers/specs/2026-08-12-card-page-design.md`, plan
at `docs/superpowers/plans/2026-08-12-card-page.md`, built as `fccbcc1..7529f0f` on
`phase2-card-page`. 275 tests green — 96 Domain, 17 Application, 3 Infrastructure, 23 integration
against the Pi's Postgres, 115 Web (bUnit), 21 Api — plus the CI-severity format gate. Verified on
the Pi end to end: `/healthz/data` returns non-zero counts for all four scraper tables the phase
reads; Charizard 630417's two live chips (`MACD +`, `clean trend R² .91`) match an independent
recomputation of all seven signals from latest-per-cell SQL (the five silent signals are silent for
the right reasons — ROC +12.7% sits under the ±15% band, the PSA 10/Grade 9 spread is *widening*
9.19x from 5.91x, drawdown is 0% at peak); the delisted chip, thin-card legend degrades, true-zero
ledger copy, and the not-found page all render their authored copy verbatim; and one page-load-driven
express visit is attributed in the worker's own journal (`Express visit
/game/pokemon-darkness-ablaze/corviknight-pre-release-156`, committed 15:36:19 CDT).

**Four things the verification turned up:**
- **The hosted publish was invisibly broken.** The API's publish copies the Web project's
  `index.html` raw, still carrying the `#[.{fingerprint}]` placeholder only a Blazor WASM project's
  own publish substitutes — a browser strips the `#` fragment, requests a file that doesn't exist,
  and the deployed app never boots. Dev serving substitutes it, so no test and no local run could
  see it; Task 21's bundle check caught it. Fixed in `ops/publish.sh` (`4ede994`): the client
  publishes through its own pipeline and its processed `wwwroot` replaces the raw copy, with a
  loud check so it cannot regress silently.
- **The corpus is effectively fully crawled.** Exactly one active card remains never-visited
  (13971735, Vaporeon [Reverse Holo] [Poke Ball]) — and it is unvisitable: the source 302s its URL
  to a search page, and the crawler has quarantined it after 12 straight failures. The
  "express visit builds a virgin page live" demo path therefore no longer exists in this corpus;
  what exists — and was verified — is the honest amber `– never visited` badge over the all-absent
  dress. This also settles D-071's open sparsity question: ~113 rows/card is the site's true
  publication density, not uncrawled backlog.
- **`cardstock_tester`'s password on the Pi had drifted** from `ops/credentials.local` (auth
  failures; `cardstock_app` unaffected). Reset to the recorded value with owner approval
  2026-08-13; the integration suite went 23/23 immediately after.
- **Two production nits, owner's call, changed nothing:** EF Core logs full SQL into the journal at
  Information level (`Logging:LogLevel` has no `Microsoft.EntityFrameworkCore` override), and a
  renderer freeze was observed *only* inside a browser-automation extension's tab — the app is
  healthy in plain and headless Chrome, so it is noted here purely so nobody re-debugs the product
  for it.

*Original framing, kept because it was the whole point:* Phase 1 is where the correctness risk
concentrates — absence means "unchanged", not "missing", and "latest" is `max(observed_at)` per key
rather than the newest month.

**Amended 2026-08-11 — Phase 1 also carries a 30-day sales read.** Owner: the tier strip's 30-day
change *"should not be based on the monthly scrape data. It should be based on the past thirty days of
sales."* Confirmed against `Cardstock Card.dc.html:398`, whose tooltip names no source at all, so
nothing contradicted it; the chart's tooltip at `:132` independently supports the same split —
*"the point firms up as the month's sales land."*

So a single strip cell draws from **two tables**: price from `price_months`, change from `sales`.
Splitting them across phases would ship a tier strip that cannot fill its own cells, so the sales read
comes with Phase 1. It is one indexed lookup and the index already exists (`sales(card_id, sold_on)`,
D-057).

**The definition, settled 2026-08-11.** It is a **percent change between two 30-day windows** —
mean sale price over the last 30 days against mean sale price over the 30 days before that — not a
single-window average. Owner chose this explicitly, and `Card.dc.html:398`'s *"{chg} **over** 30
days"* reads as a change rather than a level, which is where he had drawn it from.

```sql
-- both windows in one pass; index sales(card_id, sold_on) already exists (D-057)
SELECT grade_tier,
       avg(price_cents) FILTER (WHERE sold_on >= current_date - 30) AS recent,
       count(*)         FILTER (WHERE sold_on >= current_date - 30) AS recent_n,
       avg(price_cents) FILTER (WHERE sold_on <  current_date - 30) AS prior,
       count(*)         FILTER (WHERE sold_on <  current_date - 30) AS prior_n
FROM sales
WHERE card_id = :id AND sold_on >= current_date - 60
GROUP BY grade_tier;
```

**The windows are hardcoded and never widen.** Owner: *"the query should build in that time period…
eventually that query will work for only the latest thirty days."* Today it returns a handful of rows;
in a year it returns a full window; the query is identical. No early-days special case to unpick later
— which was the whole point of *"that way in 30 days we won't have wasted code."*

**It returns both counts alongside the change**, and the counts decide whether a number renders at all.

**Insufficient data renders a dash. No countdown, no unlock date, no `LOCKED` state.** Settled
2026-08-11 after I proposed a countdown and the owner rejected it: *"I don't want logic sticking around
forever that calculates when the data will be full, because it will be full in six weeks."* He is right,
and the reasoning generalises further than the ramp-up:

> **"Not enough sales" never expires.** A quiet card will not have two sales in 30 days in 2028 either.
> So the insufficient case needs handling permanently regardless — it is only the *countdown* that has
> a shelf life. Dropping it removes code without removing honesty.

- Below the threshold → **dash**, in the change slot. Identical rule today and in five years.
- At or above → the number.
- The threshold is **one named constant**, so tuning it is a value change, not a rewrite.
- The tooltip is **static** — *"Not enough sales in the last 60 days to compute a change"* — true now and
  permanently, with no date arithmetic in it.

This deliberately overrides the `LOCKED` treatment `card.md` §4.11 describes for this cell (*"countdown
copy… unlocks ~Mar 2027"*). That pattern still stands wherever an unlock genuinely is a one-time event;
it is wrong here, because this cell's insufficiency is a permanent possibility rather than a phase.

**For the record, since it no longer appears in the UI:** two 30-day windows need 60 countable days and
D-033's floor starts at 2026-09-01, so the earliest any card can produce a change is ~1 Nov 2026. Every
strip cell will dash until then and start filling itself with no deploy.

**Price staleness, settled the same day and measured before deciding.** A price renders if its newest
month is **the current month or the one just closed**; two or more months behind, it dashes.

*Receipt (500-card sample, 1,802 series, run 2026-08-11):* 81.3% of series are current-month, 15.2% are
one month behind, **3.5% are two or more**, tailing to 53 months. The 15% are not stale — early in a
month PriceCharting has not yet posted an average for every tier — so a strict current-month-only rule
would have dashed 19% of series, most of them healthy. The chosen rule keeps 96.5% showing a real price
and dashes only genuinely dead grades.

**The chart's current-month point stays on `price_months`.** Owner, 2026-08-11: *"use price months…
that way the data comes from all the same lineage."* So the dashed final segment and hollow dot draw
**PriceCharting's own month-to-date average** — their number firming up as their sales land, which is
what `Card.dc.html:132`'s tooltip describes — and **not** a figure we compute from our own `sales`
rows. Verified live: Charizard #24 holds two rows for `2026-08-01` (`$28.61`, `$25.00`), the current
month revising between visits exactly as documented.

The alternative was rejected on data grounds as well as lineage: our sales record is ~2 weeks old and
ragged (D-001), so computing that point ourselves would swap a well-sourced value for a thin one and
put the last point of nearly every chart into `LOW DATA`.

**So the Card page's price surfaces draw from both tables, on a clean split:** every *price* comes from
`price_months`, every *change* comes from `sales`. Worth stating because it means a single strip cell
shows a price and a change computed from **different populations** — PriceCharting's undisclosed
average versus our own captured sales.

**Which sales feed which cell.** Owner, 2026-08-11: *"PSA ten here means PSA ten. Grade nine means all
grade nines."* The pooling is already done upstream — the source pools graders below 10 and splits only
at 10 (ADR-0005) — so `Grade 9` *is* every grade nine and there is nothing for CardStock to combine.
**6 of `sales.grade_tier`'s 19 labels map; 13 feed nothing.**

| `sales.grade_tier` | Strip cell |
|---|---|
| `Ungraded` | **rendered `Raw`** — see the label rule below |
| `Grade 7` · `Grade 8` · `Grade 9` · `Grade 9.5` · `PSA 10` | its own, same label |
| `Grade 1`–`Grade 6` | none — `price_months` has no series below 7 (D-012) |
| `CGC 10` · `CGC 10 Prist.` · `BGS 10` · `BGS 10 Black` · `SGC 10` · `TAG 10` · `ACE 10` | none — one grade-10 tier exists and it is `Psa10` |
| anything unrecognised | none |

**It must be an allow-list, not a deny-list**, and that is not a style preference.
`GradeTierVocabulary.cs:16–18` states the vocabulary **grows**: *"Graders get added over time — TAG and
ACE are recent — so this list grows."* A deny-list would silently fold a future eleventh grader's 10
into the PSA 10 cell — precisely the substitution D-022 and D-057 both rejected — and it would do it
without an error, in the cell users look at first. Unknown labels fall through to "feeds no cell".

The 13 unmapped labels still render in the sales ledger. They simply have no price beside them to
change against.

**`Raw` is the display label; `Ungraded` is the stored value.** Verified 2026-08-11 against the
prototype, which is authoritative here: `Cardstock Card.dc.html:322`'s `BUCKETS` opens with `'Raw'`,
the strip allow-list at `:395` names `'Raw'`, and the ledger chip at `:453` is `'Raw'` — while both
`GradeTierVocabulary.cs:21` and `PriceTier.cs:14` store `Ungraded`. The two vocabularies are otherwise
identical, same 19 labels, same spellings down to `CGC 10 Prist.`. `card.md` C-2 already recorded this
for the chart legend, calling it *"consistent with the later app-wide rename"*; it holds for the strip
and the ledger chips as well. **Translate at the render boundary and nowhere else** — no query, enum, or
mapping key may ever spell it `Raw`.

**Open — how far apart are those two numbers?** Owner: *"I wonder how off those two numbers end up
being… I doubt they're spot on."* This is answerable now: compare `price_months` for the current month
against the mean of our own `sales` for the same card, month and mapped tier. It also bears on
`about-data.md:238`, which currently records as an **assumption** that PriceCharting's monthly average
is built from realized sales at all. Not blocking Phase 1; worth running before any copy claims the two
agree.

**On "why not just finish one page at a time"** (owner asked, 2026-08-11, and the answer belongs
here rather than in a conversation): the Card page **cannot be finished**. Its sales ledger and census
panel need post-seam data that does not exist until 2027 (D-001), so a page-at-a-time order stalls on
the very first page. What *can* be finished is every price surface on it, which is what Phases 1–2 are.
"Phases" are this project's build order, not a superpowers mechanism — superpowers is
brainstorm → plan → execute, applied to whatever chunk it is pointed at.

---

### D-074 — Cost basis is FIFO. This unblocks the Binder
Owner, 2026-08-11: *"First in, first out."*

Forced by D-067 — once holdings are derived from transactions rather than stored, there is no
running average to fall back on, so a SELL has to name which purchase lot it consumed. `binder.md`
§7.4 was promoted to blocking for exactly this reason and is now resolved.

**The rule:** a SELL consumes open lots oldest-first. Realized P&L is proceeds minus the FIFO cost
of the units sold. Remaining lots keep their own purchase dates and prices.

**Why FIFO over average cost**, and it is not a matter of taste: `binder.md` §3.9 defines the
**Avg hold** tile as *mean(sell date − buy date) over closed positions*. Pooling two purchases into
one average destroys the buy date, so that tile becomes undefined. FIFO names the consumed lot, so
holding period falls out for free. Average cost would have silently broken a designed stat.

**Why not specific identification:** it requires asking "which lot?" on every sell — friction on the
core interaction, and no prototype supports it. Additive later if tax-lot optimisation ever matters;
the transactions are all retained either way.

**What stays true:** the holdings column header "Avg cost" (`binder.md:194`) remains honest. It is
the average of the lots **still held**, which is a display question independent of the accounting
method.

**Two consequences to carry into the build:**
- **Corrections cascade for free.** Editing a historical BUY changes which lots exist and therefore
  the FIFO matching downstream of it. Because holdings are derived (D-067), this recomputes rather
  than needing repair — the reason that shape was chosen.
- **Overselling now has a definite meaning.** `binder.md` §4.9 rule 3 records that SELL-edit accepts
  any quantity with no cap against holdings. Under FIFO that is a sale with no lots left to consume,
  so it is not merely untidy — it is unrepresentable, and the write path must reject it.

---

### D-071 — ✅ ADR-0001 is built and running. The boundary is enforced by Postgres, not convention
Implemented 2026-08-11 on branch `first-slice`. Every claim below was checked by direct query after
the fact, not inferred from the code.

**The write boundary, tested as `cardstock_app` against the live database:**

| Statement | Result |
|---|---|
| `SELECT` on `cards` / `sets` / `price_months` | **91,570 · 788 · 10,352,706 rows** |
| `INSERT INTO public.sets` | `ERROR: permission denied for table sets` |
| `UPDATE public.cards` | `ERROR: permission denied for table cards` |
| `DELETE FROM public.sales` | `ERROR: permission denied for table sales` |
| `CREATE TABLE public.…` | `ERROR: permission denied for schema public` |
| `CREATE TABLE cardstock.…` | `ERROR: permission denied for schema cardstock` |
| `INSERT`/`DELETE` in `cardstock.users` | succeeds |

The last two rows matter together: the runtime role holds full DML in its own schema and **no DDL
anywhere**, so it cannot migrate even the tables it owns.

**`ToView` proved itself.** The scaffolded `20260811203346_InitialCreate` contains exactly two
`CreateTable` calls, both `schema: "cardstock"`, and the string `"public"` appears nowhere in it —
with all five crawler tables present in the model. `MigrationContentTests` now asserts this
permanently.

**The history tables did not collide.** `to_regclass('cardstock.__cardstock_migrations_history')`
resolves; `to_regclass('public.__cardstock_migrations_history')` is null. This is the override
earning its keep — `HasDefaultSchema` alone would have put CardStock's rows in the crawler's table.

**The crawler was unaffected**, still visiting cards and writing rows throughout. Connection usage
peaked at 5 of 100.

**First real data figures for the project**, replacing every estimate used in design: **91,570
cards**, **788 sets**, **10,352,706 price-month rows**, database 2.3 GB, disk 210 GB free.

---

### D-072 — `ALTER DEFAULT PRIVILEGES` merges into the crawler's existing ACL row. Now Verified
Carried as **Inferred** when ADR-0001 was written; confirmed by query 2026-08-11.

```
SELECT defaclrole::regrole, defaclnamespace::regnamespace, defaclacl FROM pg_default_acl;
 pokemon_owner | public | {pokemon_app=ar/pokemon_owner,cardstock_app=r/pokemon_owner}
```

CardStock's future-table read grant lives **inside `pokemon_owner`'s own row**, not a separate one.

**Consequence, and it is a real trap:** `DROP OWNED BY pokemon_owner`, or rebuilding the Pi from the
crawler's `postgres-setup.sql`, silently removes CardStock's access to tables created by future
crawler migrations. The symptom appears weeks later as `permission denied for table X` on a table
nobody remembers adding. The verification query is recorded in `ops/cardstock-postgres-setup.sql`.

---

### D-073 — Test databases live on the Pi; there is no local Postgres
Owner, 2026-08-11: *"we're not installing Postgres locally… that's where database development is
going to be."*

Integration tests build and drop a `cardstock_test_<guid>` database per test on the Pi over the LAN,
as `cardstock_tester`. Verified this works without touching the Pi's configuration: `pg_hba.conf`
already carries `host all all 192.168.0.0/24 scram-sha-256` and `listen_addresses = '*'`.

**Consequence for ordering:** `ops/cardstock-postgres-setup.sql` must run *before* any integration
test can execute, because it creates `cardstock_tester`. CI is unaffected — it uses its own
`postgres:15` service container.

**Worth knowing:** the `ToView` write guarantee is proven in a database owned by `cardstock_tester`,
which holds **none** of the production grants. A grant-only defence would pass that test for the
wrong reason.

---

### D-065 — 🔒 Schema separation and migration ownership → **ADR-0001**
Owner, 2026-08-11. One `pokemon` database; the scraper keeps `public`; CardStock owns a `cardstock`
schema under `cardstock_owner`. Scraper tables are mapped in EF as **views**, never tables. Foreign
keys into `public.cards` are real and hand-written. Each repo migrates only its own schema, by hand
from a dev machine — nothing auto-migrates, mirroring the scraper.

Full reasoning, alternatives, and consequences: **`docs/adr/0001-schema-separation-and-migration-ownership.md`**.

**The receipt that decided it against a separate database** — every scraper grant is scoped
`IN SCHEMA public` (`PokemonInvestBatch/ops/postgres-setup.sql:32,34,36`, 45-line file read in
full), so a schema split already delivers the protection a separate database was supposed to buy,
without losing single-statement joins.

**The receipt that decided `ToView` over `ExcludeFromMigrations`** — scaffolded migrations read
directly 2026-08-11 under EF Core 10.0.10 / Npgsql 10.0.3: the `ExcludeFromMigrations` variant emits
`fk_holdings_cards_card_id → principalSchema: "public"`; the `ToView` variant emits one
`CreateIndex` and nothing else; omitting the mapping emits `CreateTable(schema: "public")` in `Up()`
and **`DropTable(schema: "public")` in `Down()`**.

---

### D-066 — 🔒 Identity: email + password in an HttpOnly cookie backed by a session row → **ADR-0002**
Owner, 2026-08-11. ASP.NET Core cookie authentication (`HttpOnly`, `Secure`, `SameSite=Lax`), with
the session held in `cardstock.sessions` via `ITicketStore` rather than as claims inside the cookie.
Password hashing via `PasswordHasher<T>`; policy stays exactly as designed — minimum 12, no
complexity rule (`docs/screens/account.md:139`). Verification and reset are single-use hashed tokens
with expiries.

Full reasoning: **`docs/adr/0002-identity-is-a-cookie-backed-by-a-session-row.md`**.

**Why not a JWT in `localStorage`:** D-029's XSS surface (`sales.title` stored raw) makes any
JS-readable credential exfiltrable, and a self-contained token cannot be revoked before expiry —
which contradicts deletion actually deleting (D-069).

**Owner accepted the dependency this creates:** transactional email for verification, reset, and
email change. Verification stays in v1 rather than being deferred. This is the project's first
dependency outside the Pi.

---

### D-067 — Binder data shape: one binder, corrections rather than edits, holdings derived
Owner, 2026-08-11, three rulings taken together:

- **One binder per user.** No `binders` table; `user_id` sits directly on transactions.
- **Transactions are edited in the UI and stored as corrections.** This was already the designed
  behaviour, not a new decision — `binder.md:292` ("the edit button is always visible on every
  row"), `:296` ("Every edit is stored as a correction under the hood"), `:507`. The dormant VOID
  render path found in the Class F audit is what this wires up.
- **Holdings are derived from transactions, not stored.** No `holdings` table. Rationale: a stored
  total can drift from the transactions behind it with no way to tell which is right, and the
  correction model forces a recompute on every edit anyway.

**Consequence:** the D-065 foreign key attaches to `transactions.card_id`, at the point data enters.

**Spec updated** per the maintenance rule: `docs/screens/binder.md` §8.

---

### D-068 — Binder gallery rearrange: proposed, designed, rejected
Raised by the owner 2026-08-11 as a missing feature — drag to arrange the gallery view and save the
arrangement. Explored in full, then **withdrawn by the owner the same day**: *"Let's scrap this
feature. I don't love any of the solutions."*

Recorded so it is not rediscovered and re-argued. What the exploration established, should it
return:

- The gallery already exists as a view toggle (`binder.md:95`); what was new was *independent,
  persisted ordering*.
- It collides with a documented invariant — `binder.md:852`, "Holdings and gallery always show the
  same rows in the same order." **That invariant stands, untouched.**
- The table has six sortable columns (`binder.md:568`), so the unresolved question was what
  dragging means while sorted by value. The proposal was a seventh "custom" order mode with drag
  enabled only inside it; the owner did not like it, nor the physical-binder slots alternative.
- Owner's refinement before withdrawing: both table and gallery should share one order and both be
  reorderable.

**Nothing was written.** `git status` clean at the time of withdrawal; `binder.md:852` unmodified.

---

### D-069 — Backups deferred by the owner, which supersedes D-053's premise
Owner, 2026-08-11: *"Don't worry about the backup. When this is all completed, I'll worry about
backup and load balancing, etcetera. Keep it simple, stupid."*

Raised as a concern first — D-017's facts are unchanged and `sales`/`populations` remain
unrebuildable — and the owner ruled. Recorded as a decision, not a gap.

**Consequence, and it is not merely bookkeeping:** D-053 made account deletion a bounded 30-day
window *because* off-box backups were treated as mandatory, and a deleted row survives in a dump
until rotation. With no dumps, "immediately and permanently" becomes keepable — which is what
`Cardstock Profile.dc.html:181` already promises. **D-053 is superseded while this holds.**

**⚠️ The trap to avoid later:** if backups are added without revisiting this, the privacy promise
becomes false the day the first dump is written, silently. Whoever adds backups must re-open D-053
in the same change.

---

### D-070 — The Pi's environment, verified by direct query
Run 2026-08-11 over ssh, `sudo -u postgres psql -d pokemon`:

| Fact | Value | Consequence |
|---|---|---|
| `server_version` | **15.18** (Debian 12) | Pin CI to `postgres:15`, matching the scraper's. That repo's `README.md:289` claim of "16+" is **wrong** |
| `max_connections` | **100** (default) | Three .NET processes at Npgsql's default pool of 100 each would request 300. Both CardStock roles carry an explicit `CONNECTION LIMIT`, and every connection string sets `Maximum Pool Size` |
| Disk | **210 GB free of 235 GB**, database 2.3 GB | Storage is a non-issue; the metric-snapshot volume concern is dropped |

---

### D-005 — Blazor is the frontend. Not up for debate
Supporting processes are .NET console/worker apps. The existing Postgres in `../PokemonInvestBatch` is the data source.

**Reason:** this is a .NET portfolio piece; demonstrating Blazor is a goal of the project, not an implementation detail. Owner, 2026-08-10.

**Notes:** Owner also relaxed the rest — "I know the handoff doc gives you very explicit instructions about architecture and design choices, but that was probably me taking it too far." Everything in `HANDOFF.md` other than Blazor + Postgres is open. This explicitly includes the spec's "no HTTP API for the first-party UI" rule.

---

### D-006 — Build now. Do not wait for data to accumulate
Series get labelled honestly; indicators unlock as depth accrues.

**Reason:** Owner, 2026-08-10 — "I realize data is going to have to build up for this product to get even better. But that's no reason not to build the product today."

**Notes:** An agent argued that computing RSI / Bollinger / drawdown / z-score over `price_months.price_cents` is mechanically distorted, because that column is PriceCharting's monthly *average*, not a close. The premise is accepted; the conclusion (don't build) is rejected. It converts to a **labelling requirement**: the UI must not call these "monthly closes." The sufficiency framework already in the design is the correct mechanism for the rest.

---

### D-011 — CardStock ships publicly with open signup
Owner, 2026-08-10, answering the deployment question during brainstorming. Resolves the largest open scope question.

**What this turns on:** auth is required and non-optional; a tunnel (Cloudflare or equivalent) rather than port-forwarding a residential connection; per-user rate limiting in front of `express-visit` (D-024 — the worker's guardrails bound load on the *source site*, nothing bounds how often a user can trigger one); card-image **licensing becomes a live question**, because serving is distribution where storing was not (D-010); and the accuracy of public claims on About Data and Legal becomes a real exposure rather than a stylistic concern.

**And it makes render mode the highest-stakes decision in the build** — concurrency is precisely where Interactive Server on a residential Pi is weakest. See D-013, now materially sharpened by D-035.

---

### D-042 — Harvest from `PROJECT_LOG.md` before it is retired
All lines read directly 2026-08-10. The file is Tier 3 (D-040) and slated for deletion; these items exist nowhere else and are extracted so they survive it.

**`:241` — the API was deferred, never forbidden.** *"**API design: OUT OF SCOPE** for this conversation. **Final UI drives API decisions.** ✅ user ruling."* This is a better source than the spec parenthetical S-002 rested on. The UI is now designed, so **D-014 is not overturning a ruling — it is the ruling coming due on its own schedule.**

**`:218` — static data required, non-scraped, one-time.** (1) A **set metadata table** — release date + era/series for ~303 sets. (2) A **character tag table** — card → Pokémon, derived from `cards.name` × a Pokédex species list. Both are prerequisites for Browse's era shelves, the Set and Character pages, and the Screener's Era and Character filters. Neither exists in the scraper (D-004's eight tables). These are CardStock's own tables to build and curate.

**`:235` — 🚩 conflicts with D-026.** *"Dominant-color extraction from card art → character/set page header accents. **NEW derived column on cards** (computed from stored images)."* A new column on `cards` means CardStock writing to the **scraper's** table — precisely the case D-026 left open and D-037 proposed to make structurally impossible via a `SELECT`-only role. Resolvable by putting the derived colour in a CardStock-owned table keyed by card id, but it needs a ruling.

**`:106` — Binder is strictly private.** *"No social layer, no public profiles, no leaderboards, no shared/forkable strategies. ✅ decided."* Matters more now that D-011 made the app publicly signup-able.

**`:242` — charting locked.** TradingView Lightweight Charts via JS interop, *"Blazor wrapper component = portfolio centerpiece."* Series markers give the trigger triangles natively; v5 panes carry MACD/RSI sub-charts. Screener grid is QuickGrid + virtualization.

**`:137` — the "show anyway" override burns in.** Overriding a Tier-2 lock renders a **persistent** low-confidence badge into the chart region, deliberately so it **survives screenshots**. An anti-misuse detail that would be easy to lose and hard to reinvent.

**`:201` — undecided, still open.** A possible "data events" feed (restatements, cap incidents) as the honest analog to a news feed. Marked ⬜ undecided there; still undecided.

**Superseded, do not harvest:** `:204–210`'s seven critical user flows include *"Get alerted → signals feed at login + email when away."* Alerts were cut wholesale (`HANDOFF.md` §4), so flow 6 is dead as written.

---

### D-040 — Document authority: the mockups and the scraper codebase are absolute truth
Owner, 2026-08-10: *"The mockups and Pokemon investment scraper codebase are the absolute truth."* Recorded in full in `CLAUDE.md` under "Document authority."

Tier 1 — `CardStock Mockup/*.dc.html` and `../PokemonInvestBatch/`. Tier 2 — `HANDOFF.md`, `DESIGN_NOTES.md`, `DISPLAY_VOCABULARY.md`, derived but current. Tier 3 — `CARDSTOCK_UI_SPEC_v1.md`, `PROJECT_LOG.md`, `BRAND_BRIEF.md`, historical. `DECISIONS.md` overrides all three where it records an owner decision.

**This resolves method, not just precedence.** Several open contradictions become settleable by opening the HTML instead of arguing between documents — D-009 (the "Apr '25 liquidity seam" in `DESIGN_NOTES.md:35`) is exactly that shape: check what the prototype actually draws.

---

### D-041 — No candlesticks, no news. Confirmed independently of the stale spec
Owner, 2026-08-10: *"We can't do candlesticks with our data type. and we're not doing news."*

**Why this needed confirming:** the Tier-1 permanently-impossible list lived at `CARDSTOCK_UI_SPEC_v1.md:57` and `PROJECT_LOG.md:136` — both now Tier 3 under D-040, and the owner had flagged the spec section as possibly deleted. The exclusions are now owner-confirmed and no longer depend on either document.

**Candlesticks:** structurally impossible, not merely unimplemented. `price_months.price_cents` is a single monthly value (D-003 — six tiers, one integer each); OHLC needs four points per period and intraday sequencing that does not exist at the source (`DATA_MODEL.md:481` — historical sales volume and pre-observation history are "unavailable from source, permanently").

**News:** scope decision, no data source, not pursued.

**Applies the Tier-1 rule:** omitted from the app entirely, not rendered as disabled controls. `PROJECT_LOG.md:136` carried the rationale worth keeping — *"a disabled control that never enables is a broken promise that erodes trust in every other disabled control."* Harvested here so it survives that file.

---

### D-038 — v1 ships the full UI with locks visible
Owner, 2026-08-10. Every screen ships. Locked rows render with real countdowns and progress computed against the 2026-09-01 floor (D-033). Nothing is hidden because it isn't ready.

**Rationale:** a terminal that states plainly "these indicators are dark, and here is exactly when each unlocks" demonstrates rigor that most portfolio projects fake. The locks are the story, not an apology for one. This is the same posture as the two honesty rules the whole design is built on.

**Consequences:**
- **The sufficiency engine is on the critical path, not deferrable.** Every screen depends on rendering locks correctly, so sub-project D moves up. It is small now that D-033 replaced per-card derivation with one floor — but it cannot be skipped.
- **D-032's recalibration must land before Charts or Screener ship.** Locked rows are now a shipping feature, so wrong countdowns would be a user-visible lie in the product's most load-bearing claim.
- **`Cardstock About Data.dc.html` must carry the floor and its reason** (D-033). With locks everywhere, the explanation page stops being optional.
- **The Card page carries the first impression.** It is the surface that works completely today — identity, six-tier strip, price history to Dec 2020, sales ledger, census bars, real images (D-010). Success criterion #1 ("a hiring manager clicks a link and is impressed in 90 seconds") should be designed around it and the landing page, not around Charts.

**Unverified:** the "32 indicator rows down to roughly 12, 27 screener filters down to about 14" figures are survey-agent estimates from reading the mockups. Real counts should be established before the About Data copy quotes any number.

---

### D-036 — The Blazor app runs on the same Pi as the scraper
Owner, 2026-08-10. Resolves the hosting half of D-016.

**Consequences, all favourable:** the loopback intake API (D-024) works exactly as designed with no change to `PokemonInvestBatch`; Postgres never has to listen on a network interface, so it stays on loopback or a Unix socket and is unreachable from outside **by construction**; no `pg_hba.conf` opening, no LAN-exposed database.

**The genuine cost is blast radius.** A compromised web app is adjacent to Postgres and the crawler. Accepted knowingly — owner asked directly whether it could be locked down "99%" and the answer is yes, provided the items in D-037 are done.

**Note the inversion:** splitting onto two Pis would have been *worse* on the axis of concern, because it forces Postgres onto the network and forces the scraper's intake API to abandon the bind-address trust model that ADR-0006 rests on.

---

### D-037 — Security posture for a public, single-box deployment
Follows from D-011 (public, open signup) and D-036 (same box). Not yet designed — this is the checklist the design must satisfy.

**Network exposure — two viable routes, owner has the hardware for either.** Updated 2026-08-10: the home network runs a **static IP on business-grade hardware capable of VLANs and firewall rules.** That removes the assumption behind my original "tunnel is non-negotiable" framing.
- **Segmented direct exposure** — Pi on an isolated DMZ VLAN, inbound 443 only, **no lateral route to the rest of the LAN**. This directly addresses D-036's blast-radius cost: a compromised web app is still adjacent to Postgres and the crawler on that Pi, but it is contained to that Pi rather than sitting inside the home network. Legitimate with this hardware.
- **Cloudflare Tunnel or equivalent** — outbound-only, no inbound holes at all. Still worth weighing even with good hardware, because it also brings DDoS absorption, a WAF, and origin-IP concealment that a home firewall does not.
- Not yet decided. Both are defensible; the VLAN route keeps everything under the owner's control, the tunnel route offloads the classes of attack a home firewall handles worst.
- **Dedicated `cardstock_app` Postgres role** — `SELECT`-only on the scraper's eight tables, full DML on CardStock's own, no DDL. This enforces D-026 **at the database** rather than by convention, making the app structurally incapable of writing scraper data. Migrations use a separate role, at deploy only.
- **An abuse-shape check in front of `express-visit`** — **not** a throttle on usage. See D-062: legitimate browsing is self-limiting via the 24h staleness gate, and browsing-driven express visits are demand-weighted crawling worth having. The limit exists only to catch scripted enumeration, and should be generous enough that a person browsing hard never meets it. As of ADR-0008 the worker has no spacing floor, so this is the **only** remaining bound.
- **systemd hardening on the web unit** — `NoNewPrivileges`, `ProtectSystem=strict`, `PrivateTmp`, dedicated user, and `MemoryMax` so the web tier cannot starve the crawler.

**Application-level, where the real risk lives:**
- `sales.title` XSS — D-029. Razor `@` encodes by default; ban `MarkupString` on that path and cover it with a test.
- IDOR on binder/watchlist/saved-screen rows — every query scoped by `user_id` (the multi-tenant schema from D-034).

**Ordering note — do this first:** backups (D-017) outrank all of the above. Every other item protects against recoverable damage; `sales` and `populations` cannot be rebuilt from any source and **no backup exists today**. Hardening a box whose unique data is unbacked is the wrong order.

**Unverified, needs checking before launch:** `Cardstock Legal.dc.html` reportedly promises "no third-party trackers." If the existing New Relic OTLP stack touches the web tier, that promise is false on day one. Either keep New Relic off the web tier or amend the copy. I have not read that file.

---

### D-034 — The auth model has been decided three different ways; today's answer settles it
**Receipts, read directly 2026-08-10:**
- `uploads/PROJECT_LOG.md:105` — "**Multi-user**, open **free signup**, **email + password** auth. ✅ decided"
- `uploads/PROJECT_LOG.md:254` — "**Open public signup REVERSED → invite-only.** Registration behind an invite code (friends only); **no verification emails**; minimal password reset… (Supersedes branch-1b open-signup decision)"
- `HANDOFF.md` §4, Added since the spec — "Email + password auth with transactional email (verify / reset / email-change only)"

So the log reversed open signup to invite-only, then the prototypes were built with open account creation *and* verification email — contradicting the reversal.

**Resolution:** D-011 supersedes `PROJECT_LOG.md:254`. Open signup stands, which means **the built prototypes are already correct** and the invite-only entry is the outlier. Nothing in `Cardstock Account.dc.html` needs rework on this axis.

**Carried forward from `:254` regardless:** the schema stays multi-tenant from day one — `user_id` on every user-facing table — so commercialisation is a config change, not a rewrite. That rationale is unaffected by the signup decision.

---

### D-035 — 🚩 The spec's stack choice rests on an assumption D-011 just voided
`uploads/CARDSTOCK_UI_SPEC_v1.md:47`, read directly 2026-08-10: *"**Hosting:** Raspberry Pi fleet (16 GB, quad-core), **~1 concurrent user expected.**"*

Line 46 chooses **Interactive Server** rendering in the same breath. That choice was sound *for one user*. Interactive Server holds a stateful SignalR circuit and a server-side render tree per connection and round-trips every interaction — so its cost scales with concurrent visitors, on a box already running Postgres and a continuous crawler.

With D-011 (public, open signup), "~1 concurrent user expected" is no longer an assumption anyone can rely on. **The premise under the stack decision is void, so the decision has to be re-made rather than inherited.** D-005 fixes Blazor; it does not fix the render mode.

Note also the word **"fleet"** — the spec implies more than one Pi. Whether the web app shares a box with the scraper is unresolved and determines whether the loopback intake API (D-024) is reachable at all.

---

### D-033 — Sufficiency floor: no post-seam metric counts observations before 2026-09-01
Owner, 2026-08-10. **Resolves D-032.**

**It is a floor, not a claim.** This is *not* an assertion about when data began — that is per-card and ragged (D-001). It is a deliberate, disclosed cutoff: the collector was still being stabilised through August 2026, so earlier observations are discarded rather than trusted.

**Why a floor rather than the true per-card dates:** a single global constant is only safe if it sits *later* than every card's real first visit, and then it errs toward **LOCKED**. Understating maturity is always safe; overstating it is the one failure this product's brand cannot survive — which is exactly what the old hard-coded constants did (D-032).

**Why 2026-09-01:** the owner expects the scraper's bugs resolved by the end of August 2026, so September 1 is the first day of data he is willing to stand behind. An earlier proposal of "August 2026" was rejected as inconsistent with that same reasoning — if August is the stabilisation month, August data is the suspect data.

**Consequences:**
- `DISPLAY_VOCABULARY.md` carries **one anchor date plus denominators**. Numerators are arithmetic against today. No authored ratios, ever again.
- **D-015 drops out of first position.** A per-card sufficiency projection is no longer a prerequisite for drawing a locked row. The analytics tier is still needed for Screener query performance across 91k cards, but that is a different justification.
- **It does not shorten any wait.** 24 post-seam months → ~**Sept 2028**. 12 months of census → ~**Sept 2027**. This simplifies how the constraint is expressed, not how old the data is. The v1 scope question (D-011, and whether to ship with liquidity and supply indicators dark) is untouched.
- `Cardstock About Data.dc.html` should carry the floor **and its reason**. "We discarded our own early data because we didn't trust it" is the same posture as the rest of the design and is a stronger story than an unexplained date.

**⚠ Must verify before implementing:** the floor is only safe if it is later than every card's first visit. One query settles it — whether any card still has a null `last_visited_at`, or the `max()` of per-card first observation. The survey agent's "~12.4-day corpus lap" is **unverified**; I found nothing supporting it in `DATA_MODEL.md` or `ops/README.md`. If the corpus laps in under a month, 2026-09-01 is comfortably clear.

---

### D-007 — Verify everything; cost is not a consideration
Owner, 2026-08-10: "whether it's clarifying or debugging, is verify everything. I don't care about extra tokens spent or the extra time or the redundancy."

Encoded as the first section of `CLAUDE.md`.

---

### D-008 — Tooling is superpowers, not GSD
No `.planning/` directory in this repo. Owner, 2026-08-10.

---

## Disputed

### D-032 — every locked-row progress ratio in `DISPLAY_VOCABULARY.md` is wrong, and wrong in the direction that overstates readiness
**✅ Resolved 2026-08-10 by D-033** — a single disclosed floor at 2026-09-01, with denominators authored and numerators computed. The block is lifted once the recalibration below is applied; the finding is kept for the reasoning trail.

The §10 Charts inventory and §9 Screener cautions hard-code progress ratios and unlock dates. The arithmetic proves they were derived from `HANDOFF.md` §5's false dates (D-001):

| Row | Says | Implied start | Reality |
|---|---|---|---|
| Amihud illiquidity (`:157`) | "24 post-seam months · ~Apr 2027 **(16/24 mo)**" | Apr 2025 — 16 months before today | Seam is late **Jul 2026** → ~**1/24**, unlocking ~**Jul 2028** |
| Supply overhang (`:164`) | "12M census history **(7/12 mo)**" | Jan 2026 — 7 months before today | Census starts late **Jul 2026** → ~**1/12**, unlocking ~**Jul 2027** |
| Amihud percentile (`:113`) | "Needs ~24 post-seam months (Apr '27)" | same | same |
| Supply overhang caution (`:119`) | "7/12 so far" | same | same |

Today is 2026-08-10. Apr 2025 → today is exactly 16 months; Jan 2026 → today is exactly 7. The ratios are not approximations — they are computed from the two dates D-001 disproved.

**Why this is the most serious finding so far:** a user sees a progress bar reading "16/24 mo" and concludes the feature is two-thirds ready. It is roughly one twenty-fourth ready. The product whose entire differentiator is *never compute on insufficient data* would be **overstating data sufficiency inside its own honesty apparatus** — and doing it in the one place users are told to trust.

**`DISPLAY_VOCABULARY.md` also contradicts itself twice**, independent of the seam problem:
- **Discount-to-list coverage:** `:36` says "4.4% coverage"; `:159` says "listed price on **12%** of rows." Same file. See D-031 — 4.4% is the credible figure.
- **Seasonality unlock:** `:36` says "corpus-locked until ~**Nov 2028**"; `:145` says "3 observed cycles · **Nov 2027** (1/3)." Same file, one year apart.

**Also needs a query:** `:160` "Cross-marketplace gap — ≥5 sales/venue/window **(1/5 venues)**" assumes eBay-only, while `DATA_MODEL.md:102,:227` document five sources. Same issue as D-031.

**Scope of the audit:** §2 (the five sufficiency states), §9 (Screener filter cautions), §10 (Charts row inventory), plus §1's `◌` chip "unlock countdown" tooltips and §3's feed UNLOCK (◆) events — every surface that renders a countdown, ratio, or unlock date.

**The design's machinery is not at fault.** LOCKED / LOW DATA / progress-with-denominator is exactly the right apparatus for a young dataset. Only the constants are wrong. The fix is recalibration, not redesign — and the honest recalibration will show far more locked rows for far longer, which is a product-scope conversation (D-011 and the v1 scope question), not just an edit.

All lines read directly 2026-08-10. Owner asked for this to be tracked, 2026-08-10.

---

### D-064 — Demo mode: the flow was designed, then cut, and is now wanted back
Raised by the owner 2026-08-10 while reviewing something unrelated: *"I vetoed demo mode. But I need a way to put it back… I need a hiring manager to be able to just go to the website and play around. But I don't wanna give it away for free to everybody."*

**It was already designed.** Harvested from `CARDSTOCK_UI_SPEC_v1.md` §2.3 before that file was retired — the only place it existed:

> **The hiring-manager flow (criterion 1, explicitly designed).** Landing page → "View live demo" → dropped into a pre-seeded read-only account on **Home** (watchlists populated, signals firing, binder seeded) → most likely clicks: a watchlist row (peek panel), "Open full chart →" (trigger triangles), Screener preset (density), Binder (P&L-vs-index). Write actions show a quiet "Demo mode — sign in to save" nudge. **Total friction: zero clicks of signup.**

Demo mode was then cut wholesale on 2026-08-10 (`HANDOFF.md` §4, "the marketing pages carry that story now"), and the owner has since reversed that.

**Corrections to the original design, from what has been settled since:**
- **Access is not public.** Owner, 2026-08-10: *"the marketing and landing pages is all they should be able to do."* Everything behind `/product` requires login. D-011 is open *signup*, not open *access*.
- **Not one shared read-only account.** Owner proposed a pre-seeded user resetting nightly. Better: **per-session**, since the multi-tenant schema (D-034) makes a demo session just a `user_id` that expires. That removes collisions between concurrent visitors — a hiring manager should never watch their binder change under them — and removes the nightly reset job.
- **Writes should probably work**, not be read-only. Logging a transaction is a core interaction, and the session is discarded anyway. The original "sign in to save" nudge was designed for a shared account that could not tolerate writes.
- **Demo sessions must be excluded from usage stats**, or the numbers are meaningless.
- **The removal was incomplete.** All 11 marketing CTAs still land on `Cardstock Account.dc.html:56`, which renders "Browse the demo →" (D-044). Whatever is built, that remnant needs reconciling rather than deleting.

**Still open:** how a demo session is granted — an open "Try the demo" button on the landing page, or a token the owner hands out per application.

---

### D-063 — 🔒 Architecture: WebAssembly client, stateless API, static marketing, .NET worker
Owner, 2026-08-10. **Resolves D-013 (render mode), D-014 (read API), and D-016 (repo topology).** The last architectural blocker.

| Tier | Choice |
|---|---|
| App (all authenticated screens) | **`InteractiveWebAssembly`** |
| Marketing (`/product`, D-058) | **Static SSR** |
| Between them | **A stateless minimal API** |
| Alongside | **A .NET worker** (D-039) |

**Solution structure**, mirroring the layering proven in `PokemonInvestBatch` (D-023):

```
src/  CardStock.Domain          (references nothing)
      CardStock.Application     (use cases, DTOs, contracts)
      CardStock.Infrastructure  (EF Core, Postgres, intake client)
      CardStock.Api             (stateless, versioned)
      CardStock.Web             (WASM client)
      CardStock.Worker          (index, metrics, screen evaluation)
tests/ one project per source project
```

**Why, in order of weight:**

1. **Every app page is an interactive island** (owner, 2026-08-10). The prototypes settle this — drag-to-reorder and resizable columns on the watchlist, 24 toggles with parameter steppers on Charts, a two-level filter editor on Screener, modals and tabs on Binder. The "mostly static app" plan does not survive contact with them. Only marketing is genuinely static.
2. **Interactive Server is stateful in the way enterprise architecture avoids.** A circuit per user holds server-side render-tree state, so the web tier cannot scale out without sticky sessions or a backplane, and a deploy breaks every open session. Owner's stated goal is to *"mimic an enterprise application."* Stateless API + client-held UI state is that shape by default.
3. **Latency.** Under Interactive Server every toggle, filter, and tab switch round-trips to a residential uplink. WASM makes all of it local — network only when new data is genuinely needed.
4. **The contract boundary is the demonstration.** DTOs distinct from domain models, versioned endpoints, OpenAPI — the most commonly probed thing in enterprise .NET review.
5. **Rate limiting gets a natural seam.** D-062 requires an abuse-shape check; with an API that is ASP.NET Core's built-in rate limiter applied declaratively. Under Interactive Server it would be ad-hoc logic inside a component.
6. **Testability.** The sibling repo already runs CI tests against a real Postgres service container; endpoint tests extend that pattern directly. Circuit tests do not.

**What this forces:** the browser cannot reach the worker's loopback intake API (D-024, bound to `127.0.0.1`). **`express-visit` must be proxied through `CardStock.Api`**, which runs on the same box and can reach it. One extra hop, and the natural place to enforce D-062's limit.

**What it costs, stated honestly:** more projects, a serialization layer, a WASM runtime download on first load, and harder debugging than a single server-rendered app. It also ships slower — which the owner explicitly removed from consideration, since the architecture story *is* the deliverable here.

**Rejected:**
- **`InteractiveServer`** — least code and the spec's original choice, and a legitimate enterprise pattern for *internal* LOB apps on a corporate LAN. Wrong for this one: public internet, residential uplink, unknown concurrency, on a box already running Postgres and a continuous crawler.
- **`InteractiveAuto`** — every component must work both server- and client-rendered, so you build the API *and* the circuit code. Most work, and the payoff only appears at traffic this will not see.

**Supersedes S-002** and `CARDSTOCK_UI_SPEC_v1.md:46` outright. `PROJECT_LOG.md:241` said *"final UI drives API decisions"* (D-042) — the UI is designed, so this is that ruling coming due rather than a reversal. `DATA_MODEL.md:472` has listed "web app read API — undesigned" as an open TODO naming this app as its consumer; that TODO is now answered.

**Terminology note recorded because it caused confusion:** "stateless" describes the API tier only. Application state very much exists — holdings and watchlists in Postgres, identity in a cookie or token sent per request, UI state (open panes, column widths, active tab) in the browser. What is excluded is the *server* holding a session in its own memory. The practical payoff is deploying without disconnecting anyone.

---

### D-062 — Express visits have no spacing floor. Rate limiting is now CardStock's job alone
Owner removed the express spacing floor in `PokemonInvestBatch` on 2026-08-10, recorded there as **ADR-0008 — "express visits have no spacing floor."** Verified directly in the sibling repo: `ExpressSpacingSeconds` and `_lastExpressFetch` no longer appear anywhere in `src`.

**Why it was removed.** Express exists so a human-facing app can get a card NOW. The floor was global — not per-user, not per-card — so a visitor browsing several stale cards waited ~10s **each**. ADR-0006 introduced it as a replacement guardrail for the polite gate it skips, which was correct for the single-operator case it was designed against; it did not survive contact with a public site and ordinary browsing.

> **⚠ Corrected 2026-08-11 — see D-076.** The two bullets struck through below were wrong when
> written, and the line numbers cited for them do not contain what this entry claimed. **There is no
> single-flight and no 504.** Read `ExpressVisitRunner.cs` directly before relying on anything in this
> entry about the worker's guardrails.

**What still exists in the worker** (`ExpressVisitRunner.cs`, re-read 2026-08-11):
- ~~**Single-flight** — `SemaphoreSlim(1,1)`, `:44`, `:109`, `:163`. One outbound fetch at a time.~~
  **False.** The only `SemaphoreSlim` in the sibling's `src/` is `PoliteGate.cs:13`, which express
  bypasses. `:26` — *"in parallel with any other express visit, with no floor, no queue, and no
  timeout."* Express fetches are **concurrent and unbounded**.
- **Same-card coalescing** — `:44–47`, `:56–85`. Concurrent requests for one card ride a single fetch and all hear the answer. ✅ still true.
- **`gate.RecordFetchNow()`** — `:134`. The express fetch stamps the polite gate so the scheduled lane re-spaces around it. ✅ still true.
- The shared `CardVisitor` pipeline, so `last_visited_at` still resets (`CardPageWriter.cs:112`) — which the 24h staleness check depends on. ✅ still true.

**🚩 The consequence that lands on this repo.** ADR-0006 promised *"worst-case extra site load is bounded: one request per spacing floor."* **That bound is gone.** ~~With single-flight as the only limiter, the ceiling becomes one fetch per fetch-duration — roughly 30–60 requests/minute rather than 6.~~ **There is no ceiling.** Nothing in the worker limits express concurrency, so the bound is exactly whatever CardStock sends at once — which strengthens the case for D-037's abuse-shape limit rather than weakening it.

**So CardStock is now the sole guardrail** — but the guardrail is narrower than "rate limit users," and that distinction is the decision.

**Legitimate load is self-limiting and must not be throttled.** Owner, 2026-08-10: *"If it's being called a bunch, that means a lot of cards are being updated, and people are using it and wanting to see those cards."* Correct. The 24h gate means a call only fires when a real person opens a genuinely stale card. A human browses 20–60 cards an hour; ten concurrent users is ~10 calls/minute. Throttling that would be the product refusing to do its job.

**And browsing-driven express visits are a feature, not a cost.** They are *demand-weighted crawling* — cards people care about get fresh, cards nobody opens stay stale. That is arguably a better prioritisation signal than the scheduler's own staleness heuristic, and it costs nothing.

**The guardrail is an abuse-shape check, not a throttle.** The only thing producing sustained load the 24h gate cannot absorb is scripted enumeration of card ids — no human pattern, just iteration. Target a limit generous enough that a person browsing hard never notices it (order of a few hundred express calls per account per hour) and enumeration trips it within minutes.

**And the reason is self-interested, not defensive.** The harm from enumeration falls on **PriceCharting**, not on CardStock: it would send them a sustained stream at a rate the crawler on the same box deliberately never approaches. The downside is asymmetric — if they block the address, `sales` and `populations` stop accumulating and cannot be rebuilt from any source (D-017).

**The intended call pattern** (owner, 2026-08-10): on card page load, read `cards.last_visited_at`; if older than 24 hours, call `express-visit`; the visit resets the field; a second viewer sees it fresh and proceeds without a call. Volume is therefore bounded by *distinct stale cards viewed*, not by page views — and same-card races coalesce for free.

**Spec updates queued:** `docs/screens/card.md` (the refresh flow and the missing loading/failure states — express can still return 502/422/~~504~~ **500**, and the page must render cached data rather than an error). ✅ **Done 2026-08-11 — D-077.**

---

### D-061 — ✅ Class B closed: false data claims corrected across marketing, Screener and Charts
Owner, 2026-08-10. Completes the copy corrections begun in D-060. Corrected values written into `docs/screens/marketing.md`, `screener.md`, and `charts.md` under "Corrected copy / values — build this."

**Marketing (the most exposed instance, being public under D-011).** Six seam assertions replaced. **No date substitutes the date** — the seam is per-card and ragged, so the copy describes the behaviour instead: *"where our sales record starts for a card, we mark it — we never smooth across it."* That is also the stronger claim, since it says more about the product's rigour than a date would. Explicitly recorded: **do not substitute "Jul '26"** — closer to true, but still asserts a shared date the data lacks. The Screener landing's "12 filters" is corrected to **28**.

**Screener.** Six caution strings corrected: listed-price coverage 12% → **4.4%**; the eBay-only assertion dropped pending a query, since five sources are documented; census and ledger start dates changed from fixed months to per-card language; the `7 OBS` badge becomes computed `N OBS`. Unlock dates recomputed from the floor — 24-month liquidity lands **~Sept 2028**, 12-month census **~Sept 2027**.

**Charts — the worst case, because the error is in *logic*, not copy.** `Charts:388–398` hardcodes `SEAM = Apr '25` driving the liquidity panes and `RSEAM = Jul '26` on the price chart. **Neither survives**: a single vertical line across all cards draws a boundary that does not exist. The replacement is `min(sold_on)` per `grade_tier`, per card — a derivation `DATA_MODEL.md:449` already names as available.

**The rule extracted from all of it, now stated in each spec:** *author the denominator; never author the ratio, the numerator, or the unlock date.* Every authored number found in this pass was wrong in the direction that **overstates readiness** — the one direction this product cannot afford.

**Class B is now closed.** All ~25 false data claims across five surfaces are corrected in the specs. Class A (doc staleness) and Class F (prototype self-contradictions) remain, and both are mechanical.

---

### D-060 — ✅ About Data rewritten; all 22 false claims resolved
Owner, 2026-08-10: *"Fix all the twenty two data issues."* Done — the corrected copy is in `docs/screens/about-data.md` under **"Corrected copy — build this."**

**Not written into the prototype.** Per D-052 the mockups are frozen, so the spec is where the corrected page becomes true. The prototype's copy is superseded.

**Every statement in the new copy carries a receipt** in a margin note — `DATA_MODEL.md` line numbers, ADR-0005, or a ledger entry. Nothing is asserted from memory.

**Structural changes, not just corrections:**

- **The page now names pricecharting.com** (D-059) and states the consequence most readers would miss: the individual sales are real transactions, but the **monthly price line is not built from them** — it is the source's own average, stored unaltered. That single distinction resolves §6.1, §6.10, and the framing problem in §6.0 at once.
- **The seam is described as per-card**, with the collection start date given plainly (28 July 2026) and the ragged boundary explained rather than flattened into one line.
- **Monthly prices are called out as the exception** — complete back to ~Dec 2020 regardless of first visit — which fixes the §6.6 error that *understated* the one genuinely deep series by 32 months.
- **A "What we cannot know" section** replaces the false coverage claims: no historical sale counts, nothing older than the ~30-row bucket windows, no census before first visit, no grading company below grade 10.
- **The 2026-09-01 floor is stated with its reason** — absent from the prototype entirely, and required by D-033 now that D-038 ships locks everywhere.
- **Pooling is explained honestly**, including the 91% PSA figure and an explicit statement that no multiplier is applied — satisfying ADR-0005's instruction that the interface must not imply company-neutrality.

**Seven claims were deleted rather than corrected**, because no true version exists: sale counts back to Aug 2023, the exclusion pipeline, daily refresh, the footer stamp on every page, the "under 5% of rows" figure, the English-language claim, and the April 2025 seam.

**One build note carried forward, not copy:** the prototype promised "we mark the affected window on charts" for census restatements. D-046 §6.2 confirmed that marking is unbuilt — detection exists only as an operational alert. The promise is omitted from the copy; either build the annotation or leave the claim out.

---

### D-059 — Legal and licensing posture is deferred; accuracy is not
Owner, 2026-08-10: *"this is just a portfolio piece. If I ever sell it, I'll have to contact a lawyer about all the law stuff… I'll worry about that shit in the future."*

**Deferred, and future sessions should stop raising it:** data licensing, terms-of-service exposure, card-image copyright, republication risk, and any negotiation with the source. These are revisited **if and when the project is commercialised**, with a lawyer, not before. The survey's risk register raised several of these at length; they are noted and parked.

**Explicitly NOT deferred — the distinction that matters:** the 22 false claims in D-046 are not a licensing problem. They are factually wrong. "Sale counts back to August 2023" describes data that has never existed at any point, from any source. Correcting them is correctness work, identical whether this stays a portfolio piece forever or not.

**Consequence for the About Data rewrite (D-5 in the register):** the page **names pricecharting.com**. Chosen not for legal caution but because it is the *simpler* option — the alternative requires writing careful euphemisms that stay truthful without naming anything, which is more effort and is precisely how the page got into trouble originally. One sentence settles it.

**And it serves the stated goal.** Success criterion #1 is a portfolio/interview piece. An About Data page that states plainly where the data comes from, what is derived versus observed, and what cannot be known is a **stronger** artifact than a vague one — arguably the best single piece of evidence of engineering judgment in the project.

**Related items that stay open because they are engineering, not law:** D-010 (images exist on disk; whether to serve them is still a build decision), D-017 (backups), D-037 (the security checklist).

---

### D-058 — Marketing gets its own URL prefix; the app keeps the clean names
Owner, 2026-08-10. Resolves D-1 / D-045 — the marketing/app route collision.

**Rejected the alternative:** one URL resolved by login state (`/` serving Landing when logged out, Home when logged in). That is the common SaaS pattern and was my initial recommendation, but the owner chose separation.

**The route map:**

| | Route |
|---|---|
| App | `/` (Home) · `/screener` · `/charts` · `/binder` · `/browse` · `/card/{id}` · `/set/{id}` · `/character/{name}` · `/settings` |
| Marketing | `/product` (Landing) · `/product/screener` · `/product/charts` · `/product/binder` |
| Auth | `/signin` · `/create` · `/forgot` · `/reset` |

A logged-out visitor hitting `/` redirects to `/product`.

**The prefix name is provisional** — `/product` unless the owner prefers `/features`, `/about`, or another.

**The advantage this has over auth-resolved roots, which I under-weighted when recommending against it:** the static/interactive boundary becomes a **URL prefix rather than a runtime auth check**. Every `/product/*` route is unconditionally static and cacheable, with no per-request branch deciding what to render. That simplifies both caching and the render-mode split (D-013), and it means a CDN or reverse proxy can serve marketing without consulting the app at all.

**Consistent with the extraction findings:** the marketing pages are already light-only (0 `data-theme` occurrences) and carry no interactivity beyond a CSS-animated ticker — so they were never going to share the app's rendering path anyway. D-045 recorded that; separation makes it explicit rather than incidental.

**Spec update queued:** `docs/screens/marketing.md` §1 identity, and every screen spec's route line.

---

### D-057 — Holdings at untracked tiers are valued from the last observed sale at that exact tier
Owner, 2026-08-10. Resolves D-2 / D-012 — the tier→price mapping function.

**A three-level ladder, first match wins:**

| Level | Condition | Renders |
|---|---|---|
| **1** | The tier has a price series (the 6 in `price_months`) | Everything — value, chart, unrealized P&L, vs-index |
| **2** | No series, but `sales` holds an observation at that exact tier for that card | "Last observed sale: **$X** · N days ago" — a real transaction, with its age stated |
| **3** | Neither | Unvalued, excluded from portfolio totals with a visible count |

**Pooling to a nearby tier was considered and rejected.** ADR-0005 pools grades 1–9.5 because *the source already pools them* — the pooled figure is an observation. Substituting here would be a different act: grades 1–6 → Grade 7 swaps in a materially higher grade, and non-PSA 10s → PSA 10 ignores the reason the source splits at 10 at all. Both err toward **flattering the holder's portfolio**, which is the worst direction for this product.

**Level 2 is an observation, not an estimate** — the D-022 line. It reports a sale that happened, at the exact grade owned, and says when. It is *more* honest than pooling, because it never substitutes a different grade.

**What level 2 cannot give:** history, a chart, unrealized P&L over time, or a vs-index comparison. It is a point, not a series, so the holding is excluded from anything time-based.

**Scope is narrower than it first appears.** Cost basis is user-entered, so it always works — and a **closed** position reports real P&L at any tier, because both sides are user-entered. Only *open* positions at exotic tiers lose anything.

**Retrieval is a plain indexed lookup — no title parsing.** `sales.grade_tier` is a first-class column (`DATA_MODEL.md:230`, `string(40)`, required, "21 distinct labels driven by the page's own selector") and `BGS 10 Black` is one of its values (verified, `GradeTierVocabulary.cs`). Index exists on `sales(card_id, sold_on)`.

```sql
SELECT price_cents, sold_on, source FROM sales
WHERE card_id = :id AND grade_tier = :tier
ORDER BY sold_on DESC LIMIT 1;
```

**Why it is this cheap:** the source splits by company *at* grade 10, so the exotic slabs are their own bucket labels. Below 10 it pools, and recovering the company there would need title mining — which ADR-0005 already rejected as unsupportable (<3% of cards qualify). The easy case is exactly the one needed.

**Level 3 shrinks over time.** It means "this card has never had an observed sale at this tier," and the ledger is two weeks old (D-001). That is a countdown the sufficiency framework already knows how to express.

**Two build requirements:**
- **The gallery and table views must agree.** The gallery currently shows a value with no `EST` badge while the table badges the same holding (`Binder:99–102`, `:121`). Same holding, same honesty treatment, both places.
- **`sales.title` is stored raw** (`DATA_MODEL.md:233` — "XSS is the render layer's concern, by design"). D-029 applies anywhere a level-2 value surfaces alongside its listing.

**Spec updates queued:** `docs/screens/binder.md`, `docs/screens/card.md`.

---

### D-056 — Plain `LOW CONFIDENCE` is abolished; it collapses into `LOW DATA`
Owner, 2026-08-10. Resolves D-4 in the contradiction register.

**What the prototype actually does** (`Charts:576`, read directly): a two-level system —
```
p.badge = fz ? 'LOW CONFIDENCE · BURNED IN' : (w ? 'LOW CONFIDENCE' : '')
```
So plain `LOW CONFIDENCE` already fired automatically whenever `suff(id)` was non-null, and `· BURNED IN` marked the user override. `DESIGN_NOTES.md:33, :131, :146` calling it "Charts-only" was stale — but that was never the real problem.

**The real problem was two badges for one meaning.** `DISPLAY_VOCABULARY.md:55` declares five states "the complete render set" — OK / LOW DATA / LOCKED / UNDEFINED window / UNSTABLE FIT — and `LOW CONFIDENCE` is not among them. Plain `LOW CONFIDENCE` and `LOW DATA` were both amber, both meant "the data is thin," and no distinction between them survives inspection. That is precisely the drift a display vocabulary exists to prevent.

**Decided:**

| State | Meaning | Cause |
|---|---|---|
| `LOW DATA` | Below the sufficiency floor. Amber, `N OBS`, tooltip states the floor rule and what improves it | Automatic |
| `LOW CONFIDENCE · BURNED IN` | A lock was overridden. Permanent — `state.forced` is never cleared by any path, so it survives screenshots | **User-caused** |

Plain `LOW CONFIDENCE` no longer exists. `:55`'s five states become true again, and `BURNED IN` keeps its real meaning: *you* chose this, not *the data is thin*. The existing `NEW · 7 OBS` probation badge is `LOW DATA` with its count and needs no change.

**Applied to the specs:** `docs/screens/charts.md` badge inventory (level 2 rewritten to `LOW DATA`), `docs/screens/home.md` §8 row 2 marked resolved, and the feed row copy becomes *"…reached 30 days for PSA 10 — starts LOW DATA"*.

**Note for the build:** D-049 records that the burn-in machinery is orphaned — `force()` has no call site — so `LOW CONFIDENCE · BURNED IN` is currently unreachable. It has to be wired as part of building the LOCKED row form.

---

### D-055 — Watchlist rows are keyed by `(card_id, tier)`, and the grade labels are corrected
Owner, 2026-08-10. Resolves D-3 in the contradiction register.

**The docs win over the prototype here** — `HANDOFF.md:155` and `DESIGN_NOTES.md:110` say one row per card + tier, and that is what gets built. The prototype keys on card id alone (`Home:412–415`), so `(card, tier)` is not representable and the same card cannot be watched at two tiers.

**Why card + tier:**
1. **Signals are tier-specific.** ROC on PSA 10 and ROC on Raw are different series with different answers; a signal-tracking row without a tier is ambiguous by construction.
2. **The product's own thesis needs it.** Grading arbitrage — `Psa10 ÷ Ungraded` (D-004's valuation group) — means watching one card at two tiers deliberately.
3. **The display already assumes it.** Home has a Tier column with its own resize handle (`Home:96`) and every row carries a tier (`:507`). Only the key was missing.

**Where the tier gets chosen:** not on the Card page. `DESIGN_NOTES.md:112` already makes Charts the editor ("you pick which signals it tracks in Charts"), and Charts has a one-tier rule, so the analysis tier is already first-class there. The Card page adds at a sensible default; the tier is edited from the watchlist row's ⋯ menu or in Charts.

**Grade label correction.** The prototype seeds `PSA 9` and `PSA 8` (`Home:371`, `:381`). Those are not real tiers — ADR-0005 pools grading companies below grade 10, so the labels are **`Grade 9`** and **`Grade 8`**. This is the direct cause of a live defect: the peek's tier highlight is raw string equality against `TIER_LABELS` (`Home:511`), so `'PSA 9' !== 'Grade 9'` and nothing highlights.

**Fix the labels, not the matcher.** Loosening the comparison would paper over a vocabulary error with a fuzzy match, and D-022 records the owner rejecting exactly that class of move — asserting a grading company the record never named.

**Applied to the spec** (per the maintenance rule in `CLAUDE.md`): `docs/screens/home.md` §3 data contract updated to build values, and §8 rows 5 and 21 marked resolved with the reasoning preserved rather than deleted.

---

### D-053 — Account deletion is a bounded window matching backup rotation
**⚠️ Superseded 2026-08-11 by D-069, conditionally.** This entry's entire load-bearing premise was
that off-box backups are mandatory, so "immediately and permanently" could not be honoured. The
owner has deferred backups; with no dumps, immediate deletion is keepable and
`Cardstock Profile.dc.html:181` is correct as written. **If backups are ever added, this entry
revives and the privacy copy must change in the same commit** — otherwise the promise becomes false
silently. Original reasoning kept below.

Owner, 2026-08-10. Resolves the C-1 Tier-1 conflict in D-043.

**The `Cardstock Legal.dc.html:57` version wins** — data "removed within 30 days," with the number set to match actual backup rotation. `Cardstock Profile.dc.html:181, :191`'s "immediately and permanently… no recovery" is superseded and its copy must be rewritten.

**Why the conflict resolves this way rather than by preference:** D-017 forces it. `sales` and `populations` cannot be rebuilt from any source, so off-box backups are mandatory. The moment they exist, "immediately and permanently" is unkeepable — a deleted row survives in last night's dump until that dump rotates out. A bounded window is the only promise a backed-up system can actually honour, so the alternative was not merely worse, it was false.

**Two consequences to carry into the build:**
- **The retention number and the backup rotation are one setting, not two.** If rotation changes, the privacy policy changes. Worth wiring so they cannot drift apart.
- **`Legal:57` instructs a step the product cannot perform.** It tells users to "export your binder as CSV first," but Profile has no export affordance and the Binder's CSV control generates no file (`HANDOFF.md` §6, verified). Either build the export before launch or cut that sentence. Under D-011 this is a public instruction, so leaving it broken is not neutral.

**Spec updates queued** (per the maintenance rule): `docs/screens/legal.md`, `docs/screens/profile.md`, `docs/screens/binder.md`.

---

### D-054 — One documentation location: `docs/`
Owner, 2026-08-10: *"I want to end up with one location for updated non-contradictory docs, you decide how that happens."*

**Decided.** `docs/` is the only documentation location. The repo root keeps exactly two control-plane files: `CLAUDE.md` (must be there — the harness loads it) and `DECISIONS.md` (kept there because relocating it would rewrite 117 cross-references for no clarity gain). `CardStock Mockup/` holds **no markdown, by rule**.

**Target end state:** `CLAUDE.md` + `DECISIONS.md` + `docs/screens/*.md` + `docs/brand.md` + `docs/adr/`. One document per question — what are the rules, what did we decide, what do I build.

**Done:** `uploads/PROJECT_LOG.md` deleted (harvested as D-042); `brand-system.md` moved to `docs/brand.md`; `docs/README.md` written as the index.

**✅ Completed 2026-08-10. `CardStock Mockup/` now contains zero markdown.** Ten files resolved:

| File | Disposition |
|---|---|
| `uploads/PROJECT_LOG.md` | Deleted — harvested as D-042 |
| `brand-package/BRAND_BRIEF.md` | Deleted — byte-identical duplicate, no harvest needed |
| `BACKTEST_WARNINGS.md` | Deleted — all 15 checks harvested into `screens/screener.md` |
| `BRAND_BRIEF.md` | Deleted — trade-dress prohibition, positioning and tone harvested into `brand.md` |
| `uploads/Brand package creation/README.md` | Deleted — colour-separation rule, series assignment, logo rules and scales into `brand.md`; ticker/deck timings and the only responsive guidance into `screens/marketing.md` |
| `HANDOFF.md` | Deleted — §7 conventions verified already covered; **§1's two honesty rules rescued into `CLAUDE.md`**, where they had never been stated |
| `uploads/CARDSTOCK_UI_SPEC_v1.md` | Deleted — success criteria and persona into `CLAUDE.md`; the hiring-manager demo flow into D-064 |
| `DISPLAY_VOCABULARY.md` | Deleted — verified fully superseded; `brand.md:156–160` carries the four-mode palette *more* completely, and view modes live across six screen specs |
| `uploads/compass_artifact_*.md` | **Kept** → `docs/signals.md`. The most accurate document in the project — zero false seam dates, and it states that `[S1]` is a monthly average rather than OHLC, the exact fact About Data got wrong |
| `DESIGN_NOTES.md` | **Kept** → `docs/design-rationale.md`, frozen |

**Deviation from the original plan, recorded deliberately.** This entry said `DESIGN_NOTES.md`'s "rulings migrate to the screen specs and its reasoning to `docs/adr/`." The rulings did migrate. **The reasoning did not** — converting 167 lines of dated, dense rationale into ADRs is a large job with modest payoff, and the document is already good at its one function. It was relocated and frozen instead, with a header stating its authority and its two limits. If that reasoning is ever needed as formal ADRs, the source is there to convert.

**Note on retired-file citations.** Several `docs/screens/*.md` §8 rows cite line numbers in files that no longer exist. The quoted claim is preserved verbatim in each row, and the files remain in git history — `git log --diff-filter=D --name-only` locates the deleting commit. The audit trail survives; only re-running a citation now requires git.

**`docs/CONTRADICTIONS.md` is scaffolding and deletes itself** when its classes are worked through. Recorded in `docs/README.md` so a future reader does not preserve it out of caution.

**Handle `DESIGN_NOTES.md` last and carefully.** It has proven the most reliable document in the set — its census branch rules reproduce the seeded arithmetic exactly (D-051), and the Card audit called that "the single most valuable doc find for the build." Its rulings migrate to the screen specs and its reasoning to `docs/adr/` before it goes.

---

### D-052 — Tier colours become tokens across all three modes; the prototypes are frozen
Owner, 2026-08-10, answering the first of the contradiction-queue decisions. Two decisions in one.

**Part 1 — the tier palette.** The question started as "Card and Charts disagree on three hex values." Verification showed a larger problem: the tier palette participates properly in **none** of the three modes.

| Palette | Light | Dark | Colorblind |
|---|---|---|---|
| `brand/brand-tokens.css` `--series-1..6` | ✅ `:14–15` | ✅ `:28–29` | ❌ **no block exists** |
| `Cardstock Card.dc.html:325` `TIER_COLORS` (19) | 3 via `PAL`, **16 frozen hex** | those 16 unchanged | those 16 unchanged |
| `Cardstock Charts.dc.html:375` `TIER_COLORS` (19) | same, 3 differing from Card | unchanged | unchanged |

Only `PSA 10` (`PAL.acc`), `Grade 9` (`PAL.warn`) and `Raw` (`PAL.mut2`) theme at all. `brand-tokens.css` has exactly two blocks — `:root` and `[data-theme="dark"]` — with **zero** occurrences of cvd/colorblind/okabe. And nothing links that file except the Brand System reference page.

**Decision:** do not pick between the two frozen literals. **Adopt `--series-1..6` as tokens for the six price tiers**, add the missing colorblind block, and declare the union once globally — which D-050 established the Blazor build needs regardless, since the app has no `:root` light block at all.

Six series tokens map exactly onto D-003's six price tiers (Card strip, Charts series). The remaining 13 `TIER_COLORS` entries are sales-ledger grade chips — a separate, lower-stakes set, because chips carry their label and colour is not the sole carrier there. Card and Charts can no longer diverge, because both reference the same tokens.

**Part 2 — the prototypes are frozen (option A).** No `.dc.html`, `.js`, or `.css` file gets edited. Verified as of this decision: 19 files changed since the initial commit, all markdown, and the only file touched inside `CardStock Mockup/` is `HANDOFF.md`.

**Rationale:** the prototypes are the reference everything was verified against, so editing them moves the ground under that verification — and they are scheduled for replacement, making edits throwaway. The tier palette proves the point: it *cannot* be fixed correctly in the prototypes, because they have no `:root` light block and no CVD series block. Doing it right means building the token architecture the Blazor app needs anyway.

**The consequence the owner identified:** *"as soon as we don't make these changes to the mock ups, the mock ups are no longer the record of truth."* Correct, and now encoded — `docs/screens/*.md` is promoted to the build reference in `CLAUDE.md`, with the prototypes demoted to source-and-visual-tiebreak. They remain authoritative about *themselves*; they stop being authoritative about *facts*.

**And the discipline that keeps it from rotting:** every decision changing a screen must land in that screen's spec, not only here. The ledger records why and when; the spec records what to build. Recorded in `CLAUDE.md` under "The maintenance rule that makes this work."

---

### D-051 — ✅ The Card page tier strip is 6 cells, and it matches the database exactly
Direct extraction of `Cardstock Card.dc.html`, 2026-08-10. The strip is a literal `repeat(6, 1fr)` grid (`:84`) filtering the 19-value `BUCKETS` down to **PSA 10 / 9.5 / 9 / 8 / 7 / Raw** (`:395`).

**This resolves a contradiction in CardStock's favour.** `HANDOFF.md:76` calls it a "19-tier strip" and is simply wrong; `DESIGN_NOTES.md:55` describes the six-tier arrangement correctly in every particular. And six is exactly what `price_months.tier` carries (D-003). **The Card page's price surfaces are a clean 1:1 with the data** — no gap, no approximation, nothing to reconcile.

That is the strongest argument yet for the Card page as the first build (sub-project C): its tier strip, price chart, sales ledger, census bars, and images all have real backing data today, and it needs no market index, no metrics tier, and no auth.

The 19 values still exist on this page — as ledger grade chips, filter chips, and sort rank — which is correct, because `sales.grade_tier` genuinely carries 19 values. Six for price series, nineteen for sales. Both right, in different places.

**Also verified — a document that is correct.** The census summary-sentence branch rules at `DESIGN_NOTES.md:52–53` reproduce the seeded output arithmetic *exactly*, every threshold checked. After this many corrections it is worth recording that `DESIGN_NOTES.md` is largely reliable where it describes design rulings; the failures have been concentrated in data claims and in stale entries it never revisited.

**Confirmed as specified:** the Listed-column drop, down to the dotted amber `#8F6614` border and the `listed X → sold Y` tooltip (`:204`, `:352`, `:457`); and the dashed-line + hollow-end-dot current-month treatment with no projection (`:414`, `:132`).

**Not built, adding to the D-049 pattern:** seam markers render **nowhere** — `SEAMS` (`:321`) is dead data, `isSeam` is always false, and no markup branch exists. `DESIGN_NOTES.md:47` says they render in date sort while `:54` says they were removed; the HTML sides with `:54`, and the file contradicts itself.

**Stale Tier 3 corrected:** `PROJECT_LOG.md:282` requires the card refresh be asynchronous. The prototype asserts synchronous, and `express-visit` (D-024) is synchronous by design. The log is wrong; sync stands. Feeds D-025.

---

### D-049 — 🚩 The LOCKED row form is dead code. The lock UI D-038 ships was never prototyped
Direct extraction of `Cardstock Charts.dc.html`, 2026-08-10: `locked()` (`:595`) and `force()` (`:403`) have **zero call sites**. No LOCKED chip, no disabled switch, no progress bar, and no working "show anyway →" ever renders.

All six rows the docs call "locked" are built by `lockedOr` (`:596–598`), which returns **an ordinary pane-opening toggle** with a `LOW DATA` badge and a permanent amber note — and **silently discards the progress ratios passed to it** (`:607`, `:625–628`, `:634`). `DISPLAY_VOCABULARY.md:145,157,158,160,164` documents those ratios; none reaches the screen.

The burn-in machinery is real but orphaned: `state.forced` gates a `LOW CONFIDENCE · BURNED IN` pane badge and is never set or cleared by any reachable path.

**This corrects D-032.** I wrote that the wrong ratios were "user-visible progress bars overstating readiness." In the prototype they are not visible at all — the error lives in `DISPLAY_VOCABULARY.md`, not on screen. The recalibration to the D-033 floor is still required, but it is a documentation fix plus a **build**, not a repair of something working.

**And it puts real weight behind D-038.** Shipping "the full UI with locks visible" means building lock UI that Tier 1 does not contain: the disabled control, the countdown, the progress ratio, and the burn-in override are design intent captured only in `DISPLAY_VOCABULARY.md`, with orphaned scaffolding in Charts. That is net-new work, not a port. It should be scoped as such.

**Also settled:** Charts has **31** rows — 24 toggles + 7 readouts — not the 32 in `HANDOFF.md:73` nor the 29 in `DESIGN_NOTES.md:6`. `DISPLAY_VOCABULARY.md` §10 already lists 31 and is right.

**Two hardcoded seams in the chart code:** `SEAM` = Apr '25 driving the liquidity panes, and `RSEAM` = Jul '26 on the price chart (`:388–398`, `:227`, `:245`). The second is roughly right by accident; the first is D-001's disproved date wired into rendering logic. Adds to D-048's inventory.

---

### D-050 — Brand tokens: the app and the brand package disagree, and five WCAG failures are undocumented
Direct extraction of `Cardstock Brand System.dc.html`, `brand/brand-tokens.css`, and the app pages, 2026-08-10.

**Two token systems exist and no prototype links the brand package.** `brand/brand-tokens.css` is standalone; the app carries light values as `var(--x, #LITERAL)` inline fallbacks plus a four-branch `PAL` object (`Cardstock Home.dc.html:323–330`), with only dark and colorblind declared in per-page helmet styles. **There is no `:root` light block anywhere.** A Blazor rebuild must invert this — declare the union once, globally.

**Colorblind mode is confirmed hue-only**, four independent ways in code. The one addition beyond hue: Charts dashes the MACD signal line under CVD (`:791`) — redundant encoding, which strengthens rather than violates the rule.

**WCAG — one documented failure, five not:**

| Token | Value | Ratio | Status |
|---|---|---|---|
| `--muted` | `#8A8A86` | 3.31–3.47 | The known issue. **Already fixed in the app** (`--mut2` → `#6B6B66`, 5.36) but **still failing in the brand package** |
| `--neg` CVD | `#CC5F00` | 3.86–4.04 | **Undocumented** — colorblind mode makes negative text fail in light theme |
| `--neg2` CVD | `#D55E00` | 3.70–3.87 | Undocumented |
| `--pos2` | `#189E63` | 3.29–3.44 | Undocumented, 6 text usages |
| `--neg2` | `#D64545` | 4.19–4.38 | Undocumented, 7 text usages |
| `--brand-foil` | `#9A7B2D` | 3.83–4.00 | Undocumented, 11px badge text |

Items 3–5 disappear if all text routes through `--pos`/`--neg` rather than the graphic tokens. `--mut3` `#8F8F8A` passes only because it has zero text usages — that demotion is load-bearing and must survive the rebuild. Dark theme has no failures.

**Note the irony to avoid repeating:** `DESIGN_NOTES.md:26` still records the contrast pass as deferred; it shipped 2026-08-10 and the old value has zero occurrences in app pages. The same file self-contradicts on the accent (`:26` vs `:136`) — the real values are `#4A63D0` / `#3A4FB8`.

**No `prefers-color-scheme` anywhere** — a first visit is always light, regardless of OS setting.

---

### D-048 — 🚩 The false data claims are baked into prototype *copy*, not just the markdown docs
This is the consolidation that matters for the rebuild. D-001's disproved dates are not confined to `HANDOFF.md` — they are written into the prototypes themselves, which means correcting the documents does not fix the product.

**Inventory so far, all verified by direct extraction 2026-08-10:**

| Surface | Lines | Wrong claim |
|---|---|---|
| Marketing (3 pages) | `Landing:202,235,236`; `Charts Landing:45,74–75,113`; `Screener Landing:92` | "Apr '25 seam" (D-044) |
| About Data | `:52,69,71,72` + 18 more | Apr 2025 seam, Aug 2023 history, sale counts (D-046) |
| Screener | `:505,511` | "census begins Jan 2026", "ledger begins Apr 2025" |
| Screener | `:490,494,548,552–554,774` | "Apr '27", "7/12", "Jan '26 — 7 obs", `7 OBS` — the D-032 ratios |
| Screener | `:491,550` | "~12% of rows" for listed-price coverage; the real figure is 4.4% (D-031) |

**This is the standing exception to D-040.** The rule says the prototypes are absolute truth and the HTML wins. That holds for *what the design is* — layout, states, interactions, copy tone. It does **not** hold for *what the data is*, where `../PokemonInvestBatch` is the authority. Where a prototype states a fact about the data, it can be wrong, and here it is.

**Consequence:** the copy correction is a cross-cutting workstream over the prototypes and every derived spec — not a documentation edit. Every date, ratio, and coverage percentage in user-facing copy must be re-derived from the 2026-09-01 floor (D-033) before anything ships, per D-038 shipping locks everywhere.

**Also settled by this pass:** the Screener has **28** filter metrics — not the 27 in `HANDOFF.md:72`, nor the 29 in `DESIGN_NOTES.md:7`. `DISPLAY_VOCABULARY.md`'s own table already lists 28 while its prose says 27.

---

### D-046 — 🚩🚩 The About Data page is substantially false, and it is the page whose entire job is being true
Direct audit of `Cardstock About Data.dc.html`, 2026-08-10: **22 claims FALSE, 13 UNVERIFIABLE, 1 required statement missing.** This is the page the design points to whenever a metric is omitted, locked, or caveated — Tier 1 exclusions are "omitted from the app entirely. One 'About our data' page explains why." If it is wrong, the honesty framework has no floor under it.

**The organising concept is false.** "The April 2025 seam" is the page's central structure and appears at `:52`, `:69`, `:71`, `:72`. D-001 disproved it — the seam is per-card, ragged, and begins late Jul 2026.

**Selected FALSE claims, each with the receipt against it:**

| Claim | Line | Why it's false |
|---|---|---|
| "Before April 2025 our archive holds… **sale counts**" | `:71` | `DATA_MODEL.md:481–482` — historical sales volume is "unavailable from source, permanently" |
| "monthly aggregates back to **August 2023**" | `:71` | `price_months` backfills to **~Dec 2020** (D-002). Understates the one genuinely deep series by ~32 months |
| "From April 2025 forward we keep **every individual transaction**" | `:71` | The source keeps ~30-row bucket windows and discards older rows forever (`DATA_MODEL.md:102`) |
| "Prices come from **realized sales only**" | `:62` | The plotted series is the source's own computed monthly average, not a transaction (D-003) |
| "the seam is **drawn as a marker on charts**" | `:72` | It is ragged and per-card; a single marker cannot represent it |
| "Sales data **refreshes daily**" | `:79` | Contradicted by the crawler's actual cadence |
| "**footer stamp on every page**" | `:79` | Neither this page nor Legal has one |

**The largest single exposure is framing, not any one line.** The page never names pricecharting.com, while using "our archive," "we keep," and "Excluded" throughout. It reads as first-party collection of a corpus that is 100% scraped from one third party — and it is the page a reader would consult specifically to learn provenance. Under D-011 (public signup) that is a public misrepresentation, and it also discards the attribution that would otherwise be the strongest mitigation if the source ever objected.

**Missing:** the 2026-09-01 sufficiency floor (D-033) appears nowhere. With D-038 shipping locks across every screen, the page that explains them must state the floor and why it exists.

**Verdict: this page needs rewriting from the data up, not editing.** Its structure encodes a fact pattern that does not exist.

---

### D-047 — Two previously-unverified flags are now confirmed present in `Cardstock Legal.dc.html`
Both were recorded earlier with an explicit "I have not read that file." Both are now read directly, 2026-08-10.

- **`:55` — "no third-party trackers"** and analytics "limited to aggregate, anonymous usage counts." D-037's concern is real: if the existing New Relic OTLP stack touches the web tier, this is false from day one. Also promises "no aggregate we publish can be traced back to a person's holdings" — a k-anonymity commitment with no mechanism behind it.
- **`:57` — account data "removed within 30 days."** Confirms the D-043 Tier-1 conflict against `Cardstock Profile.dc.html:181`'s "immediately and permanently," and confirms the D-017 backup interaction.

Unlike About Data, nothing on the Legal page is outright false today. The exposure is promises that are not yet true — which become false on launch unless the implementation matches them.

---

### D-044 — 🚩 The marketing pages assert the false Apr '25 seam in 7 places
The seam that D-001 disproved is stated as fact across three public marketing pages: `Cardstock Landing.dc.html:202, :235, :236`; `Cardstock Charts Landing.dc.html:45, :74–75, :113`; `Cardstock Screener Landing.dc.html:92`.

**This is the highest-priority copy fix in the project.** D-011 made the site publicly signup-able, so these are public factual claims about data provenance — on the pages a visitor sees first, for a product whose entire differentiator is honesty about data. Every one is wrong by roughly 15 months.

Also flagged as high-risk by the same pass: all 30-day sales and census ticker stats, and the Screener landing's headline "churn" filter, which is post-seam-gated and therefore not computable yet.

**Note the tier interaction:** the mockups are Tier 1 (D-040), so normally the HTML wins. Here the HTML is simply wrong about the world, and `../PokemonInvestBatch` — also Tier 1 — overrides it. Tier 1 is authoritative about *what the design is*, never about *what the data is*.

**Related, unresolved — demo mode was removed incompletely.** Demo mode has 0 occurrences across all four marketing pages, but all 11 marketing CTAs land on `Cardstock Account.dc.html:56`, which still renders "Browse the demo →" into the app. `DESIGN_NOTES.md:141` records the removal and omits the Account page.

---

### D-045 — Marketing and app routes collide; the HTML cannot settle it
`HANDOFF.md:83` puts the Landing at marketing `/` while `:71` puts app Home at `/`. Same for the three pillar pages at `/screener`, `/charts`, `/binder` (`HANDOFF.md:84`) against the app's own `/screener`, `/charts`, `/binder` (`:72–74`).

The prototypes link by bare filename, so Tier 1 **cannot** resolve this — it is a genuine design decision, not a documentation error.

**Why it matters for the build:** this is a routing decision entangled with render mode (D-013). The obvious resolution is auth-resolved roots — `/` serves the Landing when logged out and Home when logged in — which fits the static-SSR-for-marketing, interactive-for-app split cleanly, since the two branches want different render modes anyway.

Two smaller findings from the same pass, both affecting how marketing pages get built:
- **No `prefers-reduced-motion` in any of the four marketing pages** (six app pages have it). The ticker animates unconditionally — `@keyframes cdstkTicker` at `Landing:20`, applied `:334` as `44s linear infinite`. Contradicts the brand package README `:115`. The ticker motion is pure CSS over a duplicated list (`items.concat(items)`, `:326`), so pausing it is trivial.
- **The marketing pages are light-only** — 0 `data-theme` occurrences across all four, contradicting `DESIGN_NOTES.md:105`'s "dark mode app-wide, all 10 pages."

---

### D-043 — 🚩 Tier 1 contradicts Tier 1: the account-deletion promise
Two prototypes state incompatible deletion policies. D-040 says the mockups are absolute truth — but it has no rule for when two mockups disagree, so this needs an owner ruling and D-040 needs a tiebreak clause.

| Source | Says |
|---|---|
| `Cardstock Legal.dc.html:57` | data "removed within **30 days**" |
| `Cardstock Profile.dc.html:181, :191` | "**immediately and permanently**… no recovery" |

**Why it matters now rather than later:** D-011 made the app publicly signup-able, so the privacy policy is a public commitment rather than placeholder copy. Shipping both texts means one of them is false to every user who reads it.

**Interaction with D-017:** once off-box backups exist — and they must — "immediately and permanently" becomes impossible to honour literally, because deleted rows survive in dumps until those dumps age out. A bounded window is the only promise a backed-up system can actually keep. That argues for the Legal page's version, with the retention window set to match backup rotation.

Also verified in the same pass: `Cardstock Legal.dc.html:57` tells users to "export your binder as CSV first" — but the Profile page has **no export affordance** (0 occurrences). The Binder's CSV control exists but generates no file (`HANDOFF.md` §6). So the policy instructs a step the product cannot perform.

**Needed:** one deletion policy, written once, reflected in both prototypes and whatever ships.

**Second instance, found 2026-08-10 — tier colours.** `Cardstock Card.dc.html:325` and `Cardstock Charts.dc.html:375` assign **different colours to the same tiers** (9.5, 8, and 7 diverge). Code versus code, both Tier 1, same conflict class. A card viewed on its own page and the same card in the Charts playground would render its tiers in different colours.

This one is more consequential than it sounds, because `HANDOFF.md` §7 states colour never carries meaning alone and every state pairs a hue with a glyph — a tier palette that changes between screens undermines the reader's ability to learn the mapping at all. Needs one palette, defined once, in the shared component library (D-050 makes the same argument about tokens).

**Two instances now means this is a category, not an accident.** Every future Tier-1 conflict goes here rather than being silently resolved.

---

### D-031 — Two more numbers in `HANDOFF.md` §5 contradict better sources
The §5 data-dependency table carries at least two more values that disagree with documents closer to the data.

**Listed-price coverage.** `HANDOFF.md` §5 says "~12% of rows." `DESIGN_NOTES.md:46` says *"production coverage is 4.4% (143,062 of 3,265,910 sales)"* — a precise count that reads like it came from a real query, and it was the basis for dropping the column from the Card page entirely. `DATA_MODEL.md:232` says only "most rows have none." **The 4.4% figure is the credible one; ~12% looks stale.** Not yet settled by a live query.

**Venue depth.** `HANDOFF.md` §5 says "eBay-only today," gating the cross-marketplace-gap indicator. `DATA_MODEL.md:102` and `:227` both document five sources: **ebay, tcgplayer, goldin, heritage, pwcc**. The schema captures five; whether observed volume is effectively eBay-only is a distribution question a query would settle in seconds. As written, the gate may be locking an indicator that has data behind it.

**Unverified in the same table:** "Annual cycles · 1 of 3."

All read directly 2026-08-10. Raised, not corrected — these need a live query, not an edit.

---

### D-009 — `DESIGN_NOTES.md:35` still specifies an "Apr '25 liquidity seam" that D-001 says cannot exist
The line reads: *"Seams: liquidity seam Apr '25 (churn/vol panes), resolution seam Jul '26 amber dashed line on price chart."* Two seams, one of which has no data behind it.

**Unresolved:** is the Apr '25 liquidity seam an error that propagated into `HANDOFF.md`, a different data source that was once planned, or a deliberate design fiction in the mockups? It is currently specified to render in the churn and volume panes.

**Needs:** an owner ruling. Raised 2026-08-10.

---

## Open

### D-086 — The sibling untracked DATA_MODEL.md; CardStock's Tier-1 pointer now targets a private scribble
Surfaced 2026-08-13 by the enrichment agent's final report. PokemonInvestBatch commit `74eff03`
(2026-08-01, "The data model notes go back to being private scribbles") deliberately untracked
`DATA_MODEL.md` and gitignores it — while CardStock's `CLAUDE.md` durable-pointer table cites
`../PokemonInvestBatch/DATA_MODEL.md` as Tier-1 authority, and this ledger cites it by line number
throughout. The file still exists on disk and was brought current by the enrichment work (on disk
only); the sibling's citable contract is its ADRs. **Needs an owner ruling:** repoint CardStock's
authority table at the sibling's ADRs (+ schema tests), or re-track the file over there.

Related corrections from the same report, recorded so estimates stop citing stale figures:
production has **789 sets, not ~303** (Japanese alone 395 — the research fixture was stale), and
measured enrichment coverage is **41.2% overall / 92.7% of numbered English** — below the research's
45–75% band because the set denominator was wrong in the optimistic direction. Receipts:
sibling ADR-0009, `scratchpad/tcgdex-audit/audit-report.txt`, sibling commits `80a944c`/`88c156d`
(local, unpushed).


### D-079 — Metadata enrichment via TCGdex: researched, feasible, owner to implement in the sibling repo
Raised by the owner 2026-08-12 during Phase 2 brainstorming, after learning the scraper's data cannot
fill the Card page's `{set} · 215/203 · {character}` subline (no number column, no set size, no
species — `DATA_MODEL.md` §3.1–3.2). Owner: the solution "should not live in this project… I can
implement it in the batch process if it is possible." Research says it is possible.

**Verified by me, live, 2026-08-12** (re-ran the probes myself after the research workflow):
- `GET api.tcgdex.net/v2/en/cards/swsh7-215` → `localId "215"`, `name "Umbreon VMAX"`, `dexId [197]`,
  `set.cardCount { official: 203, total: 237 }` — all three missing fields, on the exact example card.
  `official` vs `total` is the printed-size vs secret-cards split.
- `tcgdex/cards-database` LICENSE is plain **MIT** — permanent Postgres storage, modification, and
  commercial use (D-034) expressly permitted; only obligation is notice preservation.
- `tcgdex/server` Docker tags ship `linux/arm64` — self-hostable beside the worker on the Pi.
- pokemontcg.io's bulk repo `PokemonTCG/pokemon-tcg-data` has **no license** (GitHub `license: null`)
  and its live API returned 5xx on most probes → wrong foundation for a permanent enrichment column.

**Claimed by the research workflow, not re-verified by me** (full receipts in
`docs/superpowers/handoffs/2026-08-12-tcgdex-enrichment-research.txt`, salvaged from the crashed
session's task directory): an executed number-driven join matched **283/283** numbered products on two sampled
150-product PriceCharting pages (Evolving Skies, Base Set), with ~97% name agreement and known synonym
classes (Electric/Lightning Energy, gender symbols, é). Set-name mapping is the real work: ~124/300
PC set names exact-match after stripping the "Pokemon " prefix; ~20 need hand-aliases; the ~157
Japanese/Chinese/Korean/Topps sets do not auto-join (TCGdex serves Japanese sets under its `ja`
locale only). Known breakers: Celebrations Classic Collection (PC `#4` vs TCGdex `CC002`), the
`Pokemon Promo` grab-bag's bare numbers. `localId` can be non-numeric (`TG23`) → text column, not int.

**Open, owner's call:** whether and when to implement in `../PokemonInvestBatch` (vendored/pinned
TCGdex release or self-hosted image, enrichment table keyed by card id with an explicit
`match_status`, species stored 0..n). **Phase 2 does not block on this** — the Card page ships on
today's columns and upgrades its subline when enriched columns appear. Related: the image-source
question is settled the other way — TCGdex rejected for images (holo/non-holo), recorded in
`DATA_MODEL.md` §2 (`:105–114`) the same day (that file has no §2.4 heading — pointer corrected
2026-08-12 while appending the update below).

**Update, 2026-08-12 (restarted session):** owner confirmed the direction — *"if we are going to use
TCGdex to populate the missing data, that's fine, but that needs to be done in the Pokemon invest
batch context"* — and chose the delivery: a written handoff now, then **a subagent scoped to
`../PokemonInvestBatch` alone, spawned after Phase 2 planning wraps**. The brief lives at
`docs/superpowers/handoffs/2026-08-12-tcgdex-enrichment-handoff.md`, with the salvaged research
receipts beside it. The three fields were re-verified live from this session as well:
`api.tcgdex.net/v2/en/cards/swsh7-215` → `localId "215"`, `dexId [197]`,
`set.cardCount {official: 203, total: 237}`. The owner adds, same day: *"this will not be the last
data enrichment that we're gonna come across"* — recorded as an expectation for the sibling ADR to
weigh, not a constraint (per `CLAUDE.md`'s rule on expectations).

### D-011 — Public URL, or private portfolio piece?
The single answer that most reorders everything downstream. Private (Tailscale / localhost / screen-share) drops image licensing, provenance rewrites, rate limiting, and residential-exposure concerns off the critical path entirely. Public gates all of them.

### D-012 — Specify the tier→price mapping function for Binder holdings
**Reframed 2026-08-10.** This is not "which tiers are missing." It is: *given a holding recorded at any of the Binder's 118 labels, what price series values it, and what happens for the 93 with none?* See D-003 — the Binder being the most granular surface is correct by design.

Three honest options for the unmapped 93, none of which may invent precision (D-022):
1. **Pool to the nearest backed tier** — a CGC 9.5 already pools to `Grade9Half` correctly, so this is only novel for grades 1–6 and the non-PSA 10s. Must be disclosed, never silent.
2. **Show the holding unvalued** — render the position with no market value and a stated reason.
3. **Exclude from portfolio totals** — with a visible count, in the spirit of the Screener's "N cards hidden" pattern.

Whatever is chosen applies identically to cost basis, P&L, and the vs-index comparison, and must be visible on both the table and gallery views — which currently disagree (see below).


**Corrected 2026-08-10.** My earlier framing used "CGC 9.5" as the example. That was wrong: the source pools grading companies for grades 1–9.5 and splits only at 10 (ADR-0005), so a CGC 9.5 **does** have a price series — the pooled `Grade9Half`. See D-022.

The tiers genuinely without a price series are:
- **Grades 1–6** — `price_months` carries only 7, 8, 9, 9.5 below 10 (D-003)
- **Every non-PSA 10** — CGC 10, CGC 10 Pristine, BGS 10, BGS 10 Black, SGC 10, TAG 10, ACE 10. `price_months` has exactly one grade-10 tier and it is `Psa10`.

So the open question is narrower and sharper: a user owns a BGS 10 Black. It has sales rows but no price series. Value it at PSA 10? At PSA 10 with a haircut? Leave it unvalued and exclude it from portfolio totals?

**Strong steer from D-022:** the owner has already rejected exactly this move once. Applying a multiplier to approximate an unobserved company price was rejected as "statistically dishonest." Valuing a BGS 10 Black at `Psa10 × factor` is the same decision in a new place. Affects cost basis, P&L, and "vs market index" — the product's stated emotional centre.

**Widened 2026-08-10 by direct extraction of the Binder prototype — the real number is far larger than the framing above.** The tier picker is not a 19-value list. It is a **7 grader × N grade cross product producing 118 labels** (`Cardstock Binder.dc.html:368–377`), including half-grades at every 0.5 step from 1.5 to 9.5, plus `CGC 10 Pristine`, `BGS 10 Black Label`, `TAG 10 Pristine`, `SGC 10 Pristine`.

**93 of those 118 labels have no price series.** Only 7 labels overlap the canonical vocabulary at all, and `tierRank` (`:451`) returns `−1` for most of what the picker emits. Spellings diverge too — the picker writes `CGC 10 Pristine` where the vocabulary has `CGC 10 Prist.`

**Two further findings that bear directly on the ruling:**
- **Dead code already implements the rejected approach.** A dormant `bucketOf` (`:415–423`) encodes `n ≥ 10 → PSA 10` and `< 8 → PSA 7` — precisely the approximation D-022 records the owner rejecting as statistically dishonest. It must not be revived by accident during the rebuild.
- **The correction modal silently asserts a grader that was never recorded.** Re-opening a `Grade N` transaction forces the grader field to **PSA** (`:522–532`), attributing a company the record never named — directly against ADR-0005's instruction that the interface must not imply the pooled figure is company-neutral.

**Also:** the gallery view shows a holding's value with **no `EST` badge** while the table badges the same holding (`:99–102`, `:121`), under an IRON RULE strip rendered on both. Same number, two different honesty treatments.

### D-013 — Render mode: Interactive Server, WebAssembly, Auto, or per-component?
Coupled to D-014. Interactive Server holds a SignalR circuit per visitor and round-trips every interaction to a residential Pi — weakest exactly where success criterion #1 ("a hiring manager clicks a link") lives. WebAssembly moves the C# into the browser, which cannot open a Postgres connection, forcing D-014 to be yes.

**Sharpened by D-024:** the browser also cannot reach the worker's loopback intake API. So under WebAssembly, *both* data reads and express-visit calls need a server-side component on the Pi — the browser can only talk to that component. Under Interactive Server the app is already server-side and can call `127.0.0.1` directly. This makes render mode and read-API a single decision, not two.

### D-026 — What access does CardStock have to the scraper's eight tables?
**✅ Ruled 2026-08-11 by D-065 / ADR-0001 — read-only, enforced by grants.** Owner, presented with
the consequence that reversing it later is a superuser statement on the Pi rather than a code
change: *"D-026 makes sense to me as it stands."* `cardstock_app` receives `SELECT` in schema
`public` and nothing else. The original framing is kept below for the reasoning trail.

Reading is certain. Writing is **undecided and deliberately left undecided.**

Owner, 2026-08-10: *"CardStock will have to interact with PokemonInvestBatch tables, but I foresee it being only read. However, that is not something to make as a rule."*

**Evidence that pulls toward read-only**, worth weight but not binding on CardStock: ADR-0006 Consequences — "sibling apps speak HTTP to the worker, never SQL to its tables," and it deliberately adds no new DB grants. That is how the scraper's author drew the line from his side.

**Do not** design as though read-only were already a constraint, and **do not** assume write access is available. If a design needs to write, that is a decision to raise here, not a rule to break or a permission to assume.

**Process note:** I twice wrote this into `CLAUDE.md` as a settled architectural boundary before it had been decided. Recorded there under "Expectations are not constraints."

### D-025 — Which CardStock scenarios call which intake endpoint?
Owner, 2026-08-10: "there are specific scenarios where cardstock are going to use these endpoints." Open — the scenarios need enumerating and mapping to `refresh-request` (fire-and-forget) vs `express-visit` (synchronous, gate-bypassing).

**Known so far:** `CardStock Mockup/DESIGN_NOTES.md:54` already specifies one — *"card page visits trigger a fresh scrape"*, with the footer reading "Sales & prices refreshed just now." That is `express-visit` semantics: synchronous, user is waiting.

**Needs deciding alongside it:** whether a public deployment (D-011) requires per-user rate limiting in front of express-visit. The worker's guardrails bound load on the *source site*, but nothing bounds how often an authenticated user can trigger one.

### D-014 — Does a read API exist?
No longer blocked by the spec (see D-005). Forced by WebAssembly; optional under Interactive Server. Still 100% .NET either way — an ASP.NET Core project doing the EF Core query.

**Relevant precedent, verified 2026-08-10:** an HTTP API already exists in the sibling repo — `docs/adr/0006-localhost-intake-api-and-express-visits.md`, "A localhost intake API, with express visits outside the polite gate." It is an *intake/command* surface, not a read surface, and it is loopback-only. So the question of whether CardStock gets a read API is still open, but HTTP plumbing and a decision precedent both already exist. **ADR-0006 has not been read in full** — do that before relying on any detail beyond its title.

### D-039 — A companion .NET console/worker app, and what belongs in it
Raised by the owner 2026-08-10: *"we're gonna have to have a companion console app to run things such as 'screen activity' and maybe update values for this ticker and the others."* Correct, and it absorbs and expands D-015.

**Why it is structurally required, not an optimisation.** `DESIGN_NOTES.md` defines the Home feed as *"the diff log those screens emit on data refresh"* — ENTER/EXIT rows produced by comparing each saved screen's membership against its previous run. "What changed since last time" **cannot** be computed while rendering a page; it requires a prior run to diff against. The feed is batch work by construction.

**Candidate contents:**
- Market index construction (D-004 — no index exists in any form)
- Per-card metric materialization for Screener filters and Charts rows (the old D-015)
- Saved-screen evaluation and feed-row generation
- Ticker aggregates (MARKET / INDEX / NEW 12M HIGHS)
- Per-card sufficiency state against the 2026-09-01 floor (D-033)

**Precedent and consistency:** `PokemonInvestBatch` is already exactly this shape — a .NET worker on a timer under systemd. D-005 explicitly permits it (*"Supporting processes are .NET console/worker apps"*). Mirroring it is consistent across both repos and yields a second portfolio artifact.

**Write scope:** it writes **CardStock's own tables** (index values, metric snapshots, feed rows, sufficiency state) and reads the scraper's. Same boundary as the web app per D-026/D-037, so it needs a role with DML on CardStock tables and `SELECT` on the scraper's.

**Effect on render mode (D-013):** none directly — a separate process writing rows is orthogonal to how a browser talks to the web app. Second-order, it argues **for** static SSR: once the ticker, feed, screen membership, and index are precomputed rows, page rendering is a cheap `SELECT` with no computation on the request path, which is exactly where WebAssembly's benefit is smallest.

**Open:** its schedule and trigger (timer? on crawl completion? both?), whether it is one worker or several, whether it shares a solution with the web app, and how it coordinates with the crawler's own cadence.

---

### D-015 — Shape of the analytics / metric materialization tier
Owner's instinct, 2026-08-10: a separate calculating process, "especially for things like screens." D-004 confirms nothing exists today. Open: what it computes, on what schedule, into what tables, and whether it lives in this repo or `PokemonInvestBatch`. Note the sufficiency framework means it must emit **states** (LOW DATA / LOCKED / UNSTABLE FIT) alongside values, not nulls.

### D-016 — Repo topology
CardStock standalone talking to a read API? Grow `PokemonInvestBatch` to serve HTTP? Monorepo? Entangled with D-013, D-014, D-015 — likely one decision, not four.

### D-017 — Backups
**The irrecoverability half is now verified.** `DATA_MODEL.md:481–485`: "**Unavailable from source, permanently:** historical sales volume; sales beyond the bucket windows; pre-observation census history." And `:399`: population "History begins at each card's first visit (the site publishes no history)." Meanwhile `price_months` backfills whole on any visit (D-002), so it is fully re-crawlable.

So the asymmetry is real: **`sales` and `populations` are the irreplaceable assets; `price_months` is not.**

**And no backup exists.** `grep -riE "backup|pg_dump|wal|restore|snapshot"` across `ops/README.md`, `ops/*.sh`, and `DATA_MODEL.md` returns no backup strategy of any kind — the only hits are unrelated uses of "snapshot" (census) and "Deployment deltas." Run directly 2026-08-10.

**Still open — the decision, not the facts:** whether to add an off-box dump of those two tables. This is the scraper repo's call, not CardStock's, but CardStock's differentiating indicators depend on both, so it belongs on the list. Owner has not ruled.

**Note:** an off-box dump interacts with the 30-day account-deletion promise in `Cardstock Legal.dc.html` once user tables exist. Unverified — I have not read that file.

### D-018 — Code organization
Owner, 2026-08-10: this is a portfolio piece, so structure is a deliverable, not an afterthought. Open: solution layout, project boundaries, where the Blazor app / read API / analytics worker each live, and how that reconciles with D-016 (repo topology).

**Precedent to check first:** `../PokemonInvestBatch` uses a strict one-directional layered structure (Domain / Application / Infrastructure / Worker) with 6 test projects. Mirroring it is likely the right answer and is cheaper than designing fresh — but the specifics have not been read yet.

### D-019 — Code standards
Owner, 2026-08-10. Open: analyzer set and severity, nullable reference types, warnings-as-errors, naming and file-scoped namespaces, formatting enforcement in CI.

**Precedent to check first:** `../PokemonInvestBatch` already carries `.editorconfig` and `Directory.Build.props`. Read both before proposing anything.

### D-020 — GitHub repository: public, documented, with CI
Owner, 2026-08-10: "a fully functional GitHub for this project with documentations, CI, and ADR." Open: repo name and visibility, README shape, what documentation ships, and what the CI workflow runs (build, test, format check, coverage?).

**Note:** this repo was `git init`-ed locally on 2026-08-10 with no remote. Nothing has been pushed.

**Precedent to check first:** `../PokemonInvestBatch/.github/` already has workflows.

### D-021 — ADRs for CardStock
**✅ Resolved 2026-08-11 — by adopting the split this entry itself proposed.** `docs/adr/` exists,
Nygard format, numbered, mirroring the sibling repo, with `docs/adr/README.md` holding the index
table. ADR-0001 and ADR-0002 are the first two. The ledger entry points at the ADR; the ADR holds
the alternatives and consequences and is not edited after acceptance.

Owner, 2026-08-10. Open: format, numbering, and where they live (`docs/adr/` to match the sibling repo is the obvious default).

**Relationship to this ledger — needs a ruling.** They overlap and should not both record the same thing. Proposed split: an ADR is a *considered architectural decision with alternatives weighed and consequences stated* — D-013 through D-016 are all ADR-shaped. The ledger is the faster-moving register of facts, open questions, and small calls. When a ledger entry gets big enough to need alternatives and consequences, it graduates to an ADR and the ledger entry points at it.

---

## Superseded

### S-001 — `HANDOFF.md` §5: "Per-sale ledger (post-seam) · Apr 2025, per card" and "Census snapshots · Jan 2026"
Replaced 2026-08-10 by D-001. The file now carries a dated correction note in §5.

### S-002 — Spec: "no HTTP API for the first-party UI; API design explicitly out of scope"
**Exact text, read directly 2026-08-10** — `CardStock Mockup/uploads/CARDSTOCK_UI_SPEC_v1.md:46`: *"Blazor Web App, Interactive Server rendering; components → services → Postgres directly (no HTTP API for the first-party UI; API design explicitly out of scope)."*

**Not superseded so much as re-read correctly.** Owner, 2026-08-10: "I don't know why it says there can't be an API because an API is one of a couple solutions that we could implement." The parenthetical was a statement about what that document would and would not specify — not a ruling that an API is forbidden. Both the API question (D-014) and the render mode it names in the same breath (D-013) are open.
