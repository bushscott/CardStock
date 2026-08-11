# Screen spec — Set

**Authority:** extracted from `CardStock Mockup/Cardstock Set.dc.html` (242 lines), read directly 2026-08-10.
The prototype is Tier 1 (`CLAUDE.md` "Document authority"). Every claim below cites a line in that file
unless it is explicitly labelled as coming from a derived document. Where a derived document disagreed,
the HTML won and the disagreement is recorded in §8.

Seeded data (14 cards, 12 index points) is **illustrative**. What is authoritative is the structure,
the state space, and the copy.

---

## 1. Identity

| | |
|---|---|
| **Screen label** | `Set` — `data-screen-label="Set"` (`:35`) |
| **Prototype** | `CardStock Mockup/Cardstock Set.dc.html` |
| **Route** | **Unresolved by the HTML.** The file is static and hard-codes one set. `HANDOFF.md:78` says `/set/{id}`; `uploads/CARDSTOCK_UI_SPEC_v1.md:203` says `/set/{slug}`. See §7 and §8. |
| **Nav section** | Browse — the `Browse` nav link carries the active treatment (weight 600, ink colour, 2px accent underline) at `:47`; all other nav links are muted weight 500 |
| **Entry points** | Browse set tiles (`Cardstock Browse.dc.html:172–181`, every tile `href` → this page) and nav search "Sets" group (`cardstock-search.js:39`) |
| **Breadcrumb** | `Browse › Evolving Skies` — `Browse` links to `Cardstock Browse.dc.html`, the set name is plain ink (`:58`) |
| **Purpose** | One set as an investable universe: identity, the set's own price index, and a sortable roster of its cards. Derived-doc wording: "one set as an investable universe" (`uploads/CARDSTOCK_UI_SPEC_v1.md:205`). |
| **Props** | None. `data-props=""` (`:144`); `DESIGN_NOTES.md:141` records that the last prop (`demoMode`) was deleted 2026-08-10. |

---

## 2. Layout

Vertical stack, `min-height: 100vh`, `flex-direction: column`, base font-size 15px (`:35`).

1. **Nav** (`:37–52`) — 48px, sticky, `z-index: 20`. Logo + wordmark → Home; five section links (Home, Screener,
   Charts, Binder, Browse); flex spacer; `<cardstock-search>`; 28px circular account avatar → Profile. Shared
   chrome, identical on every app page (`HANDOFF.md:88`).
2. **Accent bar** (`:54`) — 4px full-bleed strip, `linear-gradient(90deg, #2B2D42, #5C6B9E, #7E6BA8)`. **Hard-coded,
   not data-bound.** Three stops; the first two are Umbreon's accent pair, the third (`#7E6BA8`) is the Espeon/Rainbow
   purple used in `CARDS` (`:159`, `:151`-analogue).
3. **Main** (`:56`) — `max-width: 1480px`, centred, `padding: 14px 20px 28px`, `gap: 14px` between children.
4. **Breadcrumb** (`:58`).
5. **Header card** (`:60–81`) — white card, 10px radius, 16px pad, single flex row:
   - *left block* (`:61–67`): H1 set name (Inter Tight 700, 26px) + set-code chip; sub-line with card count and
     first-observation date.
   - *flex spacer* (`:68`).
   - *right block* (`:69–80`): 220px-wide set-index sparkline with caption, then a right-aligned two-line stack of
     30D and 90D percentages.
6. **Toolbar** (`:83–96`) — density segmented control (terminal | binder), the literal word `sort`, four sort pills,
   flex spacer, right-aligned shown-count.
7. **Exclusion banner** (`:98–100`) — conditional, amber, full width. Sits **above both views**, so it is visible in
   binder density too.
8. **Roster — terminal density** (`:102–120`) — card-surface section: sticky-styled header row + data rows, both on
   the same CSS grid template.
9. **Roster — binder density** (`:122–137`) — `repeat(auto-fill, minmax(180px, 1fr))` tile grid, 12px gap.
10. **Footer note** (`:139`) — muted 12.5px methodology line.

Exactly one of (8) and (9) renders; they are mutually exclusive `sc-if` blocks keyed on `isTerm` / `isBind`
(`:102`, `:122`).

---

## 3. Data contract

### 3.1 Set header

