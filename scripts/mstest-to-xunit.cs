#!/usr/bin/env dotnet
// Converts an MSTest suite to xUnit v3, file by file, by rewriting the syntax tree — never the text.
//
// ⭐ WHY A SYNTAX TREE. Whether `Assert.AreEqual(a, b, x)` becomes `Assert.Equal` with a tolerance,
// `Assert.Equal` with ignoreCase, or `MessageAssert.Equal` with a message depends on the ARGUMENT
// COUNT and the shape of the third argument. A regex cannot find argument boundaries through nested
// calls, lambdas and interpolated strings; it splits them wrongly and silently. Roslyn's parser is
// the compiler's own, so the boundaries are exact and every comment and line break survives.
//
// ⛔ IT NEVER GUESSES. A construct outside the rules below is left EXACTLY as written and listed as
// UNMAPPED with its file and line. The MSTest attribute or call then fails to compile against
// xUnit, so nothing unconverted can pass unnoticed: the build is the second list.
//
// ⭐ EVERY MESSAGE SURVIVES. xUnit gives Equal / Null / Same / Contains no message parameter, and an
// MSTest suite's messages are usually the point of the assertion. A message-bearing call becomes
// `MessageAssert.X(…, message)`, which calls xUnit's own assertion and prefixes the message only on
// failure, so the expected/actual diff is kept. `--emit-helpers` writes that class (see the end of
// this file for its source and where it came from).
//
// Rules (MSTest 4 → xUnit v3):
//   [TestClass]                      removed
//   [TestMethod]                     [Fact], or [Theory] when the method carries [DataRow]/[DynamicData]
//   [DataRow(args)]                  [InlineData(args)]            (named arguments: UNMAPPED)
//   [DynamicData(nameof(X))]         [MemberData(nameof(X))]       (any other form: UNMAPPED)
//   [Ignore("why")]                  Skip = "why" on the Fact      (bare [Ignore]: UNMAPPED)
//   [Timeout(n)]                     Timeout = n on the Fact, ASYNC methods only — xUnit v3 fails a
//                                    synchronous test that carries a timeout, so a sync one is UNMAPPED
//   [TestInitialize] void M()        a constructor calling M()     (class already has one: UNMAPPED)
//   [TestCleanup] void M()           IDisposable, Dispose() calling M()
//   [TestCleanup] async Task M()     IAsyncDisposable, DisposeAsync() awaiting M()
//   [assembly: DoNotParallelize]     [assembly: CollectionBehavior(DisableTestParallelization = true)]
//   [DoNotParallelize] on a class    [Collection("DoNotParallelize")] — the definition is emitted
//                                    by --emit-helpers
//   [assembly: Parallelize(…)]       UNMAPPED: xUnit has no method-level parallelism; decide by hand
//   TestContext property             removed; TestContext.CancellationToken and
//                                    TestContext.CancellationTokenSource.Token become
//                                    TestContext.Current.CancellationToken
//   using …TestTools.UnitTesting;    using Xunit;  (--no-using: removed, for a project with a
//                                    global <Using Include="Xunit" />)
//   Assert / StringAssert / CollectionAssert calls: see AssertRules below.
//
// Usage:  dotnet run --file scripts/mstest-to-xunit.cs -- <file-or-directory> [--dry-run] [--no-using]
//         dotnet run --file scripts/mstest-to-xunit.cs -- --emit-helpers <file.cs> --namespace <ns>
//
// Exit codes: 0 everything mapped · 2 something UNMAPPED (converted files are still written, so the
// remainder can be finished by hand) · 1 bad arguments or a file that failed to parse.
//
// ⚠ `--file` is required in THIS repository: its root holds a .csproj, and a bare
// `dotnet run <file.cs>` binds to that project instead (see verify-release.cs).

#:package Microsoft.CodeAnalysis.CSharp

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

string? target = null, emitPath = null, emitNamespace = null;
bool dryRun = false, noUsing = false;
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--dry-run": dryRun = true; break;
        case "--no-using": noUsing = true; break;
        case "--emit-helpers" when i + 1 < args.Length: emitPath = args[++i]; break;
        case "--namespace" when i + 1 < args.Length: emitNamespace = args[++i]; break;
        default:
            if (args[i].StartsWith("--", StringComparison.Ordinal) || target is not null)
            {
                return Usage($"unexpected argument '{args[i]}'");
            }
            target = args[i];
            break;
    }
}

if (emitPath is not null)
{
    if (emitNamespace is null || target is not null)
    {
        return Usage("--emit-helpers needs --namespace, and takes no conversion target");
    }
    File.WriteAllText(emitPath, Helpers.Source(emitNamespace), new UTF8Encoding(false));
    Console.WriteLine($"wrote {emitPath}");
    return 0;
}

if (target is null)
{
    return Usage("no file or directory given");
}

string[] files = File.Exists(target)
    ? [Path.GetFullPath(target)]
    : Directory.Exists(target)
        ? [.. Directory.EnumerateFiles(target, "*.cs", SearchOption.AllDirectories)
              .Where(p => !IsBuildOutput(p))
              .Select(Path.GetFullPath)
              .Order(StringComparer.Ordinal)]
        : [];
if (files.Length == 0)
{
    return Usage($"'{target}' is not a .cs file or a directory holding any");
}

Report report = new();
int changed = 0;
foreach (string path in files)
{
    byte[] raw = File.ReadAllBytes(path);
    bool bom = raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF;
    string text = new UTF8Encoding(false, true).GetString(raw, bom ? 3 : 0, raw.Length - (bom ? 3 : 0));

    // Relative to what was asked for, so the report reads the same from any working directory.
    string baseDir = Directory.Exists(target) ? Path.GetFullPath(target) : Path.GetDirectoryName(path)!;
    string relative = Path.GetRelativePath(baseDir, path).Replace('\\', '/');
    string? converted;
    try
    {
        converted = Converter.Convert(text, relative, report, noUsing);
    }
    catch (ParseFailure failure)
    {
        Console.Error.WriteLine($"{relative}: does not parse ({failure.Message}); left untouched");
        return 1;
    }

    if (converted is null || converted == text)
    {
        continue;
    }
    changed++;
    if (!dryRun)
    {
        File.WriteAllText(path, converted, new UTF8Encoding(bom));
    }
}

report.Print(Console.Out, files.Length, changed, dryRun);
return report.Unmapped.Count == 0 ? 0 : 2;

static bool IsBuildOutput(string path)
{
    string[] parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    return parts.Contains("bin") || parts.Contains("obj");
}

static int Usage(string problem)
{
    Console.Error.WriteLine($"mstest-to-xunit: {problem}");
    Console.Error.WriteLine("usage: dotnet run --file scripts/mstest-to-xunit.cs -- <file-or-dir> [--dry-run] [--no-using]");
    Console.Error.WriteLine("       dotnet run --file scripts/mstest-to-xunit.cs -- --emit-helpers <file.cs> --namespace <ns>");
    return 1;
}

sealed class ParseFailure(string message) : Exception(message);

/// <summary>What was converted, by rule, and everything that was not — with file and line.</summary>
sealed class Report
{
    public SortedDictionary<string, int> Rules { get; } = new(StringComparer.Ordinal);
    public List<(string File, int Line, string Text)> Unmapped { get; } = [];
    public SortedSet<string> HelpersUsed { get; } = new(StringComparer.Ordinal);

    public void Converted(string rule) => Rules[rule] = Rules.GetValueOrDefault(rule) + 1;

    /// <remarks>
    /// ⛔ The line comes from the stamp <see cref="Marks.StampLines"/> put on the node BEFORE
    /// rewriting, never from its position: a node the rewriter has already replaced is the root of a
    /// new tree, so its position is measured from itself, not from the file.
    /// </remarks>
    public void Skip(string file, SyntaxNode node, string what, string why) =>
        Unmapped.Add((file, Marks.OriginalLine(node), $"UNMAPPED {what} — {why}"));

