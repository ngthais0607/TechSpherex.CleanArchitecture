#!/bin/sh
set -eu
: "${ConnectionStrings__DefaultConnection:?Set database connection string}"
: "${PGDATABASE:?Set PGDATABASE}"
mode=${MIGRATION_MODE:-fresh}
has_migration() {
  psql -X -v ON_ERROR_STOP=1 -tAc "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name='__EFMigrationsHistory')" | grep -qx t || return 1
  psql -X -v ON_ERROR_STOP=1 -tAc "SELECT EXISTS (SELECT 1 FROM \"__EFMigrationsHistory\" WHERE migration_id='$1')" | grep -qx t
}
case "$mode" in
  fresh)
    /app/efbundle
    ;;
  upgrade)
    if has_migration 20261016012629_RequireUserTenant; then
      /app/efbundle
      exit 0
    fi
    if psql -X -v ON_ERROR_STOP=1 -tAc "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name='__EFMigrationsHistory')" | grep -qx t; then
      if psql -X -v ON_ERROR_STOP=1 -tAc "SELECT EXISTS (SELECT 1 FROM \"__EFMigrationsHistory\" WHERE migration_id > '20261016012629_RequireUserTenant')" | grep -qx t; then
        echo 'Inconsistent migration history: refusing an implicit downgrade' >&2
        exit 1
      fi
    fi
    if ! has_migration 20261016012538_AddUserTenant; then
      /app/efbundle 20261016012538_AddUserTenant
    fi
    if [ ! -s /migration/user-tenants.csv ]; then
      echo 'Upgrade stopped: mount approved user-tenants.csv at /migration/user-tenants.csv' >&2
      exit 1
    fi
    psql -X -v ON_ERROR_STOP=1 <<'SQL'
BEGIN;
CREATE TEMP TABLE user_tenant_mapping(user_id uuid PRIMARY KEY, tenant_id varchar(50) NOT NULL);
\copy user_tenant_mapping FROM '/migration/user-tenants.csv' CSV HEADER
\i /app/scripts/apply-user-tenants.sql
COMMIT;
SQL
    /app/efbundle 20261016012629_RequireUserTenant
    /app/efbundle
    ;;
  *) echo 'MIGRATION_MODE must be fresh or upgrade' >&2; exit 2 ;;
esac
