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

| Surface | Tiers | Source |
|---|---|---|
| Price series | 6 | `price_months.tier` |
| Sales ledger | 19 | `sales.grade_tier` |
| Binder holding | user-entered, arbitrarily specific | the user |

The UI's 19 tiers are legitimate wherever they describe a *sale* or a *holding*. The limit binds only where the UI plots or filters on a **price series**. See D-012 for the live question this raises.

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

### D-007 — Verify everything; cost is not a consideration
Owner, 2026-08-10: "whether it's clarifying or debugging, is verify everything. I don't care about extra tokens spent or the extra time or the redundancy."

Encoded as the first section of `CLAUDE.md`.

---

### D-008 — Tooling is superpowers, not GSD
No `.planning/` directory in this repo. Owner, 2026-08-10.

---

## Disputed

### D-009 — `DESIGN_NOTES.md:35` still specifies an "Apr '25 liquidity seam" that D-001 says cannot exist
The line reads: *"Seams: liquidity seam Apr '25 (churn/vol panes), resolution seam Jul '26 amber dashed line on price chart."* Two seams, one of which has no data behind it.

**Unresolved:** is the Apr '25 liquidity seam an error that propagated into `HANDOFF.md`, a different data source that was once planned, or a deliberate design fiction in the mockups? It is currently specified to render in the churn and volume panes.

**Needs:** an owner ruling. Raised 2026-08-10.

---

### D-010 — `HANDOFF.md:112` calls card imagery "100% placeholder slots… the largest open risk," but the images exist
`DATA_MODEL.md:105–109` and §3.9 document ~3.6 GB of real photos at `{ImageDirectory}/{hash}/1600.jpg`, keyed by `cards.image_hash`, with finish-faithful variants per printing.

**Unresolved:** the *data* clearly exists — but the licensing question is untouched, so the risk framing may still be right for the wrong reason. Owner has not ruled on whether to amend the doc.

**Status of the receipt:** agent-reported from `DATA_MODEL.md`; **not yet opened directly.** Per D-007 this needs first-hand verification before it moves to Verified.

---

## Open

### D-011 — Public URL, or private portfolio piece?
The single answer that most reorders everything downstream. Private (Tailscale / localhost / screen-share) drops image licensing, provenance rewrites, rate limiting, and residential-exposure concerns off the critical path entirely. Public gates all of them.

### D-012 — How is a binder holding valued when its tier has no price series?
**Corrected 2026-08-10.** My earlier framing used "CGC 9.5" as the example. That was wrong: the source pools grading companies for grades 1–9.5 and splits only at 10 (ADR-0005), so a CGC 9.5 **does** have a price series — the pooled `Grade9Half`. See D-022.

The tiers genuinely without a price series are:
- **Grades 1–6** — `price_months` carries only 7, 8, 9, 9.5 below 10 (D-003)
- **Every non-PSA 10** — CGC 10, CGC 10 Pristine, BGS 10, BGS 10 Black, SGC 10, TAG 10, ACE 10. `price_months` has exactly one grade-10 tier and it is `Psa10`.

So the open question is narrower and sharper: a user owns a BGS 10 Black. It has sales rows but no price series. Value it at PSA 10? At PSA 10 with a haircut? Leave it unvalued and exclude it from portfolio totals?

**Strong steer from D-022:** the owner has already rejected exactly this move once. Applying a multiplier to approximate an unobserved company price was rejected as "statistically dishonest." Valuing a BGS 10 Black at `Psa10 × factor` is the same decision in a new place. Affects cost basis, P&L, and "vs market index" — the product's stated emotional centre.

### D-013 — Render mode: Interactive Server, WebAssembly, Auto, or per-component?
Coupled to D-014. Interactive Server holds a SignalR circuit per visitor and round-trips every interaction to a residential Pi — weakest exactly where success criterion #1 ("a hiring manager clicks a link") lives. WebAssembly moves the C# into the browser, which cannot open a Postgres connection, forcing D-014 to be yes.

### D-014 — Does a read API exist?
No longer blocked by the spec (see D-005). Forced by WebAssembly; optional under Interactive Server. Still 100% .NET either way — an ASP.NET Core project doing the EF Core query.

**Relevant precedent, verified 2026-08-10:** an HTTP API already exists in the sibling repo — `docs/adr/0006-localhost-intake-api-and-express-visits.md`, "A localhost intake API, with express visits outside the polite gate." It is an *intake/command* surface, not a read surface, and it is loopback-only. So the question of whether CardStock gets a read API is still open, but HTTP plumbing and a decision precedent both already exist. **ADR-0006 has not been read in full** — do that before relying on any detail beyond its title.

### D-015 — Shape of the analytics / metric materialization tier
Owner's instinct, 2026-08-10: a separate calculating process, "especially for things like screens." D-004 confirms nothing exists today. Open: what it computes, on what schedule, into what tables, and whether it lives in this repo or `PokemonInvestBatch`. Note the sufficiency framework means it must emit **states** (LOW DATA / LOCKED / UNSTABLE FIT) alongside values, not nulls.

### D-016 — Repo topology
CardStock standalone talking to a read API? Grow `PokemonInvestBatch` to serve HTTP? Monorepo? Entangled with D-013, D-014, D-015 — likely one decision, not four.

### D-017 — Backups
Claimed (unverified): `sales` and `populations` cannot be reconstructed if lost — the source publishes no history — while `price_months` is fully re-crawlable. If true, a nightly off-box dump of two tables is cheap insurance against losing the only irreplaceable asset in the project. **Needs first-hand verification per D-007.**

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
Owner, 2026-08-10. Open: format, numbering, and where they live (`docs/adr/` to match the sibling repo is the obvious default).

**Relationship to this ledger — needs a ruling.** They overlap and should not both record the same thing. Proposed split: an ADR is a *considered architectural decision with alternatives weighed and consequences stated* — D-013 through D-016 are all ADR-shaped. The ledger is the faster-moving register of facts, open questions, and small calls. When a ledger entry gets big enough to need alternatives and consequences, it graduates to an ADR and the ledger entry points at it.

---

## Superseded

### S-001 — `HANDOFF.md` §5: "Per-sale ledger (post-seam) · Apr 2025, per card" and "Census snapshots · Jan 2026"
Replaced 2026-08-10 by D-001. The file now carries a dated correction note in §5.

### S-002 — Spec §1.4: "no HTTP API for the first-party UI; API design explicitly out of scope"
Relaxed by the owner 2026-08-10. See D-005, D-014.
