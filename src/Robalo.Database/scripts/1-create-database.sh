#!/usr/bin/env bash
database="${1:-robalo.db}"

[[ "$database" == *.* ]] || database+=".db"

set -o pipefail

cat src/Robalo.Database/migrations/*.sql | sqlite3 -bail "$database" && echo "Database $database was created"