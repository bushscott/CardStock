# Shared components and app chrome

**Authority.** Everything below is read directly out of `CardStock Mockup/*.dc.html`, `cardstock-search.js`, `image-slot.js`, and `support.js` (Tier 1 per `CLAUDE.md:19-21`). Markdown docs are cited only where they make a claim; where a doc and the code disagree, the code is recorded as the answer and the disagreement is listed in §8.

All paths below are relative to `/Users/scott/RiderProjects/CardStock/CardStock Mockup/` unless stated otherwise. Line numbers were read 2026-08-10.

**Scope note.** These are the pieces repeated across pages. Per-screen behaviour lives in the per-screen specs.

---

## 1. App chrome — the 48px nav

### 1.1 Which pages carry it

Eleven of the seventeen prototypes render the nav. Verified by grepping `height: 48px; background: var(--card` across all `*.dc.html`:

| Page | Nav | `<cardstock-search>` | Pre-paint script | Active tab |
|---|---|---|---|---|
| `Cardstock Home.dc.html` | :39 | :52 | :35 | Home (`href="#"`, :45) |
| `Cardstock Screener.dc.html` | :36 | :45 | :32 | Screener (`href="#"`) |
| `Cardstock Charts.dc.html` | :35 | :45 (styled) | :31 | Charts (`href="#"`, :40) |
| `Cardstock Binder.dc.html` | :40 | :52 | :35 | Binder (self-href, :48) |
| `Cardstock Browse.dc.html` | :38 | :50 | :33 | Browse (self-href, :47) |
| `Cardstock Card.dc.html` | :37 | :50 | :33 | none |
| `Cardstock Set.dc.html` | :38 | :50 | :33 | none |
| `Cardstock Character.dc.html` | :38 | :50 | :33 | none |
| `Cardstock About Data.dc.html` | :30 | yes | :28 | none |
| `Cardstock Legal.dc.html` | :26 | yes | :24 | none |
| `Cardstock Profile.dc.html` | :29 | :39 | **absent** | none |

No nav at all: `Cardstock Landing.dc.html`, the three per-pillar Landing pages, `Cardstock Account.dc.html` (logged-out), `Cardstock Brand System.dc.html`.

The nav markup is **byte-identical** between Home and Card apart from the active-tab styling — `diff` of `Cardstock Home.dc.html:37-54` against `Cardstock Card.dc.html:35-52` returns exactly two hunks: the `data-screen-label` wrapper and the Home link. Confirms it is one component, not eleven copies that drifted.

### 1.2 Container

`Cardstock Home.dc.html:39`:

```
height: 48px; background: var(--card, #FFFFFF); border-bottom: 1px solid var(--line, #E4E4E0);
display: flex; align-items: center; gap: 24px; padding: 0 20px;
position: sticky; top: 0; z-index: 20;
```

Sticky at `top: 0`, `z-index: 20`. Page content that sticks under it uses `top: 48px` (e.g. the Home watchlist header row, `Cardstock Home.dc.html:93`, which is `position: sticky; top: 48px; z-index: 10`) — so the nav's height is load-bearing on other components' offsets. Make it a token.

### 1.3 Logo link

`Cardstock Home.dc.html:41-42`. An `<a href="Cardstock Home.dc.html" aria-label="Cardstock home">` wrapping an inline 24×24 SVG plus the wordmark.

- SVG: `viewBox="0 0 32 32"`, `aria-hidden="true"`. Two rounded rects (the back one `transform="rotate(-12 13 16)"`), a teal polyline, and a teal end dot. Stroke/fill are set through `style="stroke: var(--ink, #1C1C1E)"` etc., **not** presentation attributes — deliberate, so the mark inverts in dark mode (`DESIGN_NOTES.md:105`: "SVG chrome attrs converted to style form").
- Teal is its own token, `--logoTeal`, defaulting `#0E8A7B` and overridden to `#3FBFAD` in dark (`Cardstock Home.dc.html:32`). It is the only token defined in a standalone dark rule.
- Wordmark: `Inter` 700, 18px, `letter-spacing: -0.03em`, `color: var(--ink)`.
- The anchor sets `color: inherit; text-decoration: none` to defeat the global `a { color: var(--acc) }` rule at `Cardstock Home.dc.html:19`.

### 1.4 The five section links

`Cardstock Home.dc.html:44-50`. A flex row, `gap: 2px`, `height: 100%`. Order and targets are identical on all eleven pages (verified per page):

| Label | Target |
|---|---|
| Home | `Cardstock Home.dc.html` |
| Screener | `Cardstock Screener.dc.html` |
| Charts | `Cardstock Charts.dc.html` |
| Binder | `Cardstock Binder.dc.html` |
| Browse | `Cardstock Browse.dc.html` |

Resting state (`Cardstock Home.dc.html:46`): `padding: 0 12px; font-weight: 500; font-size: 15px; color: var(--mut, #5B5B57); border-bottom: 2px solid transparent; margin-bottom: -1px`.

Active state (`Cardstock Home.dc.html:45`): `font-weight: 600; color: var(--ink, #1C1C1E); border-bottom: 2px solid var(--acc, #4A63D0)`. The `margin-bottom: -1px` pulls the 2px underline over the nav's own 1px bottom border on both states.

**Two active-state idioms exist.** Home, Screener, and Charts set the active tab's `href="#"`; Binder and Browse keep the real self-link and only change the styling. Card, Set, Character, About Data, Legal, and Profile mark **no** tab active at all — they are leaves that live under Browse/Charts but never highlight it. In Blazor, `NavLink` with `Match=NavLinkMatch.Prefix` would light Browse on the Card page, which is *not* what the prototypes do. Either replicate the prototype (exact-match only, leaves show nothing) or raise it as a deliberate change.

### 1.5 Spacer and search

`<div style="flex: 1;"></div>` (`Cardstock Home.dc.html:51`) pushes search + account right. Then `<cardstock-search></cardstock-search>` (`:52`), bare on ten pages.

Charts is the one exception: `Cardstock Charts.dc.html:45` passes `style="flex: 0 1 280px; min-width: 110px; width: auto;"` because Charts packs two extra nav controls (an add-to-watchlist button at `:46` and a Views ▾ menu at `:47-63`) and needs the search to shrink. `DESIGN_NOTES.md:124` records this deliberately. The Blazor component must therefore accept an external width override rather than hard-coding 280px.

### 1.6 Account circle

`Cardstock Home.dc.html:53`:

```
<a href="Cardstock Profile.dc.html" aria-label="Account" title="Profile & settings"
   style="width: 28px; height: 28px; border-radius: 50%; border: 1px solid var(--line);
          background: var(--mutbg, #F3F3EE); color: var(--mut, #5B5B57);
          font-weight: 600; font-size: 14px; flex-shrink: 0;">O</a>
```

A 28px circle containing a single initial, `O` (the demo user is `otto@gmail.com`, `Cardstock Profile.dc.html:46`). It is a **plain link, not a dropdown menu** — one click goes straight to Profile. There is no account menu anywhere in the prototypes.

On Profile itself (`Cardstock Profile.dc.html:40`) it degrades to a non-interactive `<span>` with `title="You are here"` and accent-coloured border/text (`border: 1px solid var(--acc); color: var(--acc)`). That is the you-are-here treatment; the tab strip is *not* used for it.

### 1.7 The notification bell — stripped, and no remnant survives

`DESIGN_NOTES.md:120` says: *"UI remnants stripped 2026-08-08: nav bell removed from all 7 pages (Charts never had one), Screener 'Email me' button removed, Home unreadAlerts prop/tweak removed."* `HANDOFF.md:97` repeats it. The bell was originally spec'd at `uploads/CARDSTOCK_UI_SPEC_v1.md:127` and `:256` (with an unread badge).

**Verified clean.** `grep -rn "bell\|Bell\|🔔\|notification\|Notification" *.dc.html` returns **zero matches** across all seventeen prototypes. No stub element, no commented-out markup, no orphaned `unreadAlerts` prop. The nav ends at search + account circle. Do not build a bell, and do not reserve space for one.

