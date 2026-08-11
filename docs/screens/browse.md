# Browse — screen specification

**Authority:** extracted from `CardStock Mockup/Cardstock Browse.dc.html` (318 lines), read directly 2026-08-10. That file is Tier 1 per `CLAUDE.md:20`. Every behavioural statement below carries an `HTML:line` citation. Where a Tier‑2/3 document disagrees, the HTML wins and the disagreement is recorded in §8 — nothing is averaged.

Line references written bare (`:113`) are lines in `Cardstock Browse.dc.html`. Other files are named.

---

## 1. Identity

| | |
|---|---|
| **Screen name** | Browse — `data-screen-label="Browse"` (`:35`), `<h1>Browse</h1>` (`:57`) |
| **Route** | `/browse` — **not in the HTML.** `HANDOFF.md:75` and `CARDSTOCK_UI_SPEC_v1.md:116` both say `/browse`; they agree, so this is safe. |
| **Nav position** | Fifth of five section links, rendered in its active form: weight 600, `--ink` text, 2px `--acc` bottom border (`:47`) vs weight 500 / `--mut` / transparent border for the other four (`:43–46`) |
| **Props** | **None.** `data-props=""` (`:159`). No `demoMode`, no `emptyState`. Consistent with `DESIGN_NOTES.md:141`. |
| **Purpose** | `CARDSTOCK_UI_SPEC_v1.md:198`: *"'show me what exists' — the wandering mode search can't serve."* The HTML supports this reading: two exhaustive catalogues (all sets, all species), no metrics beyond one delta per tile, no analysis controls. |
| **Position in hierarchy** | Root of `Browse → Set → Card` (`CARDSTOCK_UI_SPEC_v1.md:127`). Renders **no breadcrumb**, correctly — it is the root. |

**Chrome (identical to every app page):** 48px sticky nav, `z-index: 20` (`:37`); logo + wordmark → Home (`:39–40`); five section links (`:43–47`); flex spacer (`:49`); `<cardstock-search>` (`:50`); 28px circular account initial "O" → Profile (`:51`). Main is `max-width: 1480px`, `padding: 14px 20px 28px`, `flex-direction: column`, `gap: 18px` (`:54`). Base font-size 15px (`:35`).

**Helmet (`:10–34`):** favicon; Inter / Inter Tight / JetBrains Mono from Google Fonts (`:14`); `image-slot.js` (`:15`); `cardstock-search.js` (`:32`); dark-theme token block (`:27–30`); CVD override `--neg2: #D55E00` (`:25`); pre-paint script reading `localStorage['cardstock-cvd']` and `['cardstock-theme']` (`:33`).

---

## 2. Layout — the two modes

One `state.mode` field, initialised to `'sets'` (`:209`), drives everything. Two derived booleans, `isSets` and `isPoke` (`:246`), gate three `<sc-if>` regions.

### 2.1 The mode switch

A two-button segmented control sits inline with the `<h1>`, inside a `1px --line` border, `border-radius: 6px`, `overflow: hidden`, `background: --card` (`:58`).

| Button | Label | Handler | Tooltip (`title`) |
|---|---|---|---|
| Left (`:59`) | `by set` | `modeSets` → `setState({mode:'sets'})` (`:247`) | "Browse by set — every release, its size, and its market value" |
| Right (`:60`) | `by pokémon` | `modePoke` → `setState({mode:'poke'})` (`:247`) | "Browse by Pokémon — every species and all of its printings" |

Both are JetBrains Mono 13px/600, `height: 28px`, `padding: 0 14px`, no border, `cursor: pointer`. Selection is expressed by colour only, computed in `renderVals`:

- Selected: `background: --acc`, `color: --card` (`:248–249`)
- Unselected: `background: --card`, `color: --mut` (`:248–249`)

There is no `aria-pressed`, no `role="tablist"`, and no text/shape difference between selected and unselected — **selection is conveyed by colour alone.** Flagged in §7.

### 2.2 Mode "by set" — `isSets` (`:109–130`)

A single `<section>` (`:110`) containing **one flat CSS grid**: `repeat(auto-fill, minmax(230px, 1fr))`, `gap: 12px` (`:111`). `<sc-for list="{{ allSets }}">` emits one anchor tile per set (`:112–127`).

**There are no era shelves.** `allSets` is the whole `SETS` array sorted alphabetically by name, mapped through `mkSet` (`:230`). `mkSet` (`:224–229`) does not read `era`, and the template never mentions it. `this.ERAS` (`:183–187`) is declared in the constructor and referenced nowhere else in the file (single occurrence, verified by grep). See §8 rows 1–5.

No heading, no group label, no count, no sort control, no pagination, no empty state.

### 2.3 Mode "by pokémon" — `isPoke` (`:65–107` and `:132–154`)

Two separate `sc-if isPoke` regions, in DOM order:

1. **Filter bar** (`:66–106`) — `+ filter` button and its popover, then the applied-filter chips, a flex spacer, and the match count. `margin-top: -6px` pulls it tight to the header.
2. **Results** — a caption, *"Ordered by total market value across all printings"* (`:133`, 12.5px `--mut2`, `margin-top: -8px`); then a grid `repeat(auto-fill, minmax(190px, 1fr))`, `gap: 12px` (`:134`) of species tiles (`:135–149`); then the no-match panel (`:151–153`).

