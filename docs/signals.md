# Signal Inventory for a PriceCharting-Sourced Pokémon Card Screening Platform
*A practical reference for turning three data substrates into an indicator-playground and screener spec*

## TL;DR
- **Build your v1 around momentum, relative-strength-vs-index, grade-tier spread (grading arbitrage), and supply-vs-price divergence signals** — these four families are both academically supported (momentum in cards is documented in Engelberg, Thompson & Williams 2020, "Stock Market Anomalies and Baseball Cards," *The Financial Review* 55(3):461–479, where **3-month card momentum strategies earned 5.6% per month vs. under 1% per month for equity momentum**) and honestly computable from monthly average prices + population census, with no dependence on the fragile sale-ledger seam.
- **Roughly a third of the classic TA toolkit is invalid or misleading on this data**: candlestick/OHLC patterns, gap analysis, intraday oscillators, VWAP, and any true volume indicator (OBV, A/D, MFI, CMF) pre-seam cannot be honestly computed because you have monthly *average* points (no OHLC) and no deep historical volume. Message their absence rather than fake them.
- **The dataset's unique edge is cross-substrate composites** — population-growth-vs-price divergence, grade-tier spread compression, churn acceleration, and cross-marketplace price gaps — that no single competitor (PriceCharting, Card Ladder, Market Movers, GemRate) surfaces in one place. These should be the differentiated core of the "indicator playground."

## Key Findings

**1. Monthly resolution + ~68 points is enough for slow indicators, fatal for fast ones.** With one point per month, a "14-period RSI" spans 14 months, and equity defaults (12/26/9 MACD, 20-period Bollinger, 50/200 SMA) consume half your history or more. Every oscillator must be re-tuned to 3–9 month windows, and any classic signal requiring >36 months of lookback leaves you <32 usable observations. Academic work on technical indicators finds their predictive content concentrates in medium-frequency (roughly 16–64 month) oscillations — which happens to be the band monthly card data can actually resolve, so slow trend/momentum signals are the right fit and fast ones are noise.

**2. Momentum is the best-supported predictor and it is real in cards specifically — and unusually strong.** Engelberg, Thompson & Williams (2020) analyzed **37,116 distinct cards / 1,662,273 (card, date, price) observations** from 72 Beckett price-guide issues covering card years 1948–1996, and found baseball cards "exhibit anomalies that are analogous to those… in financial markets, namely, momentum, price drift in the direction of past fundamental performance, and IPO underperformance," with momentum stronger among active players and newer sets. Critically, **short-run (3-month) momentum strategies earned 5.6% per month, whereas stock-market momentum earns less than 1% per month** — card momentum was ~5–6x stronger than equity momentum. This is direct evidence that cross-sectional and time-series momentum ranking — the cheapest thing to compute from monthly averages — has genuine and large predictive power in a card market.

**3. Population dynamics are the supply-side signal collectors already trust — and you can compute them going forward.** Practitioner consensus (The Hockey News, PreGradeCards, Card Ladder) is that rising PSA-10 population compresses the PSA-10-over-PSA-9 premium — PreGradeCards states that "as PSA 10 population grows from hundreds to thousands, the premium compresses from 5x to 2x" — and that pop growth is a sell signal while stagnant pop + rising demand is a buy signal. A dated Pokémon example of this dynamic: Prismatic Evolutions PSA 10s sold for ~10x raw in early 2025, fell to ~5x raw by mid-2025 as population grew, and settled at ~4–6x raw by late 2025. Your change-only census (2026 onward) makes grading-activity deltas a first-class, differentiated signal — with the hard caveat that you have no pre-2026 population history and must flag grader restatements.

**4. Grade-tier spread ("grading arbitrage") is the highest-conviction valuation signal and is native to your six-tier price structure.** Every competitor tool (Slabfy, PokemonPriceTracker, PullRate, PokePrices) ships a raw→PSA-10 ROI calculator; you already store all six tiers per card, so you can compute expected grading value at portfolio scale. Rule of thumb across sources: the graded premium generally needs to be **at least ~3x the raw price** (PokemonPriceTracker/PokeTracker) — and more conservatively 4x+ plus ~$40 minimum raw value — to clear grading fees + marketplace fees. Note that modern PSA-10 premiums have compressed to just 1.3–2x on many cards (PokemonPriceTracker, 2026), making the screen especially valuable for weeding out no-longer-profitable submissions.

**5. Liquidity is a priced, honest signal — but only post-seam.** The Amihud (2002) illiquidity ratio — defined in "Illiquidity and Stock Returns: Cross-Section and Time-Series Effects," *Journal of Financial Markets* 5(1):31–56, as "the average daily ratio of absolute stock return to dollar volume" (studied on NYSE stocks 1964–1997) — is the standard academic price-impact proxy and adapts cleanly to monthly card data. Sales-count churn, monthly volume, and dispersion within a grade bucket are all computable — but ONLY forward of each card/grade's seam date, because the source shows ~30 most-recent sales per bucket and deep volume can never be backfilled.

