# Screen: Profile & settings

**Source of truth:** `CardStock Mockup/Cardstock Profile.dc.html` (260 lines), read directly 2026-08-10.
Every `:N` citation below is against that file unless another file is named.
Tier 1 per `CLAUDE.md` §"Document authority" — where a markdown doc disagrees, the file wins.

---

## 1. Identity

| | |
|---|---|
| **Screen label** | `Profile` — `data-screen-label="Profile"` (`:27`) |
| **File** | `CardStock Mockup/Cardstock Profile.dc.html` |
| **Route** | `/settings` (`HANDOFF.md:79`). The HTML defines no route and reads no hash — unlike Account, there is no `location.hash` branch. **One route, one screen, no sub-routes.** |
| **Purpose** | The whole authenticated settings surface: profile identity, appearance, account management, and destructive actions. Per `DESIGN_NOTES.md:99` (user ruling) *"only logged-out flows get screens; change email/password, delete account, sign out are INLINE on Profile."* |
| **Sections** | Four cards: **Profile** (`:49`), **Appearance** (`:79`), **Account** (`:114`), **Danger zone** (`:178`) — `aria-label` on each `<section>`. |
| **Entered from** | The account circle in the 48px nav of every app page (`DESIGN_NOTES.md:107`, "all 8 pages"). |

---

## 2. Layout

1. **Nav — 48px, sticky, `z-index: 20`** (`:29–41`), `background: var(--card)`, 1px bottom rule,
   `gap: 24px`, `padding: 0 20px`. Identical to every other app page. Contents in order:
   - Logo lockup → `Cardstock Home.dc.html`, `aria-label="Cardstock home"`: 24px inline SVG mark
     + wordmark Inter 700 / 18px / `-0.03em` (`:30`).
   - Five section links, 15px weight 500 `var(--mut)`, `padding: 0 12px`, each with a transparent
     2px bottom border reserved for the active state: **Home · Screener · Charts · Binder ·
     Browse** (`:32–36`). **No link to Profile itself** — and none of the five is styled active,
     because Profile is not one of the five sections.
   - `flex: 1` spacer (`:38`), then `<cardstock-search>` (`:39`, from `./cardstock-search.js` at `:16`).
   - Account circle (`:40`): 28px round, `1px solid var(--acc)`, `var(--mutbg)` fill, `var(--acc)`
     text, weight 600, initial **`O`**. On this screen it is a **`<span>`, not a link**, with
     `title="You are here"` — the self-reference is deliberately inert.
2. **Content column** (`:43`) — `max-width: 760px`, centered, `padding: 28px 24px 72px`,
   flex column, `gap: 16px`. Narrower than the data screens; this is a reading-width form page.
3. **Page header** (`:44–47`) — `h1` "Profile & settings", Inter Tight 700 / 27px, `margin: 0 0 4px`;
   sub-line 13.5px `var(--mut2)`: "Signed in as `otto@gmail.com` · member since Mar 2026", email in
   JetBrains Mono 13px.
4. **Four section cards**, all identical shells: `var(--card)` fill, `1px solid var(--line)`,
   `border-radius: 8px`, `padding: 18px 20px`. Each `h2` is Inter Tight 700 / 17.5px.
   Profile and Appearance use `margin: 0 0 14px` on the `h2`; Account and Danger zone use
   `0 0 6px` because their first row supplies its own padding.
5. **Delete modal** (`:187–200`) — rendered outside the content column, `position: fixed; inset: 0`,
   `z-index: 100`, scrim `rgba(15,15,12,0.45)`, flex-centered, `padding: 16px`.
   Dialog: `width: 420px; max-width: 100%`, `var(--card)`, 1px `var(--line)`, radius 10, padding 20.

### 2.1 Section internals

- **Profile** (`:51–76`) — flex row, `gap: 20px`, `align-items: flex-start`: a 72×72 `<image-slot>`
  avatar (`:52`) beside a **two-column grid** (`1fr 1fr`, `gap: 0 16px`) holding Display name and
  Timezone, with the action row spanning `grid-column: 1 / -1` (`:69`).