### 2.4 How switching works — and what it does not do

- Mode lives **only** in component state (`:209`). Not in the URL, not in `localStorage`, not in a prop.
- Switching **does not clear** `pokeFilters`. Go to by‑pokémon, apply `type = Fire`, switch to by‑set, switch back — the chip is still there and still applied (`:209`, `:291`; nothing in `modeSets`/`modePoke` at `:247` touches filter state).
- Switching does **not** close an open filter popover: `pAddOpen`/`pEditor` are untouched by the mode handlers (`:247`). The popover is simply unmounted with its `sc-if` and reappears open on return.
- The filter bar exists **only** in by‑pokémon mode. There is no way to filter, sort, or search sets.

---

## 3. Data contract — every rendered field

### 3.1 Set tile

Source array `this.SETS` (`:171–182`), mapped by `mkSet` (`:224–229`), rendered at `:113–126`.

| View field | Source field | Type | Transform | Rendered at |
|---|---|---|---|---|
| `href` | `s.href` | string | passthrough | `:113` — anchor target |
| `tip` | `s.tip` | string | passthrough | `:113` — `title` attribute |
| `fan1` | `s.fans[0]` | `[hex, hex]` | `linear-gradient(160deg, a, b)` (`:221`) | `:117` — **front** card |
| `fan2` | `s.fans[1]` | `[hex, hex]` | same | `:116` — back-right card |
| `fan3` | `s.fans[2]` | `[hex, hex]` | same | `:115` — back-left card |
| `slotId` | `s.name` | string | `'art-set-' + name.toLowerCase().replace(/[^a-z0-9]+/g,'-')` (`:227`) | `:118` — `<image-slot id>` |
| `name` | `s.name` | string | passthrough | `:121` — Inter Tight 600 / 15.5px / centred |
| `count` | `s.count` | int | `String(count)`, rendered as `{count} cards` | `:123` — JetBrains Mono 12px, `--mut2` |
| `chg` | `s.chg` | number (percent) | `pct()` (`:222`), rendered as `{chg} 30d` | `:124` — JetBrains Mono 12px |
| `chgFg` | `s.chg` | colour | `fgOf()` (`:223`) — `--pos` if `>= 0`, `--neg2` if `< 0` | `:124` |

**`s.era` is carried in the seed data and never read.** Values present: `'WOTC'`, `'Sun & Moon'`, `'Sword & Shield'`, and `null` on Vivid Voltage (`:181`). `mkSet` does not copy it; the template does not reference it.

Seeded sets (`:172–181`), illustrative — 10 rows, but `DATA_MODEL.md:135` says ~303 sets exist:

| Name | `era` | `count` | `chg` |
|---|---|---|---|
| Base Set | WOTC | 102 | +1.8 |
| Neo Genesis | WOTC | 111 | +0.9 |
| Hidden Fates | Sun & Moon | 163 | +2.6 |
| Sword & Shield | Sword & Shield | 216 | −0.4 |
| Evolving Skies | Sword & Shield | 237 | +4.1 |
| Fusion Strike | Sword & Shield | 284 | +1.2 |
| Brilliant Stars | Sword & Shield | 186 | +2.9 |
| Lost Origin | Sword & Shield | 217 | +3.4 |
| Silver Tempest | Sword & Shield | 215 | +1.7 |
| Vivid Voltage | **`null`** | 203 | +0.6 |

### 3.2 Era table — as the HTML has it

`this.ERAS` (`:183–187`) — **three** entries, declared and never rendered:

| `era` | `years` | Source line |
|---|---|---|
| WOTC | 1999–2003 | `:184` |
| Sun & Moon | 2017–2019 | `:185` |
| Sword & Shield | 2020–2022 | `:186` |

Year separators are en dashes. This is the complete era vocabulary in this file. Docs assert eight — see §8 rows 2 and 3.

**Why this matters for implementation.** Every era shelf the documents describe depends on a set-metadata table that does not exist. Verified directly in the sibling repo, 2026-08-10: `../PokemonInvestBatch/DATA_MODEL.md:139–146` lists the entire `sets` table as `id`, `slug`, `name`, `discovered_at`, `last_seen_at`, `last_walked_at` — **no era, no series, no release date**, and the table is described as "enumeration bookkeeping only" (`:135–137`). The gap is registered in `DECISIONS.md:199` (D-042 harvest of `PROJECT_LOG.md:218`), which names a **set metadata table** (release date + era/series for ~303 sets) as a prerequisite for "Browse's era shelves, the Set and Character pages, and the Screener's Era and Character filters," and states it does not exist. `CARDSTOCK_UI_SPEC_v1.md:200` and `:382` name it `set_metadata(set_id, released_on, era)`, static and hand-curated.

*Citation correction:* the era/release-date gap is **not** what D-004 says. D-004 (`DECISIONS.md:61–63`) records that there is no index table and no metrics table among the eight DbSets; it does not mention `sets` columns. The correct receipts for "no era, no release date" are `DATA_MODEL.md:139–146` (primary) and `DECISIONS.md:199` (the derived record).

