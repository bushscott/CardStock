# Shared components and app chrome

**Authority.** Everything below is read from the prototypes (Tier 1: `CardStock Mockup/*.dc.html`, `cardstock-search.js`, `image-slot.js`), verified 2026-08-10. Where a Tier 2/3 doc disagrees, the code is recorded as correct and the disagreement is listed in §8.

**Scope.** These are the components a Blazor rebuild should build **once** and reuse. Paths below are relative to `/Users/scott/RiderProjects/CardStock/CardStock Mockup/` unless absolute.

---

## 1. App chrome

### 1.1 Which pages have it

Eleven pages carry the 48px app nav, verified by locating `height: 48px; background: var(--card…` in each file:

| Page | Nav line | Active tab |
|---|---|---|
| `Cardstock Home.dc.html` | 39 | Home |
| `Cardstock Screener.dc.html` | 36 | Screener |
| `Cardstock Charts.dc.html` | 35 | Charts |
| `Cardstock Binder.dc.html` | 39 | Binder |
| `Cardstock Browse.dc.html` | 37 | Browse |
| `Cardstock Set.dc.html` | 37 | Browse |
| `Cardstock Character.dc.html` | 37 | Browse |
| `Cardstock Card.dc.html` | 37 | **none** (see §8) |
| `Cardstock Profile.dc.html` | 29 | none (account circle marks "you are here") |
| `Cardstock About Data.dc.html` | 32 | none |
| `Cardstock Legal.dc.html` | 28 | none |

Pages that deliberately do **not** get this nav: `Cardstock Account.dc.html` (auth, centred card, no nav at all — grep for `<nav` returns nothing), and the five marketing/system pages (`Cardstock Landing.dc.html:26`, `Binder Landing:24`, `Charts Landing:24`, `Screener Landing:24`, plus `Brand System`), which use a different sticky translucent nav — `background: rgba(241,241,236,0.92); backdrop-filter: blur(8px); z-index: 50`.

### 1.2 The bar

`Cardstock Home.dc.html:39` (identical string in `Cardstock Card.dc.html:37`):

```
height: 48px; background: var(--card, #FFFFFF); border-bottom: 1px solid var(--line, #E4E4E0);
display: flex; align-items: center; gap: 24px; padding: 0 20px;
position: sticky; top: 0; z-index: 20;
```

**One documented variant:** `Cardstock Screener.dc.html:36` replaces `position: sticky; top: 0` with `flex-shrink: 0` — the Screener page is a full-height flex column with its own internal scrollers, so the nav is pinned by layout rather than by stickiness. Every other app page is `position: sticky; top: 0`. A Blazor `<AppNav>` needs a parameter for this (e.g. `Sticky="false"` on Screener).

Children in order: logo block → tab strip → `<div style="flex: 1;">` spacer → `<cardstock-search>` → *(Charts only: two extra controls)* → account circle.

### 1.3 Logo link

`Cardstock Home.dc.html:41–42`. An `<a href="Cardstock Home.dc.html" aria-label="Cardstock home">` wrapping an inline 24×24 SVG (`viewBox="0 0 32 32"`, `aria-hidden="true"`) plus the wordmark `<span>` "Cardstock" — `Inter` 700, 18px, `letter-spacing: -0.03em`, `color: var(--ink)`. The anchor is `color: inherit; text-decoration: none`, overriding the global `a { color: var(--acc) }` rule.

The mark is two rotated card rects (back card `stroke: var(--ink)` rotated −12°; front card `fill: var(--card)`, `stroke: var(--ink)`) plus a rising polyline and end-dot in **`var(--logoTeal, #0E8A7B)`** — the only place teal is used in the product (`Cardstock Brand System.dc.html:101`: "Teal now lives in the logo only"). Dark theme overrides `--logoTeal: #3FBFAD` (`Cardstock Home.dc.html:32`).

### 1.4 The five section links

Container: `display: flex; gap: 2px; height: 100%` (`Home:44`). Exactly five links, always in this order, on every page that has the nav:

| Label | Target |
|---|---|
| Home | `Cardstock Home.dc.html` |
| Screener | `Cardstock Screener.dc.html` |
| Charts | `Cardstock Charts.dc.html` |
| Binder | `Cardstock Binder.dc.html` |
| Browse | `Cardstock Browse.dc.html` |

Inactive: `padding: 0 12px; font-weight: 500; font-size: 15px; color: var(--mut, #5B5B57); border-bottom: 2px solid transparent; margin-bottom: -1px`.
Active: `font-weight: 600; color: var(--ink, #1C1C1E); border-bottom: 2px solid var(--acc, #4A63D0)` (`Home:45`). The `margin-bottom: -1px` pulls the 2px underline over the nav's own 1px bottom border.

**Self-link href is inconsistent in the prototype.** Home (`Home:45`), Screener (`Screener:38`) and Charts (`Charts:40`) point their active tab at `href="#"`; Binder (`Binder:48`) and Browse (`Browse:47`) point theirs at their own real page. Blazor should pick one rule — the real href, so `NavLink` active matching works — and drop the `#` variant.

**Descendant pages light the ancestor tab.** Set (`Set:47`) and Character (`Character:47`) both mark **Browse** active. Card does not mark anything (§8).

### 1.5 Search entry point

`<cardstock-search></cardstock-search>` — bare element on ten pages (`About Data:42`, `Binder:52`, `Browse:50`, `Card:50`, `Character:50`, `Home:52`, `Screener:46`, `Profile:39`, `Legal:38`, `Set:50`). Charts is the exception: `Cardstock Charts.dc.html:45` sets `style="flex: 0 1 280px; min-width: 110px; width: auto;"` so it can compress — that nav also carries a watch button and a saved-views menu. Full spec in §2.

### 1.6 Account circle

`Cardstock Home.dc.html:53`:

```html
<a href="Cardstock Profile.dc.html" aria-label="Account" title="Profile & settings"
   style="width: 28px; height: 28px; border-radius: 50%;
          border: 1px solid var(--line, #E4E4E0); background: var(--mutbg, #F3F3EE);
          color: var(--mut, #5B5B57); font-weight: 600; font-size: 14px;
          text-decoration: none; flex-shrink: 0;">O</a>
```

It is a **link to the Profile page, not a dropdown menu** — there is no account menu in any prototype. The "O" is the demo user's initial (`otto@gmail.com`, `Cardstock Profile.dc.html:46`); in Blazor this is the signed-in user's initial.

**Current-page variant.** On Profile itself (`Cardstock Profile.dc.html:40`) the same circle becomes a non-interactive `<span>` with `title="You are here"`, `border: 1px solid var(--acc)` and `color: var(--acc)`. Same geometry, accent-coloured, no href.

### 1.7 Notification bell — confirmed gone, no remnant

`grep -rniI -e bell -e notification --include='*.html'` over `CardStock Mockup/` returns **zero hits in any `.dc.html` file**. No bell SVG, no unread badge, no `unreadAlerts` prop, no `/alerts` link. The account circle sits directly after the search element in every nav.

