# Marketing pages — authoritative screen spec

Extracted from the Tier 1 prototypes, 2026-08-10. Every statement below was read out of the HTML.
Where a Tier 2/3 document disagreed, the HTML won and the disagreement is recorded in §8.

**Citation shorthand** — all files live in `CardStock Mockup/`:

| Prefix | File | Lines |
|---|---|---|
| `L:` | `Cardstock Landing.dc.html` | 346 |
| `SL:` | `Cardstock Screener Landing.dc.html` | 152 |
| `CL:` | `Cardstock Charts Landing.dc.html` | 147 |
| `BL:` | `Cardstock Binder Landing.dc.html` | 150 |

Other repo files are cited by full relative path.

---

## Flags — read before implementing

1. **Demo mode is gone from the marketing pages, but survives one hop downstream.** All four pages contain zero occurrences of "demo" (verified by grep, 0 hits each). No "View live demo" CTA exists. **However**, every conversion CTA on all four pages targets `Cardstock Account.dc.html`, and that page still renders a `Browse the demo →` button that navigates into the app (`Cardstock Account.dc.html:56`, handler `:144`). The 2026-08-10 removal (`DESIGN_NOTES.md:140–141`) listed Home/Browse/Binder/Card/Set/Character — **Account was not in that list.** So the marketing funnel still terminates in a live demo affordance. Owner decision needed: was Account an oversight, or is the sign-in demo button deliberately retained?
2. **The Apr '25 seam is on the marketing site in five places and D-001 says it cannot exist.** See §6.3.
3. **The Landing ticker's 16 stats are all period-over-period deltas that are not computable at launch.** See §6.3.
4. **No `prefers-reduced-motion` gating on the Landing**, which is the only animated marketing page. See §4.2.

---

## 1. Identity

| Page | File | Route | Purpose |
|---|---|---|---|
| Landing (overview) | `Cardstock Landing.dc.html` | **logged-out `/`** | The front door. Product thesis, three-pillar toolkit, methodology, footer. The only marketing page with motion or dynamic values. |
| Screener Landing | `Cardstock Screener Landing.dc.html` | marketing product page (path unresolved — see §7) | Single-pillar pitch for the Screener. |
| Charts Landing | `Cardstock Charts Landing.dc.html` | marketing product page (path unresolved) | Single-pillar pitch for Charts. |
| Binder Landing | `Cardstock Binder Landing.dc.html` | marketing product page (path unresolved) | Single-pillar pitch for Binder. |

**The `/` collision is real and must be handled in routing.** `HANDOFF.md:83` assigns `Cardstock Landing.dc.html` to "marketing `/`"; `HANDOFF.md:71` assigns `Cardstock Home.dc.html` to `/`. Both are correct: **`/` serves Landing when logged out and Home when logged in.** These are two different components behind one path, not one component with a variant — they share no chrome (Landing has no app nav, no search, no account circle, no theme toggle).

The three product pages are a tier below: they are reachable from the Landing footer and from each other's navs, and each links back to the Landing.

---

## 2. Layout

### 2.1 Shared shell (all four)

**Nav** — sticky, `top:0`, `z-index:50`, `background: rgba(241,241,236,0.92)`, `backdrop-filter: blur(8px)`, `1px solid #E4E4E0` bottom border. Inner rail `max-width:1080px`, `padding:14px 40px`, flex space-between, gap 24. (`L:26–40`, `SL:24–38`, `CL:24–38`, `BL:24–38`)

Left: 24px logo SVG + "Cardstock" wordmark 18px/700, letter-spacing −0.03em.
- On the Landing the lockup is a plain `<div>`, **not a link** (`L:28`) — correct, it is home.
- On the three product pages the lockup is an `<a>` to the Landing (`SL:26`, `CL:26`, `BL:26`).
- **Only the Landing carries the `CDSTK` mono chip** beside the wordmark (`L:31`).

Right: text links 13.5px/500 `#55555A`, then a filled `Sign up →` pill (13.5px/600, white on `#4A63D0`, radius 7, padding 8×14).

**Footer** — byte-identical across all four. `#0F0F11`, `1px solid #232427` top, rail `max-width:1080px`, `padding:56px 40px 28px`, grid `1.4fr 1fr` gap 32. (`L:247–270`, `SL:125–148`, `CL:120–143`, `BL:123–146`)
- Column 1: 26px dark logo mark + 19px/700 wordmark, then the brand blurb 14.5px/1.65 `#9A9A96`, `max-width:340px` — *"Precise numbers over adjectives. No hype, no exclamation marks…"* (`L:255`, `SL:133`, `CL:128`, `BL:131`).
- Column 2: mono 11px `PRODUCT` label, then four links — Screener / Charts / Binder / About the data (`L:259–262`, `SL:137–140`, `CL:132–135`, `BL:135–138`).
- Bottom bar: `1px #232427` top, `margin-top:40px`, `padding-top:18px`, flex space-between wrap. Left = the fan-made disclaimer. Right = Privacy · Terms · © 2026 Cardstock. (`L:265–267`, `SL:143–145`, `CL:138–140`, `BL:141–143`)

### 2.2 Landing (`L`)

Section order: Nav → **ticker bar** → Hero → Features → Methodology (conditional) → Footer.

**Ticker bar** (`L:42`) — `background:#131316`, `padding:10px 0`, `overflow:hidden`, full-bleed (no rail). Sole content is the `{{ tickerTrack }}` binding. Sits between nav and hero.

