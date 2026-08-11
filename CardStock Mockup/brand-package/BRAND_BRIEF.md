# Cardstock — brand package brief

Paste this into the design-system project. Screenshots of every page are in screenshots/.

## Product
Cardstock: a "Bloomberg terminal for Pokémon card collectors" — a market-data web app for the collectible card aftermarket. What it does:
- Home: portfolio value, market indexes, top movers, watchlist
- Screener: filter/rank thousands of cards by price, grade premium, volume, returns
- Charts: price history with compare, indicators, and strategy backtesting
- Binder: your collection as a portfolio — lots, cost basis, P&L, buy/sell flows
- Browse/Set/Character/Card: reference pages for every set, character, and printing
- About Data: methodology page (sources, the Apr '25 data seam, sufficiency rules)
Fan-made demo; not affiliated with Nintendo/TPCi — brand must NOT imitate official Pokémon trade dress (no Pokéballs, no official yellow/blue logotype vibes).

## Where the app took inspiration
- Bloomberg terminal: density, mono numerals, keyboard shortcuts ("/" to search), data-first chrome
- Retail brokerage/screener tools (Finviz, Koyfin, Yahoo Finance): screener tables, sparklines, movers lists
- TCGplayer / PriceCharting: the domain — card pricing, sets, grades — but with a calmer, more professional skin
- Paper/print sensibility: warm off-white background, hairline rules, restrained color — "cardstock" the material

## Audience & tone
Serious adult collectors/investors. Data-dense, calm, trustworthy, a little nerdy. Think finance tool with warmth, not a toy. No emoji, no gradients-everywhere AI look.

## Current UI (the package must sit on top of this, not fight it)
- Type: Inter (UI), JetBrains Mono (numbers, tickers, kbd hints)
- Light theme tokens: ink #1C1C1E, bg #FAFAF7, card #FFFFFF, line #E4E4E0, muted #8A8A86, hover #F6F6F2
- Has dark mode + colorblind-safe chart modes — brand colors need to survive all three
- Density: compact rows, 13–15px UI text, thin 1px hairlines, 6–8px radii

## What the package should deliver
1. Logo: wordmark + small square mark (favicon/nav, works at 20px)
2. Brand accent palette: 1 primary + 1–2 supports, with dark-mode variants; chart series colors (6+, CVD-safe) if you want to own those too
3. a / a:hover link color
4. Voice notes: microcopy style (matter-of-fact, no hype)
5. Optional: empty-state / onboarding illustration style

## Deliverable format
A design system Claude Design can attach to another project (components optional — tokens, logo SVG, and usage page are what matters).
