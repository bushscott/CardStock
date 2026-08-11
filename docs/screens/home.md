# Home — screen specification

**Extracted from:** `CardStock Mockup/Cardstock Home.dc.html` (629 lines), read in full 2026-08-10.
**Authority:** Tier 1. Where this file cites the prototype it is authoritative; §8 records every place a
markdown document disagrees with it. Line references below are `Home.dc.html` line numbers unless a file
is named. Seeded sample values (card names, dollar figures, percentages) are illustrative and are quoted
here only where the *shape* or *copy* of a field cannot be described without them.

---

## 1. Identity

| | |
|---|---|
| **Screen name** | Home (`data-screen-label="Home"`, :37). Nav tab label "Home" (:45). |
| **Route** | Documents disagree — `HANDOFF.md:71` says `/`, `uploads/CARDSTOCK_UI_SPEC_v1.md:111` says `/home`. The prototype resolves nothing: it only self-links as `Cardstock Home.dc.html` (:41). Unresolved, see §7. |
| **Purpose** | The returning user's first screen: what the market did in the chosen window, what my watched cards are doing, what entered or left my saved screens since I was last here, and what my binder is worth. |
| **Auth** | Logged-in app chrome (account circle, watchlists, binder). No logged-out variant exists in this file. |

---

## 2. Layout

The page is a flex column, `min-height: 100vh`, base font-size 15px (:37). **The whole document scrolls** —
nothing on Home is a fixed-height app frame (contrast `DESIGN_NOTES.md:6`, which describes Charts that way).

Top to bottom in *visual* order:

1. **Nav bar** — 48px, `position: sticky; top: 0; z-index: 20` (:39). Stays pinned through all scrolling.
2. **Market ticker bar** — 36px, *not* sticky (:56). Scrolls away with the page.
3. **`<main>`** — flex column, `gap: 16px`, `padding: 16px 20px`, `max-width: 1480px`, centred (:83).
   - **Row 1** — a two-column CSS grid `minmax(0,3fr) minmax(0,2fr)`, `gap:16px`, `align-items:stretch`
     (:147): **Screen activity** (3fr, left) and **Binder** (2fr, right).
   - **Row 2** — **Watchlist**, full width of `main` (:85).
4. **Footer** — links left, corpus counts right (:306).

**DOM order is the reverse of visual order for the two main rows.** The Watchlist `<section>` is written
first in the DOM and carries `order: 2` (:85); the activity/binder grid is written second and carries
`order: 1` (:147). A Blazor rebuild must decide deliberately whether to keep CSS-order inversion (which
puts the watchlist first in tab and screen-reader order) or reorder the markup. See §7.

**Overlays**, outside the flow:

- **Peek panel** — `position: fixed`, `top: 96px` (= 48 nav + 36 ticker + 12), `right: 20px`,
  `bottom: 16px`, `width: 480px`, `max-width: calc(100vw - 40px)` (:231). It is a right-edge drawer at
  every viewport width, always full remaining height, and it scrolls internally (`overflow: auto`) with
  its own sticky header (:232). It is *authored inside* the row-1 grid (:230) but `position: fixed`
  removes it from that grid.
- **Hover art preview** — `position: fixed`, 164×226, `z-index: 100`, `pointer-events: none` (:315).

**Stacking order actually produced** (a rebuild should preserve or consciously fix it):
hover preview 100 > row-actions menu 40 (:124) > nav 20 (:39) > watchlist column header 10 (:93)
> **peek panel 10** (:231). The peek panel's style attribute declares `z-index: 50` and then `z-index: 10`
later in the same attribute; CSS last-wins, so the effective value is **10**. Consequence: an open
row-actions menu (40) paints over the peek panel. In practice they rarely coexist because opening a row
clears `menuIdx` (:573), but the ordering is a latent defect.

**No responsive breakpoints exist.** The only media query in the file is
`prefers-reduced-motion` (:25). Column ratios, the 480px peek width and the 1480px main cap are fixed.

---

## 3. Data contract

Every rendered field. "Source" is the plausible production origin; the prototype seeds all of it
client-side, so sources are inferred from `DECISIONS.md` (D-004, D-039) and `CARDSTOCK_UI_SPEC_v1.md:161`
and are flagged where speculative.

### 3.1 Nav bar (:39–54) — shared chrome, not Home-specific

| Element | Content | Notes |
|---|---|---|
| Logo lockup | Cardstock mark + wordmark | `<a>` to Home, `aria-label="Cardstock home"` (:41). Mark uses `--logoTeal`. |
| Section links | Home · Screener · Charts · Binder · Browse | Home is active: weight 600, `--ink`, 2px `--acc` bottom border (:45). Others weight 500, `--mut`, transparent border (:46–49). |
| Search | `<cardstock-search>` (:52) | External component. Placeholder "Search cards, sets, characters", `aria-label="Search"`, `/` kbd hint (`cardstock-search.js:57,58,62`). |
| Account circle | Single letter, seeded `O` | 28px circle → `Cardstock Profile.dc.html`; `aria-label="Account"`, `title="Profile & settings"` (:53). Presumably the user's display-name initial. |

There is **no notification bell** — removed by ruling (`DESIGN_NOTES.md:120`).

### 3.2 Market ticker (:56–81, data at :434–472)

**Fixed left cell** (:57–64): the literal label `MARKET` (12.5px, `--mut2`, `letter-spacing: .06em`) plus
the window `<select>`.

| Field | Label | What it is | Format | Source |
|---|---|---|---|---|
| `win` | *(none — `aria-label="Stats window"`)* | Selected stats window | Enum `30d` \| `7d` \| `90d`; **options are authored in that order**, `30d` first (:60–62); default `30d` (:331) | UI state only |

**Scrolling stat list.** Each item renders as four inline spans in this grammar (:69):

`{l} {n} {v} {x}` — label (`--mut`) · optional subject name (`--ink`) · value (colour `c`, weight 500) ·
optional suffix (`--mut2`). `n` and `x` are absent on most items and simply render nothing.

The list is **16 items, identical in composition across all three windows** — only the numbers change
(:437–469). `hint-placeholder-count="10"` (:68) is a Design-Composer streaming hint, **not** a product
limit.

