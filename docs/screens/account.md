# Account — logged-out auth screens

**Source of truth:** `CardStock Mockup/Cardstock Account.dc.html` (155 lines), read directly 2026-08-10.
All `:NN` citations in this document refer to that file unless another file is named.
Per `CLAUDE.md` §"Document authority", this prototype is Tier 1. Where a markdown doc disagrees with it,
the HTML wins and the disagreement is recorded in §8.

---

## 1. Identity

| | |
|---|---|
| Screen label | `Account` (`data-screen-label="Account"`, :25) |
| Prototype | `CardStock Mockup/Cardstock Account.dc.html` |
| Composition | **One** Design Composer component holding **five mutually exclusive views** |
| Purpose | Every logged-out entry point: sign in, create an account, request a reset, acknowledge a sent email, set a new password from an email link. Also the only surviving in-app "browse the demo" affordance (:56). |
| Chrome | None. No nav, no search, no account circle. Centered card on an empty page. |

### The five views

| # | View key | `h1` | Rendered by | Derived route (Tier 2) |
|---|---|---|---|---|
| 1 | `signin` | "Sign in" | `sc-if isSignin` :35–59 | `/signin` |
| 2 | `create` | "Create your account" | `sc-if isCreate` :61–72 | `/create` |
| 3 | `forgot` | "Reset your password" | `sc-if isForgot` :74–81 | `/forgot` |
| 4 | `sent` | "Check your email" | `sc-if isSent` :83–88 | **none listed** — see §7 |
| 5 | `reset` | "Set a new password" | `sc-if isReset` :90–99 | `/reset` |

The view enum is declared in the component props (`:112`): `["signin","create","forgot","sent","reset"]`, default `"signin"`.
Booleans are computed at `:139`; resolution order is `state.view ?? props.view ?? 'signin'` (`:134`).

**Routes are not verified.** The prototype has no router — views switch via component state, a prop, and one
hash check. The route column above is copied from `CardStock Mockup/HANDOFF.md:80` (Tier 2, derived). That
line names four routes for five views; view 4 is unrouted. Tier 3 `uploads/CARDSTOCK_UI_SPEC_v1.md:108`
proposes `/login  /register (invite code)  /reset` — superseded, see §8.

### The `#reset` flow (arriving from an email link)

`componentDidMount` (`:115–121`) checks `location.hash === '#reset'` and, if so, sets `view: 'reset'` (`:119`).
That is the whole mechanism: the hash simulates a user clicking the link in a password-reset email and landing
directly on view 5 with no prior in-app navigation. Consequences for implementation:

- The link target must render view 5 **cold** — no session, no prior form submission, nothing in memory.
- The prototype carries **no token** in the hash and **hard-codes** the account address in the body copy (`:92`,
  "You followed a reset link for `o•••@gmail.com`"). A real link must carry a single-use token, and the screen
  must resolve token → masked email server-side before painting.
- Once `state.view` is set by the hash, the `view` prop no longer governs (`:134`) — the hash wins for the
  rest of the page's life.
- Submitting view 5 (`doReset`, `:147`) does **not** sign the user in. It returns to view 1 with `flash: true`,
  which paints the green "Password updated — sign in with your new password." banner (`:37`). The button's
  tooltip says "Save the new password and sign in" (`:98`) — the tooltip and the behaviour disagree; the
  behaviour is re-authentication. Flagged in §7.

---

## 2. Layout

Single centred column, full viewport (`:25`): `min-height:100vh`, `display:flex`, `flex-direction:column`,
`align-items:center`, `justify-content:center`, `padding:40px 16px`, `background:var(--bg)`, base font
Inter 15px, `color:var(--ink)`.

| Band | Lines | Spec |
|---|---|---|
| Brand lockup | :26–32 | Column, `gap:4px`, `margin-bottom:22px`. Row of 32px inline SVG card mark + wordmark "Cardstock" (Inter 700, 25px, `-0.03em`), `gap:12px`. Sub-line "Pokémon card market analytics", 14px, `--mut2`. **Not a link** — the only page where the lockup is inert (confirmed `DESIGN_NOTES.md:138`). |
| Card | :33 | `width:380px; max-width:100%`, `background:var(--card)`, `1px solid var(--line)`, `border-radius:10px`, `padding:22px`. Holds exactly one of the five views. |
| Legal footnote | :102 | "Fan project · not affiliated with Nintendo or The Pokémon Company." 12.5px `--mut2`, `margin-top:16px`. No Terms/Privacy links exist on this screen. |
| Prototype jumper | :103–108 | "Prototype screens:" + `sc-for` over `jumps` (`:149`) rendering 5 text buttons, active one at `--ink`/600 weight. **Not product UI** — a demo affordance and the only untooltipped control in the file (`DESIGN_NOTES.md:155`). Do not port. |

