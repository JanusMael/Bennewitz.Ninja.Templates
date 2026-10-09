# 00008 — The trimming enforcement

> Status: **approved 2026-10-09**. Supersedes nothing.

Trimming became a family policy on 2026-09-28, decided with XamlQuality: a repository that trims sets
`IsTrimmable` and `EnableTrimAnalyzer` on its libraries, every family library it depends on, transitively,
must be trimmable too, and with neither property set nothing is enforced. The conventions checker is to
enforce both sides. Side 1, the repository's own libraries, is this repository's to write; side 2, every
`Bennewitz.Ninja.*` package in a trimming repository's restore graph, is XamlQuality's, sent as a patch
against the canonical script (the split confirmed 2026-09-29). This plan is side 1, and where side 2 lands.

## What exists today

| Where | What it does |
|---|---|
| `Props.Check` in `repo-conventions.cs` | A library without both properties is a NOTE, and a FAIL once `repository.json` says `"trimming": "required"`. Apps and tools are not held to it. A project can be exempted from the `Trimming` rule, with its reason |
| `repository.json`, `"trimming"` | `"required"` in all five templates and in AppServices, ScopedEditors, AssemblyQuality, JsonC and CodeQuality; absent in Templates, XamlQuality, AutoVersioning and FileServer (surveyed 2026-10-09) |
| `docs/repository-conventions.md` | "Trimming is a stage": the NOTE, then the FAIL, by the key |
| `bbpkg`'s `src/Directory.Build.props` | Sets both properties for every library under `src/` |

So today trimming is *declared*, by a key, while the policy says it is *evident*, from the projects.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **A repository trims when any of its projects evaluates `IsTrimmable`, `EnableTrimAnalyzer`, `PublishTrimmed` or `PublishAot` to `true`**, read from the evaluation the checker already makes | The policy keys enforcement to the properties, not to a declaration. An app published trimmed or natively trims too: its own libraries are inside that publish, and under `TrimMode=partial` an unmarked one is silently left whole |
| 2 | **When a repository trims, every library must evaluate both properties to `true`, or it FAILS.** When it does not trim, nothing is reported, not even a NOTE | The policy's two halves, exactly. The NOTE on a repository that does not trim is noise the policy rules out |
| 3 | **Libraries only**, as today. Apps, tools, tests, analyzers and templates are not held to it | A tool runs on the installed framework; an app's trimming is decided by its kind (`bbweb` deliberately does not trim); an analyzer runs in the compiler |
| 4 | **The per-project `Trimming` exemption stays**, with its reason, and a stale one is still reported | A library that truly cannot be trimmable yet needs an honest way out that does not hide the next one |
| 5 | **`"trimming"` is retired from `repository.json`.** The checker reports a key still present as a NOTE ("no longer read; remove it"), never a FAIL, and the templates stop shipping it | Two sources of truth, the key and the properties, can disagree; the properties are what a publish obeys. A NOTE lets each family repository drop the key on its next sync without going red |
| 6 | **One predicate, `Trims`, is what both sides call.** Side 2 checks the restore graph only when it is true, so the two halves cannot disagree about whether a repository trims | Side 2 is written elsewhere; sharing the predicate is the contract between the two patches |
| 7 | **Side 2 lands in this plan when XamlQuality's patch arrives, after side 1, and does not block the next release** (the maintainer, 2026-10-09: the release waits for side 1) | The two halves are independent once the predicate exists |

### Dismissed

- **Keeping `"trimming": "required"` as an override that forces enforcement.** A repository that wants it
  sets the properties, which is the same act and also what makes the publish analyse its libraries.
- **Failing a library that sets one property but not the other, whether or not the repository trims.**
  Decision 1 already makes it a trimming repository, so decision 2 fails it; a separate rule would say
  the same thing twice.
- **Making the retired key a FAIL.** Every family repository that carries it would go red on its next
  sync for a key that now means nothing.

## Scope

**In:** the predicate and the library rule in the canonical `repo-conventions.cs`, its copies here and in
every template; the retired key's NOTE; `docs/repository-conventions.md`'s trimming section and its
`repository.json` table; the templates' `repository.json`; tests; and, when it arrives, XamlQuality's
side 2 behind the same predicate.

**Out:** side 2's design, which is XamlQuality's; removing the key from each family repository, which
each does on its next sync, prompted by the NOTE and by the post-release note; any change to what an
app template trims.

## Steps

| # | Step | Verified by |
|---|---|---|
| 1 | The predicate and the rule (decisions 1–4) and the retired key's NOTE (decision 5) in the canonical script, with the documentation, copied to `scripts/` and every template | `PropsTests` fixtures, each failing against today's script: a repository trimming through a library's `IsTrimmable`, through an app's `PublishTrimmed` and through `PublishAot`, each failing an unmarked library; one trimming through nothing, reporting no trimming finding at all; an exemption silencing the FAIL, and reported once unneeded; the key present giving the NOTE. `TemplateCopiesTests` passes. This repository's own `check` conforms |
| 2 | The templates drop `"trimming"` from `repository.json` | `verify-release` passes, every generated repository's offline check showing only the intended gaps. Canaried: with `IsTrimmable` removed from `bbpkg`'s `src/Directory.Build.props`, `verify-release` fails on the generated `bbpkg`'s trimming FAIL |
| 3 | Side 2, XamlQuality's patch, behind `Trims` (decisions 6 and 7) | XamlQuality's own fixture fails on an untrimmable `Bennewitz.Ninja.*` package in a trimming repository's restore graph, and the same graph in a repository that does not trim reports nothing |
