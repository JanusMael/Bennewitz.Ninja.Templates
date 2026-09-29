# 00007 — A console template, and one entry point for every app

> Status: **approved 2026-09-29**. Supersedes nothing. Extends
> [`00005`](00005-app-templates.md), whose scope left a console template to "a later plan if wanted".

The package ships four templates. A console app, the most common entry point of all, has none, and
each one starts by copying the last. The entry points the templates do ship disagree: `bbavalonia`
returns an exit code, logs a fatal error and flushes its log; `bbweb` and `bbapi` do none of it. This
plan adds `bbconsole` and gives every app template the same entry-point behaviour, with that
behaviour in a package so that a fix reaches every app, not only the next one generated.

## What exists to extract from

| Source | What it shows |
|---|---|
| ObexNet (`2ad50ac`, 2026-09-07) | A console app released trimmed and single-file per runtime to GitHub Releases, nothing packed (`ObexNet.csproj`, `release.yml`). Its entry point is the pattern to avoid: `void Main` (`Program.cs:9`), so a fatal error exits 0; an unknown flag is printed to stdout and exits 0 (line 40); a JSON error line is built from an unescaped exception message (line 77) |
| `bbavalonia`'s `Program.cs` | Half of it: `int Main`, `Log.Fatal` and exit code 1 on failure, `Log.CloseAndFlush` in `finally`, and an `AppDomain.UnhandledException` hook that shows a native dialog. It has no `TaskScheduler.UnobservedTaskException` hook, and it is a `WinExe` that logs to a rolling file and Trace, with no stderr |
| `bbweb`, `bbapi` | Log through Microsoft.Extensions.Logging, no Serilog. Top-level `app.Run()`, never disposed. Their tests run the real entry point through `WebApplicationFactory<Program>` |
| AppServices | The family's app-support packages. `AppServices.Logging` is the one project allowed Serilog; `IsAotCompatible` is deliberately not set (`src/Directory.Build.props`), since nothing has measured it. Nothing yet wraps an entry point |

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **Every app's entry point carries the same four behaviours** (the maintainer's choice, 2026-09-28): (a) `int Main`, and any unhandled exception is reported as fatal and returns 1, with the app's log flushed on every exit; (b) `AppDomain.UnhandledException` and `TaskScheduler.UnobservedTaskException` are reported before anything else runs; (c) diagnostics and the fatal report go to stderr, results to stdout; (d) Ctrl+C cancels a `CancellationToken` handed to the work and exits 130, and `--version` prints the release version | ObexNet gets each of these wrong, and each way misleads a script calling the app. `bbavalonia` has (a) but neither (b)'s second hook nor (c). The version is `[AssemblyMetadata("PublicVersion")]`, never `AssemblyInformationalVersion`, which reads "Built with ♥" (`00005`'s drift) |
| 2 | **The behaviour lives in AppServices, as a small entry-point helper** (the maintainer's choice, 2026-09-28); each template calls it in two or three lines | A template's text is copied once and never updated, so a fix to a hand-written wrapper would reach no app already generated. A package bump reaches every one |
| 3 | **What the helper is called, where it lives in AppServices and its API are AppServices' to design**, in its own plan. What it must satisfy is this plan's, and step 1 sends it: decisions 1, 4 and 5, and these constraints: **the fatal report is written to stderr directly**, needing no logger, so it survives a failure before any logging is configured; the helper **depends on nothing beyond the BCL**, and the app's own logger is flushed through a callback the app passes, so no logging framework is imposed; it is **`IsAotCompatible` and trim-safe**, measured, because `bbapi` publishes with native AOT and warnings as errors; it **re-throws `HostAbortedException`**, which `WebApplicationFactory` uses to stop the entry point it runs, **never calls `Environment.Exit`**, and **can run many times in one process**, as a test host runs it | Without these, the helper would pull a logging framework into a native AOT build, log every web test's normal shutdown as fatal, or break the test host |
| 4 | **Each app kind keeps what its host already does.** A web host (`bbweb`, `bbapi`) turns SIGTERM and Ctrl+C into a graceful shutdown itself, so it takes (a)–(c) and `--version`, not the console's Ctrl+C handling; `--version` is answered before the host is built, since everything else on a web app's command line is configuration (`--urls`, `--environment`, `--Key value`). The web templates set their console logger's `LogToStandardErrorThreshold` so diagnostics reach stderr, and dispose the app on exit so the log is flushed. `bbavalonia`, a `WinExe` with no console, keeps its native fatal-error dialog and its rolling-file log for (c), and its window's close is its cancellation | Doubling a host's own shutdown handling would race it. A GUI app has no stderr a user sees |
| 5 | **Exit codes** (the maintainer's choice, 2026-09-29): 0 success, 1 unhandled failure, 2 a usage error (an unknown option, a missing argument, with the usage on stderr), 130 cancelled by Ctrl+C. Code 2 applies where the app owns its command line, which is `bbconsole`; a web app's arguments are configuration, so it never rejects one (the maintainer's scoping, 2026-09-29) | The conventional meanings, so a calling script can tell a bad invocation from a crash. ObexNet exits 0 on both |
| 6 | **`bbconsole`** is a repository template like the other four: one console project and one test project, the family conventions, central versions, the two-list packaging guard with an empty `packages.push`, `--RepoName` as the app templates have it | `00005` decision 2's argument holds for a console app too |
| 7 | **`bbconsole` publishes trimmed and single-file per runtime to GitHub Releases** (the maintainer's choice, 2026-09-29), as ObexNet does, with trim warnings as errors and no baseline. `verify-release` gains a publish kind for it, since its `Trimmed` kind compares against `bbavalonia`'s baseline | A console app with no UI framework has no framework warnings to hold to a baseline, unlike `bbavalonia`; a zero-warning gate is simpler and stricter |
| 8 | **The usage text, option parsing and the work itself are the generated app's.** The template ships one example command and its test, not an argument-parsing framework | Which parser, if any, is each app's choice; the entry-point contract of decision 1 does not depend on it |
| 9 | **`bbconsole`'s tests run the built executable as a child process**: exit code 0 on success, 1 on a failure the example command raises when given a test-only argument, with the message on stderr, 2 on an unknown option, and `--version` printing the release version. Exit code 130 is tested by sending SIGINT on Linux and macOS in CI; on Windows it is not verified, since a console Ctrl+C cannot be sent to a child process reliably | These are the contract, and an exit code or a stream can only be seen from outside the process |

### Dismissed

- **The wrapper as template text only.** Fixes would never reach an app already generated
  (decision 2).
- **Template text now, the helper later.** One more rewrite of every template for a few days' head
  start.
- **Serilog in the helper.** It would reach `bbapi`'s native AOT build beside Microsoft.Extensions.Logging,
  and AppServices keeps Serilog to `AppServices.Logging` alone.
- **Native AOT for `bbconsole`, as `bbapi` has.** It needs the platform's C toolchain on every
  machine that builds a release, and a console tool's dependencies are less often AOT-safe than a
  minimal API's. An app that wants it opts in.
- **A `dotnet tool` template, from chisel.** A tool is packed and pushed to nuget.org, a different
  release from an app's; a later plan if wanted.
- **ObexNet's entry point as the model.** It has each of the flaws decision 1 fixes.

## Scope

**In:** the requirement AppServices' helper is built against (decisions 1, 3, 4, 5); `bbconsole`;
every app template's entry point calling the helper; `verify-release` generating, building, testing
and publishing `bbconsole`, and running it; the package README and `docs/repository-conventions.md`
naming the fifth template.

**Out:** the helper's design and release, which are AppServices'; a `dotnet tool` template; moving
any existing app onto the helper, which is each app's own change.

## Steps

| # | Step | Verified by |
|---|---|---|
| 1 | Send AppServices the requirement of decision 3, and wait for its helper to ship. Everything after waits for it | AppServices' release, verified from nuget.org by its session, including its own measurement that the helper publishes with native AOT with no warnings |
| 2 | In one change, as `verify-release` requires of a new template: `bbconsole` (the project, its tests of decision 9, the trimmed single-file release per runtime, CI, the family documents, calling the helper), its case in `verify-release` with the new publish kind, and its files on the required list | A generated repository builds with warnings as errors and its tests pass. `verify-release` passes, publishing `bbconsole` for the machine's runtime and running it with `--version` and with an unknown option; with `--version` changed to print the informational version, it fails |
| 3 | `bbweb`, `bbapi` and `bbavalonia` call the helper, each as decision 4 says | Their existing tests still pass, the web ones through `WebApplicationFactory`, which proves `HostAbortedException` is not reported as fatal. In `verify-release`, each published app is also run once with its `Program` body replaced by one that throws on start-up, a replacement `verify-release` writes into its scratch copy and never ships: it must exit 1 with the fatal report on stderr (the log file, for `bbavalonia`) |
| 4 | The package README, the package description and `docs/repository-conventions.md` name `bbconsole`, then release | Verified from nuget.org: `dotnet new list` shows five templates, and `verify-release --published` passes |
