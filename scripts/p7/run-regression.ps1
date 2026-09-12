# AgriCheck V3 — Full regression runner
param(
    [string]$ApiBaseUrl = "http://localhost:5000",
    [switch]$SkipSmoke
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

Write-Host "=== AgriCheck V3 Regression Suite ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "[1/2] Integration tests..." -ForegroundColor Yellow
Push-Location (Join-Path $Root "backend")
dotnet test tests/AgriCheck.IntegrationTests --logger "console;verbosity=minimal"
if ($LASTEXITCODE -ne 0) {
    Pop-Location
    Write-Host "Integration tests FAILED" -ForegroundColor Red
    exit 1
}
Pop-Location
Write-Host "Integration tests PASSED" -ForegroundColor Green
Write-Host ""

if (-not $SkipSmoke) {
    Write-Host "[2/2] API smoke test ($ApiBaseUrl)..." -ForegroundColor Yellow
    & "$PSScriptRoot\smoke-test.ps1" -ApiBaseUrl $ApiBaseUrl
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Smoke test FAILED (is API running?)" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "[2/2] Smoke test SKIPPED" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Regression suite PASSED ===" -ForegroundColor Green