Caveat on the count: the doc says "all 7 pages," but eleven pages carry the nav today. The removal note is from 2026-08-08 and pages were added afterwards; either way the *current* state is what matters and it is bell-free everywhere.

### 1.8 Footer (a second piece of shared chrome, not requested but repeated)

`Cardstock Home.dc.html:306-313` — `border-top: 1px solid var(--line); padding: 10px 20px`, flex space-between, three links (`About our data`, `Legal#privacy`, `Legal#terms`) and a right-hand mono corpus count. It recurs on the long-scroll pages. Worth a shared component; not spec'd further here.

---

## 2. Nav search — `cardstock-search.js`

129 lines. One IIFE registering the custom element `cardstock-search`, guarded by `if (customElements.get('cardstock-search')) return;` (`:5`).

### 2.1 Structure

**Shadow DOM, `mode: 'open'`** (`:51`). The file's own header comment (`:3`) explains why: *"internals are invisible to the page's renderer, so streaming re-renders can't duplicate the input."* That is a Design-Composer concern that evaporates in Blazor — see §8, because `DESIGN_NOTES.md:123` claims light DOM.

The shadow root holds four children, built imperatively in `connectedCallback` (`:49-82`): a `<style>` (`:52-53`), the `<input>` (`:54-59`), the `/` keycap `<span>` (`:60-62`), and the results `<div class="cs-menu">`, initially `display: none` (`:63-65`).

Host box: `:host{display:block;position:relative;width:280px;height:30px}` (`:25`).

Input (`:26`): full-width, `height: 30px`, `border: 1px solid var(--line, #E4E4E0)`, `border-radius: 6px`, `background: var(--bg, #FAFAF7)`, `padding: 0 30px 0 10px` (the right padding clears the keycap), `Inter` 15px, `color: var(--ink)`. `placeholder="Search cards, sets, characters"` (`:57`), `aria-label="Search"` (`:58`), `autocomplete="off"` (`:59`).

Keycap (`:27`): absolutely positioned `right: 8px; top: 6px`, `JetBrains Mono` 12.5px, `color: var(--mut2)`, 1px `--line` border, `border-radius: 4px`, `background: var(--card)`. Its text content is the literal `/` (`:62`).

Menu (`:28`): `position: absolute; top: 36px; left: 0; right: -60px` — it is deliberately **60px wider than the input, overhanging to the right**. `z-index: 80`, `background: var(--card)`, 1px `--line` border, `border-radius: 8px`, `box-shadow: 0 10px 28px rgba(20,19,26,0.14)`, `padding: 5px`, `max-height: 340px`, `overflow-y: auto`.

Theme tokens are read as `var(--x, hex)` throughout the shadow CSS. CSS custom properties inherit through the shadow boundary, so dark/CVD theming works even though the element is shadowed; fonts are re-declared explicitly rather than inherited.

### 2.2 Corpus (frozen in the component)

Three hard-coded arrays, `:6-24`:

- `SPECIES` — 16 entries: Charizard, Umbreon, Lugia, Rayquaza, Mewtwo, Espeon, Giratina, Blastoise, Gengar, Sylveon, Snorlax, Alakazam, Machamp, Dragonite, Leafeon, Glaceon.
- `SETS` — 10 entries: Base Set, Neo Genesis, Hidden Fates, Sword & Shield, Evolving Skies, Fusion Strike, Brilliant Stars, Lost Origin, Silver Tempest, Vivid Voltage.
- `CARDS` — 15 objects `{ n: name, s: set }`.

In Blazor this becomes a server query. `DESIGN_NOTES.md:123` says the corpus was lifted from Browse's arrays and frozen inside the component — a prototype convenience, not a design decision.

### 2.3 Trigger and matching

`_render()` (`:92-126`) runs on every `input` event (`:71`).

1. `var q = this._in.value.trim().toLowerCase()` (`:93`).
2. Menu is emptied via `menu.textContent = ''` (`:95`).
3. **`if (q.length < 2) { menu.style.display = 'none'; return; }`** (`:96`) — fires at **two or more characters after trimming**. One character shows nothing at all.
4. `groupsFor(q)` (`:35-47`) filters each array with `indexOf(q) !== -1` on the lowercased name — **case-insensitive substring match anywhere in the string, not prefix**. "zard" matches Charizard. There is no fuzzy matching, no scoring, no diacritic folding.

### 2.4 Result types, grouping, ordering, caps

| Group label | Source | Cap | Subtitle shown | Link target |
|---|---|---|---|---|
| `Characters` | `SPECIES` | `.slice(0, 4)` | literal `"character"` | `Cardstock Character.dc.html` |
| `Sets` | `SETS` | `.slice(0, 4)` | literal `"set"` | `Cardstock Set.dc.html` |
| `Cards` | `CARDS` | `.slice(0, 5)` | the card's **set name** (`c.s`) | `Cardstock Card.dc.html` |

Source: `groupsFor`, `:36-41`. Maximum 13 results.

**Group order is fixed: Characters → Sets → Cards** (`:43-45`), regardless of match quality. A group is omitted entirely when it has no hits (`if (chars.length) g.push(...)`).

**Within a group, order is source-array order** — the corpus arrays are traversed in declaration order and truncated with `slice`. There is no relevance ranking, no prefix-before-substring boost, no recency, no popularity. A Blazor implementation that adds ranking is a deliberate improvement, not a port; flag it.

Group heading (`:29`, `.cs-grp`): 10.5px, weight 600, `letter-spacing: 0.07em`, `text-transform: uppercase`, `color: var(--mut2)`, `padding: 6px 8px 2px 8px`.

Result row (`:30`, `.cs-item`): an `<a>`, `display: flex; align-items: baseline; gap: 8px`, `border-radius: 5px`, `padding: 5px 8px`, 13.5px, `color: var(--ink)`, `text-decoration: none`. Hover: `background: var(--hov, #F6F6F2)` (`:31`). Name span is weight 500 (`:32`); subtitle span is 12px `var(--mut2)` (`:33`).

All text goes in through `textContent` (`:109`, `:117`, `:120`), never `innerHTML` — keep that property when porting, given `DECISIONS.md` D-029 on raw stored strings.

### 2.5 States

- **Idle / under two characters** — menu `display: none`. Nothing renders; there is no "start typing" panel, no recent-searches list, no popular-searches list.
- **Results** — menu `display: block`, groups rendered in order.
- **No matches** — `:99-105`. A single `.cs-none` div, `padding: 8px`, 12.5px, `var(--mut2)` (`:34`), text `No matches for “<query>”` using **curly quotes** (`“` / `”`, `:102`) and the **trimmed original-case** input, not the lowercased one.
- **Loading** — does not exist. The corpus is synchronous and in-memory. A server-backed Blazor version needs a state the prototype has no design for; see §7.
- **Error** — does not exist either. Same caveat.

### 2.6 Keyboard

One document-level `keydown` listener, `_onKey` (`:73-78`), attached in `connectedCallback` (`:81`) and removed in `disconnectedCallback` (`:85`).

```js
var inSelf = document.activeElement === self;
var tag = (document.activeElement && document.activeElement.tagName) || '';
if (e.key === '/' && !inSelf && !/input|select|textarea/i.test(tag)) { e.preventDefault(); self._in.focus(); }
else if (e.key === 'Escape' && (self._in.value || inSelf)) { self._clear(); self._in.blur(); }
```

- **`/` focuses the box.** Suppressed when focus is already in the component, and suppressed when focus is in any other `input`, `select`, or `textarea` — so typing a slash into a filter field does not steal focus. `preventDefault()` stops the `/` character being typed into the search box itself.
- The `inSelf` check works because focus inside an open shadow root retargets `document.activeElement` to the **host element**; the host's tagName is `CARDSTOCK-SEARCH`, which the regex does not match, hence the separate `inSelf` guard.
- **`Escape` clears the query and blurs** — but only when there is a value or focus is inside. It calls `_clear()` (`:87-91`), which sets `value = ''` and re-renders, which hides the menu because `q.length < 2`.

**There is no arrow-key navigation and no Enter-to-select.** No `aria-activedescendant`, no `role="listbox"`/`role="option"`, no roving highlight. Results are mouse-only. This is a genuine accessibility gap in the prototype, not something to reproduce faithfully — see §7.