**Hero** (`L:44–177`) — `overflow-x: clip` on the section; rail `padding:52px 40px 36px`, grid `1.05fr 0.95fr`, gap 48, `align-items:center`.
- Left column (`L:46–54`), flex column gap 20: mono eyebrow `POKÉMON TCG AFTERMARKET DATA` 12px/500 tracking 0.08em `#4A63D0` (`L:47`) · `<h1>` 52px/800 tracking −0.03em line-height 1.06 `max-width:520px` `text-wrap:pretty` — **"The trading terminal for Pokémon cards."** (`L:48`) · body 17px/1.6 `#55555A` `max-width:480px` (`L:49`) · a **single** CTA `Sign up →` 15px/600 (`L:51`) · mono 12px caption *"press `/` to search · fan-made"* with the `/` in a bordered radius-4 chip (`L:53`).
- Right column (`L:55–175`), `position:relative; min-height:440px`:
  - Three absolutely-positioned card-art image slots — `hero-card-right` 330×462 at `top:-24 right:-48` radius 16 (`L:56–58`); `hero-card-mid` 150×210 rotated 11° at `top:158 left:-24` radius 11 (`L:59–61`); `hero-card-left` 120×168 rotated −8° at `left:4 bottom:0` radius 10 (`L:62–64`).
  - A **four-panel shuffle deck** (slots A–D), each 360px wide / `max-width:92%`, absolutely positioned `top:32px right:16px`, `cursor:pointer`, `transform-origin:50% 80%` (`L:293–294`). Panel bodies are `#131316` cards with `1px #2A2B2E` border, radius 12, `box-shadow: 0 24px 48px rgba(28,28,30,0.25)`:
    - **A — Watchlist** (`L:65–93`): header `WATCHLIST · 30D` / `LIVE`; four rows of name + set·grade / price + delta separated by `1px #232427`; a full-width sparkline SVG; footer *"press `/` to search"* / *"12M · normalized"*.
    - **B — Binder** (`L:94–114`): title `Binder` / `Performance →`; three stat blocks TOTAL VALUE / UNREALIZED / VS MARKET; a summary row (positions · sets, cost, 1M delta); caption strip `BINDER — value & P&L` / `/home`.
    - **C — Screener** (`L:115–143`): title `"Quiet Accumulation"` / `12 matches`; a 4-column grid CARD / PRICE / ROC 3M / CHURN with three rows; caption strip `SCREENER — "Quiet Accumulation"` / `/screener`.
    - **D — Charts** (`L:144–173`): header card name + PSA 10 + price/delta; a 336×130 SVG with gridlines, three price labels, a Bollinger-style envelope (two dashed bounds + fill + dashed mid) and a solid `#8C9BF2` price polyline with endpoint dot; a stat row ROC 12M / RS / BB; caption strip `CHARTS — price history · PSA 10` / `/charts`.
  - Deck caption, absolute `right:24px bottom:-30px`, mono 11px `#8A8A86`: `{{ frontLabel }} · click to shuffle` (`L:174`).

**Features** — `id="features"`, `border-top:1px solid #E4E4E0`, rail `padding:16px 40px 56px`, `position:relative` (`L:179–213`).
- Three more card-art slots layered behind the grid: `features-card` 130×182 rot 10° (`L:181–183`), `features-card-2` 120×168 rot −9° (`L:184–186`), `features-card-3` 110×154 rot −6° (`L:187–189`).
- Mono eyebrow `THE TOOLKIT` (`L:190`) · `<h2>` 30px/700 **"Three ways in."** (`L:191`).
- 3-column grid gap 20, `margin-top:28px` (`L:192`). Each card: `#FFFFFF`, `1px #E4E4E0`, radius 10, padding 24, flex column gap 12 — a 64×40 SVG glyph, `<h3>` 17px/650, body 14px/1.55 `#55555A`, and a mono 11.5px indigo kicker.
  - Screener (`L:193–198`) — kicker `saved screens · backtest →`
  - Charts (`L:199–204`) — kicker `open in charts →`
  - Binder (`L:205–210`) — kicker `+ binder`
  - The kickers are **plain text, not links** (see §5.1).

**Methodology** — wrapped in `<sc-if value="{{ showMethodology }}">` (`L:215`), `id="data"`, `background:#131316`, rail `padding:52px 40px 56px` (`L:216–245`).
- Two card-art slots: `data-card` 130×182 rot −7° (`L:218–220`), `data-card-2` 120×168 rot −11° (`L:221–223`).
- Mono eyebrow `WHERE THE NUMBERS COME FROM` in `#8C9BF2` (`L:224`) · a baseline-aligned row holding `<h2>` **"Trust is a feature."** in `#F2F2EE` and the link `About the data →` (`L:226–227`).
- 3-column grid of `#1B1C1F` cards, `1px #2A2B2E`, radius 10, padding 22 (`L:229–242`): `PER-SALE LEDGERS` (`L:230–233`), `THE APR '25 SEAM` (`L:234–237`), `SUFFICIENCY RULES` (`L:238–241`). The third embeds an inline `LOW CONFIDENCE` chip — mono 10.5px `#C9A84C` on `rgba(201,168,76,0.08)` with a `rgba(201,168,76,0.4)` border, radius 5 (`L:240`).

### 2.3 Product landings (`SL` / `CL` / `BL`)

All three share one skeleton; only the eyebrow, headline, body, hero mock and three feature cards differ. **No ticker. No image slots. No methodology section. No conditional sections.**

**Hero** — rail `padding:52px 40px 56px`, grid `1fr 1fr` gap 48 (`SL:41`, `CL:41`, `BL:41`).
- Left: mono eyebrow `PRODUCT · SCREENER|CHARTS|BINDER` · `<h1>` **48px**/800 `max-width:480px` · body 17px/1.6 `max-width:460px` · **two** CTAs in a flex row gap 18 — `Sign up →` (filled) and `All of Cardstock →` (14px/600 text link).
  - `SL:43–48` "Every printing, ranked your way."
  - `CL:43–48` "Price history you can trust."
  - `BL:43–48` "Your binder is a portfolio."
- Right: a single 500px-wide (`max-width:100%`) dark product mock, `#131316`, `1px #2A2B2E`, radius 12, `overflow:hidden`, 13px `#F2F2EE`. Built entirely from markup and inline SVG — **no raster screenshots**.
  - **Screener mock** (`SL:52–95`): filter-chip header — a `"QUIET ACCUMULATION"` name chip plus three removable filter chips (`churn ≥ ×1.5`, `ROC 3M > 0`, `z-score < 1`, each with a decorative `✕`) and a right-aligned `12 matches` (`SL:53–59`); a 5-column header row Card/Price/ROC 3M/Churn/Z-score (`SL:60–62`); four result rows, row 2 zebra-striped `#1B1C1F` (`SL:63–90`), row 4 carrying a `LOW CONFIDENCE` outline chip inline with the card name (`SL:85`); a `#0F0F11` footer bar reading `honest floor Apr '25` with a `Backtest this screen →` pill (`SL:91–94`).
  - **Charts mock** (`CL:52–90`): header with card name, `Evolving Skies · 215/203`, and `$1,309 ▲ +6.1%` (`CL:53–59`); a tier chip row — PSA 10 active (`#8C9BF2` dot, indigo border), Grade 9 (`#E0A93C`), Raw (`#A5A5A0`), plus a right-aligned `EMA 12 · BB 20·2 · RSI` (`CL:60–65`); a 476×170 SVG (`CL:66–85`) with three gridlines, y-labels $1,350/$1,100/$850, a **vertical gold dashed line at x=168 labelled `APR '25 SEAM`** (`CL:74–75`), a Bollinger envelope, a `#8C9BF2` PSA 10 polyline, an `#E0A93C` Grade 9 polyline, a hollow endpoint marker (`CL:81`), and x-labels 2024/2025/2026; a `#0F0F11` footer reading `monthly closes · current month still revising ○` and `ROC 12M +18.2% · RS 94th` (`CL:86–89`).
  - **Binder mock** (`BL:52–93`): a three-cell stat strip Total value / Unrealized / Vs market divided by `1px #232427` (`BL:53–57`); a 5-column header Position/Qty/Cost/Value/P&L (`BL:58–60`); four position rows (`BL:61–88`); a `#0F0F11` footer reading `14 positions · 6 sets · cost $15,324` and `▲ +$412 1M` (`BL:89–92`).

