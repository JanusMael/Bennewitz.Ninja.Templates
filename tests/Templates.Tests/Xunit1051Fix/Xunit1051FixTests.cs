using System.Diagnostics;

namespace Templates.Tests.Xunit1051Fix;

/// <summary>
/// <c>scripts/xunit1051-fix.cs</c>, run exactly as a person runs it, over one xUnit v3 project with
/// a call of each shape it must handle, compared with an expected output read line by line.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>Why the script exists.</b> xUnit's own fixer for xUnit1051 cannot Fix All, so
/// <c>dotnet format</c> reports "Unable to fix" and changes nothing. First run over OpenForge2k:
/// 469 sites, 146 of which needed a named <c>ct:</c>, and a clean build after.
/// </para>
/// <para>
/// ⚠ The fixture holds the two cases that broke a first draft: a call passed as another call's
/// argument (its span ties with the ArgumentSyntax around it, so the innermost node must be taken),
/// and a call that already names an argument (a positional token after it is CS8323).
/// </para>
/// </remarks>
public sealed class Xunit1051FixTests : IClassFixture<Xunit1051FixTests.Run>
{
    private readonly Run _run;

    public Xunit1051FixTests(Run run) => _run = run;

    [Fact]
    public void Every_flagged_call_is_given_the_token_and_nothing_else_changes() =>
        Assert.Equal(Normalize(Fixture("Cases.expected.cs.txt")), Normalize(_run.Converted));

    [Fact]
    public void Every_site_is_counted_and_the_misbound_ones_are_named() =>
        Assert.Contains("5 sites flagged, 5 calls given the token, in 1 files", _run.Output, StringComparison.Ordinal);

    [Fact]
    public void A_clean_result_is_exit_code_0() =>
        Assert.True(_run.ExitCode == 0, _run.Output);

    private static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string Fixture(string name) => File.ReadAllText(Path.Combine(Run.FixtureDirectory(), name));

    /// <summary>One run of the script, shared by the tests: it builds the fixture three times.</summary>
    public sealed class Run : IDisposable
    {
        private readonly string _directory =
            Path.Combine(Path.GetTempPath(), "xunit1051-fix-fixture-" + Guid.NewGuid().ToString("N"));

        public Run()
        {
            Directory.CreateDirectory(_directory);
            File.Copy(Path.Combine(FixtureDirectory(), "Cases.cs.txt"), Path.Combine(_directory, "Cases.cs"));
            File.WriteAllText(Path.Combine(_directory, "Cases.csproj"),
                File.ReadAllText(Path.Combine(FixtureDirectory(), "Cases.csproj.txt"))
                    .Replace("{XUNIT_VERSION}", XunitVersion(), StringComparison.Ordinal));

            // `--file` because the repository root holds a .csproj (see scripts/verify-release.cs).
            ProcessStartInfo start = new("dotnet") { WorkingDirectory = RepoRoot(), RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string argument in (string[])["run", "--file", Path.Combine("scripts", "xunit1051-fix.cs"), "--",
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
            Converted = File.ReadAllText(Path.Combine(_directory, "Cases.cs"));
        }

        public int ExitCode { get; }

        public string Output { get; }

        public string Converted { get; }

        public void Dispose() => Directory.Delete(_directory, recursive: true);

        public static string FixtureDirectory() =>
            Path.Combine(RepoRoot(), "tests", "Templates.Tests", "Xunit1051Fix", "Fixtures");

        private static string XunitVersion()
        {
            var props = System.Xml.Linq.XDocument.Load(Path.Combine(RepoRoot(), "Directory.Packages.props"));
            string? version = props.Descendants("PackageVersion")
                .SingleOrDefault(p => (string?)p.Attribute("Include") == "xunit.v3")
                ?.Attribute("Version")?.Value;
            Assert.False(string.IsNullOrEmpty(version), "Directory.Packages.props pins no xunit.v3 version");
            return version;
        }

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
