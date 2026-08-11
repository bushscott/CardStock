# Brand system — authoritative token & identity specification

**Status:** derived from Tier-1 sources, verified 2026-08-10.
**Method:** every value below was read out of the prototypes or `brand/brand-tokens.css`. Markdown docs were used only to name intent, and where they disagreed with code, the code won and the disagreement is recorded in §9.

Paths in this document are relative to `CardStock Mockup/` unless absolute.

---

## 1. Identity

### What this reference is

`Cardstock Brand System.dc.html` is a **documentation surface, not a product screen.** It has no nav, no ticker, no theme toggle, no `data-screen-label`, no route. It renders the brand package as a static page: max-width `1020px`, padding `64px 40px 96px`, six numbered sections (`Cardstock Brand System.dc.html:20`, `:36`, `:98`, `:151`, `:174`, `:207`, `:225`).

The upstream handoff says so explicitly: *"Use it as the spec reference; it isn't a product screen"* (`uploads/Brand package creation/README.md:109`).

**Do not build a Blazor route for it.** Its content belongs in this document. If a living style guide is wanted later, that is a new product decision, not a port of this page.

### What the brand covers

Cardstock is a market-data terminal for the Pokémon card aftermarket — *"a data-first terminal with paper sensibility"*. The brand is deliberately **not** Pokémon trade dress: no Pokéballs, no official yellow/blue logotype vibes (`BRAND_BRIEF.md:13`; misuse panels rendered at `Cardstock Brand System.dc.html:78-92`).

Voice, from `Cardstock Brand System.dc.html:210`: *"Matter-of-fact and openly nerdy. Precise numbers over adjectives. Jargon is welcome, explained once. Keyboard shortcuts are copy. No exclamation marks, no emoji; dry humor lives in empty states only."*

### The two token vocabularies — read this before anything else

There are **two disjoint sets of token names in this repository**, and only one of them is actually wired up.

| | Brand package tokens | App tokens |
|---|---|---|
| Defined in | `brand/brand-tokens.css` (31 lines) | inline `<style>` in each `*.dc.html` `<helmet>` + `var(--x, <literal>)` fallbacks |
| Names | `--brand-primary`, `--brand-primary-strong`, `--brand-logo-teal`, `--brand-foil`, `--focus-ring`, `--link`, `--link-hover`, `--series-1..6`, `--ink`, `--bg`, `--card`, `--line`, `--muted`, `--hover` | `--acc`, `--accH`, `--btn`, `--btnH`, `--ink`, `--bg`, `--card`, `--mut`, `--mut2`, `--mut3`, `--mutbg`, `--hov`, `--line`, `--line2`, `--line3`, `--line4`, `--inbg`, `--accBg`, `--accMut`, `--tooltipBg`, `--logoTeal`, `--pos`, `--pos2`, `--neg`, `--neg2`, `--neg3`, `--posBg*`, `--negBg*`, `--warn`, `--warnInk`, `--warnBg` |
| Consumed by | **nothing** — verified: no `.dc.html` links the stylesheet, and no `--brand-*`, `--focus-ring`, `--series-*`, or `--link` reference exists in any prototype | all 11 app screens + 5 marketing pages |

`brand-tokens.css` appears exactly once in the prototypes and only as prose text in a file list (`Cardstock Brand System.dc.html:245`), never as a `<link>`.

**Implementation ruling for Blazor: build against the app token names.** They are what every screen actually renders. Treat `brand-tokens.css` as a colour dictionary, not as a stylesheet to ship. §9 records the naming contradiction.

---

## 2. Colour tokens

### 2.1 Chrome / neutral tokens (theme-varying, colourblind-invariant)

Light values are the literals written as `var(--x, <literal>)` fallbacks throughout the markup and restated in the JS palette at `Cardstock Home.dc.html:329`. Dark values come from the `:root[data-theme="dark"]` block at `Cardstock Home.dc.html:29` (byte-identical block present in Binder `:29`, Card `:27`, Browse `:27`, Charts `:25`, Set `:27`, Character `:27`, Screener `:26`; About Data `:21` and Legal `:21` carry a shorter subset).

| Token | Light | Dark | Role |
|---|---|---|---|
| `--bg` | `#FAFAF7` | `#161614` | Page ground ("paper") |
| `--card` | `#FFFFFF` | `#1E1E1C` | Panel / surface / nav / sticky header |
| `--ink` | `#1C1C1E` | `#E9E9E5` | Primary text, logo strokes |
| `--mut` | `#5B5B57` | `#B4B4AE` | Secondary text (inactive nav, labels) |
| `--mut2` | `#6B6B66` | `#A8A8A2` | Tertiary text (captions, sub-rows, column heads, axis labels) |
| `--mut3` | `#8F8F8A` | `#9A9A94` | **Decorative strokes/handles only — never text** (see §8) |
| `--mutbg` | `#F3F3EE` | `#2A2A27` | Muted chip fill, avatar disc |
| `--hov` | `#F6F6F2` | `#282825` | Row / menu-item hover |
| `--line` | `#E4E4E0` | `#33332F` | Standard hairline (1px) |
| `--line2` | `#D9D9D4` | `#3E3E39` | Stronger divider |
| `--line3` | `#C9C9C4` | `#4A4A44` | Resize handles, heaviest rule |
| `--line4` | `#F0F0EC` | `#262623` | Faintest rule (table row separators) |
| `--inbg` | `#FAFAF7` | `#262624` | Input field background (`Cardstock Profile.dc.html:216-217`) |
| `--acc` | `#4A63D0` | `#8C9BF2` | Accent: links, active tab underline, focus outline, primary chart line |
| `--accH` | `#3A4FB8` | `#AAB6F6` | Accent hover |
| `--btn` | `#4A63D0` | `#4A63D0` | Primary button fill — **does not lighten in dark** |
| `--btnH` | `#3A4FB8` | `#AAB6F6` | Primary button hover (`Cardstock Profile.dc.html:216-217`) |
| `--accBg` | `#EEF1FB` | `#252B44` | Accent-tinted surface |
| `--accMut` | `#B9C4E8` | `#3A4570` | Muted accent (inactive accent strokes) |
| `--tooltipBg` | `rgba(255,255,255,0.95)` | `rgba(30,30,28,0.95)` | Chart tooltip ground |
| `--logoTeal` | `#0E8A7B` | `#3FBFAD` | **Logo sparkline only** (`Cardstock Home.dc.html:32,41`) |

`:root[data-theme="dark"]` also sets `color-scheme: dark` (`Cardstock Home.dc.html:29`), which recolours native scrollbars and form controls. Do not omit it.

### 2.2 State tokens — the full 2×2 (theme × colourblind)

This is the only axis colourblind mode touches. Authority: `Cardstock Home.dc.html:27` (light CVD), `:30` (dark standard), `:31` (dark CVD), and the four-branch JS palette at `Cardstock Home.dc.html:323-330`, cross-checked against `Cardstock Profile.dc.html:218-221`.

