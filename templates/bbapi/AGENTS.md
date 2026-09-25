# AGENTS.md — ApiStem

> For anyone changing this repository, human or agent. The invariants a change must not break, the
> commands, and the checklists for recurring work. Each top-level directory has an `AGENTS.md` of
> its own for what only its files show. Work state is [`PROGRESS.md`](PROGRESS.md). What every
> repository in this family carries, and how it is checked, is prescribed in
> [`docs/repository-conventions.md`](https://github.com/JanusMael/Bennewitz.Ninja.Templates/blob/main/docs/repository-conventions.md)
> in Bennewitz.Ninja.Templates.

## What this repository is

<!-- bbpkg: describe what this API is for and who calls it -->

An HTTP API on ASP.NET Core minimal APIs, compiled ahead of time to one native executable, served by
Kestrel, released as one binary per platform on GitHub Releases and built as a container image.
Generated from the `bbapi` template in Bennewitz.Ninja.Templates.

## Layout

| Directory | What it holds |
|---|---|
| `src/` | The API: `src/ApiStem`, its endpoints and its JSON contract |
| `tests/` | The API over HTTP in memory, and the packaging guard |
| `scripts/` | `repo-conventions.cs` |
| `docs/` | `releasing.md`, the release runbook |
| `.github/` | The workflows, `repository.json`, and the pointer for tools that read `.github/` |

`Dockerfile` and `.dockerignore` at the root build the container image, compiling the API natively.

## Invariants

| Invariant | Failure if broken | Guarded by |
|---|---|---|
| The API compiles to native code: `PublishAot`, minimal APIs, and every JSON type in `ApiJson` | A reflection path compiles and fails in the native binary on the first request that reaches it | the AOT analyser in every build, warnings as errors; the tests, which serialise without reflection as the native binary does; CI's `container` job, which requests the native API |
| No MVC, no Razor, no reflection-based serialisation | Each needs reflection that native AOT cannot provide | the same |
| `/healthz` answers `ok`, and `/version` names the build | A load balancer or orchestrator cannot tell the API is up; nobody can tell which build is live | `ApiTests`; CI's `container` job |
| Errors answer as problem details, never with the exception | A stack trace tells an attacker what the API runs | `ApiTests.An_exception_in_production_is_a_problem_without_the_exception` |
| The OpenAPI document describes every endpoint but `/healthz` | A client generated from it misses an endpoint, or calls the probe | `ApiTests.The_OpenAPI_document_describes_the_endpoints` |
| The container image runs as a non-root user | A compromise of the API is a compromise of the container's root | `Dockerfile`, `USER app`; CI's `container` job |
| The API is not packable, and any packable project is in `packages.push` or `packages.local` | A package nobody chose is published, permanently | `PackagingTests` |
| The release attaches binaries to a GitHub Release and pushes nothing to nuget.org | An API appears on nuget.org as a package nobody can use | `PackagingTests`; `.github/workflows/release.yml` |
| Packages resolve from nuget.org only | A second source added later silently starts supplying packages | `NuGet.config`, `packageSourceMapping` |
| Versions are pinned centrally | Two projects drift to different versions of one dependency | `Directory.Packages.props` |
| Warnings are errors | A warning ships, and here an AOT warning is a runtime failure | `Directory.Build.props`; `ci.yml` builds with `-warnaserror` |
| The version is the tag, `vYYYY.Q.MMDD` | The binary and the tag disagree | `release.yml`, step `Resolve version and tag` |
| The repository meets the family conventions | Documentation or settings go missing unnoticed | `scripts/repo-conventions.cs`, run by CI |

## Commands

```bash
dotnet build ApiStem.slnx -c Release -warnaserror
dotnet test --solution ApiStem.slnx
dotnet run --project src/ApiStem
dotnet publish src/ApiStem/ApiStem.csproj -c Release -r win-x64 -o publish/win-x64
docker build -t apistem . && docker run -p 8080:8080 apistem
dotnet run --file scripts/repo-conventions.cs -- check
```

- `dotnet run` runs under the JIT on `http://localhost:5080`. Only a publish is native: it needs the
  platform's own C++ toolchain (on Windows, the MSVC build tools, found through `vswhere.exe`) and
  cannot target another operating system. `docker build` compiles for linux-x64 anywhere Docker runs.
- The OpenAPI document is at `/openapi/v1.json`.
- Tests run on Microsoft.Testing.Platform (`global.json`), so `dotnet test` takes `--solution` and
  rejects VSTest-only switches such as `--nologo`.
- Write `-p:` rather than `/p:`: Git Bash on Windows rewrites a leading-slash argument into a path.

## Checklists

**Adding an endpoint:** map it with typed results (`Results<Ok<T>, …>`), so the OpenAPI document
names each response; add every type it reads or writes to `ApiJson` in `Json.cs`; and add a test in
`ApiTests`. A type missing from `ApiJson` builds clean; the test fails, and so would CI's `container` job.

**Adding a dependency:** check it is AOT-compatible before relying on it. A package that is not
produces AOT warnings in the build, which fail it.

**Behind a reverse proxy:** only a proxy on loopback is trusted for `X-Forwarded-*` headers. For one
elsewhere, a container network or another host, add it to `ForwardedHeadersOptions` in `Program.cs`.

**Adding a library under `src/`:** it is packable only if something outside this repository will
reference it. If so, add its id to `packages.push`, or to `packages.local` with the reason, and
bring over what `bbpkg` generates for publishing: the pack job and `assert-packages.cs`, a
`Push to NuGet.org` release step, and trusted publishing on nuget.org. `repo-conventions` then holds
it to the package properties and to trimming.

**Adding a top-level directory:** give it an `AGENTS.md` and a `CLAUDE.md` containing `@AGENTS.md`,
or exempt it in `.github/repository.json` under `undocumented`, with the reason. CI fails until
one of the two is done.

**Releasing:** `docs/releasing.md`.

**Every change:** update `PROGRESS.md` in the same commit. Commits are Conventional Commits.
