# Progress

Work state for `Bennewitz.Ninja.Templates`. Plan: [plans/00001-package-template.md](plans/00001-package-template.md),
approved 2026-09-20 and frozen — drift from it is recorded here, never edited into it.

## Status

| Step | State | Notes |
|---|---|---|
| 1 · Repo + approved plan | **done** | `ca2552f`, the plan alone |
| 2 · Packaging project + skeleton | **done** | Packs clean; layout verified inside the `.nupkg` |
| 3 · `template.json` | **done** | Install-from-nupkg → generate → names verified |
| 4 · Packaging tests | **done** | `PackagingTests` + `scripts/assert-packages.cs`; both mutation-tested |
| 5 · Both CI and release workflows | **done** | Preflight, `RELEASING` gating, `NUGET_USER` guard, nothing globbed |
| 6 · `package-release` skill, in `bb-skills` | **done** | Approved as `*.proposed`, then installed alone — see below |
| 7 · `verify-release` | next | Pack → local install → feed poll → published install |
| 8 · First release | — | Also flips `isTemplate`, currently `false` by design |

Generating a repo from the template currently yields: **build clean with `-warnaserror`, 11 tests
passing, pack succeeding, and `assert-packages` green** — on the first run, with no edits.

## Drift from the approved plan

**Renamed to `Bennewitz.Ninja.Templates`** — repo, package id and directory. The plan's Decisions
table names `Bennewitz.Ninja.PackageTemplate`, decided before the ecosystem convention was checked.

Every `PackageType=Template` package with meaningful download share is **plural**:
`Microsoft.Android.Templates`, `Microsoft.iOS.Templates`, `Microsoft.MacCatalyst.Templates`,
`Aspire.ProjectTemplates`, `Uno.ProjectTemplates.Dotnet`, `HotChocolate.Templates.Server`. Singular
survives only in `Furion.Template.*`, where it is a prefix across a dozen per-flavour packages.

The structural reason matters more than the convention: a template package **bundles one or more
templates**, so the id names a container. `bbpkg` is the first; a second ships inside the same
package instead of claiming a new id, which a singular name fights.

Done while nothing was published and no remote existed, which is the only cheap moment for it. The
repo name still follows the package stem, so that decision is unchanged.

**Steps 4 and 5 were done together.** The plan orders the packaging tests before the workflows, but
the tests assert against those workflows, so step 4 alone could only have been written blind. No
scope changed; only the order.

**The release workflow is driven by `packages.push` rather than repeating the ids.** The plan's
step 4 verification expects each id named literally in the workflow. A template cannot know how many
ids a repository will have — and more importantly, a workflow that repeats the list has two homes
for it, and the second silently rots when a package is added. That is the failure this whole
mechanism exists to prevent, reintroduced by the guard meant to stop it. So the workflow reads the
file, the test asserts it reads the file and globs nothing, and `assert-packages` closes the loop
against the packed output.

**Package path has no `templates/` segment.** The plan's step 2 verification names
`content/templates/bbpkg/.template.config/template.json`. The explicit
`PackagePath="content\%(RecursiveDir)…"` produces `content/bbpkg/.template.config/template.json`,
because `%(RecursiveDir)` is relative to the wildcard root and so excludes `templates/` itself.

The shorter path is correct and is what the engine expects; the plan's line described the behaviour
of `ContentTargetFolders`, which that step replaced. No action — recorded so the next reader is not
surprised by the mismatch.

## Verified, not assumed

Each of these was run, not reasoned about. They are the findings that four rounds of plan review did
not surface.

- **`dotnet pack` fails without the NU5128 suppression.** `Directory.Build.props` sets
  `TreatWarningsAsErrors`, and a template package ships no `lib/` by design. Reproduced twice
  independently before the suppression existed.
- **`IncludeContentInPack=false` is not an alternative fix** — it drops the packed README and pack
  dies with NU5017 instead.
- **Content outside the packaging project's cone flattens**, dropping `.template.config/`, and the
  resulting package still installs, still lists, and still "generates" — emitting the extracted
  nupkg. Any check written against an exit code passes on it. This is why step 3 installs from the
  packed `.nupkg` and asserts the generated tree.
- **One `sourceName` cannot produce both names.** Verified end to end:
  `dotnet new bbpkg -n Widget` yields `AssemblyName` `Widget` and `PackageId`
  `Bennewitz.Ninja.Widget`, the latter from a `join` generator over `name`.
- **NuGet excludes dotfiles from a package by default** (NU5119), so a template ships generated
  repos with no `.gitignore` and no `.gitattributes`. `NoDefaultExcludes` fixes it. ⚠ This is a
  *warning*: the family's `TreatWarningsAsErrors` is the only reason it surfaced here rather than in
  somebody's generated repo.
- **`PackagePath` must be the bare root `content\`.** Both more explicit forms are wrong, and both
  were tried: `content\%(RecursiveDir)%(Filename)%(Extension)` is correct for every file *with* an
  extension and packs `LICENSE` to `content/bbpkg/LICENSE/bbpkg/LICENSE`, because NuGet reads a
  final segment with no extension as a folder. `content\%(RecursiveDir)` is a folder too, so NuGet
  appends the recursive path a second time.
- **Both guards fail when they should.** Removing the id from `packages.push` fails
  `Every_packable_project_is_classified` *and* makes `assert-packages` exit `1` — checked without a
  pipe, since `$?` after one reports the last command's status rather than the script's.
- **Duplicate template installs break generation.** Installing the same identity from a folder and
  from a `.nupkg` leaves two registrations and `dotnet new` fails with "Sequence contains more than
  one matching element". Uninstall until `dotnet new list` is clean, and clear
  `~/.templateengine/packages/`, before reinstalling.

## The skill, as built (step 6)

It lives at `C:\c\cl\bb-skills\package-release\SKILL.md` and is installed to `~/.claude/skills/`.

⚠ **Its gate is not `packages.push`.** That was the first design, and testing it against the real
repositories showed it would decline in `Bennewitz.Ninja.XamlQuality` — the repository that taught it
every rule it states — because none of the five package repos has that file yet. It greps for
`NuGet/login` instead, engages in all five, and declines in a repository that does not publish to
nuget.org at all. That matters because it is installed globally and fires everywhere: FileServer,
AutoVersioning and chisel all glob their pushes by deliberate design.

⚠ **`Install-Skills.ps1` was NOT run.** It replaces every skill, and four of the eight differ from
their installed copies in both directions. `package-release` was installed on its own by copying its
folder. Resolving that drift is deferred.

## Next

Step 7, `verify-release`. Then step 8, which is the template's own `release.yml` and first release —
until that exists, this repository cannot publish itself, so the workflow shipped inside
`templates/bbpkg/.github/workflows/` is guarded only by the generated repo's own tests. That is the
weakness [the plan](plans/00001-package-template.md) names rather than hides.

⚠ Still outstanding from the plan: this repository has **no `release.yml` of its own** yet, so it
cannot publish itself. That is step 8, and until it exists the shipped workflow inside
`templates/bbpkg/.github/workflows/` is guarded only by the generated repo's own tests — which is
the weakness the plan names rather than hides.
