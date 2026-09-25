# AGENTS.md — `tests/`

The test projects. `tests/Directory.Build.props` makes every project here an xUnit v3 test
executable that is never packed.

<!-- bbpkg: list what the tests cover beyond the pages, the endpoints and the packaging guard, as they grow -->

| Path | What it covers |
|---|---|
| `SiteStem.Tests/SiteTests.cs` | The site over HTTP, in memory: the home page through the layout, `/healthz`, `/version`, the stylesheet, the 404 page, and the error page in Production |
| `SiteStem.Tests/ComponentTests.cs` | When the site has components: one prerenders into its view, the page loads the circuit script, and the circuit hub negotiates |
| `SiteStem.Tests/PackagingTests.cs` | The site is never packed, and any library added later is declared |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| Tests run on Microsoft.Testing.Platform | `dotnet test` takes `--solution` and rejects VSTest-only switches | `global.json`, `test.runner` |
| The site is started by `WebApplicationFactory<Program>`, never on a port | The tests run the real pipeline without a process, a port or a race to start one | `SiteTests`; `public partial class Program` in `Program.cs` |
| A test that changes the host does it on its own factory, from `WithWebHostBuilder` | The class's shared factory is used by every other test in it | `SiteTests.An_exception_in_production_renders_the_error_page_without_the_exception` |

⛔ **Never weaken a test to make it pass.** When one fails, the site is wrong, not the test.