    public void Print(TextWriter o, int scanned, int changed, bool dryRun)
    {
        o.WriteLine($"{scanned} file(s) scanned, {changed} {(dryRun ? "would change (dry run)" : "changed")}");
        foreach ((string rule, int count) in Rules)
        {
            o.WriteLine($"  {count,6}  {rule}");
        }
        if (HelpersUsed.Count > 0)
        {
            o.WriteLine($"needs the emitted helpers (--emit-helpers): {string.Join(", ", HelpersUsed)}");
        }
        o.WriteLine(Unmapped.Count == 0 ? "UNMAPPED: none" : $"UNMAPPED: {Unmapped.Count}");
        foreach ((string file, int line, string text) in Unmapped.OrderBy(u => u.File, StringComparer.Ordinal).ThenBy(u => u.Line))
        {
            o.WriteLine($"  {file}:{line}: {text}");
        }
    }
}

/// <summary>One file: parse, rewrite in place (pass 1), then the line-level edits (pass 2).</summary>
static class Converter
{
    public static readonly CSharpParseOptions Options =
        CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

    const string UnitTesting = "Microsoft.VisualStudio.TestTools.UnitTesting";

    public static string? Convert(string text, string file, Report report, bool noUsing)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(text, Options);
        Diagnostic? error = tree.GetDiagnostics().FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        if (error is not null)
        {
            throw new ParseFailure(error.ToString());
        }

        SyntaxNode root = tree.GetRoot();
        if (!LooksLikeMsTest(root))
        {
            return null;
        }

        string eol = text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        SyntaxNode rewritten = new Pass1(file, report, noUsing, HasXunitUsing(root)).Visit(Marks.StampLines(root));
        string result = Pass2.Apply(rewritten, eol);

        // ⛔ A conversion must never turn parseable source into unparseable source. If it does, that
        // is a defect in THIS tool, and the file is refused rather than written.
        Diagnostic? introduced = CSharpSyntaxTree.ParseText(result, Options).GetDiagnostics()
            .FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        if (introduced is not null)
        {
            throw new ParseFailure($"the conversion produced unparseable output — a defect in this tool: {introduced}");
        }
        return result;
    }

    static bool LooksLikeMsTest(SyntaxNode root) =>
        root.DescendantNodes().Any(n => n switch
        {
            UsingDirectiveSyntax u => u.Name?.ToString() == UnitTesting,
            AttributeSyntax a => Pass1.MsTestAttributes.Contains(Pass1.AttributeName(a)),
            MemberAccessExpressionSyntax m => m.Expression is IdentifierNameSyntax { Identifier.ValueText: "Assert" or "StringAssert" or "CollectionAssert" },
            _ => false,
        });

    static bool HasXunitUsing(SyntaxNode root) =>
        root.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(u => u.Alias is null && u.Name?.ToString() == "Xunit");

    public static bool IsUnitTestingUsing(UsingDirectiveSyntax u) =>
        u.Alias is null && u.StaticKeyword.IsKind(SyntaxKind.None) && u.Name?.ToString() == UnitTesting;
}

/// <summary>Annotations pass 1 leaves for pass 2, which does the edits that add or remove lines.</summary>
static class Marks
{
    public const string Delete = "mstest-to-xunit:delete";
    public const string Lifecycle = "mstest-to-xunit:lifecycle";
    public const string InitMethod = "mstest-to-xunit:init";
    public const string CleanupMethod = "mstest-to-xunit:cleanup";
    const string Line = "mstest-to-xunit:line";

    /// <summary>Stamps every node a report can name with its line in the ORIGINAL file.</summary>
    public static SyntaxNode StampLines(SyntaxNode root) =>
        root.ReplaceNodes(
            root.DescendantNodes().Where(n => n is AttributeSyntax or MemberDeclarationSyntax
                or InvocationExpressionSyntax or MemberAccessExpressionSyntax),
            (original, rewritten) => rewritten.WithAdditionalAnnotations(new SyntaxAnnotation(Line,
                (original.GetLocation().GetLineSpan().StartLinePosition.Line + 1).ToString(System.Globalization.CultureInfo.InvariantCulture))));

    public static int OriginalLine(SyntaxNode node) =>
        node.GetAnnotations(Line).FirstOrDefault()?.Data is { } d
            ? int.Parse(d, System.Globalization.CultureInfo.InvariantCulture)
            : node.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
}

/// <summary>
/// Everything that rewrites a node in place. Nothing here adds or removes a LINE — deletions and
/// insertions are annotated and done in <see cref="Pass2"/>, where line boundaries are visible.
/// </summary>
sealed class Pass1(string file, Report report, bool noUsing, bool hasXunitUsing) : CSharpSyntaxRewriter
{
    public static readonly HashSet<string> MsTestAttributes = new(StringComparer.Ordinal)
    {
        "TestClass", "TestMethod", "DataRow", "DynamicData", "Ignore", "Timeout", "TestInitialize",
        "TestCleanup", "ClassInitialize", "ClassCleanup", "AssemblyInitialize", "AssemblyCleanup",
        "DoNotParallelize", "Parallelize", "TestCategory", "Owner", "Priority", "Description",
        "DataTestMethod", "STATestMethod", "STATestClass", "ExpectedException", "TestProperty", "WorkItem",
    };

    public static string AttributeName(AttributeSyntax a)
    {
        string name = a.Name switch
        {
            QualifiedNameSyntax q => q.Right.Identifier.ValueText,
            AliasQualifiedNameSyntax q => q.Name.Identifier.ValueText,
            SimpleNameSyntax s => s.Identifier.ValueText,
            _ => a.Name.ToString(),
        };
        return name.EndsWith("Attribute", StringComparison.Ordinal) ? name[..^"Attribute".Length] : name;
    }

    // ── usings ───────────────────────────────────────────────────────────────────────────────

    public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
    {
        if (!Converter.IsUnitTestingUsing(node))
        {
            return node;
        }
        report.Converted("using …UnitTesting → " + (noUsing || hasXunitUsing ? "removed" : "using Xunit"));
        return noUsing || hasXunitUsing
            ? node.WithAdditionalAnnotations(new SyntaxAnnotation(Marks.Delete))
            : node.WithName(SyntaxFactory.IdentifierName("Xunit").WithTriviaFrom(node.Name!));
    }

    // ── assembly attributes ──────────────────────────────────────────────────────────────────

    public override SyntaxNode? VisitAttributeList(AttributeListSyntax node)
    {
        if (node.Target?.Identifier.ValueText != "assembly")
        {
            return base.VisitAttributeList(node);
        }
        return node.WithAttributes(SyntaxFactory.SeparatedList(node.Attributes.Select(a =>
        {
            switch (AttributeName(a))
            {
                case "DoNotParallelize":
                    report.Converted("[assembly: DoNotParallelize] → CollectionBehavior");
                    return Attr("CollectionBehavior(DisableTestParallelization = true)", a);
                case "Parallelize":
                    report.Skip(file, a, "[assembly: Parallelize(…)]",
                        "xUnit parallelises by collection (per class by default) and has no method-level mode; choose the assembly's behaviour by hand");
                    return a;
                default:
                    return a;
            }
        }), node.Attributes.GetSeparators()));
    }

    // ── classes ──────────────────────────────────────────────────────────────────────────────

    /// <summary>Members the current class's [MemberData] names; xUnit requires them public (xUnit1016).</summary>
    HashSet<string> _memberDataTargets = new(StringComparer.Ordinal);

