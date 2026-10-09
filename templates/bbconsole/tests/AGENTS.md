# AGENTS.md — `tests/`

The test projects. `tests/Directory.Build.props` makes every project here an xUnit v3 test
executable that is never packed.

<!-- bbpkg: list what the tests cover beyond the command line and the packaging guard, as they grow -->

| Path | What it covers |
|---|---|
| `CliStem.Tests/CommandLineTests.cs` | The built executable, run as a child process: a count on stdout and exit 0, `--version`, an unknown option and no file exiting 2 with the usage, a missing file exiting 1 with the report, and Ctrl+C exiting 130 |
| `CliStem.Tests/PackagingTests.cs` | The app is never packed, and any library added later is declared |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| Tests run on Microsoft.Testing.Platform | `dotnet test` takes `--solution` and rejects VSTest-only switches | `global.json`, `test.runner` |
| The command line is tested by running the executable, never by calling `Main` | An exit code and the two streams can only be seen from outside the process, and `AppMain` hooks process-wide events | `CommandLineTests` |
| Every test that starts the app has a timeout | An app that ignores cancellation would hang the run instead of failing it | `CommandLineTests`, `Timeout` |

⚠ **Ctrl+C is verified on Linux and macOS only.** The test sends SIGINT; on Windows a console Ctrl+C
cannot be sent to a child process reliably, so the test is skipped there. CI runs it on Linux.

⛔ **Never weaken a test to make it pass.** When one fails, the app is wrong, not the test.
