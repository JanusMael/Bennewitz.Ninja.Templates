# 00010 — An analyzer package from the template

> Status: **approved 2026-10-30**. Supersedes nothing. The number was reserved on 2026-10-09,
> when the maintainer chose an analyzer template after the trimming enforcement.

`bbpkg` generates a repository whose package is a library. A Roslyn analyzer is a package too, and
everything around it is the same: the release workflow, the packaging guard, the conventions, the
documents. Only the project and its tests differ, and they differ a lot. CodeQuality was generated
from `bbpkg` and made into an analyzer by hand: `git diff e457b5d v2026.3.925` in
JanusMael/Bennewitz.Ninja.CodeQuality is the whole difference, and every line of it that is not a
rule is something the next analyzer would have to rediscover. CodeQuality now lives in Inspections,
where the same files are the living copy.

## What exists today

| Where | What it does |
|---|---|
| `bbpkg` | One library under `src/`, `Greeting`, trimmable, targeting `net10.0`; `TrimmableTests` reads the mark off the compiled assembly |
| `bbweb --blazor` | The precedent for a variant: a `bool` parameter, `sources.modifiers` excluding the files the variant does not want, and its own case in `verify-release` |
| `repo-conventions.cs` | Already knows the analyzer role: a project with `IsRoslynComponent` must target `netstandard2.0`, and the `Trimming` rule holds libraries only |
| Inspections' `src/CodeQuality` | The analyzer's project settings, each commented with its failure; `ScopedAnalyzer<TScope>`; `Catalog`; release tracking files |
| Inspections' `tests/CodeQuality.Tests` | `AnalyzerShapeTests`, `AnalyzerConventionTests`, `RulesCatalogTests`, `RuleTest<TAnalyzer>` over `Microsoft.CodeAnalysis.CSharp.Analyzer.Testing` |
| Inspections' `Directory.Packages.props` | Roslyn pinned at `4.14.0` (the maintainer, 2026-09-25), Workspaces pinned with it, and `System.Security.Cryptography.Pkcs` `10.0.14`, without which `Analyzer.Testing` `1.1.4`, still the newest, restores a vulnerable `9.0.6` and fails (GHSA-2fmv-rv2c-59x7, GHSA-846f-f764-76mx) |

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **An option, `dotnet new bbpkg --analyzer`, not a sixth template.** A `bool` parameter, as `bbweb --blazor` is: `sources.modifiers` exclude the library's files when it is set and the analyzer's when it is not, and the few shared files that differ (`Directory.Packages.props`, `README.md`, `repository.json`'s topics, `src/AGENTS.md` and `tests/AGENTS.md`) carry template conditionals | Some forty files would otherwise be copied into a second template and drift from the first: the workflows, three scripts, the documents, the guard. An analyzer repository is a package repository with a different project |
| 2 | **The analyzer project carries CodeQuality's settings, with their comments**: `netstandard2.0`, `IsRoslynComponent`, `EnforceExtendedAnalyzerRules`, the DLL packed under `analyzers/dotnet/cs` and nothing under `lib/`, `DevelopmentDependency`, `SuppressDependenciesWhenPacking`, both trimming properties `false`, every package reference `PrivateAssets="all"`, and the release tracking files | Each setting is there because its absence failed, and the comment says how. Copied, they arrive with the reasons; rediscovered, they arrive one failure at a time |
| 3 | **It ships `ScopedAnalyzer<TScope>`, `Catalog`, and one sample rule in place of `Greeting`**, which a new repository replaces on its first day, as it replaces `Greeting`. The sample rule derives from the base, has a gate with a real out-of-scope case, and has three tests: it fires on a violation, stays silent on clean code, and stays silent out of scope | The base is the family's way of writing an analyzer that costs nothing where it has nothing to say. The three tests show the shape every rule's tests take, since a rule whose gate is wrong passes every "no diagnostics" test |
| 4 | **A rule's ID prefix is a parameter, `--rule-prefix`, defaulting to `BNXX`**: `BN` and the product's initials, the family's scheme. `Catalog` and `RulesCatalogTests` read it from one constant | The prefix is a consumer's suppressions, so it is chosen once, at the start. A default that is visibly not a choice is caught by review and by the README's rules table |
| 5 | **The Roslyn group comes with it, as Inspections pins it**: Roslyn `4.14.0`, Workspaces pinned to the same version so the tests run on the oldest compiler the package claims, `Microsoft.CodeAnalysis.Analyzers` `5.9.0`, `Analyzer.Testing` `1.1.4`, and **the `Pkcs` pin at `10.0.14`**, its comment naming the advisories and when it can go | An analyzer compiled against Roslyn X loads in compiler X or later only, so the pin is a promise to consumers, and 4.14 is the family's. Without the `Pkcs` pin a generated analyzer repository fails restore on its first build |
| 6 | **The guard tests come with it, generalised**: `AnalyzerShapeTests` in place of `TrimmableTests` (the target, no reference outside what the compiler host provides, the trimming marks off, only itself packed under `analyzers/dotnet/cs`), `AnalyzerConventionTests` (every analyzer sealed and derived from the base, the base's `Initialize` sealed, no public type) and `RulesCatalogTests` (every rule's ID in the scheme, its section in `README.md`, the README's rules table matching the descriptors). `PackagingTests` is unchanged | They are what keeps a hand-written analyzer from shipping as a runtime reference, or with a dependency the compiler cannot load, or with a rule nobody documented. Each guards a failure CodeQuality met or was written against |
| 7 | **`verify-release` gains a case, `bbpkg --analyzer`**, which builds, tests and packs the generated repository, reads the `.nupkg` (the DLL under `analyzers/dotnet/cs` and nothing under `lib/`, no dependency group, `developmentDependency`), then **restores the packed analyzer into a throwaway project and sees the sample rule's diagnostic** | A test of the tree cannot show that the compiler loads the package; only a consumer's build can |
| 8 | **No new conventions rule.** `repo-conventions` already holds the analyzer role to `netstandard2.0` and leaves it out of `Trimming` | The generated repository's offline check shows only the intended gaps, which the new case verifies |

### Dismissed

- **A sixth template, `bbanalyzer`.** Decision 1: two copies of everything that is not the project.
- **A repository with a library and an analyzer side by side.** Inspections is one, made by hand; a
  template option for it can follow when a second repository needs it.
- **A code fix provider.** It needs Workspaces at run time and so its own assembly, packed beside the
  analyzer; nothing in the family has one.
- **Roslyn pinned lower.** CodeQuality's rules once built on 4.0.1, but the family chose 4.14 for both
  its analyzers, and the sample rule cannot tell which is right for the next repository's rules.
- **No sample rule, only the base.** The tests would then have nothing to show the three-way shape
  on, and `verify-release` nothing to see fire in a consumer's build.

## Scope

**In:** decisions 1 to 8 in `templates/bbpkg/`, `scripts/verify-release.cs` and this repository's
tests; this repository's `README.md` and the package's description, which list the templates; a
Templates release carrying them.

**Out:**

- Source generators, code fix providers, and Visual Basic.
- CodeQuality itself, which stays in Inspections as it is.
- Any family repository: none is waiting for this to become an analyzer repository.

⛔ **The Templates release is made on the maintainer's word, as every release is.**

## Steps

| # | Step | Verified by |
|---|---|---|
| 1 | Decisions 1, 2, 4 and 5: the option, the analyzer project, its `Directory.Packages.props` group, and the conditionals in the shared files | `dotnet new bbpkg --analyzer` builds with `-warnaserror`, its tests pass and it packs. A plain `dotnet new bbpkg` generates a tree identical to today's, file for file. Canaried: with the `Pkcs` pin removed, the generated repository's restore fails |
| 2 | Decisions 3 and 6: the base, `Catalog`, the sample rule and the guard tests | Each guard test fails on the defect it names, planted in a generated repository: the DLL packed under `lib/`, a public type, an analyzer not derived from the base, a rule missing from the README, an ID outside the prefix. The sample rule's three tests pass, and its firing test fails with the gate made to return null |
| 3 | Decision 7: the `verify-release` case | `verify-release` passes all seven cases. Canaried: with `IncludeBuildOutput` set to `true` in the template, the new case fails on the package's layout |
| 4 | Decision 8 and the documents: `README.md`, the package description, `bbpkg`'s `AGENTS.md` set and `docs/repository-conventions.md` where it names the analyzer role | The root tests; this repository's own `check` conforms; the generated repository's offline check shows only the intended gaps |
| 5 | **A Templates release**, on the maintainer's word | `verify-release`, then `verify-release --published <version>` |
