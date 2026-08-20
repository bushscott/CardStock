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

## 2a. Where the passwords are

The three role passwords generated when `cardstock-postgres-setup.sql` was first run live in
**`ops/credentials.local`**, which is gitignored and `chmod 600`. The committed `.sql` keeps its
`CHANGE_ME` placeholders and is a template, not a record.

The `cardstock_app` password also sits in `/opt/cardstock/api/appsettings.Production.json` on the Pi,
owned by the `cardstock` service account and mode 600.

### Resetting one, when it is lost

Happened 2026-08-12 with `cardstock_tester`. Run **on the Pi**:

```bash
NEWPW=$(openssl rand -base64 48 | tr -dc 'A-Za-z0-9' | head -c 32)
sudo -u postgres psql -qc "ALTER ROLE cardstock_tester WITH PASSWORD '$NEWPW'"
echo "cardstock_tester = $NEWPW"
```

Then write it into `ops/credentials.local`.

**Alphanumeric only, deliberately.** `;` is the connection-string separator, so a password containing
one silently truncates `CARDSTOCK_TEST_DB` and the failure reads as a wrong password rather than a
malformed string.

**Which roles this is safe for.** `cardstock_tester` is used only by the test harness, so resetting it
breaks nothing running. **`cardstock_app` is different** — the deployed API holds it, so a reset must
be followed by editing `/opt/cardstock/api/appsettings.Production.json` and restarting the unit, or
the site goes down. `cardstock_owner` is used only at migration time, by hand.

**It leaves the plaintext in the Pi's shell history**, and in the Postgres log if `log_statement` is
on. `sudo -u postgres psql -c "\password cardstock_tester"` prompts instead and sends only the hash,
at the cost of having to invent and remember the password yourself.

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

**If `dotnet test` appears to hang before any test runs**, it is MSBuild node
contention rather than the database — observed 2026-08-11, twice, sitting in
MSBuild for 7+ minutes with no testhost process and no test database created.
The fix:

```bash
dotnet build-server shutdown && pkill -f MSBuild.dll
dotnet build CardStock.slnx -c Release -m:1
dotnet test CardStock.slnx -c Release --no-build -m:1
```

`-m:1` and a separate `--no-build` test step make it reliable. A run that reaches
the tests at all completes in about a second.

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

## 5. Deploying the API

```bash
./ops/publish.sh publish/api
./ops/deploy.sh
```

`deploy.sh` is this section's steps, executable (added 2026-08-13): the rsync,
the ownership fix + unit restart, and the health probe:

```bash
rsync -az --delete --exclude='appsettings.Production.json*' \
  --rsync-path='sudo rsync' publish/api/ scott@192.168.0.56:/opt/cardstock/api/
ssh scott@192.168.0.56 'sudo chown -R cardstock:cardstock /opt/cardstock/api && sudo systemctl restart cardstock-api'
curl -s http://192.168.0.56:5180/healthz/data
```

- `publish.sh` publishes the WASM client through its own pipeline and overlays
  its processed `wwwroot` onto the API bundle (the script's comment says why),
  then fails loudly if `index.html` references a script the bundle lacks.
- **`--exclude='appsettings.Production.json*'` is load-bearing.** The
  production config (and its dated backups) exists only on the Pi; a bare
  `--delete` removes it and the service crash-loops on restart with no
  connection string.
- The unit listens on `0.0.0.0:5180` (`ASPNETCORE_URLS` in
  `cardstock-api.service`).

### Production configuration keys (Phase 2)

`/opt/cardstock/api/appsettings.Production.json` carries, beyond the
connection string:

| Key | Production value | Why |
|---|---|---|
| `Worker:IntakeBaseUrl` | `http://127.0.0.1:5155` | the crawler's intake API — loopback-only by design (its ADR-0006), so only server-side code can reach it |
| `ImageStore:Directory` | `/var/lib/pokemon/images` | the crawler's image store; the API serves `{hash}/1600.jpg` from it |

### Image-store access — no grant needed

Inspected 2026-08-13: `/var/lib/pokemon` and the whole image tree are
world-readable (directories `drwxr-xr-x`, files `-rw-r--r--`, all
`pokemon:pokemon`), and `sudo -u cardstock ls /var/lib/pokemon/images`
succeeds. The setfacl/group grant the Phase 2 plan anticipated is therefore
unnecessary — recorded so nobody adds one on reflex. If the crawler ever
tightens those modes, the image endpoint starts returning 404s (the disk is
the fact); the fix then is `setfacl -R -m u:cardstock:rX
/var/lib/pokemon/images` plus a default ACL for files the crawler writes
afterwards.

## TLS and port 443 (D-132)

Kestrel serves HTTPS-only on 443; endpoints and cert paths live in the Pi-only
appsettings.Production.json (never deployed — see the rsync exclude). Certs:
Let's Encrypt via certbot DNS-01 against Cloudflare (token in
/root/.secrets/certbot/cloudflare.ini, 600). On renew, certbot runs
/etc/letsencrypt/renewal-hooks/deploy/cardstock.sh (source:
ops/certbot-deploy-hook.sh) which copies PEMs to /etc/cardstock/tls
(root:cardstock 640) and restarts the unit. LAN access without Cloudflare:
add "cardstock.pro" to /etc/hosts on the dev machine pointing at the Pi
(192.168.0.56 until the Pi's DMZ move, 192.168.30.56 after — flip it
alongside deploy.sh's IPs) — the cert genuinely matches, so no warnings.
HSTS ramps per D-132 §G only.

## Test databases through the tunnel (D-131)

Since the Public exposure phase, Postgres listens on loopback only — `192.168.0.56:5432` no
longer answers, deliberately (D-131/D-132; the LAN pg_hba grant is gone too). Mac-side DB-gated
test runs go through the SSH forward instead:

```
ssh -fN pi-db     # ~/.ssh/config: Host pi-db → LocalForward 5433 127.0.0.1:5432
CARDSTOCK_TEST_DB="Host=127.0.0.1;Port=5433;Username=cardstock_tester;Password=<ops/credentials.local>;Database=pokemon_test" \
  dotnet test tests/CardStock.Integration.Tests
```

Postgres sees a loopback connection and auths it with the existing scram rules; `pokemon_test`
stays the never-written template. Ad-hoc queries are unchanged: ssh in, `sudo -u postgres psql`.
The sibling repo's `POKEMON_TEST_DB` takes the same `Host=127.0.0.1;Port=5433` form.