**Features** — `border-top:1px solid #E4E4E0`, rail `padding:36px 40px 56px`, 3-column grid gap 20 (`SL:100–123`, `CL:95–118`, `BL:98–121`). Cards are structurally identical to the Landing's. **Unlike the Landing there is no eyebrow and no `<h2>` above the grid.**
- Screener: "Filters that mean something" (`SL:105`) / "Save a thesis" (`SL:111`) / "Backtest honestly" (`SL:117`).
- Charts: "Every grade tier" (`CL:100`) / "Indicators for thin markets" (`CL:106`) / "The seam stays marked" (`CL:112`).
- Binder: "Cost basis & P&L" (`BL:103`) / "Vs the market" (`BL:109`) / "A real ledger" (`BL:115`).

### 2.4 Images and assets the pages depend on

| Asset | Kind | Used by | Where |
|---|---|---|---|
| `hero-card-right`, `hero-card-mid`, `hero-card-left` | `<image-slot>` card art | Landing only | `L:57`, `L:60`, `L:63` |
| `features-card`, `features-card-2`, `features-card-3` | `<image-slot>` card art | Landing only | `L:182`, `L:185`, `L:188` |
| `data-card`, `data-card-2` | `<image-slot>` card art | Landing only | `L:219`, `L:222` |
| `brand/favicon.svg` | favicon | all four | `L:14`, `SL:11`, `CL:11`, `BL:11` |
| Google Fonts — Inter 400/500/600/700/800 + JetBrains Mono 400/500/700 | webfont CSS, external | all four | `L:11–13`, `SL:12–14`, `CL:12–14`, `BL:12–14` |
| `./support.js` | DC runtime | all four | `L:6`, `SL:6`, `CL:6`, `BL:6` |
| `./image-slot.js` | image-slot component | **Landing only** | `L:22` |

**There are exactly eight bitmaps on the entire marketing site, all on the Landing, all Pokémon card art, all decorative.** The backing images are stored as base64 **WebP** data URIs in `CardStock Mockup/.image-slots.state.json`; its eight top-level keys match the eight slot ids exactly (verified by parsing the file — payloads range 18.9 KB to 127.5 KB of base64, each entry `{u, s:"1", x:"0", y:"0"}`). An unfilled slot renders its `placeholder="drop card image"` state.

Everything else — logo marks, feature glyphs, sparklines, the Charts price chart, the Bollinger envelopes — is **inline SVG**. There are no product screenshots anywhere.

**Available but unreferenced:** `brand/og-image.png` (1200×630 social card), `brand/apple-touch-icon.png`, `brand/favicon-16.png`, `brand/favicon-32.png`, `brand/logo-mark.svg`, `brand/logo-mark-dark.svg`, `brand/brand-tokens.css`. **None of the four pages carry any `<meta name="description">`, `og:*`, or `twitter:*` tag** (grep: zero hits outside the Brand System page). For pages whose whole job is to be linked and shared, that is a gap the prototypes do not cover.

---

## 3. Data contract

### 3.1 Which pages have one

**Only the Landing.** `SL`, `CL` and `BL` contain no `<script type="text/x-dc">`, no `data-props`, no `{{ }}` bindings, and no `sc-if`/`sc-for` (verified by grep: `sc-for` 0 hits in all four; `sc-if` 2 hits on the Landing only, both the open/close of one element). Every figure on those three pages is a literal in the markup. An implementation must decide, per figure, whether it stays hard-coded marketing art or binds to live data — the prototype takes no position.

### 3.2 Landing props and bindings

Props (`L:273–277`):

| Prop | Editor | Default | Section | Effect |
|---|---|---|---|---|
| `tickerMotion` | boolean | `true` | Motion | `true` → ticker animates; `false` → `animation:'none'` (`L:310`, `L:334`) |
| `cardShuffle` | boolean | `true` | Motion | gates the 4.2 s auto-advance only (`L:282`) |
| `showMethodology` | boolean | `true` | Sections | mounts/unmounts the whole `#data` section (`L:215`, `L:335`) |

Bindings (`L:333–341`):

| Binding | Type | Consumed at |
|---|---|---|
| `tickerTrack` | React element (the whole animated track) | `L:42` |
| `slotA` … `slotD` | style objects from `slotStyle()` | `L:65`, `L:94`, `L:115`, `L:144` |
| `frontLabel` | string from `{A:'watchlist', B:'/home', C:'/screener', D:'/charts'}` (`L:311`) | `L:174` |
| `showMethodology` | boolean | `L:215` |
| `shuffleNow` / `pauseShuffle` / `resumeShuffle` | handlers | `L:65`, `L:94`, `L:115`, `L:144` |

### 3.3 The ticker — complete enumeration

Defined at `L:315–325`. Sixteen items, each `{l: label, n?: name, v: value, c: colour, x?: suffix}`. Colours from `L:314`: `G = #46C08A` (positive), `L = #D0655E` (negative), `K = #F2F2EE` (neutral ink).

| # | Label (`l`) | Name (`n`) | Value (`v`) | Colour | Suffix (`x`) |
|---|---|---|---|---|---|
| 1 | `SALES` | — | `41,208 ▲ +9%` | G | — |
| 2 | `VOLUME` | — | `$2.1M ▲ +11%` | G | — |
| 3 | `BREADTH` | — | `58% advancing` | G | — |
| 4 | `INDEX` | — | `▲ +2.4%` | G | `30d` |
| 5 | `VINTAGE` | — | `58% of $ vol` | K | — |
| 6 | `GRADING` | — | `+4,120 slabs · gem 46%` | K | — |
| 7 | `MEDIAN SALE` | — | `$84 ▼ −3%` | L | — |
| 8 | `VENUE` | — | `ebay 82% · auction 11% · tcgp 7%` | K | — |
| 9 | `NEW 12M HIGHS` | — | `▲ 214` | G | `30d` |
| 10 | `MEDIAN ROC` | — | `▲ +1.4%` | G | — |
| 11 | `TOP WINNER` | `Espeon Gold Star` | `▲ +14%` | G | — |
| 12 | `TOP LOSER` | `Blastoise Holo PSA 8` | `▼ −6.1%` | L | — |
| 13 | `TOP SALE` | `Lugia 1st Ed PSA 10` | `$18,500` | K | `goldin` |
| 14 | `MOST ACTIVE` | `Charizard ex SAR` | `214 sales` | K | — |
| 15 | `HOT SET` | `Evolving Skies` | `▲ +4.8%` | G | — |
| 16 | `CHARACTER LEADER` | `Giratina` | `▲ +6.2%` | G | — |

