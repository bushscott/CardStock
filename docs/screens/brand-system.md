# Brand system — tokens, type, glyphs, theming

> **Authority.** Everything below is read from Tier 1 code: the `.dc.html` prototypes and `brand/brand-tokens.css`.
> Markdown docs (`DESIGN_NOTES.md`, `DISPLAY_VOCABULARY.md`, `HANDOFF.md`, `BRAND_BRIEF.md`) are Tier 2/3 and are cited only where they add rationale — never as the value of a token. Where they disagree with the code, §9 records it.
> Paths are relative to `CardStock Mockup/` unless absolute.

---

## 1. Identity

**This is not a product screen.** `Cardstock Brand System.dc.html` is a *specimen sheet* — a static, single-column documentation page (max-width 1020px, padding `64px 40px 96px`, `Cardstock Brand System.dc.html:20`) with six numbered sections: 01 Logo · 02 Color · 03 Type · 04 Components · 05 Voice · 06 Empty states. It has no nav, no search, no theme toggle, and no app chrome. It ships with the brand package and is not routed in the Blazor app.

It carries three prototype props, all default `true` (`Cardstock Brand System.dc.html:255–259`): `tickerBadge` (the `CDSTK` chip), `showDonts` (the four misuse tiles), `showIllustrationSpec` (section 06).

**What this document covers**, and what a Blazor implementation must reproduce:

| Scope | Where it is authoritative |
|---|---|
| Brand accent, foil, logo teal, chart series | `brand/brand-tokens.css` + `Cardstock Brand System.dc.html` §02 |
| App runtime palette (light / dark / CVD ×2) | the `<style>` helmet block + the `PAL` object in every app `.dc.html` |
| Typography families, weights, scale | app helmet font links + inline styles; `Cardstock Brand System.dc.html` §03 |
| Glyph grammar (color never alone) | `DISPLAY_VOCABULARY.md` §1–§3 + the live preview strip in `Cardstock Profile.dc.html:99–107` |
| Theme + colorblind persistence | pre-paint script, present on 11 app pages |
| Logo assets | `brand/*.svg`, `brand/*.png` |

**Two token systems exist and they are not the same file.** `brand/brand-tokens.css` is the *brand package* deliverable — a standalone `:root` + `[data-theme="dark"]` sheet. **No app prototype links it.** The app pages instead inline the light values as `var(--token, #LITERAL)` fallbacks and declare only the dark/CVD overrides in a helmet `<style>`. See §7 and §9.

---

## 2. Color tokens

### 2.1 How light theme is expressed (critical for Blazor)

There is **no `:root { }` light block in any app prototype.** Light is the absence of `data-theme`. Every light value lives in two places, kept in sync by hand:

1. **Inline CSS fallbacks** — `color: var(--mut2, #6B6B66)` etc. `DISPLAY_VOCABULARY.md:76` gives the reason: *"inline styles use var(--x, <light-standard literal>) so streaming paints light."*
2. **The `PAL` JavaScript object**, for values computed in script (chart strokes, SVG fills). Four branches: light-standard, light-CVD, dark-standard, dark-CVD. `Cardstock Home.dc.html:323–330`, identical in `Cardstock Charts.dc.html:331–338`, `Cardstock Screener.dc.html:419–426`, `Cardstock Binder.dc.html:332–339`, `Cardstock Card.dc.html:264–271`, `Cardstock Browse.dc.html:161–168`, `Cardstock Set.dc.html:146–153`, `Cardstock Character.dc.html:134–141`.

`PAL` is the single most complete statement of the palette in the codebase. A Blazor implementation should invert this: declare a real `:root` light block, and keep the same hexes.

### 2.2 Chrome (theme-only; identical in standard and colorblind modes)

| Token | Light | Dark | Role |
|---|---|---|---|
| `--bg` | `#FAFAF7` | `#161614` | Page ground (warm off-white "cardstock") |
| `--card` | `#FFFFFF` | `#1E1E1C` | Panel / row surface |
| `--ink` | `#1C1C1E` | `#E9E9E5` | Primary text |
| `--mut` | `#5B5B57` | `#B4B4AE` | Secondary text (labels, inactive nav) |
| `--mut2` | `#6B6B66` | `#A8A8A2` | Tertiary text (captions, axis labels, footnotes) |
| `--mut3` | `#8F8F8A` | `#9A9A94` | **Decorative strokes/handles only — never text** (see §8) |
| `--mutbg` | `#F3F3EE` | `#2A2A27` | Neutral chip / avatar fill |
| `--hov` | `#F6F6F2` | `#282825` | Row + button hover |
| `--line` | `#E4E4E0` | `#33332F` | Standard 1px hairline |
| `--line2` | `#D9D9D4` | `#3E3E39` | Emphasised divider |
| `--line3` | `#C9C9C4` | `#4A4A44` | Heavy/handle rule, dashed placeholders |
| `--line4` | `#F0F0EC` | `#262623` | Faintest rule (zebra, inner grid) |
| `--inbg` | `#FAFAF7` | `#262624` | Input field fill |
| `--acc` | `#4A63D0` | `#8C9BF2` | Links, active tab underline, focus outline |
| `--accH` | `#3A4FB8` | `#AAB6F6` | Link/accent hover |
| `--btn` | `#4A63D0` | `#4A63D0` | Primary button fill (**does not lift in dark**) |
| `--btnH` | `#3A4FB8` | `#AAB6F6` | Primary button hover |
| `--accBg` | `#EEF1FB` | `#252B44` | Accent-tinted surface |
| `--accMut` | `#B9C4E8` | `#3A4570` | Accent-muted stroke |
| `--tooltipBg` | `rgba(255,255,255,0.95)` | `rgba(30,30,28,0.95)` | Tooltip ground |
| `--logoTeal` | `#0E8A7B` | `#3FBFAD` | Logo sparkline ONLY |

Light values: `Cardstock Home.dc.html:329` (PAL light chrome) and `Cardstock Profile.dc.html:217` (`--inbg`, `--btnH`).
Dark values: `Cardstock Home.dc.html:29` + `:32`; PAL dark chrome `Cardstock Home.dc.html:328`.
`--tooltipBg` light: inline fallback, e.g. `rgba(255,255,255,0.95)`; one page uses `0.96` — see §9.

### 2.3 State colors — the 2×2 matrix (theme × colorblind)

The complete authority is the `PAL` branch block, `Cardstock Home.dc.html:324–327`. `--pos`/`--neg` are the *text/glyph* hues; `--pos2`/`--neg2`/`--neg3` are the *graphic* hues (chart strokes, sparklines, bar fills).

| Token | Light standard | Light CVD | Dark standard | Dark CVD |
|---|---|---|---|---|
| `--pos` | `#157A50` | `#0B69A8` | `#4CC08D` | `#58A9E6` |
| `--pos2` | `#189E63` | `#0072B2` | `#4CC08D` | `#58A9E6` |
| `--neg` | `#C13A3A` | `#CC5F00` | `#E57B7B` | `#F5924E` |
| `--neg2` | `#D64545` | `#D55E00` | `#E57B7B` | `#F5924E` |
| `--neg3` | `#A93838` | `#B34E00` | `#E57B7B` | `#E8874D` |
| `--warn` / `--warnInk` | `#8F6614` | `#8F6614` (unchanged) | `#D6A54A` | `#D6A54A` (unchanged) |

Tint bases (alpha applied per use site; `PAL` exposes them as functions `posBg(a)` / `negBg(a)`, `Cardstock Home.dc.html:324–327`):

