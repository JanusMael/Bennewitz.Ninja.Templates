using System.Reflection;
using Bennewitz.Ninja.AssemblyQuality;
using Bennewitz.Ninja.AssemblyQuality.Rules;

namespace Bennewitz.Ninja.AppStem.Tests.Architecture;

/// <summary>
/// AssemblyQuality's rules over the app's compiled assembly, read the way it ships.
/// </summary>
/// <remarks>
/// Each rule is held to AssemblyQuality's checks: it skipped nothing, it found nothing, and, where
/// the app gives it a candidate, it inspected something. Findings alone cannot fail a rule that never
/// looked. BNAQ1001 counts only methods that take a token, which a new app has none of.
/// BNAQ1003, the layering rule, is left out: it needs a table of which project may reference what,
/// and a one-project app has no layers yet. Add it with the second project.
/// </remarks>
public sealed class AssemblyQualityTests
{
    private static readonly Assembly Shipped = typeof(App).Assembly;

    /// <summary>BNAQ1001: no public method takes a <c>CancellationToken</c> with a default.</summary>
    [Fact]
    public void BNAQ1001_no_public_method_takes_a_defaulted_cancellation_token()
    {
        AssemblyRuleResult result = new CancellationTokenRule().Analyze(AssemblyScanContext.Of(Shipped));

        Assert.Empty(result.Skipped);
        Assert.Empty(result.Findings);
    }

    /// <summary>
    /// BNAQ1002: no Serilog type reaches the public surface, such as a logger in a view model's
    /// constructor. The rule's default namespaces are serializer DOMs this app does not reference,
    /// so with them it could never fire and would report that it inspected nothing. Add each library
    /// the app comes to reference directly whose types should stay inside it.
    /// </summary>
    [Fact]
    public void BNAQ1002_no_referenced_implementation_type_appears_in_the_public_surface()
    {
        AssemblyRuleResult result = SurfaceLeakRule.Only(["Serilog"]).Analyze(AssemblyScanContext.Of(Shipped));

        Assert.True(result.Inspected > 0, "BNAQ1002 inspected nothing, so it proved nothing.");
        Assert.Empty(result.Skipped);
        Assert.Empty(result.Findings);
    }

    /// <summary>
    /// BNAQ1004: no namespace segment shadows a referenced root. A segment named <c>Avalonia</c>
    /// inside this assembly would make <c>Avalonia.Media</c> resolve to it, failing with CS0234.
    /// </summary>
    [Fact]
    public void BNAQ1004_no_namespace_segment_shadows_a_referenced_root()
    {
        AssemblyRuleResult result = new NamespaceShadowRule().Analyze(AssemblyScanContext.Of(Shipped));

        Assert.True(result.Inspected > 0, "BNAQ1004 inspected nothing, so it proved nothing.");
        Assert.Empty(result.Skipped);
        Assert.Empty(result.Findings);
    }
}