Span rendering (`L:326–332`): `inline-flex`, `gap:8px`, `align-items:baseline`, `margin-right:40px`, `white-space:nowrap`, JetBrains Mono 12px. Child order and colour: label `#B9B9B4` → name `#D6D6D0` (omitted when absent) → value `it.c` at weight 500 → suffix `#B9B9B4` (omitted when absent).

**Window.** The bar carries **no window control and no global window label.** The source comment at `L:313` states the intent: *"Same market-stat items as the app's Home ticker (30d window), on the landing's dark bar."* **Verified true** — the 16 items are byte-identical to the `'30d'` array in `Cardstock Home.dc.html:448–458`. Home offers three windows (`'7d'` `L:437–447`, `'30d'` `:448–458`, `'90d'` `:459–469`, defaulting to 30d at `:471`) behind a switcher; the Landing is hard-pinned to the 30d set with no switcher and no visible "30D" label. The only windows stated on screen are the per-item `x:'30d'` suffixes on `INDEX` and `NEW 12M HIGHS`.

**Consequence for implementation:** the Landing ticker and the Home ticker must be *one* data source. They are currently two literal arrays in two files, and any drift becomes a publicly visible disagreement between the logged-out and logged-in `/`.

**Motion mechanism — CSS animation, not data-driven.** There is no per-tick state, no timer, and no index. The full item list is rendered once, doubled (`items.concat(items)` at `L:326` → 32 spans), laid out in a `display:flex; width:max-content` track, and translated by CSS:

- `@keyframes cdstkTicker { from { transform: translateX(0); } to { transform: translateX(-50%); } }` (`L:20`)
- `animation: motion ? 'cdstkTicker 44s linear infinite' : 'none'` (`L:334`), where `motion = this.props.tickerMotion ?? true` (`L:310`)
- The `-50%` endpoint lands exactly on the duplicate boundary, which is what makes the loop seamless. **If the doubling is dropped the animation breaks** — the two are a single mechanism.
- Clipping comes from `overflow:hidden` on the bar (`L:42`).

### 3.4 Literal figures on the product landings

These are the values a real implementation has to classify as art-or-data. Screener (`SL`): `12 matches` (`:58`); filter thresholds `churn ≥ ×1.5`, `ROC 3M > 0`, `z-score < 1` (`:55–57`); four rows Giratina V $845 / +16.7% / ×2.4 / +0.6 (`:63–69`), Umbreon VMAX $1,486 / +10.9% / ×1.6 / +0.9 (`:70–76`), Sylveon VMAX $612 / +1.4% / ×1.9 / −0.2 (`:77–83`), Leafeon VMAX $497 / +3.8% / ×1.7 / +0.4 with `LOW CONFIDENCE` (`:84–90`); `honest floor Apr '25` (`:92`); `12 filters · honest floors` (`:107`). Charts (`CL`): `$1,309 ▲ +6.1%` (`:58`); axis values $1,350/$1,100/$850 (`:71–73`); `APR '25 SEAM` (`:75`); `ROC 12M +18.2% · RS 94th` (`:88`); `6 tiers · compare mode` (`:102`); `indicators · composites G1–G4` (`:108`). Binder (`BL`): `$18,432` / `+$3,108 ▲ +20.3%` / `+8.7 pp · 12M` (`:54–56`); four positions (`:61–88`); `14 positions · 6 sets · cost $15,324` and `▲ +$412 1M` (`:90–91`); `+8.7 pp · 12M` again (`:111`). The Landing's four deck panels carry the same figures (`L:72–90`, `L:102–109`, `L:127–138`, `L:148–168`) — Giratina $845 +16.7%, Umbreon $1,486 +10.9% / $1,309 +6.1%, Sylveon $612 +1.4%, Blastoise $318 −5.1%, binder $18,432 / +$3,108 / +8.7 pp. **These figures are cross-page consistent and should stay that way.**

---

## 4. States

### 4.1 Landing — complete state space

**Ticker:** two states.
- *Animating* (`tickerMotion` true, default) — 44 s linear infinite loop.
- *Static* (`tickerMotion` false) — `animation:'none'`; the track still renders at full width with items 1..n visible from the left edge and the rest clipped. **It is not hidden, and it does not become scrollable** — the tail is unreachable.

**Shuffle deck:** four interacting states.
- *Idle* — the four slots occupy positions 0–3 from `state.order` (`L:279`, initial `['A','B','C','D']`). Position table (`L:300–305`):

  | pos | transform | z-index | opacity |
  |---|---|---|---|
  | 0 (front) | `translate(0,0) rotate(0deg) scale(1)` | 40 | 1 |
  | 1 | `translate(-36px,-26px) rotate(-5deg) scale(0.97)` | 30 | 1 |
  | 2 | `translate(26px,-46px) rotate(4deg) scale(0.94)` | 20 | 0.85 |
  | 3 | `translate(-4px,-58px) rotate(-1deg) scale(0.91)` | 10 | **0** |

  Base transition `transform 0.65s cubic-bezier(0.25,0.7,0.25,1), opacity 0.65s ease` (`L:294`).
- *Exiting* — the front card gets `z-index:50`, `translate(150px,110px) rotate(14deg)`, `opacity:0`, with a faster easing-in curve `transform 0.6s cubic-bezier(0.5,0,0.8,0.4), opacity 0.55s ease-in` (`L:296`). While exiting it is filtered out of the effective order so the others advance behind it (`L:298`).
- *Paused* — `state.paused` true while the pointer is over any slot (`L:339–340`). Pausing suppresses the auto-advance only; a click still shuffles.
- *Re-entry guarded* — `doShuffle()` returns immediately if a card is already exiting (`L:287`), so rapid clicks cannot stack.

  Timing: auto-advance `setInterval` 4200 ms, gated on `!paused && (cardShuffle ?? true)` (`L:281–284`); the landing `setTimeout` is 620 ms, after which `order` rotates by one and `exiting` clears (`L:289–291`). Both timers are cleared on unmount (`L:285`).

  With `cardShuffle` false the deck is frozen but **still fully interactive** — the caption still reads "click to shuffle" and `onClick` still fires `shuffleNow` (`L:338`), because the prop gates only the interval.