| Tint | Standard base | CVD base |
|---|---|---|
| positive | `rgba(24,158,99,α)` | `rgba(0,114,178,α)` |
| negative | `rgba(214,69,69,α)` | `rgba(213,94,0,α)` |
| warn | `rgba(176,127,26,α)` in every mode | same |

Observed α values in the prototypes: `.06 .07 .08 .10 .12 .25` light, `.18 .20` dark (`Cardstock Profile.dc.html:219–221`; `Cardstock Screener.dc.html:24`; `Cardstock Home.dc.html:27`). Named helper tokens exist where a tint is used in an inline style: `--posBg`, `--posBg10`, `--negBg`, `--negBg06`, `--negBg07`, `--negBg08`, `--negBg10`, `--negBg25`, `--warnBg`.

The CVD hues are Okabe-Ito **blue `#0072B2`** and **vermillion `#D55E00`**, darkened/lightened per surface for contrast (`#0B69A8`, `#CC5F00`, `#B34E00` light; `#58A9E6`, `#F5924E`, `#E8874D` dark).

### 2.4 The CSS override cascade

Colorblind mode is *not* a full palette. It is a thin override that touches only `--pos*`, `--neg*` and their tints. Four selectors, in this order (`Cardstock Home.dc.html:27–32`):

| Selector | What it sets |
|---|---|
| `:root[data-cvd="1"]` | light CVD state hues + tints |
| `:root[data-theme="dark"]` | **all** dark chrome + `--warn` (state-neutral) |
| `:root[data-theme="dark"]:not([data-cvd="1"])` | dark standard state hues |
| `:root[data-theme="dark"][data-cvd="1"]` | dark CVD state hues |

Every page declares only the subset it actually uses inline, so **no two pages carry the same block**:

| Block | Pages | Shape |
|---|---|---|
| `:root[data-cvd="1"]` | 9 pages, all different | Home 7 tokens (`:27`) · Screener 5 (`:24`) · Charts 2 (`:23`) · Card 2 (`:25`) · About Data 2 (`:24`) · Binder 1 (`:27`) · Browse 1 (`:25`) · Character 1 (`:25`) · Set 1 (`:25`). Absent from Legal, Profile, Account. |
| Dark chrome — **long** (adds `--line3`, `--tooltipBg`, `--accBg`, `--accMut`) | 8: Home, Screener, Charts, Binder, Card, Browse, Set, Character | `Cardstock Home.dc.html:29` |
| Dark chrome — **short** | 2: About Data, Legal | `Cardstock Legal.dc.html:21` — same tokens minus those four, plus `--logoTeal` folded in |
| Dark chrome — **none** | 2: Profile, Account | theme applied in-component instead — see §7.4 |

**A Blazor implementation must declare the union once, globally**, and take the long block as canonical.

### 2.5 Brand tokens (`brand/brand-tokens.css`)

Header comment: *"Cardstock brand tokens — v1.1 (Aug 2026) / Indigo primary (teal is logo-only)"* (`brand/brand-tokens.css:1–2`).

| Token | Light | Dark | Role |
|---|---|---|---|
| `--brand-primary` | `#4A63D0` "Index Indigo" | `#8C9BF2` | Everything interactive |
| `--brand-primary-strong` | `#3A4FB8` | `#AAB6F6` | Hover / pressed |
| `--brand-logo-teal` | `#0E8A7B` "Ledger Teal" | `#3FBFAD` | Mark + favicon **ONLY — never text/UI chrome** |
| `--brand-foil` | `#9A7B2D` | `#C9A84C` | Premium / grade accents, sparing |
| `--focus-ring` | `0 0 0 3px rgba(74,99,208,0.22)` | `0 0 0 3px rgba(140,155,242,0.25)` | Focus shadow |
| `--link` | `#4A63D0` | `#8C9BF2` | |
| `--link-hover` | `#3A4FB8` | `#AAB6F6` | |
| `--ink` | `#1C1C1E` | — | restated for reference |
| `--bg` | `#FAFAF7` | — | restated |
| `--card` | `#FFFFFF` | — | restated |
| `--line` | `#E4E4E0` | — | restated |
| `--muted` | `#8A8A86` | — | restated — **stale, see §8/§9** |
| `--hover` | `#F6F6F2` | — | restated |

Citations: light `brand/brand-tokens.css:5–18`, dark `:21–29`.

### 2.6 Chart series (6, Okabe-Ito derived)

`brand/brand-tokens.css:14–15` (light) and `:28–29` (dark); named in `Cardstock Brand System.dc.html:141–146`.

| Token | Name | Light | Dark |
|---|---|---|---|
| `--series-1` | Indigo | `#4A63D0` | `#8C9BF2` |
| `--series-2` | Amber | `#C98A0D` | `#E0A93C` |
| `--series-3` | Teal | `#0E8A7B` | `#3FBFAD` |
| `--series-4` | Orchid | `#B85C9E` | `#D98BC4` |
| `--series-5` | Sky | `#4C9FD8` | `#7BBCE8` |
| `--series-6` | Graphite | `#71716D` | `#A5A5A0` |

Rules (`Cardstock Brand System.dc.html:101`, `:138`): assign in order, greys last; **no series line may borrow the ▲/▼ green–red**; the six are tuned from Okabe-Ito.
There is **no CVD variant of the series palette** — the series colors already are the CVD-safe set. Verified: no `--series-*` appears under any `[data-cvd]` selector anywhere.

### 2.7 Brand-specimen-only colors

These appear in `Cardstock Brand System.dc.html` and nowhere in the app. Do not implement as product tokens.

| Hex | Where | Use |
|---|---|---|
| `#B0413E` | `:75`, `:79`, `:83`, `:87`, `:91`, `:185`, `:213` | "DON'T" label, destructive-button text on the specimen |
| `#55555A` | `:69`, `:158`, `:170`, `:243` | Specimen body copy |
| `#131316` | `:55`, `:57`, `:130` | Specimen dark-panel ground (the app uses `#161614`/`#1E1E1C`) |
| `#F2F2EE` / `#9A9A96` | `:58`, `:64`, `:66`, `:132` | Specimen dark ink / dark muted |
| `#ECECE6` | `:57`, `:61–63`; `brand/logo-mark-dark.svg` | Dark-mode logo stroke |
| `#FFDE4D` / `#2A5CC8` | `:78` | The "official yellow/blue" anti-pattern tile |
| `#8B5CF6` | `:82` | The gradient anti-pattern tile |
| `#C9C9C4` | `:232`, `:236` | Schematic dashed placeholder (= app `--line3`) |

---

## 3. Typography

### 3.1 Families

Three families. Google Fonts, `display=swap`, with `preconnect` to `fonts.googleapis.com` and `fonts.gstatic.com` (`Cardstock Home.dc.html:12–14`).

| Family | Loaded weights | Role | Fallback stack |
|---|---|---|---|
| **Inter** | 400, 500, 600, 700 (app) · 400–800 (brand page, landing) | UI, prose, labels, buttons | `'Inter', system-ui, sans-serif` (`Cardstock Home.dc.html:18`) |
| **Inter Tight** | 600, 700 | Section headings on app pages (47 usages across the prototypes) | `'Inter Tight', sans-serif` |
| **JetBrains Mono** | 400, 500, 600 (app) · 400, 500, 700 (brand page) | **Every number**, ticker, timestamp, kbd hint, eyebrow, badge | `'JetBrains Mono', monospace` |

