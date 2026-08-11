# Screen: Account (logged-out auth shell)

**Source of truth:** `CardStock Mockup/Cardstock Account.dc.html` (155 lines), read directly 2026-08-10.
Every line citation below is `:N` against that file unless another file is named.
Tier 1 per `CLAUDE.md` §"Document authority" — where a markdown doc disagrees with this file, the file wins.

---

## 1. Identity

| | |
|---|---|
| **Screen label** | `Account` — `data-screen-label="Account"` (`:25`) |
| **File** | `CardStock Mockup/Cardstock Account.dc.html` |
| **Purpose** | The entire logged-out surface. Five mutually exclusive views inside one 380px card: sign in, create account, request reset, "check your email", set a new password. |
| **Chrome** | **None.** No 48px nav, no search, no account circle. This is the only app-adjacent screen with no shared chrome — the brand lockup at `:26–32` stands in for it. |

### 1.1 The five views and their routes

The prototype addresses views by a `view` state string, not by URL. Only one URL fragment is
implemented in the file: `#reset` (`:119`). Route names below come from `HANDOFF.md:80`
(Tier 2, derived) mapped onto the view keys that actually exist in `:112` / `:139`.

| # | View key (`:112`) | `sc-if` flag | Renders at | Documented route | Reached by |
|---|---|---|---|---|---|
| 1 | `signin` | `isSignin` | `:35–59` | `/signin` | default (`:134`); `goSignin` (`:148`) |
| 2 | `create` | `isCreate` | `:61–72` | `/create` | `goCreate` from `:58` |
| 3 | `forgot` | `isForgot` | `:74–81` | `/forgot` | `goForgot` from `:47` or the error banner link `:40` |
| 4 | `sent` | `isSent` | `:83–88` | **none documented** | `doCreate` (`:145`), `doForgot` (`:146`) |
| 5 | `reset` | `isReset` | `:90–99` | `/reset`, entered as `#reset` | `location.hash === '#reset'` on mount (`:119`) |

`sent` is a real, distinct view with its own heading and copy but no route in any document —
see §7. `HANDOFF.md:80` says "5 logged-out views" and then lists four routes.

### 1.2 The signup question — what the HTML actually implements

This settles the live decision. All three findings are from the file, not from a document:

- **Account creation is OPEN.** The create view is linked in plain sight from the bottom of the
  sign-in card — *"New to Cardstock? **Create an account**"* (`:58`), tooltip *"Create a free
  account — email and password only."* Nothing gates it.
- **There is NO invite-code field, and no invite anything.** `grep -ic invite` over the file
  returns **0**. (Same grep over `Cardstock Profile.dc.html`: also **0**.) No invite input, no
  invite copy, no "private beta" explanation, no invite gate on any of the five views.
- **Email verification IS implemented.** Three independent receipts: the Create account button's
  tooltip is *"Create the account and send a **verification** link to your email"* (`:70`);
  `doCreate` sets `sentKind: 'verification'` and switches to the `sent` view (`:145`); the `sent`
  view renders *"We sent a {{ sentWhat }} link to o•••@gmail.com. It expires in 30 minutes."*
  (`:85`), where `sentWhat` is that `sentKind` (`:142`).

**Conclusion: the HTML implements public open signup with email + password and a verification
email.** That agrees with `DECISIONS.md` D-011 and D-034 (`:284–292`) and contradicts the older
`PROJECT_LOG.md:254` reversal on every point. See §8 rows 1–4.

---

## 2. Layout

Single centered column, no grid, no responsive breakpoints (only `max-width: 100%` on the card).

1. **Page frame** (`:25`) — `min-height: 100vh`, flex column, `align-items: center`,
   `justify-content: center`, `padding: 40px 16px`, `background: var(--bg)`, `color: var(--ink)`,
   base font Inter 15px. Vertically centered, so short views float mid-viewport.
