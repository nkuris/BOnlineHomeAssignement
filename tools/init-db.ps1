# tools/init-db.ps1
# Helper to start DB, apply EF migrations, and bring up the app + client for local development.
# Usage: powershell -ExecutionPolicy Bypass -File .\tools\init-db.ps1 [-SaPassword <password>]
param(
	[string]$SaPassword = $null
)

$ErrorActionPreference = 'Stop'

function Read-EnvFileValue($path, $key) {
	if (-not (Test-Path $path)) { return $null }
	$lines = Get-Content $path | Where-Object { $_ -match "^\s*[^#]" }
	foreach ($l in $lines) {
		if ($l -match "^\s*($([regex]::Escape($key)))\s*=\s*(.*)\s*$") {
			return $matches[2].Trim()
		}
	}
	return $null
}

# Prefer explicit parameter, then .env, then environment variable
if (-not $SaPassword) {
	$envFile = Join-Path $PSScriptRoot '..\.env' | Resolve-Path -ErrorAction SilentlyContinue
	if ($envFile) { $envFile = $envFile.Path }
	$val = $null
	if ($envFile) { $val = Read-EnvFileValue $envFile 'MSSQL_SA_PASSWORD' }
	if (-not $val) { $val = [Environment]::GetEnvironmentVariable('MSSQL_SA_PASSWORD') }
	if (-not $val) {
		Write-Host "MSSQL SA password not found in .env or environment. Please provide with -SaPassword or set .env." -ForegroundColor Yellow
		exit 1
	}
	$SaPassword = $val
}

Write-Host "Using SA password from input or .env. (not echoed)"
$env:MSSQL_SA_PASSWORD = $SaPassword

# Start only the DB first
Write-Host "Bringing up mssql container..."
docker compose up -d mssql

# Wait for TCP port 1433 on localhost to be open (max ~5 minutes)
$maxAttempts = 150
$attempt = 0
while ($attempt -lt $maxAttempts) {
	$attempt++
	$ok = Test-NetConnection -ComputerName 'localhost' -Port 1433 -WarningAction SilentlyContinue
	if ($ok -and $ok.TcpTestSucceeded) { break }
	Write-Host "Waiting for mssql TCP (attempt $attempt/$maxAttempts)..." -NoNewline
	Start-Sleep -Seconds 2
}
if ($attempt -ge $maxAttempts) {
	Write-Host "Timed out waiting for mssql TCP port 1433" -ForegroundColor Red
	docker compose ps
	exit 1
}
Write-Host "mssql TCP reachable"

# No migrator service in this repo. The server applies migrations in background on startup.
# Start server and client now; migrations will run inside the server process.
Write-Host "No migrator configured. Starting server and client; migrations will run in the server's background service."

# Start server and client
Write-Host "Starting server and client containers..."
docker compose up --build -d server client

# Wait for server health endpoint
$healthUrl = 'http://localhost:5000/health'
$maxAttempts = 60
$attempt = 0
while ($attempt -lt $maxAttempts) {
	$attempt++
	try {
		$resp = Invoke-RestMethod -Uri $healthUrl -UseBasicParsing -TimeoutSec 5
		if ($resp -and $resp.status -eq 'Healthy') {
			Write-Host "Server health = Healthy"
			exit 0
		}
		else {
			Write-Host "Health endpoint returned: $($resp.status) - waiting..."
		}
	}
	catch {
		Write-Host "Waiting for server health (attempt $attempt/$maxAttempts)..."
	}
	Start-Sleep -Seconds 2
}
Write-Host "Server did not report healthy after waiting." -ForegroundColor Yellow
Write-Host "Check logs: docker logs --follow <server_container_name>" 
exit 1