### 3.3 Species tile

Source `this.SPECIES` (`:188–205`), mapped at `:304–311`, rendered at `:136–148`.

| View field | Source | Transform | Rendered at |
|---|---|---|---|
| `href` | literal | `'Cardstock Character.dc.html'` for every species (`:310`) | `:136` |
| `tip` | computed | `'Character page for ' + name + ' — prototype renders Umbreon data for every species'` (`:310`) | `:136` |
| `accent` | `SPECIES_ACCENTS[name]` (`:207`) | `linear-gradient(160deg, a, b)`; fallback `['#8A9BB8','#D6E0EC']` (`:306`) | `:138` — 44px circle |
| `initial` | `name[0]` (`:305`) | first character | `:138` — Inter Tight 700/17px, `rgba(255,255,255,0.92)` |
| `name` | `s.name` | passthrough | `:140` — Inter Tight 600/15.5px, ellipsis on overflow |
| `printings` | `s.printings` | `String()` (`:307`), rendered `{n} printings` | `:141` — JetBrains Mono 11.5px `--mut2` |
| `value` | `s.value` | `'$' + (v >= 1000 ? Math.round(v/1000) + 'K' : v)` (`:308`) | `:145` — JetBrains Mono 14.5px/700 |
| `chg` | `s.chg` | `pct()`, rendered `{chg} 90d` | `:146` — JetBrains Mono 12px |
| `chgFg` | `s.chg` | `fgOf()` | `:146` |

**Computed but never rendered:** `sets` (`:307`), `type` (`:305`), `gen` (`:305`). `type` and `gen` are also used by the filter predicate; `sets` is used by nothing. See §8 row 10.

**Never surfaced anywhere** — filter-only attributes: `status`, `stage`, `color`, `egg`, `habitat` (`:189–205`, consumed only at `:233–244`).

Seeded species, in array order (`:189–204`) — 16 rows. Fields per row: `name, type, gen, status, stage, color, egg, habitat, printings, sets, value, chg`. Illustrative only; the array order happens to be value-descending, which is what makes the caption at `:133` look true (see §6).

`this.REGIONS` (`:206`) — the complete generation→region map, 9 entries: `1 Kanto · 2 Johto · 3 Hoenn · 4 Sinnoh · 5 Unova · 6 Kalos · 7 Alola · 8 Galar · 9 Paldea`. Region is **derived from generation**, never stored on a species — matching `DESIGN_NOTES.md:71`.

### 3.4 Filter bar

| View field | Meaning | Rendered at |
|---|---|---|
| `pAddOpen` | popover open | `:69` |
| `pShowMenu` | `pAddOpen && !pEditor` (`:255`) — attribute list visible | `:71` |
| `pShowEditor` | `!!pEditor` (`:256`) — option editor visible | `:80` |
| `pMenu[]` | `{ name, tip, add }` per attribute (`:257–266`) | `:73–78` |
| `pEdName` | display name of the attribute being edited (`:267`) | `:83` |
| `pEdOpts[]` | `{ label, mark, tip, bg, bd, pick }` per option (`:269–281`) | `:86–91` |
| `pEdPreview` | expression preview or `pick at least one` (`:288`) | `:94` |
| `pEdAddOff` / `pEdAddBg` / `pEdAddCur` | Add button disabled / background / cursor (`:283`, `:289`) | `:95` |
| `pokeChips[]` | `{ label, remove }` per applied filter (`:296–302`) | `:101–103` |
| `speciesCount` | `` `${matched} of ${total} species` `` (`:303`) | `:105` |
| `pokeNoMatch` | `matched === 0` (`:312`) | `:151` |

**Dead view fields — no bound element exists:** `pokeQ` and `setPokeQ` (`:251`), backed by `state.pokeQ` (`:209`) and the unused local `pq` (`:231`). There is no text input anywhere in the markup. See §8 row 9.

### 3.5 Attribute filters — complete enumeration

`ATTRS` (`:233–242`). Menu section header is **"Pokédex"** (`:72`). Every attribute's tooltip is generated: `'Filter species by ' + name.toLowerCase() + ' — fixed Pokédex data'` (`:259`).

| # | Key | Display name | Option source | Option ordering | Option label | Line |
|---|---|---|---|---|---|---|
| 1 | `type` | Type | distinct `s.type` | default lexicographic | value verbatim | `:234` |
| 2 | `gen` | Generation | distinct `s.gen` | numeric ascending | `'Gen ' + v` | `:235` |
| 3 | `region` | Region | distinct `REGIONS[s.gen]` | by position in `REGIONS` (Kanto→Paldea) | value verbatim | `:236` |
| 4 | `status` | Status | **fixed list** `['Ordinary','Legendary','Mythical']`, filtered to values present in the population | declaration order | value verbatim | `:237` |
| 5 | `stage` | Evolution stage | **fixed list** `['Basic','Stage 1','Stage 2']`, filtered to values present | declaration order | value verbatim | `:238` |
| 6 | `color` | Pokédex color | distinct `s.color` | default lexicographic | value verbatim | `:239` |
| 7 | `egg` | Egg group | distinct `s.egg` | default lexicographic | value verbatim | `:240` |
| 8 | `habitat` | Habitat | distinct `s.habitat` | default lexicographic | value verbatim | `:241` |

