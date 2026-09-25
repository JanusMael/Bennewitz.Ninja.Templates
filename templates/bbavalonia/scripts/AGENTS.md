# AGENTS.md — `scripts/`

File-based C# apps, run with `dotnet run --file`. Each is compiled with this repository's
`Directory.Build.props`, so warnings are errors here too.

| Script | What it does | Run by |
|---|---|---|
| `check-trim-warnings.cs` | Compares a trimmed publish's ILLink warnings with `src/AppStem/trim-warnings.txt`, failing on a change in either direction | CI's `trim` job, and the release for every platform |
| `repo-conventions.cs` | Checks, and applies, the family's repository conventions: documentation, build properties, GitHub settings and rulesets | CI's `conventions` job, the release, and the maintainer |

## Rules

| Rule | Why |
|---|---|
| **`repo-conventions.cs` is never edited here** | It is a copy of `templates/bbpkg/scripts/repo-conventions.cs` in Bennewitz.Ninja.Templates, identical in every family repository. Change it there and copy it back; run from that repository, `check --repo` reports every copy that differs |
| `check-trim-warnings.cs` compares warnings as a set of `ILxxxx: member: message`, with the location and project path removed | The same warning then compares equal on every machine and platform |
| A file-based app is compiled trimmed | Reflection-based serialisation fails the build with `IL2026`. Build JSON with `System.Text.Json.Nodes`, and a `JsonArray` through its constructor |
