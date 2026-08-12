# CardStock

A market-data terminal for the Pokémon card aftermarket. Design is complete; implementation has not started. Data comes from the scraper and Postgres database in the sibling repo `../PokemonInvestBatch`.

## What CardStock is, and the two rules it is built on

A market-data application for the Pokémon card aftermarket: price history, a screener, charts with backtesting, and a binder that treats a collection as a portfolio. Fan-made, not affiliated with Nintendo, The Pokémon Company, or Creatures Inc.

### Success criteria, ranked — these drive every priority

1. **Portfolio piece.** A hiring manager clicks a link, spends 90 seconds, and is impressed. The first-90-seconds path gets polish priority.
2. **Personal profit.** The owner finds cards via his own signals and the Binder proves he beat the market. **Backtest + Binder-vs-benchmark are the product's emotional centre.**
3. **Someday sellable.** Deferred until proven — but the schema is multi-tenant from day one, so commercialisation is a config change, not a rewrite (D-034).

### The persona

**The finance-fluent card investor.** Treats Pokémon cards as an asset class and is already fluent in technical analysis. Wants to *tune parameters and compare results* — **the playground is the point, not an advanced mode.** Secondary visitor: the hiring manager, who never registers. **Deliberately underserved:** the finance-naive collector — not blocked, not designed for.

Binding consequences: indicators use **real names**; parameters are **numeric inputs exposed by default**, never hidden behind an "advanced" door; **density over onboarding**; explainers only for this dataset's quirks.

### The two rules

The product's distinguishing commitment is **honesty about data**. Two rules follow from it, and nearly every design decision in `DECISIONS.md` traces back to one of them. Harvested from `HANDOFF.md` §1 before that file was retired (D-054); they existed nowhere else.

### 1. Never smooth over a discontinuity

Two data sources meet at a seam that is **per-card and ragged** — each card's per-sale history begins at its own first crawler visit. Charts draw each boundary where it actually falls; they never blend it, and never draw one shared line across all cards (D-001, D-061).

The current month is an aggregation of partial data. It renders as a dashed line ending in a hollow point — **never a projection.**

### 2. Never compute on insufficient data

Every metric has a sufficiency floor. Below it, the metric does **not render a number**. It renders a *state*, naming the rule it failed and when it will pass:

`OK` · `LOW DATA` · `LOCKED` · `UNDEFINED window` · `UNSTABLE FIT`

Those five are the complete set (D-056 collapsed a sixth into `LOW DATA`). Locked controls state their unlock condition and progress. All of it is measured from the 2026-09-01 floor (D-033), and **the denominator is authored while the numerator is computed** — every hand-written ratio found in this project was wrong in the direction that overstates readiness (D-061).

### The copy posture that follows

Precise numbers over adjectives. No hype. No exclamation marks. Colour never carries meaning alone — every state pairs a hue with a glyph, and colourblind mode swaps hue only (`docs/brand.md` §4).

## The overriding rule: verify everything

Whether clarifying, designing, or debugging — **verify.** Do not relay a claim you have not checked. Do not accept a document's assertion because the document is authoritative-looking. Do not accept a subagent's finding without opening the file yourself. Do not reason from memory of this codebase when you could run the query.

Extra tokens, extra time, and redundant checking are all explicitly acceptable costs (owner, 2026-08-10). Being wrong is not. When in doubt, check it again.

**Expectations are not constraints.** An anticipated shape — "this will probably only ever read those tables" — is not a rule until the owner makes it one. Expectations belong in `DECISIONS.md` as **Open**, never in this file as a boundary. This has already happened twice, both times in the table-access section below, both times by me writing a reasonable inference under a heading that made it sound settled.

**A receipt covers only the sentence it supports.** The most likely way to break this rule is not to invent a claim outright — it is to quote something real and then write a generalization beside it, letting the citation lend credibility to a sentence it never covered. This has already happened once in this file: an accurate quote from ADR-0006 ("sibling apps speak HTTP to the worker, never SQL to its tables") sat next to an invented rule ("anything that changes scraper state goes over HTTP"), under a heading calling it a hard architectural boundary. Corrected 2026-08-10. If a sentence is broader than its receipt, it is a **Claim**, no matter what sits next to it.

## Where things live

**One documentation location: `docs/`.** Nothing authoritative lives outside it except the two control-plane files at this root — `CLAUDE.md` and `DECISIONS.md`. `README.md` also sits at the root but is **derived, not authoritative**: it restates for a public audience what the documents below already establish, so when it disagrees with them it is the one that is wrong.

