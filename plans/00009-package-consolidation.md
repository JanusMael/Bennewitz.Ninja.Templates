# 00009 — Package families consolidate into Inspections and AppKit

> Status: **approved 2026-10-22**. Supersedes [`00002`](00002-layerededitors-becomes-two-package-repositories.md)
> decision 1's repository count only; its package boundaries, names and API shape stand.

Six package families in five repositories become two repositories. Each family keeps its ids, its
version line and its own release. The groupings and the reasons for them are derived in
[`docs/package-consolidation.md`](../docs/package-consolidation.md), measured on 2026-10-20; this
plan records what follows from them.

| New repository | Grown from | Families moving in | Ids |
|---|---|---|---|
| `Bennewitz.Ninja.Inspections` | `Bennewitz.Ninja.XamlQuality`, renamed | XamlQuality (3), AssemblyQuality (1), CodeQuality (1) | 5 |
| `Bennewitz.Ninja.AppKit` | `Bennewitz.Ninja.AppServices`, renamed | AppServices (5), ScopedEditors (3), DiffView (2, unpublished) | 10 |

## Decisions

The first eight are the maintainer's, from the document's decisions table. The rest are how this
plan carries them out.

| # | Decision | Why |
|---|---|---|
| 1 | **Inspections**: AssemblyQuality, XamlQuality and CodeQuality | One product read three ways: compiled output, markup, source |
| 2 | **AppKit**: AppServices, ScopedEditors and DiffView | One Avalonia pin. The name promises neither desktop-only nor Avalonia-only |
| 3 | **Nothing else merges.** AutoVersioning, Templates, JsonC, FileServer, chisel and KubernetesClient.StrategicPatch stay where they are | Too little would be shared |
| 4 | **Grow by renaming** the largest family's repository | Its issues, tags and releases stay, and GitHub redirects the old URL that every published version links to |
| 5 | **One release per family**, from one workflow and one trusted-publishing policy per repository | No id is republished unchanged. One owner, so one policy with a pattern per family is enough |
| 6 | **Emptied repositories are archived with a pointer**, never deleted | Their published versions link to them forever |
| 7 | **`AgentForge.*` becomes `Bennewitz.Ninja.AgentForge.*`**, stays in ClaudeForge, and stays on GitHub Packages | The bare ids are another owner's on nuget.org. Going public is a separate, later decision |
| 8 | **XamlQuality gains `packages.push`; AutoVersioning gains it and stops pushing a glob** | A glob publishes whatever pack produced, permanently |
| 9 | **Tags are `<family>/v<YYYY.Q.MMDD>`**, the family a lower-cased stem: `xamlquality/v2026.4.1101`. A dispatched release takes a `family` input beside `version`. In a repository with families, a bare `v*` tag or a dispatch without a family fails loudly and publishes nothing | A slash reads as a namespace, and the version after `/v` parses exactly as today's tags do. A bare tag would be ambiguous about which family it releases |
| 10 | **Families are declared in `.github/repository.json`**, `"families": { "<stem>": ["<id>", …] }`. `packages.push` stays the flat list, and the conventions check fails unless the families' ids are exactly that list | One machine-read home per fact: `packages.push` says what publishes, `families` says what releases together |
| 11 | **A packable project never references a packable project of another family.** Across families the reference is a `PackageReference` to a published version. Test projects and non-packable projects (tools, demos) are exempt. The conventions check enforces it by evaluating each project's `PackageId` | A family release packs at that family's version, so a cross-family `ProjectReference` would pack a dependency on a version of the other family that need not exist. None crosses today |
| 12 | **A repository without `families` is unchanged**: `v*.*.*` tags, everything releases together, the same `release-tags` ruleset. Every multi-family addition (the `refs/tags/*/v*` ruleset include, the family input, the family-scoped assertion) is conditional on `families` being present | Every other family repository is single-family. The ruleset check compares include lists exactly, so an unconditional addition would fail all of them on their next sync |
| 13 | **Templates ships multi-family support before either merge.** Resolving tag → family → version → ids moves out of `release.yml`'s bash into a script with unit tests. `assert-packages.cs` gains a family argument and compares a family's packed ids with that family's declared ids. The GitHub release attaches only that family's packages, and its notes start from the same family's previous tag (`--notes-start-tag`). `repo-conventions.cs`, the ruleset and `docs/repository-conventions.md` follow | The release logic is the irreversible path, and a conventions run on a generated repository never executes the workflow's YAML. A script can be tested where bash in a workflow cannot. `--generate-notes` alone would diff against another family's tag |
| 14 | **CI runs every family on every pull request; no path filters** | `requiredChecks` names CI's jobs, and a job a path filter skips never reports, which blocks every pull request indefinitely |
| 15 | **Flat `src/` and `tests/`**, one solution per repository. History is imported with `git filter-repo`, renaming paths into place. **Imported tags are renamed `<family>/v…`**; the grown repository's own `v*` tags are left as they are | Project names are already unique across each group. Tags are not: `v2026.3.928` exists in all three Inspections repositories and `v2026.3.924` in two AppKit ones. The grown repository's tags already have GitHub Releases attached |
| 16 | **Files both sides have are resolved by one rule each** (the table under *Merging*). Imported approved plans move unchanged to `plans/<family>/`; the merged repository's own plans continue the grown repository's sequence | XamlQuality and AssemblyQuality both have a `00001`, as do AppServices and DiffView. Approved plans are never edited, so they move whole rather than being renumbered |
| 17 | **One central pin per package, the highest any family uses.** That raises Avalonia to 12.1.3 for DiffView and Semi.Avalonia to 12.1.0.1 for ScopedEditors, which their next releases will carry as dependency floors | One `Directory.Packages.props` per repository. The raise is real and must be said, not discovered from a consumer's restore |
| 18 | **Inside a repository, tests reference another family by project, and the package pin goes.** XamlQuality's tests use the in-tree AssemblyQuality | The gate is then the head of AssemblyQuality, which is the point of putting them together; a project and a package of one id in one restore is avoided |
| 19 | **Trimming stays `required`.** A XamlQuality library that is not yet trim-clean gets a per-project `Trimming` exemption in `repository.json` with its reason, removed when `plans/00008` side 2 lands for it | Four of the five source repositories require it. Dropping it for the merged repository would loosen four to accommodate one |
| 20 | **The grown repository's existing policy is edited to name the new repository and gain the incoming families' patterns, or deleted before its replacement is created — never two at once.** The archived repositories' policies are deleted only after each of their families' first release from the new home is verified | Whether nuget.org matches a renamed repository by its name or by its id is not established here. If by id, a second policy for the same repository is the `403` trap. An archived repository's policy matches nothing else, so it is a harmless fallback until then |
| 21 | **Moving a family is not a reason to release it.** Its first release from the new home waits for a change of its own | Decision 5. DiffView is the exception only in that its first release is its first ever |
| 22 | **Inspections goes first, AppKit second.** The AutoVersioning fix and the AgentForge rename are independent and can land at any time | Inspections is half the size, and it holds the tools AppKit's own tests use |