**Typography inside the card:** `h1` Inter Tight 700 19.5px; field labels 13px/600 `--mut`; helper text 12.5px
`--mut2`; body copy 13.5px `--mut`; email addresses JetBrains Mono 13px; inputs 34px tall, `--inbg` fill,
6px radius; primary buttons full-width 34px, `--btn` fill, white text, 15px/600.

**Theme:** a wrapper div applies `{{ themeVars }}` as inline CSS custom properties (`:24`, computed at
`:122–131`); every style reads `var(--x, <light literal>)` so the page paints light before the theme resolves.
Full token map at `:124–129`; the palette table in `DISPLAY_VOCABULARY.md:77–84` matches it exactly for state
colours (see §8 for the chrome tokens that do not match).

---

## 3. Data contract

### 3.1 Inbound

| Name | Source | Type | Default | Notes |
|---|---|---|---|---|
| `view` | prop (`:112`) | enum `signin\|create\|forgot\|sent\|reset` | `signin` | In production this is the route, not a prop. |
| `showError` | prop (`:112`) | bool | `false` | Seeded sign-in failure. In production: the result of a failed credential check. |
| `location.hash` | browser (`:119`) | `#reset` | — | Sets view 5 on mount. |
| `localStorage['cardstock-theme']` | device (`:117`) | `'dark'` | unset ⇒ light | Read only; this screen never writes it. |
| `localStorage['cardstock-cvd']` | device (`:118`) | `'1'` | unset ⇒ off | Read only. |
| masked email | copy (`:85`, `:92`) | string | `o•••@gmail.com` | Seeded. Mask format = first character + `•••` + `@domain`. |
| `sentWhat` | state (`:114`, `:142`) | `'verification' \| 'reset'` | `'verification'` | Single word interpolated into the sent-view sentence (`:85`). |

### 3.2 Form fields

No field in this file has a `name`, `id`, `required`, `minlength`, `pattern`, or any bound value/change
handler. All inputs are uncontrolled. The list below is the complete set.

| View | Field | `type` | `autocomplete` | Line | Validation present in HTML |
|---|---|---|---|---|---|
| signin | Email | `email` | `email` | :44 | Native `type=email` only |
| signin | Password | `password` | `current-password` | :49 | None |
| create | Email | `email` | `email` | :64 | Native `type=email` only |
| create | Password | `password` | `new-password` | :66 | None. Helper copy `:67`: "12+ characters — length beats complexity." |
| create | Confirm password | `password` | `new-password` | :69 | None — **match is not checked** |
| forgot | Email | `email` | `email` | :78 | Native `type=email` only |
| reset | New password | `password` | `new-password` | :94 | None. Same 12+ helper at `:95` |
| reset | Confirm new password | `password` | `new-password` | :97 | None — **match is not checked** |

**There is no invite-code field on any view.** `grep -ic invite` over the file returns **0**.

**Validation to implement** (the HTML states the rule, it does not enforce it):

- Password ≥ 12 characters (`:67`, `:95`). Length is the only stated rule; no complexity requirement, and
  none should be added — the copy explicitly argues against it.
- Confirm-password must equal password (create, reset) — implied by the field existing, never enforced here.
- Email format — native constraint only.
- Everything else (uniqueness, rate limits, token validity) has no representation in the prototype.

### 3.3 Account creation model — as implemented

Confirmed by reading, not inferred:

- **Open signup.** View 2 is reachable from view 1 with no gate: "New to Cardstock? **Create an account**"
  (`:58`, `goCreate`). The form asks for email, password, confirm — nothing else (`:62–71`).
- **No invite code anywhere.** 0 occurrences of "invite" in the file (also 0 in `Cardstock Profile.dc.html`).
- **Email verification is real and is the happy path.** The submit button's tooltip (`:70`) reads
  "Create the account and send a verification link to your email"; `doCreate` (`:145`) transitions to view 4
  with `sentKind: 'verification'`; view 4 renders "We sent a **verification** link to `o•••@gmail.com`.
  It expires in **30 minutes**." (`:85`).
