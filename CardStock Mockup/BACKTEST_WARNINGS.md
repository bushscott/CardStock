# Cardstock — Backtest warning checks

Warnings the real backtest engine should compute after every run. Each renders as a caution banner under the stat tiles (pattern established in the Screener prototype: red tint for risk warnings, amber for data-coverage limits, grey for aging notes).

## Implemented in prototype
1. **Concentration (set/era)** — flag when > 50% of buy signals come from one set or era. The screen may be finding one set's moment, not a repeatable pattern.
2. **Aging / maturity** — horizons with no aged entries are disabled, with the date the first cohort matures.
3. **Honest floor** — window is bounded by the youngest metric's data start; always shown, with the reason.

## To add in real app
4. **Concentration (character)** — same check keyed on character (e.g. all Charizards).
5. **Small sample** — fewer than ~10 aged entries at the selected horizon: stats shown but flagged as anecdote, not evidence.
6. **Mean >> median** — mean return more than ~2× median: a few big winners carry the screen; hit rate is the safer read.
7. **Single-winner dependence** — removing the best entry flips mean (or screen-vs-index verdict) negative.
8. **Entry clustering in time** — most entries within one or two adjacent months: the screen caught one market episode, not a recurring setup. Outcomes are correlated, not independent trials.
9. **Regime dependence** — all positive outcomes fall inside one broad market upswing; screen untested in a flat/down market. (Compare entry dates against index drawdown periods.)
10. **Thin liquidity at entry** — entries whose cards had < N sales/month at entry date: the simulated "buy" may not have been executable at the recorded price.
11. **Wide dispersion at entry** — entry price drawn from a month with high price dispersion: entry price itself is uncertain; returns inherit that error.
12. **Survivorship** — corpus only contains cards still tracked today; delisted/dead cards are missing, biasing results upward. (Structural — disclose always until corpus keeps dead cards.)
13. **Overlapping capital** — many entries open simultaneously: equity curve assumes you funded all of them; real returns depend on position sizing.
14. **Parameter fragility** — nudging a filter threshold ±10% changes entries by > ~40%: the screen is tuned to noise. (Expensive; run on demand, not every backtest.)
15. **Lookahead in composites** — composite signals (G1/G2/G4) must themselves be computed from point-in-time data; warn if any component metric was revised after the fact.

## Severity tiers
- **Red (risk)**: 1, 4, 6, 7, 8, 9 — the result may mislead.
- **Amber (data)**: 5, 10, 11, 12, 15 — the inputs are shaky.
- **Grey (mechanics)**: 2, 3, 13 — explains what the numbers can/can't say.
- **On-demand**: 14.