| # | Label (`l`) | `n` (subject) | Value (`v`) shape | `x` (suffix) | Colour rule | Plausible source |
|---|---|---|---|---|---|---|
| 1 | `SALES` | — | integer count + ▲/▼ + signed % | — | pos/neg by direction | `sales` count over window |
| 2 | `VOLUME` | — | `$NNNK`/`$N.NM` + ▲/▼ + signed % | — | pos/neg | Σ `sales.price_cents` |
| 3 | `BREADTH` | — | `NN% advancing` | — | pos (seeded) | share of cards up over window |
| 4 | `INDEX` | — | ▲/▼ + signed % | **`30d`** | pos/neg | market index (**does not exist** — D-004) |
| 5 | `VINTAGE` | — | `NN% of $ vol` | — | `--ink` (neutral) | vintage-era share of $ volume |
| 6 | `GRADING` | — | `+N,NNN slabs · gem NN%` | — | `--ink` | `populations` deltas + gem rate |
| 7 | `MEDIAN SALE` | — | `$NN` + ▲/▼ + signed % | — | pos/neg | median `sales.price_cents` |
| 8 | `VENUE` | — | `ebay NN% · auction NN% · tcgp N%` | — | `--ink` | `sales.source` mix. Note the display buckets (`ebay`/`auction`/`tcgp`) are **not** the source enum (`DISPLAY_VOCABULARY.md:61`) — auction pools goldin/heritage/pwcc. |
| 9 | `NEW 12M HIGHS` | — | ▲ + integer count | **`30d`** | pos | count of cards at a 12M high |
| 10 | `MEDIAN ROC` | — | ▲/▼ + signed % | — | pos/neg | median rate-of-change |
| 11 | `TOP WINNER` | card name | ▲ + signed % | — | pos | best performer over window |
| 12 | `TOP LOSER` | card name | ▼ + signed % | — | neg | worst performer |
| 13 | `TOP SALE` | card name | `$NN,NNN` | **venue** (`goldin`, `pwcc`) | `--ink` | max `sales.price_cents` |
| 14 | `MOST ACTIVE` | card name | `NNN sales` | — | `--ink` | max sale count |
| 15 | `HOT SET` | set name | ▲ + signed % | — | pos | best set index |
| 16 | `CHARACTER LEADER` | species name | ▲ + signed % | — | pos | best species aggregate |

**The 30d-pinning rule.** Items 4 (`INDEX`) and 9 (`NEW 12M HIGHS`) carry `x: '30d'` in **all three**
window datasets, and their values are **byte-identical across windows** (`▲ +2.4%` and `▲ 214`,
:439/450/461 and :442/453/464). They are monthly-data metrics and do not re-window; the `30d` suffix is
their disclosure. This matches `DESIGN_NOTES.md:28`. A rebuild must render these two from a fixed 30-day
computation regardless of the selector, and must keep the visible `30d` tag. Item 13's `x` shows that
the `x` slot is a **generic suffix**, not a window field.

**Colours** are computed, not tokenised at render (:435): `G = PAL.pos2`, `L = PAL.neg2`, `K = PAL.ink`,
selected from a 4-branch theme × colour-blind palette (:323–330).

### 3.3 Watchlist (:85–145)

**Tab strip** (:86–92)

| Field | Label | Format | Source |
|---|---|---|---|
| Section title | `Watchlist` | Inter Tight 700 / 17.5px | static |
| Title tooltip | "Single cards you follow, each tracking the combination of signals you pinned for it. Chips show each signal's current state. Edit a row's tracked signals in Charts (⋯ → Open full chart)." (:87) | `title` attr | static |
| Tab name | list name (seeded `Main`, `Grading candidates`) | Inter, 15px | `watchlists.name` |
| Tab count | member count | mono 12.5px, `--mut2` (:89) | `count(watchlist_cards)` |
| Tab tooltip | active: `Showing "{name}" — {n} card(s)`; inactive: `Switch to "{name}" — {n} card(s)`. Singular/plural handled (:595) | computed | — |
| `+ new list` | literal `+ new list`, tooltip "Create another watchlist — rows can be moved between lists" (:91) | — | — |

**Column headers** (:93–102) — sticky at `top: 48px`, i.e. flush under the nav, `z-index: 10`.
Uppercase, 12.5px, weight 600, `letter-spacing: .05em`, `--mut2`. Grid template (:616):

`48px · {card}px · {tier}px · {price}px · {chg}px · {spark}px · minmax(0,1fr) · 18px`

| Col | Header text | Resizable | Default width |
|---|---|---|---|
| 1 | *(blank — art)* | no | 48px fixed |
| 2 | `Card` | yes (`rsCard`) | 220px |
| 3 | `Tier` | yes (`rsTier`) | 52px |
| 4 | `Price` | yes (`rsPrice`) | 76px |
| 5 | `1M %` | yes (`rsChg`) | 52px |
| 6 | `12M` | yes (`rsSpark`) | 68px |
| 7 | `Tracked signals` + `· set in Charts` in `--mut2` (:100) | no | `minmax(0,1fr)` |
| 8 | *(blank — ⋯ menu)* | no | 18px fixed |

Header labels are centred within their cell; the resize grip is a `│` glyph at the right edge,
`cursor: col-resize`, `title="Drag to resize"`, `--line3` turning `--acc` on hover (:95).
Column 7's tooltip: "The signals you pinned for this card in Charts — chips show each one's current
state. Edit via ⋯ → Open full chart." (:100)

**Row fields** (:104–134, computed :550–591). Row min-height 66px, bottom border `--line4`.

| Field | Column | What it is | Format | Source |
|---|---|---|---|---|
| Art thumb | 1 | `<image-slot id="art-{cardId}" shape="rounded" radius="4" placeholder=" ">` in a 48×66 box (:105–107) | Empty placeholder renders at `opacity: 0` (:22); backing box shows the card's accent gradient `linear-gradient(160deg, {a0}, {a1})` (:558) | `cards.image_hash`; accents from `card_accents` (spec:382) — **licensing-blocked**, `HANDOFF.md:141` |
| Card name | 2 | line 1, weight 600, ellipsis-truncated (:109) | text | `cards.name` |
| Set line | 2 | line 2, 12.5px `--mut2`, ellipsis (:110) | `{Set name} · {number}/{set total}` | `sets.name` + card number |
| Tier | 3 | mono 12.5px `--mut`, centred (:112) | grade-bucket label; seeded `PSA 10`, `PSA 9`, `PSA 8`, `Raw` | `watchlist_cards` primary tier |
| Price | 4 | mono 14.5px weight 700, centred (:113) | `$` + `toLocaleString('en-US')`, **whole dollars, no cents** (:433) | latest `price_months` for that tier |
| 1M % | 5 | mono 14px, centred, coloured (:114) | `series[11]/series[10] − 1` (:478) rendered as sign + 1 decimal + `%`, using **U+2212** for negative (:479); colour `pos2` when ≥ 0 else `neg2` (:557) | last two monthly closes |
| 12M sparkline | 6 | SVG `viewBox="0 0 64 18"`, `preserveAspectRatio="none"`, `width:100%` (:115) | Filled polygon under a 1.25px polyline. Fill = `posBg(0.12)` / `negBg(0.10)`; stroke = the 1M % colour. Points min-max normalised to y∈[3,17] over the 12 values (:473–476). Polygon closes to `0,17 … 63,17` (:569) | trailing 12 `price_months` rows |
| Tracked-signal chips | 7 | wrapping flex, `overflow: hidden` (:116) | See below | `watchlist_cards.signals_json` × computed signal state |
| ⋯ button | 8 | `aria-label="Row actions"`, `title="More actions for this card"`, 16px `--mut2` (:122) | — | — |