- **Creation never lands in the app.** `doCreate` goes to view 4, never to Home — unlike `doSignin`/`goDemo`
  (`:143–144`), which do. The prototype therefore implies verify-before-use, though it never says so.

This matches `DECISIONS.md` D-011 and D-034. The invite-only/no-verification entry
(`uploads/PROJECT_LOG.md:254`) is contradicted by the HTML on every point. See §8 row 1.

---

## 4. States

### 4.1 View state (mutually exclusive)

Five `sc-if` blocks over five booleans derived from a single `view` string (`:139`). Exactly one renders.
Every view lives in the same 380px card, so the card height is the only thing that changes.

### 4.2 Banner states (view 1 only)

| State | Condition | Copy | Colour tokens | Line |
|---|---|---|---|---|
| Success flash | `state.flash && view === 'signin'` (`:140`) | "Password updated — sign in with your new password." | `--pos` on `--posBg`, 6px radius | :37 |
| Credential error | `props.showError && view === 'signin' && !state.flash` (`:141`) | "That email and password don't match. **Reset your password**" (link → `goForgot`) | `--neg` on `--negBg` | :40 |

Both sit above the `h1`, 13.5px, `padding:8px 10px`, `margin-bottom:14px`. They are **mutually exclusive by
construction** — `showErr` requires `!flash` (`:141`). `flash` is set only by `doReset` (`:147`) and cleared by
any `go()` navigation (`:135`), so it survives exactly one arrival at view 1.

The error is a **single generic message**. There are no field-level errors, no red field borders, and no
distinction between "unknown email" and "wrong password" — preserve that; it is anti-enumeration behaviour.

### 4.3 Sent view (view 4) sub-states

One layout, two meanings, selected by `sentKind`:

| `sentKind` | Set by | Sentence rendered (`:85`) |
|---|---|---|
| `verification` | `doCreate` (`:145`), and the initial state value (`:114`) | "We sent a verification link to `o•••@gmail.com`. It expires in 30 minutes." |
| `reset` | `doForgot` (`:146`) | "We sent a reset link to `o•••@gmail.com`. It expires in 30 minutes." |

Secondary line (`:86`), static: "Nothing after a few minutes? Check spam — or resend in 0:58."
The `0:58` is **hard-coded text**, not a countdown, and there is **no resend control** anywhere in the file.

### 4.4 States that do NOT exist in the HTML

Recorded so nobody "restores" them from memory. None of the following appear anywhere in the file:

- **Expired / used / invalid reset link.** The 30-minute expiry is stated (`:85`) but no expired-link view
  exists. `#reset` always renders the working form.
- **Expired / invalid verification link**, and any post-verification confirmation screen.
- Submitting / pending / disabled-button state on any of the five submits.
- Rate-limit, lockout, or "too many attempts" state.
- "Account deleted" or "signed out" acknowledgement on arrival (both Profile exits land here silently —
  `Cardstock Profile.dc.html:248`, `:254`).
- Field-level validation errors, password-strength meter, or caps-lock hint.
- Unverified-account sign-in refusal.

### 4.5 Theme / colour-vision state

`theme` and `cvd` are read from `localStorage` on mount (`:117–118`) and applied through `themeVars` (`:138`).
There is **no control** for either on this screen — it consumes the device preference set on Profile.
Because the read happens in `componentDidMount`, the page paints light first and re-paints; there is no
pre-paint script in the helmet (`:11–22`). See §8.

---

## 5. Interactions

Every interactive control in the file, in document order. Tooltips are the `title` attribute and are
product copy — 10 controls carry one; only the prototype jumper does not.