App font link (`Cardstock Home.dc.html:14`):
`Inter:wght@400;500;600;700` + `Inter+Tight:wght@600;700` + `JetBrains+Mono:wght@400;500;600`. `Cardstock Charts.dc.html` and `Cardstock Screener.dc.html` additionally load mono 700.
Brand-specimen link (`Cardstock Brand System.dc.html:13`): `Inter:wght@400;500;600;700;800` + `JetBrains+Mono:wght@400;500;700` — **no Inter Tight**.

`DESIGN_NOTES.md:24` records that Space Grotesk was tried and rejected.

### 3.2 The mono-numbers rule

**Every number is JetBrains Mono — including numbers inside running prose.** The brand page states the division of labour verbatim: *"Inter talks, JetBrains Mono counts"* (`Cardstock Brand System.dc.html:154`), and enumerates the scope: *"every number, ticker, timestamp, and `/` kbd hint. Tabular by nature — columns align"* (`:162`). `DESIGN_NOTES.md:24` and `:87` lock it: *"JetBrains Mono for ALL numbers."*

Practically, in Blazor: a numeric value in a sentence gets its own `<span class="num">`. The prototypes do exactly this — e.g. the specimen's own inline `/` key hint is a mono `<span>` nested inside an Inter paragraph (`Cardstock Brand System.dc.html:162`, `:220`), and the version stamp `v1.0 · Aug 2026` is a mono block inside an Inter header (`:33`).

Mono also carries: eyebrows/section numbers (`:37`, `:99`, `:152`, `:175`, `:208`, `:227`), all badges and chips (`:28`, `:197–200`), all column captions, and the `CDSTK` ticker chip.

### 3.3 Scale

**Base is 15px and the whole app is scaled +15%** — `HANDOFF.md:109` (*"Typography +15% throughout, base 15px"*), `DESIGN_NOTES.md:24`, `:87`. The 15px base is set on each app page's root screen div, not on `body`: `Cardstock Home.dc.html:37` — `<div data-screen-label="Home" style="…font-size: 15px;">`. The +15% is historical (a one-time uplift already baked into every literal), not a runtime multiplier — implement the literals below, do not re-scale them.

App scale, by frequency across all prototypes:

| px | Weight(s) | Typical use |
|---|---|---|
| 9 / 9.5 / 10 / 10.5 | 500–600 mono | Micro-badges, sparkline captions |
| 11 / 11.5 | 500–600 mono | Chips, pills, table micro-labels |
| 12 / 12.5 | 500–600 | Captions, helper text, secondary labels |
| 13 / 13.5 | 500–600 | Dense table cells, buttons, chip text |
| 14 / 14.5 | 500–600 | Standard controls, secondary body |
| **15** | 400–600 | **Base — body, nav links, table body** |
| 15.5 / 16 | 600 | Emphasised row values |
| 17 / 17.5 / 18 / 18.5 | 600–700 (Inter Tight) | Panel/section headings |
| 19 / 19.5 | 700 | Page sub-heads |
| 21 / 22 | 700 | Page titles |
| 24–30 | 700 | Hero numbers, logo wordmark (30px on the specimen) |
| 48 / 52 | 800 | Marketing display only |

Marketing scale — **landing pages only** (`Cardstock Brand System.dc.html:166–170`):

| Role | Size / weight | Detail |
|---|---|---|
| Display | 48 / 800 | `letter-spacing -0.03em`, `line-height 1.05` |
| Heading | 28 / 700 | `letter-spacing -0.02em` |
| Eyebrow | 12 / 500 mono | `letter-spacing 0.08em`, uppercase, indigo `#4A63D0` |
| Body | 16 / 400 | `line-height 1.6`, max-width ~560px, color `#55555A` |

The specimen states the boundary explicitly: *"landing prose only; the app keeps its 13–15px density"* (`:170`).

### 3.4 Weight and tracking

Observed weights across the prototypes: **600** (266×) · **500** (121×) · **700** (115×) · **650** (12×) · **400** (7×) · **800** (5×). 600 is the workhorse; 400 is rare because most app text is a label.

Tracking rule (`Cardstock Brand System.dc.html:158`): *"Tight tracking (-0.02 to -0.03em) above 20 px, normal below."* Observed values: `-0.03em` (28×, wordmark + display), `-0.02em` (10×, headings), `-0.01em` (5×). Positive tracking is reserved for uppercase mono eyebrows: `0.06em` (38×), `0.05em` (20×), `0.08em` (12×), `0.07em` (5×), `0.04em` (1×).

Line-height: `1.5` for helper prose, `1.6` for marketing body, `1.05` for display (`Cardstock Brand System.dc.html:158`, `:167`, `:170`).

Wordmark lockups:

| Context | Spec | Cite |
|---|---|---|
| App nav | mark 24px + wordmark Inter 700 / 18px / `-0.03em`, gap 10px | `Cardstock Home.dc.html:41–42`; `Cardstock Brand System.dc.html:53` |
| Specimen header | mark 30px + wordmark 24px / 700 / `-0.03em`, gap 11px | `Cardstock Brand System.dc.html:24–26` |
| Specimen hero | mark 40px + wordmark 30px / 700 / `-0.03em`, gap 13px | `:42–44` |

---

## 4. Semantic rules

### 4.1 The rule

**Color never carries meaning alone. Every state pairs a hue with a glyph.**

- `HANDOFF.md:150`: *"Color never carries meaning alone. Every state pairs a hue with a glyph (▲ ▼ – ● ◌ ◆). Colorblind mode swaps hue only; glyphs, labels, and grammar are identical."*
- `DISPLAY_VOCABULARY.md:2`: *"Icon always accompanies color (▲ ▼ – ● ◌ ◆); never color alone."*
- In the product UI itself, `Cardstock Profile.dc.html:94`: *"Swaps green→blue and red→orange everywhere state color appears. Glyphs ▲ ▼ – ◌ never change."*

### 4.2 Glyph inventory (complete)

| Glyph | Name | Meaning | Paired hue (light std → light CVD) | Cite |
|---|---|---|---|---|
| `▲` | up triangle | Bullish hit / positive direction / screen ENTER on a bullish thesis | `--pos` `#157A50` → `#0B69A8` | `DISPLAY_VOCABULARY.md:9`, `:47`; `Cardstock Profile.dc.html:101` |
| `▼` | down triangle | Bearish hit / negative direction / adverse exit / drawdown state | `--neg` `#C13A3A` → `#CC5F00` | `DISPLAY_VOCABULARY.md:9`, `:48`; `Cardstock Profile.dc.html:102` |
| `–` | en dash (amber) | **Caution** — notable but directionless. Complete amber band list: RSI 70–80 · RS decile exit (80–89th within 3 mo of ≥90) · Pop Δ 60d ≥ +2%. No other signal has one. | `--warn` `#8F6614` (**unchanged in CVD**) | `DISPLAY_VOCABULARY.md:49`, `:53`; `Cardstock Profile.dc.html:103` |
| `–` | en dash (grey) | **Quiet** — signal is tracked but between bands; nothing to report | `--mut2` `#6B6B66` on `--mutbg` | `DISPLAY_VOCABULARY.md:50`; `Cardstock Profile.dc.html:104` |
| `◌` | dotted circle | **Pending / insufficient** — not yet computable; label carries an unlock ETA (`— 12d` under 60 days, `— Mar '27` beyond). Also: current month provisional on sparklines. | `--mut2` grey | `DISPLAY_VOCABULARY.md:51`, `:55`, `:71`; `Cardstock Profile.dc.html:105` |
| `●` | filled circle | **Liquidity / descriptive state**, never directional — volume, Amihud, dispersion, cross-market gap | grey (`--mut2`) | `DISPLAY_VOCABULARY.md:34–38`, `:54` |
| `◆` | diamond | **Composite membership** (card matches a preset/user screen) and **sufficiency UNLOCK** feed rows | thesis-colored: `--pos` bullish screen, `--neg` avoid screen; amber for unlock rows | `DISPLAY_VOCABULARY.md:43`, `:58` |
| `✓` | check | Affirmative badge state (`WATCHING ✓`) | `--acc` indigo | `Cardstock Brand System.dc.html:200` |
| `✕` | cross | "Don't" marker — **specimen page only**, not product UI | `#B0413E` | `Cardstock Brand System.dc.html:79`, `:83`, `:87`, `:91` |

