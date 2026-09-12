# Import V3 staging from ETL output — scaffold
param(
    [Parameter(Mandatory = $true)]
    [string]$V3ConnectionString,

    [string]$InputDir = "$PSScriptRoot\output"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $InputDir)) {
    throw "Input directory not found: $InputDir. Run export-v2-users.ps1 first."
}

Write-Host "V3 ETL Import — staging scaffold" -ForegroundColor Cyan
Write-Host "V3 connection: $($V3ConnectionString -replace 'Password=[^;]+', 'Password=***')"
Write-Host ""
Write-Host "Expected input files (not yet present = dry-run only):"
@("users.csv", "user_roles.csv", "agencies.csv") | ForEach-Object {
    $path = Join-Path $InputDir $_
    $exists = Test-Path $path
    Write-Host "  [$([char]$(if ($exists) { 10003 } else { 10007 }))] $_"
}

Write-Host ""
Write-Host "Production import requires:" -ForegroundColor Yellow
Write-Host "  1. Custom C# CLI or SQL scripts with idempotent upserts"
Write-Host "  2. Staging dry-run per scripts/etl/README.md"
Write-Host "  3. PO sign-off before production load"
