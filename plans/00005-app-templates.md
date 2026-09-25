# 00005 — Templates for an Avalonia app, a Kestrel web site and a Kestrel API

> Status: **approved 2026-09-24**. Supersedes nothing. Extends
> [`00001`](00001-package-template.md); depends on [`00004`](00004-standard-build-properties.md).

`Bennewitz.Ninja.Templates` ships one template, `bbpkg`, for a repository that publishes NuGet
packages. Several desktop apps are being built with Avalonia, and web sites and APIs on Kestrel are
coming, and each one today starts by copying whatever the last one did. This plan adds three templates
to the same package, each extracted from software that already works rather than designed from
nothing.

## What exists to extract from

The maintainer's named examples, measured 2026-09-24 on this machine:

| Example | Kind | What it shows |
|---|---|---|
| ClaudeForge | Avalonia desktop app | `net10.0`; Avalonia with Fluent and Semi.Avalonia, Inter fonts; CommunityToolkit.Mvvm; compiled bindings by default; `PublishTrimmed` and `PublishSingleFile`, with a `TRIMMING.md`; headless tests; XamlQuality; Serilog |
| bleedink.com | Kestrel web site | `Microsoft.NET.Sdk.Web`, `net10.0`; MVC with `.cshtml` views (`AddControllersWithViews`), static assets through `MapStaticAssets`, `/healthz` and `/version` endpoints, and interactive Blazor components embedded in its views; xUnit v3 with `Microsoft.AspNetCore.Mvc.Testing`; AutoVersioning; warnings as errors; consumes `Bennewitz.Ninja.FileServer` |
| AppServices, ScopedEditors | Avalonia libraries | Headless Avalonia tests on xUnit v3 (`Avalonia.Headless`), with per-test isolation, measured and documented in each repository's tests |
| FileServer | Kestrel app and a library | A web host and a CLI; releases self-contained single-file binaries per runtime identifier to GitHub Releases, and builds a container image in CI |
| ObexNet | Console app | `PublishTrimmed` and `PublishSingleFile`, released to GitHub Releases, nothing packed |
| chisel | `dotnet tool` | `PackAsTool` with `ToolCommandName`, published to nuget.org through trusted publishing |
| dotnet-autopsy | Diagnostics toolkit | Mostly scripts and runbooks around one small executable; a reference for documentation rather than for a template |

No Kestrel API exists yet.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **Three new templates in the same package**: short names `bbavalonia`, `bbweb` and `bbapi`, in `templates/bbavalonia/`, `templates/bbweb/` and `templates/bbapi/` | `00001` named the package as a container for exactly this: a new template ships inside it rather than claiming an id. `verify-release`'s packed-content check already allows any folder with its own `.template.config/` |
| 2 | **Each carries everything `bbpkg` carries that is not about publishing packages**: the family conventions (`repository.json`, the documents, the `conventions` job), the standard properties of `00004`, central versions, `NuGet.config`, `global.json`, AutoVersioning, and the two-list packaging guard with an empty `packages.push`, so a library added later is guarded from its first commit | These are the conventions every family repository must meet, whatever it builds; a template that omitted them would generate a repository that fails `check` for reasons that are not its own |
| 3 | **Built in order of evidence: `bbavalonia` from ClaudeForge, then `bbweb` from bleedink.com and FileServer, then `bbapi` from `bbweb`** | A template extracted from a working app inherits what was learned the hard way; one designed from nothing inherits nothing. The API template has no app to extract from, so it comes last and takes its host, tests and deployment from the web template, minus the pages |
| 4 | **`bbavalonia`**: one app project and one test project; CommunityToolkit.Mvvm; the Semi.Avalonia theme, by the maintainer's choice, which matches ClaudeForge and ScopedEditors' Semi bundle; Inter fonts; compiled bindings by default; Serilog through `Bennewitz.Ninja.AppServices.Logging`; every interactive control named for automation, per XamlQuality's `docs/ai-drivable-ui.md`, with XQ1001, XQ1002 and the AssemblyQuality rules run as tests; headless UI tests on xUnit v3, set up as AppServices and ScopedEditors set theirs up | The choices ClaudeForge made and kept, except its tests: ClaudeForge's are MSTest, and the family's standard is xUnit v3, which AppServices and ScopedEditors already run headless. The drivable-UI method and the XamlQuality rules are the family's answer to "can an agent drive and verify this", and a template is where that answer reaches every new app |
| 5 | **`bbavalonia` publishes trimmed and single-file**, with the ILLink warnings held to a committed baseline, as ScopedEditors' `trimcheck/` does | ClaudeForge shows an Avalonia app trims, and ObexNet shows the single-file release; compiled bindings are what make the trim possible, and holding the warnings to a baseline is what keeps it true |
| 6 | **`bbweb`**: one site project on MVC with `.cshtml` views and static assets through `MapStaticAssets`, `/healthz` and `/version` endpoints, and one test project over `WebApplicationFactory`; not trimmed. A template parameter, `--blazor`, off by default and chosen by the maintainer, adds interactive server components embedded in the views, as bleedink.com does, with a test that renders one | What bleedink.com runs, minus its content. Server-rendered views need no JavaScript build. MVC is not trim-compatible, which `00004` decision 8 anticipated. Off by default because a render mode and a circuit are more than a site needs on its first day; a parameter because bleedink.com shows a family site does grow into them |
| 7 | **`bbapi`**: minimal APIs with the request delegate generator, OpenAPI, a health endpoint, and `PublishAot` | An API has no views, so the whole host can be trimmed and compiled ahead of time, which is where Kestrel's start-up and memory numbers come from |
| 8 | **Apps release to GitHub Releases, not nuget.org**: self-contained single-file binaries per runtime identifier, as ObexNet and FileServer's CLI release; `bbweb` and `bbapi` also build a container image in CI, pushed nowhere | An app is run, not referenced. A container registry is a deployment decision each app makes for itself |
| 9 | **`verify-release` generates, builds and tests every template**, and publishes one runtime identifier of each app | The same argument as `bbpkg`'s: a template that installs and "generates" can still be broken, and only building what it generates shows otherwise |
| 10 | **Each app template's documents cite the shared lessons**: `bbavalonia`'s `AGENTS.md` points at XamlQuality's two documents for Avalonia and drivable-UI lessons, and none of the templates keeps a copy | Decided family-wide on 2026-09-24 |

