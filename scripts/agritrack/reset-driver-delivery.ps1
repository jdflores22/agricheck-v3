# Reset assigned container: Assigned status, reference pickup/warehouse coords, seed GPS near pickup.
# Pickup: 14.454427, 120.990610 | Warehouse: 14.459128, 120.987523
param(
    [string]$ApiBaseUrl = "https://agricheck-v3-production.up.railway.app",
    [string]$Email = "driver@agricheck.local",
    [string]$Password = "Driver@12345",
    [string]$ContainerUuid = ""
)

$ErrorActionPreference = "Stop"

function Invoke-AgriApi {
    param(
        [string]$Method,
        [string]$Path,
        [string]$Token = "",
        [object]$Body = $null
    )
    $uri = "$($ApiBaseUrl.TrimEnd('/'))$Path"
    $headers = @{ Accept = "application/json" }
    if ($Token) { $headers.Authorization = "Bearer $Token" }
    $params = @{
        Uri = $uri
        Method = $Method
        Headers = $headers
        ContentType = "application/json"
    }
    if ($null -ne $Body) {
        $params.Body = ($Body | ConvertTo-Json -Compress)
    }
    return Invoke-RestMethod @params
}

Write-Host "Login ($Email)..." -ForegroundColor Cyan
$login = Invoke-AgriApi -Method POST -Path "/api/mobile/auth/login" -Body @{ email = $Email; password = $Password }
if (-not $login.success) {
    throw "Login failed: $($login.message)"
}
$token = $login.data.tokens.accessToken

Write-Host "List assigned containers..." -ForegroundColor Cyan
$list = Invoke-AgriApi -Method GET -Path "/api/mobile/containers/assigned" -Token $token
if (-not $list.success) {
    throw "List failed: $($list.message)"
}
$containers = @($list.data)
if ($containers.Count -eq 0) {
    throw "No assigned containers for this driver."
}

$target = if ($ContainerUuid) {
    $containers | Where-Object { $_.uuid -eq $ContainerUuid } | Select-Object -First 1
} else {
    $containers | Select-Object -First 1
}
if (-not $target) {
    throw "Container not found in assigned list."
}

Write-Host "Reset $($target.containerNumber) ($($target.uuid)) was status=$($target.status)..." -ForegroundColor Yellow
$reset = Invoke-AgriApi -Method POST -Path "/api/mobile/containers/$($target.uuid)/reset-delivery-test" -Token $token -Body @{}
if (-not $reset.success) {
    throw "Reset failed: $($reset.message)"
}

Write-Host "OK status=$($reset.data.status) GPS=$($reset.data.lastLatitude),$($reset.data.lastLongitude)" -ForegroundColor Green