| Field | Rendered as | Line | Backing today |
|---|---|---|---|
| Set name | H1, Inter Tight 700 26px | `:63` | `sets.name` — **backed** (`../PokemonInvestBatch/DATA_MODEL.md:143`) |
| Set name (breadcrumb) | trailing crumb, ink | `:58` | same |
| **Set code** (`SWSH07`) | mono 11.5px chip, uppercase-look, `mutbg` fill, 1px border, 3px radius, letter-spacing 0.06em | `:64` | **NO BACKING DATA.** `sets` has exactly six columns — `id, slug, name, discovered_at, last_seen_at, last_walked_at` (`DATA_MODEL.md:141–146`). No set code. Requires the set-metadata table. |
| Tracked card count (`237`) | mono inline number inside the sub-line | `:66` | `count(cards WHERE set_id = …)` — **backed**, with the caveat that `cards.set_id` is never updated if a card moves sets (`DATA_MODEL.md:157`; spec calls this "accepted, invisible to users", `CARDSTOCK_UI_SPEC_v1.md:208`) |
| First-observation date (`Dec 2021`) | mono inline, copy is literally "first sale observed" | `:66` | **SEMANTICALLY UNBACKED.** The per-sale ledger begins at each card's first crawler visit, late Jul 2026, ragged per card (D-001, `DECISIONS.md:22`). No sale can be observed from 2021. Only `price_months` reaches back (~Dec 2020, D-002 / `DECISIONS.md:37`). Either the copy must change to "first price observed" or the field must change. |
| Set-index sparkline | `<polyline points="{{ idxPts }}">`, 1.8px accent stroke, `vector-effect="non-scaling-stroke"`, `viewBox="0 0 220 52"`, `preserveAspectRatio="none"`, 52px tall | `:71–73` | **NO BACKING DATA.** No index table of any kind exists in the scraper (D-004, `DECISIONS.md:61`). |
| Sparkline caption | `set index · 12M`, 11px muted, centred | `:74` | copy constant |
| 30D change (`+4.1%`) | mono 15px 700, colour `var(--pos)` | `:77` | **Static literal in the template — not a binding.** Requires a set index (D-004). Cross-check: Browse's Evolving Skies tile carries `chg: 4.1` (`Cardstock Browse.dc.html:176`). |
| 90D change (`+9.7%`) | mono 15px 700, colour `var(--pos)` | `:78` | same — static literal, requires a set index |
| `30D` / `90D` window labels | 11.5px 600 muted, inline after the number | `:77–78` | copy constants |

**Index series shape.** `this.IDX` is 12 numbers (`:172`), one per month, base-100 style (`100 … 118`).
Projection (`:197`): `x = i / (n − 1) × 220`, `y = 48 − (v − min) / (max − min) × 42`. So the polyline occupies
y ∈ [6, 48] inside a 52-unit box, min-max normalised per series — **not** a fixed y-scale. A flat series
(`min === max`) divides by zero; see §7.

### 3.2 Toolbar

| Field | Rendered as | Line | Backing |
|---|---|---|---|
| Density buttons | two 28px mono buttons in a bordered 6px-radius group, `terminal` then `binder` | `:85–86` | client state |
| `sort` label | literal lowercase word, 12.5px muted | `:88` | copy constant |
| Sort pills | 26px mono buttons, 5px radius, from `sorts` (`:90–92`) | `:202–205` | client state; see 3.5 |
| Shown count | mono 12.5px, `"{n} of 237 cards"` | `:95`, `:206` | numerator = rows rendered; denominator = tracked card count |

### 3.3 Exclusion banner

| Field | Value | Line |
|---|---|---|
| Visibility flag | `hasExcluded = excluded > 0 && sk === 'pop'` | `:207` |
| Copy | `"{n} cards excluded from this sort — pop Δ 60d needs two census observations and their first was Jul 2026. They'll join the sort next census."` | `:208` |
| Styling | `rgba(176,127,26,0.06)` fill, `rgba(176,127,26,0.25)` border, 8px radius, 12.5px, `var(--warnInk)` | `:99` |

The date `Jul 2026` is hard-coded in the copy. In production the census seam is **per-card and ragged**
(D-001), so this sentence cannot be a single global string.

### 3.4 Roster row — every field

