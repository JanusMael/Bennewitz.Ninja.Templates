# 00013 — One way to read upstream sources

> Status: **approved 2026-11-16**. Supersedes nothing.

On 2026-11-16 the maintainer directed that every family repository reading upstream library sources do it
one way, the best of the ways already in use. AppKit merged them that day: DiffView's and XamlQuality's
scripts of the same name, and ClaudeForge's way of pinning, into `reference/` in
JanusMael/Bennewitz.Ninja.AppKit at `d3bdafe` (its commit `adf2284`). It offered the result to Templates as
the canonical copy, as Templates holds `repo-conventions.cs` and `release-target.cs`. The maintainer chose,
through `choices` the same day, to draft a plan first.

## What exists today

| Where | What it is |
|---|---|
| AppKit `reference/` | `fetch-reference.cs`, a .NET 10 file-based app of about 1,200 lines; `sources.json`, its manifest; `README.md`, `AGENTS.md` and `CLAUDE.md`; three MSBuild shields, `Directory.Build.props` and `Directory.Build.targets`, both empty, and `Directory.Packages.props`, with central package management off. Git ignores only the checkouts, through `/reference/*/` |
| How AppKit's script pins | A source that names `packages` is fetched at the commit their nuspecs' `<repository commit>` records for the version the repository pins, read from `Directory.Packages.props`, or from tracked project, props and targets files where there is none. A source no package pins names a `ref`. Checkouts are shallow, sparse, LF and long-path; git never prompts, and each git call is killed after 15 minutes. `--status` reports, `--update` fetches what is off its pin. It refuses to fetch until git ignores every checkout folder, and exits 2 on a manifest, pin or ignore problem |
| DiffView `reference/` | Its own `sources.json`, `README.md`, `Directory.Packages.props`, `AGENTS.md` and `CLAUDE.md`, with the fetch script in `scripts/` as `.cs`, `.sh` and `.ps1`. **Its tests and its theme audit read the checkouts**: its `reference/AGENTS.md` says "The tests and the theme audit read upstream as it is at the pin" |
| Inspections | XamlQuality's script, `scripts/fetch-reference.cs`, with its manifest `scripts/fetch-reference.json`, and no tracked `reference/`, read on 2026-11-16. It was one of AppKit's inputs |
| ClaudeForge | No file named `fetch-reference.*` and no tracked `reference/`, read on 2026-11-16; its way of pinning, not a script, was AppKit's input |
| Templates | Nothing. No template ships `reference/`, and `repo-conventions.cs` knows nothing of it |

AppKit verified its script on Windows only. AppKit's `README.md` says "Nothing automated reads them: no
test, build or CI job", which is true of AppKit and false of DiffView.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **Templates holds the canonical `reference/` kit at its own root, `reference/`**: `fetch-reference.cs`, the three shields, `README.md`, `AGENTS.md`, `CLAUDE.md` and a `sources.json` that lists no source. No template ships it (the maintainer, 2026-11-16, through `choices`) | Only a repository that reads upstream sources needs it, so a template that shipped it would hand every new repository an empty folder to explain. At the root it is outside `templates/`, so it is never packed, and Templates' own `check` holds it to the documents rule like any top-level directory |
| 1a | **Every template points to it where a new repository looks first.** Each template's root `AGENTS.md` gains a checklist entry, *Reading an upstream library's source*: copy `reference/` from Templates, list the sources in its `sources.json`, and read a checkout rather than single files fetched through `gh api`. `docs/repository-conventions.md`'s *Starting a new repository* gains the same step, and Templates' `README.md`, the page `dotnet new` users find on nuget.org, one line naming the kit | The maintainer asked that a repository starting out find the kit without it being shipped. The root `AGENTS.md` is generated into every repository and is the first file an agent reads there; the conventions document is where a person setting one up reads; the README is what is seen when choosing a template |
| 2 | **The kit is AppKit's at `adf2284`, taken whole, with one change to its `README.md`**: the source table and the sentence "Nothing automated reads them" become what each repository writes for itself, since DiffView's tests do read them. The script is not edited in the taking | It is the merge the maintainer asked for, already done and verified where it was made. Editing it on the way in would make the canonical copy something nobody has run |
| 3 | **A repository that tracks `reference/fetch-reference.cs` is held to it by `check`**: `.gitignore` must make git ignore `/reference/*/`, and `reference/` carries `AGENTS.md` and `CLAUDE.md`, which the top-level documents rule already requires. A repository without the file is asked nothing. A FAIL, not a NOTE (the maintainer, 2026-11-16, through `choices`). In a checkout, which is how CI runs `check`, git is asked, `git check-ignore --no-index`, for every `dir` in `sources.json` and for a probe folder under `reference/`, so any `.gitignore` that ignores them passes. `check --repo` has no checkout: it reads `.gitignore` through the API, passes a line `/reference/*/` or `reference/*/`, and otherwise fails, saying git was not asked | The ignore line is what keeps a checkout's thousands of files out of a commit. The script refuses to fetch without it, but only when someone runs it with a source to fetch: run here on 2026-11-16 with a manifest listing none, it exits 0 with or without the line. `check` sees it on every pull request. Asking git, not matching text, is what lets DiffView's own form of the ignore pass |
| 4 | **A copy of the fetch script that differs from Templates' fails `check` run from Templates, `check --repo`, as a differing `repo-conventions.cs` does**, and is not checked inside each repository | A hash inside every copy of `repo-conventions.cs` would make each change to the fetch script a conventions release. `check` run from Templates already fails a copy of `repo-conventions.cs` that differs from the template's ("drift"); the fetch script is one more file in that comparison. The shields and `README.md` are not compared: a repository may say more in its own README |
| 5 | **The kit runs on Linux and macOS before it is prescribed.** A test in `Templates.Tests` runs `fetch-reference.cs` against a bare git repository it makes in a temporary folder, through a `ref` entry with a `file://` url, and asserts the states `missing`, `fetched`, `present` and `off-pin` and the exit codes 0, 1 and 2. Templates' CI runs it on `ubuntu-latest`; a run on macOS is the maintainer's or a peer's, recorded in `PROGRESS.md` | AppKit verified it on Windows only, and the family runs on all three. A test against a local repository needs no network and no package, so it is deterministic in CI |
| 6 | **Each repository adopts the kit itself, at its next sync or sooner**, by copying `reference/` from Templates and writing its own `sources.json` and README table. AppKit already has it, and says it has told DiffView, XamlQuality and ClaudeForge what changes for each | As every other convention reaches the family. DiffView's three scripts and Inspections' script and manifest in `scripts/` give way to the kit in `reference/`; DiffView's tests keep reading the checkouts, now fetched by it. A sync that brings decision 7's rule fails a repository until it has moved, so the move and the sync are one change |
| 7 | **`check` fails a tracked file named `fetch-reference.*` anywhere but `reference/fetch-reference.cs`** | The maintainer's direction is one way for every repository that reads upstream sources. Decision 3 binds only a repository that already has the kit, so without this a repository on an older script would pass `check` for good: DiffView's `scripts/fetch-reference.{cs,sh,ps1}` and Inspections' `scripts/fetch-reference.cs` and `.json` today. Both earlier scripts carry the name, so one path test finds them. A repository that reads upstream sources by some other name is not caught; nothing short of reading its code would catch it |

