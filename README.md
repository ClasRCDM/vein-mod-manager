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
.github/workflows/               Build and smoke-test workflow
```

## Build

Requirements:

- Windows
- .NET 8 SDK

```powershell
dotnet restore
dotnet build
```

## Smoke Test

```powershell
dotnet run --project tests\VeinModManager.SmokeTests\VeinModManager.SmokeTests.csproj -- src\VeinModManager\ModTemplate\ItemAndContainerModifier
```

## Publish

```powershell
dotnet publish src\VeinModManager\VeinModManager.csproj -c Release -r win-x64 --self-contained true
```

The published app is written to:

```text
src/VeinModManager/bin/Release/net8.0-windows/win-x64/publish/
```

## Branch Flow

This repository uses:

```text
feature/* -> dev -> staging -> main
```

See [docs/BRANCHING.md](docs/BRANCHING.md).

## Roadmap

See [ROADMAP.md](ROADMAP.md) for the current working list.

## Security

See [SECURITY.md](SECURITY.md) for contribution safety guidelines.