Every surviving mention is in prose or superseded spec text: `DESIGN_NOTES.md:80`, `DESIGN_NOTES.md:120`, `HANDOFF.md:97` (all recording the removal), and `uploads/PROJECT_LOG.md:183,190`, `uploads/CARDSTOCK_UI_SPEC_v1.md:93,127,256` (Tier 3, describing the bell as if it exists — do not implement from those; see §8).

**Do not build a bell.** Do not build an alert badge, an alert centre, or an "Email me" control.

---

## 2. Nav search — `<cardstock-search>`

Source: `cardstock-search.js` (129 lines, self-registering IIFE, guarded by `if (customElements.get('cardstock-search')) return;` at line 5). Loaded per page as `<script src="./cardstock-search.js"></script>` inside `<helmet>` (`Home:34`, `Card:32`).

**Shadow DOM, deliberately.** Line 51 attaches `{ mode: 'open' }`. The header comment (lines 2–3) gives the reason: *"internals are invisible to the page's renderer, so streaming re-renders can't duplicate the input."* Blazor has the same hazard class (re-render on interactive server circuits), so the isolation intent carries over even though the mechanism will differ.

### 2.1 Trigger and shape

- **Host box:** `:host{display:block;position:relative;width:280px;height:30px}` (line 25).
- **Input** (`.cs-in`, lines 26, 54–59): full-width, 30px tall, `1px solid var(--line)`, `border-radius: 6px`, `background: var(--bg, #FAFAF7)`, `padding: 0 30px 0 10px` (right padding clears the kbd hint), `Inter` 15px, `color: var(--ink)`.
- **Placeholder text:** `"Search cards, sets, characters"` (line 57). `aria-label="Search"` (58), `autocomplete="off"` (59).
- **Keyboard hint chip** (`.cs-kbd`, lines 27, 60–62): a `<span>` reading `/`, absolutely positioned `right: 8px; top: 6px`, `JetBrains Mono` 12.5px, `color: var(--mut2)`, bordered, `background: var(--card)`. Decorative — no click handler.

### 2.2 Keyboard shortcuts

Both handlers are on `document`, attached in `connectedCallback` (lines 80–81) and removed in `disconnectedCallback` (84–85).

- **`/` focuses the input** (line 76). Guarded: it fires only when the active element is not the component itself *and* the active element's tag is not `input`, `select`, or `textarea` — so typing a slash inside any other field is unaffected. Calls `e.preventDefault()` so the `/` is not typed into the box.
- **`Escape` clears and blurs** (line 77), but only when `self._in.value` is non-empty **or** the component is the active element. So Escape inside an empty, unfocused search is a no-op and stays available to close whatever else is open on the page.

There is **no arrow-key navigation and no Enter handler.** Results are plain anchors; selection is mouse/Tab-and-Enter through native link behaviour. A Blazor rebuild should add roving-focus arrow keys and Enter-to-open — that is a gap in the prototype, not a design decision (§7).

### 2.3 Corpus, result types and grouping

The prototype searches a hardcoded demo corpus. Three types, in this fixed group order (`groupsFor`, lines 35–47):

| Group label | Source array | Cap | Result `href` | Sub-label shown |
|---|---|---|---|---|
| `Characters` | `SPECIES` (16 names, line 6) | 4 | `Cardstock Character.dc.html` | literal `"character"` |
| `Sets` | `SETS` (10 names, line 7) | 4 | `Cardstock Set.dc.html` | literal `"set"` |
| `Cards` | `CARDS` (15 objects `{n, s}`, lines 8–24) | 5 | `Cardstock Card.dc.html` | the card's set name (`c.s`) |

Groups with zero matches are omitted entirely (lines 43–45) — there is no empty group header.

### 2.4 Result ordering

**Corpus order, not relevance order.** Each list is `Array.filter` over the source array followed by `.slice(0, N)` (lines 36–41). There is no scoring, no prefix-before-substring preference, and no sort. The first N array entries that match win. Group order is fixed Characters → Sets → Cards regardless of match quality or count.

**Matching is a case-insensitive substring test:** `n.toLowerCase().indexOf(q) !== -1`, where `q = this._in.value.trim().toLowerCase()` (line 93). Not fuzzy, not token-aware, not diacritic-folded.

### 2.5 States

- **Below threshold / empty:** `_render` hides the menu outright when `q.length < 2` (line 96) — `menu.style.display = 'none'`. There is **no** recent-searches list, no suggested/default panel, no "start typing" hint. One character shows nothing.
- **No matches:** menu opens with a single `.cs-none` div reading `No matches for “<trimmed raw input>”` (lines 99–104), using curly quotes `“`/`”`. Styling: `padding: 8px; font-size: 12.5px; color: var(--mut2)`. Note the message echoes the **trimmed but not lowercased** input.
- **Results:** menu is `position: absolute; top: 36px; left: 0; right: -60px` — it is deliberately **60px wider than the input, bleeding rightward** (line 28). `z-index: 80`, `background: var(--card)`, `1px solid var(--line)`, `border-radius: 8px`, `box-shadow: 0 10px 28px rgba(20,19,26,0.14)`, `padding: 5px`, `max-height: 340px; overflow-y: auto`.
- **Group header** (`.cs-grp`, line 29): 10.5px, 600, `letter-spacing: 0.07em`, `text-transform: uppercase`, `color: var(--mut2)`, `padding: 6px 8px 2px 8px`.
- **Result row** (`.cs-item`, lines 30–33): an `<a>`, `display: flex; align-items: baseline; gap: 8px; border-radius: 5px; padding: 5px 8px; font-size: 13.5px`. Name span is `font-weight: 500`; sub span is 12px `var(--mut2)`. Hover: `background: var(--hov, #F6F6F2)`.

### 2.6 Navigation on select

Each result is a real `<a href>` (lines 112–113) with **no click handler and no `preventDefault`** — selecting a result is a full document navigation to the group's target page. All results within a group share one destination; the prototype passes no card/set/character identifier. In Blazor these become per-entity routes.

### 2.7 Dismissal

- **Outside mousedown:** `document.addEventListener('mousedown', …)` where the handler calls `self._clear()` when `!self.contains(e.target)` (lines 72, 80). Because the menu lives in shadow DOM, `contains` on the host still covers it. `mousedown`, not `click` — dismissal happens on press, before the mouse-up.
- **Escape:** clears and blurs (line 77).
- **`_clear()` sets `value = ''` then re-renders** (lines 87–91), so dismissal *wipes the query*; it does not merely hide the menu. Reopening always starts from empty.
- **No blur handler** — tabbing away from the input leaves the menu open until a document mousedown or Escape.
- **Listener lifecycle:** the two document listeners are added on every `connectedCallback` and removed on `disconnectedCallback`; the shadow root is built only once (`if (!this.shadowRoot)`, line 50), so reconnection reuses the existing DOM and does not stack handlers.

---

## 3. Image slot — `<image-slot>`

