# AGENTS.md — `tests/`

The test projects. `tests/Directory.Build.props` makes every project here an xUnit v3 test
executable that is never packed.

<!-- bbpkg: list the test projects and what each one covers beyond the packaging guards -->

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| Tests run on Microsoft.Testing.Platform | `dotnet test` takes `--solution` and rejects VSTest-only switches | `global.json`, `test.runner` |
| `tests/Directory.Build.props` imports the root props explicitly | Without it, every test project silently loses the root's target framework and nullable settings | the import line itself |
<!--#if (Analyzer) -->
| The tests under `Packaging/` stay as strong as they are | They are the only thing standing between a packaging mistake and a permanent release | `PackagingTests`, `AnalyzerShapeTests` |
| Every rule is proved three ways: it fires on a violation, stays silent on clean code, and stays silent out of scope | A rule whose gate is wrong reports nothing, so it passes every test that expects silence; only the firing test fails it | `Rules/`, one test class per rule, through `RuleTest<TAnalyzer>` |
| The README's rules table is generated from the descriptors, and every rule has a `### <id>` section | Each rule's help link is its section; a section that does not exist is a 404 on the rule a consumer was just shown. `RULES_UPDATE_DOCS=1` regenerates the table | `RulesCatalogTests` |
| The rules run on the Roslyn pin, through `Microsoft.CodeAnalysis.CSharp.Workspaces` | The testing package floors Workspaces at 1.0.1; the pin is what makes the tests run on the oldest compiler the package claims | `PkgStem.Tests.csproj`, comment |
<!--#else -->
| The tests under `Packaging/` stay as strong as they are | They are the only thing standing between a packaging mistake and a permanent release | `PackagingTests`, `TrimmableTests` |
<!--#endif -->
| The compiled library passes AssemblyQuality's BNAQ1001, BNAQ1002, BNAQ1004 and BNAQ1005, each skipping nothing and, where it can, inspecting something; BNAQ1005: every compiled grant names an assembly the two grant files at the root allow | A consumer binds against the compiled shape, which a source analyser does not see. BNAQ1002 inspects nothing until the library references a namespace to keep inside it; then name it and assert `Inspected` | `Architecture/AssemblyQualityTests` |

<!--#if (Analyzer) -->
⛔ **Never weaken a packaging test to make it pass.** `PackagingTests` reads the project files and
both package lists; `AnalyzerShapeTests` reads the analyzer's shape off the compiled assembly and
its project file. When one fails, the package list or the project is wrong, not the test.
<!--#else -->
⛔ **Never weaken a packaging test to make it pass.** `PackagingTests` reads the project files and
both package lists; `TrimmableTests` reads the trimmable mark off the compiled assembly. When one
fails, the package list or the project is wrong, not the test.
<!--#endif -->