| Token | Light standard | Light CVD | Dark standard | Dark CVD | Role |
|---|---|---|---|---|---|
| `--pos` | `#157A50` | `#0B69A8` | `#4CC08D` | `#58A9E6` | Bullish text/glyph |
| `--pos2` | `#189E63` | `#0072B2` | `#4CC08D` | `#58A9E6` | Bullish stroke (chart lines, sparklines) |
| `--neg` | `#C13A3A` | `#CC5F00` | `#E57B7B` | `#F5924E` | Bearish text/glyph |
| `--neg2` | `#D64545` | `#D55E00` | `#E57B7B` | `#F5924E` | Bearish stroke, destructive menu item |
| `--neg3` | `#A93838` | `#B34E00` | `#E57B7B` | `#E8874D` | Bearish deep (borders, error field) |
| `--posBg(α)` | `rgba(24,158,99,α)` | `rgba(0,114,178,α)` | `rgba(24,158,99,α)` | `rgba(0,114,178,α)` | Bullish chip/area fill |
| `--negBg(α)` | `rgba(214,69,69,α)` | `rgba(213,94,0,α)` | `rgba(214,69,69,α)` | `rgba(213,94,0,α)` | Bearish chip/area fill |
| `--warn` / `--warnInk` | `#8F6614` | `#8F6614` (unchanged) | `#D6A54A` | `#D6A54A` (unchanged) | Amber data-caution |
| `--warnBg` | `rgba(176,127,26,0.12)` | unchanged | `rgba(176,127,26,0.20)` | unchanged | Caution chip fill |

Alpha values in use: chips `0.10`; Screener variants `0.06`, `0.07`, `0.08`, `0.25`; chart area fills `0.4`. `Cardstock Profile.dc.html:219-220` uses `0.18`/`0.20` for dark chip fills where the app screens compute alpha via `PAL.posBg(α)`.

**Amber is deliberately CVD-invariant.** It is not a directional colour, so it needs no hue swap; its glyph is `–`, which is already directionless. Verified: no `:root[data-cvd="1"]` block in any screen mentions `--warn`.

### 2.3 The colourblind-safe (Okabe–Ito) variant

`DISPLAY_VOCABULARY.md:76`: *"CVD hues are Okabe-Ito (blue `#0072B2`, vermillion `#D55E00`) adjusted for contrast per surface."*

The two pure Okabe–Ito anchors are `#0072B2` (blue) and `#D55E00` (vermillion). Everything else in the CVD column is one of those two darkened or lightened so it holds contrast on its surface:

| Okabe–Ito anchor | Derived values | Where used |
|---|---|---|
| Blue `#0072B2` | `#0B69A8` (darkened, light text) · `#58A9E6` (lightened, dark theme) · `rgba(0,114,178,α)` (fills, both themes) | replaces every green |
| Vermillion `#D55E00` | `#CC5F00` (light text) · `#B34E00` (light deep) · `#F5924E` (dark) · `#E8874D` (dark deep) · `rgba(213,94,0,α)` (fills) | replaces every red |

**Critical implementation note: there is no canonical light-CVD block.** Each screen declares only the tokens it uses, so the nine `:root[data-cvd="1"]` blocks differ:

| File:line | Declares |
|---|---|
| `Cardstock Home.dc.html:27` | `--pos --pos2 --neg --neg2 --posBg10 --negBg08 --negBg10` (the fullest) |
| `Cardstock About Data.dc.html:24` | `--pos --neg` |
| `Cardstock Card.dc.html:25` | `--pos --neg2` |
| `Cardstock Charts.dc.html:23` | `--pos2 --neg2` |
| `Cardstock Screener.dc.html:24` | `--neg2 --neg3 --negBg07 --negBg06 --negBg25` |
| `Cardstock Binder.dc.html:27` · `Set:25` · `Character:25` | `--pos` |
| `Cardstock Browse.dc.html:25` | `--neg2` |

Blazor must emit **one complete CVD block** covering the union of these tokens. The per-screen fragmentation is a prototype artefact, not a design rule.

### 2.4 Brand-package colours (`brand/brand-tokens.css`)

Recorded for completeness. **None of these token names are consumed by any prototype** (§1).

| Token | Light | Dark | Line (light / dark) | Role as documented |
|---|---|---|---|---|
| `--brand-primary` | `#4A63D0` | `#8C9BF2` | `:5` / `:21` | "Index Indigo" — same value as `--acc` |
| `--brand-primary-strong` | `#3A4FB8` | `#AAB6F6` | `:6` / `:22` | Same value as `--accH` |
| `--brand-logo-teal` | `#0E8A7B` | `#3FBFAD` | `:7` / `:23` | "Ledger Teal" — mark + favicon **only** |
| `--brand-foil` | `#9A7B2D` | `#C9A84C` | `:8` / `:24` | Premium / grade accents, sparing |
| `--focus-ring` | `0 0 0 3px rgba(74,99,208,0.22)` | `0 0 0 3px rgba(140,155,242,0.25)` | `:9` / `:25` | Focus ring (**not what the app does** — §9) |
| `--link` | `#4A63D0` | `#8C9BF2` | `:11` / `:26` | Link colour |
| `--link-hover` | `#3A4FB8` | `#AAB6F6` | `:12` / `:27` | Link hover |
| `--ink` | `#1C1C1E` | — | `:17` | Restated app neutral |
| `--bg` | `#FAFAF7` | — | `:17` | Restated app neutral |
| `--card` | `#FFFFFF` | — | `:17` | Restated app neutral |
| `--line` | `#E4E4E0` | — | `:18` | Restated app neutral |
| `--muted` | `#8A8A86` | — | `:18` | **Stale — fails WCAG AA, see §8** |
| `--hover` | `#F6F6F2` | — | `:18` | Restated app neutral |

**Chart series palette** (`brand/brand-tokens.css:14-15` light, `:28-29` dark; swatches at `Cardstock Brand System.dc.html:141-146`):

| Token | Name | Light | Dark |
|---|---|---|---|
| `--series-1` | Indigo | `#4A63D0` | `#8C9BF2` |
| `--series-2` | Amber | `#C98A0D` | `#E0A93C` |
| `--series-3` | Teal | `#0E8A7B` | `#3FBFAD` |
| `--series-4` | Orchid | `#B85C9E` | `#D98BC4` |
| `--series-5` | Sky | `#4C9FD8` | `#7BBCE8` |
| `--series-6` | Graphite | `#71716D` | `#A5A5A0` |

Rules stated on the page: *"Assign in order; grays last. Direction ▲/▼ keeps the app's green–red"* (`Cardstock Brand System.dc.html:138`), and no series line may borrow the directional green/red (`:101`).

**The app does not use this palette.** Charts and Card colour series **per grade tier**, not per series index — see §2.5 and §9.

### 2.5 Grade-tier colours (what the charts actually use)

`Cardstock Charts.dc.html:375` and `Cardstock Card.dc.html:325` define `TIER_COLORS`, keyed by the 19-value grade vocabulary. Two entries are token-derived (`PAL.acc`, `PAL.warn`, `PAL.mut2`); the rest are literals shared by both files:

| Tier | Colour | Tier | Colour |
|---|---|---|---|
| `Raw` | `PAL.mut2` (`#6B6B66` / `#A8A8A2`) | `PSA 10` | `PAL.acc` (`#4A63D0` / `#8C9BF2`) |
| `Grade 1` | `#A08D78` | `CGC 10` | `#1F8FA8` |
| `Grade 2` | `#97906E` | `CGC 10 Prist.` | `#0F6E86` |
| `Grade 3` | `#7F9668` | `TAG 10` | `#8646B8` |
| `Grade 4` | `#6A9678` | `ACE 10` | `#C24B4B` |
| `Grade 5` | `#5E9490` | `SGC 10` | `#5C6B9E` |
| `Grade 6` | `#578AA3` | `BGS 10` | `#8A7139` |
| `Grade 9` | `PAL.warn` (`#8F6614` / `#D6A54A`) | `BGS 10 Black` | `#2B2D42` |

