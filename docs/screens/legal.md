# Screen spec — Privacy & Terms (`legal`)

**Source of truth:** `CardStock Mockup/Cardstock Legal.dc.html` (75 lines), read directly 2026-08-10. Every quote below carries its line number in that file. Per `CLAUDE.md`, the prototype is Tier 1 and authoritative for **what the page says**; §6 audits whether what it says is **true**.

---

## 1. Identity

| Field | Value |
|---|---|
| Screen label | `Privacy & Terms` — `data-screen-label="Privacy &amp; Terms"` (line 26) |
| `<h1>` | "Privacy &amp; Terms" (line 43) |
| Prototype file | `Cardstock Legal.dc.html` |
| Proposed route | `/legal` |
| In-page anchors | `#privacy` (line 51), `#terms` (line 61) |
| Anchor link targets | `href="#privacy"` (line 47), `href="#terms"` (line 48) |
| Outbound link | `Cardstock About Data.dc.html` → "About our data" (line 65) |
| Nav entry | **None.** The page is not in the primary nav (lines 30–36); it is reached from elsewhere — `Cardstock Home.dc.html` contains 2 references to this file |

**Purpose.** A single public page carrying both the privacy policy and the terms of use, written to be short enough to read. The subtitle states the intent explicitly: *"Short, in plain language, and true. Last updated August 2026."* (line 44).

**Public-facing risk.** This is one of two pages on the site that makes binding factual and legal promises to users rather than describing product behaviour. Copy changes here are not cosmetic.

---

## 2. Layout

Single centred column, no sidebar, no footer.

```
sticky nav (48px)                                  line 28
└─ logo+wordmark · Home Screener Charts Binder Browse · spacer · search · avatar "O"
container  max-width 820px, margin 0 auto, padding 32px 24px 80px    line 42
├─ h1  "Privacy & Terms"                           line 43
├─ subtitle (14.5px, --mut2)                       line 44
├─ pill row (flex, wrap, gap 6px, mb 28px)         line 46
│   ├─ [Privacy]        → #privacy                 line 47
│   └─ [Terms of use]   → #terms                   line 48
├─ section#privacy   card, mb 14px                 lines 51–59
│   ├─ h2 "Privacy"                                line 52
│   └─ 4 paragraphs, each led by a bold run-in label
└─ section#terms     card, no bottom margin        lines 61–70
    ├─ h2 "Terms of use"                           line 62
    └─ 5 paragraphs, each led by a bold run-in label
```

**Shared tokens and metrics** (both sections identical):

| Property | Value | Line |
|---|---|---|
| Section card | `background: var(--card, #FFFFFF)`, `border: 1px solid var(--line, #E4E4E0)`, `border-radius: 8px`, `padding: 20px 22px` | 51, 61 |
| `scroll-margin-top` | `62px` on both sections — clears the 48px sticky nav with 14px breathing room | 51, 61 |
| h1 | `'Inter Tight'`, 27px, 700, `margin: 0 0 6px` | 43 |
| h2 | `'Inter Tight'`, 18.5px, 700, `margin: 0 0 10px` | 52, 62 |
| Body copy | 14.5px, `line-height: 1.6`, `color: var(--mut, #5B5B57)` | 53, 63 |
| Run-in label | `<strong style="color: var(--ink, #1C1C1E);">` — bold, full-ink, inline, ends with a period | 54–57, 64–68 |
| Pill | 13px/600, `border-radius: 99px`, `padding: 4px 12px`, `background: var(--card)` | 47–48 |
| Page bg | `var(--bg, #FAFAF7)`; `min-height: 100vh`; flex column | 26 |

**Note on section spacing:** `#privacy` carries `margin-bottom: 14px` (line 51); `#terms` has `margin: 0` implied by omission (line 61) — it is the last block on the page.

**No footer.** The document ends at the container close (line 71). This matters — see §6, the "footer stamp on every page" claim on the About Data page.

---

## 3. Content inventory

Every user-visible string, quoted exactly.

### 3.1 Chrome

