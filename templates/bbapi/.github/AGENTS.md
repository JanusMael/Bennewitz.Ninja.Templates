# AGENTS.md — `.github/`

The workflows and the repository's GitHub settings.

| File | What it is |
|---|---|
| `workflows/ci.yml` | Build and test, the native container image built and requested, and the conventions check, on every push and pull request |
| `workflows/release.yml` | On a `v*.*.*` tag: the conventions, build and tests, then one native executable per platform, each built on a runner of its own operating system, attached to a GitHub Release |
| `repository.json` | What this repository's GitHub settings vary by: description, topics, required checks, exemptions. `scripts/repo-conventions.cs` applies and checks it |
| `copilot-instructions.md` | A pointer to the root `AGENTS.md` for tools that look here |

## Rules

| Rule | Why |
|---|---|
| **A job's name is part of the `main` ruleset.** Renaming or removing a job that `repository.json` lists under `requiredChecks` means updating the list and running `apply` in the same change | A required check that no job reports blocks every pull request, and GitHub never says why. `check` fails on the mismatch |
| Each platform publishes on a runner of its own operating system | Native AOT links with the platform's own toolchain; there is no cross-OS publish |
| The release names each asset it attaches, one per platform | A glob attaches whatever is in the folder, and silently omits a platform whose publish produced nothing |
| CI's `container` job requests the native API | It is the only place the native binary answers a request before a release; the tests run under the JIT |
| The container image is built and run in CI, and pushed nowhere | A registry is a deployment decision this repository makes for itself, in a workflow of its own |
| Nothing here pushes to nuget.org | An API is run, not referenced. Publishing a library takes what `bbpkg` generates; see the root `AGENTS.md` |