| Path | Holds |
|---|---|
| `README.md` | The repo's front door, for a public audience. At the root because GitHub renders it there. Descriptive, never authoritative — every figure in it is sourced from below, and it is the one document written for people who have read none of the others |
| `CLAUDE.md` (this file) | Rules, hard constraints, document authority. At the root because Claude Code loads it from there |
| `DECISIONS.md` | The ledger — every claim and decision, with status and a re-runnable receipt |
| `docs/README.md` | Index and read order |
| `docs/screens/*.md` | **The build reference**, one per screen |
| `docs/brand.md` | Tokens across all three modes, typography, glyphs, theming, WCAG findings, brand prohibitions |
| `docs/signals.md` | All 29 indicators with formulas, caveats and priority, plus what cannot be honestly supported |
| `docs/design-rationale.md` | Frozen Jul–Aug 2026 design log — the *why* and the rejected alternatives |
| `docs/adr/` | Architecture decision records |
| `docs/CONTRADICTIONS.md` | Temporary backlog register — **deletes itself when worked through** |
| `CardStock Mockup/` | **Frozen prototypes. No markdown, by rule.** ✅ achieved 2026-08-10 |

**The ledger records *why* and *when*. The screen specs record *what to build*.** A decision that changes a screen belongs in both.

**Do not add documentation anywhere else.** If a new document seems necessary, it almost certainly belongs as a section of an existing one — the reason this structure exists is that the project previously had eleven mutually contradictory markdown files across four directories.

## Document authority — which source wins

Owner's ruling, 2026-08-10: **"The mockups and the PokemonInvestBatch scraper codebase are the absolute truth."** Everything else is derived and may be stale. When two sources disagree, the higher tier wins — do not average them, and do not pick the one that reads more confidently.

**Tier 1 — authoritative. Cite these.**
- **`docs/screens/*.md` — the build reference.** One spec per screen, extracted directly from the prototypes on 2026-08-10 with line citations back to them, and carrying the data corrections the prototypes get wrong. **Build from these.** They inherit Tier 1 authority because they are a cited extraction of Tier 1, and they are the only design record being kept current.
- `CardStock Mockup/*.dc.html` — the prototypes. The **source** those specs were extracted from, and the visual/behavioural tiebreak for anything a spec is silent on. **Frozen as of 2026-08-10** — see below.
- `../PokemonInvestBatch/` — source, schema, and ADRs. Authoritative for everything about the data.

**The prototypes are frozen. Do not edit them.** They are the reference everything else was verified against, so editing them moves the ground under that verification — and they are scheduled for replacement by the Blazor build, making edits throwaway work. Corrections live in `docs/screens/` and `DECISIONS.md`.

**Their copy contains known-false claims about the data.** Roughly 25 of them, every instance enumerated in `docs/CONTRADICTIONS.md` **Class B** — the "Apr '25 seam" in seven marketing locations, "sale counts," "back to August 2023," "refreshes daily," and more. **Never quote prototype copy as a fact about the data.** The prototypes are authoritative that the page *says* something, never that it is true.

### The maintenance rule that makes this work

Freezing the prototypes moves the record of truth to `docs/screens/`. That only holds if the specs are actually maintained — otherwise they decay into another stale tier and nothing has been gained.

**So: every decision that changes a screen must land in that screen's spec, not only in `DECISIONS.md`.** The ledger records *why* and *when*; the screen spec records *what to build*. A decision recorded in only one of them is a decision that will be missed.

Each spec's §8 (Contradictions) is the audit trail — when a decision resolves one of its rows, update the row rather than deleting it, so the reasoning survives the way an ADR's does.

**Tier 2 — current and useful, but derived.** Trustworthy about the design, verify anything they say about data (see the provenance banner in `HANDOFF.md`).
- `HANDOFF.md`, `DESIGN_NOTES.md`, `DISPLAY_VOCABULARY.md`

**Tier 3 — historical. Do not cite as current.**
- `uploads/CARDSTOCK_UI_SPEC_v1.md` — approved 2026-08-01, superseded in parts by design work that followed. `HANDOFF.md` calls it "Stale in parts" and §4 lists the known deltas.
- `uploads/PROJECT_LOG.md`, `BRAND_BRIEF.md` — decision history. Reasoning worth harvesting, conclusions frequently reversed.

**`DECISIONS.md` overrides all three tiers** where it records an owner decision, because it is the only document being kept current deliberately.

**When two Tier 1 sources disagree, the hierarchy cannot resolve it — escalate.** Do not pick the one that reads better, and do not average them. Log it in `DECISIONS.md` as needing an owner ruling. This is not hypothetical: `Cardstock Legal.dc.html:57` promises account data is removed within 30 days while `Cardstock Profile.dc.html:181` promises deletion is immediate and permanent (D-043).