2. **Brand lockup** (`:26–32`) — flex column, `gap: 4px`, `margin-bottom: 22px`.
   - Row: 32px inline SVG mark (two rotated card rects + teal sparkline polyline + endpoint dot,
     `:28`) + wordmark **Cardstock**, Inter 700 / 25px / `-0.03em` (`:29`), `gap: 12px`.
   - Sub-line: "Pokémon card market analytics", 14px, `var(--mut2)` (`:31`).
3. **The card** (`:33`) — `width: 380px; max-width: 100%`, `background: var(--card)`,
   `1px solid var(--line)`, `border-radius: 10px`, `padding: 22px`. **All five views render inside
   this one card**; the card itself never changes size or chrome between views.
4. **Legal footnote** (`:102`) — "Fan project · not affiliated with Nintendo or The Pokémon
   Company.", 12.5px `var(--mut2)`, `margin-top: 16px`.
5. **Prototype jump rail** (`:103–108`) — "Prototype screens:" followed by five buttons rendered
   from `jumps` (`:149`): `sign in · create · forgot · email sent · reset`. The active one is
   `var(--ink)` / weight 600, the rest `var(--mut2)` / weight 400 (`:136`).
   **Prototype scaffolding — do not ship.**

### 2.1 Internal rhythm inside the card (constant across views)

| Element | Spec | Lines |
|---|---|---|
| View heading `h1` | Inter Tight 700, 19.5px, `margin: 0` | `:42`, `:62`, `:75`, `:84`, `:91` |
| Sub-copy under heading | 13.5px `var(--mut)`, `margin-top: 8px` | `:76`, `:85`, `:92` |
| Field label | 13px, weight 600, `var(--mut)`, `margin: 14px 0 5px` | `:43`, `:63`, `:65`, `:68`, `:77`, `:93`, `:96` |
| Text input | full width, **34px** high, `1px solid var(--line)`, radius 6, `background: var(--inbg)`, `padding: 0 10px`, Inter 15px | `:44`, `:49`, `:64`, `:66`, `:69`, `:78`, `:94`, `:97` |
| Helper text under input | 12.5px `var(--mut2)`, `margin-top: 5px` | `:67`, `:95` |
| Primary button | full width, 34px, `var(--btn)` on white text, radius 6, Inter 600 15px, `margin-top: 18px`, hover `var(--btnH)` | `:50`, `:70`, `:79`, `:98` |
| Secondary button | same box, `var(--card)` fill + `1px solid var(--line)`, `var(--ink)` text, hover `var(--hov)` | `:56`, `:87` |
| Inline text button | transparent, no border, no padding, Inter 13.5px `var(--acc)` | `:58`, `:71`, `:80` |
| Banner (flash / error) | 13.5px, radius 6, `padding: 8px 10px`, `margin-bottom: 14px`, above the `h1` | `:37`, `:40` |
| Masked email | JetBrains Mono 13px inline span | `:85`, `:92` |
| "or" divider | two 1px `var(--line)` rules flanking a 12.5px `var(--mut2)` "or", `margin: 16px 0` | `:51–55` |

Fonts loaded (`:14`): Inter 400/500/600/700, Inter Tight 600/700, JetBrains Mono 400/500/600.
Global focus ring: `2px solid var(--acc)`, `outline-offset: 1px`, `border-radius: 2px` (`:20`).

---

## 3. Data contract

### 3.1 Props (Design Composer editor knobs, `:112`)

| Prop | Editor | Options | Default | Effect |
|---|---|---|---|---|
| `view` | enum | `signin`, `create`, `forgot`, `sent`, `reset` | `signin` | Initial view; overridden by `state.view` once the user navigates (`:134`) |
| `showError` | boolean | — | `false` | Arms the sign-in credential-error banner (`:141`) |

Both are `section: "Prototype"` — they exist to make states inspectable, not as production inputs.

### 3.2 Component state (`:114`)