    public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax original)
    {
        HashSet<string> outer = _memberDataTargets;
        _memberDataTargets = new(StringComparer.Ordinal);
        var node = (ClassDeclarationSyntax)base.VisitClassDeclaration(original)!;
        node = PublishDataMembers(node);
        _memberDataTargets = outer;

        // Class attributes.
        var lists = new List<AttributeListSyntax>();
        foreach (AttributeListSyntax list in node.AttributeLists)
        {
            var kept = new List<AttributeSyntax>();
            foreach (AttributeSyntax a in list.Attributes)
            {
                switch (AttributeName(a))
                {
                    case "TestClass" when a.ArgumentList is null or { Arguments.Count: 0 }:
                        report.Converted("[TestClass] removed");
                        break;
                    case "DoNotParallelize":
                        report.Converted("[DoNotParallelize] class → [Collection(\"DoNotParallelize\")]");
                        report.HelpersUsed.Add("DoNotParallelizeDefinition");
                        kept.Add(Attr("Collection(\"DoNotParallelize\")", a));
                        break;
                    case string n when MsTestAttributes.Contains(n):
                        report.Skip(file, a, $"[{a}] on class {node.Identifier.ValueText}", "no class-level rule");
                        kept.Add(a);
                        break;
                    default:
                        kept.Add(a);
                        break;
                }
            }
            lists.Add(kept.Count == 0
                ? list.WithAdditionalAnnotations(new SyntaxAnnotation(Marks.Delete))
                : list.WithAttributes(SyntaxFactory.SeparatedList(kept)));
        }
        node = node.WithAttributeLists(SyntaxFactory.List(lists));

        // The TestContext property goes; xUnit's TestContext.Current replaces every use of it.
        node = node.ReplaceNodes(
            node.Members.OfType<PropertyDeclarationSyntax>().Where(p =>
                p.Identifier.ValueText == "TestContext" && p.Type.ToString().TrimEnd('?') == "TestContext"),
            (_, p) =>
            {
                report.Converted("TestContext property removed");
                return p.WithAdditionalAnnotations(new SyntaxAnnotation(Marks.Delete));
            });

        return Lifecycle(node);
    }

    /// <summary>
    /// MSTest's DynamicData reads a private member; xUnit's MemberData must be public (xUnit1016,
    /// an error). Only the access modifier changes — the member's shape is left alone.
    /// </summary>
    ClassDeclarationSyntax PublishDataMembers(ClassDeclarationSyntax node)
    {
        if (_memberDataTargets.Count == 0)
        {
            return node;
        }
        var targets = node.Members.Where(m => _memberDataTargets.Contains(MemberName(m))).ToList();
        return node.ReplaceNodes(targets, (orig, _) =>
        {
            SyntaxTokenList mods = orig.Modifiers;
            if (mods.Any(SyntaxKind.PublicKeyword))
            {
                return orig;
            }
            int access = mods.IndexOf(SyntaxKind.PrivateKeyword) is var p and >= 0 ? p
                : mods.IndexOf(SyntaxKind.ProtectedKeyword) is var q and >= 0 ? q
                : mods.IndexOf(SyntaxKind.InternalKeyword);
            if (access >= 0)
            {
                SyntaxToken pub = SyntaxFactory.Token(SyntaxKind.PublicKeyword).WithTriviaFrom(mods[access]);
                var rest = mods.Where((t, i) => i != access && !t.IsKind(SyntaxKind.PrivateKeyword)
                                                && !t.IsKind(SyntaxKind.ProtectedKeyword) && !t.IsKind(SyntaxKind.InternalKeyword));
                report.Converted("[MemberData] target made public");
                return orig.WithModifiers(SyntaxFactory.TokenList(rest.Prepend(pub)));
            }
            if (orig.AttributeLists.Count == 0 && mods.Count > 0)
            {
                // implicit private, e.g. `static IEnumerable<object[]> Rows => …`
                SyntaxToken first = mods[0];
                SyntaxToken pub = SyntaxFactory.Token(first.LeadingTrivia, SyntaxKind.PublicKeyword,
                    SyntaxFactory.TriviaList(SyntaxFactory.Space));
                report.Converted("[MemberData] target made public");
                return orig.WithModifiers(mods.Replace(first, first.WithLeadingTrivia()).Insert(0, pub));
            }
            report.Skip(file, orig, $"[MemberData] target {MemberName(orig)}", "xUnit requires it public; change its access by hand");
            return orig;
        });
    }

    static string MemberName(MemberDeclarationSyntax m) => m switch
    {
        PropertyDeclarationSyntax p => p.Identifier.ValueText,
        MethodDeclarationSyntax x => x.Identifier.ValueText,
        FieldDeclarationSyntax f => f.Declaration.Variables.Count == 1 ? f.Declaration.Variables[0].Identifier.ValueText : "",
        _ => "",
    };

    /// <summary>[TestInitialize] → a constructor; [TestCleanup] → Dispose / DisposeAsync.</summary>
    ClassDeclarationSyntax Lifecycle(ClassDeclarationSyntax node)
    {
        var inits = node.Members.OfType<MethodDeclarationSyntax>().Where(m => Has(m, "TestInitialize")).ToList();
        var cleanups = node.Members.OfType<MethodDeclarationSyntax>().Where(m => Has(m, "TestCleanup")).ToList();
        if (inits.Count == 0 && cleanups.Count == 0)
        {
            return node;
        }

        string? init = null, cleanup = null;
        bool asyncCleanup = false;
        var accepted = new List<MethodDeclarationSyntax>();

        if (inits.Count > 0)
        {
            MethodDeclarationSyntax m = inits[0];
            string? why =
                inits.Count > 1 ? "more than one [TestInitialize]" :
                node.Members.OfType<ConstructorDeclarationSyntax>().Any(c => !c.Modifiers.Any(SyntaxKind.StaticKeyword))
                    || node.ParameterList is not null ? "the class already has a constructor" :
                !IsPlainVoid(m) ? "only a synchronous, parameterless, non-static void method maps to a constructor" :
                null;
            if (why is null) { init = m.Identifier.ValueText; accepted.Add(m); report.Converted("[TestInitialize] → constructor"); }
            else { report.Skip(file, m, $"[TestInitialize] {m.Identifier.ValueText}", why); }
        }

        if (cleanups.Count > 0)
        {
            MethodDeclarationSyntax m = cleanups[0];
            bool isAsyncTask = m.ReturnType.ToString() == "Task" && m.ParameterList.Parameters.Count == 0
                               && !m.Modifiers.Any(SyntaxKind.StaticKeyword);
            string? why =
                cleanups.Count > 1 ? "more than one [TestCleanup]" :
                node.Members.OfType<MethodDeclarationSyntax>().Any(x => x.Identifier.ValueText is "Dispose" or "DisposeAsync")
                    || (node.BaseList?.Types.Any(t => t.Type.ToString() is "IDisposable" or "IAsyncDisposable" or "System.IDisposable") ?? false)
                    ? "the class is already disposable" :
                !IsPlainVoid(m) && !isAsyncTask ? "only a parameterless void or async Task method maps to Dispose" :
                null;
            if (why is null)
            {
                cleanup = m.Identifier.ValueText;
                asyncCleanup = !IsPlainVoid(m);
                accepted.Add(m);
                report.Converted(asyncCleanup ? "[TestCleanup] async → IAsyncDisposable" : "[TestCleanup] → IDisposable");
            }
            else { report.Skip(file, m, $"[TestCleanup] {m.Identifier.ValueText}", why); }
        }

        node = node.ReplaceNodes(accepted, (orig, _) =>
        {
            MethodDeclarationSyntax m = orig;
            string mark = Has(m, "TestInitialize") ? Marks.InitMethod : Marks.CleanupMethod;
            m = RemoveAttributes(m, a => AttributeName(a) is "TestInitialize" or "TestCleanup");

            // A public method on a test class that is not a test is an ERROR under xUnit's analyzers
            // (xUnit1013). The method is now only called from the constructor or Dispose, so it
            // becomes private — unless something could be overriding it.
            int pub = m.Modifiers.IndexOf(SyntaxKind.PublicKeyword);
            if (pub >= 0 && !m.Modifiers.Any(SyntaxKind.VirtualKeyword) && !m.Modifiers.Any(SyntaxKind.OverrideKeyword)
                && !m.Modifiers.Any(SyntaxKind.AbstractKeyword))
            {
                m = m.WithModifiers(m.Modifiers.Replace(m.Modifiers[pub],
                    SyntaxFactory.Token(SyntaxKind.PrivateKeyword).WithTriviaFrom(m.Modifiers[pub])));
                report.Converted("lifecycle method made private (xUnit1013)");
            }
            return m.WithAdditionalAnnotations(new SyntaxAnnotation(mark));
        });

        string data = $"{init}|{cleanup}|{(asyncCleanup ? "async" : "")}";
        return init is null && cleanup is null
            ? node
            : node.WithAdditionalAnnotations(new SyntaxAnnotation(Marks.Lifecycle, data));
    }

    static bool IsPlainVoid(MethodDeclarationSyntax m) =>
        m.ReturnType.ToString() == "void" && m.ParameterList.Parameters.Count == 0
        && !m.Modifiers.Any(SyntaxKind.StaticKeyword) && !m.Modifiers.Any(SyntaxKind.AsyncKeyword)
        && m.TypeParameterList is null;

    // ── test methods ─────────────────────────────────────────────────────────────────────────

    public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax original)
    {
        var node = (MethodDeclarationSyntax)base.VisitMethodDeclaration(original)!;
        List<AttributeSyntax> all = [.. node.AttributeLists.SelectMany(l => l.Attributes)];
        AttributeSyntax? testMethod = all.FirstOrDefault(a => AttributeName(a) == "TestMethod");
        if (testMethod is null)
        {
            foreach (AttributeSyntax a in all.Where(a => MsTestAttributes.Contains(AttributeName(a))
                                                         && AttributeName(a) is not ("TestInitialize" or "TestCleanup")))
            {
                report.Skip(file, a, $"[{a}] on {node.Identifier.ValueText}", "not on a [TestMethod]; no rule");
            }
            return node;
        }
        if (testMethod.ArgumentList is { Arguments.Count: > 0 })
        {
            report.Skip(file, testMethod, $"[{testMethod}]", "a display name has no xUnit equivalent here; convert by hand");
            return node;
        }

        bool theory = all.Any(a => AttributeName(a) is "DataRow" or "DynamicData");
        var factArgs = new List<string>();
        var folded = new HashSet<AttributeSyntax>();
        var replaced = new Dictionary<AttributeSyntax, AttributeSyntax>();

        foreach (AttributeSyntax a in all)
        {
            var args = a.ArgumentList?.Arguments ?? default;
            switch (AttributeName(a))
            {
                case "TestMethod":
                    break;
                case "DataRow":
                    if (args.Any(x => x.NameEquals is not null || x.NameColon is not null))
                    {
                        report.Skip(file, a, $"[{a}]", "named DataRow arguments (DisplayName, IgnoreMessage…) have no InlineData equivalent");
                    }
                    else
                    {
                        replaced[a] = a.WithName(SyntaxFactory.IdentifierName("InlineData").WithTriviaFrom(a.Name));
                        report.Converted("[DataRow] → [InlineData]");
                    }
                    break;
                case "DynamicData":
                    if (args.Count == 1 && args[0].Expression is InvocationExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "nameof" } } or LiteralExpressionSyntax)
                    {
                        replaced[a] = a.WithName(SyntaxFactory.IdentifierName("MemberData").WithTriviaFrom(a.Name));
                        report.Converted("[DynamicData] → [MemberData]");
                        _memberDataTargets.Add(args[0].Expression switch
                        {
                            InvocationExpressionSyntax { ArgumentList.Arguments: [var only] } => only.Expression is MemberAccessExpressionSyntax ma
                                ? ma.Name.Identifier.ValueText : only.Expression.ToString(),
                            LiteralExpressionSyntax l => l.Token.ValueText,
                            _ => "",
                        });
                    }
                    else
                    {
                        report.Skip(file, a, $"[{a}]", "only DynamicData(nameof(Member)) maps to MemberData");
                    }
                    break;
                case "Ignore":
                    if (args.Count == 1 && args[0].NameEquals is null)
                    {
                        factArgs.Add($"Skip = {args[0].Expression}");
                        folded.Add(a);
                        report.Converted("[Ignore] → Skip =");
                    }
                    else
                    {
                        report.Skip(file, a, "[Ignore] without a reason", "xUnit needs a skip reason; supply one");
                    }
                    break;
                case "Timeout":
                    if (args.Count == 1 && IsAsync(node))
                    {
                        factArgs.Add($"Timeout = {args[0].Expression}");
                        folded.Add(a);
                        report.Converted("[Timeout] → Timeout =");
                    }
                    else
                    {
                        report.Skip(file, a, $"[{a}] on {node.Identifier.ValueText}",
                            "xUnit v3 fails a SYNCHRONOUS test that carries a Timeout; make the test async, or drop the timeout, by hand");
                    }
                    break;
                case string n when MsTestAttributes.Contains(n):
                    report.Skip(file, a, $"[{a}] on {node.Identifier.ValueText}", "no rule for this attribute");
                    break;
            }
        }

        string fact = (theory ? "Theory" : "Fact") + (factArgs.Count > 0 ? "(" + string.Join(", ", factArgs) + ")" : "");
        replaced[testMethod] = Attr(fact, testMethod);
        report.Converted(theory ? "[TestMethod] → [Theory]" : "[TestMethod] → [Fact]");

        node = node.ReplaceNodes(replaced.Keys, (orig, _) => replaced[orig]);
        return RemoveAttributes(node, a => folded.Any(f => f.IsEquivalentTo(a)));
    }

    static bool IsAsync(MethodDeclarationSyntax m) =>
        m.Modifiers.Any(SyntaxKind.AsyncKeyword)
        || m.ReturnType.ToString() is "Task" or "ValueTask" || m.ReturnType.ToString().StartsWith("Task<", StringComparison.Ordinal)
        || m.ReturnType.ToString().StartsWith("ValueTask<", StringComparison.Ordinal);

    static bool Has(MethodDeclarationSyntax m, string name) =>
        m.AttributeLists.SelectMany(l => l.Attributes).Any(a => AttributeName(a) == name);

    /// <summary>
    /// Removes attributes; a list left empty is annotated for pass 2 to delete WITH its line, so
    /// no blank line or stray indentation is left behind.
    /// </summary>
    static MethodDeclarationSyntax RemoveAttributes(MethodDeclarationSyntax m, Func<AttributeSyntax, bool> remove) =>
        m.WithAttributeLists(SyntaxFactory.List(m.AttributeLists.Select(list =>
        {
            var kept = list.Attributes.Where(a => !remove(a)).ToList();
            return kept.Count == list.Attributes.Count ? list
                : kept.Count == 0 ? list.WithAdditionalAnnotations(new SyntaxAnnotation(Marks.Delete))
                : list.WithAttributes(SyntaxFactory.SeparatedList(kept));
        })));

    static AttributeSyntax Attr(string text, AttributeSyntax like) =>
        SyntaxFactory.ParseCompilationUnit($"[{text}] class C {{}}")
            .DescendantNodes().OfType<AttributeSyntax>().First().WithTriviaFrom(like);

    // ── TestContext ──────────────────────────────────────────────────────────────────────────

    public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
    {
        // TestContext.CancellationTokenSource.Token  and  TestContext.CancellationToken
        bool viaSource = node is { Name.Identifier.ValueText: "Token", Expression: MemberAccessExpressionSyntax
        {
            Name.Identifier.ValueText: "CancellationTokenSource",
            Expression: IdentifierNameSyntax { Identifier.ValueText: "TestContext" },
        } };
        bool direct = node is { Name.Identifier.ValueText: "CancellationToken", Expression: IdentifierNameSyntax { Identifier.ValueText: "TestContext" } };
        if (viaSource || direct)
        {
            report.Converted("TestContext token → TestContext.Current.CancellationToken");
            return SyntaxFactory.ParseExpression("TestContext.Current.CancellationToken").WithTriviaFrom(node);
        }
        if (node.Expression is IdentifierNameSyntax { Identifier.ValueText: "TestContext" }
            && node.Name.Identifier.ValueText != "Current")
        {
            report.Skip(file, node, $"TestContext.{node.Name}", "only the cancellation token maps; use TestContext.Current by hand");
            return node;
        }
        return base.VisitMemberAccessExpression(node);
    }

    // ── assertions ───────────────────────────────────────────────────────────────────────────

    public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax original)
    {
        var node = (InvocationExpressionSyntax)base.VisitInvocationExpression(original)!;
        if (node.Expression is not MemberAccessExpressionSyntax
            {
                Expression: IdentifierNameSyntax { Identifier.ValueText: "Assert" or "StringAssert" or "CollectionAssert" } receiver,
            } access)
        {
            return node;
        }

        string family = receiver.Identifier.ValueText;
        string method = access.Name.Identifier.ValueText;
        var args = node.ArgumentList.Arguments;
        if (args.Any(a => a.NameColon is not null || !a.RefKindKeyword.IsKind(SyntaxKind.None)))
        {
            report.Skip(file, node, $"{family}.{method}", "named, ref or out arguments");
            return node;
        }

        if (AssertRules.AnalyzerForm(family, method, args) is var (specialTo, typeArgument, specialArgs))
        {
            report.Converted($"{family}.{method} → {specialTo}{(typeArgument is null ? "" : "<T>")} (the form xUnit's analyzers require)");
            string specialReceiver = specialTo[..specialTo.IndexOf('.')];
            string specialMethod = specialTo[(specialTo.IndexOf('.') + 1)..];
            if (specialReceiver == "MessageAssert")
            {
                report.HelpersUsed.Add("MessageAssert." + specialMethod);
            }
            SyntaxToken id = SyntaxFactory.Identifier(specialMethod).WithTriviaFrom(access.Name.Identifier);
            SimpleNameSyntax specialName = typeArgument is null
                ? SyntaxFactory.IdentifierName(id)
                : SyntaxFactory.GenericName(id, SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(typeArgument.WithoutTrivia())));
            // Each new argument keeps the trivia of the position it lands in; separators are reused.
            var newArgs = specialArgs.Select((e, i) => SyntaxFactory.Argument(e.WithoutTrivia()).WithTriviaFrom(args[Math.Min(i, args.Count - 1)])).ToList();
            var separators = args.GetSeparators().Take(newArgs.Count - 1).ToList();
            // A form can take MORE arguments than the original (IsTrue(xs.Any(p)) → Contains(xs, p)),
            // and then there is no separator to reuse.
            while (separators.Count < newArgs.Count - 1)
            {
                separators.Add(SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space));
            }
            return node
                .WithExpression(access
                    .WithExpression(SyntaxFactory.IdentifierName(specialReceiver).WithTriviaFrom(receiver))
                    .WithName(specialName))
                .WithArgumentList(node.ArgumentList.WithArguments(SyntaxFactory.SeparatedList(newArgs, separators)));
        }

        (string To, int[] Order)? rule = AssertRules.Map(family, method, access.Name is GenericNameSyntax, args, out string? why);
        if (rule is null)
        {
            report.Skip(file, node, $"{family}.{method}({args.Count} argument{(args.Count == 1 ? "" : "s")})", why ?? "no rule");
            return node;
        }

        (string to, int[] order) = rule.Value;
        string newReceiver = to[..to.IndexOf('.')];
        string newMethod = to[(to.IndexOf('.') + 1)..];
        if (newReceiver == "MessageAssert")
        {
            report.HelpersUsed.Add("MessageAssert." + newMethod);
        }
        report.Converted($"{family}.{method} → {to}");

        SimpleNameSyntax name = access.Name is GenericNameSyntax g
            ? g.WithIdentifier(SyntaxFactory.Identifier(newMethod).WithTriviaFrom(g.Identifier))
            : SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(newMethod).WithTriviaFrom(access.Name.Identifier));
        var newAccess = access
            .WithExpression(SyntaxFactory.IdentifierName(newReceiver).WithTriviaFrom(receiver))
            .WithName(name);

        // Reorder arguments, but leave each POSITION's trivia where it was, so a call laid out over
        // several lines keeps its layout after a swap.
        var reordered = new List<ArgumentSyntax>();
        for (int i = 0; i < order.Length; i++)
        {
            reordered.Add(args[order[i]].WithTriviaFrom(args[i]));
        }
        var list = SyntaxFactory.SeparatedList(reordered, args.GetSeparators());
        return node.WithExpression(newAccess).WithArgumentList(node.ArgumentList.WithArguments(list));
    }
}