### 2.7 Dismissal

A document-level `mousedown` listener, `_onDoc` (`:72`), attached at `:80`, removed at `:85`:

```js
this._onDoc = function (e) { if (!self.contains(e.target)) self._clear(); };
```

- Fires on **`mousedown`, not `click`** — the menu closes on press, not release. That ordering matters: a `click` listener would fire after the anchor's own activation.
- `self.contains(e.target)` works because shadow-DOM events retarget to the host when observed at the document.
- It **clears the query**, it does not merely hide the menu. Clicking away and clicking back leaves an empty box. `DESIGN_NOTES.md:123` describes this as "outside mousedown closes," which understates it.
- Blur alone does **not** dismiss — there is no `blur` handler.

### 2.8 Navigation on select

Each result is a plain `<a href>` (`:112-113`) doing a full page navigation. Every character result goes to the same `Cardstock Character.dc.html`, every set to `Cardstock Set.dc.html`, every card to `Cardstock Card.dc.html` — **there is no id in the href**. Prototype shorthand; the Blazor version routes by id/slug (`HANDOFF.md:78` maps Character → `/character/{name}`).

Because navigation is a link, the component itself has no "on select" callback and no state to reset — the page unloads.

### 2.9 Blazor port notes

- Owns a `/`-key global shortcut and a document-level pointer-dismiss. Both need `@onkeydown` at a layout level or a small JS interop shim; Blazor has no document-level event without one.
- Shadow DOM is unnecessary in Blazor — its stated purpose (`:3`) was to survive Design-Composer streaming re-renders. Drop it and let the app's cascade apply.
- The 280px fixed width must be overridable (Charts).
- Debounce the corpus query. The prototype filters synchronously on every keystroke because the corpus is 41 items.

---

## 3. Image slot — `image-slot.js`

### 3.1 What this file actually is

**It is third-party scaffolding, not CardStock code.** Line 1: `// @ds-adherence-ignore -- omelette starter scaffold (raw elements/hex/px by design)`. Line 2: *"Copied omelette starter. Re-running copy_starter_component with this kind overwrites this file with the latest version."* It is the mockup tool's generic drop-an-image placeholder — 1,225 lines covering drag-and-drop ingestion, canvas re-encoding to WebP at `MAX_DIM = 1200` (`:151`), pan/scale reframing with a top-layer `popover` spill surface, a sidecar persistence store, and Unsplash attribution enforcement.

**Almost none of that survives the rebuild.** In Blazor this collapses to "render a card image, or a styled placeholder when there is none." The parts below are the parts that carry product meaning.

### 3.2 Where it appears

`<script src="./image-slot.js"></script>` is in the `<helmet>` of ten pages. Slot instances, counted by grep:

| Page | Slots | Ids |
|---|---|---|
| `Cardstock Landing.dc.html` | 8 | `hero-card-{right,mid,left}` (:57,:60,:63), `features-card`, `features-card-2`, `features-card-3` (:182,:185,:188), `data-card`, `data-card-2` (:219,:222) |
| `Cardstock Home.dc.html` | 3 | `{{ row.slotId }}` (:106), `{{ peek.slotId }}` (:242), `{{ pvSlot }}` (:316) |
| `Cardstock Screener.dc.html` | 3 | `{{ row.slotId }}` (:362), `{{ bc.slotId }}` (:395), `{{ pvSlot }}` (:412) |
| `Cardstock Card.dc.html` | 2 | `art-umbreon` twice (:60 inline, :104 lightbox) |
| `Cardstock Charts.dc.html` | 1 | `art-umbreon` (:73) |
| `Cardstock Binder.dc.html` | 1 | `{{ gc.slotId }}` (:95) |
| `Cardstock Browse.dc.html` | 1 | `{{ se.slotId }}` (:118) |
| `Cardstock Set.dc.html` | 1 | `{{ tl.slotId }}` (:127) |
| `Cardstock Character.dc.html` | 1 | `{{ tl.slotId }}` (:94) |
| `Cardstock Profile.dc.html` | 1 | `profile-avatar` (:52) |

### 3.3 The id convention — narrower than the docs claim

`DESIGN_NOTES.md:29` says the ids are `art-<cardid>`. That is true on two pages and false on six. Actual generators:

| Page:line | Expression | Result |
|---|---|---|
| `Cardstock Home.dc.html:507` | `slotId: 'art-' + c.id` | `art-umbreon` — matches the doc |
| `Cardstock Home.dc.html:558` | `slotId: 'art-' + id` | same |
| `Cardstock Screener.dc.html:747`, `:839` | `slotId: 'art-' + c.id` | same |
| `Cardstock Set.dc.html:235` | `'art-' + c.name.toLowerCase().replace(/[^a-z0-9]+/g, '-')` | **slug of the display name**, e.g. `art-umbreon-vmax-alt-art-` |
| `Cardstock Character.dc.html:208` | same expression | same |
| `Cardstock Binder.dc.html:484` | `'art-' + h.card.toLowerCase().replace(...)` | same, from the holding's card name |
| `Cardstock Browse.dc.html:227` | `'art-set-' + s.name.toLowerCase().replace(...)` | **`art-set-` prefix**, a set not a card |
| `Cardstock Card.dc.html:60`, `:104`; `Cardstock Charts.dc.html:73` | literal | `art-umbreon` hard-coded |
| `Cardstock Landing.dc.html`, `Cardstock Profile.dc.html:52` | literal | `hero-card-*`, `features-card*`, `data-card*`, `profile-avatar` — no `art-` prefix at all |

Two consequences. First, the slug forms have a **trailing hyphen** when the name ends in a bracket (`(Alt Art)` → `...-alt-art-`), because the regex replaces the closing paren. Second, the name-slug and the id-slug **do not agree**: Home's `art-umbreon` and Set's `art-umbreon-vmax-alt-art-` are different sidecar keys for the same card, so a photo dropped on one page does not appear on the other.

The id is the sidecar persistence key (`:24-25`, `:1064`, `:1096`), and every slot on a page must have a distinct one. In Blazor the id stops being a persistence key entirely — the image comes from the database. Keep an id only if something needs to address the element.

### 3.4 Attributes actually used in CardStock

Observed set (the component supports more, `:441`): `id`, `shape`, `radius`, `placeholder`, and inline `style` for fixed sizing. **`src`, `fit`, `mask`, `credit`, and `credit-href` are used nowhere in any CardStock page** — which is exactly why every slot is empty in the prototypes.

`shape` is `rounded` everywhere except `Cardstock Profile.dc.html:52`, which uses `shape="circle"` for the avatar. `_render` maps `circle` → `border-radius: 50%`, `pill` → `9999px`, `rounded` → `radius` px defaulting to 12 (`:1074-1081`).

Radius values in use: 4 (48×66 row thumbs, 164×226 hover preview is 8), 5 (gallery/shelf tiles), 6 (Card inline art, Charts panel art), 8 (hover preview, Screener preview), 9/10/11/16 (Landing hero fan), 10 (Card lightbox).

### 3.5 Sizes across pages

The slot has no intrinsic size — `:host{width:100%;height:100%;aspect-ratio:3/2}` (`:291-293`), so it fills a sized wrapper. Every CardStock usage sizes the wrapper, not the slot (except Profile, which sizes the element inline).

| Context | Box | Where |
|---|---|---|
| Watchlist / screener row thumb | 48 × 66 | `Cardstock Home.dc.html:105`, `Cardstock Screener.dc.html:361` |
| Hover preview (floating) | 164 × 226 | `Cardstock Home.dc.html:315`, `Cardstock Screener.dc.html:411` |
| Peek panel art | 178 × 246 | `Cardstock Home.dc.html:241` |
| Charts left-panel art | 96 × 133 | `Cardstock Charts.dc.html:72` |
| Card page inline art | 217 × 300 | `Cardstock Card.dc.html:59` |
| Card page lightbox | `min(62vh, 78vw)`, `aspect-ratio: 325 / 450` | `Cardstock Card.dc.html:103` |
| Profile avatar | 72 × 72, circle | `Cardstock Profile.dc.html:52` |

