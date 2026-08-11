# ADR-0001: CardStock's tables live in their own schema, and each repo migrates its own

**Date:** 2026-08-11
**Status:** Accepted

## Context

CardStock has no data of its own. Everything it renders comes from the eight tables the
`PokemonInvestBatch` crawler owns in the `public` schema of the `pokemon` database. But CardStock
also needs roughly twenty tables nobody else has any business owning — users, transactions,
watchlists, saved screens, and the market index and metric snapshots its worker will compute.

So two applications need to share one Postgres on one Raspberry Pi, and until now the crawler has
never coexisted with anything. There is no precedent in that repo to copy: it configures no schema,
no migrations history table, and no table mappings at all.

```
grep -rn "HasDefaultSchema\|MigrationsHistoryTable\|MigrationsAssembly\|ExcludeFromMigrations\|ToView" \
  src tests --include="*.cs" | grep -v /obj/     # → empty, 2026-08-11
```

Three things make this sharper than an ordinary "share a database" question.

**The crawler's data cannot be rebuilt.** `sales` and `populations` begin at each card's first
visit and the source publishes no history, so a destructive mistake is permanent. There is no
backup today; the owner has deliberately deferred that (D-017).

**EF Core will happily destroy tables it does not own.** Left unmapped, the scaffolder emits
`CreateTable(schema: "public")` in `Up()` and `DropTable(schema: "public")` in `Down()` for the
crawler's tables. This is not hypothetical — it is reproduced in the probe cited below, and the
crawler's own authors already hit the same class of bug and hand-edited around it:

> *"The scaffolder saw the entity type change name and produced DropTable("shapes") +
> CreateTable("fingerprints"), which is faithful to the model and would have destroyed every
> archived fingerprint."*
> — `PokemonInvestBatch/…/Migrations/20260808022824_RenameShapesToFingerprints.cs:10–16`

**The crawler's grants are all schema-scoped.** Every `GRANT` and `ALTER DEFAULT PRIVILEGES` in
`ops/postgres-setup.sql` is scoped `IN SCHEMA public` (`:32`, `:34`, `:36`; the file is 45 lines,
read in full). Nothing it does can reach into another schema, and nothing another schema does can
disturb it.

## Decision

**One database. Two schemas. Two owners. Two migration lineages.**

1. **The `pokemon` database stays as it is.** The crawler keeps `public`. CardStock creates and
   owns a `cardstock` schema under a new `cardstock_owner` role, with a runtime `cardstock_app`
   role that holds `USAGE` but never `CREATE`.

2. **CardStock reads the crawler's tables and never writes them.** Enforced by grants —
   `cardstock_app` gets `SELECT` in `public` and nothing else. This settles D-026, which the owner
   had deliberately left open; ruled 2026-08-11.

3. **The crawler's tables are mapped in EF as views, never as tables.**

   ```csharp
   b.Entity<ScraperCard>(e => { e.HasKey(x => x.Id); e.ToView("cards", "public"); });
   ```

   `ToView` is the load-bearing choice, not a stylistic one. The obvious alternative,
   `ToTable("cards", "public", t => t.ExcludeFromMigrations())`, silently keeps the hazard it
   appears to remove.

4. **Foreign keys into the crawler's tables are real, and hand-written.** `transactions.card_id`
   references `public.cards(id)`. Because `ToView` hides the relationship from EF, the constraint
   is written directly in a migration rather than scaffolded — which also means EF never touches
   it again, since the scaffolder only diffs what its model snapshot knows about. Requires a
   one-off `GRANT REFERENCES ON public.cards, public.sets TO cardstock_owner`.

5. **Each repo migrates only its own schema, by hand, from a developer machine.** This mirrors the
   crawler exactly: nothing auto-migrates there, and nothing will here.

   ```
   # in PokemonInvestBatch, 2026-08-11:
   grep -rn "Database.Migrate\|MigrateAsync" src --include="*.cs"   # → empty
   ```

   The same must hold in this repo, and is checked the same way.

   Neither `CardStock.Api` nor `CardStock.Worker` may call `Migrate()`. Two units racing one
   history table at boot is a second reason on top of the crawler's.

6. **CardStock pins its own migrations history table:**
   `MigrationsHistoryTable("__cardstock_migrations_history", "cardstock")`. This is required, not
   decorative — `HasDefaultSchema("cardstock")` alone does *not* relocate the history table. It
   stays unqualified and resolves through `search_path` straight onto the crawler's.

7. **Cross-repo migration ordering:** additive crawler changes deploy first. Destructive ones —
   renaming or dropping a column CardStock reads — require CardStock to deploy first and stop
   reading it. "Crawler first, always" is wrong for exactly the migrations that matter.