**Dismissed:**

- **`<family>-v<version>` tags**, ClaudeForge's `packages-v` form. They work, but the hyphen
  collides with the prerelease separator when the reader skims.
- **Inferring families from id prefixes.** `Bennewitz.Ninja.AppServices*` would quietly swallow any
  future `AppServicesX`.
- **Sections in `packages.push`.** Every existing reader drops `#` lines, so a section header there
  would be a comment that one script obeys and the rest ignore.
- **One version per repository.** Each release would ship up to nine unchanged ids.
- **A folder per family** (`families/XamlQuality/src/…`). Decision 15 rejects it.
- **Path-filtered CI.** Decision 14.
- **Renumbering imported plans into one sequence.** It edits approved plans.

## Merging

Each merge is an unrelated-history merge into the grown repository, so every file both sides have
conflicts. Each conflicts the same way, by one rule:

| File | Resolution |
|---|---|
| `README.md` | Rewritten: one section per family, each the family's README condensed, linking its rules table |
| `AGENTS.md`, per-directory `AGENTS.md` | Merged by hand; the contract each family states is kept, a repeated one once |
| `PROGRESS.md` | Rewritten: one resume section for the repository; each family's open items carried, its history evicted |
| `plans/` | Decision 16 |
| `Directory.Build.props`, `tests/Directory.Build.props` | The grown repository's file; a property only an incoming family sets moves into that family's projects |
| `Directory.Packages.props` | Decision 17 |
| `global.json` | Identical (`10.0.400`) across all six; no conflict |
| `.github/repository.json` | The grown repository's file plus `families`, the union of `requiredChecks`, and decision 19's exemptions |
| `.github/workflows/*`, `scripts/*` | The canonical files from the Templates release of step 3, plus each family's own additions carried as they are: XamlQuality's `Verification tools` job, a required check, and DiffView's `spike-injected-input.yml` |
| `docs/publishing.md` | The Templates version; one policy listing every family's pattern |
| `packages.push` | The union, which decision 10 checks against `families` |

