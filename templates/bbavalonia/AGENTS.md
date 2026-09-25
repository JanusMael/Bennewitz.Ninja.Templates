# AGENTS.md — AppStem

> For anyone changing this repository, human or agent. The invariants a change must not break, the
> commands, and the checklists for recurring work. Each top-level directory has an `AGENTS.md` of
> its own for what only its files show. Work state is [`PROGRESS.md`](PROGRESS.md). What every
> repository in this family carries, and how it is checked, is prescribed in
> [`docs/repository-conventions.md`](https://github.com/JanusMael/Bennewitz.Ninja.Templates/blob/main/docs/repository-conventions.md)
> in Bennewitz.Ninja.Templates.

## What this repository is

<!-- bbpkg: describe what this app is for and who uses it -->

A desktop app on Avalonia, released as one self-contained, trimmed, single-file binary per platform
on GitHub Releases. Generated from the `bbavalonia` template in Bennewitz.Ninja.Templates.

## Layout

| Directory | What it holds |
|---|---|
| `src/` | The app: `src/AppStem`, its views, view models and the trim-warning baseline |
| `tests/` | Headless UI tests, the XamlQuality and AssemblyQuality rules, and the packaging guard |
| `scripts/` | File-based apps: `check-trim-warnings.cs` and `repo-conventions.cs` |
| `docs/` | `releasing.md`, the release runbook |
| `.github/` | The workflows, `repository.json`, and the pointer for tools that read `.github/` |

## Invariants

| Invariant | Failure if broken | Guarded by |
|---|---|---|
| Bindings are compiled: `AvaloniaUseCompiledBindingsByDefault`, and `x:DataType` on every view | A reflection binding works in a debug run and does nothing in the trimmed release, silently | `AppStem.csproj`; the trim analyser in every build |
| A trimmed publish's ILLink warnings equal `src/AppStem/trim-warnings.txt` | A new trim hazard ships, or ILLink stops analysing Avalonia and nobody notices | CI's `trim` job; the release, for every platform |
| Every interactive control has an `AutomationId` and an automation `Name` | An agent, a test or a screen reader cannot find or announce it | `AutomationNameTests` (BNXQ1001, BNXQ1002) |
| The app is not packable, and any packable project is in `packages.push` or `packages.local` | A package nobody chose is published, permanently | `PackagingTests` |
| The release attaches binaries to a GitHub Release and pushes nothing to nuget.org | An app appears on nuget.org as a package nobody can use | `PackagingTests`; `.github/workflows/release.yml` |
| Packages resolve from nuget.org only | A second source added later silently starts supplying packages | `NuGet.config`, `packageSourceMapping` |
| Versions are pinned centrally, every Avalonia package at one version | A mismatch is a restore error, or worse, a runtime one | `Directory.Packages.props` |
| Warnings are errors | A warning ships | `Directory.Build.props`; `ci.yml` builds with `-warnaserror` |
| The version is the tag, `vYYYY.Q.MMDD` | The binary and the tag disagree | `release.yml`, step `Resolve version and tag` |
| The repository meets the family conventions | Documentation or settings go missing unnoticed | `scripts/repo-conventions.cs`, run by CI |

## Commands

```bash
dotnet build AppStem.slnx -c Release -warnaserror
dotnet test --solution AppStem.slnx
dotnet run --project src/AppStem
dotnet publish src/AppStem/AppStem.csproj -c Release -r win-x64 --self-contained -o publish/win-x64
dotnet run --file scripts/repo-conventions.cs -- check
```

- A published binary run with `--smoke` opens the main window and exits 0: the quickest proof that
  a trimmed build still starts, loads its theme and binds.
- Tests run on Microsoft.Testing.Platform (`global.json`), so `dotnet test` takes `--solution` and
  rejects VSTest-only switches such as `--nologo`.
- Write `-p:` rather than `/p:`: Git Bash on Windows rewrites a leading-slash argument into a path.

## Checklists

**Adding a view:** give it `x:DataType`, an `AutomationId` and a `Name` on every interactive control,
and a headless test that drives it by `AutomationId`. Raise the `Inspected` floor in
`AutomationNameTests` to the new count.

**Adding a dependency:** pin it in `Directory.Packages.props`, then run CI's `trim` job, or the
publish it runs. A new ILLink warning fails it; either fix the cause or add the line to
`src/AppStem/trim-warnings.txt` with the reason, in the same commit.

**Adding a library under `src/`:** it is packable only if something outside this repository will
reference it. If so, add its id to `packages.push`, or to `packages.local` with the reason, and
bring over what `bbpkg` generates for publishing: the pack job and `assert-packages.cs`, a
`Push to NuGet.org` release step, and trusted publishing on nuget.org. `repo-conventions` then holds
it to the package properties and to trimming.

**Adding a top-level directory:** give it an `AGENTS.md` and a `CLAUDE.md` containing `@AGENTS.md`,
or exempt it in `.github/repository.json` under `undocumented`, with the reason. CI fails until
one of the two is done.

**Releasing:** `docs/releasing.md`.

**Avalonia and drivable-UI lessons go to XamlQuality.** `docs/avalonia-gotchas.md` and
`docs/ai-drivable-ui.md` in
[JanusMael/Bennewitz.Ninja.XamlQuality](https://github.com/JanusMael/Bennewitz.Ninja.XamlQuality)
are the one living copy of each. Read them before changing a view, a binding or a test that drives
the UI. Send a new finding or a correction to the XamlQuality session by message, with the versions
and the measurement or source behind it, or open an issue there when no session is running. Keep no
copy here.

**Every change:** update `PROGRESS.md` in the same commit. Commits are Conventional Commits.
