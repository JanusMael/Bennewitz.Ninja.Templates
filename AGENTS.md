# AGENTS.md — Bennewitz.Ninja.Templates

> For anyone changing this repository, human or agent. The invariants a change must not break, the
> commands, and the checklists for recurring work. Each top-level directory has an `AGENTS.md` of
> its own. Work state is [`PROGRESS.md`](PROGRESS.md); plans are in [`plans/`](plans/).

## What this repository is

The `bbpkg` template, shipped two ways: as the NuGet package `Bennewitz.Ninja.Templates`
(`dotnet new bbpkg`) and as a GitHub template repository. A generated repository publishes .NET
packages to nuget.org through trusted publishing, with CI, a release workflow, the two-list
packaging guard, and the family's documentation and conventions.

It is also where the family's conventions are prescribed:
[`docs/repository-conventions.md`](docs/repository-conventions.md), enforced by
`scripts/repo-conventions.cs` in every family repository.

## Layout

| Directory | What it holds |
|---|---|
| `templates/` | The shipped content. `templates/bbpkg/` is the template; nothing beneath it describes this repository |
| `scripts/` | File-based apps: the release gate, the packaging guard, the conventions check, and a test-suite converter |
| `tests/` | `Templates.Tests`, which reads this repository's files and runs its scripts |
| `docs/` | The conventions, runbooks, and records of past decisions |
| `plans/` | Numbered plans; approved ones are frozen |
| `.github/` | This repository's own workflows and `repository.json` |

## Invariants

| Invariant | Failure if broken | Guarded by |
|---|---|---|
| The packaging project packs `templates/**` with `PackagePath="content/"`: the bare root, with a forward slash | `.template.config/` is flattened away. The package still installs, lists and "generates", emitting the extracted nupkg | `Bennewitz.Ninja.Templates.csproj` comments; `scripts/verify-release.cs` content and tree checks |
| `NoDefaultExcludes` stays on | NuGet drops dotfiles, so a generated repository arrives without `.gitignore`, `.gitattributes` and `.github/` | `Bennewitz.Ninja.Templates.csproj`; `verify-release` |
| Every placeholder is substituted: each template's stem, `PKG_ID`, `REPO_OWNER`, `REPO_NAME` | A generated repository carries a literal placeholder | `verify-release`, placeholder check |
| Every template has a case in `verify-release` | A template ships without ever being generated, built or tested | `verify-release`, which compares `templates/` and the package against its cases |
| Every file a generated repository must carry is on `verify-release`'s required list | A file dropped from the template ships nowhere, and nothing fails | `verify-release`, `required` |
| `templates/bbpkg/scripts/repo-conventions.cs` is the canonical copy; `scripts/repo-conventions.cs` is identical to it | This repository checks itself with a different script than it ships | `repo-conventions check`, drift; this repository's `conventions` job |
| The release pushes the ids `packages.push` names, never a glob, in both release workflows | A package nobody chose is published, permanently | `PackagingTests` |
| A script run from this root passes `--file` | `dotnet run <file.cs>` binds to `Bennewitz.Ninja.Templates.csproj` instead and fails | `PackagingTests.A_script_run_from_this_repository_root_passes_the_file_flag` |
| An approved plan is never edited | The record of what was agreed turns into a record of what was built | `plans/AGENTS.md` |

## Commands

```bash
dotnet test tests/Templates.Tests/Templates.Tests.csproj -c Release
dotnet run --file scripts/verify-release.cs
dotnet run --file scripts/verify-release.cs -- --published <version>
dotnet run --file scripts/repo-conventions.cs -- check
dotnet run --file scripts/repo-conventions.cs -- check --repo JanusMael/<repository>
```

- `verify-release` packs this tree, installs it from the `.nupkg` into a template hive of its own,
  generates a repository from every template, `bbweb` in both variants, builds and tests each, and
  publishes each app for this machine. On Windows the native `bbapi` publish needs the C++ tools of
  Visual Studio or Build Tools; on Linux, `clang` and `zlib1g-dev`, which GitHub's Ubuntu runners carry.
  Run it before tagging, and with `--published` after releasing. It leaves the global template
  registration alone.
- Tests run on Microsoft.Testing.Platform (`global.json`), so `dotnet test` rejects VSTest-only
  switches such as `--nologo`.

## Checklists

**Changing the template:** edit under `templates/bbpkg/`. A file every generated repository must
carry goes on `verify-release`'s required list in the same change. Then run the tests and
`verify-release`.

**Changing the conventions:** change `templates/bbpkg/scripts/repo-conventions.cs` and
`docs/repository-conventions.md` together, and copy the script over `scripts/repo-conventions.cs`.
`check --repo` then reports every family repository whose copy is behind.

**Releasing:** `verify-release` green, then tag `vYYYY.Q.MMDD` and push; the version is the tag.
After the run, `verify-release --published <version>` checks the package people will actually get.

**Avalonia and drivable-UI lessons go to XamlQuality.** `docs/avalonia-gotchas.md` and
`docs/ai-drivable-ui.md` in
[JanusMael/Bennewitz.Ninja.XamlQuality](https://github.com/JanusMael/Bennewitz.Ninja.XamlQuality)
are the one living copy of each. Send a new finding or a correction to the XamlQuality session by
message, with the versions and the measurement or source behind it, or open an issue there when no
session is running. Keep no copy here.

**Every change:** update `PROGRESS.md` in the same commit. Commits are Conventional Commits.
