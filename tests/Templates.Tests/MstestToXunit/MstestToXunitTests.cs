using System.Diagnostics;

namespace Templates.Tests.MstestToXunit;

/// <summary>
/// <c>scripts/mstest-to-xunit.cs</c>, run exactly as a person runs it, over fixtures whose expected
/// output was read line by line before it was accepted.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>Two fixtures, two promises.</b> <c>Rules</c> exercises every rule and must convert with
/// nothing unmapped. <c>Unmapped</c> holds forms the tool must REFUSE — each listed with its line,
/// and each left exactly as written, because a guessed conversion that compiles is worse than an
/// MSTest attribute that does not.
/// </para>
/// <para>
/// ⚠ <b>The fixtures are <c>.cs.txt</c>, not <c>.cs</c></b>, so this project does not compile them.
/// The expected output was also compiled and run separately — with the emitted helpers, under
/// warnings-as-errors and xUnit's analyzers — when it was accepted; this test pins that output.
/// </para>
/// </remarks>
public sealed class MstestToXunitTests : IClassFixture<MstestToXunitTests.Run>
{
    private readonly Run _run;

    public MstestToXunitTests(Run run) => _run = run;

    [Fact]
    public void Every_rule_converts_to_the_accepted_output() =>
        Assert.Equal(Normalize(Fixture("Rules.expected.cs.txt")), Normalize(_run.Converted("Rules.cs")));

    [Fact]
    public void Refused_forms_are_left_exactly_as_written_apart_from_what_did_convert() =>
        Assert.Equal(Normalize(Fixture("Unmapped.expected.cs.txt")), Normalize(_run.Converted("Unmapped.cs")));