Supporting rules:
- The five tracked-pill states are exhaustive: hit-bullish · hit-bearish · caution · quiet · pending. *"A tracked signal ALWAYS renders exactly one pill, in exactly one of five states; no other pill forms exist"* (`DISPLAY_VOCABULARY.md:45–51`).
- Glance rule: **colored = hit, grey = nothing to report** (`DISPLAY_VOCABULARY.md:9`).
- Direction hues are reserved. The brand may not borrow them: *"Direction chips (▲ ▼) keep the app's green/red — the brand never borrows them"* (`Cardstock Brand System.dc.html:202`), and no chart series may use them (`:101`, `:138`).
- The logo sparkline is likewise fenced off: *"never recolor it — its teal is reserved for the logo; red/green mean market direction; UI chrome is indigo"* (`Cardstock Brand System.dc.html:71`).

### 4.3 Confirmed from code: colorblind mode swaps hue only

Verified four ways, all in Tier 1 code:

1. **The CSS override sets only state hues.** Across all 9 `:root[data-cvd="1"]` blocks and all 8 `:root[data-theme="dark"][data-cvd="1"]` blocks, the *only* properties ever declared are `--pos`, `--pos2`, `--neg`, `--neg2`, `--neg3` and their `rgba` tints. No chrome, type, radius, spacing or content token appears under any `[data-cvd]` selector on any page (`Cardstock Home.dc.html:27`, `:31`; `Cardstock Screener.dc.html:24`; `Cardstock Charts.dc.html:23`; `Cardstock Card.dc.html:25`).
2. **`--warn` is outside the swap.** It is declared in the `[data-theme]` block, never in a `[data-cvd]` block — so amber `#8F6614` / `#D6A54A` is identical in all four modes (`Cardstock Home.dc.html:29`; `Cardstock Profile.dc.html:221` sets `--warn` from `dark` alone, ignoring `cvd`).
3. **`PAL`'s CVD branches change only `pos/pos2/neg/neg2/neg3/posBg/negBg`.** The chrome object `ch` is selected by `d` (dark) alone — `cvd` is not consulted (`Cardstock Home.dc.html:328–329`). Same in `Cardstock Profile.dc.html:215–217`.
4. **Glyphs are literal text in the markup, outside any conditional.** The Profile live-preview strip hard-codes `▲ RS 94th`, `▼ EMA 3/9`, `– RSI 71`, `– MACD –`, `◌ Churn — 12d` and only the *color* comes from a token (`Cardstock Profile.dc.html:101–105`). Toggling `cvd` cannot reach them.

The only non-hue CVD behaviour found anywhere: in Charts, the MACD signal line becomes **dashed** when CVD is on — `dash: localStorage.getItem('cardstock-cvd') === '1' ? '4 3' : 'none'` (`Cardstock Charts.dc.html:791`). That is a *redundant encoding added* for CVD users, not a grammar change; it never removes or alters a glyph or label.

---

## 5. Spacing, borders, density

**There is no numeric spacing scale token set.** No `--space-*`, `--radius-*`, `--size-*` custom property exists in any prototype or in `brand/brand-tokens.css`. All spacing is literal px in inline styles. The values below are the de-facto scale, by observed frequency — a Blazor implementation should promote them to tokens with these exact numbers.

### 5.1 Spacing

| Step | px | Frequency (`gap:`) |
|---|---|---|
| micro | 2, 3, 4, 5 | 4px 44× · 5px 12× · 2px 11× · 3px 4× |
| tight | 6, 8, 10 | 8px 61× · 10px 59× · 6px 27× |
| standard | 12, 14, 16 | 12px 42× · 14px 24× · 16px 17× |
| loose | 18, 20, 22, 24 | 24px 19× · 20px 18× · 18px 8× · 22px 7× |
| section | 28, 48 | 28px 6× · 48px 4× |

Specimen section rhythm: `padding: 48px 0` between numbered sections, each closed by `border-bottom: 1px solid #E4E4E0` (`Cardstock Brand System.dc.html:36`, `:98`, `:151`, `:174`, `:207`, `:226`).

### 5.2 Radii

| px | Frequency | Use |
|---|---|---|
| 2 | 21× | Focus-outline rounding (`Cardstock Home.dc.html:21`) |
| 3 | 18× | Micro |
| 4 | 52× | Chips, pills, kbd keys |
| 5 | 32× | Badges (`Cardstock Brand System.dc.html:197–200`) |
| **6** | **113×** | **Default control radius** — inputs, small panels, segmented buttons |
| 7 | 8× | Specimen buttons/inputs (`:182–185`, `:191`) |
| **8** | **89×** | **Default card/panel radius** |
| 9, 10, 12 | 1× / 44× / 7× | 10px = pill toggle track (`Cardstock Profile.dc.html:96`) |
| 99 | 8× | Full pill |
| 50% | — | Avatar circle (`Cardstock Home.dc.html:53`) |

`BRAND_BRIEF.md:28` describes "6–8px radii" — the code confirms exactly that as the dominant pair.

### 5.3 Borders and elevation

- **Hairlines are 1px, always.** `1px solid var(--line, #E4E4E0)` is the universal rule. No prototype uses a 2px border on chrome.
- The active nav tab is a **2px** bottom border in `--acc`, with `margin-bottom: -1px` so it overlaps the nav's own hairline (`Cardstock Home.dc.html:45`).
- Logo strokes are `stroke-width="2"` at a 32-unit viewBox (`Cardstock Home.dc.html:41`).
- **There is no named elevation scale**, but shadows do exist — 46 inline `box-shadow` declarations, all on floating layers only (menus, modals, tooltips, drag rows, peek panels). Never on a resting card. The de-facto tiers:

| Tier | Value | Count | Use |
|---|---|---|---|
| tooltip / popover | `0 3px 10px rgba(20,19,26,0.08)` · `0 4px 12px rgba(20,19,26,0.08)` | 4 | Hover tooltips, chart crosshair boxes |
| dropdown / menu | `0 6px 20px rgba(20,19,26,0.10)` · `0 8px 24px rgba(20,19,26,0.12–0.13)` | 13 | Row `⋯` menus, selects, peek panel |
| modal (light) | `0 24px 48px rgba(28,28,30,0.25)` · `0 18px 36px rgba(28,28,30,0.22)` · `0 12px 24px rgba(28,28,30,0.2)` | 13 | Dialogs, delete confirm |
| dark-surface | `0 16px 32px rgba(0,0,0,0.45)` · `0 14px 40px rgba(20,19,26,0.35)` · `0 12px 36px rgba(20,19,26,0.3)` | 5 | Marketing dark hero art |
| drag affordance | `inset 0 2px 0 <color>` | 1 | Drop-target indicator on watchlist rows |

