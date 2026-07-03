# Branching Workflow

The project uses this flow:

```text
feature/* -> dev -> staging -> main
```

## Branches

### feature/*

Use feature branches for new work, bug fixes, UI polish, docs, and tests.

Examples:

```text
feature/settings-import
feature/nexus-release-docs
feature/category-editor-fix
```

### dev

`dev` is active development. Feature branches merge here first.

### staging

`staging` is the dress rehearsal branch. Use it for final release checks, screenshots, smoke tests, and packaged builds.

### main

`main` is production/release and should be protected. Only promote tested `staging` changes into `main`.

## Release Promotion

1. Merge feature branches into `dev`.
2. Test `dev`.
3. Merge `dev` into `staging`.
4. Publish release candidate builds from `staging`.
5. Merge `staging` into `main`.
6. Tag the release from `main`.
