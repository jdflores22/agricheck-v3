# AgriCheck V3 — Performance baseline (requires running API)
param(
    [string]$ApiBaseUrl = "http://localhost:5000",
    [int]$Iterations = 50
)

$ErrorActionPreference = "Stop"

function Measure-Request {
    param([string]$Method, [string]$Path, [object]$Body = $null, [hashtable]$Headers = @{})
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $params = @{
        Uri = "$ApiBaseUrl$Path"
        Method = $Method
        Headers = $Headers
        ContentType = "application/json"
        UseBasicParsing = $true
    }
    if ($null -ne $Body) { $params.Body = ($Body | ConvertTo-Json -Compress) }
    Invoke-WebRequest @params | Out-Null
    $sw.Stop()
    return $sw.ElapsedMilliseconds
}

function Get-Percentile {
    param([double[]]$Values, [double]$P)
    $sorted = $Values | Sort-Object
    $idx = [math]::Ceiling(($P / 100) * $sorted.Count) - 1
    if ($idx -lt 0) { $idx = 0 }
    return [math]::Round($sorted[$idx], 0)
}

Write-Host "AgriCheck V3 Performance Baseline — $Iterations iterations" -ForegroundColor Cyan
Write-Host "Target: $ApiBaseUrl"
Write-Host ""

$scenarios = @(
    @{ Name = "GET /health"; Method = "GET"; Path = "/health" }
    @{ Name = "GET /api/v1/health"; Method = "GET"; Path = "/api/v1/health" }
    @{
        Name = "POST /api/v1/auth/login"
        Method = "POST"
        Path = "/api/v1/auth/login"
        Body = @{ email = "admin@agricheck.local"; password = "Admin@12345" }
    }
)

foreach ($scenario in $scenarios) {
    $times = @()
    Write-Host "Running: $($scenario.Name)..." -NoNewline
    for ($i = 0; $i -lt $Iterations; $i++) {
        try {
            $ms = Measure-Request -Method $scenario.Method -Path $scenario.Path -Body $scenario.Body
            $times += $ms
        } catch {
            Write-Host " ERROR on iteration $i" -ForegroundColor Red
            break
        }
    }
    if ($times.Count -gt 0) {
        $p50 = Get-Percentile $times 50
        $p95 = Get-Percentile $times 95
        $avg = [math]::Round(($times | Measure-Object -Average).Average, 0)
        Write-Host " p50=${p50}ms p95=${p95}ms avg=${avg}ms" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "Compare results against targets in docs/P7-REGRESSION-TEST-PLAN.md section 6." -ForegroundColor Cyan