Eight filters, not four. See §8 row 6.

**Option values as they resolve against the seeded population** (structure is authoritative; membership is a function of the data):

| Filter | Options rendered from the seed |
|---|---|
| Type | Dark, Dragon, Fairy, Fighting, Fire, Ghost, Grass, Ice, Normal, Psychic, Water |
| Generation | Gen 1, Gen 2, Gen 3, Gen 4, Gen 6 |
| Region | Kanto, Johto, Hoenn, Sinnoh, Kalos |
| Status | Ordinary, Legendary — **Mythical is filtered out**, no seeded species has it (`:237`) |
| Evolution stage | Basic, Stage 1, Stage 2 |
| Pokédex color | Black, Blue, Brown, Gray, Green, Pink, Purple, Red, White |
| Egg group | Amorphous, Field, Human-Like, Monster, No eggs, Water 1 |
| Habitat | Cave, Forest, Mountain, Rare, Snow, Urban, Waters-edge |

**Design consequence to carry into implementation:** six of the eight option lists are derived from the *loaded species population*, so the vocabulary shrinks with the corpus, and two (`status`, `stage`) are fixed candidate lists intersected with the population. Whether production shows the full Pokédex vocabulary or only present values is unresolved — §7.

---

## 4. States — complete state space

`state = { mode, pokeQ, pokeFilters, pAddOpen, pEditor }` (`:209`). `pokeQ` is inert. The reachable space:

### 4.1 Mode

| State | Trigger | Renders |
|---|---|---|
| **S1 — By set** (initial) | default `mode:'sets'` (`:209`); `by set` click (`:59`, `:247`) | Flat set grid (`:109–130`). Filter bar absent. |
| **S2 — By pokémon** | `by pokémon` click (`:60`, `:247`) | Filter bar (`:65–107`) + species grid (`:132–154`). Set grid absent. |

### 4.2 Filter popover — by-pokémon only

| State | Trigger | Renders |
|---|---|---|
| **S3 — Closed** (initial) | `pAddOpen:false` (`:209`); toggle off (`:253`); mouseleave with no editor (`:254`, `:70`); mousedown outside `[data-pfilter-pop]` (`:212`) | `+ filter` button only |
| **S4 — Attribute list** | `+ filter` click while closed (`:253`); back `‹` from editor (`:268`, `:82`) | Popover: "Pokédex" header + 8 attribute rows with `›` chevrons (`:71–79`) |
| **S5 — Option editor, nothing picked** | attribute row click when that attribute has no existing filter (`:260–265`) | Header `‹ {pEdName}` (`:81–84`); option rows with empty checkboxes; preview `pick at least one`; **Add disabled** (`--accMut`, `cursor: not-allowed`) (`:283`, `:288–289`) |
| **S6 — Option editor, ≥1 picked** | option row click (`:275–279`); or opening an attribute that already has a chip, which pre-seeds the draft (`:262–263`) | Checked boxes show `✓` on `--acc` (`:272`, `:274`); preview `attr = X` or `attr ∈ X, Y` (`:288`); **Add enabled** (`--acc`, `cursor: pointer`) |

The editor holds a **draft** (`pEditor.sel`). Toggling options mutates only the draft; results do not change until **Add** (`:290–293`). Leaving via `‹` or an outside click discards it.

### 4.3 Results

| State | Trigger | Renders |
|---|---|---|
| **S7 — No filters** | `pokeFilters: []` (`:209`) | All species; no chips; count reads `16 of 16 species` (`:303`) |
| **S8 — Filtered, matches exist** | ≥1 chip, `speciesAll.length > 0` (`:243–244`) | Chips (`:101–103`), reduced grid, `N of 16 species` |
| **S9 — Filtered, no matches** | `speciesAll.length === 0` → `pokeNoMatch` (`:312`) | Chips + an **empty grid element** (the `sc-for` emits nothing but the grid container remains) + a panel: **"No species match these filters — remove one to widen the net."** (`:152`) — `--card` background, 1px `--line`, radius 10, 40px padding, centred, 14px `--mut2` |

### 4.4 Presentation states

| State | Trigger |
|---|---|
| **S10 — Light theme** (default) | absence of `cardstock-theme=dark` in `localStorage` (`:33`, `:167`) |
| **S11 — Dark theme** | `localStorage['cardstock-theme'] === 'dark'` → `data-theme="dark"` set pre-paint (`:33`); tokens at `:27–30`, JS mirror at `:166` |
| **S12 — CVD palette** | `localStorage['cardstock-cvd'] === '1'` → `data-cvd="1"` (`:33`); `--neg2` becomes `#D55E00` (`:25`); JS palette swaps to blue/orange (`:162`, `:164`) |
| **S13 — Positive delta** | `chg >= 0` → `--pos`, leading `+` (`:222–223`) |
| **S14 — Negative delta** | `chg < 0` → `--neg2`, leading `−` U+2212 (`:222–223`) |
| **S15 — Reduced motion** | `prefers-reduced-motion` → animations clamped to 0.01ms (`:23`) |