**Methodology section:** present (default) or absent. When `showMethodology` is false the entire `#data` section unmounts — **and the nav's "Data" link (`L:35`) becomes a dead in-page anchor.** The nav is outside the `sc-if` and is not conditioned on the prop. This is a genuine defect in the state space, not an authoring choice, and the Blazor implementation must either drop the nav item with the section or drop the prop.

**Image slots:** filled (data URI present in `.image-slots.state.json`) or placeholder ("drop card image").

**Not present anywhere on any of the four pages:** loading, empty, error, skeleton, offline, or authenticated-variant states. The nav renders `Log in` / `Sign up →` unconditionally — there is no logged-in variant of the marketing chrome.

### 4.2 Reduced motion — **not implemented**

`Cardstock Landing.dc.html` contains **no `@media (prefers-reduced-motion: reduce)` rule.** Verified by grep across the repo: the guard `@media (prefers-reduced-motion: reduce) { * { animation-duration: 0.01ms !important; } }` exists in `Cardstock Home.dc.html:25`, `Cardstock Binder.dc.html:25`, `Cardstock Browse.dc.html:23`, `Cardstock Set.dc.html:23`, `Cardstock Card.dc.html:23` and `Cardstock Character.dc.html:23` — and in none of the four marketing pages. `image-slot.js:382` carries its own guard, but only for its loading spinner.

So the Landing — the **only** marketing page with motion, and the one carrying both an infinite 44 s ticker and a 4.2 s auto-advancing deck — is the one page with no OS-level motion respect. The only off-switches are the two DC props, which are author-time toggles, not user preferences.

This is also the one place the brand package explicitly asked for the opposite: *"Respect `prefers-reduced-motion` — both animations should have an off path (they are toggleable props in the prototype)"* (`CardStock Mockup/uploads/Brand package creation/README.md:115`). The prototype delivered the props and left the media query to implementation.

**Required behaviour for the Blazor build** (derived, not copied from the HTML — flag as a design decision if the owner wants different):
- Under `prefers-reduced-motion: reduce`, the ticker must stop. Stopping it via the existing `animation-duration: 0.01ms` idiom would snap the track to its `-50%` end state, which is visually indistinguishable from the start (the list is doubled) — so that idiom is safe here, but `animation: none` is clearer.
- A stopped ticker leaves items past the fold unreachable. Either the bar becomes horizontally scrollable when motion is off, or the item set is trimmed to what fits. **The prototype does neither and this is unresolved — see §7.**
- The deck's auto-advance must not run; click-to-shuffle should remain, with the transition shortened rather than the interaction removed.

### 4.3 Product landings

One state each. No props, no conditionals, no timers, no motion beyond CSS `:hover` colour changes on links.

---

## 5. Interactions

### 5.1 Landing

| Element | Line | Destination / behaviour |
|---|---|---|
| Nav "Features" | `L:34` | in-page anchor `#features` → `L:179` |
| Nav "Data" | `L:35` | in-page anchor `#data` → `L:216` — **dead when `showMethodology` is false** |
| Nav "Log in" | `L:36` | `Cardstock Account.dc.html` |
| Nav "Sign up →" | `L:37` | `Cardstock Account.dc.html` |
| Hero "Sign up →" | `L:51` | `Cardstock Account.dc.html` |
| Deck slot A/B/C/D | `L:65`, `L:94`, `L:115`, `L:144` | `onClick` → `shuffleNow`; `onMouseEnter` → `pauseShuffle`; `onMouseLeave` → `resumeShuffle`. **Not navigational** — the panels do not link into the app. |
| "About the data →" | `L:227` | `Cardstock About Data.dc.html` |
| Footer Screener | `L:259` | `Cardstock Screener Landing.dc.html` |
| Footer Charts | `L:260` | `Cardstock Charts Landing.dc.html` |
| Footer Binder | `L:261` | `Cardstock Binder Landing.dc.html` |
| Footer About the data | `L:262` | `Cardstock About Data.dc.html` |
| Footer Privacy | `L:267` | `Cardstock Legal.dc.html#privacy` |
| Footer Terms | `L:267` | `Cardstock Legal.dc.html#terms` |

**Reads as an affordance but is inert:** the feature-card kickers `saved screens · backtest →` (`L:197`), `open in charts →` (`L:203`), `+ binder` (`L:209`) are plain `<div>`s. The deck's `Performance →` (`L:99`) is a `<span>`. The `/home` `/screener` `/charts` caption strips (`L:112`, `L:141`, `L:171`) are labels, not links.

**`/` to search does not work on this page.** Both the hero caption (`L:53`) and the watchlist panel (`L:89`) advertise "press `/` to search", but no marketing page loads `cardstock-search.js` (grep: zero hits in all four) and none contains a search input. The shortcut is app chrome being quoted as marketing copy.

### 5.2 Product landings

| Element | Screener | Charts | Binder | Destination |
|---|---|---|---|---|
| Nav brand lockup | `SL:26` | `CL:26` | `BL:26` | Landing |
| Nav "Overview" | `SL:31` | `CL:31` | `BL:31` | Landing |
| Nav sibling 1 | `SL:32` Charts | `CL:32` Screener | `BL:32` Screener | that product landing |
| Nav sibling 2 | `SL:33` Binder | `CL:33` Binder | `BL:33` Charts | that product landing |
| Nav "Log in" | `SL:34` | `CL:34` | `BL:34` | `Cardstock Account.dc.html` |
| Nav "Sign up →" | `SL:35` | `CL:35` | `BL:35` | `Cardstock Account.dc.html` |
| Hero "Sign up →" | `SL:47` | `CL:47` | `BL:47` | `Cardstock Account.dc.html` |
| Hero "All of Cardstock →" | `SL:48` | `CL:48` | `BL:48` | Landing |
| Footer ×4 + Privacy/Terms | `SL:137–140`, `:145` | `CL:132–135`, `:140` | `BL:135–138`, `:143` | same targets as the Landing footer |

Each nav omits its own page — the cross-links always point at the other two products.

**Inert but affordance-shaped:** `Backtest this screen →` (`SL:93`) is a `<span>` styled as a button; the filter chips' `✕` glyphs (`SL:55–57`) are decorative; the Charts tier chips PSA 10 / Grade 9 / Raw (`CL:61–63`) are static.

### 5.3 Demo-mode affordances — status

