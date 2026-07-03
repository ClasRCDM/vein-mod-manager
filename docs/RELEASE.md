# Release Checklist

Use this checklist before uploading a public build.

## Code

- `dotnet restore VeinModManager.sln` passes.
- `dotnet build VeinModManager.sln --configuration Release --no-restore` passes.
- `dotnet test VeinModManager.sln --configuration Release --no-build --verbosity normal` passes.
- `dotnet publish src\VeinModManager\VeinModManager.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts\VeinModManager-win-x64` passes.
- `artifacts\VeinModManager-win-x64.zip` is uploaded by the manual workflow.
- `artifacts\VeinModManager-win-x64.zip.sha256` is uploaded with the release zip checksum.
- GitHub release artifacts are produced only by manually running the `dotnet` workflow.
- No private local files are staged.
- No generated build or diagnostic output is staged.

## App

- Auto Detect works on a normal Steam install.
- Config save creates a backup.
- `ui_config.lua` import works through the Settings cog.
- Existing generated edits are preserved after saving one new value.

## Branches

- Feature work merged to `dev`.
- `dev` promoted to `staging` after tests.
- `staging` promoted to `main` only after release rehearsal.

## Package

- Include `artifacts\VeinModManager-win-x64\`.
- Include bundled `ModTemplate`.
- Include README.
- Do not include private local settings or user-specific config.