| # | Control | View | Line | Handler | Consequence |
|---|---|---|---|---|---|
| 1 | "Reset your password" link inside error banner | 1 | :40 | `goForgot` | → view 3, `flash:false` |
| 2 | "Forgot?" button (right of the Password label) | 1 | :47 | `goForgot` | → view 3. Tooltip: "Send a password reset link to your email" |
| 3 | **Sign in** (primary, full width) | 1 | :50 | `doSignin` (`:143`) | Prototype: `location.href = 'Cardstock Home.dc.html'`. **No credential check.** Production: authenticate, then → Home; on failure re-render view 1 with the error banner. |
| 4 | **Browse the demo →** (secondary, full width) | 1 | :56 | `goDemo` (`:144`) | Same navigation as #3 in the prototype. Tooltip: "Explore the whole app with seeded data — nothing you change is saved". Sub-line `:57`: "Read-only, seeded data — no account needed." |
| 5 | "Create an account" | 1 | :58 | `goCreate` | → view 2. Tooltip: "Create a free account — email and password only" |
| 6 | **Create account** (primary) | 2 | :70 | `doCreate` (`:145`) | → view 4 with `sentKind:'verification'`. Tooltip: "Create the account and send a verification link to your email" |
| 7 | "Sign in" (from "Already have one?") | 2 | :71 | `goSignin` | → view 1, `flash:false` |
| 8 | **Send reset link** (primary) | 3 | :79 | `doForgot` (`:146`) | → view 4 with `sentKind:'reset'`. Tooltip: "Email me a link to set a new password" |
| 9 | "← Back to sign in" | 3 | :80 | `goSignin` | → view 1 |
| 10 | **Back to sign in** (secondary, full width) | 4 | :87 | `goSignin` | → view 1 |
| 11 | **Update password** (primary) | 5 | :98 | `doReset` (`:147`) | → view 1 with `flash:true` (green banner). Tooltip says "…and sign in"; the code does not sign in. |
| 12 | Jumper buttons ×5 | all | :106 | `j.go` (`:149`) | Prototype only — direct view switch. Do not port. |

Divider between #3 and #4: 1px rules with a centred lowercase "or" (`:51–55`).

---

## 6. Rules and invariants

1. **One card, five views.** Never two views at once; the enum at `:112` is the complete state space.
2. **The `sent` view is dual-purpose.** Verification and reset share one layout; only the noun swaps (`:85`).
   Any change to that screen affects both flows.
3. **Both link kinds expire in 30 minutes** — the sentence is shared, so the copy asserts one TTL for
   verification and reset alike (`:85`).
4. **Success and error are mutually exclusive** (`:141`); the success flash always wins.
5. **The credential error is deliberately vague** (`:40`) and always offers the reset path inline.
6. **Masked email on logged-out screens** (`:85`, `:92`): first character + `•••` + `@domain`. The full
   address is only ever shown once signed in (`Cardstock Profile.dc.html:46`, `:120`).
7. **Password guidance is length-only** (`:67`, `:95`) — do not add complexity rules.
8. **This screen reads device preferences, never writes them** (`:117–118`).
9. **The lockup is not a link here** and there is no nav; the page is a dead end except through its own
   controls.
10. **The demo is a first-class affordance on sign-in** (`:56–57`) — the only in-app demo entry left after
    `DESIGN_NOTES.md:141` removed the rest.
11. **No Terms/Privacy consent** exists on the create view; the footer (`:102`) is a disclaimer, not a link.

---

## 7. Open questions

1. **Route for the `sent` view.** `HANDOFF.md:80` gives four routes for five views. Options: a real route
   (`/check-email`), or a non-routable state of `/create` and `/forgot`. Undecided by any source.
2. **Reset/verify link format.** No token appears anywhere (`:119` is a bare `#reset`). Need: token shape,
   TTL enforcement (copy says 30 min), single-use semantics, and the token → masked-email lookup that
   populates `:92`.
3. **No expired/used-link screen is designed.** This is the largest gap in the file. Copy, layout, and the
   recovery path ("request a new link") all need authoring.
4. **Resend.** Copy promises "resend in 0:58" (`:86`) but there is no button and no timer. Need: cooldown
   length, whether the countdown is live, and where the resend control sits.
5. **Is verification enforced?** `doCreate` never reaches Home, implying verify-before-use, but nothing states
   whether an unverified user can sign in, and no "please verify" state exists on view 1.
6. **Does `doSignin` on success ever land somewhere other than Home?** The prototype hard-codes Home
   (`:143`); return-URL behaviour after a deep link is undesigned.
7. **Demo session semantics.** "Read-only, seeded data" (`:57`) is a claim this screen cannot implement.
   Tier 3 `CARDSTOCK_UI_SPEC_v1.md:246` describes a seeded demo `user_id` with writes intercepted; that is
   historical, not current, and needs a decision.
8. **Is 12 characters a hard minimum or advice?** (`:67`, `:95`.)
9. **Terms/Privacy acceptance at signup**, given `Cardstock Legal.dc.html` exists and is unlinked from here.
10. **Rate limiting / lockout** on sign-in and on reset requests — no state exists for either.
11. **Tooltip vs behaviour on `:98`** ("Save the new password and sign in" vs returning to sign-in). Which is
    right? The banner copy at `:37` argues for re-authentication.

---

## 8. Contradictions found