**On the four marketing pages: none.** Zero occurrences of "demo" in any of them. No `/demo` link, no "View live demo", no "Browse the demo". The 2026-08-10 copy pass explicitly stripped the word — *"the word 'demo' removed (footer + hero caption; watchlist chip → LIVE)"* (`CardStock Mockup/DESIGN_NOTES.md:147`) — which is why the watchlist panel now reads `LIVE` (`L:69`) where the pre-removal draft read `DEMO` (`CardStock Mockup/uploads/Brand package creation/Cardstock Landing.dc.html:69`), and why the footer reads "fan-made" (`L:266`) where the draft read "fan-made demo" (`…/Brand package creation/Cardstock Landing.dc.html:272`).

**One hop downstream: yes, still live.** All eleven conversion CTAs across the four pages land on `Cardstock Account.dc.html`, which renders `Browse the demo →` with the tooltip *"Explore the whole app with seeded data — nothing you change is saved"* (`Cardstock Account.dc.html:56`) and navigates straight to Home (`Cardstock Account.dc.html:144`). The removal note names six pages — *"deleted from Home/Browse/Binder/Card/Set/Character"* (`DESIGN_NOTES.md:141`) — and Account is not among them. `DESIGN_NOTES.md:153` still lists "Account (all 7 auth actions incl. demo-browse)" as current, and `:155` calls Account's prototype jumper "a demo affordance, not product UI", so the Account page's demo button appears to be *intentionally retained* rather than missed. **Owner ruling needed before the sign-up funnel is built** (§7).

---

## 6. Rules and invariants

### 6.1 The prohibitions hold

Verified across all four files by grep — zero occurrences of any of: `pricing`, `per month`, `free trial`, `subscribe`, `testimonial`, `trusted by`, `users trust`, star ratings, customer logos, or user counts. (The one regex hit, `SL:118`, was the substring "rated" inside *"Overfit screens get called out, not celebrated."*)