## Scope

**In:** the two merges; multi-family support in Templates; the AutoVersioning and XamlQuality fixes;
DiffView's conformance; the AgentForge rename; archiving the four emptied repositories; deleting their
old policies; the links in family documentation that name a moved repository.

**Out:**

- Publishing AgentForge anywhere but GitHub Packages.
- Renaming any id except AgentForge's.
- Changing any family's version scheme.
- The other six repositories, beyond AutoVersioning's fix.
- Consumers' package references. Ids do not change, so ClaudeForge, the Templates scaffolds and
  dotnet-autopsy need nothing.
- Releasing any family solely because it moved (decision 21).

⛔ **No tag is pushed and no repository is renamed or archived without the maintainer's word at that
step.** A rename, an archive and a published version are each visible to everyone the moment they
happen, and the last one is permanent.

⛔ **No repository is imported while it has an open pull request.** Each one merges or closes first;
an open one would be stranded in an archived repository.

## Steps

| # | Step | Verified by |
|---|---|---|
| 1 | **AutoVersioning: `packages.push`, an explicit-id push, `assert-packages`.** Its release reads the ids from the file, as `bbpkg`'s does | CI green; a blank-version preflight prints exactly `Bennewitz.Ninja.AutoVersioning`; no `dotnet nuget push` in `.github/workflows/` names a glob |
| 2 | **XamlQuality: `packages.push`.** Its `release.yml` reads the file instead of naming three ids by hand | `assert-packages` passes against a local pack; the preflight prints the three ids |
| 3 | **Templates: multi-family support** (decisions 9–13), in the Templates release after the one already queued, on the maintainer's word | Unit tests of the resolution script: each tag form, each dispatch form, a bare tag and a missing family refused, a family's ids resolved from `repository.json`; `assert-packages` tests for a family pack that matches, one with an extra id, one with a missing id; a `conventions` test that a single-family repository's ruleset is unchanged and fails a mismatch between `families` and `packages.push` and a cross-family packable `ProjectReference`; every existing test still passes; `verify-release --published` |
| 4 | **DiffView conforms** before it moves: `repository.json`, `packages.push`, the canonical scripts, `AGENTS.md` or an exemption for `fixtures/`, `mappings/` and `reference/`; its open pull request merges or closes | `conventions` green in DiffView itself; `gh pr list` empty |
| 5 | **Inspections: rename `Bennewitz.Ninja.XamlQuality`, import AssemblyQuality and CodeQuality** on a branch, resolving by the *Merging* table; update `RepositoryUrl`, `PackageProjectUrl`, the README and `repository.json`. The maintainer runs `apply` from that branch so the ruleset's required checks are the merged CI's job names, then the pull request merges with every check read as passing | `dotnet build -warnaserror`, the full suite, and a local pack whose ids equal `packages.push`; `conventions` green; `IAssemblyRule.cs`'s history reaches the import of AssemblyQuality's first commit, found through `.git/filter-repo/commit-map` (`bd44084` before the rewrite); `git tag` lists `assemblyquality/v2026.3.929` and no duplicate |
| 6 | **Inspections: the policy (decision 20), `NUGET_USER`, then a blank-version preflight** | The preflight logs in from the renamed repository; nuget.org's policy page shows one policy naming Inspections, listing a pattern for each of the three families, and none naming XamlQuality. A login alone proves only that some policy matched |
| 7 | **Archive AssemblyQuality and CodeQuality**: transfer open issues, a final README commit naming Inspections, archive | `gh repo view` reports `isArchived: true` for both; `gh issue list` and `gh pr list` on each are empty |
| 8 | **AppKit**: steps 5–7 over AppServices, ScopedEditors and DiffView | As steps 5–7, with ten ids |
| 9 | **Each family's first release from its new home**, when it next changes, on the maintainer's word; DiffView's is its first ever. After each, delete the policy of the repository that family left | Each id read back from `api.nuget.org/v3-flatcontainer`, its new version's `RepositoryUrl` naming the new repository; the release's assets are that family's ids only; the old policy gone from nuget.org's page |
| 10 | **AgentForge rename** in ClaudeForge | `release-packages.yml`'s pack produces five `Bennewitz.Ninja.AgentForge.*` ids; ClaudeForge builds against them |
| 11 | **Links**: every family document naming a moved repository names its new home; this repository's `docs/package-consolidation.md` and `PROGRESS.md` record the outcome | A search of the family repositories finds no link to a moved repository outside an approved plan |