## Details

Throughout, I refer to the three substrates as **[S1] monthly six-tier average price**, **[S2] per-sale ledger (post-seam)**, and **[S3] population census (post-first-observation, 2026+)**. Every signal notes which it reads.

---

### CATEGORY A — TREND / MOMENTUM

**A1. Rate of Change / Momentum (n-month return)** — *Category: momentum*
- **Definition:** `ROC_n = (P_t / P_{t-n}) - 1`, computed per grade tier on [S1]. Ship 1, 3, 6, 12-month variants. SQL sketch: `(p.avg_price / lag(p.avg_price, n) OVER (PARTITION BY card_id, tier ORDER BY month)) - 1`.
- **Substrate/min data:** [S1] only; needs n+1 monthly points. Valid from ~n months after Dec 2020 for any tier.
- **Parameters:** 3M and 6M as primary momentum horizons; 12M as the "trend" horizon.
- **Predicts:** Continuation. Directly supported by Engelberg et al. (2020), where 3-month card momentum earned 5.6%/month — the 3M horizon is the single best-evidenced parameter choice.
- **Caveats:** Monthly averaging smooths intramonth spikes; a card that doubled and reverted within a month shows muted ROC. Current (revising) month should be excluded or flagged.
- **Priority:** **v1, high confidence.**

**A2. Short-window EMA/SMA and crossovers** — *Category: trend*
- **Definition:** EMA/SMA over [S1] price; crossover of fast over slow as trend flip.
- **Substrate/min data:** [S1]; a 3/9 crossover needs ~12 months for stability.
- **Parameters:** Use **3/6/9-month** windows, NOT equity 12/26/50/200. A 9-month SMA is the "trend baseline"; 3-over-9 EMA cross is the primary trend-flip trigger. (Practitioner OmniaChart uses 30/50-day SMA on daily card data; the monthly analog is 3–6 month.)
- **Predicts:** Trend regime.
- **Caveats:** Whipsaw on thin cards where monthly averages jump on one sale.
- **Priority:** **v1, high.**

**A3. MACD (re-tuned)** — *Category: trend/momentum*
- **Definition:** `MACD = EMA_fast − EMA_slow`; signal = EMA of MACD. On [S1].
- **Parameters:** **3/6/4 months** (not 12/26/9). Even so this consumes ~10 months of warmup.
- **Predicts:** Momentum inflection.
- **Caveats:** Redundant with A1/A2 at monthly resolution; histogram is jumpy. Include as a "familiar" indicator for Webull-migrants but rank below raw ROC.
- **Priority:** **v2, medium.**

**A4. Trend strength / R² of log-price regression** — *Category: trend*
- **Definition:** Fit `ln(P) ~ t` over trailing 6–12 months; report slope (monthly drift) and R² (trend cleanliness).
- **Substrate:** [S1].
- **Predicts:** Distinguishes clean uptrends from noisy ones — addresses the "price up but is it real" problem better than a single ROC.
- **Priority:** **v1, medium-high** (cheap, robust to monthly data).

---

### CATEGORY B — MEAN REVERSION

**B1. Distance-from-moving-average (z-score / %-from-MA)** — *Category: mean reversion*
- **Definition:** `z = (P_t − SMA_k) / σ_k` over trailing k months on [S1].
- **Parameters:** k = 6–12 months. |z| > ~1.5–2 flags stretched.
- **Predicts:** Short-horizon reversal. Collectibles show mean-reversion especially in illiquid losers (crypto/collectibles literature); card corrections after hype spikes are well-documented (2021 bubble → 2022–23 crash).
- **Caveats:** In a structural regime change (print-run flood, franchise decline) "cheap vs MA" keeps getting cheaper — reversion signals are dangerous on modern oversupplied cards.
- **Priority:** **v1, medium.**

**B2. Bollinger Bands (re-tuned) / band-width** — *Category: mean reversion + volatility*
- **Definition:** `SMA_k ± m·σ_k` on [S1]. Report band-width as a volatility proxy and %B as position.
- **Parameters:** **k = 6, m = 2** (not 20/2). Independent testing shows Bollinger is weak as a standalone even on daily equities (one large test found a ~33% success rate at default settings across the Dow 30); on 68 monthly points treat it as a *visualization/volatility* overlay, not a trade trigger.
- **Predicts:** Volatility regime; band touches are weak reversion hints.
- **Caveats:** No OHLC means bands are built on average prints, understating true range.
- **Priority:** **v2, low-medium** (mostly for chart familiarity).

**B3. RSI (re-tuned)** — *Category: momentum oscillator / mean reversion*
- **Definition:** Standard RSI on monthly [S1] month-end averages.
- **Parameters:** **6–9 month** RSI, not 14. Overbought/oversold at 70/30 but widen to 80/20 given card volatility.
- **Predicts:** Exhaustion / reversal.
- **Caveats:** With 6-month lookback and monthly data, RSI is slow; a card can stay "overbought" through an entire multi-month run. Practitioner lore (marketcorpus) even treats monthly RSI>70 as *bullish continuation*, not a sell — consistent with momentum dominating at these horizons. Message carefully.
- **Priority:** **v2, medium** (users will expect it).

