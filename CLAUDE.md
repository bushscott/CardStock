# CardStock

A market-data terminal for the Pokémon card aftermarket. Design is complete; implementation has not started. Data comes from the scraper and Postgres database in the sibling repo `../PokemonInvestBatch`.

## The overriding rule: verify everything

Whether clarifying, designing, or debugging — **verify.** Do not relay a claim you have not checked. Do not accept a document's assertion because the document is authoritative-looking. Do not accept a subagent's finding without opening the file yourself. Do not reason from memory of this codebase when you could run the query.

Extra tokens, extra time, and redundant checking are all explicitly acceptable costs (owner, 2026-08-10). Being wrong is not. When in doubt, check it again.

**A receipt covers only the sentence it supports.** The most likely way to break this rule is not to invent a claim outright — it is to quote something real and then write a generalization beside it, letting the citation lend credibility to a sentence it never covered. This has already happened once in this file: an accurate quote from ADR-0006 ("sibling apps speak HTTP to the worker, never SQL to its tables") sat next to an invented rule ("anything that changes scraper state goes over HTTP"), under a heading calling it a hard architectural boundary. Corrected 2026-08-10. If a sentence is broader than its receipt, it is a **Claim**, no matter what sits next to it.

## Hard constraints

- **Blazor.** The frontend is a Blazor application. This is a .NET portfolio piece and the framework is not up for debate. Supporting processes are .NET console/worker apps.
- **The existing Postgres database** in `../PokemonInvestBatch` is the data source. No new datastore.

Everything else in `CardStock Mockup/HANDOFF.md` is open for discussion.

That includes the line most likely to be misread as a prohibition — `uploads/CARDSTOCK_UI_SPEC_v1.md:46`: *"Blazor Web App, Interactive Server rendering; components → services → Postgres directly (no HTTP API for the first-party UI; API design explicitly out of scope)."* Read directly 2026-08-10. That is a **scoping note about what that document covers, not an architectural ruling** — and the owner has since said so explicitly: an API is one of several solutions on the table. Render mode is likewise open. See D-013, D-014, S-002.

## Related repository — `../PokemonInvestBatch`

CardStock has no data of its own. Everything it renders comes from a separate, already-running system in the sibling directory. **That repo is the authority on what data exists; this one is the authority on what the product does with it.**

**What it is:** a .NET 10 batch worker that politely crawls pricecharting.com into PostgreSQL, deployed as a systemd unit on a 16 GB Raspberry Pi 5 (64-bit Raspberry Pi OS, Debian 12/13, SSD), published self-contained for `linux-arm64`. Four source projects in a strictly one-directional reference chain — `Domain ← Application ← Infrastructure ← Worker`, with Domain referencing nothing — plus six test projects. First commit 2026-07-27.

*Receipts (all read directly 2026-08-10):* `Directory.Build.props` (`net10.0`) · `DATA_MODEL.md:89` ("All data comes from pricecharting.com") · `ops/README.md:29` (the Pi spec) · `ops/pokemon-invest-batch.service` (the systemd unit) · `ops/README.md:82` (`-r linux-arm64 --self-contained`) · the four `src/*.csproj` `ProjectReference` sets · `PokemonInvestBatch.slnx` · `git log --reverse`.

*Not verified:* whether the deployed binary matches current HEAD. Treat "what is running right now" as unknown.

**How it stores things**, which shapes everything CardStock reads: history is **append-only and change-only** (ADR-0001). A row exists only when a value *changed*, so absence means "unchanged," not "missing," and a naive `WHERE month = X` returns nothing for most cards in most months. "Latest" means `max(observed_at)` per key, not the newest month. Any read layer that ignores this will compute plausible-looking wrong numbers.

Eight `DbSet`s (`Persistence/PokemonDbContext.cs:8–22`): `sets`, `cards` — mutable catalog; `price_months`, `populations`, `sales` — append-only history; `visits`, `fingerprints`, `parse_failures` — crawler bookkeeping. The three-way grouping is descriptive shorthand for reading convenience; `DATA_MODEL.md` does not use those terms.

### The ownership rule — this is a hard architectural boundary

Each codebase migrates and writes **only its own tables**.

- **CardStock's own tables** (users, binders, holdings, transactions, watchlists, saved screens, and anything else the product invents) belong to CardStock. It migrates them and writes them directly. Normal EF Core, normal CRUD.
- **The scraper's eight tables are read-only to CardStock.** `SELECT` freely. Never `INSERT`, `UPDATE`, `DELETE`, or migrate them.

There is **no write path** into the scraper's tables — not SQL, and not HTTP either. The intake API below is not one; see the warning there.

Verified: `docs/adr/0006`, Consequences — "sibling apps speak HTTP to the worker, never SQL to its tables."

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

Express guardrails already exist (single-flight, 10 s spacing floor, same-card coalescing, and the express fetch stamps the polite gate so the scheduled lane re-spaces around it), so worst-case extra site load is bounded to one request per spacing floor.

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
