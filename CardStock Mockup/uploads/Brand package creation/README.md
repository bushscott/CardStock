# Handoff: Cardstock — Brand System & Landing Page

## Overview
Cardstock is a fan-made Pokémon TCG aftermarket data app ("the terminal for the Pokémon card aftermarket"): graded-card price history, a screener across every printing, and a binder tracked like a portfolio. This bundle contains the brand package (logo, color, type, components, voice) and the marketing landing page.

## About the Design Files
The files in this bundle are **design references created in HTML** — prototypes showing intended look and behavior, not production code to copy directly. The task is to **recreate these designs in the target codebase's existing environment** (React, Vue, Next.js, etc.) using its established patterns and libraries. If no environment exists yet, choose the most appropriate framework and implement there.

The `.dc.html` files are a component format: markup lives between `<x-dc>` tags, logic in a `class Component` script block, and `{{ name }}` holes are values returned from `renderVals()`. Read them as annotated markup — inline styles are literal CSS, and `style-hover="..."` is a hover state.

## Fidelity
**High-fidelity.** Final colors, typography, spacing, and interactions. Recreate pixel-perfectly using the codebase's libraries.

---

## Design Tokens

Canonical source: `brand-tokens.css` (in this bundle).

### Color — brand
| Token | Light | Dark | Use |
|---|---|---|---|
| `--brand-primary` | `#4A63D0` | `#8C9BF2` | Links, buttons, focus rings, active states, eyebrow labels |
| `--brand-primary-strong` | `#3A4FB8` | `#AAB6F6` | Hover on primary |
| `--brand-logo-teal` | `#0E8A7B` | `#3FBFAD` | **Logo mark + favicon ONLY** — never text or UI chrome |
| `--brand-foil` | `#9A7B2D` | `#C9A84C` | Grade premiums, LOW CONFIDENCE badges. Sparing |

**Critical rule:** the primary is indigo, not green. Green/red are reserved for market direction (▲ `#46C08A` / ▼ `#D0655E` on dark). Teal is logo-only. This separation is the whole reason for the indigo primary — do not reintroduce green or teal into interactive chrome.

### Color — neutrals (app-owned, unchanged)
`--ink: #1C1C1E` · `--bg: #F1F1EC` (landing page surface) · `--card: #FFFFFF` · `--line: #E4E4E0` · `--muted: #8A8A86` · `--hover: #F6F6F2`
Secondary text: `#55555A`. Dark surfaces: `#131316` (panels), `#1B1C1F` (cards on dark), `#0F0F11` (footer), `#2A2B2E` / `#232427` (borders on dark), `#F2F2EE` / `#B9B9B4` / `#8F8F8B` / `#71716D` (text on dark).

### Chart series (assign in order)
S1 `#4A63D0`/`#8C9BF2` · S2 `#C98A0D`/`#E0A93C` · S3 `#0E8A7B`/`#3FBFAD` · S4 `#B85C9E`/`#D98BC4` · S5 `#4C9FD8`/`#7BBCE8` · S6 `#71716D`/`#A5A5A0` (light/dark). Tuned from Okabe–Ito; no series borrows the ▲/▼ green–red.

### Typography
Two families, strict division of labor:
- **Inter** (400/500/600/650/700/800) — UI, headings, prose. Tracking `-0.02em` to `-0.03em` above 20px; normal below.
- **JetBrains Mono** (400/500/700) — every number, ticker, timestamp, ticker symbol, kbd hint, eyebrow label.

Marketing scale (landing only): Display 52/800/-0.03em/1.06 · H2 30/700/-0.02em · H3 17/650 · Body 17/1.6 and 14/1.55 · Eyebrow 12 mono, letter-spacing `0.08em`, uppercase, indigo. The app keeps its 13–15px density.

### Other
Radius: 5px (badges) · 7px (buttons, inputs) · 8–10px (cards) · 12px (dark panels) · 14–16px (large card slots).
Focus ring: `0 0 0 3px rgba(74, 99, 208, 0.22)` light, `rgba(140, 155, 242, 0.25)` dark. Never remove focus without a ring.
Shadows: `0 24px 48px rgba(28,28,30,0.25)` (floating dark panels) · `0 12px 24px rgba(28,28,30,0.2)` (scattered cards) · `0 16px 32px rgba(0,0,0,0.45)` (cards on dark).
Spacing: section padding `52px 40px 56px`; page max-width 1080px; grid gaps 20px.