/// <summary>
/// The assertion table. Each rule names its target as "Receiver.Method" and the order in which the
/// original arguments are passed to it. A call no rule matches is left alone and reported.
/// </summary>
static class AssertRules
{
    enum Arg { Message, Number, Bool, Comparison, Lambda, Unknown }

    public static (string To, int[] Order)? Map(string family, string method, bool generic,
        SeparatedSyntaxList<ArgumentSyntax> args, out string? why)
    {
        why = null;
        int n = args.Count;
        Arg Kind(int i) => Classify(args[i].Expression);
        int[] Same = [.. Enumerable.Range(0, n)];
        int[] Swap = n switch { 2 => [1, 0], 3 => [1, 0, 2], _ => [] };

        (string, int[])? r = (family, method, n) switch
        {
            // ── Assert ──
            ("Assert", "AreEqual", 2) => ("Assert.Equal", Same),
            ("Assert", "AreEqual", 3) => Kind(2) switch
            {
                Arg.Message => ("MessageAssert.Equal", Same),
                Arg.Number => ("Assert.Equal", Same),   // tolerance
                Arg.Bool => ("Assert.Equal", Same),     // ignoreCase
                _ => null,
            },
            ("Assert", "AreEqual", 4) when Kind(2) == Arg.Number && Kind(3) == Arg.Message => ("MessageAssert.Equal", Same),
            ("Assert", "AreNotEqual", 2) => ("Assert.NotEqual", Same),
            ("Assert", "AreNotEqual", 3) when Kind(2) == Arg.Message => ("MessageAssert.NotEqual", Same),
            ("Assert", "AreSame", 2) => ("Assert.Same", Same),
            ("Assert", "AreSame", 3) => ("MessageAssert.Same", Same),
            ("Assert", "AreNotSame", 2) => ("Assert.NotSame", Same),
            ("Assert", "AreNotSame", 3) => ("MessageAssert.NotSame", Same),
            // IsTrue/IsFalse/IsNull's second parameter can only be the message, whatever its shape.
            ("Assert", "IsTrue", 1 or 2) => ("Assert.True", Same),
            ("Assert", "IsFalse", 1 or 2) => ("Assert.False", Same),
            ("Assert", "IsNull", 1) => ("Assert.Null", Same),
            ("Assert", "IsNull", 2) => ("MessageAssert.Null", Same),
            ("Assert", "IsNotNull", 1) => ("Assert.NotNull", Same),
            ("Assert", "IsNotNull", 2) => ("MessageAssert.NotNull", Same),
            // MSTest's IsInstanceOfType passes for a derived type: that is xUnit's IsAssignableFrom,
            // NOT IsType, which demands the exact type.
            ("Assert", "IsInstanceOfType", 1) when generic => ("Assert.IsAssignableFrom", Same),
            ("Assert", "IsInstanceOfType", 2) when generic => ("MessageAssert.IsAssignableFrom", Same),
            ("Assert", "IsInstanceOfType", 2) => ("Assert.IsAssignableFrom", Swap),
            ("Assert", "IsInstanceOfType", 3) => ("MessageAssert.IsAssignableFrom", Swap),
            ("Assert", "IsNotInstanceOfType", 1) when generic => ("Assert.IsNotAssignableFrom", Same),
            ("Assert", "Fail", 1) => ("Assert.Fail", Same),
            ("Assert", "Inconclusive", 1) => ("Assert.Skip", Same),
            // ThrowsExactly is exact, as xUnit's Throws is; MSTest's Throws accepts derived types.
            // ⚠ `() => throw new X()` converts to every delegate type, and xUnit's overload resolution
            // then picks its OBSOLETE Throws<T>(Func<Task>) — CS0619, an error. Left for a human.
            ("Assert", "ThrowsExactly" or "Throws", 1) when ThrowLambda(args[0].Expression) => null,
            ("Assert", "ThrowsExactly", 1) => ("Assert.Throws", Same),
            ("Assert", "ThrowsExactly", 2) => ("MessageAssert.Throws", Same),
            ("Assert", "ThrowsExactlyAsync", 1) => ("Assert.ThrowsAsync", Same),
            ("Assert", "ThrowsExactlyAsync", 2) => ("MessageAssert.ThrowsAsync", Same),
            ("Assert", "Throws", 1) => ("Assert.ThrowsAny", Same),
            ("Assert", "ThrowsAsync", 1) => ("Assert.ThrowsAnyAsync", Same),
            // MSTest 4's Contains/StartsWith/EndsWith already take (expected, actual), as xUnit does.
            ("Assert", "Contains" or "DoesNotContain", 2) when Kind(0) == Arg.Lambda => ($"Assert.{method}", Swap),
            ("Assert", "Contains" or "DoesNotContain" or "StartsWith" or "EndsWith", 2) => ($"Assert.{method}", Same),
            ("Assert", "Contains" or "DoesNotContain" or "StartsWith" or "EndsWith", 3) => Kind(2) switch
            {
                Arg.Message => ($"MessageAssert.{method}", Same),
                Arg.Comparison => ($"Assert.{method}", Same),
                _ => null,
            },
            ("Assert", "IsEmpty", 1) => ("Assert.Empty", Same),
            ("Assert", "IsNotEmpty", 1) => ("Assert.NotEmpty", Same),

            // ── StringAssert: (value, substring) — xUnit takes (substring, value) ──
            ("StringAssert", "Contains" or "StartsWith" or "EndsWith", 2) => ($"Assert.{method}", Swap),
            ("StringAssert", "Contains" or "StartsWith" or "EndsWith", 3) => Kind(2) switch
            {
                Arg.Message => ($"MessageAssert.{method}", Swap),
                Arg.Comparison => ($"Assert.{method}", Swap),
                _ => null,
            },
            ("StringAssert", "Matches" or "DoesNotMatch", 2) => ($"Assert.{method}", Swap),
            ("StringAssert", "Matches" or "DoesNotMatch", 3) when Kind(2) == Arg.Message => ($"MessageAssert.{method}", Swap),

            // ── CollectionAssert ──
            ("CollectionAssert", "AreEqual", 2) => ("Assert.Equal", Same),
            ("CollectionAssert", "AreEqual", 3) when Kind(2) == Arg.Message => ("MessageAssert.SequenceEqual", Same),
            ("CollectionAssert", "AreNotEqual", 2) => ("Assert.NotEqual", Same),
            // (collection, element) — xUnit takes (element, collection)
            ("CollectionAssert", "Contains" or "DoesNotContain", 2) => ($"Assert.{method}", Swap),
            ("CollectionAssert", "Contains" or "DoesNotContain", 3) => ($"MessageAssert.{method}", Swap),
            // Same elements, same multiplicity, any order. xUnit's Equivalent is a structural
            // comparison with different rules, so this goes to a helper that implements MSTest's.
            ("CollectionAssert", "AreEquivalent", 2 or 3) => ("MessageAssert.SameElements", Same),
            ("CollectionAssert", "AllItemsAreUnique", 1) => ("Assert.Distinct", Same),
            _ => null,
        };

        if (r is null)
        {
            why = (family, method, n) switch
            {
                ("Assert", "AreEqual", 3 or 4) => "the third argument is neither a message, a numeric tolerance nor an ignoreCase flag that can be recognised from its syntax",
                ("CollectionAssert", "AreEqual", 3) => "a comparer argument; xUnit takes an IEqualityComparer — convert by hand",
                ("Assert", "ThrowsExactly" or "Throws", 1) => "a throw-expression lambda binds to xUnit's obsolete Throws<T>(Func<Task>); give it a statement body or an Action type by hand",
                _ => "no rule for this form",
            };
        }
        return r;
    }

