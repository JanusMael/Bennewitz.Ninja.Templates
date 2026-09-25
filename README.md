# Bennewitz.Ninja.Templates

`dotnet new` templates for Bennewitz.Ninja repositories: a NuGet package published through
**Trusted Publishing (OIDC)**, with no long-lived API key anywhere, and three kinds of app. Every
repository they generate builds with warnings as errors, passes its own tests, and meets the family's
[repository conventions](https://github.com/JanusMael/Bennewitz.Ninja.Templates/blob/main/docs/repository-conventions.md) from its first commit.

```bash
dotnet new install Bennewitz.Ninja.Templates
```

| Template | Generates | Releases to |
|---|---|---|
| `bbpkg` | A class library and its tests, packed and guarded | nuget.org, through trusted publishing |
| `bbavalonia` | An Avalonia desktop app: MVVM, the Semi theme, named controls and headless UI tests, published trimmed and single-file against a warning baseline | GitHub Releases, one binary per runtime |
| `bbweb` | A Kestrel web site on MVC views, with `/healthz` and `/version`; `--blazor` adds interactive server components | GitHub Releases, single-file per runtime; a container image built in CI |
| `bbapi` | A Kestrel API on minimal APIs with OpenAPI, compiled with native AOT | GitHub Releases, native per runtime; a chiseled container image built in CI |

```bash
dotnet new bbpkg -n Widget --RepoOwner JanusMael
dotnet new bbavalonia -n Notebook --RepoOwner JanusMael
dotnet new bbweb -n Gallery --RepoOwner JanusMael --blazor
dotnet new bbapi -n Catalog --RepoOwner JanusMael
```

⚠ **`-n` takes the unprefixed stem.** `-n Widget` produces assembly `Widget` and package id
`Bennewitz.Ninja.Widget`, which is the convention across these repos. Passing the full id instead
doubles the prefix in the namespace. An app's repository is named after the app, so the app
templates default `--RepoName` to `-n`; pass it when the repository is named otherwise.

⭐ The package id is plural because a template package is a **container**: every template ships
inside this one package rather than claiming an id of its own.

## What comes with every template

| | |
|---|---|
| `ci.yml` | build, test and the conventions check on every push; the apps add a trimmed publish or a container build |
| `release.yml` | tag-triggered release; `bbpkg`'s starts with a **credential preflight** that logs in to NuGet.org and stops |
| `packages.push` / `packages.local` | what publishes, and what packs but must never reach nuget.org. Empty in an app, so a library added later is guarded from its first commit |
| `AGENTS.md`, `PROGRESS.md`, `.github/repository.json` | the family's documents and settings, checked by `scripts/repo-conventions.cs` |
| `docs/publishing.md` or `docs/releasing.md` | the runbook: for `bbpkg` the policy fields, the version rule and what to check after; for an app, the per-runtime release |

## The two things that cost a real release

Both were learned the expensive way on `Bennewitz.Ninja.XamlQuality` `2026.3.920`, and both are
encoded here rather than left to memory.

**One trusted-publishing policy, never one per package id.** NuGet.org mints one API key per token
exchange, scoped to one matching policy. Two policies matching the same repository and workflow
means one is chosen and the other's package is rejected `403` — after the first has already
published permanently. A single policy carries as many glob patterns as the pushable set needs.

**Packed is not pushable.** `dotnet pack` over a solution can produce packages that must stay
private. The push names every id explicitly and a test asserts the packed set equals
`packages.push ∪ packages.local`, so a new packable project fails the build until somebody
classifies it — loudly and early, rather than permanently and late.

## Licence

MIT. See [LICENSE](https://github.com/JanusMael/Bennewitz.Ninja.Templates/blob/main/LICENSE).