| Key | Initial | Meaning |
|---|---|---|
| `view` | `null` | Current view; `null` means "fall through to `props.view`" (`:134`) |
| `sentKind` | `'verification'` | Which link the `sent` view claims to have mailed — `'verification'` or `'reset'` |
| `flash` | `false` | Post-reset success banner armed on the sign-in view |
| `theme` | `null` | `'dark'` if `localStorage['cardstock-theme'] === 'dark'` (`:117`), else light |
| `cvd` | `null` | `true` if `localStorage['cardstock-cvd'] === '1'` (`:118`), else standard palette |

### 3.3 Form fields — the complete inventory

**No `<form>` element exists anywhere in the file** (0 occurrences), and **no input carries
`required`, `minlength`, `maxlength`, `placeholder`, `name`, `id`, or a value binding** (0
occurrences). Every input is an unbound, unvalidated control. All validation described below is
**copy only** — the prototype states the rules, it does not enforce them.

| View | Field | `type` | `autocomplete` | Line | Stated validation |
|---|---|---|---|---|---|
| signin | Email | `email` | `email` | `:44` | none stated |
| signin | Password | `password` | `current-password` | `:49` | none stated |
| create | Email | `email` | `email` | `:64` | none stated |
| create | Password | `password` | `new-password` | `:66` | **"12+ characters — length beats complexity."** (`:67`) |
| create | Confirm password | `password` | `new-password` | `:69` | no helper text; match rule implied by the label only |
| forgot | Email | `email` | `email` | `:78` | none stated |
| reset | New password | `password` | `new-password` | `:94` | **"12+ characters — length beats complexity."** (`:95`) |
| reset | Confirm new password | `password` | `new-password` | `:97` | no helper text |
| sent | — | — | — | — | no fields |

**The password policy is exactly one rule: minimum length 12, no complexity requirement.** It is
shown on `create` and `reset` and deliberately *not* shown on `signin`. Identical wording in both
places — treat it as a shared literal.

### 3.4 Literal content that is illustrative, not structural

| Value | Line | Note |
|---|---|---|
| `o•••@gmail.com` | `:85`, `:92` | Hardcoded seeded email. **Masking format is the contract:** first character, `•••`, then the full domain including TLD. |
| `It expires in 30 minutes.` | `:85` | Applies to **both** link kinds, because the `sent` view is shared. |
| `resend in 0:58` | `:86` | Static text. No timer, no resend control — see §7. |

### 3.5 Server-side data this screen implies (not in the HTML)

Nothing on this page reads product data. What it implies the backend must own: a user record
keyed by email with a password hash; a verification token and a reset token, both with a
**30-minute** TTL (`:85`); a resend throttle (`:86`); and the account's theme/CVD prefs are *not*
here — they live in `localStorage` (`:117–118`), i.e. per device.

---

## 4. States

### 4.1 View selection

`view = state.view ?? (props.view ?? 'signin')` (`:134`). The five `sc-if` blocks test
`view === '<key>'` (`:139`), so exactly one renders — mutual exclusion is structural, not
coincidental. Every navigation helper `go(v)` sets `{ view: v, flash: false }` (`:135`).

### 4.2 Sign in — three sub-states

| Sub-state | Condition | Renders |
|---|---|---|
| **Default** | neither below | heading + 2 fields + Sign in + "or" + demo + create link |
| **Success flash** | `flashOn = state.flash && view === 'signin'` (`:140`) | Green banner above the heading: **"Password updated — sign in with your new password."** — `var(--pos)` on `var(--posBg)` (`:37`) |
| **Credential error** | `showErr = props.showError && view === 'signin' && !state.flash` (`:141`) | Red banner above the heading: **"That email and password don't match. [Reset your password]"** — `var(--neg)` on `var(--negBg)`, the link calls `goForgot` (`:40`) |

**Precedence is explicit:** `!state.flash` in `:141` means the success flash **suppresses** the
error banner. The two can never render together. Only one banner slot exists.

