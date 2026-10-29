# AGENTS.md — `scripts/`

File-based C# apps, run with `dotnet run`. Each is compiled with this repository's
`Directory.Build.props`, so warnings are errors here too.

| Script | What it does | Run by |
|---|---|---|
| `assert-packages.cs` | Checks that the packages `dotnet pack` actually produced are exactly the ones the two package lists declare; with `--family`, exactly that family's ids, each carrying the family's README, `docs/<family>.md` | CI's `pack` job, and the release before it publishes |
| `release-target.cs` | Resolves what a release publishes: the tag or the dispatch, into the version, the tag, the family where `.github/repository.json` declares families, and the ids. Refuses a tag of the wrong kind and a version that is not `YYYY.Q.MMDD`. `projects` lists a family's projects for its pack; `notes` writes a family's release notes from its own commits | The release, first, before anything is built |
| `repo-conventions.cs` | Checks, and applies, the family's repository conventions: documentation, GitHub settings and rulesets | CI's `conventions` job, the release preflight, and the maintainer |

## Rules

| Rule | Why |
|---|---|
| `assert-packages.cs` reads each id from the `.nuspec` inside the package, never from the file name | `<id>.<version>.nupkg` cannot be split reliably: nothing separates an id ending in `.Widget` from one ending in `.Widget.2026` |
| What a release publishes is decided in `release-target.cs`, never in the workflow's bash | The workflow's YAML first runs at a real tag, past which a version is permanent; the script is tested in Bennewitz.Ninja.Templates, where it is changed |
| **`repo-conventions.cs` is never edited here** | It is a copy of `templates/bbpkg/scripts/repo-conventions.cs` in Bennewitz.Ninja.Templates, identical in every family repository. Change it there and copy it back; run from that repository, `check --repo` reports every copy that differs |
| A file-based app is compiled trimmed | Reflection-based serialisation fails the build with `IL2026`. Build JSON with `System.Text.Json.Nodes`, and a `JsonArray` through its constructor |