| Line | Text |
|---|---|
| 29 | Wordmark "Cardstock", `aria-label="Cardstock home"`, links to `Cardstock Home.dc.html` |
| 31–35 | Nav: "Home", "Screener", "Charts", "Binder", "Browse" |
| 38 | `<cardstock-search></cardstock-search>` custom element |
| 39 | Avatar "O", `aria-label="Account"`, `title="Profile &amp; settings"`, → `Cardstock Profile.dc.html` |

### 3.2 Header

| Line | Text |
|---|---|
| 43 | "Privacy &amp; Terms" |
| 44 | "Short, in plain language, and true. Last updated August 2026." |
| 47 | "Privacy" (pill) |
| 48 | "Terms of use" (pill) |

### 3.3 Privacy section (lines 51–59)

| Line | Label | Claim (quoted) |
|---|---|---|
| 52 | — | "Privacy" |
| 54 | **What we store.** | "Your account email, your binder (positions, cost basis, transactions), your watchlists and saved screens, and display preferences (theme, color-vision mode). That's the list." |
| 55 | **What we don't do.** | "We don't sell or share your data, run ads, or use third-party trackers. Analytics are limited to aggregate, anonymous usage counts. Your binder is visible to you alone; no aggregate we publish can be traced back to a person's holdings." |
| 56 | **Cookies &amp; local storage.** | "We use local storage for sign-in sessions and preferences — nothing that follows you around the web." |
| 57 | **Deletion.** | "Delete your account from Profile &amp; settings and everything above is removed within 30 days. Export your binder as CSV first if you want a copy." |

**The stored-data enumeration, itemised** (line 54) — this is a closed list, sealed by "That's the list.":
1. account email
2. binder — positions, cost basis, transactions
3. watchlists
4. saved screens
5. display preferences — theme, color-vision mode

### 3.4 Terms section (lines 61–70)

| Line | Label | Claim (quoted) |
|---|---|---|
| 62 | — | "Terms of use" |
| 64 | **Not financial advice.** | "Cardstock describes what the market did, not what it will do. Signals, screens, and backtests are research tools; decisions and their outcomes are yours." |
| 65 | **Data accuracy.** | "We work hard on data quality (see [About our data]), but sales records and census reports contain errors upstream of us. The service is provided as-is, without warranty of accuracy or availability." |
| 66 | **Fair use.** | "Your account is for you. Don't scrape at volume, resell our data, or probe other users' information. We may suspend accounts that do." |
| 67 | **Intellectual property.** | "Cardstock is a fan-made project, not affiliated with, endorsed by, or sponsored by Nintendo, The Pokémon Company, Creatures Inc., or any grading company or marketplace. Pokémon names and card references are used for identification only; all trademarks belong to their owners." |
| 68 | **Changes.** | "If these terms change materially, we'll say so on this page and date it — no silent edits. That's how we treat charts, and how we treat this." |

---

## 4. States / interactions

The page is **almost entirely static**. There is no data binding, no conditional rendering, no loading state, and no empty state in the prototype.

| Interaction | Behaviour | Line |
|---|---|---|
| Pill "Privacy" | Same-page anchor jump to `#privacy`; target has `scroll-margin-top: 62px` so the h2 clears the sticky nav | 47, 51 |
| Pill "Terms of use" | Same-page anchor jump to `#terms`; same scroll margin | 48, 61 |
| Deep link | `/legal#privacy` and `/legal#terms` must land on the corresponding section, not the top | 51, 61 |
| "About our data" link | Cross-page navigation to the About Data screen | 65 |
| Nav / wordmark / avatar | Standard cross-page navigation | 29–39 |
| Link hover | `color: var(--accH)`, `text-decoration: underline` | 19 |
| Focus | `outline: 2px solid var(--acc)`, `outline-offset: 1px`, `border-radius: 2px` | 20 |
| Theme | Dark palette applied when `localStorage['cardstock-theme'] === 'dark'` sets `data-theme="dark"` on `<html>` before paint | 21, 24 |
| CVD mode | `localStorage['cardstock-cvd'] === '1'` sets `data-cvd="1"`. **This page defines no `--pos`/`--neg` overrides** (contrast the About Data page, lines 22–24) — the flag is read but has no styling effect here, correctly, since the page shows no gain/loss colour | 24 |