48:66, 178:246, 96:133, 217:300 and 164:226 all reduce to ≈ 0.7222 — i.e. **325:450**, the portrait card ratio named in the `placeholder` text at `Cardstock Card.dc.html:60` and in `uploads/CARDSTOCK_UI_SPEC_v1.md:214` ("local image store `{hash}/1600.jpg`, 325×450 portrait"). One aspect-ratio token covers every card-art box.

### 3.6 Placeholder rendering

`.empty` (`:324-331`) is an absolutely-positioned flex column, centred, `gap: 6px`, `cursor: pointer`, containing an inline 28×28 outline "picture" SVG (`:427-431` — rounded rect, small circle, mountain path, `stroke="currentColor"`, `stroke-width="1.6"`) and a caption whose text is `this.getAttribute('placeholder') || 'Drop an image'` (`:1109`).

A 1.5px **dashed ring** (`.ring`, `:334-335`) sits over the frame at `opacity: .35`, following the same `border-radius` as the frame, and is hidden once `data-filled` is set (`:337`). Colours are `currentColor`, so the placeholder chrome inherits the page's ink and reads in both themes (`:286-290`).

**CardStock suppresses the placeholder on most surfaces.** Nine pages carry this rule in their `<style>` block:

```css
image-slot[placeholder=" "]::part(empty) { opacity: 0; }
```

(`Cardstock Home.dc.html:22`, `Cardstock Card.dc.html:22`, `Cardstock Browse.dc.html:22`, `Cardstock Screener.dc.html:22`, `Cardstock Set.dc.html:22`, `Cardstock Character.dc.html:22`, `Cardstock Binder.dc.html:18`, and Charts.) A `placeholder=" "` (single space) therefore means **"render nothing"** — the wrapper's accent gradient shows through instead. Only the surfaces that want visible placeholder copy pass real text: `card art 325×450` (Card), `card art` (Home peek), `drop card image` (Landing), `Avatar` (Profile).

That gradient is the real empty state on list surfaces. `Cardstock Home.dc.html:105` sets `background: {{ row.thumbBg }}`, built at `:558` as `linear-gradient(160deg, ${ac[0]}, ${ac[1]})` from a per-card accent pair. So the Blazor loading/missing state for a card image is **the card's accent gradient**, not a grey box and not a spinner.

### 3.7 How a real image replaces a placeholder — and why it matters

Two paths exist in `_render` (`:1096-1172`):

```js
let stored = this.id ? getSlot(this.id) : this._local;
if (stored && stored.u && !/^data:image\//i.test(stored.u)) stored = null;   // :1097
const srcAttr = this.getAttribute('src') || '';                              // :1098
this._userUrl = (stored && stored.u) || null;
const url = this._userUrl || srcAttr;                                        // :1100
```

- **Sidecar path** — a user-dropped image, persisted in `.image-slots.state.json` next to the HTML, keyed by `id`. Only `data:image/…` URLs are accepted from it (`:1097`), because the sidecar is agent-writable. Irrelevant to Blazor: delete it.
- **`src` attribute path** — author-controlled, *"passes through unchanged"* (`:1094-1095`). **This is the path CardStock needs.** A user drop wins over `src` (`:1100`); clearing the drop reveals `src` again.

When a URL resolves, the slot sets `data-filled`, shows the `<img>`, hides `.empty` and the dashed ring, and runs `_clampView()` / `_applyView()` to position the image (`:1150-1154`). When it does not, the `<img>`'s `src` is removed and `.empty` is shown (`:1165-1171`). Replacing an already-displayed image sets `data-swapping`, which hides the stale frame with `visibility: hidden` and shows a 22px spinner ring until the new image fires `load`/`error` (`:1128-1141`, `:373-380`) — a real UX rule worth keeping: **never show the previous card's art while the next one decodes.**

The `.image-slots.state.json` in the mockup folder is 299 KB, so some slots do carry dropped demo images in the prototype's local state.

**Why the real-image path is load-bearing.** `DECISIONS.md` D-010 (root `DECISIONS.md:83-89`) records ~3.6 GB of real photos at `{ImageDirectory}/{hash}/1600.jpg`, joined via `cards.image_hash`, refreshed hourly at 50 per sweep, with receipts in `../PokemonInvestBatch/DATA_MODEL.md:292-295`, `:160`, `:325`, `:105`. `DATA_MODEL.md:464` anticipates this app serving them. So the Blazor component's primary state is **filled**, and the placeholder is the exception — the inverse of the prototype, where every slot is empty and `HANDOFF.md:114` (§6, "Not built, deliberately") correctly describes them as placeholder slots.

D-010 also flags the one open item: **licensing**. Storing is not serving; nobody has read the terms. Do not treat "images exist" as "images may be published."

### 3.8 Blazor component shape

```
<CardArt CardId="..." ImageHash="..." Width="48" Height="66" Radius="4"
         Placeholder="" AccentFrom="#5C63B8" AccentTo="#3E4489" />
```

Required behaviours, all evidenced above: 325:450 aspect box; accent-gradient ground when no image; optional placeholder caption + dashed ring when caption is non-blank; `border-radius` per instance; hold the previous frame hidden (not stale-visible) during a swap; `<img loading="lazy">` for the list surfaces. Everything else in `image-slot.js` — drag-drop, canvas encode, reframe, popover spill, sidecar, Unsplash attribution — is discarded.

---

## 4. Cross-cutting UI patterns

Each of these appears on three or more pages with copy-pasted markup. Each should be exactly one Blazor component.

### 4.1 Resizable columns via header pipes

Present on Home (5 handles), Screener (4), Binder (2), Card (1), Set (1), Character (1). Counted with `grep -c 'col-resize'`.

**Markup**, `Cardstock Home.dc.html:95`:

```html
<div style="display: flex; align-items: center; min-width: 0;">
  <span style="flex: 1; text-align: center; overflow: hidden; white-space: nowrap;">Card</span>
  <span onMouseDown="{{ rsCard }}" title="Drag to resize"
        style="cursor: col-resize; color: var(--line3, #C9C9C4); padding: 2px 3px;
               margin-right: -6px; flex-shrink: 0;"
        style-hover="color: var(--acc, #4A63D0);">│</span>
</div>
```

The handle is the literal box-drawing character `│` (U+2502), not a border or pseudo-element. Resting colour `--line3`, hover `--acc`. `margin-right: -6px` pulls it into the grid gap. Header labels are `text-align: center` (`DESIGN_NOTES.md:29` — "headers+data centered").

**Behaviour**, `Cardstock Home.dc.html:332-346`:

```js
startResize(key) {
  return (e) => {
    e.preventDefault(); e.stopPropagation();
    const startX = e.clientX, startW = this.state.colW[key];
    const mv = (ev) => {
      const w = Math.max(36, Math.min(420, startW + ev.clientX - startX));
      this.setState(s => ({ colW: Object.assign({}, s.colW, { [key]: w }) }));
    };
    const up = () => { /* detach mv+up; clear body cursor + userSelect */ };
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
    document.addEventListener('mousemove', mv);
    document.addEventListener('mouseup', up);
  };
}
```

- Clamp **36 px to 420 px**. Same clamp on Card:282, Character:156, Set:175, Screener:428, Binder:435.
- While dragging, `document.body` gets `cursor: col-resize` and `userSelect: none`; both are cleared on mouseup.
- `preventDefault` + `stopPropagation` on mousedown stop the row's own click/drag handlers firing.
- Widths live in a `colW` state bag (`Cardstock Home.dc.html:331`: `colW: { card: 220, tier: 52, price: 76, chg: 52, spark: 68 }`) and are injected as a `grid-template-columns` string (`{{ gridCols }}`) applied to **both** the header row and every data row (`Cardstock Home.dc.html:93` and `:104`) — that is what keeps them aligned.
- Screener and Binder pass a **second argument** naming the bucket (`startResize(key, 'hColW')`, `startResize(key, 'tColW')`, `startResize(key, 'btColW')`) because those pages have more than one independently-resizable table.
- Mouse events only. No touch, no keyboard resize, no persistence across reloads.