    [Fact]
    public void Refused_forms_are_each_listed_with_their_line()
    {
        string[] expected = [.. Lines(Fixture("Unmapped.report.txt"))];
        string[] actual = [.. Lines(_run.Output).Where(l => l.Contains(": UNMAPPED ", StringComparison.Ordinal))];

        Assert.NotEmpty(expected);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Something_unmapped_is_exit_code_2_so_a_script_cannot_mistake_it_for_success() =>
        Assert.Equal(2, _run.ExitCode);

    [Fact]
    public void The_emitted_helpers_declare_every_member_the_rules_call()
    {
        string helpers = Normalize(_run.Helpers);
        string converted = _run.Converted("Rules.cs");

        Assert.Contains("namespace Fixture.Namespace;", helpers, StringComparison.Ordinal);
        foreach (string member in System.Text.RegularExpressions.Regex.Matches(converted, @"(?:MessageAssert|OrdinalAssert)\.(\w+)")
                     .Select(m => m.Groups[1].Value).Distinct())
        {
            Assert.Contains($" {member}", helpers, StringComparison.Ordinal);
        }
        Assert.Contains("internal static class OrdinalAssert", helpers, StringComparison.Ordinal);

        // ⛔ MSTest's string assertions are ORDINAL and xUnit's default to the culture, so no emitted
        // string helper may call xUnit's without a comparison. Measured when this was added:
        // Contains("coop", "co­op") passes in xUnit's default and fails in MSTest.
        foreach (string call in (string[])[
                     "Assert.Contains(expectedSubstring, actualString)", "Assert.DoesNotContain(expectedSubstring, actualString)",
                     "Assert.StartsWith(expectedStart, actualString)", "Assert.EndsWith(expectedEnd, actualString)"])
        {
            Assert.DoesNotContain(call, helpers, StringComparison.Ordinal);
            Assert.Contains(call.TrimEnd(')') + ", StringComparison.Ordinal)", helpers, StringComparison.Ordinal);
        }
        Assert.Contains("[CollectionDefinition(\"DoNotParallelize\", DisableParallelization = true)]", helpers,
            StringComparison.Ordinal);
    }

    /// <summary>
    /// A converted assertion must decide as its original did, which the text of the rules cannot
    /// show. This RUNS the emitted helpers against each <c>IsTrue|IsFalse(x.Contains(y))</c> they
    /// replace, over collections that bring their own comparer.
    /// </summary>
    /// <remarks>
    /// ⛔ Measured on xunit.v3.assert 3.2.2: xUnit asks a set for itself but compares a dictionary's
    /// <c>Keys</c> by the default equality, so <c>IsFalse(keys.Contains("A"))</c> over
    /// case-insensitive keys holding <c>"a"</c> converted to a <c>DoesNotContain</c> that passed
    /// where the original failed. Canaried: with the pre-fix helper this test fails on exactly those
    /// two lines.
    /// </remarks>
    [Fact]
    public void The_emitted_collection_helpers_decide_as_the_original_Contains_did()
    {
        string directory = Path.Combine(Path.GetTempPath(), "mstest-to-xunit-semantics-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "Helpers.cs"), _run.Helpers);
            File.WriteAllText(Path.Combine(directory, "run.cs"),
                Fixture("HelperSemantics.run.cs.txt").Replace("{XUNIT_VERSION}", XunitVersion(), StringComparison.Ordinal));

            (int exitCode, string output) = Run.Dotnet(directory, "run", "--file", "run.cs");

            Assert.True(exitCode == 0, output);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>The repository's pinned xunit.v3; its assert and core packages ship at the same version.</summary>
    private static string XunitVersion()
    {
        var props = System.Xml.Linq.XDocument.Load(Path.Combine(Run.RepoRoot(), "Directory.Packages.props"));
        string? version = props.Descendants("PackageVersion")
            .SingleOrDefault(p => (string?)p.Attribute("Include") == "xunit.v3")
            ?.Attribute("Version")?.Value;
        Assert.False(string.IsNullOrEmpty(version), "Directory.Packages.props pins no xunit.v3 version");
        return version;
    }

    private static IEnumerable<string> Lines(string text) =>
        Normalize(text).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim());

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(Run.RepoRoot(), "tests", "Templates.Tests", "MstestToXunit", "Fixtures", name));

    /// <summary>
    /// One run of the tool over fresh copies of both inputs, shared by the tests: compiling a
    /// file-based app is the slow part, and every assertion reads the same run.
    /// </summary>
    public sealed class Run : IDisposable
    {
        private readonly string _directory =
            Path.Combine(Path.GetTempPath(), "mstest-to-xunit-" + Guid.NewGuid().ToString("N"));

        public Run()
        {
            Directory.CreateDirectory(_directory);
            File.Copy(Path.Combine(FixtureDirectory(), "Rules.input.cs.txt"), Path.Combine(_directory, "Rules.cs"));
            File.Copy(Path.Combine(FixtureDirectory(), "Unmapped.input.cs.txt"), Path.Combine(_directory, "Unmapped.cs"));

            (ExitCode, Output) = Tool(_directory);

            string helpers = Path.Combine(Path.GetTempPath(), "mstest-to-xunit-helpers-" + Guid.NewGuid().ToString("N") + ".cs");
            (int helperExit, string helperOutput) = Tool("--emit-helpers", helpers, "--namespace", "Fixture.Namespace");
            Assert.True(helperExit == 0, helperOutput);
            Helpers = File.ReadAllText(helpers);
            File.Delete(helpers);
        }

        public int ExitCode { get; }

        public string Output { get; }

        public string Helpers { get; }

        public string Converted(string name) => File.ReadAllText(Path.Combine(_directory, name));

        public void Dispose() => Directory.Delete(_directory, recursive: true);

        public static string RepoRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Bennewitz.Ninja.Templates.csproj")))
            {
                directory = directory.Parent;
            }
            Assert.NotNull(directory);
            return directory.FullName;
        }

        private static string FixtureDirectory() =>
            Path.Combine(RepoRoot(), "tests", "Templates.Tests", "MstestToXunit", "Fixtures");

        // `--file` because the repository root holds a .csproj (see scripts/verify-release.cs).
        private static (int ExitCode, string Output) Tool(params string[] arguments) =>
            Dotnet(RepoRoot(), ["run", "--file", Path.Combine("scripts", "mstest-to-xunit.cs"), "--", .. arguments]);

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
    }
}
