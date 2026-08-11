# Screen spec — Character

**Authority:** extracted from `CardStock Mockup/Cardstock Character.dc.html` (215 lines), read directly 2026-08-10.
The prototype is Tier 1 (`CLAUDE.md` "Document authority"). Every claim below cites a line in that file unless it
is explicitly labelled as coming from a derived document. Where a derived document disagreed, the HTML won and the
disagreement is recorded in §8.

Seeded data (8 Umbreon printings) is **illustrative**. What is authoritative is the structure, the state space,
and the copy. Note in particular that the header aggregates (34 printings, 19 sets) describe a corpus the seeded
roster does not contain — that gap is itself part of the contract (§6.2).

---

## 1. Identity

| | |
|---|---|
| **Screen label** | `Character` — `data-screen-label="Character"` (`:35`) |
| **Prototype** | `CardStock Mockup/Cardstock Character.dc.html` |
| **Route** | **Contested.** `HANDOFF.md:78` says `/character/{name}`; `uploads/CARDSTOCK_UI_SPEC_v1.md:118` and `:210` say `/pokemon/{species}`. The HTML is static and settles neither. See §7 and §8. |
| **Nav section** | Browse — the `Browse` nav link carries the active treatment (`:47`) |
| **Entry points** | Browse "by pokémon" species grid (`Cardstock Browse.dc.html:60`, `:135–141`), nav search "Characters" group (`cardstock-search.js:37`), and the Card page's species link (`Cardstock Card.dc.html:66`) |
| **Breadcrumb** | `Browse › Umbreon` (`:58`) |
| **Purpose** | The species aggregate: every printing of one Pokémon across all sets and eras, with corpus-level totals. Spec wording: "every printing of one Pokémon; the page collectors bookmark" (`uploads/CARDSTOCK_UI_SPEC_v1.md:212`). |
| **Priority history** | Spec filed it P2 (`uploads/CARDSTOCK_UI_SPEC_v1.md:210`); it was built in v1 (`HANDOFF.md:108`, `DESIGN_NOTES.md:86`) |
| **Props** | None (`:132`, `data-props=""`); `DESIGN_NOTES.md:141` records the last prop was removed 2026-08-10 |

---

## 2. Layout

Vertical stack, `min-height: 100vh`, base font-size 15px (`:35`). Identical chrome to the Set page.

