# AGENTS.md — `src/`

The app. `src/CliStem` is a console app, published trimmed as one self-contained executable.

<!-- bbpkg: list the projects under src/ and what each one is, once there is more than the app -->

| Path | What it is |
|---|---|
| `CliStem/Program.cs` | The entry point: hands the command line, the work and the usage to `AppMain.RunConsoleAsync` |
| `CliStem/LineCount.cs` | The example command, in the shape every command takes: usage errors thrown, results on stdout, the token honoured |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| `Main` only calls `AppMain.RunConsoleAsync` | The exit codes, the fatal report, Ctrl+C and `--version` are the package's, so a fix there reaches this app with an update. A hand-written `try`/`catch` around it would answer first and differ | `CommandLineTests` |
| A bad command line throws `UsageException` | It is what exits 2 with the usage on stderr; anything else exits 1 as a crash | `CommandLineTests` |
| Nothing writes to stdout but results | A script reading the output takes every line as data | `CommandLineTests` |
| Every wait honours the `CancellationToken`, and a read from standard input waits off the calling thread | `Console.In`'s reads block the caller, so a token checked after one is never reached while it waits | `CommandLineTests.Ctrl_C_cancels_the_work_and_exits_130`, on Linux |
| `PublishTrimmed` stays `true`, with ILLink's warnings as errors, and nothing here needs reflection | The trimmer removes what reflection would reach, and the binary fails on that path at runtime | the trim analyser, warnings as errors; CI's `publish` job |
| `InvariantGlobalization` stays `true` | A self-contained Linux binary would otherwise need libicu on every machine it runs on | `CliStem.csproj` |
| The app project stays `IsPackable` `false` | An app is run, not referenced | `PackagingTests` |