**Dynamic content — one candidate only.** "Last updated August 2026." (line 44) is the sole string that changes over time. Nothing else on the page reads from the database.

---

## 5. Rules and invariants

1. **Both policies live on one page.** Privacy and Terms are sections of a single document, not separate routes. Any implementation that splits them breaks `#privacy` / `#terms` deep links.
2. **The stored-data list is closed.** "That's the list." (line 54) converts an enumeration into a promise. Any new persisted user field — session IP, last-login timestamp, email-verification token, audit rows, a referral source — makes line 54 false and must be added to the copy in the same change.
3. **`scroll-margin-top: 62px` is required on both anchor targets** (lines 51, 61) because the nav is `position: sticky; top: 0` at 48px (line 28).
4. **The "no silent edits" rule** (line 68) binds the maintainers: material changes require both a visible note on this page and a date update to line 44. The subtitle date is therefore load-bearing, not decorative.
5. **The 30-day deletion window** (line 57) is a hard SLA on account deletion, and it reaches beyond the primary database — see §6 and D-017.
6. **CSV export must exist before deletion is offered** — line 57 tells the user to "Export your binder as CSV first", so the export path is a prerequisite of the deletion path, not an independent feature.
7. **The unaffiliated disclaimer is duplicated** on the About Data page (line 124 there) and must stay in sync; this page's version additionally names "Creatures Inc." and "marketplace".
8. **No footer, no data-freshness stamp on this page.** Correct — the page renders no market data.
9. **The page must remain reachable without an account.** It carries the terms a visitor agrees to at signup; gating it behind auth would be self-defeating. (Design inference — not stated in the HTML.)

---

## 6. Factual audit

Method: each claim is checked against `../PokemonInvestBatch/DATA_MODEL.md`, the scraper source, and `DECISIONS.md`. **VERIFIED** = a receipt supports it. **FALSE** = a receipt contradicts it. **UNVERIFIABLE** = it is a promise about software that does not exist yet, or no receipt exists either way.

A structural caveat covering the whole page: **CardStock has not been built.** `CLAUDE.md:3` — "Design is complete; implementation has not started." Every privacy claim is therefore a *specification of intended behaviour*, and the honest global verdict on §Privacy is UNVERIFIABLE-BY-CONSTRUCTION. What follows audits whether each claim is *achievable and consistent with decisions already made* — which is where the real failures are.

### 6.1 Privacy — what is stored

> **"Your account email, your binder (positions, cost basis, transactions), your watchlists and saved screens, and display preferences (theme, color-vision mode). That's the list."** (line 54)

**Verdict: UNVERIFIABLE, and at high risk of becoming FALSE.**

- The enumerated categories are consistent with `CLAUDE.md:59`, which lists CardStock's own tables as "users, binders, holdings, transactions, watchlists, saved screens, and whatever else the product invents".
- **The risk is that trailing clause.** "whatever else the product invents" is exactly what "That's the list." forbids.
- D-034 settles an auth model ("the multi-tenant schema from D-034", cited at `DECISIONS.md:276`). Any auth implementation stores at minimum a password hash or an external identity-provider subject id, plus session records. **Neither is on the list.** A password hash is not "your account email".
- D-011 ships "publicly with open signup" (`DECISIONS.md:185`), and D-037 requires "**Per-user rate limiting in front of `express-visit`**" (`DECISIONS.md:271`). Per-user rate limiting is per-user state that must be recorded somewhere. Not on the list.
- **Receipt for the concern, not a resolution:** no schema for CardStock's own tables exists yet to check against. Re-audit this line the day the first CardStock migration is written.

### 6.2 Privacy — the no-trackers promise

> **"We don't sell or share your data, run ads, or use third-party trackers."** (line 55)

**Verdict: UNVERIFIABLE today — and D-037 flags it as the single likeliest false statement on the site.**

`DECISIONS.md:280` (D-037) — *"`Cardstock Legal.dc.html` reportedly promises 'no third-party trackers.' If the existing New Relic OTLP stack touches the web tier, that promise is false on day one. Either keep New Relic off the web tier or amend the copy. I have not read that file."*

