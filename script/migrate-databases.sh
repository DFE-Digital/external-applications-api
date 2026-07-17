#!/bin/sh
# Runs EF migration bundles for both SaaS databases.
# Azure init containers typically invoke /sql/migratedb — keep that entrypoint stable.
set -eu

EA_CONN="${ConnectionStrings__DefaultConnection:-}"
TC_CONN="${ConnectionStrings__TenantConfigDatabase:-}"

if [ -z "$EA_CONN" ]; then
  echo "ERROR: ConnectionStrings__DefaultConnection is not set."
  exit 1
fi

if [ -z "$TC_CONN" ]; then
  echo "ERROR: ConnectionStrings__TenantConfigDatabase is not set."
  exit 1
fi

echo "Applying ExternalApplications migrations..."
/sql/migratedb-ea --connection "$EA_CONN"

echo "Applying TenantConfig migrations..."
/sql/migratedb-tenantconfig --connection "$TC_CONN"

echo "All database migrations applied successfully."