Shadow tint is `rgba(20,19,26,α)` for in-app layers and `rgba(28,28,30,α)` for modals — two near-identical inks that were never unified. Pick one when tokenising.
- Focus, two mechanisms:
  - App: `*:focus-visible { outline: 2px solid var(--acc, #4A63D0); outline-offset: 1px; border-radius: 2px; }` (`Cardstock Home.dc.html:21`).
  - Brand/inputs: `box-shadow: 0 0 0 3px rgba(74,99,208,0.22)` light, `rgba(140,155,242,0.25)` dark — `--focus-ring` (`brand/brand-tokens.css:9`, `:25`; `Cardstock Brand System.dc.html:191–192`). The specimen's rule: *"Never remove focus without a ring."*

### 5.4 Density

| Fixed chrome | Height | Cite |
|---|---|---|
| Nav bar | 48px, sticky, `z-index: 20` | `Cardstock Home.dc.html:39` |
| Ticker strip | 36px | `Cardstock Home.dc.html:56` |
| Account avatar | 28×28 circle | `Cardstock Home.dc.html:53` |
| Segmented button | 30px | `Cardstock Profile.dc.html:85–86` |
| Toggle switch | 36×20 track, 16px knob, `translateX(16px)` on | `Cardstock Profile.dc.html:96–97` |

User-selectable density modes (`DISPLAY_VOCABULARY.md:184–189`):

| Surface | Modes | Meaning |
|---|---|---|
| Screener, Set, Character | terminal / binder | terminal = more rows, tighter type, every metric column; binder = fewer rows with card art |
| Binder holdings | table / gallery | gallery renders the collection as card art |

Density persists per device via `localStorage`, like theme (`DISPLAY_VOCABULARY.md:189`; `HANDOFF.md:156`).

Motion: `@media (prefers-reduced-motion: reduce) { * { animation-duration: 0.01ms !important; } }` (`Cardstock Home.dc.html:25`). Named keyframes: `peekIn`, `ticker` (`:23–24`). Toggle knob transition `0.15s` (`Cardstock Profile.dc.html:97`).

---

## 6. Logo and iconography

### 6.1 The mark

Two cards fanned, the front one charting. *"The mark is stroke-drawn at the app's hairline weight; the sparkline is the only colored element"* (`Cardstock Brand System.dc.html:39`).

Geometry — identical in every variant, `viewBox="0 0 32 32"`, `fill="none"` (`brand/logo-mark.svg`):

| Element | Attributes |
|---|---|
| Back card | `<rect x=5.5 y=5.5 w=15 h=21 rx=2.5 stroke-width=2 transform="rotate(-12 13 16)">` |
| Front card | `<rect x=12 y=5.5 w=15 h=21 rx=2.5 stroke-width=2>` — filled with the page ground |
| Sparkline | `<polyline points="15,21.5 17.5,17 19.5,18.5 23.5,12.5" stroke-width=2 linecap/linejoin=round>` |
| End dot | `<circle cx=23.5 cy=12.5 r=1.7>` |

### 6.2 Asset inventory (`brand/`)

| File | Dimensions | Colors | Notes |
|---|---|---|---|
| `logo-mark.svg` | 32 viewBox | stroke `#1C1C1E`, front-card fill `#FAFAF7`, sparkline + dot `#0E8A7B` | Light variant |
| `logo-mark-dark.svg` | 32 viewBox | stroke `#ECECE6`, front-card fill `#131316`, sparkline + dot `#3FBFAD` | Dark variant |
| `favicon.svg` | 32 viewBox | tile `#0E8A7B` `rx=7`; single card `x=9.5 y=6.5 w=13 h=19 rx=2` + sparkline `12.5,20.5 15.5,15.5 17.5,17.5 20,11.5` + dot `r=1.6`, all `#FFFFFF` | **Filled tile — different geometry from the mark** (one card, not two) |
| `favicon-16.png` | 16×16 | — | |
| `favicon-32.png` | 32×32 | — | |
| `apple-touch-icon.png` | 180×180 | — | |
| `og-image.png` | 1200×630 | — | Social card (`Cardstock Brand System.dc.html:248`) |
| `brand-tokens.css` | — | — | §2.5 |

The specimen's own file list (`Cardstock Brand System.dc.html:245`) names exactly these eight. **There is no wordmark SVG** — the wordmark is live text (Inter 700, `-0.03em`), never an image.

The app links only `favicon.svg`: `<link rel="icon" href="./brand/favicon.svg">` (`Cardstock Home.dc.html:11`). The PNG favicons, apple-touch-icon and OG image are shipped but unreferenced by any prototype — a Blazor host must wire them up itself.

### 6.3 In-app rendering: theme-aware inline SVG

The nav mark is **inlined, not `<img>`**, so it inherits theme tokens with no asset swap (`Cardstock Home.dc.html:41`):

- card strokes → `stroke: var(--ink, #1C1C1E)`
- front-card fill → `fill: var(--card, #FFFFFF)`
- sparkline + dot → `stroke`/`fill: var(--logoTeal, #0E8A7B)`

`--logoTeal` flips to `#3FBFAD` under `:root[data-theme="dark"]`. It is a dedicated one-token rule on **11** pages, including Profile and Account which have no other dark chrome (`Cardstock Home.dc.html:32`; `Cardstock Profile.dc.html:23`; `Cardstock Account.dc.html:21`). The 12th themed page, `Cardstock Legal.dc.html`, folds `--logoTeal: #3FBFAD` into its single main dark block instead (`:21`) — same result, different shape. Note that the in-app dark mark keeps `--ink` `#E9E9E5` and `--card` `#1E1E1C`, while the shipped `logo-mark-dark.svg` uses `#ECECE6` / `#131316`. Use the inline form in Blazor; the standalone SVGs are for external contexts.

The specimen confirms one geometry only: *"dark · same geometry, no separate dark mark"* (`Cardstock Brand System.dc.html:66`) and *"strokes flip to #ECECE6 · teal lifts to #3FBFAD"* (`:64`).

Since 2026-08-10 the nav lockup is an `<a>` to Home on all nav pages, with `color: inherit; text-decoration: none` inline so the global `a` rule cannot tint it (`Cardstock Home.dc.html:41`; rationale `DESIGN_NOTES.md:138`).

### 6.4 Sizes and usage rules

| Rule | Value | Cite |
|---|---|---|
| Mark sizes | 32 / 24 / 20 px; favicon 16px filled tile | `Cardstock Brand System.dc.html:51` |
| Nav lockup | mark 24px + wordmark 18px, gap 10px | `:53`; matches `Cardstock Home.dc.html:41–42` |
| Clearspace | one card-width (½ mark width) on all sides | `:70` |
| Minimum size | never below 16px; **below 20px use the filled favicon tile** | `:70` |
| Sparkline direction | fixed geometry, not data — never redraw falling | `:71` |
| Sparkline color | never recolor; teal is logo-reserved | `:71` |

The four "DON'T" tiles (`:76–93`), each a rendered anti-example: ✕ official yellow/blue vibes · ✕ gradients on the mark · ✕ falling or market-red line · ✕ on imagery — use the filled tile.
`BRAND_BRIEF.md:13` gives the reason for the first: fan-made, not affiliated with Nintendo/TPCi, *"no Pokéballs, no official yellow/blue logotype vibes."* The specimen header carries the disclaimer *"fan-made · not affiliated with Nintendo/TPCi"* (`:33`).

### 6.5 Empty-state illustration style — "schematics, not illustrations"

`Cardstock Brand System.dc.html:229`: *"Not illustrations — schematics. 2px ink strokes, dashed placeholders, one indigo accent, a mono caption. Drawn like chart annotations, never scenes or characters."*

