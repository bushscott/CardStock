# Pokédex phase — design

**Date:** 2026-08-14 · **Status:** approved by the owner section-by-section in the brainstorm of
this date · **Roadmap position:** first phase after the Card page, per D-103 · **Ledger:** D-103
(roadmap), D-104 (species icon), D-105 (anglicized names), D-106 (scraper-side placement + all
sourcing rulings), D-107 (v2 register).

**Terminology rule for every document that follows:** "the scraper" = the sibling process
(`PokemonInvestBatch.Worker`, the systemd unit on the Pi). "The analytics worker" =
`CardStock.Worker`, a later phase. No bare "worker."

## 1. Shape

This phase produces **no CardStock code and no CardStock schema changes — none.** It is one
coherent piece of scraper-repo work: a new ADR there, migrations, a one-shot import, a new lane,
and tests — delivered per the D-079 precedent (handoff brief → subagent scoped to
`../PokemonInvestBatch`), and accepted on live receipts verified from this repo's side. CardStock's
read models over these tables arrive with the Catalog phase (Browse, Set, Character, About-data),
which is the next phase in D-103's order.

Why the scraper owns it (owner ruling, D-106): species tagging is derived from scraped titles —
"I consider this scraping." The tables live in the scraper's own schema, written only by it;
CardStock reads them through the SELECT grants it already has (D-065). No cross-schema writes
exist anywhere. This deliberately reverses D-069.10's "species is out of the sibling entirely."

## 2. Schema — seven new scraper-owned tables, one filesystem convention

Column lists are the design contract; exact types/names follow the scraper's conventions and are
the implementer's call where unstated.

| Table | Columns | Notes |
|---|---|---|
| `species` | `id` (PK = national dex number), `name`, `slug`, `generation`, `region`, `color`, `habitat` (**nullable** — Gen 4+ species have none), `status` (`ordinary`/`legendary`/`mythical`), `stage`, `evolves_from_species_id` (nullable self-FK), `gradient_start`, `gradient_end` | ~1,025 rows, base species only |
| `species_types` | `species_id` FK, `slot` (1–2), `type` | multi-valued, max 2 |
| `species_egg_groups` | `species_id` FK, `egg_group` | multi-valued, max 2; display names, not PokéAPI internal ids |
| `species_names` | `species_id` FK, `language`, `name` | the dataset's 12 languages incl. `ja`; imported now because it is free, consumed later |
| `card_species` | `card_id` FK → `cards.id`, `species_id` FK → `species.id`, `method` (`title-match`/`manual`) | the junction; PK (card_id, species_id); **manual rows are never machine-modified** |
| `card_tagging` | `card_id` PK/FK, `status` (`tagged`/`no-species`/`quarantined`), `method`, `tagged_name` (the title as matched), `updated_at` | **one row per card, always** — the owner's "all cards link to the Pokédex" rule, enforced and countable. A card with no row is by definition untagged work |
| `set_details` | `set_id` PK/FK, `code`, `released_on`, `series`, `era`, `match_status` (`matched`/`pending`) | **one row per set, always** (789); enrichment-precedent side table — no columns added to `sets` |

Filesystem: `species-icons/{dex}.png` beside the card-image corpus — one retro pixel menu sprite
per species. Missing icon ⇒ logged gap + the existing gradient-tile fallback convention.

Indexes the consumers need: `card_species(species_id, card_id)` (Character page direction),
`card_species(card_id)` (Card-page subline direction), plus the `card_tagging` anti-join support.

**Multi-valued filter semantics** (for the Catalog phase to honor): AND across attributes, OR
within one — a Grass/Poison species matches a type filter on either value.

## 3. The import (one-shot, idempotent)

A console command in the scraper repo, re-runnable (upsert by dex number).

