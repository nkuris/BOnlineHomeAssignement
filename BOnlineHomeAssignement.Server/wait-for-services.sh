#!/bin/bash
set -euo pipefail

: ${WAIT_TIMEOUT:=300}
: ${SLEEP:=3}

echo "[wait-for-services] starting (timeout=${WAIT_TIMEOUT}s)..."

# Determine SQL host and port from connection string if available
SQL_CONN="${ConnectionStrings__DefaultConnection:-}"
SQL_HOST=""
SQL_PORT=1433

if [[ -n "${SQL_CONN}" ]]; then
  # Extract server part from connection string and split host:port
  server_part=$(echo "${SQL_CONN}" | sed -n 's/.*[sS]erver=\s*\([^;]*\).*/\1/p') || true
  if [[ -n "${server_part}" ]]; then
    # Split host and port if port is included
    SQL_HOST=$(echo "${server_part}" | cut -d',' -f1)
    # Check if port is explicitly specified in server part
    if echo "${server_part}" | grep -q ','; then
      SQL_PORT=$(echo "${server_part}" | cut -d',' -f2)
    fi
  fi
fi
SQL_HOST="${SQL_HOST:-mssql}"

if [[ -z "${SQL_HOST}" ]]; then
  echo "[wait-for-services] No SQL host configured; skipping SQL wait"
fi

wait_for_tcp() {
  local host="$1"; local port="$2"; local name="$3"
  local start_time=$(date +%s)
  echo "[wait-for-services] waiting for $name at $host:$port"
  while true; do
	if timeout 3 bash -c "echo > /dev/tcp/${host}/${port}" >/dev/null 2>&1; then
	  echo "[wait-for-services] $name reachable at $host:$port"
	  return 0
	fi
	now=$(date +%s)
	elapsed=$((now - start_time))
	if [ "$elapsed" -ge "$WAIT_TIMEOUT" ]; then
	  echo "[wait-for-services] timed out waiting for $name at $host:$port after ${WAIT_TIMEOUT}s"
	  return 1
	fi
	echo "[wait-for-services] $name not reachable yet, sleeping ${SLEEP}s..."
	sleep ${SLEEP}
  done
}

# Wait for SQL server if configured
if [ -n "$SQL_HOST" ]; then
  if ! wait_for_tcp "$SQL_HOST" "$SQL_PORT" "SQL Server"; then
	echo "[wait-for-services] SQL Server not available, exiting"
	exit 1
  fi
else
  echo "[wait-for-services] No SQL host configured; skipping SQL wait"
fi

# RabbitMQ is not used in this solution
# All dependencies are ready - exec the app
echo "[wait-for-services] all dependencies ready, launching app"
exec dotnet BOnlineHomeAssignement.Server.dll
exec dotnet BOnlineHomeAssignement.Server.dll
