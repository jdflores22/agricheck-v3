# AgriCheck V3 - API smoke test (requires running API)
param(
    [string]$ApiBaseUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"
$script:passed = 0
$script:failed = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method = "GET",
        [string]$Path,
        [hashtable]$Headers = @{},
        [object]$Body = $null,
        [int[]]$ExpectedStatus = @(200)
    )

    $uri = "$ApiBaseUrl$Path"
    try {
        $params = @{
            Uri = $uri
            Method = $Method
            Headers = $Headers
            ContentType = "application/json"
            UseBasicParsing = $true
        }
        if ($null -ne $Body) {
            $params.Body = ($Body | ConvertTo-Json -Compress)
        }
        $response = Invoke-WebRequest @params
        if ($ExpectedStatus -contains $response.StatusCode) {
            Write-Host "PASS: $Name ($($response.StatusCode))" -ForegroundColor Green
            $script:passed++
            return $response
        }
        Write-Host "FAIL: $Name - expected $($ExpectedStatus -join '/') got $($response.StatusCode)" -ForegroundColor Red
        $script:failed++
    }
    catch {
        $code = $null
        if ($_.Exception.Response) { $code = $_.Exception.Response.StatusCode.value__ }
        if ($code -and ($ExpectedStatus -contains $code)) {
            Write-Host "PASS: $Name ($code)" -ForegroundColor Green
            $script:passed++
            return $null
        }
        Write-Host "FAIL: $Name - $($_.Exception.Message)" -ForegroundColor Red
        $script:failed++
    }
    return $null
}

function Get-Token {
    param([string]$Email, [string]$Password, [string]$Label)
    $body = @{ email = $Email; password = $Password }
    $resp = Test-Endpoint -Name "Login $Label" -Method POST -Path "/api/v1/auth/login" -Body $body
    if ($null -eq $resp) { return $null }
    $json = $resp.Content | ConvertFrom-Json
    if (-not $json.success) {
        Write-Host "FAIL: Login $Label" -ForegroundColor Red
        $script:failed++
        return $null
    }
    return $json.data.tokens.accessToken
}

Write-Host "AgriCheck V3 Smoke Test - $ApiBaseUrl" -ForegroundColor Cyan
Write-Host ""

Test-Endpoint -Name "Health /health" -Path "/health"
Test-Endpoint -Name "Health /api/v1/health" -Path "/api/v1/health"
Test-Endpoint -Name "Mobile health" -Path "/api/mobile/health"

$accounts = @(
    @{ Email = "admin@agricheck.local"; Password = "Admin@12345"; Label = "Admin"; Path = "/api/v1/admin/dashboard" },
    @{ Email = "importer@agricheck.local"; Password = "Importer@12345"; Label = "Importer"; Path = "/api/v1/client/dashboard" },
    @{ Email = "evaluator@agricheck.local"; Password = "Evaluator@12345"; Label = "Evaluator"; Path = "/api/v1/agency/dashboard" },
    @{ Email = "mav.admin@agricheck.local"; Password = "MavAdmin@12345"; Label = "MAV Admin"; Path = "/api/v1/mav/admin/dashboard" },
    @{ Email = "warehouse@agricheck.local"; Password = "Warehouse@12345"; Label = "Warehouse"; Path = "/api/v1/ops/warehouse/dashboard" },
    @{ Email = "driver@agricheck.local"; Password = "Driver@12345"; Label = "Driver"; Path = "/api/v1/ops/driver/dashboard" }
)

foreach ($acct in $accounts) {
    $token = Get-Token -Email $acct.Email -Password $acct.Password -Label $acct.Label
    if ($token) {
        Test-Endpoint -Name "$($acct.Label) dashboard" -Path $acct.Path -Headers @{ Authorization = "Bearer $token" }
    }
}

Test-Endpoint -Name "Mobile login" -Method POST -Path "/api/mobile/auth/login" -Body @{
    email = "driver@agricheck.local"
    password = "Driver@12345"
}

Write-Host ""
Write-Host "Results: $script:passed passed, $script:failed failed" -ForegroundColor $(if ($script:failed -eq 0) { "Green" } else { "Red" })
if ($script:failed -gt 0) { exit 1 }
