# Build AgriCheck SPA and prepare files for Hostinger public_html upload.
# The live site (hostingersite.com) does NOT auto-update from git — upload frontend/dist after this script.

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$frontend = Join-Path $repoRoot 'frontend'

Push-Location $frontend
npm run build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Pop-Location

$dist = Join-Path $frontend 'dist'
Write-Host ''
Write-Host 'Build OK:' $dist
Write-Host 'Upload ALL contents of frontend/dist to Hostinger File Manager -> public_html'
Write-Host 'Then hard-refresh: https://dimgrey-hummingbird-677957.hostingersite.com/operator/containers'
Write-Host ''

if (Test-Path $dist) {
    explorer.exe $dist
}