### 4.2 Row-actions overflow menu

**Trigger**, `Cardstock Home.dc.html:122`: a bare `⋯` button, `aria-label="Row actions"`, `title="More actions for this card"`, `font-size: 16px`, `color: var(--mut2)`, `padding: 2px`, inside a `position: relative` wrapper.

**Panel**, `Cardstock Home.dc.html:124`: `role="menu"`, `position: absolute; right: 0; top: 22px; z-index: 40`, `background: var(--card)`, 1px `--line`, `border-radius: 6px`, `box-shadow: 0 6px 20px rgba(20,19,26,0.12)`, `min-width: 190px`, `padding: 4px`, `text-align: left`.

**Items** (`:125-130`): full-width left-aligned buttons, `role="menuitem"`, `Inter` 14.5px, `padding: 6px 8px`, `border-radius: 4px`, hover `background: var(--hov)`. A 1px `--line` divider with `margin: 4px 0` separates groups (`:128`). The destructive item is `color: var(--neg2, #D64545)` with hover `background: var(--negBg08)` (`:130`). Every item carries a consequence `title`.

**Dismissal — three rules, and they are not the same on every page.**

1. **Mouse-leave the panel.** `onMouseLeave="{{ row.closeMenu }}"` on the panel itself (`Cardstock Home.dc.html:124`; also `Cardstock Charts.dc.html:50` for the Views menu, `Cardstock Screener.dc.html:63` rail menu, `:101` add-filter menu, `Cardstock Card.dc.html:158`/`:174` legend popovers).
2. **Outside click**, via a document listener registered in `componentDidMount`. `Cardstock Home.dc.html:539-544`:

```js
this.docClick = (e) => {
  if (this.state.menuIdx !== null
      && !e.target.closest('[role="menu"]')
      && (e.target.getAttribute && e.target.getAttribute('aria-label') !== 'Row actions')) {
    this.setState({ menuIdx: null });
  }
};
document.addEventListener('click', this.docClick);
```

  The second clause is what stops the trigger's own click from immediately re-closing the menu it just opened. Screener uses `[data-rail-menu]` / `aria-label !== 'Screen actions'` and `[data-filter-pop]` instead of `[role="menu"]` (`Cardstock Screener.dc.html:683-687`); Card uses `[data-watch-pop]` and `[data-lg-pop]` (`Cardstock Card.dc.html:276-277`). Same shape, three different hook attributes — unify on one in Blazor.
3. **Selecting an item closes it** — each item's handler ends in `closeMenu` (`Cardstock Home.dc.html:127`, `:129`, `:130`).

Every page removes the listener in `componentWillUnmount` (`Cardstock Home.dc.html:546`, `Cardstock Screener.dc.html:689`). **Escape does not close these menus** anywhere — a gap, see §7.

Only one menu is open at a time: state is a single `menuIdx` (`Cardstock Home.dc.html:331`) / `railMenu` (Screener), and opening a row's peek also nulls it (`Cardstock Home.dc.html:573`: `open: () => this.setState({ peekId: id, focusIdx: ix, menuIdx: null })`).

### 4.3 Tooltips — consequence, not identity

**The convention, verbatim** (`DESIGN_NOTES.md:151-155`, "Tooltip pass (2026-08-10)"):

> User ruling: **"too many tooltips is better than not enough."** Every interactive control on all 10 app pages now carries a `title` explaining its CONSEQUENCE, not its name (the label already says the name). ~110 controls, up from 84 element-level tooltips.

Counted `title="` occurrences per page: Screener 36, Card 27, Charts 25, Binder 24, Home 23, Profile 16, Browse 11, Account 10, Set 7, Character 6, Legal 1, About Data 1 — 187 total attribute occurrences (some are dynamic `title="{{ x.tip }}"` bindings that expand to many at runtime).

The rule is visible in the copy. Not "Remove" but *"Stop following this card — its tracked signals are forgotten"* (`Cardstock Home.dc.html:130`). Not "Add to binder" but *"Log a purchase of this card — opens the binder transaction form"* (`Cardstock Home.dc.html:127`, repeated verbatim at `Cardstock Card.dc.html:82`). Not "Close" but *"Close the preview — the watchlist stays as it is"* (`Cardstock Home.dc.html:237`). Not "Saved views" but *"…Applying one changes which signals are tracked."* (`Cardstock Charts.dc.html:48`).

**Two mechanisms, both in use:**

- **Native `title`** on the element. The default for controls. Where the tooltip is the *only* affordance the element has, it is paired with `cursor: help` (`Cardstock Card.dc.html:95` signal chips) or `cursor: default` (`Cardstock Home.dc.html:87`, `:100` section headings). Static text lives in the markup; dynamic text arrives as a `{{ x.tip }}` binding fed from `renderVals` (`DESIGN_NOTES.md:154` enumerates which).
- **Custom chart tooltip**, for crosshair readouts, styled with its own token: `background: var(--tooltipBg, rgba(255,255,255,0.95))`, 1px `--line`, `border-radius: 6px`, `padding: 5px 9px`, `pointer-events: none`, `box-shadow: 0 4px 12px rgba(20,19,26,0.08)` (`Cardstock Card.dc.html:135`, `Cardstock Binder.dc.html:191`, `Cardstock Charts.dc.html:251` — the last uses `padding: 6px 9px` and a lighter shadow). `--tooltipBg` flips to `rgba(30,30,28,0.95)` in dark.

Chip tooltips follow their own grammar (`DISPLAY_VOCABULARY.md:5`): *"icon + short name + evidence number, tooltip = one-sentence evidence with window and threshold"* — e.g. `Cardstock Home.dc.html:360`: `"Relative strength vs market index, 3M: 94th percentile"`.

`DESIGN_NOTES.md:155` records the single deliberate exception: Account's prototype-screen jumper row, "a demo affordance, not product UI."

**Blazor implication.** Native `title` gives no styling control, no touch story, and a ~1s browser delay. Replacing it with a styled tooltip component is a real decision (see §7) — but whatever renders it, the *content rule* is the durable part: consequence, not identity.

### 4.4 Badges

Three visually distinct families:

**Signal chips** — the dominant one. `Cardstock Home.dc.html:118`: `display: inline-flex; align-items: center; gap: 3px; font-family: 'JetBrains Mono'; font-size: 11.5px; font-weight: 500; padding: 1px 6px; border-radius: 4px; white-space: nowrap; background: {{ c.bg }}; color: {{ c.fg }}`. `Cardstock Card.dc.html:95` is the same plus `border: 1px solid {{ sg.bd }}` and `cursor: help`.

Colour comes from a four-key `CHIP` map (`Cardstock Home.dc.html:349-354`):

| Key | Foreground | Background |
|---|---|---|
| `gain` | `PAL.pos` | `PAL.posBg(0.10)` |
| `loss` | `PAL.neg` | `PAL.negBg(0.10)` |
| `warn` | `PAL.warnInk` | `rgba(176,127,26,0.12)` |
| `muted` | `PAL.mut2` | `PAL.mutbg` |

Glance rule (`DISPLAY_VOCABULARY.md:8`): **colored = hit** (green ▲ bullish, red ▼ bearish, amber – caution), **grey = nothing to report** (quiet `–`, or insufficient `◌` whose tooltip is an unlock countdown). Chips always render, including in their quiet state.

**Status badges** — small uppercase mono pills. `DEFAULT` on the active saved view (`Cardstock Charts.dc.html:55`): `JetBrains Mono` 11px weight 600, `color: var(--mut2)`, `background: var(--mutbg)`, `padding: 1px 5px`, `border-radius: 3px`. Also `LOCKED` (`Cardstock Charts.dc.html:149`), `LOW DATA` (`Cardstock Charts.dc.html:597`, `Cardstock Screener.dc.html:490-494`), `METADATA PENDING` (`Cardstock About Data.dc.html:115`). `DISPLAY_VOCABULARY.md:55` defines the complete honesty-state set: **OK · LOW DATA · LOCKED · UNDEFINED window · UNSTABLE FIT**.