### 4.5 Image-slot states

| State | Trigger |
|---|---|
| **S16 — Set art empty** (what this prototype actually ships) | No `art-set-*` entry exists in `CardStock Mockup/.image-slots.state.json` — it holds only the Landing page's eight slots (`hero-card-*`, `features-card*`, `data-card*`), verified 2026-08-10. `placeholder=" "` (`:118`) combined with `image-slot[placeholder=" "]::part(empty) { opacity: 0 }` (`:22`) hides the empty-state chrome entirely, so the front card renders as **pure gradient**. This is deliberate, not a broken image. |
| **S17 — Set art filled** | An image assigned to `art-set-{slug}`; it paints inside the 78×108 front card at `radius 5`, covering the `fan1` gradient |

### 4.6 States the HTML does **not** implement

Recorded because documents assert them and an implementer will look for them:

- **METADATA PENDING** — zero occurrences of `PENDING` in the file. No badge, no honesty affordance, no consumer of `era: null`.
- **"Uncategorized" shelf** — zero occurrences of `Uncategorized` or `shelf`. `Vivid Voltage`'s `era: null` (`:181`) is read by nothing; it sorts alphabetically into the flat grid like every other set.
- **Loading / skeleton** — none, in either mode.
- **Error** — none.
- **Empty catalogue** — by-set has no counterpart to `pokeNoMatch`; an empty `SETS` array would render an empty grid with no message.
- **Species search** — no input element (see §3.4).

---

## 5. Interactions

| # | Control | Line | Consequence |
|---|---|---|---|
| 1 | `by set` button | `:59`, `:247` | `mode:'sets'`. Set grid mounts, filter bar and species grid unmount. Filter state preserved. |
| 2 | `by pokémon` button | `:60`, `:247` | `mode:'poke'`. Filter bar + species grid mount with previously applied filters still active. |
| 3 | `+ filter` button | `:68`, `:253` | Toggles `pAddOpen` **and always clears `pEditor`** — so clicking it while an editor is open closes the whole popover, discarding the draft. |
| 4 | Popover `mouseleave` | `:70`, `:254` | Closes **only if no editor is open** (`if (!this.state.pEditor)`). Moving the mouse out of the attribute list dismisses it; moving out of the editor does not. |
| 5 | Document `mousedown` outside `[data-pfilter-pop]` | `:67`, `:211–214` | Closes popover and clears the editor. Guarded by `e.target.isConnected`, so a click on an element removed during the same event does not trigger it. Listener added on mount (`:214`), removed on unmount (`:217`). |
| 6 | Attribute row (8 of them) | `:74–77`, `:260–265` | Opens the editor for that attribute. **If a chip already exists for it, the draft is pre-seeded with the chip's current values** (`:262–263`) — editing, not restarting. |
| 7 | `‹` back | `:82`, `:268` | `pEditor: null` → returns to the attribute list with the popover still open. Draft discarded; `aria-label="Back to attributes"`. |
| 8 | Option row | `:87–90`, `:275–279` | Toggles that value in the draft. Checkbox fills `--acc` with `✓`, or empties to `--card` with a `--line3` border (`:272`, `:274`). **Results do not change.** Tooltip flips between "Include X in the results" and "Stop including X" (`:273`). |
| 9 | `Add` | `:95`, `:290–293` | No-op when nothing is picked (disabled attribute + early return). Otherwise: **replaces** any existing filter for that attribute (`filter(f => f.attr !== attr).concat([...])`), then closes editor *and* popover. Results and count recompute. |
| 10 | Chip `✕` | `:102`, `:300` | Removes that attribute's filter entirely (all its values). `aria-label="Remove filter"`. Hover turns it `--neg2`. |
| 11 | Set tile | `:113` | Navigates to `Cardstock Set.dc.html`. **All ten sets share this one href** (`:172–181`). Hover: `box-shadow 0 6px 20px rgba(20,19,26,0.10)`, 0.15s ease, underline suppressed. |
| 12 | Species tile | `:136`, `:310` | Navigates to `Cardstock Character.dc.html`. Same href for every species. Same hover treatment. |
| 13 | Nav links / logo / account circle | `:39–47`, `:51` | Home, Screener, Charts, Binder, Browse, Profile. Logo has `aria-label="Cardstock home"`. |
| 14 | `<cardstock-search>` | `:50` | Shared web component. Corpus is frozen inside `cardstock-search.js`: 16 species (`:6`), 10 sets (`:7`), 5 cards (`:16–20`); fires at ≥2 chars, caps at 4 characters / 4 sets / 5 cards (`:36–41`); rows link to Character / Set / Card pages. |
| 15 | Keyboard focus | `:21` | `*:focus-visible` → 2px `--acc` outline, `outline-offset: 1px`. Every control is a real `<button>` or `<a>`, so tab order is natural. |

---

## 6. Rules and invariants