Two worked examples (`:232`, `:236`), both `viewBox="0 0 96 64"`, rendered at 120px wide, with a 12px mono `--mut2` caption:

| Example | Construction |
|---|---|
| empty binder | dashed card slot `stroke="#C9C9C4" stroke-width=2 stroke-dasharray="5 4"` + a `#4A63D0` plus sign |
| no data yet | baseline `#C9C9C4` + known polyline `#1C1C1E` + dashed future `#4A63D0 stroke-dasharray="4 4"` |

### 6.6 Voice (governs microcopy, included because it is part of the brand contract)

*"Matter-of-fact and openly nerdy. Precise numbers over adjectives. Jargon is welcome, explained once. Keyboard shortcuts are copy. No exclamation marks, no emoji; dry humor lives in empty states only."* (`Cardstock Brand System.dc.html:210`)

| DO | DON'T |
|---|---|
| No positions. Add your first card to start tracking cost basis. | Your collection is waiting for you! 🎉 |
| 3 sales in 30d — LOW CONFIDENCE. Treat as directional. | Not enough data, check back soon! |
| Price feed unavailable. Last good tick 14:32 UTC. | Oops! Something went wrong. |
| Press `/` to search. Most pages are two keys away. | Welcome aboard! Let's take a quick tour. |

(`Cardstock Brand System.dc.html:214–221`.)

---

## 7. Theming mechanics

### 7.1 State model

Two independent, orthogonal booleans on `<html>`:

| Attribute | Set when | Absent means |
|---|---|---|
| `data-theme="dark"` | user chose dark | **light** (there is no `data-theme="light"`) |
| `data-cvd="1"` | user enabled colorblind-safe palette | standard hues |

Four resulting modes. Light-standard is the zero-attribute default, which is why every inline `var()` fallback is a light-standard literal.

### 7.2 Persistence

`localStorage`, per device, **not per account** (`HANDOFF.md:156`; `DISPLAY_VOCABULARY.md:189`).

| Key | Values | Written at |
|---|---|---|
| `cardstock-theme` | `'dark'` \| `'light'` | `Cardstock Profile.dc.html:234–235` |
| `cardstock-cvd` | `'1'` \| `'0'` | `Cardstock Profile.dc.html:237` |

Writers (`Cardstock Profile.dc.html:234–237`):

```
setLight:  localStorage.setItem('cardstock-theme', 'light')
setDark:   localStorage.setItem('cardstock-theme', 'dark')
toggleCvd: localStorage.setItem('cardstock-cvd', cvd ? '0' : '1')
```

Note the read is strict-equality on the exact strings — `'light'` and `'0'` are stored but never tested for; any value other than `'dark'`/`'1'` falls through to the default. There is no `prefers-color-scheme` media query anywhere in the codebase: the OS preference is **not** consulted, and first visit is always light-standard.

### 7.3 The pre-paint script — exactly how the flash is avoided

One line, verbatim and byte-identical on **10** pages — `Cardstock Home.dc.html:35`, `Cardstock Screener.dc.html:32`, `Cardstock Charts.dc.html:31`, `Cardstock Binder.dc.html:35`, `Cardstock Card.dc.html:33`, `Cardstock Browse.dc.html:33`, `Cardstock Set.dc.html:33`, `Cardstock Character.dc.html:33`, `Cardstock About Data.dc.html:28`, `Cardstock Legal.dc.html:24` (verified: `grep -l` returns exactly these 10):

```html
<script>if(localStorage.getItem('cardstock-cvd')==='1')document.documentElement.setAttribute('data-cvd','1');if(localStorage.getItem('cardstock-theme')==='dark')document.documentElement.setAttribute('data-theme','dark');</script>
```

Why there is no flash — four properties, all load-bearing:

1. **It is in `<head>`, after the `<style>` that defines the `[data-theme]` / `[data-cvd]` rules** (style at `Cardstock Home.dc.html:16–33`, script at `:35`). The rules already exist in the CSSOM when the attribute lands, so the attribute takes effect on the *first* style resolution.
2. **It is synchronous and inline** — no `src`, no `async`, no `defer`, no `DOMContentLoaded`. HTML parsing blocks on it. It runs before `<body>` is parsed and therefore before any box is laid out.
3. **`localStorage` is a synchronous API.** The read completes inside the same parser pause; nothing is deferred to a later task.
4. **It mutates `document.documentElement`, which already exists** while `<head>` is being parsed. The attribute is set on the element the selectors are anchored to, so no re-parenting or re-render is needed.

Consequence: the browser's first paint is already dark and/or CVD. The failure mode this prevents is the "white flash" — first paint light, then a script at the end of `<body>` (or a Blazor circuit callback) repaints dark.

**Blazor implementation note.** In Blazor Server / Interactive Server the C# circuit is established *after* first paint, so theme must not be applied from `OnAfterRenderAsync` or the flash returns. Emit this exact `<script>` inline in the `<head>` of `App.razor` / `_Host.cshtml`, after the token stylesheet link and before `blazor.web.js`. It is also `<HeadContent>`-hostile — it must be in the static host page, not injected per-component.

### 7.4 The second mechanism — component-scoped theming on Profile and Account

`Cardstock Profile.dc.html` and `Cardstock Account.dc.html` **do not carry the pre-paint script** and **do not declare the `:root[data-theme="dark"]` chrome block.** Verified: the only `[data-theme]` selector on either page is the one-token `--logoTeal` rule (`Cardstock Profile.dc.html:23`; `Cardstock Account.dc.html:21`).

Instead they compute the full token set in the component and apply it as an inline style object on a `display: contents` wrapper (`Cardstock Profile.dc.html:214–222`):

```
vars(dark, cvd) → Object.assign({ display: 'contents', colorScheme: dark ? 'dark' : 'light' }, ch, s, w)
```

with `ch` = chrome (13 tokens, chosen by `dark` only, `:216–217`), `s` = state hues + tints (chosen by `dark` × `cvd`, `:218–220`), `w` = warn (chosen by `dark` only, `:221`). Initial state is hydrated in `componentDidMount` from the same two `localStorage` keys (`:208–212`).

This exists so the Appearance panel can preview a theme change instantly without a reload. It is a **prototype artifact, not a second design system** — the token values are identical to the global ones. In Blazor, use the global attribute mechanism everywhere and re-render on toggle; a `display: contents` wrapper is not needed.

Two token sets differ slightly in the component path — `--accH: '#8CA4F0'` and `--btnH: '#AAB6F6'` in dark (`Cardstock Profile.dc.html:216`) versus `--accH: '#AAB6F6'` globally (`Cardstock Home.dc.html:29`). See §9.

### 7.5 Script-computed colors

Anything drawn in JavaScript (chart strokes, sparkline fills, SVG markers) reads `PAL`, not CSS. `PAL` is an IIFE evaluated **once at class-definition time** from `localStorage` (`Cardstock Home.dc.html:323`), so script-drawn color does not react to a live toggle — it is correct on load only. A Blazor implementation should read the CSS custom properties (or pass the resolved palette from the server) rather than reproduce this snapshot behaviour.

The Appearance panel's own copy states the intent: *"Applies across every Cardstock page"* and *"applies immediately and is remembered on this device"* (`Cardstock Profile.dc.html:83`, `:85`).

---

## 8. Known issues

### 8.1 The recorded, deferred failure

**Token: `--muted` = `#8A8A86` in `brand/brand-tokens.css:18`.**

`DESIGN_NOTES.md:26` (Tier 2) records it: *"muted #5B5B57/#8A8A86 … Known issue (deferred): #8A8A86 small text fails WCAG AA (3.2:1) — user postponed contrast pass."* `DESIGN_NOTES.md:166` lists it as open todo #4.

