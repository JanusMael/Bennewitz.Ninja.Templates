# 00006 — Solution-wide friend grants

> Status: **approved 2026-09-28**. Supersedes nothing. Extends
> [`00003`](00003-repository-conventions.md) and [`00004`](00004-standard-build-properties.md).

Family repositories grant `InternalsVisibleTo` project by project, and several document a rule of
"grants to tests only". That rule costs more than it protects. A seam between two projects of one
solution becomes public API for want of a grant, and a test that needs an internal of a second
project cannot reach it. None of the grants is a security boundary either, since no family assembly
is strong-named. This plan makes every project of a solution a friend of every other and generates
the list, so it cannot drift from the solution.

## Prior art

ClaudeForge consolidated its grants on 2026-09-14 (`053306a7`). `AssemblyInfo.InternalsVisibleTo.cs` sits beside
`ClaudeForge.slnx`, and every csproj links it by a relative, forward-slashed path.
`SharedFriendGrantsTests` checks four things: the file exists, every project links it, the link path
is portable, and no project declares a grant of its own in either spelling. Its `CLAUDE.md`,
lines 77–93, says what that costs: `internal` now means solution-internal, and a grant names an
**assembly**, not a root namespace. A grant naming `Bennewitz.Ninja.X` where the assembly is `X`
compiles, ships and grants nothing.

