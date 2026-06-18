# Deploy KMNarrator to the local Kingmaker install
$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$src = Join-Path $root "deploy\KMNarrator"
$dest = "D:\SteamLibrary\steamapps\common\Pathfinder Kingmaker\Mods\KMNarrator"

if (-not (Test-Path (Join-Path $src "KMNarrator.dll"))) {
    Write-Error "Build first: dotnet build KMNarrator.slnx -c Release"
}

New-Item -ItemType Directory -Force -Path $dest | Out-Null
Copy-Item -Force (Join-Path $src "KMNarrator.dll") $dest
Copy-Item -Force (Join-Path $src "NAudio.dll") $dest
Copy-Item -Force (Join-Path $src "NLayer.dll") $dest
Copy-Item -Force (Join-Path $src "Info.json") $dest

Write-Host "Deployed to $dest"
Get-ChildItem $dest -Filter *.dll | Select-Object Name, Length