**Count badges** — a mono number appended to a tab label rather than a pill: `Cardstock Home.dc.html:89`, `JetBrains Mono` 12.5px `var(--mut2)`.

One `Badge` component with a variant enum covers all three; the honesty states are the enum that matters.

### 4.5 The peek / drawer family

Four distinct overlay behaviours. They are frequently conflated; they are not the same thing.

**(a) Hover preview** — no click, follows the pointer, `pointer-events: none`. `Cardstock Home.dc.html:314-318`:

```html
<sc-if value="{{ hasPv }}">
  <div style="position: fixed; left: {{ pvX }}px; top: {{ pvY }}px; width: 164px; height: 226px;
              border-radius: 8px; background: {{ pvBg }};
              box-shadow: 0 14px 40px rgba(20,19,26,0.35); z-index: 100; pointer-events: none;">
    <image-slot id="{{ pvSlot }}" shape="rounded" radius="8" placeholder=" "></image-slot>
  </div>
</sc-if>
```

Triggered by `onMouseEnter` on the 48×66 thumb wrapper only, not the whole row (`Cardstock Home.dc.html:105`), cleared by `onMouseLeave` → `pvOut: () => this.setState({ pv: null })` (`:621`). Positioning, `Cardstock Home.dc.html:559-568`:

```js
const r = e.currentTarget.getBoundingClientRect();
const row = e.currentTarget.closest('[role="button"]');
const tbl = row.parentElement.getBoundingClientRect();
const hdr = row.parentElement.firstElementChild.getBoundingClientRect();
const minY = Math.max(8, hdr.bottom + 4);
const maxY = Math.min(window.innerHeight - 234, tbl.bottom - 230);
const y = Math.max(minY, Math.min(maxY, r.top + r.height / 2 - 113));
this.setState({ pv: { slot: 'art-' + id, bg: ..., x: r.right + 10, y } });
```

x is `thumb.right + 10`; y is vertically centred on the thumb then clamped so the preview never covers the sticky header and never leaves the viewport or the table. Identical code at `Cardstock Screener.dc.html:748`. **This, not a CSS transform, is the "hover scale" the docs describe** — 164/48 ≈ 3.42×, but it is a separate fixed-position element. See §8.

The one genuine CSS hover-scale in the whole prototype is `Cardstock Charts.dc.html:72`: `transition: transform 0.15s ease, box-shadow 0.15s ease; transform-origin: left top;` with `style-hover="transform: scale(2.2); z-index: 40; position: relative; box-shadow: 0 12px 36px rgba(20,19,26,0.3);"` — 96×133 → **2.2×**, anchored top-left.

**(b) Peek panel** — a right-side drawer. `Cardstock Home.dc.html:231`: `<aside role="dialog" aria-label="Card peek">`, `position: fixed; top: 96px; right: 20px; bottom: 16px; width: 480px; max-width: calc(100vw - 40px)`, `background: var(--card)`, 1px `--line`, **`border-top: 3px solid {{ peek.accent }}`** (per-card accent stripe), `border-radius: 8px`, `box-shadow: 0 8px 28px rgba(20,19,26,0.10)`, `overflow: auto`, `animation: peekIn 0.16s ease-out`.

`peekIn` is declared in the page `<style>` (`Cardstock Home.dc.html:23`): `from { transform: translateX(18px); } to { transform: translateX(0); }` — an 18px slide, no fade. `top: 96px` = 48px nav + 36px ticker + 12px. Its header is `position: sticky; top: 0` inside the scroll container with a close `✕` (`:232-238`). **No scrim, no focus trap, no Escape handler** despite `role="dialog"`.

**(c) Modal overlays** — full scrim, centred or click-to-close. Three instances: `Cardstock Card.dc.html:102` art lightbox (`position: fixed; inset: 0; background: rgba(20,19,26,0.55); z-index: 200; cursor: zoom-out`, with `onClick="{{ stopClick }}"` on the inner panel to stop propagation, `:103`); `Cardstock Binder.dc.html:237` transaction form (`rgba(20,19,26,0.45)`); `Cardstock Profile.dc.html:188` delete-account confirm (`rgba(15,15,12,0.45)`, `z-index: 100`, `role="dialog"`). Scrim opacity varies 0.45–0.55 — normalise.

**(d) Static side rails** — not overlays. `Cardstock Charts.dc.html:70` Indicators (272px, `border-right`, collapsible via `sc-if leftOpen`) and `Cardstock Screener.dc.html:53` Saved screens (232px). Layout siblings, in flow.

### 4.6 Confirm-flash on save

The universal "it worked" pattern: set a boolean, render an inline confirmation, clear it on a timer. **No toasts anywhere in the prototypes.**

| Where | Code | Duration |
|---|---|---|
| Screener save screen | `Cardstock Screener.dc.html:860` | 1800 ms |
| Binder export CSV | `Cardstock Binder.dc.html:534` | 1800 ms |
| Profile save | `Cardstock Profile.dc.html:240` | 2200 ms |
| Charts add-to-watchlist | `Cardstock Charts.dc.html:728` | 2200 ms |
| Profile password update | `Cardstock Profile.dc.html:247` | 2600 ms |
| Charts panel glow (deep-link `#signals`) | `Cardstock Charts.dc.html:342` | 2600 ms |

Canonical form, `Cardstock Profile.dc.html:240`:

```js
doSave: () => { this.setState({ savedFlash: true }); clearTimeout(this._t1);
                this._t1 = setTimeout(() => this.setState({ savedFlash: false }), 2200); },
```

Note the `clearTimeout` before re-arming — rapid double-saves must not let an early timer cancel a later flash. The rendered confirmation is minimal: `Cardstock Profile.dc.html:72` is `<span style="font-size: 13.5px; color: var(--pos, #157A50);">Saved ✓</span>` beside the button. `Cardstock Account.dc.html:36` uses the same idiom for a post-reset banner (`flashOn`), and notably suppresses the error message while the flash is up (`:141`: `showErr: … && !st.flash`).

Three durations for one pattern is drift, not design. Pick one (1800 for acknowledgements, 2600 for anything carrying explanatory text is a defensible split) and record the choice.

### 4.7 Also repeated, worth folding in

- **`style-hover`** — a Design-Composer pseudo-attribute used on ~every interactive element (`Cardstock Home.dc.html:95`, `:104`, `:125`…). In Blazor these are ordinary CSS `:hover` rules. It exists only because the prototypes are inline-styled.
- **Row hover** — `style-hover="background: var(--hov, #F6F6F2);"` on every table row.
- **Focus ring** — one global rule per page: `*:focus-visible { outline: 2px solid var(--acc, #4A63D0); outline-offset: 1px; border-radius: 2px; }` (`Cardstock Home.dc.html:21`).
- **Reduced motion** — `@media (prefers-reduced-motion: reduce) { * { animation-duration: 0.01ms !important; } }` (`Cardstock Home.dc.html:25`). Note it kills animation only, not `transition` — the Charts hover scale still animates.
- **Drag-to-reorder rows** — Home watchlist only (`Cardstock Home.dc.html:104`, the five `onDrag*` handlers plus `row.rowOpacity` and `inset 0 2px 0 {{ row.dropBd }}` as the drop indicator).
- **Sparkline SVG** — `Cardstock Home.dc.html:115`, `viewBox="0 0 64 18"`, `preserveAspectRatio="none"`, a filled polygon under a 1.25px polyline coloured by sign. Repeated on Screener and Set.

---

## 5. Theming hooks

### 5.1 The pre-paint script

One line, identical on ten pages, always the **last** element in `<helmet>` (`Cardstock Home.dc.html:35`, `Cardstock Card.dc.html:33`, and at `:35`/`:33`/`:32`/`:31`/`:28`/`:24` on Binder, Character, Screener, Charts, About Data, Legal; `:33` on Set and Browse):

```html
<script>if(localStorage.getItem('cardstock-cvd')==='1')document.documentElement.setAttribute('data-cvd','1');if(localStorage.getItem('cardstock-theme')==='dark')document.documentElement.setAttribute('data-theme','dark');</script>
```

Synchronous, before body paint, to avoid a light flash. Two independent keys (`DISPLAY_VOCABULARY.md:86`):

