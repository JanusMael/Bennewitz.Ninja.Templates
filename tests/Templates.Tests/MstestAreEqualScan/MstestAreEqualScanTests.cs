using System.Diagnostics;

namespace Templates.Tests.MstestAreEqualScan;

/// <summary>
/// <c>scripts/mstest-areequal-scan.cs</c>, run exactly as a person runs it, over one MSTest project
/// whose every case says in a comment what it must be classified as.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>Why the scan exists.</b> MSTest's <c>AreEqual</c> compares by <c>Equals</c>, so a
/// collection that does not override it is compared by REFERENCE; xUnit's <c>Equal</c> compares
/// collections by their ELEMENTS. A converted assertion over such a value still passes and no longer
/// notices a copy. Only the compiler knows an argument's type, so this cannot be a converter rule.
/// </para>
/// <para>
/// ⚠ The fixture project sets <c>TreatWarningsAsErrors</c> on purpose: the scan reports through a
/// warning, and a suite built that way must still build. Measured the first time it was run over a
/// real suite (ClaudeForge's MSTest tree, 2,935 sites): nothing needed a look.
/// </para>
/// </remarks>
public sealed class MstestAreEqualScanTests : IClassFixture<MstestAreEqualScanTests.Run>
{
    private readonly Run _run;

    public MstestAreEqualScanTests(Run run) => _run = run;

    [Fact]
    public void Every_case_is_classified_as_its_comment_says() =>
        Assert.Equal(
            [.. Lines(Fixture("Cases.report.txt"))],
            [.. Lines(_run.Output).Where(l => l.StartsWith("NEEDS A LOOK ", StringComparison.Ordinal))]);

    [Fact]
    public void Every_call_is_seen_including_the_scalar_ones_it_does_not_list()
    {
        string[] tally = [.. Lines(_run.Output)];
        Assert.Contains("10 AreEqual/AreNotEqual sites", tally);
        Assert.Contains("SCALAR          AreEqual     3", tally);
        Assert.Contains("COLLECTION-REF  AreNotEqual  1", tally);
    }

    [Fact]
    public void Something_to_look_at_is_exit_code_2_so_a_script_cannot_mistake_it_for_success() =>
        Assert.True(_run.ExitCode == 2, _run.Output);

    private static IEnumerable<string> Lines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim());

    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(Run.FixtureDirectory(), name));

    /// <summary>One run of the scan, shared by the tests: it builds two projects, which is the slow part.</summary>
    public sealed class Run : IDisposable
    {
        private readonly string _directory =
            Path.Combine(Path.GetTempPath(), "mstest-areequal-scan-fixture-" + Guid.NewGuid().ToString("N"));

        public Run()
        {
            Directory.CreateDirectory(_directory);
            File.Copy(Path.Combine(FixtureDirectory(), "Cases.csproj.txt"), Path.Combine(_directory, "Cases.csproj"));
            File.Copy(Path.Combine(FixtureDirectory(), "Cases.cs.txt"), Path.Combine(_directory, "Cases.cs"));

            // `--file` because the repository root holds a .csproj (see scripts/verify-release.cs).
            ProcessStartInfo start = new("dotnet") { WorkingDirectory = RepoRoot(), RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string argument in (string[])["run", "--file", Path.Combine("scripts", "mstest-areequal-scan.cs"), "--",
                         Path.Combine(_directory, "Cases.csproj")])
            {
                start.ArgumentList.Add(argument);
            }
            using Process process = Process.Start(start)!;
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            ExitCode = process.ExitCode;
            Output = stdout.Result + stderr.Result;
        }

        public int ExitCode { get; }

        public string Output { get; }

        public void Dispose() => Directory.Delete(_directory, recursive: true);

        public static string FixtureDirectory() =>
            Path.Combine(RepoRoot(), "tests", "Templates.Tests", "MstestAreEqualScan", "Fixtures");

        private static string RepoRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Bennewitz.Ninja.Templates.csproj")))
            {
                directory = directory.Parent;
            }
            Assert.NotNull(directory);
            return directory.FullName;
        }
    }
}
