# Profile & settings

**Source of truth:** `CardStock Mockup/Cardstock Profile.dc.html` (260 lines), read directly 2026-08-10.
All `:NN` citations refer to that file unless another file is named.
Per `CLAUDE.md` §"Document authority", this prototype is Tier 1. Where a markdown doc disagrees with it,
the HTML wins and the disagreement is recorded in §8.

---

## 1. Identity

| | |
|---|---|
| Screen label | `Profile` (`data-screen-label="Profile"`, :27) |
| Page title | "Profile & settings" (`h1`, :45) |
| Prototype | `CardStock Mockup/Cardstock Profile.dc.html` |
| Derived route (Tier 2) | `/settings` — `CardStock Mockup/HANDOFF.md:79`. Not verifiable from the HTML: the file has no router. |
| Entry point | The account circle in the nav of every other app page (`DESIGN_NOTES.md:107`). On this page the circle is a non-interactive `<span>` with `title="You are here"` (`:40`). |
| Purpose | The whole signed-in settings surface in one page: identity, device appearance preferences, account credentials and session, and account deletion. |
| Design ruling behind it | "only logged-out flows get screens; change email/password, delete account, sign out are **INLINE** on Profile" (`DESIGN_NOTES.md:99`). There is no `/settings/email`, `/settings/password`, or delete route. |

Four sections plus one modal:

| Section | `aria-label` | Lines |
|---|---|---|
| Profile | `Profile` | :49–77 |
| Appearance | `Appearance` | :79–112 |
| Account | `Account` | :114–176 |
| Danger zone | `Danger zone` | :178–184 |
| Delete-account modal | `role="dialog" aria-label="Delete account"` | :187–200 |

---

## 2. Layout

**Nav** (`:29–41`) — the shared 48px app chrome: `position:sticky; top:0; z-index:20`, `--card` background,
1px bottom rule, `padding:0 20px`, `gap:24px`.
Contents in order: lockup `<a>` → `Cardstock Home.dc.html` (24px mark + wordmark 18px/700, `:30`); five
section links — Home, Screener, Charts, Binder, Browse (`:32–36`), 15px/500 `--mut`, transparent 2px bottom
border reserved for the active state (**no link is marked active on this page**); flex spacer (`:38`);
`<cardstock-search>` web component (`:39`); account circle (`:40`) — 28px, `border-radius:50%`, 1px `--acc`
border, `--mutbg` fill, `--acc` initial "O", 14px/600.

**Content column** (`:43`) — `max-width:760px`, centred, `padding:28px 24px 72px`, `display:flex; flex-direction:column; gap:16px`.

**Header block** (`:44–47`) — `h1` Inter Tight 700 27px; sub-line 13.5px `--mut2`: "Signed in as
`otto@gmail.com` · member since Mar 2026" (address in JetBrains Mono 13px).

**Section cards** — every section shares: `--card` background, `1px solid var(--line)`, `border-radius:8px`,
`padding:18px 20px`; `h2` Inter Tight 700 17.5px. Danger zone's `h2` is `--neg` (`:179`).

- **Profile** (`:51`): flex row, `gap:20px`. Left: 72px circular avatar slot. Right: 2-column CSS grid
  (`1fr 1fr`, `gap:0 16px`) holding Display name and Timezone, with the save row spanning both columns
  (`grid-column:1/-1`, `:69`).
- **Appearance** (`:81–111`): two space-between rows separated by a top rule (`:91`), then the preview panel
  (`:100`) — `--bg` fill, 1px rule, 6px radius, `padding:12px 14px`, uppercase 12.5px "PREVIEW" label and a
  wrapping chip strip.
- **Account** (`:117–175`): three space-between rows (Email, Password, Session), each `padding:10px 0`, rules
  between (`:146`, `:169`). Inline forms and the pending banner render *between* rows as inset panels
  (`--bg` fill, 1px rule, 6px radius, `padding:12px 14px`, `:127`, `:154`).
- **Danger zone** (`:180`): one space-between row — explanatory paragraph left, outlined destructive button
  right (`flex-shrink:0`).
