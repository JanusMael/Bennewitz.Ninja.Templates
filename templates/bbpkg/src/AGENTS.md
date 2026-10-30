# AGENTS.md — `src/`

The shipped projects. Everything here becomes a package, so everything here is permanent once
released.

<!-- bbpkg: list the projects under src/ and what each one ships -->

## Rules

| Rule | Why | Guarded by |
|---|---|---|
| Every project is packable and its id is declared in `packages.push` or `packages.local` | An undeclared package is either published by accident or silently never published | `PackagingTests` |
<!--#if (Analyzer) -->
| `src/Directory.Build.props` imports the root props explicitly | MSBuild applies only the closest `Directory.Build.props`. Without the import, every project here silently loses the root's nullable settings and package metadata | the import line itself |
| The analyzer targets `netstandard2.0` and references only what the compiler host provides | netstandard2.0 is the one target both Visual Studio's MSBuild and the dotnet CLI load; a DLL the host does not provide is nowhere near the analyzer at run time, and the rule goes quiet as `AD0001` | `AnalyzerShapeTests`, off the compiled assembly |
| The DLL is packed under `analyzers/dotnet/cs` alone: `IncludeBuildOutput`, `SuppressDependenciesWhenPacking` and `DevelopmentDependency` stay as they are | Under `lib/` a consumer gets a runtime reference and the compiler never loads the analyzer | `AnalyzerShapeTests`, off the project file |
| The analyzer sets `IsTrimmable` and `EnableTrimAnalyzer` to `false` | The SDK warns `NETSDK1212` for both on netstandard2.0, and no consumer publishes an analyzer trimmed | the project file, comment; `AnalyzerShapeTests` |
| Every analyzer is internal, sealed, and derives from `ScopedAnalyzer<TScope>` | The compiler runs every analyzer in every compilation that references the package; the base's gate is what keeps one that has nothing to say from costing anything | `AnalyzerConventionTests` |
| Every rule's ID is `Catalog.Prefix` and four digits, and the prefix never changes once one has shipped | A consumer's suppressions name the IDs; a rename breaks every one of them | `RulesCatalogTests` |
| Every rule is in `AnalyzerReleases.Unshipped.md`, and moves to `Shipped.md` with the release that ships it | Release tracking (RS2008) fails the build otherwise | the build |
| Roslyn stays at the pin in `Directory.Packages.props` | It is the oldest compiler a consumer can use the package with | `Directory.Packages.props`, comment |
<!--#else -->
| `src/Directory.Build.props` imports the root props explicitly | MSBuild applies only the closest `Directory.Build.props`. Without the import, every project here silently loses the root's target framework, nullable settings and package metadata | the import line itself; `TrimmableTests` fails if the file stops applying |
| `IsTrimmable` and `EnableTrimAnalyzer` stay on | `IsTrimmable` is compiled into the assembly and travels in the package; the analyser runs here or nowhere, because no consumer's build analyses a packaged library | `TrimmableTests` reads the mark off the compiled assembly |
| `IsAotCompatible` stays off | It would also switch on the AOT and single-file analysers, a claim about these libraries that nothing has measured | `src/Directory.Build.props`, comment |
<!--#endif -->
| Assembly names are unprefixed; namespaces and package ids carry `Bennewitz.Ninja.` | The family's naming convention | the root `Directory.Build.props`, `RootNamespace` |
<!--#if (!Analyzer) -->

⚠ **The trim analyser cannot see compiled XAML.** A project with `.axaml` files needs an ILLink pass
as well; `src/Directory.Build.props` explains how.
<!--#endif -->
