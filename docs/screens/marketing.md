# Marketing screens — Landing, Screener Landing, Charts Landing, Binder Landing

**Source of truth:** the four `.dc.html` prototypes, read directly 2026-08-10. Everything below is
derived from those files and cites `file:line`. Where a markdown doc disagrees, the HTML wins and the
disagreement is recorded in §8 — never averaged.

**Citation shorthand used throughout:**

| Prefix | File (all under `CardStock Mockup/`) | Lines |
|---|---|---|
| `L:` | `Cardstock Landing.dc.html` | 346 |
| `S:` | `Cardstock Screener Landing.dc.html` | 152 |
| `C:` | `Cardstock Charts Landing.dc.html` | 147 |
| `B:` | `Cardstock Binder Landing.dc.html` | 150 |

---

## 0. Read this first — the three findings that change implementation

1. **The Apr '25 seam is stated as fact in six places across three pages, and DECISIONS.md D-001 says
   it is false.** `L:202`, `L:235`, `L:236`, `C:45`, `C:74–75`, `C:113`, `S:92`. D-001: per-sale and
   census history begin at each card's **first crawler visit, late Jul 2026** — per-card and ragged,
   not a single shared April 2025 date. The marketing site's central trust claim ("the seam stays
   marked, never smoothed") currently names the wrong seam. Full analysis in §6.4 and §8.

2. **The Landing ticker's motion is pure CSS animation, and it is the only page in the whole
   prototype set with an infinite animation and no `prefers-reduced-motion` gate.** `@keyframes
   cdstkTicker` (`L:20`), applied at `L:334`. Gating is a Design-Composer author prop only
   (`tickerMotion`, `L:274`). Verified by grep: 0 occurrences of `prefers-reduced` in any of the four
   files, while `Cardstock Home.dc.html:25`, `Browse:23`, `Binder:25`, `Card:23`, `Set:23`,
   `Character:23` all carry the reduce query. See §3.1 and §4.3.

3. **Demo mode is fully gone from the marketing pages themselves — but every conversion CTA lands one
   hop from a live demo button.** Zero occurrences of "demo" in all four files (verified by grep). All
   11 sign-up/log-in CTAs target `Cardstock Account.dc.html`, which still renders
   `Browse the demo →` (`Cardstock Account.dc.html:56`, handler `:144`) navigating straight into the
   app. See §5.4.

---

## 1. Identity

| Page | Prototype | Route | Purpose |
|---|---|---|---|
| **Landing (overview)** | `Cardstock Landing.dc.html` | **logged-out `/`** | The marketing front door. Positions the whole product: hero + live market ticker + three-pillar toolkit grid + data-methodology section. The only page of the four with runtime logic. |
| **Screener Landing** | `Cardstock Screener Landing.dc.html` | marketing `/screener` (see route collision below) | Pillar page for the Screener. |
| **Charts Landing** | `Cardstock Charts Landing.dc.html` | marketing `/charts` | Pillar page for Charts. |
| **Binder Landing** | `Cardstock Binder Landing.dc.html` | marketing `/binder` | Pillar page for Binder. |

### 1.1 The `/` collision — logged-out Landing vs logged-in Home

`HANDOFF.md:71` assigns `/` to `Cardstock Home.dc.html` (the app). `HANDOFF.md:83` assigns
"marketing `/`" to `Cardstock Landing.dc.html`. Both are correct and both are the same URL: **`/`
resolves by authentication state** — anonymous visitors get the Landing, authenticated users get
Home. This must be an auth-conditional route in Blazor, not two distinct paths. Nothing in the HTML
contradicts this; the Landing's nav offers "Log in" and "Sign up" (`L:36–37`) and carries no app
chrome, while Home carries the 48px app nav.

### 1.2 The pillar-page route collision is unresolved

`HANDOFF.md:84` gives the three pillar pages "marketing `/screener` etc." while `HANDOFF.md:72–74`
gives the *app* screens `/screener`, `/charts`, `/binder`. Four routes, two claimants each. The HTML
cannot settle this — the prototypes link by filename (`Cardstock%20Screener%20Landing.dc.html`), not
by route. Open question §7.1.

### 1.3 Page identity is not marked in the HTML

None of the four pages sets a `<title>`, meta description, canonical URL, or Open Graph tag. The only
head content is font preconnects, the Google Fonts stylesheet, `<link rel="icon" href="brand/favicon.svg">`,
and a small `<style>` block (`L:10–23`, `S:10–21`, `C:10–21`, `B:10–21`). `brand/og-image.png` exists
on disk (64 KB) but **no page references it**. See §6.6.

### 1.4 Shell relationship

The three pillar pages are structurally identical to each other: same nav, same hero grid, same
three-card feature strip, same footer, differing only in eyebrow, headline, body copy, hero-art
mock, and which two sibling pillars the nav cross-links. The Landing is a superset: it adds the
ticker, the shuffle deck, the methodology section, and the only `data-props` block.

---

## 2. Layout

### 2.1 Shared shell (all four pages)

| Element | Landing | Pillars | Notes |
|---|---|---|---|
| Page background | `#F1F1EC` (`L:16`) | `#F1F1EC` (`S:16`, `C:16`, `B:16`) | Landing sets `body` only; pillars set `html, body`. |
| Body font | Inter (`L:24`) | Inter (`S:22`) | JetBrains Mono for all numerals, eyebrows, and metadata. |
| Ink | `#1C1C1E` | same | |
| Link colour | `#4A63D0`, hover `#3A4FB8` + underline (`L:17–18`) | same | |
| Selection | `rgba(74,99,208,0.18)` | same | |
| Content width | `max-width: 1080px`, `padding: … 40px` | same | Consistent across every section on every page. |
| Nav | sticky, `top:0`, `z-index:50`, `rgba(241,241,236,0.92)` + `backdrop-filter: blur(8px)`, 1px `#E4E4E0` bottom border, `14px 40px` (`L:26–40`) | identical (`S:24–38`, `C:24–38`, `B:24–38`) | |
| Footer | dark `#0F0F11`, top border `#232427`, `56px 40px 28px` (`L:247–270`) | identical (`S:125–148`, `C:120–143`, `B:123–146`) | Byte-identical content across all four. |

**Nav left cluster differs between Landing and pillars.** On the Landing the logo mark + wordmark +
`CDSTK` chip is a **plain `<div>`, not a link** (`L:28–32`). On the pillars the mark + wordmark is
wrapped in `<a href="Cardstock Landing.dc.html">` and the `CDSTK` chip is absent (`S:26–29`, `C:26–29`,
`B:26–29`). Implementation should keep both behaviours: on `/` the logo is inert; on pillar pages it
is the home link.

**Nav right cluster:**

| Page | Links, left to right |
|---|---|
| Landing (`L:34–37`) | Features (`#features`) · Data (`#data`) · Log in · **Sign up →** (filled `#4A63D0`, radius 7, `8px 14px`) |
| Screener (`S:31–35`) | Overview · Charts · Binder · Log in · **Sign up →** |
| Charts (`C:31–35`) | Overview · Screener · Binder · Log in · **Sign up →** |
| Binder (`B:31–35`) | Overview · Screener · Charts · Log in · **Sign up →** |

No pillar page links to itself; there is no active/current-tab state anywhere in the marketing nav,
and **no `href="#"` exists in any of the four files** (verified by grep, 0 hits each).

**Footer** (identical in all four; Landing cited):

- Two-column grid `1.4fr 1fr`, gap 32 (`L:249`).
- Left: 26px logo mark (teal `#3FBFAD` checkmark on `#0F0F11`) + "Cardstock" wordmark 19px/700
  (`L:252–253`), then the brand blurb, 14.5px/1.65, `#9A9A96`, `max-width: 340px` (`L:255`):
  *"Precise numbers over adjectives. No hype, no exclamation marks. We treat cardboard with the
  analytical rigor everyone in your life says it doesn't deserve. The market suggests otherwise."*
- Right: a single **PRODUCT** column (`L:258`) → Screener · Charts · Binder · About the data
  (`L:259–262`). No BRAND column, no Company/Legal column, no social links, no newsletter capture.
- Bottom bar (`L:265–267`), separated by a `#232427` rule with `margin-top: 40px; padding-top: 18px`:
  left = `CDSTK · fan-made · not affiliated with Nintendo, The Pokémon Company, or Creatures Inc.`;
  right = Privacy · Terms · `© 2026 Cardstock`.

### 2.2 Landing — section-by-section

