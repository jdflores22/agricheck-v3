# Validate ETL row counts — scaffold
param(
    [string]$InputDir = "$PSScriptRoot\output"
)

$ErrorActionPreference = "Stop"
Write-Host "ETL Validation (dry-run)" -ForegroundColor Cyan

$files = @("users.csv", "user_roles.csv", "agencies.csv")
$ok = $true

foreach ($f in $files) {
    $path = Join-Path $InputDir $f
    if (Test-Path $path) {
        $lines = (Get-Content $path | Measure-Object -Line).Lines - 1
        Write-Host "  OK  $f — $lines data rows" -ForegroundColor Green
    } else {
        Write-Host "  SKIP $f — not found (export not run)" -ForegroundColor Yellow
        $ok = $false
    }
}

if ($ok) {
    Write-Host "All expected files present. Run manual FK checks before production import." -ForegroundColor Green
} else {
    Write-Host "Export incomplete. This is expected until V2 export is executed." -ForegroundColor Yellow
}
