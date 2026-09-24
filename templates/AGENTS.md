# AGENTS.md — `templates/`

⛔ **Everything beneath this directory is shipped content, not this repository's own files.**
`templates/bbpkg/` becomes somebody else's repository. The `AGENTS.md`, `CLAUDE.md`, `PROGRESS.md`
and `README.md` inside it describe that generated repository, and its workflows never run here.
A tool that loads `templates/bbpkg/AGENTS.md` while working in this directory is reading
instructions for a generated repository, not for this one.

| Path | What it is |
|---|---|
| `bbpkg/` | The only template. `.template.config/template.json` defines its short name, `bbpkg`, and its parameters |

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| `PkgStem`, `PKG_ID` and `REPO_OWNER` are placeholders, substituted at generation | Written literally anywhere under `bbpkg/`, they become the generated repository's names | `verify-release`, placeholder check |
| The `<!-- bbpkg: … -->` markers are intended | They are what the generated repository's `check` reports until someone replaces them. This repository's `repository.json` lists `templates/bbpkg` under `content`, so its own `check` skips them | `repo-conventions check` |
| A file every generated repository must have is on `verify-release`'s required list | Removed from the template, it would ship nowhere, and nothing else fails | `verify-release`, `required` |
| `bbpkg/scripts/repo-conventions.cs` is the canonical copy for the whole family | Every other copy, this repository's included, is compared against it | `repo-conventions check --repo` |
| The shipped workflows are tested by path | Actions never runs them in this repository, so nothing else would notice one rotting | `PackagingTests`, `ConventionsWorkflowTests` |
| Nothing under `bbpkg/` is built from here | The packaging project compiles nothing; `verify-release` builds a generated copy instead | `Bennewitz.Ninja.Templates.csproj`, `Compile Remove` |