**Practical consequence:** several open contradictions are settleable by opening the HTML rather than debating documents. If `DESIGN_NOTES.md` says a screen does X and the prototype does Y, the answer is Y and the note is stale.

## Hard constraints

- **Blazor.** The frontend is a Blazor application. This is a .NET portfolio piece and the framework is not up for debate. Supporting processes are .NET console/worker apps.
- **The existing Postgres database** in `../PokemonInvestBatch` is the data source. No new datastore.

## Architecture — settled (D-063)

| Tier | Choice |
|---|---|
| App — all authenticated screens | **`InteractiveWebAssembly`** |
| Marketing — the `/product` prefix (D-058) | **Static SSR** |
| Between them | **A stateless minimal API** |
| Alongside | **A .NET worker** — index, metrics, screen evaluation (D-039) |

```
src/  CardStock.Domain          (references nothing)
      CardStock.Application     (use cases, DTOs, contracts)
      CardStock.Infrastructure  (EF Core, Postgres, intake client)
      CardStock.Api             (stateless, versioned)
      CardStock.Web             (WASM client)
      CardStock.Worker
tests/ one project per source project
```

**"Stateless" describes the API tier only.** State exists — holdings and watchlists in Postgres, identity in a cookie or token sent per request, UI state (open panes, column widths, active tab) in the browser. What is excluded is the *server* holding a session in its own memory, so a deploy never disconnects anyone.

**Consequence:** the browser cannot reach the worker's loopback intake API. **`express-visit` is proxied through `CardStock.Api`**, which is also the natural place to enforce D-062's abuse-shape limit.

Everything else in `CardStock Mockup/HANDOFF.md` is open for discussion.

That includes the line most likely to be misread as a prohibition — `uploads/CARDSTOCK_UI_SPEC_v1.md:46`: *"Blazor Web App, Interactive Server rendering; components → services → Postgres directly (no HTTP API for the first-party UI; API design explicitly out of scope)."* Read directly 2026-08-10. That is a **scoping note about what that document covers, not an architectural ruling** — and the owner has since said so explicitly: an API is one of several solutions on the table. Render mode is likewise open. See D-013, D-014, S-002.

## Related repository — `../PokemonInvestBatch`

CardStock owns its own data — users, transactions, watchlists, saved screens, and everything its worker computes, including the market index that exists nowhere today (ADR-0001, D-004). What it does **not** own is *market* data: prices, sales, populations and the card catalog all come from the separate, already-running system in the sibling directory. **That repo is the authority on what data exists; this one is the authority on what the product does with it.**

**What it is:** a .NET 10 batch worker that politely crawls pricecharting.com into PostgreSQL, deployed as a systemd unit on a 16 GB Raspberry Pi 5 (64-bit Raspberry Pi OS, Debian 12/13, SSD), published self-contained for `linux-arm64`. Four source projects in a strictly one-directional reference chain — `Domain ← Application ← Infrastructure ← Worker`, with Domain referencing nothing — plus six test projects. First commit 2026-07-27.

*Receipts (all read directly 2026-08-10):* `Directory.Build.props` (`net10.0`) · `DATA_MODEL.md:89` ("All data comes from pricecharting.com") · `ops/README.md:29` (the Pi spec) · `ops/pokemon-invest-batch.service` (the systemd unit) · `ops/README.md:82` (`-r linux-arm64 --self-contained`) · the four `src/*.csproj` `ProjectReference` sets · `PokemonInvestBatch.slnx` · `git log --reverse`.

*Not verified:* whether the deployed binary matches current HEAD. Treat "what is running right now" as unknown.

**How it stores things**, which shapes everything CardStock reads: history is **append-only and change-only** (ADR-0001). A row exists only when a value *changed*. "Latest" means `max(observed_at)` per key, not the newest month. Any read layer that ignores this will compute plausible-looking wrong numbers.

**But "absence means unchanged" is not one rule across both histories, and reading it that way fabricates prices.** Corrected 2026-08-11 after reading the crawler's parser and write planner directly.

| | `populations` | `price_months` |
|---|---|---|
| The cell | `(grader, grade)` — value observed over time | `(tier, month)` — the month **is** part of the key |
| A cell with no row | **Was zero at every observation** (`DATA_MODEL.md:53–56`) | **The site published no point there.** A real gap |
| Between two stored rows | Flat — that is the storage contract | Says nothing about other months; each month is its own cell |

