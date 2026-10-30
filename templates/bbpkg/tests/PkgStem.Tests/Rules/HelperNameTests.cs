using Bennewitz.Ninja.PkgStem.Rules;
using Microsoft.CodeAnalysis;

namespace PkgStem.Tests.Rules;

/// <summary>
/// BNXX1001, proved the three ways every rule is: it fires on a violation, stays silent on clean
/// code, and stays silent out of scope.
/// </summary>
/// <remarks>
/// ⚠ Only the first can fail a rule whose gate is wrong. A gate that never lets a compilation in
/// passes the other two, which is why a rule without a firing test proves nothing.
/// </remarks>
public sealed class HelperNameTests
{
    [Fact]
    public Task A_public_helper_in_a_library_is_reported() =>
        RuleTest<HelperNameAnalyzer>.Run("""
            public class {|BNXX1001:StringHelper|}
            {
                public class {|BNXX1001:NestedHelper|} { }
            }
            """);

    [Fact]
    public Task A_type_named_for_what_it_does_or_out_of_a_consumers_reach_is_not() =>
        RuleTest<HelperNameAnalyzer>.Run("""
            public class StringJoiner
            {
                private class NestedHelper { }
            }

            internal class StringHelper
            {
                public class NestedHelper { }
            }
            """);

    [Fact]
    public Task An_application_is_out_of_scope()
    {
        RuleTest<HelperNameAnalyzer> test = new("""
            public class StringHelper { }

            internal static class Program
            {
                private static void Main() { }
            }
            """);
        test.TestState.OutputKind = OutputKind.ConsoleApplication;

        return test.Run();
    }
}
