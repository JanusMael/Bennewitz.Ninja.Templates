# Progress

Work state for `Bennewitz.Ninja.Templates`. Plan: [plans/00001-package-template.md](plans/00001-package-template.md),
approved 2026-09-20 and frozen — drift from it is recorded here, never edited into it. The same
holds for [plans/00003](plans/00003-repository-conventions.md), in progress: see its own section.

## Resume

*Replaced, never appended, at each handoff. Rewritten 2026-10-22 after `Bennewitz.Ninja.Templates
2026.4.1022` was released and verified from nuget.org (its section below); item 2 updated 2026-10-29
when AssemblyQuality released from Inspections and Inspections' links were repointed, when
`plans/00011` was approved, when AssemblyQuality's old policy was deleted and the last link pull
requests merged, and as `plans/00011`'s steps were done.*

**Next, in order** (the maintainer's decisions of 2026-10-09 and 2026-10-22, below among the locked
decisions):
1. **Sent 2026-10-22; follow up when each repository syncs.** The note to each family repository,
   from `check --repo` run against each that day: issues AppServices#16, ScopedEditors#12,
   AutoVersioning#6, FileServer#12, JsonC#4 and CodeQuality#3 (no session running), and messages to
   the AssemblyQuality, XamlQuality and DiffView sessions. Every copy of the script was drifted; the
   retired key remains in AppServices, ScopedEditors, AssemblyQuality, JsonC and CodeQuality; no
   repository globs. TrueCourseCalculator, Geo.Core and GeoHash have no remote and no session, so
   they had no note. What it was to carry, so each adopts the friend grants and the release's other
   conventions on its next sync: FileServer and ScopedEditors (each documents "grants
   to tests only"), AssemblyQuality (its BNAQ fixtures opt out with `SolutionFriendGrants=false`), and
   TrueCourseCalculator, Geo.Core and GeoHash (multi-targeted, prefixed assemblies: a grant to them in
   an External file needs the `// repo-conventions: assembly name` marker). AppServices has already
   adopted the grants (JanusMael/Bennewitz.Ninja.AppServices#9). The same note says the `"trimming"`
   key is retired: drop it from `repository.json`. TrueCourseCalculator, Geo.Core and GeoHash also push
   `artifacts/*.nupkg`, which the no-glob rule fails once they adopt the script (local-only today: no
   remote, no script copy). Each `Bennewitz.Ninja.*` library also starts failing `Trimming` on its next
   sync if its repository trims and the library lacks `IsTrimmable` or `EnableTrimAnalyzer`.
2. **[`plans/00009`](plans/00009-package-consolidation.md): Inspections is done, steps 5 to 7, and
   AssemblyQuality released from it** (their sections below). Next, in order:
   - **[`plans/00011`](plans/00011-package-families-completed.md), approved 2026-10-29**: steps 1
     to 4 done, released as `2026.4.1029`; next, Inspections' sync (its section below), then a
     draft of `plans/00012`, a single-family repository's packed README. AppKit merges onto that
     release. The `Pkcs` pin goes to `plans/00010`.
   - **Step 11, AppKit's links**, after step 8; DiffView's among them. Inspections' are done.
   - **CodeQuality's old policy on nuget.org** is deleted after its first release from Inspections
     (decision 20); AssemblyQuality's is gone.
   - **Step 8, AppKit**, after step 4, DiffView conforming, which DiffView's own session was sent one
     note about on 2026-10-29 and is not followed up from here. Its policy must be edited to the
     renamed repository's name before any release (step 6's finding).
3. **Investigate xunit.v3 4.x** (4.0.1, with TrxReport 2.5.1): what it changes for the templates, this
   repository's tests and `mstest-to-xunit`. Report; the maintainer decides the scope.
4. **`plans/00010`, an analyzer template**, after the trimming enforcement. `git diff e457b5d
   v2026.3.925` in JanusMael/Bennewitz.Ninja.CodeQuality is the whole difference an analyzer needed.
5. [`plans/00004`](plans/00004-standard-build-properties.md) step 8 closes when
   [DiffView#2](https://github.com/JanusMael/Bennewitz.Ninja.DiffView/pull/2) merges and DiffView's CI
   runs the check green. DiffView is not watched from this session (the maintainer's word,
   2026-09-28).

**Waiting on others:**
- XamlQuality: side 2 of the trimming enforcement, `plans/00008` step 3, behind `Trimming.Trims`. ⚠ On
  2026-10-22 the running XamlQuality session said it does not hold that patch; the design came from an
  earlier session on 2026-10-10. Nobody may be writing it; it needs picking up in XamlQuality.
- XamlQuality: its sync to `2026.4.1022` is queued behind its current work, on the maintainer's
  go-ahead. No project there sets a trimming property, so it does not trim.
- AssemblyQuality: synced to `2026.4.1022` on its unpushed branch `feat/friend-grants` (`ba229fe`, on
  the friend-grants adoption `c385770`): `check --offline` conforms, 54 of 54 tests. The push and pull
  request wait on the maintainer's word.
- DiffView#2 stays red on `conventions` until DiffView's session writes its `AGENTS.md` set.
- bleedink.com: its `/version` reads `AssemblyInformationalVersion`, so it answers "Built with ♥";
  passed to its session on 2026-10-09.

**Peer sessions on this machine** (addresses change when a session restarts; `ListAgents` gives the
current names): AppServices, AssemblyQuality, XamlQuality, FileServer, AutoVersioning, CodeQuality.

**Environment:** Docker Desktop is running (engine 29.8.0). A native Windows publish takes the newest
Visual Studio or Build Tools with the C++ tools, now Build Tools 2026. An agent shell sets
`NoDefaultCurrentDirectoryInExePath=1`, so there `vswhere.exe` must be on `PATH`
(`%ProgramFiles(x86)%\Microsoft Visual Studio\Installer`); a normal terminal does not need it, and
`verify-release` adds it for its own publish. A deep scratch path overflows Windows' path limit for
TrueCourseCalculator's long project names: clone it under a short `%TEMP%` path with
`core.longpaths=true`.

**Locked decisions** (the maintainer's; do not reopen):
- Every family repository meets `docs/repository-conventions.md`: the settings baseline lives in the
  script; `main` is gated by pull requests with 0 approvals and an admin bypass in mode `always`;
  AI-facing content is in `AGENTS.md` with tool pointers; a `README.md` at the root only; Dependabot
  security updates on; one script copy per repository, drift-checked from here.
- Avalonia and drivable-UI lessons go to XamlQuality, family-wide.
- Enforce the standard `Directory.Build.props`; trimming is a goal that may roll out slowly.
- A library or tool packs a README under any name.
- The `nuget` topic is required only where `packages.push` names an id.
- Work only from a git worktree under this session's scratch directory, never in the shared checkout
  `C:\c\cl\Bennewitz.Ninja.Templates`, which other sessions use. Push with
  `git -c credential.helper= -c "credential.helper=!gh auth git-credential" push …`.
- A change in a family repository updates that repository's `PROGRESS.md` in the same commit, in
  that file's own format.
- Green pull requests in this repository may be admin-merged (the maintainer, 2026-09-25 and since);
  auto-merge is off by the family baseline. The repository allows rebase and squash merges, not merge
  commits; its pull requests are rebase-merged.
- One release per calendar day, `YYYY.Q.MMDD`, three parts, family-wide (2026-09-25). No same-day
  patch: a fourth part already means AutoVersioning's `HHmm`, and a prerelease suffix sorts below
  the release it would fix.
- A release happens only on the maintainer's explicit word for that release.
- Trimming is a family policy (2026-09-28, decided with XamlQuality): a repository that trims sets
  `IsTrimmable` and `EnableTrimAnalyzer` on its libraries, and every family library it depends on,
  transitively, must be trimmable too; with neither property set, nothing is enforced. The
  conventions checker enforces both sides.
- Friend grants (`plans/00006`): solution-wide, generated, linked; opt out per project; adoption on
  next sync with the opt-out as the escape hatch, no repository-wide switch; a prefixed External name
  is a NOTE silenced by `// repo-conventions: assembly name` (2026-10-01).
- Entry points (`plans/00007`): exit codes 0/1/2/130 with 2 for `bbconsole` only; `bbconsole` trimmed
  single-file; the behaviour lives in an AppServices helper.
- The repository is not a GitHub template (`isTemplate: false`, 2026-09-28); templates ship only
  through `dotnet new`.
- Decided 2026-10-09, through `choices`:
  - The trimming enforcement: draft `plans/00008` for side 1 now, and tell XamlQuality its side 2 is
    unblocked.
  - A release never pushes a `*.nupkg` glob: a conventions FAIL, in the next release.
  - AssemblyQuality in every template, in the next release. (Asked for `bbpkg`, `bbweb` and `bbapi`;
    `bbconsole`, which postdates the question, is included on the same reasoning.)
  - xunit.v3 4.x: investigate and report first; the scope is decided after.
  - An analyzer template: `plans/00010` (first named `00009`, which the package consolidation
    took on 2026-10-22), after the trimming enforcement.
  - The next release waits for the no-glob rule, AssemblyQuality in every template, and the trimming
    enforcement's side 1.
  - The family is told about the friend grants after that release, in one note per repository.
  - bleedink.com's `/version` defect goes to its session by message.
  - The previous session's leftover worktrees are removed once each is checked clean and merged.

## Status

| Step | State | Notes |
|---|---|---|
| 1 · Repo + approved plan | **done** | `ca2552f`, the plan alone |
| 2 · Packaging project + skeleton | **done** | Packs clean; layout verified inside the `.nupkg` |
| 3 · `template.json` | **done** | Install-from-nupkg → generate → names verified |
| 4 · Packaging tests | **done** | Both halves. Shipped: `PackagingTests` + `scripts/assert-packages.cs`. Root: `tests/Templates.Tests`, 13 tests, asserting **both** release workflows by path. Mutation-tested |
| 5 · Both CI and release workflows | **done** | Both halves. The root pair was missing until step 8 and this row wrongly read done — see the drift note |
| 6 · `package-release` skill, in `bb-skills` | **done** | Approved as `*.proposed`, then installed alone — see below |
| 7 · `verify-release` | **done** | `scripts/verify-release.cs`; pack → packed-content → install-from-nupkg → generate → tree → build/test/pack. Feed poll and published install are step 8's, per the plan |
| 8 · First release | **done** | `2026.3.921` published 2026-09-21 and verified from the feed: installed from nuget.org, generated, tree asserted, generated repo builds/tests/packs. `isTemplate` flipped |

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
- **`PackagePath` must be the bare root `content/`.** Both more explicit forms are wrong, and both
  were tried: `content/%(RecursiveDir)%(Filename)%(Extension)` is correct for every file *with* an
  extension and packs `LICENSE` to `content/bbpkg/LICENSE/bbpkg/LICENSE`, because NuGet reads a
  final segment with no extension as a folder. `content/%(RecursiveDir)` is a folder too, so NuGet
  appends the recursive path a second time.
- **⛔ The trailing separator must be a FORWARD slash, and this one shipped broken.** NuGet decides
  "folder or file?" by whether `PackagePath` ends in a directory separator, and a backslash is
  only a separator on Windows. `content\` was correct on Windows and **flattened on Linux**,
  dropping `.template.config/` — precisely the failure the bare-root form exists to prevent,
  reintroduced by the separator rather than the path. Every earlier observation in this section
  was a Windows pack, which is why nothing caught it; the release runs on `ubuntu-latest`, so the
  first real release would have published a template that installs, lists and "generates" while
  emitting the extracted nupkg.
  ⭐ Found by `verify-release` on this repository's **first CI run**, in the 26 seconds after the
  branch merged. That is the whole argument for running the gate on the platform that publishes
  rather than trusting a green local check — and the bug predates all of step 7, having been in
  the packaging project since step 2.
- **Both guards fail when they should.** Removing the id from `packages.push` fails
  `Every_packable_project_is_classified` *and* makes `assert-packages` exit `1` — checked without a
  pipe, since `$?` after one reports the last command's status rather than the script's.
- **Duplicate template installs break generation.** Installing the same identity from a folder and
  from a `.nupkg` leaves two registrations and `dotnet new` fails with "Sequence contains more than
  one matching element". Uninstall until `dotnet new list` is clean, and clear
  `~/.templateengine/packages/`, before reinstalling.
- **`dotnet run <file.cs>` binds to the PROJECT when the working directory has one.** From this
  repository root it tries to run `Bennewitz.Ninja.Templates.csproj` and fails with *"The current
  OutputType is 'Library'"*. `verify-release` therefore needs `dotnet run --file`, while
  `assert-packages` does not: a generated repository keeps its projects under `src/` and `tests/`,
  so nothing at its root captures the bare form. Both invocations are correct where they appear,
  which is why they differ.
- **This repository's own `Directory.Build.props` still carried XamlQuality's identity** — both
  `PackageProjectUrl` and `RepositoryUrl` pointed at `Bennewitz.Ninja.XamlQuality`, and
  `AssemblyProduct` read `XamlQuality`. The shipped props under `templates/bbpkg/` were always
  correct, tokenised as `REPO_OWNER`/`PKG_ID`; this was the template repo's own copy-paste
  leftover. Found by `verify-release` on its first run, before step 8 could make it permanent.
  Fixed, and the check compares the packed nuspec against `git remote get-url origin` rather than
  a constant, so a rename cannot rot it.
- **The same leftover was in `Directory.Packages.props`, and grep did not find it.** A
  `System.CommandLine` `PackageVersion` nothing here references, under a comment about "the rules
  LIBRARY", beside an xunit comment explaining the ThemeAudit port. Both describe XamlQuality
  without ever writing the word, so a search for `XamlQuality` — which is what confirmed the
  `Directory.Build.props` fix had no twins — came back clean. It was found by reading the file.
  ⚠ **The lesson is about the search, not the file:** copy-paste residue is identifiable by what
  it *describes*, and a string search only finds residue that names its origin. Removed; the two
  genuinely-used entries and the TrxReport version-pin rationale stayed. Verified by deleting
  every `bin/` and `obj/` outside `templates/` and restoring from scratch, because an existing
  `obj/` caches the dependency graph and would have hidden a pin that was doing work.
- **`grep -qv '^[[:space:]]*#'` is not "has a real id" — a blank line is not a comment either.**
  The preflight's `packages.local` guard used the negated form, so a single blank line made it
  print the `⛔ That pattern must NOT match anything in packages.local:` header with nothing
  beneath it — while `packages.local`'s own header promises blank lines are ignored. Code
  contradicting its file's documented contract. Replaced in **both** workflows with the positive
  form `grep -qE '^[[:space:]]*[^#[:space:]]'`, checked against six shapes: comments only,
  comments plus a blank line, blanks only, a bare id, an indented id, and an empty file. Only the
  two blank-line cases changed behaviour. Cosmetic — it never affected what was pushed — but it
  shipped inside the template, so every generated repository would have inherited it.
- **`dotnet test --nologo` is rejected under Microsoft.Testing.Platform.** `global.json` selects
  MTP, which forwards unrecognised switches to the test app; xunit's runner answers
  *"Unknown option '--nologo'"* and the run ends **"Zero tests ran"**. It does exit non-zero
  (5), so CI fails rather than passing vacuously — but the message names the option, not the
  platform, so it reads like a typo rather than a runner difference. Neither workflow passes it.
- **The root packaging tests fail when they should**, checked rather than assumed, one mutation at
  a time with everything else restored in between:
  - id removed from `packages.push` → `Every_packable_project_is_classified` fails, naming
    `Bennewitz.Ninja.Templates`; 12 others still pass.
  - a `*.nupkg` glob put into the **shipped** `gh release create` →
    `The_release_workflow_globs_nothing_and_publishes_what_is_declared` fails for the shipped
    path. That is the mutation that matters: the shipped workflow never runs in this repository,
    so this test is the only thing standing between a glob and somebody's first generated release.
- **⛔ `NUGET_USER` was a secret, and the masking is what made the first release cost six runs.**
  The value was not a nuget.org username. Every preflight therefore returned nuget.org's
  deliberately vague 401 — `No matching trust policy owned by user '***' was found` — with the
  one diagnostic fact redacted by GitHub's secret masking. Six runs went into the policy fields
  instead: Environment, scopes, creator-versus-owner, delete-and-recreate. **The policy was
  correct from the first attempt.** Setting the value to the profile name `JanusMael` turned it
  green immediately.
  ⭐ It is now a repo **variable**, in both workflows and in the shipped runbook, so the error
  names the value. A profile name is public; the secret bought no protection and cost the
  diagnosis. The runbook's "Working the 401" list now *starts* with "read the value out loud".
  ⚠ The trap worth naming: the push step takes `--api-key`, so "this variable is the API key" is
  the natural reading. It is not — the key is an **output** of `NuGet/login`, minted per run. The
  action's documented input is *"Your NuGet account username."*

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

## `scripts/mstest-to-xunit.cs` — outside the plans

Added 2026-09-23 at the maintainer's request. It lives here because this repository defines the
family's test shape (`bbpkg` ships `xunit.v3` on MTP), and it is not specific to any one suite.
OpenForge2k's `plans/00006` (draft) runs it at a pinned commit of this repository.

- **Rewrites the syntax tree** (Roslyn `5.9.0`, pinned in `Directory.Packages.props`, because a
  file-based app under central package management may not carry a `#:package` version — NU1008).
- **Never guesses.** Anything outside its rules is left as written and listed with its line; exit
  code 2 says so. The MSTest leftover then fails the build, which is the second list.
- **`--emit-helpers`** writes `MessageAssert` — derived from `Bennewitz.Ninja.ScopedEditors`' hand
  port, extended — and the `DoNotParallelize` collection definition.
- ✅ **Proven on a real suite before it was committed:** OpenForge2k's `JsonC.Tests` converted with
  nothing unmapped, built with 0 warnings, and ran 73 of 73, the same identities by fully-qualified
  name (the theory rows by count — their display names differ by design).
- ⭐ **The pilot's build found rules the table lacked**, all now in it: `MemberData` must be public
  (xUnit1016); `Equal(0|1, xs.Count)` must be `Empty`/`Single` (xUnit2013), and a filtered count the
  predicate overloads (xUnit2029/2030); a typeof must use the generic `IsAssignableFrom<T>`
  (xUnit2007, CA2263); former setup methods must not stay public (xUnit1013); and a throw-lambda
  binds to xUnit's obsolete `Throws<T>(Func<Task>)` (CS0619), so it is refused.
- ⭐ **2026-09-24, OpenForge2k's second project (`AgentForge.Artifacts.Tests`) found one more:**
  `IsTrue`/`IsFalse(xs.Any(p))` must be `Contains`/`DoesNotContain(xs, p)` (xUnit2012). It is the
  first form that takes MORE arguments than the original, which crashed the rewrite (no separator to
  reuse) until the site learned to make one.
- ⭐ **2026-09-24, the rest of OpenForge2k's suite needed six more**, all now in the table:
  `[Description]` → `[Trait("Description", …)]` (the maintainer's choice); `nameof(x)` is a message;
  named `delta:`/`ignoreCase:`/`message:` arguments become positional — ⚠ and a call that then finds
  no rule is returned WITH its names, since a refused form is left exactly as written; the
  4-argument `StringComparison` + message string forms; `AreEqual`/`AreNotEqual` with a
  `StringComparer` and a message; `CollectionAssert.AreNotEqual` and `AllItemsAreUnique` with a
  message. Each has a `MessageAssert` overload. The new expected output compiled with the emitted
  helpers under warnings-as-errors and xUnit's analyzers before it was accepted; breaking the
  `nameof` rule fails the two fixture tests that see it.
- ⛔⛔ **Every string assertion was converted WEAKER until 2026-09-24.** MSTest's `StringAssert.*` and
  string `Assert.Contains/StartsWith/EndsWith` are ORDINAL; xUnit's default to the CURRENT CULTURE,
  which ignores e.g. a soft hyphen. Measured both sides: `Contains("coop", "co­op")` FAILS in
  MSTest (all five forms) and PASSES in xUnit's default; `DoesNotContain` flips the other way. A
  converted suite stays green precisely because the weakened assertion still passes, so nothing
  showed it. Now every such form goes to an emitted `OrdinalAssert` (or the message helpers, now
  ordinal): its string overloads compare ordinally, and its generic overloads take collections, so
  overload resolution binds as MSTest's did — which also settles MSTest 4's `Assert.Contains` being
  string-or-collection. `IsTrue(s.StartsWith(x))` still maps to xUnit's default, because
  `string.StartsWith(string)` is itself culture-sensitive. Pinned: the helper test fails if any
  emitted string helper calls xUnit without a comparison; canaried both halves.
- ✅ **Found in review of #1, fixed forward:** `IsFalse(x.Contains(y))` →
  `OrdinalAssert.DoesNotContain(y, x)` was WEAKER when `x` brings its own comparer and is not a
  set. ⚠ The review's example was a set, and **a set was never affected**: measured on
  xunit.v3.assert 3.2.2, xUnit asks any `ISet<T>` for itself even through `IEnumerable<T>`
  (`HashSet`, `SortedSet`, `FrozenSet`, `ImmutableHashSet`, all case-insensitive, agree with
  MSTest both ways). A dictionary's `Keys` is the case: case-insensitive keys holding `"a"` fail
  `IsFalse(keys.Contains("A"))` in MSTest and passed after conversion. The generic `OrdinalAssert`
  overloads now decide by the collection's own `Contains`, as `x.Contains(y)` and MSTest 4's
  `Assert.Contains` both do. ⭐ The fixture tests compare TEXT and could not see this;
  `The_emitted_collection_helpers_decide_as_the_original_Contains_did` RUNS the emitted helpers
  against each original, and canaried red on exactly the two `Keys` lines with the old helper.
- ✅ **The same family, fixed 2026-09-25:** MSTest's three collection-membership families disagree,
  measured on MSTest 4.3.3 — `x.Contains(y)` and `Assert.Contains/DoesNotContain` ask the collection,
  `CollectionAssert.Contains/DoesNotContain` compares each element by the default equality — and
  `MessageAssert.Contains<T>` had served both of the last two. Now each family has its own helper:
  the `Assert.*` message forms ask the collection (through `OrdinalAssert`), and every
  `CollectionAssert` form goes to a new `ElementAssert`, which hands xUnit a plain iterator so a set
  cannot answer for itself. ⭐ The semantics test now uses **MSTest as the oracle**: it runs every
  such MSTest form beside its converted call — 108 pairs over case-insensitive dictionary keys, a
  `HashSet`, a `SortedSet` and a plain `List` — and requires `checked 108, disagreed 0`. Canaried:
  with both halves reverted it reports exactly the 10 predicted disagreements (`"A"` over `keys`
  and `sortedSet`). OpenForge2k's 119 converted sites are all `List<string>`/`string[]`, so none
  changed verdict.
- Tests: `tests/Templates.Tests/MstestToXunit`, over `.cs.txt` fixtures whose output was compiled
  and run with the emitted helpers before it was accepted. Canaried both ways — a corrupted expected
  file and a broken rule each fail exactly one test.
- ✅ **`scripts/xunit1051-fix.cs`, the step after (2026-09-25):** xUnit1051 asks every call that
  takes a `CancellationToken` to pass the test's, and xUnit's fixer cannot Fix All, so
  `dotnet format` reports "Unable to fix" and changes nothing. The script injects a per-project
  SARIF log (exact spans: a call passed as an argument ties with its `ArgumentSyntax`, and a chained
  call shares its start with the inner one), appends the token by syntax tree, rebuilds, and names
  each token the compiler rejected — `cancellationToken:`, `ct:`, `token:` in turn, so the compiler
  picks the name. First run, OpenForge2k: 469 sites, 146 named `ct:`, one line each, build clean.
  Tests: `Xunit1051FixTests`, over a fixture with one call of each shape.
- ✅ **`scripts/mstest-areequal-scan.cs`, its companion (2026-09-25):** the one weakening no syntax
  rule can see. MSTest's `AreEqual` compares by `Equals`, so a collection without an override is
  compared by REFERENCE, and xUnit's `Equal` compares elements. The script builds a Roslyn analyzer
  and injects it into the target's build through `CustomAfterMicrosoftCommonTargets`, then lists
  every collection-typed or wholly opaque `AreEqual` (exit 2), or says none (exit 0), or says it
  could not conclude (exit 3: a failed build, or no site seen). First real run, OpenForge2k's MSTest
  tree: **2,935 sites, none to look at** — corroborated by MSTest 4.3.3's own MSTEST0065, which
  flags statically-typed collection `AreEqual` and raised nothing there. ⓘ On that MSTest a plain
  build already finds the static cases; the scan adds the opaque ones, older MSTest versions, and
  one report per solution. Tests: `MstestAreEqualScanTests`, over a warnings-as-errors fixture;
  canaried — ignoring `Equals` overrides fails exactly the classification test.

## `docs/windows-defender-dev-exclusions.md` — outside the plans

Added 2026-09-23 at the maintainer's request: a Windows-only runbook for excluding development
locations from Defender real-time scanning, with PowerShell 7 snippets to add, verify, measure and
remove exclusions, and the Dev Drive alternative. Prompted by GraphVizDotNet's gate pass, whose
cost is dominated by thousands of short-lived process launches. Not packed — the package ships
`templates/**` only.

## Next

All eight steps are done. `Bennewitz.Ninja.Templates 2026.3.921` is published and verified from
the feed, and the repository is marked as a GitHub template. What remains is follow-on work, none
of it blocking:

1. ✅ **`NUGET_USER` carried to `Bennewitz.Ninja.XamlQuality`** on 2026-09-21, and verified by a
   real credential preflight rather than by reading the diff: the run logged the profile name in
   plain text, which as a secret had printed `***`. ⓘ That run also proved a trusted-publishing
   policy is **not branch-scoped** — it matched from a feature branch — so credentials can be
   proven before anything is tagged.
2. ✅ **[`plans/00002`](plans/00002-layerededitors-becomes-two-package-repositories.md) is
   complete** — all nine steps, 2026-09-23. Seven ids published at `2026.3.923` across
   `Bennewitz.Ninja.AppServices` (4) and `Bennewitz.Ninja.ScopedEditors` (3), verified from the
   nuget.org flat-container rather than from a green run, and consumed from a project whose
   `nuget.config` names only nuget.org, so they resolve with no PAT anywhere. The publish landed
   inside the policies' seven-day window, which by nuget.org's own description makes both permanent;
   not re-checked in the UI. Drift is recorded below.

   ⛔ **But `2026.3.923` shipped without `IsTrimmable`, and the gap started in THIS repository** —
   see the drift record. Both package repositories are fixed and ship `2026.3.924` on 2026-09-24.

   ⚠ **`2026.3.924` carries more than the trim fix, each change verified against a control that
   fires.** In the packages:
   - **Inside the two Avalonia packages, the assembly and namespaces become `.AvaloniaUI`**,
     because `Bennewitz.Ninja.AssemblyQuality`'s AQ1004 forbids a namespace segment that shadows a
     referenced assembly's root. The package ids keep `.Avalonia`: renamed at first, they were put
     back the same evening before anything was published, so `.924` is the next version of the same
     packages and nothing is deprecated. ⚠ Every `avares://` URI a host wrote changes with the assembly name,
     and a stale one is not silent: a build error in AXAML, and an exception at layout in a
     single-family `FontFamily`. Every rename since the split, with its reason, is in
     [`docs/layered-editors-renames.md`](docs/layered-editors-renames.md).
   - **No `CancellationToken` has a default** (AQ1001), so `ShareTextAsync`'s `uri` became required
     as well, and `ShareOutcome` gained `Cancelled`.
   - **`AvaloniaDiagnosticsOptions` gains `ConfigureLogger` and `EventListener`**, so a host extends
     the log pipeline and receives events without reaching into the library. The jmui session wires
     ClaudeForge's F12 windows through them.
   - **Both Avalonia packages require Avalonia 12.1.3 or later** (AppServices `d1c5c9c`,
     ScopedEditors `64769ae`), the developer's floor for every project in the family: 12.1.3 fixes UI
     Automation selection never reaching the client on Windows (AvaloniaUI/Avalonia#22151). ⚠ A
     host referencing Avalonia directly below that fails restore with `NU1605`, measured against both
     packed packages. ClaudeForge and AgentForge pin 12.1.0, and jmui has been told.

   In the repositories, guarding what ships:
   - **The original test suites were ported**, MSTest 4 → xunit v3: 92 cases into AppServices and
     219 into ScopedEditors, with class-by-class case-count parity against the originals. The plan's
     scope said "moving the projects and their tests"; step 2 moved only the projects. **17 more**
     followed into AppServices (`cfb5b14`): `DefaultShareService` and the two dialogs had been
     tested only from ClaudeForge, through public API, so the scan that sized the port missed them.
   - **The family's AssemblyQuality rules run as tests** over every shipped assembly in both
     repositories, so the two rules `2026.3.923` broke would now fail CI.
   - **ScopedEditors' CI publishes every assembly trimmed and ROOTED**, and fails if ILLink's
     warnings change in either direction (`5b08b46`). It is the only check that reads compiled XAML.
   - **ScopedEditors lays out text in every bundled font by its documented URI** (`ab442f7`).
   - **ScopedEditors guards its own markup again** (`3ee1593`, `14bd2a9`): the package-side halves
     of the five ClaudeForge scans the move left behind, namely AXAML accessibility and Expander
     names (now XamlQuality's XQ1002 and XQ1001), `LE.*` token integrity, the danger banner and
     severity-glyph sizing. `LE.DangerText` and `LE.DangerBorder` stay declared for hosts, on a list
     that fails once either is deleted or used by the package itself. 12 cases against the
     originals' 10 package-side ones, each proven by a planted defect: the two added are that list's
     self-check and a check that every `LE.*` key the C# resolves is declared. ⓘ No OpenForge2k app
     resolves any `LE.*` key; every mention there is a comment.

   What was learned along the way is in XamlQuality's
   [`docs/avalonia-gotchas.md`](https://github.com/JanusMael/Bennewitz.Ninja.Inspections/blob/main/docs/avalonia-gotchas.md)
   (`d80e53b`): the library side of trim safety, and the four ways an `avares://` URI fails.
3. ✅ **The trim fix is released as `Bennewitz.Ninja.Templates 2026.3.923`**, 2026-09-23, and
   verified from the feed rather than from the green run. `verify-release --published 2026.3.923`
   installed it FROM NUGET.ORG, generated a repository and built, tested and packed it — the tree
   check now requires `src/Directory.Build.props`, so passing it proves the published package carries
   the file. The `.nupkg` downloaded from the flat-container was also read directly:
   `src/Directory.Build.props` with `IsTrimmable` and `EnableTrimAnalyzer` both `true`, and the
   shipped `TrimmableTests`. ⚠ A repository generated from `2026.3.922` or earlier lacks all three
   and has to add `src/Directory.Build.props` by hand.
4. **Stage two belongs to the jmui session**: its `plans/00005` in OpenForge2k, which its
   maintainer widened to ClaudeForge and AgentForge together. As of 2026-09-23 it reports steps 1–8
   done on `feat/scopededitors-stage-two` (`de22e55`, pushed), with the F12 hook adopted. Step 9
   waits for `.924` on nuget.org and gets the flat-container proof once that is published.
   ✅ The markup guards it reported as covering nothing are ported into ScopedEditors, along with a
   fifth, the Expander-name check, which was left behind the same way. See item 2.
5. **`Bennewitz.Ninja.AutoVersioning` is pinned at `2026.3.916`**, raised from `.914` on 2026-09-24
   in both the template and this repository. Upstream calls it a drop-in: its suppressions move to
   `Build.targets`, so a `GenerateAutoVersionedAssemblyInfo` set in a `.csproj` now takes effect.
   Verified by the 18 tests and a passing `verify-release`. ⚠ **Not yet released**: repositories
   generated from `2026.3.923` still get `.914`, so the next template release carries it. Nothing
   else under `templates/` has changed since `.923` except that `IsContinuousIntegration` is gone
   from both `Directory.Build.props` files: `.916` no longer reads it, and nothing else did.

⛔ **This entry used to reserve `plans/00002` for `Bennewitz.Ninja.DiffView`, and that was wrong
twice over.** DiffView stopped being the interesting case when its ThemeAudit tool moved to
XamlQuality: it now packs two ordinary ids and names both explicitly, so it would never exercise
the `packages.local` half the template exists for. ClaudeForge does — eleven packable projects
across three families, one of which must not reach nuget.org. ⚠ DiffView is handled by a session on
another machine and is **not** tracked here; if it ever needs a plan it takes the next free number,
not this one.

✅ The `NUGET_USER` **secret is gone** from this repository and from XamlQuality, AppServices and
ScopedEditors — checked 2026-09-23, all four report no secrets and a `NUGET_USER` variable. This
entry previously said it was still present; that was stale.

## Drift from `plans/00002`

The plan is approved and frozen, so this is where its drift lives. All nine steps are done;
seven ids published at `2026.3.923`, with no bare `Bennewitz.Ninja.ScopedEditors` — still 404 on
nuget.org, and the policy's dotted pattern could not have authorised one.

**Step 1 was "strip and populate", not "generate".** The repositories were created with GitHub's
*Use this template* rather than `dotnet new bbpkg`, so each arrived as a full copy of this one,
carrying a root `Bennewitz.Ninja.Templates.csproj`. They were populated from a freshly generated
`bbpkg` tree rather than stripped by hand, because generation emits a correctly tokenised
`Directory.Build.props` and stripping inherits the identity leftovers this repository has already
paid for twice. ⭐ **`verify-release` caught the inherited leftover on its own** — CI was red on the
initial commit of both repositories, reporting packed metadata pointing at
`Bennewitz.Ninja.Templates`. The check written in step 7 here, firing in a different repository,
before anything was released.

**⛔ The plan is wrong about `LiveLogWindowSink`, and it changed what ships.** The Out section states
it "ships without the window it is named after, and never referenced one". It does reference one:
`LiveLogWindowSink.Emit` calls `LiveLogWindow.EnqueueLog` directly, and the type is `internal`, so it
can neither ship without the window nor cross an assembly boundary to whoever wires it. The split
document's claim that `AppServices.Logging` touches "no window, no Avalonia" is false for that one
file and true for `BucketedRollingFileSink`.

Resolved by **severing the edge** rather than publishing the held-back windows.
`AvaloniaDiagnostics` was the only shipping type reaching into that cluster — six reaches and three
options — so `ToggleLiveLogWindow`, `ToggleEventTailWindow`, the sink wiring, the window
construction and `EnableLiveLogWindow` / `LiveLogWindowTitle` / `EnableEventTailWindow` /
`EventTailWindowTitle` / `EventTailLaunchLabel` are gone from the first release. Host-fed events keep
their rolling **file**, which never needed a window. `LiveLogWindow`, `LiveTailWindow`, `HeaderLink`
and the sink stay in OpenForge2k.

**⚠ `AppServices.Avalonia` references `AppServices`, an edge the split document's graph does not
draw.** `ShowNativeFatalError` wraps `NativeErrorDialog`, a T0 implementation, so the edge already
existed in the code and the graph missed it. Kept rather than dropping the wrapper: T3 → T0 is the
natural direction.

**⛔ The bundled fonts nearly did not travel, and nothing would have failed.**
`ScopedEditors.Avalonia` embeds five JetBrains Mono faces and their OFL licence as
`AvaloniaResource`, ~1.2 MB, and a move carrying only `.cs` and `.axaml` drops them. A bad
`avares://` path **falls back to a default face rather than throwing**, which is the precise failure
bundling exists to prevent. Verified after packing by reading the `!AvaloniaResources` blob out of
the assembly. ⚠ The assembly name changed with the rename, so a consumer's URI must change with it —
`ClaudeForge/App.axaml` still says `avares://LayeredEditors.Avalonia/Assets/Fonts`. Stage two's work.

**⚠ `GenerateDocumentationFile` is on here and was off in OpenForge2k**, so roughly 250 public
members needed comments on arrival. Overrides and `IValueConverter` members took `<inheritdoc/>`;
accessors, singletons and constructors took written summaries. ⭐ The compiler caught three that
would otherwise have shipped **wrong**: a generated `param` tag naming a parameter that does not
exist, a stale `paramref` on `BaseSizeFrom` pointing at a `culture` parameter it never had, and a
styled property documented as an attached one.

⭐ **The packaging guard failed before it passed**, naming all three unclassified ids in AppServices
and refusing them. That is step 6 arriving early because a test demanded it.

**Decided 2026-09-23:** the trusted-publishing expiry clock is treated as **non-binding** —
reactivation is a UI click, while a published version is permanent — so steps 4–8 are done properly
rather than compressed to beat it.


**⛔ `2026.3.923` shipped without `IsTrimmable`, and the root cause is this template.** OpenForge2k set
`IsTrimmable` and `EnableTrimAnalyzer` for every shipped assembly in a **nested**
`src/Directory.Build.props`, deliberately and with measured reasons in its comments. The move copied
project files and never read that one — and `bbpkg` set neither, so the generated repositories had
nothing to fall back on. The plan never mentions trimming, so no step was positioned to notice.

The CODE was never the problem, and that was measured rather than assumed. The Roslyn trim analyser
reports zero warnings under `src/` in both repositories, proven to be running by a planted
`Type.GetType(string)` that reddened with `IL2057`. ⭐ And because **the analyser cannot see compiled
XAML** — XamlIl weaves that IL in after Roslyn — an ILLink trimmed publish of the *published*
packages, every assembly rooted with `TrimmerRootAssembly` and `TrimmerSingleWarn=false`, reported
nothing from them; its only warnings were three distinct `IL2070`s in one `Avalonia.Controls.DataGrid` method.

What was missing is the DECLARATION, and it travels in the package: `IsTrimmable` compiles to
`[AssemblyMetadata("IsTrimmable", "True")]`, and an app publishing with `TrimMode=partial` trims
**only** assemblies carrying it. ClaudeForge publishes exactly that way, so consuming `2026.3.923`
keeps these assemblies whole and outside its trim analysis. Nothing fails — which is how it shipped.

Fixed in all three repositories with one byte-identical `src/Directory.Build.props`, which imports
the root props explicitly because MSBuild applies only the closest one. Verified from **evaluated**
properties rather than files, and proven by mutation:

| Mutation | Result |
|---|---|
| `src/Directory.Build.props` removed, package repo | guard **failed**, naming every assembly |
| trim break planted, **plain** `dotnet build -c Release` | **failed** on `IL2057` — green before the fix |
| repository generated from the fixed template | guard **passed** as generated |
| same, with `src/Directory.Build.props` removed | guard **failed** |

ⓘ **How it would have been caught:** diff the evaluated properties of the source project against the
moved one — `dotnet msbuild <proj> -getProperty:IsTrimmable -getProperty:EnableTrimAnalyzer` — not the
files. It is the second loss of the same shape in this plan: the bundled fonts were the first. A
file-by-file move carries only what it can see.

⚠ `IsAotCompatible` stays off everywhere. It would also switch on the AOT and single-file analysers,
which is a claim about these libraries that nothing has measured.

**Step 5's guards were written as the plan asked, and two of its mutations could not be.** A reference
into the other family cannot be added while none of its ids is published — the restore fails and the
build breaks before any test runs — and an UNUSED `PackageReference` never reaches the assembly
reference table, so the reflection check cannot see it either. Both were re-aimed at references that
genuinely exist. ⭐ The first finding is the csproj-versus-reflection argument demonstrated rather than
asserted: an unused bad `ProjectReference` fails the project check and leaves the reflection check
green, in both repositories.

## `plans/00003` — repository conventions

[`plans/00003`](plans/00003-repository-conventions.md), approved 2026-09-24 (`faa5c4f`) and frozen.

| Step | State | Notes |
|---|---|---|
| 1 · `repository.json` + `repo-conventions.cs` | **done** | `check`, `check --admin`, `check --release`, `apply [--dry-run]`, `--repo`. 22 tests in `RepoConventions/`; 9 planted defects, all caught. Against AppServices live, `check --admin --repo` reported 26 findings, including the empty description, the missing topics and every baseline difference |
| 2 · What `GITHUB_TOKEN` can read | **done** | Measured by a throwaway workflow on a deleted branch, `permissions: contents: read`, which is this repository's default. See below |
| 3 · `docs/repository-conventions.md` + template documents + `verify-release` tree check | **done** | The template ships `AGENTS.md` and a `CLAUDE.md` pointer at the root and in `src/`, `tests/`, `scripts/`, `docs/` and `.github/`, plus `PROGRESS.md`, `.github/copilot-instructions.md` and `.github/repository.json`. `verify-release` requires all 16, failed naming `tests\CLAUDE.md` with only that file removed, and passes with all present. A generated repository's `check` reports only the empty description and 8 markers, nothing structural |
| 4 · CI job + release preflight in the template's workflows | **done** | Shipped `ci.yml` gains a `conventions` job (`contents: read`), and `release.yml` runs `check --release` before login and push, in the preflight dispatch too. `conventions` joins the template's `requiredChecks`. 4 tests in `ConventionsWorkflowTests`; 5 planted defects, all caught. The two `NUGET_USER secret` comments in the shipped `release.yml` now say variable |
| 5 · Adopt it all in this repository, then `apply` | **done** | `AGENTS.md` and a pointer at the root and in all six top-level directories, `.github/repository.json`, the `conventions` CI job and the release preflight; `ConventionsWorkflowTests` covers both repositories' workflows. `apply` ran 2026-09-24 with the maintainer's go-ahead, and `check --admin` then passed against the rulesets as GitHub returns them. The direct push after it went through the admin bypass, with GitHub listing the two rules bypassed. CI green on `e1d2695`, `verify` and `conventions` both |
| 6 · The other seven: script, CI job and `repository.json`, then `apply` | **done** | All seven applied 2026-09-24 with the maintainer's go-ahead; `check --admin --repo` then reported only documentation gaps in each. Pushed direct to `main` in six; DiffView through [DiffView#2](https://github.com/JanusMael/Bennewitz.Ninja.DiffView/pull/2), still open. AutoVersioning's classic branch protection replaced by its ruleset. A throwaway draft PR in each of the six showed every required check reporting, then was closed and its branch deleted: all green except AutoVersioning, see below. Worked from worktrees of `origin/main`, because XamlQuality's and DiffView's clones sit on other sessions' branches |
| 7 · Documentation in AppServices and ScopedEditors | **done** | `AGENTS.md` and a pointer at the root and in every top-level directory, `.github/copilot-instructions.md` and a `PROGRESS.md` in each (AppServices `a0b4706`, ScopedEditors `1f80a3d`). Drafted by one agent per repository from the code, then reviewed: every cited name was grep-checked against the tree and every cited hash against the history. `conventions` is required in both, the release preflight is in both, and `check --release` passes. `check --admin` conforms in both; CI green on every job |
| 8 · XamlQuality and AssemblyQuality reshaped | **done** | XamlQuality `b306e49`: `CLAUDE.md`'s narrative moved verbatim into `AGENTS.md` ahead of the operational rules, and all 15 headings of the two old files are present in the merged one; `CLAUDE.md` is now the pointer. AssemblyQuality `d653a09`: its `CLAUDE.md` was already the pointer; its root `AGENTS.md` gained the conventions link and a "What this repository is" section, and `docs/publishing.md` no longer calls `NUGET_USER`'s store a secret. Directory documents in both, drafted per repository by an agent and checked: every cited name resolves in the tree. `conventions` required and the release preflight added in both; `check --release` passed before the push, since AssemblyQuality plans `2026.3.924` today. `check --admin` conforms in both; CI green on every job |
| 9 · FileServer, AutoVersioning, then DiffView by pull request | **done except DiffView** | FileServer `d9edae6` and AutoVersioning `3e27363`: `AGENTS.md` and a pointer at the root and in every top-level directory (FileServer's `publish/` holds hand-written scripts, so it is documented, not exempted), `.github/copilot-instructions.md`, `conventions` required, and the release preflight, before `Publish all RIDs` in FileServer. Every cited name resolves; AutoVersioning's own build and tests ran clean before the push. `check --admin` conforms in both; CI green on every job. **DiffView is handed to its own session** (maintainer's decision, 2026-09-24): its `main` was already red before any of this, on `ReferenceAuditTests.The_committed_report_equals_a_fresh_run`, and the ruleset now requires those checks, so no DiffView pull request, DiffView#2 included, can merge until that session makes `main` green |
| 10 · Record the outcome | **done, 7 of 8** | `check --admin --repo` from this repository, 2026-09-24: Templates, AppServices, ScopedEditors, XamlQuality, AssemblyQuality, FileServer and AutoVersioning conform. DiffView reports 23 findings, all waiting on DiffView#2 and its documentation |

✅ **Released as `Bennewitz.Ninja.Templates 2026.3.924`**, 2026-09-24, tag `v2026.3.924` at
`6425705`, so `dotnet new bbpkg` now generates everything above. The credential preflight passed
first, including the new `check --release`. Verified from the feed rather than the green run: the
nuget.org flat-container lists `2026.3.924`; the `.nupkg` downloaded from it carries 40 content
entries, none outside `content/bbpkg/`, among them `scripts/repo-conventions.cs` and `AGENTS.md`;
and `verify-release --published 2026.3.924` installed it from nuget.org, generated a repository,
asserted the tree, and built, tested and packed it. It also carries AutoVersioning `2026.3.916` and
drops `IsContinuousIntegration`.

✅ **Released as `Bennewitz.Ninja.Templates 2026.3.925`**, 2026-09-25, tag `v2026.3.925` at
`f743d42`. It carries the shared-lessons rule in the template's root `AGENTS.md` (`4a778c0`), and
everything else under `templates/` since `v2026.3.924`: `repo-conventions.cs` checking the family's
build properties (`caa8c03`), generated repositories requiring trimming with `verify-release`
checking their properties (`8ad9aa8`), a package README under any name (`a8bd8de`), the `nuget`
topic required only where `packages.push` names an id (`fb6961a`), and, by the maintainer's
decision, the second template `bbavalonia` (`f5f7871`). The credential preflight passed first.
Verified from the feed rather than the green run: the nuget.org flat-container lists `2026.3.925`;
the `.nupkg` downloaded from it carries 89 content entries, 40 under `content/bbpkg/` and 49 under
`content/bbavalonia/`, none outside the two, each with its `.template.config/template.json`, and
`content/bbpkg/AGENTS.md` says the lessons go to XamlQuality; `verify-release --published
2026.3.925` installed it from nuget.org, generated a repository, asserted the tree, and built,
tested and packed it; and `dotnet new list`, in a clean hive after installing it from nuget.org,
shows both `bbpkg` and `bbavalonia`.

**Decided 2026-09-24, on XamlQuality's request:** Avalonia and drivable-UI lessons go to
XamlQuality, whose `docs/avalonia-gotchas.md` and `docs/ai-drivable-ui.md` are the one living copy
of each. Family-wide, by the maintainer's choice: `docs/repository-conventions.md` gains "Shared
lessons", and this repository's and the template's root `AGENTS.md` state the rule. It reaches
generated repositories with the next template release after `2026.3.924`; existing family
repositories carry it only once their own sessions add it.

**Decided 2026-09-24** (maintainer, through `choices`):
1. Step 9 proceeds for FileServer and AutoVersioning.
2. DiffView goes to its own session, which owns its red `main` and DiffView#2. No DiffView session runs on this machine, so it has not been told yet.
3. ScopedEditors' README names the package in its Semi warning: done in `3b0c45f`.
4. Stale items found along the way go into each repository's own `PROGRESS.md`: XamlQuality `45c080a` and AssemblyQuality `eea0de4` gained a Follow-ups section; AppServices and ScopedEditors already listed theirs under Next.

⛔ **Step 5 leaked this repository's own documents into the package, and nothing caught it.**
`templates/AGENTS.md` and `templates/CLAUDE.md`, which describe the `templates/` directory, fell
under `Content Include="templates/**/*"` and packed to `content/AGENTS.md` and `content/CLAUDE.md`.
`verify-release` passed, because every check it had asserted what must be present, never what must
not. Found by listing the packed content before a release. Fixed by excluding `templates/*`, which
keeps every template folder beneath it, and by a fourth packed-content check in `verify-release`:
every `content/` entry must lie inside a folder with its own `.template.config/template.json`. That
check failed naming both files before the exclusion, and passes after it. Nothing was released with
the leak: the last release, `2026.3.923`, predates step 5.

**Found during step 9, not fixed here:**
- FileServer: CI builds `samples/SampleWebApp` without `-p:FileServerVersion`, so it restores the default from nuget.org rather than the package it just packed, while two comments say otherwise; the sample's `FileServerVersion` was not bumped after `v2026.9.23`; `publish/Pack-Local.ps1` reads `$env:USERPROFILE`, null on Linux and macOS; `NUGET_USER` is still a secret there, not a variable; the release runs no tests before publishing.
- AutoVersioning: the release pushes a `*.nupkg` glob and runs no tests; `AnalyzerReleases.Shipped.md` is empty across three tagged releases; `Microsoft.Bcl.HashCode` is referenced but not shipped, which `EnumTypeInfo.GetHashCode` would need on netstandard2.0 if anything called it during generation; no test builds a consuming project.

**What a workflow's read-only `GITHUB_TOKEN` reads** (2026-09-24):

| Returned | Not returned |
|---|---|
| description, homepage, topics, default branch | the six merge options: `allow_merge_commit`, `allow_squash_merge`, `allow_rebase_merge`, `allow_auto_merge`, `delete_branch_on_merge`, `allow_update_branch` |
| `has_issues`, `has_wiki`, `has_projects`, `has_discussions` | `security_and_analysis` |
| the rulesets list, the tree, file contents | vulnerability alerts, classic branch protection |

The feature toggles were a finding: the anonymous stand-in did not predict them. `check` now checks
every setting GitHub returns, at either depth, and notes by name the ones it could not read.
**Rulesets, measured in step 5's CI run:** the token reads each ruleset's rules, conditions and
required checks, but not `bypass_actors`. So CI checks every ruleset rule, and only `check --admin`
checks who may bypass.

### Drift from `plans/00003`

**The family baseline lives in the script, not in `repository.json`.** Decision 1 makes
`repository.json` the source of truth for merge options, features, security toggles and rulesets.
As built, those values are in `Baseline` inside `scripts/repo-conventions.cs`, and
`repository.json` holds only what varies from one repository to the next: description, homepage,
topics, required checks, shipped-content paths, and exempt directories with their reasons. Two
reasons:
- A per-repository copy of the baseline could differ from one repository to the next, which is the
  inconsistency this plan exists to remove. The script is byte-identical everywhere because
  decision 10 checks that it is.
- `check` can then compare a repository with no `repository.json` yet against the baseline. Step 1's
  own verification against AppServices depends on that.

**The copy comparison ignores line endings.** Decision 10 says "byte for byte". A Windows checkout
converts to CRLF, so a strict byte comparison would report every Windows copy as drifted.

**The canonical copy is `templates/bbpkg/scripts/repo-conventions.cs`**, and this repository's
`scripts/repo-conventions.cs` is a copy of it. Both are identical as committed.

⭐ **Planting defects found a real flaw, not just a weak test.** `check` compared a ruleset's bypass
against a hard-coded `always` instead of against the baseline `apply` writes, so the two could have
drifted apart silently. The expected bypass is now derived from the baseline, like every other
facet, and the planted `pull_request` mode fails the conforming test.

⛔ **Step 6 broke AutoVersioning's `main` for about 35 minutes.** Its project sits at the repository
root, so the SDK's default glob compiled `scripts/repo-conventions.cs` into it and the build failed
with `CS9314` on the `#!` line (`162fdbf`). The local `check` in the worktree compiled the script
alone and could not see it; the throwaway PR's required `build` did. Fixed in `3100149`, which adds
`scripts\**` to `DefaultItemExcludes` beside `Tests\**`, and `build` is green again. The conventions
document now warns about a root project in its checklist for bringing a repository in.

**The release preflight is not in the other seven yet.** Plan step 6 names the script, the CI job
and `repository.json`. The preflight would refuse every release until that repository's
documentation exists, so it lands with the documentation in steps 7–9, when the `conventions` job
also becomes required.

⛔ **The marker check matched prose about markers.** It looked for `<!-- bbpkg:` anywhere in a
line, so this repository's own check failed on `docs/repository-conventions.md`,
`templates/AGENTS.md` and `plans/00003`, where the syntax appears in backticks. A marker is now a
line that begins with it, which is how the template writes every one. Found by adopting the
convention here, before any other repository ran it.

⚠ **An agent never sees a marker in documentation its tool loaded automatically.** Working inside
`templates/bbpkg/`, this session's tool loaded the template's own `CLAUDE.md` and, through it,
`AGENTS.md` as instructions for this repository, with the HTML-comment markers stripped out. So a
marker can only be found by reading the file or by running `check`, and `templates/AGENTS.md` now
says that the documents beneath it describe a generated repository.

⚠ **A file-based app compiles trimmed.** A `JsonArray` built from a collection expression, or passed
a `JsonObject` to `Add`, binds to the generic `Add<T>` and fails the build with `IL2026`/`IL3050`.
The script builds its arrays through the constructor.

## `plans/00004` — standard build properties

[`plans/00004`](plans/00004-standard-build-properties.md), approved 2026-09-24 (`6616e4b`) and frozen.

| Step | State | Notes |
|---|---|---|
| 1 · Remove `IsContinuousIntegration`, AutoVersioning `2026.3.916` | **done except DiffView's merge** | AppServices `a50294e`, ScopedEditors `3f44048`, XamlQuality `8b9e4c3`, AssemblyQuality `ad75b45`, FileServer `1f16364`, direct to `main`; DiffView by [DiffView#3](https://github.com/JanusMael/Bennewitz.Ninja.DiffView/pull/3). In each: the untouched tree, evaluated with `GITHUB_ACTIONS=true` after a restore, gave `IsContinuousIntegration = 'true'`; with the change every project in the solution evaluates it empty; the repository's own CI build and tests passed locally before the push, and CI is green on every job after it |
| 2 · Measure the baseline across the family | **done** | See the measurement below. Three repositories missed parts of the baseline, and the maintainer decided each |
| 3 · The property check in `repo-conventions.cs` | **done** | Restore, evaluation with `GITHUB_ACTIONS=true`, roles (with *other* for non-packable libraries), the baseline, package rules for libraries and tools, `props` exemptions with reasons and a note for a stale one, the trimming stage, `--offline`; documented in `docs/repository-conventions.md`. 15 tests in `PropsTests` over three real repositories built in a temp directory; 8 planted defects, each caught, each also compiled and run to prove the catch was not a build failure. Against real repositories before any test: AppServices conforms, AutoVersioning shows step 2's gaps |
| 4 · The template requires trimming; `verify-release` runs `check --offline` | **done** | The template's `repository.json` requires trimming; `ConventionsWorkflowTests.The_template_requires_trimming` pins it. `verify-release` gains step 9: the generated repository's OWN script copy, `check --offline`, with a guard that the properties were evaluated at all, allowing only the empty description and the markers. Passes as shipped (8 markers). Planted in the template: dropping `Nullable` or `IsTrimmable` was caught earlier, by the generated build and its `TrimmableTests`; re-adding `IsContinuousIntegration` and changing `AssemblyCompany`, which build and test cleanly, were each caught by step 9 alone |
| 5 · This repository adopts the new script | **done** | Done by step 3: `scripts/repo-conventions.cs` is the new script, and this repository's CI ran the property check green on `caa8c03`, the packaging project as role *template* |
| 6 · Each family repository adopts the script, with step 2's decisions | **done except DiffView's merges** | Direct to `main`, the script copy first and then `a8bd8de`'s again: AppServices `ef0f573` `cf30a9b`, ScopedEditors `c0c331c` `a01aa2a`, XamlQuality `de4a4b0` `006745b`, AssemblyQuality `d147dba` `b9711a3`. FileServer `f10c674` (central package management, its tests gain AutoVersioning; version-neutral, build, tests and sample build passed), `762cdd9` (both Dockerfiles copy `Directory.Packages.props` before the restore), `514fec0`. AutoVersioning `f093331` (a root `Directory.Build.props` and central versions; only its use of itself exempted, with the reason), `2631253`. CI is green on every job of every final commit, and `check --repo` from here reports no FAIL and no drift for all six. DiffView: the script copy is on [DiffView#2](https://github.com/JanusMael/Bennewitz.Ninja.DiffView/pull/2); [DiffView#3](https://github.com/JanusMael/Bennewitz.Ninja.DiffView/pull/3), with `main` merged in, builds cleanly and has no property FAIL, only the two trimming notes |
| 7 · Trimming, repository by repository | **done; DiffView by pull request** | Required, each proved both ways: the check conforms, and a planted loss of the property fails the check and the repository's mark test. AppServices `8e07ab7` and ScopedEditors `3a20d6a`: already marked, `"trimming": "required"`. AssemblyQuality `e9f63e1`: marked through `IsAotCompatible`, now required, and `TrimmableTests` added. DiffView: both libraries marked in [DiffView#5](https://github.com/JanusMael/Bennewitz.Ninja.DiffView/pull/5), analyzer clean, its existing `TrimMode=link` trim check the ILLink pass, a mark test added; required in [DiffView#2](https://github.com/JanusMael/Bennewitz.Ninja.DiffView/pull/2). **Stay at the note:** XamlQuality and FileServer, for the reasons below |
| 8 · The record | **written; closes with DiffView's merges** | Required: Templates, AppServices, ScopedEditors, AssemblyQuality, and DiffView once #2 merges. At the note: XamlQuality's library (22 analyzer findings; 18 of them reflect over the consumer's compiled types, and fixing them honestly means `[RequiresUnreferencedCode]` on the public rule API, which the plan leaves to XamlQuality) and FileServer's library (see the drift below). Not held: AutoVersioning, an analyzer; apps and tools, by decision 8 |

**Step 2's measurement, 2026-09-24.** Every project in the eight repositories' solutions (or, with
none, every csproj outside `templates/`), from each `origin/main`, restored and evaluated in Release
with `GITHUB_ACTIONS=true`, against decision 4's baseline and, for libraries and tools, decision 5's
package properties:

| Repository | Projects that miss something | What they miss |
|---|---|---|
| Templates, AppServices, ScopedEditors, XamlQuality, AssemblyQuality | none | — |
| FileServer | all three | central package management; `Bennewitz.Ninja.FileServer.Tests` also references no AutoVersioning |
| AutoVersioning | the analyzer and its tests | `ImplicitUsings`, `TreatWarningsAsErrors`, central package management, `AssemblyCompany`, and AutoVersioning applied to itself |
| DiffView | all seven | AutoVersioning `2026.3.819` and `IsContinuousIntegration`, which DiffView#3 fixes; `PackageReadmeFile` on its three packable projects |

Trimming notes (decision 7): XamlQuality's library, FileServer's library and both DiffView libraries
are not yet trimmable; AppServices', ScopedEditors' and AssemblyQuality's are.

⚠ **The measurement's first run was wrong everywhere, and was caught as implausible.** It reported
every project below AutoVersioning `2026.3.916`, this repository included, because under central
package management a `PackageReference` item carries no version; the version is on the
`PackageVersion` item. Step 3's check must read it the same way: `VersionOverride`, then the
reference's own `Version` (FileServer, without central management), then the `PackageVersion` item.

⚠ **Decision 3's roles have a gap:** AssemblyQuality's test fixtures `Absent` and `Orphan` are
libraries that are neither packable nor tests. They meet the baseline, and step 3 gives them the
baseline's role without the package properties.

**Decided 2026-09-24 on step 2's gaps** (maintainer, through `choices`), carried out in step 6:
1. **FileServer migrates** to central package management, and its tests gain AutoVersioning, in the
   commit that adopts the new check; its build and tests prove the migration.
2. **AutoVersioning fixes its real gaps**: `TreatWarningsAsErrors`, central versions,
   `AssemblyCompany` and `ImplicitUsings` where they build cleanly. Only "AutoVersioning applied to
   itself" is exempted, with its reason: a package cannot generate its own build.
3. **DiffView packs its README** into its three packable projects, by pull request alongside
   DiffView#3; both merge once DiffView's session makes `main` green.

### Drift from `plans/00004`

⛔ **FileServer's library is analyzer-clean when marked, and marking it breaks its consumers.** With
its three `MapGet(string, Delegate)` calls moved to `RequestDelegate` handlers, the trim analyzer and
an ILLink pass both report nothing in it. But a consumer publishing with `TrimMode=partial` then
trims the library, keeps each compiled Razor view type without its constructor, and every listing
and Markdown page answers 500 (`Views_FileServer_Directory does not have a default constructor`):
measured on a self-contained trimmed publish, against 200s from the same publish unmarked. MVC
creates views by reflection no analysis sees. An embedded `ILLink.Descriptors.xml` preserving
`AspNetCoreGeneratedDocument` restored all three pages, but holding that would need a trimmed-publish
job in FileServer's CI, and the library pulls in MVC, which Microsoft does not support trimming, so
the mark would buy consumers little. **Not marked**; the experiment was discarded. The same blind
spot as compiled XAML: a Razor library's trim analysis is a trimmed publish that serves a page, not
a build.

**Removing `EnableTrimAnalyzer` is not a defect, so it cannot be planted.** The SDK turns the
analyzer on whenever `IsTrimmable` is true: with the line removed it still evaluates `true`, and the
first planted defect of that shape survived for that reason. Planting it as `false` is caught.

⛔ **None of the family commits for `plans/00004` updated their repository's `PROGRESS.md`**, 22
commits across six repositories, although each repository's `AGENTS.md` asks for it in the same
change. Found in step 7. AppServices `d9f3312`, ScopedEditors `05ed128`, AutoVersioning `59eecc5`
and FileServer `5d04f8a` record them after the fact, FileServer's with why its library is not marked
trimmable; AssemblyQuality's step 7 commit records its own. XamlQuality's list takes only what a
consumer can see, so its commits stay out, and FileServer's `CHANGELOG.md` takes only what a user
would notice, which none of these is: the Docker break arrived and left between two releases.

⛔ **Step 2 misreported DiffView's READMEs, and decision 5's rule changes because of it.** The
measurement flagged `PackageReadmeFile` on all three of DiffView's packable projects, and this was
reported as "no README". In fact `DiffView.Avalonia` and `DiffView.Core` deliberately pack
`docs/hosting-diffview.md`, a hosting guide, as their README; the rule compared the value with
`README.md` literally, so a different name read as a miss. Only the `ThemeAudit` tool packs none.
Found in step 6 when a pack showed both. **Decided 2026-09-24** (maintainer): the rule is that a
library or tool packs a README under any name, since its purpose is that nuget.org shows one;
`dotnet pack` already fails with `NU5039` when the named file is missing. `PropsTests` pins both
sides, and a planted defect in the rule was caught.

**Decision 3 is moot for DiffView: it has no project left without a README.** Its `ThemeAudit` tool,
the one packable project that packed none, moved to XamlQuality in DiffView#1, merged by DiffView's
session. There, as `XamlQuality.ThemeAudit`, it evaluates `PackageReadmeFile` to `README.md`, and
XamlQuality's `check --offline` at `006745b` reports no property FAIL. DiffView gets no README change.

**DiffView#3 conflicted with the move, and was brought up to date by a merge, not a rebase.** Both
touched the Tooling group in `Directory.Packages.props`. The resolution keeps `main`'s entries with
AutoVersioning at `2026.3.916` and drops `System.CommandLine`, which only `ThemeAudit` used
(`603896c`). A force-push of rebased branches was refused by this session's permission check, so both
DiffView pull requests carry a merge commit from `main` instead.

**FileServer's `Dockerfile.bundle` has the restore fix but no build proves it.** CI builds only the
main `Dockerfile`, which failed with `NU1015` until `762cdd9`; the bundle's Dockerfile got the same
line, unbuilt, since Docker's engine was not running on this machine.


⛔ **The plan's premise about AutoVersioning is false, and it came from this session.** `00004`'s
"What the family looks like today" says AutoVersioning's package `Build.props` in `2026.3.819` and
`2026.3.914` sets `IsContinuousIntegration`, and decision 10 builds on it. It does not: the line is
inside an XML comment in that file, setup advice for consumers, and a text search matched it. The
package only declared the property compiler-visible. So each repository's own line was the only
thing setting it, and step 1's removal alone removed it; the `2026.3.916` pins it added are harmless
and now required by the baseline. Found in step 3, when a test built on the premise failed.

**Step 3's restore is kept but not proven by a test.** Decision 1's reason was exactly that false
premise. A restore is still needed in general, since any package's build props could set a
property the rules read, but no fixture here has such a package, so no test fails without it.

⛔ **A test that asserts only absence passes on a script that never ran.** A planted defect written
as `if (false)` failed to compile under warnings-as-errors, and
`A_conforming_repository_has_no_property_finding`, which only asserted no `FAIL props` line, still
passed. It now requires the "projects evaluated" line too. Each planted defect was then also
compiled and run on its own, so a catch cannot come from a build failure.


**FileServer pinned AutoVersioning `2026.2.522`, not `2026.3.914`.** The plan's table and step 1 say
`.914`; the pin was in two csproj files, since FileServer has no `Directory.Packages.props`. A survey
script that errored on FileServer printed the previous repository's value. Both files now pin
`2026.3.916`, and FileServer's build, 159 tests and sample build passed on it.

⛔ **Verifying the removal locally would have proved nothing without `GITHUB_ACTIONS=true`.** The
property is `$(GITHUB_ACTIONS)`, empty on a developer machine, so it evaluates empty whether or not
anything sets it. Each repository was first evaluated untouched with the variable set, and gave
`'true'`; only then did "empty after the change" mean something. Step 2's measurement and step 3's
check must set it the same way, or neither can fail on this property.

**DiffView's local run has 33 failing tests, identical with and without the change**, compared
test by test on the same worktree: rendering snapshots on this machine and the theme-audit report
test that also fails its CI. None is caused by step 1. DiffView#3 merges when its `main` is green,
which is DiffView's session's work. Its build also fetches reference checkouts during the build
(`scripts/fetch-reference.cs`), which failed once in a fresh worktree and succeeded when run directly.

## `plans/00005` — app templates

[`plans/00005`](plans/00005-app-templates.md), approved 2026-09-24 (`886f107`) and frozen.

**Decided 2026-09-25** (maintainer, asked during step 2): **the `nuget` topic is required only
where `packages.push` names an id**; `csharp` and `dotnet` stay required everywhere. A generated app
failed the conventions on `"topics" lacks "nuget"` although it publishes no package, and every app,
site or API would have. `repo-conventions.cs` changes with `docs/repository-conventions.md`, and
every family repository takes the new copy after it merges.

**Decided 2026-09-25** (maintainer): **`2026.3.925` ships `bbavalonia`**, ahead of steps 5 and 6,
since the merge landed an hour before the scheduled release. `verify-release` covering it and
the package README describing it follow in a later release.

The family took the new script copy the same morning, each with its `PROGRESS.md` entry:
AppServices `118dacc`, ScopedEditors `c1a26bd`, AssemblyQuality `bdd4bef`, AutoVersioning
`ca38307`, FileServer `7205fc5`, XamlQuality `35a94bf` (script only: its list takes what a
consumer sees), and DiffView on #2 (`e5f1203`).

| Step | State | Notes |
|---|---|---|
| 1 · Wait for `00004`'s script | **done** | `00004` step 5 was done on 2026-09-24 |
| 2 · `bbavalonia` from ClaudeForge | **done**, merged in #6 as `fb6961a`, `f5f7871`, `4e63eca`, admin-merged green by the maintainer's word | Generated from the PACKED template as `Notebook`: builds with warnings as errors, 14 of 14 tests pass, `check --offline` fails only on the empty description and the markers, a trimmed publish for linux-x64 matches the 7-warning baseline, and a trimmed single-file win-x64 binary run with `--smoke` exits 0 in about 2 s. All six runtime identifiers publish from one Windows machine with the same 7 warnings, all in `Avalonia.DesignerSupport`. 9 planted defects, each caught: an unnamed button (XQ1002) or Expander (XQ1001), the Semi theme removed, the name box unbound, the view model's trim dropped, the app made packable, a defaulted `CancellationToken` (AQ1001), and the baseline changed in each direction |
| 3 · `bbweb` from bleedink.com and FileServer | **done**, merged in #7 | Generated from the PACKED template as `Gallery`, both variants: build with warnings as errors, 10 of 10 tests without `--blazor` and 13 of 13 with it, `check --offline` failing only on the description and the markers. A self-contained single-file win-x64 publish with `-p:Version=2026.3.920` serves `/healthz` (`ok`), `/version` (`2026.3.920`, its build stamp, the commit), the home page (with the counter prerendered when `--blazor`), the stylesheet and a 404. The container image of each variant, built on Docker 29.8.0 as CI's `container` job builds it, serves `/healthz`, the home page and `/version` (`1.0.0`, the build stamp, the commit passed in) and runs as uid 1654, `app`. 9 planted defects, each caught: `/healthz` changed, `/version` reading the informational version, the exception handler removed, static assets unmapped, status pages not re-executed, the site packable, and with `--blazor` the hub unmapped, the circuit script dropped and the component rendered static |
| 4 · `bbapi` from `bbweb` | **done**, merged in #8 | Generated from the PACKED template as `Catalog`: builds with warnings as errors and the AOT analyser, 11 of 11 tests pass, `check --offline` fails only on the description and the markers. A native linux-x64 build, compiled in the SDK's AOT image, runs in a 35.3 MB chiseled image as `app` and answers `/healthz`, `/version`, a greeting and the OpenAPI document. A native win-x64 build, 11.8 MB, answers the same in about 0.75 s from process start, at 23.5 MB working set, with 400 and 404 as problem details. 9 planted defects, each caught, the last, a type missing from the JSON context, by the tests and by the native container |
| 5 · `verify-release` covers every template | **done**, merged in #9 as `f6b8cf4`, admin-merged green by the maintainer's word | Five cases, each generated from the packed package into a template hive of the run's own: `bbpkg` (12 tests, packed and guarded), `bbavalonia` (15), `bbweb` (10), `bbweb --blazor` and `bbapi`, each building with warnings as errors, passing its tests and `check --offline` with only the description and 8 markers. The package must hold exactly the templates on disk, and those exactly the script's cases. 3 planted defects, each caught and naming only its own case: a type error in `Counter.razor` (only `bbweb --blazor` fails; the plain variant excludes it), `bbapi`'s `Dockerfile` deleted, and a fifth template with no case. The global registration's timestamp is unchanged by the run |
| 6 · Document the templates, and release | **done**: `2026.3.928` published 2026-09-28 from `af909cf` and verified from the feed | The preflight logged in; the tag's run published; nuget.org lists `2026.3.928`; installed from nuget.org, `dotnet new list` shows all four templates, and `verify-release --published 2026.3.928` passed, all five generated repositories building, passing their tests and publishing for win-x64. `plans/00005` is complete. On the same day the maintainer had the repository's GitHub template flag turned off (`isTemplate: false`), since that route copied the template factory rather than generating anything; plans/00001's "shipped two ways" is drift, recorded here. The package README, its nuget.org description and tags, and `docs/repository-conventions.md` describe all four templates. The README no longer offers `gh repo create --template`: this repository is the GitHub template, so that copies the packaging project and every template rather than generating a `bbpkg` repository. The release is `v2026.3.926`, since `2026.3.925` is taken and one release per calendar day stands |

### Drift from `plans/00005`

**The plan's premise about ClaudeForge's tests is wrong: they are xUnit v3, not MSTest.** Decision 4
contrasts the two; it changes nothing, since the template follows AppServices and ScopedEditors,
which run headless on xUnit v3 through `HeadlessUnitTestSession` exactly as ClaudeForge does.

**ClaudeForge holds no trim-warning baseline.** It gates on zero warnings through warnings as
errors. Decision 5 asks for a baseline "as ScopedEditors' `trimcheck/` does", so the template follows
ScopedEditors: `ILLinkTreatWarningsAsErrors` off, the compiler's warnings still errors, and
`scripts/check-trim-warnings.cs` comparing the set in both directions, in CI's `trim` job and for
every platform in the release. The baseline is the app's own publish, not a separate `trimcheck/`
project, since an app's publish is the thing that ships.

**ClaudeForge takes Semi through ScopedEditors' theme bundle**, not a `SemiTheme` of its own, and
references a Fluent theme it never uses. The template references `Semi.Avalonia` directly, with its
locale fixed to `en-US`, and nothing else.

**ClaudeForge names controls with `AutomationProperties.Name` only.** Decision 4 asks for every
interactive control "named for automation"; the template sets `AutomationId` as well, and its window
test finds each control by it.

**`bbavalonia` has no pack job and no `assert-packages.cs`.** Decision 2 asks for the two-list guard
with an empty `packages.push`; the guard that runs on every commit, `PackagingTests`, is there and
allows zero packable projects. The pack job's `assert-packages.cs` fails on a pack that produces
nothing, by design, so an app cannot run it. Adding a library brings both over, as the generated
`AGENTS.md` says.

**`--smoke` is not in the plan.** Step 2's "runs and exits cleanly" needs a GUI app to exit on its
own; `--smoke` opens the main window and exits 0 once it has opened.

**The generated documents reuse the `<!-- bbpkg:` marker.** The conventions check knows one marker;
a marker per template would change the script in every family repository for a name.

**Only the executable is archived for release.** A win-x64 publish also leaves the native
libraries' `.pdb` files beside it, about 100 MB, which ClaudeForge strips with a target of its own.

⛔ **Every template's local builds recorded no version, and `bbavalonia` shipped showing "Built with
♥" as its version.** Found in step 3, when `/version` answered `{"version":"Built with ♥"}` and a test
asserting only "not empty" passed on it. Two defects, one inherited from `bbpkg`:
- AutoVersioning puts `Built with ♥ <commit>` in `AssemblyInformationalVersion`, by design; the
  release version is `[AssemblyMetadata("PublicVersion")]`. `bbavalonia`'s About panel read the
  informational version, and so does bleedink.com's `/version`.
- `Directory.Build.props` defaulted `PublicVersion` to `$(Version)` before the SDK gives `Version`
  its default, so it was empty on every local build, though `bbpkg`'s comment said "reads 1.0.0".
  Measured: `""` from the props file, `1.0.0` from a `Directory.Build.targets`, and `-p:Version=`
  wins in both. A release, which passes `-p:Version=` globally, was never affected.

All three templates now default `PublicVersion` in `Directory.Build.targets`, on `verify-release`'s
required list; `bbavalonia` and `bbweb` read the metadata; and each has a test asserting the version's
SHAPE, which a planted return to the informational version fails. The repositories generated from
`bbpkg` before this carry the early default, harmless to their packages; bleedink.com's `/version` is
that site's own to fix.

**bleedink.com's Blazor is the pre-.NET 8 model**: `AddServerSideBlazor`, `MapBlazorHub`,
`blazor.server.js` and the Component Tag Helper, and it embeds no interactive component, only a
router. Decision 6 says "as bleedink.com does", so `bbweb --blazor` uses the same model and adds one
component, a counter, tested over HTTP as bleedink.com tests its circuit: prerendered markup, the
script served, the hub negotiating.

**bleedink.com is hosted by IIS; `bbweb` is Kestrel.** Left out: its garbage-collector settings for a
CPU-capped pool, `/diag`, `web.config`, ReadyToRun for cold starts after idle shutdown, and the
publish checks for in-process hosting. Added, since IIS no longer terminates TLS in front of it:
forwarded headers, trusting loopback proxies only.

**The container image was proven locally, not by a generated repository's CI.** Docker Desktop's
engine was first down and WSL has none; the maintainer brought it up, and both variants' images
built and served, as the step row says. CI's `container` job itself first runs in a generated
repository. The image leaves out FileServer's `HEALTHCHECK`, which needs curl installed in the runtime
image and passed only because `curl -f` follows a redirect as success; CI requests `/healthz` from
outside instead.

**FileServer's release passes `-p:ReadyToRun=true`**, which is not the SDK's property
(`PublishReadyToRun`), so its binaries are probably not ReadyToRun. FileServer's own to fix; found by
this step's survey.

⛔ **A type missing from `bbapi`'s JSON context passes the build and every JIT test, and the native
API answers it with a 500.** Measured: `Greeting` left out of `ApiJson` built clean with warnings
as errors and the AOT analyser, and all 11 tests passed, because the test host falls back to
reflection. The first drafts of the generated documents said the analyser "catches most misses";
it caught none. The test project now sets `JsonSerializerIsReflectionEnabledByDefault` to `false`,
as the native binary has it: the same defect then fails two tests, and still 500s in the native
container, which CI's `container` job requests. The documents say what was measured.

**The release's native matrix is unexercised.** Native AOT has no cross-OS publish, so each runtime
identifier builds on a runner of its operating system: `ubuntu-24.04-arm` for linux-arm64, and
win-arm64 and osx-x64 cross-architecture on `windows-latest` and `macos-latest`. Proven here:
linux-x64 in Docker and win-x64 natively. On this machine the Windows publish needed the Visual
Studio Installer folder on `PATH`: the AOT compiler finds MSVC through `vswhere.exe` by its full
path, but the install's `VsDevCmd.bat` then calls `vswhere.exe` by name from the Installer folder,
which fails where `NoDefaultCurrentDirectoryInExePath` is set, as it is in an agent shell. Found in
step 5; GitHub's Windows runners carry it on `PATH`.

**`bbapi`'s image is chiseled**: no shell, so CI reads the image's configured user rather than
running `id` in the container, and no ICU, so the project sets `InvariantGlobalization`.

**`verify-release` publishes one runtime identifier of each app: the machine's own**, not a fixed
one, since native AOT has no cross-OS publish. `bbavalonia` publishes trimmed and is held to its
warning baseline by its own `check-trim-warnings.cs`; `bbweb` in both variants publishes
self-contained single-file, and `bbapi` natively, and each is started from its publish folder and
must answer `/healthz` with `ok` and `/version` with the version passed to the publish. Step 5 first
merged without it (#9), on the claim that the native toolchain would be needed on every machine;
this machine had it, and the maintainer asked for decision 9 as written. `bbavalonia` is not run: a
GUI app needs a display, and its `--smoke` run stays in the manual checks. Measured on win-x64: 7
trim warnings, as the baseline; the two sites answer in about 0.7 s and the native API in 0.6 s.

⛔ **`verify-release` emptied the maintainer's global template registration.** It uninstalled
`Bennewitz.Ninja.Templates` before and after every run, and so did the step 2–4 scratch scripts; on
2026-09-25 `dotnet new list bb` found nothing until the maintainer reinstalled `2026.3.925`. Step 5
installs into `--debug:custom-hive` under the run's scratch directory and uninstalls nothing.


⛔ **The first push of step 2 failed this repository's own `conventions` job.** `repository.json`
listed only `templates/bbpkg` as shipped content, so the check read `bbavalonia`'s markers as this
repository's unfinished documents and evaluated its projects as this repository's. The step's
verification ran the GENERATED repository's check, never this one's. `templates/bbavalonia` is now
content, and `TemplateCopiesTests.Every_template_is_declared_shipped_content` fails for the next
template that is not, which a planted removal confirmed.

## `plans/00006` — solution-wide friend grants

[`plans/00006`](plans/00006-solution-friend-grants.md), approved 2026-09-28 (`e19966c`) and frozen.
Asked for by AssemblyQuality on the maintainer's decision of that day. The draft went through three
review rounds (two by this session, one by an independent reviewer) and AssemblyQuality's own review
before approval. The maintainer confirmed directly the decisions first relayed by AssemblyQuality:
the cross-repository guard runs at the provider's release (decision 9), and a public assembly that
shares an unprefixed family name receiving its grants is an accepted, documented risk (decision 8).
The rollout's escape hatch is the per-project opt-out, with no repository-wide switch (decision 10).

| Step | State | Notes |
|---|---|---|
| 1 · The convention and the checker, in one change | **done** on `feat/friend-grants-convention` | `docs/repository-conventions.md` gains "Friend grants"; `repo-conventions.cs` gains `grants` and the checks, sharing one restore and evaluation with the property rules (`Projects.Evaluate`), now also run by `check --release`. `GrantsTests`, 26 cases since the review's fixes, over three real fixture repositories built on the shipped `Directory.Build.targets`: every one of the plan's fixtures, plus `grants` refusing `--repo`, string literals, the multi-targeted, listed, split and aliased grants, the `False` opt-out and the prefix note. 105 of 105 tests pass. ⛔ The pull request's first `conventions` run failed on this repository's own `GrantsTests.cs`: the source scan matched the attribute's text inside string literals. It now matches only at the start of a line, where an assembly attribute stands (`1c3d127`) |
| 2 · Every template links and ships both files | **done**, same branch | Each template's `Directory.Build.targets` links both files; each ships them, written by `grants --root templates/<t>` (`<Stem>`, `<Stem>.Tests`); both are on `verify-release`'s required list; each template's `AGENTS.md` names `grants`. Planted in a generated `bbpkg`: an `internal` member called from a planted test compiles and passes (13 of 13); with the library opted out, `CS0122` |
| 3 · `verify-release` covers the grant list | **done**, same branch | `verify-release` passes, every generated repository's offline check clean of grant findings; a stale list fails its check (`GrantsTests`) |
| 4 · This repository adopts it | **done**, same branch | A root `Directory.Build.targets`, the two files (`Templates.Tests` alone: the packaging project is a template), and the script copy. `check --offline` here conforms |
| 5 · `bbavalonia` calls BNAQ1005 | **done**, same branch | AssemblyQuality 2026.3.929; the allowed names are the two grant files' own. A generated `bbavalonia` passes 17 of 17; with a grant to `Nobody.Ships.This` planted in its source, BNAQ1005 fails, and so does its own conventions check |

✅ **Released as `Bennewitz.Ninja.Templates 2026.3.1001`**, 2026-10-01, tag `v2026.3.1001` at
`fb51c6c`, on the maintainer's word. It carries the friend grants with the review's fixes (#20, #22)
and `bbavalonia` on AssemblyQuality 2026.3.929 and XamlQuality 2026.3.928 (#13, #15, #17). Before the
tag: `verify-release` passed locally on `fb51c6c`, CI on `main` was green, and the blank-version
preflight logged in and pushed nothing. Verified from the feed rather than the green run: the
nuget.org flat-container lists `2026.3.1001`; the `.nupkg` downloaded from it carries 198 content
entries across the four templates, each with `AssemblyInfo.InternalsVisibleTo.cs` and its `External`
twin, and `bbavalonia` pins AssemblyQuality 2026.3.929 and XamlQuality 2026.3.928; and
`verify-release --published 2026.3.1001` installed it from nuget.org, and all five generated
repositories built, passed their tests and published for win-x64.

### Drift from `plans/00006`

**Steps 1–5 land in one pull request, not one each.** The checker the plan changes in step 1 is the
canonical copy, and this repository's `conventions` job fails when its own copy differs from it, so
step 4's copy must land with it; every generated repository runs the new checker inside
`verify-release`, so step 2's files must too. Step 5 joined once AssemblyQuality 2026.3.929 shipped
on the same day. Each step is its own commit in that request.

**The open question is settled: a file-based app does import `Directory.Build.targets`**, measured on
SDK 10.0.401 by a probe that compiled a linked file into `dotnet run --file app.cs`. It sets
`FileBasedProgram=true`, which the link now excludes, as decision 5 says.

**`check --release` did not evaluate projects before**, so the grant checks brought the evaluation to
the release preflight, as decision 3 requires; it runs only the grant checks there, not the property
rules.

⛔ **A review after the merge (2026-09-30, an independent reviewer and a trial adoption in AppServices)
found the checker wrong in real repositories, before any release shipped it.** Fixed on
`fix/friend-grants-review`, each case now a test that fails against the merged checker:
- **A multi-targeted project's own grants were invisible.** Its outer evaluation holds none of its own
  Compile items, nor an item conditioned on a framework, so `[assembly: InternalsVisibleTo]` in its
  source passed. TrueCourseCalculator's `Geo.Core` declares five; the check reported none. Each
  framework is now evaluated and the items combined; the trial now reports them.
- **The source scan missed real grants and failed on text.** A grant after another attribute in one
  section, one split across lines and one through a `using` alias all compiled and went unseen; a raw
  string whose line begins with the attribute's text failed, as CodeQuality's tests have one. The scan
  now reads code with comments and every literal stripped, the whole `[assembly: …]` section, aliases
  included; the CodeQuality trial reports only its real grant.
- **The opt-out was compared case-sensitively**, unlike MSBuild, so `False` unlinked the files and then
  failed the check. Now case-insensitive. The link is also limited to C# projects.
- **Decision 4's premise, "family assemblies are unprefixed", is false** for AutoVersioning, FileServer,
  Geo.Core, GeoHash and TrueCourseCalculator, so failing every prefixed External name would reject a
  correct grant to them. **Decided 2026-10-01** (maintainer): a prefixed name is a NOTE, never a
  failure, and the note shows how to silence it, with the exact line: end the grant's line with
  `// repo-conventions: assembly name`. A wrong name still fails the consumer's build with `CS0122`.
- **Duplicate internal types now collide (`CS0436`)**, reproduced with two multi-targeted projects each
  carrying a `NotNullWhenAttribute` polyfill. The checker cannot see it; the docs now say how to avoid
  it. The AppServices and TrueCourseCalculator trials built clean under warnings as errors.
- **Two of the plan's verifications are manual, not automated**: step 2's planted `internal` and its
  `CS0122` once opted out, and step 5's planted grant failing BNAQ1005. Both were run, as the step rows
  say; `verify-release` does not repeat them.

## `plans/00007` — a console template, and one entry point for every app

[`plans/00007`](plans/00007-console-template-and-entry-points.md), approved 2026-09-29 (`fc9aa19`) and
frozen. The maintainer chose its entry-point behaviours and the AppServices helper on 2026-09-28, and
on 2026-09-29 the exit codes 0/1/2/130 with 2 scoped to `bbconsole`, and a trimmed single-file
`bbconsole`. An independent review of the first draft found eight problems, all confirmed and fixed
before approval: among them, that `bbavalonia` has neither the `UnobservedTaskException` hook nor
stderr, as the draft had claimed, and that the helper must survive `WebApplicationFactory`.

| Step | State | Notes |
|---|---|---|
| 1 · AppServices ships the helper | **done** 2026-10-01 | `2026.4.1001`, tag `v2026.4.1001` at `03ebb64`, built in JanusMael/Bennewitz.Ninja.AppServices#8. Checked here, not taken on report: all five AppServices ids list `2026.4.1001` in the nuget.org flat-container, `.EntryPoint` among them, and the release run and the required `aot-linux` and `aot-windows` jobs are green on `03ebb64`. AppServices reports a warning-free native AOT publish on both, the probe's contract passing 18 of 18 on Linux, including real SIGINT and SIGTERM, and 14 on win-x64 with the 4 signal cases skipped |
| 2 · `bbconsole` and its `verify-release` case | **done** 2026-10-09 | `templates/bbconsole`, stem `CliStem`: one console project calling `AppMain.RunConsoleAsync`, an example command counting lines (`LineCount`), and `CommandLineTests`, which run the built executable as a child process: exit 0 with the count on stdout, `--version` printing `PublicVersion`, 2 with the usage for an unknown option and for no file, 1 with the report for a missing file, and 130 after SIGINT, skipped on Windows. Trimmed and single-file with ILLink's warnings as errors, no baseline; the release publishes six runtime identifiers from Linux; CI's `publish` job publishes linux-x64 and runs it. `verify-release` gains its case and a `Console` publish kind, which runs the published binary: `--version` must print exactly the published version, and an unknown option must exit 2 with the usage on stderr and nothing on stdout. Verified: 10 of 10 tests on Linux (WSL, SDK 10.0.401), 9 and the SIGINT skip on Windows; a trimmed linux-x64 publish with no warnings, 13 MB; `verify-release` passes, all six generated repositories, 244 package entries in five templates; 105 of 105 root tests. Canaried: `--version` changed to print the informational version fails the generated repository's own test (actual "Built with ♥"), and with that test silenced, fails `verify-release`'s check of the published binary |
| 3 · The other app templates call the helper | **done** 2026-10-09 | `bbweb` and `bbapi` build and run their host inside `AppMain.RunHostAsync`, send every log line to stderr (`LogToStandardErrorThreshold`) and dispose the app on exit; both take `AppServices.EntryPoint 2026.4.1001`. `bbavalonia` calls `AppMain.RunDesktop` with `AvaloniaDiagnostics.EntryPointOptions()` and configures logging inside the run, on `AppServices.Avalonia 2026.4.1001`. `verify-release` now runs `--version` on every published app, which must print exactly the published version, and publishes each app once more with its `Program.cs` replaced, in the scratch copy only, by one that throws inside the entry point: it must exit 1 with the fatal report naming the failure on stderr, and for `bbavalonia` in its log file too. Verified: `verify-release` passes, every existing test passing (the web ones through `WebApplicationFactory`), `bbavalonia`'s trim baseline unchanged at 7. Canaried in one run, every other case still passing: `bbapi` calling its host without `AppMain` fails the `--version` check (the API took `--version` as configuration and served until killed at 60 s), and a planted throw outside `AppMain` fails the exit-1 check (the runtime's own code, -532462766) |
| 4 · Documents, and a release | **done**: documents 2026-10-09; released as `2026.4.1022` on 2026-10-22 (its section below) | The package README names `bbconsole` with an example and the shared entry point; the package description and tags name it; `docs/repository-conventions.md` lists it in both template lists. The root `AGENTS.md` no longer describes only `bbpkg`, nor a GitHub template repository, which this stopped being on 2026-09-28 |

### Drift from `plans/00007`

**Decision 9's exit-1 case is a missing file, not a test-only argument.** The example command
counts lines, so a path that does not exist is a real failure it does not handle:
`FileNotFoundException` escapes and the entry point reports it and exits 1. A flag that exists only for
tests would ship in every generated app.

⛔ **A read from standard input never saw Ctrl+C, and only Linux showed it.** `Console.In` is a
synchronised reader whose `ReadLineAsync` runs synchronously, so the work blocked inside the read and
`WaitAsync` on the token was never reached: measured in WSL, the app was still waiting 5 s after
SIGINT, and the test timed out at 30 s. On Windows the test is skipped, so a Windows run alone passes.
Standard input is now read on a thread of its own, and the wait for it observes the token: exit 130,
and the test passes. ⓘ The harness has a trap of its own: a non-interactive bash starts a background
job with SIGINT ignored, so a by-hand reproduction needs `set -m`.

**CI's job that publishes and runs the binary is `publish`**, a required check beside `build` and
`conventions`; `bbavalonia`'s equivalent is `trim`, which holds a baseline this template does not have.

**Step 3's planted failure for `bbavalonia` does not use the package's own `OnFatal`.**
`EntryPointOptions()` logs the failure and then opens the native fatal-error dialog, which blocks until
someone dismisses it, so an unattended `verify-release` would never end. Its replacement keeps the
package's `FlushLog` and `OnUnobservedTaskException` and logs the failure without the dialog; the log
file, written by the trimmed binary's own Serilog pipeline, is what the check reads. The dialog itself is
verified by nobody.

**`verify-release` also runs `bbconsole`'s planted failure and every app's `--version`**, beyond the
three templates step 3 names: the check costs one publish per app, and it is the only place a published
`bbweb`, `bbapi` or `bbavalonia` is asked for its version.

⚠ **`bbavalonia`'s BNAQ1002 test inspected nothing once its entry point moved.** The rule was limited
to Serilog, and the app referenced Serilog only through `Log.Fatal` and `Log.CloseAndFlush` in
`Program.cs`, which the package now calls. With no candidate the test fails by design ("inspected
nothing, so it proved nothing"), and it did. The rule now covers `Bennewitz.Ninja.AppServices` too,
which the app references directly, so the entry point's options and the diagnostics types are held out
of its public surface.

⛔ **Decision 3's reason and step 3's verification are wrong about `WebApplicationFactory`.** Found
by AppServices on 2026-09-30 in the .NET 10 sources: `WebApplicationFactory` resolves the entry point
with `stopApplication: false` (aspnetcore `release/10.0`,
`src/Mvc/Mvc.Testing/src/WebApplicationFactory.cs:288-292`), so it never throws
`HostAbortedException`; the entry point runs until the factory is disposed.
`HostFactoryResolver` throws it only when stopping the application, the default the design-time tools
take (`dotnet ef`, build-time OpenAPI) (runtime `release/10.0`, `HostFactoryResolver.cs:64`, `:340`).
So:
- the helper must still re-throw `HostAbortedException`, but for the design-time tools, not the test
  host; it matches the type by its short name, as the resolver catches it (`:249`), since the
  resolver throws a private type of that name when the public one does not load (`:353-364`);
- step 3's "the existing web tests still pass, which proves `HostAbortedException` is not reported as
  fatal" cannot fail. What proves it is a resolution that stops the application: a test that listens
  on the `Microsoft.Extensions.Hosting` diagnostic source and throws the public
  `HostAbortedException` on its `HostBuilt` event, as the resolver does (`:221`, `:317`, `:336`).
  `Microsoft.Extensions.HostFactoryResolver.Sources` is not on nuget.org. AppServices' own plan now
  carries that test; step 3 relies on it, and the existing web tests prove only that the helper does
  not break the test host.

**Decision 1(a) and decision 3 conflict for a crash on another thread.** 1(a) says any unhandled
exception returns 1; 3 says the helper never calls `Environment.Exit`. An exception on a thread other
than the one running `Main` terminates the process through the runtime, with the runtime's own exit
code, and only `Environment.Exit` could turn that into 1. AppServices' approved plan (its
`plans/00001`, 2026-09-30) keeps decision 3: such a crash is reported as fatal on stderr and the log is
flushed from the terminating `UnhandledException` hook, but the exit code is the runtime's. An
exception on the entry point's own thread still returns 1.

**AppServices' helper is approved** (2026-09-30): `Bennewitz.Ninja.AppServices.EntryPoint`, with
`AppMain.RunConsoleAsync`, `RunHostAsync` and `RunDesktop`, `AppMainOptions` (`Usage`, `FlushLog`,
`OnFatal`, `OnUnobservedTaskException`), `UsageException`, and `AvaloniaDiagnostics.EntryPointOptions()`
for `bbavalonia`. `FlushLog` also runs on `ProcessExit` (SIGTERM). Its native AOT measurement is a
required CI job on linux-x64 and win-x64. Step 1 is done once it is released and verified.

**As released (`2026.4.1001`), the API steps 2 and 3 call**, namespace
`Bennewitz.Ninja.AppServices.EntryPoint`, referencing nothing but the framework:
`AppMain.RunConsoleAsync(Assembly, string[], Func<string[], CancellationToken, Task<int>>, AppMainOptions?)`,
`RunHostAsync(Assembly, string[], Func<string[], Task>, AppMainOptions?)` and
`RunDesktop(Assembly, string[], Func<string[], int>, AppMainOptions?)`. `bbavalonia` takes
`AvaloniaDiagnostics.EntryPointOptions()` from `Bennewitz.Ninja.AppServices.Avalonia`
(namespace `Bennewitz.Ninja.AppServices.AvaloniaUI`). `--version` as the only argument prints
`[AssemblyMetadata("PublicVersion")]` and exits 0, or 1 if it is missing.

⛔ **SIGTERM is not `ProcessExit` on Linux.** AppServices measured that .NET 10 raises no `ProcessExit`
on SIGTERM there, so its plan's premise, and this plan's "`FlushLog` also runs on `ProcessExit`
(SIGTERM)" above, were wrong. As released, a console or desktop run registers a SIGTERM
`PosixSignalRegistration` that flushes the log and lets the signal end the process (exit code 143); a
web run relies on its host's own SIGTERM handling. A run flushes once, so an app that handles SIGTERM
itself, such as a generic host, belongs in `RunHostAsync`, not inside `RunConsoleAsync`.

## `plans/00008` — the trimming enforcement

[`plans/00008`](plans/00008-trimming-enforcement.md), approved 2026-10-09 and frozen. The maintainer
chose, through `choices`, each of the decisions that could have gone another way: all four properties
make a repository a trimming one, a repository that does not trim gets no finding at all, and the
`"trimming"` key is retired with a NOTE where it remains. Side 2 is XamlQuality's, which was told the
plan is unblocked the same day.

| Step | State | Notes |
|---|---|---|
| 1 · The predicate, the library rule and the retired key's NOTE, with the documentation | **done** 2026-10-22 | `Trimming.Trims` and `Trimming.Reasons` in the canonical script, over the four properties, now in `Projects.Names`. The rule is `Trimming` among the library rules, so its exemption and the stale-exemption NOTE come from the shared path; the FAIL names up to three projects that make the repository trim. `TrimmingRequired` is gone, and `"trimming"` present is a `repository.json` NOTE. `PropsTests` gains three runs over a `Directory.Build.props` that sets no trimming property: one trimming through nothing, one through an app's `PublishTrimmed`, one through `PublishAot`. Six new tests, each failing against the previous script while the other 17 pass; 23 of 23 with the new one, 112 of 112 root tests. This repository's own `check` conforms |
| 2 · The templates drop `"trimming"` | **done** 2026-10-22 | All five templates' `repository.json`. `ConventionsWorkflowTests.The_template_requires_trimming` becomes `No_template_ships_the_retired_trimming_key`, over every template. `verify-release` passes, every generated repository's offline check showing only the intended gaps. Canaried with `IsTrimmable` removed from `bbpkg`'s `src/Directory.Build.props`: `verify-release` fails on `bbpkg` alone, and the generated repository's own `check --offline`, with no `"trimming"` key, fails `Widget (library): is not trimmable, and the repository trims (Widget: EnableTrimAnalyzer)` |
| 3 · Side 2, XamlQuality's patch, behind the same predicate | waits for the patch | Does not block the release. XamlQuality's design (its message of 2026-10-10): walk `project.assets.json` and read `[AssemblyMetadata("IsTrimmable", "True")]` from each `Bennewitz.Ninja.*` package's assemblies with System.Reflection.Metadata. It calls `Trimming.Trims` |

### Drift from `plans/00008`

- **A `Trimming` exemption in a repository that does not trim is reported as no longer needed.** The plan
  says only that a stale exemption is still reported; with nothing to exempt from, the exemption is
  stale too. It is a NOTE about the exemption, not a trimming finding about a library.
- **Step 2's canary fails `verify-release` earlier than the plan says.** The plan expects the generated
  `bbpkg`'s trimming FAIL; the generated repository's own `TrimmableTests` fails first, at its test
  step, so the conventions step never runs. The FAIL was shown instead by running that repository's
  `check --offline` on the tree `--keep` left. Two guards for one loss, and the earlier one wins.

⚠ **A script canary must touch the file it swaps in.** `Copy-Item` keeps the source's timestamp, and
`dotnet run --file` then reused its cached build of the newer script: the first canary of step 1 passed
all 23 tests against the previous script. With the copy's `LastWriteTime` set to now, the six new tests
failed as they should.

## Symbols in both configurations — outside the plans

Asked for by the maintainer on 2026-10-09: every template generates or embeds PDBs in Debug and in
Release. Every template's `Directory.Build.props` already did, `portable` in Debug and `embedded` in
Release, with no project overriding it. Two gaps were closed:

- **`bbapi`'s native symbols were thrown away at every release.** A native binary's symbols are files of
  their own (`.pdb` on Windows, `.dbg` on Linux, the `.dSYM` bundle on macOS), and the release archived
  only the executable and its settings. They are now a second asset per runtime identifier,
  `ApiStem-<version>-<rid>-symbols`, named in the release like the binary; a missing one fails the
  release. `verify-release` asserts the native publish writes this platform's file.
- **The conventions held only libraries and tools to symbols, and only in Release.** `DebugType` is now
  `embedded` in Release for every project but a template package, and a new rule, `DebugSymbols`, reads
  a second, Debug evaluation and requires `portable` or `embedded`. `PropsTests` gains an app without
  Release symbols and a library without Debug symbols; both fail against the previous script, and
  every other case is unchanged (18 of 18).

⚠ **`DebugSymbols` does not decide whether symbols are emitted; `DebugType` does.** Measured on SDK
10.0.401: with `DebugType` `embedded`, Release evaluates `DebugSymbols` to `false` by default and the PDB
is embedded all the same. The first version of the rule also required `DebugSymbols` not to be `false`,
and failed every correct project in the fixtures. The rule is named `DebugSymbols` for what it protects;
it reads `DebugType`.

## No package glob in a release — outside the plans

The maintainer's decision of 2026-10-09: a release never publishes a `*.nupkg` glob, and the
conventions check fails it, not merely notes it. Until now only this repository's own two release
workflows were held to it, by `PackagingTests`; every family repository now is, through `check`.

`Workflows.CheckPublishing` fails a `.nupkg` or `.snupkg` path holding `*` or `?` anywhere in a
workflow that publishes (`nuget push`, `gh release create` or `upload`, `action-gh-release`), at every
depth including `--release`, so the preflight stops the publish. It reads the whole workflow because
the family's globs sit on continued or folded lines apart from their command. A workflow that
publishes nothing may glob, and comments are skipped. Documented as its own section of
`docs/repository-conventions.md`.

`RepoConventionsTests` gains three: AutoVersioning's former release (both globs, at release depth),
Geo.Core's folded `run: >` shape, and a passing case of exact files per id, a glob in a commented line
and one in a workflow that only lists packages. The two failing cases fail against the previous
script. The passing case was proven by two planted over-reaches, each caught by it alone: comments
read as code, and every workflow treated as publishing. 30 of 30 in the class.

⚠ **A planted mutant must still compile.** The first version of the second mutant, `if (false)`, is
unreachable code, an error under warnings-as-errors, so the script never built and 29 of 30 tests
failed for that reason alone. `line.Length >= 0` in place of the publishing match compiles and
failed only the passing case.

Who it reaches today, read from GitHub on 2026-10-22: no family repository. AutoVersioning dropped its
glob that day (its `aa0b9bd`, PR #5), and FileServer already had; a stale local checkout of either
still shows one. TrueCourseCalculator, Geo.Core and GeoHash glob, but are local-only and carry no
script.

## The root tests in 93 s instead of about 260 — outside the plans

Asked for by the maintainer on 2026-10-22. Measured class by class first, each alone: `GrantsTests`
106 s, `PropsTests` 93 s, `RepoConventionsTests` 25 s, the other six 37 s together. The first three
shared one serial collection, because parallel `dotnet run --file` builds of one script contend for
its output, so the suite's wall-clock was that chain. A `dotnet run --file` launch also cost about 0.5 s
against 0.06 s for the built program.

`ConventionsScript` builds `repo-conventions.cs` once per test run, `--no-incremental`, and launches
the program; the collection is gone, and `PropsTests`' six repositories and `GrantsTests`' three run
at once. One test, `The_script_runs_as_a_person_runs_it`, still launches it with `dotnet run --file`.
116 of 116 in 93 s, and 97 s on a second pass. `tests/AGENTS.md` records the rule.

⭐ **The canary trap is gone with it.** The file-based cache trusts timestamps, so the first canary of
`plans/00008` step 1 ran a stale build. With the pre-no-glob script copied in under a January
timestamp, the forced build read it as it was and the two glob tests failed, as they should.

## "No FAIL" assertions that could not fail in CI — outside the plans

Found 2026-10-23, when `plans/00009` step 3's first CI run went red: under GitHub Actions
`repo-conventions.cs` prefixes a failing finding with `::error::`, so a test line reading
`::error::FAIL …` never `StartsWith("FAIL …")`. A positive assertion of that form failed, which is how
it was noticed; the 15 `DoesNotContain(…StartsWith("FAIL …"))` in `PropsTests` and `GrantsTests`
instead passed whatever the script found, in CI only. `ConventionsScript.Lines` now strips the prefix,
and both classes read lines through it.

Proven 2026-10-23 with two defects planted that only those assertions guard (the grant list always
stale; `AssemblyCompany` wanted as another value), the suite run with `GITHUB_ACTIONS=true`: **0 of
157 failed on `main`**, 9 failed with the fix, each an absence or "not blamed" assertion. Clean, 157 of
157 pass with and without the variable.

## AssemblyQuality in every template — outside the plans

The maintainer's decision of 2026-10-09, for the next release. `bbpkg`, `bbweb`, `bbapi` and `bbconsole`
gain `tests/<Stem>.Tests/Architecture/AssemblyQualityTests.cs`, as `bbavalonia` has it, with
AssemblyQuality `2026.3.929`, its latest, pinned in each: BNAQ1001, BNAQ1002, BNAQ1004 and BNAQ1005
over the compiled assembly, each asserting it skipped nothing and, where it can, that it inspected
something. BNAQ1003 is left out as in `bbavalonia`: a one-project repository has no layers yet.

- **BNAQ1002 covers `Bennewitz.Ninja.AppServices` in the three apps,** which each references through
  the shared entry point, so it inspects something there.
- **In `bbpkg` it cannot fail yet, and says so.** A new library references no namespace worth keeping
  inside it, so the default set inspects nothing. The test asserts only that it skipped and found
  nothing; its comment and `tests/AGENTS.md` say to name the first such dependency and assert
  `Inspected` then.

`verify-release` passes: each generated repository gains exactly the four tests (`bbpkg` 16, `bbweb`
14, `bbweb --blazor` 17, `bbapi` 15, `bbconsole` 14 with SIGINT skipped on Windows). Canaried with one
planted defect per template, each failing only its own rule: a defaulted token in `bbpkg` (BNAQ1001),
an `AppMainOptions` returned from a public method in `bbweb` (BNAQ1002), a stray grant in `bbapi`
(BNAQ1005), a `Planted.System` namespace in `bbconsole` (BNAQ1004).

⚠ **A namespace planted under the app's own root broke the build instead.** `Bennewitz.Ninja.CliStem.System`
made the tests' `System.Globalization`, read from inside `Bennewitz.Ninja.CliStem.Tests`, resolve to it:
CS0234, exactly the failure BNAQ1004 exists for, but at compile time, so the rule never ran. A plant
outside the tests' namespace chain compiles and reaches the rule.

⚠ **The raw `templates/bbweb` folder is not a valid build of the template.** Its `ComponentTests.cs`
belongs to the `--blazor` variant and is excluded by `template.json` otherwise, so testing the folder
directly fails three tests that `dotnet new` would never have emitted. Test a template through
`verify-release`, or through `dotnet new` from the packed template.

## `plans/00009` — package families consolidate into Inspections and AppKit

[`plans/00009`](plans/00009-package-consolidation.md), approved 2026-10-22 and frozen. The catalog
and the groupings it rests on are [`docs/package-consolidation.md`](docs/package-consolidation.md),
measured 2026-10-20. The maintainer chose, through `choices`: groupings A and B in full; the names
`Bennewitz.Ninja.Inspections` and `Bennewitz.Ninja.AppKit`; growing the largest family's repository by
renaming it; a release per family; archiving with a pointer; `Bennewitz.Ninja.AgentForge.*` staying in
ClaudeForge on GitHub Packages; multi-family support in the release after the queued one; old policies
lingering until each family's first release from its new home. Decisions 9–22 were proposed and
accepted with the plan.

| Step | State | Notes |
|---|---|---|
| 1 · AutoVersioning: `packages.push`, no glob push | **done** 2026-10-22 | JanusMael/Bennewitz.Ninja.AutoVersioning#5, merged as `aa0b9bd`, `build` and `conventions` green. The queued release's no-glob rule confirms it on the next sync. The preflight has not been run |
| 2 · XamlQuality: `packages.push` | **done** 2026-10-22 | JanusMael/Bennewitz.Ninja.XamlQuality#94, merged as `2694968`, every required check green. The preflight has not been run |
| 3 · Templates: multi-family support | **done**, released 2026-10-23 as `2026.4.1023` | Below; [JanusMael/Bennewitz.Ninja.Templates#41](https://github.com/JanusMael/Bennewitz.Ninja.Templates/pull/41). `verify-release --published 2026.4.1023` passed |
| 4 · DiffView conforms | waits on DiffView's own session | The maintainer's word, 2026-10-22, keeping that of 2026-09-24. DiffView#2 is open; no import while it is. Blocks AppKit only |
| 5 · Inspections: rename, import | **done** 2026-10-26 | Below. [JanusMael/Bennewitz.Ninja.Inspections#98](https://github.com/JanusMael/Bennewitz.Ninja.Inspections/pull/98), merged as `a18a303` by a merge commit, every check green; `check --admin` conforms |
| 6 · Inspections: the policy | **done** 2026-10-29 | Below. One policy, edited, naming Inspections with a pattern per family; the blank-version preflight logged in, run `40933076378` |
| 7 · Archive AssemblyQuality and CodeQuality | **done** 2026-10-29 | Below. Both archived, no open issue or pull request on either |
| 8 · AppKit | after Inspections | |
| 9 · Each family's first release from its new home | **AssemblyQuality done** 2026-10-29 | Below. `assemblyquality/v2026.4.1029`, verified from the feed; its old policy deleted by the maintainer the same day. XamlQuality and CodeQuality when each next changes |
| 10 · AgentForge rename in ClaudeForge | **already true before the plan** | See the drift below |
| 11 · Links | **Inspections' done** 2026-10-29 | Below. AppKit's after step 8, by the maintainer's decision |

### Step 3, as built

What a release publishes is resolved by a new shipped script, `templates/bbpkg/scripts/release-target.cs`,
which the shipped `release.yml` runs first, before anything is built:

| Part | What it does |
|---|---|
| `release-target.cs resolve` | The tag (`v<version>`, or `<family>/v<version>` where `families` is declared) or the dispatch's `version` and `family`, into `VERSION`, `TAG`, `FAMILY`, `IDS` and `PREVIOUS_TAG`, appended to `$GITHUB_ENV`. Refuses a bare tag or a dispatch without a family where there are families, a family tag or a family dispatch where there are none, an undeclared family, and a family id `packages.push` lacks |
| `release-target.cs projects` | After the build, the project that packs each of the family's ids, by evaluated `PackageId`; the release packs those alone |
| `assert-packages.cs --family` | A family's pack must hold exactly the family's ids; without `--family`, unchanged |
| `release.yml` | Tags `v*.*.*` and `*/v*.*.*`; a `family` dispatch input; `fetch-depth: 0` for the tags; push and GitHub release iterate `IDS`; a family's notes start at `PREVIOUS_TAG` |
| `repo-conventions.cs` | `families` parsed and checked: a stem fit for a tag and declared once, the ids exactly `packages.push`'s, at every depth. `release-tags` includes `refs/tags/*/v*` only where `families` is declared. A packable project referencing another family's packable project FAILS, read by evaluated `PackageId` and `ProjectReference` |

Tests: 157, from 116. `ReleaseTargetTests` (each tag and dispatch form, each refusal, the previous tag,
the version check, `projects`), `AssertPackagesTests` (a family pack that matches, one with an extra
id, one with a missing id), four conventions tests (the single-family ruleset unchanged, the family
include, `families` against `packages.push`, a bad stem), and a seventh `PropsTests` run for the
cross-family reference. Canaried 2026-10-22 with six defects planted at once, each in one new rule;
nine tests failed, each on its own defect, and every file was restored byte for byte.
`verify-release` also resolves a `v` tag in the generated `bbpkg`, compiling the script under that
repository's own build properties.

### Step 5, as built

Begun 2026-10-26, when AssemblyQuality#17, the last open pull request among the three, had merged.
Built in a fresh clone of XamlQuality, on a branch, `feat/inspections`. Its commits: AssemblyQuality's history and CodeQuality's, each imported whole by
`git filter-repo` with its tags renamed `<family>/v…` and AssemblyQuality's plans moved to
`plans/assemblyquality/`; then the merge itself, by the plan's *Merging* table, with this
repository's canonical files from `2026.4.1023`; then the documents. Its `PROGRESS.md` holds the
merged state, what changes for each family's consumers, and the decisions below in full.

Verified on the branch, 2026-10-26, against the plan's step 5 and the release's own commands:

| Check | Result |
|---|---|
| `dotnet build Inspections.slnx -c Release -warnaserror` | 0 warnings, 0 errors |
| `dotnet test --solution Inspections.slnx` | 916 of 916 passed |
| `dotnet pack` of the solution, then `assert-packages.cs` | the five ids of `packages.push`, nothing else |
| Each family as the release packs it: `release-target.cs projects --family`, a pack of those projects, `assert-packages.cs --family` | `xamlquality` three ids, `assemblyquality` one, `codequality` one; each passes |
| `release-target.cs resolve` | `codequality/v2026.4.1026` resolves with `PREVIOUS_TAG` `codequality/v2026.3.928`; `xamlquality/v…` with none, so its notes say it is the family's first; a bare `v2026.4.1026` is refused |
| `repo-conventions.cs check --offline --root … --repo JanusMael/Bennewitz.Ninja.Inspections` | conforms |
| `git tag` | `assemblyquality/v…` for `2026.3.922`, `.925`, `.928` and `.929`; `codequality/v…` for `2026.3.925` and `.928`; XamlQuality's own `v…`; no duplicate |
| `IAssemblyRule.cs`'s history | `git log --follow` reaches `bd44084`, AssemblyQuality's first commit, which the rewrite left unchanged: `commit-map` maps it to itself, since only later commits held `plans/` |

### What step 5 found for the merge

- **XamlQuality's `main` takes squash and rebase merges only**, the family baseline. The branch
  holds two merge commits, the imports, and either kind of merge would drop the imported histories
  off `main`, leaving the renamed tags on commits `main` does not reach. The plan's step 5 says only
  that the pull request merges.
- **XamlQuality's `main` moved after the branch was cut**: PR #96, docs, and the maintainer's call
  for a release, on 2026-10-26. The branch takes it by a merge of XamlQuality's `main`, regenerating
  `PROGRESS.md`, before it is pushed.

### How step 5 landed, 2026-10-26

On the maintainer's word, in this order:

1. XamlQuality released `2026.4.1026` under its own name first, and its `main` after the release,
   with XamlQuality#97, was merged into the branch; the verification above passed again on it.
2. `JanusMael/Bennewitz.Ninja.XamlQuality` renamed `Bennewitz.Ninja.Inspections`. The branch and the
   six `assemblyquality/v…` and `codequality/v…` tags pushed, the tags before `apply` put
   `refs/tags/*/v*` under the release-tags ruleset.
3. Inspections#98 opened, and `repo-conventions.cs apply --root <branch>` run: the description, the
   topics, the `main` ruleset's required checks renamed to the merged CI's jobs, and the family tag
   include. The `conventions` runs that started before it failed on exactly those four settings and
   passed when rerun.
4. A merge commit allowed on the repository, and the pull request merged by one with the admin
   bypass, since the `main` ruleset's `pull_request` rule also allows only squash and rebase. Merged
   as `a18a303`, every check green.
5. `apply` run again from `main`, disallowing merge commits; `check --admin` conforms. On `main`,
   `IAssemblyRule.cs`'s history reaches `bd44084`, and `assemblyquality/v2026.3.929` and
   `codequality/v2026.3.928` are ancestors of `main`.

### Step 6, 2026-10-29

The maintainer edited XamlQuality's policy on nuget.org rather than adding one, by decision 20:
named `Bennewitz.Ninja.Inspections — release.yml`, patterns `Bennewitz.Ninja.XamlQuality*`,
`Bennewitz.Ninja.AssemblyQuality*` and `Bennewitz.Ninja.CodeQuality*`, workflow `release.yml`, no
environment. `NUGET_USER` carried over the rename.

**Decision 20's open question is answered: nuget.org matches the repository by name.** The policy
records the repository's id beside its name, `#1378633436`, which the rename did not change, yet the
first blank-version preflight failed with a 401, *"No matching trust policy owned by user
'JanusMael'"*, while the policy still showed `Bennewitz.Ninja.XamlQuality`. Edited to
`Bennewitz.Ninja.Inspections`, the next one logged in (run `40933076378`). So a renamed repository's
policy must be edited before its first release, and step 8 has the same edit to make for
AppServices. Inspections' `docs/publishing.md` gains the cause in its 401 checklist
(JanusMael/Bennewitz.Ninja.Inspections#100); `bbpkg`'s runbook has no rename in it, and does not need
one until a repository it generated is renamed.

### Step 7, 2026-10-29

By the maintainer's decisions of that day, taken one at a time:

| Decision | Choice |
|---|---|
| When | Archive both now: nothing blocked it once steps 5 and 6 were done |
| CodeQuality#3, its one open issue | Closed as done rather than transferred, as the plan's letter had it: it asked for the conventions of `2026.4.1022`, and Inspections carries `2026.4.1023`'s |
| The final README commits | Pushed straight to each `main` with the admin bypass, not through pull requests: a notice under the title naming Inspections, its README there, the unchanged ids and the `<family>/v…` tags, above the README as it was |
| The AssemblyQuality session | Not messaged; the maintainer's to tell |

Each README is kept whole below its notice: the help links CodeQuality `2026.3.928` shipped point at its
`#bncq…` sections, and its packed README at AssemblyQuality's `#bnaq…` ones. Then both archived;
`gh api` reports `archived: true` for each, with no open issue or pull request on either. Their
policies on nuget.org stay until each family's first release from Inspections, by decision 20.

### After Inspections: the maintainer's decisions of 2026-10-29

| Decision | Choice |
|---|---|
| Step 9 | Release AssemblyQuality now: it had a change of its own, its #17, unreleased since `2026.3.929`, so decision 21 does not hold it, and it proves the family release path end to end |
| Its notes, once the defect below was found | Release anyway and replace the generated notes by hand; the packages, the permanent part, were right |
| Step 11 | Inspections' moved repositories' links now, AppKit's after step 8 |
| `bbpkg`'s gaps for families | A plan, `plans/00011`, before AppKit, so AppKit merges onto canonical files instead of repeating step 5's hand fixes; the `Pkcs` pin to `plans/00010` |
| DiffView | One note about step 4, then left to its session |

### Step 9: AssemblyQuality `2026.4.1029`, 2026-10-29

Tagged `assemblyquality/v2026.4.1029` on Inspections' `3092aee`, CI green there; release run
`40939268891` resolved the family, built, tested, packed and asserted it alone, logged in through
the edited policy, and pushed `Bennewitz.Ninja.AssemblyQuality 2026.4.1029`, its one asset. Read back
from nuget.org: the flat container lists it; the served nuspec's repository is Inspections at
`3092aee`; its DLL is byte-identical to the release asset's; a project restoring from nuget.org alone
gets it, version `2026.4.1029.1921`, and finds all six rules. Against `2026.3.929`, the package adds
the friend grants of #17, sixteen, all to Inspections' own assemblies, names Inspections, and packs
`docs/assemblyquality.md` as its README; no dependency, rule or public API changed.

**A defect of step 3: a family's generated notes list the whole repository's pull requests.**
`release.yml` passes `--generate-notes --notes-start-tag "$PREVIOUS_TAG"`, and GitHub lists every pull
request merged between the two tags, whichever family it touched. For this release that was 95 of
XamlQuality's, since the imported tag `assemblyquality/v2026.3.929` shares no history with them, and not
#17, which merged in the archived repository. It would list other families' work in every later
family release too. Found by previewing the notes through the API before tagging; the release's notes
were replaced by hand after the run. The fix, notes scoped to the family's own paths, is
`plans/00011`'s. **GitHub also marked the release "Latest"** for the whole repository, over
XamlQuality's `v2026.4.1026`; with three families the marker means little, and whether a family
release takes it is `plans/00011`'s to decide too.

### Step 11, Inspections' links, 2026-10-29

Every link naming Bennewitz.Ninja.XamlQuality now names Bennewitz.Ninja.Inspections: this
repository's `AGENTS.md`, `docs/repository-conventions.md`, `PROGRESS.md`'s one link, and the
`AGENTS.md` that `bbavalonia` and `bbpkg` generate, here; ClaudeForge's seven documents, 18 links,
in ClaudeForge#126, and JsonC's `AGENTS.md` in Bennewitz.Ninja.JsonC#5, each changing nothing but the
repository's name, since the documents kept their paths and headings. Bare references to a past pull
request, such as `JanusMael/Bennewitz.Ninja.XamlQuality#94`, are left: GitHub redirects them, and
they record where the work happened. AssemblyQuality and CodeQuality are linked from nothing outside
their own and Inspections' documents, which carry the moved notices. DiffView's links wait for step 8,
since a pull request there would hold up its import. TailBlazer's hit is a local `CLAUDE.md` in a
checkout of another owner's repository, and not this family's. The maintainer merged ClaudeForge#126
(`7482f4b`) and Bennewitz.Ninja.JsonC#5 (`e7b75af`) the same day, and deleted AssemblyQuality's old
policy on nuget.org.

### What step 5 found for the templates

Each is a candidate for `bbpkg`, not yet a change here:

- **A repository with families needs a root `AssemblyProduct`.** AutoVersioning fails the build
  without one (`BAUTOVERSIONING03`); each package then sets its family's.
- **A family's packages need their family's README.** `bbpkg`'s root props pack the root
  `README.md` into every package. Inspections packs each family's `docs/<family>.md` instead, through
  a `FamilyReadme` property in each package's project, and the root README condenses them.
- **A family's README is a nuget.org page, so its links must be absolute**, and the maintainer's
  sections, releasing and publishing, belong in the root README instead.
- **`System.Security.Cryptography.Pkcs` needed a pin at `10.0.14`.** `Microsoft.CodeAnalysis.Analyzer.Testing`
  brings in `9.0.6` through `NuGet.Packaging` `7.0.3`, which GHSA-2fmv-rv2c-59x7 and
  GHSA-846f-f764-76mx (2026-10-13) made a restore error. CodeQuality's own `main` has failed restore
  so since that date, unseen. An analyzer template (`plans/00010`) would meet it first.

### Drift from `plans/00009`

- **The release also checks the version**, in every repository, families or not: `YYYY.Q.MMDD`,
  three parts, the quarter of the date. Decision 13 moved the resolution into a script; refusing a
  malformed version there costs nothing, and it would have refused `2026.3.1001`.
- **A family's first release has no previous tag**, so its GitHub release says it is the first
  rather than generating notes against another family's tag.
- **This repository's own release is unchanged.** `release-target.cs` ships in `bbpkg` only, as
  `assert-packages.cs`'s root copy is kept apart by intent: a template-motivated edit must not change
  how Templates releases itself.
- **A family's key is matched without regard to case and stored lower-case**; its tags must be
  lower-case exactly, and a dispatch's family is lower-cased. An id in two families FAILS.
- **Decision 7 and step 10 rested on a misread catalog.** ClaudeForge's `src/Directory.Build.props`
  has set `PackageId` to `Bennewitz.Ninja.$(MSBuildProjectName)` since `c3f34ca8` (2026-09-13), and
  GitHub Packages holds `Bennewitz.Ninja.AgentForge.*`. The catalog read only each `.csproj` and the
  root props, so it reported the bare ids and a collision with `subhajitlabs`' `AgentForge.*` on
  nuget.org that does not exist. Nothing was changed in ClaudeForge; the catalog is corrected.

## `plans/00011` — package families, completed

[`plans/00011`](plans/00011-package-families-completed.md), approved 2026-10-29 and frozen. It
supersedes `plans/00009` decision 13's release notes only. It answers what Inspections had to fix by
hand, so AppKit merges onto canonical files: a family's README packed into its packages, its
`AssemblyProduct`, conventions checks for both, release notes scoped to the family, and GitHub's
"Latest". The maintainer chose, through `choices`: `"bareTags"`, so a grown repository's bare `v…`
tags count as its first family's previous releases; a family release marked "Latest" when no tag is
newer; and a single-family repository's packed README, which has the same relative links, left to a
plan of its own, `plans/00012`, drafted after this one is released. A `step-back` review added a
real pack through `bbpkg`'s root props to step 2, made the link rule precise, counted unpacked
projects a package references among its family's paths, and made "Latest" depend on the tags.

The notes rule was measured on Inspections before the plan was written. A family's own paths are
read across the whole range, and the shared build inputs only on commits descending from the
family's previous tag: this gives AssemblyQuality `2026.4.1029` exactly `7e82d76`, `1701070`,
`7e5236f` and `ed5544b`, #17, which changed only root files and tests.

| Step | State | Notes |
|---|---|---|
| 1 · The `notes` verb, `bareTags` and `LATEST` | **done** 2026-10-29 | Below |
| 2 · `FamilyReadme`, the `AssemblyProduct` and README rules, the packed README compared | **done** 2026-10-29 | Below |
| 3 · `docs/repository-conventions.md` and the shipped files' comments | **done** 2026-10-29 | With steps 1 and 2, each in its own change, as the conventions checklist asks |
| 4 · A Templates release | **done** 2026-10-29 | `2026.4.1029`, its section below |
| 5 · Inspections syncs, with `"bareTags": "xamlquality"` | **next** | With the template's README item in its root props |

### Step 1, as built

`release-target.cs` gains a `notes` verb and two values from `resolve`; `release.yml` passes the
notes file and `--latest=$LATEST` for a family, and keeps `--generate-notes` without families;
`repo-conventions.cs` checks `bareTags`, copied to `scripts/` and every template.

| Part | What it does |
|---|---|
| `resolve` | `PREVIOUS_TAG` counts the bare `v…` tags as the `bareTags` family's; refuses a `bareTags` that is no declared family. `LATEST` is `false` when a tag of any family, or a bare one, has a higher version; empty without families |
| `notes` | The family's projects, any unpacked project they reference (followed by evaluated `ProjectReference`), and `docs/<stem>.md`, read across `<previous>..HEAD`; the shared build inputs only with `--ancestry-path`; merges left out. No previous tag: "The first release of the … family." |
| `repo-conventions.cs` | `bareTags` without `families`, or naming no declared family, FAILS |

Tests: 173, from 157. Against today's scripts, 18 of the new and changed tests failed, each for its
own missing behaviour; one new test, a family's own newer tag beating the bare ones, passes against
either, as a guard. Run on Inspections at `assemblyquality/v2026.4.1029`, `notes` from
`assemblyquality/v2026.3.929` lists `7e5236f`, `1701070` and `7e82d76` as the family's and
`ed5544b`, #17, as the shared build's, and nothing else.

### Steps 2 and 3, as built

| Part | What it does |
|---|---|
| `bbpkg`'s root `Directory.Build.props` | Packs `$(FamilyReadme)` as `README.md` when a project sets it, and the root `README.md` otherwise. A comment on its `AssemblyProduct` says a family's packages set their own |
| `repo-conventions.cs` | With families, FAILS a family without `docs/<stem>.md`; a relative link in one (inline, image, reference definition, HTML `href` or `src`; not in fenced or inline code; `#…`, a scheme and `//` are absolute), with its line; and a family package, by evaluated `PackageId`, whose evaluated `FamilyReadme` is not `docs/<stem>.md` or whose `AssemblyProduct` is not the stem without case or separators. The README rules read the tree, so `check --repo` reaches them too |
| `assert-packages.cs --family` | Fails when `docs/<family>.md` is missing, or a package's README, the file its `.nuspec` names, is not that file byte for byte, or there is none |
| Documents | `docs/repository-conventions.md`'s *Package families*; `bbpkg`'s `AGENTS.md` (an invariant), `scripts/AGENTS.md` and `docs/publishing.md` |

Tests: 189, from 173. Against today's scripts and props, the 14 new tests failed, each for its own
missing behaviour, and the reshaped families fixture's existing tests passed. `PropsTests`' families
run now has two families, `alpha` and `Beta-Kit`, so a stem's separator is compared; the real pack,
`FamilyPackTests`, packs three projects through the template's own build files and also runs the
guard over each family's package. `check --offline --root` against Inspections conforms, and the same
check against a copy with a relative link in `docs/codequality.md` and `AssemblyProduct` set to
`Inspections` in `CodeQuality.csproj` FAILS exactly those two. `verify-release` passed: the generated
`bbpkg` has no families and packs its root README as before.

### Drift from `plans/00011`

- **Step 3 was done with steps 1 and 2, not after them.** The conventions checklist asks for the
  document in the same change as the script, and the rest was four lines of `bbpkg`'s documents.

### The maintainer's decisions after step 3

Taken through `choices` on 2026-10-29: release this day; in step 5, Inspections replaces its own
README item in `Directory.Build.props` with the template's, which falls back to the root README, so
it matches the template as AppKit will; no note to the other family repositories, since only
Inspections declares families and the new rules are inert elsewhere; and after step 5, draft
`plans/00012`.

## Release `2026.4.1029`

✅ **Released as `Bennewitz.Ninja.Templates 2026.4.1029`**, 2026-10-29, tag `v2026.4.1029` at
`a21bd4c`, on the maintainer's word. It carries `plans/00011` steps 1 to 3: a family's release notes,
`bareTags` and "Latest" from `release-target.cs`, a family's packed README and `AssemblyProduct`, and
the conventions rules and the guard that check them.

Before the tag: `verify-release` passed locally on `a21bd4c`, and CI on `main` was green. The release
pushed the one id in `packages.push` under the unchanged policy and `NUGET_USER`, so no preflight was
run. The `release` run passed, and the GitHub release carries one asset,
`Bennewitz.Ninja.Templates.2026.4.1029.nupkg`. Verified from the feed: `verify-release --published
2026.4.1029` installed it from nuget.org, and all six generated repositories built, passed their tests
and published for win-x64. The flat container's version list did not show it yet at that point; the
install is the stronger evidence.

## Release `2026.4.1023`

✅ **Released as `Bennewitz.Ninja.Templates 2026.4.1023`**, 2026-10-23, tag `v2026.4.1023` at
`d1ae2e2`, on the maintainer's word. It carries `plans/00009` step 3: package families in `bbpkg`'s
release (`release-target.cs`, `assert-packages --family`, `<family>/v…` tags) and the families rules
in `repo-conventions.cs`, which reach each family repository on its next sync of the script.

Before the tag: `verify-release` passed locally on `d1ae2e2`, CI on `main` was green, and the release
pushed the one id in `packages.push` under the unchanged policy and `NUGET_USER`. The `release` run
passed, and the GitHub release carries one asset, `Bennewitz.Ninja.Templates.2026.4.1023.nupkg`.
Verified from the feed: the flat container listed `2026.4.1023` about a minute after the run, and
`verify-release --published 2026.4.1023` installed it from nuget.org; all six generated repositories
built, passed their tests and published for win-x64.

## Release `2026.4.1022`

✅ **Released as `Bennewitz.Ninja.Templates 2026.4.1022`**, 2026-10-22, tag `v2026.4.1022` at
`198e5cd`, on the maintainer's word. It carries `bbconsole` and the shared entry point in every app
(`plans/00007`), AutoVersioning `2026.3.928` and Avalonia `12.1.4`, symbols in both configurations with
`bbapi`'s native symbols released, the trimming enforcement's side 1 (`plans/00008` steps 1–2), the
no-glob rule, and AssemblyQuality in every template.

Before the tag: `verify-release` passed locally on `198e5cd`, CI on `main` was green, and the release
workflow's only push target was the one id in `packages.push`, covered by the policy every earlier
release used; no preflight was run, nothing about the policy or `NUGET_USER` having changed. The
`release` run passed (`publish`), and the GitHub release carries one asset,
`Bennewitz.Ninja.Templates.2026.4.1022.nupkg`. Verified from the feed rather than the green run: the
flat container listed `2026.4.1022` about six minutes after the run; `verify-release --published
2026.4.1022` installed it from nuget.org, and all six generated repositories built, passed their
tests (`bbpkg` 16, `bbavalonia` 17, `bbweb` 14, `bbweb --blazor` 17, `bbapi` 15, `bbconsole` 14 with
SIGINT skipped on Windows) and published for win-x64; and `dotnet new install` from nuget.org into a
throwaway hive lists the five templates.

⚠ **`2026.3.1001` carried the wrong quarter.** `YYYY.Q.MMDD` takes the calendar quarter
(`docs/publishing.md`), so 1 October was `2026.4.1001`, as AppServices' release that day was named.
A published version is permanent, and it does no harm to ordering: `2026.4.1022` sorts above it.
