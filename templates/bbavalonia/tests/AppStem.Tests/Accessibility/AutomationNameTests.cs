using Bennewitz.Ninja.AppStem.Tests.Support;
using Bennewitz.Ninja.XamlQuality;
using Bennewitz.Ninja.XamlQuality.Rules;

namespace Bennewitz.Ninja.AppStem.Tests.Accessibility;

/// <summary>
/// XamlQuality's naming rules over the app's markup: what an automation client, a screen reader or
/// an agent driving the app reads out must be there. See docs/ai-drivable-ui.md in
/// Bennewitz.Ninja.XamlQuality.
/// </summary>
/// <remarks>
/// ⛔ Each test asserts how much it inspected as well as what it found. A rule that inspected
/// nothing finds nothing, so a moved folder or an unparsed file would otherwise pass in silence.
/// </remarks>
public sealed class AutomationNameTests
{
    private static XamlScanContext Markup()
    {
        XamlScanContext context = XamlScanContext.Load(RepoPaths.App);
        Assert.True(context.Files.Count > 0, $"No markup found under {RepoPaths.App}.");
        return context;
    }

    /// <summary>XQ1002: every interactive control carries an automation Name.</summary>
    [Fact]
    public void XQ1002_every_interactive_control_is_named()
    {
        XamlRuleResult result = new InteractiveAutomationNameRule().Analyze(Markup());

        // The template's window has two: the name box and Greet. Raise this as the app grows.
        Assert.True(result.Inspected >= 2, $"XQ1002 inspected {result.Inspected} controls; expected at least 2.");
        Assert.True(result.Findings.Count == 0, string.Join(Environment.NewLine, result.Findings));
    }

    /// <summary>
    /// XQ1001: every Expander carries an automation Name, which its header does not supply: a
    /// screen reader announces an unnamed Expander as just "expander".
    /// </summary>
    [Fact]
    public void XQ1001_every_expander_is_named()
    {
        XamlRuleResult result = new ExpanderAutomationNameRule().Analyze(Markup());

        Assert.True(result.Inspected > 0, "XQ1001 inspected no Expander, so it proved nothing.");
        Assert.True(result.Findings.Count == 0, string.Join(Environment.NewLine, result.Findings));
    }
}