Confirmed by computation against the tokens' own backgrounds:

| Foreground | Background | Ratio | AA normal (4.5) | AA large (3.0) |
|---|---|---|---|---|
| `#8A8A86` | `--bg` `#FAFAF7` | **3.31:1** | FAIL | pass |
| `#8A8A86` | `--card` `#FFFFFF` | **3.47:1** | FAIL | pass |

The doc's "3.2:1" is close but not exact — 3.31:1 on the brand background, 3.47:1 on white. The failure is real.

**But the scope in the doc is stale.** The contrast pass *was* run on 2026-08-10 (`DESIGN_NOTES.md:135–137`), moving the app's `--mut2` from `#8A8A86` to `#6B6B66`. Verified in code: **`#8A8A86` appears 0 times in all 13 app prototypes.** It survives only in `brand/brand-tokens.css:18`, in `Cardstock Brand System.dc.html` (39 occurrences — the specimen's own caption color) and in `Cardstock Landing.dc.html` (3). So:

- **Product UI: fixed.** `--mut2` `#6B6B66` scores 5.36:1 on card, 5.12:1 on bg, 4.81:1 on mutbg — passes AA everywhere.
- **Brand package + specimen page + landing: still failing.** Anyone who consumes `brand-tokens.css` literally inherits the bug.

The paired mitigation also landed: `--mut3` was moved from `#B0B0AB` to `#8F8F8A` and **demoted to non-text use only**, with every text usage promoted to `--mut2` (`DESIGN_NOTES.md:137`). Verified in code: `color: var(--mut3, …)` occurs **0 times** across all prototypes. `#8F8F8A` at 3.11–3.25:1 is legal for its remaining role (non-text graphics need 3:1) and illegal for text — the demotion is load-bearing, not cosmetic. `DESIGN_NOTES.md:137` states the resulting rule: *"small-text grey hierarchy is now 2 levels (mut, mut2), differentiate via size/weight instead."*

### 8.2 Full audit — every text token against every background it can sit on

WCAG 2.x AA: 4.5:1 normal text, 3:1 for ≥18.66px bold or ≥24px, 3:1 non-text.

**Light theme — additional failures found:**

| Token | Value | on `--card` `#FFFFFF` | on `--bg` `#FAFAF7` | on `--mutbg` `#F3F3EE` | Verdict |
|---|---|---|---|---|---|
| `--neg` (CVD) | `#CC5F00` | **4.04** | **3.86** | **3.62** | **FAIL** — used as text 6× (`Cardstock Profile.dc.html:102`, `Cardstock Home.dc.html`, `Cardstock Account.dc.html`). This is a **new failure introduced by colorblind mode**: standard `--neg` `#C13A3A` passes at 5.34, its CVD replacement does not. |
| `--neg2` (std) | `#D64545` | **4.38** | **4.19** | **3.93** | **FAIL (marginal)** — used as text 7× (`color: var(--neg2, #D64545)`) |
| `--neg2` (CVD) | `#D55E00` | **3.87** | **3.70** | **3.47** | **FAIL** — `Cardstock Screener.dc.html:25`, `Cardstock Card.dc.html:24` etc. |
| `--pos2` (std) | `#189E63` | **3.44** | **3.29** | **3.09** | **FAIL** — used as text 6× (`color: var(--pos2, #189E63)`) |
| `--pos2` (CVD) | `#0072B2` | 5.19 | 4.96 | 4.66 | pass |
| `--mut3` | `#8F8F8A` | 3.25 | 3.11 | 2.92 | pass **as graphic only**; 0 text usages — compliant by demotion |
| `--brand-foil` | `#9A7B2D` | **4.00** | **3.83** | **3.60** | **FAIL** as text — used as 11px badge text (`Cardstock Brand System.dc.html:198–199`, FOIL / LOW CONFIDENCE chips) |
| `--brand-logo-teal` | `#0E8A7B` | 4.25 | 4.07 | 3.82 | pass as graphic (logo-only rule); would fail as text — the "never text/UI chrome" rule (`brand/brand-tokens.css:7`) keeps it legal |
| `--muted` (brand) | `#8A8A86` | **3.47** | **3.31** | 3.11 | **FAIL** — §8.1 |

**Light theme — passing:**

| Token | Value | on card | on bg |
|---|---|---|---|
| `--ink` | `#1C1C1E` | 17.01 | 16.27 |
| `--mut` | `#5B5B57` | 6.82 | 6.52 |
| `--mut2` | `#6B6B66` | 5.36 | 5.12 |
| `--acc` | `#4A63D0` | 5.27 | 5.04 |
| `--accH` | `#3A4FB8` | 7.02 | 6.72 |
| `--warn` / `--warnInk` | `#8F6614` | 5.15 | 4.92 |
| `--pos` (std) | `#157A50` | 5.34 | 5.11 |
| `--pos` (CVD) | `#0B69A8` | 5.83 | 5.58 |
| `--neg` (std) | `#C13A3A` | 5.34 | 5.11 |
| `--neg3` (std) | `#A93838` | 6.34 | 6.07 |
| `--neg3` (CVD) | `#B34E00` | 5.24 | 5.01 |
| white on `--btn` `#4A63D0` | `#FFFFFF` | 5.27 | — |
| white on `--btnH` `#3A4FB8` | `#FFFFFF` | 7.02 | — |

**Dark theme — no failures.** Every text token clears AA on every dark background:

| Token | Value | on `--bg` `#161614` | on `--card` `#1E1E1C` | on `--mutbg` `#2A2A27` |
|---|---|---|---|---|
| `--ink` `#E9E9E5` | | 14.89 | 13.72 | 11.82 |
| `--mut` `#B4B4AE` | | 8.70 | 8.02 | 6.91 |
| `--mut2` `#A8A8A2` | | 7.58 | 6.99 | 6.02 |
| `--mut3` `#9A9A94` | | 6.41 | 5.90 | 5.09 |
| `--acc` `#8C9BF2` | | 6.98 | 6.43 | 5.54 |
| `--accH` `#AAB6F6` | | 9.26 | 8.53 | 7.36 |
| `--warn` `#D6A54A` | | 8.06 | 7.43 | 6.40 |
| `--pos` std `#4CC08D` | | 7.97 | 7.34 | 6.33 |
| `--neg` std `#E57B7B` | | 6.43 | 5.92 | 5.11 |
| `--pos` CVD `#58A9E6` | | 7.10 | 6.55 | 5.64 |
| `--neg` CVD `#F5924E` | | 7.86 | 7.24 | 6.24 |
| `--neg3` CVD `#E8874D` | | 6.91 | 6.36 | 5.49 |
| `--logoTeal` `#3FBFAD` | | 8.00 | 7.37 | 6.35 |

`DESIGN_NOTES.md:136` claims *"dark theme already passed"* — **confirmed**, the lowest dark text ratio found is 4.92 (`--mut3` on `--accBg`), and 5.09 on real backgrounds.

### 8.3 Summary of open accessibility debt

