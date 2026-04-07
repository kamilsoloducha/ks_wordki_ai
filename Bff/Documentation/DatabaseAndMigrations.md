# Database and Migrations

## Current direction

Project is moving toward relational database usage with PostgreSQL.

- local PostgreSQL definition is in `Local/docker-compose.yml`
- connection strings are stored in `appsettings.json` and `appsettings.Development.json`
- users module currently contains EF Core model and mapping

## Modular schema strategy

Recommended schema-per-module approach:

- `users` - users data and users outbox/events table
- `cards` - cards and groups data
- `lessons` - lessons data
- `integration` (optional, future) - shared integration objects like global outbox

This keeps module boundaries explicit and simplifies future module extraction.

## Outbox

Outbox pattern is used to store integration messages in database in the same transaction as domain changes.

Current users module already includes:

- `SharedEventMessage` model in shared kernel
- users module table mapping for shared event messages in `users` schema
- saving outbox records in register/confirm flows

## Migration strategy

Preferred strategy for this codebase:

1. one dedicated migrations project
2. multiple module DbContexts (one per module)
3. single deployment entrypoint for running migrations

Why:

- avoids migration conflicts across modules
- keeps deployment repeatable
- keeps module ownership over data model

## Local development

Start local PostgreSQL:

```bash
docker compose -f Local/docker-compose.yml up -d
```

Stop local PostgreSQL:

```bash
docker compose -f Local/docker-compose.yml down
```

## Reset database (dev) — undo all applied migrations

To wipe **all** tables and schemas created by the unified migrations project (`AppMigrationDbContext`) and clear EF migration history, run the SQL script:

[`sql/reset-database-from-migrations.sql`](sql/reset-database-from-migrations.sql)

Example:

```bash
cd Bff
psql -h localhost -U wordki -d wordki_dev -v ON_ERROR_STOP=1 \
  -f Documentation/sql/reset-database-from-migrations.sql
```

Then recreate the schema from scratch:

```bash
bash Documentation/util/update-database.sh
```

Use the same database name as in `ConnectionStrings:MigrationDatabase` (or the connection your startup project uses for `AppMigrationDbContext`).

## Notes

- avoid cross-schema foreign keys between modules unless absolutely necessary
- keep module integration through identifiers and events
- keep event handlers idempotent because retries can happen