**That file is now read. The promise is on line 55, quoted above, verbatim as D-037 anticipated.** D-037's open item is confirmed as real, and its resolution is now purely a deployment question.

What I verified about the New Relic stack, read directly 2026-08-10 in `../PokemonInvestBatch`:

| Receipt | What it establishes |
|---|---|
| `ops/README.md:9` — "Worker → OTLP via `NewRelic:LicenseKey` in appsettings.Production.json (US endpoint otlp.nr-data.net)." | Application telemetry is exported **off-box to a third party** |
| `ops/README.md:10` — "Host: `newrelic-infra` via NR apt repo" | A **host-level** agent runs on the Pi — not scoped to the worker process |
| `ops/README.md:12` — "Logs: `/etc/newrelic-infra/logging.d/pokemon.yml` forwards the worker's systemd unit + postgres log." | Log **forwarding** to New Relic is already configured, currently scoped to the worker unit and postgres |
| `ops/README.md:11` — role `newrelic` with `pg_monitor`, `nri-postgresql` with `ENABLE_QUERY_MONITORING` | Query monitoring is on at the database the web tier will share |
| `src/PokemonInvestBatch.Worker/Program.cs:42–47` — `NewRelic:LicenseKey`, `o.Endpoint = new Uri("https://otlp.nr-data.net:4317")` | The export is unconditional when a key is present |
| D-036, `DECISIONS.md:252` — "The Blazor app runs on the same Pi as the scraper" | The web tier lands **on the box the host agent already monitors** |

**Assessment, stated precisely because the distinction is the whole answer:**
- **Narrow reading — defensible.** "Third-party trackers" conventionally means browser-side tracking: a New Relic Browser JS agent, ad pixels, cross-site cookies. **No browser agent exists anywhere in the repo.** `grep -rniE "new ?relic|otlp|opentelemetry"` across the scraper repo returns only server-side .NET, ops config, and docs — no `nr-loader`, no injected script. Under this reading, line 55 is true.
- **Broad reading — at risk.** `newrelic-infra` is a **host** agent with log forwarding already wired. If the Blazor unit's journal is added to `logging.d/`, HTTP request logs — which for a public app carry IP addresses, user agents, and authenticated user identifiers — flow to a third party. That is sharing user data with a vendor, which line 55's first clause ("don't sell or share your data") also touches.
- **Not decided.** D-037 explicitly leaves this open, and D-036 places the web tier on the monitored box, which is the condition that makes it live.