| # | Issue | Token / value | Ratio | Status |
|---|---|---|---|---|
| 1 | Documented, deferred | `--muted` `#8A8A86` in `brand/brand-tokens.css:18` | 3.31 / 3.47 | Fixed in app (`--mut2` → `#6B6B66`), **not fixed in the brand package** |
| 2 | **Not documented** | `--neg` CVD `#CC5F00` used as text | 3.86 / 4.04 | Open — colorblind mode makes light-theme negative text fail |
| 3 | **Not documented** | `--neg2` std `#D64545` used as text | 4.19 / 4.38 | Open (marginal) |
| 4 | **Not documented** | `--neg2` CVD `#D55E00` used as text | 3.70 / 3.87 | Open |
| 5 | **Not documented** | `--pos2` std `#189E63` used as text | 3.29 / 3.44 | Open |
| 6 | **Not documented** | `--brand-foil` `#9A7B2D` as badge text | 3.83 / 4.00 | Open |
| 7 | Compliant by rule, fragile | `--mut3` `#8F8F8A`, `--logoTeal` `#0E8A7B` | 3.1–4.3 | Legal only while the "no text" rule holds |

Items 2–6 are the reason `--pos`/`--neg` (text) and `--pos2`/`--neg2`/`--neg3` (graphic) are split tokens. The split is not consistently honoured: `--pos2` and `--neg2` are used as `color:` in 13 places. Fixing that (route all text through `--pos`/`--neg`) resolves 3, 4 and 5 without changing any hex.

---

## 9. Contradictions found

| # | Claim | Source doc:line | What the code actually does |
|---|---|---|---|
| 1 | Muted grey is `#8A8A86` and the contrast pass is deferred | `DESIGN_NOTES.md:26`, `:166` | The pass shipped on 2026-08-10. `#8A8A86` appears **0 times** in the 13 app prototypes; `--mut2` is `#6B6B66` (`Cardstock Home.dc.html:329`). The stale value survives only in `brand/brand-tokens.css:18`, the specimen page and `Cardstock Landing.dc.html`. |
| 2 | `--muted` is `#8A8A86` | `brand/brand-tokens.css:18` (Tier 1 file, but the *brand* file) | The app's muted tokens are `--mut` `#5B5B57`, `--mut2` `#6B6B66`, `--mut3` `#8F8F8A` (`Cardstock Home.dc.html:329`). The brand file's neutral block was never re-synced after the contrast pass. It is labelled *"existing app neutrals (reference)"* (`:16`) — the reference is wrong. |
| 3 | `mut2 #8A8A86→#A8A8A2` in the light→dark map | `DISPLAY_VOCABULARY.md:78` | Light `--mut2` is `#6B6B66`, not `#8A8A86`. The dark value `#A8A8A2` is correct. |
| 4 | Accent is `#3B5BD6`, hover `#2E49B8` | `DESIGN_NOTES.md:26` | Superseded by the brand pass: `--acc` `#4A63D0`, `--accH` `#3A4FB8` (`Cardstock Home.dc.html:329`). `DESIGN_NOTES.md:136` documents the swap but line 26 was never updated — the same file contradicts itself. |
| 5 | `accent #3B5BD6→#7290EA · button #3B5BD6→#4A66D8` (dark) | `DISPLAY_VOCABULARY.md:78` | Dark `--acc` is `#8C9BF2`, dark `--btn` is `#4A63D0` (`Cardstock Home.dc.html:29`). Pre-brand-pass values. |
| 6 | "gain #189E63, loss #D64545" as the state colors | `DESIGN_NOTES.md:26` | Those are `--pos2`/`--neg2`, the **graphic** hues. The text hues are `--pos` `#157A50` / `--neg` `#C13A3A` (`Cardstock Home.dc.html:327`). |
| 7 | Six chart-series colors ship with the brand and the app owns them | `Cardstock Brand System.dc.html:101`, `:251` | **Not implemented.** No `--series-*` token is referenced by any app prototype; Charts keeps its own per-grade `TIER_COLORS`. `DESIGN_NOTES.md:133` admits it: *"NOT done: chart series recolor to brand 6-series palette."* |
| 8 | Foil `#9A7B2D` is a live support color for grade premiums / PSA 10 | `Cardstock Brand System.dc.html:111–114`; `brand/brand-tokens.css:8` | **Unused in the app.** `DESIGN_NOTES.md:133`: *"Foil #9A7B2D unused in app so far (candidate: LOW CONFIDENCE badges — currently warn gold)."* LOW CONFIDENCE renders in `--warn` `#8F6614`. |
| 9 | Version is "v1.0 · Aug 2026" | `Cardstock Brand System.dc.html:33`, `:251` | `brand/brand-tokens.css:1` says **v1.1** (Aug 2026). The specimen page was not re-stamped when the tokens moved to 1.1 (the 1.1 change is teal → logo-only). |
| 10 | Focus is a 3px ring at 22% indigo | `Cardstock Brand System.dc.html:192`; `brand/brand-tokens.css:9` | The app uses `outline: 2px solid var(--acc)` with `outline-offset: 1px` (`Cardstock Home.dc.html:21`). The 3px `box-shadow` ring appears only on the specimen's demo input. Two different focus treatments exist; the app's wins. |
| 11 | Tokens ship as CSS custom properties with a `[data-theme="dark"]` block | `Cardstock Brand System.dc.html:245` | True of `brand-tokens.css`, but **no prototype links it.** The app declares dark/CVD in a per-page inline `<style>` and carries light values as `var()` fallbacks. There is no shared stylesheet at all. |
| 12 | Dark `--accH` is `#AAB6F6` | `Cardstock Home.dc.html:29`; `brand/brand-tokens.css:27` | `Cardstock Profile.dc.html:216` sets dark `--accH` to **`#8CA4F0`** and dark `--btnH` to `#AAB6F6`. One page disagrees with the other twelve. Treat `#AAB6F6` as correct (12 pages + brand file). |
| 13 | Light `--tooltipBg` is `rgba(255,255,255,0.95)` | inline fallbacks, 3 occurrences | `Cardstock Charts.dc.html:251` uses `rgba(255,255,255,0.96)`. Single-site drift; use `0.95`. |
| 14 | Light `--line` is `#E4E4E0` | 199 occurrences | 1 occurrence falls back to `#D9D9D4` (which is `--line2`) — `Cardstock Profile.dc.html:236`, the CVD toggle's off-state track. It is the only exception. |
| 15 | The pre-paint script is on every app page | `HANDOFF.md:88` (*"Chrome shared by every app page … pre-paint script reading localStorage"*) | **10** pages carry it. `Cardstock Profile.dc.html` and `Cardstock Account.dc.html` do not — they theme via an in-component `display: contents` wrapper (`Cardstock Profile.dc.html:214–222`) and would flash on load. |
| 16 | Colorblind glyph set is `▲▼–◌◆` | `DISPLAY_VOCABULARY.md:76` | Incomplete — `●` is a sixth glyph with its own meaning (liquidity/descriptive state, `DISPLAY_VOCABULARY.md:34–38`, `:54`). `HANDOFF.md:150` lists all six correctly: `▲ ▼ – ● ◌ ◆`. The Profile UI copy lists only four (`Cardstock Profile.dc.html:94`) because `●` and `◆` do not appear in the preview strip. |
| 17 | Colorblind mode swaps hue only | `HANDOFF.md:150`; `DISPLAY_VOCABULARY.md:76`; `Cardstock Profile.dc.html:96` | **Essentially true, with one addition:** Charts dashes the MACD signal line when CVD is on (`Cardstock Charts.dc.html:791`). That adds a redundant encoding; it does not alter any glyph, label or grammar. Worth noting because a literal reading of "hue only" would omit it. |
| 18 | Theme follows the OS | (not claimed, but the natural assumption) | **No `prefers-color-scheme` query exists anywhere.** First visit is always light-standard regardless of OS setting. Only `prefers-reduced-motion` is honoured (`Cardstock Home.dc.html:25`). |
