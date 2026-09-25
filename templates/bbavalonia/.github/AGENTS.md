# AGENTS.md — `.github/`

The workflows and the repository's GitHub settings.

| File | What it is |
|---|---|
| `workflows/ci.yml` | Build and test, the trimmed publish against its warning baseline, and the conventions check, on every push and pull request |
| `workflows/release.yml` | On a `v*.*.*` tag: the conventions, build and tests, then a trimmed single-file binary per platform, each held to the warning baseline, attached to a GitHub Release |
| `repository.json` | What this repository's GitHub settings vary by: description, topics, required checks, exemptions. `scripts/repo-conventions.cs` applies and checks it |
| `copilot-instructions.md` | A pointer to the root `AGENTS.md` for tools that look here |

## Rules

| Rule | Why |
|---|---|
| **A job's name is part of the `main` ruleset.** Renaming or removing a job that `repository.json` lists under `requiredChecks` means updating the list and running `apply` in the same change | A required check that no job reports blocks every pull request, and GitHub never says why. `check` fails on the mismatch |
| The release names each asset it attaches, one per platform | A glob attaches whatever is in the folder, and silently omits a platform whose publish produced nothing |
| Every platform's publish is held to the warning baseline, not only CI's linux-x64 | A platform-specific trim hazard would otherwise ship unexamined |
| Nothing here pushes to nuget.org | An app is run, not referenced. Publishing a library takes what `bbpkg` generates; see the root `AGENTS.md` |