| Key | Trigger value | Attribute set on `<html>` |
|---|---|---|
| `cardstock-theme` | `'dark'` | `data-theme="dark"` |
| `cardstock-cvd` | `'1'` | `data-cvd="1"` |

Only the "on" value is checked — `'light'` and `'0'` are written by the toggles (`Cardstock Profile.dc.html:234`, `:237`) but simply fail the equality test, so light + non-CVD is the default with no attribute. Dark and CVD **compose**: there are separate rules for dark-standard and dark-CVD.

**Blazor.** Server prerendering cannot read `localStorage`, so this stays as a raw `<script>` in `App.razor`'s `<head>` — or the preference moves server-side into the user row and is emitted into the attribute during render. That is a real decision, not a port detail. See §7.

`Cardstock Profile.dc.html` is the exception: **it has no pre-paint script** (`:10-25`). It applies theme through a `{{ themeVars }}` inline style on a wrapper `<div>` (`:26`) so the Appearance section's live-preview strip can re-theme instantly without a reload. Its `<style>` block carries only the `--logoTeal` dark override (`:23`), and its `html, body` background is the hard-coded `#FAFAF7` (`:18`) rather than `var(--bg)` — meaning **Profile's page background does not go dark**. Either a bug or a deliberate consequence of the live-preview approach; it needs an owner ruling before the rebuild copies it.

### 5.2 Token classes components rely on

Tokens are CSS custom properties on `:root`, always consumed as `var(--name, <light hex>)` so the light value is the inline fallback. Dark overrides live in a single `:root[data-theme="dark"]` block per page (`Cardstock Home.dc.html:29`).

**Chrome tokens** (light default → dark):

| Token | Light | Dark | Role |
|---|---|---|---|
| `--bg` | `#FAFAF7` | `#161614` | page ground |
| `--card` | `#FFFFFF` | `#1E1E1C` | panel / nav / menu surface |
| `--ink` | `#1C1C1E` | `#E9E9E5` | primary text |
| `--mut` | `#5B5B57` | `#B4B4AE` | secondary text, inactive nav links |
| `--mut2` | `#6B6B66` | `#A8A8A2` | tertiary text, group headings |
| `--mut3` | `#8F8F8A` | `#9A9A94` | quaternary |
| `--mutbg` | `#F3F3EE` | `#2A2A27` | muted fill (account circle, status badge) |
| `--hov` | `#F6F6F2` | `#282825` | hover ground |
| `--line` | `#E4E4E0` | `#33332F` | primary border |
| `--line2` | `#D9D9D4` | `#3E3E39` | |
| `--line3` | `#C9C9C4` | `#4A4A44` | **resize-handle pipe** |
| `--line4` | `#F0F0EC` | `#262623` | row separators |
| `--acc` | `#4A63D0` | `#8C9BF2` | accent, links, active tab underline, focus ring |
| `--accH` | `#3A4FB8` | `#AAB6F6` | accent hover |
| `--btn` | `#4A63D0` | `#4A63D0` | filled button ground (unchanged in dark) |
| `--accBg` | `#EEF1FB` | `#252B44` | accent tint |
| `--accMut` | `#B9C4E8` | `#3A4570` | |
| `--warn` / `--warnInk` | `#8F6614` | `#D6A54A` | |
| `--tooltipBg` | `rgba(255,255,255,0.95)` | `rgba(30,30,28,0.95)` | chart tooltip only |
| `--logoTeal` | `#0E8A7B` | `#3FBFAD` | logo mark only |

(Light values from `Cardstock Home.dc.html:329`, dark from `:29`.)

**State tokens** — `--pos`, `--pos2`, `--neg`, `--neg2`, `--neg3` and the tint helpers `--posBg10`, `--negBg08`, `--negBg10`. Four combinations exist (theme × CVD), declared at `Cardstock Home.dc.html:27` (light-CVD), `:30` (dark-standard), `:31` (dark-CVD), with light-standard as the inline fallbacks:

| | Light std | Light CVD | Dark std | Dark CVD |
|---|---|---|---|---|
| `--pos` | `#157A50` | `#0B69A8` | `#4CC08D` | `#58A9E6` |
| `--pos2` | `#189E63` | `#0072B2` | `#4CC08D` | `#58A9E6` |
| `--neg` | `#C13A3A` | `#CC5F00` | `#E57B7B` | `#F5924E` |
| `--neg2` | `#D64545` | `#D55E00` | `#E57B7B` | `#F5924E` |
| `--neg3` | `#A93838` | `#B34E00` | `#E57B7B` | `#E8874D` |

**The `PAL` duplicate.** Anything drawn by logic rather than CSS — SVG stroke colours, gradients, chip backgrounds — cannot use `var()`, so every page re-declares the whole palette in JavaScript as a four-branch `PAL` object reading the same two `localStorage` keys (`Cardstock Home.dc.html:323-330`; also `Cardstock Charts.dc.html:498`, `:791` which additionally switches a dashed stroke on in CVD). **This is the single biggest theming liability in the port**: two parallel palettes that must agree. In Blazor, resolve it once — either read computed custom properties, or make the C# palette the single source and emit the CSS from it.

`color-scheme: dark` is set inside the dark block (`Cardstock Home.dc.html:29`) so native form controls follow.

CVD also changes non-colour affordances: dashed line patterns (`Cardstock Charts.dc.html:791`) so series stay distinguishable without hue.

---

## 6. Design Composer runtime — `support.js` (scaffolding, discard)

`support.js` is 1,911 lines, generated (`:1` — *"GENERATED from dc-runtime/src/*.ts — do not edit"*), and is the mockup tool's template engine, not product code. It parses each prototype's `<x-dc>` element as a template and the `<script data-dc-script>` block as logic, requiring `class Component extends DCLogic` (`:1708`, with `DCLogic` aliased to the internal `StreamableLogic` at `:1898`), then renders the result through React (`:9-21`). Inside the template it interprets `{{ }}` interpolations, the control-flow tags `sc-if` / `sc-for` / `sc-else` / `dc-import` / `x-import` (`:487`), and the `style-hover` pseudo-attribute; the `hint-placeholder-count` (`:614`) and `hint-placeholder-val` (`:648`) attributes exist purely so a partially-streamed page renders a plausible number of skeleton rows before its data arrives. **It carries no product data and no design decisions.** Every one of its constructs has a direct Blazor equivalent — `sc-for` → `@foreach`, `sc-if` → `@if`, `{{ x }}` → `@x`, `DCLogic` state + `setState` → component fields + `StateHasChanged`, `style-hover` → a CSS `:hover` rule — and the entire file, together with the `<x-dc>` / `<helmet>` / `data-dc-script` wrapper in every `.dc.html`, is deleted in the rebuild. Read it only to decode what a prototype is doing; never port it, and never treat a `hint-placeholder-*` value as a product requirement.

---

## 7. Open questions

