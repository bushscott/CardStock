# CardStock ops

The Pi is `192.168.0.56`, running PostgreSQL 15.18 and the PokemonInvestBatch
crawler. CardStock shares that database and adds its own schema — see
`../docs/adr/0001-schema-separation-and-migration-ownership.md`.

## 1. One-time Postgres setup

Change the three passwords first, then run as the superuser, **after**
PokemonInvestBatch's migrations are current:

```bash
scp ops/cardstock-postgres-setup.sql scott@192.168.0.56:/tmp/
ssh scott@192.168.0.56 'sudo -u postgres psql -v ON_ERROR_STOP=1 -f /tmp/cardstock-postgres-setup.sql'
```

## 2. Migrations

Applied by hand from a developer machine, as `cardstock_owner`. **Nothing
auto-migrates** — neither the API nor the Worker calls `Migrate()`, so the two
units cannot race one history table at boot. This mirrors the crawler, which
does the same.

```bash
dotnet tool restore

dotnet ef migrations add <Name> \
  -p src/CardStock.Infrastructure -s src/CardStock.Infrastructure \
  -o Persistence/Migrations --context CardStockDbContext

CARDSTOCK_DB="Host=192.168.0.56;Database=pokemon;Username=cardstock_owner;Password=..." \
dotnet ef database update \
  -p src/CardStock.Infrastructure -s src/CardStock.Infrastructure \
  --context CardStockDbContext
```

`--context` is required from the moment a second `DbContext` exists in the
assembly.

### Before applying: read the generated migration

Confirm by eye that the string `"public"` appears nowhere in `Up()` or `Down()`,
and that no `CreateTable`/`DropTable` mentions `cards`, `sets`, `price_months`,
`populations`, or `sales`. `MigrationContentTests` asserts this too, but a
migration is applied by a human and this is the last cheap moment to catch it.

### After applying: verify ownership

A wrong credential is the one remaining hole. It would create CardStock's tables
owned by `pokemon_owner`, silently granting `pokemon_app` access to
`cardstock.users`.

```sql
SELECT c.relname, c.relowner::regrole
FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
WHERE n.nspname = 'cardstock' AND c.relowner <> 'cardstock_owner'::regrole;
-- must return zero rows
```

And confirm the history table landed where it should:

```sql
SELECT to_regclass('cardstock.__cardstock_migrations_history');  -- must NOT be null
SELECT to_regclass('public.__cardstock_migrations_history');     -- must be null
```

## 3. Running the tests

Database development happens on the Pi; there is no local Postgres by decision
(owner, 2026-08-11). Tests build and drop a `cardstock_test_<guid>` database per
test, over the LAN.

```bash
CARDSTOCK_TEST_DB="Host=192.168.0.56;Database=postgres;Username=cardstock_tester;Password=...;Maximum Pool Size=10" \
  dotnet test CardStock.slnx
```

With `CARDSTOCK_TEST_DB` unset the database tests skip rather than fail, and the
model guards still run. CI sets it to its own `postgres:15` service container.

### When the schema drift test fails

`SchemaDriftTests` fingerprints the crawler's migration sources. A failure means
the sibling gained or changed a migration. Read it, decide whether CardStock
reads the column it touched, then regenerate both files:

```bash
cd ../PokemonInvestBatch && dotnet ef migrations script \
  -p src/PokemonInvestBatch.Infrastructure -s src/PokemonInvestBatch.Infrastructure \
  -o ../CardStock/tests/CardStock.TestSupport/Fixtures/scraper-schema.sql
```

For the fingerprint, **copy the `actual:` value from the test's own failure
message** into `Fixtures/scraper-schema.fingerprint`. Do not compute it with an
external tool: EF scaffolds migrations with a UTF-8 BOM, which `File.ReadAllText`
strips and most other readers do not, so an externally-computed hash will not
match.

## 4. Cross-repo migration ordering

- **Additive** crawler changes deploy first.
- **Destructive** ones — renaming or dropping a column CardStock reads — require
  CardStock to deploy first and stop reading it.

"Crawler first, always" is wrong for exactly the migrations that matter.
