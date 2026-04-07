-- =============================================================================
-- Wordki BFF — full reset of objects created by EF Core (AppMigrationDbContext)
-- =============================================================================
-- Database: PostgreSQL
--
-- This script removes:
--   • schema `cards`  (groups, cards, card_sides, results, cards.users, …)
--   • schema `users`  (users module users, shared_event_messages, …)
--   • public.__EFMigrationsHistory — EF migration history for this context
--
-- After running, the database has NO Wordki application tables. Re-apply migrations:
--   dotnet ef database update \
--     --project src/Wordki.Bff.Migrations/Wordki.Bff.Migrations.csproj \
--     --startup-project src/Wordki.Bff.Api \
--     --context AppMigrationDbContext
--   (or: bash Documentation/util/update-database.sh from repo Bff root)
--
-- WARNING: Irreversible data loss. Use only on local/dev databases (e.g. wordki_dev).
--
-- Usage (adjust user/database/host):
--   psql -h localhost -U wordki -d wordki_dev -v ON_ERROR_STOP=1 -f Documentation/sql/reset-database-from-migrations.sql
-- =============================================================================

BEGIN;

DROP SCHEMA IF EXISTS cards CASCADE;
DROP SCHEMA IF EXISTS users CASCADE;
DROP SCHEMA IF EXISTS lessons CASCADE;

-- Single migrations history table for AppMigrationDbContext (Npgsql default: public schema)
DROP TABLE IF EXISTS public."__EFMigrationsHistory" CASCADE;

COMMIT;