This plan keeps that shape and changes two things. The list is **generated** from the solution
rather than written by hand, and `Directory.Build.targets` links it rather than each csproj.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **Every project of a solution grants `InternalsVisibleTo` to every assembly the solution builds**, tests included; grants between product projects are encouraged | `internal` then means solution-internal. A member no other assembly may reach is `private`. One list that is obviously complete replaces many that are individually precise and collectively unknowable, which is ClaudeForge's finding |
| 2 | **The list is generated**: `AssemblyInfo.InternalsVisibleTo.cs` at the repository root, one `[assembly: InternalsVisibleTo("…")]` per assembly, from the evaluated `AssemblyName` of every project `check` evaluates (the solution's, or every csproj outside `content` where there is no solution, skipping the *template* role, as `00004`'s property checks do), sorted ordinally so every machine writes the same bytes, with a header saying it is generated and by what. A project lists itself, which compiles: ClaudeForge's `AgentForge.Core` links a file that names it | A hand-kept list drifts the day a project is added or renamed. The evaluated `AssemblyName` is what the compiler matches, which rules out the root-namespace mistake by construction |
| 3 | **`repo-conventions.cs` generates it**, with a new verb, `grants`. Every `check` that runs on a checkout (token, admin, release and offline) regenerates the list in memory and fails on a difference, line endings aside. `check --repo` notes that it cannot, as it does for `00004`'s properties | The script already evaluates every project's properties for `00004`, and every family repository already carries one drift-checked copy. A second script would be a second copy to keep in step. Through the API there is nothing to evaluate |
| 4 | **`AssemblyInfo.InternalsVisibleTo.External.cs`**, beside it, is written by hand and never touched by the generator. It holds grants to the maintainer's other repositories, and it always exists: a template ships it with a comment and no grants, and `check` fails without it. `check` also fails on an External name that starts with `Bennewitz.Ninja.` | Cross-repository grants are allowed and encouraged where they reduce friction, but they are not derivable from the solution, so they need a file of their own that the generator cannot overwrite. Linking a missing file breaks the build. The External file is the one place left where a grant can name a root namespace instead of an assembly; BNAQ1005 cannot catch that, because it allows the External file's names by design (AssemblyQuality's `PROGRESS.md`), and family assemblies are unprefixed |
| 5 | **The root `Directory.Build.targets` links both files** into every project, by `$(MSBuildThisFileDirectory)`, never copied, unless the project sets `<SolutionFriendGrants>false</SolutionFriendGrants>`, is in the *template* role, or is a file-based app under `scripts/`. `check` evaluates each project's `Compile` items and fails when a project that has not opted out does not compile both files | Targets, not props, so the link lands after the project's own items and a `Compile Remove` glob in the csproj cannot silently drop it; the opt-out is the one way out, and it sits in the csproj it applies to. File-based apps are compiled with `Directory.Build.*` (`verify-release.cs` says the shipped script compiles under a generated repository's warnings as errors); linked, a missing or broken grant file would stop `repo-conventions.cs` itself compiling, so `grants` could never repair it. The *template* role compiles nothing: this repository's packaging project removes every `Compile` item. MSBuild imports only the nearest `Directory.Build.targets`, so a nested one would silently cut its projects out of the grants; the file's content cannot show that, only the evaluated items can. None of the five checked on 2026-09-28 has a `Directory.Build.targets` (AppServices, FileServer, ScopedEditors, AssemblyQuality, XamlQuality), so each adding the convention adds one |
| 6 | **An opted-out project links neither file and may declare grants of its own, and it still receives grants**: it stays in the generated list | Opting out is about what a project exposes, not about what it may call. A project that needs grants the scheme would hide is already visibly outside it: AssemblyQuality's BNAQ1005 and BNAQ1006 tests need fixture projects whose grants are deliberately wrong or withdrawn (`tests/fixtures/Absent` and `tests/fixtures/Orphan`, both listed in `AssemblyQuality.slnx`), and the shared file would grant them everything |
| 7 | **No project that has not opted out declares a grant of its own**, in any spelling: the SDK `<InternalsVisibleTo>` item, an `<AssemblyAttribute>` for it, or `[assembly: InternalsVisibleTo(…)]` in any file a project compiles other than the two above. `check` reads all three from each project's evaluated items (`InternalsVisibleTo`, `AssemblyAttribute`, and the `Compile` items it then reads), never from a text scan of the tree, and fails on each | Two sources of grants is how ClaudeForge's per-project lists became unknowable. The External file is the one place for a grant the generator cannot derive. BNAQ1005 would not catch a stray grant naming an allowed assembly. A text scan of the tree would fail on the generator's own source, on test fixtures, and on the files a template ships |
| 8 | **No strong-name signing, and the convention says why.** A grant matches by assembly name. On modern .NET, public signing satisfies a keyed grant without the private key, so neither form is a security boundary. Grants name only assemblies the family ships, and public consumers get none. Family assembly names are unprefixed, so a public project that happens to share one, such as `CodeQuality`, would receive its grants: the maintainer accepts that risk (2026-09-28), and the convention says so. Someone outside the family who generates a repository from a template gets the same scheme within their own solution, with an empty External file; no family internal is visible to them | A convention that implied the grants protect something would mislead whoever reads it. Signing would cost every repository key management for no protection |
| 9 | **An internal that another repository uses is an informal promise**: its owner does not change it without releasing a fixed consumer. **The guard runs at the provider's release, not in the consumer's tests** (the maintainer's decision, 2026-09-28): the provider's tests load the already-published consumer assemblies beside its new build and run AssemblyQuality's BNAQ1006, which checks every internal they reference still resolves | A consumer compiles against the provider version it references, so a vanished internal already fails the consumer's own build; BNAQ1006 there could almost never fail. Skew breaks only a downstream app that resolves a newer provider under a consumer built against an older one, which only the provider's release can see coming. How the provider finds and loads its consumers (AssemblyQuality proposes `<PackageDownload>` of each consumer package and an `AssemblyLoadContext`) is being proven in AssemblyQuality's `plans/00001`; the convention for it is a later plan |
| 10 | **Existing repositories adopt it when they next take the conventions script**, not in a sweep. AppServices, FileServer and ScopedEditors rewrite their "grants to tests only" rule in that same change. A repository not ready to convert a project sets `SolutionFriendGrants` to `false` on it, keeping its own grants, and converts it later; there is no repository-wide switch (the maintainer's decision, 2026-09-28) | Taking the new script is what makes `check` require it, so adoption and the rule change land together. The opt-out of decision 6 already lets a project keep its grants, visibly outside the scheme, so a second mechanism would only let whole repositories sit unconverted |
| 11 | **AssemblyQuality's BNAQ1005**, which checks that every compiled grant names an allowed assembly, is called from `bbavalonia`'s `AssemblyQualityTests` once it ships. BNAQ1006 follows | `bbavalonia` is the only template that references AssemblyQuality. `bbpkg`, `bbweb` and `bbapi` gain it only if a later plan adds AssemblyQuality to them |

### Dismissed

- **ClaudeForge's hand-kept list.** It is complete only as long as someone remembers. The generator
  and its drift check make it complete by construction.
- **A link in each csproj.** That is one line per project to forget, and a test to catch the
  forgetting. `Directory.Build.targets` reaches every project with nothing to forget.
- **Grants to tests only.** That is the current rule, and the reason a seam between two product
  projects becomes public API.
- **Strong-name signing.** It gives no protection on modern .NET (decision 8) and costs every
  repository a key.
- **A per-project list of grantees.** It is the precision ClaudeForge gave up, for the same reason.
- **BNAQ1006 in the consumer's tests.** The consumer's compiler already catches what it would
  (decision 9).

## Scope

**In:** the generator and the check in `repo-conventions.cs`; the linking in every template's root
`Directory.Build.targets`; the two files in every template, generated for the template's own projects;
`docs/repository-conventions.md`; this repository adopting it; `verify-release` proving a generated
repository's list is current; `bbavalonia` calling BNAQ1005 once AssemblyQuality ships it.

**Out:** moving any other repository over (each on its next sync, decision 10); BNAQ1005 and BNAQ1006
themselves, which are AssemblyQuality's; the provider-side release check of decision 9, which waits
for AssemblyQuality's spike and gets a plan of its own; any change to which types are `internal`.

## Steps

| # | Step | Verified by |
|---|---|---|
| 1 | One change, as this repository requires of the conventions: `docs/repository-conventions.md` gains the convention (decisions 1–10, the opt-out, the two files, decision 8's paragraph), and `repo-conventions.cs` gains `grants` and the checks of decisions 3, 4, 5 and 7 | `RepoConventionsTests`, each against a fixture that starts green: a third project added without regenerating (fails); the list differing only in line endings (passes); the External file deleted (fails, and `repo-conventions.cs` still compiles and says so); an External name starting with `Bennewitz.Ninja.` (fails); a project whose nested `Directory.Build.targets` drops the link (fails); the same project opted out (passes); an opted-out project declaring a grant of its own (passes, and it is still in the list); an `<InternalsVisibleTo>` item, an `<AssemblyAttribute>` grant and an `[assembly: InternalsVisibleTo]` in a compiled source file (each fails); the generator's own source and a fixture containing that text, compiled by no project (pass); a project named differently from its assembly (the list carries the assembly name); a solution-less repository (lists every csproj outside `content`). The MSBuild property that marks a file-based app is measured, not assumed |
| 2 | Every template's root `Directory.Build.targets` links both files, as decision 5 says, and each template ships both files, the generated one written for its own projects under the stem placeholder. Both go on `verify-release`'s required list | `verify-release` passes, every generated repository building with warnings as errors, which a clash between two projects' internal types (CS0436) would fail. In a repository generated from each template, a planted `internal` member of the product project, called from a planted test, compiles; with `SolutionFriendGrants` set to `false` on the product project, it does not. Nothing planted ships |
| 3 | `verify-release`: the offline `check` of each generated repository covers the grant list, so the template's copy must be current after substitution | With a project added to a generated repository and the list not regenerated, its `check` fails, and `verify-release` with it |
| 4 | This repository adopts it: a root `Directory.Build.targets`, which it has none of today, the two files, and the script copy. The packaging project, in the *template* role, links neither | This repository's `conventions` job is green, and the packaging project still packs no compiled output; `check --repo` lists every family repository whose copy is now behind |
| 5 | When AssemblyQuality ships BNAQ1005, `bbavalonia`'s `AssemblyQualityTests` calls it, passing the solution's assemblies and the External file's names. Steps 1–4 do not wait for it | A generated `bbavalonia` passes. With a grant to an assembly nobody ships planted anywhere but the two files, as a compiled attribute the conventions check is not run on, BNAQ1005 fails. A name planted in the External file is allowed by BNAQ1005's design; decision 4's `check` covers that file |
