# NetTrafficMon Build Script
# Uses native Windows built-in Microsoft .NET Framework C# compiler (csc.exe)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $scriptDir) { $scriptDir = (Get-Location).Path }

$cscPath = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $cscPath)) {
    $cscPath = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $cscPath)) {
    Write-Error "csc.exe not found in standard .NET Framework directories."
    exit 1
}

$sources = (Get-ChildItem -Path "$scriptDir\src\*.cs").FullName
$references = @(
    "System.dll",
    "System.Core.dll",
    "System.Drawing.dll",
    "System.Windows.Forms.dll",
    "System.Web.Extensions.dll"
)
$refArgs = $references | ForEach-Object { "/r:$_" }
$outExe = "$scriptDir\NetTrafficMon.exe"
$tempExe = "$scriptDir\NetTrafficMon_temp.exe"
$iconArg = ""

if (Test-Path "$scriptDir\app.ico") {
    $iconArg = "/win32icon:$scriptDir\app.ico"
}

Write-Host "Compiling NetTrafficMon..." -ForegroundColor Cyan
& $cscPath /nologo /target:winexe /optimize+ /platform:anycpu $iconArg $refArgs "/out:$tempExe" $sources

if ($LASTEXITCODE -eq 0 -and (Test-Path $tempExe)) {
    if (Test-Path $outExe) {
        $oldExe = "$outExe.old"
        if (Test-Path $oldExe) { Remove-Item $oldExe -Force -ErrorAction SilentlyContinue }
        try {
            Remove-Item $outExe -Force -ErrorAction Stop
        } catch {
            Rename-Item -Path $outExe -NewName (Split-Path $oldExe -Leaf) -Force -ErrorAction SilentlyContinue
        }
    }
    Move-Item -Path $tempExe -Destination $outExe -Force

    $fileSize = (Get-Item $outExe).Length / 1KB
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host " Build Successful: NetTrafficMon.exe" -ForegroundColor Green
    Write-Host " Size: $([Math]::Round($fileSize, 1)) KB" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Green
} else {
    Write-Host "Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}
