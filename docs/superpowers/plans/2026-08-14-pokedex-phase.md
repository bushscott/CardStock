# Pokédex Phase Implementation Plan (scraper repo)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Pokédex to the scraper — a species table imported from a pinned PokéAPI dataset, retro pixel icons, a title-matching lane that links every card to the species it names, and set metadata — all scraper-owned, read by CardStock.

**Architecture:** Everything lives in `../PokemonInvestBatch` (from CardStock's checkout; this plan's paths are relative to the PokemonInvestBatch repo root). Pure matching/parsing logic goes in `Application/Pokedex` (functional core, ADR-0003); persistence and mirror fetching in `Infrastructure`; one new `BackgroundService` lane in `Worker/Lanes` that self-bootstraps exactly like `EnrichmentLane` (first sweep fetches mirrors, imports species, backfills tags; later sweeps are incremental and usually no-ops).

**Tech Stack:** .NET 10, EF Core + Npgsql with `UseSnakeCaseNamingConvention()`, Postgres 15, xunit. No new NuGet packages.

**Spec:** `../CardStock/docs/superpowers/specs/2026-08-14-pokedex-phase-design.md` — read it first; it carries the why and the acceptance receipts. CardStock ledger context: D-103–D-107 in `../CardStock/DECISIONS.md`.

## Global Constraints

- **Repo conventions are law:** `TreatWarningsAsErrors=true`, file-scoped namespaces, XML doc comments on public members explaining *why*, 4-space C#, one test project per source project.
- **ADR before code** (repo convention): the phase's ADR is Task 1 and every later commit may cite it.
- **The polite gate is never touched.** All network fetches use dedicated `HttpClient`s against GitHub hosts, like `ImageLane`/`EnrichmentLane`.
- **Tables:** exact names `species`, `species_types`, `species_egg_groups`, `species_names`, `card_species`, `card_tagging`, `set_details` (snake_case falls out of DbSet property names — verify in the generated migration).
- **`card_species`/`card_tagging` are current-state, not append-only** — a deliberate, ADR-documented deviation (they are derived, rebuildable state, not observations). `method = Manual` rows are never machine-modified.
- **Manual overrides are operator SQL** (ADR-0002 precedent, manual-only delisting) — documented statements, no console verb.
- **English-only matching** is safe corpus-wide (D-105: 51/91,646 non-ASCII names, all punctuation) — but the normalizer must fold diacritics both ways (titles write "Flabebe", the dataset writes "Flabébé").
- **Not-a-card rows** (`not_a_card_at IS NOT NULL`) are excluded from tagging (a "Pokémon Pikachu" handheld must not tag Pikachu); every other card gets a `card_tagging` row.
- Copy rule: sign glyphs, dates, and user copy are CardStock's problem — nothing in this phase renders UI.

---

### Task 1: ADR-0011 and configuration

**Files:**
- Create: `docs/adr/0011-the-pokedex-lives-in-the-scraper.md`
- Modify: `docs/adr/README.md` (append index row)
- Modify: `src/PokemonInvestBatch.Worker/ScraperOptions.cs` (append properties)
- Modify: `src/PokemonInvestBatch.Worker/Program.cs` (options validation only, in the existing `AddOptions<ScraperOptions>()` chain)

**Interfaces:**
- Produces: `ScraperOptions.PokeapiDataBaseUrl`, `PokeapiSpritesBaseUrl`, `PokeapiDataPin`, `PokeapiSpritesPin`, `PokedexMirrorDirectory` (default `"pokeapi-mirror"`), `SpeciesIconDirectory` (default `"species-icons"`), `PokedexTaggingIntervalHours` (default `24`), `TcgdexSeriesEraPath` (default `"tcgdex-series-eras.json"` — reserved, unused until Task 10).

