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

`Cardstock Home.dc.html:22`, `Cardstock Card.dc.html:22`, `Cardstock Binder.dc.html:18`, `Cardstock Character.dc.html:22` (and matching lines on Browse, Set, Screener, Charts). So `placeholder=" "` — a single space — is the convention for **"render nothing, show only the coloured gradient behind me."** Those slots sit on a `thumbBg` linear-gradient supplied by the page (`Home:558`: `linear-gradient(160deg, ${ac[0]}, ${ac[1]})`), which is what the user actually sees today. Slots with real copy (`"card art 325×450"`, `"card art"`, `"drop card image"`) show the caption.

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

*(pending)*

## 5. Theming hooks

*(pending)*

## 6. Design Composer runtime (scaffolding — discard)

*(pending)*

## 7. Open questions

*(pending)*

## 8. Contradictions found

*(pending)*