### Dismissed

- **Ship the kit in `bbpkg`, or in every template.** Every generated repository would carry an empty
  manifest and a script it does not use.
- **Leave the canonical copy in AppKit.** A family convention whose canonical copy lives in one family
  repository is changed by that repository's releases, and `check --repo` would compare against it from
  outside Templates.
- **A hash of the script inside `repo-conventions.cs`, checked in every repository.** It couples two
  scripts' release cycles; decision 4 gets the same report without it.
- **Make the checkouts tracked, or a submodule.** Thousands of upstream files in every clone, and a
  submodule pins a commit by hand where the script reads it from the package.

## Scope

**In:** the kit at Templates' root; the pointers of decision 1a in every template's root `AGENTS.md`,
the conventions document and Templates' `README.md`; the `check` rules of decisions 3 and 7 and the drift
failure of decision 4, in `repo-conventions.cs` and its tests; the cross-platform test of decision 5; a section in
`docs/repository-conventions.md`; the copy in `scripts/repo-conventions.cs`; a Templates release that ships
the new `repo-conventions.cs`.

**Out:** changing any family repository from here; what any repository lists in its `sources.json`; any
change to `fetch-reference.cs`'s behaviour beyond taking it. Once taken, the script changes in Templates'
copy, whichever session finds the need, and reaches every repository, AppKit included, at its sync, as
`repo-conventions.cs` does; a rule on which upstreams a repository should read.

## Steps

| # | Step | Verified by |
|---|---|---|
| 1 | Take AppKit's `reference/` at `adf2284` into Templates' root, the script and shields byte for byte, the README with decision 2's change, `sources.json` listing no source; `/reference/*/` in Templates' `.gitignore` | The script's hash equals AppKit's at `adf2284`; `dotnet run --file reference/fetch-reference.cs -- --status` exits 0 with nothing to report; Templates' `check` conforms, `reference/` among the documented directories |
| 2 | The cross-platform test of decision 5 | It passes on Windows here and on `ubuntu-latest` in CI; a canary, the `/reference/*/` line removed from the test repository's `.gitignore`, makes the script exit 2 and the test see it |
| 3 | `repo-conventions.cs`: decision 3's rule, both copies, with tests for a repository with the script and the ignore line, with the script and an ignore in another form that git honours, with the script and no ignore, and without the script; and the `--repo` reading of `.gitignore` | The new tests pass; a canary repository with the script and no ignore line fails `check`; `check --repo JanusMael/Bennewitz.Ninja.AppKit` conforms on that rule |
| 4 | `repo-conventions.cs`: decision 4's drift failure for `reference/fetch-reference.cs` under `check --repo` | `check --repo` against AppKit finds its copy identical; against a fixture with one byte changed, fails it as drift |
| 4a | `repo-conventions.cs`: decision 7's rule, both copies, with tests for `scripts/fetch-reference.cs`, `.sh` and `.ps1`, and for the kit's own path | The new tests pass; `check --repo JanusMael/Bennewitz.Ninja.DiffView` fails on its three scripts, `check --repo JanusMael/Bennewitz.Ninja.Inspections` on its two files, and `check --repo JanusMael/Bennewitz.Ninja.AppKit` passes |
| 5 | `docs/repository-conventions.md`: a section "Reading upstream sources", a row in "What every repository carries" for a repository that tracks the script, and the step in "Starting a new repository" | The section names the kit's files, the rule and the report; `check` and the tests that read this document pass |
| 5a | Decision 1a's pointers: the checklist entry in the root `AGENTS.md` of `bbpkg`, `bbavalonia`, `bbweb`, `bbapi` and `bbconsole`, linking the kit by its absolute GitHub URL, and the line in Templates' `README.md` | A test in `Templates.Tests` reads each template's root `AGENTS.md` and finds the entry and its link; `verify-release` still passes every template; the README's new link is absolute, since nuget.org shows the README and resolves no relative link |
| 6 | `PROGRESS.md` in each change; then a Templates release on the maintainer's word for it, and the family adopts at its next sync | Before the release, a run of step 2's test on macOS, by the maintainer or a peer, recorded in `PROGRESS.md`; the release's own gates and `verify-release --published`; each sync's `check` |
