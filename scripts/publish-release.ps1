param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root "dist"
$appOut = Join-Path $dist "VeinDumpManager"
$installerOut = Join-Path $dist "VeinDumpManagerInstaller"

if (Test-Path $dist) {
    Remove-Item -LiteralPath $dist -Recurse -Force
}

New-Item -ItemType Directory -Force $dist | Out-Null

dotnet publish (Join-Path $root "tools\vein-ue4ss-control-panel\Vein.Ue4ss.DumpLog.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $appOut

dotnet publish (Join-Path $root "tools\vein-dump-manager-installer\VeinDumpManager.Installer.csproj") `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -o $installerOut

$appZip = Join-Path $dist "VeinDumpManager-ready-to-ship.zip"
$installerZip = Join-Path $dist "VeinDumpManagerInstaller-ready-to-ship.zip"

Compress-Archive -Path (Join-Path $appOut "*") -DestinationPath $appZip -Force
Compress-Archive -Path (Join-Path $installerOut "*") -DestinationPath $installerZip -Force

Write-Host "Created:"
Write-Host "  $appZip"
Write-Host "  $installerZip"
