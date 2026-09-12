# Export V2 users (read-only) — scaffold
# Usage: .\export-v2-users.ps1 -V2ConnectionString "Server=...;Database=agricheck;..."

param(
    [Parameter(Mandatory = $true)]
    [string]$V2ConnectionString,

    [string]$OutputDir = "$PSScriptRoot\output"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null

Write-Host "V2 ETL Export — users scaffold" -ForegroundColor Cyan
Write-Host "This script documents the export queries. Wire to mysql CLI or MySqlConnector for production use."
Write-Host ""

$queries = @{
    users = @"
SELECT u.id, u.email, u.password, u.is_verified, u.created_at,
       p.first_name, p.last_name, p.company_name
FROM user u
LEFT JOIN user_profile p ON p.user_id = u.id
WHERE u.deleted_at IS NULL
"@
    roles = @"
SELECT u.email, r.code
FROM user u
JOIN user_role ur ON ur.user_id = u.id
JOIN role r ON r.id = ur.role_id
"@
    agencies = @"
SELECT code, name, is_active FROM agency
"@
}

$manifest = @{
    exportedAt = (Get-Date).ToUniversalTime().ToString("o")
    v2Connection = ($V2ConnectionString -replace "Password=[^;]+", "Password=***")
    queries = $queries
    note = "Run queries against V2 MySQL with read-only credentials. Save results as CSV in output/."
}

$manifestPath = Join-Path $OutputDir "export-manifest.json"
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $manifestPath -Encoding UTF8

Write-Host "Wrote manifest: $manifestPath" -ForegroundColor Green
Write-Host "Next: execute queries via mysql client, then run import-v3-staging.ps1 on staging."
