# AGENTS.md — `templates/`

⛔ **Everything beneath this directory is shipped content, not this repository's own files.**
`templates/bbpkg/` becomes somebody else's repository. The `AGENTS.md`, `CLAUDE.md`, `PROGRESS.md`
and `README.md` inside it describe that generated repository, and its workflows never run here.
A tool that loads `templates/bbpkg/AGENTS.md` while working in this directory is reading
instructions for a generated repository, not for this one.

| Path | What it is |
|---|---|
| `bbpkg/` | A repository that publishes NuGet packages. Each template's `.template.config/template.json` defines its short name and its parameters |
| `bbavalonia/` | An Avalonia desktop app, trimmed and single-file |
| `bbweb/` | A Kestrel web site on MVC views; `--blazor` adds interactive server components |
| `bbapi/` | A Kestrel API on minimal APIs, compiled with native AOT |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| Each template's stem (`PkgStem`, `AppStem`, `SiteStem`, `ApiStem`, in either case), `PKG_ID`, `REPO_OWNER` and `REPO_NAME` are placeholders, substituted at generation | Written literally anywhere under a template, they become the generated repository's names | `verify-release`, placeholder check, in every template |
| The `<!-- bbpkg: … -->` markers are intended | They are what the generated repository's `check` reports until someone replaces them. This repository's `repository.json` lists `templates/bbpkg` under `content`, so its own `check` skips them | `repo-conventions check` |
| A file every generated repository must have is on `verify-release`'s required list for its template | Removed from the template, it would ship nowhere, and nothing else fails | `verify-release`, `Required` per case |
| Every template has a case in `verify-release` | A template without one would ship without ever being generated, built or tested | `verify-release` fails when `templates/` and its cases disagree |
| `bbpkg/scripts/repo-conventions.cs` is the canonical copy for the whole family | Every other copy, this repository's included, is compared against it | `repo-conventions check --repo` |
| The shipped workflows are tested by path | Actions never runs them in this repository, so nothing else would notice one rotting | `PackagingTests`, `ConventionsWorkflowTests` |
| Nothing under `bbpkg/` is built from here | The packaging project compiles nothing; `verify-release` builds a generated copy instead | `Bennewitz.Ninja.Templates.csproj`, `Compile Remove` |