- **Appearance** (`:81–111`) — two justified rows (label block left, control right); the second row
  is separated by `border-top: 1px solid var(--line)` + `margin-top: 10px` (`:91`). Then the
  preview strip (`:100–111`): its own bordered box on `var(--bg)`, an uppercase 12.5px
  `letter-spacing: 0.06em` "PREVIEW" eyebrow, and a wrapping flex row of chips.
- **Account** (`:117–175`) — three justified rows (Email, Password, Session), each with a
  title/value block left and a 30px secondary button right; rows 2 and 3 carry `border-top`
  (`:146`, `:169`). Expandable panels drop in **below** their row (`:126`, `:139`, `:153`), each
  a bordered `var(--bg)` box with `padding: 12px 14px` and `margin-bottom: 10px`.
- **Danger zone** (`:180–183`) — one justified row: explanatory paragraph left, outlined red button
  right (`flex-shrink: 0`). The `h2` itself is `color: var(--neg)` (`:179`) — the only colored
  heading on the page.

Fonts (`:14`): Inter 400/500/600/700, Inter Tight 600/700, JetBrains Mono 400/500/600.
External components: `./image-slot.js` (`:15`), `./cardstock-search.js` (`:16`).
Focus ring: `2px solid var(--acc)`, offset 1px (`:22`).

---

## 3. Data contract

### 3.1 Props (`:204`)

| Prop | Editor | Default | Section | Effect |
|---|---|---|---|---|
| `emailPending` | boolean | `false` | States | Seeds `emailMode` to `'pending'` (`:227`) |
| `openDeleteModal` | boolean | `false` | States | Seeds the delete modal open (`:228`) |

Both are state-inspection knobs, not production inputs. State overrides them once the user acts
(`?? ` fallthrough at `:227–228`).

### 3.2 Component state (`:206`)

| Key | Initial | Meaning |
|---|---|---|
| `theme` | `null` | `'light'` / `'dark'`; `null` → light (`:226`) |
| `cvd` | `null` | colorblind-safe palette on; `null` → false (`:226`) |
| `savedFlash` | `false` | "Saved ✓" next to Save changes |
| `emailMode` | `null` | `'idle'` / `'open'` / `'pending'` (`:241`) |
| `pwOpen` | `false` | password panel expanded |
| `pwFlash` | `false` | " · updated ✓" next to the password row |
| `delOpen` | `null` | delete modal visible |
| `delText` | `''` | text typed into the DELETE confirm box |
| `tz` | `'America/Chicago'` | selected timezone |

### 3.3 Displayed account facts (seeded, illustrative)

| Field | Value in file | Line | Format contract |
|---|---|---|---|
| Signed-in email | `otto@gmail.com` | `:46`, `:120` | **Unmasked** here (contrast Account, which masks) — JetBrains Mono 13px |
| Member since | `Mar 2026` | `:46` | month + year, no day |
| Avatar initial (nav) | `O` | `:40` | single uppercase char |
| Display name | `Otto` | `:56` | `defaultValue` — uncontrolled input |
| Password last changed | `Mar 2026` | `:149` | month + year; **static text, never updated by `pwUpdate`** |
| Session | `This device · Chicago, IL` | `:172` | device + coarse geo. Exactly one session, always. |
| Pending new email | `otto.new@fastmail.com` | `:141` | hardcoded — does **not** echo what was typed |
| Deletion counts | `12 transactions`, `3 saved screens`, `4 watchlist rows` | `:191` | live counts in production |

### 3.4 Form fields

