# AGENTS.md — `tests/`

The test projects. `tests/Directory.Build.props` makes every project here an xUnit v3 test
executable that is never packed.

<!-- bbpkg: list what the tests cover beyond the window, the rules and the packaging guard, as they grow -->

| Path | What it covers |
|---|---|
| `AppStem.Headless.Tests/MainWindowTests.cs` | The main window driven by `AutomationId` on the headless platform, under the real theme |
| `AppStem.Headless.Tests/Session/` | The headless app, which is the real `App`; the fixture that gives the session's thread the UI dispatcher before any test runs; the guards on that and on the one-session premise |
| `AppStem.Tests/MainWindowViewModelTests.cs` | The main window's view model, with no headless session |
| `AppStem.Tests/Accessibility/` | XamlQuality's BNXQ1001, BNXQ1002, BNXQ1007 and BNXQ1011 over the app's markup |
| `AppStem.Tests/Architecture/` | AssemblyQuality's BNAQ1001, BNAQ1002 (over Serilog) and BNAQ1004 over the compiled app, each asserting it skipped nothing, and BNAQ1005: every compiled grant names an assembly the two grant files at the root allow; and only `AppStem.Headless.Tests` references `Avalonia.Headless` |
| `AppStem.Tests/Packaging/` | The app is never packed, and any library added later is declared |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| Tests run on Microsoft.Testing.Platform | `dotnet test` takes `--solution` and rejects VSTest-only switches | `global.json`, `test.runner` |
| **A test that dispatches on the headless session goes in `AppStem.Headless.Tests`; one that does not, does not** | That assembly shares one `Application` for the whole process. A test that expects no app, `Application.Current` null, breaks beside it | `AppStem.Tests` has no `Avalonia.Headless`, so a headless test there does not compile; `TestProjectTests` fails any project but the headless one that references it |
| **The headless assembly is isolated per assembly, and its fixture claims the UI dispatcher first** | Per test, every dispatch clears `Dispatcher.UIThread`, and a background thread that reads it during the rebuild takes it: 1,815 of 2,000 dispatches failed, measured in ClaudeForge. Per assembly without the fixture, a read before the first dispatch takes it for good | `HeadlessSessionOwnership`, which fails every headless test otherwise; `HeadlessSessionOwnershipTests` |
| Headless tests run one at a time | One session drives one dispatcher and one shared `Application`, which two tests must not drive at once | `[assembly: Parallelization]` in `Session/HeadlessTestApp.cs`; `HeadlessSessionTests` |
| **The body passed to `Session.Dispatch` is synchronous** | An `async` lambda becomes a task nothing awaits, and the test passes whatever it does | review |
| A headless test restores what it changes, in a `finally`: windows it opens, theme, resources | Avalonia disposes nothing between tests, so the next test inherits it | review |
| A rule test asserts how much it inspected, not only that it found nothing | A rule that inspected nothing finds nothing | each rule test |

⛔ **Never weaken a test to make it pass.** When `PackagingTests` or a rule test fails, the project or
the markup is wrong, not the test. The shared lessons behind these rules are XamlQuality's
`docs/avalonia-gotchas.md` and `docs/ai-drivable-ui.md`.