**The error is prop-driven, not behavioural.** `doSignin` (`:143`) navigates to Home
unconditionally; nothing in the prototype ever *sets* an error. The banner is a designed state
awaiting a real credential check.

**Error granularity:** one card-level banner, one generic message that distinguishes neither
field. There is no field-level error styling anywhere in the file (no red border variant, no
per-input message slot).

### 4.3 Create account
Single state. No pending/submitting state, no "email already registered" state, no
confirm-mismatch state, no password-too-short state. `doCreate` (`:145`) transitions
unconditionally to `sent` with `sentKind: 'verification'`.

### 4.4 Forgot password
Single state, sub-copy *"Enter your account email and we'll send a reset link."* (`:76`).
`doForgot` (`:146`) transitions unconditionally to `sent` with `sentKind: 'reset'`.
**No "unknown email" state exists** — the success view is shown regardless, which is correct
anti-enumeration behaviour but is not stated as a rule anywhere (§7).

### 4.5 Check your email — two variants, one template
Heading "Check your email" (`:84`). Body interpolates `{{ sentWhat }}` (`:85`):

| `sentKind` | Set by | Renders |
|---|---|---|
| `'verification'` | `doCreate` (`:145`), and the initial state (`:114`) | "We sent a **verification** link to o•••@gmail.com. It expires in 30 minutes." |
| `'reset'` | `doForgot` (`:146`) | "We sent a **reset** link to o•••@gmail.com. It expires in 30 minutes." |

Secondary line (`:86`): "Nothing after a few minutes? Check spam — or resend in 0:58."
Only control: a secondary **Back to sign in** button (`:87`).
Note `go()` never resets `sentKind`, so jumping straight to `sent` from the prototype rail shows
whichever kind was last set, defaulting to `verification`.

### 4.6 Set a new password (arriving from the email link)

**This is the `#reset` flow.** On mount, `componentDidMount` checks
`if (location.hash === '#reset') upd.view = 'reset'` (`:119`) and switches the view. That single
line is the entire simulation of "the user clicked the link in their email": open
`Cardstock Account.dc.html#reset` and the card renders the set-new-password view instead of
sign-in. It is an exact-match string test on the hash — no token is parsed, no expiry is checked,
and the email shown (`:92`) is the same hardcoded literal as the `sent` view.

The view renders: heading "Set a new password" (`:91`); context line **"You followed a reset link
for o•••@gmail.com."** (`:92`) — i.e. the address is known from the link, not typed; two password
fields (`:94`, `:97`) with the 12+ helper (`:95`); and one primary **Update password** button
(`:98`), tooltip *"Save the new password and sign in."*

`doReset` (`:147`) sets `{ view: 'signin', flash: true }` — so the flow terminates on the sign-in
view showing the green "Password updated" banner (§4.2). **The user is not auto-signed-in**; the
tooltip says "and sign in" but the implemented behaviour is "return to sign in and re-authenticate."

**There is no expired-link state.** The copy promises a 30-minute expiry (`:85`) but no view,
banner, or branch handles an expired, already-used, or malformed token. See §7.
**There is also no escape hatch** — `reset` is the only view with no back-link to sign in.

### 4.7 Theme and colorblind state
Read once, on mount, from `localStorage`: `cardstock-theme === 'dark'` → dark (`:117`);
`cardstock-cvd === '1'` → CVD palette (`:118`). Defaults are light and standard (`:138`).
`vars(dark, cvd)` (`:122–131`) returns a token object applied to a `display: contents` wrapper
(`:24`) with `colorScheme` set to match. Four palettes exist: light/standard, light/CVD,
dark/standard, dark/CVD — the semantic pair `--pos`/`--neg` (plus `--posBg`/`--negBg`) is what CVD
swaps (`:127–128`); `--warn` is unchanged by CVD (`:129`).

**No theme control exists on this screen** — it only consumes what Profile wrote.

