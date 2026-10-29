# 00012 — Every packed README is a nuget.org page

> Status: **approved 2026-10-29**. Supersedes nothing. Follows
> [`00011`](00011-package-families-completed.md), whose *Out* list left it here.

`plans/00011` made a family's packed README fit for nuget.org: its own file, absolute links, and a
check on each. A repository without families still packs `bbpkg`'s root `README.md` into every
package, and that file is written for the repository, not for nuget.org. Read from each repository's
`main` on 2026-10-29, AppServices, ScopedEditors and JsonC each pack the template's four relative
links, `docs/publishing.md`, `packages.push`, `packages.local` and `LICENSE`, every one dead on
nuget.org, and the template's *Releasing* section, which tells a package's consumer how to set up
trusted publishing. Some repositories pack another file: FileServer its library's own
`src/Bennewitz.Ninja.FileServer/README.md`, DiffView its hosting guide. Nothing reads any of them for
links.

## What exists today

| Where | What it does |
|---|---|
| `bbpkg`'s `README.md` | The package's description, an *Install* section, a *Releasing* summary of `docs/publishing.md`, and a *Licence* section; four relative links |
| `bbpkg`'s root `Directory.Build.props` | Packs the root `README.md` into every packable project without `FamilyReadme` |
| `docs/repository-conventions.md` | "`README.md`: for humans. In a package repository it is packed, so it is also the nuget.org description" |
| `assert-packages.cs` | Run by CI's `pack` job on every push and pull request, and by the release. With `--family` it compares each packed README with `docs/<stem>.md`; otherwise it reads no README |
| `repo-conventions.cs` | Fails a relative link in a family's `docs/<stem>.md` (`plans/00011` decision 2); reads no other README |

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **The root `README.md` stays the package's README in a repository without families**, and is written for both readers: what the package does, how to install and use it, its licence. Maintainer instructions live in `docs/publishing.md` and `AGENTS.md`, which already hold them in full | One file, as the conventions already say. A second file for nuget.org would say the same things and drift from the first. A consumer on nuget.org has no use for a trusted-publishing policy |
| 2 | **`assert-packages.cs` fails a package of `packages.push` whose packed README holds a relative link**, with or without `--family`: the rule of `plans/00011` decision 2, applied to the README each package's `.nuspec` names, whatever its file name. A package of `packages.local` is not read | It reads what pack produced, so it holds every published package to the rule, whichever file a repository packs: the template's root README, FileServer's library README, DiffView's hosting guide, a family's. CI's `pack` job runs it on every pull request, so a link breaks the pull request that adds it, not a release. A `packages.local` package never reaches nuget.org, so its README is nobody's page there |
| 3 | **The link reader is one rule in two scripts**: `assert-packages.cs` carries a copy of `repo-conventions.cs`'s `Links`; the tests run the same cases against both and hold the two copies identical, as `TemplateCopiesTests` holds the copies of `repo-conventions.cs` | Each script is a file-based app copied into every repository on its own; neither can reference the other, so only a test keeps the copies from drifting |
| 4 | **`bbpkg`'s `README.md` links absolutely**, through the placeholders (`https://github.com/REPO_OWNER/PKG_ID/blob/main/LICENSE`), and drops *Releasing*; `AGENTS.md` already says "**Releasing:** `docs/publishing.md`" | What every new repository starts from passes the rule it is checked by |
| 5 | **Each family repository fixes its README when it next syncs**, as it adopts every other rule: its CI's `pack` job fails until it does | The failure names the file, the line and the link. No repository is changed from here |

### Dismissed

- **A separate packed file, `docs/package.md` or the like, beside the root README.** Two documents
  saying what the package is, and the second one only nuget.org shows.
- **A `repo-conventions` rule on the root `README.md`.** Whether the root README is packed at all is
  decided by the build: DiffView packs a hosting guide and not its root README, so a rule on the
  root file would fail a file nobody packs, and miss the one that is.
- **Rewriting relative links at pack time**, to `github.com/…/blob/main/…`. A build step that edits
  documentation, untestable by reading the file, and it hides the relative links from whoever reads
  the README on GitHub, where they keep working and nothing says they are packed.
- **A NOTE first, a FAIL later.** A rule that warns is a rule nobody reads; the repositories adopt it
  when they sync, which is when they read the failures.

## Scope

**In:** decisions 1–4 in `bbpkg`'s `README.md`, `scripts/assert-packages.cs` and their tests;
`docs/repository-conventions.md`'s `README.md` row; `bbpkg`'s `AGENTS.md` and `scripts/AGENTS.md`; a
Templates release carrying them.

**Out:**

- Changing any family repository's README from here (decision 5). Inspections packs only its
  families' READMEs, which `plans/00011` already holds to the rule.
- This repository's own `scripts/assert-packages.cs`, kept apart from the template's by intent; its
  package's README has no relative link.
- The app templates, which publish nothing to nuget.org.

⛔ **The Templates release is made on the maintainer's word, as every release is.**

## Steps

| # | Step | Verified by |
|---|---|---|
| 1 | Decisions 2 and 3: the link check in `assert-packages.cs`, with and without `--family` | `AssertPackagesTests` failing against today's script: a package whose README has a relative link, inline, as an image, a reference definition or an HTML attribute, fails with the package, the line and the link; one whose relative links are inside code passes; a README under another name is read; a `packages.local` package with a relative link passes. The same link cases run against `repo-conventions.cs` and pass there unchanged, and a test fails when the two `Links` classes differ by a character |
| 2 | Decisions 1 and 4: `bbpkg`'s `README.md`, and the documents that describe it | `verify-release`: the generated `bbpkg` packs its README and `assert-packages` passes on it. `FamilyPackTests`' project without `FamilyReadme` packs a root README with a relative link and the guard fails it. `TemplateCopiesTests` passes; this repository's own `check` conforms |
| 3 | **A Templates release**, on the maintainer's word | `verify-release`, then `verify-release --published <version>` |
| 4 | Recorded in `PROGRESS.md`: which family repositories' packed READMEs the rule fails today, measured by packing each repository's `main` with the released script | A table of repositories and findings |