- **Modal** (`:188`): `position:fixed; inset:0; z-index:100`, scrim `rgba(15,15,12,0.45)`, centred, dialog
  `width:420px; max-width:100%`, `--card` fill, 10px radius, `padding:20px`; actions right-aligned, `gap:8px`.

**Theme:** a wrapper div applies `{{ themeVars }}` inline (`:26`, computed `:214–223`); every style reads
`var(--x, <light literal>)` so the page paints light first. Token map at `:216–221`.

---

## 3. Data contract

### 3.1 Inbound props and device storage

| Name | Source | Type | Default | Notes |
|---|---|---|---|---|
| `emailPending` | prop (`:204`) | bool | `false` | Seeds `emailMode` to `'pending'` (`:227`). |
| `openDeleteModal` | prop (`:204`) | bool | `false` | Seeds the modal open (`:228`). |
| `localStorage['cardstock-theme']` | device (`:209`) | `'light' \| 'dark'` | unset ⇒ light | Read on mount, written by the segmented control (`:234–235`). |
| `localStorage['cardstock-cvd']` | device (`:210`) | `'1' \| '0'` | unset ⇒ off | Read on mount, written by the toggle (`:237`). |

### 3.2 Displayed account facts (read-only)

| Field | Seeded value | Line | Notes |
|---|---|---|---|
| Sign-in email | `otto@gmail.com` | :46, :120 | Shown **unmasked** when signed in (contrast `Cardstock Account.dc.html:85`). Mono 13px. |
| Member since | `Mar 2026` | :46 | Month + year granularity. |
| Password last changed | `Mar 2026` | :149 | Month + year granularity. |
| Session descriptor | `This device · Chicago, IL` | :172 | One session only; no list, no last-seen, no device/browser detail. |
| Pending new email | `otto.new@fastmail.com` | :141 | Only rendered in the pending state. |
| Deletion counts | 12 transactions · 3 saved screens · 4 watchlist rows | :191 | Must be live counts in production. |
| Avatar initial | `O` (nav circle) | :40 | Derived from the display name/email in production. |

### 3.3 Editable fields

| Section | Field | Control | Line | Bound? | Validation in HTML |
|---|---|---|---|---|---|
| Profile | Avatar | `<image-slot id="profile-avatar" shape="circle" placeholder="Avatar">`, 72×72 | :52 | n/a | Prototype tool (`image-slot.js`), not product UI. No image is assigned in `.image-slots.state.json`, so it renders as an empty "Avatar" placeholder. Upload UX is undesigned. |
| Profile | Display name | `<input type="text" defaultValue="Otto">` | :56 | **No** — uncontrolled | None. No `maxlength`, no charset rule, no uniqueness. |
| Profile | Timezone | `<select value={{tz}} onChange={{setTz}}>` | :60 | **Yes** (`:238`) | Closed list of 5 (below). |
| Account | New email | `<input type="email">` | :129 | No | Native `type=email` only |
| Account | Current password (email change) | `<input type="password">` | :131 | No | None |
| Account | Current password (pw change) | `<input type="password" autocomplete="current-password">` | :156 | No | None |
| Account | New password | `<input type="password" autocomplete="new-password">` | :158 | No | None. Helper `:159`: "12+ characters — length beats complexity." |
| Account | Confirm new password | `<input type="password" autocomplete="new-password">` | :161 | No | None — **match is not checked** |
| Modal | Type-DELETE confirm | `<input type="text" value={{delText}} onChange={{setDelText}}>` | :193 | **Yes** (`:253`) | `delText === 'DELETE'` — exact, **case-sensitive** (`:229`). Mono 14px. |

**Timezone options** (`:61–65`), exactly five: `America/New_York (ET)`, `America/Chicago (CT)`,
`America/Denver (MT)`, `America/Los_Angeles (PT)`, `UTC`. Initial state `America/Chicago` (`:206`),
consistent with the seeded session location (`:172`). Helper (`:67`): "Refresh stamps and daily windows
display in this timezone."

**Note the two identity models.** Display name / avatar / timezone are behind a **Save changes** button
(`:70`), so they are account-scoped, deliberate edits. Theme and colorblind mode have **no save button** —
they apply on click and write to `localStorage`. See §6.

