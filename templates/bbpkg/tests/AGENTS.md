# AGENTS.md — `tests/`

The test projects. `tests/Directory.Build.props` makes every project here an xUnit v3 test
executable that is never packed.

<!-- bbpkg: list the test projects and what each one covers beyond the packaging guards -->

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| Tests run on Microsoft.Testing.Platform | `dotnet test` takes `--solution` and rejects VSTest-only switches | `global.json`, `test.runner` |
| `tests/Directory.Build.props` imports the root props explicitly | Without it, every test project silently loses the root's target framework and nullable settings | the import line itself |
| The tests under `Packaging/` stay as strong as they are | They are the only thing standing between a packaging mistake and a permanent release | `PackagingTests`, `TrimmableTests` |
| The compiled library passes AssemblyQuality's BNAQ1001, BNAQ1002, BNAQ1004 and BNAQ1005, each skipping nothing and, where it can, inspecting something; BNAQ1005: every compiled grant names an assembly the two grant files at the root allow | A consumer binds against the compiled shape, which a source analyser does not see. BNAQ1002 inspects nothing until the library references a namespace to keep inside it; then name it and assert `Inspected` | `Architecture/AssemblyQualityTests` |

⛔ **Never weaken a packaging test to make it pass.** `PackagingTests` reads the project files and
both package lists; `TrimmableTests` reads the trimmable mark off the compiled assembly. When one
fails, the package list or the project is wrong, not the test.
