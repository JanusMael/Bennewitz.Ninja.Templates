# 00011 — Package families, completed

> Status: **approved 2026-10-29**. Supersedes [`00009`](00009-package-consolidation.md)
> decision 13's release notes only; the rest of 00009 stands.

`plans/00009` step 3 taught `bbpkg` to release one family at a time. Building Inspections on it, and
releasing AssemblyQuality from there, found what it left out: everything that makes a family's
release *read* as that family's. A family's packages pack the repository's README, its assemblies
carry the repository's product name unless a project overrides it, and its GitHub release lists every
pull request merged anywhere in the repository since the family's previous tag: 95 of XamlQuality's
for AssemblyQuality `2026.4.1029`, and not AssemblyQuality#17, the one change it shipped. Inspections
was fixed by hand. This plan makes those fixes canonical, so AppKit (`plans/00009` step 8) merges
onto them instead of repeating them.

## What exists today

| Where | What it does |
|---|---|
| `bbpkg`'s root `Directory.Build.props` | Sets `AssemblyProduct` to the repository's stem, and packs the root `README.md` into every packable project |
| Inspections' root `Directory.Build.props` | Packs `$(FamilyReadme)` instead, which each package's csproj sets beside its family's `AssemblyProduct`. A package without one packs no README, and `dotnet pack` fails it with `NU5039` |
| `release.yml` | A family's notes are `--generate-notes --notes-start-tag "$PREVIOUS_TAG"`, a family's first release says only that it is the first. GitHub decides "Latest" itself |
| `release-target.cs resolve` | `PREVIOUS_TAG` is the family's newest `<family>/v…` tag below the version. A grown repository's bare `v…` tags are nobody's, so XamlQuality's first family release would call itself its first, after eight releases |
| `repo-conventions.cs` | Checks the families against `packages.push`, the stems, and cross-family references. Nothing about a family's README or product |

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **A family's README is `docs/<stem>.md`**, and each of its packable projects names it in `FamilyReadme`. The root props pack `$(FamilyReadme)` as `README.md` when it is set, and the root `README.md` otherwise | nuget.org shows each package its own family's page, not the repository's overview. One fixed place per family is something a check can find. Without families nothing changes: no project sets the property |
| 2 | **A family's README links absolutely.** A relative link or image in `docs/<stem>.md` FAILS: inline, reference-style (`[x]: target`) or an HTML `href` or `src`, outside fenced and inline code | It is a nuget.org page, where a relative link resolves against nuget.org and goes nowhere. An anchor within the page (`#…`) is allowed. A link shown as an example inside code is text, not a link |
| 3 | **A family's packages set their family's `AssemblyProduct`**: the stem, compared without case and without its separators (`XamlQuality` for `xamlquality`). The root's stays the repository's, for tests, tools and scripts, since AutoVersioning fails a project without one (`BAUTOVERSIONING03`) | The product is stamped into every shipped assembly, permanently, per version |
| 4 | **`repo-conventions check` FAILS, in a repository with families, on a family without `docs/<stem>.md`, a family package whose `FamilyReadme` or `AssemblyProduct` is not its family's, and a relative link in a family README.** The projects' properties are read evaluated, as the cross-family reference rule reads them | Each is caught on the pull request that breaks it, before any tag |
| 5 | **`assert-packages --family` also compares each package's packed `README.md` with `docs/<stem>.md`, byte for byte** | The last gate before the push, reading what pack produced. Decision 4 checks what the projects say; this checks what they did |
| 6 | **A family's release notes are the commits since its previous tag that changed its packages**, written by a new `release-target.cs notes` verb and passed to `gh release create` as `--notes-file`. A commit is the family's when it touched the family's own paths, the directories of the projects that pack its ids and of any project they reference that packs nothing, and its `docs/<stem>.md`; or when it touched a build input every package shares (the root's `Directory.Build.*`, `Directory.Packages.props`, `global.json`, `NuGet.config` and `AssemblyInfo.*.cs`, and `src/Directory.Build.*`) **and descends from the family's previous tag**. Merge commits are left out. One line per commit, its subject and short hash, so a squash-merged pull request reads as its title and number; the shared inputs' commits under a heading of their own | GitHub's generated notes cannot be scoped to paths. The two scopes differ because of imported histories: no other family's history touches this family's paths, so those are read across the whole range, but each imported history has root build files of the same names, so those count only from the family's own tag onward. Measured on Inspections: this gives AssemblyQuality `2026.4.1029` the merge's three commits and `ed5544b`, #17, which touched only root files; without the descent rule, its shared list held XamlQuality's and CodeQuality's whole pre-merge histories; with the descent rule on its own paths too, XamlQuality's first family release would lose the merge's commits, made on a branch cut before its tag. A script is testable against fixture repositories, which a workflow step is not |
| 7 | **One family may claim the repository's bare `v…` tags as its own**: `"bareTags": "<stem>"` in `repository.json`. `resolve` then counts them as that family's when it finds the previous tag. `check` FAILS a value that is not a declared family, or the key without `families` | A grown repository's bare tags are its first family's releases, and only the maintainer can say so. Inspections sets `xamlquality`, AppKit will set `appservices`. A family with neither, DiffView's first release, still says it is the first |
| 8 | **A family release is marked "Latest" unless a release tag of any family, or a bare `v…` tag, has a higher version**: `resolve` sets `LATEST` from the tags, and `gh release create` takes `--latest=$LATEST`. So the marker always names the repository's newest release, whichever family's, and a dispatch re-releasing an older version leaves it where it is | Left to GitHub, it is decided by creation date and by version, as a `<family>/v…` tag may not parse as one. A single-family repository is unchanged: its tags parse, and its newest release is its highest |