*Receipts:* `Domain/Parsing/CardDetailParser.cs:318–331` stores exactly the points the site's chart contains, with no filling or filtering. `Infrastructure/Persistence/ChangeOnlyPlanner.cs:22` compares per `(tier, month)` cell against the last stored value, with "never observed" defaulting to zero — so a first visit writes every nonzero cell the site published. `DATA_MODEL.md:176–180`: a first visit backfills the whole chart; afterwards "a typical visit adds 0–2 rows (the current month moved); closed months carry exactly one row forever."

**So on the month axis, gaps are gaps.** Carrying a price forward across a month with no row invents a number the source never published. `DATA_MODEL.md:53–56`'s "history between two stored rows is flat" is written about `populations` and does not transfer.

**Any month can revise, including closed ones.** `DATA_MODEL.md:110` claims *"Closed months are immutable server-side; only the current month revises between visits"* and `:179` says closed months carry *"exactly one row forever."* **Both are false** — verified 2026-08-12 (D-078): card 630437 restated its July prices on 4 August, after July had closed. So the PK ending in `observed_at` is load-bearing for **every** month, and a read that resolves latest-per-key only for the newest month will silently return superseded prices.

*Not verified:* how sparse the month axis actually is. `price_months` holds 10,352,706 rows over 91,570 cards (D-071) — **113 per card**, against 408 for a dense six-tier backfill to Dec 2020. Either series are genuinely sparse or much of the corpus is uncrawled, and the two imply different read layers. One query settles it: `SELECT count(*) FILTER (WHERE last_visited_at IS NULL), count(*) FROM cards WHERE delisted_at IS NULL AND not_a_card_at IS NULL;`

Eight `DbSet`s (`Persistence/PokemonDbContext.cs:8–22`): `sets`, `cards` — mutable catalog; `price_months`, `populations`, `sales` — append-only history; `visits`, `fingerprints`, `parse_failures` — crawler bookkeeping. The three-way grouping is descriptive shorthand for reading convenience; `DATA_MODEL.md` does not use those terms.

### Table access — the expected shape, not a rule

**CardStock's own tables** — users, binders, holdings, transactions, watchlists, saved screens, and whatever else the product invents — belong to CardStock. It migrates and writes them directly.

**The scraper's eight tables: reading is certain, writing is undecided.** The owner expects CardStock will only ever read them, and has deliberately declined to make that a rule (2026-08-10: *"I foresee it being only read. However, that is not something to make as a rule."*). So do not design around read-only as though it were a constraint, and do not assume write access is available either. Open decision — **D-026**.

**What is verified is the sibling repo's position, not CardStock's obligation.** ADR-0006's Consequences say "sibling apps speak HTTP to the worker, never SQL to its tables," and note that it needs no new DB grants. That is real evidence of how the scraper's author drew the boundary and it deserves weight — but adopting it as binding on CardStock is a decision to make, not a fact to inherit.

Separately and firmly: the intake API is not a write channel for this or anything else — see below.

### The intake API — built for this app

The worker hosts a minimal HTTP API in-process, and ADR-0006 was written anticipating CardStock: *"The product this scraper feeds is a trading website. Its web application… will live on the same Raspberry Pi, read the same Postgres, and sometimes need a card's data refreshed ahead of its normal turn."*

| Route | Semantics |
|---|---|
| `POST /cards/{id}/refresh-request` | Fire-and-forget. Stamps `cards.refresh_requested_at`, returns 202. Card takes the next crawl slot unless a burn-window-due card owns it. 404 unknown · 409 delisted/not-a-card |
| `POST /cards/{id}/express-visit` | Synchronous. Runs the visit immediately, bypassing the polite gate, holds the response until commit. 200 parsed · 404 unknown · 409 **not-a-card only** · 422 refused · 500 errored · 502 upstream. **No 504 — there is no timeout** (`IntakeApi.cs:52–74`, D-076). Delisted cards *are* visitable here, deliberately (`ExpressVisitRunner.cs:115–121`) |
| `GET /healthz` | Liveness |

**Bound to `127.0.0.1` only. No auth, no TLS** — trust comes from the bind address (`ScraperOptions.cs:65`, `Intake/IntakeApi.cs:19–30`).

> **This is not a CRUD surface, and not a write channel.** It exists for two specific scenarios — *"refresh this card soon"* and *"refresh this card now, and tell me when it's done."* Both endpoints take a card id and nothing else; neither accepts data. Do not reach for them as a general way to mutate scraper state, and do not extend them into one. Owner, 2026-08-10: "those two endpoints exist for two very specific scenarios. They do not exist for normal CRUD operations for the database at large."