Source: `image-slot.js` (1225 lines). **Read the first two lines before treating any of it as CardStock design:** line 1 is `// @ds-adherence-ignore -- omelette starter scaffold (raw elements/hex/px by design)` and line 2 says the file is a copied starter that gets overwritten wholesale on re-copy. It is a **generic design-tool placeholder component**, not a CardStock-authored one. What CardStock owns is *how it is used* — the ids, the sizes, the wrappers, the hover behaviour. The Blazor equivalent should be a small `<CardArt>` component; almost none of this file's machinery (drag-drop ingest, canvas re-encode, reframe/crop, sidecar persistence, Unsplash attribution enforcement) should be ported.

### 3.1 The id convention — docs are half right

Docs claim `art-<cardid>`. **The code uses four different id schemes**, all `art-`-prefixed for cards:

| Scheme | Where | Example |
|---|---|---|
| `'art-' + c.id` (short demo id) | `Cardstock Home.dc.html:507`, `:558`; `Cardstock Screener.dc.html:747`, `:839` | `art-umbreon`, `art-zardex` |
| `'art-' + name.toLowerCase().replace(/[^a-z0-9]+/g, '-')` (slugified **name**) | `Cardstock Binder.dc.html:484`, `Cardstock Character.dc.html:208`, `Cardstock Set.dc.html:235` | `art-umbreon-vmax-alt-art-` |
| `'art-set-' + slug` (**sets**, not cards) | `Cardstock Browse.dc.html:227` | `art-set-evolving-skies` |
| Hardcoded literal | `Cardstock Card.dc.html:60`, `:104`; `Cardstock Charts.dc.html:73` | `art-umbreon` |

Non-card slots do not use the prefix at all: `profile-avatar` (`Cardstock Profile.dc.html:52`), and the marketing slots `hero-card-left/mid/right`, `features-card`, `features-card-2`, `features-card-3`, `data-card`, `data-card-2` (`Cardstock Landing.dc.html:57–222`).

**The id is a persistence key, not a semantic identifier** — `image-slot.js:24–25`: *"Persistence key. REQUIRED for the drop to survive reload — every slot on the page needs a distinct id."* Slots sharing an id share one image. `Cardstock Card.dc.html` uses `art-umbreon` **twice** (line 60 inline, line 104 in the enlarge lightbox) precisely so both show the same art.

**For Blazor:** the id convention should be discarded in favour of the real key. Card images live at `{ImageDirectory}/{hash}/1600.jpg` joined via `cards.image_hash` (DECISIONS.md D-010, receipts `../PokemonInvestBatch/DATA_MODEL.md:292–295`, `:160`). The DOM id becomes irrelevant; the `image_hash` is the identity.

### 3.2 Attributes used by CardStock

`observedAttributes` (line 440) is `['shape','radius','mask','fit','placeholder','src','id','credit','credit-href']`. CardStock pages use only **four**: `id`, `shape` (always `rounded`, except `circle` on the profile avatar), `radius`, and `placeholder`. No page sets `src`, `fit`, `mask`, `credit`, or `credit-href` — every slot in every prototype is empty by design (`HANDOFF.md:114`, quoted in D-010: *"Every card, set, and species image is a placeholder slot"*).

### 3.3 Sizes and radii used across pages

Sizing rule (`image-slot.js:63–72`): the slot fills its container (`width/height: 100%`); with an indefinite parent height it falls back to full width at 3:2. Every CardStock usage therefore wraps it in an explicitly sized div.

| Context | Wrapper size | `radius` | File:line |
|---|---|---|---|
| Home / Screener table row thumbnail | `48 × 66` | 4 | `Home:105–106`, `Screener:361–362` |
| Charts header card art (hover-zooms) | `96 × 133` | 6 | `Charts:72–73` |
| Card page inline art | `217 × 300` | 6 | `Card:59–60` |
| Card page enlarge lightbox | `min(62vh, 78vw)`, `aspect-ratio: 325/450` | 10 | `Card:103–104` |
| Home peek drawer art | `178 × 246` | 4 | `Home:241–242` |
| Home / Screener floating hover preview | `164 × 226` (`position: fixed`, `pointer-events: none`, `z-index: 100`) | 8 | `Home:315–316`, `Screener:411–412` |
| Grid tiles — Binder, Character, Set, Screener basket | `aspect-ratio: 325/450` | 5 | `Binder:94–95`, `Character:93–94`, `Set:126–127`, `Screener:394–395` |
| Browse set fan card | `78 × 108` | 5 | `Browse:117–118` |
| Profile avatar | `72 × 72`, `shape="circle"` (inline `style` on the element itself) | — | `Profile:52` |
| Marketing (Landing hero/feature/data cards) | 110–330px wide, rotated | 9–16 | `Landing:57–222` |

**The canonical card aspect ratio is `325 / 450`** (≈0.722), used both as a literal `aspect-ratio` and as the `217×300` / `96×133` / `178×246` / `164×226` / `48×66` fixed pairs. `Cardstock Card.dc.html:60` states it in the placeholder copy: `placeholder="card art 325×450"`.

### 3.4 Hover scale

Exactly **one** slot scales on hover, and it is the wrapper that scales, not the component. `Cardstock Charts.dc.html:72`:

```
base:  width: 96px; height: 133px; border-radius: 6px;
       box-shadow: 0 4px 14px rgba(20,19,26,0.18);
       transition: transform 0.15s ease, box-shadow 0.15s ease;
       transform-origin: left top;
hover: transform: scale(2.2); z-index: 40; position: relative;
       box-shadow: 0 12px 36px rgba(20,19,26,0.3);
```

`transform-origin: left top` is load-bearing — the 96px thumbnail grows down-and-right to ~211×293 without pushing into the nav above it. `grep -n "scale(" *.dc.html` finds no other hover-scale on any image; the only other `scale()` hits are the Landing page's card-shuffle keyframe transforms (`Landing:301–304`).

Table rows do **not** scale. They use the floating-preview pattern instead (§4.5).

### 3.5 Placeholder rendering, and how CardStock suppresses it

Shadow DOM built in the constructor (`image-slot.js:501–529`). Exported CSS parts: `frame`, `image`, `empty`, `attribution-error`, `loading`, `ring`, `credit`. The empty state (`:505–507`) is an icon + a `.cap` div carrying the `placeholder` attribute text (`:1109`, defaulting to `'Drop an image'`) + a `.sub` div reading `or browse files`. Visibility is toggled via `style.display` — `flex` when empty, `none` when filled (`:1151`, `:1170`) — with `data-filled` set/removed on the host (`:1152`, `:1171`).

**CardStock's own suppression rule.** Every app page that shows grid/row thumbnails ships this one line in its `<helmet>` `<style>`:

```css
image-slot[placeholder=" "]::part(empty) { opacity: 0; }
```

Seven pages carry it, verified by grep: `Binder:18`, `Browse:22`, `Card:22`, `Character:22`, `Home:22`, `Set:22`, `Screener:22`. **Charts does not** — its one slot (`Charts:73`) uses `placeholder=" "` but has no suppression rule, so the empty icon still renders inside the hover-zoom thumbnail. Likely an oversight; either way, `placeholder=" "` — a single space — is the convention for **"render nothing, show only the coloured gradient behind me."** Those slots sit on a `thumbBg` linear-gradient supplied by the page (`Home:558`: `linear-gradient(160deg, ${ac[0]}, ${ac[1]})`), which is what the user actually sees today. Slots with real copy (`"card art 325×450"`, `"card art"`, `"drop card image"`) show the caption.

