# Handoff — TCGdex metadata enrichment, to be built in PokemonInvestBatch

**From:** the CardStock repo's Phase 2 session, 2026-08-12.
**To:** a session working in `/Users/scott/RiderProjects/PokemonInvestBatch`, and only there — nothing in this task touches the CardStock repo.
**Owner decision trail:** CardStock `DECISIONS.md` D-079 (raised and researched 2026-08-12) and D-084-era discussion the same day. The owner has said: enrichment belongs in the batch repo, not in CardStock.
**Receipts appendix:** `2026-08-12-tcgdex-enrichment-research.txt` beside this file — the full research report with a `src:`/`q:` quote for every fact below. Treat the numbers as claims verified by that research session; re-verify what you build on, per house rules.

## The mission

The CardStock Card page renders a subline of the form `{set name} · 215/203`. Two fields in it do not exist anywhere in this repo's schema (`DATA_MODEL.md` §3.1–3.2):

1. **Collector number** per card (`215`) — today it exists only embedded in `cards.name` (`"Umbreon VMAX #215"`, `"Charizard [Shadowless] #4"`). CardStock parses it out at render as a stopgap.
2. **Official set size** per set (`203`, the denominator). Note secret cards are numbered *past* it — `215/203` is normal, not an error.

Design and implement enrichment of these from **TCGdex** (https://tcgdex.dev), as a batch process in this repo, following this repo's own conventions (ADR for the decision, this repo's planning process, owner approval before build). The recommended shape below is the research session's conclusion, not a mandate.

## Why TCGdex, and not pokemontcg.io — settled, with receipts

- **TCGdex answers both fields directly.** `GET api.tcgdex.net/v2/en/cards/swsh7-215` → `localId: "215"`, `set.cardCount: { official: 203, total: 237 }`. Re-verified live twice on 2026-08-12, once from each session. (The same endpoint also carries species `dexId`s — explicitly out of scope; see the final section.)
- **License is plain MIT** (`tcgdex/cards-database`): permanent Postgres storage, modification, and commercial use expressly permitted; only obligation is notice preservation. (MIT covers their compilation, not Pokémon IP — same non-affiliation posture this project already carries.)
- **Self-hostable on the Pi:** `tcgdex/server` Docker images ship `linux/arm64`. Alternatively the per-set JSON for `/en` is ~218 cacheable requests — a one-shot mirror is small. Evaluate pin/mirror/self-host versus a live dependency: TCGdex is community-maintained and **coverage lags brand-new sets** (PriceCharting already lists 2026 sets like "Pokemon Phantasmal Flames").
- **pokemontcg.io was evaluated and rejected** for permanent columns: its bulk-data repo has **no license** (GitHub `license: null`, no LICENSE file), its ToS is silent on storing/redistributing data while making access terminable at will, and its live API returned 500/502 on most probes while its `/v2/sets` worked — all on 2026-08-12. Wrong foundation for a permanent enrichment column, fine as a cross-check at most.

## The join, as actually executed (not hypothesized)

**Number-driven, with name as a confirmation gate.** PriceCharting embeds the collector number in the card name and URL slug; TCGdex's `localId` matches it verbatim. An executed join on two sampled 150-product pages matched **283/283 numbered products** (Evolving Skies 139/139 vs `swsh7`, Base Set 144/144 vs `base1`) with ~97% exact name agreement; every mismatch fell in known synonym classes (Electric/Lightning Energy, Dark/Darkness, Steel/Metal, gender symbols, `é`, VSTAR casing).

**Set mapping is the real work.** ~124 of PC's ~300 distinct set names exact-match TCGdex `/en` after normalization (strip leading `Pokemon `, casefold, accent-fold, unify `&`/`and`, strip punctuation); ~20 more need a one-time hand-alias table (151, Pokémon GO, Expedition Base Set, McDonald's Collection YYYY, …); the remaining ~157 (127 Japanese, 15 Chinese, 5 Korean, 10 Topps, plus Burger King/Oreo-style products) **do not auto-join** — TCGdex serves Japanese sets only under its `ja` locale with Japanese-script names (`/en/S6a` → 404).

