# AGENTS.md — `.github/`

This repository's own workflows and settings. The workflows under
`templates/bbpkg/.github/` are shipped content and never run here.

| File | What it is |
|---|---|
| `workflows/ci.yml` | `verify`: the tests and `verify-release`. `conventions`: `repo-conventions check` |
| `workflows/release.yml` | Publishes `Bennewitz.Ninja.Templates` on a `v*.*.*` tag, after the tests, the packaging guard and `verify-release`. Dispatched with the version blank, it only proves the credentials |
| `repository.json` | This repository's description, topics and required checks. `content` lists `templates/bbpkg`, whose markers are intended |
| `copilot-instructions.md` | A pointer to the root `AGENTS.md` |

## Rules

| Rule | Why |
|---|---|
| **The job names `verify` and `conventions` are required checks.** Renaming one means changing `repository.json` and running `apply` in the same change | A required check that no job reports blocks every pull request, and GitHub never says why |
| **`release.yml` keeps its file name** | The trusted-publishing policy on nuget.org names it, and the OIDC token is bound to the name |
| Every `dotnet run` of a script passes `--file` | From this root the bare form binds to the packaging project. `PackagingTests` checks both workflows |
