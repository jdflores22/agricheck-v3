# Build AgriCheck SPA and upload to Hostinger public_html.
#
# Setup (one time):
#   Copy scripts/hostinger.deploy.example.json -> scripts/hostinger.deploy.local.json
#   Preferred: sshHost, sshPort (65002), sshUser, sshPassword from hPanel -> SSH Access
#   Optional fallback: ftpHost, ftpUser, ftpPassword
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
    $prepareApk = Join-Path $repoRoot 'scripts\prepare-agritrack-download.mjs'
    if (Test-Path $prepareApk) {
        node $prepareApk
    }
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
    Write-Host 'Deploy config not found.' -ForegroundColor Yellow
    Write-Host "Copy scripts/hostinger.deploy.example.json to scripts/hostinger.deploy.local.json"
    Write-Host 'Set sshHost, sshPort, sshUser, sshPassword (Hostinger hPanel -> SSH Access).'
    Write-Host ''
    Write-Host 'Build is ready at:' $dist
    Write-Host 'Manual: upload ALL files inside dist/ to Hostinger File Manager -> public_html'
    Write-Host 'URL: https://dimgrey-hummingbird-677957.hostingersite.com/operator/containers'
    if (Test-Path $dist) { explorer.exe $dist }
    exit 2
}

$config = Get-Content $configPath -Raw | ConvertFrom-Json
$remotePath = ([string]$config.remotePath).TrimEnd('/')
$sshHost = [string]$config.sshHost
$sshPort = if ($config.sshPort) { [string]$config.sshPort } else { '65002' }
$sshUser = [string]$config.sshUser
$sshPassword = [string]$config.sshPassword
$ftpHost = [string]$config.ftpHost
$ftpUser = [string]$config.ftpUser
$ftpPassword = [string]$config.ftpPassword

$useSsh = -not [string]::IsNullOrWhiteSpace($sshHost) `
    -and -not [string]::IsNullOrWhiteSpace($sshUser) `
    -and -not [string]::IsNullOrWhiteSpace($sshPassword)

if ($useSsh) {
    $env:HOSTINGER_SSH_HOST = $sshHost
    $env:HOSTINGER_SSH_PORT = $sshPort
    $env:HOSTINGER_SSH_USER = $sshUser
    $env:HOSTINGER_SSH_PASSWORD = $sshPassword
    if ($remotePath) { $env:HOSTINGER_SSH_REMOTE_DIR = $remotePath }
    $sftpScript = Join-Path $PSScriptRoot 'deploy-hostinger-sftp.mjs'
    Write-Host "Uploading via SFTP (SSH) to ${sshHost}:${sshPort} ..."
    node $sftpScript
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    Write-Host ''
    Write-Host 'Deploy complete (SFTP).'
    Write-Host 'Verify: https://dimgrey-hummingbird-677957.hostingersite.com/operator/containers'
    exit 0
}

if ([string]::IsNullOrWhiteSpace($ftpHost) -or [string]::IsNullOrWhiteSpace($ftpUser) -or [string]::IsNullOrWhiteSpace($ftpPassword)) {
    throw 'hostinger.deploy.local.json must set SSH (sshHost, sshUser, sshPassword) or FTP (ftpHost, ftpUser, ftpPassword).'
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
