#!/usr/bin/env dotnet
// Finds the MSTest Assert.AreEqual/AreNotEqual calls whose meaning changes under xUnit v3, which
// only the compiler can tell: MSTest compares by Equals, so a collection that does not override it
// is compared by REFERENCE; xUnit's Equal compares collections by their ELEMENTS. A converted
// AreEqual on such a value still passes, and no longer notices code that returns a copy.
//
//   dotnet run --file scripts/mstest-areequal-scan.cs -- <solution-or-project> [--configuration <c>]
//
// Run it over the MSTest tree, BEFORE converting. It builds an analyzer, then builds the target with
// that analyzer injected through CustomAfterMicrosoftCommonTargets, so no scanned file changes.
//
// ⓘ MSTest 4.3.3's own MSTEST0065 flags AreEqual on a statically-typed IEnumerable, so on that
// version a plain build already finds the collection cases. This scan adds what it does not: the
// sites where both sides are object/interface-typed, which it lists as OPAQUE; suites on an MSTest
// without that rule, or with it suppressed; and one exit-coded report for a whole solution.
//
// Exit codes: 0 nothing needs a look · 2 sites listed as NEEDS A LOOK · 3 the scan could not run
// (a failed build, or no AreEqual seen at all — indistinguishable from an analyzer that never loaded).

using System.Diagnostics;
using System.Text.RegularExpressions;

if (args.Length < 1 || args[0] is "-h" or "--help")
{
    Console.WriteLine("usage: dotnet run --file scripts/mstest-areequal-scan.cs -- <solution-or-project> [--configuration <c>]");
    return args.Length < 1 ? 3 : 0;
}

string target = Path.GetFullPath(args[0]);
string configuration = "Debug";
for (int i = 1; i < args.Length; i++)
{
    if (args[i] == "--configuration" && i + 1 < args.Length)
    {
        configuration = args[++i];
    }
    else
    {
        Console.Error.WriteLine($"unknown argument: {args[i]}");
        return 3;
    }
}
if (!File.Exists(target))
{
    Console.Error.WriteLine($"not found: {target}");
    return 3;
}

