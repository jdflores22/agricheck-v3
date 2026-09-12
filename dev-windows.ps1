# AgriCheck V3 — Start API + Frontend in separate windows

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path

Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$Root\backend\src\AgriCheck.Api'; dotnet run --launch-profile http"
Start-Sleep -Seconds 2
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$Root\frontend'; npm run dev"

Write-Host "AgriCheck V3 started in separate windows." -ForegroundColor Green