---

## Logo

Two cards fanned, front card charting. Stroke-drawn at 2px on a 32×32 viewBox; the sparkline is the only colored element (teal).

- `logo-mark.svg` — light backgrounds (ink `#1C1C1E` strokes)
- `logo-mark-dark.svg` — dark backgrounds (`#ECECE6` strokes, `#3FBFAD` sparkline)
- `favicon.svg` + `favicon-16.png`, `favicon-32.png`, `apple-touch-icon.png` (180) — filled teal tile, white mark
- `og-image.png` — 1200×630 social card

**Rules.** Clearspace: one card-width (½ mark width) on all sides. Never below 16px; below 20px use the filled favicon tile. Nav lockup: mark 24px + wordmark 18px (Inter 700, `-0.03em`), gap 10px, optional `CDSTK` mono chip.

**Never:** official yellow/blue logotype vibes or Pokéball geometry · gradients or drop shadows on the mark · rotation beyond the built-in fan · the outline mark on card art (use the filled tile) · a falling or recolored sparkline (it is fixed geometry, not data).

---

## Screens / Views

### 1. Landing page — `Cardstock Landing.dc.html`

Surface `#F1F1EC`. Sections in order:

**Nav** — sticky, `rgba(241,241,236,0.92)` + `backdrop-filter: blur(8px)`, 1px `#E4E4E0` bottom border, padding `14px 40px`, max-width 1080. Left: mark 24px + "Cardstock" (18px/700/-0.03em) + `CDSTK` chip (mono 10.5px, `#8A8A86`, 1px border, radius 5, padding 1px 6px). Right, gap 22px: "Features", "Data" (13.5px/500 `#55555A`, hover `#4A63D0`), "Log in" (same), "Sign up →" (13.5px/600 white on `#4A63D0`, radius 7, padding 8px 14px, hover `#3A4FB8`).

**Ticker** — full-bleed `#131316`, 10px vertical padding, overflow hidden. A `max-content` flex track of the item list duplicated twice, animated `transform: translateX(0) → translateX(-50%)` over 44s linear infinite. Each item: mono 12px, gap 8px — symbol `#71716D`, name `#D6D6D0`, delta `#46C08A` (▲) or `#D0655E` (▼), `margin-right: 40px`, nowrap. Items: GIRA Giratina V (Alt Art) +16.7% · UMBR Umbreon VMAX (Alt Art) +10.9% · SYLV Sylveon VMAX (Alt Art) +1.4% · CHZD Charizard ex SAR -0.4% · BLAS Blastoise Holo -5.1% · GNGR Gengar VMAX (Alt Art) +0.8% · MKT Market 30d +2.4% · VOL Volume $2.1M +11%.

**Hero** — `overflow-x: clip`, grid `1.05fr 0.95fr`, gap 48px, padding `52px 40px 60px`, items center.
Left column (gap 20px): eyebrow "CDSTK · POKÉMON TCG AFTERMARKET DATA" · H1 "The terminal for the Pokémon card aftermarket." (52/800, max-width 520px, `text-wrap: pretty`) · body "Five years of graded-card price history, a screener across every printing, and your binder tracked like a portfolio. For collectors who read pop reports for fun." (17/1.6 `#55555A`, max-width 480px) · "Sign up →" button (15/600, padding 11px 20px, radius 8) · mono 12px "press / to search · fan-made demo" with the `/` in a 1px-bordered radius-4 chip.
Right column: `position: relative; min-height: 440px` containing the **shuffle deck** (below) plus three card image slots.

