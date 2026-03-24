#requires -Version 5.1
<#
.SYNOPSIS
  Chạy local gọn 1 lệnh: dừng process cũ, build, chạy profile https.
#>
$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Set-Location $root

Write-Host "Stopping old SWD1813 dotnet processes..." -ForegroundColor Cyan
& "$PSScriptRoot\stop-dev-server.ps1"

Write-Host "Building..." -ForegroundColor Cyan
dotnet build "$root\SWD1813.csproj"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Running HTTPS profile..." -ForegroundColor Cyan
dotnet run --project "$root\SWD1813.csproj" --launch-profile https --no-build
