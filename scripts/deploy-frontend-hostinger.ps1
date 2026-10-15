# Build AgriCheck SPA and upload to Hostinger public_html via FTP.
#
# Setup (one time):
#   Copy scripts/hostinger.deploy.example.json -> scripts/hostinger.deploy.local.json
#   Fill ftpUser / ftpPassword from Hostinger hPanel -> Websites -> FTP Accounts
#
# Run:
#   powershell -ExecutionPolicy Bypass -File scripts/deploy-frontend-hostinger.ps1

param(
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$frontend = Join-Path $repoRoot 'frontend'
$dist = Join-Path $frontend 'dist'
$configPath = Join-Path $PSScriptRoot 'hostinger.deploy.local.json'

if (-not $SkipBuild) {
    Push-Location $frontend
    npm run build
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Pop-Location
}

if (-not (Test-Path $dist)) {
    throw "Missing build output: $dist"
}

if (-not (Test-Path $configPath)) {
    Write-Host ''
    Write-Host 'FTP config not found.' -ForegroundColor Yellow
    Write-Host "Copy scripts/hostinger.deploy.example.json to scripts/hostinger.deploy.local.json and set FTP credentials."
    Write-Host ''
    Write-Host 'Build is ready at:' $dist
    Write-Host 'Manual: upload ALL files inside dist/ to Hostinger File Manager -> public_html'
    Write-Host 'URL: https://dimgrey-hummingbird-677957.hostingersite.com/operator/containers'
    if (Test-Path $dist) { explorer.exe $dist }
    exit 2
}

$config = Get-Content $configPath -Raw | ConvertFrom-Json
$ftpHost = [string]$config.ftpHost
$ftpUser = [string]$config.ftpUser
$ftpPassword = [string]$config.ftpPassword
$remotePath = ([string]$config.remotePath).TrimEnd('/')

if ([string]::IsNullOrWhiteSpace($ftpHost) -or [string]::IsNullOrWhiteSpace($ftpUser) -or [string]::IsNullOrWhiteSpace($ftpPassword)) {
    throw 'hostinger.deploy.local.json must set ftpHost, ftpUser, and ftpPassword.'
}

function Get-FtpUri([string]$path) {
    $normalized = $path.Replace('\', '/')
    if (-not $normalized.StartsWith('/')) { $normalized = "/$normalized" }
    return "ftp://$ftpHost$normalized"
}

function Ensure-FtpDirectory([string]$remoteDir, [System.Net.NetworkCredential]$credential) {
    $parts = $remoteDir.Trim('/').Split('/', [System.StringSplitOptions]::RemoveEmptyEntries)
    $current = ''
    foreach ($part in $parts) {
        $current += "/$part"
        $uri = Get-FtpUri $current
        try {
            $request = [System.Net.FtpWebRequest]::Create($uri)
            $request.Method = [System.Net.WebRequestMethods+Ftp]::MakeDirectory
            $request.Credentials = $credential
            $request.UsePassive = $true
            $response = $request.GetResponse()
            $response.Close()
        } catch {
            # Directory may already exist.
        }
    }
}

function Upload-FtpFile([string]$localFile, [string]$remoteFile, [System.Net.NetworkCredential]$credential) {
    $uri = Get-FtpUri $remoteFile
    $request = [System.Net.FtpWebRequest]::Create($uri)
    $request.Method = [System.Net.WebRequestMethods+Ftp]::UploadFile
    $request.Credentials = $credential
    $request.UseBinary = $true
    $request.UsePassive = $true
    $bytes = [System.IO.File]::ReadAllBytes($localFile)
    $request.ContentLength = $bytes.Length
    $stream = $request.GetRequestStream()
    $stream.Write($bytes, 0, $bytes.Length)
    $stream.Close()
    $response = $request.GetResponse()
    $response.Close()
}

$credential = New-Object System.Net.NetworkCredential($ftpUser, $ftpPassword)
Ensure-FtpDirectory -remoteDir $remotePath -credential $credential

$files = Get-ChildItem -Path $dist -Recurse -File
$uploaded = 0
foreach ($file in $files) {
    $relative = $file.FullName.Substring($dist.Length).TrimStart('\', '/').Replace('\', '/')
    $remoteFile = "$remotePath/$relative"
    $remoteDir = Split-Path $remoteFile -Parent
    if ($remoteDir) {
        Ensure-FtpDirectory -remoteDir $remoteDir.Replace('\', '/') -credential $credential
    }
    Upload-FtpFile -localFile $file.FullName -remoteFile $remoteFile -credential $credential
    $uploaded++
    Write-Host "Uploaded $relative"
}

Write-Host ''
Write-Host "Done. Uploaded $uploaded files to $remotePath on $ftpHost"
Write-Host 'Verify: https://dimgrey-hummingbird-677957.hostingersite.com/operator/containers'