**Shuffle deck** — four absolutely-positioned cards (`top: 32px; right: 16px; width: 360px; max-width: 92%`), each a dark panel (`#131316`, 1px `#2A2B2E`, radius 12, shadow `0 24px 48px rgba(28,28,30,0.25)`). Order state `['A','B','C','D']`; every 4200ms the front card is marked exiting, animates out, and after 620ms the order rotates.
- Resting transforms by stack position: `translate(0,0) rotate(0) scale(1)` z40 op1 · `translate(-36px,-26px) rotate(-5deg) scale(0.97)` z30 op1 · `translate(26px,-46px) rotate(4deg) scale(0.94)` z20 op0.85 · `translate(-4px,-58px) rotate(-1deg) scale(0.91)` z10 op0.
- Exit: `translate(150px,110px) rotate(14deg)`, opacity 0, `transform 0.6s cubic-bezier(0.5,0,0.8,0.4)`, `opacity 0.55s ease-in`, z50. Resting transition: `transform 0.65s cubic-bezier(0.25,0.7,0.25,1), opacity 0.65s ease`. `transform-origin: 50% 80%`.
- Hover pauses the interval; click deals the next card. A mono 11px caption below-right reads "<front label> · click to shuffle" where labels are watchlist / /home / /screener / /charts.
- **Card A — Watchlist**: header row "WATCHLIST · 30D" / "DEMO" (indigo); four rows of name + set·grade / price + delta, each separated by a 1px `#232427` rule; a full-width sparkline SVG (`#8C9BF2`, 2px, endpoint dot); footer "press / to search" and "12M · normalized".
- **Card B — Binder**: title "Binder" + "Performance →"; three stat blocks (TOTAL VALUE `$18,432`, UNREALIZED `+$3,108` / ▲ +20.3%, VS MARKET `+8.7` pp · 12M) in mono 21/700; footer row "14 positions · 6 sets" / "Cost $15,324" / "▲ +$412 1M", all `white-space: nowrap`.
- **Card C — Screener**: title '"Quiet Accumulation"' + "12 matches"; a 4-column grid (CARD / PRICE / ROC 3M / CHURN) with three rows (Giratina V $845 +16.7% ×2.4 · Umbreon VMAX $1,486 +10.9% ×1.6 · Sylveon VMAX $612 +1.4% ×1.9).
- **Card D — Charts**: title "Umbreon VMAX (Alt Art)" + "PSA 10" chip, right-aligned "$1,309 ▲ +6.1%"; a 336×130 SVG with three `#232427` gridlines and mono 9px `$1,350 / $1,100 / $850` labels, **Bollinger bands** (two independent smooth `C` curves that squeeze in the quiet stretch and widen through the rally, `rgba(140,155,242,0.45)` 1px dashed `3 3`, fill between at `rgba(140,155,242,0.10)`, plus a faint middle band `rgba(140,155,242,0.30)` dashed `2 4`), then the price polyline `#8C9BF2` 2px with endpoint dot; footer chips "ROC 12M +18.2%", "RS 94th", "BB 20 · 2".

**Features** — `position: relative`, eyebrow "THE TOOLKIT", H2 "Three ways in.", then a 3-col grid (gap 20) of white cards (1px `#E4E4E0`, radius 10, padding 24, gap 12): each has a 64×40 line-art SVG, H3, 14/1.55 body, and a mono 11.5px indigo action line. Content — **Screener**: "Rank every printing by churn, z-score, and grade premium. Save a thesis as a screen, then backtest it." / "saved screens · backtest →". **Charts**: "Monthly closes from PSA 10 down to raw. Compare cards, overlay indicators — the Apr '25 data seam stays marked, never smoothed." / "open in charts →". **Binder**: "Cost basis, unrealized P&L, performance vs the market index. Your binder is a portfolio — treat it like one." / "+ binder".

**Methodology** — full-bleed `#131316`. Eyebrow "WHERE THE NUMBERS COME FROM", H2 "Trust is a feature." with "About the data →" right-aligned. 3-col grid of `#1B1C1F` cards (1px `#2A2B2E`, radius 10, padding 22): **PER-SALE LEDGERS** "Every price is built from recorded sales, not listings — deduplicated, with the ledger one click away." · **THE APR '25 SEAM** "Two data sources meet in April 2025. Charts mark the seam instead of smoothing it over." · **SUFFICIENCY RULES** "Thin markets get flagged, not filled in. Below threshold, a grade reads LOW CONFIDENCE — visible, never hidden." (inline foil badge).

