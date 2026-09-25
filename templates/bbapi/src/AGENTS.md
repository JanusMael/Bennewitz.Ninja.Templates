# AGENTS.md — `src/`

The API. `src/ApiStem` is an ASP.NET Core minimal API on Kestrel, compiled ahead of time to one
native executable.

<!-- bbpkg: list the projects under src/ and what each one is, once there is more than the API -->

| Path | What it is |
|---|---|
| `ApiStem/Program.cs` | The host: the slim builder, JSON, OpenAPI, problem details, forwarded headers, `/healthz`, `/version` |
| `ApiStem/Greetings.cs` | The example resource, in the shape every endpoint takes: typed results, a route group |
| `ApiStem/Json.cs` | `ApiJson`, the source-generated JSON context holding every type the API reads or writes, and `VersionInfo` |
| `ApiStem/Properties/launchSettings.json` | Local development only: `http://localhost:5080` |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| `PublishAot` stays `true`, and nothing here needs reflection | The native binary cannot do what reflection does; a path that needs it fails at runtime, after every JIT test passed | the AOT analyser, warnings as errors; CI's `container` job |
| Every type an endpoint reads or writes is in `ApiJson` | Without it the native binary answers 500, and the build does not warn | the tests, with serialisation by reflection off; CI's `container` job |
| Endpoints return typed results | The OpenAPI document names each response only when the type says what it is | `ApiTests.The_OpenAPI_document_describes_the_endpoints` |
| `InvariantGlobalization` stays `true` | The chiseled container image has no ICU; culture data would be missing at runtime | `ApiStem.csproj` |
| Errors are problem details and never carry the exception | A stack trace tells an attacker what the API runs | `ApiTests` |
| Only loopback proxies are trusted for `X-Forwarded-*` | A header from anyone else would let a client claim any address or scheme | `Program.cs` |
| The API project stays `IsPackable` `false` | An API is run, not referenced | `PackagingTests` |
