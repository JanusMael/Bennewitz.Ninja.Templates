# AGENTS.md — `scripts/`

File-based C# apps. Run every one with `dotnet run --file`: from this root, a bare
`dotnet run <file.cs>` binds to `Bennewitz.Ninja.Templates.csproj` instead. Each is compiled with
this repository's `Directory.Build.props`, so warnings are errors, and compiled trimmed.

| Script | What it does | Tested by |
|---|---|---|
| `verify-release.cs` | The release gate: pack, install from the `.nupkg`, generate, assert the tree, then build, test and pack the generated repository. `--published <version>` installs from nuget.org instead | CI's `verify` job runs it on every push |
| `assert-packages.cs` | The packed-versus-declared guard, run by this repository's release | `PackagingTests` covers the workflow that calls it |
| `repo-conventions.cs` | Checks and applies the family's repository conventions. **A copy**: the canonical file is `templates/bbpkg/scripts/repo-conventions.cs` | `RepoConventionsTests` |
| `mstest-to-xunit.cs` | Converts an MSTest suite to xUnit v3 by syntax tree, listing what it refuses to guess | `MstestToXunitTests` |
| `mstest-areequal-scan.cs` | Run BEFORE converting: finds the `AreEqual` calls a conversion would weaken, which only the compiler can see — a collection MSTest compares by reference and xUnit by elements. Builds an analyzer and injects it into the target's build; no scanned file changes | `MstestAreEqualScanTests` |

## Rules

| Rule | Why |
|---|---|
| **`repo-conventions.cs` is changed in `templates/bbpkg/scripts/` and copied here**, never edited here first | This repository's `check` fails on a copy that differs, and every family repository follows the template's copy |
| Build JSON with `System.Text.Json.Nodes`, and a `JsonArray` through its constructor | Reflection-based serialisation, a collection expression, or `Add(JsonObject)` binds to trim-unsafe overloads and fails the build with `IL2026` |
| A script's output is asserted on its content, never only on its exit code | A template that flattened still exits 0 at every step. `verify-release` exists because of that |
