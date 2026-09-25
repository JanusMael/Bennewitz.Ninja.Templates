# 00004 — Every family project builds with the standard properties, and trimming follows

> Status: **approved 2026-09-24**. Supersedes nothing. Extends
> [`00003`](00003-repository-conventions.md).

`00003` made the family's documentation and GitHub settings checkable. It said nothing about how a
repository **builds**, and the build is where the family has drifted without anyone seeing it. This
plan makes `repo-conventions check` enforce the standard build properties on every project, removes
`IsContinuousIntegration` everywhere, and starts trimming as a goal that each repository reaches in
its own time.

## What the family looks like today

Measured 2026-09-24 from each repository's `origin/main`:

| Repository | Root `Directory.Build.props` against the template's | `src/Directory.Build.props` (`IsTrimmable`) | AutoVersioning pin |
|---|---|---|---|
| Templates, and `bbpkg` | the reference | yes | `2026.3.916` |
| AppServices, ScopedEditors | identical but for `IsContinuousIntegration` | yes | `2026.3.914` |
| XamlQuality, AssemblyQuality | identical but for `IsContinuousIntegration` | **no** | `2026.3.914` |
| DiffView | adds `AvaloniaUseCompiledBindingsByDefault` and `IsContinuousIntegration`; lacks `PackageReadmeFile` | **no** | `2026.3.819` |
| FileServer | **diverges**: no target framework, nullable, central versions, package metadata or AutoVersioning reference in the root props | **no** | `2026.3.914` |
| AutoVersioning | a root-level analyzer project, `netstandard2.0`; its root props are a `.template` for consumers | n/a | itself |

Beyond the eight family repositories, the maintainer names ClaudeForge, chisel, bleedink.com,
dotnet-autopsy and ObexNet as good examples of their kinds. Among them are two roles the family
repositories above do not show: chisel packs an executable as a `dotnet tool` (`PackAsTool`), and
ObexNet publishes a trimmed single-file executable and packs nothing.

Two facts shape the design:

- **`IsContinuousIntegration` is set by AutoVersioning itself.** Its package `Build.props` in
  `2026.3.819` and `2026.3.914` sets `<IsContinuousIntegration>$(GITHUB_ACTIONS)</IsContinuousIntegration>`
  and declares it compiler-visible; `2026.3.916` removes it, and its release notes say the generator
  never used it. So the line in each repository's props is redundant, and the property only
  disappears from a build once that repository pins `2026.3.916`.
- **A file can say one thing and the build another.** The `2026.3.923` trim gap was a property set in a
  nested `Directory.Build.props` that a move never read; the files looked right and the evaluated
  build did not. A check of file text would have passed it.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **The check reads EVALUATED properties, after a restore.** `check` restores the repository's solution (`*.slnx`, else `*.sln`), failing if the restore fails, then runs `dotnet msbuild <project> -getProperty:… -getItem:PackageReference` for each project in it and compares the values against the rules below | A property can be set, overridden or imported anywhere: a csproj, a nested props file, a package's `build/` props. Only the evaluated value says what the build does, which is the lesson of the `2026.3.923` trim gap. ⛔ A package's props are imported only after a restore: without one, AutoVersioning `2026.3.914`'s `IsContinuousIntegration` evaluates as unset and the check would pass exactly the case it exists for |
| 2 | **Evaluation needs a checkout, so the properties are checked in local mode only**: CI's `conventions` job and a maintainer's working tree. `check --repo`, which reads a repository through the API, reports the properties as out of reach, never as passing | Evaluating needs the SDK, a restore and every imported file. Remote mode has none of them |
| 3 | **A project's role is derived, not declared**, taking the first that matches: *template* (`PackageType` Template), *analyzer* (`IsRoslynComponent`), *test* (`IsTestProject`), *tool* (`PackAsTool`), *library* (packable), *app* (an executable that is not packable) | Every project already says what it is. A declared role is one more thing that can disagree with the project. The order matters: an xUnit v3 test project is a non-packable executable, and would read as an app if *app* were tested first |
| 4 | **The baseline for every project, confirmed by measurement before it is enforced**: `TargetFramework` `net10.0` (an analyzer: `netstandard2.0`; a tool may multi-target, as chisel does, if `net10.0` is among its targets); `Nullable` `enable`; `ImplicitUsings` `enable`; `TreatWarningsAsErrors` `true`; `ManagePackageVersionsCentrally` `true`; `GenerateAutoVersionedAssemblyInfo` `true` with `Bennewitz.Ninja.AutoVersioning` at `2026.3.916` or later; `AssemblyCompany` `Bennewitz.Ninja`; and `IsContinuousIntegration` **unset**. Step 2 evaluates these across the family first; a property that a conforming repository does not meet is brought to the maintainer rather than enforced silently | The template's values, and the root props of the four repositories generated from it. They have not yet been evaluated across the family: AutoVersioning's analyzer, for one, sets no `ImplicitUsings`. `IsContinuousIntegration` unset is what "removed everywhere" means once decision 1 applies: it forces the `2026.3.916` pin, because older pins set it |
| 5 | **A library or a tool also needs** `Authors`, `PackageLicenseExpression` `MIT`, `RepositoryUrl` naming its own repository, `PackageReadmeFile` `README.md`, and `DebugType` `embedded` in Release | What makes a package acceptable on nuget.org and traceable to its source; every template-generated repository already has them |
| 6 | **A property may be exempted per project** in `.github/repository.json`, under `props`, with the reason, as `undocumented` exempts a directory. An exemption without a reason fails | A repository is sometimes right to differ: a test fixture that must reproduce an old framework, an analyzer's target. The reason is what makes an exemption reviewable |
| 7 | **Trimming is a stage, not yet a rule.** A library that lacks `IsTrimmable` and `EnableTrimAnalyzer` is reported as a NOTE by default. A repository opts in with `"trimming": "required"` in `repository.json`, after which the same finding fails. The template generates repositories with it required | Turning trimming on can surface analyzer warnings, and warnings are errors, so it is real work in some repositories. The note keeps the gap visible everywhere without turning a family of green builds red on one day |
| 8 | **Apps and tools are not held to trimming by this plan.** `PublishTrimmed` for an app is `00005`'s decision, per template; a `dotnet tool` runs on the installed framework, so a trimmable mark on it would mean nothing | ObexNet and ClaudeForge show a console app and an Avalonia app both publish trimmed; an MVC site mostly cannot. That is a per-kind choice, made where the app templates are |
| 9 | **Rollout is one commit per repository**: the new script copy and whatever property fixes the check then asks for land together | The drift check already requires every copy of the script to match the template's, so a repository that has not taken the new script shows up as drifted rather than as silently unchecked. No staging machinery is needed |
| 10 | **`IsContinuousIntegration` goes first, before the check exists**, by the maintainer's direction, with the AutoVersioning `2026.3.916` pin that makes it disappear from the build and not only from the file | The line is redundant everywhere today, and removing it needs nothing else; but under `2026.3.914` or `2026.3.819` the package still sets the property, so the pin is part of removing it |