**Filter algebra**
1. **At most one filter per attribute.** Enforced at add time: the new condition is concatenated after removing any prior condition on the same attribute (`:291`). Chips are therefore 1:1 with attributes, max 8.
2. **Within an attribute: OR.** `f.vals.some(...)` (`:244`).
3. **Across attributes: AND.** `st.pokeFilters.every(...)` (`:243–244`). Matches the `+ filter` tooltip: *"results must satisfy every filter"* (`:68`).
4. **Comparison is string-coerced on both sides** — `String(v) === String(ATTRS[f.attr].of(s))` (`:244`). Load-bearing for `gen`, whose source values are numbers and whose draft keys are strings (`:263`, `:277`).
5. **A filter cannot be added with zero values** (`:286`, `:290`). There is no "match nothing" state reachable from an empty selection.
6. **Draft edits are transactional** — nothing takes effect until `Add`; back / outside-click / `+ filter` all discard.

**Ordering**
7. **Sets are ordered alphabetically ascending by name** (`:230`), on a copy (`slice()`). Not by era, not by release date, not by value. The comparator `(a,b) => a.name < b.name ? -1 : 1` returns `1` for equal names — harmless here, but an implementation should use a total ordering.
8. **Species are not sorted at all.** `speciesAll` is `SPECIES.filter(...)` (`:243`), which preserves array order, and the mapper (`:304`) preserves it again. The caption *"Ordered by total market value across all printings"* (`:133`) is true only because the seed array happens to be value-descending. **A real implementation must apply `ORDER BY total_value DESC` explicitly** or the caption becomes a lie.

**Formatting**
9. `pct(n)` (`:222`) — sign, then `Math.abs(n).toFixed(1)`, then `%`. Positive/zero prefix is ASCII `+`; negative prefix is **U+2212 MINUS SIGN**, not a hyphen.
10. `fgOf(n)` (`:223`) — `>= 0` is positive-coloured. **Zero renders as `+0.0%` in the positive colour.**
11. Sign is always printed, so direction never depends on colour alone — honouring the spec invariant at `CARDSTOCK_UI_SPEC_v1.md:11`. (The mode toggle at `:59–60` does *not* honour it; see §7.)
12. Money (`:308`) — `$` + `Math.round(value/1000) + 'K'` at or above 1000, else the bare integer. No thousands separators, no cents, no locale formatting.
13. Set delta window is **30d** (`:124`); species delta window is **90d** (`:146`). Both are literal text in the template, not data.
14. `count` and `printings` are stringified in the mapper (`:225`, `:307`), never formatted — a 4-digit count would render unseparated.

**Counting**
15. `speciesCount` (`:303`) is `matched of TOTAL`, where TOTAL is the entire species list, never the filtered one.

**Labelling**
16. **Chips and the editor preview use the raw attribute *key*, not the display name** (`:288`, `:299`). The menu row says "Egg group"; the chip says `egg = Field`. Likewise `gen`, `habitat`, `stage`, `color`. Option labels *do* use the display transform (`Gen 1`). This asymmetry is in the HTML and should be treated as intentional terminal-style shorthand unless the owner rules otherwise.
17. Operators in chip/preview text: `=` for one value, `∈` (U+2208) for many (`:288`, `:299`).

**Imagery — "where card images earn their keep"**
18. **Only set tiles have an image slot.** Species tiles carry a 44px gradient circle with a letter (`:138`), not art (`:136–148`). On this screen the phrase from `PROJECT_LOG.md:214` applies to the set fan alone.
19. The fan is **three rectangles, one slot** (`:114–119`), inside a 118px-tall relatively-positioned box:
    - back-left `fan3`: **74×102**, `top: 4px`, `translateX(-88%) rotate(-8deg)`, radius 5, shadow `0 3px 10px` (`:115`)
    - back-right `fan2`: **74×102**, `top: 4px`, `translateX(-12%) rotate(8deg)`, radius 5, same shadow (`:116`)
    - front `fan1`: **78×108**, `top: 0`, `translateX(-50%)`, radius 5, shadow `0 5px 14px` (`:117`) — the only one containing `<image-slot>` (`:118`)
20. 78×108 is **0.7222**, exactly the 325×450 portrait card ratio the spec mandates (`CARDSTOCK_UI_SPEC_v1.md:338`). The slot uses `shape="rounded" radius="5"` to match the card corner.
21. **Unfilled slots are invisible by design** — `placeholder=" "` plus the `::part(empty)` opacity rule (`:22`, `:118`). The gradient is the fallback art, so a set with no images still reads as a set of cards.
22. Slot ids are derived, not authored: `art-set-{slugified name}` (`:227`) — e.g. `art-set-base-set`, `art-set-sword-shield`. **The slug collapses runs of non-alphanumerics to a single `-`**, so two sets differing only in punctuation would collide.

**Tile geometry**
23. Set tile: min column 230px, `padding: 14px`, `radius: 10px`, 1px `--line` border, `--card` background (`:111`, `:113`).
24. Species tile: min column 190px, `padding: 13px`, `radius: 10px`, same border/background (`:134`, `:136`).
25. Both grids are `auto-fill` — column count is viewport-driven, and neither grid paginates or virtualizes.

**Structural**
26. Both catalogues render **every** row in one pass. With ~303 sets (`DATA_MODEL.md:135`) and a full species list, this needs a paging or virtualization decision the prototype does not make.
27. `hint-placeholder-count` / `hint-placeholder-val` attributes (`:65`, `:73`, `:86`, `:101`, `:112`, `:135`) are Design Composer authoring hints for static preview — **not** runtime defaults and not part of the contract.