### 3.4 Appearance controls — persistence scope

| Preference | Control | Storage | Scope | Applies |
|---|---|---|---|---|
| Theme (Light / Dark) | Segmented pair, `:87–88` | `localStorage['cardstock-theme'] = 'light' \| 'dark'` (`:234–235`) | **Per device** | Immediately, and "Applies across every Cardstock page." (`:84`) |
| Colorblind-safe palette | `role="switch"` toggle, `:96–98` | `localStorage['cardstock-cvd'] = '1' \| '0'` (`:237`) | **Per device** | Immediately, app-wide |
| Density | **Does not exist on this screen** | — | — | — |

Verified: `grep -ic densit` over this file returns **0**. There is no density control, no density copy, and no
third localStorage key. The claim that density persists per device (`HANDOFF.md:156`,
`DISPLAY_VOCABULARY.md:203`) is not settleable from this file — density is a per-surface control on
Screener / Set / Character / Binder (`DISPLAY_VOCABULARY.md:199–201`). See §8.

**Per-device is verified for theme and colorblind mode**: both are written only to `localStorage`
(`:234–237`), both are re-read only from `localStorage` (`:209–210`), neither is included in the
Save-changes payload, and nothing in the file associates either with the account.

There is **no per-account preference storage anywhere in this file** — no theme preference field, no
"sync across devices" affordance.

**Colorblind semantics** (`:94`): "Swaps green→blue and red→orange everywhere state color appears.
Glyphs ▲ ▼ – ◌ never change." Hue-only substitution; `--warn` amber and greys are identical in both palettes
(`:221`).

**Preview strip** (`:102–109`), 7 live samples that re-tint as the toggles change: `▲ RS 94th` (pos),
`▼ EMA 3/9` (neg), `– RSI 71` (warn), `– MACD –` (muted), `◌ Churn — 12d` (muted), `+4.2%` (pos, 14px),
`−1.8%` (neg, 14px). Chips are JetBrains Mono 11px/600, `padding:1px 6px`, 4px radius.

**Token values** (`:216–221`) — light: `--bg #FAFAF7`, `--card #FFFFFF`, `--line #E4E4E0`, `--ink #1C1C1E`,
`--mut #5B5B57`, `--mut2 #6B6B66`, `--hov #F6F6F2`, `--mutbg #F3F3EE`, `--logoTeal #0E8A7B`, `--acc/--btn
#4A63D0`, `--accH/--btnH #3A4FB8`, `--inbg #FAFAF7`. Dark: `#161614 / #1E1E1C / #33332F / #E9E9E5 / #B4B4AE /
#A8A8A2 / #282825 / #2A2A27 / #3FBFAD`, `--acc #8C9BF2`, `--accH #8CA4F0`, `--btn #4A63D0`, `--btnH #AAB6F6`,
`--inbg #262624`. State colours (`:219–220`) match `DISPLAY_VOCABULARY.md:77–84` exactly; the chrome accents
do not match `DISPLAY_VOCABULARY.md:85` — see §8.

---

## 4. States

### 4.1 Complete state map

| State var | Values | Initial | Line |
|---|---|---|---|
| `theme` | `null → 'light' \| 'dark'` | `null`, treated as light (`:226`) | :206, :209, :234–235 |
| `cvd` | `null → true \| false` | `null`, treated as false (`:226`) | :206, :210, :237 |
| `tz` | one of 5 zone ids | `'America/Chicago'` | :206, :238 |
| `savedFlash` | bool | `false` | :206, :239–240 |
| `emailMode` | `'idle' \| 'open' \| 'pending'` | `props.emailPending ? 'pending' : 'idle'` (`:227`) | :241–244 |
| `pwOpen` | bool | `false` | :245–246 |
| `pwFlash` | bool | `false` | :245, :247 |
| `delOpen` | bool | `props.openDeleteModal` (`:228`) | :251–252 |
| `delText` | string | `''` | :206, :253 |

### 4.2 Email-change state machine (`:241–244`)