    /// <summary>
    /// Two-argument equality checks that xUnit's analyzers reject as errors, rewritten to the exactly
    /// equivalent assertion they ask for. Only these, and only without a message:
    /// <c>(null, x)</c> → Null / NotNull (xUnit2003), <c>(true|false, x)</c> → True / False (xUnit2004),
    /// <c>(0|1, c.Count|c.Length|c.Count())</c> → Empty / Single (xUnit2013),
    /// and <c>IsTrue|IsFalse(c.Any(p))</c> → Contains / DoesNotContain (xUnit2012).
    /// </summary>
    public static (string To, TypeSyntax? TypeArgument, ExpressionSyntax[] Args)? AnalyzerForm(
        string family, string method, SeparatedSyntaxList<ArgumentSyntax> args)
    {
        if (family != "Assert")
        {
            return null;
        }

        // IsInstanceOfType(x, typeof(T)[, message]) → IsAssignableFrom<T>(x[, message]): the
        // non-generic form with a typeof is xUnit2007 and CA2263, both errors.
        if (method == "IsInstanceOfType" && args.Count is 2 or 3 && args[1].Expression is TypeOfExpressionSyntax typeOf)
        {
            return args.Count == 2
                ? ("Assert.IsAssignableFrom", typeOf.Type, [args[0].Expression])
                : ("MessageAssert.IsAssignableFrom", typeOf.Type, [args[0].Expression, args[2].Expression]);
        }

        // IsTrue(xs.Any(p)) / IsFalse(xs.Any(p)) → Contains / DoesNotContain(xs, p): the boolean form
        // is xUnit2012, an error. Only without a message, like the equality forms below.
        if (method is "IsTrue" or "IsFalse" && args.Count == 1 && AnyWithPredicate(args[0].Expression) is var (anySource, anyPredicate))
        {
            return (method == "IsTrue" ? "Assert.Contains" : "Assert.DoesNotContain", null, [anySource, anyPredicate]);
        }

        if (method is not ("AreEqual" or "AreNotEqual") || args.Count != 2)
        {
            return null;
        }
        bool equal = method == "AreEqual";
        ExpressionSyntax expected = args[0].Expression, actual = args[1].Expression;

        if (expected.IsKind(SyntaxKind.NullLiteralExpression))
        {
            return (equal ? "Assert.Null" : "Assert.NotNull", null, [actual]);
        }
        if (equal && expected.IsKind(SyntaxKind.TrueLiteralExpression))
        {
            return ("Assert.True", null, [actual]);
        }
        if (equal && expected.IsKind(SyntaxKind.FalseLiteralExpression))
        {
            return ("Assert.False", null, [actual]);
        }
        if (equal && expected is LiteralExpressionSyntax { Token.ValueText: "0" or "1" } n
            && n.IsKind(SyntaxKind.NumericLiteralExpression))
        {
            bool zero = n.Token.ValueText == "0";
            // A filtered count: xUnit2029 / xUnit2030 want the predicate overloads.
            if (FilteredCount(actual) is var (source, predicate))
            {
                return (zero ? "Assert.DoesNotContain" : "Assert.Single", null, [source, predicate]);
            }
            if (CountedCollection(actual) is { } collection)
            {
                return (zero ? "Assert.Empty" : "Assert.Single", null, [collection]);
            }
        }
        return null;
    }

