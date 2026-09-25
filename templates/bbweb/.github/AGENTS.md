# AGENTS.md — `.github/`

The workflows and the repository's GitHub settings.

| File | What it is |
|---|---|
| `workflows/ci.yml` | Build and test, the container image built and run, and the conventions check, on every push and pull request |
| `workflows/release.yml` | On a `v*.*.*` tag: the conventions, build and tests, then a self-contained single-file server per platform, with its static assets, attached to a GitHub Release |
| `repository.json` | What this repository's GitHub settings vary by: description, topics, required checks, exemptions. `scripts/repo-conventions.cs` applies and checks it |
| `copilot-instructions.md` | A pointer to the root `AGENTS.md` for tools that look here |

## Rules

| Rule | Why |
|---|---|
| **A job's name is part of the `main` ruleset.** Renaming or removing a job that `repository.json` lists under `requiredChecks` means updating the list and running `apply` in the same change | A required check that no job reports blocks every pull request, and GitHub never says why. `check` fails on the mismatch |
| The release names each asset it attaches, one per platform | A glob attaches whatever is in the folder, and silently omits a platform whose publish produced nothing |
| The release archives the whole publish folder, less the `.pdb` files | A single-file site still serves `wwwroot/` and reads its static-asset manifest from beside the executable |
| The container image is built and run in CI, and pushed nowhere | A registry is a deployment decision this repository makes for itself, in a workflow of its own |
| Nothing here pushes to nuget.org | A site is run, not referenced. Publishing a library takes what `bbpkg` generates; see the root `AGENTS.md` |