| State | Renders | Trigger in | Trigger out |
|---|---|---|---|
| `idle` | Email row + **Change** button (`:122–124`) | `emailCancel` from either other state (`:244`) | `emailOpen` → `open` (`:242`) |
| `open` | Inset form: New email, Current password, explainer, Send confirmation / Cancel (`:126–138`). The **Change** button is hidden (`:122`). | `emailOpen` | `emailSend` → `pending` (`:243`); `emailCancel` → `idle` |
| `pending` | Warn-tinted banner (`--warnBg`, `--warn` text) naming the target address + inline **Cancel** (`:139–144`). Change button still hidden. | `emailSend`, or `props.emailPending` | `emailCancel` → `idle` (`:244`) |

Pending copy (`:141`): "Confirmation sent to `otto.new@fastmail.com` — the switch happens when you click the
link there."

### 4.3 Password-change states

`pwOpen` toggles the inset form (`:153–167`) and flips the row button label between **Change** and **Close**
(`:245`, `:151`). `pwUpdate` (`:247`) closes the form and sets `pwFlash`, appending "· updated ✓" in `--pos`
to the "Last changed Mar 2026" line (`:149`) for **2600 ms**.

### 4.4 Profile-save state

`doSave` (`:240`) sets `savedFlash` immediately and clears it after **2200 ms**, rendering "Saved ✓" in
`--pos` beside the button (`:71–73`). It is unconditional and optimistic — no request, no validation, no
error branch, and **nothing is persisted** (reloading resets `tz` to `America/Chicago`).

### 4.5 Delete-account states

| State | Condition | Rendering |
|---|---|---|
| Closed | `delOpen === false` | Danger-zone row only |
| Open, unconfirmed | `delOpen && delText !== 'DELETE'` | Modal; **Delete forever** has `disabled` (`:196`), `--mutbg` fill, `--mut2` text, `cursor:not-allowed` (`:250`) |
| Open, confirmed | `delOpen && delText === 'DELETE'` | Button enabled, `--neg` fill, white text, `cursor:pointer` (`:250`) |

Opening resets `delText` to `''` (`:251`); cancelling does the same (`:252`).

### 4.6 States that do NOT exist in the HTML

- No error state anywhere: wrong current password, email already in use, invalid email, password mismatch,
  password too short, save failure, delete failure. Not one is rendered.
- No pending/spinner/disabled state on any submit except the delete confirm.
- No expired or resend affordance for the email-change confirmation link.
- No "email verified / unverified" badge on the Email row.
- No toast layer; every acknowledgement is inline and self-clearing.
- No session list, no "sign out everywhere", no revoke.
- No 2FA, no connected accounts, no data export, no notification settings (alerts are v2 —
  `DESIGN_NOTES.md:120`).
- No unsaved-changes guard on the Profile form.
- The modal has **no Escape handler and no scrim-click handler** — Cancel (`:195`) is the only way out.

---

## 5. Interactions