8. **No role gets a custom `search_path`.** Every statement EF emits is schema-qualified; the
   crawler's history table is not. Putting `cardstock` ahead of `public` on any role would
   silently relocate it.

### The evidence for `ToView`

Reproduced 2026-08-11 against EF Core 10.0.10 / Npgsql 10.0.3 / EFCore.NamingConventions 10.0.1,
and verified by reading the scaffolded migrations directly:

| Mapping | What `dotnet ef migrations add` produces |
|---|---|
| `ToTable(…, ExcludeFromMigrations())` | Cross-schema FKs into the crawler: `fk_holdings_cards_card_id → principalSchema: "public"`, and the same for `sets` |
| `ToView(…)` | One `CreateIndex`. Nothing referencing `public` at all |
| *mapping omitted entirely* | `CreateTable(schema: "public")` in `Up()`, **`DropTable(schema: "public")` in `Down()`** |

`ToView` also makes an EF-level write to a crawler table throw
`InvalidOperationException: … not mapped to a table` instead of reaching Postgres and failing on a
permission it might one day hold.

### Build-time guards

Because the failure modes above are silent, three model tests run without a database, mirroring
`PokemonInvestBatch/tests/…/SchemaModelTests.cs`:

- **A.** Every entity with a table name must have `GetSchema() == "cardstock"`.
- **B.** Every crawler-owned entity must have a null table name and a non-null view name.
- **C.** The migrations history table name and schema are pinned to their expected values.

Plus one CI check: no file in `Persistence/Migrations/` (excluding `.Designer.cs` and the model
snapshot, which legitimately record `"public"` for view mappings) may contain the string
`"public"`.

## Alternatives considered

**A separate `cardstock` database on the same cluster.** Rejected. Its whole justification is that
only a database boundary makes CardStock structurally incapable of harming `sales`, and that is
false — object ownership already does this. `DROP` and `ALTER` require ownership, not privilege, so
`cardstock_owner` gets `42501 must be owner of table cards` regardless of grants. Meanwhile
Postgres cannot join across databases without `postgres_fdw`, and
`holdings ⋈ cards ⋈ price_months` is the query the entire product rests on. It pays a real cost for
protection it does not add.

**CardStock's tables alongside the crawler's in `public`, distinguished by a `cs_` prefix.**
Rejected. The prefix is a namespace enforced by a hand-written loop and a test someone must
remember to write, when Postgres already has a namespace mechanism. It also forfeits
`GRANT SELECT ON ALL TABLES IN SCHEMA public`, since that would sweep up CardStock's own user
tables — so read grants must be enumerated and re-enumerated forever.

**One migration lineage in the crawler's repo, owning both applications' tables.** Rejected. It
couples two deploy cadences and puts CardStock's product decisions inside a repo whose job is
crawling.

## Consequences

**What this buys.** A clean grant surface. A separate migration history, so neither repo's
scaffolder can see or clobber the other's. Ownership legible from `\dn`. Single-statement joins
across the boundary. And one `pg_dump -d pokemon` that is transactionally consistent across both
applications, whenever backups do arrive.

**It isolates names and grants, and nothing else.** Same cluster, same WAL, same
`max_connections`, same SSD, same crash domain. A long CardStock migration holds a snapshot that
pins the database-wide xmin horizon and blocks dead-tuple cleanup in the crawler's append-heavy
tables for its duration.

**Connection budget is now shared and tight.** The Pi runs `max_connections = 100` (verified
2026-08-11), against which three .NET processes each taking Npgsql's default pool of 100 would ask
for 300. Both roles therefore carry an explicit `CONNECTION LIMIT`, and every connection string
sets `Maximum Pool Size` rather than accepting the default.

**It visibly diverges from the conventions CLAUDE.md asks us to mirror.** The crawler has zero
schema configuration and not one `ToTable`. CardStock carries `HasDefaultSchema`, a history-table
override, a view-mirror file, and three model tests. The divergence is forced — the crawler never
had to coexist with anything — but a reviewer opening both repos sees two different EF postures.

**The mirrored entities are unversioned copies of another repo's schema, and drift is silent.**
It has precedent: `20260801024826_WidenImageHash` changed a column's type, and
`20260808022824_RenameShapesToFingerprints` renamed a table and its columns. Mitigated by
committing a generated `scraper-schema.sql` fixture, plus a test that regenerates it from the
sibling repo when present and fails on any diff.

**Postgres 15.18 is the floor** (verified on the Pi, 2026-08-11). CI pins `postgres:15` to match,
as the crawler's CI already does. The claim of "16+" in that repo's README is wrong.
