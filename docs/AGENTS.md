# AGENTS.md — `docs/`

Documents for humans. None of them is packed; the package ships `templates/**` only.

| Document | What it is | Kept in step with |
|---|---|---|
| `repository-conventions.md` | **Prescriptive.** What every family repository carries and how it is checked | `templates/bbpkg/scripts/repo-conventions.cs`. The two change in the same commit |
| `layered-editors-package-split.md` | How the LayeredEditors libraries were divided into packages, derived from measured imports | A record of `plans/00002`; not updated as the packages evolve |
| `layered-editors-renames.md` | Every rename since that split, and why | Extended when a family package is renamed |
| `windows-defender-dev-exclusions.md` | A Windows-only runbook for excluding development paths from real-time scanning | Nothing in this repository; it stands alone |

## Rules

| Rule | Why |
|---|---|
| A rule in `repository-conventions.md` that `repo-conventions.cs` does not enforce is stated as a checklist step, not as a requirement the script checks | A reader must be able to tell what CI will catch from what only they can do |
| Records of past decisions are not rewritten to match the present | What was decided, and why, is the point of keeping them; drift is recorded in `PROGRESS.md` |