    /// <summary><c>xs.Any(p)</c> → <c>(xs, p)</c>, for a lambda <c>p</c>.</summary>
    static (ExpressionSyntax Source, ExpressionSyntax Predicate)? AnyWithPredicate(ExpressionSyntax e) => e switch
    {
        InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Any" } m,
            ArgumentList.Arguments: [{ Expression: LambdaExpressionSyntax p }],
        } => (m.Expression, p),
        _ => null,
    };

    static bool ThrowLambda(ExpressionSyntax e) =>
        e is LambdaExpressionSyntax { ExpressionBody: ThrowExpressionSyntax };

    /// <summary><c>xs.Count()</c>, <c>xs.Count</c>, <c>xs.Length</c> → <c>xs</c>.</summary>
    static ExpressionSyntax? CountedCollection(ExpressionSyntax e) => e switch
    {
        MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Count" or "Length" } m => m.Expression,
        InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Count" } m,
            ArgumentList.Arguments.Count: 0,
        } => m.Expression,
        _ => null,
    };

    /// <summary><c>xs.Where(p).Count()</c> and <c>xs.Count(p)</c> → <c>(xs, p)</c>, for a lambda <c>p</c>.</summary>
    static (ExpressionSyntax Source, ExpressionSyntax Predicate)? FilteredCount(ExpressionSyntax e) => e switch
    {
        InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Count" } m,
            ArgumentList.Arguments: [{ Expression: LambdaExpressionSyntax p }],
        } => (m.Expression, p),
        InvocationExpressionSyntax
        {
            Expression: MemberAccessExpressionSyntax
            {
                Name.Identifier.ValueText: "Count",
                Expression: InvocationExpressionSyntax
                {
                    Expression: MemberAccessExpressionSyntax { Name.Identifier.ValueText: "Where" } w,
                    ArgumentList.Arguments: [{ Expression: LambdaExpressionSyntax p }],
                },
            },
            ArgumentList.Arguments.Count: 0,
        } => (w.Expression, p),
        _ => null,
    };

    static Arg Classify(ExpressionSyntax e) => e switch
    {
        LiteralExpressionSyntax l when l.IsKind(SyntaxKind.StringLiteralExpression) => Arg.Message,
        LiteralExpressionSyntax l when l.IsKind(SyntaxKind.NumericLiteralExpression) => Arg.Number,
        LiteralExpressionSyntax l when l.IsKind(SyntaxKind.TrueLiteralExpression) || l.IsKind(SyntaxKind.FalseLiteralExpression) => Arg.Bool,
        PrefixUnaryExpressionSyntax { Operand: LiteralExpressionSyntax o } when o.IsKind(SyntaxKind.NumericLiteralExpression) => Arg.Number,
        InterpolatedStringExpressionSyntax => Arg.Message,
        BinaryExpressionSyntax b when b.IsKind(SyntaxKind.AddExpression)
            && (Classify(b.Left) == Arg.Message || Classify(b.Right) == Arg.Message) => Arg.Message,
        InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax
        {
            Expression: PredefinedTypeSyntax p, Name.Identifier.ValueText: "Format" or "Join" or "Concat",
        } } when p.Keyword.IsKind(SyntaxKind.StringKeyword) => Arg.Message,
        MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax { Identifier.ValueText: "StringComparison" } } => Arg.Comparison,
        LambdaExpressionSyntax => Arg.Lambda,
        ParenthesizedExpressionSyntax p => Classify(p.Expression),
        _ => Arg.Unknown,
    };
}