- **Sources, vendored and pinned:** `PokeAPI/api-data` (BSD-3-Clause — verified live 2026-08-14)
  and `PokeAPI/sprites`, each pinned at a commit recorded in the scraper's ADR. The importer reads
  files off disk. **Zero network calls at runtime, zero payments, not an API at all.** Refresh for
  a future generation = bump the pin, re-run.
  - Rejected source: pokemondb.net — its About page says "Do not steal our content!", offers no
    export, recommends PokeAPI; its data also lacks color and habitat (verified live 2026-08-14).
    It remains the *style reference* for the icons only.
- **Universe:** base species through the pinned dataset (~1,025). Regional forms collapse to base
  species and need no matcher aliases — "Alolan Vulpix #21" contains "Vulpix". Paradox Pokémon
  are their own dex entries.
- **Authored maps, applied at import, totality enforced by tests:**
  - generation → region (9 rows, Kanto…Paldea), stored on `species`;
  - egg-group display names — prefer the dataset's own localized names arrays, ~15-row hand map
    as fallback (PokéAPI's internal id for Field is `ground`; display names are the contract);
  - `status` from `is_legendary` / `is_mythical` (Ordinary/Legendary/**Mythical** — the Browse
    filter vocabulary includes Mythical even though the prototype seed never showed it);
  - **evolution stage = chain depth from the chain root** (0 = Basic). This makes Umbreon
    "Stage 1 · evolves from Eevee" exactly as the Character chip renders. Pinned consequence:
    babies make Pikachu "Stage 1 · evolves from Pichu" — Pokédex-true, differs from TCG stage
    intuition, accepted deliberately (the chip is a Pokédex chip per the prototype's own tooltip).
- **Gradients:** seeded from an authored primary-type → two-stop palette map (~18 rows);
  hand-overridable per species later.
- **Icons:** from the pinned sprites clone, each species' newest available menu icon →
  `species-icons/{dex}.png`. Species with no icon in any generation: logged, gradient fallback.
- **Import receipts, printed:** species count vs the pinned dataset; every row 1–2 types;
  habitat null-rate consistent with the Gen 4+ share; zero unmapped egg groups; per-language name
  counts; icon count + named gaps.

## 4. The tagging lane

A new lane in the scraper (daily), beside its enrichment lane. The initial backfill is the same
code run once over the full corpus.

- **Work set per run:** cards with no `card_tagging` row (indexed anti-join), plus cards whose
  current `name` ≠ `card_tagging.tagged_name` — upstream title corrections re-tag automatically.
- **Matcher:**
  1. Normalize the title: strip the trailing collector token (`#65`) and bracket qualifiers
     (`[1st Edition]`, `[No Rarity]`); fold alias classes — `♀`/`♂`, the U+2011 hyphen family
     (`Chien‑Pao`), any curly-apostrophe variants.
  2. Scan against species names **longest first, word-boundary matched, consuming each matched
     span**. Consequences that are the point: Mewtwo never also yields Mew; `Mime Jr.` never
     yields Mr. Mime; "Pikachu & Zekrom GX" yields exactly {Pikachu, Zekrom}.
  3. English-only matching is safe corpus-wide: **51 of 91,646** active card names contain any
     non-ASCII character, all punctuation (D-105, re-runnable query in the ledger).
- **Denylist beats matches:** an enumerated, test-covered seed list of item-card patterns —
  titles ending in `Energy`, containing `Spirit Link`, `Poké Doll`, `Fossil`, and similar —
  forces `no-species` even when a species name appears ("Clefairy Doll", "Charizard Spirit
  Link"). Grows via spot-checks; lives in code with the tests.
- **Statuses:** ≥1 match → `tagged` + junction rows (`title-match`). Zero matches →
  `no-species` (trainers/energy/items — legitimate, countable). ≥4 species in one title, or a
  denylist-vs-match conflict → `quarantined` for review.
- **Manual override:** a console verb — `tag-card <id> <dex…>` / `tag-card <id> none` — writing
  `method = manual` junction rows and the status. The lane never modifies manual rows (tested).
- **Performance envelope** (owner asked; answered with numbers 2026-08-14): steady-state daily
  run is one anti-join (milliseconds) and usually zero work — new cards arrive on the scraper's
  7-day enumeration cadence. The one-time backfill is ~94M short substring checks (seconds of
  CPU) plus ~92k status + ~80k junction rows written in chunked transactions (~minutes). No
  network use — the lane cannot contend with the polite gate, which is the crawl's real
  bottleneck. Lighter than the hourly image sweep that already runs.

## 5. Set enrichment (same lane family)

Populate `set_details` from the scraper's existing TCGdex set mapping: code, release date,
series, and a series→era map. One row per set, always. Expected coverage at close: ~150 English
sets `matched`; the ~530 Japanese/Chinese/Korean/unmapped sets sit `pending` honestly, worked
down as an ongoing curation backlog (owner ruling in D-106; D-107 parks the AI Japanese-matching
idea that would collapse it). Coverage is reported per run, computed against the 789 denominator.

## 6. Tests and error handling

Scraper conventions throughout: one test project per source project, warnings as errors, the
Postgres 15 CI container for persistence tests.

- **Matcher trap fixture (hand-authored):** substring nests (Mew/Mewtwo, Kabuto/Kabutops,
  Porygon/Porygon2/-Z, the Nidoran family), punctuation (`♀/♂`, `Chien‑Pao`, `Farfetch'd`,
  `Mr. Mime` vs `Mime Jr.`, `Type: Null`, `Jangmo-o`/`Kommo-o`, `Tapu Koko`), multi-species
  titles, form/owner prefixes ("Alolan Vulpix", "Misty's Staryu", "Dark Charizard"), denylist
  cases, normalization cases.
- **Import totality tests:** unmapped egg group / type / region at import is a **hard, loud
  failure** — the parser's schema-drift posture, applied to reference data. Stage rule pinned on
  known chains (Eevee line, Pichu line).
- **Idempotency test:** everything run twice; the second run changes nothing.
- **Override protection test:** the lane may never alter `method = manual` rows.
- **Lane failure:** log and retry next run; never disturbs other lanes (ADR-0004 posture).

## 7. Acceptance — the phase closes on receipts, not on "done"

Verified live on the Pi from this repo's side (verify-everything, D-007), after backfill:

1. Invariant: zero cards without a `card_tagging` row; zero sets without a `set_details` row.
2. Coverage split reported over the full denominators (91,646 cards; 789 sets) — computed, never
   predicted.
3. A 100-card random sample printed (title → assigned species/status) for eyeball review with the
   owner; the quarantine list reviewed the same way.
4. `species` complete against the pinned dataset; icon count with named gaps.
5. One Character-page-shaped smoke query (Umbreon's printings via the junction) returns sane rows.

## 8. Delivery

1. This spec + ledger entries (already landed) + the character.md spec note (D-104) — this repo.
2. A handoff brief in `docs/superpowers/handoffs/` scoping the scraper-side work.
3. A subagent scoped to `../PokemonInvestBatch` implements: its own ADR, migrations, importer,
   lane, console verb, tests, deploy.
4. Acceptance receipts (§7) verified; phase closed in the ledger; the **Catalog phase** brainstorm
   is next.

## 9. Non-goals, explicit

- **No UI.** Browse/Set/Character/About-data are the Catalog phase.
- **No TCGdex in the tagger** (owner ruling; plain title matching suffices — revisit only if
  spot-checks fail).
- **No art-cameo tagging** — named-species rule stands; the Character page's footer copy gets
  reworded to match in the Catalog phase.
- **No localized display** — `species_names` is stored, not yet consumed.
- **No up-front non-English set curation** — pending states + backlog (D-106); AI matching is
  v2 (D-107).
- **No CardStock schema changes, none** — repeated because it is the phase's most unusual
  property.