---

## 7. Open questions

1. **Era shelves: build them or not?** The HTML is Tier 1 and renders a flat alphabetical grid. But `this.ERAS` (`:183–187`) and `SETS[].era` sit in the file unused, which reads like an intent that was cut or never wired. Owner ruling needed: ship the flat grid, or implement the shelves the documents describe.
2. **If shelves: which era vocabulary?** Three eras with 1999–2003 / 2017–2019 / 2020–2022 (`:184–186`), or the eight from `CARDSTOCK_UI_SPEC_v1.md:199` / `DISPLAY_VOCABULARY.md:123` with different end years? Browse and the Screener's Era filter must not diverge.
3. **Does the "Uncategorized" + METADATA PENDING honesty state ship?** Nothing in the HTML implements it. `era: null` exists in the data (`:181`) but drives no rendering. This is the only honesty affordance the docs claim for this screen.
4. **Species search box.** `pokeQ`/`setPokeQ`/`pq` are dead (`:209`, `:231`, `:251`) and both the spec (`:199`) and `DESIGN_NOTES.md:71` describe one. Wire an input, or delete the state?
5. **`sets` per species** — computed (`:307`), never rendered. Show "34 printings across 19 sets", or drop the field?
6. **Set routing.** All ten tiles point at the same flat filename (`:172–181`). `HANDOFF.md:75` says `/set/{id}`; `CARDSTOCK_UI_SPEC_v1.md:207` says `/set/{slug}` with a verbatim-URL rule. Same question for Character: `HANDOFF.md:75` says `/character/{name}`. Needs one answer before either page is built.
7. **Filter option vocabulary** — derived from the loaded population (six of eight attributes) or the full Pokédex vocabulary? Today `Mythical` silently disappears when no seeded species has it (`:237`). Empty-vocabulary and single-option cases are unspecified.
8. **Where do the species value and 90d delta come from?** No character aggregate exists — `card_characters` is unbuilt (`DECISIONS.md:199`) and no index table exists at all (D-004, `DECISIONS.md:61–63`). Both numbers on every species tile are currently unsourceable.
9. **Where does the set-index 30d move come from?** Same problem: `CARDSTOCK_UI_SPEC_v1.md:200` sources it from an `indices` table that does not exist (D-004).
10. **Where do the fan gradients come from?** Hard-coded triples per set in the prototype (`:172–181`). `CARDSTOCK_UI_SPEC_v1.md:207` proposes a derived `card_accents` table; `DECISIONS.md` D-042 flags that a derived accent column on `cards` would mean writing to the scraper's tables (open — D-026).
11. **Which images fill the front slot?** `CARDSTOCK_UI_SPEC_v1.md:200` says top cards by latest PSA-10 price. Only one slot exists, so "top-3" needs reducing to "top-1" or the fan needs two more slots. And `HANDOFF.md` §6 records that all card imagery is an unresolved **licensing** question — the largest open risk on this screen.
12. **Scale strategy** — ~303 sets and a full species list in unpaginated `auto-fill` grids. Virtualize, page, or lazy-load?
13. **Loading, error, and empty-catalogue states** are undesigned in both modes.
14. **URL state.** Mode and filters live only in component state. Should `/browse` accept `?mode=` and filter params so a filtered view is linkable and back-button-safe?
15. **Accessibility gap to close, not to copy.** The mode toggle signals selection by colour alone (`:248–249`) with no `aria-pressed` or `role="tablist"`; the filter popover has no `role="dialog"`/`aria-expanded` and no focus trap or Escape handler (dismissal is mouse-only — `:70`, `:212`). The prototype is a visual document, not an a11y reference.
16. **Chip labels use raw attribute keys** (`egg = Field`, `:299`). Intentional terminal shorthand, or a prototype shortcut to clean up?

---

## 8. Contradictions found

Every row: the document's claim, the exact source line, and what `Cardstock Browse.dc.html` actually does. The HTML wins in all of them.

