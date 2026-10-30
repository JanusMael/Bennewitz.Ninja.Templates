using System.Text.Json.Nodes;

namespace Templates.Tests.RepoConventions;

/// <summary>
/// The build-property check of <c>scripts/repo-conventions.cs</c> (plans/00004), run as CI runs it
/// over small REAL repositories: each is restored and every project evaluated by MSBuild.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>Nine runs, several projects each.</b> A restore is the slow part, so each run carries several
/// projects, each conforming or broken in exactly one way, and every test reads the run it needs. Three
/// of them trim through nothing but the project the run is about (plans/00008), one declares
/// two package families (plans/00009), each with its README and product (plans/00011), and two take
/// the same family packages from nuget.org, one trimming and one not (plans/00008 step 3).
/// </para>
/// <para>
/// ⛔ <b>The cases the check exists for:</b> a csproj overriding a correct <c>Directory.Build.props</c>;
/// <c>IsContinuousIntegration</c> set to <c>$(GITHUB_ACTIONS)</c>, as the family set it, which a check
/// evaluating without <c>GITHUB_ACTIONS=true</c> would pass; an AutoVersioning pin below the floor
/// through <c>VersionOverride</c>; and an xUnit v3-shaped test project, a non-packable executable that
/// must be role <c>test</c>, not <c>app</c>.
/// </para>
/// <para>
/// ⓘ The script is built once and launched as a program (<see cref="ConventionsScript"/>), so this
/// class runs in parallel with the other conventions classes, and its nine runs with each other.
/// </para>
/// </remarks>
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
        Assert.Contains("NOTE props: 5 projects evaluated after a restore", _runs.Conforming, StringComparison.Ordinal);
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
        Assert.Contains("FAIL props: NoReadme (library): sets no PackageReadmeFile", _runs.Defective, StringComparison.Ordinal);

    /// <remarks>DiffView's libraries pack a hosting guide as their README; any name will do.</remarks>
    [Fact]
    public void A_readme_under_another_name_is_a_readme() =>
        Assert.DoesNotContain(Lines(_runs.Conforming), l => l.StartsWith("FAIL props: OtherReadme", StringComparison.Ordinal));

    /// <summary>A repository trims when any project sets a trimming property, here every library but one (plans/00008).</summary>
    [Fact]
    public void An_unmarked_library_fails_where_the_libraries_trim() =>
        Assert.Contains("FAIL props: NotTrimmable (library): is not trimmable, and the repository trims (", _runs.Defective, StringComparison.Ordinal);

    /// <remarks>
    /// ⛔ No library sets either property: only the app's publish makes the repository trim, and it
    /// trims the library too. Apps themselves are not held to the rule.
    /// </remarks>
    [Fact]
    public void An_unmarked_library_fails_where_an_app_publishes_trimmed()
    {
        Assert.Contains("FAIL props: Good (library): is not trimmable, and the repository trims (TrimmedApp: ", _runs.PublishTrimmed, StringComparison.Ordinal);
        Assert.Contains("PublishTrimmed", _runs.PublishTrimmed, StringComparison.Ordinal);
        Assert.DoesNotContain(Lines(_runs.PublishTrimmed), l => l.StartsWith("FAIL props: TrimmedApp", StringComparison.Ordinal));
    }

    [Fact]
    public void An_unmarked_library_fails_where_an_app_publishes_natively()
    {
        Assert.Contains("FAIL props: Good (library): is not trimmable, and the repository trims (AotApp: ", _runs.PublishAot, StringComparison.Ordinal);
        Assert.Contains("PublishAot", _runs.PublishAot, StringComparison.Ordinal);
        Assert.DoesNotContain(Lines(_runs.PublishAot), l => l.StartsWith("FAIL props: AotApp", StringComparison.Ordinal));
    }

    /// <remarks>The policy enforces nothing where nothing trims: not a FAIL, and not a NOTE either.</remarks>
    [Fact]
    public void A_repository_that_does_not_trim_has_no_trimming_finding()
    {
        Assert.Contains("NOTE props: 4 projects evaluated after a restore", _runs.Untrimmed, StringComparison.Ordinal);
        Assert.DoesNotContain(Lines(_runs.Untrimmed), l => l.Contains("trimm", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void A_trimming_exemption_excuses_the_library_and_a_stale_one_is_noted()
    {
        Assert.DoesNotContain(Lines(_runs.Exempted), l => l.StartsWith("FAIL props: NotTrimmable", StringComparison.Ordinal));
        Assert.Contains("NOTE props: Good: the exemption for Trimming is no longer needed", _runs.Exempted, StringComparison.Ordinal);
    }

    /// <summary>
    /// The other side (plans/00008 step 3): where the repository trims, a library that takes a
    /// <c>Bennewitz.Ninja.*</c> package without IsTrimmable fails, the package named with its version.
    /// </summary>
    /// <remarks>⛔ The first assertion is the vacuity guard: every project was evaluated.</remarks>
    [Fact]
    public void A_library_taking_an_untrimmable_family_package_fails_where_the_repository_trims()
    {
        Assert.Contains("NOTE props: 6 projects evaluated after a restore", _runs.Packages, StringComparison.Ordinal);
        Assert.Contains("FAIL props: TakesOld (library): takes Bennewitz.Ninja.AppServices.Abstractions 2026.3.923, without IsTrimmable, and the repository trims (", _runs.Packages, StringComparison.Ordinal);
    }

    /// <remarks>AppServices.Abstractions reaches the app only through AppServices.</remarks>
    [Fact]
    public void An_app_fails_on_a_transitive_untrimmable_family_package_too() =>
        Assert.Contains("FAIL props: AppTakesOld (app): takes Bennewitz.Ninja.AppServices 2026.3.923, Bennewitz.Ninja.AppServices.Abstractions 2026.3.923, without IsTrimmable", _runs.Packages, StringComparison.Ordinal);

    /// <remarks>
    /// Good takes AutoVersioning, an MSBuild package that adds no assembly, so there is nothing of it to
    /// trim; TakesNew takes the version of AppServices.Abstractions that is trimmable.
    /// </remarks>
    [Theory]
    [InlineData("Good")]
    [InlineData("TakesNew")]
    public void A_project_whose_family_packages_are_trimmable_or_add_no_assembly_passes(string project) =>
        Assert.DoesNotContain(Lines(_runs.Packages), l => l.StartsWith($"FAIL props: {project} ", StringComparison.Ordinal));

    /// <remarks>A test runs untrimmed: its restore graph owes nothing, as XamlQuality's is used only from tests.</remarks>
    [Fact]
    public void A_test_project_s_family_packages_are_not_held_to_it() =>
        Assert.DoesNotContain(Lines(_runs.Packages), l => l.StartsWith("FAIL props: TestsTakeOld", StringComparison.Ordinal));

    [Fact]
    public void A_TrimmablePackages_exemption_excuses_the_project() =>
        Assert.DoesNotContain(Lines(_runs.Packages), l => l.StartsWith("FAIL props: ExemptTakesOld", StringComparison.Ordinal));

    /// <summary>The same graphs where nothing trims: no finding about a package, and the exemption is stale.</summary>
    [Fact]
    public void Untrimmable_family_packages_are_not_reported_where_the_repository_does_not_trim()
    {
        Assert.Contains("NOTE props: 6 projects evaluated after a restore", _runs.UntrimmedPackages, StringComparison.Ordinal);
        Assert.DoesNotContain(Lines(_runs.UntrimmedPackages), l => l.StartsWith("FAIL", StringComparison.Ordinal) && l.Contains("IsTrimmable", StringComparison.Ordinal));
        Assert.Contains("NOTE props: ExemptTakesOld: the exemption for TrimmablePackages is no longer needed", _runs.UntrimmedPackages, StringComparison.Ordinal);
    }

    /// <summary>The retired key is a NOTE, never a FAIL, so a repository still carrying it stays green.</summary>
    [Fact]
    public void The_retired_trimming_key_is_noted()
    {
        Assert.Contains("NOTE repository.json: \"trimming\" is no longer read; remove it.", _runs.Exempted, StringComparison.Ordinal);
        Assert.DoesNotContain("\"trimming\"", _runs.Defective, StringComparison.Ordinal);
    }

    /// <summary>Every project that compiles embeds its symbols in Release, an app as much as a package.</summary>
    [Fact]
    public void An_app_without_symbols_in_Release_is_caught() =>
        Assert.Contains("FAIL props: AppWithoutSymbols (app): builds Release with DebugType \"none\"", _runs.Defective, StringComparison.Ordinal);

    /// <remarks>
    /// ⛔ The project is correct in Release, which is how every other rule evaluates it; a check that
    /// read only Release would pass it.
    /// </remarks>
    [Fact]
    public void A_project_without_symbols_in_Debug_is_caught()
    {
        Assert.Contains("FAIL props: DebugWithoutSymbols (library): builds Debug with DebugType \"none\"", _runs.Defective, StringComparison.Ordinal);
        Assert.DoesNotContain(Lines(_runs.Defective), l => l.StartsWith("FAIL props: DebugWithoutSymbols (library): builds Release", StringComparison.Ordinal));
    }

    [Fact]
    public void A_conforming_project_beside_broken_ones_is_not_blamed() =>
        Assert.DoesNotContain(Lines(_runs.Defective), l => l.StartsWith("FAIL props: Good", StringComparison.Ordinal));

    [Fact]
    public void An_exemption_with_a_reason_excuses_its_rule() =>
        Assert.DoesNotContain(Lines(_runs.Exempted), l => l.StartsWith("FAIL props: NoReadme", StringComparison.Ordinal));

    [Fact]
    public void An_exemption_without_a_reason_fails() =>
        Assert.Contains("\"props\" exempts Override's Nullable without a reason", _runs.Exempted, StringComparison.Ordinal);

    [Fact]
    public void An_exemption_that_is_no_longer_needed_is_noted() =>
        Assert.Contains("NOTE props: Good: the exemption for Nullable is no longer needed", _runs.Exempted, StringComparison.Ordinal);

    [Fact]
    public void Offline_refuses_to_read_another_repository_through_the_API()
    {
        (int exit, string output) = Runs.Script(Runs.RepoRoot(), "check", "--offline", "--repo", "o/r");

        Assert.Equal(2, exit);
        Assert.Contains("--offline checks a checkout", output, StringComparison.Ordinal);
    }

    /// <summary>
    /// A family's release packs at that family's version (plans/00009 decision 11), so a packable
    /// project referencing another family's project would pack a dependency on a version that need not
    /// exist. Read by evaluated PackageId; the families' keys are matched as their tags spell them.
    /// </summary>
    [Fact]
    public void A_packable_project_referencing_another_family_s_project_fails()
    {
        Assert.Contains("NOTE props: 5 projects evaluated after a restore", _runs.Families, StringComparison.Ordinal);
        Assert.Contains("FAIL families: Beta, of the beta-kit family, references Good, of the alpha family, as a project.", _runs.Families, StringComparison.Ordinal);
    }

    [Fact]
    public void A_reference_within_a_family_or_from_a_project_that_packs_nothing_passes()
    {
        // Gamma references Beta, its own family's; FamilyTests references both families and packs nothing.
        string failure = Assert.Single(Lines(_runs.Families), l => l.StartsWith("FAIL families", StringComparison.Ordinal) && l.Contains(" as a project.", StringComparison.Ordinal));
        Assert.StartsWith("FAIL families: Beta,", failure, StringComparison.Ordinal);
    }

    /// <summary>A family's README is packed as each of its packages' README.md, so it must be there to pack (plans/00011).</summary>
    [Fact]
    public void A_family_without_its_README_fails()
    {
        Assert.Contains("FAIL families: The beta-kit family has no docs/beta-kit.md.", _runs.Families, StringComparison.Ordinal);
        Assert.DoesNotContain("has no docs/alpha.md", _runs.Families, StringComparison.Ordinal);
    }

    [Fact]
    public void A_family_package_naming_another_family_s_README_fails() =>
        Assert.Contains("FAIL families: Beta, of the beta-kit family, has FamilyReadme \"docs/alpha.md\"; the family's is docs/beta-kit.md.", _runs.Families, StringComparison.Ordinal);

    /// <remarks>Without one, the root props pack the repository's README into the family's package.</remarks>
    [Fact]
    public void A_family_package_without_a_FamilyReadme_fails() =>
        Assert.Contains("FAIL families: Delta, of the beta-kit family, sets no FamilyReadme", _runs.Families, StringComparison.Ordinal);

    [Fact]
    public void A_family_package_with_another_product_fails() =>
        Assert.Contains("FAIL families: Gamma, of the beta-kit family, has AssemblyProduct \"Fixture\"", _runs.Families, StringComparison.Ordinal);

    /// <remarks>
    /// Good's is <c>Alpha</c>, Beta's <c>BetaKit</c> and Delta's <c>Beta.Kit</c>, against the stems
    /// <c>alpha</c> and <c>Beta-Kit</c>: a product is the stem without regard to case or its separators.
    /// </remarks>
    [Fact]
    public void A_family_s_product_is_its_stem_without_case_or_separators()
    {
        string failure = Assert.Single(Lines(_runs.Families), l => l.StartsWith("FAIL families", StringComparison.Ordinal) && l.Contains("AssemblyProduct", StringComparison.Ordinal));
        Assert.StartsWith("FAIL families: Gamma,", failure, StringComparison.Ordinal);
    }

    [Fact]
    public void A_family_package_that_conforms_is_not_blamed() =>
        Assert.DoesNotContain(Lines(_runs.Families), l => l.StartsWith("FAIL families: Good,", StringComparison.Ordinal));

    /// <summary>
    /// A family's README is a nuget.org page, where a relative link goes nowhere: each of
    /// <see cref="LinkCases.Relative"/> fails, and nothing else in <see cref="LinkCases.Markdown"/> does.
    /// </summary>
    [Fact]
    public void A_relative_link_in_a_family_README_fails()
    {
        string[] links = [.. Lines(_runs.Families)
            .Where(l => l.StartsWith("FAIL families: docs/alpha.md:", StringComparison.Ordinal))
            .Select(l => l["FAIL families: ".Length..l.IndexOf(", relatively", StringComparison.Ordinal)])];

        Assert.Equal(
            [.. LinkCases.Relative.Select(link => $"docs/alpha.md:{link.Line} links to \"{link.Target}\"")],
            links);
    }

    private static IEnumerable<string> Lines(string text) => ConventionsScript.Lines(text);

    /// <summary>The nine runs, each over its own repository, shared by every test in the class.</summary>
    /// <remarks>Each run's output is assigned once, from its own thread, before any test reads it.</remarks>
    public sealed class Runs
    {
        private static readonly string[] Conforming_ = ["Good", "GoodTests", "GoodApp", "GoodTool", "OtherReadme"];
        private static readonly string[] Broken = ["Override", "CiProperty", "OldAutoVersioning", "NoReadme", "NotTrimmable", "AppWithoutSymbols", "DebugWithoutSymbols"];

        /// <remarks>The runs share nothing but the built script, so they run at once.</remarks>
        public Runs() =>
            Parallel.Invoke(
                () => Conforming = Run(Conforming_, new JsonObject()),
                () => Defective = Run([.. Conforming_, .. Broken], new JsonObject()),
                () => Exempted = Run([.. Conforming_, .. Broken], new JsonObject
                {
                    ["trimming"] = "required",
                    ["props"] = new JsonObject
                    {
                        ["NoReadme"] = new JsonObject { ["PackageReadmeFile"] = "The fixture's reason." },
                        ["NotTrimmable"] = new JsonObject { ["Trimming"] = "The fixture's reason." },
                        ["Override"] = new JsonObject { ["Nullable"] = " " },
                        ["Good"] = new JsonObject { ["Nullable"] = "Stale: Good meets it.", ["Trimming"] = "Stale: Good is trimmable." },
                    },
                }),

                // Over a Directory.Build.props that sets no trimming property, so only these projects decide.
                () => Untrimmed = Run(["Good", "GoodTests", "GoodApp", "GoodTool"], new JsonObject(), trims: false),
                () => PublishTrimmed = Run(["Good", "GoodApp", "TrimmedApp"], new JsonObject(), trims: false),
                () => PublishAot = Run(["Good", "GoodApp", "AotApp"], new JsonObject(), trims: false),

                // Good is the alpha family, and conforms; Beta references it, and Gamma references Beta,
                // its own family's. Beta names alpha's README, Gamma has another product, Delta names no
                // README, and the beta-kit family has none to name.
                () => Families = Run(["Good", "Beta", "Gamma", "Delta", "FamilyTests"], new JsonObject
                {
                    ["families"] = new JsonObject { ["alpha"] = new JsonArray("Good"), ["Beta-Kit"] = new JsonArray("Beta", "Gamma", "Delta") },
                }, push: ["Good", "Beta", "Gamma", "Delta"],
                properties: new Dictionary<string, string>
                {
                    ["Good"] = Family("docs/alpha.md", "Alpha"),
                    ["Beta"] = Family("docs/alpha.md", "BetaKit"),
                    ["Gamma"] = Family("docs/beta-kit.md", "Fixture"),
                    ["Delta"] = Family("", "Beta.Kit"),
                },
                files: new Dictionary<string, string> { ["docs/alpha.md"] = LinkCases.Markdown }),

                // The family packages a project takes (plans/00008 step 3), from nuget.org, whose
                // versions never change: AppServices.Abstractions 2026.3.923 carries no IsTrimmable
                // and 2026.3.924 does; AppServices 2026.3.923 takes the first. The same graphs twice,
                // in a repository that trims and in one that does not.
                () => Packages = Run(PackageTakers, PackageExemption),
                () => UntrimmedPackages = Run(PackageTakers, PackageExemption, trims: false));

        private static readonly string[] PackageTakers = ["Good", "TakesOld", "TakesNew", "AppTakesOld", "TestsTakeOld", "ExemptTakesOld"];

        private static JsonObject PackageExemption => new()
        {
            ["props"] = new JsonObject { ["ExemptTakesOld"] = new JsonObject { ["TrimmablePackages"] = "The fixture's reason." } },
        };

        public string Conforming { get; private set; } = "";

        public string Defective { get; private set; } = "";

        public string Exempted { get; private set; } = "";

        public string Untrimmed { get; private set; } = "";

        public string PublishTrimmed { get; private set; } = "";

        public string PublishAot { get; private set; } = "";

        public string Families { get; private set; } = "";

        public string Packages { get; private set; } = "";

        public string UntrimmedPackages { get; private set; } = "";

        private static string Family(string readme, string product) =>
            $"<FamilyReadme>{readme}</FamilyReadme><AssemblyProduct>{product}</AssemblyProduct>";

        private static string Run(string[] projects, JsonObject extra, bool trims = true, string[]? push = null,
            IReadOnlyDictionary<string, string>? properties = null, IReadOnlyDictionary<string, string>? files = null)
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
                string trimming = trims ? "<IsTrimmable>true</IsTrimmable><EnableTrimAnalyzer>true</EnableTrimAnalyzer>" : "";
                Write(directory, "Directory.Build.props",
                    $$"""
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
                        {{trimming}}
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
                        <PackageVersion Include="Bennewitz.Ninja.AppServices" Version="2026.3.923" />
                        <PackageVersion Include="Bennewitz.Ninja.AppServices.Abstractions" Version="2026.3.923" />
                      </ItemGroup>
                    </Project>
                    """);
                Write(directory, "Fixture.slnx",
                    "<Solution>\n" + string.Concat(projects.Select(p => $"  <Project Path=\"src/{p}/{p}.csproj\" />\n")) + "</Solution>\n");
                foreach (string project in projects)
                {
                    string csproj = Csproj(project);
                    if (properties?.GetValueOrDefault(project) is { } added)
                    {
                        int end = csproj.IndexOf("</PropertyGroup>", StringComparison.Ordinal);
                        csproj = csproj[..end] + added + csproj[end..];
                    }
                    Write(directory, $"src/{project}/{project}.csproj", csproj);
                }
                foreach ((string path, string text) in files ?? new Dictionary<string, string>())
                {
                    Write(directory, path, text);
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
                if (push is not null)
                {
                    Write(directory, "packages.push", string.Join('\n', push) + "\n");
                }

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
            "OtherReadme" => Sdk("<IsPackable>true</IsPackable><PackageReadmeFile>hosting-guide.md</PackageReadmeFile>"),
            "NotTrimmable" => Sdk("<IsPackable>true</IsPackable><IsTrimmable>false</IsTrimmable>"),
            // Each makes the repository trim by its publish alone, over a Directory.Build.props that does not.
            "TrimmedApp" => Sdk("<IsPackable>false</IsPackable><OutputType>Exe</OutputType><PackageReadmeFile></PackageReadmeFile><PublishTrimmed>true</PublishTrimmed>"),
            "AotApp" => Sdk("<IsPackable>false</IsPackable><OutputType>Exe</OutputType><PackageReadmeFile></PackageReadmeFile><PublishAot>true</PublishAot>"),
            // An app was not held to DebugType before: Release symbols were a package rule.
            "AppWithoutSymbols" => Sdk("<IsPackable>false</IsPackable><OutputType>Exe</OutputType><PackageReadmeFile></PackageReadmeFile><DebugType>none</DebugType>"),
            // Correct in Release, as every rule reads it; only the Debug evaluation can see this.
            "DebugWithoutSymbols" => Sdk("<IsPackable>true</IsPackable>",
                "<PropertyGroup Condition=\"'$(Configuration)' == 'Debug'\"><DebugType>none</DebugType></PropertyGroup>"),
            "Beta" => Sdk("<IsPackable>true</IsPackable>", "<ItemGroup><ProjectReference Include=\"../Good/Good.csproj\" /></ItemGroup>"),
            "Gamma" => Sdk("<IsPackable>true</IsPackable>", "<ItemGroup><ProjectReference Include=\"../Beta/Beta.csproj\" /></ItemGroup>"),
            // Packs nothing, so it may reference any family.
            "FamilyTests" => Sdk("<IsPackable>false</IsPackable><IsTestProject>true</IsTestProject><OutputType>Exe</OutputType>",
                "<ItemGroup><ProjectReference Include=\"../Good/Good.csproj\" /><ProjectReference Include=\"../Beta/Beta.csproj\" /></ItemGroup>"),
            "TakesOld" or "ExemptTakesOld" => Sdk("<IsPackable>true</IsPackable>",
                "<ItemGroup><PackageReference Include=\"Bennewitz.Ninja.AppServices.Abstractions\" /></ItemGroup>"),
            "TakesNew" => Sdk("<IsPackable>true</IsPackable>",
                "<ItemGroup><PackageReference Include=\"Bennewitz.Ninja.AppServices.Abstractions\" VersionOverride=\"2026.3.924\" /></ItemGroup>"),
            // Takes AppServices, which takes AppServices.Abstractions: the second only transitively.
            "AppTakesOld" => Sdk("<IsPackable>false</IsPackable><OutputType>Exe</OutputType><PackageReadmeFile></PackageReadmeFile>",
                "<ItemGroup><PackageReference Include=\"Bennewitz.Ninja.AppServices\" /></ItemGroup>"),
            "TestsTakeOld" => Sdk("<IsPackable>false</IsPackable><IsTestProject>true</IsTestProject><OutputType>Exe</OutputType>",
                "<ItemGroup><PackageReference Include=\"Bennewitz.Ninja.AppServices.Abstractions\" /></ItemGroup>"),
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
            ConventionsScript.Run(workingDirectory, arguments);

        private static (int ExitCode, string Output) Execute(string file, string workingDirectory, params string[] arguments) =>
            ConventionsScript.Execute(file, workingDirectory, arguments);

        public static string RepoRoot() => ConventionsScript.RepoRoot();
    }
}
