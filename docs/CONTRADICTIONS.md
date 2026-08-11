# CardStock — Consolidated contradiction register

Built 2026-08-10 from a direct extraction of all 16 prototypes into `docs/screens/*.md`. Roughly **250 contradictions** were found. This file classifies them, because the class determines who resolves each one.

Per-screen detail with line citations lives in `docs/screens/<screen>.md` §8. This register is the index and the decision queue.

## Classification

| Class | Meaning | Who resolves |
|---|---|---|
| **A** | Doc is stale; the prototype is right | Me — edit the doc |
| **B** | The prototype is wrong about the *data* | Me — rewrite the copy from `../PokemonInvestBatch` |
| **C** | Tier 1 contradicts Tier 1 | **You** — the hierarchy cannot break the tie |
| **D** | Genuine design decision, never made | **You** |
| **E** | Specified but never built | **You** — build it or drop it |
| **F** | The prototype contradicts itself (latent bug) | Me — do not reproduce faithfully |

The A and B classes are the large majority and need no input from you.

---

# Class C — Tier 1 vs Tier 1 · needs your ruling

Two prototypes disagree. D-040 makes the mockups absolute truth but says nothing about mockups disagreeing with each other, so these escalate by rule.

### C-1 · Account deletion policy
| Source | Says |
|---|---|
| `Cardstock Legal.dc.html:57` | data "removed within **30 days**" |
| `Cardstock Profile.dc.html:181, :191` | "**immediately and permanently**… no recovery" |

Public commitment under D-011. **D-017 forces the answer**: once off-box backups exist — and they must, since `sales` and `populations` cannot be rebuilt — "immediately and permanently" is unkeepable, because deleted rows survive in dumps until rotation. A bounded window is the only promise a backed-up system can honour.

Related defect: `Legal:57` tells users to "export your binder as CSV first," but Profile has no export affordance and the Binder's CSV control generates no file. The policy instructs a step the product cannot perform.

**Recommendation:** adopt the Legal page's bounded window, set to match backup rotation, and rewrite the Profile copy.

### C-2 · Tier colours
`Cardstock Card.dc.html:325` and `Cardstock Charts.dc.html:375` assign different colours to the same tiers:

| Tier | Card | Charts |
|---|---|---|
| Grade 9.5 | `#6E4DB8` | `#7A56C9` |
| Grade 8 | `#2E7F78` | `#4C8F8A` |
| Grade 7 | `#B0552E` | `#A96A4A` |

PSA 10, Grade 9, and Raw match. The Card page links straight into Charts (`Card:69`, `:117`), so a user sees both in one session. `HANDOFF.md` §7 requires colour to pair with a glyph and never carry meaning alone — a palette that shifts between screens defeats learning the mapping at all.

**Recommendation:** one palette in the shared component library. D-050 makes the same argument for tokens generally.

---

# Class D — Genuine design decisions · needs your ruling

### D-1 · Marketing and app routes collide
`HANDOFF.md:83` puts the Landing at `/`; `:71` puts app Home at `/`. Same collision for `/screener`, `/charts`, `/binder` (`:84` vs `:72–74`). The prototypes link by bare filename, so **Tier 1 cannot settle it.**

Entangled with render mode (D-013): auth-resolved roots fit the static-marketing / interactive-app split cleanly, since the branches want different render modes anyway.

### D-2 · The tier→price mapping function
See D-012. The Binder records holdings at **118 labels**; **93 have no price series**. Three honest options, none of which may invent precision (D-022): pool to the nearest backed tier with disclosure, render unvalued, or exclude from totals with a visible count.

Aggravating: dormant `bucketOf` (`Binder:415–423`) already implements the PSA approximation you rejected in ADR-0005. It must not be revived by accident.

### D-3 · Watchlist row identity
`HANDOFF.md:155` and `DESIGN_NOTES.md:110` say one row per **card + tier**. The Card page's picker has **no tier selector** (`Card:73–79`), and Home's watchlist keys rows by card id alone — so `(card, tier)` is not representable. Either the key is card-only, or the picker needs a tier control.

### D-4 · `LOW CONFIDENCE` scope
`DESIGN_NOTES.md:33, :131, :146` treat it as a Charts-only badge. `Home:423` uses it as the state a newly unlocked indicator *starts in*. One meaning is needed.

