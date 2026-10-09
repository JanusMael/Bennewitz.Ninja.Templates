# AGENTS.md — CliStem

> For anyone changing this repository, human or agent. The invariants a change must not break, the
> commands, and the checklists for recurring work. Each top-level directory has an `AGENTS.md` of
> its own for what only its files show. Work state is [`PROGRESS.md`](PROGRESS.md). What every
> repository in this family carries, and how it is checked, is prescribed in
> [`docs/repository-conventions.md`](https://github.com/JanusMael/Bennewitz.Ninja.Templates/blob/main/docs/repository-conventions.md)
> in Bennewitz.Ninja.Templates.

## What this repository is

<!-- bbpkg: describe what this tool is for and who runs it -->

A console app, published trimmed as one self-contained executable per platform and released on
GitHub Releases. Its entry point is `AppMain` from Bennewitz.Ninja.AppServices.EntryPoint, which
every family app shares. Generated from the `bbconsole` template in Bennewitz.Ninja.Templates.

## Layout

| Directory | What it holds |
|---|---|
| `src/` | The app: `src/CliStem`, its entry point and its commands |
| `tests/` | The app run as a child process, and the packaging guard |
| `scripts/` | `repo-conventions.cs` |
| `docs/` | `releasing.md`, the release runbook |
| `.github/` | The workflows, `repository.json`, and the pointer for tools that read `.github/` |

## Invariants

| Invariant | Failure if broken | Guarded by |
|---|---|---|
| Exit codes: 0 success, 1 an unhandled failure, 2 a usage error, 130 Ctrl+C | A calling script cannot tell a crash from a bad invocation, or either from success | `AppMain`; `CommandLineTests`; CI's `publish` job |
| Results go to stdout; diagnostics, the usage and the fatal report to stderr | A script piping the output gets an error message as data | `CommandLineTests` |
| `--version` prints the release version, `PublicVersion`, never `AssemblyInformationalVersion` | AutoVersioning sets the latter to "Built with ♥", so nobody can tell which build runs | `AppMain`; `CommandLineTests`; CI's `publish` job |
| Work that waits honours the `CancellationToken` it is handed | Ctrl+C sets the exit code but the app goes on waiting | `CommandLineTests.Ctrl_C_cancels_the_work_and_exits_130`, on Linux |
| The release is trimmed, and ILLink's warnings are errors | A trimmed binary fails at runtime on a path the trimmer removed | `CliStem.csproj`; CI's `publish` job |
| The app is not packable, and any packable project is in `packages.push` or `packages.local` | A package nobody chose is published, permanently | `PackagingTests` |
| The release attaches binaries to a GitHub Release and pushes nothing to nuget.org | An app appears on nuget.org as a package nobody can use | `PackagingTests`; `.github/workflows/release.yml` |
| Packages resolve from nuget.org only | A second source added later silently starts supplying packages | `NuGet.config`, `packageSourceMapping` |
| Versions are pinned centrally | Two projects drift to different versions of one dependency | `Directory.Packages.props` |
| Warnings are errors | A warning ships, and here a trim warning is a runtime failure | `Directory.Build.props`; `ci.yml` builds with `-warnaserror` |
| The version is the tag, `vYYYY.Q.MMDD` | The binary and the tag disagree | `release.yml`, step `Resolve version and tag` |
| The repository meets the family conventions | Documentation or settings go missing unnoticed | `scripts/repo-conventions.cs`, run by CI |

## Commands

```bash
dotnet build CliStem.slnx -c Release -warnaserror
dotnet test --solution CliStem.slnx
dotnet run --project src/CliStem -- notes.txt
dotnet publish src/CliStem/CliStem.csproj -c Release -r win-x64 --self-contained -o publish/win-x64
dotnet run --file scripts/repo-conventions.cs -- check
dotnet run --file scripts/repo-conventions.cs -- grants
```

- A publish is trimmed and single-file, from any operating system to any runtime identifier.
- Tests run on Microsoft.Testing.Platform (`global.json`), so `dotnet test` takes `--solution` and
  rejects VSTest-only switches such as `--nologo`.
- Write `-p:` rather than `/p:`: Git Bash on Windows rewrites a leading-slash argument into a path.

## Checklists

**Adding a command or an option:** parse it in the work `Program.cs` hands to `AppMain`; throw
`UsageException` for an unknown option or a missing argument, and add it to the usage text; write
results to stdout; pass the `CancellationToken` to everything that waits; and add a test in
`CommandLineTests`. Which parser to use, if any, is this app's choice: it must still leave the exit
codes and the streams as they are.

**Adding a dependency:** check it is trim-compatible before relying on it. A package that is not
produces ILLink warnings in CI's `publish` job, which fail it.

**Adding a library under `src/`:** it is packable only if something outside this repository will
reference it. If so, add its id to `packages.push`, or to `packages.local` with the reason, and
bring over what `bbpkg` generates for publishing: the pack job and `assert-packages.cs`, a
`Push to NuGet.org` release step, and trusted publishing on nuget.org. `repo-conventions` then holds
it to the package properties and to trimming.

**Adding, removing or renaming a project:** run `repo-conventions grants`, which rewrites
`AssemblyInfo.InternalsVisibleTo.cs`: every project grants its internals to every assembly this
repository builds, so `internal` means solution-internal and what no other assembly may reach is
`private`. Never declare a grant in a project; grants to another repository go in
`AssemblyInfo.InternalsVisibleTo.External.cs`, by assembly name. A project that must stay out sets
`<SolutionFriendGrants>false</SolutionFriendGrants>`. CI fails on a stale list or a stray grant.

**Adding a top-level directory:** give it an `AGENTS.md` and a `CLAUDE.md` containing `@AGENTS.md`,
or exempt it in `.github/repository.json` under `undocumented`, with the reason. CI fails until
one of the two is done.

**Releasing:** `docs/releasing.md`.

**Every change:** update `PROGRESS.md` in the same commit. Commits are Conventional Commits.
