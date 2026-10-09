# AGENTS.md — `scripts/`

File-based C# apps, run with `dotnet run --file`. Each is compiled with this repository's
`Directory.Build.props`, so warnings are errors here too.

| Script | What it does | Run by |
|---|---|---|
| `repo-conventions.cs` | Checks, and applies, the family's repository conventions: documentation, build properties, GitHub settings and rulesets | CI's `conventions` job, the release, and the maintainer |

## Rules

| Rule | Why |
|---|---|
| **`repo-conventions.cs` is never edited here** | It is a copy of `templates/bbpkg/scripts/repo-conventions.cs` in Bennewitz.Ninja.Templates, identical in every family repository. Change it there and copy it back; run from that repository, `check --repo` reports every copy that differs |
| A file-based app is compiled trimmed | Reflection-based serialisation fails the build with `IL2026`. Build JSON with `System.Text.Json.Nodes`, and a `JsonArray` through its constructor |