Source record shape (`this.CARDS`, `:157–171`): `{ name, price, roc, rs, pop, vol, acc[2] }`. `pop` is nullable
(`:165`, `:168` — `pop: null`). Every other field is non-null in the seed.

| Column | Header | Header tooltip | Cell format | Cell line | Backing today |
|---|---|---|---|---|---|
| Card | `Card` | `Card name` | 14px 500, centred, ellipsised; whole name is an `<a>` → Card page, ink colour, hovers to accent | `:111` | `cards.name` — **backed** (`DATA_MODEL.md:159`) |
| PSA 10 | `PSA 10` | `Latest monthly PSA 10 price — click to sort` | `money()` = `'$' + Math.round(n).toLocaleString('en-US')` (`:187`), mono 13.5px 700 | `:112` | `price_months` where `tier = Psa10`, latest per (card, month) by `max(observed_at)` — **backed** (`DATA_MODEL.md:181–191`) |
| ROC 3M | `ROC 3M` | `3-month rate of change — click to sort` | `pct()` = sign + `abs(n).toFixed(1)` + `%`; **negatives use U+2212 MINUS, not hyphen** (`:188`); mono 13px; colour `PAL.pos` if `roc ≥ 0` else `PAL.neg2` (`:224`) | `:113` | derivable from `price_months`; change-only storage means "unchanged" months have no row (`CLAUDE.md:53`) |
| RS pct | `RS pct` | `Relative strength vs market index, percentile — click to sort` | `String(rs) + 'th'` (e.g. `94th`), mono 13px, ink | `:114`, `:225` | **NOT BACKED.** Requires a market index (D-004). |
| Pop Δ 60d | `Pop Δ 60d` | `PSA 10 census growth over 60 days — click to sort` | `null → '—'` (U+2014) else `'+' + n.toFixed(1) + '%'`; **always signed `+`, there is no negative branch** (`:226`) | `:115` | `populations` deltas via `LAG(…)` (`DATA_MODEL.md:197`), but census history starts at each card's first visit, late Jul 2026 (D-001) and D-033 floors post-seam metrics at 2026-09-01 (`DECISIONS.md:309`) — **today no card qualifies** |
| — | — | — | Pop Δ colour: `null → PAL.mut3`; `≥ 5 → PAL.neg2`; else `PAL.mut` (`:227`). Note the polarity: **high census growth is rendered as negative**, supply flooding the market. | `:115` | — |
| — | — | — | Pop Δ per-cell `title`: null → `Census too young — first observation Jul 2026, deltas begin next census`; else `+{n}% PSA 10 census growth over 60 days` (`:228`) | `:115` | — |
| Sales / mo | `Sales / mo` | `Observed sales per month, all tiers` | `String(vol)`, mono 13px, muted | `:116`, `:229` | `sales` table — **backed in shape**, but the ledger begins late Jul 2026 (D-001), so ~2 weeks of history exists as of 2026-08-10 |

Every header cell also carries a **resize grip**: a `│` glyph, `cursor: col-resize`, `title="Drag to resize"`,
colour `var(--line3)` hovering to accent (`:106`).

### 3.5 Sort model

`sorts` (`:202`) — four pills, in this order:

| Pill label | Sort key | Column it drives | Column header key |
|---|---|---|---|
| `value` | `value` | PSA 10 price | `price` |
| `ROC 3M` | `roc` | ROC 3M | `roc` |
| `RS` | `rs` | RS pct | `rs` |
| `pop Δ` | `pop` | Pop Δ 60d | `pop` |

Note the label drift: pill `value` ↔ column `PSA 10`; pill `RS` ↔ column `RS pct`.
Pill tooltip: `Sort by {label}`, plus ` — click again to reverse the order` **only when that pill is active** (`:203`).

`val()` (`:191`): `roc → c.roc`, `rs → c.rs`, `pop → c.pop`, **default → c.price** (so `value` means price).
Comparator (`:194`): `(dir === 'asc' ? 1 : -1) × (val(a) − val(b))` — numeric only, no tiebreaker, no name sort.

### 3.6 Binder tile