| # | Section | Lines | Content |
|---|---|---|---|
| 1 | Nav | `L:26–40` | `data-screen-label="Nav"`. As above. |
| 2 | **Ticker bar** | `L:42` | One line: `<div style="background:#131316; padding:10px 0; overflow:hidden;">{{ tickerTrack }}</div>`. Dark bar, full-bleed, directly under the nav. Full spec in §3.1. |
| 3 | **Hero** | `L:44–177` | `data-screen-label="Hero"`, `overflow-x: clip`. Grid `1.05fr 0.95fr`, gap 48, `padding: 52px 40px 36px`, vertically centred. |
| 4 | **Features** | `L:179–213` | `id="features"`, top border `#E4E4E0`, `padding: 16px 40px 56px`. |
| 5 | **Methodology** | `L:215–245` | `id="data"`, dark `#131316`, `padding: 52px 40px 56px`. **Wrapped in `<sc-if value="{{ showMethodology }}">`** — conditionally rendered. |
| 6 | Footer | `L:247–270` | As above. |

**Hero left column** (`L:46–54`), flex column gap 20:

1. Mono eyebrow 12px/500, letter-spacing `0.08em`, `#4A63D0`: `POKÉMON TCG AFTERMARKET DATA` (`L:47`).
2. `<h1>` **52px**/800, `letter-spacing -0.03em`, `line-height 1.06`, `max-width 520px`,
   `text-wrap: pretty`: **"The trading terminal for Pokémon cards."** (`L:48`).
3. Body 17px/1.6 `#55555A`, `max-width 480px` (`L:49`): *"Five years of graded-card price history, a
   screener across every printing, and your binder tracked like a portfolio. For collectors who read
   pop reports for fun."*
4. **A single CTA** — `Sign up →`, 15px/600, white on `#4A63D0`, radius 8, `11px 20px` (`L:51`). The
   flex row is built for two (`gap: 18px`, `L:50`) but holds only one. There is no secondary CTA on
   the Landing hero — unlike all three pillar heroes.
5. Mono caption 12px `#8A8A86` (`L:53`): `press / to search · fan-made`, with `/` in a bordered key
   cap. **No search input exists on this page** — see §5.5.

**Hero right column** (`L:55–175`) — `position: relative; min-height: 440px`. Two superimposed
layers:

*Layer 1 — three static card images*, absolutely positioned, all `image-slot` custom elements:

| Slot id | Position | Size | Rotation | Radius | Line |
|---|---|---|---|---|---|
| `hero-card-right` | `top:-24px; right:-48px` | 330×462 | 0° | 16 | `L:56–58` |
| `hero-card-mid` | `top:158px; left:-24px` | 150×210 | +11° | 11 | `L:59–61` |
| `hero-card-left` | `left:4px; bottom:0` | 120×168 | −8° | 10 | `L:62–64` |

*Layer 2 — the four-panel shuffle deck* (`L:65–173`), each panel `{{ slotA }}`…`{{ slotD }}` with
`onClick`/`onMouseEnter`/`onMouseLeave`. Full behaviour in §3.2. Panel contents:

- **A — Watchlist** (`L:66–92`): header `WATCHLIST · 30D` / `LIVE`; four rows (name, set · grade,
  price, delta); a 9-point sparkline SVG (`L:87`); footer `press / to search` + `12M · normalized`.
- **B — Binder** (`L:95–113`): header "Binder" + `Performance →`; three stat cells; a summary row;
  chrome caption `BINDER — value & P&L` / `/home`.
- **C — Screener** (`L:116–142`): header `"Quiet Accumulation"` + `12 matches`; a 4-column grid
  (CARD · PRICE · ROC 3M · CHURN) with three rows; caption `SCREENER — "Quiet Accumulation"` /
  `/screener`.
- **D — Charts** (`L:145–172`): card header + price/delta; a 130px-tall SVG with 3 gridlines, 3 axis
  labels, Bollinger band fill + two dashed band edges, a dashed mid-line, an 11-point close polyline
  and an end dot; a stats row (ROC 12M / RS / BB); caption `CHARTS — price history · PSA 10` /
  `/charts`.

Below the deck, `position: absolute; right:24px; bottom:-30px`, mono 11px `#8A8A86`:
`{{ frontLabel }} · click to shuffle` (`L:174`).

**Features section** (`L:179–213`):

- Three more decorative `image-slot` cards, absolutely positioned *behind* the grid: `features-card`
  (130×182, +10°, `L:181–183`), `features-card-2` (120×168, −9°, `L:184–186`), `features-card-3`
  (110×154, −6°, `L:187–189`).
- Mono eyebrow `THE TOOLKIT` (`L:190`); `<h2>` 30px/700 **"Three ways in."** (`L:191`).
- Three-column card grid, gap 20, `margin-top: 28px` (`L:192`). Each card: white, 1px `#E4E4E0`,
  radius 10, padding 24, flex column gap 12, containing a 64×40 line-art SVG, an `<h3>` 17px/650, a
  14px/1.55 `#55555A` paragraph, and a mono 11.5px `#4A63D0` kicker.

| Card | Heading | Kicker | Lines |
|---|---|---|---|
| 1 | Screener | `saved screens · backtest →` | `L:193–198` |
| 2 | Charts | `open in charts →` | `L:199–204` |
| 3 | Binder | `+ binder` | `L:205–210` |

The three kickers **are not links** — plain `<div>`s. See §5.5.

**Methodology section** (`L:216–244`, inside the `sc-if`):

- Two decorative `image-slot` cards on the dark ground: `data-card` (130×182, −7°, `L:218–220`) and
  `data-card-2` (120×168, −11°, `L:221–223`).
- Mono eyebrow `WHERE THE NUMBERS COME FROM` in `#8C9BF2` (`L:224`).
- A baseline-aligned row (`L:225–228`): `<h2>` 30px/700 `#F2F2EE` **"Trust is a feature."** and, right,
  the link **`About the data →`** (`#8C9BF2`, hover `#AAB6F6`) → `Cardstock About Data.dc.html`.
- Three dark panels (`#1B1C1F`, border `#2A2B2E`, radius 10, padding 22), each a mono eyebrow +
  14px/1.6 `#B9B9B4` paragraph:

| Panel | Eyebrow | Line |
|---|---|---|
| 1 | `PER-SALE LEDGERS` | `L:230–233` |
| 2 | `THE APR '25 SEAM` | `L:234–237` |
| 3 | `SUFFICIENCY RULES` | `L:238–241` |

Panel 3 embeds a live `LOW CONFIDENCE` chip inline in the sentence (`L:240`): mono 10.5px, `#C9A84C`
on `rgba(201,168,76,0.08)`, 1px `rgba(201,168,76,0.4)`, radius 5, `1px 6px`, `white-space: nowrap`.

### 2.3 Pillar pages — shared layout

All three: nav → hero → features → footer. No ticker, no shuffle, no methodology section, no props,
no script block, no `image-slot` elements.

