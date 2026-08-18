# Browse — screen specification

**Source of truth:** `CardStock Mockup/Cardstock Browse.dc.html` (318 lines), read in full 2026-08-10. Every line citation below is to that file unless another path is named. Where a markdown document disagreed with the prototype, the prototype was taken as correct and the disagreement is recorded in §8.

> **Amended 2026-08-15 (Catalog phase design, D-110 — build from
> `docs/superpowers/specs/2026-08-15-catalog-phase-design.md`, which supersedes this spec where
> they differ).** Owner rulings of that date: **(a)** set mode gains an ordering control —
> `a–z` (default) | `release date` | `era` — with **data-driven era shelves** (9 eras verified
> live incl. the new `ME`) plus two labeled tail shelves, "no era" (33 matched side-products)
> and "metadata pending" (622); §4.2's "shelves do not exist" now describes the default view
> only, and §7.1–§7.3 are answered. **(b)** Species tiles render the **pixel species icon**
> over the gradient (initial = fallback), settling D-104's open half; §2.3's avatar row is
> superseded. **(c)** Mode goes in the URL (`?mode=pokemon`), superseding §2.1's "not in the
> URL". **(d)** The species grid gets the explicit `ORDER BY` total value DESC that §6.3
> requires. **(e)** The two tile Δs (`30d`, `90d`) render per the D-102 vocabulary — dash +
> ◌-with-gate-tooltip — until the analytics worker; no interim change methods (§7.6 answered:
> deferred). **(f)** Filter vocabularies come from the species tables (rule 7 confirmed);
> habitat's editor carries a Gen 1–3-only explainer. **(g)** §4.6 stands: no species search;
> the global search box is a separate future conversation. **(h)** §7.8 answered: the
> per-species `sets` count stays off the tile. Full walls render (789/1,025) with lazy images.

> **Amended 2026-08-18 (build).** Ties resolve deterministically: a set's top-value card breaks
> equal latest-PSA-10 values by lowest card id, and the species wall breaks equal total values
> by dex number — the same data always renders the same wall.

