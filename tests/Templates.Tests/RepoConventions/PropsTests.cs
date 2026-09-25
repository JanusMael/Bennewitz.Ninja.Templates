using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Templates.Tests.RepoConventions;

/// <summary>
/// The build-property check of <c>scripts/repo-conventions.cs</c> (plans/00004), run as CI runs it
/// over small REAL repositories: each is restored and every project evaluated by MSBuild.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>Three runs, many projects each.</b> A restore is the slow part, so each run carries several
/// projects, each conforming or broken in exactly one way, and every test reads the run it needs.
/// </para>
/// <para>
/// ⛔ <b>The cases the check exists for:</b> a csproj overriding a correct <c>Directory.Build.props</c>;
/// <c>IsContinuousIntegration</c> set to <c>$(GITHUB_ACTIONS)</c>, as the family set it, which a check
/// evaluating without <c>GITHUB_ACTIONS=true</c> would pass; an AutoVersioning pin below the floor
/// through <c>VersionOverride</c>; and an xUnit v3-shaped test project, a non-packable executable that
/// must be role <c>test</c>, not <c>app</c>.
/// </para>
/// <para>
/// ⓘ In the same collection as <see cref="RepoConventionsTests"/>: both run the same file-based app,
/// and parallel builds of one file contend for its output.
/// </para>
/// </remarks>
[Collection("repo-conventions")]
public sealed class PropsTests : IClassFixture<PropsTests.Runs>
{
    private readonly Runs _runs;

    public PropsTests(Runs runs) => _runs = runs;

