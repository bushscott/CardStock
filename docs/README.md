# CardStock documentation

**This directory is the only place documentation lives.** If a document is authoritative, it is here. If it is somewhere else, it is either the control plane at the repo root or it is stale and scheduled for removal.

## Read order

| If you are… | Read |
|---|---|
| New to the project | `../CLAUDE.md`, then this file |
| Building a screen | `screens/<screen>.md` — that is the build reference |
| Implementing an indicator or screener metric | `signals.md` |
| Wondering why something is the way it is | `../DECISIONS.md` |
| Styling anything | `brand.md` |
| Wondering which document to trust | `../CLAUDE.md` § Document authority |

## What is here

| Path | Holds | Lifetime |
|---|---|---|
| `screens/*.md` | **The build reference.** One spec per screen: identity, layout, data contract, states, interactions, invariants, open questions, and an audit trail of contradictions found. Extracted directly from the prototypes with line citations. | Permanent — maintained as decisions land |
| `brand.md` | Colour tokens across light / dark / colourblind, typography, the glyph vocabulary, theming mechanics, known WCAG failures, and the brand rules and prohibitions harvested from the package handoff. | Permanent |
| `signals.md` | **The signal inventory** — all 29 indicators (25 atomic A1–F4, 4 composites G1–G4) with formulas, caveats, and v1 priority ranking. Plus the "signals users will expect but you cannot honestly support" table, with the reason for each. | Permanent |
| `CONTRADICTIONS.md` | The classified register of ~250 contradictions found during extraction, and the decision queue. | **Temporary — deletes itself.** See below |
| `adr/` | Architecture decision records, Nygard format, mirroring `../../PokemonInvestBatch/docs/adr/`. | Permanent |

## The control plane, at the repo root

Two files, both required to be there:

- **`../CLAUDE.md`** — project rules, hard constraints, and document authority. Must be at the root because Claude Code loads it from there.
- **`../DECISIONS.md`** — the ledger. Every consequential claim and decision, with a status and a receipt you can re-run.

The division: **the ledger records *why* and *when*. The screen specs record *what to build*.** A decision that changes a screen belongs in both.

## `CONTRADICTIONS.md` is scaffolding

It exists to work through one backlog and should not outlive it. Every row resolves into one of three places:

- Classes **A**, **B**, **F** → corrections applied to the screen specs
- Classes **C**, **D** → decisions recorded in `../DECISIONS.md`
- Class **E** → scope calls, also in the ledger

When the classes are worked through, the file has nothing left in it and gets deleted. **If it is still here once the backlog is closed, it has failed at its job.**

## What is deliberately not here

`../CardStock Mockup/` holds the **frozen prototypes** — HTML, JS, CSS, and image assets. It contains **no markdown**, by rule. Those files were the source these specs were extracted from, and they are the visual tiebreak for anything a spec is silent on, but they are no longer edited and no longer the record of truth. See `../CLAUDE.md` § Document authority.