**The consequence that constrains the frontend:** a browser cannot reach a loopback endpoint on the Pi. Any CardStock code that calls these endpoints must run **server-side, on that machine**. This is a live constraint on the render-mode decision — see D-013 and D-014.

**The spacing floor was removed 2026-08-10 (their ADR-0008).** Express visits no longer wait on each other. What remains in the worker: **same-card coalescing** (concurrent requests for one card ride a single fetch) and `RecordFetchNow` (the express fetch stamps the polite gate so the scheduled lane re-spaces around it).

**There is no single-flight** — this file claimed one until 2026-08-11 and it was never true post-ADR-0008. `ExpressVisitRunner.cs:26`: *"in parallel with any other express visit, with no floor, no queue, and no timeout."* The only `SemaphoreSlim` in the sibling's `src/` is `PoliteGate.cs:13`, which express bypasses. **Express fetches are concurrent and unbounded** (D-076). A hung upstream returns 502 only after `HttpClient`'s 60-second cap (`Worker/Program.cs:80`), so no CardStock render may ever block on an express call.

> **CardStock is now the only thing bounding express load.** ADR-0006's "worst-case extra site load is bounded to one request per spacing floor" no longer holds. Per-user rate limiting in front of any express call is **required, not optional** — see D-037 and D-062.

Durable pointers, so this never has to be re-derived:

| What | Where |
|---|---|
| Schema, storage rules, and the "what can never be backfilled" section | `../PokemonInvestBatch/DATA_MODEL.md` |
| Domain vocabulary | `../PokemonInvestBatch/GLOSSARY.md` |
| Architecture decision records | `../PokemonInvestBatch/docs/adr/` |
| Price tier enum — 6 values: `Ungraded, Grade7, Grade8, Grade9, Grade9Half, Psa10` | `…/src/PokemonInvestBatch.Domain/Parsing/PriceTier.cs:10–18` |
| Grade tier vocabulary — 19 values: `Ungraded`, `Grade 1`–`Grade 9`, `Grade 9.5`, `PSA 10`, `CGC 10`, `CGC 10 Prist.`, `BGS 10`, `BGS 10 Black`, `SGC 10`, `TAG 10`, `ACE 10` | `…/src/PokemonInvestBatch.Domain/Parsing/GradeTierVocabulary.cs` |
| EF Core context — the 8 `DbSet`s that exist | `…/src/PokemonInvestBatch.Infrastructure/Persistence/PokemonDbContext.cs:8–22` |

**Mirror its conventions rather than inventing new ones** — consistency across the two repos is itself part of the portfolio story. Verified 2026-08-10:

- `Directory.Build.props` — `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, **`TreatWarningsAsErrors=true`**, `InvariantGlobalization=true`
- `.editorconfig` — 4-space C#, 2-space for project/config files, LF, file-scoped namespaces at `warning`, explicit accessibility modifiers at `warning`
- `.github/workflows/ci.yml` — restore → build → test → `dotnet format --verify-no-changes`, with a Postgres 15 service container pinned to the Pi's version because tests assert on generated SQL
- `docs/adr/` — Michael Nygard format, numbered, **never edited after the fact**; a reversal gets a new ADR that supersedes the old one, and `README.md` holds the index table
- `.slnx` solution format, `src/` and `tests/` folders, one test project per source project

**Caution:** that repo's documentation is authoritative about the scraper but has already been shown to disagree with CardStock's design docs (see D-001). Verify across both, never from one.

## The ledger — read this before asserting anything

`DECISIONS.md` at this root is the running register of what is true about this project and what has been decided.

1. Any consequential claim about the data, the stack, or the architecture goes in `DECISIONS.md`.
2. **Never write "Verified" without a receipt** — a `file:line` or a SQL query someone else can re-run. A subagent's report is **Claimed**, not Verified. So is a plausible inference.
3. Design rulings about the *interface* stay in `CardStock Mockup/DESIGN_NOTES.md`, which is good at that job. The ledger is for facts, data, and architecture.
4. In long sessions, append tersely as things settle and consolidate at the end. This is note-taking, not paperwork — don't stop a conversation to file an entry.

Why this exists: `HANDOFF.md` asserted the per-sale ledger began Apr 2025 while `DESIGN_NOTES.md:41` said Jul '26. Two "Current" documents in the same folder, contradicting each other, and the wrong one reached an engineering handoff — because nothing ever checked them against each other.

## Tooling

Use the **superpowers** skills (`brainstorming` → `writing-plans` → `executing-plans` / `subagent-driven-development`, with `test-driven-development` inside). Do not use GSD (`gsd-*`) and do not create a `.planning/` directory here.