**Hero** (`S:40–98`, `C:40–93`, `B:40–96`): grid `1fr 1fr` (not the Landing's `1.05fr 0.95fr`), gap
48, `padding: 52px 40px 56px`, `overflow-x: clip`, vertically centred.

Left column, flex gap 20: mono eyebrow → `<h1>` **48px**/800 `max-width 480px` → 17px/1.6 body
`max-width 460px` → **two** CTAs in a `gap: 18px` row: primary `Sign up →` (filled indigo) and
secondary text link `All of Cardstock →` (14px/600) → `Cardstock Landing.dc.html`.

| Page | Eyebrow | Headline | Lines |
|---|---|---|---|
| Screener | `PRODUCT · SCREENER` | **"Every printing, ranked your way."** | `S:43–44` |
| Charts | `PRODUCT · CHARTS` | **"Price history you can trust."** | `C:43–44` |
| Binder | `PRODUCT · BINDER` | **"Your binder is a portfolio."** | `B:43–44` |

Right column: a **500px-wide dark app replica** (`#131316`, 1px `#2A2B2E`, radius 12,
`box-shadow: 0 24px 48px rgba(28,28,30,0.25)`, `overflow: hidden`, `max-width:100%`), right-aligned
(`S:51–52`, `C:51–52`, `B:51–52`). These are static hand-built HTML/SVG replicas of real app screens,
not screenshots.

**Features strip** (`S:100–123`, `C:95–118`, `B:98–121`): top border `#E4E4E0`,
`padding: 36px 40px 56px`, three-column grid gap 20. **No eyebrow and no section heading** — unlike
the Landing, which has `THE TOOLKIT` / "Three ways in." Card anatomy is identical to the Landing's.

| Page | Card 1 | Card 2 | Card 3 |
|---|---|---|---|
| Screener (`S:103–120`) | "Filters that mean something" · `12 filters · honest floors` | "Save a thesis" · `saved screens →` | "Backtest honestly" · `backtest · vs market →` |
| Charts (`C:98–115`) | "Every grade tier" · `6 tiers · compare mode` | "Indicators for thin markets" · `indicators · composites G1–G4` | "The seam stays marked" · `about the data →` |
| Binder (`B:101–118`) | "Cost basis & P&L" · `lots · cost · P&L` | "Vs the market" · `+8.7 pp · 12M` | "A real ledger" · `transactions · export CSV` |

All nine kickers are plain `<div>`s, not links — including Charts' `about the data →` (`C:114`),
which reads as a link and is not one.

---

## 3. Data contract

Everything on all four pages is **statically authored** except the Landing's `tickerTrack`, its four
slot styles, and `frontLabel`. Nothing fetches. The three pillar pages contain **zero** dynamic
values — no `{{ }}` bindings, no `sc-for`, no `sc-if`, no `data-props`, no `<script type="text/x-dc">`.

### 3.1 The Landing ticker — complete enumeration

**Mechanism (this is the answer to "CSS or data-driven"): pure CSS animation. Not data-driven.**

- `@keyframes cdstkTicker { from { transform: translateX(0); } to { transform: translateX(-50%); } }`
  — declared once in the helmet `<style>` (`L:20`).
- The track is built in `renderVals()` (`L:334`):
  `React.createElement('div', { style: { display:'flex', width:'max-content', animation: motion ? 'cdstkTicker 44s linear infinite' : 'none' } }, spans)`.
- `const motion = this.props.tickerMotion ?? true` (`L:310`).
- **The item list is duplicated** — `items.concat(items).map(...)` (`L:326`) — so 16 unique stats
  render as 32 spans. The `-50%` end state lands exactly on the start of the second copy, giving a
  seamless loop. **This duplication is load-bearing: halving `translateX` requires exactly two copies.**
- Each span: `display:inline-flex; gap:8px; align-items:baseline; margin-right:40px; white-space:nowrap;`
  JetBrains Mono 12px (`L:327`), composed of up to four children (`L:328–331`): label `#B9B9B4` →
  optional name `#D6D6D0` → value in the item's colour, weight 500 → optional suffix `#B9B9B4`.
- Palette (`L:314`): `G = #46C08A` (positive), `L = #D0655E` (negative), `K = #F2F2EE` (neutral).
- **No values ever change.** There is no timer, no state, and no re-render tied to the ticker. Motion
  is entirely the `transform` animation.

**Windows.** The whole set is the **30-day** window. The source comment at `L:313` states it:
*"Same market-stat items as the app's Home ticker (30d window), on the landing's dark bar."* Verified
against `Cardstock Home.dc.html:449–457` — all 16 values are byte-identical to Home's `'30d'` array.
Two items carry an **explicit `30d` suffix chip** overriding the implied window; that suffix is the
monthly-data honesty rule (`DESIGN_NOTES.md:28`: "INDEX + NEW 12M HIGHS always 30d-labeled").
**Unlike Home, the Landing has no 7d/30d/90d selector** — the window is fixed and unlabelled except on
those two items.

**All 16 stats, in render order** (`L:316–325`):

| # | Label (`l`) | Name (`n`) | Value (`v`) | Suffix (`x`) | Colour | Window |
|---|---|---|---|---|---|---|
| 1 | `SALES` | — | `41,208 ▲ +9%` | — | pos | 30d (implied) |
| 2 | `VOLUME` | — | `$2.1M ▲ +11%` | — | pos | 30d (implied) |
| 3 | `BREADTH` | — | `58% advancing` | — | pos | 30d (implied) |
| 4 | `INDEX` | — | `▲ +2.4%` | `30d` | pos | **30d (explicit)** |
| 5 | `VINTAGE` | — | `58% of $ vol` | — | neutral | 30d (implied) |
| 6 | `GRADING` | — | `+4,120 slabs · gem 46%` | — | neutral | 30d (implied) |
| 7 | `MEDIAN SALE` | — | `$84 ▼ −3%` | — | neg | 30d (implied) |
| 8 | `VENUE` | — | `ebay 82% · auction 11% · tcgp 7%` | — | neutral | 30d (implied) |
| 9 | `NEW 12M HIGHS` | — | `▲ 214` | `30d` | pos | **30d (explicit)**, measured against a 12M high-water mark |
| 10 | `MEDIAN ROC` | — | `▲ +1.4%` | — | pos | 30d (implied) |
| 11 | `TOP WINNER` | `Espeon Gold Star` | `▲ +14%` | — | pos | 30d (implied) |
| 12 | `TOP LOSER` | `Blastoise Holo PSA 8` | `▼ −6.1%` | — | neg | 30d (implied) |
| 13 | `TOP SALE` | `Lugia 1st Ed PSA 10` | `$18,500` | `goldin` | neutral | 30d (implied); suffix is the venue |
| 14 | `MOST ACTIVE` | `Charizard ex SAR` | `214 sales` | — | neutral | 30d (implied) |
| 15 | `HOT SET` | `Evolving Skies` | `▲ +4.8%` | — | pos | 30d (implied) |
| 16 | `CHARACTER LEADER` | `Giratina` | `▲ +6.2%` | — | pos | 30d (implied) |

**Implementation contract:** one query returning 16 rows shaped
`{ label, name?, value, suffix?, direction }` for a fixed 30-day window, rendered twice into one flex
track. `direction` (pos/neg/neutral) drives colour; the ▲/▼ glyph is baked into `value` in the
prototype and should be derived from `direction` in the real implementation. Item 4 (`INDEX`) and
item 9 (`NEW 12M HIGHS`) must always render the literal `30d` suffix regardless of any future window
selector.

**Sufficiency risk on 12 of 16 items — see §6.4.**

### 3.2 The shuffle deck — state machine (`L:279–341`)

**State** (`L:279`): `{ order: ['A','B','C','D'], exiting: null, paused: false }`.

- `componentDidMount` (`L:280–284`): `setInterval` at **4200 ms**; each tick calls `doShuffle()` only
  if `!paused && (props.cardShuffle ?? true)`.
- `componentWillUnmount` (`L:285`): clears both the interval and the landing timeout.
- `doShuffle()` (`L:286–292`): **re-entry guard** — returns immediately if `state.exiting` is set.
  Otherwise sets `exiting = order[0]`, then after **620 ms** rotates (`order.slice(1).concat(order[0])`)
  and clears `exiting`.
- `slotStyle(id)` (`L:293–308`): base is `position:absolute; top:32px; right:16px; width:360px;
  max-width:92%; cursor:pointer; transform-origin:50% 80%;` with transition
  `transform 0.65s cubic-bezier(0.25,0.7,0.25,1), opacity 0.65s ease`.
  - **Exiting panel** (`L:295–297`): `zIndex 50`, `transform: translate(150px,110px) rotate(14deg)`,
    `opacity 0`, transition `transform 0.6s cubic-bezier(0.5,0,0.8,0.4), opacity 0.55s ease-in`.
  - **Otherwise** position is the index in `order` with the exiting panel filtered out (`L:298–299`);
    a panel not found defaults to position 3.

| Position | Transform | z-index | Opacity |
|---|---|---|---|
| 0 (front) | `translate(0,0) rotate(0deg) scale(1)` | 40 | 1 |
| 1 | `translate(-36px,-26px) rotate(-5deg) scale(0.97)` | 30 | 1 |
| 2 | `translate(26px,-46px) rotate(4deg) scale(0.94)` | 20 | 0.85 |
| 3 (back) | `translate(-4px,-58px) rotate(-1deg) scale(0.91)` | 10 | **0** |

Source: `L:300–305`. Note position 3 is fully transparent — only three panels are ever visible.

- `frontLabel` (`L:311–312`, `L:337`): label map `{ A:'watchlist', B:'/home', C:'/screener',
  D:'/charts' }`; front = `order[0]`, or the first non-exiting entry while a shuffle is in flight;
  fallback `'watchlist'`. Rendered at `L:174` as `{{ frontLabel }} · click to shuffle`.
- Handlers exposed (`L:338–340`): `shuffleNow`, `pauseShuffle`, `resumeShuffle`.

### 3.3 Author-time props (`L:273–277`) — Landing only

| Prop | Editor | Default | Section | Effect |
|---|---|---|---|---|
| `tickerMotion` | boolean | `true` | Motion | `true` → `animation: cdstkTicker 44s linear infinite`; `false` → `animation: none` (`L:310`, `L:334`). |
| `cardShuffle` | boolean | `true` | Motion | Gates only the **auto**-advance interval (`L:282`). Click/hover still work when `false`. |
| `showMethodology` | boolean | `true` | Sections | Renders/omits the entire `#data` section (`L:215`, `L:335`). |

The three pillar pages have **no** `data-props` and no script block at all.

### 3.4 Static seeded values — the illustrative set

These are hand-authored copy, not a data contract, but the **shape** is binding. Recurring entities
across all four pages: Umbreon VMAX (Alt Art) · Evolving Skies · PSA 10 · `215/203`; Giratina V (Alt
Art) · Lost Origin · PSA 10; Sylveon VMAX (Alt Art); Blastoise Holo · Base Set 2 · Grade 9; Charizard
Holo · Base Set · Grade 9; Leafeon VMAX (Alt Art).

**Landing — watchlist panel A** (`L:71–86`): four rows `{name, set · grade, price, Δ%}` —
Giratina V `$845 ▲ +16.7%` · Umbreon VMAX `$1,486 ▲ +10.9%` · Sylveon VMAX `$612 ▲ +1.4%` ·
Blastoise Holo `$318 ▼ −5.1%`. Sparkline: 9 points (`L:87`). Footer: `12M · normalized` (`L:90`).

**Landing — binder panel B** (`L:102–109`): `TOTAL VALUE $18,432` · `UNREALIZED +$3,108 ▲ +20.3%` ·
`VS MARKET +8.7 pp · 12M`; summary `14 positions · 6 sets`, `Cost $15,324`, `▲ +$412 1M`.

**Landing — screener panel C** (`L:119–138`): screen `"Quiet Accumulation"`, `12 matches`; columns
CARD · PRICE · ROC 3M · CHURN; three rows with churn multipliers ×2.4 / ×1.6 / ×1.9.

**Landing — charts panel D** (`L:148–168`): Umbreon VMAX PSA 10 `$1,309 ▲ +6.1%`; y-labels
`$1,350 / $1,100 / $850`; `ROC 12M +18.2%` · `RS 94th` · `BB 20 · 2`.

**Screener Landing hero mock** (`S:53–94`): screen chip `"QUIET ACCUMULATION"` plus three removable
filter chips each with a `✕` — `churn ≥ ×1.5`, `ROC 3M > 0`, `z-score < 1` — and `12 matches`.
Five columns: Card · Price · ROC 3M · Churn · Z-score. Four rows; row 2 has an alternating
`#1B1C1F` background (`S:70`); **row 4 (Leafeon VMAX) carries an inline `LOW CONFIDENCE` badge**
(mono 9px, `#C9A84C` on transparent, 1px `#C9A84C`, radius 3 — a *different* badge treatment from
the Landing's methodology chip). Footer bar: `honest floor Apr '25` (left) and a
`Backtest this screen →` button-styled **`<span>`** (right, `S:91–93`).

**Charts Landing hero mock** (`C:53–89`): header Umbreon VMAX (Alt Art) · `Evolving Skies · 215/203` ·
`$1,309 ▲ +6.1%`. Three tier chips — **PSA 10 active** (`#8C9BF2` border + dot), Grade 9 (`#E0A93C`
dot), Raw (`#A5A5A0` dot) — plus `EMA 12 · BB 20·2 · RSI`. SVG (`C:66–85`): 4 gridlines, y-labels
`$1,350/$1,100/$850`, x-labels `2024 / 2025 / 2026`, a **dashed `#C9A84C` vertical seam line at
x=168 labelled `APR '25 SEAM`** (`C:74–75`), a Bollinger fill + two dashed edges, a 14-point `#8C9BF2`
close polyline, a 14-point `#E0A93C` second series, and a **hollow end-point** (`fill:#131316`,
`stroke:#8C9BF2`, `C:81`) for the still-revising current month. Footer:
`monthly closes · current month still revising ○` and `ROC 12M +18.2% · RS 94th`.

**Binder Landing hero mock** (`B:53–92`): three stat cells (Total value `$18,432` · Unrealized
`+$3,108 ▲ +20.3%` · Vs market `+8.7 pp · 12M`); five columns Position · Qty · Cost · Value · P&L;
four rows — Umbreon VMAX PSA 10 · 2 lots · qty 2 · `$2,410` → `$2,972` = `+$562`; Charizard Holo Base
Set Grade 9 · 1 lot · `$4,850` → `$6,120` = `+$1,270`; Blastoise Holo Base Set 2 Grade 9 · `$342` →
`$318` = **`−$24`** (the only red row); Giratina V Lost Origin PSA 10 · `$690` → `$845` = `+$155`.
Footer: `14 positions · 6 sets · cost $15,324` and `▲ +$412 1M`.

**Cross-page consistency is exact and must be preserved:** `$18,432`, `+$3,108`, `+20.3%`, `+8.7 pp`,
`14 positions · 6 sets`, `$15,324`, `+$412 1M` are identical in `L:102–109` and `B:54–91`; the
Umbreon/Giratina/Sylveon prices are identical in `L:72–85`, `L:128–137`, and `S:65–82`; `$1,309 ▲ +6.1%`,
`ROC 12M +18.2%`, `RS 94th` are identical in `L:149–167` and `C:58–88`.

### 3.5 Colour tokens used by the dark surfaces

`#131316` (panel ground) · `#0F0F11` (footer / panel chrome bar) · `#1B1C1F` (inner panel, alt row) ·
`#2A2B2E` (panel border) · `#232427` (inner rule) · `#F2F2EE` (ink) · `#D6D6D0` (secondary ink) ·
`#B9B9B4` (muted — `DESIGN_NOTES.md:147` records the deliberate lightening to this value) ·
`#8C9BF2` (accent on dark) · `#46C08A` (positive) · `#D0655E` (negative) · `#C9A84C` (warning /
seam / low-confidence) · `#E0A93C` (Grade 9 series) · `#A5A5A0` (Raw series).

Light surfaces: `#F1F1EC` (page) · `#FFFFFF` (card) · `#E4E4E0` (line) · `#1C1C1E` (ink) ·
`#55555A` (body) · `#8A8A86` (muted) · `#4A63D0` (accent) · `#3A4FB8` (accent hover) ·
`#0E8A7B` (logo checkmark, light) · `#3FBFAD` (logo checkmark, dark footer).

---

## 4. States

### 4.1 Landing state space (complete)

| Axis | Values | Source |
|---|---|---|
| Methodology section | rendered / omitted | `sc-if` on `showMethodology` (`L:215`) |
| Ticker motion | animating / static | `tickerMotion` (`L:310`, `L:334`) |
| Deck auto-advance | on / off | `cardShuffle` (`L:282`) |
| Deck rotation | 4 orderings (A/B/C/D front) | `state.order` (`L:279`, `L:290`) |
| Deck transition | at rest / exiting | `state.exiting` (`L:288`, `L:291`) |
| Deck hover | paused / running | `state.paused` (`L:339–340`) |

The product of these is 2 × 2 × 2 × 4 × 2 × 2 = 64 nominal combinations; the meaningful visual set is
**4 deck orderings × 2 transition phases**, plus the two motion switches and the section toggle.

### 4.2 States that do **not** exist on these pages

No loading, empty, error, offline, or partial-data state. No skeletons. No auth-aware variation
(these are logged-out pages; there is no "you're already signed in" branch). No dark mode — **zero
`data-theme` occurrences in all four files** (verified by grep), while every app page carries a dark
token block (`DESIGN_NOTES.md:105`). No colourblind (`data-cvd`) variant. No responsive breakpoints:
every grid is a fixed `1fr 1fr` / `1fr 1fr 1fr` with no `@media` query anywhere in the four files —
see §7.4.

### 4.3 Reduced motion — the gap, stated precisely

**There is no `prefers-reduced-motion` handling on any of the four marketing pages.** Verified by
grep across the whole mockup directory: the query appears in `Cardstock Home.dc.html:25`,
`Cardstock Browse.dc.html:23`, `Cardstock Binder.dc.html:25`, `Cardstock Card.dc.html:23`,
`Cardstock Set.dc.html:23`, `Cardstock Character.dc.html:23`, and inside `image-slot.js:382` — and in
**none** of Landing / Screener Landing / Charts Landing / Binder Landing.

The app pages all use the same one-liner:
`@media (prefers-reduced-motion: reduce) { * { animation-duration: 0.01ms !important; } }`

Consequences for the Landing specifically:

1. **The 44 s infinite ticker keeps animating for a user who asked the OS to stop motion.** The only
   off-switch is `tickerMotion`, a Design-Composer *author* prop — it is not exposed to the visitor
   and has no runtime source.
2. **The 4.2 s deck shuffle likewise keeps running** — and `cardShuffle` is the same kind of
   author-only prop. Note the app pages' one-liner would not have stopped the deck anyway: the deck
   moves via `transition`, not `animation`, so `animation-duration: 0.01ms` does not touch it. A
   correct implementation needs `transition-duration` handled too, plus suppression of the interval.
3. **The Landing ticker also lacks the hover-pause the app ticker has.** `Cardstock Home.dc.html:66`
   carries `style-hover="animation-play-state: paused;"`; `L:334` has no hover behaviour. The deck,
   by contrast, *does* pause on hover (`L:65`, `L:94`, `L:115`, `L:144`).

The brand package anticipated this and the prototype did not implement it:
`uploads/Brand package creation/README.md:115` — *"Respect `prefers-reduced-motion` — both animations
should have an off path (they are toggleable props in the prototype)."* Treated here as a **known
prototype gap to close in implementation**, not a design ruling to copy. Recommended contract:
`prefers-reduced-motion: reduce` ⇒ ticker renders static (one copy of the 16 items, horizontally
scrollable) and the deck renders its resting stack with auto-advance disabled, click still working.

### 4.4 Image-slot states

The eight `image-slot` elements (§6.6) are custom elements from `image-slot.js` with
`placeholder="drop card image"`. Unfilled they render a placeholder; `image-slot.js:382` gives the
loading shimmer its own reduced-motion off-path. Whether the prototype's local
`.image-slots.state.json` (299 KB) contains dropped images for these specific ids is a design-time
detail, not a runtime state.

---

## 5. Interactions

### 5.1 Every link, with destination

**Landing — 12 anchors total.**

| # | Control | Line | Destination |
|---|---|---|---|
| 1 | Nav "Features" | `L:34` | `#features` (in-page, `L:179`) |
| 2 | Nav "Data" | `L:35` | `#data` (in-page, `L:216`) — **dangles if `showMethodology` is false** |
| 3 | Nav "Log in" | `L:36` | `Cardstock Account.dc.html` |
| 4 | Nav "Sign up →" | `L:37` | `Cardstock Account.dc.html` |
| 5 | Hero "Sign up →" | `L:51` | `Cardstock Account.dc.html` |
| 6 | "About the data →" | `L:227` | `Cardstock About Data.dc.html` |
| 7 | Footer "Screener" | `L:259` | `Cardstock Screener Landing.dc.html` |
| 8 | Footer "Charts" | `L:260` | `Cardstock Charts Landing.dc.html` |
| 9 | Footer "Binder" | `L:261` | `Cardstock Binder Landing.dc.html` |
| 10 | Footer "About the data" | `L:262` | `Cardstock About Data.dc.html` |
| 11 | Footer "Privacy" | `L:267` | `Cardstock Legal.dc.html#privacy` |
| 12 | Footer "Terms" | `L:267` | `Cardstock Legal.dc.html#terms` |

**Screener Landing — 13 anchors.** Logo → Landing (`S:26`); nav Overview → Landing (`S:31`),
Charts → Charts Landing (`S:32`), Binder → Binder Landing (`S:33`), Log in → Account (`S:34`),
Sign up → Account (`S:35`); hero Sign up → Account (`S:47`), "All of Cardstock →" → Landing (`S:48`);
footer Screener/Charts/Binder/About the data (`S:137–140`); Privacy/Terms (`S:145`).

**Charts Landing — 13 anchors.** Same pattern; nav cross-links are Screener (`C:32`) and Binder
(`C:33`). Logo `C:26`; nav `C:31–35`; hero `C:47–48`; footer `C:132–135`, `C:140`.

**Binder Landing — 13 anchors.** Nav cross-links Screener (`B:32`) and Charts (`B:33`). Logo `B:26`;
nav `B:31–35`; hero `B:47–48`; footer `B:135–138`, `B:143`.

**Total conversion CTAs across the four pages: 11** — three on the Landing (`L:36`, `L:37`, `L:51`)
and eight on the pillars (`S:34`, `S:35`, `S:47`; `C:34`, `C:35`, `C:47`; `B:34`, `B:35`, `B:47`).
**All 11 target `Cardstock Account.dc.html`.** No CTA anywhere in the marketing site enters the app
directly.

### 5.2 Hover states

All nav/footer links use the `style-hover` attribute (the Design Composer hover mechanism):
secondary links → `color: #4A63D0` (light) or `#8C9BF2` (dark footer), `text-decoration: none`; the
filled Sign-up button → `background: #3A4FB8`, `color: #FFFFFF`. Global `a:hover` (`L:18`) sets
`#3A4FB8` + underline for anything without an explicit hover style. `About the data →` hovers to
`#AAB6F6` (`L:227`).

### 5.3 The shuffle deck — the only non-navigational interaction on the entire marketing site

Bound identically on all four panels (`L:65`, `L:94`, `L:115`, `L:144`):

| Event | Handler | Behaviour |
|---|---|---|
| `onClick` | `shuffleNow` (`L:338`) | Advances immediately; no-op while a card is exiting (`L:287`). |
| `onMouseEnter` | `pauseShuffle` (`L:339`) | `paused = true` — suspends auto-advance only. |
| `onMouseLeave` | `resumeShuffle` (`L:340`) | `paused = false`. |

Discoverability comes from `cursor: pointer` (`L:294`) and the caption `… · click to shuffle`
(`L:174`). **No keyboard affordance**: the panels are `<div>`s with no `tabindex`, no `role`, and no
key handler. **No accessibility metadata anywhere on these pages** — the four files contain no `role`,
`aria-*`, or `alt` attribute of any kind.

### 5.4 Demo mode — status as of 2026-08-10

**On the four marketing pages: absent.** Zero occurrences of "demo" in any of them (grep-verified, 0
hits each). No "View live demo", no `/demo` route, no demo chip. This is the result of the deliberate
copy pass recorded at `DESIGN_NOTES.md:147` — *"the word 'demo' removed (footer + hero caption;
watchlist chip → LIVE)"* — which is why the watchlist panel now reads `LIVE` (`L:69`) and the footer
reads "fan-made" (`L:266`). `HANDOFF.md:100` confirms the ruling: *"Demo mode — removed 2026-08-10;
the marketing pages carry that story now."*

