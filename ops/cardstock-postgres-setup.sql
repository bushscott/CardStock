-- CardStock — one-time Postgres setup on the Pi.
--
--   sudo -u postgres psql -v ON_ERROR_STOP=1 -f cardstock-postgres-setup.sql
--
-- ON_ERROR_STOP is not optional. Without it a failed GRANT is silent, and the
-- application then fails much later with a permission error that reads like a
-- code bug.
--
-- CHANGE THE THREE PASSWORDS BELOW BEFORE RUNNING.
-- Run only after PokemonInvestBatch's own migrations are current.

-- CONNECTION LIMITs are load-bearing, not tidiness. The Pi runs
-- max_connections = 100 (verified 2026-08-11), shared with the crawler, and
-- three .NET processes at Npgsql's default pool size of 100 each would ask for
-- 300. Every CardStock connection string also sets Maximum Pool Size.
CREATE ROLE cardstock_owner  LOGIN PASSWORD 'CHANGE_ME_OWNER'  CONNECTION LIMIT 3;
CREATE ROLE cardstock_app    LOGIN PASSWORD 'CHANGE_ME_APP'    CONNECTION LIMIT 30;

-- Integration tests build and drop a database per test. They run from a
-- developer machine over the LAN -- pg_hba.conf already allows the subnet with
-- scram-sha-256 -- because database development happens on this box and there
-- is no local Postgres by decision (owner, 2026-08-11).
CREATE ROLE cardstock_tester LOGIN PASSWORD 'CHANGE_ME_TEST'   CREATEDB CONNECTION LIMIT 20;

\connect pokemon

-- Created by the superuser so cardstock_owner never needs CREATE on the
-- database itself. Safe to re-run: EF's EnsureSchema is a pg_namespace-guarded
-- DO block, not a bare CREATE SCHEMA.
CREATE SCHEMA cardstock AUTHORIZATION cardstock_owner;

-- The runtime role: full DML inside cardstock, never DDL.
GRANT USAGE ON SCHEMA cardstock TO cardstock_app;   -- USAGE only, never CREATE
ALTER DEFAULT PRIVILEGES FOR ROLE cardstock_owner IN SCHEMA cardstock
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO cardstock_app;
ALTER DEFAULT PRIVILEGES FOR ROLE cardstock_owner IN SCHEMA cardstock
    GRANT USAGE ON SEQUENCES TO cardstock_app;

-- Read the crawler, and nothing more (D-026, ruled 2026-08-11). ALL TABLES is
-- safe here precisely BECAUSE CardStock's own tables are not in this schema.
GRANT USAGE ON SCHEMA public TO cardstock_app;
GRANT SELECT ON ALL TABLES IN SCHEMA public TO cardstock_app;

-- Keeps that read alive across FUTURE crawler migrations. Must be run by a
-- superuser or a member of pokemon_owner.
--
-- NOTE: this writes into pokemon_owner's existing pg_default_acl entry rather
-- than creating a new one, so it sits alongside the crawler's own. Verify it
-- survives any Pi rebuild or DROP OWNED:
--   SELECT defaclrole::regrole, defaclnamespace::regnamespace, defaclacl
--   FROM pg_default_acl;
ALTER DEFAULT PRIVILEGES FOR ROLE pokemon_owner IN SCHEMA public
    GRANT SELECT ON TABLES TO cardstock_app;

-- Documents intent. A no-op today, because none of these were ever granted.
REVOKE INSERT, UPDATE, DELETE, TRUNCATE, REFERENCES, TRIGGER
    ON ALL TABLES IN SCHEMA public FROM cardstock_app;

-- The crawler must never read user data. Belt and braces: the crawler's own
-- ALTER DEFAULT PRIVILEGES is scoped IN SCHEMA public and can never fire here.
REVOKE ALL ON SCHEMA cardstock FROM pokemon_app;

-- ---------------------------------------------------------------------------
-- Two statements that must NOT be added to this file.
--
-- REVOKE ALL ON DATABASE pokemon FROM PUBLIC
--   The crawler has no explicit CONNECT grant -- ops/postgres-setup.sql in the
--   sibling repo grants only USAGE on the schema -- so it connects on PUBLIC's
--   default. Revoking it stops the crawler, and presents as a Postgres outage.
--
-- ALTER ROLE ... SET search_path
--   Every statement EF emits is schema-qualified; the crawler's migrations
--   history table is not. Putting cardstock ahead of public on any role would
--   silently relocate it.
-- ---------------------------------------------------------------------------
