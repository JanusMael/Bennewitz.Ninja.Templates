using System.Text;
using Bennewitz.Ninja.PkgStem;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PkgStem.Tests;

/// <summary>
/// Guards what every rule says about itself: an ID in the package's scheme, the package as its
/// category, its README section as its help link, and a README table that matches the descriptors.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>The README rules table is generated.</b> Set <c>RULES_UPDATE_DOCS=1</c> and run the tests
/// to regenerate it; otherwise this fails when the table and the analyzers disagree, which is the
/// only way a table written by hand stays true.
/// </para>
/// <para>
/// ⚠ <b>The help link is checked against the README's headings</b>, because a link to a section
/// that does not exist is what a consumer sees as a 404 on the rule they were just told about.
/// </para>
/// </remarks>
public sealed class RulesCatalogTests
{
    private const string Begin = "<!-- BEGIN GENERATED RULES -->";
    private const string End = "<!-- END GENERATED RULES -->";

    [Fact]
    public void Every_rule_has_an_id_in_the_scheme_and_says_where_it_comes_from()
    {
        DiagnosticDescriptor[] rules = Rules();
        Assert.NotEmpty(rules);

        foreach (DiagnosticDescriptor rule in rules)
        {
            Assert.Matches("^" + Catalog.Prefix + "[0-9]{4}$", rule.Id);
            Assert.Equal(Catalog.Category, rule.Category);
            Assert.Equal(Catalog.HelpLink(rule.Id), rule.HelpLinkUri);
        }

        Assert.Equal(rules.Length, rules.Select(rule => rule.Id).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Every_rule_has_its_section_in_the_README()
    {
        string readme = Readme();

        foreach (DiagnosticDescriptor rule in Rules())
        {
            Assert.Contains("\n### " + rule.Id + "\n", readme, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_README_rules_table_matches_the_descriptors()
    {
        string readme = Readme();

        int begin = readme.IndexOf(Begin, StringComparison.Ordinal);
        int end = readme.IndexOf(End, StringComparison.Ordinal);
        Assert.True(begin >= 0 && end > begin, "README.md has no generated rules block between the BEGIN and END markers.");

        string expected = Begin + "\n" + Table() + End;
        string actual = readme.Substring(begin, end + End.Length - begin);

        if (expected != actual && Environment.GetEnvironmentVariable("RULES_UPDATE_DOCS") == "1")
        {
            File.WriteAllText(ReadmePath(), readme.Substring(0, begin) + expected + readme.Substring(end + End.Length));
            actual = expected;
        }

        Assert.True(expected == actual, "The README rules table has drifted from the analyzers. Set RULES_UPDATE_DOCS=1 and run the tests to regenerate it.\nExpected:\n" + expected + "\nActual:\n" + actual);
    }

    private static string Table()
    {
        StringBuilder table = new();
        table.Append("| Id | Default | Reports |\n|---|---|---|\n");

        foreach (DiagnosticDescriptor rule in Rules())
        {
            table.Append("| [`").Append(rule.Id).Append("`](#").Append(rule.Id.ToLowerInvariant()).Append(") | ")
                .Append(rule.IsEnabledByDefault ? "on" : "off").Append(" | ")
                .Append(rule.Title.ToString()).Append(" |\n");
        }

        return table.ToString();
    }

    private static DiagnosticDescriptor[] Rules() =>
    [
        .. typeof(ScopedAnalyzer<>).Assembly.GetTypes()
            .Where(type => type.IsDefined(typeof(DiagnosticAnalyzerAttribute), inherit: false))
            .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type)!)
            .SelectMany(analyzer => analyzer.SupportedDiagnostics)
            .OrderBy(rule => rule.Id, StringComparer.Ordinal),
    ];

    /// <summary>The README with its line endings normalised, so a CRLF checkout reads as git stores it.</summary>
    private static string Readme() => File.ReadAllText(ReadmePath()).Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string ReadmePath() => Path.Combine(RepoRoot(), "README.md");

    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !Directory.EnumerateFiles(directory.FullName, "*.slnx").Any())
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