| Field | Rendered as | Line |
|---|---|---|
| Art | `<image-slot shape="rounded" radius="5" placeholder=" ">` in a `325 / 450` aspect box | `:126–127` |
| Slot id | `'art-' + name.toLowerCase().replace(/[^a-z0-9]+/g, '-')` | `:235` |
| Thumb background | `linear-gradient(160deg, {acc[0]}, {acc[1]})` — two hex accents per card | `:126`, `:234` |
| Name | 600 13.5px, single line, ellipsised | `:129` |
| Price | mono 13.5px 700, left of a space-between row | `:131` |
| ROC 3M | mono 12px, coloured pos/neg2 | `:132` |

The **whole tile** is an `<a>` → Card page (`:125`), 8px radius, hover raises `0 6px 20px rgba(20,19,26,0.10)`.
Binder tiles carry **no set/number/year subtitle** — unlike the Character page's tiles.

Card art: real photos exist on disk at `{ImageDirectory}/{hash}/1600.jpg`, joined via `cards.image_hash`
(~3.6 GB, D-010 `DECISIONS.md:83`). Licensing is the open risk, not availability (D-011).
The two-colour accent per card is **not backed** — it needs a `card_accents`-style derived table, and putting it
on `cards` would mean writing the scraper's table (flagged in `DECISIONS.md:201`).

### 3.7 Footer

`Showing the set's most-traded cards · prices are latest monthly PSA 10 · full roster ships with the real corpus`
(`:139`, 12.5px muted). Note the typographic apostrophe (U+2019).

---

## 4. States

### 4.1 Density (mutually exclusive, exhaustive)

| State | Trigger | Effect |
|---|---|---|
| **terminal** (default) | initial `state.view = 'terminal'` (`:174`); click `terminal` (`:85`, `:199`) | Table renders (`:102`), tile grid hidden. Button pair: active = accent bg / card fg; inactive = card bg / muted fg (`:200–201`) |
| **binder** | click `binder` (`:86`, `:199`) | Tile grid renders (`:122`), table hidden |

Density lives in component state only — it is **not persisted**. Only theme and CVD read `localStorage` (`:33`).

### 4.2 Sort (8 states: 4 keys × 2 directions)

| State | Trigger |
|---|---|
| key ∈ {value, roc, rs, pop} | click that sort pill (`:204`) **or** the matching column header (`:220`) |
| dir = `desc` | default for any *newly selected* key — both handlers reset to `desc` on key change (`:204`, `:220`) |
| dir = `asc` | click the **already-active** pill or header a second time (toggles) |

Initial state: `sort: 'value'`, `sortDir: 'desc'` (`:174`).
Active column shows `▾` (U+25BE, desc) or `▴` (U+25B4, asc) appended to its header text (`:219`). **Sort pills show
no arrow** — direction is only legible from the table header, which does not exist in binder density.

### 4.3 Sufficiency exclusion

| State | Trigger | Effect |
|---|---|---|
| **No exclusion** | `sort ≠ 'pop'`, or every card has non-null `pop` | Banner hidden. All rows render; null `pop` cells still show `—` |
| **Exclusion active** | `sort === 'pop'` **and** ≥1 card has `pop == null` (`:192–193`, `:207`) | Cards with null `pop` are dropped from `rows` **and** from `tiles` (both derive from `sorted`, `:222`/`:231`); amber banner appears above the roster; `shownCount` numerator falls accordingly |

This is the screener's sufficiency-exclusion pattern, but the HTML implements it for **exactly one metric**
(`pop`) — `val()`/`included` special-case only that key (`:191–192`). RS pct, which equally depends on data that
does not exist, has no exclusion path.

### 4.4 Per-cell states

| Cell | States |
|---|---|
| ROC 3M | `≥ 0` → pos colour + `+` prefix · `< 0` → neg2 colour + U+2212 prefix (`:188`, `:224`) |
| Pop Δ 60d | `null` → `—`, mut3 colour, "census too young" tooltip · `≥ 5` → neg2 colour · `0 ≤ n < 5` → mut colour (`:226–228`). **No negative-value rendering path exists** — the formatter always prefixes `+` |
| Header cell | sortable (has `c.s`) → click sets sort · non-sortable (`Card`, `Sales / mo`, `s: null`) → click is a **no-op `() => {}`** but the span still shows `cursor: pointer` and a tooltip with no "click to sort" suffix (`:211`, `:216`, `:220`) |

### 4.5 Column resize (transient)

