<#
    Publishes a portable, self-contained build of TripleDevBox.

    The result (dist\win-x64) runs on any Windows 10/11 x64 machine with NO
    prerequisites installed - the .NET runtime and the Windows App SDK are both
    bundled. The csproj trim target removes unused Windows ML / DirectML / NPU
    components and non-English WinUI satellites.

    Usage:  .\publish.ps1            # publish + report
            .\publish.ps1 -Zip       # also produce dist\TripleDevBox-win-x64.zip
#>
param([switch]$Zip)

$ErrorActionPreference = 'Stop'
$proj = Join-Path $PSScriptRoot 'TripleDevBox.csproj'
$dist = Join-Path $PSScriptRoot 'dist\win-x64'

if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }

Write-Host "Publishing self-contained (Release, win-x64)..." -ForegroundColor Cyan
dotnet publish $proj -c Release -r win-x64 --self-contained true -o $dist
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }

$files = Get-ChildItem $dist -Recurse -File
$sizeMB = [math]::Round(($files | Measure-Object Length -Sum).Sum / 1MB, 1)
Write-Host ""
Write-Host "Published to : $dist"           -ForegroundColor Green
Write-Host "Files        : $($files.Count)"
Write-Host "Total size   : $sizeMB MB"
Write-Host "Launcher     : TripleDevBox.exe"

# Sanity: confirm the .NET runtime really was bundled (self-contained).
$bundled = @('coreclr.dll','hostfxr.dll','hostpolicy.dll') |
    Where-Object { Test-Path (Join-Path $dist $_) }
Write-Host "Bundled .NET runtime files present: $($bundled -join ', ')"

if ($Zip) {
    $zip = Join-Path $PSScriptRoot 'dist\TripleDevBox-win-x64.zip'
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path (Join-Path $dist '*') -DestinationPath $zip
    Write-Host "Zip          : $zip ($([math]::Round((Get-Item $zip).Length/1MB,1)) MB)" -ForegroundColor Green
}