**Required before launch (restating D-037's own instruction):** either (a) keep New Relic off the web tier and add nothing to `logging.d/` for the Blazor unit, or (b) amend line 55. Option (b) is cheap and honest — "we use a hosted service to monitor server errors" costs the brand nothing. Option (a) is the stronger claim but must be enforced, not assumed. **Do not ship line 55 unchanged without picking one.**

### 6.3 Privacy — the analytics claim

> **"Analytics are limited to aggregate, anonymous usage counts."** (line 55)

**Verdict: UNVERIFIABLE, and in tension with the New Relic stack.**

- No CardStock analytics implementation exists to inspect.
- The scraper's precedent is *operational, never market or user* telemetry: `DATA_MODEL.md:16` — "Relic dashboard of *operational* metrics (row counts, delays, failures — never the market". `DATA_MODEL.md:326` — the Stats lane "emits `crawl.*` gauges to New Relic". That precedent is compatible with line 55.
- **But APM traces are not aggregate and not anonymous.** OpenTelemetry request spans carry URL paths, and a CardStock URL path is `/card/{id}` or `/binder` — per-request, per-user, retained by a vendor. If the web tier gets the same OTLP treatment as the worker (`src/PokemonInvestBatch.Worker/Program.cs:40–47` shows the pattern), "aggregate, anonymous usage counts" is not an accurate description of what leaves the box.
- Same remedy as §6.2: scope the telemetry, or scope the sentence.

### 6.4 Privacy — binder confidentiality

> **"Your binder is visible to you alone; no aggregate we publish can be traced back to a person's holdings."** (line 55)

**Verdict: UNVERIFIABLE (no implementation), but architecturally supported and consistent with decisions.**

- D-037, `DECISIONS.md:276` — "IDOR on binder/watchlist/saved-screen rows — every query scoped by `user_id` (the multi-tenant schema from D-034)." The first clause has a named mitigation.
- The second clause is a **k-anonymity promise** with no stated mechanism. No decision, threshold, or suppression rule exists anywhere in `DECISIONS.md` or the mockups. If CardStock ever publishes a "most-held cards" or "average cost basis" aggregate, the promise needs a minimum-cohort rule behind it. See §7.

### 6.5 Privacy — cookies and local storage

> **"We use local storage for sign-in sessions and preferences — nothing that follows you around the web."** (line 56)

**Verdict: VERIFIED for the preferences half; UNVERIFIABLE for the sessions half.**

- Preferences: the prototypes do exactly this. `Cardstock Legal.dc.html:24` — `localStorage.getItem('cardstock-cvd')` and `localStorage.getItem('cardstock-theme')`. Two keys, both display preferences, both matching the categories enumerated on line 54. Identical code on the About Data page (line 28 there). **Verified against Tier-1 sources.**
- Sessions: no auth exists yet. **A precision problem worth flagging now:** ASP.NET Core authentication uses **cookies**, not local storage, and Blazor Interactive Server (D-013, open) adds a SignalR circuit. The paragraph is headed "Cookies &amp; local storage." but its body mentions only local storage, so a cookie-based session would not contradict the heading — though it would make the body sentence imprecise. Revisit when D-013/D-034 are implemented.

### 6.6 Privacy — the deletion window

> **"Delete your account from Profile &amp; settings and everything above is removed within 30 days."** (line 57)

**Verdict: UNVERIFIABLE, with a known, documented complication.**

`DECISIONS.md:475` (D-017) — *"an off-box dump interacts with the 30-day account-deletion promise in `Cardstock Legal.dc.html` once user tables exist. Unverified — I have not read that file."*

**That file is now read, and D-017's note is confirmed: the 30-day promise is real and is on line 57.**

The interaction is concrete:
- D-017 (`DECISIONS.md:466–473`) is weighing an off-box backup of the irreplaceable scraper tables. `DECISIONS.md:471` — a grep across `ops/` and `DATA_MODEL.md` "returns no backup strategy of any kind"; **no backup exists today**, so nothing is currently violated.
- The moment backups exist and include CardStock's user tables, "removed within 30 days" requires **backup rotation shorter than 30 days**, or documented exclusion of user tables from the dump, or a replay-deletes-on-restore procedure.
- D-017 scopes the backup to `sales` and `populations` — "**`sales` and `populations` are the irreplaceable assets**" (`DECISIONS.md:469`) — which, if honoured literally, leaves user tables out of the dump and the promise intact. That is a happy accident of scope, not a decision. **Make it a decision.**
- 30 days is a generous window (GDPR erasure is "without undue delay", commonly read as ≤30 days), so the promise is achievable. It is not achievable *by inaction*.

### 6.7 Privacy — deletion prerequisite

> **"Export your binder as CSV first if you want a copy."** (line 57)

**Verdict: VERIFIED against Tier-1 mockups.** CSV export appears in `Cardstock Binder.dc.html`, `Cardstock Binder Landing.dc.html`, and `Cardstock Screener.dc.html` (grep for "CSV" across `CardStock Mockup/*.dc.html`, run 2026-08-10). The feature the sentence points at is designed and exists in the prototype set.

### 6.8 Terms — not financial advice

> **"Cardstock describes what the market did, not what it will do. Signals, screens, and backtests are research tools; decisions and their outcomes are yours."** (line 64)

**Verdict: VERIFIED as consistent with product design — this is the sharpest-drafted sentence on the page.**

- It is the same posture the About Data page takes: `Cardstock About Data.dc.html:112` — "No projected or extrapolated data points — a partial month renders as partial, never as a forecast." — and `:125` — "signals describe the past, not the future."
- It is structurally guaranteed rather than merely promised: D-041 (`DECISIONS.md:229`) establishes that `price_months.price_cents` is a single monthly value and OHLC "needs four points per period and intraday sequencing that does not exist at the source". A product built on monthly averages **cannot** produce the intraday predictive apparatus the sentence disclaims.
- "backtests" as a named feature is corroborated by `Cardstock About Data.dc.html:113` ("Backtests start at each screen's honest floor").
- **Legal note, not a data note:** a not-financial-advice disclaimer is a mitigation, not an immunity, and D-011's public launch is what makes it matter. Nothing in the repo suggests a lawyer has reviewed this page. Flagged in §7.

### 6.9 Terms — data accuracy

> **"We work hard on data quality (see About our data), but sales records and census reports contain errors upstream of us. The service is provided as-is, without warranty of accuracy or availability."** (line 65)

**Verdict: VERIFIED on the upstream-errors clause; the phrasing understates how much is upstream.**

- Upstream errors are documented and specific. `DATA_MODEL.md:209–213` — "graders occasionally **restate** their counts (PSA restated ~June 2026; one card's grade cell jumped 397 → 99,246)". That is a real, named, dated instance of exactly the error class the sentence describes. **Strong verification.**
- "we work hard on data quality" is verifiable in spirit: the scraper hard-rejects schema drift (`DATA_MODEL.md:116–117` — "The parser also hard-rejects unknown `chart_data` series as schema drift, so if the site ever adds a volume series, the crawl halts loudly the same day"), runs a canary lane every 6 h (`DATA_MODEL.md:324`), and records `parse_failures`. Real quality engineering exists.
- **The understatement:** "upstream of us" implies a boundary where CardStock's own collection begins. There isn't one. `DATA_MODEL.md:89` — "All data comes from pricecharting.com". **100% of market data is upstream of us**, including the price aggregates themselves. The sentence is not false; it is thinner than the truth. This is the same first-party-implication problem that runs through the About Data page — see `about-data.md` §6.
- Availability disclaimer: appropriate. D-036 puts the app on a single Raspberry Pi 5 sharing a box with the crawler and Postgres. Single point of failure, no redundancy, and per D-017 **no backups**. Disclaiming availability is not boilerplate here; it is accurate.

### 6.10 Terms — fair use

> **"Your account is for you. Don't scrape at volume, resell our data, or probe other users' information. We may suspend accounts that do."** (line 66)

**Verdict: UNVERIFIABLE (no enforcement exists), with one uncomfortable irony and one missing control.**

- **Irony worth stating plainly:** CardStock forbids users from scraping it, while 100% of its market data is obtained by scraping pricecharting.com — `DATA_MODEL.md:89`, and `CLAUDE.md:47` describes the sibling worker as a system that "politely crawls pricecharting.com into PostgreSQL". This is not a factual error in the sentence, but it is a defensible-only-with-care position, and it is adjacent to the genuinely unresolved question in D-010 (`DECISIONS.md:90`): "**What is genuinely open:** licensing. No repo records reading any terms of service".
- "resell our data" — asserts a proprietary interest in data scraped from a third party. Whether that interest exists is a licensing question nobody has answered. Same D-010 receipt.
- **Missing control:** D-037 (`DECISIONS.md:271`) requires "Per-user rate limiting in front of `express-visit`", noting "nothing bounds user-triggered frequency". Line 66's "Don't scrape at volume" is currently a request with no technical backstop, against an endpoint that reaches a third-party site.

### 6.11 Terms — intellectual property / fan-made disclaimer

> **"Cardstock is a fan-made project, not affiliated with, endorsed by, or sponsored by Nintendo, The Pokémon Company, Creatures Inc., or any grading company or marketplace. Pokémon names and card references are used for identification only; all trademarks belong to their owners."** (line 67)

**Verdict: VERIFIED as internally consistent, and correctly placed. The disclaimer is necessary but not sufficient for the real open risk.**

- Consistent with the About Data page's near-identical disclaimer at `Cardstock About Data.dc.html:124`. This page's version is **broader**: it adds "Creatures Inc." and "or marketplace", and says "fan-made project" where About Data says "fan-made analytics project".
- The entities named are the right ones. Marketplaces appear as sale sources — `DATA_MODEL.md:227` — "ebay, tcgplayer, goldin, heritage, pwcc"; graders appear as census sources — `DATA_MODEL.md:204` — "only `psa` or `cgc`".
- **What the disclaimer does not cover — the actual open risk: card images.** D-010 (`DECISIONS.md:83–90`) verifies ~3.6 GB of real photos on disk, fetched from `images.pricecharting.com/{hash}/1600.jpg` (`DATA_MODEL.md:105`), and states: "**What is genuinely open:** licensing. No repo records reading any terms of service, and storing is a different act from serving." A trademark-identification disclaimer does not address **copyright** in third-party product photography served to the public. Nominative fair use of the word "Pokémon" and republication of another site's images are different legal questions, and only the first is addressed here.
- Note also that "not affiliated with… any… marketplace" sits beside "resell our data" (line 66) and a dataset sourced from those marketplaces via pricecharting. Consistent, but it underlines that the whole product's legal footing rests on the unread terms of service in D-010.

### 6.12 Terms — changes

> **"If these terms change materially, we'll say so on this page and date it — no silent edits. That's how we treat charts, and how we treat this."** (line 68)

**Verdict: VERIFIED as a coherent, honoured-by-design commitment.**

- The chart analogy is real and checkable. `Cardstock About Data.dc.html:63` — "When a grader restates a past census (it happens), we mark the affected window on charts rather than silently rewriting history." — and `:116` — "When a grader restates history, the restatement window stays visibly marked." The cross-reference is accurate, not rhetorical.
- The subtitle "Last updated August 2026." (line 44) is the mechanism the sentence commits to. It exists.
- **Implementation consequence:** the date must be a maintained value. If it is hard-coded in a `.razor` file and forgotten, the page violates its own rule silently — the exact failure mode D-032 punished elsewhere ("wrong in the direction that overstates readiness", `DECISIONS.md:342`).

### 6.13 Subtitle

> **"Short, in plain language, and true. Last updated August 2026."** (line 44)

**Verdict: "Short" and "in plain language" — VERIFIED (nine paragraphs total, no defined terms, no capitalised party names). "True" — see §6.2, §6.3, and §6.1; it is the claim this whole section exists to test, and it is not yet earned.**

"Last updated August 2026" is consistent with the project timeline (`DECISIONS.md` entries dated 2026-08-10; scraper first commit 2026-07-27 per `CLAUDE.md:47`).

### 6.14 Audit summary

| # | Claim | Line | Verdict |
|---|---|---|---|
| 1 | Closed list of stored data, "That's the list." | 54 | UNVERIFIABLE — omits auth credentials/sessions implied by D-034; high risk of becoming FALSE |
| 2 | "don't sell or share your data, run ads, or use third-party trackers" | 55 | UNVERIFIABLE — D-037's flagged risk, now confirmed present; false under a broad reading if New Relic touches the web tier |
| 3 | "Analytics are limited to aggregate, anonymous usage counts" | 55 | UNVERIFIABLE — OTLP request traces would not be aggregate or anonymous |
| 4 | "Your binder is visible to you alone" | 55 | UNVERIFIABLE — mitigation named in D-037, not built |
| 5 | "no aggregate we publish can be traced back to a person's holdings" | 55 | UNVERIFIABLE — no k-anonymity rule exists anywhere |
| 6 | Local storage for sessions and preferences | 56 | VERIFIED (preferences) / UNVERIFIABLE (sessions; likely cookies, not local storage) |
| 7 | "removed within 30 days" | 57 | UNVERIFIABLE — D-017 backup interaction confirmed; safe only while no backup exists |
| 8 | "Export your binder as CSV first" | 57 | VERIFIED — CSV export present in Binder and Screener mockups |
| 9 | Not financial advice | 64 | VERIFIED as consistent with design; D-041 makes forecasting structurally impossible |
| 10 | Upstream errors in sales and census | 65 | VERIFIED — `DATA_MODEL.md:209–213`, PSA restatement 397 → 99,246 |
| 11 | As-is, no warranty of availability | 65 | VERIFIED as appropriate — single Pi, no backups (D-017) |
| 12 | Fair use / anti-scraping | 66 | UNVERIFIABLE — no enforcement; D-037's rate limit unbuilt; sits awkwardly beside our own scraping |
| 13 | Fan-made, unaffiliated, trademarks | 67 | VERIFIED as consistent — but does not cover image copyright (D-010) |
| 14 | No silent edits | 68 | VERIFIED as coherent; cross-reference to chart restatement marking is accurate |

**No claim on this page is outright FALSE against a receipt.** That is a genuinely better result than the About Data page. The exposure here is concentrated in claims that are *not yet true and will silently become false* if the New Relic scope (§6.2/§6.3), the auth schema (§6.1), or the backup design (§6.6) is decided without revisiting this copy.

---

## 7. Open questions

1. **D-037's New Relic question must be closed before launch.** Keep the OTLP/`newrelic-infra` stack off the Blazor web tier, or amend line 55. Currently undecided (`DECISIONS.md:280`).
2. **Does the stored-data list survive the auth implementation?** Password hashes, IdP subject ids, session rows, email-verification tokens, and rate-limit counters are all implied by D-034/D-011/D-037 and none appear on line 54.
3. **What backs the k-anonymity promise?** "no aggregate we publish can be traced back to a person's holdings" (line 55) needs a minimum-cohort threshold. Does CardStock publish holdings aggregates at all? If not, delete the clause rather than defend it.
4. **Does the 30-day deletion window bind backups?** D-017 is unresolved. Decide explicitly that user tables are excluded from any off-box dump, or set rotation under 30 days.
5. **Is the page reachable pre-auth, and is there a signup-time consent link?** Neither is specified in any prototype. Only `Cardstock Home.dc.html` links here (2 references).
6. **Route and file name.** `/legal` is proposed here, not decided. Note the screen label is "Privacy & Terms" while the file is "Legal" — pick one for the route and the nav string.
7. **Has anyone read pricecharting.com's terms of service?** D-010 (`DECISIONS.md:90`) says no. This page asserts a proprietary interest in the data (line 66) and serves third-party images, both of which depend on that answer.
8. **Who maintains "Last updated August 2026"?** Line 68 makes it a commitment; nothing makes it automatic.
9. **Is the "Creatures Inc." / "marketplace" wording delta from the About Data disclaimer intentional?** Two versions of the same disclaimer will drift.
10. **Legal review.** A public site (D-011) with a financial-adjacent product, a privacy policy making enumerated promises, and unexamined third-party terms. Nothing in the repo records a review.

---

## 8. Contradictions found

1. **D-037's flagged risk is real and now confirmed.** `DECISIONS.md:280` says "reportedly promises 'no third-party trackers'… I have not read that file." The file is read: the promise is at line 55, verbatim. D-037's caveat can be upgraded from *reported* to *verified present*, and its remedy is now blocking rather than hypothetical.
2. **D-017's flagged interaction is real and now confirmed.** `DECISIONS.md:475` says "an off-box dump interacts with the 30-day account-deletion promise… Unverified — I have not read that file." The 30-day promise is at line 57, verbatim.
3. **"Don't scrape at volume" (line 66) vs the product's own method.** CardStock's entire dataset is scraped from one third party (`DATA_MODEL.md:89`; `CLAUDE.md:47`). Not a contradiction *within* the page, but a public-facing asymmetry that a reader can spot, adjacent to the unresolved licensing question in D-010.
4. **"resell our data" (line 66) vs "all data comes from pricecharting.com" (`DATA_MODEL.md:89`).** The page asserts ownership of data it did not originate, while D-010 records that no terms of service have been read.
5. **Two versions of the unaffiliated disclaimer.** Line 67 here names "Creatures Inc." and "or marketplace"; `Cardstock About Data.dc.html:124` omits both. Both are Tier-1 prototypes, so neither is wrong — but they must be reconciled into one shared component or they will diverge.
6. **"Cookies & local storage." heading vs a body that describes only local storage** (line 56). Minor, but the likely auth implementation (ASP.NET Core cookie auth, and a SignalR circuit if D-013 lands on Interactive Server) is cookie-based, which the body sentence does not mention.
7. **The page's own "true" claim (line 44) vs three claims that are not yet true** (§6.1, §6.2, §6.3). The subtitle is a promise about the rest of the page, and the page currently cannot honour it in full.