### Dismissed

- **GitHub's notes, configured by labels** (`.github/release.yml` categories). They still cannot be
  scoped by path, and every pull request would need its family's label by hand.
- **Notes from the pull requests API, filtered by their files.** A call per pull request, nothing
  testable offline, and an imported family's pull requests are in its archived repository.
- **`--latest=false` on family releases.** The marker would stay forever on the last release before
  the repository had families, or on none.
- **A `primary` family that alone takes "Latest".** A key in `repository.json` for a marker nothing reads.
- **Deriving the README from `AssemblyProduct`.** One property would decide two things, one of them
  invisibly; Inspections' explicit `FamilyReadme` is built and released.
- **A family README anywhere.** Decision 4 needs a place to look.

## Scope

**In:** decisions 1–8 in `bbpkg`'s root `Directory.Build.props`, `release.yml`, `release-target.cs`,
`assert-packages.cs` and the canonical `repo-conventions.cs`, its copy in `scripts/`;
`docs/repository-conventions.md`'s *Package families*; tests; a Templates release carrying them; and
Inspections' sync to that release.

**Out:**

- **A single-family repository's packed README.** `bbpkg`'s root `README.md` is packed into every
  package with relative links and a section on releasing, so the same defect is on every
  single-family package page. Changing it touches every family repository, so it is a plan of its
  own, `plans/00012`, drafted after this one is released.
- The `System.Security.Cryptography.Pkcs` pin, which is `plans/00010`'s.
- AppKit's merge, `plans/00009` step 8, which follows this plan's release.
- Releasing any Inspections family to exercise the new notes (`plans/00009` decision 21), and
  rewriting the notes of releases already made.

⛔ **The Templates release is made on the maintainer's word, as every release is.**

## Steps

| # | Step | Verified by |
|---|---|---|
| 1 | Decisions 6–8: the `notes` verb and `bareTags` in `release-target.cs`; `LATEST` in `resolve`; `release.yml` writes and passes the notes and `--latest` for a family; `bareTags` parsed and checked in `repo-conventions.cs` | `ReleaseTargetTests` over fixture repositories, each failing against today's script: a family's notes holding its own commits and the shared inputs' heading, and none of another family's; an unrelated history merged in after the family's tag, whose own root build files are listed for neither family; a commit on a branch cut before the tag and merged after it, listed when it touched the family's paths; a first release with no previous tag; `bareTags` making a bare tag the previous one, and only for its family; `LATEST` true for the highest version among every family's tags and the bare ones, and false for a version below another family's newer tag. A `RepoConventionsTests` case each for a `bareTags` naming no family and one without `families`. Then `notes` run in Inspections from `assemblyquality/v2026.3.929` to `assemblyquality/v2026.4.1029` lists `7e82d76`, `1701070`, `7e5236f` and `ed5544b`, #17's commit, and nothing else |
| 2 | Decisions 1–5: the root props item, the four conventions rules, the README comparison in `assert-packages.cs` | A `PropsTests` fixture with two families failing each rule in turn, against today's script; `AssertPackagesTests` failing a family pack that carries the root README. A fixture of two families packed for real, by `dotnet pack` through `bbpkg`'s root props, each package carrying its family's `docs/<stem>.md` as `README.md` and a project without `FamilyReadme` the root `README.md`: the guard's other tests read hand-made packages, `verify-release`'s `bbpkg` has no families, and Inspections keeps its own props, so nothing else would pack through the new item before AppKit. `verify-release` passes: the generated `bbpkg` has no families and packs its root README as before. `check --offline --root` against Inspections conforms |
| 3 | `docs/repository-conventions.md`'s *Package families*, and the shipped files' comments | `TemplateCopiesTests` passes; this repository's own `check` conforms |
| 4 | **A Templates release**, on the maintainer's word | `verify-release`, then `verify-release --published <version>` |
| 5 | **Inspections syncs** to that release, setting `"bareTags": "xamlquality"` | `conventions` green there; `resolve` for a `xamlquality/v…` tag gives `PREVIOUS_TAG` `v2026.4.1026`, and for `codequality/v…`, `codequality/v2026.3.928`; `notes` from each family's previous tag lists the merge's own commits and that family's, and none of another family's history |