### D-5 · About Data rewrite
See D-046 — 22 false claims, 13 unverifiable, structured around a seam that does not exist. This is a rewrite, not an edit, and it needs your voice.

---

# Class E — Specified but never built · build or drop

Every screen came back smaller than documented. Each of these is design intent with no implementation, so shipping it is **net-new work** rather than a port.

| Feature | Documented at | Reality |
|---|---|---|
| **Charts LOCKED row form** — disabled control, countdown, progress ratio, working "show anyway" | `DISPLAY_VOCABULARY.md:136` | `locked()` and `force()` have **zero call sites**. All six "locked" rows are ordinary toggles with a `LOW DATA` badge; `lockedOr` discards the ratios. **D-038 ships this, so it must be built** (D-049) |
| **Browse era shelves** + Uncategorized + METADATA PENDING | `DESIGN_NOTES.md:71`, spec `:199`, `:201` | `ERAS` read by nothing; 0 occurrences of the other two strings |
| **Card page seam markers** | `DESIGN_NOTES.md:47` | `SEAMS` is dead data, `isSeam` always false — and `DESIGN_NOTES.md:54` says they were removed. Same file, both positions |
| **Character index chart** | task brief, docs | No `<svg>` in the file at all |
| **Set code, card-number denominator, era, release date** | `DESIGN_NOTES.md:72` | None displayed. Header shows card count only |
| **Density control** | `HANDOFF.md:156`, `DISPLAY_VOCABULARY.md:203` | 0 occurrences; Appearance has exactly two controls |
| **Species search box** | `DESIGN_NOTES.md:71`, spec `:199` | `pokeQ`/`pq`/`setPokeQ` computed, never bound. No `<input>` exists |
| **Signal chip selection logic** — firing-only, priority order, cap 4, "+N more" | `DISPLAY_VOCABULARY.md:7`, `:37` | Static 3-element literal. The seeded chips match documented triggers exactly, so the vocabulary is right and only the selection is missing |
| **Screener filtering** | throughout | No filter affects results; `matchLabel` ignores chips (`Screener:856`) |
| **Concentration warning** | `DESIGN_NOTES.md:16` | Nothing computed; `warn` is seeded |
| **Loading / empty / error states** | spec `:163`, `:201`, `:429` | None exist on any screen |
| **Virtualization** (watchlist, screener grid) | spec `:157`, `:87`, `:46` | Plain loops everywhere |
| **Focus trap, roving tabindex, Esc-restores-focus** on the peek panel | spec `:287`, `:359` | None of the three |
| **Responsive breakpoints** | spec `:354` | Zero width media queries in any prototype |
| **`prefers-reduced-motion`** on marketing | brand README `:115` | Absent from all four marketing pages; six app pages have it |
| **Dark mode on marketing** | `DESIGN_NOTES.md:105` | 0 `data-theme` — marketing is light-only |
| **`prefers-color-scheme`** | — | Absent everywhere; first visit is always light |

**Note on accessibility:** the missing focus trap, keyboard handlers, breakpoints, and reduced-motion support are not merely unbuilt features — under D-011 they are a public product's baseline. Worth treating as one workstream.

---

# Class B — The prototype is wrong about the data · I fix

D-040's standing exception: prototypes are authoritative about *what the design is*, never about *what the data is*. `../PokemonInvestBatch` wins here.

### The Apr '25 seam, wired into copy and logic

| Surface | Lines |
|---|---|
| Marketing | `Landing:202, 235, 236`; `Charts Landing:45, 74–75, 113`; `Screener Landing:92` |
| About Data | `:52`, `:69`, `:71`, `:72` — including a section heading and a nav pill |
| Screener | `:505`, `:511` |
| Charts | `SEAM = Apr '25` **hardcoded into rendering logic**, `:388–398` |
| Card | seeded sales dated Mar–Aug 2026 and `SEAMS` Mar–Jun 2026 — all before the scraper existed |

### Other data claims that are false