### Dismissed

- **Checking the text of `Directory.Build.props`.** It cannot see an override or an import, and it
  would have passed the `2026.3.923` trim gap.
- **A shared MSBuild SDK or props package** (`Bennewitz.Ninja.Sdk`) that every repository imports. One
  source for the properties, but a new package whose every release touches the whole family, and a
  build whose settings are no longer readable in the repository. The check keeps each repository's
  properties in the repository and makes drift visible instead.
- **Declaring each project's role in `repository.json`.** See decision 3.

## Scope

**In:** the property check (decisions 1–7) in `scripts/repo-conventions.cs`, its tests, the template,
this repository, and every family repository; the `IsContinuousIntegration` removal and the
AutoVersioning `2026.3.916` pin everywhere; trimming turned on and required in each library repository
where it can be done without changing public API.

**Out:** trimming for apps (`00005`); FileServer's larger realignment beyond what the baseline requires
(its CLI and web projects are apps, and decision 6 exempts what it must keep); a trimming fix that
needs a public API change, which becomes that repository's own work, recorded where it is found;
ClaudeForge, chisel, bleedink.com, dotnet-autopsy and ObexNet, which are not family repositories
under `00003` and adopt the conventions only if and when their own sessions choose to.

## Steps

| # | Step | Verified by |
|---|---|---|
| 1 | Remove `IsContinuousIntegration` everywhere: delete the line from the root props of AppServices, ScopedEditors, XamlQuality, AssemblyQuality and FileServer, and raise their AutoVersioning pin from `2026.3.914` to `2026.3.916`, which its release notes call a drop-in, direct to `main`; DiffView the same, from `2026.3.819`, by pull request | After a restore, `dotnet msbuild -getProperty:IsContinuousIntegration` evaluates empty for every project in each repository; each repository's own build and tests pass before the push, and CI is green after it |
| 2 | Measure: restore and evaluate decision 4's properties for every project in the eight family repositories, and record the table in `PROGRESS.md` | The table exists; any property a conforming repository misses is raised with the maintainer before step 3 enforces it |
| 3 | Add decisions 1–7 to `repo-conventions.cs`: the restore, evaluation, roles, the baseline, exemptions, the trimming stage, and an `--offline` flag that checks the tree and the properties without calling GitHub | Tests over small real projects generated in a temp directory, restored and evaluated by `dotnet msbuild`, one per role, with one planted defect per rule, each caught. Among them: a property set in a csproj overriding a correct `Directory.Build.props`; a project pinned to AutoVersioning `2026.3.914`, whose `IsContinuousIntegration` comes only from the package and must be caught; and an xUnit v3 test project, which must be role *test*, not *app* |
| 4 | The template: `repository.json` gains `"trimming": "required"`; `verify-release` runs `check --offline` on the generated repository and expects only the markers and the empty description | `verify-release` fails when a generated project loses a baseline property, and passes as shipped |
| 5 | This repository adopts the new script; its packaging project is role *template* | CI green; `check` conforms |
| 6 | Each family repository, one at a time, in one commit: the script copy and the property fixes; DiffView by pull request | `check` conforms in each repository's CI, and `check --repo` from here reports no drift. The trimming notes each one prints are recorded in `PROGRESS.md` |
| 7 | Trimming, repository by repository, easiest first (XamlQuality and AssemblyQuality are plain libraries; AppServices and ScopedEditors already mark theirs; DiffView and ScopedEditors' Avalonia assemblies need their ILLink pass): turn it on, fix what the analyzer reports, then set `"trimming": "required"` | The repository's build is clean with the analyzer on, the trimmable mark is read off the compiled assembly, and `check` fails there if either is removed |
| 8 | Record the outcome in `PROGRESS.md` | `check --offline` conforms in every repository's own CI; the repositories still at the trimming note, and why, are listed |