Mouse-down on a header grip (`:106`) starts a drag (`:175–184`): captures `clientX` and current width, then on
`mousemove` sets `colW[key] = max(52, startW + dx)`; `mouseup` unbinds both listeners. Widths are per-column
component state, not persisted, and there is no keyboard or touch path.

### 4.6 METADATA PENDING — **absent**

There is **no METADATA PENDING state anywhere on this screen.** The `SWSH07` chip renders unconditionally as
static markup (`:64`) with no conditional wrapper, no placeholder, and no honesty badge. This directly contradicts
`Cardstock About Data.dc.html:115` ("Missing metadata renders as METADATA PENDING, not as a silent blank or a
guess") and the pattern `DESIGN_NOTES.md:70–71` describes for set metadata. Since era, release date, and set code
all come from a table that does not yet exist, **the pending state is exactly the one this screen needs and does
not have.** See §7.

### 4.7 States that do not exist in the prototype

Not implemented, and required before this ships: loading / skeleton, empty roster, set-not-found (404),
query error, negative set-index change (both 30D and 90D hard-code `var(--pos)`, `:77–78`), a flat index series,
a single-card set, and any per-metric LOW DATA / LOCKED / UNSTABLE FIT badge (the honesty vocabulary
`HANDOFF.md:43` says every metric must carry).

---

## 5. Interactions

| # | Control | Line | Consequence |
|---|---|---|---|
| 1 | Logo / wordmark | `:39` | → Home |
| 2 | Nav links ×5 | `:43–47` | → Home / Screener / Charts / Binder / Browse. `Browse` is the active tab |
| 3 | `<cardstock-search>` | `:50` | Shared typeahead. `/` focuses, Esc clears+blurs, fires at ≥2 chars, groups Characters (4) / Sets (4) / Cards (5) (`DISPLAY_VOCABULARY.md:194–195`) |
| 4 | Account avatar `O` | `:51` | → Profile; `title="Profile & settings"` |
| 5 | Breadcrumb `Browse` | `:58` | → Browse |
| 6 | `terminal` button | `:85` | `setState({view:'terminal'})`; tooltip "Terminal density — more rows, tighter type, every metric column" |
| 7 | `binder` button | `:86` | `setState({view:'binder'})`; tooltip "Binder density — fewer rows with card art" |
| 8 | Sort pill ×4 | `:91`, `:204` | Sets sort key; same key again flips direction. Re-sorts **both** table and tiles; may trigger/clear the exclusion banner |
| 9 | Column header text ×4 (`PSA 10`, `ROC 3M`, `RS pct`, `Pop Δ 60d`) | `:106`, `:220` | Identical effect to the matching pill; also moves the `▾`/`▴` arrow |
| 10 | Column header text ×2 (`Card`, `Sales / mo`) | `:211`, `:216` | **No-op.** Pointer cursor and hover-to-accent still fire — a false affordance |
| 11 | Resize grip ×6 | `:106`, `:175` | Drag adjusts that column's px width, floor 52px; `gridCols` recomputes (`:209`) |
| 12 | Row card name link | `:111` | → Card page (every row links to the same static prototype) |
| 13 | Binder tile | `:125` | → Card page; hover raises a shadow |
| 14 | Pop Δ cell hover | `:115`, `:228` | Native tooltip, branching on null vs value |
| 15 | Sort-pill hover | `:91`, `:203` | Native tooltip, branching on active vs inactive |

Accessibility as built: `*:focus-visible` gives a 2px accent outline (`:21`); `prefers-reduced-motion` caps
animation (`:23`). The table is a **CSS grid of `<div>`/`<span>`, not a `<table>`** (`:104`, `:110`) — no
`role="table"`, no `aria-sort`, no header/cell association. Sortable headers are `<span onClick>`, not buttons,
so they are not keyboard reachable. The resize grip has no keyboard equivalent.

---

## 6. Rules and invariants

1. **Exactly one density renders.** `isTerm` and `isBind` are complementary derivations of one enum (`:198`);
   they can never both be true or both false.
2. **One sorted list feeds both densities.** `rows` and `tiles` both map `sorted` (`:222`, `:231`), so density
   never changes ordering or membership.
3. **Sufficiency exclusion is membership, not blanking.** An excluded card disappears from the roster; it is not
   shown greyed. Exclusion applies only under `sort === 'pop'` (`:192`).
4. **A new sort key always starts `desc`.** Direction only toggles when the key is unchanged (`:204`, `:220`).
5. **Pop Δ has an inverted colour polarity.** Growth ≥ 5% is painted with the *negative* token (`:227`): more
   supply is bad news for a holder. This is deliberate and must not be "fixed" to the usual up-is-green rule.
6. **Money is rounded to whole dollars** and thousands-separated `en-US` (`:187`). Percentages are one decimal
   with a **Unicode minus** (`:188`).
7. **The shown-count denominator is corpus-wide, the numerator is not.** `"{sorted.length} of 237 cards"` (`:206`)
   compares rendered rows against tracked cards. With the seeded 14-card subset and a pop sort this reads
   `12 of 237 cards` while the banner says `2 cards excluded` — 12 + 2 ≠ 237. Against the real corpus the
   numerator must be "rows currently rendered" and the denominator "cards in the set", and the banner count must
   be measured against the same population.
8. **Card art is never load-bearing.** `placeholder=" "` plus the `::part(empty) { opacity: 0 }` rule (`:22`)
   means a missing image degrades to the accent gradient with no broken-image affordance.
9. **Column width floor is 52px** (`:179`); the name column is `minmax(Npx, 1.4fr)` while the other five are fixed
   px (`:209`) — only the name column absorbs slack.
10. **Prices are one tier only.** Everything on this screen is PSA 10 (`:212`, `:139`). The 19-value grade
    vocabulary is not exposed here.

---

## 7. Open questions

1. **Route.** `/set/{id}` (`HANDOFF.md:78`) or `/set/{slug}` (`CARDSTOCK_UI_SPEC_v1.md:203`)? `sets.slug` is unique
   and is the site's verbatim href key (`DATA_MODEL.md:142`), which favours slug, but this needs a ruling.
2. **Where do era and release date go?** Both derived docs put them in this header (`DESIGN_NOTES.md:72`,
   spec `:206`); the HTML shows neither. Is the HTML the deliberate final answer, or did the fields get dropped
   for lack of data? Note the Character page *does* render a Year column, so release date is needed regardless.
3. **What is `SWSH07`?** Set code / series code has no source. Is it hand-curated alongside era and release date
   in the set-metadata table, or parsed from `sets.slug`?
4. **"first sale observed Dec 2021" is not computable.** Should the copy become "first price observed"
   (backed by `price_months`, ~Dec 2020) or should the field be dropped?
5. **Set index definition.** No index exists (D-004). What is a "set index" — chained per-card monthly relatives
   with a min-active-count guard, as `CARDSTOCK_UI_SPEC_v1.md:382` sketches? What are its base, its rebalancing
   rule, and its own sufficiency floor? Which of 12M / 30D / 90D survive if the answer is "not enough months"?
6. **Negative index change.** Both header percentages hard-code the positive token. Confirm the negative
   treatment matches the row-level rule (`PAL.neg2`) rather than `--neg` / `--neg3`.
7. **Pop Δ 60d is unavailable for every card today** (census starts late Jul 2026, D-001; floor 2026-09-01, D-033),
   which means the exclusion banner would exclude 100% of the roster and the sort would render empty. Does the pill
   disable, does the whole column lock, or does the banner become a full-column LOCKED state?
8. **RS pct has no exclusion path** even though it depends on a nonexistent index. Should it get the same
   sufficiency treatment as pop Δ?
9. **RS window.** `DISPLAY_VOCABULARY.md:106` gives RS windows 1M · **3M**; the column header and tooltip
   name no window (`:214`). Which one does this column show?
10. **Roster membership.** The footer says "the set's most-traded cards … full roster ships with the real corpus"
    (`:139`). Is the shipped page the full roster (and the footer is prototype scaffolding), or is there a real
    top-N rule? If top-N, ranked by what — `vol`, `observed_sales_per_day`, price?
11. **Density persistence.** `DISPLAY_VOCABULARY.md:203` says density persists per device; the prototype does not
    persist it. Is persistence in scope for v1?
12. **Card number.** No card-number column exists on this roster, and `cards` has no number field — a number would
    have to be parsed out of `cards.name`/`cards.url`, and the printed-set-size denominator (the `203` in
    `215/203`) has no source at all. Sibling screens display it (`Cardstock Card.dc.html:66`,
    `Cardstock Charts.dc.html:77`). Should the Set roster carry it, and where would it come from?
13. **Accessibility.** Does the shipped table become a semantic `<table>` with `aria-sort`, and do sortable headers
    become buttons? Non-sortable headers must lose the pointer cursor.

---

## 8. Contradictions found

| Claim | Source doc:line | What the HTML actually does |
|---|---|---|
| Header shows "era/release/count" | `CardStock Mockup/DESIGN_NOTES.md:72` | Header sub-line is **only** `237 cards tracked · first sale observed Dec 2021` (`:66`). No era, no release date anywhere on the page |
| Header shows "name, era, release date, card count" | `uploads/CARDSTOCK_UI_SPEC_v1.md:206` | Same — name, set-code chip, count, first-observation date (`:63–66`) |
| "dominant-accent bar from top card's art" / "(Umbreon dark blues)" | `uploads/CARDSTOCK_UI_SPEC_v1.md:206`, `DESIGN_NOTES.md:72` | The 4px bar is a hard-coded three-stop literal `#2B2D42, #5C6B9E, #7E6BA8` (`:54`). Not data-bound, and the third stop is not part of the top card's accent pair (`:157`) |
| Route is `/set/{id}` | `CardStock Mockup/HANDOFF.md:78` | Conflicts with `/set/{slug}` at `uploads/CARDSTOCK_UI_SPEC_v1.md:203`. The HTML is a static file and settles neither |
| "sufficiency exclusions per sort metric as in the screener" | `uploads/CARDSTOCK_UI_SPEC_v1.md:208` | Exclusion exists for exactly one metric, `pop` (`:192`, `:207`). RS pct, equally unbacked, has none |
| "Missing metadata renders as METADATA PENDING, not as a silent blank or a guess" | `CardStock Mockup/Cardstock About Data.dc.html:115` | The `SWSH07` chip is unconditional static markup (`:64`). No pending state exists on this screen for any set-metadata field |
| "Density and theme choices persist per device (localStorage)" | `CardStock Mockup/DISPLAY_VOCABULARY.md:203` | Density is component state (`:174`); only theme and CVD read `localStorage` (`:33`). A reload returns to `terminal` |
| Set page displays a card-number denominator (e.g. `215/203`) | task brief; pattern exists at `Cardstock Card.dc.html:66`, `Cardstock Charts.dc.html:77`, `Cardstock Home.dc.html:356` | **No card number appears anywhere in `Cardstock Set.dc.html`.** The six roster columns are Card / PSA 10 / ROC 3M / RS pct / Pop Δ 60d / Sales / mo (`:211–216`) |
| Set page displays release year and era | task brief | Neither is rendered (`:60–81`). Year appears on the **Character** page instead (`Cardstock Character.dc.html:118`) |
| `sets` carries era / release date / set code | implied by `uploads/CARDSTOCK_UI_SPEC_v1.md:207` reading `sets` + `set_metadata` | `sets` has six columns only — `id, slug, name, discovered_at, last_seen_at, last_walked_at` (`../PokemonInvestBatch/DATA_MODEL.md:141–146`), confirming D-004 (`DECISIONS.md:61`) and D-042 (`DECISIONS.md:199`) |
| "first sale observed Dec 2021" | the HTML's own copy, `:66` | Contradicts D-001 (`DECISIONS.md:22`): the per-sale ledger starts at each card's first visit, late Jul 2026. The prototype's own copy is the unbacked claim here |

---

## 9. Non-scraped data this screen requires

**Set-metadata table** (release date + era for ~303 sets — `DECISIONS.md:199`):
`SWSH07` set-code chip (`:64`). Era and release date are *not* rendered by this prototype but are specified by both
derived docs and are needed by the Character page's Year column.

**Character-tag table** (card → Pokémon): not used by this screen.

**Computed / derived, also nonexistent:** the set index (sparkline, 30D, 90D — D-004), RS-vs-index percentile
(D-004), and the per-card two-colour accent used by binder tiles and the header bar (`card_accents`-style;
the "new column on `cards`" version is flagged as conflicting with D-026 at `DECISIONS.md:201`).
