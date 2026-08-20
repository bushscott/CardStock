# Architecture Decision Records

Each file records one significant decision: what the situation was, what was chosen, what was
rejected and why, and what it costs. They are numbered in the order the decisions were made and
are not edited afterwards — if a decision is reversed, a new ADR supersedes the old one, so the
reasoning trail stays intact.

The format is Michael Nygard's, from *Documenting Architecture Decisions* (2011), mirroring
`../../../PokemonInvestBatch/docs/adr/` so the two repos read the same way.

**The division of labour:** an ADR holds the reasoning behind one architectural decision and is
frozen once accepted. `../../DECISIONS.md` is the running ledger of everything true and decided
about the project, and stays current. A decision that changes a screen also belongs in that
screen's spec under `../screens/`.

| ADR | Decision | Date |
|---|---|---|
| [0001](0001-schema-separation-and-migration-ownership.md) | CardStock's tables live in their own schema, and each repo migrates its own | 2026-08-11 |
| [0002](0002-identity-is-a-cookie-backed-by-a-session-row.md) | Identity is email and password, carried in an HttpOnly cookie backed by a session row | 2026-08-11 |
| [0003](0003-public-exposure-through-a-cloudflare-tunnel.md) | The app goes public through a Cloudflare Tunnel, with the Pi in a DMZ | 2026-08-20 |