**One hop downstream: still live.** All 11 conversion CTAs land on `Cardstock Account.dc.html`, which
renders a full-width `Browse the demo →` button (`Cardstock Account.dc.html:56`) whose handler
navigates straight to `Cardstock Home.dc.html` (`:144`), with the tooltip *"Explore the whole app with
seeded data — nothing you change is saved."* `DESIGN_NOTES.md:141` names the pages the demo affordance
was deleted from — *"Home/Browse/Binder/Card/Set/Character"* — and **Account is not among them**, so
this reads as deliberate retention rather than an oversight. Either way the marketing funnel still
terminates in a demo entry point. **Owner ruling needed before the sign-up funnel is built** (§7.2).

### 5.5 Affordances that look interactive and are not

| Element | Line | Why it matters |
|---|---|---|
| `press / to search` hero caption | `L:53` | Promises a keyboard shortcut. **No search input exists on the Landing**; `cardstock-search.js` is a shared *app*-nav component (`HANDOFF.md:86`) and is not loaded by any marketing page. Either wire `/` to something or drop the caption. |
| `press / to search` inside panel A | `L:89` | Acceptable — it is chrome inside a rendering of an app screen. |
| Feature-card kickers ×3 | `L:197`, `L:203`, `L:209` | Styled accent-blue with `→`, but plain `<div>`s. |
| Feature-card kickers ×9 on pillars | `S:107`, `S:113`, `S:119`; `C:102`, `C:108`, `C:114`; `B:105`, `B:111`, `B:117` | Same. `C:114` (`about the data →`) is the most likely to be clicked and the most likely to disappoint. |
| `Performance →` in panel B | `L:99` | Chrome inside the app replica. |
| `Backtest this screen →` | `S:93` | Rendered as a filled indigo button but is a `<span>`. Chrome inside the app replica. |
| Filter chips with `✕` | `S:55–57` | Look removable; static. Chrome inside the app replica. |
| Tier chips (PSA 10 / Grade 9 / Raw) | `C:61–63` | Look toggleable; static. Chrome inside the app replica. |

