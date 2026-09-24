# Progress

Work state for `Bennewitz.Ninja.Templates`. Plan: [plans/00001-package-template.md](plans/00001-package-template.md),
approved 2026-09-20 and frozen — drift from it is recorded here, never edited into it. The same
holds for [plans/00003](plans/00003-repository-conventions.md), in progress: see its own section.

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
- Tests: `tests/Templates.Tests/MstestToXunit`, over `.cs.txt` fixtures whose output was compiled
  and run with the emitted helpers before it was accepted. Canaried both ways — a corrupted expected
  file and a broken rule each fail exactly one test.

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
   [`docs/avalonia-gotchas.md`](https://github.com/JanusMael/Bennewitz.Ninja.XamlQuality/blob/main/docs/avalonia-gotchas.md)
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
| 5 · Adopt it all in this repository, then `apply` | next | The first ruleset anywhere: its CI run answers whether the token reads a ruleset's rules |
| 6–10 | not started | |

**What a workflow's read-only `GITHUB_TOKEN` reads** (2026-09-24):

| Returned | Not returned |
|---|---|
| description, homepage, topics, default branch | the six merge options: `allow_merge_commit`, `allow_squash_merge`, `allow_rebase_merge`, `allow_auto_merge`, `delete_branch_on_merge`, `allow_update_branch` |
| `has_issues`, `has_wiki`, `has_projects`, `has_discussions` | `security_and_analysis` |
| the rulesets list, the tree, file contents | vulnerability alerts, classic branch protection |

The feature toggles were a finding: the anonymous stand-in did not predict them. `check` now checks
every setting GitHub returns, at either depth, and notes by name the ones it could not read.
⚠ No repository has a ruleset yet, so whether the token reads a ruleset's rules and its bypass list
is still unmeasured. Step 5 creates the first one; `check` in that run's CI answers it.

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

⚠ **A file-based app compiles trimmed.** A `JsonArray` built from a collection expression, or passed
a `JsonObject` to `Add`, binds to the generic `Add<T>` and fails the build with `IL2026`/`IL3050`.
The script builds its arrays through the constructor.