| Claim | Source doc:line | What the HTML actually does |
|---|---|---|
| "Open public signup **REVERSED → invite-only**. Registration behind an invite code (friends only); **no verification emails**; minimal password reset." | `CardStock Mockup/uploads/PROJECT_LOG.md:254` (quoted at `DECISIONS.md:287`) | **Contradicted on every point.** `grep -ic invite` over `Cardstock Account.dc.html` = **0**. The create view collects email + password + confirm only (`:62–71`) and is linked openly from sign-in (`:58`). Verification email is the built flow: `:70` tooltip "…send a verification link to your email"; `doCreate` sets `sentKind:'verification'` (`:145`); `:85` renders "We sent a verification link… It expires in 30 minutes." The HTML implements **open signup with email verification** — i.e. `DECISIONS.md` D-011/D-034 (`:284–292`) are correct and the log entry is the outlier. |
| "**Access:** invite-only registration (email + password behind an invite code)" | `uploads/CARDSTOCK_UI_SPEC_v1.md:49` | No invite field, no invite copy, no invite gate (0 occurrences). |
| "Register: invite code + email + password (+confirm)… **No email verification round-trip in v1** (invite code is the gate)." | `uploads/CARDSTOCK_UI_SPEC_v1.md:148` | The verification round-trip *is* the create flow (`:70`, `:145`, `:85`). No invite code exists. |
| "States: error = **inline field messages** ('Invite code not recognized'), never toast-only." | `uploads/CARDSTOCK_UI_SPEC_v1.md:150` | One card-level banner above the `h1`, one generic message, no field-level messaging anywhere (`:40`). |
| Routes `/login  /register (invite code)  /reset`; "Auth shell" | `uploads/CARDSTOCK_UI_SPEC_v1.md:108`, `:145` | View keys are `signin / create / forgot / sent / reset` (`:112`, `:139`) — five, not three. `HANDOFF.md:80` maps `/signin /create /forgot /reset`. |
| "`/signin` `/create` `/forgot` `/reset` — **5 logged-out views**" | `CardStock Mockup/HANDOFF.md:80` | Four routes listed for five views; the `sent` view ("Check your email", `:83–88`) has no route. |
| "Account (**all 7 auth actions** incl. demo-browse)" tooltipped | `CardStock Mockup/DESIGN_NOTES.md:153` | **10** controls carry a `title` (lines 47, 50, 56, 58, 70, 71, 79, 80, 87, 98), driven by 8 distinct handlers. The count is stale, the substance (everything but the jumper row is tooltipped, `DESIGN_NOTES.md:155`) is correct. |
| "CVD palette: … neg red→vermillion (**#B44A00** light / **#E8874D** dark)" | `CardStock Mockup/DESIGN_NOTES.md:103` (repeated `:104`) | `:127–128` set `--neg` to **`#CC5F00`** (light CVD) and **`#F5924E`** (dark CVD). `DISPLAY_VOCABULARY.md:81` agrees with the HTML; `DESIGN_NOTES.md:103` is stale. |
| "accent #3B5BD6→#7290EA · button #3B5BD6→#4A66D8" | `CardStock Mockup/DISPLAY_VOCABULARY.md:85` (dark values repeated at `DESIGN_NOTES.md:105`) | `:125` light `--acc`/`--btn` = **`#4A63D0`**, `--accH`/`--btnH` = `#3A4FB8`. `:124` dark `--acc` = **`#8C9BF2`**, `--accH` = `#8CA4F0`, `--btn` = **`#4A63D0`**, `--btnH` = `#AAB6F6`. Neither light nor dark matches the doc. |
| "light mut2 #8A8A86 → dark #A8A8A2" | `CardStock Mockup/DISPLAY_VOCABULARY.md:85` | `:125` light `--mut2` = **`#6B6B66`** (dark `#A8A8A2` does match). `DESIGN_NOTES.md:136` records the darkening; the vocabulary line was never updated. |
| "Chrome shared by every app page: 48px nav …, theme + colorblind tokens, **pre-paint script reading `localStorage`**." | `CardStock Mockup/HANDOFF.md:88` | Account has **no nav at all** and **no pre-paint script**. The helmet holds only `:root[data-theme="dark"]{--logoTeal}` (`:21`); `localStorage` is read in `componentDidMount` (`:117–118`), i.e. after first paint, so this page flashes light before applying a dark preference. (`DESIGN_NOTES.md:102` describes this page correctly; `:104–105` and `HANDOFF.md:88` describe the *other* pages' architecture and over-generalise to this one.) |
