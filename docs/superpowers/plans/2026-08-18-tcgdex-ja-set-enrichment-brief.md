# Handoff brief — TCGdex `ja`-locale set enrichment (implements in `../PokemonInvestBatch`)

**Origin:** owner ask, 2026-08-18, during Catalog UAT — the release-date wall shows
"622 sets awaiting metadata," and the owner asked whether the fix is manual entry or a
cross-reference. It is the cross-reference: this brief extends the existing TCGdex join
(their ADR-0009) to the Japanese shelf. Lineage: D-079 (the join itself), D-105 (the gap
statement), D-116 (this brief). **Delivery per the D-079/D-106 precedent:** implemented in
the sibling repo from a fresh context window; acceptance re-run from CardStock before the
ledger closes it.

## What a set match fills in — verified against the entity, not assumed

`set_details` (`PokedexEntities.cs:107-124`) carries exactly: `MatchStatus`, `Code`
(TCGdex set id verbatim), `ReleasedOn`, `Series`, `Era` (from the curated
`tcgdex-series-eras.json`, ADR-0011). So a matched Japanese set gains **code, release
date, series, and era** — which in CardStock moves it off the pending shelf and into the
date wall and era shelves with zero CardStock code changes. There are **no card-count
columns on `set_details`**; per-card data (collector numbers etc.) is `tcgdex_enrichments`
and is **stage 2 below, a separate decision**.

## Today's numbers — all measured 2026-08-18

- 789 sets; **167 matched**, all via the English locale — the mirror fetches `/v2/en`
  only (`TcgdexMirror.cs:155,178`).
- **622 pending = 395 Japanese + 139 Chinese/Korean + 88 other** (side products TCGdex
  does not carry). Query:
  `SELECT count(*) FILTER (WHERE s.name ILIKE '%japanese%'), count(*) FILTER (WHERE s.name ILIKE '%chinese%' OR s.name ILIKE '%korean%'), count(*) FROM sets s LEFT JOIN set_details d ON d.set_id = s.id AND d.match_status = 0 WHERE d.set_id IS NULL;`
- TCGdex `ja` locale, probed live: **177 sets**, names in Japanese script
  (`GET api.tcgdex.net/v2/ja/sets` → `PMCG1 拡張パック` …). Per-set documents carry what
  we need: `GET /v2/ja/sets/SV2a` → `releaseDate 2023-06-16`, `serie ポケモンカードゲーム
  スカーレット&バイオレット`, `cardCount official 165 / total 210`.
- **Ceiling, stated plainly: at most 177 of the 395 can ever match.** PriceCharting's
  Japanese shelf over-counts TCGdex's (promos, subsets, box products). Stage 0 measures
  the real overlap before anyone promises a number.

## Design constraints — read from the existing code, binding

1. **The Japanese exclusion is deliberate, not a bug.** `SetMapping.cs` partitions
   non-English sets away from name matching so "Pokemon Korean Scarlet & Violet 151"
   never meets TCGdex's "151" — *"a near-miss here writes wrong-but-plausible data (the
   exact failure this project's rules exist to prevent)."* The extension keeps that rule:
   **no EN↔JA fuzzy name matching, ever.** The `ja` join is a **hand-curated alias map**
   (PriceCharting slug → TCGdex `ja` set id), bounded at ≤177 rows, same `Mapped` shape
   the English side's `TcgdexSetAliases` uses. AI-assisted alias generation stays parked
   in the v2 register (D-107) — not this brief.
2. **Mirror architecture stands:** extend the pinned filesystem mirror
   (`DATA_MODEL.md` §3.10) with the `ja` documents — fetched once, delete-to-repin.
3. **`tcgdex-series-eras.json` gains the `ja` serie names** (ordinal-exact keys, per its
   contract) so `Era` fills; a missing key stays a null era on a Matched row, as today.
4. `MatchStatus` semantics unchanged; every unmapped Japanese set keeps its honest
   Unmapped verdict. No set may gain a date TCGdex does not state.
5. House rules: warnings-as-errors, tests per existing patterns, ADR if the implementer
   judges this extends ADR-0009's stated posture rather than merely exercising it.

## Stages

- **Stage 0 — probe (cheap, do first):** pull the `ja` list into the mirror; hand-map the
  recognizable modern sets (SV/S series transliterate legibly); report the true
  overlap count and the alias-curation effort for the rest. Decision point with the owner.
- **Stage 1 — the join:** ja mirror + partition-scoped mapped-join path + era rows +
  `set_details` sweep over the alias table.
- **Stage 2 — per-card enrichment for mapped ja sets (separate decision):** the
  number-driven join can work (`localId "001"` ↔ "#1", latin digits both sides), but
  `CardNameAgreement`'s guard cannot (EN names vs Japanese script). Suggested replacement
  guard: species agreement — TCGdex `dexId` vs the Pokédex substrate's species tag for
  the PriceCharting card. Implementer designs; nothing ships unguarded.

## Acceptance — re-run from the CardStock side before closing

1. Pending count drops 622 → 622−N with N reported per partition (only Japanese moves).
2. `/browse` release-date wall shows mapped Japanese sets under their TCGdex dates; era
   shelves gain them; the pending tail shrinks by exactly N.
3. Spot receipts: three mapped sets' `ReleasedOn`/`Era`/`Code` equal their TCGdex `ja`
   documents, read live.
4. CardStock changed zero lines.

## Out of scope

Chinese/Korean locales (TCGdex coverage unmeasured — separate probe), the ~88 side
products (no TCGdex counterpart exists; they stay honestly pending or become a manual
decision later), AI alias generation (v2 register).