**Chips** (:117–119). One chip per *tracked* signal, always rendered whatever its state. Grammar:
`{icon} {text}`, mono 11.5px weight 500, 4px radius, `title` = evidence sentence. Colour comes from a
4-key map (:349–354):

| Key | Foreground | Background | Icons seen in seed |
|---|---|---|---|
| `gain` | `--pos` | `posBg(0.10)` | `▲`, `◆` |
| `loss` | `--neg` | `negBg(0.10)` | `▼` |
| `warn` | `--warnInk` | `rgba(176,127,26,0.12)` | `–` |
| `muted` | `--mut2` | `--mutbg` | `–`, `◌` |

Chip text shapes present in the seed (:360–410) — the complete inventory is
`DISPLAY_VOCABULARY.md:11–34`: `RS 94th`, `RS 84th`, `MACD +`, `MACD –`, `RSI 71`, `Churn ×1.6`,
`Churn +140%`, `Churn — 12d`, `z +1.62`, `Pop Δ +2.4%`, `Pop Δ +5.1%`, `EMA 3/9 ▼`, `Quiet Accum`,
`Arb EV +$62`. Note the composite `◆ Quiet Accum` uses `gain` styling, so `◆` is not a state of its own.

**Chip legend** (:136–144), bottom bar, 12.5px `--mut2`, with tooltip "Chip color = the signal's current
state, not its identity. Colored means it hit; grey means nothing to report." Four swatches:

| Swatch | Copy | Tokens |
|---|---|---|
| 1 | `▲ hit bullish` | `--pos` on `posBg10` |
| 2 | `▼ hit bearish` | `--neg` on `negBg10` |
| 3 | `– caution` | `--warnInk` on `rgba(176,127,26,.12)` |
| 4 | `– quiet · ◌ soon` | `--mut2` on `--mutbg` |

Four swatches cover the five pill states of `DISPLAY_VOCABULARY.md:40–47` — *quiet* and *pending* share
the grey swatch by design.

**Keyboard hint** (:143), right-aligned in the same bar: `↑↓ rows · Enter peek · / search`.

**Row-actions menu items** (:124–131) — copy and tooltips are load-bearing:

| Item | Colour | Tooltip | Wired? |
|---|---|---|---|
| `Open full chart` | `--ink` | "Opens Charts with this row's tracked signals pinned — any pin changes save back to this row via Update watchlist." | yes → `Cardstock Charts.dc.html` (:589) |
| `Open card page` | `--ink` | "Full reference page — every grade tier, the sales ledger, and census data" | yes → `Cardstock Card.dc.html` (:590) |
| `Add to binder` | `--ink` | "Log a purchase of this card — opens the binder transaction form" | **no** — closes menu only (:127) |
| *(divider)* | `--line` | — | — |
| `Move to list…` | `--ink` | "Move this row to another watchlist — its tracked signals come with it" | **no** (:129) |
| `Remove from watchlist` | `--neg2`, hover bg `negBg08` | "Stop following this card — its tracked signals are forgotten" | **no** (:130) |

### 3.4 Screen activity (:149–167)

| Field | Content | Source |
|---|---|---|
| Title | `Screen activity` (:151) | static |
| Title tooltip | "Cards that entered or exited one of your screens when the data refreshed. Manage screens in the Screener." | static |
| Header meta | `7 since your last visit · 1 unlock · your screens →` (:152) | **Hardcoded literal text in the prototype**, not derived from the feed. In production: count of rows since `last_seen_at`; count of unlock rows; link → `/screener`. |

Row anatomy (:155–165, built :601–610):

| Field | Format | Source |
|---|---|---|
| Icon (`f.i`) | mono 12.5px, 12px fixed width, coloured by state (:156). Seen: `▲ ▼ – ◆` | derived from row type + thesis direction |
| Card name (`f.name`) | weight 500, 15px (:159) | `cards.name` — resolved by joining the event's card id (:602) |
| Rule (`f.rule`) | 14px weight 600, **same colour as icon** (:160). Shapes: `Entered "{screen name}"`, `Exited "{screen name}"`, `Indicator unlocked: {metric}` (:417–423) | `saved_screens.name`; unlock rows name the metric |
| Evidence (`f.ev`) | 13px `--mut`, one line, mid-dot separated metric clauses, e.g. `churn 30d +140% vs 90d · price −0.4% 1M · pop flat` (:417) | the screen's satisfied filter values at evaluation time |
| Timestamp (`f.t`) | mono 12px `--mut2`, right, relative: `2h ago`, `1d ago`, `3d ago` (:164) | `signal_events.fired_at` rendered relative |

Rows compute `thumbBg` and `letter` (:606) that the template never renders — **dead fields**; do not
reimplement.

### 3.5 Binder card (:169–228)

Entirely static markup — no template binding anywhere in this section. Every number below is seeded.