**No `<form>` element exists.** No input carries `required`, `minlength`, `maxlength`,
`placeholder`, `name`, or `id` (except the avatar's `id="profile-avatar"`). All validation below
is copy, except the DELETE gate which is genuinely enforced.

| Section | Field | Type | Binding | Line | Validation |
|---|---|---|---|---|---|
| Profile | Display name | `text` | `defaultValue="Otto"`, **uncontrolled** — no state, no `onChange` | `:56` | none |
| Profile | Avatar | `<image-slot shape="circle" placeholder="Avatar">` 72×72 | component-managed | `:52` | none stated |
| Profile | Timezone | `<select>` | `value={{tz}}` / `onChange={{setTz}}` | `:60` | closed list, 5 options |
| Account | New email | `email` | unbound | `:129` | none |
| Account | Current password (email change) | `password` | unbound, **no `autocomplete`** | `:131` | none |
| Account | Current password (pw change) | `password` | unbound, `autocomplete="current-password"` | `:156` | none |
| Account | New password | `password` | unbound, `autocomplete="new-password"` | `:158` | **"12+ characters — length beats complexity."** (`:159`) |
| Account | Confirm new password | `password` | unbound, `autocomplete="new-password"` | `:161` | no helper |
| Modal | Type DELETE to confirm | `text`, **JetBrains Mono 14px** | `value={{delText}}` / `onChange={{setDelText}}` | `:193` | `delText === 'DELETE'` — exact, case-sensitive, untrimmed (`:229`) |

**Timezone — the complete option list** (`:61–65`), five values, all US plus UTC:
`America/New_York (ET)` · `America/Chicago (CT)` · `America/Denver (MT)` ·
`America/Los_Angeles (PT)` · `UTC`.
Helper: *"Refresh stamps and daily windows display in this timezone."* (`:67`) — this is the
field's purpose: it formats the `AsOfStamp` values elsewhere in the app.

The 12+ password rule is **word-for-word identical** to `Cardstock Account.dc.html:67`/`:95`.
One shared literal, three places.

### 3.5 Persistence — per DEVICE vs per ACCOUNT

This is the split the docs get wrong. Verified against the writes in `renderVals`:

**Per DEVICE — `localStorage`, written immediately, no Save button:**

| Preference | Key | Values written | Write line | Read line |
|---|---|---|---|---|
| Theme | `cardstock-theme` | `'light'` / `'dark'` | `:234`, `:235` | `:209` (`=== 'dark'`) |
| Colorblind-safe palette | `cardstock-cvd` | `'1'` / `'0'` | `:237` | `:210` (`=== '1'`) |

Both write to `localStorage` **and** `setState` in the same handler, so the change is instant and
survives reload on that browser. The UI says so in its own copy: both theme tooltips read
*"…applies immediately and is **remembered on this device**"* (`:87`, `:88`), and the theme
description says *"Applies across every Cardstock page."* (`:84`).

**Per ACCOUNT — server-side, gated behind an explicit Save:** display name, avatar, and timezone,
all covered by one "Save changes" button whose tooltip is *"Save your display name, avatar, and
timezone"* (`:70`). Plus email (`:117–144`) and password (`:146–167`), each with its own flow.

**Neither, in the prototype:** `doSave` (`:240`) only raises `savedFlash` — it writes nothing.
`tz` lives in component state (`:238`) and resets to `America/Chicago` on reload. The display-name
input is uncontrolled and its value is never read. These are unwired mockup affordances, not a
design statement.

**There is exactly ONE localStorage key pair on this page and neither is a density setting** — see
§3.6.

### 3.6 Density — the control does not exist

`grep -ci density` over `Cardstock Profile.dc.html` returns **0**. So do `compact`, `comfortable`,
`notification`, `alert`, `export`, `2fa`, `two-factor`, and `retention`.

**The Appearance section contains exactly two controls**: the Theme segmented pair (`:86–89`) and
the Colorblind-safe palette switch (`:96–98`), plus a non-interactive preview strip
(`:100–111`). There is no third row, no density selector, and no global density preference
anywhere in the file.

What *does* exist under the name "density" lives on other screens as **per-surface view modes**
held in component state — `terminal | binder` on Screener/Set/Character, `table | gallery` on
Binder holdings (`DISPLAY_VOCABULARY.md:197–202`). Those are local view toggles, not an account or
device preference, and `docs/screens/set.md:173` records that Set's density is not persisted at
all. See §8 rows 1–2.

---

## 4. States

Seven `sc-if` blocks (`:71`, `:122`, `:126`, `:139`, `:149`, `:153`, `:187`).

### 4.1 Appearance — 4 combinations, all live-previewed

`vars(dark, cvd)` (`:214–223`) returns the token object applied to a `display: contents` wrapper
(`:26`) with `colorScheme` set to match.

| dark | cvd | `--pos` / `--neg` | Line |
|---|---|---|---|
| light | off | `#157A50` / `#C13A3A` | `:220` |
| light | on | `#0B69A8` / `#CC5F00` | `:220` |
| dark | off | `#4CC08D` / `#E57B7B` | `:219` |
| dark | on | `#58A9E6` / `#F5924E` | `:219` |

Chrome tokens swap on `dark` only (`:216–217`). `--warn` swaps on `dark` only and is **unchanged
by CVD** (`:221`) — the amber stays amber in colorblind mode, disambiguated by glyph.

**Theme segmented control state** (`:232–233`): the selected half is `var(--btn)` on `#FFFFFF`, the
unselected is `transparent` on `var(--mut)`. Exhaustive and mutually exclusive — exactly one of
Light/Dark is always selected; there is no "system/auto" option.

**CVD switch state** (`:236`): `role="switch"` with `aria-checked={{cvdOn}}` (`:96`); track is
`var(--btn)` when on, `var(--line, #D9D9D4)` when off; the 16px white knob translates
`translateX(16px)` on, `none` off, with a 0.15s transition (`:97`).

**Preview strip** (`:102–109`) — 7 elements, all reading `var()` so they restyle instantly on any
of the four combinations: `▲ RS 94th` (pos) · `▼ EMA 3/9` (neg) · `– RSI 71` (warn) ·
`– MACD –` (muted) · `◌ Churn — 12d` (muted) · `+4.2%` (pos, 14px) · `−1.8%` (neg, 14px).
This is the honest demonstration of the "never color alone" rule — every chip pairs hue with a glyph.

### 4.2 Profile save
Idle, or **Saved ✓** in `var(--pos)` beside the button (`:71–73`). `doSave` (`:240`) sets the flash
and schedules `this._t1` to clear it after **2200 ms**. No pending/submitting state, no error state.

### 4.3 Email change — a three-mode machine

`emailMode = state.emailMode ?? (props.emailPending ? 'pending' : 'idle')` (`:227`).
Modes are exclusive: `emailIdle` / `emailFormOn` / `emailPendingOn` (`:241`).

| Mode | Renders | Lines |
|---|---|---|
| **idle** | Email row with a **Change** button on the right | `:117–125` |
| **open** | Change button **disappears** (it lives inside the `emailIdle` guard at `:122`); a bordered panel opens with New email, Current password, the explainer *"We send a confirmation link to the new address; your sign-in email switches after you click it."* (`:132`), and **Send confirmation** / **Cancel** | `:126–138` |
| **pending** | Amber banner on `var(--warnBg)` in `var(--warn)`: *"Confirmation sent to `otto.new@fastmail.com` — the switch happens when you click the link there."* plus a compact 26px **Cancel** | `:139–144` |

**The old address remains the signed-in email throughout** — `:120` still shows `otto@gmail.com` in
pending mode. The switch is deferred to the confirmation click, which happens off-screen.

### 4.4 Password change — two states plus a flash

| State | Renders | Lines |
|---|---|---|
| **collapsed** | Row reads "Last changed Mar 2026"; button label is **Change** (`pwBtnLabel`, `:245`) | `:146–152` |
| **expanded** | Same row, button label flips to **Close**; panel below with Current / New / Confirm + the 12+ helper + **Update password** / **Cancel** | `:153–167` |
| **flash** | `pwFlash` appends `· updated ✓` in `var(--pos)` inline after "Last changed Mar 2026" (`:149`) | — |

`pwUpdate` (`:247`) collapses the panel, raises `pwFlash`, and schedules `this._t2` to clear it
after **2600 ms**. The "Last changed" date itself never changes. No error state, no
confirm-mismatch state, no pending state.

Both Cancel (`:164`) and the header Close (`:151`) call the same `pwToggle` (`:246`).

### 4.5 Session
Single, static state. One session shown, no list, no "sign out of all devices", no last-seen
timestamp, no device inventory.

### 4.6 Danger zone and the delete modal

**Closed** (`:178–184`) — copy: *"Deletes your binder history, saved screens, and watchlist —
immediately and permanently. Public market data is unaffected."* (`:181`), with an outlined
`var(--neg)` **Delete account…** button (`:182`).

**Open** (`:187–200`) — `role="dialog"`, `aria-label="Delete account"`. Heading "Delete this
account?" (`:190`). Body: *"Your binder history (12 transactions), 3 saved screens, and 4 watchlist
rows are deleted **immediately**. There is **no recovery**. Public market data is unaffected."*
(`:191`).

**Two sub-states, driven by the typed text** (`delOk = st.delText === 'DELETE'`, `:229`):

| Sub-state | Delete forever button | Lines |
|---|---|---|
| **Armed** (`delText === 'DELETE'`) | `var(--neg)` fill, `#FFFFFF` text, `cursor: pointer`, enabled | `:250` |
| **Disarmed** (anything else) | `var(--mutbg)` fill, `var(--mut2)` text, `cursor: not-allowed`, `disabled` | `:196`, `:249–250` |

The gate is **exact, case-sensitive, and untrimmed** — `delete`, `Delete`, and `DELETE ` all leave
it disarmed. `delOpenFn` and `delClose` both reset `delText: ''` (`:251–252`), so the box is always
empty on open and never retains a previous attempt.

### 4.7 Retention — what the copy actually says

**No retention or grace period is stated anywhere on this screen.** `grep -ci retention` = 0,
`grace` = 0. The copy asserts the opposite three times: *"immediately and permanently"* (`:181`),
*"deleted immediately"* and *"There is no recovery"* (`:191`). The design as built is **hard,
instant deletion with no undo window.**

This directly contradicts `Cardstock Legal.dc.html:57` — read directly 2026-08-10 — which states:
*"Delete your account from Profile & settings and everything above is **removed within 30 days**.
Export your binder as CSV first if you want a copy."* Two Tier-1 prototypes in conflict; see §8
row 4 and §7.

### 4.8 Theme boot
`componentDidMount` (`:207–212`) reads both keys and calls `setState` **only if** at least one is
set — so the default path does no re-render. The helmet (`:10–25`) contains **no inline pre-paint
script** (0 occurrences) and nothing sets `data-theme` or `data-cvd` on `:root`; the lone rule
`:root[data-theme="dark"] { --logoTeal: #3FBFAD; }` (`:23`) is therefore dead on this page.
Consequence: a dark-mode user gets a **light first paint**, then a flip. Same defect as Account.

---

## 5. Interactions

16 `title` tooltips, one per interactive control, all describing consequence
(`DESIGN_NOTES.md:150`, `:153`).

| # | Control | Line | Handler | Tooltip | Consequence |
|---|---|---|---|---|---|
| 1 | Logo lockup | `:30` | `<a href>` | — | → `Cardstock Home.dc.html` |
| 2–6 | Home · Screener · Charts · Binder · Browse | `:32–36` | `<a href>` | — | → that page |
| 7 | `<cardstock-search>` | `:39` | external component | — | shared nav search |
| 8 | Account circle | `:40` | **none — `<span>`** | "You are here" | inert |
| 9 | Display name | `:56` | none | — | uncontrolled; value never read |
| 10 | Avatar slot | `:52` | `image-slot.js` | — | component supplies its own affordance |
| 11 | Timezone select | `:60` | `setTz` (`:238`) | — | sets `state.tz`; **no save, no persistence** |
| 12 | **Save changes** | `:70` | `doSave` (`:240`) | "Save your display name, avatar, and timezone" | "Saved ✓" for 2200 ms; **writes nothing** |
| 13 | **Light** | `:87` | `setLight` (`:234`) | "Light theme — applies immediately and is remembered on this device" | `localStorage['cardstock-theme']='light'` + repaint |
| 14 | **Dark** | `:88` | `setDark` (`:235`) | "Dark theme — applies immediately and is remembered on this device" | `localStorage['cardstock-theme']='dark'` + repaint |
| 15 | **CVD switch** | `:96` | `toggleCvd` (`:237`) | "Colorblind-safe palette — swaps state hues app-wide; glyphs and wording are unchanged" | writes `'1'`/`'0'` to `cardstock-cvd` + repaint |
| 16 | **Change** (email) | `:123` | `emailOpen` | "Change the address you sign in with — the switch happens only after you confirm from the new address" | → mode `open`; button itself disappears |
| 17 | **Send confirmation** | `:134` | `emailSend` | "Send a confirmation link to the new address — your sign-in email switches only after you click it" | → mode `pending` (unconditional; no validation) |
| 18 | **Cancel** (email form) | `:135` | `emailCancel` | "Leave your sign-in email unchanged" | → mode `idle` |
| 19 | **Cancel** (pending banner) | `:142` | `emailCancel` | "Leave your sign-in email unchanged" | → mode `idle`; **abandons the pending change** |
| 20 | **Change / Close** (password) | `:151` | `pwToggle` (`:246`) | "Set a new password — you stay signed in on this device" | toggles the panel |
| 21 | **Update password** | `:163` | `pwUpdate` (`:247`) | "Save the new password — you stay signed in on this device" | collapse + "updated ✓" for 2600 ms |
| 22 | **Cancel** (password) | `:164` | `pwToggle` | "Leave your password unchanged" | collapses |
| 23 | **Sign out** | `:174` | `signOut` (`:248`) | "Sign out of this device only — other sessions stay active" | `location.href = 'Cardstock Account.dc.html'` |
| 24 | **Delete account…** | `:182` | `delOpenFn` (`:251`) | "Permanently deletes your binder, saved screens, and watchlist. Market data is unaffected. Requires typing DELETE to confirm." | opens modal, clears `delText` |
| 25 | DELETE confirm input | `:193` | `setDelText` (`:253`) | — | arms/disarms the confirm button |
| 26 | **Cancel** (modal) | `:195` | `delClose` (`:252`) | "Keep your account and everything in it" | closes, clears `delText` |
| 27 | **Delete forever** | `:196` | `delConfirm` (`:254`) | "Permanently delete your binder, saved screens, and watchlist. This cannot be undone." | `location.href = 'Cardstock Account.dc.html'` |

### 5.1 Not present
No modal dismissal by scrim click, Escape key, or close ✕ (`:188` has no handler). No focus trap.
No keyboard shortcuts. No notification/alert-email preferences. No data export. No two-factor auth.
No connected-accounts or API-token section. No session list. No "delete my data but keep the account".

---

## 6. Rules and invariants

1. **Settings are capabilities, not screens.** Email change, password change, sign out, and
   deletion all happen inline on this one page (`DESIGN_NOTES.md:99`). The only settings screen is
   `/settings`.
2. **Theme and colorblind mode are the only per-device preferences** — `cardstock-theme` and
   `cardstock-cvd` (`:234`, `:235`, `:237`). Written on click, no Save button, no server round trip.
3. **Everything else in Profile is per-account** and requires an explicit Save (`:70`) or its own
   confirmation flow.
4. **Theme is a binary choice.** Light or Dark, always exactly one (`:232–233`). No system/auto.
5. **Colorblind mode swaps hue only.** *"Glyphs ▲ ▼ – ◌ never change."* (`:94`). Amber
   (`--warn`) is not swapped at all (`:221`).
6. **Appearance changes apply app-wide and immediately** — *"Applies across every Cardstock page."*
   (`:84`); no confirm, no undo, no preview-then-apply.
7. **Email changes are confirmed from the NEW address, not the old one** (`:123`, `:132`, `:134`),
   and the sign-in address does not change until that link is clicked (`:141`).
8. **Email change requires the current password** (`:131`) — reauthentication before a
   security-sensitive change. Password change requires it too (`:156`).
9. **Changing your password does not sign you out** — *"you stay signed in on this device"*
   (`:151`, `:163`). Nothing states what happens to other sessions.
10. **Sign out is device-scoped** — *"other sessions stay active"* (`:174`).
11. **Password policy is length-only: 12+ characters** (`:159`), identical wording to
    `Cardstock Account.dc.html:67`/`:95`.
12. **Destructive actions require typed confirmation**, gated on an exact case-sensitive
    `'DELETE'` (`:229`); the button is genuinely `disabled` until then (`:196`).
13. **Deletion is immediate, permanent, and unrecoverable** as this screen describes it (`:181`,
    `:191`) — no retention window, no grace period, no undo (but see §8 row 4).
14. **Deletion destroys user data only; public market data is unaffected** — stated twice
    (`:181`, `:191`) and again in the tooltip (`:182`). The scraper's eight tables are untouched.
15. **The deletion scope is exactly three things**: binder history, saved screens, watchlist
    (`:181`, `:191`, `:182` — consistent in all three places).
16. **Both terminal actions land on the auth shell** — sign out (`:248`) and delete (`:254`) both
    navigate to `Cardstock Account.dc.html`.
17. **The account circle is inert on its own page** (`:40`) — a `<span>` with "You are here".
18. **Every inline style reads `var(--x, <light literal>)`** so a streaming first paint is light and
    correct (`DESIGN_NOTES.md:102`) — verified throughout `:27–200`.
19. **Timers are cleaned up** — `componentWillUnmount` clears `_t1` and `_t2` (`:213`).

---

## 7. Open questions

1. **Deletion retention is contradicted between two Tier-1 prototypes.** This screen says
   "immediately and permanently… no recovery" (`:181`, `:191`); `Cardstock Legal.dc.html:57` says
   "removed within 30 days". Tier rules cannot settle a Tier-1 vs Tier-1 conflict — **the owner must
   choose**, and one prototype's copy then has to change. This is the highest-priority item on the page.
2. **What actually gets deleted?** The enumeration is binder history, saved screens, watchlist —
   but the heading says "Delete this **account**" (`:190`). Does the user row, email address, and
   credential go too? Can the address immediately re-register (§1.2 of `account.md`: signup is
   open)? Undefined.
3. **Legal points to an export that Profile does not have.** `Legal.dc.html:57` says "Export your
   binder as CSV first" — `grep -ci export` on this file = **0**. CSV export lives on Binder
   (`DESIGN_NOTES.md:153`). Either the delete modal should link to it, or Legal should say where it is.
4. **Does a density preference exist at all?** No control here, and Set's density is not persisted
   (`docs/screens/set.md:173`). Are the per-surface `terminal|binder` and `table|gallery` modes
   meant to persist per device, and if so should they be centralized here or stay local?
5. **No error states anywhere.** Wrong current password, email already in use, malformed email,
   password too short, confirm mismatch, network failure — none has a designed treatment on this
   page, and unlike Account there is not even a banner slot to put one in.
6. **No pending/submitting state** on Save changes, Send confirmation, Update password, or Delete
   forever. All four are network calls.
7. **"Last changed Mar 2026" never updates** (`:149`) even after a successful change — should it,
   and does the server track it?
8. **The pending banner hardcodes `otto.new@fastmail.com`** (`:141`) rather than echoing the typed
   address. Production must interpolate; also unspecified is whether the confirmation link expires
   (Account uses 30 minutes — `Cardstock Account.dc.html:85`) and whether it can be resent.
9. **Cancelling a pending email change** (`:142`) — does it invalidate the already-sent
   confirmation token server-side, or merely hide the banner?
10. **Only five timezones** (`:61–65`), all US plus UTC. Placeholder, or a real v1 scoping decision?
    It silently excludes every non-US user of a publicly-signup-able app (D-011).
11. **Timezone semantics are unstated beyond display.** "Refresh stamps and daily windows"
    (`:67`) — does it affect the daily-window boundaries used in computation, or only formatting?
12. **The modal has no dismissal affordances** — no Escape, no scrim click, no ✕, no focus trap
    (`:187–200`). Accessibility gap on the most destructive control in the app.
13. **Session shows one device with no list.** Is multi-session management in scope? Rules 9 and 10
    both explicitly scope to "this device" while saying nothing about the others.
14. **Avatar behaviour is entirely delegated** to `image-slot.js` (`:52`) — upload, crop, size
    limit, remove, and default-when-empty are unspecified here.
15. **Display name has no constraints** (`:56`) — no length limit, no uniqueness, and no stated
    purpose, since `DECISIONS.md:203` records the binder is strictly private with no public profiles.
    What is the display name *for*?
16. **Dark-mode users get a light first paint** (§4.8) — add a pre-paint script or accept the flash?
17. **No notification or email preferences.** `HANDOFF.md`/`DESIGN_NOTES.md:88` scope transactional
    email to verify/reset/email-change and defer alert email to v2 — so there is nothing to
    configure yet, but the section will be needed when alerts land.

---

## 8. Contradictions found

| Claim | Source doc:line | What the HTML actually does |
|---|---|---|
| "**Theme, colorblind mode, and density** persist per device, not per account." | `CardStock Mockup/HANDOFF.md:156` | **Two of three.** Only `cardstock-theme` (`:234–235`) and `cardstock-cvd` (`:237`) are written to `localStorage`. **No density control exists on this screen** — `grep -ci density` over the file = **0**, and the Appearance section holds exactly two controls plus a preview strip (`:79–112`). There is no density preference to persist. |
| "**Density** and theme choices persist per device (localStorage), not per account." | `CardStock Mockup/DISPLAY_VOCABULARY.md:203` (repeated at `docs/screens/brand-system.md:382`) | Same defect, and it omits CVD while asserting density. The per-surface modes it describes (`terminal\|binder`, `table\|gallery`, `:198–201`) are **component state**, not stored preferences — `docs/screens/set.md:173` independently records Set's density as not persisted. No settings screen governs them. |
| "Every state pairs a hue with a glyph (**▲ ▼ – ● ◌ ◆**)" — 6 glyphs | `CardStock Mockup/HANDOFF.md:150` | Profile's own CVD description names **4**: *"Glyphs ▲ ▼ – ◌ never change."* (`:94`), and the preview strip (`:103–107`) uses exactly those four. `●` and `◆` appear on neither. The user-facing promise is narrower than the doc's. |
| "Delete your account from Profile & settings and everything above is **removed within 30 days**." | `CardStock Mockup/Cardstock Legal.dc.html:57` (read directly 2026-08-10) | Profile states the opposite three times: "**immediately and permanently**" (`:181`), "deleted **immediately**" and "There is **no recovery**" (`:191`). **Tier-1 vs Tier-1** — document authority cannot resolve it; needs an owner ruling. |
| "**Export your binder as CSV first** if you want a copy" (in the deletion paragraph) | `Cardstock Legal.dc.html:57` | `grep -ci export` over Profile = **0**. Neither the danger-zone row (`:180–183`) nor the modal (`:187–200`) offers or links to an export. The user is told to do something the screen gives them no way to do. |
| New tables `users (…, **theme_pref**)` — theme stored per account | `uploads/CARDSTOCK_UI_SPEC_v1.md:149` | Theme is **device-scoped in `localStorage`**, never sent to a server (`:234–235`), and the UI copy commits to that: "remembered on **this device**" (`:87–88`). A `theme_pref` column would contradict the built behaviour. |
| "no verification emails; minimal password reset" (the invite-only reversal) | `uploads/PROJECT_LOG.md:254`, quoted at `DECISIONS.md:287` | Profile implements a **third** transactional email — the email-change confirmation (`:132`, `:134`, `:141`) — on top of Account's verification and reset. Three transactional emails exist in the prototypes, matching `HANDOFF.md` §4 ("verify / reset / email-change only"), not the reversal. `grep -ci invite` over this file = **0**: no invite management, no "invite a friend", no remaining-invite quota. |
| "Chrome shared by **every** app page: … **pre-paint script reading `localStorage`**" | `CardStock Mockup/HANDOFF.md:88` | The helmet (`:10–25`) has **zero** inline scripts — only `image-slot.js` and `cardstock-search.js`. Nothing sets `data-theme`/`data-cvd`, so the `:root[data-theme="dark"]` rule at `:23` never fires and dark-mode users see a light flash. `DESIGN_NOTES.md:104` describes that pre-paint architecture as rolled out app-wide on 2026-08-09; Profile does not have it. |
| Danger-zone deletion covers "binder/screens/watchlist" | `CardStock Mockup/DESIGN_NOTES.md:101` | **Confirmed, not contradicted** — `:181`, `:191`, `:182` all agree on exactly those three. Recorded here because it is the one deletion-scope claim that survives checking; the retention period (row 4) does not. |
