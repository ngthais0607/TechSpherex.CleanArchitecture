#!/bin/sh
set -eu
# Creates a NEW database only. It never drops or overwrites an existing database.
if [ "$#" -ne 3 ] || [ "$2" != --confirm-target ]; then
  echo 'Usage: restore-db.sh /backups/snp-depot_TIMESTAMP.dump --confirm-target NEW_DATABASE_NAME' >&2
  exit 2
fi
backup=$1
target=$3
case "$target" in ''|*[!a-zA-Z0-9_]*) echo 'Target must contain only letters, numbers, underscores' >&2; exit 2 ;; esac
case "$target" in [0-9]*) echo 'Target must start with a letter or underscore' >&2; exit 2 ;; esac
[ "${#target}" -le 63 ] || exit 2
[ "$target" != "${PGDATABASE:-}" ] || { echo 'Refusing restore to configured application database' >&2; exit 2; }
[ "$(dirname "$backup")" = /backups ] && [ "$(readlink -f "$backup")" = "$backup" ] && [ -f "$backup" ] || { echo 'Backup must be a regular file directly inside /backups' >&2; exit 2; }
case "$(basename "$backup")" in snp-depot_????????T??????Z.dump) ;; *) echo 'Invalid backup filename' >&2; exit 2 ;; esac
pg_restore --list "$backup" >/dev/null
# createdb fails if the target exists; failure leaves every existing DB untouched.
createdb --maintenance-db=postgres --template=template0 "$target"
pg_restore --exit-on-error --single-transaction --no-owner --no-acl --dbname="$target" "$backup"
psql -X -v ON_ERROR_STOP=1 --dbname="$target" --file=/scripts/verify-restore.sql
printf 'Restored into new database %s\n' "$target"
