# ===========================================================================
#  Noor — Windows installer build script (PowerShell)
#
#  Publishes Noor self-contained for win-x64, then runs Inno Setup (iscc)
#  to produce dist\NoorSetup-<version>-win-x64.exe
#
#  Usage (from repo root):
#    pwsh packaging/windows/build-installer.ps1
#
#  Requires:
#    - .NET 10 SDK
#    - Inno Setup 6 (iscc on PATH)  https://jrsoftware.org/isdl.php
# ===========================================================================
[CmdletBinding()]
param(
    [string]$Version = $env:NOOR_VERSION,
    [string]$Framework = "net10.0-windows10.0.19041.0",
    [string]$Runtime = "win-x64",
    [string]$PublishDir = "publish/win"
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path "$PSScriptRoot/../..").Path
Set-Location $Root

if (-not $Version) { $Version = "1.0.0" }
$env:NOOR_VERSION = $Version

Write-Host "==> Publishing Noor ($Framework, $Runtime, self-contained)..." -ForegroundColor Cyan
dotnet publish Noor/Noor.csproj `
    -c Release `
    -f $Framework `
    -r $Runtime `
    --self-contained `
    -p:PublishSingleFile=False `
    -p:PublishTrimmed=False `
    -o $PublishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }

$iscc = (Get-Command iscc -ErrorAction SilentlyContinue).Source
if (-not $iscc) {
    $iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
}
if (-not (Test-Path $iscc)) {
    throw "Inno Setup compiler (iscc.exe) not found. Install Inno Setup 6: https://jrsoftware.org/isdl.php"
}

Write-Host "==> Running Inno Setup..." -ForegroundColor Cyan
& $iscc /Qp "$Root\packaging\windows\noor.iss"
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed (exit $LASTEXITCODE)" }

$out = Join-Path $Root "dist\NoorSetup-$Version-$Runtime.exe"
Write-Host "==> Done: $out" -ForegroundColor Green
Get-Item $out | Format-List Name, Length, FullName
