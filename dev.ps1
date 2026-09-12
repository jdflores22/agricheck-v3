# AgriCheck V3 — Start API + Frontend (development)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

Write-Host "Starting AgriCheck V3..." -ForegroundColor Green
Write-Host "  API:      http://localhost:5000" -ForegroundColor Cyan
Write-Host "  Swagger:  http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host "  Frontend: http://localhost:5173" -ForegroundColor Cyan
Write-Host ""

$apiJob = Start-Job -ScriptBlock {
    Set-Location $using:Root
    Set-Location backend\src\AgriCheck.Api
    dotnet run --launch-profile http
}

Start-Sleep -Seconds 3

Set-Location "$Root\frontend"
npm run dev

# Cleanup when frontend exits
Stop-Job $apiJob -ErrorAction SilentlyContinue
Remove-Job $apiJob -ErrorAction SilentlyContinue