1. **Search result ranking.** The prototype has none — fixed group order, corpus-array order within groups. What is the intended ordering against 101,882 cards (`Cardstock Home.dc.html:312`)? Prefix-before-substring? Market-value weighting? Needs an owner ruling.
2. **Search keyboard navigation.** No arrow keys, no Enter-to-select, no `role="listbox"`/`option`, no `aria-activedescendant`. Reproducing this faithfully ships a keyboard-inaccessible control. Assume it should be added — confirm.
3. **Search loading and error states.** Undesigned, because the prototype corpus is synchronous and local. A server query needs both, plus a debounce interval and a minimum-query threshold (the prototype's is 2 characters).
4. **Group caps.** 4/4/5 with no "show all" affordance and no indication that results were truncated. Does the real search need a "see all results" row or a dedicated results page?
5. **Active-nav semantics for leaf pages.** Card, Set, Character, About Data, Legal, and Profile highlight nothing. Should Card/Set/Character light Browse? Blazor's `NavLink` prefix matching would do so by default — a decision, not an accident.
6. **Profile's missing pre-paint script and hard-coded `#FAFAF7` body background** (`Cardstock Profile.dc.html:18`). Bug, or intended cost of live theme preview? If the theme preference moves server-side, this whole divergence disappears.
7. **Theme persistence location.** `localStorage` + pre-paint script, or a server-side user preference emitted during prerender? The prototype answers only the first. Interacts with the render-mode decision (D-013, D-014).
8. **The dual palette.** CSS `var()` tokens and the JS `PAL` object must agree. Which is authoritative in Blazor?
9. **Tooltip mechanism.** Native `title` gives no styling, no touch behaviour, and a browser-controlled delay — with ~110 tooltips carrying real explanatory copy, that is a lot of unstyleable UI. Custom tooltip component, or keep native? The *content* rule (consequence, not identity) is settled; the mechanism is not.
10. **Confirm-flash duration.** Three values (1800 / 2200 / 2600 ms) for one pattern. Pick one, or define when each applies.
11. **Card image licensing** — D-010 (`DECISIONS.md:83-89`). Images exist on disk; nobody has read the source's terms, and serving is a different act from storing. This gates whether the image-slot's real-image path can ship at all.
12. **Card image sizing.** Stored as `{hash}/1600.jpg`; the largest use is the Card lightbox at `min(62vh, 78vw)` and the smallest is a 48×66 row thumb. Serving 1600px files to a 48px slot in an 8-row table is 8 full-size decodes per view. Resize pipeline, or `srcset`/thumbnail derivatives? Undecided.
13. **Missing-image fallback.** `cards.image_hash` presumably can be null, and a file can be absent. The prototype's answer for "no image" is the accent gradient — is that also the answer for "image failed to load"?
14. **Peek panel dialog semantics.** `role="dialog"` with no focus trap, no Escape handler, no scrim, and no `aria-modal`. Non-modal drawer with the wrong role, or an unfinished modal?
15. **Escape does not close overflow menus** on any page. Add it?
16. **Column-width persistence.** Widths are in-memory only; a reload resets them. Persist per user, per table?
17. **Touch and keyboard resizing.** The pipe handles are `mousedown`-only. No touch, no keyboard alternative.

---

## 8. Contradictions found

| Claim | Source doc:line | What the code actually does |
|---|---|---|
| Nav search is "light DOM so theme vars/fonts inherit" | `DESIGN_NOTES.md:123` | **Shadow DOM.** `cardstock-search.js:51` — `this.attachShadow({ mode: 'open' })`, with the header comment at `:3` stating the reason ("internals are invisible to the page's renderer"). Theme vars still work because CSS custom properties inherit through the shadow boundary, and fonts are re-declared explicitly in the shadow CSS (`:26`) rather than inherited — so the stated *outcome* holds while the stated *mechanism* is wrong. Matters for the port: the shadow root's justification is a Design-Composer artefact and should be dropped. |
| "Search box on ALL 10 page navs" | `DESIGN_NOTES.md:123` | **Eleven pages** carry the nav and all eleven carry `<cardstock-search>`: Home:52, Screener:45, Charts:45, Binder:52, Browse:50, Card:50, Set:50, Character:50, About Data, Legal, Profile:39. The "10 pages" figure recurs at `DESIGN_NOTES.md:105` and `:152` and is one short throughout. |
| Watchlist thumbs have "hover scale 3.4×" | `DESIGN_NOTES.md:29` | **No CSS scale on Home at all.** The effect is a *separate* fixed-position 164×226 preview element rendered under `sc-if hasPv` (`Cardstock Home.dc.html:314-318`), positioned by `pvIn` (`:559-568`) at `thumb.right + 10` with a clamped y, and torn down by `pvOut` (`:621`). 164/48 ≈ 3.42, so the ratio is right and the mechanism is not — a Blazor dev implementing `transform: scale(3.4)` would get a clipped, in-flow, wrongly-anchored result. The only real CSS hover-scale in the prototypes is `Cardstock Charts.dc.html:72`, at **`scale(2.2)`** with `transform-origin: left top`. |
| Image-slot ids are `art-<cardid>` | `DESIGN_NOTES.md:29` | True **only on Home and Screener** (`Cardstock Home.dc.html:507`, `:558`; `Cardstock Screener.dc.html:747`, `:839`). Set:235, Character:208, and Binder:484 slugify the **display name** (`'art-' + name.toLowerCase().replace(/[^a-z0-9]+/g,'-')`, producing a trailing hyphen for names ending in `)`), Browse:227 uses the `art-set-` prefix for sets, Card:60/:104 and Charts:73 hard-code `art-umbreon`, and Landing/Profile use non-`art-` ids (`hero-card-*`, `features-card*`, `data-card*`, `profile-avatar`). Consequence: Home's `art-umbreon` and Set's `art-umbreon-vmax-alt-art-` are different keys for the same card. |
| Nav bell "removed from all 7 pages" | `DESIGN_NOTES.md:120`; `HANDOFF.md:97` | Removal is **confirmed complete** — `grep -rn "bell\|Bell\|🔔\|notification"` over all seventeen `*.dc.html` returns zero matches, no stub, no commented markup. The count is stale: eleven pages carry the nav today, not seven (the note is dated 2026-08-08). Recorded because the count, not the fact, is what is wrong. |
| Nav carries a bell (alerts) and an account **menu** (theme toggle, settings, About our data, sign out) | `uploads/CARDSTOCK_UI_SPEC_v1.md:127`, `:256` | Tier 3 / superseded, listed for completeness. No bell exists. The account affordance is a **plain 28px link** straight to Profile (`Cardstock Home.dc.html:53`) — no dropdown, no theme quick-switch, no DEMO tag slot (`uploads/CARDSTOCK_UI_SPEC_v1.md:256` lists a "DEMO tag slot"; demo mode was cut 2026-08-10, `HANDOFF.md:100`). Theme and sign-out live on the Profile page. |
| "Chrome shared by every app page: … pre-paint script reading `localStorage`" | `HANDOFF.md:88` | True on ten of the eleven nav pages. **`Cardstock Profile.dc.html` has no pre-paint script** (`:10-25`); it themes via a `{{ themeVars }}` inline style on a wrapper div (`:26`) and hard-codes `background: #FAFAF7` on `html, body` (`:18`) instead of `var(--bg)`, so its page ground does not follow dark mode. Everything else in that sentence checks out. |
| Nav search corpus links "go to Character/Set/Card pages" | `DESIGN_NOTES.md:123` | Correct but incomplete in a way that matters for the port: the hrefs carry **no identifier** (`cardstock-search.js:37`, `:39`, `:41`) — every character result points at the same `Cardstock Character.dc.html`, every set at `Cardstock Set.dc.html`, every card at `Cardstock Card.dc.html`. |
| Row-actions menu "closes on outside click AND mouse-leave" | `DESIGN_NOTES.md:29` | Accurate for Home (`Cardstock Home.dc.html:124` mouse-leave, `:539-544` outside click), and selecting an item closes it too. Worth recording that the outside-click hook attribute is **not uniform**: Home matches `[role="menu"]` plus `aria-label !== 'Row actions'`; Screener matches `[data-rail-menu]` / `[data-filter-pop]` with `aria-label !== 'Screen actions'` (`Cardstock Screener.dc.html:683-687`); Card matches `[data-watch-pop]` / `[data-lg-pop]` (`:276-277`). No page closes on Escape. |
| Nav search "Esc clears+blurs, outside mousedown closes" | `DESIGN_NOTES.md:123` | Esc is exact (`cardstock-search.js:77`). "Outside mousedown closes" understates it: `_onDoc` calls `_clear()` (`:72`, `:87-91`), which **wipes the query**, not just the menu. |
| Every card, set, and species image is a placeholder slot | `HANDOFF.md:114` (§6, "Not built, deliberately") | **Not a contradiction — verified accurate**, and recorded here because `DECISIONS.md:83-89` (D-010) documents this exact line being twice misread as a claim about the database. It describes the *prototypes*, where every slot is genuinely empty (no page sets `src`). Real images do exist on disk (~3.6 GB, `{ImageDirectory}/{hash}/1600.jpg`, joined via `cards.image_hash`), which is why `image-slot.js`'s author-`src` path (`:1098`, `:1100`) is the one part of that file worth porting. |