    /// <remarks>
    /// ⛔ The first assertion is the vacuity guard. Without it, a script that failed to compile, or
    /// never reached the property check, prints no FAIL line and would pass this test.
    /// </remarks>
    [Fact]
    public void A_conforming_repository_has_no_property_finding()
    {
        Assert.Contains("NOTE props: 4 projects evaluated after a restore", _runs.Conforming, StringComparison.Ordinal);
        Assert.DoesNotContain(Lines(_runs.Conforming), l => l.StartsWith("FAIL props", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Good (library)")]
    [InlineData("GoodTests (test)")]
    [InlineData("GoodApp (app)")]
    [InlineData("GoodTool (tool)")]
    public void Each_project_gets_the_role_it_declares(string role) =>
        Assert.Contains(role, _runs.Conforming, StringComparison.Ordinal);

    [Fact]
    public void A_csproj_overriding_a_correct_Directory_Build_props_is_caught() =>
        Assert.Contains("FAIL props: Override (library): Nullable is \"disable\"", _runs.Defective, StringComparison.Ordinal);

    /// <remarks>
    /// The project sets it as the family did, to <c>$(GITHUB_ACTIONS)</c>. That is empty on a
    /// developer machine, so this passes only because the check evaluates as CI does.
    /// </remarks>
    [Fact]
    public void IsContinuousIntegration_is_caught_as_CI_evaluates_it() =>
        Assert.Contains("FAIL props: CiProperty (library): IsContinuousIntegration evaluates to \"true\"", _runs.Defective, StringComparison.Ordinal);

    [Fact]
    public void An_AutoVersioning_pin_below_the_floor_is_caught_through_VersionOverride() =>
        Assert.Contains("FAIL props: OldAutoVersioning (library): references Bennewitz.Ninja.AutoVersioning 2026.3.914", _runs.Defective, StringComparison.Ordinal);

    [Fact]
    public void A_library_without_its_packed_readme_is_caught() =>
        Assert.Contains("FAIL props: NoReadme (library): PackageReadmeFile is \"\"", _runs.Defective, StringComparison.Ordinal);

    [Fact]
    public void An_untrimmable_library_is_a_note_until_trimming_is_required()
    {
        Assert.Contains("NOTE props: NotTrimmable is not yet trimmable", _runs.Defective, StringComparison.Ordinal);
        Assert.Contains("FAIL props: NotTrimmable is not yet trimmable", _runs.Required, StringComparison.Ordinal);
    }

    [Fact]
    public void A_conforming_project_beside_broken_ones_is_not_blamed() =>
        Assert.DoesNotContain(Lines(_runs.Defective), l => l.StartsWith("FAIL props: Good", StringComparison.Ordinal));

    [Fact]
    public void An_exemption_with_a_reason_excuses_its_rule() =>
        Assert.DoesNotContain(Lines(_runs.Required), l => l.StartsWith("FAIL props: NoReadme", StringComparison.Ordinal));

    [Fact]
    public void An_exemption_without_a_reason_fails() =>
        Assert.Contains("\"props\" exempts Override's Nullable without a reason", _runs.Required, StringComparison.Ordinal);

    [Fact]
    public void An_exemption_that_is_no_longer_needed_is_noted() =>
        Assert.Contains("NOTE props: Good: the exemption for Nullable is no longer needed", _runs.Required, StringComparison.Ordinal);

    [Fact]
    public void Offline_refuses_to_read_another_repository_through_the_API()
    {
        (int exit, string output) = Runs.Script(Runs.RepoRoot(), "check", "--offline", "--repo", "o/r");

        Assert.Equal(2, exit);
        Assert.Contains("--offline checks a checkout", output, StringComparison.Ordinal);
    }

    private static IEnumerable<string> Lines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(l => l.Trim());

    /// <summary>The three runs, each over its own repository, shared by every test in the class.</summary>
    public sealed class Runs
    {
        private static readonly string[] Conforming_ = ["Good", "GoodTests", "GoodApp", "GoodTool"];
        private static readonly string[] Broken = ["Override", "CiProperty", "OldAutoVersioning", "NoReadme", "NotTrimmable"];

        public Runs()
        {
            Conforming = Run(Conforming_, new JsonObject());
            Defective = Run([.. Conforming_, .. Broken], new JsonObject());
            Required = Run([.. Conforming_, .. Broken], new JsonObject
            {
                ["trimming"] = "required",
                ["props"] = new JsonObject
                {
                    ["NoReadme"] = new JsonObject { ["PackageReadmeFile"] = "The fixture's reason." },
                    ["Override"] = new JsonObject { ["Nullable"] = " " },
                    ["Good"] = new JsonObject { ["Nullable"] = "Stale: Good meets it." },
                },
            });
        }

        public string Conforming { get; }

        public string Defective { get; }

        public string Required { get; }

        private static string Run(string[] projects, JsonObject extra)
        {
            string directory = Path.Combine(Path.GetTempPath(), "repo-conventions-props-" + Guid.NewGuid().ToString("N"));
            try
            {
                Write(directory, "NuGet.config",
                    """
                    <?xml version="1.0" encoding="utf-8"?>
                    <configuration>
                      <packageSources>
                        <clear />
                        <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                      </packageSources>
                    </configuration>
                    """);
                Write(directory, "Directory.Build.props",
                    """
                    <Project>
                      <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                        <Nullable>enable</Nullable>
                        <ImplicitUsings>enable</ImplicitUsings>
                        <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
                        <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
                        <GenerateAutoVersionedAssemblyInfo>true</GenerateAutoVersionedAssemblyInfo>
                        <AssemblyCompany>Bennewitz.Ninja</AssemblyCompany>
                        <Authors>Fixture</Authors>
                        <PackageLicenseExpression>MIT</PackageLicenseExpression>
                        <RepositoryUrl>https://github.com/o/r</RepositoryUrl>
                        <PackageReadmeFile>README.md</PackageReadmeFile>
                        <IsTrimmable>true</IsTrimmable>
                        <EnableTrimAnalyzer>true</EnableTrimAnalyzer>
                      </PropertyGroup>
                      <PropertyGroup Condition="'$(Configuration)' == 'Release'">
                        <DebugType>embedded</DebugType>
                      </PropertyGroup>
                      <ItemGroup>
                        <PackageReference Include="Bennewitz.Ninja.AutoVersioning" PrivateAssets="all" />
                      </ItemGroup>
                    </Project>
                    """);
                Write(directory, "Directory.Packages.props",
                    """
                    <Project>
                      <ItemGroup>
                        <PackageVersion Include="Bennewitz.Ninja.AutoVersioning" Version="2026.3.916" />
                      </ItemGroup>
                    </Project>
                    """);
                Write(directory, "Fixture.slnx",
                    "<Solution>\n" + string.Concat(projects.Select(p => $"  <Project Path=\"src/{p}/{p}.csproj\" />\n")) + "</Solution>\n");
                foreach (string project in projects)
                {
                    Write(directory, $"src/{project}/{project}.csproj", Csproj(project));
                }

                JsonObject config = new()
                {
                    ["description"] = "Fixture.",
                    ["topics"] = new JsonArray("csharp", "dotnet", "nuget"),
                    ["requiredChecks"] = new JsonArray(),
                };
                foreach ((string key, JsonNode? value) in extra)
                {
                    config[key] = value?.DeepClone();
                }
                Write(directory, ".github/repository.json", config.ToJsonString());

                (int initExit, string initOutput) = Execute("git", directory, "init", "-q");
                Assert.True(initExit == 0, initOutput);

                (int _, string output) = Script(RepoRoot(), "check", "--offline", "--root", directory, "--repo", "o/r");
                return output;
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        private static string Csproj(string project) => project switch
        {
            "GoodTests" => Sdk("<IsPackable>false</IsPackable><IsTestProject>true</IsTestProject><OutputType>Exe</OutputType>"),
            // An app packs nothing, so it needs no packed readme; the package rules must not reach it.
            "GoodApp" => Sdk("<IsPackable>false</IsPackable><OutputType>Exe</OutputType><PackageReadmeFile></PackageReadmeFile>"),
            "GoodTool" => Sdk("<IsPackable>true</IsPackable><OutputType>Exe</OutputType><PackAsTool>true</PackAsTool>"),
            "Override" => Sdk("<IsPackable>true</IsPackable><Nullable>disable</Nullable>"),
            "CiProperty" => Sdk("<IsPackable>true</IsPackable><IsContinuousIntegration>$(GITHUB_ACTIONS)</IsContinuousIntegration>"),
            "OldAutoVersioning" => Sdk("<IsPackable>true</IsPackable>",
                "<ItemGroup><PackageReference Update=\"Bennewitz.Ninja.AutoVersioning\" VersionOverride=\"2026.3.914\" /></ItemGroup>"),
            "NoReadme" => Sdk("<IsPackable>true</IsPackable><PackageReadmeFile></PackageReadmeFile>"),
            "NotTrimmable" => Sdk("<IsPackable>true</IsPackable><IsTrimmable>false</IsTrimmable>"),
            _ => Sdk("<IsPackable>true</IsPackable>"),
        };

        private static string Sdk(string properties, string items = "") =>
            $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup>{properties}</PropertyGroup>\n  {items}\n</Project>\n";

        private static void Write(string root, string relative, string text)
        {
            string path = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }

        public static (int ExitCode, string Output) Script(string workingDirectory, params string[] arguments) =>
            Execute("dotnet", workingDirectory, ["run", "--file", Path.Combine("scripts", "repo-conventions.cs"), "--", .. arguments]);

        private static (int ExitCode, string Output) Execute(string file, string workingDirectory, params string[] arguments)
        {
            ProcessStartInfo start = new(file) { WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true };
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
    }
}