`Cardstock Landing.dc.html` and `Cardstock Profile.dc.html` do **not** ship the suppression rule — their slots are meant to read as fillable.

### 3.6 How a real image replaces a placeholder

Two paths exist in the component; only one is relevant to Blazor.

**Path A — the `src` attribute (the one that matters).** `_render` (`:1100`) computes `const url = this._userUrl || srcAttr`. With no user drop, setting `src` alone is sufficient: the `<img part="image">` gets the URL, `display: block`, the empty state is hidden, `data-filled` is set (`:1141–1152`). Replacing an already-shown image sets `data-swapping` first so the stale frame is never revealed while the new one loads (`:1134`). Clearing `src` restores the placeholder (`:1155–1171`).

*This is the whole real-image contract for CardStock:* the slot is already a `src`-driven `<img>` with a placeholder fallback. Real card art exists — ~3.6 GB at `{ImageDirectory}/{hash}/1600.jpg`, keyed by `cards.image_hash`, refreshed hourly at 50/sweep (DECISIONS.md D-010; `../PokemonInvestBatch/DATA_MODEL.md:292–295`, `:160`, `:325`; `:464` anticipates this app serving them). So the Blazor `<CardArt>` reduces to: given `image_hash`, emit an `<img src>` at the right aspect ratio, and fall back to the gradient tile when the hash is null or the file is missing. **The open item is licensing, not availability** (D-010, D-011) — storing is a different act from serving.

**Path B — user drop/browse (discard).** Drag-drop or click-to-browse accepts `image/png|jpeg|webp|avif` only (`ACCEPT`, `:156`; SVG excluded as script-bearing, GIF excluded because the canvas re-encode keeps one frame). The file is re-encoded through a canvas to WebP q=0.85, longest side capped at `min(1200, 2× slot width)` (`MAX_DIM :151`, `toDataUrl :259–273`), and persisted as a data URL in a `.image-slots.state.json` sidecar next to the HTML (`STATE_FILE :94`) via `window.omelette.writeFile`. Outside that runtime the slot is read-only (`:15`). A user drop overrides `src`; clearing it reveals `src` again (`:41–44`). There is also a `Replace` / `Edit` control pair gated on `data-editable` (`:527–528`, `:578`) and a pan/scale reframe mode persisted as `{u, s, x, y}` (`:237–241`, scale clamped to 1–5 by `S_MAX`/`clampS` at `:232–233`).

**None of Path B ports.** Note the sidecar in this repo is real and large: `CardStock Mockup/.image-slots.state.json` is 299 KB of stored drop state.

**Also discard: the Unsplash attribution machinery** (`:96–148`, `:1120–1218`) — an Unsplash-host `src` with no `credit` renders an error tile *instead of* the photo, and credit links get `utm_source=claude_design&utm_medium=referral` appended. That exists because the design tool sources stock photos. CardStock serves its own scraped images and needs none of it.

---

## 4. Cross-cutting UI patterns

Seven patterns are re-implemented independently on multiple pages. Each should become one Blazor component.

### 4.1 Resizable columns via header pipes

**Where:** Home (`Home:95–99`), Screener (`Screener:354–356`), Binder (`Binder:112`, `:147`), Card sales ledger (`Card:194`), Character (`Character:111`), Set (`Set:106`).

**Structure.** The table is a CSS grid whose `grid-template-columns` is a bound string (`{{ gridCols }}`, `Home:93`) built from a `colW` state object. Each header cell is `display: flex; align-items: center; min-width: 0` containing a centred label `<span style="flex: 1; text-align: center; overflow: hidden; white-space: nowrap;">` followed by the drag handle:

```html
<span onMouseDown="{{ rsCard }}" title="Drag to resize"
      style="cursor: col-resize; color: var(--line3, #C9C9C4);
             padding: 2px 3px; margin-right: -6px; flex-shrink: 0;"
      style-hover="color: var(--acc, #4A63D0);">│</span>
```

The handle is the literal box-drawing character **`│` (U+2502)**, not a border or pseudo-element. `margin-right: -6px` cancels the grid `gap: 6px` so the pipe sits on the column seam. It tints to `var(--acc)` on hover.