1. **Nav** (`:37–52`) — 48px sticky bar; logo → Home, five section links, search component, account avatar.
2. **Accent bar** (`:54`) — 4px strip, `linear-gradient(90deg, #2B2D42, #5C6B9E)`. **Two stops, hard-coded**
   (the Set page's equivalent has three, `Cardstock Set.dc.html:54`).
3. **Main** (`:56`) — `max-width: 1480px`, `padding: 14px 20px 28px`, `gap: 14px`.
4. **Breadcrumb** (`:58`).
5. **Header card** (`:60–77`) — flex row, 20px gap:
   - 64px **accent avatar circle** (`:61`) — `linear-gradient(160deg, #2B2D42, #5C6B9E)`, centred initial letter
     in Inter Tight 700 26px, `rgba(255,255,255,0.92)`.
   - Identity block (`:62–69`): H1 species name, then a wrapping row of Pokédex chips.
   - Flex spacer (`:70`).
   - **Stat strip** (`:71–76`) — four right-aligned stats, 22px gap, each an 11px uppercase label over a mono
     19px 700 value.
6. **Toolbar** (`:79–87`) — density segmented control (**binder first, then terminal**), a static caption,
   flex spacer, shown-count.
7. **Roster — binder density** (`:89–105`) — `repeat(auto-fill, minmax(180px, 1fr))` tile grid, 12px gap.
   **Declared first, and it is the default.**
8. **Roster — terminal density** (`:107–125`) — card-surface section, header row + data rows on one CSS grid.
9. **Footer note** (`:127`).

Exactly one of (7) and (8) renders — mutually exclusive `sc-if` blocks on `isBind` / `isTerm` (`:89`, `:107`).
**There is no chart, sparkline, or SVG of any kind on this screen.**

---

## 3. Data contract

### 3.1 Species header

| Field | Rendered as | Line | Backing today |
|---|---|---|---|
| Species name | H1, Inter Tight 700 26px | `:63` | External Pokédex schema (`DESIGN_NOTES.md:70`) — **not in the scraper's eight tables** |
| Species name (breadcrumb) | trailing crumb, ink | `:58` | same |
| Avatar initial | first letter of the species name, inside the 64px gradient circle | `:61` | derived from the name; the gradient itself is hard-coded |
| Pokédex chips | mono 11.5px 600, `mutbg` fill, 1px border, 3px radius, `cursor: help`, each with a `title` | `:65–67`, `:174–181` | External Pokédex schema — see 3.2 |
| **Printings** | label `PRINTINGS` (11px uppercase 600, letter-spacing 0.06em) over mono 19px 700 `34` | `:72` | Requires the **character-tag join table** (`card_characters`) — does not exist (`DECISIONS.md:199`) |
| **Sets** | same shape, `19` | `:73` | `count(DISTINCT cards.set_id)` over the species' cards — needs the same join table |
| **Total value** | same shape, `$96.4K` — note the compact `K` suffix, unlike the roster's full `$1,486` | `:74` | Sum of latest PSA 10 across all printings; `price_months` is backed, the species membership is not |
| **90d** | label `90D`, mono 19px 700, colour `var(--pos)` | `:75` | **NO BACKING DATA** — a character index does not exist (D-004, `DECISIONS.md:61`) |

All four stat values are **static literals in the template, not bindings** (`:72–75`). They match Browse's
Umbreon record exactly — `printings: 34, sets: 19, value: 96400, chg: 6.8` (`Cardstock Browse.dc.html:190`) —
so the two screens are already contracted to the same aggregate.

### 3.2 Pokédex chips — the full seeded set

Six chips (`:174–181`), rendered in this order. Note `hint-placeholder-count="5"` (`:65`) understates the actual
six — the hint is design-time placeholder scaffolding only (`support.js:614`), not a cap.

| Chip label | Tooltip | Underlying Pokédex field |
|---|---|---|
| `Dark` | `Pokédex type` | type — `Cardstock Browse.dc.html:190` `type: 'Dark'` |
| `Gen 2` | `First appeared in Generation 2 (Gold/Silver)` | generation — Browse `gen: 2` |
| `Stage 1` | `Evolution stage — evolves from Eevee` | evolution stage — Browse `stage: 'Stage 1'`. **The "evolves from Eevee" half is not in any data field** |
| `Black` | `Official Pokédex color` | colour — Browse `color: 'Black'` |
| `Field egg group` | `Pokédex egg group` | egg group — Browse `egg: 'Field'`; the label appends " egg group" |
| `Urban habitat` | `Pokédex habitat` | habitat — Browse `habitat: 'Urban'`; the label appends " habitat" |

**Not rendered although present in the Browse species record:** `status` (`Ordinary` / `Legendary`,
`Cardstock Browse.dc.html:190–192`). **Not rendered although listed as an available Pokédex field:** region and
evolution line (`DESIGN_NOTES.md:70`). Chip count is therefore variable-but-fixed-set: a species with dual typing
would presumably add a chip, but the HTML shows no multi-value branch.

Per `DESIGN_NOTES.md:70` and `HANDOFF.md:107`, the Pokédex schema is **external and pre-populated**, so species
attributes never render METADATA PENDING. That ruling covers the chips — it does **not** cover the Year column,
which comes from set metadata (3.4).

### 3.3 Toolbar

| Field | Rendered as | Line |
|---|---|---|
| Density buttons | two 28px mono buttons, `binder` **then** `terminal` (reverse of the Set page's order) | `:81–82` |
| Caption | static copy `every Umbreon printing we track, all eras`, 12.5px muted | `:84` |
| Shown count | mono 12.5px, `"{n} of 34 printings"` | `:86`, `:186` |

The caption embeds the species name in prose and asserts completeness ("every … we track, all eras"), which sits
uneasily beside a shown-count of `8 of 34` — see §6.

### 3.4 Roster row — every field

Source record shape (`this.PRINTS`, `:144–153`): `{ name, set, year, price, roc, vol, acc[2] }`. **No nullable
field exists in the seed** — unlike the Set page, there is no null-handling path anywhere in this component.

| Column | Header | Header tooltip | Cell format | Cell line | Backing today |
|---|---|---|---|---|---|
| Card | `Card` | `Printing name` | 14px 500, centred, ellipsised; whole name is an `<a>` → Card page | `:116` | `cards.name` — **backed** (`DATA_MODEL.md:159`) |
| Set | `Set` | `Set this printing belongs to` | mono 12.5px, muted, centred; **plain text, not a link** | `:117` | `sets.name` via `cards.set_id` — **backed** (`DATA_MODEL.md:143`, `:157`) |
| Year | `Year` | `Release year — click to sort` | `String(c.year)`, mono 12.5px, muted | `:118`, `:201` | **NO BACKING DATA.** `sets` has no release date — its six columns are `id, slug, name, discovered_at, last_seen_at, last_walked_at` (`DATA_MODEL.md:141–146`). Requires the set-metadata table (`DECISIONS.md:199`) |
| PSA 10 | `PSA 10` | `Latest monthly PSA 10 price — click to sort` | `money()` = `'$' + Math.round(n).toLocaleString('en-US')` (`:168`), mono 13.5px 700 | `:119` | `price_months` where `tier = Psa10`, latest per key by `max(observed_at)` — **backed** (`DATA_MODEL.md:181–191`) |
| ROC 3M | `ROC 3M` | `3-month rate of change — click to sort` | `pct()` = sign + `abs(n).toFixed(1)%`; negatives use **U+2212 MINUS** (`:169`); colour `PAL.pos` if `≥ 0` else `PAL.neg2` (`:202`) | `:120` | Derivable from `price_months`; change-only storage means absence ≠ missing (`CLAUDE.md:53`) |
| Sales / mo | `Sales / mo` | `Observed sales per month, all tiers` | `String(vol)`, mono 13px, muted | `:121`, `:202` | `sales` table — **backed in shape**, but the ledger begins at each card's first visit, late Jul 2026 (D-001, `DECISIONS.md:22`) |

Column widths (`:155`): `name 230, set 130, year 70, price 100, roc 92, vol 90`.
Grid template (`:187`): `minmax({name}px, 1.4fr)` then five fixed px tracks — only the name column absorbs slack.
Every header carries a `│` resize grip, `cursor: col-resize`, `title="Drag to resize"` (`:111`).

### 3.5 Sort model

There are **no sort pills on this screen.** Sorting exists only as clickable column headers (`:188–199`):

| Header | Sortable | Sort key | `val()` (`:171`) |
|---|---|---|---|
| `Card` | no (`s: null`) | — | click is a no-op `() => {}` (`:198`) |
| `Set` | **no** (`s: null`) | — | no-op |
| `Year` | yes | `year` | `c.year` |
| `PSA 10` | yes | `value` | falls through to `c.price` |
| `ROC 3M` | yes | `roc` | `c.roc` |
| `Sales / mo` | no (`s: null`) | — | no-op |

Comparator (`:172`): `(dir === 'asc' ? 1 : -1) × (val(a) − val(b))`. Numeric only; no tiebreaker; no alphabetical
sort on Card or Set despite both being displayed and both being obvious sort candidates for a cross-set roster.

Initial state (`:155`): `sort: 'value'`, `sortDir: 'desc'` — the grid opens ordered by PSA 10 price, descending.

### 3.6 Binder tile — the image grid

| Field | Rendered as | Line |
|---|---|---|
| Art | `<image-slot shape="rounded" radius="5" placeholder=" ">` inside a `325 / 450` aspect box | `:93–94` |
| Slot id | `'art-' + name.toLowerCase().replace(/[^a-z0-9]+/g, '-')` | `:208` |
| Thumb background | `linear-gradient(160deg, {acc[0]}, {acc[1]})` | `:93`, `:207` |
| Name | 600 13.5px, single line, ellipsised | `:96` |
| **Set · Year subtitle** | `{set} · {year}`, mono 11.5px muted — **present here, absent from the Set page's tiles** | `:97` |
| Price | mono 13.5px 700 | `:99` |
| ROC 3M | mono 12px, coloured pos/neg2 | `:100` |

Whole tile is an `<a>` → Card page (`:92`); hover raises `0 6px 20px rgba(20,19,26,0.10)`.

**Grid ordering.** The tile grid is `repeat(auto-fill, minmax(180px, 1fr))` (`:90`) — **uniform cells, every tile
the same size**, filled in DOM order from `tiles`, which maps the same `sorted` array as the table (`:204`).
So ordering is: **current sort key + direction, defaulting to PSA 10 price descending.** There is no
value-weighting, no flagship treatment, no span-2 cell, and no manual ordering. It is a plain reading-order grid
that reflows by viewport width.

Card art: real photos exist on disk at `{ImageDirectory}/{hash}/1600.jpg` via `cards.image_hash`
(~3.6 GB, D-010, `DECISIONS.md:83`); licensing is the open risk (D-011). The two-colour accent per printing is
**not backed** — it needs a `card_accents`-style CardStock-owned table (`DECISIONS.md:201`).

### 3.7 Footer

`Prices are latest monthly PSA 10 · a card picturing multiple Pokémon appears under every species it features`
(`:127`, 12.5px muted).

This second clause is the **only place in the prototype where the many-to-many model surfaces to the user**, and
it is the HTML's own statement of the counting rule — see §6.

---

## 4. States

### 4.1 Density (mutually exclusive, exhaustive)

| State | Trigger | Effect |
|---|---|---|
| **binder** (default) | initial `state.view = 'binder'` (`:155`); click `binder` (`:81`, `:183`) | Tile grid renders (`:89`), table hidden. Active button = accent bg / card fg; inactive = card bg / muted fg (`:184–185`) |
| **terminal** | click `terminal` (`:82`, `:183`) | Table renders (`:107`), tile grid hidden |

**Binder is the default here; terminal is the default on the Set page** (`Cardstock Set.dc.html:174`). The button
order is reversed to match. Both are deliberate: the spec calls for "Binder default here"
(`uploads/CARDSTOCK_UI_SPEC_v1.md:213`) and the HTML agrees.

Density is component state only — **not persisted**. Only theme and CVD read `localStorage` (`:33`).

### 4.2 Sort (6 states: 3 keys × 2 directions)

| State | Trigger |
|---|---|
| key ∈ {value, year, roc} | click that column header (`:198`) |
| dir = `desc` | default whenever the key changes (`:198`) |
| dir = `asc` | click the already-active header again |

Active header shows `▾` (U+25BE, desc) or `▴` (U+25B4, asc) appended to its text (`:197`).

**The sort control only exists in terminal density.** In binder — the default view — there is no way to change
sort at all: no pills, no dropdown, no headers. Sort state does persist across a density switch (it lives in
component state), so the only path to a non-default binder ordering is: switch to terminal → sort → switch back.

### 4.3 Per-cell states

| Cell | States |
|---|---|
| ROC 3M (row and tile) | `≥ 0` → `PAL.pos` + `+` prefix · `< 0` → `PAL.neg2` + U+2212 prefix (`:169`, `:202`, `:206`). The seed exercises both (`Umbreon ex`, `roc: -0.8`, `:150`) |
| Header cell | sortable → click sets sort and moves the arrow · non-sortable (`Card`, `Set`, `Sales / mo`) → **no-op** while still showing `cursor: pointer` and a hover-to-accent style (`:111`, `:198`) |
| Pokédex chip | single state; `cursor: help` + native tooltip (`:66`) |

### 4.4 Column resize (transient)

Mouse-down on a grip (`:111`) starts a drag (`:156–165`): `colW[key] = max(52, startW + dx)` on `mousemove`,
listeners removed on `mouseup`. Per-column state, not persisted, no keyboard or touch path.

### 4.5 Sufficiency / exclusion — **absent**

There is **no exclusion mechanism on this screen**: no `included` filter, no `hasExcluded` flag, no amber banner,
no null-handling in any formatter. Compare the Set page, which has all four (`Cardstock Set.dc.html:192`, `:207`,
`:99`, `:226`). Every sortable metric here (Year, PSA 10, ROC 3M) is treated as always-present.

### 4.6 METADATA PENDING — **absent**

There is **no METADATA PENDING state anywhere on this screen.** The Year column and the tiles' `set · year`
subtitle render unconditionally (`:118`, `:97`) even though release date comes from the set-metadata table that
does not exist. `DESIGN_NOTES.md:70` exempts *Pokédex* attributes from the pending state precisely because
"card/set metadata … can be missing" — and Year is set metadata, not Pokédex metadata, so the exemption does not
reach it. `Cardstock About Data.dc.html:115` promises "Missing metadata renders as METADATA PENDING, not as a
silent blank or a guess." The prototype has no such branch. See §7.

### 4.7 States that do not exist in the prototype

Not implemented, and required before this ships: loading / skeleton; **species with one printing** (the spec
mandates a distinct treatment — "skip the grid ceremony and link the Card page prominently",
`uploads/CARDSTOCK_UI_SPEC_v1.md:215`); species with zero tracked printings; species-not-found (404); query
error; **negative 90d** (the header hard-codes `var(--pos)`, `:75`); a printing whose set has no release year; and
any LOW DATA / LOCKED badge from the honesty vocabulary (`HANDOFF.md:43`).

---

## 5. Interactions

| # | Control | Line | Consequence |
|---|---|---|---|
| 1 | Logo / wordmark | `:39` | → Home |
| 2 | Nav links ×5 | `:43–47` | → Home / Screener / Charts / Binder / Browse; `Browse` is the active tab |
| 3 | `<cardstock-search>` | `:50` | Shared typeahead; `/` focuses, Esc clears+blurs, ≥2 chars, grouped Characters (4) / Sets (4) / Cards (5) (`DISPLAY_VOCABULARY.md:194–195`) |
| 4 | Account avatar `O` | `:51` | → Profile |
| 5 | Breadcrumb `Browse` | `:58` | → Browse |
| 6 | `binder` button | `:81` | `setState({view:'binder'})`; tooltip "Binder density — fewer rows with card art" |
| 7 | `terminal` button | `:82` | `setState({view:'terminal'})`; tooltip "Terminal density — more rows, tighter type, every metric column" |
| 8 | Column header ×3 (`Year`, `PSA 10`, `ROC 3M`) | `:111`, `:198` | Sets sort key (resets direction to `desc`) or flips direction if already active; re-sorts **both** table and tiles |
| 9 | Column header ×3 (`Card`, `Set`, `Sales / mo`) | `:189–190`, `:194` | **No-op.** Pointer cursor and hover-to-accent still fire — a false affordance |
| 10 | Resize grip ×6 | `:111`, `:156` | Drag adjusts that column's width, floor 52px; `gridCols` recomputes (`:187`) |
| 11 | Row card name link | `:116` | → Card page |
| 12 | Binder tile | `:92` | → Card page; hover raises a shadow |
| 13 | Pokédex chip hover | `:66` | Native tooltip; `cursor: help`. Chips are **not** links and do not filter anything |

**Interactions that are conspicuously absent:** the Set column is not a link to the Set page (`:117`), even though
the reverse link exists (the Set page's rows link to Card, and the Card page links to both Set and Character,
`Cardstock Card.dc.html:66`). No sort control in binder density. No filter, no era grouping, and no "compare to
another species" affordance.

Accessibility as built: `*:focus-visible` gives a 2px accent outline (`:21`); `prefers-reduced-motion` caps
animation (`:23`). The table is a **CSS grid of `<div>`/`<span>`, not a `<table>`** (`:109`, `:115`) — no
`role="table"`, no `aria-sort`. Sortable headers are `<span onClick>`, not buttons, so they are not keyboard
reachable. The avatar circle's initial is decorative but not marked `aria-hidden`.

---

## 6. Rules and invariants

### 6.1 Structural

1. **Exactly one density renders.** `isBind` / `isTerm` are complementary derivations of one enum (`:182`).
2. **One sorted list feeds both densities.** `rows` and `tiles` both map `sorted` (`:200`, `:204`), so density
   never changes ordering or membership. The image grid's order *is* the table's order.
3. **A new sort key always starts `desc`** (`:198`).
4. **Money is rounded whole dollars, `en-US` separated** (`:168`); percentages are one decimal with a
   **Unicode minus** (`:169`); the header's total value uses a compact `K` suffix instead (`:74`) — two different
   money formats on one screen.
5. **Prices are one tier only** — PSA 10 everywhere (`:192`, `:127`).
6. **Column width floor is 52px** (`:160`).
7. **Card art is never load-bearing** — `placeholder=" "` plus `::part(empty) { opacity: 0 }` (`:22`) degrades a
   missing image to the accent gradient.

### 6.2 The species aggregate — what the HTML implies

8. **The header counts the corpus; the roster counts what is shown.** Header stats are static literals for the
   full species (34 printings across 19 sets, `:72–73`), while the roster renders 8 seeded printings spanning
   6 distinct sets, and `shownCount` reads `8 of 34 printings` (`:186`). The invariant to implement:
   *header aggregates are computed over all printings of the species, never over the rendered page.*
9. **Total value is a sum over printings, not a market cap.** `$96.4K` (`:74`) equals Browse's `value: 96400`
   (`Cardstock Browse.dc.html:190`); the 8 seeded prices sum to `$9,687`, an order of magnitude less. So it is a
   corpus-level sum of one latest PSA 10 price per printing — **unweighted by population, print run, or
   sales volume**. Nothing in the HTML implies otherwise.
10. **Sets count is distinct sets, not printings.** 34 printings across 19 sets (`:72–73`); the seed shows three
    printings from a single set — Evolving Skies appears at `:145`, `:146`, `:151` — so the two counts are
    independently derived and `sets ≤ printings` always.
11. **A card counts ONCE per featured species.** The HTML's own footer states the rule:
    "a card picturing multiple Pokémon appears under every species it features" (`:127`). Combined with
    `DESIGN_NOTES.md:69` and `HANDOFF.md:107` — card ↔ species is many-to-many via a join table, and species
    aggregates count each card once per featured species — the implementable rule is:

    > For species S, the roster is `SELECT cards.* FROM card_characters JOIN cards … WHERE species = S`.
    > `printings = count(*)`, `sets = count(DISTINCT set_id)`, `total value = sum(latest PSA 10)`, each over that
    > set of cards. A card featuring N species contributes its **full, undivided** value to all N species
    > aggregates — there is no 1/N split and no "primary species" tiebreak. Set-level and market-level totals
    > therefore must **not** be computed by summing species totals; they would double-count multi-species cards.

    The prototype does not *demonstrate* this: all 8 seeded printings are single-species Umbreon cards, and the
    `PRINTS` records carry no species field at all (`:144–153`). The footer sentence is the only evidence in the
    HTML, and it is consistent with the derived docs — this is the one place where doc and prototype agree.
12. **Species membership is by featured Pokémon, not by card name.** The rule as recorded is
    `card_characters` derived from `cards.name` × a Pokédex species list (`DECISIONS.md:199`). Every seeded
    printing happens to contain "Umbreon" in its name (`:145–152`), so name-matching and species-tagging are
    indistinguishable in the seed. They will diverge in the corpus (tag teams, "Eevee Heroes"-style cards, cards
    picturing a species not in the title) — that divergence is exactly what the join table exists to hold.
13. **No cross-species leakage.** Nothing on this page shows the other species on a multi-species card. A tag-team
    card would render here with only its name to disclose that it belongs to two rosters.

---

## 7. Open questions

1. **Route.** `/character/{name}` (`HANDOFF.md:78`) or `/pokemon/{species}`
   (`uploads/CARDSTOCK_UI_SPEC_v1.md:118`, `:210`)? Needs a ruling, plus the key: display name, slug, or
   Pokédex number?
2. **The character index chart does not exist.** `uploads/PROJECT_LOG.md:214` and
   `uploads/CARDSTOCK_UI_SPEC_v1.md:213` both specify one; the HTML has no chart. Was it deliberately cut, or is
   the prototype incomplete? If it stays cut, the `90d` stat still needs an index to compute (D-004).
3. **What is a "character index"?** Same open definition as the set index — constituents, weighting, base,
   min-active-count guard, sufficiency floor.
4. **Negative 90d.** The header hard-codes the positive token (`:75`). Confirm the negative treatment matches the
   row rule (`PAL.neg2`).
5. **Year has no source.** Release date requires the set-metadata table for ~303 sets (`DECISIONS.md:199`).
   Until it exists, what does the Year column render — blank, `—`, or METADATA PENDING? And what does
   *sorting* by Year do when the value is absent (the Set page's answer for a missing metric is exclusion; this
   screen has no exclusion mechanism)?
6. **Which Pokédex fields make chips?** The HTML shows six (type, generation, stage, colour, egg group, habitat);
   `DESIGN_NOTES.md:70` names region, evolution line, and status among the available fields, and Browse carries
   `status` (`Cardstock Browse.dc.html:190`). Is the chip set fixed, and what happens for dual types, no-egg
   species, or a species with no habitat?
7. **Should chips be interactive?** They are `cursor: help` tooltips today (`:66`). Browse offers exactly these
   attributes as filters (`Cardstock Browse.dc.html:235`), so a chip → filtered-Browse link is an obvious
   affordance that the prototype does not have.
8. **No sort control in the default view.** Binder is the default (`:155`) and has no sort UI. Do the Set page's
   sort pills come to this screen, or does binder stay a fixed value-descending grid?
9. **Should the Set column link to the Set page?** It is plain text today (`:117`).
10. **Single-printing species.** The spec mandates a distinct layout (`uploads/CARDSTOCK_UI_SPEC_v1.md:215`); the
    HTML has no such branch. In or out for v1?
11. **Multi-species disclosure.** Should a tag-team card show its co-featured species on the tile or row, and
    should the header stats warn that a card is counted in several species aggregates?
12. **Roster completeness.** The caption claims "every Umbreon printing we track, all eras" (`:84`) while the count
    reads `8 of 34` (`:186`). Is the shipped page the full roster (making the caption true and the count
    redundant), or is there a top-N rule the caption then contradicts?
13. **Total-value semantics.** Sum of latest PSA 10 across printings (§6.2 #9) — confirm, and confirm the
    behaviour when a printing has no PSA 10 observation at all (skip, or fall back to a lower tier?).
14. **Density persistence.** `DISPLAY_VOCABULARY.md:203` says per-device persistence; the prototype does not
    persist. In scope for v1?
15. **Accessibility.** Semantic `<table>` + `aria-sort`, button-ised sortable headers, and removal of the pointer
    cursor from non-sortable headers.

---

## 8. Contradictions found

| Claim | Source doc:line | What the HTML actually does |
|---|---|---|
| Character page has a "character index chart" | `CardStock Mockup/uploads/PROJECT_LOG.md:214` | **No chart, sparkline, or SVG exists on the page.** The header carries four static numeric stats only (`:72–75`) |
| Header has "dominant-color accent (bar + sparkline tint) … character index 90d %" | `uploads/CARDSTOCK_UI_SPEC_v1.md:213` | No sparkline to tint (`:60–77`); `90d +6.8%` is a plain stat (`:75`); the 4px bar is a hard-coded two-stop literal `#2B2D42, #5C6B9E` (`:54`), not derived from art |
| "value-weighted image grid (flagship largest)" | `uploads/CARDSTOCK_UI_SPEC_v1.md:213` | Uniform grid — `repeat(auto-fill, minmax(180px, 1fr))`, every tile identical (`:90`). Ordering is the current sort, defaulting to price desc (`:155`, `:204`); no flagship, no weighting |
| Tiles show "art, tier price, 1M %" | `uploads/CARDSTOCK_UI_SPEC_v1.md:213` | Tiles show art, name, `set · year`, PSA 10 price, and **ROC 3M** — not 1M (`:96–100`, `:193`) |
| Header has a "sort control" | `uploads/CARDSTOCK_UI_SPEC_v1.md:213` | No sort control in the header or toolbar. Sorting exists only as terminal-density column headers (`:188–199`), i.e. **absent from the default view** |
| Footer has an "as of" stamp | `uploads/CARDSTOCK_UI_SPEC_v1.md:213` | Footer is a methodology note (`:127`); no stamp. Consistent with `HANDOFF.md:101` — AsOfStamp was removed app-wide |
| "species with one printing skip the grid ceremony and link the Card page prominently" | `uploads/CARDSTOCK_UI_SPEC_v1.md:215` | No such branch exists; there is exactly one roster rendering path per density (`:89`, `:107`) |
| Route is `/character/{name}` | `CardStock Mockup/HANDOFF.md:78` | Conflicts with `/pokemon/{species}` (`uploads/CARDSTOCK_UI_SPEC_v1.md:118`, `:210`). The static HTML settles neither |
| Character page is P2 | `uploads/CARDSTOCK_UI_SPEC_v1.md:210` | Built in v1 — already corrected by `HANDOFF.md:108` and `DESIGN_NOTES.md:86`; recorded here so the stale spec line is not re-read as current |
| Pokédex species records carry "name, generation, region, type(s), evolution line, status" | `CardStock Mockup/DESIGN_NOTES.md:70` | The chips render type, generation, **stage, colour, egg group, habitat** (`:174–181`). Region, evolution line, and status are not shown; three fields the note never lists are |
| "Density and theme choices persist per device (localStorage)" | `CardStock Mockup/DISPLAY_VOCABULARY.md:203` | Density is component state (`:155`); only theme and CVD read `localStorage` (`:33`). A reload returns to `binder` |
| "Missing metadata renders as METADATA PENDING, not as a silent blank or a guess" | `CardStock Mockup/Cardstock About Data.dc.html:115` | The Year column and the tile `set · year` subtitle render unconditionally (`:118`, `:97`) with no pending state, though release date has no backing table |
| Browse's species list is the same aggregate | (no conflict — recorded as corroboration) | `Cardstock Browse.dc.html:190` `printings: 34, sets: 19, value: 96400, chg: 6.8` matches this header exactly (`:72–75`); chips match Browse's `type/gen/stage/color/egg/habitat` |

---

## 9. Non-scraped data this screen requires

**Character-tag table** (`card_characters`, card → Pokémon, many-to-many — `DECISIONS.md:199`,
`DESIGN_NOTES.md:69`): the entire roster (`:144`), the Printings stat (`:72`), the Sets stat (`:73`), the Total
value stat (`:74`), and the shown-count denominator (`:186`). **Without it this screen cannot exist at all.**

**Set-metadata table** (release date + era for ~303 sets — `DECISIONS.md:199`): the `Year` column (`:118`), its
sort key (`:191`), and the tile `set · year` subtitle (`:97`). The toolbar caption's "all eras" claim (`:84`) also
leans on era existing.

**External Pokédex schema** (`DESIGN_NOTES.md:70`, `HANDOFF.md:107` — pre-populated, joined not authored): the
species name (`:63`), the avatar initial (`:61`), and all six chips (`:174–181`).

**Computed / derived, nonexistent:** the character index behind the `90d` stat (D-004, `DECISIONS.md:61`), and the
per-printing two-colour accent used by tiles and the avatar circle (`card_accents`-style CardStock-owned table;
the "new column on `cards`" variant is flagged as conflicting with D-026 at `DECISIONS.md:201`).

**Backed by the scraper today:** card names, set names, PSA 10 prices and ROC (via `price_months`), and sales-per-
month (via `sales`, subject to the late-Jul-2026 seam, D-001).