| Region | Label | Value shape | Colour rule | Source |
|---|---|---|---|---|
| Header | `Binder` | Inter Tight 700 17.5px | `--ink` | — |
| Header | `Performance →` | link → `Cardstock Binder.dc.html#performance` (:172) | `--acc` | — |
| Tile row 1 | `Total value` | mono 25.5px/700, `$NN,NNN` | `--ink` | Σ holdings × latest price |
| Tile row 1 | `Unrealized` | mono 25.5px/700, signed `$N,NNN`; sub-line mono 12.5px `▲ +NN.N%` | both `--pos2` / `--neg2` by sign (:181–182) | value − cost basis |
| Tile row 1 | `vs market index` | mono 25.5px/700, signed number + ` pp` in 14px (:186); sub-line `trailing 12M` in `--mut2` | `--pos2` / `--neg2` | portfolio return − index return, **percentage points** |
| Tile row 2 | `Positions` | mono 15px/600 `{n}` + weight-400 `--mut2` `across {m} sets` (:193) | `--ink` | count of holdings, distinct sets |
| Tile row 2 | `Cost basis` | mono 15px/600 `$NN,NNN` | `--ink` | Σ `binder_transactions` |
| Tile row 2 | `1M change` | mono 15px/600, `▲ +$NNN` | `--pos2` / `--neg2` | value delta over 1 month |
| List | `Best position` | `{card name} {▲ +NN%}` — name Inter 500, figure mono 600 coloured (:207) | `--pos2` | best % gainer |
| List | `Worst position` | `{card name} {▼ −N%}` (:211). Label is **"Worst position", never "Laggard"** (`DESIGN_NOTES.md:30`) | `--neg2` | worst % performer |
| List | `Largest holding` | `{card name} {NN% of value}` (:215) | `--mut` (deliberately not signed/coloured) | max share of total value |
| Chart | *(none)* | SVG `viewBox="0 0 300 48"`, `preserveAspectRatio="none"` (:218–222) | 3 layers, see below | 12 monthly points |
| Legend | `— portfolio` · `┄ market index` · right-aligned `12M · normalized` (:223–227) | mono 12px `--mut2` | — |

Chart series (:219–221), painted back to front:
1. Filled area under the portfolio line, `rgba(74, 99, 208, 0.07)` — a **literal**, not a token, so it
   does not respond to theme or colour-blind mode.
2. Market index — `--mut2`, `stroke-width: 1`, `stroke-dasharray: "3 3"`.
3. Portfolio — `--acc`, `stroke-width: 1.5`, solid.

No axes, no labels, no tooltips, `aria-hidden="true"`. The "normalized" claim in the legend is
unenforced by the seed geometry.

### 3.6 Peek panel (:230–302, built :488–518)

Rendered only when `hasPeek` (:230). Border-top is a 3px bar in `peek.accent` = the card's primary
accent hex (:508, palette :426–431).