**Footer** — `#0F0F11`, 1px `#232427` top. Grid `1.2fr 1fr 1fr`: brand column (dark mark 26px + wordmark 19px, then 14.5/1.65 `#9A9A96` blurb "Precise numbers over adjectives. No hype, no exclamation marks. We treat cardboard with the analytical rigor everyone in your life says it doesn't deserve. The market suggests otherwise."), PRODUCT links (Screener, Charts, Binder, About the data), BRAND links (Brand system, Logo files, Tokens). Bottom bar, mono 11px `#71716D`: "CDSTK · fan-made demo · not affiliated with Nintendo, The Pokémon Company, or Creatures Inc." and "© 2026 Cardstock".

**Scattered card slots (8).** Empty drop targets sized to the 5:7 trading-card ratio, each absolutely positioned *behind* real content so they never create layout shift or whitespace. In production replace with real card images (`object-fit: cover`, matching radius + shadow `0 12px 24px rgba(28,28,30,0.2)`, or `0 16px 32px rgba(0,0,0,0.45)` on dark):
| id | Section | Position | Size | Rotation |
|---|---|---|---|---|
| hero-card-right | Hero | `top:-24px; right:-48px` | 330×462 | 0° (straight) |
| hero-card-mid | Hero | `top:158px; left:-24px` | 150×210 | 11° |
| hero-card-left | Hero | `left:4px; bottom:0` | 120×168 | -8° |
| features-card | Features grid | `right:26px; bottom:34px` | 130×182 | 10° |
| features-card-2 | Features grid | `left:20px; bottom:30px` | 120×168 | -9° |
| features-card-3 | Features grid | `right:34px; top:128px` | 110×154 | -6° |
| data-card | Methodology grid | `right:44px; bottom:36px` | 130×182 | -7° |
| data-card-2 | Methodology grid | `left:0; bottom:30px` | 120×168 | -11° |

### 2. Brand system page — `Cardstock Brand System.dc.html`
Documentation surface (max-width 1020px, padding `64px 40px 96px`), numbered sections 01–06: Logo (light/dark lockups, size ramp, clearspace, four rendered misuse examples), Color (swatches, link treatments, 6-series chart palette), Type (Inter/Mono split + marketing scale), Components (buttons, input with focus ring, badges/chips), Voice (DO/DON'T pairs), Empty states (schematic style). Use it as the spec reference; it isn't a product screen.

---

## Interactions & Behavior
- **Ticker**: infinite CSS marquee, 44s linear. Duplicate the item array so the `-50%` translate loops seamlessly.
- **Shuffle deck**: 4.2s auto-advance, 620ms deal animation; `mouseenter` pauses, `mouseleave` resumes, `click` deals immediately. Guard against re-entry while a card is exiting. Clear timers on unmount. Respect `prefers-reduced-motion` — both animations should have an off path (they are toggleable props in the prototype).
- **Hover**: nav links and footer links shift to the primary; primary buttons darken to `#3A4FB8` (light) / lighten to `#AAB6F6` (dark); text links underline.
- **Focus**: 3px indigo ring, never removed.
- No responsive breakpoints are specified in the prototype (desktop-first at 1080px). At narrower widths, collapse the hero to one column, the 3-col grids to one, and hide or reduce the decorative card slots.

## State Management
Landing page only:
- `order: ['A','B','C','D']` — deck stacking order
- `exiting: string | null` — the card mid-deal
- `paused: boolean` — hover pause
- Props/flags: `tickerMotion`, `cardShuffle`, `showMethodology`
No data fetching in the prototype; all figures are demo content.

## Assets
- `brand/` — logo SVGs, favicons (16/32/180), `og-image.png`, `brand-tokens.css`. All generated for this project; free to ship.
- `assets/screens/` — original app screenshots, included for context only. They are **not** used in the final design (they rasterized poorly); the deck cards are real markup.
- Pokémon card images are **not** included — the scattered slots are empty placeholders for user-supplied art. Ensure any card imagery used is properly licensed; the product is fan-made and the footer disclaimer must stay.

## Files
- `Cardstock Landing.dc.html` — the landing page
- `Cardstock Brand System.dc.html` — brand documentation
- `brand/brand-tokens.css` — canonical tokens
- `brand/*.svg`, `brand/*.png` — logo and social assets
- `image-slot.js`, `support.js` — prototype-runtime helpers, **not** for production
