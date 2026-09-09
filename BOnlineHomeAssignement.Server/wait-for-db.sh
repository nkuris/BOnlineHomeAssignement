#!/usr/bin/env bash
# wait-for-db.sh
# Wait for SQL Server TCP port and attempt login check (sqlcmd or bundled dotnet tool) then exec the supplied command.

set -euo pipefail

# Prefer full connection string if provided
CONN_STRING="${ConnectionStrings__DefaultConnection:-}"

# Fallback host/port
DB_HOST=${DB_HOST:-mssql}
DB_PORT=${DB_PORT:-1433}

if [ -n "$CONN_STRING" ]; then
  # try to parse Server=host,port from the connection string
  server=$(echo "$CONN_STRING" | awk -F'Server=' '{print $2}' | awk -F';' '{print $1}' 2>/dev/null || true)
  server=$(echo "$server" | sed -E 's/^tcp://I' | sed -E 's/^\s+|\s+$//')
  if [ -n "$server" ]; then
	host=$(echo "$server" | awk -F',' '{print $1}')
	port=$(echo "$server" | awk -F',' '{print $2}')
	DB_HOST=${host:-$DB_HOST}
	DB_PORT=${port:-$DB_PORT}
  fi
fi

echo "Waiting for SQL Server at $DB_HOST:$DB_PORT..."

tries=0
max=180
sleep_seconds=2

while [ $tries -lt $max ]; do
  tries=$((tries + 1))
  echo "Attempt $tries: checking TCP $DB_HOST:$DB_PORT..."

  # Try TCP connect using /dev/tcp (built-in) or nc
  if (exec 3<>/dev/tcp/"$DB_HOST"/"$DB_PORT") 2>/dev/null; then
	echo "TCP connect OK"
	break
  fi

  if command -v nc >/dev/null 2>&1; then
	if nc -z "$DB_HOST" "$DB_PORT" >/dev/null 2>&1; then
	  echo "TCP connect OK (nc)"
	  break
	fi
  fi

  echo "Not reachable yet, sleeping ${sleep_seconds}s..."
  sleep $sleep_seconds
done

if [ $tries -ge $max ]; then
  echo "Timed out waiting for TCP $DB_HOST:$DB_PORT after $max attempts" >&2
  exit 1
fi

# If we have a connection string and a check tool, attempt login test
if [ -n "$CONN_STRING" ]; then
  echo "Connection string available; attempting login check"

  # If sqlcmd exists, use it (will require SA credentials to be present in env or connection string)
  if [ -x /opt/mssql-tools/bin/sqlcmd ]; then
	echo "Using sqlcmd to validate login"
	user=$(echo "$CONN_STRING" | tr ';' '\n' | grep -i -E 'User Id=|User=|Uid=' | head -n1 | sed -E 's/.*=//') || true
	password=$(echo "$CONN_STRING" | tr ';' '\n' | grep -i 'Password=' | head -n1 | sed -E 's/Password=//I') || true
	hostpart=$(echo "$CONN_STRING" | awk -F'Server=' '{print $2}' | awk -F';' '{print $1}' || true)
	hostpart=$(echo "$hostpart" | sed -E 's/^tcp://I' || true)
	host=$(echo "$hostpart" | awk -F',' '{print $1}' || true)
	port=$(echo "$hostpart" | awk -F',' '{print $2}' || true)
	port=${port:-1433}
	if [ -n "$user" ] && [ -n "$password" ]; then
	  if /opt/mssql-tools/bin/sqlcmd -S "$host,$port" -U "$user" -P "$password" -Q "SELECT 1" >/dev/null 2>&1; then
		echo "sqlcmd login succeeded"
		exec "$@"
	  else
		echo "sqlcmd login failed with provided credentials" >&2
		exit 2
	  fi
	else
	  echo "sqlcmd available but connection string does not contain user/password; skipping sqlcmd login test"
	fi
  fi

  # If our bundled dotnet checker exists, use it
  if [ -f /app/tools/checksa/CheckSa.dll ]; then
	echo "Using bundled dotnet checker to validate login"
	if dotnet /app/tools/checksa/CheckSa.dll "$CONN_STRING" >/dev/null 2>&1; then
	  echo "Dotnet checker login succeeded"
	  exec "$@"
	else
	  echo "Dotnet checker login failed" >&2
	  exit 3
	fi
  fi

  echo "No login checker available (sqlcmd or bundled dotnet). Proceeding after TCP success."
fi

# If requested, wait for migrations sentinel file written by an external migrator service
if [ "${WAIT_FOR_MIGRATIONS:-}" = "true" ]; then
  SENTINEL_PATH="${MIGRATION_SENTINEL:-/app/migrations/migrations_done}"
  echo "WAIT_FOR_MIGRATIONS is true; waiting for sentinel file at $SENTINEL_PATH"
  mig_tries=0
  mig_max=300
  mig_sleep=2
  while [ $mig_tries -lt $mig_max ]; do
	mig_tries=$((mig_tries + 1))
	if [ -f "$SENTINEL_PATH" ]; then
	  echo "Migration sentinel found: $SENTINEL_PATH"
	  break
	fi
	echo "Migration sentinel not present yet, sleeping ${mig_sleep}s... (attempt $mig_tries/$mig_max)"
	sleep $mig_sleep
  done
  if [ $mig_tries -ge $mig_max ]; then
	echo "Timed out waiting for migration sentinel after $mig_max attempts" >&2
	exit 1
  fi
fi

exec "$@"
