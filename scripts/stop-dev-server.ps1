#requires -Version 5.1
<#
.SYNOPSIS
    Dừng tiến trình dotnet đang chạy SWD1813 (giải phóng khóa bin\Debug\SWD1813.dll khi build lỗi MSB3027).
#>
$ErrorActionPreference = "SilentlyContinue"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path.Replace('\', '/')
$stopped = 0
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" | ForEach-Object {
    $cmd = $_.CommandLine
    if ($null -eq $cmd) { return }
    if ($cmd -like "*SWD1813*" -or $cmd -like "*$root*") {
        Stop-Process -Id $_.ProcessId -Force
        Write-Host "Stopped dotnet PID $($_.ProcessId)" -ForegroundColor Yellow
        $stopped++
    }
}
if ($stopped -eq 0) {
    Write-Host "Không tìm thấy dotnet đang chạy SWD1813 (hoặc không có quyền dừng)." -ForegroundColor DarkGray
}
