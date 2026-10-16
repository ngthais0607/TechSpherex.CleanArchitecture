#!/bin/sh
set -eu
umask 077
: "${PGDATABASE:?Set PGDATABASE}"
: "${PGUSER:?Set PGUSER}"
backup_dir=/backups
retention=${BACKUP_RETENTION:-7}
case "$retention" in ''|*[!0-9]*) echo 'Retention must be a positive integer' >&2; exit 2 ;; esac
[ "$retention" -gt 0 ] || exit 2
[ "$retention" -le 10000 ] || { echo 'Retention must not exceed 10000' >&2; exit 2; }
mkdir -p "$backup_dir"
[ "$(readlink -f "$backup_dir")" = /backups ] || { echo 'Unsafe backup directory' >&2; exit 2; }
stamp=$(date -u +%Y%m%dT%H%M%SZ)
output="$backup_dir/snp-depot_$stamp.dump"
[ ! -e "$output" ] || { echo 'Backup timestamp collision; retry next second' >&2; exit 1; }
partial="$output.partial"
trap 'rm -f -- "$partial"' EXIT HUP INT TERM
pg_dump --format=custom --no-owner --no-acl --file="$partial" "$PGDATABASE"
pg_restore --list "$partial" >/dev/null
mv -- "$partial" "$output"
# Prune only completed files with the exact generated numeric timestamp pattern.
find "$backup_dir" -maxdepth 1 -type f -name 'snp-depot_????????T??????Z.dump' -print |
  grep -E '^/backups/snp-depot_[0-9]{8}T[0-9]{6}Z\.dump$' | LC_ALL=C sort -r |
  tail -n "+$((retention + 1))" | while IFS= read -r old; do
    [ "$(dirname "$old")" = /backups ] || exit 2
    rm -- "$old"
  done
printf '%s\n' "$output"