| Claim | Where | Truth |
|---|---|---|
| "sale counts" back to Aug 2023 | `About Data:71` | Historical sales volume is **permanently unavailable** (`DATA_MODEL.md:481`). And the claim is inverted — counts exist only *after* the seam |
| "back to August 2023" | `About Data:71` | ~**Dec 2020** (D-002). Understates the one deep series by 32 months |
| "every individual transaction" | `About Data:71` | Source keeps ~30-row buckets and discards the rest forever |
| "populations… graders publish monthly" | `About Data:63` | Scraped from the source's embedded blob; two graders only (`psa`, `cgc`), no publication cadence, no history |
| "Excluded: bulk lots, damage notes…" | `About Data:64` | **No exclusion pipeline exists** — grep across the scraper returns no sale-content filtering |
| "Prices come from realized sales only" | `About Data:62` | The plotted series is the source's own monthly average, not sales we aggregated |
| "Sales data refreshes daily" | `About Data:79` | Priority queue with a 30-day starvation floor |
| "footer stamp on every page" | `About Data:79` | Neither About Data nor Legal has one |
| listed prices "~12% of rows" | `HANDOFF.md:128`, `Charts:420, 627`, `Screener:491, 550`, `DISPLAY_VOCABULARY.md:115` | **4.4%** (`DESIGN_NOTES.md:46`, D-031) |
| census "7 obs", "7/12 mo", "Jan '26" | `Card:237`, `Screener:548, 552–554`, `DISPLAY_VOCABULARY.md:117` | ~**1/12**, unlocking ~Jul 2027 under the D-033 floor |
| "eBay-only depth", "1/5 venues" | `Charts:421, 628` | Five sources documented: ebay, tcgplayer, goldin, heritage, pwcc |
| "~Apr 2027", "Nov 2027", "≈ Jan 2027" | `Charts:607, 625`, `About Data:94` | Recompute from the 2026-09-01 floor (D-033) |

**The framing problem, which outranks any single line:** About Data never names pricecharting.com while writing "our archive," "we keep," and "Excluded" throughout. It reads as first-party collection of a corpus that is entirely scraped — on the page a reader consults to learn provenance, and it discards the attribution that would otherwise mitigate a complaint.

---

# Class A — Doc is stale, the prototype is right · I fix

The largest class, needing no decisions. Counts by screen, detail in `docs/screens/<screen>.md` §8:

| Screen | Rows | Screen | Rows |
|---|---|---|---|
| `home.md` | 27 | `character.md` | 23 |
| `card.md` | 21 + 11 corroborations | `binder.md` | 22 |
| `charts.md` | 24 | `browse.md` | 20 |
| `screener.md` | 24 | `../brand.md` | 18 |
| `set.md` | 17 | `account.md` / `profile.md` | 13 / 11 |
| `marketing.md` | 14 | `shared-components.md` | 11 |

Representative examples: the notification bell is fully gone (0 hits across all 17 prototypes) though the spec still describes it; the Screener has **28** filter metrics, not 27 or 29; Charts has **31** rows, not 32 or 29; the contrast pass shipped but `DESIGN_NOTES.md:26` still records it as deferred.

**Counter-note worth keeping:** `DESIGN_NOTES.md` is largely *reliable* where it describes design rulings. Its census summary-sentence branch rules reproduce the seeded arithmetic exactly, every threshold verified — the Card audit calls it "the single most valuable doc find for the build." The failures cluster in data claims and stale entries never revisited.

---

# Class F — The prototype contradicts itself · do not reproduce

Latent bugs. A faithful rebuild would carry them forward.

**Card page** (`card.md` §8.2): the hollow-dot tooltip says "Aug" while the last chart month is Jul '26; the badge says 7 observations and draws 7 delta bars when 7 observations yield 6; column 5's resize grip is keyed `'src'` so it resizes column 4; the hollow dot follows the first visible series but always paints in the PSA 10 accent; five inert `text-decoration-*` properties where the visible rule is `border-bottom`.

**Home**: rows are `role="button" tabindex="0"` with **no key handler**, so tab+Enter does nothing; `menuIdx` survives tab switches; the peek's six-tier ladder never highlights `PSA 9`/`PSA 8` cards because the match is string equality against a different vocabulary; the drawer's `z-index` is declared twice.

**Binder**: the full VOID render path survives — chip, amber, line-through, 62% opacity, tooltip — but is unreachable because every write sets `v: false`. BUY never validates quantity at all; `''`, `0`, `-5`, and `abc` all pass and coerce to 1. The correction modal binds `Grade 9.5`, `CGC 10 Prist.`, and `BGS 10 Black` to options that do not exist in its own list.

**Browse**: the species grid renders in literal array order under a caption claiming it is ordered by market value.

**Screener**: 11 seeded chips violate the "chips are generated, never authored" rule, and at least one (`New 12M high`) is not producible by the generator at all.
