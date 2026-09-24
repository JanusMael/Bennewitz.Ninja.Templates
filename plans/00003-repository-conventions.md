# 00003 — Every family repository carries its conventions, and CI says when it does not

> Status: **approved 2026-09-24**. Supersedes nothing. Extends [`00001`](00001-package-template.md).

`bbpkg` generates the code, the workflows and the packaging guard, and nothing else a repository
needs. `Bennewitz.Ninja.AppServices` and `Bennewitz.Ninja.ScopedEditors`, the two repositories
generated from it so far, both went live with no GitHub description, no topics, no AI-facing
documentation and settings nobody chose. Nothing failed, so nobody noticed.

This plan makes the template carry three conventions, and makes CI red wherever a repository does
not meet them:

1. **Repository settings are code.** Description, topics, merge options, features and the rulesets
   that gate `main` and the release tags live in a committed file. A script applies them and
   checks them.
2. **Documentation exists at the root and in every sub-area.** Each repository has a `README.md`,
   and AI-facing documentation that names no single agent as its reader.
3. **What cannot be automated is prescribed**, in the AI-facing documentation the template ships, as
   a checklist that the script's findings point back to.

## What the family looks like today

Measured 2026-09-24 across all eight `Bennewitz.Ninja.*` repositories:

| Setting | Values found | Notes |
|---|---|---|
| Description | 6 set, **2 empty** | The two empty ones are the two generated from `bbpkg` |
| Topics | 2 set, **6 empty** | DiffView and AssemblyQuality only |
| Rulesets | **none** | In any repository |
| Branch protection on `main` | AutoVersioning only | 1 approval, last-push approval and conversation resolution required. `enforce_admins` is off, so the owner bypasses it, and only the owner pushes |
| `delete_branch_on_merge` | 4 on, 4 off | |
| Merge commits | 7 allowed, DiffView off | |
| Wiki / Projects | 6 on, DiffView and AssemblyQuality off | Unused everywhere: documentation lives in the repository |
| Vulnerability alerts | **off everywhere** | Secret scanning and push protection are on everywhere |
| AI-facing documentation | XamlQuality and AssemblyQuality `CLAUDE.md` + `AGENTS.md`; DiffView `AGENTS.md`; **the other five, none** | None has sub-area documentation |
| `README.md` | all eight | |
| Path filters on a workflow trigger | none | So a required check always reports |
| A workflow that creates a `v*` tag | none | Each release runs on a tag the owner pushed, and `gh release create` uses that tag |

Top-level directories differ widely from one repository to the next. Besides `src`, `tests`,
`scripts`, `docs` and `.github` there are `trimcheck`, `fixtures`, `reference`, `docker`, `publish`,
`samples`, `plans` and `templates`, and AutoVersioning has a flat layout (`Enums`, `Hashing`,
`Syntax`, `Tests`, `Versioning`).