- **No pricing.** No plan tiers, no currency attached to the product, no "free" claim, no upgrade path. Every CTA is a bare `Sign up →`.
- **No testimonials.** No quotes, no attributed praise, no author bylines.
- **No fake social proof.** No "N collectors tracking", no press logos, no badges, no counters.
- **No urgency or hype devices.** No countdowns, no waitlist, no exclamation marks anywhere in the copy (consistent with the footer blurb's own promise, `L:255`).

Invariants that must survive implementation:
- The fan-made disclaimer appears in every footer, verbatim: *"CDSTK · fan-made · not affiliated with Nintendo, The Pokémon Company, or Creatures Inc."* (`L:266`, `SL:144`, `CL:139`, `BL:142`).
- Privacy and Terms are reachable from every page (`L:267`, `SL:145`, `CL:140`, `BL:143`).
- The footer — blurb, PRODUCT column, bottom bar — is identical on all four. Divergence is a bug.
- Every conversion CTA resolves to exactly one destination. There is no second funnel.

### 6.2 Brand rules that differ from app pages

| Dimension | Marketing | App pages |
|---|---|---|
| Headline type | 52px/800 on Landing (`L:48`), 48px/800 on product pages (`SL:44`, `CL:44`, `BL:44`) | base 15px, "+15% throughout" (`HANDOFF.md:109`) |
| Section heads | 30px/700 (`L:191`, `L:226`) | terminal-scale |
| Body copy | 17px/1.6 lede (`L:49`), 14px/1.55 in cards | 13–15px |
| Air | 52px hero top; 56px section bottoms; 56px footer top; grid gaps 20–48px | dense terminal spacing |
| Container | `max-width:1080px` rail on every section | full-width terminal chrome |
| Nav | bespoke 14px×40px sticky translucent blurred bar; wordmark + a few links + CTA | 48px app nav: logo, five section links, search, account circle (`HANDOFF.md:88`) |
| Search | **absent** — no `cardstock-search.js` on any marketing page | present on every app page (`HANDOFF.md:86`) |
| Theme | **light only.** `background:#F1F1EC` hard-coded (`L:16`, `SL:16`, `CL:16`, `BL:16`); no `[data-theme="dark"]` block, no pre-paint `localStorage` script, no colorblind tokens | light + dark + colorblind palette, persisted per device (`HANDOFF.md:113`) |
| Dark surfaces | used as **content** (ticker bar, product mocks, methodology, footer), not as a theme | a real theme |
| `text-wrap: pretty` | on every headline and lede (`L:48–49`, `SL:44–45`, `CL:44–45`, `BL:44–45`) | not a marketing-specific rule |

Shared with the app and not to be changed: Inter for prose, **JetBrains Mono for every number, label, and chip**; indigo `#4A63D0` as the single action colour (hover `#3A4FB8`); green `#46C08A` / red `#D0655E` for direction; gold `#C9A84C`/`#E0A93C` reserved for seams and low-confidence.

Full palette in use: `#1C1C1E` ink · `#F1F1EC` surface · `#FFFFFF` card · `#E4E4E0` line · `#55555A`/`#8A8A86` muted · `#4A63D0`/`#3A4FB8` indigo · `#131316`/`#1B1C1F`/`#0F0F11` dark surfaces · `#2A2B2E`/`#232427` dark lines · `#F2F2EE` dark ink · `#B9B9B4`/`#9A9A96`/`#D6D6D0` dark muted · `#8C9BF2` indigo-on-dark · `#46C08A` positive · `#D0655E` negative · `#C9A84C`/`#E0A93C` gold · `#0E8A7B` mark-teal light / `#3FBFAD` dark · `rgba(74,99,208,0.18)` selection.

### 6.3 Data claims made in marketing copy — audit against DECISIONS.md

This is the risk the task asked about, and it is material. `DECISIONS.md` overrides all tiers on matters of fact (`CLAUDE.md:30`), so where the HTML asserts a *date* or a *capability*, the ledger wins even though the HTML wins on layout.

**High risk — contradicted by a recorded decision:**

| Claim | Where | Problem |
|---|---|---|
| "the Apr '25 data seam stays marked, never smoothed" | `L:202` | D-001 (`DECISIONS.md:22–33`): per-sale and census history begin at each card's **first crawler visit, late Jul 2026**, and the seam is **per-card and ragged**, not a shared date. Owner: *"That's completely false. It just started this month."* |
| "THE APR '25 SEAM — Two data sources meet in April 2025." | `L:235–236` | same |
| "The April '25 data seam stays marked on every chart" | `CL:45` | same |
| `APR '25 SEAM` rendered on the chart itself | `CL:74–75` | same — the page *draws* the false date |
| "Two data sources meet in April 2025." | `CL:113` | same |
| "honest floor Apr '25" | `SL:92` | D-033 (`DECISIONS.md:309–316`): the sufficiency floor is **2026-09-01**. "Apr '25" is not the floor and never was. |
| "Every price is built from recorded sales, not listings — deduplicated, with the ledger one click away." | `L:232` | D-002 (`DECISIONS.md:37–40`): the deep monthly series is **backfilled to ~Dec 2020 at each card's first visit**, i.e. it is not assembled from CardStock's own observed sales. D-001: the per-sale ledger starts late Jul 2026. So "every price" is false for ~5½ of the ~5.7 years on offer, and "the ledger one click away" is unavailable for those months. |
| All 16 ticker stats | `L:315–325` | Every item is either a **period-over-period delta** (`▲ +9%`, `▲ +11%`, `▼ −3%`, `▲ +1.4%`) or a composition requiring the sales/census feeds (`VENUE`, `GRADING`, `VINTAGE`, `BREADTH`, `MOST ACTIVE`). A 30-day delta needs ≥60 days of history; the ledger begins late Jul 2026 (D-001) and nothing before 2026-09-01 counts (D-033). **None of these are computable at launch.** They sit above the fold on the logged-out `/` and the panel beside them is chipped `LIVE` (`L:69`). |

**Medium risk — unverified counts and capabilities:**

| Claim | Where | Note |
|---|---|---|
| "12 filters · honest floors" | `SL:107` | `DECISIONS.md:248` warns the "27 screener filters down to about 14" figures are **survey-agent estimates** and *"Real counts should be established before the About Data copy quotes any number."* The same rule must apply to marketing copy. |
| "indicators · composites G1–G4" | `CL:108` | Same class of unverified count. |
| "backtest it against five years of history" | `SL:45` | The advertised filters (churn, z-score) are per-sale metrics that begin late Jul 2026 (D-001). A five-year backtest of a churn screen is not computable, even though five years of monthly *price* history exists. |
| "it re-runs as new sales land" | `SL:112` | Depends on a per-sale feed that is thin by design for months. |
| "a screener across every printing" | `L:49` | Coverage claim. The corpus is whatever pricecharting publishes (~91k cards per `DECISIONS.md:320`); "every printing" is stronger than anything verified. |
| "both set to 100 at your first transaction" (vs-market index) | `BL:110` | Asserts a market index exists and is chainable from an arbitrary user start date. Not verified here. |

**Low risk / defensible:**

- "Five years of graded-card price history" (`L:49`) — supported. D-002: monthly history backfilled to ~Dec 2020; `DESIGN_NOTES.md:41` says "monthly history (~60mo)". Dec 2020 → Aug 2026 ≈ 5.7 years. Note it is scoped to *price history*, the one genuinely deep series, which is exactly the right claim to make.
- "Monthly closes from PSA 10 down to raw" (`L:202`) and "6 tiers · compare mode" (`CL:102`) — consistent with D-003 (`DECISIONS.md:44–47`): `price_months` carries exactly six tiers.
- "current month still revising ○" (`CL:87`) — consistent with `DISPLAY_VOCABULARY.md:73` (hollow marker = current month provisional).
- "Thin markets get flagged, not filled in… LOW CONFIDENCE — visible, never hidden" (`L:240`) — consistent with the sufficiency posture, though the badge wording contradicts the vocabulary doc (§8).
- "Filter thousands of cards" (`SL:45`) — understated against ~91k, therefore safe.

**Recommendation:** treat every dated or counted string on these four pages as a bindable value with a single source of truth, not as copy. The seam date in particular appears five times across two files and is currently wrong in all five.

---

## 7. Open questions

1. **What are the real routes for the three product landings?** `HANDOFF.md:84` says "marketing `/screener` etc.", which collides head-on with the app's `/screener` (`HANDOFF.md:72`). The HTML settles nothing — every link is a file-relative `.dc.html` path. Options: `/product/screener`, `/screener` served conditionally by auth state (mirroring the `/` split), or a marketing subdomain.
2. **Does the logged-out `/` redirect an authenticated user to Home, or does it render Landing for everyone?** The prototypes have no auth-aware chrome to answer this.
3. **Is Account's `Browse the demo →` button (`Cardstock Account.dc.html:56`) still wanted?** It is the terminus of every marketing CTA and it contradicts the "demo mode removed" ruling. `DESIGN_NOTES.md:153` implies it is deliberate; `HANDOFF.md:100` implies it should be gone.
4. **What does the ticker do when motion is off?** A stopped 32-span track leaves most items permanently clipped. Neither scroll-on-stop nor item-trimming is specified.
5. **Does `showMethodology` survive into production?** If it does, the nav's "Data" link must be conditioned on it too, or removed.
6. **Should the Landing and Home tickers share a component?** They currently hold duplicate literal arrays in two files. Recommend one source; needs a ruling on whether the Landing gets the window switcher or stays pinned to 30d.
7. **Is the marketing site really light-only?** No dark-theme tokens or pre-paint script exist on any of the four pages, while every app page has them. A user with dark preference will hit a light `/` then a dark app.
8. **Where do the eight card images come from in production?** They currently live as base64 WebP blobs in a prototype state file. Real card art is a licensing and asset-pipeline question the mockups do not address.
9. **Are `og:*` / `twitter:*` / `<meta name="description">` tags in scope?** `brand/og-image.png` exists and is unreferenced.
10. **Does "press `/` to search" (`L:53`, `L:89`) stay as copy on a page with no search?** Either wire a search box into the marketing nav or cut the line.
11. **Are the feature-card kickers meant to be links?** `saved screens · backtest →` (`L:197`) and `open in charts →` (`L:203`) carry arrows and indigo, but are inert text.
12. **`frontLabel` mixes vocabularies** — `A → 'watchlist'` while B/C/D are routes `/home`, `/screener`, `/charts` (`L:311`). Intentional, or should A read `/home` too (the watchlist lives on Home)?

---

## 8. Contradictions found

Format: what a document claims · where it says so · what the HTML actually does. **The HTML wins on every row except the two marked ⚠, where `DECISIONS.md` overrides all tiers on matters of fact (`CLAUDE.md:30`).**

| # | Claim | Source doc:line | What the HTML actually does |
|---|---|---|---|
| 1 | Landing's primary CTA is **"View live demo"**, dropping visitors into a pre-seeded read-only account; a `/demo` route seeds the session | `uploads/CARDSTOCK_UI_SPEC_v1.md:100`, `:109`, `:140`, `:421` | No demo affordance exists on any marketing page — zero occurrences of "demo" in all four files. The only hero CTA is `Sign up →` → `Cardstock Account.dc.html` (`L:51`). Demo mode was removed 2026-08-10 (`DESIGN_NOTES.md:140–141`). **But `Cardstock Account.dc.html:56` still renders `Browse the demo →`, and every marketing CTA lands there.** |
| 2 | Landing shows **"One real product screenshot: the Charts playground showing trigger triangles (static asset, updated manually)"** | `uploads/CARDSTOCK_UI_SPEC_v1.md:140` | No screenshot exists. Product visuals are a four-panel shuffle deck built from live markup (`L:65–173`) plus inline SVG mocks on the product pages. The only bitmaps are eight decorative card-art `image-slot`s. |
| 3 | Landing carries an honesty line: **"Built on N cards, M sales observed, updated continuously"** / "{cards} cards · {sales} sales observed · updated {x}h ago" | `uploads/CARDSTOCK_UI_SPEC_v1.md:140`, `:421` | No such line. The nearest copy is the mono caption *"press `/` to search · fan-made"* (`L:53`). No count of cards or sales appears anywhere on the marketing site. |
| 4 | Landing footer includes a **GitHub link** ("portfolio!") | `uploads/CARDSTOCK_UI_SPEC_v1.md:140` | Footer links are Screener / Charts / Binder / About the data (`L:259–262`) and Privacy / Terms / © (`L:267`). No GitHub link on any of the four pages. |
| 5 | Landing is **"single column, generous air"** | `uploads/CARDSTOCK_UI_SPEC_v1.md:140` | Hero is a two-column grid `1.05fr 0.95fr` (`L:45`); Features and Methodology are three-column grids (`L:192`, `L:229`); footer is `1.4fr 1fr` (`L:249`). Only the "generous air" half survived. |
| 6 | Nav is **"nav-lite: wordmark + 'Sign in'"** | `uploads/CARDSTOCK_UI_SPEC_v1.md:140` | Nav carries wordmark + `CDSTK` chip + Features + Data + Log in + `Sign up →` (`L:28–37`), and it is sticky and blurred, not lite. |
| 7 | Hero headline is **"Technical analysis for Pokémon cards."** with sub "Five years of price history, every sale we've ever seen, and the grading census…" | `uploads/CARDSTOCK_UI_SPEC_v1.md:421` | Headline is **"The trading terminal for Pokémon cards."** (`L:48`); the lede is *"Five years of graded-card price history, a screener across every printing, and your binder tracked like a portfolio. For collectors who read pop reports for fun."* (`L:49`). Change recorded at `DESIGN_NOTES.md:147`. |
| 8 | Feature beats are *"Screen 100,000 cards by momentum, supply, and value"* / *"Tune real indicators…"* / *"Track your binder against the market, to the cent."* | `uploads/CARDSTOCK_UI_SPEC_v1.md:421` | Feature cards are titled **Screener / Charts / Binder** (`L:195`, `L:201`, `L:207`) with entirely different body copy. No "100,000 cards" claim exists on the marketing site. |
| 9 | Every logged-out route redirects to Landing **"with a 'view demo' affordance"**, and the nav has a search box that `/` focuses | `uploads/CARDSTOCK_UI_SPEC_v1.md:127` | No marketing page loads `cardstock-search.js` (zero hits) and none has a search input, though `L:53` and `L:89` still print "press `/` to search". No view-demo affordance. |
| 10 | Footer has three columns including a **BRAND** column (Brand system, Logo files, Tokens); bottom bar reads *"CDSTK · **fan-made demo** · …"* in `#71716D` | `uploads/Brand package creation/README.md:94`; `uploads/Brand package creation/Cardstock Landing.dc.html:272` | Footer is two columns, `1.4fr 1fr`, no BRAND column (`L:249–263`); bottom bar reads *"CDSTK · **fan-made** · …"* in `#B9B9B4` (`L:266`). Both changes recorded at `DESIGN_NOTES.md:147`. |
| 11 | Hero eyebrow is **"CDSTK · POKÉMON TCG AFTERMARKET DATA"**; H1 is "The terminal for the Pokémon card aftermarket."; caption ends "fan-made **demo**" | `uploads/Brand package creation/README.md:78` | Eyebrow is `POKÉMON TCG AFTERMARKET DATA` with CDSTK dropped (`L:47`); H1 as row 7; caption ends "fan-made" (`L:53`). |
| 12 | The watchlist deck panel's header chip reads **"DEMO"** (indigo) | `uploads/Brand package creation/README.md:85` | It reads **`LIVE`** (`L:69`). Recorded at `DESIGN_NOTES.md:147`. |
| 13 | The thin-data badge is **`LOW DATA`** with an `N OBS` amber badge, and the five sufficiency states are *"the complete render set"* | `DISPLAY_VOCABULARY.md:55` | Both marketing surfaces render a badge reading **`LOW CONFIDENCE`** (`L:240`, `SL:85`). `DESIGN_NOTES.md:33` also uses "LOW CONFIDENCE pane badges", so the vocabulary doc's "complete" list is incomplete. |
| 14 | *"Respect `prefers-reduced-motion` — both animations should have an off path"* | `uploads/Brand package creation/README.md:115` | The Landing has **no** `prefers-reduced-motion` rule, while six app prototypes do (e.g. `Cardstock Home.dc.html:25`). The only off paths are the `tickerMotion` / `cardShuffle` author props (`L:274–275`, `L:282`, `L:310`). |
| 15 | Product landings live at **"marketing `/screener` etc."** | `HANDOFF.md:84` | Collides with the app's `/screener` (`HANDOFF.md:72`). The HTML settles nothing — every link is a file-relative `.dc.html` path (`L:259–261`, `SL:31–33`). Unresolved, see §7.1. |
| 16 ⚠ | The Apr '25 seam: *"Two data sources meet in April 2025"*, *"the Apr '25 data seam"*, *"honest floor Apr '25"* — **asserted by the HTML itself** | `L:202`, `L:235–236`, `SL:92`, `CL:45`, `CL:74–75`, `CL:113`; echoed at `DESIGN_NOTES.md:35` | **The HTML is wrong on the fact.** D-001 (`DECISIONS.md:22–33`) establishes that per-sale and census history begin at each card's first crawler visit in **late Jul 2026**, per-card and ragged; D-009 (`DECISIONS.md:385–388`) flags the Apr '25 liquidity seam as having no data behind it and leaves *why it was ever drawn* unresolved. The seam **marker** is authoritative design; the **date** is not. |
| 17 ⚠ | *"honest floor Apr '25"* as the sufficiency floor | `SL:92` | D-033 (`DECISIONS.md:309–316`): the floor is **2026-09-01**, chosen deliberately as a disclosed cutoff. Apr '25 is neither the floor nor a date the ledger recognises. |
