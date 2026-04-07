#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: ./create-migration.sh <MigrationName>"
  echo "Example: ./create-migration.sh InitAll"
  exit 1
fi

MIGRATION_NAME="$1"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

PROJECT_PATH="src/Wordki.Bff.Migrations/Wordki.Bff.Migrations.csproj"
DB_CONTEXT="AppMigrationDbContext"
OUTPUT_DIR="Migrations"

cd "$REPO_ROOT"

echo "Creating migration '$MIGRATION_NAME' in project 'Wordki.Bff.Migrations'..."
dotnet ef migrations add "$MIGRATION_NAME" \
  --project "$PROJECT_PATH" \
  --startup-project "src/Wordki.Bff.Api/Wordki.Bff.Api.csproj" \
  --context "$DB_CONTEXT" \
  --output-dir "$OUTPUT_DIR"