/// <summary>
/// The edits that add or remove whole lines, done as text changes on pass 1's tree so line
/// boundaries, indentation and the file's own line ending are respected exactly.
/// </summary>
static class Pass2
{
    public static string Apply(SyntaxNode root, string eol)
    {
        SourceText text = SourceText.From(root.ToFullString());
        var changes = new List<TextChange>();

        foreach (SyntaxNode node in root.GetAnnotatedNodes(Marks.Delete))
        {
            changes.Add(DeleteLines(text, node));
        }

        foreach (SyntaxNode node in root.GetAnnotatedNodes(Marks.Lifecycle))
        {
            var cls = (ClassDeclarationSyntax)node;
            string[] data = cls.GetAnnotations(Marks.Lifecycle).First().Data!.Split('|');
            string init = data[0], cleanup = data[1];
            bool asyncCleanup = data[2] == "async";

            if (init.Length > 0)
            {
                SyntaxNode m = cls.Members.First(x => x.HasAnnotations(Marks.InitMethod));
                (int lineStart, string indent) = MemberStart(text, m);
                changes.Add(new TextChange(new TextSpan(lineStart, 0),
                    $"{indent}public {cls.Identifier.ValueText}() => {init}();{eol}{eol}"));
            }
            if (cleanup.Length > 0)
            {
                SyntaxNode m = cls.Members.First(x => x.HasAnnotations(Marks.CleanupMethod));
                (_, string indent) = MemberStart(text, m);
                int after = text.Lines.GetLineFromPosition(m.Span.End).EndIncludingLineBreak;
                // Block bodies with GC.SuppressFinalize: a non-sealed class's Dispose without it is
                // CA1816, and the call is harmless on a sealed one.
                string inner = indent + (indent.Contains('\t') ? "\t" : "    ");
                string signature = asyncCleanup ? "public async ValueTask DisposeAsync()" : "public void Dispose()";
                string call = asyncCleanup ? $"await {cleanup}();" : $"{cleanup}();";
                changes.Add(new TextChange(new TextSpan(after, 0),
                    $"{eol}{indent}{signature}{eol}{indent}{{{eol}{inner}{call}{eol}{inner}GC.SuppressFinalize(this);{eol}{indent}}}{eol}"));

                string iface = asyncCleanup ? "IAsyncDisposable" : "IDisposable";
                if (cls.BaseList is { } baseList)
                {
                    changes.Add(new TextChange(new TextSpan(baseList.Span.End, 0), $", {iface}"));
                }
                else
                {
                    int anchor = cls.ParameterList?.Span.End ?? cls.TypeParameterList?.Span.End ?? cls.Identifier.Span.End;
                    changes.Add(new TextChange(new TextSpan(anchor, 0), $" : {iface}"));
                }
            }
        }

        // An insertion and a deletion can start at the same position (a constructor goes in on the
        // line whose [TestInitialize] is removed): the zero-length insertion must come first.
        return text.WithChanges(changes.OrderBy(c => c.Span.Start).ThenBy(c => c.Span.Length)).ToString();
    }

    /// <summary>
    /// The node's lines, when it stands alone on them; otherwise just the node. A deleted property
    /// takes its doc comment with it; a deleted attribute list does not (that comment documents
    /// the member below it).
    /// </summary>
    static TextChange DeleteLines(SourceText text, SyntaxNode node)
    {
        int start = node.Span.Start;
        if (node is MemberDeclarationSyntax)
        {
            SyntaxTrivia first = node.GetLeadingTrivia().FirstOrDefault(t =>
                !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia));
            if (first != default)
            {
                start = first.Span.Start;
            }
        }
        TextLine firstLine = text.Lines.GetLineFromPosition(start);
        TextLine lastLine = text.Lines.GetLineFromPosition(node.Span.End);
        bool aloneBefore = string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(firstLine.Start, start)));
        bool aloneAfter = string.IsNullOrWhiteSpace(text.ToString(TextSpan.FromBounds(node.Span.End, lastLine.End)));
        if (!(aloneBefore && aloneAfter))
        {
            return new TextChange(node.Span, "");
        }

        // A removed member or using that sat between an opening brace (or the top of the file) and
        // a blank line would leave that blank line hanging; take it too. An attribute list never
        // does this — the member it decorated still follows it.
        int end = lastLine.EndIncludingLineBreak;
        if (node is MemberDeclarationSyntax or UsingDirectiveSyntax && lastLine.LineNumber + 1 < text.Lines.Count)
        {
            TextLine next = text.Lines[lastLine.LineNumber + 1];
            string before = firstLine.LineNumber == 0 ? "" : text.Lines[firstLine.LineNumber - 1].ToString().Trim();
            if (string.IsNullOrWhiteSpace(next.ToString()) && (before.Length == 0 || before.EndsWith('{')))
            {
                end = next.EndIncludingLineBreak;
            }
        }
        return new TextChange(TextSpan.FromBounds(firstLine.Start, end), "");
    }

    /// <summary>Where a member's first line (doc comment included) begins, and its indentation.</summary>
    static (int LineStart, string Indent) MemberStart(SourceText text, SyntaxNode member)
    {
        SyntaxTrivia first = member.GetLeadingTrivia().FirstOrDefault(t =>
            !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia));
        int start = first != default ? first.Span.Start : member.Span.Start;
        TextLine line = text.Lines.GetLineFromPosition(start);
        string lineText = line.ToString();
        string indent = lineText[..(lineText.Length - lineText.TrimStart().Length)];
        return (line.Start, indent);
    }
}

