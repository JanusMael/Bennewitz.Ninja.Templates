#!/usr/bin/env dotnet
// Applies xUnit1051 ("pass TestContext.Current.CancellationToken") to every call it flags, which
// `dotnet format` cannot: xUnit's UseCancellationTokenFixer does not support Fix All, so format
// reports "Unable to fix xUnit1051" and changes nothing.
//
//   dotnet run --file scripts/xunit1051-fix.cs -- <solution-or-project> [--configuration <c>]
//
// Lift any xUnit1051 suppression first — a suppressed diagnostic is never reported, so nothing is
// found. The script builds the target with a per-project SARIF error log injected through
// CustomAfterMicrosoftCommonTargets, because SARIF carries each diagnostic's EXACT span: a chained
// call `a.B().CAsync()` has two invocations that start at the same character, and only the span
// says which one was flagged. Each flagged call gets `TestContext.Current.CancellationToken` as a
// new last argument, by syntax tree, so comments and line breaks survive. Then it rebuilds.
//
// ⚠ Appending is what the fixer does when the token is the next parameter. When a call skips an
// optional parameter before the token, the appended argument binds to THAT parameter and the call
// stops compiling — which is why the rebuild is part of the script: every such site is listed.
//
// Exit codes: 0 every site fixed and the build is clean · 2 sites listed that need a hand ·
// 3 the script could not run (a failed first build, or nothing flagged).

#:package Microsoft.CodeAnalysis.CSharp

using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

if (args.Length < 1 || args[0] is "-h" or "--help")
{
    Console.WriteLine("usage: dotnet run --file scripts/xunit1051-fix.cs -- <solution-or-project> [--configuration <c>]");
    return args.Length < 1 ? 3 : 0;
}