| # | Control | Line | Handler | Consequence |
|---|---|---|---|---|
| 1 | Nav lockup | :30 | `<a href>` | → `Cardstock Home.dc.html` |
| 2 | Nav section links ×5 | :32–36 | `<a href>` | → Home / Screener / Charts / Binder / Browse prototypes |
| 3 | Nav search | :39 | `<cardstock-search>` | Shared component; `/` focuses, `Esc` clears (`DISPLAY_VOCABULARY.md:194`) |
| 4 | Account circle | :40 | — | Inert `<span>`, `title="You are here"` |
| 5 | Avatar slot | :52 | `image-slot.js` | Prototype-only drag/drop placeholder |
| 6 | Display name input | :56 | — | Uncontrolled; value only reaches the app via #8 |
| 7 | Timezone select | :60 | `setTz` (`:238`) | Updates state immediately; still requires #8 to "save". Tooltip-free; helper at `:67` |
| 8 | **Save changes** | :70 | `doSave` (`:240`) | Shows "Saved ✓" for 2200 ms. Tooltip: "Save your display name, avatar, and timezone" — that is the exact save payload |
| 9 | **Light** segment | :87 | `setLight` (`:234`) | Writes `cardstock-theme='light'`, repaints instantly. Tooltip: "Light theme — applies immediately and is remembered on this device" |
| 10 | **Dark** segment | :88 | `setDark` (`:235`) | Writes `cardstock-theme='dark'`, repaints instantly. Same "remembered on this device" wording |
| 11 | Colorblind switch | :96 | `toggleCvd` (`:237`) | Writes `cardstock-cvd='1'\|'0'`, repaints state colours app-wide; knob translates 16px (`:236`). `aria-checked` tracks the value |
| 12 | Email **Change** | :123 | `emailOpen` (`:242`) | → `open`. Tooltip: "Change the address you sign in with — the switch happens only after you confirm from the new address" |
| 13 | **Send confirmation** | :134 | `emailSend` (`:243`) | → `pending`. Sends a confirmation link to the **new** address |
| 14 | Email **Cancel** (form) | :135 | `emailCancel` | → `idle`, sign-in email unchanged |
| 15 | Pending **Cancel** | :142 | `emailCancel` | → `idle`; abandons the pending change |
| 16 | Password **Change / Close** | :151 | `pwToggle` (`:246`) | Toggles the inset form. Tooltip: "Set a new password — you stay signed in on this device" |
| 17 | **Update password** | :163 | `pwUpdate` (`:247`) | Closes form, flashes "· updated ✓" for 2600 ms. No session invalidation |
| 18 | Password **Cancel** | :164 | `pwToggle` | Closes form unchanged |
| 19 | **Sign out** | :174 | `signOut` (`:248`) | → `Cardstock Account.dc.html` (sign-in view). Tooltip: "Sign out of this device only — other sessions stay active" |
| 20 | **Delete account…** | :182 | `delOpenFn` (`:251`) | Opens modal, clears the confirm field. Tooltip restates the scope and the typed-DELETE requirement |
| 21 | Confirm input | :193 | `setDelText` (`:253`) | Enables #23 only on exact `DELETE` |
| 22 | Modal **Cancel** | :195 | `delClose` (`:252`) | Closes, clears field. Tooltip: "Keep your account and everything in it" |
| 23 | **Delete forever** | :196 | `delConfirm` (`:254`) | → `Cardstock Account.dc.html`. Disabled until #21 matches |

16 controls carry a `title` tooltip in this file.

---

## 6. Rules and invariants

1. **Two persistence models, deliberately different.**
   - *Device, immediate, no save:* theme and colorblind mode — `localStorage` only (`:234–237`), applied on
     click, "remembered on this device" is the literal tooltip wording (`:87–88`).
   - *Account, deferred, explicit save:* display name, avatar, timezone — one Save button (`:70`) whose
     tooltip enumerates exactly those three.
   Do not merge them; the appearance controls must never gain a Save button and the profile fields must never
   auto-apply.
2. **Appearance changes are global**, not page-local: "Applies across every Cardstock page." (`:84`), and the
   colorblind swap reaches "everywhere state color appears" (`:94`).
3. **Colorblind mode changes hue only.** Glyphs ▲ ▼ – ◌, labels, wording, and amber are invariant
   (`:94`, `:221`).
4. **Email change is two-phase and confirmed from the destination address.** The sign-in email switches only
   when the link sent to the *new* address is clicked (`:123`, `:132`, `:141`). The change requires the
   current password (`:131`). It is cancellable from both `open` and `pending` (`:135`, `:142`).
5. **Changing the password does not end the session** — "you stay signed in on this device" (`:151`, `:163`).
   No other sessions are mentioned.
6. **Sign out is this-device-only**; other sessions survive (`:174`).
7. **Deletion requires a typed literal `DELETE`, case-sensitive** (`:229`), and the destructive button is
   disabled, muted, and `not-allowed` until then (`:196`, `:250`).
8. **Deletion scope is user data only.** "Deletes your binder history, saved screens, and watchlist —
   immediately and permanently. Public market data is unaffected." (`:181`); the modal repeats it with counts
   and adds "There is no recovery." (`:191`).
9. **Both exits land on the Account screen** (`:248`, `:254`) with no acknowledgement — `Cardstock
   Account.dc.html` has no "signed out" or "account deleted" state.