**Flash of light theme.** The helmet (`:15–22`) contains **no pre-paint script** (0 `<script>`
tags in the helmet); the only dark rule there is `:root[data-theme="dark"] { --logoTeal }` (`:21`)
and nothing ever sets `data-theme`. Theme is therefore applied *after* mount via `themeVars`, so a
dark-mode user gets a light first paint. App pages solve this with a pre-paint script
(`HANDOFF.md:88`); Account does not. Flagged in §8.

---

## 5. Interactions

Every interactive control in the file, in document order. All ten `title` attributes are static
tooltips describing **consequence**, per `DESIGN_NOTES.md:150` — they cover 7 distinct auth
actions plus 3 identical back-links (`DESIGN_NOTES.md:153` calls this "all 7 auth actions incl.
demo-browse").

| # | Control | View | Line | Handler | Tooltip | Consequence |
|---|---|---|---|---|---|---|
| 1 | **Forgot?** (inline, right of Password label) | signin | `:47` | `goForgot` | "Send a password reset link to your email" | → `forgot`, `flash: false` |
| 2 | **Sign in** (primary) | signin | `:50` | `doSignin` (`:143`) | "Sign in to your binder, screens, and watchlist" | `location.href = 'Cardstock Home.dc.html'` — **unconditional**, no credential check |
| 3 | **Browse the demo →** (secondary) | signin | `:56` | `goDemo` (`:144`) | "Explore the whole app with seeded data — nothing you change is saved" | `location.href = 'Cardstock Home.dc.html'` — **byte-identical target to Sign in**; no demo flag is passed |
| 4 | **Create an account** (inline text) | signin | `:58` | `goCreate` | "Create a free account — email and password only" | → `create` |
| 5 | **Reset your password** (link in error banner) | signin | `:40` | `goForgot` | — | → `forgot` |
| 6 | **Create account** (primary) | create | `:70` | `doCreate` (`:145`) | "Create the account and send a verification link to your email" | → `sent`, `sentKind: 'verification'` |
| 7 | **Sign in** (inline text) | create | `:71` | `goSignin` | "Back to the sign-in screen" | → `signin` |
| 8 | **Send reset link** (primary) | forgot | `:79` | `doForgot` (`:146`) | "Email me a link to set a new password" | → `sent`, `sentKind: 'reset'` |
| 9 | **← Back to sign in** (inline text) | forgot | `:80` | `goSignin` | "Back to the sign-in screen" | → `signin` |
| 10 | **Back to sign in** (secondary, full width) | sent | `:87` | `goSignin` | "Back to the sign-in screen" | → `signin` |
| 11 | **Update password** (primary) | reset | `:98` | `doReset` (`:147`) | "Save the new password and sign in" | → `signin`, `flash: true` (green success banner) |
| 12 | 5 × prototype jump buttons | all | `:106` | `go(v)` (`:135`) | — | → that view, `flash: false`. **Prototype only.** |

### 5.1 Not present
No keyboard `Enter`-to-submit (no `<form>`, so the browser default does not apply). No OAuth /
social sign-in buttons. No "remember me". No password-visibility toggle. No CAPTCHA. No
terms-of-service acceptance checkbox on create. No resend button on `sent`.

---

## 6. Rules and invariants

1. **Exactly one of five views renders.** Enforced by a single `view` string against five
   equality tests (`:139`), not by five independent booleans.
2. **Account creation is open to anyone.** No gate, no code, no allowlist (§1.2). `grep -ic
   invite` = **0**.
3. **Email verification is part of account creation.** `create` → `sent(verification)` is the only
   path out of the create view (`:145`).
4. **Both emailed links expire in 30 minutes** (`:85`) — the shared `sent` view makes this a
   single rule covering verification and reset alike.
5. **Password policy is length-only: 12+ characters.** Identical copy on `create` (`:67`) and
   `reset` (`:95`), absent on `signin`. No complexity, no character-class rules.
6. **Errors are card-level, not field-level.** One banner slot above the `h1`; one generic
   message that names neither field (`:40`).
7. **Success suppresses error.** `!state.flash` in `:141` guarantees the two banners are
   mutually exclusive.
8. **The reset flow ends at sign-in, not signed-in.** `doReset` returns to `signin` with a flash
   (`:147`).
9. **The forgot flow never confirms whether an address exists** — `doForgot` shows `sent`
   unconditionally (`:146`).
10. **Email addresses are always masked** in system copy: `o•••@gmail.com` (`:85`, `:92`).
11. **The demo needs no account.** "Read-only, seeded data — no account needed." (`:57`).
12. **No chrome.** No nav, no search, no account circle — the brand lockup is the only header.
13. **Theme and CVD are read, never written, here.** `localStorage` keys `cardstock-theme` and
    `cardstock-cvd` (`:117–118`) — device-scoped, and this screen has no control to change them.
14. **Every inline style reads `var(--x, <light literal>)`** so a streaming first paint is light
    and correct (`DESIGN_NOTES.md:102`); verified throughout `:25–108`.
15. **Card width is fixed at 380px**, `max-width: 100%` (`:33`) — the only responsive concession
    on the page.

---

## 7. Open questions

1. **What route serves the `sent` view?** `HANDOFF.md:80` lists four routes for five views. Is
   `sent` a route (`/check-email`), a query state on `/create` and `/forgot`, or a non-addressable
   in-page state? The prototype makes it non-addressable except via the jump rail.
2. **Expired / already-used / malformed reset link — undesigned.** The 30-minute expiry is
   promised (`:85`) but no state handles arriving after it. Same gap for the verification link.
   This is the single largest missing state on the screen.
3. **Resend is copy without a control.** "resend in 0:58" (`:86`) is static text; there is no
   resend button, no live countdown, and no stated cooldown length (0:58 implies a 60-second
   window but that is inference, not a receipt).
4. **`#reset` carries no token.** The prototype matches the literal hash (`:119`) and hardcodes
   the email (`:92`). Production needs a token in the URL — path segment, query, or fragment — and
   a decision on whether it is single-use.
5. **No validation states are designed** for: confirm-password mismatch, password under 12 chars,
   malformed email, email already registered, or unknown email on forgot. Given rule 6
   (card-level banners only), do these all become one banner, or does the design need a
   field-level error treatment it currently lacks?
6. **No pending/submitting state** on any of the four primary buttons. Every mailing action is a
   network round trip; none has a disabled/spinner variant.
7. **No rate-limit or lockout state** for repeated failed sign-ins. `DECISIONS.md:276` and D-011
   make this a public-internet surface.
8. **"Browse the demo" and "Sign in" go to the identical URL** (`:143`, `:144`) with no demo flag.
   `DESIGN_NOTES.md:141` records that the in-app demo affordance — `demoMode` prop, DEMO nav chip,
   the "sign in to save" nudge — was **deleted** from Home/Browse/Binder/Card/Set/Character. So
   what does a demo visitor actually get, and how do they leave it?
9. **Post-verification landing is undesigned.** Clicking the verification link has no destination
   view (unlike reset, which has `#reset`).
10. **The reset view has no way back to sign in** (`:90–99`) — deliberate, or an omission?
11. **Dark-mode users get a light first paint** on this page (§4.7). Add a pre-paint script, or
    accept the flash on the logged-out surface?
12. **`doReset`'s tooltip says "and sign in"** (`:98`) but the behaviour returns to the sign-in
    form (`:147`). Should a successful reset establish a session?
13. **No terms/privacy acceptance on create.** `Cardstock Legal.dc.html` exists (`HANDOFF.md`) but
    nothing on this screen links to it, and the only footer text is the fan-project disclaimer
    (`:102`).

---

## 8. Contradictions found

| Claim | Source doc:line | What the HTML actually does |
|---|---|---|
| "Open public signup **REVERSED → invite-only.** Registration behind an **invite code** (friends only); **no verification emails**; minimal password reset." | `CardStock Mockup/uploads/PROJECT_LOG.md:254` (Tier 3; quoted at `DECISIONS.md:287`) | **Contradicted on every point.** `grep -ic invite` over the file = **0**. Create collects email + password + confirm only (`:62–71`) and is openly linked from sign-in (`:58`). Verification email is the built flow: `:70`, `:145`, `:85`. `DECISIONS.md` D-011/D-034 (`:284–292`) already ruled this entry the outlier; the HTML independently confirms it. |
| "**Access:** invite-only registration (email + password behind an invite code)" | `uploads/CARDSTOCK_UI_SPEC_v1.md:49` | No invite field, no invite copy, no gate anywhere. 0 occurrences of "invite". |
| "Register: **invite code** + email + password (+confirm)… **No email verification round-trip in v1** (invite code is the gate)." | `uploads/CARDSTOCK_UI_SPEC_v1.md:148` | The verification round-trip **is** the create flow (`:70` tooltip, `:145` `sentKind:'verification'`, `:85` "We sent a verification link… expires in 30 minutes"). No invite code exists. |
| "**Purpose:** invite-only entry" / "Demo users hitting `/register` see the invite explanation ('This is a private beta — registration needs an invite code')." | `uploads/CARDSTOCK_UI_SPEC_v1.md:147`, `:150` | No private-beta copy, no invite explanation, no gated `/register` variant. The create view is unconditional. |
| "States: error = **inline field messages** ('Invite code not recognized'), never toast-only." | `uploads/CARDSTOCK_UI_SPEC_v1.md:150` | One **card-level** banner above the `h1` with one generic message — "That email and password don't match." (`:40`). No field-level error styling exists anywhere in the file. |
| Routes `/login  /register (invite code)  /reset` — "Auth shell", 3 routes | `uploads/CARDSTOCK_UI_SPEC_v1.md:108` | View keys are `signin / create / forgot / sent / reset` (`:112`, `:139`) — **five** views, and the names differ (`signin` not `login`, `create` not `register`). `HANDOFF.md:80` is closer but still lists only four routes. |
| Secondary landing CTA "Sign in / **I have an invite**" | `uploads/CARDSTOCK_UI_SPEC_v1.md:140`, `:421` | Nothing on the auth shell accepts an invite. Any landing page carrying that CTA would dead-end. |
| "5 logged-out views" with routes `/signin /create /forgot /reset` | `CardStock Mockup/HANDOFF.md:80` | Five views exist (`:112`) but only four routes are named — the `sent` view (`:83–88`) has no route. Internally inconsistent within one table cell; the HTML is the reason we can tell. |
| "Demo mode — sign in with an invite to save" nudge on all write actions; slim "DEMO" nav tag; seeded demo `user_id` | `uploads/CARDSTOCK_UI_SPEC_v1.md:246`, `:100` | `DESIGN_NOTES.md:141` records the whole demo affordance was deleted from the six app pages. Account still offers "Browse the demo →" (`:56`) but `goDemo` (`:144`) navigates to the same URL as `doSignin` (`:143`) with **no demo flag of any kind**. The demo is an unimplemented promise, not just a stale doc. |
| "Chrome shared by **every** app page: 48px nav…, **pre-paint script reading `localStorage`**" | `CardStock Mockup/HANDOFF.md:88` | Account has neither. No nav (`:25–33`), and the helmet (`:15–22`) contains **zero** `<script>` tags — theme is applied post-mount (`:117–120`), so dark-mode users see a light flash. (Account is listed in the same screen table at `:80`; whether "app page" was meant to exclude it is exactly the ambiguity.) |
| New table `invites (code, created_by, used_by, used_at)` in the schema | `uploads/CARDSTOCK_UI_SPEC_v1.md:149`, `:382` | No UI writes or reads it. `users` and `password_resets` from the same line are still implied by the built flows; `invites` is not. |