| Region | Field | Format | Source |
|---|---|---|---|
| Header | `peek.name` | 16px weight 600 (:234) | `cards.name` |
| Header | `peek.set` | 14px `--mut2` (:235) | set · number |
| Header | Close `✕` | `aria-label="Close peek"`, title "Close the preview — the watchlist stays as it is" (:237) | — |
| Art | `<image-slot id="art-{id}" placeholder="card art">` | 178×246 box (:241–242) | same slot id as the row thumb |
| Prices | Section label `Current prices` | uppercase 12.5px/600, `.06em` tracking (:245) | — |
| Prices | 6 rows: label + price | `TIER_LABELS = ['Raw','Grade 7','Grade 8','Grade 9','Grade 9.5','PSA 10']` (:425), price `$N,NNN` whole dollars. The row whose label **string-equals** the card's tier renders `--ink`/600; all others `--mut`/400 (:509–512) | latest `price_months` per tier |
| Chart | Section label | `12M · {peek.tier} · tracked signals` (:256) | — |
| Chart | `edit →` | link → `Cardstock Charts.dc.html#signals` (:257) | — |
| Chart | Y-axis max / min | `peek.yMax` / `peek.yMin` = series max/min via `toLocaleString('en-US')`, mono 10.5px `--mut2`, right-anchored at x=30 (:263–264, :515) | series bounds |
| Chart | X-axis labels | **`Aug '25` and `Jul '26` are hardcoded literals** (:265–266), not derived from the series | — |
| Chart | Gridlines | 3 horizontal `--line4` lines at y = 12 / 61 / 110, x from 34 to 312 (:260–262) | — |
| Chart | Price line | `--acc`, `stroke-width: 1.5`, 12 points, plot box L=34 R=8 T=12 B=20 over 320×130 (:491–496) | monthly closes |
| Chart | Trigger triangles | One `<polygon>` per tracked-signal firing. **Up** triangles sit *below* the point (apex y+5, base y+12), `--pos2`; **down** sit *above* (apex y−5, base y−12), `--neg2` (:497–502). Each carries a native `<title>` tooltip (:269) | signal crossing events |
| Chart | Triangle tooltip | Full sentence naming rule, parameters, month, price then, and forward returns — e.g. "MACD (3,6,4) crossed above signal — Dec 2025. Price then $1,240; +3M +2.0%, +6M +12.1%" (:359) | — |
| Chart | Current-month dot | Hollow `r=3` circle at the last point, fill `--card`, stroke `--acc`, `<title>current month still revising</title>` (:271) | month-to-date aggregate |
| Chart | `peek.summary` | 13px `--mut`, template: `{tier} {up\|down} {N}% over 3 months, {above\|below} its 6-month average.` — `N` = `series[11]/series[8] − 1` rounded to 0 dp; the average is the mean of the last 6 values (:503–505) | computed |
| Chart | Chips | Same chip component and colours as the watchlist row (:275–277, :516) | — |
| Sales | Section label | `Last 5 sales · {peek.tier}` (:281) | — |
| Sales | 4-column grid `92px 64px 1fr 70px` (:283) | date `Mon DD, YYYY` mono `--mut` · grade mono `--mut2` (**always the card's own tier**, :486) · source 12.5px `--mut2` lowercase · price mono right weight 500 | `sales(sold_on, grade_tier, source, price_cents)` |
| Sales | Source values | Seeded `ebay`, `pwcc`, `goldin`. The full enum is `ebay · tcgplayer · goldin · heritage · pwcc` (`DISPLAY_VOCABULARY.md:61`) — render verbatim, lowercase, mono | — |
| Pop | `Population Δ` | Label `--mut`, value mono `--mut` on a `--mutbg` 6px-radius bar (:291–293) | `populations` LAG delta |
| Pop | value shapes | Normal: `+N.N% (60d)`. **Insufficient-data variant:** `first observed 2026-07-30 — deltas begin next observation` (:393) — a full sentence in place of a number | — |
| Actions | `Open full chart →` | Primary: `--btn` bg, `#FFFFFF` literal fg, 30px, → `Cardstock Charts.dc.html` (:296) | — |
| Actions | `Card page` | Secondary button, title "Full reference page — every grade tier, the sales ledger, and census data" (:297) — **not wired** | — |
| Actions | `Edit signals` | Secondary button, title "Open this card in Charts with its tracked signals pinned, ready to change" (:298) — **not wired** | — |

### 3.7 Hover art preview (:314–318)

164×226 fixed panel, 8px radius, `box-shadow: 0 14px 40px rgba(20,19,26,0.35)`, `pointer-events: none`,
containing `<image-slot id="art-{id}" radius="8" placeholder=" ">` over the card's accent gradient.
164/48 ≈ 226/66 ≈ **3.42× the row thumbnail**.

### 3.8 Footer (:306–313)

| Field | Content | Target |
|---|---|---|
| Link | `About our data` | `Cardstock About Data.dc.html` |
| Link | `Privacy` | `Cardstock Legal.dc.html#privacy` |
| Link | `Terms` | `Cardstock Legal.dc.html#terms` |
| Corpus counts | `101,882 cards · 4.2M sales observed` — mono 12px, right (:312) | `count(cards)`, `count(sales)` |

There is **no** "refreshed just now" stamp on this page (grep confirms; see §8).

---

## 4. States

### 4.1 Component state (:331)

```
tab: 0 · focusIdx: -1 · peekId: null · win: '30d' · menuIdx: null · pv: null
colW: { card: 220, tier: 52, price: 76, chg: 52, spark: 68 }
```

`dragIdx` and `overIdx` are used (:549, :575–585) but **never initialised** — they are `undefined` until
the first drag. A typed rebuild should declare them nullable and default to null.

### 4.2 Element states

| Element | State | Trigger | Render |
|---|---|---|---|
| Ticker | scrolling | default | `animation: ticker 45s linear infinite`, translateX 0 → −50% over two identical copies (:24, :66–76) |
| Ticker | paused | pointer over the marquee | `animation-play-state: paused` via `style-hover` (:66), compiled to a real `:hover` rule (`support.js:428`) |
| Ticker | effectively frozen | `prefers-reduced-motion: reduce` | all animation durations forced to `0.01ms` (:25) — an infinite marquee lands instantly at −50% and stays. Not an authored reduced-motion design; see §7 |
| Ticker window | 30d / 7d / 90d | select change (:619) | All 16 stats swap; items 4 and 9 do not change (§3.2) |
| Watchlist tab | active | `tab === i` | weight 600, `--ink`, 2px `--acc` underline (:596–598) |
| Watchlist tab | inactive | otherwise | weight 500, `--mut`, transparent underline |
| Row | default | — | transparent background |
| Row | hovered | pointer | background `--hov` (:104) |
| Row | keyboard-focused | `focusIdx === index` | background `--accBg` (:571) — a *tinted* row, distinct from `:hover` |
| Row | being dragged | `dragIdx === index` | `opacity: 0.35` (:575) |
| Row | drop target | `overIdx === index && dragIdx !== null && dragIdx !== index` | `box-shadow: inset 0 2px 0 var(--acc)` — a 2px accent line along the row's **top** edge (:576) |
| Row menu | open | `menuIdx === index` | 190px min-width popover, `top: 22px`, right-aligned, z-40 (:124) |
| Row menu | closed | default | `sc-if` renders nothing (:123) |
| Chip | hit bullish | signal state `gain` | `--pos` on `posBg(.10)`, icon `▲` (or `◆` for composites) |
| Chip | hit bearish | `loss` | `--neg` on `negBg(.10)`, icon `▼` |
| Chip | caution | `warn` | `--warnInk` on amber tint, icon `–` |
| Chip | quiet | `muted` | `--mut2` on `--mutbg`, icon `–`, text `{name} –` |
| Chip | pending / LOW DATA | `muted` | `--mut2` on `--mutbg`, icon `◌`, text `{name} — {N}d`; tooltip is an unlock countdown naming the date history begins (:395) |
| Feed row | screen ENTER | rule text `Entered "…"` | icon+rule coloured by screen thesis: `▲` gain (:417) or `–` warn for a caution thesis (:419) or `▼` loss for an avoid screen (:420) |
| Feed row | screen EXIT | `Exited "…"` | seeded as `▼` with `warn` colour (:421) — icon and colour are independent fields |
| Feed row | product event / UNLOCK | `Indicator unlocked: …` | `◆` with `warn` colour; evidence names the floor rule and warns the metric "starts LOW CONFIDENCE" (:423) |
| Peek | closed | `peekId === null` | `sc-if` renders nothing (:230) |
| Peek | open | `peekId` set | slides in via `peekIn` 0.16s ease-out — **transform only, never opacity** (:23; the reason is `DESIGN_NOTES.md:37`) |
| Peek price row | highlighted | `TIER_LABELS[i] === card.tier` | `--ink` / weight 600 |
| Peek price row | normal | otherwise | `--mut` / weight 400. **For a `PSA 9` or `PSA 8` card no row ever matches**, because the ladder labels those buckets `Grade 9` / `Grade 8` (:425 vs :371, :381). See §8 |
| Peek pop Δ | value | `populations` delta available | `+N.N% (60d)` |
| Peek pop Δ | not-yet | first observation only | prose sentence, no number (:393) |
| Hover preview | hidden | `pv === null` | nothing |
| Hover preview | shown | `mouseenter` on the row thumbnail | positioned panel (§5) |
| Theme | light / dark | `localStorage['cardstock-theme']` read pre-paint (:35) and again at class-field init (:323) | `data-theme="dark"` token block (:29–32) + a dark branch of `PAL` |
| Colour-blind | standard / CVD | `localStorage['cardstock-cvd'] === '1'` | `data-cvd="1"` token block (:27) + CVD branch of `PAL`. **Hue swap only** — glyphs and copy never change |

### 4.3 States that are specified elsewhere but absent here

The prototype has **no** loading, empty, or error branch on any Home module — no skeleton rows, no
zero-state copy, no retry affordance, no `sc-if` guarding an empty list. `hint-placeholder-count`
attributes (rows 8, ticks 10, chips 2, tabs 2, feed 6, peek tiers 6 / chips 2 / sales 5) are
Design-Composer *streaming* placeholders, not product states.

Of the honesty states named in `HANDOFF.md:43`, Home shows exactly two, and only inside the peek /
chips: **LOW DATA / pending** (the `◌ Churn — 12d` chip, :395, and the "first observed …" pop Δ,
:393) and the **current-month-provisional** hollow dot (:271). **LOCKED**, **UNDEFINED window** and
**UNSTABLE FIT** never appear on Home. A rebuild still needs render paths for them wherever a chip or a
peek metric can fall below its floor.

---

## 5. Interactions

| # | Control | Event | Effect |
|---|---|---|---|
| 1 | Ticker marquee | hover | pauses the scroll animation (:66) |
| 2 | Window `<select>` | change | `win = e.target.value` (:619) → `tickerItems(win)` re-renders all 16 stats (:622) |
| 3 | Watchlist tab | click | `tab = i`, `focusIdx = -1`, `peekId = null` (:599). **`menuIdx` is not cleared** — see §7 |
| 4 | `+ new list` | click | nothing — not wired (:91) |
| 5 | Column resize grip | mousedown | `preventDefault` + `stopPropagation` (so it never starts a drag or a sort), captures `startX` and current width, attaches `mousemove`/`mouseup` on `document`, sets `document.body` `cursor: col-resize` and `userSelect: none` (:332–345) |
| 6 | Column resize | mousemove | new width `= clamp(36, 420, startW + dx)` (:337) — live, per-frame |
| 7 | Column resize | mouseup | detaches both document listeners and restores `body.cursor` / `body.userSelect` (:340). **Width is not persisted** |
| 8 | Row thumbnail | mouseenter | opens the 164×226 hover preview at `x = thumbRect.right + 10`; `y = clamp(minY, maxY, thumbCentreY − 113)` where `minY = max(8, tabStripBottom + 4)` and `maxY = min(innerHeight − 234, sectionBottom − 230)` (:559–568). Net effect: vertically centred on the row but never over the tab strip and never past the bottom of the list |
| 9 | Row thumbnail | mouseleave | `pv = null` (:621) |
| 10 | Row body | click | `peekId = id`, `focusIdx = index`, `menuIdx = null` (:573) → peek opens |
| 11 | Row | dragstart | `effectAllowed='move'`, `dataTransfer['text/plain'] = index`, sets `dragIdx`, and **closes both the menu and the peek** (:577) |
| 12 | Row | dragover | `preventDefault`, `dropEffect='move'`, sets `overIdx` only when it changes (:578) |
| 13 | Row | drop | `preventDefault` + `stopPropagation`; splices the dragged id out and re-inserts it **at the hovered index** — `ids.splice(to, 0, moved)` — then clears `dragIdx`, `overIdx` and resets `focusIdx = -1` (:579–584). No above/below midpoint logic: the indicator always draws on top, and a downward move lands *at* the target index |
| 14 | Row | dragend | clears `dragIdx` / `overIdx` (:585) |
| 15 | ⋯ button | click | `stopPropagation` (so the row does not open), then **toggles**: `menuIdx = (menuIdx === i ? null : i)` (:587) |
| 16 | Row menu | mouseleave | closes the menu (:124 → `row.closeMenu`) |
| 17 | Anywhere | click | document-level handler closes an open menu **unless** the click landed inside `[role="menu"]` or on an element whose `aria-label === "Row actions"` (:539–544). The second guard is what makes the ⋯ toggle work instead of double-firing |
| 18 | `Open full chart` | click | `stopPropagation`, `location.href = 'Cardstock Charts.dc.html'` (:589) — a full navigation, not an SPA transition |
| 19 | `Open card page` | click | `stopPropagation`, `location.href = 'Cardstock Card.dc.html'` (:590) |
| 20 | `Add to binder` / `Move to list…` / `Remove from watchlist` | click | closes the menu; **no other effect** (:127, :129, :130) |
| 21 | Feed row | click | `peekId = f.id` (:608). Note it does **not** set `focusIdx`, so arrow keys afterwards resume from wherever `focusIdx` was — a peek opened from the feed is not linked to the watchlist cursor |
| 22 | Peek `✕` | click | `peekId = null` (:615) |
| 23 | Peek `edit →` / `Open full chart →` | click | navigate to `Cardstock Charts.dc.html#signals` (:257) / `Cardstock Charts.dc.html` (:296) |
| 24 | Peek `Card page` / `Edit signals` | click | nothing — not wired (:297–298) |
| 25 | Peek trigger triangle / current-month dot | hover | native SVG `<title>` tooltip (:269, :271) — browser-timed, not a styled tooltip |
| 26 | `Performance →` | click | `Cardstock Binder.dc.html#performance` (:172) |

### 5.1 Keyboard (document-level, :519–545)

Registered on `document` in `componentDidMount`, removed in `componentWillUnmount` (:546).

| Key | Guard | Effect |
|---|---|---|
| any | `target` is `<input>` or `<textarea>` | handler returns immediately — **except** `Escape`, which blurs the field (:522) |
| `Escape` | menu open | closes the menu only, leaves the peek (:523) |
| `Escape` | no menu | closes the peek (`peekId = null`). Does **not** clear `focusIdx` or the hover preview |
| `ArrowDown` / `ArrowUp` | — | `preventDefault`; moves `focusIdx` by ±1, clamped to `[0, ids.length − 1]` of the **current tab** (:524–531). From the initial −1, *either* arrow lands on row 0 |
| `ArrowDown` / `ArrowUp` | peek already open | the peek follows the cursor: `peekId = ids[newIndex]` (:530) |
| `Enter` | `focusIdx >= 0` | opens the peek for the focused row (:533–536). With `focusIdx === -1` it does nothing |
| `/` | focus not already in an input/select/textarea | focuses the nav search (`cardstock-search.js:76`) |
| `Escape` | inside search with a value | clears and blurs the search (`cardstock-search.js:77`) |

Not implemented despite the spec's global map (`CARDSTOCK_UI_SPEC_v1.md:129`): `o`, `t`, `?`.

### 5.2 Accessibility affordances present

- `role="button" tabindex="0"` on watchlist rows (:104) and feed rows (:155) — **but neither has a key
  handler.** Tabbing to a row and pressing Enter runs the *document* handler, which reads `focusIdx`,
  not the DOM-focused element. If the user tabbed rather than arrowed, `focusIdx` is −1 and nothing
  opens. This is a real defect, not a stylistic choice.
- `role="menu"` / `role="menuitem"` on the row menu (:124–130) with no roving tabindex, no arrow-key
  navigation between items, and no focus move into the menu on open.
- `role="dialog" aria-label="Card peek"` on the peek (:231) — **no focus trap, no initial focus move,
  no focus restoration on close.**
- `aria-label` on: the ticker window select, row-actions buttons, the peek close button, the nav account
  link, the logo link, and every `<section>` (`Watchlist`, `Screen activity`, `Binder P&L`).
- `aria-hidden="true"` on: the duplicated ticker copy (:72), the sparkline SVGs (:115, :218), the logo
  glyph (:41).
- Global `*:focus-visible` ring: 2px `--acc`, 1px offset, 2px radius (:21).
- Every interactive control carries a `title` (the convention in `HANDOFF.md:153`) — these are **native
  browser tooltips**, not a styled component.

---

## 6. Rules and invariants

A rebuild must preserve all of these.

**Ticker**
1. Two identical copies of the stat list, the second `aria-hidden="true"`, translating 0 → −50% — that is
   what makes the loop seamless (:66–76).
2. `INDEX` and `NEW 12M HIGHS` are computed on 30 days regardless of the window selector and must carry a
   visible `30d` tag (§3.2). This is the monthly-data honesty rule; do not let them re-window.
3. 24px linear-gradient fade masks on both edges of the marquee, `pointer-events: none` (:78–79).
4. The select's authored option order is `30d, 7d, 90d` — default first, then ascending (:60–62).

**Watchlist**
5. Row identity is **card + tier**, one row each (`HANDOFF.md:155`); the tier column and every peek
   section header restate that tier.
6. Chips are the row's **tracked** signals and **all** render, including quiet and pending — unlike the
   Card page header, which shows firing chips only (`DISPLAY_VOCABULARY.md:7–8`). Glance rule: coloured =
   hit, grey = nothing to report.
7. Colour never carries meaning alone: every state pairs a hue with a glyph (▲ ▼ – ◌ ◆). Colour-blind
   mode swaps hue only (:27, `HANDOFF.md:150`).
8. All numbers are JetBrains Mono — prices, percentages, counts, timestamps, tab counts, footer counts.
9. Money renders as whole dollars with `en-US` grouping, no cents (:433).
10. Negative percentages use **U+2212 MINUS**, not a hyphen (:479).
11. Column widths clamp to `[36, 420]` px (:337).
12. Resize grips `preventDefault` + `stopPropagation` on mousedown so a resize never becomes a row drag
    or a row click (:334).
13. The row-actions menu closes on **all** of: outside click, mouse-leave, `Escape`, drag start, and row
    open. The outside-click guard must exempt `[role="menu"]` and `aria-label="Row actions"`
    (:539–544) — without the second exemption the toggle cancels itself.
14. Drag reorder mutates the list array in place (:582), so the new order survives tab switches within
    the session — but **not** a reload.
15. The column header row sticks at `top: 48px`, exactly the nav height (:93).
16. Editing tracked signals is **not** done on Home. Both entry points (`⋯ → Open full chart`, peek
    `edit →`) deep-link to Charts, which is the editor (`DESIGN_NOTES.md:112–116`).

**Persistence**
17. `localStorage` is read for exactly two keys, both pre-paint: `cardstock-theme === 'dark'` and
    `cardstock-cvd === '1'` (:35). Nothing on Home is written to `localStorage`.
18. Column widths, active tab, drag order, peek state and the ticker window are **all in-memory** and
    reset on reload. If a rebuild adds persistence for any of them, that is a new decision, not a port.
19. `PAL` is computed once at class-field initialisation (:323) and is not reactive — a theme change made
    elsewhere requires a reload for the JS-computed colours to follow.

**Copy**
20. No exclamation marks, no hype, precise numbers over adjectives (`HANDOFF.md:45`).
21. Tooltips explain **consequence**, not identity (`HANDOFF.md:153`) — reproduce the exact strings in
    §3, they are the design.
22. `Worst position`, never "Laggard" (`DESIGN_NOTES.md:30`).
23. The peek's current-month dot tooltip reads exactly `current month still revising` (:271); the
    current month is never projected (`DESIGN_NOTES.md:49`).

**Motion**
24. `peekIn` animates **transform only** — animating opacity freezes the panel at 0 in this runtime
    (`DESIGN_NOTES.md:37`). Keep it transform-only regardless of framework.
25. `prefers-reduced-motion: reduce` collapses every animation duration to `0.01ms` (:25).

---

## 7. Open questions

1. **Route.** `/` (`HANDOFF.md:71`) or `/home` (`CARDSTOCK_UI_SPEC_v1.md:111`)? The marketing Landing page
   also claims `/` (`HANDOFF.md:83`). Both cannot be right; the prototype does not decide.
2. **DOM order vs visual order.** Should the watchlist stay first in the DOM (and therefore first for
   keyboard and screen readers) while painting second, or should the markup match the visual order?
   `order: 1/2` (:85, :147) reads like a late layout change rather than an a11y decision.
3. **Menu index survives a tab switch.** `pick` clears `tab`, `focusIdx` and `peekId` but not `menuIdx`
   (:599). Switching from a 3-row list with the menu open on row 1 to an 8-row list leaves the menu open
   on the *new* row 1. Intended, or an oversight?
4. **`focusIdx` is index-based, not id-based.** After a drag (`focusIdx = -1`, :583) or a tab switch the
   cursor resets; and a peek opened from the feed leaves `focusIdx` stale, so the next arrow press jumps
   somewhere unrelated (:608). Should the cursor track a card id instead?
5. **Enter on a tab-focused row does nothing** when the user never used the arrow keys (§5.2). Should
   rows carry their own `keydown`, or should focus and `focusIdx` be unified?
6. **Peek z-index.** The duplicated `z-index` declaration resolves to 10, below the row menu's 40
   (:124, :231). Which was intended?
7. **Reduced motion.** `animation-duration: 0.01ms` on an infinite marquee is a stop, not a designed
   alternative. `CARDSTOCK_UI_SPEC_v1.md:365` promises "peek slide → fade" — but fade is forbidden here
   (`DESIGN_NOTES.md:37`). What is the actual reduced-motion design for the ticker and the peek?
8. **Ticker VENUE buckets.** The stat renders `ebay / auction / tcgp`, which is neither the source enum
   (`DISPLAY_VOCABULARY.md:61`) nor a documented rollup. Which sources fold into "auction", and is `tcgp`
   an approved abbreviation of `tcgplayer`?
9. **Header meta is hardcoded** (:152). "7 since your last visit" implies a per-user `last_seen_at`, and
   "1 unlock" implies unlock rows are counted separately from screen rows. Neither rule is defined, and
   the prototype never recomputes them.
10. **Peek axis labels are literals** (`Aug '25` / `Jul '26`, :265–266) while the y-axis is derived. Are
    the x labels meant to be the window bounds, or fixed to a rolling 12M?
11. **Seed self-contradiction worth resolving in production.** The `iono` card's pop Δ says "first
    observed 2026-07-30" (:393) yet its peek lists five sales dated Jul 02–Jul 28, 2026 (:482) — the
    sales table is generated from a fixed date array for every card. What does "Last 5 sales" render for
    a card with fewer than five post-floor sales, or none?
12. **Where does the peek's tier ladder come from** now that the canonical scale is 19 values and the
    Card page strip was ruled PSA-only with 5 tiles (`DESIGN_NOTES.md:59`)? See §8, row 9.
13. **Sparkline signal markers.** `DISPLAY_VOCABULARY.md:73` specifies a marker set for watchlist and
    peek sparklines (▲ ▼ hollow ◌ amber tick). The watchlist sparkline (:115) draws **no** markers; only
    the peek's larger chart does. Is the row sparkline meant to be marker-free?
14. **Multi-card selection, sorting, and virtualization** are entirely absent. Are watchlist rows meant
    to be sortable by column, given the headers are resizable but not clickable?

---

## 8. Contradictions found

| # | Claim | Source doc:line | What the HTML actually does |
|---|---|---|---|
| 1 | Home is two columns: watchlist a ~58% **left** column; signals feed the **right** column; binder card **below the feed** | `uploads/CARDSTOCK_UI_SPEC_v1.md:157–159` | Two stacked full-width rows. Row 1 = Screen activity + Binder as a `3fr / 2fr` grid (:147); Row 2 = Watchlist at full width (:85). `DESIGN_NOTES.md:25` agrees with the HTML; the spec is stale |
| 2 | A full-width one-line **market-index strip**: index 30d %, Vintage/Modern segment %, "sets →" link, optional sparkline | `uploads/CARDSTOCK_UI_SPEC_v1.md:156` | A 36px **scrolling ticker**: `MARKET` label + 7d/30d/90d select + 16 stats (:56–81, :434–472). No "sets →" link, no Modern segment (VINTAGE appears as "% of $ vol"), no sparkline |
| 3 | Feed header links `All signals →` to Alert Center history | `uploads/CARDSTOCK_UI_SPEC_v1.md:158` | Header meta reads `7 since your last visit · 1 unlock · your screens →`, linking to the Screener (:152). Alerts were cut wholesale (`HANDOFF.md:97`) — the spec line is superseded |
| 4 | Feed rows include **per-card tracked-signal threshold crossings** | `uploads/CARDSTOCK_UI_SPEC_v1.md:158` | Feed data contains only screen ENTER, screen EXIT and product-unlock rows (:416–424), matching `DISPLAY_VOCABULARY.md:58`. No crossing rows |
| 5 | Peek panel is "right **column** width" and becomes a full-height drawer only at 1024–1279px | `uploads/CARDSTOCK_UI_SPEC_v1.md:160, :287, :354` | A fixed 480px right-edge drawer at **every** width, `top: 96px` to `bottom: 16px` (:231). The file contains no breakpoints at all — the only media query is `prefers-reduced-motion` (:25) |
| 6 | Peek panel is **focus-trapped**; Esc **restores focus to the origin row** | `uploads/CARDSTOCK_UI_SPEC_v1.md:287, :359` | `role="dialog"` with no focus trap, no initial focus move, no restoration. Esc only nulls `peekId` (:523). Rows are `role="button" tabindex="0"` with no key handler (:104) |
| 7 | Global keyboard map includes `o` (open full page from peek), `t` (terminal/binder toggle), `?` (show map) | `uploads/CARDSTOCK_UI_SPEC_v1.md:129` | Only `/`, `Esc`, `↑/↓`, `Enter` exist (:519–537 + `cardstock-search.js:76`). The on-screen legend advertises exactly those: `↑↓ rows · Enter peek · / search` (:143) |
| 8 | Empty ("Add your first card — try the screener →" / "Log your first purchase" / "Nothing watched yet. Find a candidate in the Screener →"), loading (skeleton rows, module-by-module) and error (per-module inline retry) states | `uploads/CARDSTOCK_UI_SPEC_v1.md:163, :429` | None exist. Zero occurrences of skeleton/loading/empty/retry copy or guards in the file (grep). Only `hint-placeholder-count` streaming hints, which are runtime scaffolding |
| 9 | Grade scale is the canonical **19 values**, "Ungraded" renamed "Raw"; the Card-page strip is PSA-only, 5 tiles, **Grade 9.5 dropped** | `HANDOFF.md:106`, `DISPLAY_VOCABULARY.md:64`, `DESIGN_NOTES.md:59` | The peek still uses the pre-ruling **six-tier ladder** `['Raw','Grade 7','Grade 8','Grade 9','Grade 9.5','PSA 10']` (:425). Worse, the highlight test is a string equality against the card's tier (:511), and cards are seeded as `PSA 9` / `PSA 8` (:371, :381) — which never match `Grade 9` / `Grade 8`, so those cards highlight **no** row |
| 10 | "Theme, colorblind mode, and **density** persist per device" | `HANDOFF.md:156` | Only theme and CVD are read from `localStorage` (:35, :323). No density control exists on Home, and column widths, active tab and drag order are in-memory only (:331) — all reset on reload |
| 11 | AsOfStamp removed app-wide; **"footers say 'refreshed just now'"** | `HANDOFF.md:99` | The Home footer says `101,882 cards · 4.2M sales observed` (:312). The string "refreshed" appears nowhere except inside the Screen-activity title tooltip (:151); there is no staleness stamp on this page at all |
| 12 | Watchlist table is **virtualized** | `uploads/CARDSTOCK_UI_SPEC_v1.md:157` | Plain `sc-for` over the full list (:103); no windowing, no scroll container of its own — the page scrolls |
| 13 | Watchlist art hover = "**hover scale 3.4×**" | `DESIGN_NOTES.md:29` | Not a scale transform. A separate fixed 164×226 panel is positioned beside the row and clamped to the list bounds (:315, :559–568). The *ratio* is right (164/48 ≈ 3.42), the mechanism is not — a rebuild following the note would produce different behaviour (the note's version would clip and would not clamp) |
| 14 | Peek buttons are labelled `Open full chart · Open card page · **Edit tracked signals**` | `uploads/CARDSTOCK_UI_SPEC_v1.md:160` | Labels are `Open full chart →`, `Card page`, `Edit signals` (:296–298), and the last two are not wired |
| 15 | Data-source note: sparklines come from "the card's primary tier"; peek shows "pop Δ **60d**" | `uploads/CARDSTOCK_UI_SPEC_v1.md:160–161` | Consistent in shape, but the peek's pop Δ label is just `Population Δ` with the window carried **inside the value** (`+1.8% (60d)`, :358) — and one seeded card replaces the number with a prose not-yet-available sentence (:393), a state the spec does not describe |