| # | Claim | Source doc:line | What the HTML actually does |
|---|---|---|---|
| 1 | "By set = era shelves (WOTC / Sun & Moon / Sword & Shield + 'Uncategorized' shelf w/ METADATA PENDING badge — honesty state from spec §4.8)" | `CardStock Mockup/DESIGN_NOTES.md:71` | **No shelves exist.** `allSets` is one flat grid sorted alphabetically by name (`:230`, `:111–128`). `mkSet` never copies `era` (`:224–229`); the template never mentions it. `this.ERAS` (`:183–187`) is declared once and referenced nowhere. Zero occurrences of `Uncategorized`, `shelf`, or `PENDING` in the file. |
| 2 | "*By set:* era shelves (WOTC, EX, DP, BW, XY, SM, SWSH, SV…)" — eight eras | `CardStock Mockup/uploads/CARDSTOCK_UI_SPEC_v1.md:199` | The file's only era list has **three** entries — WOTC, Sun & Moon, Sword & Shield (`:184–186`) — with different labels from the spec's abbreviations, and it is never rendered. |
| 3 | Era year ranges "WOTC (1999–03) · … · SM (2017–20) · SWSH (2020–23) · SV (2023– )" | `CardStock Mockup/DISPLAY_VOCABULARY.md:123` | End years differ from the HTML: WOTC **1999–2003**, Sun & Moon **2017–2019**, Sword & Shield **2020–2022** (`:184–186`). Only three of the eight ranges have any counterpart here. |
| 4 | "*Maturity:* sets missing metadata fall into an 'Uncategorized' shelf (curation TODO surfaces honestly)" / "'Uncategorized' shelf is the honest fallback until complete" | `…/CARDSTOCK_UI_SPEC_v1.md:201` and `:446` | Not implemented. The only trace is `era: null` on Vivid Voltage (`:181`), which nothing reads — it sorts into the flat alphabetical grid indistinguishably from the other nine sets. |
| 5 | "*Loading:* shelf skeletons" | `…/CARDSTOCK_UI_SPEC_v1.md:201` | No loading state, skeleton, or spinner anywhere in the file, in either mode. |
| 6 | "Pokédex filter menu = Type / Generation / Region / Status" — repeated as "Browse species filter = Type/Generation/Region/Status" | `CardStock Mockup/DESIGN_NOTES.md:71` and `:85` | **Eight** filters, not four: Type, Generation, Region, Status, Evolution stage, Pokédex color, Egg group, Habitat (`:233–242`). |
| 7 | "Only Evolving Skies links to a built Set page; others tooltip why" | `CardStock Mockup/DESIGN_NOTES.md:71` | All ten sets link to `Cardstock Set.dc.html` (`:172–181`). The tooltips do not explain a missing page — they say "prototype renders Evolving Skies data for every set", i.e. the page exists and the *data* is reused. |
| 8 | "character pages are P2 (#)" | `CardStock Mockup/DESIGN_NOTES.md:71` | Every species links to `Cardstock Character.dc.html` (`:310`). No `#` placeholder hrefs remain. (`DESIGN_NOTES.md:164` separately records Character as DONE — the same file contradicts itself.) |
| 9 | "species picker (search-as-you-type grid ordered by total market value of printings)" / "species grid … + filter box" | `…/CARDSTOCK_UI_SPEC_v1.md:199`; `DESIGN_NOTES.md:71` | **No search input exists.** `state.pokeQ` (`:209`), `setPokeQ` (`:251`) and the local `pq` (`:231`) are declared and bound to nothing. Narrowing is by attribute chips only. |
| 10 | "species grid … (accent initial circle, **printings/sets**, total value, 90d %)" | `CardStock Mockup/DESIGN_NOTES.md:71` | Tiles render `printings` only (`:141`). `sets` is computed into the view model (`:307`) and never displayed. |
| 11 | "fan of **top-3 chase-card images**" | `…/CARDSTOCK_UI_SPEC_v1.md:199` | The fan is three gradient rectangles; only the front one holds an `<image-slot>` (`:115–119`). The two back cards can never show art. |
| 12 | "grid **ordered by** total market value of printings" — restated on-screen as "Ordered by total market value across all printings" | `…/CARDSTOCK_UI_SPEC_v1.md:199`; HTML caption `:133` | **No sort is applied.** `speciesAll` is a plain `.filter()` (`:243`) over `SPECIES` in literal array order (`:189–204`). The caption is a claim about data the code does not enforce — the seed array merely happens to be value-descending. |
| 13 | "shelves grouped by era, tiles show card count + set-index 30d move" | `CardStock Mockup/uploads/PROJECT_LOG.md:214` (Tier 3) | Tiles do show count + 30d (`:123–124`), so the second half holds. The shelves do not exist (see row 1). |
| 14 | Route `/set/{id}` | `CardStock Mockup/HANDOFF.md:75` (vs `/set/{slug}` at `…/CARDSTOCK_UI_SPEC_v1.md:207`) | The HTML settles neither — every tile points at the bare filename `Cardstock Set.dc.html` (`:172–181`), encoding no id and no slug. Doc-vs-doc, unresolved by Tier 1. |

**One citation correction, not a contradiction.** It is sometimes said that D-004 confirms the `sets` table carries no era and no release date. D-004 (`DECISIONS.md:61–63`) says something narrower — there is no index table and no metrics table among the eight DbSets — and never mentions `sets` columns. The claim is nonetheless **true**, verified directly 2026-08-10 against `../PokemonInvestBatch/DATA_MODEL.md:139–146`, which lists the whole table as `id`, `slug`, `name`, `discovered_at`, `last_seen_at`, `last_walked_at`, describing it (`:135–137`) as "enumeration bookkeeping only". The derived record lives at `DECISIONS.md:199`. Cite `DATA_MODEL.md:139–146`, not D-004.

**Where the docs are right:** `HANDOFF.md:75` — "By set and by Pokémon, attribute filters" — is an accurate one-line summary, and `DESIGN_NOTES.md:71`'s description of the set tile (3-card fan with accent gradients, front card an image-slot, name, count, 30d %) and of region-derived-from-generation matches the HTML exactly. `DESIGN_NOTES.md:70`'s ruling that Pokédex attributes need no METADATA PENDING state is consistent with what is built.