Design decision needed: should the kickers become real links? They are the only "learn more" path
from the feature grid, and the Landing's Screener/Charts/Binder cards do **not** link to their pillar
pages — the only route from the Landing to a pillar page is via the footer.

---

## 6. Rules and invariants

### 6.1 Forbidden content — confirmed absent

Grep-verified across all four files:

| Forbidden | Present? | Evidence |
|---|---|---|
| **Pricing** | **No.** | No price/tier/plan/"free trial"/"$/mo" copy. The only `$` figures are card prices inside app replicas. The word "Price" appears only as a column header (`S:61`, `L:124`) and in the Charts headline "Price history you can trust." (`C:44`). There is no pricing page, no pricing nav link, and no billing language. |
| **Testimonials** | **No.** | No quotes, names, avatars, photos, or attributed praise. The only quoted strings are screen *names* — `"Quiet Accumulation"` (`L:119`, `S:54`), `"Vintage on sale"` (`S:112`). |
| **Fake social proof** | **No.** | No user counts, "trusted by", logo walls, star ratings, review counts, press badges, or "join N collectors". No numbers on any page describe *users* — every number describes market data or a seeded portfolio. |
| Urgency / scarcity | **No.** | No countdowns, "limited", "beta spots", waitlist counters. |
| Exclamation marks in body copy | **No.** | Consistent with the footer's own promise, "No hype, no exclamation marks" (`L:255`). |

