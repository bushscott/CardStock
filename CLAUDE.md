# CardStock

A market-data terminal for the Pokémon card aftermarket. Design is complete; implementation has not started. Data comes from the scraper and Postgres database in the sibling repo `../PokemonInvestBatch`.

## The overriding rule: verify everything

Whether clarifying, designing, or debugging — **verify.** Do not relay a claim you have not checked. Do not accept a document's assertion because the document is authoritative-looking. Do not accept a subagent's finding without opening the file yourself. Do not reason from memory of this codebase when you could run the query.

Extra tokens, extra time, and redundant checking are all explicitly acceptable costs (owner, 2026-08-10). Being wrong is not. When in doubt, check it again.

## Hard constraints

- **Blazor.** The frontend is a Blazor application. This is a .NET portfolio piece and the framework is not up for debate. Supporting processes are .NET console/worker apps.
- **The existing Postgres database** in `../PokemonInvestBatch` is the data source. No new datastore.

Everything else in `CardStock Mockup/HANDOFF.md` is open for discussion — including the spec's "components → services → Postgres directly, no HTTP API" rule, which is **no longer a hard constraint** (owner, 2026-08-10).

## Related repository — `../PokemonInvestBatch`

CardStock has no data of its own. Everything it renders comes from a separate, already-running .NET 10 scraper that writes to Postgres. That repo is the authority on what data exists; this one is the authority on what the product does with it.

Durable pointers, so this never has to be re-derived:

| What | Where |
|---|---|
| Schema, storage rules, and the "what can never be backfilled" section | `../PokemonInvestBatch/DATA_MODEL.md` |
| Domain vocabulary | `../PokemonInvestBatch/GLOSSARY.md` |
| Architecture decision records | `../PokemonInvestBatch/docs/adr/` |
| Price tier enum (6 values) | `../PokemonInvestBatch/src/PokemonInvestBatch.Domain/Parsing/PriceTier.cs` |
| Grade tier vocabulary (19 values) | `../PokemonInvestBatch/src/PokemonInvestBatch.Domain/Parsing/GradeTierVocabulary.cs` |
| EF Core context — the 8 tables that exist | `../PokemonInvestBatch/src/…/PokemonDbContext.cs` |

That repo also already carries CI (`.github/`), ADRs, `.editorconfig`, and `Directory.Build.props`. **Mirror its conventions rather than inventing new ones** — consistency across the two repos is itself part of the portfolio story. Confirm the specifics before copying; they have not been read into this project yet (see D-018–D-021).

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
