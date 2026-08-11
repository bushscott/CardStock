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

Routes: `POST /cards/{id}/refresh-request` (202, fire-and-forget, takes the next crawl slot unless a burn-window-due card owns it), `POST /cards/{id}/express-visit` (synchronous, bypasses the polite gate, 200/502/422/504), `GET /healthz`.

**The ADR names this product explicitly** — "The product this scraper feeds is a trading website. Its web application… will live on the same Raspberry Pi, read the same Postgres." The integration was designed for, not retrofitted.

**Two hard consequences:**

1. **The ownership rule.** Each codebase migrates and writes only its own tables. CardStock's own tables (users, binders, holdings, watchlists, saved screens) are CardStock's to write normally. The scraper's eight tables are **read-only** to CardStock — there is no write path into them, not SQL and not HTTP.

   **Corrected 2026-08-10.** This entry originally read "Mutations go over HTTP," which was my generalization sitting next to a real quote, not something the quote said. The intake API is **not a write channel and not a CRUD surface** — both endpoints take a card id, accept no data, and exist for two specific scenarios. Owner: "those two endpoints exist for two very specific scenarios. They do not exist for normal CRUD operations for the database at large." The originating error is recorded in `CLAUDE.md` under the verify-everything rule, as the worked example of a receipt being stretched past what it covers.
2. **Loopback binding constrains the frontend.** A browser cannot reach `127.0.0.1` on the Pi. Any code calling these endpoints must run server-side on that machine. This bears directly on D-013 and D-014.

**Corrects an earlier claim.** The initial survey flagged express-visit as "an unthrottled outbound amplifier… up to 8,640 fetches/day if scripted." That overstated it: single-flight (at most one express visit in flight, ever), a 10 s spacing floor, same-card coalescing, and `PoliteGate.RecordFetchNow()` are all in place, and the ADR bounds worst case at "one request per spacing floor." The residual concern the ADR itself names is narrower — express can still poke the site once per spacing floor *during a three-strike pause*, with a "refuse express during the pause" toggle noted as the follow-up.

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

### D-062 — Express visits have no spacing floor. Rate limiting is now CardStock's job alone
Owner removed the express spacing floor in `PokemonInvestBatch` on 2026-08-10, recorded there as **ADR-0008 — "express visits have no spacing floor."** Verified directly in the sibling repo: `ExpressSpacingSeconds` and `_lastExpressFetch` no longer appear anywhere in `src`.

**Why it was removed.** Express exists so a human-facing app can get a card NOW. The floor was global — not per-user, not per-card — so a visitor browsing several stale cards waited ~10s **each**. ADR-0006 introduced it as a replacement guardrail for the polite gate it skips, which was correct for the single-operator case it was designed against; it did not survive contact with a public site and ordinary browsing.

**What still exists in the worker** (verified, `ExpressVisitRunner.cs`):
- **Single-flight** — `SemaphoreSlim(1,1)`, `:44`, `:109`, `:163`. One outbound fetch at a time.
- **Same-card coalescing** — `:48`, `:60–80`. Concurrent requests for one card ride a single fetch and all hear the answer.
- **`gate.RecordFetchNow()`** — `:144`. The express fetch stamps the polite gate so the scheduled lane re-spaces around it.
- The shared `CardVisitor` pipeline, so `last_visited_at` still resets (`CardPageWriter.cs:112`) — which the 24h staleness check depends on.

**🚩 The consequence that lands on this repo.** ADR-0006 promised *"worst-case extra site load is bounded: one request per spacing floor."* **That bound is gone.** With single-flight as the only limiter, the ceiling becomes one fetch per fetch-duration — roughly 30–60 requests/minute rather than 6.

**So CardStock is now the sole guardrail** — but the guardrail is narrower than "rate limit users," and that distinction is the decision.

**Legitimate load is self-limiting and must not be throttled.** Owner, 2026-08-10: *"If it's being called a bunch, that means a lot of cards are being updated, and people are using it and wanting to see those cards."* Correct. The 24h gate means a call only fires when a real person opens a genuinely stale card. A human browses 20–60 cards an hour; ten concurrent users is ~10 calls/minute. Throttling that would be the product refusing to do its job.

**And browsing-driven express visits are a feature, not a cost.** They are *demand-weighted crawling* — cards people care about get fresh, cards nobody opens stay stale. That is arguably a better prioritisation signal than the scheduler's own staleness heuristic, and it costs nothing.

**The guardrail is an abuse-shape check, not a throttle.** The only thing producing sustained load the 24h gate cannot absorb is scripted enumeration of card ids — no human pattern, just iteration. Target a limit generous enough that a person browsing hard never notices it (order of a few hundred express calls per account per hour) and enumeration trips it within minutes.

**And the reason is self-interested, not defensive.** The harm from enumeration falls on **PriceCharting**, not on CardStock: it would send them a sustained stream at a rate the crawler on the same box deliberately never approaches. The downside is asymmetric — if they block the address, `sales` and `populations` stop accumulating and cannot be rebuilt from any source (D-017).

**The intended call pattern** (owner, 2026-08-10): on card page load, read `cards.last_visited_at`; if older than 24 hours, call `express-visit`; the visit resets the field; a second viewer sees it fresh and proceeds without a call. Volume is therefore bounded by *distinct stale cards viewed*, not by page views — and same-card races coalesce for free.

**Spec updates queued:** `docs/screens/card.md` (the refresh flow and the missing loading/failure states — express can still return 502/422/504, and the page must render cached data rather than an error).

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

**Still to retire, each needing a harvest first:** `CARDSTOCK_UI_SPEC_v1.md`, the compass research artifact, both copies of `BRAND_BRIEF.md`, `uploads/Brand package creation/README.md`, `HANDOFF.md`, `DESIGN_NOTES.md`, `DISPLAY_VOCABULARY.md`, `BACKTEST_WARNINGS.md`.

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
Owner, 2026-08-10. Open: format, numbering, and where they live (`docs/adr/` to match the sibling repo is the obvious default).

**Relationship to this ledger — needs a ruling.** They overlap and should not both record the same thing. Proposed split: an ADR is a *considered architectural decision with alternatives weighed and consequences stated* — D-013 through D-016 are all ADR-shaped. The ledger is the faster-moving register of facts, open questions, and small calls. When a ledger entry gets big enough to need alternatives and consequences, it graduates to an ADR and the ledger entry points at it.

---

## Superseded

### S-001 — `HANDOFF.md` §5: "Per-sale ledger (post-seam) · Apr 2025, per card" and "Census snapshots · Jan 2026"
Replaced 2026-08-10 by D-001. The file now carries a dated correction note in §5.

### S-002 — Spec: "no HTTP API for the first-party UI; API design explicitly out of scope"
**Exact text, read directly 2026-08-10** — `CardStock Mockup/uploads/CARDSTOCK_UI_SPEC_v1.md:46`: *"Blazor Web App, Interactive Server rendering; components → services → Postgres directly (no HTTP API for the first-party UI; API design explicitly out of scope)."*

**Not superseded so much as re-read correctly.** Owner, 2026-08-10: "I don't know why it says there can't be an API because an API is one of a couple solutions that we could implement." The parenthetical was a statement about what that document would and would not specify — not a ruling that an API is forbidden. Both the API question (D-014) and the render mode it names in the same breath (D-013) are open.
