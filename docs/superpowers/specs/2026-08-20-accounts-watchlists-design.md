# Accounts + watchlists — design

**Date:** 2026-08-20 · **Status:** owner-approved section by section (brainstorm session, 2026-08-20); banked
**Prerequisite:** the **Public exposure** phase (D-129) ships first. This design inherits from it and does not re-specify: HTTPS-only Kestrel on 443 (D-132 amended D-129's "5180") with a browser-trusted certificate (DNS-01), HSTS, response security headers (CSP with Blazor-WASM allowances, `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options`), the hardened systemd unit, and the domain — which is also the email sending domain, with SPF/DKIM/DMARC pre-staged.
**Build references:** `docs/screens/account.md` · `docs/screens/profile.md` · `docs/screens/card.md` §3.4–3.5 · ADR-0001 · ADR-0002 · D-098 · D-103 #3.

---

## 0. Scope

Per D-103 #3, as approved: the five-view auth shell, Profile & settings, watchlist tables + API, and the Card-page "+ Watchlist ▾" picker (D-098's watchlist half). Plus, ruled during this brainstorm: **the auth wall** — after this phase, signed-out visitors see the auth shell only (§5).

**Deliberate trims** (each renders with the established deferred treatment, never stripped — D-084.1/D-087):
avatar upload (initial-only circle; 72×72 slot renders deferred) · session-sweep job (worker phase; expiry enforces at read time) · tracked-signals join table (Charts phase; nothing this phase reads it) · list rename/delete (no mockup designs them anywhere; → Home phase's open questions) · the demo button (deferred-disabled; account.md OQ-8 stays open for the marketing phase) · the Binder button (D-098's other half, Binder phase).

---

## 1. Data model

**Existing tables:** `cardstock.users` gains **three columns** — `display_name` (nullable, ≤40), `timezone` (not null, default `America/Chicago`), `password_changed_at` (backfilled from `created_at`; renders Profile's "Last changed" line, profile.md OQ-7). `cardstock.sessions`: unchanged. Scraper tables: read-only as always, untouched.

**Three new tables** (`cardstock` schema, snake_case, one migration — the lineage's second — hand-run from a dev machine per ADR-0001):

**`email_tokens`** — one table for all three emailed links (ADR-0002 §5 widened to cover email change):
- `id` PK · `user_id` FK → `users`, cascade
- `purpose`: `verify_email` / `reset_password` / `change_email`
- `token_hash` unique — SHA-256 of the URL token; the raw token exists only in the emailed link
- `new_email` nullable — `change_email` only: the address awaiting confirmation. Profile's pending banner derives from an unconsumed, unexpired row here; **Cancel consumes the token** (profile.md OQ-9), so no column lands on `users`
- `created_at` · `expires_at` (created + 30 min — the mockup's contract, account.md §3.4) · `consumed_at` nullable (single-use)
- index `(user_id, purpose, created_at)` — resend cooldown + invalidate-previous

**`watchlists`** — `id` PK · **`user_id`** FK → `users`, cascade · `name` (≤40) · `created_at` · unique `(user_id, name)` · unique `(user_id, id)` (the composite-FK target below).

**`watchlist_rows`** — `id` PK · **`user_id`** · `watchlist_id` · `card_id` · `tier` · `position` (row order; Home's drag-to-reorder binds in its phase; the picker appends) · `created_at` · unique `(watchlist_id, card_id, tier)`.
- **Composite FK `(user_id, watchlist_id)` → `watchlists (user_id, id)`, cascade** — a row claiming one user while sitting in another user's list is structurally impossible. Owner ruling: D-034's "`user_id` on every user-facing table" applies **literally, child tables included**; this composite-FK pattern binds `transactions` and `saved_screens` in their phases.
- `card_id` gets a **real hand-written FK → `public.cards(id)`** — ADR-0001 §4's first instance. Requires one new line in `ops/cardstock-postgres-setup.sql`: `GRANT REFERENCES ON public.cards TO cardstock_owner`, run once on the Pi before migrating.
- `tier` is the six-value `PriceTier`, stored as `ScraperPriceMonth.Tier` maps it, so joins to `price_months` are direct.
- index `(user_id, card_id)` — serves the picker's read in one lookup.

**Ruling — watchlist row identity (closes card.md OQ-11/C-14):** a row is **card + tier**. Home's Tier-1 mockup makes tier "part of the row's identity" (home.md §3.4 col 3); the picker gains the tier control (§4). The Tier-1-vs-Tier-1 conflict is thereby owner-resolved (the D-043 conflict class).

**Deletion is one FK cascade chain** — users → sessions, email_tokens, watchlists → watchlist_rows — so D-069's "immediately and permanently" is database-enforced.

**Caps** (API-enforced abuse hygiene, generous per D-062): 20 lists/user, 500 rows/list, both config.

---

## 2. Auth flows

**Mechanism (settled, ADR-0002 — standard ASP.NET Core, à la carte, NOT full Identity):** cookie authentication + `ITicketStore` against `cardstock.sessions`; `PasswordHasher<T>`; `RandomNumberGenerator`/SHA-256 for tokens; Blazor's `AuthenticationStateProvider`/`AuthorizeView`. Why not full Identity (the rationale ADR-0002's alternatives omitted): its seven-table schema serves roles/claims/OAuth/2FA — none designed anywhere in this product (account.md §5.1); it collides with the two live hand-shaped tables; and it rides the same cookie handler underneath, so revocable server-side sessions would still require our `ITicketStore`. Hand-written surface: the tables, the ~one-screen `ITicketStore`, token-row bookkeeping. No hand-rolled crypto.

**Endpoints** (state-changing routes behind the CSRF guard + rate limits):

| Endpoint | Behaviour |
|---|---|
| `POST auth/create` | Creates unverified user, issues `verify_email` token, mails it, → sent view. **Existing address → identical response**, mail says "you already have an account — reset instead?" (non-enumerable create; extends account.md rule 9, resolves OQ-5's already-registered state) |
| `POST auth/signin` | Wrong credentials → generic banner. Correct-but-unverified → "Verify your email first — resend the link?" (only reachable with a correct password; leaks nothing). Success → session row + cookie |
| `POST auth/signout` | Deletes the session row |
| `POST auth/forgot` | Always → sent view (rule 9) |
| `POST auth/verify-email` / `auth/reset` / `auth/confirm-email-change` | Consume their token kinds; expired/used/malformed → in-view banner "That link has expired — send a new one?" (OQ-2) |
| `POST auth/resend` | Reissues the live token; 60s server cooldown |
| `GET auth/me` | 200 `{email, memberSince}` / 401 — client boot probe |
| Profile-backing | change-password · change-email (reauth with current password; token to the **new** address) · cancel-email-change · save-profile · delete-account |

**Ruling — unverified accounts can do nothing**: verification gates the first sign-in. The verification link lands on the sign-in view with the green flash ("Email verified — sign in."), mirroring the reset flow's return-to-signin pattern (resolves OQ-9).

**Session-revocation semantics** (the mockup's open edges, ruled): password **reset** revokes *all* sessions; authenticated password **change** keeps this session and revokes others (profile.md's "you stay signed in on this device" becomes literally true); email change revokes nothing. Cookie: `__Host-` prefixed, `HttpOnly`, `Secure` (TLS exists — Phase A), `SameSite=Lax`, 30-day sliding lifetime, opaque 256-bit id.

**CSRF:** shared middleware rejects any state-changing request lacking a custom header (`X-CSRF: 1`) the WASM client always sends. Cross-site HTML can't set custom headers; with `SameSite=Lax` that's two independent stateless layers. This is the approved reading of ADR-0002 §6's "anti-forgery in shared middleware."

**Email:** typed `IEmailSender` (one method per mail kind) in Application. Infrastructure: a transactional-provider HTTP adapter (owner chose the provider route; default recommendation Resend — provider, key, and from-address confirmed at build against the Phase-A domain) plus a **file/log sink** for dev, tests, and provider-less deploys. Plain-text mail. **Provider failure surfaces honestly**: "Email couldn't be sent — try again in a minute" on the same view — never a fake sent view. (Edge, accepted with eyes open: on forgot, an unknown address attempts no send and always shows sent; the failure banner can therefore only appear for a known address during a provider outage — a theoretical enumeration window judged not worth lying to the legitimate user for.)

**Auth rate limits** (config; per-IP unless noted): signin ~10/min · create ~5/hr · forgot/resend ~5/hr · plus the per-user 60s resend cooldown in the token table.

---

## 3. Screens

### 3.1 Account shell (`/signin` · `/create` · `/forgot` · `/reset` — D-058's routes)

One routable component, five views; `sent` stays a **non-addressable in-page state** of create/forgot, as the mockup builds it (resolves OQ-1). Bare layout — brand lockup, 380px card, legal footnote, **no AppChrome**, no prototype jump rail. account.md's rhythm to the letter: one card-level banner slot above the `h1`, masked emails (first char + `•••` + full domain), the 12+ helper on create/reset, **no field-level error styling** — client-side checks (short password, confirm mismatch, malformed email) use the same banner (OQ-5 within rule 6).

Spec extensions (each lands as an account.md §7/§8 row update): submitting/disabled states on the four primary buttons (OQ-6) · expired/used-token banners (OQ-2) · a real resend button with 60s countdown on `sent` (OQ-3) · "← Back to sign in" on the reset view (OQ-10). Link routes: `/verify?token=` auto-confirms → signin + flash; `/reset?token=` carries the token the mockup's bare `#reset` lacked (OQ-4); `/confirm-email?token=` completes the email change (→ `/settings` + flash if signed in, else signin + flash). Sign-in success → `/browse` until Home exists. The theme pre-paint script in `index.html` already covers these routes — account.md §8's light-flash defect is resolved by construction.

### 3.2 Profile & settings (`/settings`, behind `AuthorizeView`)

Four cards per profile.md, 760px column, normal chrome, circle inert ("You are here").
- **Profile:** display name + timezone behind one Save ("Saved ✓" 2200 ms). Timezone stays the mockup's closed list of five (US + UTC; formats wall-clock stamps only — full IANA is a later one-liner). Avatar: 72×72 deferred slot, honest tooltip.
- **Appearance:** Light/Dark segmented pair + CVD switch writing the **existing** `cardstock-theme`/`cardstock-cvd` localStorage keys and root attributes via a small JS interop — instant, per-device, no server round-trip. Static preview strip per mockup.
- **Account:** email change interpolates the **typed** pending address (mockup hardcodes `otto.new@fastmail.com`), reauths with current password, Cancel consumes the token; password change collapses with "· updated ✓" (2600 ms) and updates `password_changed_at`; Session row reads "This device · signed in {date}" from the current session row — **no IP geolocation** (spec correction: the mockup's "Chicago, IL" would need a third-party lookup).
- **Danger zone:** typed-DELETE modal exactly as specced (exact, case-sensitive gate), plus Escape-to-close and a focus trap (OQ-12). Live counts enumerate **what exists** — this phase, watchlist rows — growing as phases land. Delete → cascade → signout → `/signin`.

Dates follow D-095 (`Dates.Full` for full dates; month+year forms like "member since Mar 2026" stay as designed).

### 3.3 Chrome

Signed-in: circle shows the account initial (display name, else email), links `/settings`. Signed-out (only the auth shell + `/about-data` are reachable): n/a beyond the wall redirect. The stale "Accounts arrive with the Binder phase" tooltip goes. Recorded in shared-components.md.

---

## 4. Watchlists — API and picker

**Four endpoints** (session-scoped; every query filters by the session's `user_id` — D-037):

| Endpoint | Behaviour |
|---|---|
| `GET cards/{cardId}/watch` | My lists (`id`, `name`, live row count) + this card's memberships as (`listId`, `tier`) pairs — one `(user_id, card_id)` lookup |
| `POST watchlists` | Create `{name}`; 409-style conflict message on duplicate name; 20-list cap |
| `POST watchlists/{id}/rows` | Add `{cardId, tier}`; idempotent via the unique constraint; 500-row cap, honest message |
| `DELETE watchlists/{id}/rows?cardId=&tier=` | Remove membership; idempotent 204 |

**The picker** (card.md §3.4 to the letter + the ruled tier extension): split-button `+ Watchlist ▾` / `Watching ✓ ▾` / `Watching ✓ (N) ▾` (N = lists containing this card at any tier), specced green active recipe. Popover top-to-bottom: **tier select** (six price tiers, default **PSA 10**) — checkboxes re-derive against the selected tier; per-list checkbox rows (15×15, checked recipe, name + live count in mono); `+ New list…` swaps to an **inline input inside the popover** (replaces the mockup's native `prompt()` — OQ-11's undesigned half). Click-outside + Escape dismiss. **Round-trip writes, not optimistic**: a toggled row disables in flight; failure prints "Couldn't save — try again" inline. Binder button: unchanged `DeferredControl`, tooltip freshness pass only.

After this phase, membership is visible only through the picker — the Home watchlist table binds to these tables in its own phase (D-103 order; home.md noted).

---

## 5. Hardening & rollout

**The wall (owner ruling):** signed-out visitors see the auth shell; **`/about-data` is the single public exception**. All app pages (`/`, `/browse`, `/card/{id}`, `/set/{id}`, `/character/{slug}`, `/settings`) **and their JSON API** require a session — the API gets a **default-deny fallback authorization policy** with `AllowAnonymous` enumerated on the short public set (`/healthz`, auth endpoints, about-data's read). Forgetting metadata on a new endpoint now fails closed (inverts D-037's worry). The WASM router mirrors it (`AuthorizeRouteView` → `/signin?return=…`) as courtesy UX; the API is the boundary; the host page + WASM bundle stay anonymous (code, not data). The prototypes agree with the wall: the designed no-account path was always the demo, never public app pages (account.md rule 11, OQ-8).

**Rate limiting:** express-refresh rekeys **per-account** (D-062's original intent — express is signed-in-only now; the per-IP partition and its stale comment go). §2's auth limits land. Behind-proxy concerns (forwarded headers) are Phase A's.

**Config:** email provider key + from-address + link base URL (the Phase-A domain) · cookie name + lifetime · auth limit numbers · watchlist caps. Secrets via the existing `credentials.local` pattern.

**Runbook** (ADR-0001 discipline; assumes Phase A shipped):
1. Append + run the `GRANT REFERENCES` line on the Pi.
2. Hand-run the migration from a dev machine (three tables, three columns; all-empty tables, no data motion).
3. Publish + deploy. **This deploy walls the previously-public app** — from this restart, visitors get the sign-in card.
4. Owner creates the first account through open signup itself (file-sink fallback if the provider key isn't ready — links read from the journal).
5. Smoke: `/healthz` · signed-out redirect · create → verify → sign in (Secure cookie present over HTTPS) → add a card at a tier → row lands with the right `user_id` → delete account → cascade verified.

**Unchanged:** Worker (sweep waits for its phase), scraper schema/grants beyond the one line, marketing tier.

---

## 6. Testing & closure

House pattern: one suite per project, Pi-gated DB fixtures via `cardstock_tester` (no local Postgres), CI unchanged (Postgres 15 pinned). No indicator math → the signal-referee rule doesn't trigger.

- **Application:** token lifecycle (one live per purpose, TTL, hash-only, single-use) · anti-enumeration create (identical response, different mail) · signin's three outcomes · reset-revokes-all vs change-revokes-others · email-change confirm/cancel consume · caps (21st list, 501st row) · idempotent add/remove.
- **Infrastructure (Pi-gated):** mappings · **composite FK rejects a cross-owner row** · full deletion cascade verified empty · both unique constraints · `ITicketStore` round-trip · schema model tests A/B/C stay green.
- **Api:** **enumerate public endpoints, assert every other mapped endpoint 401s anonymously** (the wall fails closed, forever) · cookie flow at the HTTP layer · CSRF-header rejection · 429s with test-sized limits · per-account express keying · IDOR probes (user A → user B's lists: never data).
- **Web (bUnit):** five-view shell + banner precedence (success suppresses error — rule 7) · submitting states · picker label arithmetic (0/1/N) · tier re-derivation · inline new-list flow · route-guard redirect · chrome states.
- **Integration (Pi-gated):** signup → link from the **email file-sink** → verify → sign in → add at tier → row with right `user_id` → delete → cascade. E2E with no provider account.
- **Email adapter:** fake `HttpMessageHandler` — request shape, auth header, error mapping. CI never calls a provider.

**Spec/ledger sync (the maintenance rule — part of "done"):** account.md + profile.md §7/§8 rows updated in place (~10 OQs resolve, reasoning preserved) · card.md records the tier control, closes OQ-11/C-14, D-098's watchlist half closes · home.md notes the card+tier substrate · legal.md's deletion row corrects to D-069 (the "30 days" and "export CSV first" copy is superseded) · route lines across browse/set/character/card gain the wall note, about-data marked public · shared-components.md gains the chrome states · DECISIONS.md carries this brainstorm's rulings (D-129/D-130).

---

## 7. Deferred & open (tracked, not forgotten)

| Item | Lands |
|---|---|
| Home watchlist table, tabs, drag-reorder, move/remove, list rename/delete question | Home phase |
| Tracked-signals join + editor | Charts phase |
| Binder button wiring | Binder phase |
| Avatar upload | Unscheduled (owner-taste) |
| Session sweep | Worker phase |
| Demo semantics (OQ-8) + marketing/`/product` redirect | Marketing phase |
| Timezone list beyond five · full session list UI | If ever wanted |
| Backups → deletion-promise revisit | D-069's trap note stands |

## 8. Rulings log (owner, 2026-08-20 session)

1. Watchlist rows are **card + tier**; picker gains a tier control, PSA 10 default.
2. Email via a **transactional API provider** behind `IEmailSender` (+ file sink).
3. **Verify before first sign-in**; link lands on signin + flash.
4. Approach 1: full phase scope, honest trims.
5. `user_id` on **every** table, child rows included, via composite FK (D-034 literal).
6. Standard ASP.NET Core auth à la carte; **not** full Identity (ADR-0002 stands).
7. **The auth wall**: signed-out = auth shell (+ `/about-data`); pages and API both walled, default-deny.
8. HTTPS-only (no plain-HTTP listener) — implemented in Phase A after the reorder.
9. **Phase reorder (D-129):** Public exposure ships first; this phase second.
