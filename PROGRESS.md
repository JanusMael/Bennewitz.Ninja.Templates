# Progress

Work state for `Bennewitz.Ninja.PackageTemplate`. Plan: [plans/00001-package-template.md](plans/00001-package-template.md),
approved 2026-09-20 and frozen — drift from it is recorded here, never edited into it.

## Status

| Step | State | Notes |
|---|---|---|
| 1 · Repo + approved plan | **done** | `ca2552f`, the plan alone |
| 2 · Packaging project + skeleton | **done** | Packs clean; layout verified inside the `.nupkg` |
| 3 · `template.json` | **done** | Install-from-nupkg → generate → names verified |
| 4 · Packaging tests | next | Generalise from `Bennewitz.Ninja.DiffView/tests/.../PackagingTests.cs` |
| 5 · Both CI and release workflows | — | |
| 6 · `package-release` skill, in `bb-skills` | — | Proposed as `*.proposed` before it installs |
| 7 · `verify-release` | — | |
| 8 · First release | — | |

## Drift from the approved plan

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

## Next

Step 4. `Bennewitz.Ninja.DiffView/tests/DiffView.Avalonia.Tests/PackagingTests.cs` already reads ids
from the `.nuspec` inside each `.nupkg` and asserts the release workflow names its pushes rather than
globbing — generalise both, then add the `packed == push ∪ local` assertion over the two list files.

⚠ The tests must run against **both** release workflows: this repo's own, and the one shipped inside
`templates/bbpkg/.github/workflows/`, which GitHub Actions never executes from here.
