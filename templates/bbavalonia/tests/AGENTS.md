# AGENTS.md — `tests/`

The test projects. `tests/Directory.Build.props` makes every project here an xUnit v3 test
executable that is never packed.

<!-- bbpkg: list what the tests cover beyond the window, the rules and the packaging guard, as they grow -->

| Path | What it covers |
|---|---|
| `AppStem.Tests/MainWindowTests.cs` | The main window driven by `AutomationId` on the headless platform, under the real theme, and its view model without one |
| `AppStem.Tests/Headless/` | The headless app, which is the real `App`, and the guard on the one-session premise |
| `AppStem.Tests/Accessibility/` | XamlQuality's XQ1001 and XQ1002 over the app's markup |
| `AppStem.Tests/Architecture/` | AssemblyQuality's AQ1001, AQ1002 and AQ1004 over the compiled app |
| `AppStem.Tests/Packaging/` | The app is never packed, and any library added later is declared |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| Tests run on Microsoft.Testing.Platform | `dotnet test` takes `--solution` and rejects VSTest-only switches | `global.json`, `test.runner` |
| Tests in this assembly run one at a time | One headless session drives one dispatcher, which two tests must not drive at once | `[assembly: CollectionBehavior]` in `Headless/HeadlessTestApp.cs`; `HeadlessSessionTests` |
| **The body passed to `Session.Dispatch` is synchronous** | An `async` lambda becomes a task nothing awaits, and the test passes whatever it does | review |
| Headless isolation stays per test until measured otherwise | With no `[AvaloniaTestIsolation]`, every dispatch builds the app afresh, so a warm-up proves nothing. Sharing one needs its own evidence | `Headless/HeadlessTestApp.cs`, remarks |
| A rule test asserts how much it inspected, not only that it found nothing | A rule that inspected nothing finds nothing | each rule test |

⛔ **Never weaken a test to make it pass.** When `PackagingTests` or a rule test fails, the project or
the markup is wrong, not the test. The shared lessons behind these rules are XamlQuality's
`docs/avalonia-gotchas.md` and `docs/ai-drivable-ui.md`.