The strongest positive evidence is the footer blurb itself (`L:255`, identical on all four pages),
which states the constraint the pages then obey.

### 6.2 Honesty apparatus is present *in the marketing surface*, not just the app

This is unusual and deliberate — the marketing pages surface the product's limitations rather than
hiding them:

- `LOW CONFIDENCE` chip rendered inline in methodology copy (`L:240`) and as a row badge in the
  Screener hero mock (`S:85`).
- "Thin markets get flagged, not filled in… visible, never hidden" (`L:240`).
- "Locked filters show their unlock date instead of guessing" (`S:106`).
- "signals that never fire on partial inputs. Anything without enough history stays locked, with its
  unlock date shown" (`C:107`).
- "Overfit screens get called out, not celebrated" (`S:118`).
- "the current month renders as a hollow, still-revising point" (`C:113`), drawn at `C:81` and
  captioned `monthly closes · current month still revising ○` (`C:87`).
- "preserved, never silently edited — the same standard we hold our market data to" (`B:116`).
- `honest floor Apr '25` (`S:92`) — the right idea with the wrong date (§6.4).

**Invariant:** these claims are promises the app must keep. Any implementation that silently
interpolates, smooths, or back-fills contradicts the marketing page.

### 6.3 Brand rules that differ from app pages

| Dimension | Marketing | App pages | Evidence |
|---|---|---|---|
| Page ground | `#F1F1EC` | `#FAFAF7`-family app chrome | `L:16` vs the app files' token blocks |
| Nav height | ~48px content in `14px 40px` padding, sticky + **blurred** (`backdrop-filter: blur(8px)`), 92%-opaque | 48px solid app nav with search + account circle | `L:26` vs `HANDOFF.md:88` |
| Content width | 1080px | wider app layouts | `L:27` |
| Display type | **52px/800** h1 on Landing, **48px/800** on pillars, `letter-spacing -0.03em`, `line-height 1.06`, `text-wrap: pretty` | app pages top out far smaller | `L:48`, `S:44`, `C:44`, `B:44` |
| Section heads | 30px/700 `-0.02em` | — | `L:191`, `L:226` |
| Body copy | 17px/1.6 lede, 14–14.5px/1.55–1.65 elsewhere | 12–13px dense app text | `L:49`, `L:255` |
| Vertical air | 52px hero top, 56px section bottoms, 56px footer top; 48px hero gap; 20–24px card padding | dense terminal rows | `L:45`, `L:180`, `L:217`, `L:248` |
| Eyebrows | mono 12px/500 `letter-spacing 0.08em` in accent | not used in-app | `L:47`, `S:43`, `C:43`, `B:43` |
| Rotation / overlap | decorative cards rotated ±6–11° with heavy shadows; overlapping panels | never | `L:59–63`, `L:181–188`, `L:218–222` |
| Dark mode | **none** — light only | full token system, all 10 pages | 0 `data-theme` hits vs `DESIGN_NOTES.md:105` |
| Buttons | radius 7–8, `8px 14px` / `11px 20px`, 13.5–15px/600 | app-standard controls | `L:37`, `L:51` |

**Invariant to preserve:** the marketing pages are *typographically loud and dense-data quiet*; the
app is the reverse. The one place the app's density appears on marketing pages is inside the dark
replica panels, and that contrast is the point (`DESIGN_NOTES.md:146`).

### 6.4 Data claims that could be factually wrong — **flagged**

> **⚠ The "Apr '25" seam claim appears six times and DECISIONS.md D-001 says it is false.**

| # | Claim as written | Location |
|---|---|---|
| 1 | "the Apr '25 data seam stays marked, never smoothed" | `L:202` |
| 2 | Panel eyebrow `THE APR '25 SEAM` | `L:235` |
| 3 | "Two data sources meet in April 2025. Charts mark the seam instead of smoothing it over." | `L:236` |
| 4 | "The April '25 data seam stays marked on every chart — never smoothed over." | `C:45` |
| 5 | SVG seam marker drawn at x=168 with the label `APR '25 SEAM` | `C:74–75` |
| 6 | "Two data sources meet in April 2025." | `C:113` |
| 7 | Footer chrome `honest floor Apr '25` | `S:92` |

**D-001** (`DECISIONS.md:22–33`): *"Per-sale and census history begin at each card's first crawler
visit (late Jul 2026), not Apr 2025 / Jan 2026… The seam is per-card and ragged, not a single shared
date."* Receipts: `PokemonInvestBatch/DATA_MODEL.md:404` (visits/fingerprints/parse_failures "begin at
first deployment (2026-07-28)"), `:397` (population history "begins at each card's first visit"),
`DESIGN_NOTES.md:41`, and `git log --reverse` (first commit 2026-07-27). Owner: *"That's completely
false. It just started this month."* `DATA_MODEL.md:380` independently titles its section **"Sales:
two epochs with a ragged seam."**

This is the worst possible place for the error. The seam claim is not incidental copy — it is the
**central trust proposition**, sitting under the heading "Trust is a feature." (`L:226`) and under the
eyebrow "WHERE THE NUMBERS COME FROM" (`L:224`). A marketing page that names the wrong seam date, in
the section arguing for its own honesty, is the one failure mode this brand cannot absorb. Note also
that a *single global date* is the wrong **shape**, not just the wrong value: D-001 says the seam is
per-card. Any fix must decide whether marketing shows a per-card seam (impossible in static copy), a
conservative global floor, or drops the date entirely and describes the mechanism. Cross-reference
`DECISIONS.md:312–316` (a deliberate disclosed floor of **2026-09-01**, chosen because it is later
than every card's first visit and therefore errs toward LOCKED) — that is the defensible number to
put in copy, with its reason attached, and `DECISIONS.md:322` already says the About Data page should
carry it.

**Other data claims that could be wrong, in descending risk:**

| Claim | Location | Assessment |
|---|---|---|
| `SALES 41,208`, `VOLUME $2.1M`, `MEDIAN SALE $84`, `VENUE ebay 82%…`, `TOP SALE $18,500`, `MOST ACTIVE 214 sales`, `TOP WINNER/LOSER`, `HOT SET`, `CHARACTER LEADER`, `BREADTH`, `MEDIAN ROC` — all on a **30-day** window | `L:316–325` | **High risk.** Every one is derived from the per-sale ledger, which per D-001 begins late Jul 2026. A 30-day window as of 2026-08-10 covers roughly two weeks of real data. The ticker is the first thing a visitor sees; it must not display a 30d window the data cannot fill. Options: shorten the window, label it honestly, or hold the ticker until 30 post-seam days exist. |
| `GRADING +4,120 slabs · gem 46%` (30d census delta) | `L:318` | **High risk.** Census/population history begins at each card's first visit (`DATA_MODEL.md:397`); a 30-day *delta* needs 30 post-seam days. Same fix as above. |
| "Filter thousands of cards by **churn**, z-score, grade premium, and rate of change" | `S:45` | **High risk.** `DISPLAY_VOCABULARY.md:111` gates churn as *"Post-seam only — cards with under 60 post-seam days are hidden from results"*; `:154` marks Churn 30d `POST-SEAM`. With the seam in late Jul 2026, churn is effectively unavailable at launch — yet it is the **first** filter named in the Screener hero, appears as a live column in the hero mock (`S:61`, churn ×2.4/×1.6/×1.9), and is one of the three seeded filter chips (`S:55`). The Landing repeats it: "Rank every printing by churn, z-score, and grade premium" (`L:196`) and shows a CHURN column (`L:126`). |
| "backtest it against **five years of history**" | `S:45` | **Defensible for monthly closes, and only those.** D-002 (`DECISIONS.md:37–40`): monthly price history is backfilled to ~Dec 2020 at each card's first visit — ~5.7 years as of 2026-08. Safe **if** backtests run on monthly closes. Not safe if any backtest input is sales- or census-derived (churn is exactly such an input, and it is a seeded filter in the same page's hero). |
| "**Five years** of graded-card price history" | `L:49` | Same basis; defensible for monthly closes (D-002). Worth pinning explicitly so nobody later reads it as five years of *sales*. |
| "Every price is built from recorded sales, not listings — deduplicated, with the ledger one click away." | `L:232` | **Medium risk, needs a query.** The clause "with the ledger one click away" is only true for the post-seam window; before it, a monthly close exists with no per-sale rows behind it (D-001 + D-002). Whether upstream monthly closes are themselves sales-derived is a `DATA_MODEL.md` question worth settling before this sentence ships. |
| "it re-runs as new sales land" | `S:112` | Forward-looking; fine. |
| "**thousands** of cards" | `S:45` | Understatement — `DECISIONS.md:320` cites 91k cards. Safe direction, but consider "tens of thousands". |
| "6 tiers · compare mode" | `C:102` | Consistent with `PriceTier.cs:10–18` (6 values), per `CLAUDE.md:92`. |
| "PSA 10 down to raw" / 19-tier strip elsewhere | `C:45`, `C:101` | Charts markets 6 price tiers; the Card page uses the 19-value grade vocabulary (`CLAUDE.md:93`). Not a contradiction, but the two vocabularies must not be conflated in implementation. |
| "12 filters · honest floors" | `S:107` | **Contradicts the app.** `HANDOFF.md:72` says Screener has **27 filter metrics**. Marketing understates by more than half. Direction is safe; the number is still wrong. |
| "composites G1–G4" | `C:108` | Not verified in this pass. Confirm the composite-group naming against `Cardstock Charts.dc.html` before shipping. |
| "+8.7 pp · 12M" as a **feature-card kicker** | `B:111` | This is seeded portfolio data promoted into marketing copy outside a device frame. Reads as a product claim. Recommend genericising. |

