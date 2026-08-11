# CardStock

[![CI](https://github.com/bushscott/CardStock/actions/workflows/ci.yml/badge.svg)](https://github.com/bushscott/CardStock/actions/workflows/ci.yml)

A market-data terminal for the Pokémon card aftermarket — price history, a screener, charts with
backtesting, and a binder that treats a collection as a portfolio. It reads the archive built by
[PokemonInvestBatch](https://github.com/bushscott/PokemonInvestBatch), a scraper that has collected
**10.3 million price rows** across **91,570 cards** in **788 sets**.

The interesting part is not the charts. It is that this dataset is **young, ragged, and full of
holes** — and the product's central commitment is to say so, on every screen, rather than draw a
smooth line over it.

---

## Status: design complete, implementation just started

Being straight about this, because the repo is public and the alternative is letting a file listing
imply more than exists.

| | State |
|---|---|
| **Design** | Complete. 17 interactive prototypes, 14 screen specifications, 30 indicators specified with formulas and caveats, a full brand system across light/dark/colourblind modes |
| **Architecture** | Settled and **verified running** — two ADRs, applied to the live database and deployed |
| **Implementation** | **Started.** Solution, schema, identity tables, and a deployed API that reads across the schema boundary. No screens yet |
| **Product** | Not usable. There is nothing to click |

So there is no screenshot at the top of this README. What there is instead is a design record, an
architecture proven against a live database rather than asserted, and a decision ledger showing how
both were arrived at.

---

## Why this exists, and how it was built

This is the second half of a portfolio pair. The
[scraper](https://github.com/bushscott/PokemonInvestBatch) gathers the data; this application is
what the data is *for*. That README explains at length why I build this way and what I think the
senior role actually is when a machine writes the code — the short version is that **the judgement
about what to keep is still the job**, and reading fluent, confident, internally consistent, quietly
wrong code is the skill that matters.

This repo has its own version of that story, and it is a sharper one, because here the thing that
was quietly wrong was **a document rather than a diff**.

### The seam that was never there

The design was built around a discontinuity: per-sale history began in **April 2025**, so charts had
to mark that boundary and metrics had to treat data on either side differently. Two documents in the
same folder both marked "Current" disagreed about it — one said April 2025, the other said July
2026 — and nothing had ever checked them against each other. The April date reached an engineering
handoff.

It was false. The scraper's first commit is 2026-07-27, and its own data model says plainly that
sales and census history *"begin at each card's first visit."* There is no April 2025 seam because
there was no scraper in April 2025.

The damage was not one wrong date. It had been compounded into arithmetic:

> A progress indicator read **"16 of 24 months"** — two thirds ready. Measured from the real start
> date it was **1 of 24**. Another read "7 of 12"; it was about 1 of 12.

Those numbers were not sloppy estimates. They were computed correctly from a false premise, which is
why they looked so convincing. A product whose entire differentiator is *never compute on
insufficient data* was **overstating data sufficiency inside its own honesty apparatus**, in the one
place users are told to trust.

Every hand-authored ratio found in this project turned out to be wrong, and **every one of them was
wrong in the direction that overstates readiness**. That is not a coincidence; it is what optimism
looks like when nobody makes it produce a receipt.

The fix was structural rather than a round of corrections — see
[the ledger](#the-ledger) below.

---

## What problem this solves

Pokémon card prices move like a market, but the tooling is a shopping site. You can see today's
price. You cannot easily ask *which cards are behaving unusually*, or *did my instinct actually beat
the market*.

CardStock is built for someone already fluent in technical analysis who treats cards as an asset
class. That has a binding consequence: indicators keep their **real names**, parameters are
**numeric inputs exposed by default** rather than hidden behind an "advanced" door, and density wins
over onboarding. The playground is the point, not a mode.

The deliberate trade: the finance-naive collector is **underserved by design**. Not blocked, not
designed for.

---

## The two rules

Nearly every design decision in this repo traces back to one of these.

### 1. Never smooth over a discontinuity

Two data sources meet at a seam that is **per-card and ragged** — each card's per-sale history
begins at its own first crawler visit, not on a shared date. Charts draw each boundary where it
actually falls. They never blend it, and never draw one shared line across all cards.

The current month is an aggregation of partial data. It renders as a dashed line ending in a hollow
point — **never a projection**.

### 2. Never compute on insufficient data

Every metric has a sufficiency floor. Below it, the metric does **not render a number**. It renders
a *state*, naming the rule it failed and when it will pass:

```
OK   ·   LOW DATA   ·   LOCKED   ·   UNDEFINED window   ·   UNSTABLE FIT
```

Those five are the complete set. Locked controls state their unlock condition and their progress
toward it — and **the denominator is authored while the numerator is computed**, because that is
precisely the arithmetic that went wrong above.

This is expensive. Honest accounting says several headline indicators stay `LOCKED` into 2027, and
no amount of engineering shortens that — the data simply does not exist yet. Shipping the locks
visibly, with real countdowns, was chosen over quietly shipping plausible numbers.

---

## The hard parts

### 1. Absence does not mean missing

The archive is **append-only and change-only**: a row exists only where a value *changed*. So a
naive `WHERE month = '2024-03'` returns nothing for most cards in most months — not because the
data is missing, but because the price did not move. "Latest" means `max(observed_at)` per key, not
the newest month.

Any read layer that ignores this computes plausible-looking wrong numbers, which is the worst
possible failure mode for this particular product.

### 2. Two applications, one database

CardStock owns users, transactions, watchlists, saved screens, and everything its worker will
compute. It owns **no market data** — that belongs to the scraper, which is the only writer to it.
Both live in one PostgreSQL instance on one Raspberry Pi, and the scraper had never shared its
database with anything.

[**ADR-0001**](docs/adr/0001-schema-separation-and-migration-ownership.md) settles it: separate
schemas, separate owner roles, separate migration lineages.

The part worth reading is the mapping. The obvious way to let EF Core read another application's
tables is `ToTable(..., ExcludeFromMigrations())`, which reads as though it closes the door. It does
not. Scaffolding a migration against that mapping still emits foreign keys reaching into the other
schema, and dropping the mapping entirely emits `CreateTable(schema: "public")` — with
**`DropTable(schema: "public")` in `Down()`**, against tables holding data that cannot be rebuilt
from any source.

`ToView` closes it by construction: no DDL, no foreign key, and an attempted write throws before
PostgreSQL is ever asked. That difference is invisible in review and obvious in the generated output,
which is the whole lesson — the scaffolded migration was read rather than trusted.

Three model tests now assert it permanently, and I verified they actually bite by breaking the
mapping on purpose and watching them fail.

### 3. The boundary is enforced, not documented

Documentation that says "CardStock only reads the scraper's tables" is a promise. Grants are a
mechanism. Tested against the live database as the runtime role:

| Statement | Result |
|---|---|
| `SELECT` from `cards` / `sets` / `price_months` | 91,570 · 788 · 10,352,706 rows |
| `INSERT INTO public.sets` | `permission denied for table sets` |
| `UPDATE public.cards` | `permission denied for table cards` |
| `DELETE FROM public.sales` | `permission denied for table sales` |
| `CREATE TABLE public.…` | `permission denied for schema public` |
| `CREATE TABLE cardstock.…` | `permission denied for schema cardstock` |
| `INSERT` / `DELETE` in `cardstock.users` | succeeds |

The last two rows matter together: the running application has full data access in its own schema
and **no schema-modifying rights anywhere**, including over the tables it owns. Migrations are a
deliberate act performed by a human with a different role — the same posture the scraper takes.

### 4. Identity without a JWT in localStorage

[**ADR-0002**](docs/adr/0002-identity-is-a-cookie-backed-by-a-session-row.md): email and password, in
an HttpOnly cookie backed by a session row.

The reasoning is specific rather than fashionable. Listing titles are stored exactly as scraped, so
there is a real XSS surface; any credential JavaScript can read is one an XSS can steal. And with
backups deliberately deferred, account deletion is immediate and permanent — which is only true if
the credential stops working at once, and a self-contained token cannot be revoked before it
expires.

---

## Architecture

```
src/  CardStock.Domain          references nothing
      CardStock.Application     use cases, DTOs, contracts
      CardStock.Infrastructure  EF Core, PostgreSQL, scraper read models
      CardStock.Api             stateless, versioned
      CardStock.Web             Blazor WebAssembly client
      CardStock.Worker          index, metrics, saved-screen evaluation
tests/                          one project per source project
```

| Tier | Choice |
|---|---|
| App — all authenticated screens | Blazor `InteractiveWebAssembly` |
| Marketing — the `/product` prefix | Static SSR |
| Between them | A stateless minimal API |
| Alongside | A .NET worker |

"Stateless" describes the API tier only. State exists — holdings in PostgreSQL, identity in a cookie
sent per request, UI state in the browser. What is excluded is the *server* holding a session in its
own memory, so a deploy never disconnects anyone.

---

## The ledger

[`DECISIONS.md`](DECISIONS.md) is a register of **73 entries** covering every consequential claim
about the data, the stack, and the architecture. Each carries a status:

| Status | Means |
|---|---|
| **Verified** | Checked against code, data, or a document — with a `file:line` or a query you can re-run |
| **Claimed** | Someone asserted it. Not yet checked |
| **Disputed** | Two sources contradict each other |
| **Open** | Needs an answer before something else proceeds |
| **Decided** | An owner's call. Needs a reason and a date, not proof |

The rule that makes it worth keeping: **"Verified" requires a receipt someone else can re-run.** A
subagent's report is *Claimed*. So is a plausible inference. It exists because two documents
contradicted each other for weeks and the wrong one won.

It is also where the unflattering findings live — the false seam, the ratios that overstated
readiness, and roughly 250 contradictions catalogued when the prototypes were audited against their
own documentation.

---

## Running it

Requires [.NET 10](https://dotnet.microsoft.com/) and PostgreSQL 15.

```bash
# 1. Create CardStock's roles and schema (needs the scraper's database to exist)
psql -v ON_ERROR_STOP=1 -f ops/cardstock-postgres-setup.sql

# 2. Apply migrations — as the owner role, never the runtime role
CARDSTOCK_DB="Host=…;Database=pokemon;Username=cardstock_owner;Password=…" \
  dotnet ef database update -p src/CardStock.Infrastructure \
                            -s src/CardStock.Infrastructure \
                            --context CardStockDbContext

# 3. Run the tests
dotnet test

# 4. Run the API
dotnet run --project src/CardStock.Api
```

Nothing migrates at startup — not the API, not the worker. Two units racing one migration history
table at boot is a problem you only get to have once.

Integration tests build and drop a database per test and are skipped unless `CARDSTOCK_TEST_DB` is
set, so a clone with no database still runs the model guards.

Deployment mirrors the scraper: `./ops/publish.sh` produces a self-contained `linux-arm64` build, and
a systemd unit is in [`ops/cardstock-api.service`](ops/cardstock-api.service).

---

## Design decisions

| ADR | Decision |
|---|---|
| [0001](docs/adr/0001-schema-separation-and-migration-ownership.md) | CardStock's tables live in their own schema, and each repo migrates its own |
| [0002](docs/adr/0002-identity-is-a-cookie-backed-by-a-session-row.md) | Identity is email and password, in an HttpOnly cookie backed by a session row |

Design documentation lives in [`docs/`](docs/):

| Path | Holds |
|---|---|
| [`docs/screens/`](docs/screens/) | The build reference — one specification per screen, extracted from the prototypes with line citations |
| [`docs/signals.md`](docs/signals.md) | All 30 indicators with formulas, caveats, and priority — plus the ones users will expect that **cannot be honestly supported**, with the reason for each |
| [`docs/brand.md`](docs/brand.md) | Tokens across light, dark, and colourblind modes; typography; the glyph vocabulary; known WCAG failures |
| [`docs/design-rationale.md`](docs/design-rationale.md) | The frozen design log — the *why*, and the rejected alternatives |

---

## Built with

C# / .NET 10 · Blazor WebAssembly · PostgreSQL · Entity Framework Core · xUnit · Raspberry Pi

## Not affiliated with anyone

Fan-made. Not affiliated with, endorsed by, or connected to Nintendo, The Pokémon Company, Creatures
Inc., GAME FREAK, or PriceCharting. Pokémon and all related names are trademarks of their respective
owners.

All market data is scraped from a third-party source and is **not authoritative**. Nothing here is
financial advice.

## Licence

All rights reserved. The source is here to be read and reviewed; it is not licensed for use.