> **Amended 2026-08-18 (owner UAT, D-112).** The species tile's header is a **flex row** with
> the name + printings block left and the **pixel sprite trailing right in a 68×56 box**
> (native canvas for 898/1,025 sprites — 1:1 and crisp; the 127 Gen-9-era 96×96 sprites
> downscale non-integer for now, D-112's open follow-on) — the 44×44 gradient circle +
> centred initial (`:138`) is deleted outright,
> superseding D-110 (b)'s icon-over-gradient (initial = fallback). Grounds: icon coverage is
> 1,025/1,025, so the initial only ever covered a fetch failure (`onerror` now collapses to
> text-only), and the 44px circle forced a blurring non-integer downscale of the 68×56 art.
> Two build corrections ride along: the build had **stacked** what the mockup lays out as a row
> (`:137` `display:flex` — §2.3's extraction listed the regions but lost the arrangement), and
> the footer's top margin is the mockup's 10px (`:144`), not the build's 8. The owner chose the
> mirrored order (sprite trailing) over the mockup's leading circle after a three-way
> real-pixel comparison. Wire: `SpeciesTileDto` loses `GradientStart`/`GradientEnd` (the circle
> was browse's only consumer). Sprite-size normalization (art-in-canvas varies 21×20 to 44×39)
> is an open follow-on.

**Runtime:** Design Composer. `<x-dc>` host (`:9`), template directives `sc-if` / `sc-for` resolved by `support.js:555-556`; `hint-placeholder-count` / `hint-placeholder-val` are design-time-only hints consumed when the bound value is unavailable (`support.js:614`, `support.js:648`) and carry **no** runtime meaning. All view data comes from one `renderVals()` return object (`:219-314`, dispatched at `support.js:1085`). The component takes **no props** (`data-props=""`, `:159`).

---

## 1. Identity

| | |
|---|---|
| **Screen name** | Browse (`data-screen-label="Browse"`, `:35`) |
| **Route** | `/browse` (`HANDOFF.md:75`; `uploads/CARDSTOCK_UI_SPEC_v1.md:116`. The prototype is a file — `Cardstock Browse.dc.html` — and the nav links to it by filename, `:47`.) |
| **Nav position** | Fifth and last primary tab: Home · Screener · Charts · Binder · **Browse** (`:43-47`). Active styling on Browse: weight 600, `--ink` text, 2px `--acc` bottom border (`:47`). |
| **Purpose** | The catalog entry point — two exhaustive lists of *everything in the corpus*, one keyed by set and one keyed by Pokémon species, each tile a link into a detail page. It is the product's one non-terminal, gallery-flavoured surface. |
| **Outbound links** | Set tiles → `Cardstock Set.dc.html` (all 10, `:172-181`). Species tiles → `Cardstock Character.dc.html` (`:310`). Nav → Home/Screener/Charts/Binder/Profile (`:43-51`). |
| **Page chrome** | Sticky 48px nav (`:37`), global `<cardstock-search>` web component (`:50`), profile avatar `O` (`:51`). `<main>` max-width 1480px, padding `14px 20px 28px`, column flex, gap 18px, base font-size 15px (`:35`, `:54`). |

---

## 2. Layout — the two modes

Both modes confirmed present. The screen is a single page with a **binary mode switch**; there is no third mode, no sub-tab, and no per-mode sort control.

### 2.1 The switch

- Segmented pair of buttons in the header row beside the `<h1>Browse</h1>` (`:56-63`), inside a 1px-bordered, 6px-radius, `overflow:hidden` shell (`:58`). Buttons are 28px tall, JetBrains Mono 13px/600, lowercase labels **`by set`** (`:59`) and **`by pokémon`** (`:60`).
- Tooltips: `by set` → "Browse by set — every release, its size, and its market value"; `by pokémon` → "Browse by Pokémon — every species and all of its printings" (`:59-60`).
- Handlers `modeSets` / `modePoke` set `state.mode` to `'sets'` / `'poke'` (`:247`). Initial state is **`'sets'`** (`:209`).
- Active/inactive styling is computed, not CSS: active button gets `background: --acc`, `color: --card`; inactive gets `background: --card`, `color: --mut` (`:248-249`). There is no `aria-pressed`, no `role="tablist"`, and no keyboard-arrow behaviour — they are two plain `<button>`s.
- Mode is **not** in the URL, not persisted, and not restorable. A reload returns to `by set`.

### 2.2 Mode A — `by set`

Gated by `sc-if value="{{ isSets }}"` (`:109`, `isSets = mode === 'sets'`, `:246`).

- One `<section>` containing **one flat CSS grid** — `repeat(auto-fill, minmax(230px, 1fr))`, gap 12px (`:111`). **No shelves, no era rows, no group headings, no horizontal scrollers.** The whole catalog is one alphabetical wall of tiles.
- No filter bar, no search, no sort control, no result count in this mode. The filter row (`:65-107`) is inside `sc-if isPoke` and is therefore absent.

**Set tile** (`:113-126`) — an `<a>` block, `--card` background, 1px `--line` border, radius 10, padding 14, hover box-shadow `0 6px 20px rgba(20,19,26,0.10)` with a 0.15s transition:

| Region | Spec |
|---|---|
| Fan area | 118px tall, 11px bottom margin, `position: relative` (`:114`) |
| Back-left card (`fan3`) | 74×102, radius 5, `left:50%; top:4px`, `translateX(-88%) rotate(-8deg)`, shadow `0 3px 10px` — **gradient only** (`:115`) |
| Back-right card (`fan2`) | 74×102, radius 5, `left:50%; top:4px`, `translateX(-12%) rotate(8deg)`, shadow `0 3px 10px` — **gradient only** (`:116`) |
| Front card (`fan1`) | 78×108, radius 5, `left:50%; top:0`, `translateX(-50%)`, shadow `0 5px 14px` — **carries the only `<image-slot>`** (`:117-118`) |
| Name | Inter Tight 600 / 15.5px, centred (`:121`) |
| Stat row | Centred flex, gap 8, JetBrains Mono 12px: `"{count} cards"` in `--mut2`, then `"{chg} 30d"` in a sign-derived colour (`:122-125`) |

### 2.3 Mode B — `by pokémon`

Two sibling `sc-if isPoke` blocks (`:65-107` filter bar, `:132-154` grid), so in Pokémon mode the page is: header row → filter bar → caption → species grid → optional empty panel.

- Caption line above the grid: **"Ordered by total market value across all printings"**, 12.5px `--mut2`, `margin-top:-8px` (`:133`). See §6 for why this is a claim rather than an enforced order.
- Grid: `repeat(auto-fill, minmax(190px, 1fr))`, gap 12px (`:134`) — tighter than the set grid.

**Species tile** (`:136-148`) — same card shell as a set tile but padding 13:

| Region | Spec |
|---|---|
| Header row | Flex row, `align-items:center`, gap 10 (`:137`): name + printings block left (`min-width:0`), pixel sprite trailing **right** in a 68×56 box (native for 898/1,025; the 127 Gen-9-era 96×96 canvases downscale for now), `image-rendering: pixelated`, `onerror` collapses to text-only. ~~Avatar: 44×44 gradient circle + centred initial (`:138`)~~ — deleted by D-112 (2026-08-18 banner above); the mockup's leading-circle order is superseded by the owner's trailing-sprite ruling |
| Name | Inter Tight 600 / 15.5px, single line, ellipsis on overflow (`:140`) |
| Sub-line | JetBrains Mono 11.5px `--mut2`: `"{printings} printings"` (`:141`) |
| Footer row | `space-between`, baseline-aligned: value in Mono 14.5px/700 (`:145`), then `"{chg} 90d"` in Mono 12px, sign-coloured (`:146`); margin-top 10 (`:144`) |

### 2.4 Image usage — summary

Exactly **one** `<image-slot>` element exists on the screen (`:118`); the tag appears 4 times in the file, of which `:15` is the script include and `:22` is a CSS rule. It sits on the front card of the set-tile fan, `shape="rounded" radius="5" placeholder=" "`, id `art-set-<slug>` (`:118`, `:227`). The `placeholder=" "` value pairs with `image-slot[placeholder=" "]::part(empty){opacity:0}` (`:22`) so an unfilled slot is invisible and the gradient beneath shows through. Everything else on the screen — both back cards of every fan, and every species avatar — is a **CSS gradient with no image affordance**.

---

## 3. Data contract — every field rendered

### 3.1 View model root (`renderVals()`, `:245-313`)

| Key | Type | Rendered at | Meaning |
|---|---|---|---|
| `isSets` | bool | `:109` | mode gate for the set grid |
| `isPoke` | bool | `:65`, `:132` | mode gate for the filter bar and species grid |
| `modeSets`, `modePoke` | handlers | `:59`, `:60` | mode setters |
| `msBg`, `msFg`, `mpBg`, `mpFg` | colour | `:59-60` | segmented-button fill/text per active mode |
| `allSets` | list | `:112` | the set tiles (see 3.2) |
| `pAddOpen` | bool | `:69` | filter popover open |
| `pToggleAdd`, `pCloseAdd` | handlers | `:68`, `:70` | open/close popover |
| `pShowMenu`, `pShowEditor` | bool | `:71`, `:80` | which pane inside the popover |
| `pMenu` | list | `:73` | attribute list (see 3.4) |
| `pEdName` | string | `:83` | editor header = attribute display name |
| `pEdBack` | handler | `:82` | return to attribute list |
| `pEdOpts` | list | `:86` | option rows (see 3.5) |
| `pEdPreview` | string | `:94` | live expression preview or `"pick at least one"` |
| `pEdAdd`, `pEdAddOff`, `pEdAddBg`, `pEdAddCur` | handler/bool/colour/cursor | `:95` | commit button and its disabled dressing |
| `pokeChips` | list | `:101` | active filter chips (see 3.6) |
| `speciesCount` | string | `:105` | `"{matched} of {total} species"` (`:303`) |
| `species` | list | `:135` | the species tiles (see 3.3) |
| `pokeNoMatch` | bool | `:151` | zero-result gate |
| `pokeQ`, `setPokeQ` | string / handler | **nowhere** | exposed at `:251`, bound to no element — see §4.6 |

### 3.2 `allSets[]` — set tile fields (`mkSet`, `:224-229`; list built `:230`)

| Field | Source | Format | Rendered |
|---|---|---|---|
| `name` | `SETS[].name` | raw string | `:121` |
| `count` | `SETS[].count` | `String(n)`, suffixed " cards" in markup | `:123` |
| `chg` | `SETS[].chg` | `pct()` — `'+'` or U+2212 MINUS, `abs.toFixed(1)`, `'%'` (`:222`); suffixed " 30d" in markup | `:124` |
| `chgFg` | derived | `--pos` when `chg >= 0`, `--neg2` when `< 0` (`:223`) | `:124` |
| `fan1`, `fan2`, `fan3` | `SETS[].fans[0..2]` | `linear-gradient(160deg, c0, c1)` (`:221`) | `:117`, `:116`, `:115` |
| `slotId` | derived from name | `'art-set-' + name.toLowerCase().replace(/[^a-z0-9]+/g,'-')` (`:227`) — e.g. `art-set-base-set`, `art-set-sword-shield` | `:118` |
| `href` | `SETS[].href` | all 10 = `Cardstock Set.dc.html` (`:172-181`) | `:113` |
| `tip` | `SETS[].tip` | native `title` | `:113` |

**Declared on the seed but never mapped and never rendered: `era`** (`:172-181`). See §4.2.

Seeded rows (`:172-181`) — 10 sets, illustrative:

| name | count | chg | era (unused) |
|---|---|---|---|
| Base Set | 102 | +1.8 | `WOTC` |
| Neo Genesis | 111 | +0.9 | `WOTC` |
| Hidden Fates | 163 | +2.6 | `Sun & Moon` |
| Sword & Shield | 216 | −0.4 | `Sword & Shield` |
| Evolving Skies | 237 | +4.1 | `Sword & Shield` |
| Fusion Strike | 284 | +1.2 | `Sword & Shield` |
| Brilliant Stars | 186 | +2.9 | `Sword & Shield` |
| Lost Origin | 217 | +3.4 | `Sword & Shield` |
| Silver Tempest | 215 | +1.7 | `Sword & Shield` |
| Vivid Voltage | 203 | +0.6 | **`null`** |

Nine of the ten tooltips read "Set page for {name} — prototype renders Evolving Skies data for every set"; Evolving Skies' reads "Evolving Skies — the deepest set in the corpus" (`:176`).

### 3.3 `species[]` — species tile fields (`:304-311`)

| Field | Source | Format | Rendered |
|---|---|---|---|
| `name` | `SPECIES[].name` | raw | `:140` |
| `initial` | derived | `name[0]` — first character only (`:305`) | `:138` |
| `accent` | `SPECIES_ACCENTS[name]` | `linear-gradient(160deg, …)`, fallback `['#8A9BB8','#D6E0EC']` (`:306`) | `:138` |
| `printings` | `SPECIES[].printings` | `String(n)`, suffixed " printings" | `:141` |
| `value` | `SPECIES[].value` | `'$' + (v >= 1000 ? round(v/1000)+'K' : v)` (`:308`) — e.g. `$284K` | `:145` |
| `chg` | `SPECIES[].chg` | `pct()`, suffixed " 90d" | `:146` |
| `chgFg` | derived | same sign rule as sets (`:309`) | `:146` |
| `href` | literal | `Cardstock Character.dc.html` for every species (`:310`) | `:136` |
| `tip` | derived | `"Character page for {name} — prototype renders Umbreon data for every species"` (`:310`) | `:136` |

**Computed into the view model but never rendered: `type`, `gen`, `sets`** (`:305`, `:307`). `sets` in particular — the seed carries a per-species set count (e.g. Charizard 41) and the tile does not show it.

**On the seed but never surfaced as a tile field:** `status`, `stage`, `color`, `egg`, `habitat` (`:189-204`) — these exist only to power the filters.

Seeded rows (`:189-204`) — 16 species, illustrative. Fields: name, type, gen, status, stage, colour, egg group, habitat, printings, sets, value, 90d %.

| name | type | gen | status | stage | color | egg | habitat | printings | sets | value | chg |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Charizard | Fire | 1 | Ordinary | Stage 2 | Red | Monster | Mountain | 87 | 41 | 284000 | +3.2 |
| Umbreon | Dark | 2 | Ordinary | Stage 1 | Black | Field | Urban | 34 | 19 | 96400 | +6.8 |
| Lugia | Psychic | 2 | Legendary | Basic | White | No eggs | Rare | 41 | 23 | 88200 | +1.4 |
| Rayquaza | Dragon | 3 | Legendary | Basic | Green | No eggs | Rare | 38 | 21 | 71500 | +2.1 |
| Mewtwo | Psychic | 1 | Legendary | Basic | Purple | No eggs | Rare | 52 | 30 | 64800 | −0.8 |
| Espeon | Psychic | 2 | Ordinary | Stage 1 | Purple | Field | Urban | 29 | 17 | 48900 | +5.2 |
| Giratina | Ghost | 4 | Legendary | Basic | Black | No eggs | Rare | 26 | 15 | 42300 | +4.4 |
| Blastoise | Water | 1 | Ordinary | Stage 2 | Blue | Monster | Waters-edge | 44 | 26 | 39700 | +0.7 |
| Gengar | Ghost | 1 | Ordinary | Stage 2 | Purple | Amorphous | Cave | 39 | 24 | 36200 | +1.9 |
| Sylveon | Fairy | 6 | Ordinary | Stage 1 | Pink | Field | Urban | 22 | 13 | 28800 | +3.6 |
| Snorlax | Normal | 1 | Ordinary | Stage 1 | Black | Monster | Mountain | 35 | 22 | 21400 | +0.3 |
| Alakazam | Psychic | 1 | Ordinary | Stage 2 | Brown | Human-Like | Urban | 31 | 20 | 19800 | −1.2 |
| Machamp | Fighting | 1 | Ordinary | Stage 2 | Gray | Human-Like | Mountain | 33 | 21 | 14600 | +0.5 |
| Dragonite | Dragon | 1 | Ordinary | Stage 2 | Brown | Water 1 | Waters-edge | 30 | 19 | 13900 | +1.1 |
| Leafeon | Grass | 4 | Ordinary | Stage 1 | Green | Field | Forest | 18 | 11 | 12700 | +2.8 |
| Glaceon | Ice | 4 | Ordinary | Stage 1 | Blue | Field | Snow | 17 | 11 | 11900 | +2.2 |

Note `Snorlax` is seeded `Stage 1` and `Alakazam`/`Machamp`/`Dragonite` colours are Pokédex-body colours, not type colours — the seed is illustrative and must not be treated as a Pokédex fixture.

### 3.4 `pMenu[]` — attribute menu rows (`:257-266`)

`name` (display name), `tip` (`"Filter species by {lowercased name} — fixed Pokédex data"`), `add` (handler). Rendered `:74-77` with a trailing `›` chevron.

### 3.5 `pEdOpts[]` — option rows (`:269-281`)

`label` (per-attribute label function), `mark` (`✓` when selected, else empty), `tip` (`"Include X in the results"` / `"Stop including X"`), `bg` (`--acc` when on, `--card` when off), `bd` (`--acc` when on, `--line3` when off), `pick` (toggle handler). Rendered `:87-90` as a 14×14 checkbox square plus a 13px label.

### 3.6 `pokeChips[]` — active filter chips (`:296-302`)

`label` = **raw attribute key** + operator + comma-joined labels: `f.attr + (vals.length > 1 ? ' ∈ ' : ' = ') + vals.map(lbl).join(', ')` (`:299`). So chips read `gen = Gen 1`, `stage ∈ Basic, Stage 1`, `egg = Field` — the key, not the display name. `remove` drops the whole attribute (`:300`). Rendered `:102` as an accent-tinted mono pill with an `✕`.

### 3.7 Constants declared in the component

| Constant | Line | Read by |
|---|---|---|
| `SETS` (10 rows) | `:171-182` | `allSets` (`:230`) — all fields **except `era`** |
| `ERAS` (3 rows: `WOTC 1999–2003`, `Sun & Moon 2017–2019`, `Sword & Shield 2020–2022`) | `:183-187` | **nothing** |
| `SPECIES` (16 rows) | `:188-205` | filters (`:232-244`), `species` (`:304`), `speciesCount` (`:303`) |
| `REGIONS` (9: Kanto…Paldea) | `:206` | the `region` filter only (`:236`) |
| `SPECIES_ACCENTS` (16 gradients) | `:207` | avatar gradient (`:306`) |
| `PAL` | `:161-168` | every computed colour |

---

## 4. States — complete state space

`state = { mode: 'sets', pokeQ: '', pokeFilters: [], pAddOpen: false, pEditor: null }` (`:209`). Five variables; everything below is a projection of them.

### 4.1 Mode states

| State | Trigger | Rendering |
|---|---|---|
| **Sets mode** (default) | initial state; `by set` clicked (`:247`) | `:109-130` only. Filter bar and species grid unmounted. |
| **Pokémon mode** | `by pokémon` clicked (`:247`) | `:65-107` filter bar + `:132-154` caption/grid. Set grid unmounted. |

Filters and popover state are **not reset** on mode change (`:247` sets only `mode`), so chips survive a round-trip through sets mode and reappear intact.

### 4.2 Era shelves — **DO NOT EXIST**

Verified by exhaustive read and by literal count over the file:

- `ERAS` appears **1 time** — its own assignment at `:183`. Nothing reads it. It is dead data.
- Per-set `era` values appear on all 10 seed rows (`:172-181`) and are **never mapped into the view model**: `mkSet` (`:224-229`) returns 9 keys and `era` is not among them, so no template expression can reach it.
- Literal `"shelf"` / `"shelves"`: **0 occurrences.**
- Literal `"Uncategorized"`: **0 occurrences.** There is no Uncategorized shelf, no unclassified group, no fallback bucket. `Vivid Voltage` carries `era: null` (`:181`) — the one seeded row that *would* land in such a bucket — and it renders as an ordinary tile in alphabetical position, visually identical to the other nine.
- Literal `"METADATA PENDING"`: **0 occurrences.** Literal `"metadata"` (any case): **0 occurrences.** There is no honesty badge, no curation-pending affordance, and no state that could show one.

**Era constants present in the file**, all inert: `'WOTC'`, `'Sun & Moon'`, `'Sword & Shield'` (as `ERAS[].era`, `:184-186`, with year ranges `1999–2003` / `2017–2019` / `2020–2022`); the same three strings plus `null` as `SETS[].era` (`:172-181`). Nothing reads any of them.

This is consistent with the data: `sets` carries no era and no release date (`../PokemonInvestBatch/DATA_MODEL.md:139-146`), so era grouping would require the non-scraped set-metadata table that `DECISIONS.md:199` records as not existing. The prototype's flat alphabetical grid is the design that can actually be built today; the era shelving in the older documents is unbuilt intent.

### 4.3 Filter popover states (Pokémon mode only)

| State | Condition | Rendering |
|---|---|---|
| **Closed** | `pAddOpen === false` (default) | only the dashed `+ filter` button (`:68`) |
| **Attribute list** | `pShowMenu = pAddOpen && !pEditor` (`:255`) | popover with `Pokédex` section label (`:72`) and 8 attribute rows (`:73-78`) |
| **Option editor** | `pShowEditor = !!pEditor` (`:256`) — and, because the editor lives inside the `sc-if pAddOpen` popover (`:69`), only visible while the popover is open | back chevron + attribute name header (`:81-84`), scrollable option list capped at 220px (`:85`), preview + Add footer (`:93-96`) |
| **Editor, nothing picked** | `picked.length === 0` (`:286`) | preview reads `"pick at least one"`; Add is `disabled`, background `--accMut`, cursor `not-allowed` (`:283`, `:289`) |
| **Editor, ≥1 picked** | `picked.length > 0` | preview reads `attr = v` or `attr ∈ v1, v2`; Add enabled, background `--acc`, cursor `pointer` (`:288-289`) |

Popover box: absolute, `top:31px`, `z-index:50`, width 300px, `max-height:380px`, `overflow-y:auto` (`:70`).

### 4.4 Result states (Pokémon mode)

| State | Trigger | Rendering |
|---|---|---|
| **Unfiltered** | `pokeFilters` empty | all 16 species; counter reads `16 of 16 species` (`:105`, `:303`) |
| **Filtered, ≥1 match** | every filter satisfied by ≥1 species (`:243-244`) | subset grid; counter reads `{n} of 16 species` |
| **Filtered, 0 matches** | `pokeNoMatch = speciesAll.length === 0` (`:312`) | the grid still renders **empty** (`:134-150` is not gated), and below it a centred panel: **"No species match these filters — remove one to widen the net."** — card background, 1px border, radius 10, padding 40, 14px `--mut2` (`:151-153`). Counter reads `0 of 16 species`. |

### 4.5 States that are absent

No loading state, no skeleton, no error state, no offline/stale state, no pagination or "load more", no empty state for sets mode (`allSets` is always the full 10), no per-tile disabled state, and no honesty/pending badge anywhere on this screen.

### 4.6 Species search — **DECLARED, NOT WIRED**

- `state.pokeQ` exists (`:209`), a normalized query is computed as `const pq = st.pokeQ.trim().toLowerCase()` (`:231`), and both `pokeQ` and a setter `setPokeQ` are exported from `renderVals()` (`:251`).
- **`pq` is never referenced again.** The species list is `this.SPECIES.filter(s => st.pokeFilters.every(...))` (`:243-244`) — attribute filters only, no name matching.
- The file contains **zero `<input>` elements** (literal count of `<input`: 0). Nothing binds `pokeQ` or calls `setPokeQ`. `speciesCount` (`:303`) counts filter matches only.

The only search on the screen is the **global nav typeahead** `<cardstock-search>` (`:50`), a shared web component (`cardstock-search.js`) that searches a frozen demo corpus of species/sets/cards and navigates to Character/Set/Card pages. It is page chrome, identical on every screen, and it does **not** filter the Browse grid.

**Implementation consequence:** `pokeQ` / `setPokeQ` / `pq` are dead code left behind when the inline search was replaced by the shared component (`DESIGN_NOTES.md:123-124` records that replacement). Do not port them. If a species search is wanted, it is a new decision, not a transcription.

---

## 5. Interactions

| # | Control | Line | Consequence |
|---|---|---|---|
| 1 | `by set` button | `:59` | `mode = 'sets'`. Swaps the whole body to the set grid. Filters/popover state untouched. |
| 2 | `by pokémon` button | `:60` | `mode = 'poke'`. Shows filter bar + caption + species grid. |
| 3 | Set tile | `:113` | Full-tile link → `Cardstock Set.dc.html`. Hover raises a shadow. Native `title` tooltip. All 10 tiles go to the same page. |
| 4 | Species tile | `:136` | Full-tile link → `Cardstock Character.dc.html`. Same hover/tooltip treatment. |
| 5 | `+ filter` | `:68` | `pToggleAdd` — flips `pAddOpen` **and** clears `pEditor` (`:253`), so reopening always lands on the attribute list. |
| 6 | Attribute row | `:74` | `pEditor = { attr, sel }`. If a chip for that attribute already exists, its values are pre-checked (`:262-263`), making the row an *edit* action as well as an *add*. |
| 7 | Option row | `:87` | Toggles that value in `pEditor.sel` (`:275-279`). Immediate: checkbox fills `--acc` with `✓`, preview and Add-enablement update. No re-run yet. |
| 8 | Back `‹` | `:82` | `pEditor = null` — returns to the attribute list, **discarding the in-progress selection**. Popover stays open. |
| 9 | `Add` | `:95` | No-op when disabled. Otherwise removes any existing filter with the same `attr` and appends the new one (`:290-293`) — one chip per attribute, always — then closes editor and popover. Grid and counter re-run. |
| 10 | Chip `✕` | `:102` | Removes the entire attribute filter (`:300`). Grid re-runs. There is no "clear all". |
| 11 | Mouse leaves popover | `:70` | `pCloseAdd` closes it **only if no editor is open** (`:254`), so drifting off mid-edit does not lose work. |
| 12 | `mousedown` outside | `:212` | If open and the target is outside `[data-pfilter-pop]` and still connected, closes popover **and** discards the editor. Listener added on mount, removed on unmount (`:210-218`). |
| 13 | Nav / search / avatar | `:39-51` | Standard chrome navigation. `<cardstock-search>` supplies `/`-to-focus, Esc-to-close typeahead. |

**Not present:** sort controls, density/view toggles, pagination, keyboard shortcuts local to this screen, context menus, drag-and-drop (beyond whatever `image-slot.js` offers at design time), and any bulk action.

---

## 6. Rules and invariants

1. **Exactly two modes, `sets` default.** `mode` is `'sets' | 'poke'` (`:209`, `:247`); everything else is a projection.
2. **Set ordering is alphabetical by name, ascending** — `SETS.slice().sort((a,b) => a.name < b.name ? -1 : 1)` (`:230`). Not chronological, not by value, not by size, and **not grouped**. Rendered order for the seed: Base Set, Brilliant Stars, Evolving Skies, Fusion Strike, Hidden Fates, Lost Origin, Neo Genesis, Silver Tempest, Sword & Shield, Vivid Voltage. The comparator never returns 0, so it is not a stable sort for equal names — irrelevant for unique names, worth fixing in the port.
3. **The species grid is NOT sorted.** `speciesAll` is `this.SPECIES.filter(...)` (`:243`) — literal array order, no `.sort()` anywhere on the species path (the only two `.sort()` calls in the file are the set sort at `:230` and the filter-option sorts at `:232`/`:236`). The caption "Ordered by total market value across all printings" (`:133`) is true of the seed **only because the seed array happens to be written in descending `value` order** (284000 → 11900, `:189-204`). Nothing enforces it. **The Blazor implementation must add an explicit `ORDER BY total_value DESC` to make the caption honest** — this is a real requirement the prototype leaves implicit.
4. **Filter algebra: AND across attributes, OR within an attribute.** `pokeFilters.every(f => f.vals.some(v => String(v) === String(ATTRS[f.attr].of(s))))` (`:243-244`). Comparison is stringified on both sides, which is what lets numeric `gen` round-trip through the string-keyed `sel` map.
5. **One chip per attribute, enforced on commit.** `pEdAdd` filters out same-`attr` entries before concatenating (`:291`). Re-opening an attribute pre-loads its current values (`:262-263`), so the interaction is edit-in-place.
6. **Add requires at least one value** (`:286`, `:290`); a filter can never be committed empty, so `pokeFilters` never contains an empty `vals`.
7. **Filter options are derived from the loaded species set, not from a fixed vocabulary** — `uniq()` over `SPECIES` (`:232`) for type/gen/color/egg/habitat and for region-via-`REGIONS`. `status` and `stage` *do* start from fixed vocabularies but are then `.filter()`ed to values actually present (`:237-238`), which is why **`Mythical` never appears** — no seeded species has it. In production these lists must come from the Pokédex source, not from the current page's rows.
8. **Chips and the preview expression use the raw attribute key, not the display name** (`:288`, `:299`) — `gen`, `egg`, `stage`, `habitat`. Deliberate terminal-flavoured shorthand; preserve it or change it as an explicit design decision.
9. **Percent formatting is uniform**: sign (`+` / U+2212 MINUS, not a hyphen), one decimal, `%` (`:222`). Colour is `--pos` for `>= 0` — **zero renders positive-green with a `+`** — and `--neg2` for `< 0` (`:223`).
10. **Window suffixes are fixed and different per mode**: sets show `30d` (`:124`), species show `90d` (`:146`). Both are hard-coded strings in the markup, not data.
11. **Value abbreviation:** `$` + `round(v/1000) + 'K'` at or above 1000, else the raw integer (`:308`). No thousands separators, no cents, no currency selector.
12. **Every set tile links to the same page and every species tile links to the same page** (`:172-181`, `:310`) — a prototype shortcut, stated openly in the tooltips ("prototype renders Evolving Skies data for every set" / "…Umbreon data for every species"). In production these become per-entity routes.
13. **`<image-slot>` ids must stay unique** (`:118`, `:227`); `image-slot.js` persists drops per id in a sidecar shared by every page in the directory. Design-time only — irrelevant to the Blazor port except as the marker for *where real card art belongs*: the front card of the fan, and nowhere else on this screen.
14. **Theme and CVD are read once, at construction.** `PAL` is a class-field IIFE reading `localStorage` (`:161-168`), and the inline head script stamps `data-theme` / `data-cvd` (`:33`). A theme change therefore needs a reload for the JS-computed colours (deltas, mode buttons, checkboxes) even though the CSS-variable colours would flip live. Do not carry this quirk into Blazor — resolve palette per render.
15. **Colour is never the only signal for direction**: the sign character carries it too (`:222`), and a CVD palette swaps green/red for blue/orange (`:25`, `:162-165`).
16. **No props.** `data-props=""` (`:159`); the screen is self-contained and has no demo/empty-state prop, matching `DESIGN_NOTES.md:141`.

---

## 7. Open questions

1. **Do era shelves ship at all?** The prototype deleted them; three older documents still describe them (§8). Shelving needs the non-scraped set-metadata table (`DECISIONS.md:199`; `../PokemonInvestBatch/DATA_MODEL.md:139-146` confirms `sets` has neither era nor release date). Decide: (a) build the curation table and restore shelves, (b) ship the flat alphabetical grid the prototype specifies, or (c) flat now, shelves later. Until decided, **build (b)** — it is what the authoritative artefact shows.
2. **If shelves return, does "Uncategorized" + METADATA PENDING return with them?** Both are absent from the prototype (0 literal occurrences each). `DESIGN_NOTES.md:70` argues METADATA PENDING applies to *card/set* metadata, not Pokédex attributes — so an Uncategorized set shelf would be consistent with that ruling, while a pending badge on a species filter would not.
3. **Alphabetical is the only set ordering.** ~303 real sets in one alphabetical wall is a very different object from 10 tiles. Does production need era/date grouping, a sort control, or search purely as a scale mitigation, independent of the era question?
4. **Species search.** The prototype has none (§4.6). At 16 seeded species the filters suffice; the real species count does not. Is the global nav typeahead the intended answer, or does the grid need its own query box?
5. **Set-mode filtering.** Pokémon mode has 8 filters; set mode has none. Intentional asymmetry or unfinished?
6. **Where do the aggregates come from?** Set `count` and 30d %, species `printings`/`sets`/total value/90d % are all seeded literals. Given append-only, change-only history (ADR-0001), each needs a defined derivation — especially "30d %" and "90d %", where the naive month-window query returns nothing for most cards.
7. **Species → printings/sets counts** depend on the card↔species join (`card_characters`) recorded as not-yet-existing (`DECISIONS.md:199`). Browse-by-Pokémon cannot ship before that table does.
8. **Is `sets` (per-species set count) meant to be displayed?** It is computed (`:307`) and dropped; `DESIGN_NOTES.md:71` says the tile shows "printings/sets". Show it or delete it.
9. **Chip vocabulary** — raw keys (`gen`, `egg`) vs display names (`Generation`, `Egg group`). Prototype uses keys; confirm that is the intended terminal voice.
10. **Mode is not in the URL.** Should `/browse?mode=pokemon` (and filter state) be shareable/bookmarkable?
11. **Grid virtualization / paging** for the real corpus — the prototype renders every row.
12. **Accessibility gaps to close in the port:** the mode switch has no `aria-pressed`/tablist semantics, the filter popover is a plain `div` with no `role="dialog"`/focus trap/Esc handler (only mouseleave and outside-mousedown close it, `:70`/`:212`), and option rows are `<button>`s styled as checkboxes with no `role="checkbox"`/`aria-checked`.

---

## 8. Contradictions found

| Claim | Source doc:line | What the HTML actually does |
|---|---|---|
| "By set = era shelves (WOTC / Sun & Moon / Sword & Shield…)" | `CardStock Mockup/DESIGN_NOTES.md:71` | No shelves. One flat grid, `auto-fill minmax(230px,1fr)` (`:111`), sorted **alphabetically by name** (`:230`). `ERAS` is declared at `:183` and read by nothing (1 literal occurrence, its own assignment); `SETS[].era` is never mapped by `mkSet` (`:224-229`). |
| "era shelves (WOTC, EX, DP, BW, XY, SM, SWSH, SV…)" — 8+ eras | `CardStock Mockup/uploads/CARDSTOCK_UI_SPEC_v1.md:199` | No shelves at all, and only **three** era constants exist anywhere in the file (`WOTC`, `Sun & Moon`, `Sword & Shield`, `:184-186`). `EX/DP/BW/XY/SV` appear nowhere. |
| "By set (shelves grouped by era…)" | `CardStock Mockup/uploads/PROJECT_LOG.md:214` | Same — no grouping of any kind (`:110-129`). |
| "'Uncategorized' shelf w/ METADATA PENDING badge — honesty state from spec §4.8" | `CardStock Mockup/DESIGN_NOTES.md:71` | **0 literal occurrences of "Uncategorized"; 0 of "METADATA PENDING"; 0 of "metadata".** `Vivid Voltage` has `era: null` (`:181`) and renders as an ordinary tile in alphabetical position. |
| "sets missing metadata fall into an 'Uncategorized' shelf (curation TODO surfaces honestly)" | `CardStock Mockup/uploads/CARDSTOCK_UI_SPEC_v1.md:201` | No such shelf and no honesty affordance (`:109-130`). |
| "'Uncategorized' shelf is the honest fallback until [era/release curation is] complete" | `CardStock Mockup/uploads/CARDSTOCK_UI_SPEC_v1.md:446` | Not implemented. The prototype's answer to missing era data is to **not group at all**. |
| "*Loading:* shelf skeletons" | `CardStock Mockup/uploads/CARDSTOCK_UI_SPEC_v1.md:201` | No loading state, no skeleton, no shimmer anywhere in the file. |
| "Pokédex filter menu = Type / Generation / Region / Status" (4 filters) | `CardStock Mockup/DESIGN_NOTES.md:71` and again at `:85` | **8 filters** (`ATTRS`, `:233-242`): Type, Generation, Region, Status, Evolution stage, Pokédex color, Egg group, Habitat — 48 seeded options total (11 / 5 / 5 / 2 / 3 / 9 / 6 / 7). |
| "species picker (search-as-you-type grid…)" | `CardStock Mockup/uploads/CARDSTOCK_UI_SPEC_v1.md:199` | **No search input exists** — 0 `<input>` elements in the file. `pokeQ` (`:209`), `pq` (`:231`) and `setPokeQ` (`:251`) are declared and never bound or read; filtering is attribute-only (`:243-244`). |
| "By pokémon = species grid… + filter box" | `CardStock Mockup/DESIGN_NOTES.md:71` | Filter *popover* exists (`:67-100`); the "filter box" text input does not. |
| "species grid ordered by total market value" / "grid ordered by total market value of printings" | `CardStock Mockup/DESIGN_NOTES.md:71`; `uploads/CARDSTOCK_UI_SPEC_v1.md:199` | The **caption** claims it (`:133`) but no sort is applied — `speciesAll` is a plain `.filter()` (`:243`) preserving literal array order. True of the seed by coincidence of authoring (`:189-204` is written descending). Must be made an explicit sort in the port. |
| Species tile shows "printings/sets" | `CardStock Mockup/DESIGN_NOTES.md:71` | Only `printings` is rendered (`:141`). `sets` is computed (`:307`) and never used. |
| "Only Evolving Skies links to a built Set page; others tooltip why" | `CardStock Mockup/DESIGN_NOTES.md:71` | **All 10** set tiles link to `Cardstock Set.dc.html` (`:172-181`); the tooltip explains the *data* is a stand-in ("prototype renders Evolving Skies data for every set"), not that the link is dead. |
| "character pages are P2 (#)" | `CardStock Mockup/DESIGN_NOTES.md:71` | Species tiles link to a real `Cardstock Character.dc.html` (`:310`). Superseded by the same file's later line 164 ("~~Character~~ DONE"); `:71` was not updated. |
| "no crops except the tile fan on Browse shelves" | `CardStock Mockup/uploads/CARDSTOCK_UI_SPEC_v1.md:338` | The fan exists (`:114-119`) but is not on a shelf, and only the **front** card can hold an image (`<image-slot>`, `:118`); the two rear cards are gradient-only (`:115-116`). |
| "Browse's era shelves" listed as a consumer of the set-metadata table | `DECISIONS.md:199` | Accurate about the *data gap* — `sets` has no era/release (`../PokemonInvestBatch/DATA_MODEL.md:139-146`) — but the dependency is **latent, not live**: the current prototype needs no set-metadata table because it does not group. |
| "/browse — By set and by Pokémon, attribute filters" | `CardStock Mockup/HANDOFF.md:75` | **Matches the HTML.** Recorded as a confirmed agreement, not a contradiction. |
| "no METADATA PENDING honesty state… on Pokédex attributes" | `CardStock Mockup/DESIGN_NOTES.md:70`; `HANDOFF.md:107` | **Matches** — the screen has no pending state on any species attribute (nor anywhere else). |