### 6.5 Structural invariants for implementation

1. **The ticker track must render the item list exactly twice.** The `-50%` keyframe depends on it
   (`L:20`, `L:326`).
2. **The deck's re-entry guard must survive porting.** Without `if (this.state.exiting) return`
   (`L:287`), a fast click plus the 4.2 s interval corrupts `order`.
3. **Timers must be cleared on teardown** (`L:285`) — in Blazor, `IDisposable`/`IAsyncDisposable`.
4. **Deck position 3 is opacity 0** (`L:304`). Only three panels are visible; the fourth is staged.
5. **`INDEX` and `NEW 12M HIGHS` always carry the literal `30d` suffix** (`L:317`, `L:320`;
   `DESIGN_NOTES.md:28`) — the monthly-data honesty rule.
6. **The footer is byte-identical on all four pages** — build it once as a shared component.
7. **The pillar nav never links to itself**, and no marketing page uses `href="#"` (0 hits).
8. **Two different `LOW CONFIDENCE` badge treatments exist** (`L:240` chip vs `S:85` row badge).
   Confirm against `DISPLAY_VOCABULARY.md` before standardising.
9. **The `#data` anchor and the `sc-if` are coupled** — hiding the methodology section orphans nav
   link #2 (`L:35`).

### 6.6 Images and screenshots these pages depend on

**No `<img>`, no `background-image`, no `url()` in any of the four files** (grep-verified). The
product visuals are *not* screenshots — they are hand-built HTML/SVG replicas of the app, a deliberate
choice recorded at `DESIGN_NOTES.md:146`. That means **no screenshot pipeline is needed**, and it also
means these replicas will drift from the real app unless someone owns them.

**Binary assets referenced:**

| Asset | Referenced by | On disk |
|---|---|---|
| `brand/favicon.svg` | `L:14`, `S:11`, `C:11`, `B:11` | ✔ 449 B |
| `brand/og-image.png` | **nothing** | ✔ 64 KB — exists, unused (§1.3) |
| `brand/apple-touch-icon.png`, `favicon-16.png`, `favicon-32.png`, `logo-mark.svg`, `logo-mark-dark.svg`, `brand-tokens.css` | **nothing** | ✔ present, unreferenced |

The logo mark is **inline SVG in every page**, not `logo-mark.svg`: light nav variant at `L:29`,
`S:27`, `C:27`, `B:27` (`#1C1C1E` strokes, `#0E8A7B` checkmark); dark footer variant at `L:252`,
`S:130`, `C:125`, `B:128` (`#ECECE6` strokes, `#3FBFAD` checkmark, `#0F0F11` fill).

**Content images — eight `image-slot` elements, Landing only** (grep-verified: 8 on the Landing, 0 on
each pillar page):

| id | Section | Size | Rotation | Radius | Line |
|---|---|---|---|---|---|
| `hero-card-right` | Hero | 330×462 | 0° | 16 | `L:57` |
| `hero-card-mid` | Hero | 150×210 | +11° | 11 | `L:60` |
| `hero-card-left` | Hero | 120×168 | −8° | 10 | `L:63` |
| `features-card` | Features | 130×182 | +10° | 10 | `L:182` |
| `features-card-2` | Features | 120×168 | −9° | 10 | `L:185` |
| `features-card-3` | Features | 110×154 | −6° | 9 | `L:188` |
| `data-card` | Methodology | 130×182 | −7° | 10 | `L:219` |
| `data-card-2` | Methodology | 120×168 | −11° | 10 | `L:222` |

Three hero, three features, two methodology. All are `placeholder="drop card image"`
and require the `image-slot.js` runtime, loaded **only on the Landing** (`L:22`). **These need real
Pokémon card artwork sourced and licence-cleared before launch**, and the footer disclaimer
(`L:266`) is the only rights statement present. The pillar pages need no imagery at all.

External runtime dependencies on every page: Google Fonts (Inter 400–800, JetBrains Mono 400/500/700)
via `fonts.googleapis.com` + `fonts.gstatic.com` preconnects (`L:11–13`). Blazor hosting should
self-host these.

---

## Interaction specs from the package handoff

Harvested 2026-08-10 from `CardStock Mockup/uploads/Brand package creation/README.md` before retirement (D-054). These are the package author's own timings — not reconstructable from the prototype without reverse-engineering the CSS.

**Ticker.** Infinite CSS marquee, **44s linear**. The item array is duplicated so the `-50%` translate loops seamlessly. (Matches what §3 found in the HTML — `@keyframes cdstkTicker` at `Landing:20`, applied `:334`, `items.concat(items)` at `:326`.)

**Shuffle deck.** 4.2s auto-advance, 620ms deal animation. `mouseenter` pauses · `mouseleave` resumes · `click` deals immediately. **Guard against re-entry while a card is exiting. Clear timers on unmount.**

**Both animations must respect `prefers-reduced-motion`.** The handoff says so explicitly — *"both animations should have an off path (they are toggleable props in the prototype)."* §8 confirms the guard is absent from all four pages; the author intended it and it was never wired. This is a build requirement, not a nice-to-have, under D-011.

**Hover.** Nav and footer links shift to the primary; primary buttons darken to `#3A4FB8` light / lighten to `#AAB6F6` dark; text links underline.

**Focus.** 3px indigo ring, **never removed**.

### Responsive — the only guidance that exists anywhere

> "No responsive breakpoints are specified in the prototype (desktop-first at 1080px). At narrower widths, **collapse the hero to one column, the 3-col grids to one, and hide or reduce the decorative card slots**."

Class E flags zero breakpoints across every prototype. This is the sole piece of direction on record, and it covers marketing only — the app screens have none.

### State

Landing only: `order: ['A','B','C','D']` (deck stacking) · `exiting: string | null` (card mid-deal) · `paused: boolean`. Props `tickerMotion`, `cardShuffle`, `showMethodology` are author-time toggles, invisible to visitors. **No data fetching — every figure is demo content.**

### Assets

Screenshots under `assets/screens/` are **context only** — the handoff records they "rasterized poorly" and the deck cards are real markup instead. Brand assets are "generated for this project; free to ship." Card imagery is **not** included and the slots are empty placeholders — see D-010.

`image-slot.js` and `support.js` are prototype-runtime helpers, explicitly **not for production**.

---

## Corrected copy — build this

Written 2026-08-10 (D-061) to resolve the seam claims flagged in §6.4. These are **public marketing pages under D-011**, so a false data claim here is the most exposed instance of the error in the product.

### The seam claims — replace all six

| Where | Prototype says | Build instead |
|---|---|---|
| `Landing:202` | "the Apr '25 data seam stays marked, never smoothed" | "**where our sales record starts for a card, we mark it — we never smooth across it**" |
| `Landing:235`, `:236` | Apr '25 seam, methodology chip | Same substitution — describe the boundary, never date it |
| `Charts Landing:45`, `:74–75`, `:113` | Apr '25 seam | Same |
| `Screener Landing:92` | footer `honest floor Apr '25` | **`honest floor · per screen`** — the floor is the first date every filter in that screen could be computed, which differs by screen |

**The principle, and why no date replaces the date:** the seam is per-card and ragged (D-001) — each card's sales history begins at its own first visit. There is no single date that could be substituted, so the copy must describe the *behaviour* instead. That is also the stronger marketing claim: "we mark it wherever it falls" says more about the product's rigour than any date would.

**Do not substitute "Jul '26."** It is closer to true but still asserts a shared date the data does not have, and it ignores the 2026-09-01 floor (D-033).

### Other corrections