/// <summary>
/// The source <c>--emit-helpers</c> writes into a converted test project: <c>MessageAssert</c> and
/// the <c>DoNotParallelize</c> collection. Emitted rather than copied by hand, so the helpers and the
/// rules that call them always come from the same commit of this tool.
/// </summary>
/// <remarks>
/// ⓘ <c>MessageAssert</c> began as <c>Bennewitz.Ninja.ScopedEditors</c>'
/// <c>tests/ScopedEditors.Tests/Support/MessageAssert.cs</c> (at <c>6525d87</c>), written when that
/// repository's suite was ported from MSTest by hand. The members after <c>IsAssignableFrom</c> were
/// added here for the rules that repository's port never needed.
/// </remarks>
static class Helpers
{
    public static string Source(string ns) => Template.Replace("__NAMESPACE__", ns, StringComparison.Ordinal);

    const string Template = """
        // Emitted by Bennewitz.Ninja.Templates scripts/mstest-to-xunit.cs --emit-helpers.
        // Regenerate rather than edit: the conversion rules call exactly these members.

        using System.Diagnostics.CodeAnalysis;
        using System.Text.RegularExpressions;
        using Xunit;
        using Xunit.Sdk;

        namespace __NAMESPACE__;

        /// <summary>
        /// xUnit's own assertions, with the explanatory message the original MSTest assertion carried.
        /// </summary>
        /// <remarks>
        /// <para>
        /// xUnit deliberately gives <c>Equal</c>, <c>Null</c>, <c>Same</c> and <c>Contains</c> no message
        /// parameter. A converted MSTest suite's messages are routinely the whole point of the
        /// assertion: they say WHY the value matters, which is what a reader needs when it fails.
        /// </para>
        /// <para>
        /// ⭐ Each method CALLS xUnit's assertion and prefixes the message only when it fails, so a failure
        /// still shows xUnit's expected/actual diff, and every argument is evaluated exactly once. The
        /// alternative, <c>Assert.True(Equals(e, a), message)</c>, loses the diff and evaluates twice.
        /// </para>
        /// <para>
        /// ⚠ Not an MSTest compatibility layer, and not for new tests: it exists for converted assertions
        /// that carried a message. New tests use xUnit's Assert directly.
        /// </para>
        /// </remarks>
        internal static class MessageAssert
        {
            public static void Equal<T>(T expected, T actual, string message) =>
                With(message, () => Assert.Equal(expected, actual));

            /// <summary>MSTest's AreEqual(double, double, delta, message): |expected - actual| &lt;= tolerance.</summary>
            public static void Equal(double expected, double actual, double tolerance, string message) =>
                With(message, () => Assert.Equal(expected, actual, tolerance));

            public static void NotEqual<T>(T expected, T actual, string message) =>
                With(message, () => Assert.NotEqual(expected, actual));

            public static void Null(object? value, string message) =>
                With(message, () => Assert.Null(value));

            /// <remarks>
            /// Written out rather than delegated: [NotNull] promises the caller's flow analysis that the
            /// value is non-null afterwards, and the compiler cannot see that promise kept through a lambda.
            /// </remarks>
            public static void NotNull([NotNull] object? value, string message)
            {
                if (value is null)
                {
                    throw new XunitException(message + Environment.NewLine + "Assert.NotNull() Failure: Value is null");
                }
            }

            public static void Same(object? expected, object? actual, string message) =>
                With(message, () => Assert.Same(expected, actual));

            public static void Contains<T>(T expected, IEnumerable<T> collection, string message) =>
                With(message, () => Assert.Contains(expected, collection));

            public static void Contains(string expectedSubstring, string? actualString, string message) =>
                With(message, () => Assert.Contains(expectedSubstring, actualString));

            public static T IsAssignableFrom<T>(object? value, string message)
            {
                T result = default!;
                With(message, () => result = Assert.IsAssignableFrom<T>(value));
                return result;
            }

            // ── Added for the conversion rules; not in the ScopedEditors original ──

            /// <remarks>void, like both MSTest's non-generic IsInstanceOfType and xUnit's non-generic IsAssignableFrom.</remarks>
            public static void IsAssignableFrom(Type expectedType, object? value, string message) =>
                With(message, () => Assert.IsAssignableFrom(expectedType, value));

            public static void NotSame(object? expected, object? actual, string message) =>
                With(message, () => Assert.NotSame(expected, actual));

            public static void DoesNotContain<T>(T expected, IEnumerable<T> collection, string message) =>
                With(message, () => Assert.DoesNotContain(expected, collection));

            public static void DoesNotContain(string expectedSubstring, string? actualString, string message) =>
                With(message, () => Assert.DoesNotContain(expectedSubstring, actualString));

            public static void StartsWith(string? expectedStart, string? actualString, string message) =>
                With(message, () => Assert.StartsWith(expectedStart, actualString));

            public static void EndsWith(string? expectedEnd, string? actualString, string message) =>
                With(message, () => Assert.EndsWith(expectedEnd, actualString));

            public static void Matches(Regex expectedRegex, string? actualString, string message) =>
                With(message, () => Assert.Matches(expectedRegex, actualString));

            public static void DoesNotMatch(Regex expectedRegex, string? actualString, string message) =>
                With(message, () => Assert.DoesNotMatch(expectedRegex, actualString));

            /// <summary>MSTest's CollectionAssert.AreEqual with a message: same elements, same order.</summary>
            public static void SequenceEqual<T>(IEnumerable<T>? expected, IEnumerable<T>? actual, string message) =>
                With(message, () => Assert.Equal(expected, actual));

            public static T Throws<T>(Action testCode, string message) where T : Exception
            {
                T result = default!;
                With(message, () => result = Assert.Throws<T>(testCode));
                return result;
            }

            public static async Task<T> ThrowsAsync<T>(Func<Task> testCode, string message) where T : Exception
            {
                try
                {
                    return await Assert.ThrowsAsync<T>(testCode);
                }
                catch (XunitException failure)
                {
                    throw new XunitException(message + Environment.NewLine + failure.Message, failure);
                }
            }

            /// <summary>
            /// MSTest's CollectionAssert.AreEquivalent: the same elements with the same multiplicity, in
            /// any order, compared with the default equality. Both null passes; one null fails.
            /// </summary>
            /// <remarks>
            /// ⚠ Deliberately NOT xUnit's <c>Assert.Equivalent</c>, which compares object graphs
            /// structurally and by different rules. A converted assertion must keep the meaning it had.
            /// </remarks>
            public static void SameElements<T>(IEnumerable<T>? expected, IEnumerable<T>? actual, string? message = null)
            {
                if (expected is null && actual is null)
                {
                    return;
                }
                if (expected is null || actual is null)
                {
                    Fail(message, $"one collection is null (expected {(expected is null ? "null" : "non-null")}, actual {(actual is null ? "null" : "non-null")})");
                    return;
                }

                List<T> e = [.. expected], a = [.. actual];
                var remaining = new List<T>(a);
                var missing = new List<T>();
                foreach (T item in e)
                {
                    int at = remaining.FindIndex(x => EqualityComparer<T>.Default.Equals(x, item));
                    if (at < 0)
                    {
                        missing.Add(item);
                    }
                    else
                    {
                        remaining.RemoveAt(at);
                    }
                }
                if (missing.Count > 0 || remaining.Count > 0)
                {
                    Fail(message,
                        $"collections do not hold the same elements (expected {e.Count}, actual {a.Count}); "
                        + $"missing from actual: [{string.Join(", ", missing)}]; unexpected in actual: [{string.Join(", ", remaining)}]");
                }
            }

            static void Fail(string? message, string detail) =>
                throw new XunitException((message is null or "" ? "" : message + Environment.NewLine)
                                         + "MessageAssert.SameElements() Failure: " + detail);

            private static void With(string message, Action assertion)
            {
                try
                {
                    assertion();
                }
                catch (XunitException failure)
                {
                    throw new XunitException(message + Environment.NewLine + failure.Message, failure);
                }
            }
        }

        /// <summary>
        /// The collection a converted <c>[DoNotParallelize]</c> class joins. Its tests run after every
        /// parallel collection has finished, one at a time — MSTest's meaning for the attribute.
        /// </summary>
        [CollectionDefinition("DoNotParallelize", DisableParallelization = true)]
        public sealed class DoNotParallelizeDefinition;
        """;
}