- [x] **Step 1: Write the ADR** — Nygard format, matching the tone of `0009-tcgdex-metadata-enrichment.md`. It must state: context (CardStock's Character/Browse surfaces need species data; owner ruled tagging is scraping — CardStock D-106 reversed D-069.10); the decision (seven scraper-owned tables, pinned PokéAPI dataset + sprites as local mirrors fetched once, longest-match-first title tagging, named-species rule — art cameos are untaggable); the two deviations and why (**current-state tables** because they are derived and rebuildable, unlike observation history; **targeted `DELETE`/`UPDATE` grants** on exactly these tables for `pokemon_app`, since re-tagging after a title correction must remove stale junction rows); manual overrides are operator SQL like delisting; and the pins (record the exact commit SHAs chosen in Step 2).
- [x] **Step 2: Pick and record the pins.** Browse GitHub for the current default-branch commit SHA of `PokeAPI/api-data` and `PokeAPI/sprites` (e.g. `git ls-remote https://github.com/PokeAPI/api-data.git HEAD`). Put both SHAs in the ADR and as the option defaults.
- [x] **Step 3: Append options** to `ScraperOptions.cs`, doc-commented in the file's style:

```csharp
    /// <summary>Raw-content base for the pinned PokéAPI dataset (ADR-0011).
    /// The pin is the path segment — bump it to refresh, then delete the
    /// mirror directory.</summary>
    public string PokeapiDataBaseUrl { get; init; } =
        "https://raw.githubusercontent.com/PokeAPI/api-data/";

    public string PokeapiSpritesBaseUrl { get; init; } =
        "https://raw.githubusercontent.com/PokeAPI/sprites/";

    /// <summary>Commit SHA of PokeAPI/api-data this Pokédex was built from.</summary>
    public string PokeapiDataPin { get; init; } = "<SHA from Step 2>";

    /// <summary>Commit SHA of PokeAPI/sprites the icons come from.</summary>
    public string PokeapiSpritesPin { get; init; } = "<SHA from Step 2>";

    public string PokedexMirrorDirectory { get; init; } = "pokeapi-mirror";

    public string SpeciesIconDirectory { get; init; } = "species-icons";

    public int PokedexTaggingIntervalHours { get; init; } = 24;
```

(The literal `"<SHA from Step 2>"` placeholders are filled with the real SHAs in this same step — they must never survive to a commit.)
- [x] **Step 4: Validate** in `Program.cs`, appended to the existing chain: `.Validate(o => o.PokedexTaggingIntervalHours >= 1, "Scraper:PokedexTaggingIntervalHours must be at least 1.")` and non-empty checks for both pins.
- [x] **Step 5: Build, then commit** — `dotnet build` (warnings are errors); `git add docs/adr src/PokemonInvestBatch.Worker && git commit -m "ADR-0011: the Pokédex lives in the scraper"`.

---

### Task 2: Entities, DbContext, migration

**Files:**
- Create: `src/PokemonInvestBatch.Infrastructure/Persistence/PokedexEntities.cs`
- Create: `src/PokemonInvestBatch.Application/Pokedex/TagEnums.cs`
- Modify: `src/PokemonInvestBatch.Infrastructure/Persistence/PokemonDbContext.cs`
- Create (generated): `src/PokemonInvestBatch.Infrastructure/Persistence/Migrations/*_AddPokedex.cs`
- Test: `tests/PokemonInvestBatch.Infrastructure.Tests/Persistence/PokedexPersistenceTests.cs`

**Interfaces:**
- Produces: entities `Species`, `SpeciesType`, `SpeciesEggGroup`, `SpeciesName`, `CardSpeciesLink`, `CardTagging`, `SetDetail`; enums `TagStatus { Tagged, NoSpecies, Quarantined }`, `TagMethod { TitleMatch, Manual }`, `SetMatchStatus { Matched, Pending }` (all `: short`, values appended-only like `VisitOutcome`); DbSets `SpeciesRows` (→ table `species` via explicit `ToTable`), `SpeciesTypes`, `SpeciesEggGroups`, `SpeciesNames`, `CardSpecies`, `CardTagging`, `SetDetails`.

- [x] **Step 1: Write the enums** in `TagEnums.cs` (Application, beside `TcgdexMatchStatus`), each doc-commented; `TagStatus.NoSpecies` comment states it covers trainers/energy/items and is a legitimate terminal state, not a failure.
- [x] **Step 2: Write the entities**, mirroring `Entities.cs` style. The contract:

```csharp
/// <summary>One Pokédex species (ADR-0011). PK is the national dex number,
/// never generated locally — same posture as Card.Id.</summary>
public class Species
{
    public int Id { get; set; }                       // national dex number
    public required string Name { get; set; }         // English display name ("Nidoran♀")
    public required string Slug { get; set; }         // route-safe ("nidoran-f")
    public short Generation { get; set; }
    public required string Region { get; set; }       // derived, stored ("Johto")
    public required string Color { get; set; }
    public string? Habitat { get; set; }              // null for Gen 4+
    public SpeciesStatus Status { get; set; }         // Ordinary/Legendary/Mythical (: short)
    public short Stage { get; set; }                  // chain depth from root; 0 = basic
    public int? EvolvesFromSpeciesId { get; set; }
    public required string GradientStart { get; set; } // "#RRGGBB"
    public required string GradientEnd { get; set; }
}
public class SpeciesType     { public int SpeciesId; public short Slot; public required string Type; }      // as properties
public class SpeciesEggGroup { public int SpeciesId; public required string EggGroup; }                      // as properties
public class SpeciesName     { public int SpeciesId; public required string Language; public required string Name; } // as properties
/// <summary>Card ↔ species junction. Current-state (ADR-0011 deviation).</summary>
public class CardSpeciesLink { public long CardId; public int SpeciesId; public TagMethod Method; }          // as properties
/// <summary>One row per taggable card, always — "no row" means "not yet
/// attempted", which is what the lane's anti-join hunts.</summary>
public class CardTagging
{
    public long CardId { get; set; }
    public TagStatus Status { get; set; }
    public TagMethod Method { get; set; }
    public required string TaggedName { get; set; }   // the exact title matched — rename detector
    public DateTimeOffset UpdatedAt { get; set; }
}
public class SetDetail
{
    public long SetId { get; set; }
    public SetMatchStatus MatchStatus { get; set; }
    public string? Code { get; set; }                 // TCGdex set id verbatim ("swsh7") — display formatting is CardStock's job
    public DateOnly? ReleasedOn { get; set; }
    public string? Series { get; set; }
    public string? Era { get; set; }                  // one of the 8 product eras, or null
}
```

(Write the compact one-line classes as normal `{ get; set; }` properties — the shorthand above is layout, not field syntax. Add `SpeciesStatus { Ordinary, Legendary, Mythical } : short` to `TagEnums.cs`.)
- [x] **Step 3: DbContext config** in `OnModelCreating`, matching existing style: `Species` → `ToTable("species")`, `Id` `ValueGeneratedNever()`, unique index on `Slug`, max lengths (Name/Slug 200, Region/Color/Habitat 24, Gradient 7); `SpeciesType` PK `(SpeciesId, Slot)`, Type max 16; `SpeciesEggGroup` PK `(SpeciesId, EggGroup)`, EggGroup max 24; `SpeciesName` PK `(SpeciesId, Language)`, Language max 12, Name max 200; `CardSpeciesLink` → `ToTable("card_species")`, PK `(CardId, SpeciesId)`, FK → cards Restrict, FK → species Restrict, **extra index `(SpeciesId, CardId)`** (the Character-page direction); `CardTagging` → `ToTable("card_tagging")`, PK `CardId`, FK → cards Restrict, TaggedName max 300; `SetDetail` PK `SetId`, FK → sets Restrict, Code max 32, Series max 100, Era max 24. All species-side FKs from `SpeciesType`/`SpeciesEggGroup`/`SpeciesName` → species Restrict.
- [x] **Step 4: Generate the migration** — `dotnet ef migrations add AddPokedex --project src/PokemonInvestBatch.Infrastructure --startup-project src/PokemonInvestBatch.Worker`. Open it and verify table names are exactly the seven in Global Constraints.
- [x] **Step 5: Write the failing persistence test** in `PokedexPersistenceTests.cs`, using the same throwaway-database harness as the existing Infrastructure tests (see `tests/PokemonInvestBatch.TestSupport`): insert a `Species` (with types/egg groups/names), a `Card` + `CardSpeciesLink` + `CardTagging`, a `SetDetail`; read them back; assert round-trip equality and that inserting a duplicate `(CardId, SpeciesId)` link throws.
- [x] **Step 6: Run** `dotnet test tests/PokemonInvestBatch.Infrastructure.Tests --filter PokedexPersistence` — expect fail before the migration is applied by the harness, pass after.
- [x] **Step 7: Commit** — `git commit -m "Pokedex schema: seven tables (ADR-0011)"`.

---

### Task 3: Title normalizer (pure)

**Files:**
- Create: `src/PokemonInvestBatch.Application/Pokedex/TitleNormalizer.cs`
- Test: `tests/PokemonInvestBatch.Application.Tests/Pokedex/TitleNormalizerTests.cs`

**Interfaces:**
- Produces: `static string TitleNormalizer.Normalize(string title)` — used by the matcher on **both** card titles and species names.

- [x] **Step 1: Write the failing tests** — the contract as data:

```csharp
[Theory]
[InlineData("Charizard [1st Edition] #4", "charizard")]
[InlineData("Aipom [No Rarity] #67", "aipom")]
[InlineData("Umbreon VMAX (Alt Art) #215", "umbreon vmax (alt art)")]
[InlineData("Chien‑Pao #32", "chien-pao")]                    // U+2011 → '-'
[InlineData("Farfetch’d #27", "farfetch'd")]                  // curly → straight apostrophe
[InlineData("Flabébé #83", "flabebe")]                        // diacritics folded
[InlineData("Nidoran♀ #25", "nidoran♀")]                      // gender glyphs PRESERVED
[InlineData("  Pikachu   &  Zekrom GX  #33", "pikachu & zekrom gx")] // whitespace collapsed
public void Normalizes(string title, string expected)
    => Assert.Equal(expected, TitleNormalizer.Normalize(title));
```

- [x] **Step 2: Run to verify failure** — `dotnet test tests/PokemonInvestBatch.Application.Tests --filter TitleNormalizer` → FAIL (type not found).
- [x] **Step 3: Implement**: lowercase (invariant); strip one trailing `#<token>` (regex `\s*#\S+\s*$`); remove `[...]` groups; map U+2010/2011/2012/2013 → `-`, U+2018/2019 → `'`; fold diacritics via `string.Normalize(NormalizationForm.FormD)` dropping `NonSpacingMark` — **but pass `♀` (U+2640) and `♂` (U+2642) through untouched**; collapse runs of whitespace to one space; trim.
- [x] **Step 4: Run to verify pass.**
- [x] **Step 5: Commit** — `"Pokedex: title normalizer"`.

---

### Task 4: Denylist (pure)

**Files:**
- Create: `src/PokemonInvestBatch.Application/Pokedex/ItemCardDenylist.cs`
- Test: `tests/PokemonInvestBatch.Application.Tests/Pokedex/ItemCardDenylistTests.cs`

**Interfaces:**
- Produces: `static bool ItemCardDenylist.IsItemCard(string normalizedTitle)` — called with `TitleNormalizer.Normalize` output.

- [x] **Step 1: Failing tests**: `"charizard spirit link #75"`→normalized→true; `"clefairy doll #70"`→true; `"growing grass energy #104"`→true; `"dome fossil #155"`→true; `"lillie's poke doll #197"`→true; and the guard rail — `"charizard [1st edition] #4"`→false, `"flareon #13"`→false (no species name contains any denylist term; assert that with a loop once the species fixture exists in Task 6 — here, just the literals).
- [x] **Step 2: Run** → FAIL.
- [x] **Step 3: Implement**: normalized-substring rules — ends with `" energy"` or equals `"energy"`; contains `"spirit link"`, `" doll"`, `" fossil"`, `"poke ball"`, `"'s pokedex"`. Keep the list a `private static readonly string[]` pair (suffixes, substrings) with a doc comment saying it grows via quarantine spot-checks (spec §4).
- [x] **Step 4: Run** → PASS. **Step 5: Commit** — `"Pokedex: item-card denylist"`.

---

### Task 5: Species matcher (pure — the core)

**Files:**
- Create: `src/PokemonInvestBatch.Application/Pokedex/SpeciesMatcher.cs`
- Test: `tests/PokemonInvestBatch.Application.Tests/Pokedex/SpeciesMatcherTests.cs`

**Interfaces:**
- Consumes: `TitleNormalizer.Normalize`, `ItemCardDenylist.IsItemCard`.
- Produces:

```csharp
public sealed record TagVerdict(TagStatus Status, IReadOnlyList<int> SpeciesIds);
public static class SpeciesMatcher
{
    /// <summary>candidates: (normalized name, species id), pre-sorted by name
    /// length descending by the caller (BuildCandidates does this).</summary>
    public static TagVerdict Match(string rawTitle, IReadOnlyList<(string Name, int SpeciesId)> candidates);
    public static IReadOnlyList<(string Name, int SpeciesId)> BuildCandidates(IEnumerable<(int Id, string EnglishName)> species);
}
```

- [x] **Step 1: Failing trap-fixture tests** (spec §6 verbatim — every family):

```csharp
// fixture species: (25,"Pikachu")(26,"Raichu")(172,"Pichu")(150,"Mewtwo")(151,"Mew")
// (140,"Kabuto")(141,"Kabutops")(137,"Porygon")(233,"Porygon2")(474,"Porygon-Z")
// (29,"Nidoran♀")(32,"Nidoran♂")(30,"Nidorina")(33,"Nidorino")(83,"Farfetch'd")
// (122,"Mr. Mime")(439,"Mime Jr.")(772,"Type: Null")(37,"Vulpix")(197,"Umbreon")
// (644,"Zekrom")(120,"Staryu")(6,"Charizard")(35,"Clefairy")(669,"Flabébé")(1002,"Chien-Pao")
[Theory]
[InlineData("Mewtwo #10", new[] { 150 })]                       // never also Mew
[InlineData("Mew #8", new[] { 151 })]
[InlineData("Kabutops #141", new[] { 141 })]
[InlineData("Porygon2 #233", new[] { 233 })]
[InlineData("Porygon-Z [Holo] #474", new[] { 474 })]
[InlineData("Nidoran♀ #25", new[] { 29 })]
[InlineData("Nidoran♂ [No Rarity] #32", new[] { 32 })]
[InlineData("Mime Jr. #439", new[] { 439 })]                    // never Mr. Mime
[InlineData("Mr. Mime #122", new[] { 122 })]
[InlineData("Type: Null #772", new[] { 772 })]
[InlineData("Farfetch’d #27", new[] { 83 })]
[InlineData("Flabebe #83", new[] { 669 })]                      // title anglicized, dataset accented
[InlineData("Chien‑Pao #32", new[] { 1002 })]
[InlineData("Alolan Vulpix #21", new[] { 37 })]                 // form prefix
[InlineData("Misty's Staryu #26", new[] { 120 })]               // owner prefix
[InlineData("Dark Charizard #4", new[] { 6 })]
[InlineData("Pikachu & Zekrom GX #33", new[] { 25, 644 })]      // multi-species, both
public void Tags(string title, int[] expected) { /* assert Status=Tagged and ids set-equal */ }

[Theory]
[InlineData("Professor Oak #88")]
[InlineData("Rare Candy #85")]
[InlineData("Charizard Spirit Link #75")]                        // denylist beats the match
[InlineData("Clefairy Doll #70")]
[InlineData("Growing Grass Energy #104")]
public void NoSpecies(string title) { /* assert Status=NoSpecies, empty ids */ }
```

Plus one quarantine test: a synthetic title naming five fixture species → `Status=Quarantined` with the ids preserved for review.
- [x] **Step 2: Run** → FAIL. **Step 3: Implement**: normalize title; if `IsItemCard` → `NoSpecies`; else scan candidates in given order — for each, repeated case-sensitive `IndexOf` over the normalized title (both sides already normalized), accept only where both neighbors are word boundaries (`!char.IsLetterOrDigit`, with `♀`/`♂` counting as name characters, not boundaries), blank out accepted spans in a char buffer so consumed text can't re-match; distinct species ids in first-match order; 0 → `NoSpecies`, 1–3 → `Tagged`, ≥4 → `Quarantined`. `BuildCandidates` normalizes each English name and sorts length-descending, then ordinal.
- [x] **Step 4: Run** → PASS. **Step 5: Commit** — `"Pokedex: species matcher with trap fixture"`.

---

### Task 6: Dataset parser and authored maps (pure)

**Files:**
- Create: `src/PokemonInvestBatch.Application/Pokedex/PokeapiDataset.cs` (records + parser)
- Create: `src/PokemonInvestBatch.Application/Pokedex/PokedexMaps.cs` (region, egg-group display, type gradients, stage derivation)
- Test: `tests/PokemonInvestBatch.Application.Tests/Pokedex/PokeapiDatasetTests.cs`, `PokedexMapsTests.cs`
- Test fixture: `tests/PokemonInvestBatch.Application.Tests/Pokedex/Fixtures/` — trimmed real JSON for species 197 (Umbreon), 133 (Eevee), 25/172/26 (Pichu line), 772 (Type: Null, no habitat), their `pokemon/` counterparts and evolution chains, plus `egg-group/5.json` (the `ground`→"Field" proof)

**Interfaces:**
- Produces:

```csharp
public sealed record SpeciesImport(
    int Id, string Name, string Slug, short Generation, string Region, string Color,
    string? Habitat, SpeciesStatus Status, short Stage, int? EvolvesFrom,
    IReadOnlyList<string> Types, IReadOnlyList<string> EggGroups,
    IReadOnlyDictionary<string, string> LocalizedNames,
    string GradientStart, string GradientEnd);
public static class PokeapiDataset
{
    /// <summary>Reads a mirror directory (Task 7's layout) into import records.
    /// Throws InvalidOperationException naming the file and field on anything
    /// unmapped — reference-data drift fails loudly (spec §6).</summary>
    public static IReadOnlyList<SpeciesImport> Load(string mirrorDirectory);
}
```

- [x] **Step 1: Failing tests**: `Load` over the fixture directory returns Umbreon as `(197, "Umbreon", "umbreon", 2, "Johto", "Black", "Urban", Ordinary, Stage 1, EvolvesFrom 133, Types ["Dark"], EggGroups ["Field"], names include ja, gradient non-empty)`; Pikachu derives `Stage 1, EvolvesFrom 172` (the pinned baby-case from spec §3); Type: Null has `Habitat = null`; an egg group absent from the display map throws with the group named in the message; same for an unmapped type or generation.
- [x] **Step 2: Run** → FAIL. **Step 3: Implement**:
  - Parse `pokemon-species/{n}.json` for name/slug (`names` array `en` entry is the display name; the resource `name` is the slug), generation (`generation.name` → `"generation-ii"` → 2), color, habitat (nullable), `is_legendary`/`is_mythical`, `evolves_from_species`, egg groups, all 12 `names` languages; `pokemon/{id}.json` (the default variety from `varieties`) for types; `evolution-chain/{n}.json` for stage = depth of this species from the chain root.
  - `PokedexMaps`: `Region(short generation)` — 9-entry switch (Kanto, Johto, Hoenn, Sinnoh, Unova, Kalos, Alola, Galar, Paldea; anything else throws); `EggGroupDisplay(string apiName)` — `monster`→Monster, `water1`→Water 1, `water2`→Water 2, `water3`→Water 3, `bug`→Bug, `flying`→Flying, `ground`→**Field**, `fairy`→Fairy, `plant`→**Grass**, `humanshape`→**Human-Like**, `mineral`→Mineral, `indeterminate`→**Amorphous**, `ditto`→Ditto, `dragon`→Dragon, `no-eggs`→**No eggs**; unmapped throws. `TypeGradient(string primaryType)` — an 18-row map of tasteful two-stop hex pairs, e.g. Fire `("#B4522A","#E8A46B")`, Water `("#3D6FA8","#8FC1E8")`, Grass `("#3F7A4A","#9BC98F")`, Electric `("#B08A1E","#EAD06B")`, Psychic `("#7A4E8F","#C79BD6")`, Dark `("#2B2D42","#5C6B9E")` (the existing Umbreon pair from the prototypes), Dragon `("#4A5AA8","#8FA0E0")`, Fairy `("#A85A88","#E0A8C8")`, Normal `("#8A8A86","#C9C9C4")`, Fighting `("#8F4E3A","#D69B7A")`, Flying `("#6E8AB8","#B8CCE8")`, Poison `("#6E4E8F","#B08AC9")`, Ground `("#8F7A4E","#D6C08A")`, Rock `("#7A6E5A","#B8AC94")`, Bug `("#6E8F3A","#B8D68A")`, Ghost `("#4E4E7A","#9494C9")`, Steel `("#6E7A8A","#B0BCC9")`, Ice `("#5A9BB8","#B0E0F0")`; unmapped throws.
- [x] **Step 4: Run** → PASS. **Step 5: Commit** — `"Pokedex: dataset parser and authored maps"`.

---

### Task 7: PokéAPI mirror (Infrastructure)

**Files:**
- Create: `src/PokemonInvestBatch.Infrastructure/Pokedex/PokeapiMirror.cs`
- Test: `tests/PokemonInvestBatch.Infrastructure.Tests/Pokedex/PokeapiMirrorTests.cs`

**Interfaces:**
- Consumes: `ScraperOptions.PokeapiDataBaseUrl` + `PokeapiDataPin`.
- Produces: `static bool Exists(string dir)`, `static Task<PokeapiMirrorManifest> FetchAsync(HttpClient http, string baseUrl, string pin, string dir, TimeProvider time, CancellationToken ct)`, `static string Version(string dir)` — manifest JSON (`pokeapi-mirror.manifest.json`: pin, fetched-at, file count) mirrors `TcgdexMirror`'s shape.

- [x] **Step 1: Failing tests**: `Exists` false on empty dir; `FetchAsync` against a stubbed `HttpMessageHandler` (serving three fixture species and their dependencies) writes `pokemon-species/197.json`, `pokemon/197.json`, `evolution-chain/67.json` under the dir + a manifest carrying the pin; `Version` reads it back.
- [x] **Step 2: Run** → FAIL. **Step 3: Implement**: fetch per-file from `{baseUrl}{pin}/data/api/v2/...` — first `pokemon-species/index.json` for the species list, then each species file, its default-variety `pokemon` file, its evolution chain (deduplicated), and the 15 `egg-group` files. Sequential with a small delay is fine (one-time, ~2,900 small files against GitHub's raw host; dedicated client, UA header like the TCGdex client). Any non-200 fails the fetch loudly — a partial mirror is worse than none; delete the directory on failure.
- [x] **Step 4: Run** → PASS. **Step 5: Commit** — `"Pokedex: pinned PokeAPI mirror"`.

---

### Task 8: Species icon store (Infrastructure)

**Files:**
- Create: `src/PokemonInvestBatch.Infrastructure/Pokedex/SpeciesIconStore.cs`
- Test: `tests/PokemonInvestBatch.Infrastructure.Tests/Pokedex/SpeciesIconStoreTests.cs`

**Interfaces:**
- Produces: `static Task<IconFetchResult> FetchMissingAsync(HttpClient http, string baseUrl, string pin, string iconDirectory, IReadOnlyList<int> dexNumbers, ILogger log, CancellationToken ct)` where `IconFetchResult` carries `FromMenuIcons`, `FromDefaultSprites`, `Missing` (counts + missing dex list). Files land as `{iconDirectory}/{dex}.png`.

- [x] **Step 1: Failing tests** against a stubbed handler: dex 197 served at the gen-viii icon path → written from `sprites/pokemon/versions/generation-viii/icons/197.png`; dex 1002 404s there but exists at `sprites/pokemon/1002.png` → written from the fallback; dex 9999 404s at both → counted `Missing`, no file, no throw. Existing files are skipped (idempotent).
- [x] **Step 2: Run** → FAIL. **Step 3: Implement** the two-step fallback chain exactly (menu icon → default front sprite → recorded gap; spec §3 icons). **Step 4: Run** → PASS. **Step 5: Commit** — `"Pokedex: species icon store with fallback chain"`.

---

### Task 9: Species importer (Infrastructure)

**Files:**
- Create: `src/PokemonInvestBatch.Infrastructure/Pokedex/SpeciesImporter.cs`
- Test: `tests/PokemonInvestBatch.Infrastructure.Tests/Pokedex/SpeciesImporterTests.cs` (throwaway DB)

**Interfaces:**
- Consumes: `PokeapiDataset.Load`, the Task 2 entities.
- Produces: `Task<SpeciesImportResult> ImportAsync(PokemonDbContext db, IReadOnlyList<SpeciesImport> species, CancellationToken ct)` — upsert by dex number across all four species tables; result carries `Inserted`, `Updated`, `Unchanged`.

- [x] **Step 1: Failing tests**: import two fixture species into an empty DB → 2 inserted, child rows present; re-import unchanged → `Unchanged=2`, zero writes (assert row `xmin`s or use change-tracker count); mutate one name and re-import → 1 updated, types/egg-groups/names replaced not duplicated.
- [x] **Step 2: Run** → FAIL. **Step 3: Implement** (load-all-compare-write; ~1,025 rows, no chunking needed; child tables replaced per changed species inside one transaction). **Step 4: Run** → PASS. **Step 5: Commit** — `"Pokedex: species importer, idempotent"`.

---

### Task 10: Tagging sweep + set details (Infrastructure)

**Files:**
- Create: `src/PokemonInvestBatch.Infrastructure/Pokedex/TaggingSweep.cs`
- Create: `src/PokemonInvestBatch.Infrastructure/Pokedex/SetDetailsSweep.cs`
- Modify (only if needed): `src/PokemonInvestBatch.Infrastructure/Enrichment/TcgdexMirror.cs` / `TcgdexCatalog.cs` — surface each mirrored set's `releaseDate` and `serie` if not already exposed
- Create: `tcgdex-series-eras.json.example` (repo root, beside `tcgdex-set-aliases.json` convention)
- Test: `tests/PokemonInvestBatch.Infrastructure.Tests/Pokedex/TaggingSweepTests.cs`, `SetDetailsSweepTests.cs`

**Interfaces:**
- Consumes: `SpeciesMatcher`, `TcgdexCatalog`/`SetMapper` (existing), Task 2 entities.
- Produces:

```csharp
public sealed record TaggingSweepResult(int Examined, int Tagged, int NoSpecies, int Quarantined, int LinksWritten, int LinksRemoved);
public sealed class TaggingSweep
{
    /// <summary>Work set: cards with no card_tagging row, or tagged_name !=
    /// current name — excluding not_a_card_at rows. Upserts card_tagging;
    /// diffs card_species (insert missing, delete stale TitleMatch rows;
    /// Manual rows untouched). Chunked SaveChanges like EnrichmentLane.</summary>
    public Task<TaggingSweepResult> RunAsync(PokemonDbContext db, IReadOnlyList<(string Name, int SpeciesId)> candidates, TimeProvider time, CancellationToken ct);
}
public sealed record SetDetailsSweepResult(int Matched, int Pending);
public sealed class SetDetailsSweep
{
    /// <summary>One set_details row per set, always. Matched sets get code =
    /// TCGdex set id, released_on, series, era from the series→era file
    /// (absent file = era null); unmapped sets get MatchStatus.Pending.</summary>
    public Task<SetDetailsSweepResult> RunAsync(PokemonDbContext db, CancellationToken ct);
}
```

- [x] **Step 1: Failing TaggingSweep tests** (throwaway DB, fixture species from Task 5's list): fresh card "Umbreon VMAX #215" → `card_tagging(Tagged, TitleMatch, "Umbreon VMAX #215")` + link (card, 197); trainer "Rare Candy #85" → `NoSpecies`, zero links; card with a `Manual` link and a machine re-run → manual link intact; rename "Mewtwo #10" → "Mew #8" (update `cards.name`, re-run) → old 150-link deleted, 151-link inserted, `tagged_name` updated; `not_a_card_at` card → never examined; second run over unchanged data → `Examined=0`.
- [x] **Step 2: Failing SetDetailsSweep tests**: mapped set (alias fixture) → `Matched` with code/date/series; unmapped Japanese set name → `Pending` row exists; era file maps "Sword & Shield"→"SWSH"; re-run → no changes.
- [x] **Step 3: Run both** → FAIL. **Step 4: Implement.** The series→era file follows the alias-file posture verbatim (absent = empty, malformed refuses loudly); ship `tcgdex-series-eras.json.example` seeding: Base/Gym/Neo/E-Card series → `WOTC`, EX → `EX`, Diamond & Pearl/Platinum/HeartGold & SoulSilver → `DP`, Black & White → `BW`, XY → `XY`, Sun & Moon → `SM`, Sword & Shield → `SWSH`, Scarlet & Violet → `SV`.
- [x] **Step 5: Run** → PASS. **Step 6: Commit** — `"Pokedex: tagging and set-details sweeps"`.

---

### Task 11: The lane, wiring, and log line

**Files:**
- Create: `src/PokemonInvestBatch.Worker/Lanes/PokedexLane.cs`
- Modify: `src/PokemonInvestBatch.Worker/Program.cs` (HttpClient registration + `AddHostedService<PokedexLane>()`)
- Test: `tests/PokemonInvestBatch.Worker.Tests/PokedexLaneTests.cs`

**Interfaces:**
- Consumes: everything above.
- Produces: `PokedexLane : BackgroundService` with `public const string HttpClientName = "pokeapi"` and a public `RunSweepAsync(CancellationToken)` returning a composite result record — the testable unit, exactly like `EnrichmentLane.RunSweepAsync`.

- [x] **Step 1: Write the lane**, structurally cloned from `EnrichmentLane`: loop with `PokedexTaggingIntervalHours` delay, try/catch log-and-continue; `RunSweepAsync` = ensure PokéAPI mirror (fetch if absent, pin from options) → `PokeapiDataset.Load` → `SpeciesImporter.ImportAsync` → `SpeciesIconStore.FetchMissingAsync` → `SpeciesMatcher.BuildCandidates` (from the imported species' English names) → `TaggingSweep.RunAsync` → `SetDetailsSweep.RunAsync` → one structured `LogInformation` carrying every count (species inserted/updated, icons by source + missing, examined/tagged/no-species/quarantined/links written/removed, sets matched/pending) — these are the spec §7 receipt numbers.
- [x] **Step 2: Wire** in `Program.cs`: `AddHttpClient(PokedexLane.HttpClientName, ...)` with the UA convention from the TCGdex client, and `AddHostedService<PokedexLane>()` after `EnrichmentLane`.
- [x] **Step 3: Test** — a Worker.Tests smoke test invoking `RunSweepAsync` against the fixture mirror + throwaway DB end-to-end (three species, four cards incl. a trainer and a not-a-card) asserting the composite counts.
- [x] **Step 4: Run full suite** — `dotnet test` (all projects) and `dotnet format --verify-no-changes`. **Step 5: Commit** — `"Pokedex: lane wired; first sweep self-bootstraps"`.

---

### Task 12: Grants, ops docs, deploy, backfill, acceptance

**Files:**
- Modify: `ops/README.md` (§4 grants list + a new Pokédex operations subsection)
- No code.

- [x] **Step 1: Extend ops/README §4** (the single source of truth for post-migration grants) with, run as postgres after `dotnet ef database update`:

```sql
GRANT UPDATE ON species, species_types, species_egg_groups, species_names,
    card_tagging, set_details TO pokemon_app;
GRANT UPDATE, DELETE ON card_species TO pokemon_app;
GRANT DELETE ON species_types, species_egg_groups, species_names TO pokemon_app; -- re-import child replacement
```

with one sentence citing ADR-0011's rationale (derived current-state tables; the append-only posture protects observations, and these are not observations). Note explicitly: `cardstock_app`'s SELECT arrives via the existing default privileges — verify, don't assume, in Step 4.
- [x] **Step 2: Document manual overrides** in the same subsection — the operator SQL, ADR-0002 style:

```sql
-- Pin a card's species by hand (survives every sweep):
INSERT INTO card_species (card_id, species_id, method) VALUES (<card>, <dex>, 1)
    ON CONFLICT (card_id, species_id) DO UPDATE SET method = 1;
UPDATE card_tagging SET status = 0, method = 1, updated_at = now() WHERE card_id = <card>;
-- Declare a card species-less by hand:
DELETE FROM card_species WHERE card_id = <card> AND method = 0;
UPDATE card_tagging SET status = 1, method = 1, updated_at = now() WHERE card_id = <card>;
```

(0/1 enum values per `TagStatus`/`TagMethod` ordering from Task 2 — state them in the doc.)
- [x] **Step 3: Deploy** per the existing ops/README flow: migrate as `pokemon_owner`, apply Step 1 grants, publish `linux-arm64` self-contained, restart the unit. The first sweep self-bootstraps: mirror fetch (~2,900 files + ~1,025 icons, one-time), import, full backfill (minutes, chunked — spec §4's performance envelope).
- [x] **Step 4: Acceptance receipts** (spec §7 — run on the Pi, paste results into the completion report):

```sql
-- 1. Invariants (expect 0 and 0):
SELECT count(*) FROM cards c LEFT JOIN card_tagging t ON t.card_id = c.id
    WHERE c.not_a_card_at IS NULL AND t.card_id IS NULL;
SELECT count(*) FROM sets s LEFT JOIN set_details d ON d.set_id = s.id WHERE d.set_id IS NULL;
-- 2. Coverage splits (report verbatim):
SELECT status, count(*) FROM card_tagging GROUP BY status ORDER BY status;
SELECT match_status, count(*) FROM set_details GROUP BY match_status;
-- 3. 100-card eyeball sample (owner reviews):
SELECT c.name, t.status, string_agg(s.name, ' · ') FROM card_tagging t
    JOIN cards c ON c.id = t.card_id
    LEFT JOIN card_species cs ON cs.card_id = t.card_id LEFT JOIN species s ON s.id = cs.species_id
    GROUP BY c.id, c.name, t.status ORDER BY random() LIMIT 100;
-- 3b. Full quarantine list (owner reviews):
SELECT c.id, c.name FROM card_tagging t JOIN cards c ON c.id = t.card_id WHERE t.status = 2;
-- 4. Species completeness + icon gaps (icon gaps come from the lane's log line):
SELECT count(*) FROM species;
-- 5. Character-page smoke (expect Umbreon's printings, > 20 rows):
SELECT count(*) FROM card_species WHERE species_id = 197;
-- cardstock_app read check (run as cardstock_app):
SELECT count(*) FROM species;
```

- [x] **Step 5: Commit** ops docs — `"Pokedex: grants, manual-override SQL, acceptance queries (ADR-0011)"`.

---

## Self-review notes (already applied)

**Spec coverage:** §1 shape → Tasks 1/11/12 · §2 schema → Task 2 (with the `(species_id, card_id)` index) · §3 import + maps + icons → Tasks 6/7/8/9 · §4 lane/matcher/denylist/statuses/manual/perf → Tasks 3/4/5/10/11 + 12 Step 2 · §5 set enrichment → Task 10 · §6 tests/error posture → inside every task (loud-fail maps in 6, partial-mirror delete in 7, idempotency in 9/10) · §7 acceptance → Task 12 Step 4 · §8 delivery → this plan + Task 12 · §9 non-goals → no task builds UI, TCGdex tagging, cameo tagging, or localized display. **Deviation from spec, deliberate:** the manual-override "console verb" became documented operator SQL (Task 12 Step 2), matching the repo's ADR-0002 convention — the ADR in Task 1 records it. **Type consistency:** `TagStatus`/`TagMethod`/`SetMatchStatus` defined once (Task 2), consumed by 5/10/12; `SpeciesImport` defined in 6, consumed by 9/11; candidate list shape `(string Name, int SpeciesId)` identical in 5/10/11.