string work = Path.Combine(Path.GetTempPath(), "mstest-areequal-scan-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(work);
try
{
    File.WriteAllText(Path.Combine(work, "AreEqualScan.csproj"), Scan.AnalyzerProject);
    File.WriteAllText(Path.Combine(work, "AreEqualScanAnalyzer.cs"), Scan.AnalyzerSource);
    (int analyzerExit, string analyzerOutput) = Scan.Dotnet(work, "build", "AreEqualScan.csproj", "-c", "Release", "-tl:off");
    string analyzer = Path.Combine(work, "bin", "Release", "netstandard2.0", "AreEqualScan.dll");
    if (analyzerExit != 0 || !File.Exists(analyzer))
    {
        Console.Error.WriteLine(analyzerOutput);
        Console.Error.WriteLine("the scan analyzer did not build");
        return 3;
    }

    string inject = Path.Combine(work, "inject.targets");
    File.WriteAllText(inject, Scan.InjectTargets.Replace("{ANALYZER}", analyzer, StringComparison.Ordinal));

    // --no-incremental: an up-to-date project skips the compiler, and with it every diagnostic.
    (int buildExit, string buildOutput) = Scan.Dotnet(Path.GetDirectoryName(target)!,
        "build", target, "-c", configuration, "--no-incremental", "-tl:off", "-clp:NoSummary",
        "-p:CustomAfterMicrosoftCommonTargets=" + inject);
    if (buildExit != 0)
    {
        Console.Error.WriteLine(buildOutput);
        Console.Error.WriteLine($"the target did not build (exit {buildExit}); nothing can be concluded");
        return 3;
    }

    // Keyed by location: MSBuild can print one diagnostic more than once.
    var byLocation = new Dictionary<(string Path, int Line, int Column), string[]>();
    string root = Path.GetDirectoryName(target)! + Path.DirectorySeparatorChar;
    foreach (Match m in Scan.Diagnostic().Matches(buildOutput))
    {
        string path = m.Groups["path"].Value;
        if (path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            path = path[root.Length..];
        }
        byLocation[(path.Replace('\\', '/'), int.Parse(m.Groups["line"].Value), int.Parse(m.Groups["col"].Value))] =
            m.Groups["msg"].Value.Split('|');
    }
    var sites = byLocation
        .OrderBy(s => s.Key.Path, StringComparer.Ordinal).ThenBy(s => s.Key.Line).ThenBy(s => s.Key.Column)
        .Select(s => (Site: $"{s.Key.Path}:{s.Key.Line}:{s.Key.Column}", Value: s.Value))
        .ToList();

    if (sites.Count == 0)
    {
        Console.Error.WriteLine("no Assert.AreEqual or AreNotEqual seen: not an MSTest tree, or the analyzer never ran");
        return 3;
    }

    Console.WriteLine($"{sites.Count} AreEqual/AreNotEqual sites");
    foreach (var group in sites.Select(s => s.Value).GroupBy(v => $"{v[0],-15} {v[1]}").OrderBy(g => g.Key, StringComparer.Ordinal))
    {
        Console.WriteLine($"  {group.Key,-28} {group.Count()}");
    }

    // AreNotEqual on a by-reference collection gets STRICTER under xUnit, which can only show as a
    // visible failure, so only AreEqual is a finding. A by-reference collection and an opaque value
    // are both findings: the first is weakened, the second might be.
    var findings = sites.Where(s => s.Value[1] == "AreEqual" && s.Value[0] is "COLLECTION-REF" or "COLLECTION-EQ" or "OPAQUE").ToList();    if (findings.Count == 0)
    {
        Console.WriteLine("NEEDS A LOOK: none");
        return 0;
    }
    foreach (var (site, v) in findings)
    {
        Console.WriteLine($"NEEDS A LOOK {site}: {v[0]} {v[1]} bound={v[2]} expected={v[3]} actual={v[4]}");
    }
    return 2;
}
finally
{
    Directory.Delete(work, recursive: true);
}

internal static partial class Scan
{
    // "path(line,col): warning AEQSCAN: message [project.csproj]". The message can contain "[" (int[]),
    // so it is anchored on the trailing project, never cut at the first bracket.
    [GeneratedRegex(@"^\s*(?<path>\S.*?)\((?<line>\d+),(?<col>\d+)\): warning AEQSCAN: (?<msg>.*?)\s+\[[^\[\]]*proj\]\s*$", RegexOptions.Multiline)]
    public static partial Regex Diagnostic();

    public static (int ExitCode, string Output) Dotnet(string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo start = new("dotnet") { WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        using Process process = Process.Start(start)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, stdout.Result + stderr.Result);
    }

    // ⚠ An analyzer must reference a Roslyn NO NEWER than the compiler that loads it, or it is
    // silently skipped (CS8032). 4.14 is old enough for every .NET 10 SDK.
    public const string AnalyzerProject = """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>netstandard2.0</TargetFramework>
            <LangVersion>latest</LangVersion>
            <Nullable>enable</Nullable>
            <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
            <IsRoslynComponent>true</IsRoslynComponent>
            <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
            <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>
            <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>
            <NoWarn>$(NoWarn);RS2008</NoWarn>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.14.0" PrivateAssets="all" />
          </ItemGroup>
        </Project>
        """;

    // Imported last by every project in the build. The property is read when the compiler runs, after
    // evaluation, so appending here keeps the scan's warning a warning under TreatWarningsAsErrors.
    public const string InjectTargets = """
        <Project>
          <PropertyGroup>
            <WarningsNotAsErrors>$(WarningsNotAsErrors);AEQSCAN</WarningsNotAsErrors>
          </PropertyGroup>
          <ItemGroup>
            <Analyzer Include="{ANALYZER}" />
          </ItemGroup>
        </Project>
        """;

    public const string AnalyzerSource = """
        using System.Collections.Immutable;
        using System.Linq;
        using Microsoft.CodeAnalysis;
        using Microsoft.CodeAnalysis.Diagnostics;
        using Microsoft.CodeAnalysis.Operations;

        namespace AreEqualScan;

        /// <summary>
        /// Reports every MSTest Assert.AreEqual / AreNotEqual as KIND|Method|Bound|Expected|Actual.
        ///   COLLECTION-REF  compares a non-string IEnumerable that does not override Equals: by reference
        ///   COLLECTION-EQ   compares a non-string IEnumerable that overrides Equals: by that override
        ///   OPAQUE          object / interface / type parameter on both sides: the runtime type decides
        ///   SCALAR          anything else
        /// A concrete BOUND type is what MSTest compares, so it decides. Only an opaque bound type
        /// looks at the arguments: a collection argument makes it a collection comparison, and a
        /// value-type or string argument makes it scalar, because Equals cannot then pass for anything
        /// but an equal scalar.
        /// </summary>
        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        public sealed class AreEqualScanAnalyzer : DiagnosticAnalyzer
        {
            private static readonly DiagnosticDescriptor Rule = new(
                "AEQSCAN", "AreEqual scan", "{0}", "Scan", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

            public override void Initialize(AnalysisContext context)
            {
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
                context.EnableConcurrentExecution();
                context.RegisterOperationAction(Analyze, OperationKind.Invocation);
            }

            private static void Analyze(OperationAnalysisContext context)
            {
                var invocation = (IInvocationOperation)context.Operation;
                IMethodSymbol method = invocation.TargetMethod;
                if (method.Name is not ("AreEqual" or "AreNotEqual")
                    || method.ContainingType?.ToDisplayString() != "Microsoft.VisualStudio.TestTools.UnitTesting.Assert"
                    || method.Parameters.Length < 2)
                {
                    return;
                }

                ITypeSymbol bound = method.Parameters[0].Type;
                ITypeSymbol? expected = ArgumentType(invocation, 0);
                ITypeSymbol? actual = ArgumentType(invocation, 1);
                Compilation compilation = context.Compilation;

                string kind = Classify(bound, compilation);
                if (kind == "OPAQUE")
                {
                    string e = Classify(expected, compilation), a = Classify(actual, compilation);
                    kind = e.StartsWith("COLLECTION") ? e
                        : a.StartsWith("COLLECTION") ? a
                        : IsScalarValue(expected) || IsScalarValue(actual) ? "SCALAR"
                        : "OPAQUE";
                }

                string message = string.Join("|", kind, method.Name, Show(bound), Show(expected), Show(actual));
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation(), message));
            }

            /// <summary>The argument's own type, before any conversion to the parameter's.</summary>
            private static ITypeSymbol? ArgumentType(IInvocationOperation invocation, int ordinal)
            {
                IOperation? value = invocation.Arguments.FirstOrDefault(a => a.Parameter?.Ordinal == ordinal)?.Value;
                while (value is IConversionOperation conversion)
                {
                    value = conversion.Operand;
                }
                return value?.Type;
            }

            private static string Show(ITypeSymbol? type) => type?.ToDisplayString() ?? "<null>";

            private static bool IsScalarValue(ITypeSymbol? type) =>
                type is not null && (type.SpecialType == SpecialType.System_String || (type.IsValueType && type.TypeKind != TypeKind.TypeParameter));

            private static string Classify(ITypeSymbol? type, Compilation compilation)
            {
                if (type is null || type.SpecialType == SpecialType.System_String)
                {
                    return "SCALAR";
                }
                if (type.SpecialType == SpecialType.System_Object || type.TypeKind is TypeKind.Interface or TypeKind.TypeParameter or TypeKind.Dynamic)
                {
                    return "OPAQUE";
                }
                INamedTypeSymbol enumerable = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);
                bool isEnumerable = type is IArrayTypeSymbol
                    || type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, enumerable));
                if (!isEnumerable)
                {
                    return "SCALAR";
                }
                return OverridesEquals(type) ? "COLLECTION-EQ" : "COLLECTION-REF";
            }

            private static bool OverridesEquals(ITypeSymbol type)
            {
                for (ITypeSymbol? t = type; t is not null && t.SpecialType != SpecialType.System_Object; t = t.BaseType)
                {
                    if (t.GetMembers("Equals").OfType<IMethodSymbol>().Any(m =>
                            m.IsOverride && m.Parameters.Length == 1 && m.Parameters[0].Type.SpecialType == SpecialType.System_Object))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        """;
}