**B4. Drawdown from trailing peak** — *Category: risk / mean reversion*
- **Definition:** `DD_t = P_t / max(P_{t-k..t}) − 1`; also max drawdown over full history.
- **Substrate:** [S1].
- **Predicts:** Risk state + "discount from all-time-high" as a value filter. Useful for "buy quality on a dip" screens on blue-chip vintage.
- **Priority:** **v1, medium-high** (intuitive, robust, no OHLC needed).

---

### CATEGORY C — VOLUME / LIQUIDITY (all post-seam only)

**C1. Churn / sales-per-day (trailing window)** — *Category: liquidity*
- **Definition:** count of [S2] sales in trailing 30/90/180 days ÷ window length, per grade bucket.
- **Substrate/min data:** [S2], **post-seam only**. Needs the seam to be older than the window.
- **Predicts:** Liquidity / attention. Rising churn + rising price = conviction; rising price + falling churn = thin, unreliable move (exactly the "price/pace/liquidity" framing Collector's Edge and Trend Tracker use).
- **Caveats:** Absolutely invalid pre-seam; the ~30-row window caps how far back any card's ledger goes.
- **Priority:** **v1, high** (post-seam), with explicit "insufficient post-seam history" state.

**C2. Churn acceleration** — *Category: liquidity momentum*
- **Definition:** `churn_30d / churn_90d − 1` (short-window pace vs baseline).
- **Substrate:** [S2] post-seam.
- **Predicts:** Emerging attention before price fully moves — a lead indicator.
- **Priority:** **v1, medium-high** (novel, differentiated).

**C3. Amihud illiquidity ratio (monthly adaptation)** — *Category: liquidity / price impact*
- **Definition:** `ILLIQ = |monthly return| ÷ monthly dollar volume`, where dollar volume = Σ realized prices in [S2] that month. Report as a percentile within set.
- **Substrate:** [S1] returns + [S2] volume, post-seam.
- **Predicts:** Illiquidity premium — illiquid assets earn higher expected returns as compensation (Amihud 2002; Acharya-Pedersen 2005). Also a data-quality gate: high ILLIQ = distrust the price.
- **Caveats:** Undefined in zero-sale months; needs post-seam volume. Sparse.
- **Priority:** **v2, medium** (powerful but needs enough sales).

**C4. Monthly sales volume & unique-sale count** — *Category: liquidity*
- **Definition:** count and Σ dollar value of [S2] sales per month per bucket.
- **Substrate:** [S2] post-seam.
- **Predicts:** Market depth; screener filter to exclude untradeable cards. (Aligns with practitioner advice to "always look at sales volume alongside price — a rising price with declining volume suggests a thinning market, not genuine demand.")
- **Priority:** **v1, high** (foundational; also gates other signals).

**C5. Within-bucket price dispersion** — *Category: liquidity / risk*
- **Definition:** coefficient of variation (σ/μ) of [S2] realized prices within a grade bucket over trailing window.
- **Predicts:** Pricing uncertainty / arbitrage room. High dispersion = inefficient pricing = deal-hunting opportunity, but also execution risk.
- **Substrate:** [S2] post-seam.
- **Priority:** **v2, medium** (novel).

---

### CATEGORY D — SUPPLY-SIDE / POPULATION (post-first-observation, 2026+)

**D1. Grading-activity delta (population growth rate)** — *Category: supply*
- **Definition:** `ΔPop = (Pop_t − Pop_{t-1})` per grade per grader from [S3] snapshots; also % growth.
- **Substrate/min data:** [S3], forward of first crawl observation (2026+). Needs ≥2 clean snapshots.
- **Predicts:** New supply entering the graded pool. Rising pop is a leading bearish pressure on graded premiums (universal practitioner consensus).
- **Caveats:** No pre-2026 history; **suspend/flag during grader restatement windows** — a restatement can masquerade as a huge one-period delta.
- **Priority:** **v1, high** (differentiated, trusted metric).

**D2. Population-growth-vs-price divergence** — *Category: composite supply signal (novel)*
- **Definition:** Signal = sign and magnitude of `(price ROC_n)` vs `(pop growth_n)`. Bearish when pop growth is high AND price flat/falling (supply flooding); bullish when pop is flat/shrinking-in-relative-terms AND price rising (scarcity + demand).
- **Substrate:** [S1] + [S3], 2026+.
- **Predicts:** The single most-cited supply/demand setup in card investing ("stagnant/declining pop + rising demand = buy," per CardChasersMTL). No competitor surfaces this as a computed, screenable signal.
- **Caveats:** Only forward of 2026; restatement-sensitive.
- **Priority:** **v1, flagship novel signal, high.**

**D3. Gem rate & gem-rate drift** — *Category: supply / grading quality*
- **Definition:** `GemRate = PSA10_pop / total_PSA_pop`; drift = change over time. From [S3].
- **Predicts:** Difficulty of achieving PSA-10 (low gem rate + demand = grading-scarcity premium). GemRate.com and Business of Card Grading treat this as the core pre-grade screen. Benchmark context: per GemRate (via cllct, Aug 2025), the TCG category "gemmed 50% of the time across the 7.2 million cards graded in the first half of 2025" and carried "a 53% gem rate across more than 8.6 million cards graded in 2024," while PSA overall ran a 43% gem rate on 8.9M cards (72% market share).
- **Caveats:** Your census starts 2026, so gem rate is a *level* you can read but its long-term *history* is unavailable pre-2026.
- **Priority:** **v1, medium-high** (feeds grading-arbitrage signals).

**D4. Supply overhang ("pop-to-volume ratio")** — *Category: supply/liquidity composite (novel)*
- **Definition:** `graded pop ÷ trailing annual sales count` = years of supply at current absorption.
- **Substrate:** [S3] + [S2] post-seam.
- **Predicts:** How overhung a card is; high ratio = hard to move without price cuts.
- **Priority:** **v2, medium** (novel, needs both substrates mature).

---

### CATEGORY E — VALUATION / SPREAD

**E1. Grade-tier spread / grading arbitrage EV** — *Category: valuation (flagship)*
- **Definition:** For raw→target grade: `EV = (GemRate × P_PSA10 + (1−GemRate) × P_lowergrade) − P_raw − grading_fee − marketplace_fee`. Also simple ratio `P_PSA10 / P_ungraded`.
- **Substrate:** [S1] all six tiers + [S3] gem rate.
- **Parameters:** Gate: graded premium ≥ ~3x raw (PokemonPriceTracker minimum), more conservatively 4x+ and raw ≥ ~$40 (PullRate, WhatTheSlab, Slabfy). Fee ≈ 13% marketplace + current PSA tier. Note modern PSA-10 premiums have compressed to 1.3–2x on many cards — the screen's main value today is *rejecting* unprofitable submissions.
- **Predicts:** Undervalued raw cards worth grading — a direct "undervalued hunting" screen.
- **Caveats:** Real-world gem rate ≠ pop-report gem rate for fresh pulls; alt-art/full-art cards grade harder (directionally ~25–40% PSA-10 per practitioner reports — treat as a rough prior, not a sourced constant). Use census gem rate as a prior and flag it.
- **Priority:** **v1, flagship, high.**

**E2. Grade-tier spread compression/expansion** — *Category: valuation composite (novel)*
- **Definition:** Track `P_PSA10 − P_PSA9` (or ratio) over time; compression = premium collapsing (often from pop growth), expansion = premium widening.
- **Substrate:** [S1] (+[S3] to explain via pop).
- **Predicts:** Documented inverse pop↔premium relationship (PreGradeCards: 5x→2x compression as pop rises from hundreds to thousands). Compression is an early sell/avoid signal for the top tier.
- **Priority:** **v1, medium-high** (novel, native to six-tier data).

**E3. Discount-to-list** — *Category: valuation / sentiment*
- **Definition:** where [S2] has original listed price: `1 − realized/listed`, averaged over trailing window.
- **Substrate:** [S2] post-seam, only rows with listed price.
- **Predicts:** Buyer/seller power; widening discounts = softening demand.
- **Caveats:** Listed price is optional/sparse; auction formats distort it (a $1 auction start isn't a "list"). Filter by marketplace.
- **Priority:** **v2, medium** (data completeness risk).

**E4. Cross-marketplace price divergence** — *Category: valuation / microstructure (novel)*
- **Definition:** compare mean realized [S2] price for same card/grade across ebay vs auction houses (goldin/heritage/pwcc) vs tcgplayer over trailing window.
- **Substrate:** [S2] post-seam, tagged by source.
- **Predicts:** Auction houses realize ~10–20% higher hammer on high-end graded cards (SluggerData, CardValueFinder); persistent eBay < auction-house gaps flag consignment upside or a cheap buy venue.
- **Caveats:** Needs enough sales per venue; auction "realized" often excludes/includes buyer's premium inconsistently — normalize before comparing.
- **Priority:** **v2, medium-high** (novel, differentiated, but data-hungry).

---

### CATEGORY F — RELATIVE / INDEX-BASED

**F1. Corpus-wide & set-level index** — *Category: relative baseline*
- **Definition:** Build a market index from [S1] across ~100k cards. Use a chained per-card monthly-relative index (equal- or value-weighted) rather than Card Ladder's "sum of last-sold values ÷ card count," which jumps when a new card's first sale enters. Also per-set indices (~303 sets).
- **Substrate:** [S1].
- **Method note:** True repeat-sales regression (Case-Shiller/Mei-Moses) is the gold standard for heterogeneous collectibles, but your [S1] is already a per-card average series, so a chained index of per-card monthly relatives is the pragmatic choice and sidesteps thin-trading repeat-sales bias. Card Ladder's own player index has a documented "index jumps when a new card's first sale enters" flaw (they require a card to have ≥2 sales in the last year and ≥1 in 6 months to be included) — mitigate by requiring a minimum active-card count per period.
- **Predicts:** The benchmark for all relative signals.
- **Priority:** **v1, foundational, high.**

**F2. Relative strength vs index (RS line & RS momentum)** — *Category: relative*
- **Definition:** `RS = card_return − index_return` (or ratio line) over 3/6/12 months. Rank percentile within corpus/set.
- **Substrate:** [S1] + F1.
- **Predicts:** Outperformers persist (relative momentum) — the core "trends up faster than average market" ask, and exactly what Engelberg et al.'s 5.6%/month card momentum implies. This is arguably THE signal for the platform's stated goal.
- **Priority:** **v1, flagship, high.**

**F3. Set rotation / breadth** — *Category: relative macro*
- **Definition:** rank sets by index momentum; compute advance/decline breadth across cards in a set or the whole corpus (TCGIndex surfaces exactly this — a "60-day advance and decline trend across the entire game").
- **Substrate:** [S1].
- **Predicts:** Where capital is rotating; healthy broad move vs narrow one carried by a few names.
- **Priority:** **v2, medium-high** (great for a "market overview" page).

**F4. Beta / correlation to market index** — *Category: relative risk*
- **Definition:** regress card monthly returns on index returns over trailing 12–24 months.
- **Substrate:** [S1] + F1.
- **Predicts:** Defensive vs high-beta cards; low-correlation cards for "diversification" framing.
- **Caveats:** 24-month beta uses a third of your history; unstable for thin cards.
- **Priority:** **v3, low-medium.**

---

### CATEGORY G — COMPOSITE / NOVEL (cross-substrate)

**G1. "Quiet accumulation"** — churn acceleration (C2) + flat/rising price (A1) + flat pop (D1). Bullish: attention building, supply not flooding, price not yet run. **v1, high** — the cleanest "undervalued before the move" composite.

**G2. "Supply flood warning"** — high pop growth (D1) + premium compression (E2) + flat/negative price momentum (A1). Bearish/avoid. **v1, high.**

**G3. "Grade-arb + liquidity"** — grading EV (E1) positive AND post-seam churn (C1) high enough to actually exit graded copies. Filters out arbitrage that's untradeable. **v1, medium-high.**

**G4. "Relative-strength breakout"** — RS-vs-index (F2) top-decile AND trend R² (A4) high AND drawdown (B4) modest. The disciplined momentum screen. **v1, high.**

**G5. "Newly-graded / newly-listed momentum" (caution signal)** — first-observation pop in [S3] or newly-appearing [S2] ledger for a card. Engelberg et al. (2020) found the *opposite* of naive expectation: newly issued rookie cards and new sets had cumulative abnormal returns of **–6.6% and –5.7% respectively over the 12 months following release (both t ≈ 2.8, statistically significant)** — an "IPO underperformance" effect. So treat newly-graded/newly-surfaced supply as a *caution* signal (new supply tends to underperform initially), not a buy. **v2, medium.**

---

## Signals users will EXPECT (from Webull etc.) but you CANNOT honestly support

| Expected signal | Why it can't be supported |
|---|---|
| **Candlestick / OHLC patterns (doji, engulfing, hammer)** | [S1] is a monthly *average*, not open/high/low/close. No intramonth range exists. |
| **Intraday / daily charts, gaps, opening-range** | Monthly resolution only; closed months immutable. |
| **VWAP** | Requires intraday volume-weighted prints; you have monthly averages. |
| **OBV, Accumulation/Distribution, Chaikin Money Flow, MFI** | True volume indicators need continuous volume history; [S2] volume is honest only post-seam and shallow (~30-row window), so any long-lookback volume indicator is fabricated pre-seam. |
| **True ATR / high-low volatility** | No high/low per period. Substitute band-width (B2) or return σ, and label it as return-based, not range-based. |
| **Level-2 / bid-ask spread / order book** | No live listings/quotes in the substrates; only completed sales. Discount-to-list (E3) is the closest honest proxy. |
| **Long-history (200-period) MAs, multi-year backtests pre-2020/pre-seam/pre-2026** | Price history starts ~Dec 2020; sales complete only post-seam; population only 2026+. Any signal implying deeper history is dishonest. |
| **Real-time / "live" price ticks** | Only the current month revises; closed months are frozen. |
| **Short interest, put/call, insider flow** | No analog exists in collectibles data. |

**Product guidance:** put a short "Why isn't there a candlestick chart?" explainer in the indicator playground. Turning the data-honesty constraint into visible messaging is a trust advantage over competitors that silently interpolate (PokemonPriceTracker explicitly says its "interpolation system ensures smooth price charts even when market data is sparse"; you can differentiate by showing real prints + a confidence badge).

---

## Recommendations

**Stage 1 (v1 core — ship first):** F1 (index), F2 (relative strength vs index), A1 (ROC, lead with the 3M horizon), A2 (short EMA cross), A4 (trend R²), B4 (drawdown), E1 (grading-arb EV), E2 (tier-spread compression), D1 (pop delta), D2 (pop-vs-price divergence), C1/C4 (churn & volume, post-seam), plus composites G1, G2, G4. These are the honest, high-confidence, differentiated backbone and directly serve "find cards trending up faster than the market." Momentum (A1/F2) is your headline feature because it has both the strongest academic effect size (5.6%/month in Engelberg et al.) and the cheapest data requirements.

**Stage 2 (v2):** MACD (A3), RSI (B3), Bollinger (B2), Amihud (C3), dispersion (C5), gem-rate drift (D3), supply overhang (D4), discount-to-list (E3), cross-marketplace divergence (E4), set rotation (F3), composites G3/G5.

**Stage 3 (later/experimental):** beta (F4), any ML factor blends, seasonality overlays once ≥3 Novembers of data accumulate (note the documented "summer slump" and November/Black-Friday liquidity dips in Pokémon).

**Thresholds that change the plan:**
- If, after ~6 months, post-seam ledgers for most cards remain too thin (median <~6 sales/yr), demote all [S2]-dependent signals (C-family, E3, E4) to an "advanced/low-confidence" tab and lean harder on [S1]+[S3].
- If grader restatements prove frequent/large, add a global "population data suspect" banner and auto-suppress D-family signals for affected cards for N periods.
- If a repeat-sales index materially diverges from the chained-average index, publish both and default to the more stable one.
- Once you have ≥36 clean monthly points per major set, enable longer-window trend signals and seasonality.

**Design principle:** every signal must carry a **confidence/data-sufficiency badge** (e.g., "pre-seam: volume signals hidden," "n<12 months: momentum low-confidence," "restatement window"). This is the single most important UX decision — it converts the dataset's honesty constraints into a credibility feature.

## Summary table — signals ranked by v1 priority

| Rank | Signal | Category | Substrate(s) | v1? | Confidence | Seam/observation constraint |
|---|---|---|---|---|---|---|
| 1 | F2 Relative strength vs index | Relative | S1 | ✅ | High | None |
| 2 | A1 Rate of change (3M/6M) | Momentum | S1 | ✅ | High | None |
| 3 | E1 Grading-arbitrage EV | Valuation | S1+S3 | ✅ | High | Gem rate 2026+ |
| 4 | D2 Pop-growth-vs-price divergence | Supply (novel) | S1+S3 | ✅ | High | 2026+, restatement-gated |
| 5 | F1 Corpus/set index | Relative base | S1 | ✅ | High | None |
| 6 | D1 Grading-activity delta | Supply | S3 | ✅ | High | 2026+, restatement-gated |
| 7 | A2 Short EMA/SMA cross (3/6/9) | Trend | S1 | ✅ | High | None |
| 8 | C1 Churn / sales-per-day | Liquidity | S2 | ✅ | Med-High | Post-seam only |
| 9 | C4 Monthly volume & count | Liquidity | S2 | ✅ | Med-High | Post-seam only |
| 10 | G4 RS breakout composite | Composite | S1 | ✅ | High | None |
| 11 | G1 Quiet-accumulation composite | Composite | S1+S2+S3 | ✅ | Med-High | Post-seam + 2026+ |
| 12 | G2 Supply-flood warning | Composite | S1+S3 | ✅ | Med-High | 2026+ |
| 13 | E2 Tier-spread compression | Valuation (novel) | S1(+S3) | ✅ | Med-High | None (S1) |
| 14 | A4 Trend R²/slope | Trend | S1 | ✅ | Med-High | None |
| 15 | B4 Drawdown from peak | Risk | S1 | ✅ | Med-High | None |
| 16 | C2 Churn acceleration | Liquidity (novel) | S2 | ✅ | Medium | Post-seam only |
| 17 | B1 Distance-from-MA z-score | Mean reversion | S1 | ✅ | Medium | None |
| 18 | D3 Gem rate & drift | Supply | S3 | ✅ | Med-High | 2026+ |
| 19 | F3 Set rotation / breadth | Relative macro | S1 | v2 | Med-High | None |
| 20 | E4 Cross-marketplace divergence | Microstructure (novel) | S2 | v2 | Med-High | Post-seam, venue depth |
| 21 | C3 Amihud illiquidity | Liquidity | S1+S2 | v2 | Medium | Post-seam only |
| 22 | B3 RSI (6–9M) | Oscillator | S1 | v2 | Medium | None |
| 23 | C5 Within-bucket dispersion | Liquidity/risk (novel) | S2 | v2 | Medium | Post-seam only |
| 24 | D4 Supply overhang | Supply (novel) | S2+S3 | v2 | Medium | Post-seam + 2026+ |
| 25 | A3 MACD (3/6/4) | Trend | S1 | v2 | Medium | None |
| 26 | E3 Discount-to-list | Valuation/sentiment | S2 | v2 | Medium | Post-seam, listed-price rows |
| 27 | B2 Bollinger Bands (6,2) | Volatility | S1 | v2 | Low-Med | None (weak signal) |
| 28 | G5 Newly-graded/listed (caution) | Composite | S2/S3 | v2 | Medium | Post-seam / 2026+ |
| 29 | F4 Beta/correlation | Relative risk | S1 | v3 | Low-Med | Needs 24M history |

## Caveats
- **Momentum evidence, while real and large (Engelberg et al. 2020: 5.6%/month at the 3-month horizon), is from ~37,000 baseball cards priced 1948–1996, not Pokémon.** The direction and mechanism (slow information diffusion, short-selling constraints) transfer well, but magnitudes will differ for a modern TCG. Treat momentum as a strong prior, not a guarantee.
- **Monthly averaging is a double-edged sword**: it denoises thin cards but hides real intramonth volatility and makes the current (revising) month unreliable — always exclude or flag it.
- **The seam is the central data-integrity boundary**: all volume/churn/dispersion/discount signals are honest only forward of it and can never be backfilled, because the source caps at ~30 recent sales per bucket.
- **Population census is young (2026+)** and restatement-prone; supply signals are powerful but must be restatement-gated.
- **Regime risk dominates**: the 2021 bubble/2022–23 crash (modern cards down 40–60% as The Pokémon Company flooded print runs) shows card markets undergo structural breaks where mean-reversion signals fail catastrophically. No indicator substitutes for the supply-side (D-family) context.
- **Survivorship in the ~30-row sale windows**: cards that stop selling drop out of recent-sales visibility, biasing churn/volume upward for active cards; screen results skew toward liquid names.
- Several practitioner figures (gem rates, ROI multiples, auction-house premia, alt-art grade difficulty) come from secondary/marketing sources; treat specific percentages as directional, not precise. The Engelberg et al. and Amihud figures are the peer-reviewed anchors.
---

## Chip vocabulary — restored 2026-08-12 (D-085)

> Restored verbatim from the retired `DISPLAY_VOCABULARY.md` §1 (`git show d54b40b^:"CardStock Mockup/DISPLAY_VOCABULARY.md"`).
> The retirement commit claimed that file was "verified fully superseded"; this section — the complete
> chip inventory, the only definition of every chip the product can display — had in fact migrated
> nowhere. It lives here now because every chip is a signal's display form; keep it in sync with the
> signal table above.


## 1. Signal chips (Card page header · watchlist rows · peek panel)
One chip grammar everywhere: `icon + short name + evidence number`, tooltip = one-sentence evidence with window and threshold.

**Card page header**: shows only FIRING chips (a signal in a notable state), priority-ordered, cap 4, overflow "+N more" opens all. A signal below its sufficiency floor never chips.
**Watchlist rows**: chips are the user's TRACKED signals for that card — all render regardless of state, including quiet (`–` grey muted) and insufficient (`◌` grey, tooltip = unlock countdown). Glance rule: **colored = hit** (green ▲ bullish · red ▼ bearish · amber – caution/directionless), **grey = nothing to report** (quiet or not yet computable).

> **Amended 2026-08-13 (D-092) — the Card page header rule above is superseded by the Signals
> panel.** The chip row became an unbounded panel (card.md §2.3.2): every chip-eligible signal
> the engine can evaluate renders as a ROW in exactly one of five states — **firing** (toned;
> value = the evidence number) · **quiet** (computed, inside its bands; value = the live
> reading) · **below-floor** (value `—`; tooltip names the floor and the computed progress
> toward it, never a number) · **neutral** (`●`; liquidity/state signals, never directional) ·
> **locked** (substrate missing product-wide; value names the unlock). Firing-only display,
> cap 4, and `+N more` retire with the row. The count line
> `{evaluated} evaluated · {firing} firing` is computed from the rows, and its exclusion
> sentence is authored: Bollinger, beta, discount-to-list, and seasonality are not chip-eligible
> — **25 eligible of 29**. The watchlist-pill rules below are untouched (they were always
> all-states).
>
> Phase 2's computed roster is **eight price signals + one liquidity row**: the original seven
> plus **RSI (6)** (new to the computed set) and the always-on neutral **Sales volume** row
> (`{n} / 30d` over the trailing 30 days — no "most active" superlative until corpus ranking
> exists). Substrate-locked signals render as locked rows (`RS vs index 3M`, `Pop Δ 60d`,
> `Churn 30d` in Phase 2), never seed numbers. Three inventory rows are superseded on the Card
> page surface — see the dated notes under the table.

Full chip inventory (trigger → chip text):
| Signal | Fires when | Chip | Dir |
|---|---|---|---|
| RS vs index | pct ≥ 90 / ≤ 10 | `RS 94th` | ▲/▼ · amber – on decile exit: 80–89th within 3 mo of ≥90 (`RS 84th`) |
| ROC 3M | ≥ +15% / ≤ −15% | `ROC 3M +18%` | ▲/▼ |
| ROC 1/6/12M | same bands, only if tracked | `ROC 12M +41%` | ▲/▼ |
| MACD (3,6,4) | above/below signal line | `MACD +` / `MACD −` | ▲/▼ |
| EMA 3×9 cross | crossed within last 2 mo | `EMA cross +` / `−` | ▲/▼ |
| RSI (6) | > 80 / < 20 | `RSI 82` | ▲/▼ · amber – in 70–80 (`RSI 71`; tooltip: monthly >70 often continuation) |
| Z vs 6M MA | \|z\| > 1.5 | `z +1.8` | ▲/▼ |
| Trend R² | R² ≥ 0.8 over 6–12M | `clean trend R² .91` | ▲ if slope+, ▼ if slope− |
| Drawdown | ≥ 15% off trailing peak | `−28% off peak` | ▼ (grey-red: state, not signal) |
| Tier-spread | compression trend ≥ threshold | `spread compressing` | ▼ |
| Grading-arb EV | EV > $0 (raw ≥ $40) | `arb EV +$38` | ▲ |
| Churn 30d | ≥ 2× churn 90d | `churn ×2.3` | ▲ |
| Monthly volume | top decile in corpus | `● Most active · 41 sales/30d` | ● grey |
| Monthly volume | ≤ 1 sale/30d | `● thin · 1 sale/30d` | ● grey |
| Amihud | ≥ 90th pct within set | `illiquid · 96th pct` | ● grey |
| Dispersion | σ/μ ≥ 0.20 trailing | `wide pricing ·24` | ● grey |
| Cross-mkt gap | \|gap\| ≥ 10%, venues at depth | `eBay −12% vs auction` | ● grey |
| Pop Δ 60d | ≥ +2% | `Pop Δ +5.1%` | – amber (supply caution; ≥ +5% also arms Supply-Flood composite) |
| Pop Δ 60d | ≤ 1% with churn firing | `pop flat` | ▲ |
| Gem rate drift | \|drift\| ≥ 0.3pp/90d | `gem rate −0.4pp` | ▲ if falling, ▼ if rising |
| Supply overhang | ≥ 3 years | `3.2y overhang` | ▼ |
| Composite match | card in preset/user screen today | `◆ Quiet Accum` | ◆ icon · thesis-colored (green bullish screen · red avoid screen) |

Not chip-eligible: Bollinger (visualization-grade, spec §8.3), beta (descriptive), discount-to-list (4.4% coverage), seasonality (corpus-locked until ~Nov 2028).
Priority when > cap: composites → RS → supply (pop/overhang) → momentum (ROC/MACD/EMA/RSI/z) → liquidity → the rest. Newest crossing wins ties.

Dated supersessions (2026-08-13, D-092) — **Card page panel only**; watchlist pills keep the
inventory readings until their own phase re-derives them:
- **RSI (6):** the panel fires caution `–` at **≥ 70** (value `overbought`) and positive ▲ at
  **≤ 30** (value `oversold`), quiet otherwise with the reading (`58`) — replacing the
  `> 80 / < 20` bands + amber 70–80 scheme on this surface. Floor: 7 closed months, Wilder
  smoothing, flat window reads 50.
- **Tier-spread:** the compression-only trigger is replaced. The row always shows the current
  PSA 10 / Grade 9 ratio (`×3.1`) and fires ▼ at ratio **≥ 4** or a **≥ 20% move in either
  direction** vs 6 closed months earlier; below-floor when either tier lacks the last closed
  month.
- **Monthly volume:** the Card page renders the always-on neutral `Sales volume` row
  (`{n} / 30d`, `(today−30, today]`); the top-decile `Most active` chip and the `thin` variant
  need corpus ranking, which doesn't exist yet.

### Tracked-pill states (watchlist rows) — the complete pill set
A tracked signal ALWAYS renders exactly one pill, in exactly one of five states; no other pill forms exist:
| State | Render | Example |
|---|---|---|
| Hit bullish | green ▲ · chip text from inventory | `▲ RS 94th` |
| Hit bearish | red ▼ · chip text from inventory | `▼ EMA 3/9` |
| Caution | amber – · evidence number (notable but directionless) | `– RSI 71` |
| Quiet | grey – · short name + `–` (tracked, between bands) | `– MACD –` |
| Pending | grey ◌ · short name + unlock ETA | `◌ Churn — 12d` |

- **Amber bands (complete list)**: RSI 70–80 · RS decile exit (80–89th within 3 mo of ≥90) · Pop Δ 60d ≥ +2%. No other signal has a caution band.
- **Liquidity/state signals** (volume, Amihud, dispersion, cross-market gap) are never directional: notable = ● grey + value, else quiet.
- **Pending ETA format**: days when under 60 (`— 12d`), month beyond (`— Mar ’27`); tooltip = the floor rule + the date history began.
- **Trackable set = the chip-eligible inventory above, exactly.** Bollinger, beta, discount-to-list, seasonality (not chip-eligible) and pure overlays (SMA) cannot be tracked.

