# xunit.v3 4.x for the family

Measured on 2026-10-30 against `main` at `41c078b`. The family is on `xunit.v3` `3.2.2` with
`Microsoft.Testing.Extensions.TrxReport` `1.9.1`, everywhere: this repository's tests, all five
templates, and every family repository that tests.

## The versions

| Package | Today | 4.x line, newest |
|---|---|---|
| `xunit.v3` | `3.2.2`, on `xunit.v3.mtp-v1` | `4.0.2`, on `xunit.v3.mtp-v2` |
| Microsoft.Testing.Platform, as xunit takes it | `1.9.1` | `2.5.1` |
| `Microsoft.Testing.Extensions.TrxReport` | `1.9.1` | `2.5.1` to match; `2.5.2` requires the platform at `[2.5.2, 3.0.0)` |
| `xunit.analyzers`, as xunit takes it | `1.27.0` | `2.2.0` |

4.0 drops Microsoft.Testing.Platform v1. TrxReport has to stay on the platform version xunit takes,
which is the trap the pin's comment in `Directory.Packages.props` already describes: `4.0.2` pairs
with TrxReport `2.5.1`, not with the newest, `2.5.2`.

## What breaks

| Where | What | On 4.0.2 | The fix |
|---|---|---|---|
| `bbavalonia`, `tests/AppStem.Tests/Headless/HeadlessTestApp.cs` | `[assembly: CollectionBehavior(DisableTestParallelization = true)]` | Does not compile: `CS0619`, obsolete as an error | `[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]` |
| `scripts/mstest-to-xunit.cs` | Converts `[assembly: DoNotParallelize]` to the attribute above, and its fixture `Rules.expected.cs.txt` expects it | The converted suite does not compile | Emit the `Parallelization` form, and change the fixture |
| AppServices, ScopedEditors | The same attribute, in `tests/*/Parallelization.cs` | Does not compile | The same, when each moves |
| Inspections, DiffView | `Avalonia.Headless.XUnit` (`12.1.3`, `12.1.2`); the newest, `12.1.4`, declares `xunit.v3.extensibility.core [3.2.2, )` | Restores and builds, then every `[AvaloniaFact]` and `[AvaloniaTheory]` fails at discovery: `MissingMethodException` on `TestIntrospectionHelper.GetTestCaseDetails`, an overload 4.0 removed | Drive headless Avalonia without it, as below, or stay on `3.2.2` |

The attribute's replacement was measured, not read: with four tests in two classes, a build with no
attribute ran two at once; with `Parallelization(Mode = ParallelMode.None)`, one. JsonC's
`[assembly: CollectionBehavior(CollectionBehavior.CollectionPerClass)]` compiles and behaves as
before.

### Avalonia.Headless.XUnit

The failure is Avalonia's
[#22072](https://github.com/AvaloniaUI/Avalonia/issues/22072), open since 2026-08-26 and labelled a
feature: its maintainers do not raise the dependency, since a new major version of xunit would break
their users who have not moved, and mention shims for both versions as a possibility. On 2026-10-30
nothing was in progress: Avalonia's `master` pins `xunit.v3.extensibility.core` `3.2.2`, the
project last changed on 2026-02-22, and no pull request targets xunit 4.

The way through that does not wait for Avalonia is the one `bbavalonia`, AppServices and
ScopedEditors already take: plain `Avalonia.Headless` with a `HeadlessUnitTestSession` the tests
dispatch to, and no `Avalonia.Headless.XUnit`. The issue's thread gives a variant of it: no
`Avalonia.Headless.XUnit`, and headless Avalonia set up on a thread the tests own.

## What does not

- **This repository's tests**: 203 of 203 pass on `4.0.2` with TrxReport `2.5.1`, resolved as
  `xunit.v3.mtp-v2` `4.0.2` and Microsoft.Testing.Platform `2.5.1`, with no new analyzer finding
  under warnings as errors.
- **The generated repositories**: `verify-release` with every template moved fails `bbavalonia` on
  the attribute alone, and with the attribute changed passes all six, `bbavalonia` running 17 of 17
  tests, `HeadlessSessionTests` among them, which guards that its tests run one at a time.
- **The workflows**: every `dotnet test` in them is plain. None uses a report switch 4.0 renamed
  (`--report-junit`, `--report-nunit`, `--report-ctrf`, `--report-xunit`).
- **`global.json`'s `"runner": "Microsoft.Testing.Platform"`**, which 4.0 expects on the .NET 10 SDK.

## A question the measurement raised

The pins' comment says TrxReport "is what gives CI its .trx files", and every test project
references it. Nothing asks for one: no workflow, props file or project passes `--report-trx` or
sets it in `TestingPlatformCommandLineArguments`, so as far as this repository shows, no `.trx` is
written anywhere. Either the reference can go, which removes the version trap with it, or CI is
missing the switch.

## What a move takes

1. **Here, in one change**: `xunit.v3` `4.0.2` and TrxReport `2.5.1` in this repository's and every
   template's `Directory.Packages.props`, with the pins' comment updated; `bbavalonia`'s attribute;
   `mstest-to-xunit`'s conversion and its fixture. This repository's tests and the templates' stay on
   one framework, as the root `Directory.Packages.props` asks. Verified by the root tests and
   `verify-release`, both measured above.
2. **In each family repository, when it syncs**: the same two pins, and the attribute where it has
   one (AppServices, ScopedEditors). Nothing in `repo-conventions` checks the xunit version, so no
   repository fails for staying on `3.2.2`.
3. **Inspections and DiffView**: move their headless tests off `Avalonia.Headless.XUnit` first, or
   stay on `3.2.2` until Avalonia supports 4.x.
