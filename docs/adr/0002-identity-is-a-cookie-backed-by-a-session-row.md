# ADR-0002: Identity is email and password, carried in an HttpOnly cookie backed by a session row

**Date:** 2026-08-11
**Status:** Accepted

## Context

CardStock ships publicly with open signup (D-011), so authentication is required and
non-optional. The mechanism was settled at **email and password** (D-034), and the entire
logged-out surface is already designed: `docs/screens/account.md:15` specs five mutually exclusive
views — sign in, create account, request reset, "check your email", set a new password — with a
password policy of exactly one rule, **minimum length 12, no complexity requirement**
(`account.md:139`). The screen already calls for a verification token and a reset token, both with
expiries (`account.md:154`).

What was never decided is how a session is carried, and the architecture constrains it. D-063 puts
a WebAssembly client in front of a stateless API, where "stateless" means the *server* holds no
session in its own memory, so a deploy never disconnects anyone. Identity therefore has to travel
with every request.

Two facts about this particular application push the answer:

**There is a real XSS surface.** `sales.title` is stored exactly as scraped and must be encoded at
render (D-029). Any credential that JavaScript can read is a credential an XSS can exfiltrate.

**Account deletion is now immediate and permanent.** With backups deferred (D-017, owner
2026-08-11), the bounded-window compromise in D-053 is unnecessary and deletion means deletion —
which is only true if the deleted account's credential stops working at once.

## Decision

**A session is an HttpOnly cookie whose value identifies a row in `cardstock.sessions`.**

1. **ASP.NET Core cookie authentication**, configured with `HttpOnly`, `Secure`, and
   `SameSite=Lax`. The cookie is unreadable from JavaScript, so an XSS cannot steal the session
   even where it can act within one.

2. **The session lives in Postgres, not in the cookie**, via the framework's `ITicketStore`
   extension point. This is what makes revocation real: signing out, "sign out everywhere", and
   account deletion all take effect on the next request rather than whenever a token happens to
   expire.

3. **This still satisfies D-063.** No session is held in server memory, so a deploy logs nobody
   out. The lookup is one indexed query against a 2.3 GB database on the same box.

4. **Passwords are hashed with ASP.NET Core's `PasswordHasher<T>`.** Minimum length 12, no
   complexity rule, matching the designed copy exactly. No password composition rules are added.

5. **Verification and reset are single-use tokens with expiries**, stored hashed, in their own
   table. Requesting a reset for an unknown address returns the same "check your email" response
   as a known one, so the flow does not disclose which addresses have accounts.

6. **CSRF is handled by `SameSite=Lax` plus an anti-forgery token on state-changing requests.**
   Cookies trade XSS exposure for CSRF exposure; this is the mitigation that makes it a good
   trade.

7. **Transactional email is a required external dependency** — verification, password reset, and
   email change. This is the project's first dependency outside the Pi. The owner has accepted it
   (2026-08-11) and email verification stays in v1 rather than being deferred.

## Alternatives considered

**A JWT in `localStorage`.** The conventional SPA answer for roughly 2015–2020, and rejected here
for the reason the industry has largely moved on from it: `localStorage` is readable by any script
on the page, so one XSS yields a bearer token, and a self-contained token cannot be revoked before
it expires. Both cut directly against D-029 and against deletion meaning deletion.

**A self-contained cookie carrying encrypted claims** — the ASP.NET Core default, without
`ITicketStore`. Simpler, and genuinely fine for most applications. Rejected only because a deleted
or signed-out account keeps a working cookie until expiry, which contradicts the deletion promise
this product makes on its own Profile screen.

**Deferring email verification to ship sooner** — accounts usable immediately, email only for
password reset. Offered and declined; the owner is provisioning a mail service.

## Consequences

**Identity is boring and framework-standard**, which is the intent. Cookie authentication is
built into ASP.NET Core and `ITicketStore` is a supported extension point, so nothing here is
hand-rolled except the tables.

**Every state-changing endpoint needs anti-forgery**, and that is easy to forget on a new
endpoint. It belongs in shared middleware or a filter applied by default, not per-endpoint.

**The API and the client must share an origin**, or the cookie needs CORS configured with
credentials. Both run on the same Pi, so same-origin is the expected deployment and the simpler
one.

**Sessions accumulate.** The table needs an expiry index and a periodic cleanup, which is small
work for `CardStock.Worker` (D-039) once it exists.

**Mail deliverability becomes a support surface.** A verification email that lands in spam is
indistinguishable, to the user, from a broken signup — and it is the first screen a new visitor
meets.