**What a token without admin rights can read** (measured anonymously, as a stand-in for the
workflow's `GITHUB_TOKEN`): the description, topics, homepage and rulesets. It cannot read the merge
options, `delete_branch_on_merge`, `allow_update_branch` or `security_and_analysis`.

So there is no single standard to copy. The baseline in decision 4 is a choice, taken from
whichever repositories already got each setting right.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **`.github/repository.json` is the source of truth** for description, homepage, topics, merge options, features, security toggles and rulesets. `scripts/repo-conventions.cs` has an `apply` verb and a `check` verb, and takes `--repo` so one checkout can act on another | GitHub's settings live outside git, so neither "Use this template" nor `dotnet new` can carry them, and a workflow's `GITHUB_TOKEN` cannot write them: `PATCH /repos` needs Administration, which `GITHUB_TOKEN` cannot be granted. A committed file is reviewable in a diff, and applying it is one command |
| 2 | **`check` has two depths.** Run with the workflow's token, it checks everything such a token can read: the documentation and its markers, the description, topics and homepage, and the rulesets. Run with the maintainer's credentials as `check --admin`, it also checks the merge options, features and security toggles, and it reports what it could not read as `unverified`, never as passing | Measured above: a token without admin rights cannot see the merge options or the security settings, so CI cannot be the whole check, and a check that quietly skipped them would read as clean |
| 3 | **CI runs `check` as its own job on every push and pull request.** The release preflight runs a narrower form that refuses to publish only while a *prescribed* item is missing: an empty description, a missing document, or a marker still in place. A setting that has merely drifted does not stop a release | A freshly generated repository goes red on its first push, and the job's output lists exactly what is missing. Both generated repositories published seven packages to nuget.org before anyone noticed. But a topic someone added in the web UI is no reason to block a publish |
| 4 | **The family baseline:** squash and rebase merges only; delete branches on merge; offer "update branch"; auto-merge unavailable; issues on; wiki, projects and discussions off; vulnerability alerts and Dependabot security updates on; topics include `csharp`, `dotnet` and `nuget` | Linear history suits Conventional Commits, and DiffView already turned merge commits off. Wiki and projects are unused everywhere. Alerts cost nothing and are silent until they matter. A security update arrives as a pull request that the `main` ruleset gates like any other, and a vulnerable pin in a published package reaches every consumer. All eight repositories publish to nuget.org, so `nuget` applies to each |
| 5 | **Ruleset `main`:** no deletion, no force push, pull requests required with **0** approvals and conversations resolved, and the repository's own CI jobs required and up to date. The repository-admin role bypasses it in mode `always`: the `pull_request` mode would allow bypassing only inside a pull request, and would block the maintainer's direct pushes. `check` fails when a required check names a job that the repository's workflows do not define | Every agent and outside contributor goes through a gated pull request, and the maintainer keeps pushing directly. A required approval gates nothing for a solo maintainer, who cannot approve their own pull request and bypasses the rule anyway, as AutoVersioning shows. A required check that never runs blocks every pull request forever, and nothing on GitHub reports it |
| 6 | **Ruleset `release-tags`:** `refs/tags/v*` cannot be created, updated or deleted except by the repository-admin role, which bypasses in mode `always` | Pushing a `v*` tag publishes permanently to nuget.org. Today anyone with write access can do it, and no workflow creates one, so nothing automated is blocked |
| 7 | **The AI-facing documentation is tool-neutral, and each tool gets a pointer.** The content lives in `AGENTS.md` at the root and in each sub-area. `CLAUDE.md` at each of those levels is the single line `@AGENTS.md`, and `.github/copilot-instructions.md` points at the root one. No document addresses a particular agent by name | Whether a tool reads a file depends on its filename, and each tool looks for a different one. One copy of the content, with a thin pointer per tool, reaches all of them without assuming any. Adding a tool later means adding one pointer |
| 8 | **A sub-area is every tracked top-level directory**, and `check` derives the list from the tree rather than from a fixed set. `repository.json` can exempt one with a stated reason (for example `publish/`, if it is build output). A `README.md` is required at the root; a sub-area needs `AGENTS.md` and its pointer | The family's layouts differ too much for a fixed list, which would miss `trimcheck`, `fixtures`, `docker` and all of AutoVersioning. A directory added later is covered without anyone remembering to add it |
| 9 | **The template's documents carry real content wherever it holds for every generated repository.** Where it cannot, a marker line `<!-- bbpkg: describe ... -->` stands in, and `check` fails while any marker remains. `repository.json` names the paths that are shipped content, and `check` does not read those paths. This repository's `templates/AGENTS.md` states that everything beneath it is shipped content: the `AGENTS.md` files inside describe a generated repository, not this one, and are edited as template content | Commands, the packaging guard and the trimming rule are identical in every generated repository, so shipping them is correct. What *this* repository is cannot be known at generation time, so it is made loud rather than guessed. This repository ships the markers under `templates/` on purpose, and its own `check` must not fail on them |
| 10 | **Each repository carries its own copy of `repo-conventions.cs`**, the same way generated repositories already carry `assert-packages.cs`. `check --repo`, run from this repository, compares each repository's copy byte for byte with the template's current one and reports every copy that differs | A file-based app runs from the repository it lives in, with nothing to install. Eight unwatched copies would diverge as the rules evolve, the way the guards this family ported had already gone stale. A published tool was dismissed: it would be a new package to release before this plan could finish |
| 11 | **This repository adopts the convention it ships**: its own `repository.json`, documents and CI job | The same reason it uses `packages.push`: a guard the template repository does not run on itself is one nobody has run |
| 12 | **`docs/repository-conventions.md` in this repository is where the convention is prescribed.** It states the baseline, the documentation shape and the checklist for the steps that cannot be automated. Every `AGENTS.md` the template ships links to it, and so does each repository's root `AGENTS.md` | A repository that was not generated from `bbpkg` still needs one place that says what "conforming" means. Without one, the rule lives only in a script's source and in eight copies that can drift apart |
| 13 | **Every repository keeps a `PROGRESS.md` at its root, and the template ships one.** `check` requires it. The template's copy has the work-state sections and a marker, so the generated repository's first state is recorded rather than left blank | The family keeps its work state in `PROGRESS.md`, and six of the eight repositories have one. The two that do not are the two generated from `bbpkg`: the same gap as the missing description, with the same cause |

### Dismissed

- **Probot's `.github/settings.yml` and the Settings app.** It needs a third-party GitHub App
  installed on every repository, and its schema has no rulesets.
- **Organisation-level rulesets.** These repositories live under a personal account, which has no
  organisation rulesets.
- **A `dotnet new` post-action that runs `gh repo edit`.** At generation time the GitHub repository
  usually does not exist yet, post-actions need an interactive confirmation, and it would apply the
  settings once and never check them again.
- **Leaving the GitHub settings as the only source of truth and checking for "non-empty".** That
  finds an empty description, but not a wrong merge setting or a ruleset someone loosened.
- **Required approvals.** See decision 5.

## Descriptions and topics this plan applies

The six existing descriptions are kept word for word. The two missing ones, and every topic list,
are these:

| Repository | Description | Topics |
|---|---|---|
| Templates | *kept* | `csharp`, `dotnet`, `nuget`, `dotnet-new`, `template`, `trusted-publishing`, `github-actions` |
| AppServices | Application services for desktop apps — shell launching, environment probing, sharing, dialogs and logging — behind contracts a test can fake without a UI toolkit. Ships as Bennewitz.Ninja.AppServices, .Abstractions, .Logging and .Avalonia. | `csharp`, `dotnet`, `nuget`, `avalonia`, `desktop`, `serilog`, `dialogs` |
| ScopedEditors | Schema-driven property editors for structured data set at more than one scope, where one scope overrides another: a framework-free model, UI-free view-models and Avalonia views. Ships as Bennewitz.Ninja.ScopedEditors.Abstractions, .ViewModels and .Avalonia. | `csharp`, `dotnet`, `nuget`, `avalonia`, `property-editor`, `mvvm` |
| XamlQuality | *kept* | `csharp`, `dotnet`, `nuget`, `xaml`, `avalonia`, `accessibility`, `static-analysis` |
| AssemblyQuality | *kept* | *kept*: it already includes the baseline three |
| DiffView | *kept* | *kept*: it already includes the baseline three |
| FileServer | *kept* | `csharp`, `dotnet`, `nuget`, `aspnetcore`, `file-server`, `https` |
| AutoVersioning | *kept* | `csharp`, `dotnet`, `nuget`, `source-generator`, `roslyn`, `versioning`, `msbuild` |

## Scope

**In:**

- The template: `.github/repository.json`, `scripts/repo-conventions.cs`, the CI job, the release
  preflight step, and the documentation in decisions 7–9.
- This repository: the same, per decision 11, and `docs/repository-conventions.md`, per decision 12.
- A `PROGRESS.md` in the template, and in AppServices and ScopedEditors, per decision 13.
- **Applying the baseline to all eight family repositories.** Each gets a `repository.json`, and
  `apply` runs against it.
- **The documentation in all eight family repositories**, at the root and in each sub-area.
  - AppServices and ScopedEditors use the template's documents with their markers filled in.
  - In XamlQuality and AssemblyQuality, the narrative in `CLAUDE.md` moves into `AGENTS.md`, or
    into `README.md` where it is written for humans, and `CLAUDE.md` becomes the pointer. Nothing is
    dropped in the move.
  - FileServer, AutoVersioning and DiffView are written from their code, following the same shape.
  - ⚠ **DiffView is also maintained from another machine**, so its documentation lands through a
    pull request rather than a direct push. That way it merges against whatever that session has
    pushed, instead of racing it.

**Out:**

- Releasing the template. That follows this plan through `package-release`, as `2026.3.924` or
  later.

## Steps

| # | Step | Verified by |
|---|---|---|
| 1 | Write the `repository.json` schema and `repo-conventions.cs` (`check`, `check --admin`, `apply`, `--repo`) | Unit tests in `tests/Templates.Tests` over fixture JSON and fixture API responses, as the `mstest-to-xunit` script is tested. `check --admin --repo` against AppServices today reports the empty description, the missing topics and every baseline difference |
| 2 | Measure what the workflow's `GITHUB_TOKEN` can actually read, since anonymous reads were only a stand-in | One CI run printing which fields came back. `check`'s token depth is set to exactly that list |
| 3 | Write `docs/repository-conventions.md` (decision 12). Add the template documents (decisions 7–9), linking it, and the tree check in `verify-release` that requires them | `verify-release` fails with any one of them deleted, and passes with all of them present |
| 4 | Add the CI job and the release preflight step to the template's workflows, and extend `Templates.Tests`, which already asserts both workflows by path | A generated repository's `check` fails on the markers and lists them. The tests fail if the job or the step is removed |
| 5 | Adopt all of it in this repository (decision 11), then apply this repository's `repository.json` | CI green, including the new job, with the markers under `templates/` not reported. `check --admin` passes. The ruleset appears in `gh api repos/JanusMael/Bennewitz.Ninja.Templates/rulesets` |
| 6 | For each of the other seven, one repository at a time, land the script, the CI job and `repository.json` first; then `apply`, so the ruleset only ever requires jobs that already run. The conventions job is **not** required yet: it stays red until that repository's documentation lands, and a required red job would block every pull request in the meantime. It becomes required in the step that turns it green. DiffView's changes land through a pull request. AutoVersioning's classic branch protection is replaced by its ruleset in the same step | `check --admin --repo` reports only the documentation gaps for each. After each `apply`, a throwaway pull request shows every required check reporting, and is then closed |
| 7 | Add the documentation and a `PROGRESS.md` to AppServices and ScopedEditors, with the markers filled in from their READMEs, their code and their release history, then make the conventions job required in each | `check` passes in both repositories' CI, and the job appears among the `main` ruleset's required checks |
| 8 | Reshape XamlQuality's and AssemblyQuality's `CLAUDE.md`/`AGENTS.md` pairs into the tool-neutral shape, add their sub-area documents, then make the conventions job required | `check` passes in both repositories' CI, and every heading of each old `CLAUDE.md` appears in its new home |
| 9 | Write the documentation for FileServer and AutoVersioning, then for DiffView through a pull request, and make the conventions job required in each | `check` passes in all three repositories' CI, and DiffView's pull request merges |
| 10 | Record the outcome in `PROGRESS.md` | `check --admin --repo` passes for all eight, run from here, with no copy of the script reported as drifted |
