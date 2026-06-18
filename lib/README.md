# Reference DLLs (not shipped with the mod)

## Unity Mod Manager

Copied from your game install (not committed to git):

```
Kingmaker_Data\Managed\UnityModManager\
  UnityModManager.dll
  0Harmony.dll
```

Place under `lib/UnityModManagerDlls/`. Referenced at build time only — the game loads them at runtime.

## NAudio + NLayer

Vendored under `lib/NAudio/` and `lib/NLayer/` (NAudio 1.10.0, NLayer 1.11.0). To fetch:

```powershell
dotnet restore tools/nuget-fetch/NugetFetch.csproj
# then copy from %USERPROFILE%\.nuget\packages\... per tools/nuget-fetch setup
```

Or copy from a built RT Narrator deploy folder. DLLs are copied to `deploy/KMNarrator/` at build time (`Private=true`).

## Game assemblies

Referenced via `GameManagedPath` in `Directory.Build.props`:

```
D:\SteamLibrary\steamapps\common\Pathfinder Kingmaker\Kingmaker_Data\Managed
```

Primary reference: **`Assembly-CSharp.dll`** (not `Code.dll`).

Do **not** commit game DLLs to this repository.
