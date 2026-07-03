# Vein Dump Manager App

This folder contains the main Windows Forms desktop application.

Build from the repository root:

```powershell
dotnet build .\tools\vein-ue4ss-control-panel\Vein.Ue4ss.DumpLog.csproj -c Release
```

Publish a self-contained app payload:

```powershell
dotnet publish .\tools\vein-ue4ss-control-panel\Vein.Ue4ss.DumpLog.csproj -c Release -r win-x64 --self-contained true -o .\dist\VeinDumpManager
```

For normal users, prefer the installer built from `tools/vein-dump-manager-installer`.
