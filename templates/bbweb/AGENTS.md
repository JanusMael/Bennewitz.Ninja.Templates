# AGENTS.md — SiteStem

> For anyone changing this repository, human or agent. The invariants a change must not break, the
> commands, and the checklists for recurring work. Each top-level directory has an `AGENTS.md` of
> its own for what only its files show. Work state is [`PROGRESS.md`](PROGRESS.md). What every
> repository in this family carries, and how it is checked, is prescribed in
> [`docs/repository-conventions.md`](https://github.com/JanusMael/Bennewitz.Ninja.Templates/blob/main/docs/repository-conventions.md)
> in Bennewitz.Ninja.Templates.

## What this repository is

<!-- bbpkg: describe what this site is for and who visits it -->

A web site on ASP.NET Core, served by Kestrel from MVC views, released as one self-contained binary
per platform on GitHub Releases and built as a container image. Generated from the `bbweb` template
in Bennewitz.Ninja.Templates.

## Layout

| Directory | What it holds |
|---|---|
| `src/` | The site: `src/SiteStem`, its controllers, views and static assets |
| `tests/` | The site over HTTP in memory, and the packaging guard |
| `scripts/` | `repo-conventions.cs` |
| `docs/` | `releasing.md`, the release runbook |
| `.github/` | The workflows, `repository.json`, and the pointer for tools that read `.github/` |

`Dockerfile` and `.dockerignore` at the root build the container image.

## Invariants

| Invariant | Failure if broken | Guarded by |
|---|---|---|
| `/healthz` answers `ok`, and `/version` names the build | A load balancer or orchestrator cannot tell the site is up; nobody can tell which build is live | `SiteTests`; CI's `container` job |
| An error page never shows the exception | A stack trace tells an attacker what the site runs | `SiteTests.An_exception_in_production_renders_the_error_page_without_the_exception` |
| The site is not trimmed | MVC and Razor create what they render by reflection, which no trim analysis sees, and a trimmed site fails at runtime | `SiteStem.csproj`, `PublishTrimmed` |
| The container image runs as a non-root user | A compromise of the site is a compromise of the container's root | `Dockerfile`, `USER app`; CI's `container` job |
| The site is not packable, and any packable project is in `packages.push` or `packages.local` | A package nobody chose is published, permanently | `PackagingTests` |
| The release attaches binaries to a GitHub Release and pushes nothing to nuget.org | A site appears on nuget.org as a package nobody can use | `PackagingTests`; `.github/workflows/release.yml` |
| Packages resolve from nuget.org only | A second source added later silently starts supplying packages | `NuGet.config`, `packageSourceMapping` |
| Versions are pinned centrally | Two projects drift to different versions of one dependency | `Directory.Packages.props` |
| Warnings are errors | A warning ships | `Directory.Build.props`; `ci.yml` builds with `-warnaserror` |
| The version is the tag, `vYYYY.Q.MMDD` | The binary and the tag disagree | `release.yml`, step `Resolve version and tag` |
| The repository meets the family conventions | Documentation or settings go missing unnoticed | `scripts/repo-conventions.cs`, run by CI |

## Commands

```bash
dotnet build SiteStem.slnx -c Release -warnaserror
dotnet test --solution SiteStem.slnx
dotnet run --project src/SiteStem
docker build -t sitestem . && docker run -p 8080:8080 sitestem
dotnet run --file scripts/repo-conventions.cs -- check
```

- `dotnet run` serves on `http://localhost:5080` (`Properties/launchSettings.json`). A published
  binary listens where `ASPNETCORE_URLS` says, `http://localhost:5000` by default; the container on
  port 8080.
- Tests run on Microsoft.Testing.Platform (`global.json`), so `dotnet test` takes `--solution` and
  rejects VSTest-only switches such as `--nologo`.
- Write `-p:` rather than `/p:`: Git Bash on Windows rewrites a leading-slash argument into a path.

## Checklists

**Adding a page:** a controller action and its view under `Views/`, and a test in `SiteTests` that
requests it and checks what it renders.

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