| Where | Prototype says | Truth | Receipt |
|---|---|---|---|
| `Screener Landing:107` | "12 filters" | **28** filter metrics | Direct count, `Cardstock Screener.dc.html:481–501`, `:534–563` |
| All four pages | No `prefers-reduced-motion` | The ticker animates unconditionally. **Add the guard** — motion is pure CSS (`@keyframes cdstkTicker`, `Landing:20`) over a duplicated list, so pausing it is trivial | Brand package README `:115`; six app pages already do this |
| All four pages | Light-only, 0 `data-theme` | Decide: either support dark here or amend `DESIGN_NOTES.md:105`'s "app-wide, all 10 pages" | — |

### Routing

Per **D-058**, these pages move to a `/product` prefix — `/product`, `/product/screener`, `/product/charts`, `/product/binder`. §1's identity table is superseded. A logged-out visitor at `/` redirects here.

### Demo mode

Demo mode has **0 occurrences** across all four pages, but all 11 CTAs land on `Cardstock Account.dc.html:56`, which still renders "Browse the demo →". Demo mode was removed 2026-08-10 (`HANDOFF.md` §4) and `DESIGN_NOTES.md:141` omits the Account page from the removal list. **Strip that remnant when building the Account page.**

---

## 7. Open questions

1. **Route ownership for the three pillar pages.** `HANDOFF.md:84` says marketing `/screener` etc.;
   `HANDOFF.md:72–74` gives the same paths to the app. Proposals: `/product/screener`, or
   auth-conditional resolution matching the `/` pattern (§1.1). Needs an owner ruling.
2. **Is Account's `Browse the demo →` (`Cardstock Account.dc.html:56`) still wanted?** It is the
   terminus of all 11 marketing CTAs and it survives a ruling that says demo mode was removed
   (`HANDOFF.md:100`). `DESIGN_NOTES.md:141` lists six pages it was deleted from and Account is not
   one, implying deliberate retention. Settle before building the funnel.
3. **Fix the Apr '25 seam copy — how?** Per-card seam (unrenderable in static copy), the disclosed
   2026-09-01 floor with its reason (`DECISIONS.md:312–322`), or mechanism-only copy with no date.
   Affects `L:202`, `L:235–236`, `C:45`, `C:74–75`, `C:113`, `S:92`.
4. **No responsive design exists.** Zero `@media` queries in all four files; the hero grids are fixed
   two-column and the feature strips fixed three-column. `<meta name="viewport">` is present on all
   four (`L:5`, `S:5`, `C:5`, `B:5`) but nothing responds. Breakpoints, and the ticker's behaviour on
   narrow screens, need designing.
5. **Reduced-motion contract** (§4.3) — confirm the recommended behaviour, and note that the app
   pages' `animation-duration` one-liner is insufficient for the transition-driven deck.
6. **Should feature-card kickers become links?** Currently the only Landing → pillar path is the
   footer (§5.5).
7. **Is the ticker live data or a fixed marketing snapshot?** If live, §6.4's window problem is
   blocking. If a snapshot, it needs an as-of stamp — but `HANDOFF.md:99` records `AsOfStamp` was
   removed app-wide, so the pattern would have to be reintroduced for marketing only.
8. **`press / to search` on the Landing** (`L:53`) — implement a search entry point or remove the
   caption.
9. **SEO/social metadata is entirely absent** (§1.3) — titles, descriptions, canonicals, OG/Twitter
   tags, `og-image.png` wiring, `lang` attribute, structured data.
10. **Marketing pages are light-only.** Confirm this is deliberate given the app's app-wide dark
    rollout (`DESIGN_NOTES.md:105`); a visitor arriving from a dark-themed app will see a light page.
11. **"12 filters" vs the app's 27** (`S:107` vs `HANDOFF.md:72`) — which number ships?
12. **Who owns the four hand-built app replicas** (`S:52–95`, `C:52–90`, `B:52–93`, `L:66–172`) as
    the real app evolves? They are marketing assets that assert what the product looks like.
13. **Card artwork sourcing and licensing** for the eight `image-slot` positions (§6.6).

---

## 8. Contradictions found

Every row below was resolved by opening the HTML. **The HTML wins in all cases.**

| # | Claim | Source doc:line | What the HTML actually does |
|---|---|---|---|
| 1 | "the Apr '25 data seam"; "Two data sources meet in April 2025"; "honest floor Apr '25" | `Cardstock Landing.dc.html:202`, `:235`, `:236`; `Cardstock Charts Landing.dc.html:45`, `:74–75`, `:113`; `Cardstock Screener Landing.dc.html:92` | **The HTML is the thing that is wrong here, and `DECISIONS.md` overrides all tiers.** D-001 (`DECISIONS.md:22–33`): per-sale and census history begin at each card's first crawler visit, **late Jul 2026**, per-card and ragged — not April 2025. Six prototype locations carry the false date, including the SVG seam marker at `Charts Landing:74–75`. **The single highest-priority copy fix on the marketing site.** |
| 2 | "Demo mode — removed 2026-08-10; the marketing pages carry that story now" | `CardStock Mockup/HANDOFF.md:100` | True **for the four marketing pages** (0 occurrences of "demo", grep-verified). But all 11 marketing CTAs land on `Cardstock Account.dc.html`, which still renders `Browse the demo →` (`:56`) navigating into the app (`:144`). `DESIGN_NOTES.md:141` lists the six pages it was removed from and Account is not one of them. The funnel still ends in a demo. |
| 3 | Marketing shell has "hero split (mono eyebrow · **48px/800** headline · 17px body · indigo CTA)" | `CardStock Mockup/DESIGN_NOTES.md:145` | Correct for the three pillar pages (`S:44`, `C:44`, `B:44`) but **the Landing h1 is 52px** (`L:48`). The Landing is a deliberate step up, not part of the shared spec. |
| 4 | Shared shell has "sticky blurred nav (**mark+wordmark → overview**…)" | `CardStock Mockup/DESIGN_NOTES.md:145` | True on the three pillar pages (`S:26`, `C:26`, `B:26`). **False on the Landing**, where the mark + wordmark + `CDSTK` chip is a plain `<div>` with no link (`L:28–32`) — correctly, since the Landing *is* the overview. |
| 5 | Shared shell described uniformly across all four pages | `CardStock Mockup/DESIGN_NOTES.md:145` | The Landing carries three elements the pillars do not: the dark ticker bar (`L:42`), the four-panel shuffle deck (`L:65–174`), and the `#data` methodology section (`L:215–245`). It is also the only page with `data-props`, a script block, and `image-slot.js`. |
| 6 | "only intentional `href="#"` left is **each page's current-page nav tab**" | `CardStock Mockup/DESIGN_NOTES.md:150` | **No `href="#"` exists in any of the four marketing pages** (grep: 0 hits each), and no pillar nav links to itself — there is no current-page tab in the marketing nav at all. The statement is about other pages; it does not describe these. |
| 7 | Landing route is "marketing `/`" while Home is "`/`" | `CardStock Mockup/HANDOFF.md:83` vs `:71` | Not a defect but an under-specified collision: the same URL resolved by auth state. Nothing in the HTML states this; it must be an explicit routing decision (§1.1). |
| 8 | Pillar pages at "marketing `/screener` etc." | `CardStock Mockup/HANDOFF.md:84` vs `:72–74` | Direct collision with the app's `/screener`, `/charts`, `/binder`. The HTML links by filename and cannot settle it (§7.1). |
| 9 | Screener has "**27 filter metrics**" | `CardStock Mockup/HANDOFF.md:72` | The Screener marketing page advertises "**12 filters** · honest floors" (`S:107`). Marketing understates the app by more than half. |
| 10 | "Respect `prefers-reduced-motion` — both animations should have an off path" | `CardStock Mockup/uploads/Brand package creation/README.md:115` | **Not implemented.** No `prefers-reduced-motion` query in any of the four files, while six app pages have one (`Home:25`, `Browse:23`, `Binder:25`, `Card:23`, `Set:23`, `Character:23`). The only off-paths are the author-time props `tickerMotion` and `cardShuffle` (`L:274–275`), invisible to visitors. Known prototype gap (§4.3). |
| 11 | "landing ticker swapped to the app's 30d market-stat items" | `CardStock Mockup/DESIGN_NOTES.md:147` | **Confirmed exactly** — all 16 items at `L:316–325` are byte-identical to `Cardstock Home.dc.html:449–457`. Recorded as verified, not as a contradiction. Two differences remain: the Landing has **no window selector** (Home has 7d/30d/90d) and **no hover-pause** (`Home:66` has `animation-play-state: paused`). |
| 12 | Dark mode rolled out "app-wide (2026-08-09): **all 10 pages**" | `CardStock Mockup/DESIGN_NOTES.md:105` | Consistent, but worth stating: the four marketing pages are **not** among the 10 and have **zero** `data-theme` occurrences. Marketing is light-only by construction (§7.10). |
