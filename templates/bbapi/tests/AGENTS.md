# AGENTS.md — `tests/`

The test projects. `tests/Directory.Build.props` makes every project here an xUnit v3 test
executable that is never packed.

<!-- bbpkg: list what the tests cover beyond the endpoints, the contract and the packaging guard, as they grow -->

| Path | What it covers |
|---|---|
| `ApiStem.Tests/ApiTests.cs` | The API over HTTP, in memory: `/healthz`, `/version`, the greeting and its validation problem, the OpenAPI document, the 404 problem, and an exception in Production |
| `ApiStem.Tests/PackagingTests.cs` | The API is never packed, and any library added later is declared |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| Tests run on Microsoft.Testing.Platform | `dotnet test` takes `--solution` and rejects VSTest-only switches | `global.json`, `test.runner` |
| The API is started by `WebApplicationFactory<Program>`, never on a port | The tests run the real pipeline without a process, a port or a race to start one | `ApiTests`; `public partial class Program` in `Program.cs` |
| A test that changes the host does it on its own factory, from `WithWebHostBuilder` | The class's shared factory is used by every other test in it | `ApiTests.An_exception_in_production_is_a_problem_without_the_exception` |

⚠ **The tests run under the JIT, not the native binary.** Their project turns JSON serialisation by
reflection off, as the native binary has it, so a type missing from `ApiJson` fails a test rather
than passing here and failing in production. Keep it off. What else only native AOT breaks, the
analyser in the build and CI's `container` job catch, the latter by requesting the native API.

⛔ **Never weaken a test to make it pass.** When one fails, the API is wrong, not the test.
