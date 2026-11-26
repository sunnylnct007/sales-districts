# Starts the telemetry dashboard on http://localhost:18888, starting Docker Desktop first if needed.
# Safe to run repeatedly.

docker info *> $null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Starting Docker Desktop..."
    Start-Process "$env:ProgramFiles\Docker\Docker\Docker Desktop.exe"

    $deadline = (Get-Date).AddMinutes(2)
    do {
        Start-Sleep -Seconds 3
        docker info *> $null
    } until ($LASTEXITCODE -eq 0 -or (Get-Date) -gt $deadline)

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Docker Desktop did not start within 2 minutes."
        exit 1
    }
}

docker compose -f "$PSScriptRoot\..\compose.yaml" up -d aspire-dashboard
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Telemetry dashboard: http://localhost:18888"
