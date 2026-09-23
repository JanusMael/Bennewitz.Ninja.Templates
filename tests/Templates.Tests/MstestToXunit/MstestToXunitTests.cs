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
        foreach (string member in System.Text.RegularExpressions.Regex.Matches(converted, @"MessageAssert\.(\w+)")
                     .Select(m => m.Groups[1].Value).Distinct())
        {
            Assert.Contains($" {member}", helpers, StringComparison.Ordinal);
        }
        Assert.Contains("[CollectionDefinition(\"DoNotParallelize\", DisableParallelization = true)]", helpers,
            StringComparison.Ordinal);
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

        private static (int ExitCode, string Output) Tool(params string[] arguments)
        {
            // `--file` because the repository root holds a .csproj (see scripts/verify-release.cs).
            ProcessStartInfo start = new("dotnet") { WorkingDirectory = RepoRoot(), RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string argument in (string[])["run", "--file", Path.Combine("scripts", "mstest-to-xunit.cs"), "--", .. arguments])
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