`Grade 7` / `Grade 8` / `Grade 9.5` differ between the two files — a genuine code-vs-code divergence:

| Tier | `Cardstock Card.dc.html:325` | `Cardstock Charts.dc.html:375` |
|---|---|---|
| `Grade 7` | `#B0552E` | `#A96A4A` |
| `Grade 8` | `#2E7F78` | `#4C8F8A` |
| `Grade 9.5` | `#6E4DB8` | `#7A56C9` |

**Open item for implementation:** pick one set and use it on both surfaces. Neither prototype is more authoritative than the other; this needs an owner ruling.

### 2.6 Marketing-page surfaces (landing pages only)

Not part of the app token set. Recorded so the marketing shell can be reproduced (`uploads/Brand package creation/README.md:31-32,71`, `DESIGN_NOTES.md:145,147`).

| Value | Role |
|---|---|
| `#F1F1EC` | Landing page surface (darker than app `--bg`) |
| `#131316` | Full-bleed dark sections, dark panels |
| `#1B1C1F` | Cards on dark sections |
| `#0F0F11` | Footer |
| `#2A2B2E` / `#232427` | Borders on dark |
| `#F2F2EE` / `#B9B9B4` / `#9A9A96` / `#71716D` | Text ladder on dark |
| `#55555A` | Secondary text on light marketing surfaces |
| `#46C08A` ▲ / `#D0655E` ▼ | Ticker direction colours (marketing only — **not** the app's `--pos`/`--neg`) |

Shadows (`uploads/Brand package creation/README.md:47`): `0 24px 48px rgba(28,28,30,0.25)` floating dark panels · `0 12px 24px rgba(28,28,30,0.2)` scattered cards · `0 16px 32px rgba(0,0,0,0.45)` cards on dark. In-app menu shadow is lighter: `0 6px 20px rgba(20,19,26,0.12)` (`Cardstock Home.dc.html:124`).

---

## 3. Typography

### 3.1 Families and the division of labour

Brand rule, verbatim (`Cardstock Brand System.dc.html:154`): *"No new fonts. The brand rule is the division of labor: Inter talks, JetBrains Mono counts."*

| Family | Role | Weights loaded (app screens) | Weights loaded (marketing + brand pages) |
|---|---|---|---|
| **Inter** | UI, body, labels, buttons, prose | `400;500;600;700` | `400;500;600;700;800` |
| **Inter Tight** | Section/panel headings only, always `700` | `600;700` | *not loaded* |
| **JetBrains Mono** | Every number, ticker, timestamp, kbd hint, eyebrow, badge | `400;500;600` (Charts & Screener add `700`) | `400;500;700` |

Font stacks as written: `font-family: 'Inter', system-ui, sans-serif` (`Cardstock Home.dc.html:18`), `font-family: 'Inter Tight', sans-serif`, `font-family: 'JetBrains Mono', monospace`.

Exact link tags — app screens (10 of 12): `family=Inter:wght@400;500;600;700&family=Inter+Tight:wght@600;700&family=JetBrains+Mono:wght@400;500;600&display=swap` (`Cardstock Home.dc.html:14`). Charts (`:23` region) and Screener add `;700` to JetBrains Mono. Landing, the three product landings and the Brand System page use `family=Inter:wght@400;500;600;700;800&family=JetBrains+Mono:wght@400;500;700&display=swap` (`Cardstock Brand System.dc.html:13`).

Note the split: **Inter 800 exists only on marketing/brand pages; Inter Tight exists only on app pages.** A single Blazor `_Host` font link must load the union: `Inter:wght@400;500;600;700;800`, `Inter+Tight:wght@600;700`, `JetBrains+Mono:wght@400;500;600;700`.

### 3.2 The +15% scale, base 15px

`HANDOFF.md:109`: *"**Typography +15% throughout**, base 15px."*
`DESIGN_NOTES.md:24`: *"All text scaled +15% (base now 15px) across both pages."*
`DESIGN_NOTES.md:87`: *"all text +15% (base 15px); Inter + Inter Tight + JetBrains Mono (all numbers) locked."*

**Verified in code.** Every one of the 12 app screens sets `font-size: 15px` on its root `data-screen-label` element — About Data, Legal, Account, Card, Set, Browse, Character, Home, Charts, Binder, Profile, Screener. The `+15%` is historical (the scale was multiplied once, then frozen); the shipped values are the literal sizes below. **Do not re-apply a 15% multiplier.**

The `.5px` sizes are the fingerprint of that multiplication (13 × 1.15 ≈ 15, 11 × 1.15 ≈ 12.5) and must be preserved verbatim — they are not rounding noise.

### 3.3 Size inventory (app screens, by frequency)

| Size | Uses | Typical role |
|---|---|---|
| `15px` | base + 95 explicit | Body, nav tabs, row primary text, menu items |
| `14.5px` | 29 | Menu items, table cells |
| `14px` | 62 | Buttons, mono values, secondary body |
| `13.5px` | 61 | Dense controls |
| `13px` | 108 | Dense labels, compact rows |
| `12.5px` | 119 (most common) | Captions, sub-rows, column headers, mono metadata |
| `12px` | 54 | Small mono, eyebrows |
| `11.5px` | 35 | Chips |
| `11px` | 44 | Smallest chips, badges |
| `10.5px` | 24 | Mono micro-labels |
| `10px` | 4 | Chart axis micro-labels |

Heading sizes (`Inter Tight` 700): `15px` (2 uses) · `17px` (2) · `17.5px` (9, the standard panel heading) · `18.5px` (8) · `19.5px` (5) · `27px` (3, page title). Nav wordmark is `Inter` 700 `18px` `-0.03em` (`Cardstock Home.dc.html:42`).

### 3.4 Tracking and weight rules

- Tight tracking `-0.02em` to `-0.03em` above 20px; normal below (`Cardstock Brand System.dc.html:158`).
- Wordmark and page titles: `-0.03em`. Section headings: `-0.02em`.
- Uppercase eyebrows/column heads: mono, `letter-spacing: 0.05em`–`0.08em`, `text-transform: uppercase`, weight `500`–`600` (`Cardstock Home.dc.html:93`, `Cardstock Profile.dc.html:101`, `Cardstock Brand System.dc.html:169`).
- Body weights: `400` prose · `500` secondary nav · `600` emphasis/labels/active nav · `700` headings and key numbers.

### 3.5 THE NUMBER RULE (load-bearing)

`HANDOFF.md:151`: *"**Numbers are monospace** (JetBrains Mono), everywhere, including inside prose."*
`DESIGN_NOTES.md:24`: *"JetBrains Mono for ALL numbers."*

This is not a table-alignment convention. It is absolute and it reaches inside sentences. A price, percentage, count, date, timestamp, ticker symbol, grade label, percentile or keyboard hint switches font **mid-paragraph**.

Rendering pattern from the prototypes:

```
Compare on the <a>Umbreon VMAX chart</a> · hover darkens to
<span style="font-family: 'JetBrains Mono', monospace; font-size: 12px;">#3A4FB8</span> + underline
```
(`Cardstock Brand System.dc.html:128` — the hex is mono inside running prose.)

**Blazor consequence:** you cannot satisfy this with a `.mono` class applied at the block level. You need an inline component (e.g. `<Num>`) or a formatting helper that wraps every numeric run in a mono span, and every piece of copy containing a figure must route through it. Mono runs inside prose are typically set 1–2.5px smaller than the surrounding Inter to match x-height (`15px` prose → `12–14px` mono).

What is mono, in full: prices · deltas and percentages · counts · dates and timestamps · ticker symbols (`CDSTK`, `UMBR`) · grade tier labels (`PSA 10`) · signal chips · keyboard hints (`/`) · eyebrow labels · column-header micro-labels · sales-ledger `source` enum values, rendered verbatim lowercase (`DISPLAY_VOCABULARY.md:61`).

What stays Inter: everything else, including headings that contain no figure.

---

## 4. Semantic rules — colour never carries meaning alone

### 4.1 The rule

`DISPLAY_VOCABULARY.md:2`: *"Icon always accompanies color (▲ ▼ – ● ◌ ◆); never color alone."*

Restated as product copy in the settings UI (`Cardstock Profile.dc.html:94`): *"Swaps green→blue and red→orange everywhere state color appears. **Glyphs ▲ ▼ – ◌ never change.**"*

Every state renders `glyph + short name + evidence number`. The glyph is the meaning; the hue is reinforcement. A chip with no glyph is a bug.

### 4.2 The complete glyph inventory

| Glyph | Name | Meaning | Colour token | Rendered in code |
|---|---|---|---|---|
| `▲` | Up triangle | Bullish hit — a tracked signal fired in the favourable direction | `--pos` on `--posBg(0.10)` | `Cardstock Profile.dc.html:103`, `Cardstock Home.dc.html:417,418,422` |
| `▼` | Down triangle | Bearish hit — fired adversely; also drawdown/overhang state | `--neg` on `--negBg(0.10)` | `Cardstock Profile.dc.html:104`, `Cardstock Home.dc.html:420,421` |
| `–` (amber) | En dash, amber | **Caution** — notable but directionless. Complete band list: RSI 70–80 · RS decile exit (80–89th within 3mo of ≥90) · Pop Δ 60d ≥ +2%. No other signal has a caution band | `--warn` on `--warnBg` | `Cardstock Profile.dc.html:105`, `Cardstock Home.dc.html:419` |
| `–` (grey) | En dash, grey | **Quiet** — signal is tracked and computable but sits between its bands | `--mut2` on `--mutbg` | `Cardstock Profile.dc.html:106` |
| `◌` | Dotted circle | **Pending / insufficient** — not yet computable. Chip carries the unlock ETA; tooltip carries the floor rule. Also the hollow marker for a provisional current month on sparklines | `--mut2` on `--mutbg` | `Cardstock Profile.dc.html:107`, `Cardstock Home.dc.html:395` |
| `◆` | Filled diamond | **Composite / product event** — card matched a preset or saved screen, or a sufficiency UNLOCK fired | thesis-coloured (`--pos` bullish screen, `--neg` avoid screen); amber for UNLOCK | `Cardstock Home.dc.html:423` |
| `●` | Filled circle | **Liquidity / state, never directional** — volume, Amihud, dispersion, cross-market gap. Notable = `●` grey + value, else quiet | `--mut2` | **specified only** — `DISPLAY_VOCABULARY.md:2,25-29,50`; **zero occurrences in any prototype** |

The `●` gap is real and verified: `grep` for `●` across all 17 `.dc.html` files returns 0. The four `●` signals are all liquidity metrics that are still data-locked, so no prototype had one to render. It is a specified-but-unbuilt glyph — build it, but expect no pixel reference.

### 4.3 The five tracked-pill states (complete; no others exist)

`DISPLAY_VOCABULARY.md:40-47` — *"A tracked signal ALWAYS renders exactly one pill, in exactly one of five states; no other pill forms exist."* The live reference rendering is the Profile preview strip, `Cardstock Profile.dc.html:103-107`:

| State | Render | Example |
|---|---|---|
| Hit bullish | green `▲` + chip text | `▲ RS 94th` |
| Hit bearish | red `▼` + chip text | `▼ EMA 3/9` |
| Caution | amber `–` + evidence number | `– RSI 71` |
| Quiet | grey `–` + short name + `–` | `– MACD –` |
| Pending | grey `◌` + short name + unlock ETA | `◌ Churn — 12d` |

Glance rule (`DISPLAY_VOCABULARY.md:8`): **coloured = hit, grey = nothing to report.** Pending ETA format: days under 60 (`— 12d`), month beyond (`— Mar '27`) (`DISPLAY_VOCABULARY.md:51`).

Chip styling (`Cardstock Profile.dc.html:103`): `font-family: 'JetBrains Mono'; font-size: 11px; font-weight: 600; padding: 1px 6px; border-radius: 4px;`.

### 4.4 Sufficiency states

`DISPLAY_VOCABULARY.md:55` — every metric on every surface is in exactly one of five states, and this is the complete render set: **OK** (plain) · **LOW DATA** (amber `N OBS` badge) · **LOCKED** (control disabled + countdown copy) · **UNDEFINED window** (gaps render as gaps, never zeros) · **UNSTABLE FIT** (badge).

### 4.5 Confirmed from code: colourblind mode swaps hue only

The task asked for confirmation from the code. Here is the evidence, and one exception.

**Confirmed — glyphs, labels and grammar are untouched:**

1. All nine `:root[data-cvd="1"]` blocks declare **colour properties exclusively** — no `content`, no `::before`, no font, no text change. Verified across `Home:27`, `About Data:24`, `Card:25`, `Charts:23`, `Screener:24`, `Binder:27`, `Set:25`, `Character:25`, `Browse:25`.
2. Glyphs are **static string literals in the data**, structurally unreachable by the CVD flag: `i: '▲'`, `i: '–'`, `i: '▼'`, `i: '◆'` at `Cardstock Home.dc.html:417-423`; `i:'◌'` at `:395`. The CVD branch in `PAL` (`Cardstock Home.dc.html:323-330`) returns **only hex strings and rgba functions** — it has no path to a glyph, a label or a sentence.
3. The Profile preview strip (`:103-107`) renders identical glyph+text with only `var(--pos)`/`var(--neg)` resolving differently, which is the toggle demonstrating itself.
4. The product copy states the invariant to the user (`Cardstock Profile.dc.html:94,96`).

**One exception — CVD is not strictly hue-only in Charts.** It also strengthens line encoding:

- `Cardstock Charts.dc.html:498-500`: with EMA on, `const cvd = localStorage.getItem('cardstock-cvd') === '1'` drives **stroke width `1 → 1.6`** and **dash patterns `none → '2.5 3.5'` (fast) / `none → '9 4'` (slow)**.
- `Cardstock Charts.dc.html:791`: the MACD signal line takes `dash: cvd ? '4 3' : 'none'`.

This is *additional* redundant encoding in exactly the place hue alone would be weakest — two overlapping trend lines — and it is fully consistent with the spirit of the rule. But it means `DISPLAY_VOCABULARY.md:76`'s *"swaps HUE only"* is literally false, and a Blazor chart layer that swaps hue and stops will lose the dash/width distinction. Recorded in §9.

---

## 5. Spacing, borders, density

### 5.1 There is no named spacing scale

Verified: no `--space-*`, `--radius-*` or `--size-*` token exists in `brand/brand-tokens.css` or in any prototype. Spacing is written as literals. What follows is the observed system.

### 5.2 Spacing steps in use

`2 · 3 · 4 · 5 · 6 · 8 · 10 · 12 · 14 · 16 · 20 · 24 · 28px`. Most frequent gaps: `8px` (27) · `4px` (26) · `10px` (20) · `12px` (15) · `6px` (10) · `14px` and `16px` (6 each) · `24px` (4).

- Page gutter: `20px` horizontal.
- Main content: `padding: 16px 20px; gap: 16px; max-width: 1480px; margin: 0 auto` (`Cardstock Home.dc.html:83`).
- Panel header: `padding: 10px 12px 0 12px`.
- Table row: `padding: 7px 12px`.
- Column header row: `padding: 6px 12px`.
- Menu item: `padding: 6px 8px`; menu container `padding: 4px`.
- Chip: `padding: 1px 6px`.

### 5.3 Radii

App (from `Home`, `Screener`, `Charts`, `Binder`, `Card`): `6px` (50 uses) · `4px` (40) · `8px` (38) · `5px` (19) · `3px` (14) · `2px` (14) · `10px` (12) · `1px` (3) · `50%` (avatar, toggle knob).

| Radius | Applied to |
|---|---|
| `2px` | Focus outline rounding, micro-marks |
| `4px` | Chips, pills, badges, menu items, thumbnails |
| `5px` | Mono ticker/badge chips |
| `6px` | Menus, segmented controls, inset panels, small buttons |
| `8px` | Cards and sections (the app's default panel radius) |
| `10px` | Large panels |
| `10px`/`50%` | Toggle track (`36×20`, radius `10px`) / knob (`16×16`) |

Brand-package radii, for marketing surfaces (`uploads/Brand package creation/README.md:45`): `5px` badges · `7px` buttons and inputs · `8–10px` cards · `12px` dark panels · `14–16px` large card slots. Note the app uses `6px`/`8px` buttons where the brand page shows `7px` — see §9.

### 5.4 Borders

Hairlines are **always `1px`**. Weight is expressed by token, never by thickness: `--line4` (faintest, row separators) → `--line` (standard) → `--line2` → `--line3` (heaviest, resize handles). Active nav tab is a `2px solid var(--acc)` bottom border with `margin-bottom: -1px` to overlap the nav's own hairline (`Cardstock Home.dc.html:45`). Logo strokes are `2px` on a `32×32` viewBox.

### 5.5 Density

`font-size: 15px` root. Fixed chrome heights: nav `48px`, market ticker strip `36px`, segmented-control buttons `30px`, watchlist row `min-height: 66px` with a `48×66` thumbnail (5:7 card ratio), CVD toggle `36×20`, avatar disc `28×28`.

The nav is `position: sticky; top: 0; z-index: 20`; table column headers are `position: sticky; top: 48px; z-index: 10` — i.e. **the header offset is hard-coupled to the nav height.** Row menus sit at `z-index: 40`.

User-facing density modes (`DISPLAY_VOCABULARY.md` §13): Screener/Set/Character offer **terminal / binder**; Binder holdings offers **table / gallery**; Charts offers resolution + range (`1Y · 3Y · 5Y · All`). *"Density and theme choices persist per device (localStorage), not per account."*

Column widths on Home are user-resizable and held in state, defaults `{ card: 220, tier: 52, price: 76, chg: 52, spark: 68 }`, clamped `36–420px` (`Cardstock Home.dc.html:331,337`).

### 5.6 Focus, links, motion

```css
a { color: var(--acc, #4A63D0); text-decoration: none; }
a:hover { color: var(--accH, #3A4FB8); text-decoration: underline; }
*:focus-visible { outline: 2px solid var(--acc, #4A63D0); outline-offset: 1px; border-radius: 2px; }
@media (prefers-reduced-motion: reduce) { * { animation-duration: 0.01ms !important; } }
```
`Cardstock Home.dc.html:19-21,25`. The `focus-visible` rule is byte-identical across all 12 app screens.

`DESIGN_NOTES.md:152` records the owner ruling *"too many tooltips is better than not enough"* — roughly 110 controls carry a `title` describing the control's **consequence**, not its name.

---

## 6. Logo and iconography

### 6.1 The mark

Two cards fanned, the front one charting. Stroke-drawn at the app's hairline weight; **the sparkline is the only coloured element** (`Cardstock Brand System.dc.html:39`).

Geometry — `viewBox="0 0 32 32"`, `fill="none"`, all strokes `2px` (`brand/logo-mark.svg`):

| Element | Attributes |
|---|---|
| Back card | `<rect x="5.5" y="5.5" width="15" height="21" rx="2.5" transform="rotate(-12 13 16)">`, stroke `#1C1C1E` |
| Front card | `<rect x="12" y="5.5" width="15" height="21" rx="2.5">`, fill `#FAFAF7`, stroke `#1C1C1E` |
| Sparkline | `<polyline points="15,21.5 17.5,17 19.5,18.5 23.5,12.5">`, stroke `#0E8A7B`, `stroke-linecap/linejoin: round` |
| Endpoint dot | `<circle cx="23.5" cy="12.5" r="1.7" fill="#0E8A7B">` |

### 6.2 Asset inventory (`CardStock Mockup/brand/`)

| File | Size | Contents |
|---|---|---|
| `logo-mark.svg` | 532 B | Light-background outline mark — ink `#1C1C1E` strokes, front card fill `#FAFAF7`, teal `#0E8A7B` sparkline |
| `logo-mark-dark.svg` | 532 B | Dark-background outline mark — strokes `#ECECE6`, front-card fill `#131316`, sparkline `#3FBFAD`. **Identical geometry** |
| `favicon.svg` | 449 B | Filled tile: `<rect width="32" height="32" rx="7" fill="#0E8A7B">`, single white card `x=9.5 y=6.5 w=13 h=19 rx=2`, white polyline `12.5,20.5 15.5,15.5 17.5,17.5 20,11.5`, white dot `r=1.6` at `(20, 11.5)` |
| `favicon-16.png` | 358 B | 16px raster of the tile |
| `favicon-32.png` | 668 B | 32px raster of the tile |
| `apple-touch-icon.png` | 3.6 KB | 180px |
| `og-image.png` | 65.8 KB | 1200×630 social card |
| `brand-tokens.css` | 1.2 KB | Token dictionary — **not linked by anything** (§1) |

The filled tile is a **different drawing**, not a background behind the outline mark: one card instead of two, no fan rotation, `rx=7` tile corner. Do not synthesise it by boxing the outline mark.

### 6.3 Usage rules

**Size ramp** (`Cardstock Brand System.dc.html:51`): `32 / 24 / 20px` outline mark; `16px` favicon (filled tile).

**Nav lockup** (`Cardstock Brand System.dc.html:53`, implemented at `Cardstock Home.dc.html:41-42`): mark `24px` + wordmark Inter `700` `18px` `-0.03em`, gap `10px`. Optional `CDSTK` mono chip (`11px`, weight `500`, `--mut`, `1px` border, radius `5px`, padding `2px 7px` — `Cardstock Brand System.dc.html:28`).

**In-app the mark is inline SVG with themed strokes**, not an `<img>`: `stroke: var(--ink)` on both cards, `fill: var(--card)` on the front card, `stroke/fill: var(--logoTeal)` on the sparkline and dot (`Cardstock Home.dc.html:41`). This is why there is *"no separate dark mark"* (`Cardstock Brand System.dc.html:66`) — the same markup retints. The two standalone SVGs exist for contexts that cannot carry CSS variables.

**The lockup is a link to Home on all 10 nav pages**, with inline `color: inherit; text-decoration: none` so the global `a` rule does not tint it; Account keeps a centred non-link lockup (`DESIGN_NOTES.md:138`, `Cardstock Home.dc.html:41`).

**Clearspace** (`Cardstock Brand System.dc.html:70`): one card-width (½ mark width) on all sides. Never below `16px`; **below `20px` use the filled favicon tile.**

**Sparkline always rises** (`Cardstock Brand System.dc.html:71`): *"The line in the mark is fixed geometry, not data. Never redraw it falling, and never recolor it — its teal is reserved for the logo; red/green mean market direction; UI chrome is indigo."*

**Never** (rendered as four misuse panels, `Cardstock Brand System.dc.html:78-92`): official yellow/blue trade-dress colouring · gradients on the mark · a falling or market-red sparkline · the outline mark on imagery (use the filled tile). Add, from `uploads/Brand package creation/README.md:63`: no drop shadows, no rotation beyond the built-in fan, no Pokéball geometry.

**Teal is logo-only.** `brand/brand-tokens.css:7`: *"mark + favicon ONLY — never text/UI chrome."* Verified: `#0E8A7B` and `#3FBFAD` appear in all 17 prototypes exclusively as `--logoTeal` on mark geometry.

**Favicon wiring:** `<link rel="icon" href="./brand/favicon.svg">` on every app page (`Cardstock Home.dc.html:11`).

### 6.4 Illustration style — schematics, not pictures

`Cardstock Brand System.dc.html:229`: *"Not illustrations — schematics. 2px ink strokes, dashed placeholders, one indigo accent, a mono caption. Drawn like chart annotations, never scenes or characters."*

Two reference empty states (`:232`, `:236`): a dashed card slot (`stroke #C9C9C4`, `stroke-dasharray="5 4"`) beside an indigo `+` glyph; and a known price line (`#1C1C1E`) continuing into a dashed indigo future (`#4A63D0`, `stroke-dasharray="4 4"`). Captions are mono `12px` `--mut2`.

---

## 7. Theming mechanics

### 7.1 Two independent switches

| Concern | Attribute on `<html>` | localStorage key | On-value | Default |
|---|---|---|---|---|
| Theme | `data-theme="dark"` | `cardstock-theme` | `'dark'` (`'light'` written explicitly when chosen) | light — absence of the attribute |
| Colourblind palette | `data-cvd="1"` | `cardstock-cvd` | `'1'` (`'0'` written when turned off) | off |

They are orthogonal, producing the four combinations in §2.2. The dark CSS uses `:not([data-cvd="1"])` / `[data-cvd="1"]` compound selectors to resolve the cross-product without JS (`Cardstock Home.dc.html:30-31`).

**Light is the default by omission.** There is no `:root[data-theme="light"]` block and no `prefers-color-scheme` query anywhere. Light values live as `var(--x, <literal>)` fallbacks in the markup — *"inline styles use `var(--x, <light-standard literal>)` so streaming paints light"* (`DISPLAY_VOCABULARY.md:76`). The system theme is deliberately ignored; the choice is explicit and per-device.

### 7.2 The pre-paint script — exactly how the flash is avoided

`Cardstock Home.dc.html:35` (byte-identical in About Data `:28`, Binder `:35`, Card `:33`, Browse `:33`, Legal `:24`, Character `:33`, Charts `:31`, Screener `:32`, Set `:33`):

```html
<script>if(localStorage.getItem('cardstock-cvd')==='1')document.documentElement.setAttribute('data-cvd','1');if(localStorage.getItem('cardstock-theme')==='dark')document.documentElement.setAttribute('data-theme','dark');</script>
```

Why it works, mechanically:

1. **It is inline and synchronous** — no `src`, no `defer`, no `async`, no `DOMContentLoaded`. HTML parsing halts at this tag, the script runs to completion, then parsing resumes. There is no interval in which the browser can paint.
2. **It sits in the document head, after the `<style>` block that defines `:root[data-theme="dark"]` and `:root[data-cvd="1"]`, and before any body content.** The CSS rules already exist in the stylesheet when the attribute lands, so the very first style resolution of the first body element already sees dark values.
3. **It writes to `document.documentElement`** — `<html>`, which exists as soon as parsing begins. It does not touch `<body>` (not yet parsed) and never queries the DOM.
4. **It reads localStorage, which is synchronous.** No promise, no fetch, no round trip.
5. **The default requires no work.** Light needs no attribute, so the untouched document is already correctly themed; the script only ever *adds*.
6. **`color-scheme: dark` rides along inside `:root[data-theme="dark"]`** (`Cardstock Home.dc.html:29`), so native scrollbars and form controls are dark from first paint too — a common residual flash this avoids.

If the attribute were set after first paint (a `DOMContentLoaded` handler, a component `OnInitialized`, or a deferred bundle) the user would see a white page repaint to dark — the flash of incorrect theme.

**Blazor consequence.** The pre-paint script must be an **inline `<script>` in `App.razor`'s `<head>`, placed after the token `<style>`/`<link>` and before `<body>`.** It cannot live in a `.js` file loaded with `defer`, cannot be a JSInterop call, and cannot run from a component lifecycle method — all of those execute after the first paint. Under Interactive Server this is doubly important: the pre-rendered HTML arrives over the wire and paints long before the circuit connects.

The same constraint explains the fallback pattern `var(--acc, #4A63D0)` on every inline style — it lets streamed/pre-rendered markup paint correct light colours before any stylesheet cascade fully resolves. Reproduce the fallbacks; they are load-bearing during pre-render, not redundancy.

### 7.3 Applying and persisting a change

`Cardstock Profile.dc.html:234-237`:

```js
setLight:   () => { localStorage.setItem('cardstock-theme', 'light'); this.setState({ theme: 'light' }); },
setDark:    () => { localStorage.setItem('cardstock-theme', 'dark');  this.setState({ theme: 'dark'  }); },
toggleCvd:  () => { localStorage.setItem('cardstock-cvd', cvd ? '0' : '1'); this.setState({ cvd: !cvd }); },
```

Write-then-render, no confirmation step, no Save button — the tooltips promise *"applies immediately and is remembered on this device"* (`Cardstock Profile.dc.html:87-88`).

Read-back on mount, `Cardstock Profile.dc.html:207-212`: only `'dark'` and `'1'` are honoured; anything else falls through to the defaults.

### 7.4 The Profile page's local override (prototype-only)

Profile and Account do **not** carry the full `:root[data-theme="dark"]` block (they declare only `--logoTeal`, at `Cardstock Profile.dc.html:23` and `Cardstock Account.dc.html:21`). Instead `vars(dark, cvd)` (`Cardstock Profile.dc.html:214-223`) returns a style object with `display: 'contents'` plus `colorScheme` and every token, applied to a wrapper element — so the settings page can re-theme itself live without a reload.

That is a prototype device for demonstrating the toggle in place. **In Blazor, set the attribute on `<html>` via a small JS helper and let the cascade do the work** — no wrapper, no duplicated palette.

Beware one inconsistency in that local copy: it sets dark `--accH: '#8CA4F0'`, whereas every other screen's dark block sets `--accH: #AAB6F6` (`Cardstock Home.dc.html:29`). Use `#AAB6F6` — it is the value in 10 files and the brand-package value (`brand/brand-tokens.css:22`).

### 7.5 What the settings UI looks like

`Cardstock Profile.dc.html:80-111`. Panel heading "Appearance" (Inter Tight 700 `17.5px`). Theme is a two-button segmented control (`1px var(--line)` border, radius `6px`, `overflow: hidden`, buttons `30px` tall, `padding: 0 14px`, Inter `600` `14px`; the selected button fills `var(--btn)` with `#FFFFFF` text). The colourblind control is a `role="switch"` with `aria-checked`, `36×20`, radius `10px`, track `var(--btn)` when on / `var(--line)` when off, white `16×16` knob translating `16px` over `0.15s`.

Below both sits a live **Preview** strip rendering all five pill states plus a `+4.2%` / `−1.8%` pair (`:100-110`) — the toggle demonstrating its own effect, which is also the canonical rendering of the pill vocabulary in §4.3.

---

## 8. Known issues

### 8.1 The deferred WCAG failure — `#8A8A86`

**Token:** `--muted`
**Value:** `#8A8A86`
**Defined at:** `CardStock Mockup/brand/brand-tokens.css:18`
**Also hard-coded 39× in `Cardstock Brand System.dc.html` and 3× in `Cardstock Landing.dc.html`.**

Recorded at `DESIGN_NOTES.md:26`: *"Known issue (deferred): `#8A8A86` small text fails WCAG AA (3.2:1) — user postponed contrast pass."* Still listed as open at `DESIGN_NOTES.md:166`: *"4. Deferred: WCAG contrast pass on muted grey small text."*

**Confirmed from the CSS, with an important correction.** Recomputing WCAG 2.x relative luminance for `#8A8A86`:

| Foreground | Background | Ratio | AA small text (4.5:1) | AA large/UI (3:1) |
|---|---|---|---|---|
| `#8A8A86` | `#FAFAF7` (`--bg`) | **≈ 3.3 : 1** | ✗ FAIL | ✓ pass |
| `#8A8A86` | `#FFFFFF` (`--card`) | **≈ 3.5 : 1** | ✗ FAIL | ✓ pass |

The documented figure of `3.2:1` is slightly conservative; the failure it reports is real either way. Since `--muted` is used almost exclusively for small text (captions, labels, mono metadata at `11–13px`), 4.5:1 is the applicable threshold.

**But the app was already fixed, and the brand package was not.**

`DESIGN_NOTES.md:136` records the remediation: *"Light-theme greys darkened everywhere (dark theme already passed): mut2 `#8A8A86`→`#6B6B66` (≈4.8–5.4:1 on card/bg/mutbg), mut3 `#B0B0AB`→`#8F8F8A` for decorative strokes/handles (≥3:1), and every `color:var(--mut3,…)` TEXT usage (chart axis labels, footnotes) promoted to `var(--mut2)` — small-text grey hierarchy is now 2 levels (mut, mut2), differentiate via size/weight instead."* Warn gold was darkened in the same pass, `#B07F1A`→`#8F6614` (5.1:1), at `DESIGN_NOTES.md:137`.

Verified by counting `#8A8A86` per file:

| File group | Occurrences |
|---|---|
| All 11 app screens (Home, Screener, Charts, Binder, Card, Browse, Set, Character, About Data, Profile, Account, Legal) | **0** |
| `Cardstock Brand System.dc.html` | 39 |
| `Cardstock Landing.dc.html` | 3 |
| `brand/brand-tokens.css:18` | 1 (`--muted`) |

**Resolution for the Blazor build:**
- The **app** is clean. Use `--mut #5B5B57` and `--mut2 #6B6B66`; the grey ladder for text is two levels only.
- `--mut3 #8F8F8A` is **decorative-only** — resize handles, dashed placeholder strokes, chart gridlines. Never set text in it. This is a rule, not a preference; violating it re-introduces the failure.
- **`#8A8A86` must not be carried into the app.** `brand/brand-tokens.css:18` is stale by four days relative to the contrast pass and should be corrected to `#6B6B66` if that file is ever adopted.
- The **Brand System and Landing pages still carry the failing value.** They are prototypes; if either is ported, the greys need the same treatment.

### 8.2 Other open items

| Issue | Evidence | Impact |
|---|---|---|
| Chart series palette never adopted | `DESIGN_NOTES.md:133`: *"NOT done: chart series recolor to brand 6-series palette (TIER_COLORS in Charts keeps its per-grade hues)"* — verified: `#C98A0D`, `#B85C9E`, `#4C9FD8`, `#D98BC4`, `#7BBCE8` appear **only** in `Cardstock Brand System.dc.html` | Two competing chart palettes exist. The app's per-grade `TIER_COLORS` is the one that ships |
| Foil unused in-app | `DESIGN_NOTES.md:131`: *"Foil `#9A7B2D` unused in app so far (candidate: LOW CONFIDENCE badges — currently warn gold)"* — verified: `#9A7B2D` absent from all 11 app screens | `--brand-foil` has no app role yet. LOW CONFIDENCE uses `--warn` |
| `TIER_COLORS` diverge between Card and Charts | `Cardstock Card.dc.html:325` vs `Cardstock Charts.dc.html:375` — Grade 7/8/9.5 differ (§2.5) | The same grade renders two colours on two screens. Needs an owner ruling |
| `●` glyph specified but never rendered | `DISPLAY_VOCABULARY.md:2,25-29,50` vs 0 occurrences in any prototype | No pixel reference for liquidity/state chips |
| Light-CVD blocks are per-screen fragments | Nine differing `:root[data-cvd="1"]` blocks (§2.3) | A shared Blazor stylesheet must emit their union |
| No responsive breakpoints | `uploads/Brand package creation/README.md:118`: *"No responsive breakpoints are specified in the prototype (desktop-first at 1080px)"*; Charts sets `min-width: 1080px`, Screener `overflow: hidden` on a `100vh` shell | Small-screen behaviour is undesigned |
| Fonts load from Google Fonts CDN | `Cardstock Home.dc.html:12-14` | Self-hosting is a deployment decision; weights in §3.1 must be preserved either way |

---

## 9. Contradictions found

Tier-1 code wins in every row. `DESIGN_NOTES.md` and `DISPLAY_VOCABULARY.md` are Tier 2; `BRAND_BRIEF.md` and `uploads/Brand package creation/README.md` describe the brand package as delivered, which the app then partly declined to adopt.

| # | Claim | Source doc:line | What the code actually does |
|---|---|---|---|
| 1 | *"Canonical source: `brand-tokens.css`"* | `uploads/Brand package creation/README.md:18` | **No prototype links it.** Its only appearance is prose in a file list at `Cardstock Brand System.dc.html:245`. No `--brand-*`, `--focus-ring`, `--series-*` or `--link` reference exists in any `.dc.html`. The live tokens are `--acc`/`--accH`/`--btn`/`--mut`/… declared inline per screen |
| 2 | `--muted: #8A8A86` is a current app neutral | `brand/brand-tokens.css:18`; `BRAND_BRIEF.md:26` | Replaced app-wide by `--mut2: #6B6B66` in the contrast pass (`DESIGN_NOTES.md:136`). `#8A8A86` occurs **0 times** across all 11 app screens |
| 3 | *"Deferred: WCAG contrast pass on muted grey small text"* — still open | `DESIGN_NOTES.md:26`, `DESIGN_NOTES.md:166` | **The pass was done**, in the same file at `DESIGN_NOTES.md:135-138` (dated 2026-08-10). Lines 26 and 166 were never updated. The app is fixed; only `brand-tokens.css`, the Brand System page and the Landing page still carry `#8A8A86` |
| 4 | Contrast failure is `3.2:1` | `DESIGN_NOTES.md:26` | Recomputed: **≈3.3:1** on `#FAFAF7`, **≈3.5:1** on `#FFFFFF`. Both fail AA small text (4.5:1). The doc's number is conservative; the conclusion holds |
| 5 | Chrome light→dark: *"mut2 `#8A8A86`→`#A8A8A2`"* | `DISPLAY_VOCABULARY.md:85` | Light `--mut2` is `#6B6B66`, not `#8A8A86` (`Cardstock Home.dc.html:329`). Dark `#A8A8A2` is correct |
| 6 | *"accent `#3B5BD6`→`#7290EA` · button `#3B5BD6`→`#4A66D8`"* | `DISPLAY_VOCABULARY.md:85` | Pre-branding values. Now `--acc` `#4A63D0`→`#8C9BF2` and `--btn` `#4A63D0` in both themes (`Cardstock Home.dc.html:29,329`). The swap is itemised at `DESIGN_NOTES.md:131` but §85 was never updated |
| 7 | Focus ring is `0 0 0 3px rgba(74,99,208,0.22)` / `rgba(140,155,242,0.25)` dark | `brand/brand-tokens.css:9,25`; `Cardstock Brand System.dc.html:192`; `uploads/Brand package creation/README.md:46` | Every app screen uses `*:focus-visible { outline: 2px solid var(--acc); outline-offset: 1px; border-radius: 2px; }` (`Cardstock Home.dc.html:21`) — a `2px` outline, not a `3px` box-shadow ring. Byte-identical across all 12 screens. The `--focus-ring` token is never referenced |
| 8 | *"Colorblind mode swaps HUE only"* | `DISPLAY_VOCABULARY.md:76` | Hue-only for **glyphs, labels and grammar** — confirmed. But Charts also changes **line geometry**: EMA fast/slow get `stroke-width 1 → 1.6` and dashes `'2.5 3.5'` / `'9 4'` (`Cardstock Charts.dc.html:498-500`), and the MACD signal line takes `dash: '4 3'` (`:791`). Redundant encoding, consistent in spirit, but "hue only" is literally false |
| 9 | Six-colour Okabe–Ito-tuned chart series ship with the brand | `Cardstock Brand System.dc.html:101,141-146`; `brand/brand-tokens.css:14-15,28-29`; `uploads/Brand package creation/README.md:35` | Never adopted. `#C98A0D`, `#B85C9E`, `#4C9FD8`, `#D98BC4`, `#7BBCE8` appear **only** on the Brand System page. Charts and Card use per-grade `TIER_COLORS` (`Charts:375`, `Card:325`). Acknowledged at `DESIGN_NOTES.md:133` |
| 10 | Foil `#9A7B2D` is a support colour for *"grade premiums, PSA 10 highlights"* and LOW CONFIDENCE badges | `Cardstock Brand System.dc.html:113-114,199`; `uploads/Brand package creation/README.md:26` | Absent from all 11 app screens. LOW CONFIDENCE renders in `--warn` (`#8F6614` / `#D6A54A`). `DESIGN_NOTES.md:131` confirms: *"Foil `#9A7B2D` unused in app so far"* |
| 11 | Inter weights are *"400/500/600/650/700/800"* | `uploads/Brand package creation/README.md:39` | `650` is not a loaded weight anywhere. App screens load `400;500;600;700`; marketing/brand pages load `400;500;600;700;800`. Inter Tight `600;700` (app only) is omitted from the README entirely |
| 12 | Radius scale: *"5px (badges) · 7px (buttons, inputs) · 8–10px (cards)"* | `uploads/Brand package creation/README.md:45`; `Cardstock Brand System.dc.html:182,191` | `7px` never appears in the app screens. App buttons/inputs use `6px`; chips use `4px`; `5px` is for mono ticker chips. The brand-page radii are marketing-surface values |
| 13 | *"`--bg: #F1F1EC`"* listed among app neutrals | `uploads/Brand package creation/README.md:31` (hedged as "landing page surface") | App `--bg` is `#FAFAF7`; `#F1F1EC` is the marketing surface only. `BRAND_BRIEF.md:26` correctly states `#FAFAF7` |
| 14 | Direction colours are ▲ `#46C08A` / ▼ `#D0655E` | `uploads/Brand package creation/README.md:28,75` | Those are the **landing-page ticker** values. In-app: `--pos` `#157A50` light / `#4CC08D` dark, `--neg` `#C13A3A` light / `#E57B7B` dark (`Cardstock Home.dc.html:327,30`) |
| 15 | Dark `--accH` is `#8CA4F0` | `Cardstock Profile.dc.html:216` | Every other screen sets `#AAB6F6` (`Cardstock Home.dc.html:29` and 9 more), matching `brand/brand-tokens.css:22`. Profile's local `vars()` copy is the outlier — use `#AAB6F6` |
| 16 | `Grade 7` = `#B0552E`, `Grade 8` = `#2E7F78`, `Grade 9.5` = `#6E4DB8` | `Cardstock Card.dc.html:325` | `Cardstock Charts.dc.html:375` uses `#A96A4A`, `#4C8F8A`, `#7A56C9` for the same tiers. **Code vs code** — both Tier 1, needs an owner ruling |
| 17 | *"Icon always accompanies color (▲ ▼ – ● ◌ ◆)"* — six-glyph set | `DISPLAY_VOCABULARY.md:2` | Five glyphs render. `●` has **zero occurrences** in any of the 17 prototypes. `Cardstock Profile.dc.html:94` states the invariant with only four: *"Glyphs ▲ ▼ – ◌ never change"* |
| 18 | Brand version *"v1.0 · Aug 2026"* | `Cardstock Brand System.dc.html:33,251` | `brand/brand-tokens.css:1` declares *"v1.1 (Aug 2026)"*. The page documents v1.0 while shipping v1.1 tokens |

---

## Source inventory

| Tier | File | Role here |
|---|---|---|
| 1 | `CardStock Mockup/Cardstock Brand System.dc.html` | Brand reference page — logo, colour, type, components, voice, empty states |
| 1 | `CardStock Mockup/brand/brand-tokens.css` | Brand token dictionary (v1.1) — **not wired to anything** |
| 1 | `CardStock Mockup/brand/*.svg`, `*.png` | Logo, favicon, social assets |
| 1 | `CardStock Mockup/Cardstock Home.dc.html` | **The canonical app token declaration** — fullest `:root` blocks, pre-paint script, four-branch `PAL` |
| 1 | `CardStock Mockup/Cardstock Profile.dc.html` | Theme + CVD controls, persistence, the five-pill preview strip |
| 1 | `CardStock Mockup/Cardstock Charts.dc.html`, `Cardstock Card.dc.html` | `TIER_COLORS`, CVD dash/width encoding |
| 2 | `CardStock Mockup/DISPLAY_VOCABULARY.md` | Glyph and state vocabulary — §75-86 partly stale |
| 2 | `CardStock Mockup/DESIGN_NOTES.md` | Branding pass, contrast pass, deferral log |
| 2 | `CardStock Mockup/HANDOFF.md` | Type scale and the mono-numbers rule (`:109`, `:151`) |
| 3 | `CardStock Mockup/BRAND_BRIEF.md` | Original brief — rationale only |
| 3 | `CardStock Mockup/uploads/Brand package creation/README.md` | Brand package handoff — accurate for marketing surfaces, overstates app adoption |