**Routing rules that were verified to work:**
- **TG/GG gallery cards:** PC folds them into the parent set as `#TG23`; TCGdex splits them into sibling sets (`swsh9.5tg`, `swsh12.5gg`) whose `localId`s carry the same prefix → route by number prefix.
- **Promos:** PC's one `Pokemon Promo` grab-bag maps onto 13 TCGdex per-era promo sets, routed by number prefix (`SWSH262`→`swshp`, `XY124`→`xyp`, `SM191`→`smp`). Bare-numbered modern promos (`#53`, `#44`) collide across eras — leave unmatched rather than guess.
- **Variants:** PC lists `[1st Edition]` / `[Shadowless]` / `[Reverse Holo]` as separate products; TCGdex models one card with variant flags. The mapping is deliberately **N:1** — number, set size, and species are identical across variants, so all PC variant products inherit the same enrichment; `tcgdex_card_id` must not be treated as unique per product.
- **Number normalization:** uppercase, split alpha prefix from digits, strip leading zeros (`svp` uses `001`); `localId` can be non-numeric (`TG23`) → **text column, never int**.

**Known breakers (leave unmatched or special-case; never force):**
- Celebrations Classic Collection: PC `#4` vs TCGdex `CC002` — the number join fails outright (25 cards).
- Sealed products carry no `#` → they self-select out; give them an explicit non-card status and **exclude them from every coverage denominator** (CardStock's D-061 discipline: authored denominators, never hand-waved).
- Fuzzy matching must **hard-exclude the japanese/chinese/korean/topps slug prefixes first**, or "Pokemon Korean Scarlet & Violet 151" happily matches TCGdex "151" and silently enriches Korean cards with English-set data — wrong-but-plausible, the exact failure class this project's rules exist to prevent.
- This repo's own documented TODO: `cards.set_id` is never updated when a card moves sets — a small fraction of joins will route through a stale set.

## Recommended shape (research conclusion — your ADR decides)

Two-phase batch join in the worker:

- **Phase A — set mapping** (one-time, ~300 rows, human-reviewed): partition PC sets by slug prefix, non-English/Topps → `UNMAPPED`, never fuzzy-matched; normalize and exact-match the rest; hand-alias the ~20 stragglers; attach sibling-set routes (parent → tg/gg/shiny-vault siblings; `celebrations` → `{cel25, cel25cc, swshp}`; `pokemon-promo` → the 13 promo sets).
- **Phase B — card join**: parse `^(base)\s*(\[tag\])?\s*(#num)?$` from `cards.name`; no number → `NO_NUMBER` (sealed; out of denominators); route by prefix, look up `localId`, then **confirm by name** with the synonym whitelist. Agree → `CONFIRMED`, write `{card_number, set_official_size}`. Disagree → `NAME_MISMATCH`, **no write**, review queue.
- **Storage:** enrichment in its own table keyed by the PriceCharting product id, with `tcgdex_card_id`, `match_status`, `matched_at` — unmatched is a first-class state (mirrors CardStock's LOW DATA discipline), re-runs idempotent.
- **Before committing to scope:** run one SQL against this repo's DB — card counts grouped by the slug-prefix partition — to turn set-level ~48% into a real card-level coverage number. Expect near-100% match within mapped English sets; overall could be anywhere from ~45% to ~75% of 91,596 cards. Treat Japanese sets as a deliberate later phase with a curated ~127-row alias table, not a fuzzy matcher.

**This is the first enrichment, not the only one.** The owner, same day: *"this will not be the last
data enrichment that we're gonna come across."* Let the ADR treat this build as the first instance of a
pattern — an enrichment lane with per-source provenance and an explicit match status — and record that
expectation. Do **not** generalize the code for sources that don't exist yet; both repos' rule is that
expectations get recorded, not speculatively built (CardStock `CLAUDE.md`: "Expectations are not
constraints").

## What the consumer (CardStock) needs from the result

- The two fields above, plus the **explicit per-card `match_status`** — CardStock renders unmatched cards honestly (its five-state vocabulary) and must be able to tell "no match" from "not yet attempted".
- Read access in the same manner as the existing eight tables — CardStock reads, never writes (its D-026 posture). Record the final shape in `DATA_MODEL.md` so CardStock can cite it; CardStock's Card page then swaps its name-parse stopgap for the real columns.
- Nothing in CardStock blocks on this. No deadline pressure from that side.

## Explicitly out of scope — do not revisit

**Images.** Owner settled 2026-08-12, recorded in this repo's `DATA_MODEL.md` §2 (the image-hash item): TCGdex was evaluated and **rejected as an image source** because it keeps one image per card, which cannot distinguish a holo from a non-holo printing — separate products with separate prices everywhere in this store. Images continue to come from the site's own CDN pipeline, no exceptions. This handoff adds metadata columns only. It also does not touch the intake API — enrichment is a batch lane, not an intake concern.

**Species.** Owner ruled 2026-08-12: species is **removed from this enrichment altogether** — *"in another phase, we will have to create a Pokédex, and it will belong in there."* Do not add `dexId` columns, species tables, or any number→name resolution in this build, even though the field arrives free on the same API response. The future Pokédex phase owns all of it.
