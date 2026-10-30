using System.Reflection;
using System.Text.RegularExpressions;
using Bennewitz.Ninja.AssemblyQuality;
using Bennewitz.Ninja.AssemblyQuality.Rules;
using Bennewitz.Ninja.PkgStem;

namespace PkgStem.Tests.Architecture;

/// <summary>
/// AssemblyQuality's rules over the library's compiled assembly, read the way it ships: the shape a
/// consumer binds against.
/// </summary>
/// <remarks>
/// Each rule is held to AssemblyQuality's checks: it skipped nothing, it found nothing, and, where
/// the library gives it a candidate, it inspected something. Findings alone cannot fail a rule that
/// never looked. BNAQ1001 counts only public methods that take a token, which a new library has none
/// of. BNAQ1003, the layering rule, is left out: it needs a table of which project may reference
/// what, and a one-project library has no layers yet. Add it with the second project. Scan each
/// shipped assembly once there are several.
/// </remarks>
public sealed class AssemblyQualityTests
{
#if (Analyzer)
    private static readonly Assembly Shipped = typeof(Catalog).Assembly;
#else
    private static readonly Assembly Shipped = typeof(Greeting).Assembly;
#endif

    /// <summary>
    /// BNAQ1005: every grant compiled into the library names an assembly the repository's two grant
    /// files allow: the generated list of its own assemblies, and the hand-written External file's
    /// names.
    /// </summary>
    /// <remarks>
    /// Read from the compiled assembly, so a grant from anywhere else (a stray attribute, a project
    /// item, a generator) fails here even when the conventions check cannot see it. BNAQ1005 loads
    /// nothing, so it never fills Skipped, and that assertion is left out.
    /// </remarks>
    [Fact]
    public void BNAQ1005_every_grant_names_an_allowed_assembly()
    {
        string[] allowed =
        [
            .. GrantedNames("AssemblyInfo.InternalsVisibleTo.cs"),
            .. GrantedNames("AssemblyInfo.InternalsVisibleTo.External.cs"),
        ];

        AssemblyRuleResult result = new FriendGrantRule(allowed).Analyze(AssemblyScanContext.Of(Shipped));

        Assert.True(result.Inspected > 0, "BNAQ1005 inspected no grant, so it proved nothing.");
        Assert.True(result.Findings.Count == 0, string.Join("\n", result.Findings));
    }

    /// <summary>BNAQ1001: no public method takes a <c>CancellationToken</c> with a default.</summary>
    [Fact]
    public void BNAQ1001_no_public_method_takes_a_defaulted_cancellation_token()
    {
        AssemblyRuleResult result = new CancellationTokenRule().Analyze(AssemblyScanContext.Of(Shipped));

        Assert.True(result.Skipped.Count == 0, string.Join("\n", result.Skipped));
        Assert.True(result.Findings.Count == 0, string.Join("\n", result.Findings));
    }

    /// <summary>
    /// BNAQ1002: no type from a leak-prone namespace appears in the public surface, where every
    /// consumer would come to depend on it.
    /// </summary>
    /// <remarks>
    /// ⚠ This one cannot fail yet, and says so by inspecting nothing: the rule's default namespaces
    /// are serializer DOMs, and a new library references none. It starts to inspect the day the
    /// library references one. When it takes its first dependency whose types should stay inside it,
    /// name that dependency's namespace with <c>new SurfaceLeakRule([...])</c> and assert
    /// <c>Inspected &gt; 0</c>, as the other rules here do.
    /// </remarks>
    [Fact]
    public void BNAQ1002_no_leak_prone_type_appears_in_the_public_surface()
    {
        AssemblyRuleResult result = new SurfaceLeakRule().Analyze(AssemblyScanContext.Of(Shipped));

        Assert.True(result.Skipped.Count == 0, string.Join("\n", result.Skipped));
        Assert.True(result.Findings.Count == 0, string.Join("\n", result.Findings));
    }

    /// <summary>
    /// BNAQ1004: no namespace segment shadows a referenced root. A segment named <c>System</c> inside
    /// this assembly would make <c>System.IO</c> resolve to it in every consumer, failing with CS0234.
    /// </summary>
    [Fact]
    public void BNAQ1004_no_namespace_segment_shadows_a_referenced_root()
    {
        AssemblyRuleResult result = new NamespaceShadowRule().Analyze(AssemblyScanContext.Of(Shipped));

        Assert.True(result.Inspected > 0, "BNAQ1004 inspected nothing, so it proved nothing.");
        Assert.True(result.Skipped.Count == 0, string.Join("\n", result.Skipped));
        Assert.True(result.Findings.Count == 0, string.Join("\n", result.Findings));
    }

    /// <summary>The names a grant file grants to, comment lines left out.</summary>
    private static IEnumerable<string> GrantedNames(string file) =>
        File.ReadLines(Path.Combine(RepoRoot(), file))
            .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal))
            .SelectMany(line => Regex.Matches(line, "InternalsVisibleTo\\(\"([^\"]+)\"\\)").Select(m => m.Groups[1].Value));

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
