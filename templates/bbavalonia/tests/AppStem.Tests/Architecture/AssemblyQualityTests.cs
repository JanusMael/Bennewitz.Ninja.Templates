using System.Reflection;
using Bennewitz.Ninja.AssemblyQuality;
using Bennewitz.Ninja.AssemblyQuality.Rules;

namespace Bennewitz.Ninja.AppStem.Tests.Architecture;

/// <summary>
/// AssemblyQuality's rules over the app's compiled assembly, read the way it ships.
/// </summary>
/// <remarks>
/// AQ1003, the layering rule, is left out: it needs a table of which project may reference what,
/// and a one-project app has no layers yet. Add it with the second project.
/// </remarks>
public sealed class AssemblyQualityTests
{
    private static readonly Assembly Shipped = typeof(App).Assembly;

    /// <summary>AQ1001: no public method takes a <c>CancellationToken</c> with a default.</summary>
    [Fact]
    public void AQ1001_no_public_method_takes_a_defaulted_cancellation_token()
    {
        AssemblyRuleResult result = new CancellationTokenRule().Analyze(AssemblyScanContext.Of(Shipped));

        Assert.Empty(result.Findings);
    }

    /// <summary>AQ1002: no implementation type leaks through the public surface.</summary>
    [Fact]
    public void AQ1002_no_leak_prone_type_appears_in_the_public_surface()
    {
        AssemblyRuleResult result = new SurfaceLeakRule().Analyze(AssemblyScanContext.Of(Shipped));

        Assert.True(result.Inspected > 0, "AQ1002 inspected nothing, so it proved nothing.");
        Assert.Empty(result.Findings);
    }

    /// <summary>
    /// AQ1004: no namespace segment shadows a referenced root. A segment named <c>Avalonia</c>
    /// inside this assembly would make <c>Avalonia.Media</c> resolve to it, failing with CS0234.
    /// </summary>
    [Fact]
    public void AQ1004_no_namespace_segment_shadows_a_referenced_root()
    {
        AssemblyRuleResult result = new NamespaceShadowRule().Analyze(AssemblyScanContext.Of(Shipped));

        Assert.True(result.Inspected > 0, "AQ1004 inspected nothing, so it proved nothing.");
        Assert.Empty(result.Findings);
    }
}
