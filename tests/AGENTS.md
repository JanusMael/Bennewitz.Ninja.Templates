# AGENTS.md — `tests/`

One project, `Templates.Tests`, which reads this repository's files and runs its scripts. It has
no `ProjectReference`: the packaging project compiles nothing, so there is no assembly to test.

| Folder | What it guards |
|---|---|
| `Packaging/` | Both package lists and both release workflows, this repository's and the shipped one, read by path |
| `RepoConventions/` | `scripts/repo-conventions.cs`, run over a repository built in a temp directory with GitHub's answers as fixtures, and the conventions steps in the shipped workflows |
| `MstestToXunit/` | `scripts/mstest-to-xunit.cs`, run over fixtures whose expected output was read line by line |

## Rules

| Rule | Why |
|---|---|
| Fixtures for GitHub's answers are written as GitHub returns them, never generated from the script under test | Generated, they agree with the script by construction and cannot catch it being wrong |
| A test that runs a script runs the program, with the arguments a person passes, and asserts on its output | The scripts are the product; a test of their internals would pass on a script that no longer runs |
| `repo-conventions.cs` is built once per test run, `--no-incremental`, and launched as a program through `ConventionsScript`; one test still runs it with `dotnet run --file` | `dotnet run --file` costs about 0.5 s a launch, and parallel builds of one file contend for its output, which forced every conventions class into one serial collection. A forced build also reads the file as it is, where the file-based cache trusts timestamps and ran a stale build under a canary |
| Tests that run the same script with `dotnet run --file` stay in one class | xUnit runs classes in parallel, and parallel builds of one file-based app contend for its output |
| A new guard is proven by planting the defect it catches and watching it fail | A test that has never failed has not been shown to test anything. `PROGRESS.md` records each round |

`tests/Directory.Build.props` imports the root props explicitly and makes the project an xUnit v3
test executable on Microsoft.Testing.Platform.