10. **Timezone drives time display, not storage** — "Refresh stamps and daily windows display in this
    timezone." (`:67`).
11. **The signed-in surface shows the full email** (`:46`, `:120`), unlike the logged-out screens which mask
    it.
12. **The account circle is inert on its own page** (`:40`) — the nav is otherwise identical to every other
    app page.
13. **Theme is applied after mount** (`:207–212`); there is no pre-paint script in the helmet (`:11–24`), so
    a dark-preference device paints light for one frame. Every inline style therefore carries a light
    literal fallback.

---

## 7. Open questions

1. **Deletion retention is contradicted between two Tier-1 prototypes.** This screen promises
   *immediate, unrecoverable* deletion (`:181`, `:191`); `Cardstock Legal.dc.html:57` promises removal
   *"within 30 days"*. Tier rules cannot settle a Tier-1 vs Tier-1 conflict — the owner must choose, and one
   of the two files must change. (`DECISIONS.md:475` flagged the 30-day promise as unverified; it is now
   verified at `Legal.dc.html:57`.)
2. **What exactly is deleted?** The copy lists binder history, saved screens, watchlist. It never says
   whether the user row, email address, and credentials go too — which is precisely what a retention policy
   turns on.
3. **Where does density persist?** No control here (0 matches). If `HANDOFF.md:156` /
   `DISPLAY_VOCABULARY.md:203` are right that it is device-scoped, the key name and the surfaces that own it
   need recording; if Profile should host a global density control, it is missing.
4. **Do any preferences belong to the account?** Tier 3 `CARDSTOCK_UI_SPEC_v1.md:149` puts `theme_pref` on
   the `users` table. The HTML stores nothing per account. Decide before the schema is written.
5. **Is the avatar in scope at all?** `<image-slot>` is a mockup tool, not a component. Upload, crop, size
   limits, storage, and the fallback initial ("O", `:40`) are all undesigned.
6. **Every validation error is undesigned.** Needed at minimum: wrong current password (email + password
   flows), email already registered, malformed email, password < 12 chars, confirm mismatch, save failure.
   The design has no slot for a field error anywhere on this page.
7. **Email-change link TTL, resend, and expiry state.** The pending banner has no timer and no resend; the
   Account screen's 30-minute figure (`Cardstock Account.dc.html:85`) covers verification and reset only.
8. **Multiple sessions.** The Sign out tooltip says "other sessions stay active" (`:174`) but there is no way
   to see or revoke them. Is a session list in scope, or is that copy aspirational?
9. **Is `Saved ✓` allowed to be optimistic?** `doSave` never fails (`:240`).
10. **Should the modal close on Escape / scrim click?** Currently neither (`:187–200`). Focus trap and initial
    focus are also unspecified.
11. **Nav active state.** All five links render with a transparent bottom border and no active treatment
    (`:32–36`) — Profile is not one of the five sections, so no rule is stated for "settings is open".
12. **Is display name used anywhere?** Nothing else in these two prototypes renders it; the nav shows a
    single initial.

---

## 8. Contradictions found

