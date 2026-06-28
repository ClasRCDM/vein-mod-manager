# Roadmap

This is the current working list for getting Vein Mod Manager ready for regular public releases.

## Now

- Keep folder detection reliable for normal Steam installs.
- Make config import and saving hard to mess up.
- Preserve existing generated values when users save one new edit.
- Keep the UI polished, readable, and consistent.
- Add enough checks that release builds are not guesswork.

## Next

- Add a short install guide with screenshots.
- Add a release ZIP checklist that matches the actual shipped folder.
- Add more smoke tests for imported config files and generated output.
- Show the app version in the UI and release package.
- Prepare the first GitHub/Nexus-ready build.

## Workflow

- New work starts in `feature/*`.
- Tested work moves into `dev`.
- Release review happens in `staging`.
- Stable releases land in `main`.

## Done

- Public source repo created.
- `main`, `dev`, and `staging` set up on GitHub.
- Build and smoke tests pass from the public source tree.
