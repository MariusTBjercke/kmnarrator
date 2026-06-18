# Builds a release zip for GitHub Releases (UMM mod folder layout).
# Usage: .\publish.ps1
#        .\publish.ps1 -Version 0.1.0

param(
    [string]$Version = (
        (Get-Content "$PSScriptRoot\deploy\KMNarrator\Info.json" -Raw | ConvertFrom-Json).Version
    )
)

$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$deployDir = Join-Path $root "deploy\KMNarrator"
$stageDir = Join-Path $root "artifacts\KMNarrator"
$zipPath = Join-Path $root "artifacts\KMNarrator-v$Version.zip"

Write-Host "Publishing KM Narrator v$Version..."

dotnet build (Join-Path $root "KMNarrator.slnx") -c Release

if (Test-Path $stageDir) {
    Remove-Item $stageDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $stageDir | Out-Null

Copy-Item -Recurse -Force "$deployDir\*" $stageDir
Get-ChildItem $stageDir -Filter "*.pdb" -Recurse | Remove-Item -Force

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
New-Item -ItemType Directory -Force -Path (Join-Path $root "artifacts") | Out-Null
Compress-Archive -Path $stageDir -DestinationPath $zipPath

$sizeMb = [math]::Round((Get-Item $zipPath).Length / 1MB, 2)
Write-Host ""
Write-Host "Done."
Write-Host "  Folder: $stageDir"
Write-Host "  Zip:    $zipPath ($sizeMb MB)"
Write-Host ""
Write-Host "Extract the zip into your game's Mods folder:"
Write-Host '  <Kingmaker install>\Mods\'
Write-Host ""
Write-Host "You should have Mods\KMNarrator\KMNarrator.dll (+ NAudio.dll, NLayer.dll, Info.json)."
Write-Host ""
Write-Host "Next: tag and create a GitHub Release with the zip attached."
Write-Host "  git tag v$Version"
Write-Host "  git push origin v$Version"
Write-Host "  gh release create v$Version $zipPath --title ""KM Narrator v$Version"""