| Claim | Source doc:line | What the HTML actually does |
|---|---|---|
| "**Theme, colorblind mode, and density persist per device**, not per account." | `CardStock Mockup/HANDOFF.md:156` (and `DISPLAY_VOCABULARY.md:203`, "Density and theme choices persist per device (localStorage), not per account") | **Half verified, half unsupported.** Theme and colorblind mode: confirmed device-scoped — written only to `localStorage['cardstock-theme']` / `['cardstock-cvd']` (`:234–237`), read only from there (`:209–210`), excluded from the Save payload (`:70` tooltip names display name, avatar, timezone only). **Density: no density control exists on this screen** (`grep -ic densit` = 0) and there is no third storage key, so Profile cannot be cited as evidence for the density half of the claim; density lives on Screener/Set/Character/Binder per `DISPLAY_VOCABULARY.md:199–201`. |
| "Delete your account from Profile & settings and everything above is **removed within 30 days**." | `CardStock Mockup/Cardstock Legal.dc.html:57` | Profile states the opposite twice: "**immediately and permanently**" (`:181`) and "deleted **immediately**. There is **no recovery**." (`:191`). Two Tier-1 prototypes in direct conflict on the retention period — unresolvable by document tier, needs an owner ruling. |
| `users` table carries `theme_pref` (a per-account theme preference) | `uploads/CARDSTOCK_UI_SPEC_v1.md:149` | Theme is stored per device in `localStorage` only (`:234–235`). No account-level preference is read, written, or displayed anywhere in the file. |
| "`/settings` Account settings (**theme, email, password**)" | `uploads/CARDSTOCK_UI_SPEC_v1.md:122` | The page also owns display name, avatar, timezone, the colorblind toggle, the session/sign-out row, and the delete-account danger zone (`:49–200`). |
| Nav carries a "bell (alerts)" and an "**account menu** (theme toggle, settings, About our data, sign out)" | `uploads/CARDSTOCK_UI_SPEC_v1.md:127` | Nav is lockup + 5 links + search + a **non-interactive** account circle (`:29–40`). No bell (alerts deferred to v2, `DESIGN_NOTES.md:120`), no dropdown, no in-nav theme toggle, no About-the-data link. |
| "Chrome shared by every app page: 48px nav (…, **account circle → Profile**), …, **pre-paint script reading `localStorage`**." | `CardStock Mockup/HANDOFF.md:88` | (a) On Profile the circle is a `<span title="You are here">`, not a link (`:40`). (b) There is **no pre-paint script** in this file's helmet (`:11–24`); `localStorage` is read in `componentDidMount` (`:207–212`), so a dark-preference device paints light for one frame. `DESIGN_NOTES.md:102` describes this page's architecture correctly; `HANDOFF.md:88` and `DESIGN_NOTES.md:104–105` generalise the *other* pages' pre-paint approach onto it. |
| "Account rows (email change w/ pending state, password change, sign out)" | `CardStock Mockup/DESIGN_NOTES.md:101` | Correct but incomplete: the third row is a **Session** row (`:169–175`) that displays "This device · Chicago, IL" and *hosts* Sign out. Also omits that the email-change form requires the current password (`:131`). |
| "chrome tokens (… `--acc` **#7290EA** · `--btn` **#4A66D8** · `--accBg` #252B44 · `--accMut` #3A4570 · `--mut3` · `--tooltipBg`)" for dark | `CardStock Mockup/DESIGN_NOTES.md:105` (light/dark accents repeated at `DISPLAY_VOCABULARY.md:85`: "accent #3B5BD6→#7290EA · button #3B5BD6→#4A66D8") | `:216` dark: `--acc` **`#8C9BF2`**, `--accH` `#8CA4F0`, `--btn` **`#4A63D0`**, `--btnH` `#AAB6F6`. `:217` light: `--acc`/`--btn` **`#4A63D0`**, `--accH`/`--btnH` `#3A4FB8`. Neither light nor dark accent/button matches the docs. `--accBg`, `--accMut`, `--mut3`, and `--tooltipBg` are **not defined on this page at all**. |
| "light mut2 #8A8A86 → dark #A8A8A2" | `CardStock Mockup/DISPLAY_VOCABULARY.md:85` | `:217` light `--mut2` = **`#6B6B66`** (dark `#A8A8A2` matches). `DESIGN_NOTES.md:136` records the darkening; the palette prose was not updated. |
| "CVD palette: … neg red→vermillion (**#B44A00** light / **#E8874D** dark)" | `CardStock Mockup/DESIGN_NOTES.md:103` (repeated `:104`) | `:219–220` set `--neg` to **`#CC5F00`** (light CVD) and **`#F5924E`** (dark CVD). `DISPLAY_VOCABULARY.md:81` agrees with the HTML; `DESIGN_NOTES.md:103` is stale. |
| "no verification emails; minimal password reset" (invite-only reversal) | `uploads/PROJECT_LOG.md:254`, quoted at `DECISIONS.md:287` | Not contradicted by this file directly, but note: `grep -ic invite` over `Cardstock Profile.dc.html` = **0** — no invite management, no "invite a friend", no remaining invite quota. See `docs/screens/account.md` §8 for the decisive evidence. |