string target = Path.GetFullPath(args[0]);
string configuration = "Debug";
string[] names = ["cancellationToken", "ct", "token"];
for (int i = 1; i < args.Length; i++)
{
    if (args[i] == "--configuration" && i + 1 < args.Length)
    {
        configuration = args[++i];
    }
    else if (args[i] == "--names" && i + 1 < args.Length)
    {
        names = args[++i].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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

string work = Path.Combine(Path.GetTempPath(), "xunit1051-fix-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(work);
try
{
    (int firstExit, string firstOutput, List<Site> sites) = Fix.Build(target, configuration, work, "before");
    if (firstExit != 0)
    {
        Console.Error.WriteLine(firstOutput);
        Console.Error.WriteLine($"the target did not build before any change (exit {firstExit})");
        return 3;
    }
    if (sites.Count == 0)
    {
        Console.Error.WriteLine("no xUnit1051 reported: nothing to fix, or it is suppressed (NoWarn, .editorconfig)");
        return 3;
    }

    int changed = 0;
    var unmatched = new List<Site>();
    foreach (var file in sites.GroupBy(s => s.Path, StringComparer.OrdinalIgnoreCase))
    {
        (int count, List<Site> missed) = Fix.Rewrite(file.Key, [.. file], names[0]);
        changed += count;
        unmatched.AddRange(missed);
    }
    Console.WriteLine($"{sites.Count} sites flagged, {changed} calls given the token, in {sites.Select(s => s.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count()} files");
    foreach (Site s in unmatched)
    {
        Console.WriteLine($"NEEDS A HAND {s.Display}: no call has exactly the flagged span");
    }

    (int secondExit, string secondOutput, List<Site> remaining) = Fix.Build(target, configuration, work, "after");

    // A call that skips an optional parameter before the token binds the appended argument to that
    // parameter (CS1503). Name it instead, trying each candidate name in turn: a named argument only
    // compiles when a parameter of that name accepts the token, so the compiler is the judge.
    foreach (string name in names)
    {
        var misbound = Fix.MisboundTokens(secondOutput);
        if (misbound.Count == 0)
        {
            break;
        }
        int named = 0;
        foreach (var file in misbound.GroupBy(e => e.Path, StringComparer.OrdinalIgnoreCase))
        {
            named += Fix.NameTokens(file.Key, [.. file.Select(e => (e.Line, e.Column))], name);
        }
        Console.WriteLine($"named {named} misbound tokens `{name}:`");
        (secondExit, secondOutput, remaining) = Fix.Build(target, configuration, work, "named-" + name);
    }

    var errors = Fix.Errors().Matches(secondOutput).Select(m => m.Value.Trim()).Distinct(StringComparer.Ordinal).ToList();
    foreach (string error in errors)
    {
        Console.WriteLine($"NEEDS A HAND {error}");
    }
    foreach (Site s in remaining)
    {
        Console.WriteLine($"NEEDS A HAND {s.Display}: still xUnit1051");
    }
    if (secondExit != 0 && errors.Count == 0)
    {
        Console.Error.WriteLine(secondOutput);
        Console.Error.WriteLine($"the rebuild failed (exit {secondExit}) with no compiler error to list");
        return 3;
    }
    return unmatched.Count + errors.Count + remaining.Count == 0 ? 0 : 2;
}
finally
{
    Directory.Delete(work, recursive: true);
}

internal sealed record Site(string Path, int StartLine, int StartColumn, int EndLine, int EndColumn)
{
    public string Display => $"{Path}({StartLine},{StartColumn})";
}

internal static partial class Fix
{
    [GeneratedRegex(@"^\s*\S.*?\(\d+,\d+\): error [A-Z]+\d+: .*$", RegexOptions.Multiline)]
    public static partial Regex Errors();

    // Imported last by every project. ErrorLog and WarningsNotAsErrors are read when the compiler
    // runs, after evaluation, so setting them here reaches it; the project name keeps logs apart.
    private const string InjectTargets = """
        <Project>
          <PropertyGroup>
            <ErrorLog>{LOGS}$(MSBuildProjectName).$(TargetFramework).sarif,version=2.1</ErrorLog>
            <WarningsNotAsErrors>$(WarningsNotAsErrors);xUnit1051</WarningsNotAsErrors>
          </PropertyGroup>
        </Project>
        """;

    public static (int ExitCode, string Output, List<Site> Sites) Build(string target, string configuration, string work, string phase)
    {
        string logs = Path.Combine(work, phase) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(logs);
        string inject = Path.Combine(work, phase + ".targets");
        File.WriteAllText(inject, InjectTargets.Replace("{LOGS}", logs, StringComparison.Ordinal));

        // --no-incremental: an up-to-date project skips the compiler, and with it every diagnostic.
        (int exit, string output) = Dotnet(Path.GetDirectoryName(target)!,
            "build", target, "-c", configuration, "--no-incremental", "-tl:off", "-clp:NoSummary",
            "-p:CustomAfterMicrosoftCommonTargets=" + inject);

        var sites = new HashSet<Site>();
        foreach (string sarif in Directory.EnumerateFiles(logs, "*.sarif"))
        {
            JsonNode? root = JsonNode.Parse(File.ReadAllText(sarif));
            foreach (JsonNode? result in root?["runs"]?.AsArray().SelectMany(r => r?["results"]?.AsArray() ?? []) ?? [])
            {
                if ((string?)result?["ruleId"] != "xUnit1051")
                {
                    continue;
                }
                JsonNode? location = result?["locations"]?[0]?["physicalLocation"];
                string? uri = (string?)location?["artifactLocation"]?["uri"];
                JsonNode? region = location?["region"];
                if (uri is null || region is null)
                {
                    continue;
                }
                sites.Add(new Site(new Uri(uri).LocalPath,
                    (int)region["startLine"]!, (int)region["startColumn"]!, (int)region["endLine"]!, (int)region["endColumn"]!));
            }
        }
        return (exit, output, [.. sites.OrderBy(s => s.Path, StringComparer.Ordinal).ThenBy(s => s.StartLine).ThenBy(s => s.StartColumn)]);
    }

    public static (int Changed, List<Site> Unmatched) Rewrite(string path, List<Site> sites, string firstName)
    {
        byte[] bytes = File.ReadAllBytes(path);
        bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        SourceText text = SourceText.From(new MemoryStream(bytes), Encoding.UTF8);
        SyntaxTree tree = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Preview), path);
        SyntaxNode root = tree.GetRoot();

        var targets = new List<SyntaxNode>();
        var unmatched = new List<Site>();
        foreach (Site site in sites)
        {
            // SARIF lines and columns are 1-based, and the end column is exclusive.
            TextSpan span = text.Lines.GetTextSpan(new LinePositionSpan(
                new LinePosition(site.StartLine - 1, site.StartColumn - 1), new LinePosition(site.EndLine - 1, site.EndColumn - 1)));
            // Innermost for a tie: a call passed as an argument has the same span as its ArgumentSyntax,
            // and the outermost node would be the argument, above the call.
            SyntaxNode? call = root.FindNode(span, getInnermostNodeForTie: true).AncestorsAndSelf()
                .FirstOrDefault(n => n.Span == span && n is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax);
            if (call is null)
            {
                unmatched.Add(site);
            }
            else
            {
                targets.Add(call);
            }
        }

        SyntaxNode rewritten = root.ReplaceNodes(targets, (_, current) => current switch
        {
            InvocationExpressionSyntax invocation => invocation.WithArgumentList(WithToken(invocation.ArgumentList, firstName)),
            BaseObjectCreationExpressionSyntax creation => creation.WithArgumentList(WithToken(creation.ArgumentList, firstName)),
            _ => current,
        });
        if (targets.Count > 0)
        {
            File.WriteAllText(path, rewritten.ToFullString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: bom));
        }
        return (targets.Count, unmatched);
    }

    private const string TokenExpression = "TestContext.Current.CancellationToken";

    // "path(line,col): error CS1503: Argument N: cannot convert from 'System.Threading.CancellationToken' ..."
    // after a positional token, or CS1739 "does not have a parameter named" after a wrong name.
    [GeneratedRegex(@"^\s*(?<path>\S.*?)\((?<line>\d+),(?<col>\d+)\): error (?:CS1503: Argument \d+: cannot convert from 'System\.Threading\.CancellationToken'|CS1739:)", RegexOptions.Multiline)]
    private static partial Regex Misbound();

    public static List<(string Path, int Line, int Column)> MisboundTokens(string buildOutput) =>
        [.. Misbound().Matches(buildOutput)
            .Select(m => (m.Groups["path"].Value, int.Parse(m.Groups["line"].Value), int.Parse(m.Groups["col"].Value)))
            .Distinct()];

    /// <summary>
    /// Names the token argument the compiler rejected at each (line, column). Only an argument this
    /// script wrote is touched: its expression is exactly <see cref="TokenExpression"/>.
    /// </summary>
    public static int NameTokens(string path, List<(int Line, int Column)> positions, string name)
    {
        byte[] bytes = File.ReadAllBytes(path);
        bool bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        SourceText text = SourceText.From(new MemoryStream(bytes), Encoding.UTF8);
        SyntaxNode root = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.Preview), path).GetRoot();

        var arguments = positions
            .Select(p => text.Lines.GetPosition(new LinePosition(p.Line - 1, p.Column - 1)))
            .Select(position => root.FindToken(position).Parent?.AncestorsAndSelf().OfType<ArgumentSyntax>().FirstOrDefault())
            .OfType<ArgumentSyntax>()
            .Where(a => a.Expression.ToString() == TokenExpression)
            .Distinct()
            .ToList();
        if (arguments.Count == 0)
        {
            return 0;
        }
        SyntaxNode rewritten = root.ReplaceNodes(arguments, (_, current) =>
            current.WithNameColon(SyntaxFactory.NameColon(name).WithTrailingTrivia(SyntaxFactory.Space)));
        File.WriteAllText(path, rewritten.ToFullString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: bom));
        return arguments.Count;
    }

    private static ArgumentListSyntax WithToken(ArgumentListSyntax? list, string firstName)
    {
        ArgumentSyntax token = SyntaxFactory.Argument(SyntaxFactory.ParseExpression(TokenExpression));
        // After a named argument an unnamed one may not follow out of position (CS8323), so the token
        // is named from the start; a wrong name is then CS1739, which the naming rounds correct.
        if (list is not null && list.Arguments.Any(a => a.NameColon is not null))
        {
            token = token.WithNameColon(SyntaxFactory.NameColon(firstName).WithTrailingTrivia(SyntaxFactory.Space));
        }
        if (list is null || list.Arguments.Count == 0)
        {
            return (list ?? SyntaxFactory.ArgumentList()).WithArguments(SyntaxFactory.SingletonSeparatedList(token));
        }
        var nodesAndTokens = new List<SyntaxNodeOrToken>(list.Arguments.GetWithSeparators())
        {
            SyntaxFactory.Token(SyntaxKind.CommaToken).WithTrailingTrivia(SyntaxFactory.Space),
            token,
        };
        return list.WithArguments(SyntaxFactory.SeparatedList<ArgumentSyntax>(nodesAndTokens));
    }

    private static (int ExitCode, string Output) Dotnet(string workingDirectory, params string[] arguments)
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
}
