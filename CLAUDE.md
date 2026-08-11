# CardStock

A market-data terminal for the Pokémon card aftermarket. Design is complete; implementation has not started. Data comes from the scraper and Postgres database in the sibling repo `../PokemonInvestBatch`.

## The overriding rule: verify everything

Whether clarifying, designing, or debugging — **verify.** Do not relay a claim you have not checked. Do not accept a document's assertion because the document is authoritative-looking. Do not accept a subagent's finding without opening the file yourself. Do not reason from memory of this codebase when you could run the query.

Extra tokens, extra time, and redundant checking are all explicitly acceptable costs (owner, 2026-08-10). Being wrong is not. When in doubt, check it again.

**Expectations are not constraints.** An anticipated shape — "this will probably only ever read those tables" — is not a rule until the owner makes it one. Expectations belong in `DECISIONS.md` as **Open**, never in this file as a boundary. This has already happened twice, both times in the table-access section below, both times by me writing a reasonable inference under a heading that made it sound settled.

**A receipt covers only the sentence it supports.** The most likely way to break this rule is not to invent a claim outright — it is to quote something real and then write a generalization beside it, letting the citation lend credibility to a sentence it never covered. This has already happened once in this file: an accurate quote from ADR-0006 ("sibling apps speak HTTP to the worker, never SQL to its tables") sat next to an invented rule ("anything that changes scraper state goes over HTTP"), under a heading calling it a hard architectural boundary. Corrected 2026-08-10. If a sentence is broader than its receipt, it is a **Claim**, no matter what sits next to it.

## Where things live

**One documentation location: `docs/`.** Nothing authoritative lives outside it except the two control-plane files at this root.

| Path | Holds |
|---|---|
| `CLAUDE.md` (this file) | Rules, hard constraints, document authority. At the root because Claude Code loads it from there |
| `DECISIONS.md` | The ledger — every claim and decision, with status and a re-runnable receipt |
| `docs/README.md` | Index and read order |
| `docs/screens/*.md` | **The build reference**, one per screen |
| `docs/brand.md` | Tokens across all three modes, typography, glyphs, theming, WCAG findings |
| `docs/adr/` | Architecture decision records |
| `docs/CONTRADICTIONS.md` | Temporary backlog register — **deletes itself when worked through** |
| `CardStock Mockup/` | **Frozen prototypes. No markdown, by rule.** |

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

CardStock has no data of its own. Everything it renders comes from a separate, already-running system in the sibling directory. **That repo is the authority on what data exists; this one is the authority on what the product does with it.**

**What it is:** a .NET 10 batch worker that politely crawls pricecharting.com into PostgreSQL, deployed as a systemd unit on a 16 GB Raspberry Pi 5 (64-bit Raspberry Pi OS, Debian 12/13, SSD), published self-contained for `linux-arm64`. Four source projects in a strictly one-directional reference chain — `Domain ← Application ← Infrastructure ← Worker`, with Domain referencing nothing — plus six test projects. First commit 2026-07-27.

*Receipts (all read directly 2026-08-10):* `Directory.Build.props` (`net10.0`) · `DATA_MODEL.md:89` ("All data comes from pricecharting.com") · `ops/README.md:29` (the Pi spec) · `ops/pokemon-invest-batch.service` (the systemd unit) · `ops/README.md:82` (`-r linux-arm64 --self-contained`) · the four `src/*.csproj` `ProjectReference` sets · `PokemonInvestBatch.slnx` · `git log --reverse`.

*Not verified:* whether the deployed binary matches current HEAD. Treat "what is running right now" as unknown.

**How it stores things**, which shapes everything CardStock reads: history is **append-only and change-only** (ADR-0001). A row exists only when a value *changed*, so absence means "unchanged," not "missing," and a naive `WHERE month = X` returns nothing for most cards in most months. "Latest" means `max(observed_at)` per key, not the newest month. Any read layer that ignores this will compute plausible-looking wrong numbers.

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
| `POST /cards/{id}/express-visit` | Synchronous. Runs the visit immediately, bypassing the polite gate, holds the response until commit. 200 parsed · 502 upstream · 422 refused · 504 timeout |
| `GET /healthz` | Liveness |

**Bound to `127.0.0.1` only. No auth, no TLS** — trust comes from the bind address (`ScraperOptions.cs:65`, `Intake/IntakeApi.cs:19–30`).

> **This is not a CRUD surface, and not a write channel.** It exists for two specific scenarios — *"refresh this card soon"* and *"refresh this card now, and tell me when it's done."* Both endpoints take a card id and nothing else; neither accepts data. Do not reach for them as a general way to mutate scraper state, and do not extend them into one. Owner, 2026-08-10: "those two endpoints exist for two very specific scenarios. They do not exist for normal CRUD operations for the database at large."

**The consequence that constrains the frontend:** a browser cannot reach a loopback endpoint on the Pi. Any CardStock code that calls these endpoints must run **server-side, on that machine**. This is a live constraint on the render-mode decision — see D-013 and D-014.

**The spacing floor was removed 2026-08-10 (their ADR-0008).** Express visits no longer wait on each other. What remains in the worker: **single-flight** (one outbound fetch at a time), **same-card coalescing** (concurrent requests for one card ride a single fetch), and `RecordFetchNow` (the express fetch stamps the polite gate so the scheduled lane re-spaces around it).

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