### Dismissed

- **A separate package per template.** `00001` chose a container id so that a second template would
  not need a new one.
- **Interactive Blazor components in `bbweb` always.** bleedink.com embeds them in its MVC views, but
  they bring a render mode, a circuit and a test surface that not every site needs; decision 6 makes
  them a parameter instead.
- **Razor Pages for `bbweb`.** No working family site uses them; bleedink.com uses MVC views.
- **Writing `bbapi` before a real API exists.** Considered and kept, but last: its host, tests and
  deployment come from `bbweb`, which has real apps behind it.

## Scope

**In:** the three templates, their tests in `verify-release`, `docs/repository-conventions.md` and
the package README describing them, and a release that ships them.

**Out:** moving an existing app onto a template (each app's own work); a container registry or any
deployment; installers and store packaging (ClaudeForge's winget work stays its own); Blazor
WebAssembly; a
template for a console app or a `dotnet tool`, which ObexNet and chisel would be the sources for,
and which is a later plan if wanted.

## Steps

| # | Step | Verified by |
|---|---|---|
| 1 | Wait for `00004`'s script, which knows app and tool roles | `00004` step 5 done: the check passes in this repository |
| 2 | Extract `bbavalonia` from ClaudeForge: the app shell, the MVVM wiring, logging, one window with named controls, the headless test, the XamlQuality and AssemblyQuality tests, the trimmed single-file publish and its warning baseline | A generated repository builds, its tests pass, a trimmed single-file publish for `win-x64` runs and exits cleanly, and `check --offline` reports only the markers and the empty description |
| 3 | Extract `bbweb` from bleedink.com and FileServer: the host, one controller and view, static assets, `/healthz` and `/version`, the `WebApplicationFactory` test, the container build, the per-runtime release, and the `--blazor` parameter with one interactive component and its test | Both variants, generated with and without `--blazor`, build and pass their tests (the view renders, `/healthz` returns 200, and with `--blazor` the component renders); the container image builds in CI |
| 4 | Build `bbapi` from `bbweb`: minimal APIs, OpenAPI, AOT | A generated repository builds, its tests pass, an AOT publish for `linux-x64` runs and answers health |
| 5 | `verify-release` covers all four templates, and `bbweb` in both variants; the packed-content check sees four template folders | `verify-release` fails when any one generated repository, either `bbweb` variant included, fails to build |
| 6 | Document the templates in the package README and `docs/repository-conventions.md`, and release | Verified from nuget.org: `dotnet new list` shows all four after installing the published version, and `verify-release --published` passes |