**Drag mechanics** (`Home:332–345`, near-identical at `Screener:428–443`):
1. `mousedown` → `preventDefault()` + `stopPropagation()` (so it never triggers the header's sort click), capture `startX = e.clientX` and `startW = state.colW[key]`.
2. Attach `mousemove`/`mouseup` on `document`, set `document.body.style.cursor = 'col-resize'` and `userSelect = 'none'` for the duration.
3. New width = `startW + (ev.clientX - startX)`, clamped. **Home clamps to `[36, 420]`; Screener clamps to `[40, 420]`** — a 4px divergence with no apparent reason; pick one.
4. `mouseup` removes both listeners and restores `cursor`/`userSelect`.

Screener's version takes a second `bucket` argument (`startResize(key, bucket)`, default `'colW'`) because it resizes two independent grids. Widths are **in-memory only** — nothing persists across reload. Header sort is a separate `onClick` on the label span (`Binder:112`, `Character:111`, `Card:194`), so sort and resize share the cell without conflicting.

Default widths, Home (`Home:331`): `{ card: 220, tier: 52, price: 76, chg: 52, spark: 68 }`.

> **Amended 2026-08-18 (owner UAT, D-117/D-118).** The built `RosterTable` departs from the
> mockup mechanics in three ruled ways. **(a)** The first column is a flexible
> `minmax(min, 1.4fr)` track (the build's fill-the-page choice), which made its own grip
> inert — so that grip now **redistributes**: dragging left grows the fixed columns by the
> dragged amount split proportional to their drag-start widths (and vice versa), while the
> fr share visibly shrinks; other grips stay single-column. **(b)** The clamp divergence
> above is resolved: floor 52 (the build's), ceiling 420, all columns. **(c)** Virtualized
> rows materialize once per `Rows` instance — a fresh list every render resets Virtualize's
> anchoring (the snap-to-top D-118 records) — and `ItemSize` is a contract with the real
> 30px row height. The §4.7 sticky header also now ships: `.rt-head` pins at `top: 48px`,
> `z-index: 10`, which required the wrapper to clip (`overflow: clip`) rather than be a
> scroll container — on a viewport narrower than the columns the table clips, as the
> mockup's did.

### 4.2 Row-actions overflow menu

**Trigger** (`Home:122`): `<button aria-label="Row actions" title="More actions for this card" onClick="{{ row.toggleMenu }}">⋯</button>` — the literal `⋯` (U+22EF), `background: none; border: none; color: var(--mut2); font-size: 16px; padding: 2px`. The button sits in a `position: relative` wrapper.

**Panel** (`Home:124`): `<div role="menu" onMouseLeave="{{ row.closeMenu }}">`, `position: absolute; right: 0; top: 22px; z-index: 40; background: var(--card); border: 1px solid var(--line); border-radius: 6px; box-shadow: 0 6px 20px rgba(20,19,26,0.12); min-width: 190px; padding: 4px; text-align: left`.

**Items:** full-width `<button role="menuitem">`, `font-size: 14.5px`, `padding: 6px 8px`, `border-radius: 4px`, hover `background: var(--hov)`. Destructive items are `color: var(--neg2, #D64545)` with hover `background: var(--negBg08)` (`Home:132`, "Remove from watchlist"). Separator: `<div style="height: 1px; background: var(--line); margin: 4px 0;">` (`Home:130`).

**Dismissal — three independent rules, all present:**
1. **`onMouseLeave` on the panel** closes it immediately (`Home:124`).
2. **Document click** (`Home:538–543`): closes when `menuIdx !== null` **and** the click target has no `[role="menu"]` ancestor **and** the target's `aria-label` is not `"Row actions"` (the second guard prevents the toggle button's own click from closing-then-reopening).
3. **Escape** (`Home:523`): `if (e.key === 'Escape') this.setState(this.state.menuIdx !== null ? { menuIdx: null } : { peekId: null })` — **layered dismissal**: Escape closes the menu if one is open, otherwise closes the peek. Only one layer per press.

Only one menu is open at a time — state is a single `menuIdx`, not a per-row flag. Opening a peek also clears it (`Home:573`: `{ peekId: id, focusIdx: ix, menuIdx: null }`).

The same panel shape recurs for non-row menus: Charts saved views (`Charts:50`, `top: 34px; z-index: 60; min-width: 230px`), Screener screen-actions rail menu (`Screener:63`, `data-rail-menu="1"`), Screener/Browse "add" pickers (`Screener:101`, `Browse:70`) — all `onMouseLeave`-dismissed.

### 4.3 Tooltips — convention: explain the CONSEQUENCE, not the identity

The rule is an owner ruling recorded at `DESIGN_NOTES.md:152`: *"too many tooltips is better than not enough"* — **"Every interactive control on all 10 app pages now carries a `title` explaining its CONSEQUENCE, not its name (the label already says the name)."**

The prototypes honour it. `title` attribute counts, counted directly: Screener 36, Card 27, Charts 25, Binder 24, Home 23, Profile 16, Browse 11, Account 10, Set 7, Character 6, About Data 1, Legal 1.

Representative examples showing the convention in force:

| Control | Tooltip | Where |
|---|---|---|
| "Open full chart" menu item | "Opens Charts with this row's tracked signals pinned — any pin changes save back to this row via Update watchlist." | `Home:126` |
| "Add to binder" menu item | "Log a purchase of this card — opens the binder transaction form" | `Home:128` |
| "Remove from watchlist" | "Stop following this card — its tracked signals are forgotten" | `Home:132` |
| "Move to list…" | "Move this row to another watchlist — its tracked signals come with it" | `Home:131` |
| Peek close `✕` | "Close the preview — the watchlist stays as it is" | `Home:237` |
| Saved-views button | "Saved views — a view remembers the grade tiers, indicators, resolution, and date range you have set. Applying one changes which signals are tracked." | `Charts:48` |
| Chip legend | "Chip color = the signal's current state, not its identity. Colored means it hit; grey means nothing to report." | `Home:139` |
| Account circle | "Profile & settings" | `Home:53` |
| Resize handle | "Drag to resize" | `Home:95` |

**Mechanism:** native `title` attributes throughout — no custom tooltip component exists in any prototype, despite a `--tooltipBg` token being defined (`Home:29`, used 4×). Purely explanatory (non-interactive) tooltip anchors carry `cursor: help` (`Binder:60`, `Card:237`) or `cursor: default` (`Home:100`, `:139`). Dynamic tooltips come through render-value `tip` fields (`Character:111` `title="{{ c.tip }}"`, `Home:118` `title="{{ c.tip }}"`), documented at `DESIGN_NOTES.md:154`.

**For Blazor:** one `<Hint>` wrapper that emits `title` plus the right cursor, and a lint rule that every interactive element has one. Do not build a custom floating tooltip unless the owner asks — the prototypes never did.

### 4.4 Badges

Three distinct badge families, all `JetBrains Mono`, 600 weight, `border-radius: 3px`, `padding: 1px 5px`–`1px 6px`:

- **Outlined status badge** — `font-size: 11px; letter-spacing: 0.08em; color: var(--mut2); border: 1px solid var(--line); border-radius: 3px; cursor: help`. Example: `PRIVATE` on Binder (`Binder:60`), tooltip "Binder data is strictly private — no social features, never shared".
- **Tinted warning/data-quality badge** — no border, `color: var(--warnInk, #8F6614); background: rgba(176,127,26,0.12)`. Example: `7 OBS` on Card (`Card:237`), tooltip "Census history begins Jan 2026 — 7 observations so far; deltas need two".
- **Bound filter/state badge** — `font-size: 10.5px`, colours bound per item (`{{ fi.badgeFg }}` / `{{ fi.badgeBg }}`, `Screener:108`), `white-space: nowrap`. `DEFAULT` in the Charts saved-views menu (`Charts:55`) is the same shape with `color: var(--mut2); background: var(--mutbg)`.

**Signal chips are a fourth, semantically distinct family** and should not be merged with badges: `display: inline-flex; gap: 3px; font-size: 11.5px; padding: 1px 6px; border-radius: 4px`, colour bound from a `CHIP` palette map (`Home:346–349`) keyed `gain` / `loss` / `warn` (→ `--pos`/`--posBg(0.10)`, `--neg`/`--negBg(0.10)`, `--warnInk`/`rgba(176,127,26,0.12)`), each carrying a `▲`/`▼`/`–` glyph so colour is never the sole carrier. The legend at `Home:139` states the rule: **chip colour is state, not identity.**

### 4.5 Peek / drawer, and the overlay family

**Peek drawer** — `Cardstock Home.dc.html:230–231`, the canonical instance:

```
<aside role="dialog" aria-label="Card peek"
  position: fixed; top: 96px; right: 20px; bottom: 16px;
  width: 480px; max-width: calc(100vw - 40px);
  background: var(--card); border: 1px solid var(--line);
  border-top: 3px solid {{ peek.accent }}; border-radius: 8px;
  box-shadow: 0 8px 28px rgba(20,19,26,0.10);
  display: flex; flex-direction: column; overflow: auto;
  animation: peekIn 0.16s ease-out;
```

`top: 96px` clears the 48px nav plus the 36px market ticker plus padding. It is **non-modal**: no scrim, the table stays interactive, and it is an `<aside>`. The 3px accent top border is the card's own accent colour, tying drawer to row. Animation `@keyframes peekIn { from { transform: translateX(18px); } to { transform: translateX(0); } }` (`Home:23`) — slide only, no fade — and it is neutralised by the global `@media (prefers-reduced-motion: reduce) { * { animation-duration: 0.01ms !important; } }` (`Home:25`).

*Note the source has a duplicate `z-index` on the same element (`z-index: 50` then `z-index: 10`); the later value wins. Pick one deliberately in Blazor.*

Header is `position: sticky; top: 0` inside the scroll container with a `✕` close button (`Home:236–237`). Body is `padding: 12px; display: flex; flex-direction: column; gap: 12px` (`Home:239`).

**Keyboard contract** (`Home:521–536`, hint bar rendered at `Home:143`: `↑↓ rows · Enter peek · / search`):
- `↑`/`↓` move `focusIdx`; **if a peek is already open it follows the focused row** (`Home:530`: `if (this.state.peekId) st.peekId = ids[i]`), so arrowing through the table live-updates the drawer.
- `Enter` opens the peek for the focused row (`Home:533–535`).
- `Escape` closes menu-then-peek (§4.2).
- `/` focuses nav search (owned by `cardstock-search.js`, §2.2).

**Related overlays that are NOT this pattern** (keep them separate components):
- *Card art lightbox* (`Card:101–106`) — modal: full-inset scrim `rgba(20,19,26,0.55)`, `z-index: 200`, `cursor: zoom-out` on the scrim, an inner `onClick="{{ stopClick }}"` guard so clicks on the art don't close, and a corner `✕` at `top: -14px; right: -14px`. Opened from the inline art wrapper's `cursor: zoom-in` (`Card:59`).
- *Destructive confirm modal* (`Profile:188`) — `role="dialog"`, `position: fixed; inset: 0; z-index: 100; background: rgba(15,15,12,0.45)`, centred, with a type-to-confirm field (`delText`, `Profile:206`).
- *Floating hover preview* (`Home:315–316`, `Screener:411–412`) — `position: fixed` at cursor-derived `left/top`, `164 × 226`, `z-index: 100`, **`pointer-events: none`**, driven by `onMouseEnter="{{ row.pvIn }}" / onMouseLeave="{{ pvOut }}"` on the 48×66 row thumbnail (`Home:105`, `Screener:361`). Position computed as `{ x: r.right + 10, y }` from the thumbnail's bounding rect (`Home:567`).

### 4.6 Confirm-flash on save

The universal success affordance: **the control itself becomes its own confirmation for ~2 s, then reverts.** No toast component exists anywhere in the prototypes.

| Surface | Idle → flashed label | Duration | Where |
|---|---|---|---|
| Screener "Save screen" | `Save` → `✓ Saved` | **1800 ms** | `Screener:857–860` |
| Binder CSV export | (button) → `csvDone` state | **1800 ms** | `Binder:534` |
| Profile settings save | shows `Saved ✓` in `var(--pos, #157A50)` | **2200 ms** | `Profile:71–72`, `:240` |
| Profile password change | `pwFlash` next to "Last changed Mar 2026" | **2600 ms** | `Profile:149`, `:247` |
| Charts watchlist | `Update watchlist` → `✓ Watchlist updated` → settles at `✓ On watchlist` | — | `Charts:722` |
| Charts panel glow (deep-link attention) | `panelGlow` | **2600 ms** | `Charts:342` |
| Screener backtest run | `btPhase: 'run'` → `'done'` | **1100 ms** (simulated work, not a flash) | `Screener:624` |

**Implementation shape, identical everywhere:** `clearTimeout(this._t); setState({flag: true}); this._t = setTimeout(() => setState({flag: false}), N)` — note every instance clears the prior timer first, so rapid re-saves restart the window rather than ending it early. Flashed styling is green-family: `saveFg: PAL.pos`, `saveBg: PAL.posBg(0.10)`, `saveBd: PAL.posBg(0.35)` (`Screener:858–859`).

**Durations are inconsistent (1800 / 2200 / 2600).** Standardise on one in Blazor and record the choice.

### 4.7 Sticky layering (z-index budget)

Observed stack, so a Blazor rebuild does not re-derive it: nav `z-index: 20` (`Home:39`, `position: sticky; top: 0`) → table header `z-index: 10` at `position: sticky; top: 48px` (`Home:93`, offset by the nav height) → row menu `40` (`Home:124`) → search menu `80` (`cardstock-search.js:28`) → Charts views menu `60` (`Charts:50`) → Screener rail menu `60` (`Screener:63`) → add pickers `50` (`Screener:101`, `Browse:70`) → hover preview `100` (`Home:316`) → Profile modal `100` (`Profile:188`) → Card lightbox `200` (`Card:102`). The peek's `50`/`10` duplicate is the one incoherent entry.

### 4.8 Loading vocabulary — one ring, two phases (D-114, owner UAT 2026-08-18)

Not in the prototypes (they had no loading states); ruled during Catalog UAT after a
three-variant real-pixel mock. A refresh shows **one 48px ring anchored at `inset: 20vh`,
centred**, in two consecutive phases that never move it:

1. **Runtime boot** — `index.html`'s `.loading-progress` (template mechanics kept): the arc
   **fills with real download progress** via `--blazor-load-percentage`, caption via
   `--blazor-load-percentage-text`. Track `--line`, arc `--acc`, stroke 4, round cap,
   caption JetBrains Mono 11.5 `--mut2` at `20vh + 58px`.
2. **Page data fetch** — the shared `LoadingRing` component (`Components/LoadingRing.razor`),
   geometry-identical, **indeterminate**: a 28% arc spinning at 0.9s/rev (static under
   `prefers-reduced-motion: reduce`), caption `Loading…`, `aria-busy`. Replaces the bare
   `loading-strip` on Browse (both modes), Set, Character, and Card.

A determinate 25→100 walk during the fetch was **rejected**: the response is atomic (no
partial data — the owner's own formulation), so any mid-fetch percentage would be a
fabricated number. The two definitions must stay in step — a change to either updates
`app.css` §loading-progress *and* `LoadingRing.razor.css` together.

---

## 5. Theming hooks

### 5.1 The pre-paint script

One line, byte-identical, in the `<helmet>` of **ten** of the eleven app pages (`Home:35`, `Card:33`, `Screener:32`, `Charts:31`, `Binder:35`, `Browse:33`, `Set:33`, `Character:33`, `About Data:28`, `Legal:24`):

```html
<script>if(localStorage.getItem('cardstock-cvd')==='1')document.documentElement.setAttribute('data-cvd','1');if(localStorage.getItem('cardstock-theme')==='dark')document.documentElement.setAttribute('data-theme','dark');</script>
```

It is placed **after** the `<style>` block and immediately after `<script src="./cardstock-search.js">`, and it is synchronous and inline — that is the point: it stamps the root element before first paint so there is no light-mode flash.

**The gap:** `Cardstock Profile.dc.html` — the page that *writes* both keys — has no pre-paint line at all (`grep -c "data-theme','dark'"` → 0), and neither does `Cardstock Account.dc.html`. Both read the keys from component code instead (`Profile:209–210`, `Account:117–118`), which runs after paint. A dark-theme user therefore gets a light flash on exactly the settings screen. In Blazor the script belongs in the shared layout head, so this class of omission cannot recur.

**In Blazor this must be a raw inline `<script>` in `App.razor`'s `<head>`, before any rendered content** — it cannot be a Blazor component, because components run after paint. This is a hard constraint on the render-mode decision, alongside the loopback-API constraint in `CLAUDE.md`.

### 5.2 The two switches

| localStorage key | Values written | Root attribute set | Writer |
|---|---|---|---|
| `cardstock-theme` | `'light'` \| `'dark'` | `data-theme="dark"` (only when `dark`) | `Profile:234–235` |
| `cardstock-cvd` | `'0'` \| `'1'` | `data-cvd="1"` (only when `'1'`) | `Profile:237` |

Light and non-CVD are the **absence** of an attribute, not a value — no `data-theme="light"` is ever written. Profile is the only writer; there is no nav-level theme quick-switch in any prototype.

**There is no `prefers-color-scheme` fallback anywhere.** An unset key means light. That is a deliberate-looking gap worth confirming (§7).

### 5.3 Token architecture

Tokens are declared as CSS custom properties on `:root` selectors in each page's `<helmet>` `<style>`, and consumed everywhere as `var(--token, #fallback)` — **every single usage carries a literal hex fallback**, so the light theme is effectively encoded in the fallbacks rather than in a `:root` block. That is a prototype convenience; Blazor should define the light palette explicitly on `:root` and drop the inline fallbacks.

Three override layers (`Home:27–32`, same set on every app page):

1. `:root[data-cvd="1"]` — remaps only the semantic up/down colours to an Okabe–Ito-derived blue/orange pair: `--pos: #0B69A8; --pos2: #0072B2; --neg: #CC5F00; --neg2: #D55E00;` plus `--posBg10`, `--negBg08`, `--negBg10`.
2. `:root[data-theme="dark"]` — the full chrome set: `--bg: #161614; --card: #1E1E1C; --ink: #E9E9E5; --mut: #B4B4AE; --mut2: #A8A8A2; --mut3: #9A9A94; --mutbg: #2A2A27; --hov: #282825; --line: #33332F; --line2: #3E3E39; --line3: #4A4A44; --line4: #262623; --acc: #8C9BF2; --accH: #AAB6F6; --btn: #4A63D0; --warn: #D6A54A; --warnInk: #D6A54A; --tooltipBg: rgba(30,30,28,0.95); --accBg: #252B44; --accMut: #3A4570;` plus `color-scheme: dark` and `--logoTeal: #3FBFAD`.
3. The **cross product**: `:root[data-theme="dark"]:not([data-cvd="1"])` and `:root[data-theme="dark"][data-cvd="1"]` each redefine `--pos/--pos2/--neg/--neg2/--neg3` (`Home:30–31`). Dark+CVD is `--pos: #58A9E6; --neg: #F5924E`. Four states must be styled, not two.

Per-page variance: `Cardstock Card.dc.html:25` declares a **reduced** CVD block (`--pos` and `--neg2` only) while Home declares seven properties. Same-named tokens, different coverage per page — consolidate to one shared stylesheet.

**Token inventory, by usage count across all `.dc.html`** (`grep -oh "var(--[a-zA-Z0-9]*"` | sort | uniq -c):

`--mut2` 201 · `--line` 199 · `--mut` 191 · `--ink` 160 · `--card` 143 · `--acc` 104 · `--bg` 54 · `--line4` 49 · `--hov` 43 · `--mutbg` 27 · `--logoTeal` 24 · `--line3` 19 · `--btn` 19 · `--accH` 19 · `--warnInk` 12 · `--pos` 11 · `--inbg` 11 · `--neg` 8 · `--neg2` 7 · `--btnH` 7 · `--pos2` 6 · `--warn` 5 · `--line2` 5 · `--tooltipBg` 4 · `--accBg` 4 · `--negBg` 3 · `--mut3` 3 · `--warnBg` 2 · `--posBg` 2 · `--posBg10`, `--negBg25`, `--negBg10`, `--negBg08`, `--negBg07`, `--negBg06`, `--neg3` 1 each.

Semantic roles: surfaces `--bg`/`--card`/`--mutbg`/`--hov`/`--inbg`; text `--ink`/`--mut`/`--mut2`/`--mut3`; borders `--line` → `--line4` (four weights); interactive `--acc`/`--accH`/`--accBg`/`--accMut`/`--btn`/`--btnH`; semantic `--pos`/`--pos2`/`--neg`/`--neg2`/`--neg3`/`--warn`/`--warnInk`/`--warnBg` with `--posBgNN`/`--negBgNN` alpha tints; brand `--logoTeal`.

**Colour is never the sole signal.** Every semantic colour in the prototypes rides alongside a glyph (`▲`/`▼`/`–`) or a dash pattern (`Charts:791` picks `dash: '4 3'` for the signal line when CVD is on). Preserve that in Blazor.

### 5.4 The JS-side palette mirror — do not port

Every page-level component defines `PAL = (() => { const d = localStorage.getItem('cardstock-theme') === 'dark', c = localStorage.getItem('cardstock-cvd') === '1'; …` (`Home:323`, `Card:264`, `Screener:419`, `Charts:331`, `Binder:332`, `Browse:161`, `Set:146`, `Character:134`). This exists because SVG chart geometry (`fill`, `stroke`, `polyline` colours) is computed in JS where `var()` is not available. It is a **duplicate hard-coded copy of the palette, read once at construction and never re-read** — a theme change requires a reload for charts to recolour. In Blazor, generate SVG colours from one shared palette source rather than duplicating tokens into C#.

### 5.5 Global element styles worth centralising

From every app page's `<helmet>` (`Home:17–25`):

```css
html, body { margin: 0; padding: 0; background: var(--bg, #FAFAF7); }
body { font-family: 'Inter', system-ui, sans-serif; color: var(--ink, #1C1C1E); }
a { color: var(--acc, #4A63D0); text-decoration: none; }
a:hover { color: var(--accH, #3A4FB8); text-decoration: underline; }
*:focus-visible { outline: 2px solid var(--acc, #4A63D0); outline-offset: 1px; border-radius: 2px; }
image-slot[placeholder=" "]::part(empty) { opacity: 0; }
@media (prefers-reduced-motion: reduce) { * { animation-duration: 0.01ms !important; } }
```

Typography: three Google fonts loaded on every page (`Home:14`) — **Inter** 400/500/600/700 (UI), **Inter Tight** 600/700 (page headings, e.g. `Card:65` `h1`), **JetBrains Mono** 400/500/600 (all numerics, badges, tickers). Favicon `./brand/favicon.svg` (`Home:11`).

---

## 6. Design Composer runtime (scaffolding — discard)

`support.js` (1911 lines) is a **generated, third-party template engine** — line 1: `// GENERATED from dc-runtime/src/*.ts — do not edit. Rebuild with 'cd dc-runtime && bun run build'.` It is loaded by every `.dc.html` from the real `<head>` (`Home:6`) and is what makes the prototypes run at all: it parses the `<x-dc>` wrapper and its `<script data-dc-script>` block (`support.js:24–37`), hoists `<helmet>` into the document head (`:377–378`, `:1362+`), rewrites the custom control-flow tags `sc-if` / `sc-for` / `sc-else` / `dc-import` (`:487`, `:555–558`) into React elements, and renders the whole thing through `window.React` / `window.ReactDOM` (`:9–21`), lazily loading Babel to compile the inline class components (`:1173–1191`). It is the source of every construct that looks like product syntax but is not — `{{ expr }}` bindings, `style-hover="…"`, `hint-placeholder-count`, `hint-placeholder-val`, `data-screen-label`, `<x-dc>`, `<helmet>`. **It carries no product data and none of it ports to Blazor:** `sc-for` becomes `@foreach`, `sc-if` becomes `@if`, `{{ x }}` becomes `@x`, `style-hover` becomes a CSS `:hover` rule, and the `hint-placeholder-*` attributes are authoring-time skeleton hints with no runtime meaning. Delete `support.js` (and the identical copy at `uploads/Brand package creation/support.js`) from the rebuild entirely; read it only when a prototype construct is ambiguous.

---

## 7. Open questions

1. **Search has no keyboard result navigation.** `cardstock-search.js` binds only `/` and `Escape` — no `ArrowUp`/`ArrowDown`/`Enter`, no `aria-activedescendant`, no `role="listbox"`/`role="option"`, no `aria-expanded`. Is the Blazor version expected to add a full combobox contract, or match the prototype? (Recommendation: add it; the prototype's own hint bar advertises `/ search` as a keyboard affordance.)
2. **Search result identity.** Every result in a group links to the same static page — the prototype passes no id. Confirm the real routes (`/card/{id}`, `/set/{id}`, `/character/{name}`?) and what the server-side query is (name substring over `cards`? `sets`? what is the character/species source, given the eight scraper tables?).
3. **Search minimum query length is 2 and result caps are 4/4/5.** Keep, or tune against the real corpus? There is no "see all results" row and no full search-results page in any prototype.
4. **Result ordering has no relevance model.** Corpus order + substring match will look arbitrary over a real corpus. Ranking is undesigned.
5. **Card page has no active nav tab** while its siblings Set and Character mark Browse. Should Card highlight Browse, or nothing?
6. **Active-tab href:** `#` on Home/Screener/Charts vs. self-href on Binder/Browse. Standardise which?
7. **Column widths do not persist.** Should Blazor persist `colW` per user (localStorage? server-side preference?), and does resize apply to the Screener's second grid the same way?
8. **Confirm-flash duration** is 1800/2200/2600 ms across surfaces. Pick one.
9. **Peek z-index** is declared twice on one element (`50` then `10`). Which was intended?
10. **No `prefers-color-scheme` default** — a first-time visitor always gets light. Intentional?
11. **Theme changes do not recolour charts without a reload** (§5.4). Acceptable, or must Blazor make chart colour reactive?
12. **`--tooltipBg` is defined and used 4×, but there is no custom tooltip component.** Is a styled tooltip planned, or is the token vestigial?
13. **Real card images: serving is a licensing question, not an availability one** (D-010, D-011). Until that is settled, the placeholder path must remain a first-class state, not a fallback afterthought — which the gradient-tile + `placeholder=" "` convention (§3.5) already gives us for free.
14. **Account circle initial** — derived from email/display name, and what happens with no name?

---

## 8. Contradictions found

| Claim | Source doc:line | What the code actually does |
|---|---|---|
| Nav includes a bell / alerts icon: "Persistent top nav… + global search box (`/` focuses), **bell (alerts)**, account menu" | `uploads/CARDSTOCK_UI_SPEC_v1.md:127` | No bell exists. `grep -rniI -e bell -e notification --include='*.html'` returns zero hits across all 17 `.dc.html` files. Nav is logo → 5 tabs → spacer → search → account circle (`Home:39–54`). Tier 3 doc, superseded — the removal is recorded at `DESIGN_NOTES.md:120`. |
| "`TopNav` — fixed landmark: tabs, search box, **bell (unread badge), account menu, theme quick-switch, DEMO tag slot**" | `uploads/CARDSTOCK_UI_SPEC_v1.md:256` | Four of six do not exist. No bell, no unread badge, **no account menu** (the circle is a plain `<a href="Cardstock Profile.dc.html">`, `Home:53`), **no theme quick-switch in the nav** (theme is written only from Profile, `Profile:234–237`), no DEMO tag in any app nav. |
| "Bell icon badge = unread fired events; Alert Center lists rule status and firing history" | `uploads/CARDSTOCK_UI_SPEC_v1.md:93` | Nothing of this exists. No `/alerts` link, no `unreadAlerts` prop, no alert-rule UI. |
| "Nav constant: Home / Screener / Charts / Binder + search, **alerts (bell)**, account." — and the tab list omits Browse | `uploads/PROJECT_LOG.md:183` | Five tabs, not four: Home, Screener, Charts, Binder, **Browse** (`Home:45–49`). No bell. |
| image-slot id convention is `art-<cardid>` | task brief / derived docs | Four schemes coexist: `'art-' + c.id` (`Home:507`, `:558`, `Screener:747`, `:839`), `'art-' + slugified **name**` (`Binder:484`, `Character:208`, `Set:235`), `'art-set-' + slug` for sets (`Browse:227`), and hardcoded literals (`Card:60`, `:104`, `Charts:73`). Non-card slots ignore the prefix entirely (`profile-avatar`, `hero-card-*`). |
| "nav bell removed from **all 7 pages** (Charts never had one)" | `DESIGN_NOTES.md:120` | The removal itself is confirmed, but the page count no longer matches the prototype set: **eleven** pages now carry the app nav (Home, Screener, Charts, Binder, Browse, Set, Character, Card, Profile, About Data, Legal). The note is a historical record of an 8-page-era edit; it is not a current inventory. |
| "Every interactive control on all 10 app pages now carries a `title`… **~110 controls**" | `DESIGN_NOTES.md:152` | The rule holds and is well applied, but the count is low: 175 `title="` attributes across the nine core app pages (Screener 36, Card 27, Charts 25, Binder 24, Home 23, Profile 16, Browse 11, Set 7, Character 6), 186 including Account (10) and About Data / Legal (1 each). Also "10 app pages" vs. eleven navs (About Data and Legal have 1 tooltip each and are effectively untooltipped). |
| "Every card, set, and species image is a placeholder slot. This is the largest open risk" | `HANDOFF.md:114` | **Not a contradiction — correctly scoped, and re-confirmed here.** It describes the *prototypes*, and it is accurate: every `<image-slot>` in every `.dc.html` is empty (no page sets `src`). Real images do exist in the database (D-010), and `HANDOFF.md`'s second sentence — that the open question is licensing — is right. Recorded because D-010 documents this exact line being misread twice. |
| `uploads/CARDSTOCK_UI_SPEC_v1.md:46` — "no HTTP API for the first-party UI" | Tier 3 | Out of scope for this document, but flagged because it touches the shared-chrome render-mode decision: `CLAUDE.md:41` records the owner ruling that this is a scoping note, not an architectural constraint (see D-013, D-014, S-002). The pre-paint theme script (§5.1) is an independent constraint on the same decision. |
