# Vein Mod Manager

Vein Mod Manager is a Windows desktop editor for the UE4SS `ItemAndContainerModifier` mod for VEIN.

It helps users edit item, backpack, vehicle, and container values without manually editing Lua config files.

## What It Does

- Auto-detects the VEIN Steam install when possible.
- Finds the expected UE4SS `ItemAndContainerModifier` folder.
- Writes generated overrides to `Scripts/ui_config.lua`.
- Backs up existing config files before saving.
- Imports dropped `ui_config.lua` files into the correct `Scripts` folder.
- Includes a bundled UE4SS Lua mod template.
- Keeps raw Unreal class names internally while showing friendly item names in the UI.

## Repository Layout

```text
src/VeinModManager/              Windows Forms app
tests/VeinModManager.SmokeTests/ Smoke tests for config save/load paths
docs/                            Branching, release, and security notes
.github/workflows/               Build, test, and release workflow
```

## Build and Test

Requirements:

- Windows
- .NET 8 SDK

Use the conventional solution file for local development and CI:

```powershell
dotnet restore VeinModManager.sln
dotnet build VeinModManager.sln --configuration Release --no-restore
dotnet test VeinModManager.sln --configuration Release --no-build --verbosity normal
```

## Publish

```powershell
dotnet publish src\VeinModManager\VeinModManager.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts\VeinModManager-win-x64
```

The release workflow packages:

```text
artifacts\VeinModManager-win-x64.zip
artifacts\VeinModManager-win-x64.zip.sha256
```

## Branch Flow

This repository uses:

```text
feature/* -> dev -> staging -> main
```

See [docs/BRANCHING.md](docs/BRANCHING.md).

## Security

See [SECURITY.md](SECURITY.md) for contribution safety guidelines.
