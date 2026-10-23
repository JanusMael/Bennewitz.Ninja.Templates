using System.Text.Json.Nodes;
using Templates.Tests.RepoConventions;

namespace Templates.Tests.Release;

/// <summary>
/// <c>templates/bbpkg/scripts/release-target.cs</c>, which resolves what a release publishes
/// (plans/00009 decisions 9 to 13), over small real repositories: each tag form and each dispatch
/// form, in a repository without families and in one with them.
/// </summary>
/// <remarks>
/// ⛔ <b>The release workflow's YAML never runs before a real tag,</b> and nothing a conventions run
/// does executes it. This script is the part of the irreversible path that can be tested, so every
/// refusal is asserted by its message as well as its exit code: a refusal that fires for the wrong
/// reason would pass on the exit code alone.
/// </remarks>
public sealed class ReleaseTargetTests
{
    // A new object per test: the tests run in parallel, and a JsonObject is not safe to share.
    private static JsonObject TwoFamilies() => new()
    {
        ["widgets"] = new JsonArray("Fixture.Widget"),
        ["gadgets"] = new JsonArray("Fixture.Gadget", "Fixture.Gizmo"),
    };

    [Fact]
    public void A_v_tag_releases_everything_packages_push_names_where_there_are_no_families()
    {
        using Repository repository = new(families: null);

        Resolved resolved = repository.Resolve("--event", "push", "--ref", "v2026.4.1022");

        Assert.Equal("2026.4.1022", resolved["VERSION"]);
        Assert.Equal("v2026.4.1022", resolved["TAG"]);
        Assert.Equal("", resolved["FAMILY"]);
        Assert.Equal("Fixture.Widget Fixture.Gadget Fixture.Gizmo", resolved["IDS"]);
        Assert.Equal("", resolved["PREVIOUS_TAG"]);
    }

    [Fact]
    public void A_dispatch_releases_everything_packages_push_names_where_there_are_no_families()
    {
        using Repository repository = new(families: null);

        Resolved resolved = repository.Resolve("--event", "workflow_dispatch", "--ref", "main", "--version", "2026.4.1022", "--family", "");

        Assert.Equal("v2026.4.1022", resolved["TAG"]);
        Assert.Equal("Fixture.Widget Fixture.Gadget Fixture.Gizmo", resolved["IDS"]);
    }

    [Fact]
    public void A_family_tag_is_refused_where_there_are_no_families()
    {
        using Repository repository = new(families: null);

        Refusal refusal = repository.Refuse("--event", "push", "--ref", "widgets/v2026.4.1022");

        Assert.Contains("names the family \"widgets\", but .github/repository.json declares no families", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_dispatch_naming_a_family_is_refused_where_there_are_no_families()
    {
        using Repository repository = new(families: null);

        Refusal refusal = repository.Refuse("--event", "workflow_dispatch", "--version", "2026.4.1022", "--family", "widgets");

        Assert.Contains("Dispatch with the version alone.", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_family_tag_releases_that_family_s_ids_only()
    {
        using Repository repository = new(TwoFamilies());

        Resolved resolved = repository.Resolve("--event", "push", "--ref", "gadgets/v2026.4.1022");

        Assert.Equal("2026.4.1022", resolved["VERSION"]);
        Assert.Equal("gadgets/v2026.4.1022", resolved["TAG"]);
        Assert.Equal("gadgets", resolved["FAMILY"]);
        Assert.Equal("Fixture.Gadget Fixture.Gizmo", resolved["IDS"]);
    }

    /// <summary>A dispatch's family is typed by hand, so its case is forgiven; the tag it makes is lower-case.</summary>
    [Fact]
    public void A_dispatch_names_its_family_beside_the_version()
    {
        using Repository repository = new(TwoFamilies());

        Resolved resolved = repository.Resolve("--event", "workflow_dispatch", "--ref", "main", "--version", "2026.4.1022", "--family", "Widgets");

        Assert.Equal("widgets/v2026.4.1022", resolved["TAG"]);
        Assert.Equal("widgets", resolved["FAMILY"]);
        Assert.Equal("Fixture.Widget", resolved["IDS"]);
    }

    [Fact]
    public void A_bare_tag_is_refused_where_there_are_families()
    {
        using Repository repository = new(TwoFamilies());

        Refusal refusal = repository.Refuse("--event", "push", "--ref", "v2026.4.1022");

        Assert.Contains("The tag v2026.4.1022 names no family.", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("such as widgets/v2026.4.1022", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_dispatch_without_a_family_is_refused_where_there_are_families()
    {
        using Repository repository = new(TwoFamilies());

        Refusal refusal = repository.Refuse("--event", "workflow_dispatch", "--version", "2026.4.1022", "--family", "");

        Assert.Contains("so a dispatch names the one it releases", refusal.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("push", "gizmos/v2026.4.1022", "", "The tag names the family \"gizmos\", which .github/repository.json does not declare")]
    [InlineData("workflow_dispatch", "main", "gizmos", "The dispatch names the family \"gizmos\", which .github/repository.json does not declare")]
    [InlineData("push", "Widgets/v2026.4.1022", "", "family tags are lower-case: widgets/v<version>")]
    public void A_family_the_repository_does_not_declare_is_refused(string eventName, string reference, string family, string message)
    {
        using Repository repository = new(TwoFamilies());

        Refusal refusal = repository.Refuse("--event", eventName, "--ref", reference, "--version", "2026.4.1022", "--family", family);

        Assert.Contains(message, refusal.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The notes of a family's release start from that family's newest tag below it, never from
    /// another family's tag, and never from a later one of its own.
    /// </summary>
    [Fact]
    public void The_previous_tag_is_the_family_s_newest_below_this_version()
    {
        using Repository repository = new(TwoFamilies(),"widgets/v2026.3.920", "widgets/v2026.4.1001", "gadgets/v2026.4.1015", "widgets/v2026.4.1101", "v2026.4.1020");

        Resolved resolved = repository.Resolve("--event", "push", "--ref", "widgets/v2026.4.1022");

        Assert.Equal("widgets/v2026.4.1001", resolved["PREVIOUS_TAG"]);
    }

    [Fact]
    public void A_family_s_first_release_has_no_previous_tag()
    {
        using Repository repository = new(TwoFamilies(),"widgets/v2026.4.1001");

        Resolved resolved = repository.Resolve("--event", "push", "--ref", "gadgets/v2026.4.1022");

        Assert.Equal("", resolved["PREVIOUS_TAG"]);
    }

    [Theory]
    [InlineData("v2026.3.1001", "names quarter 3, but 2026-10-01 is in quarter 4: 2026.4.1001")]
    [InlineData("v2026.4.1022.1", "is not YYYY.Q.MMDD")]
    [InlineData("v2026.4.1022-beta", "is not YYYY.Q.MMDD")]
    [InlineData("v2026.4.1032", "names no date")]
    [InlineData("v2026.2.229", "names no date")]
    public void A_version_that_is_not_a_family_version_is_refused(string tag, string message)
    {
        using Repository repository = new(families: null);

        Refusal refusal = repository.Refuse("--event", "push", "--ref", tag);

        Assert.Contains(message, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_dispatched_version_with_its_tag_s_v_is_refused()
    {
        using Repository repository = new(families: null);

        Refusal refusal = repository.Refuse("--event", "workflow_dispatch", "--version", "v2026.4.1022");

        Assert.Contains("The version v2026.4.1022 is not YYYY.Q.MMDD", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_leap_day_is_a_date()
    {
        using Repository repository = new(families: null);

        Resolved resolved = repository.Resolve("--event", "push", "--ref", "v2028.1.229");

        Assert.Equal("2028.1.229", resolved["VERSION"]);
    }

    [Fact]
    public void A_family_naming_an_id_packages_push_does_not_is_refused()
    {
        using Repository repository = new(new JsonObject { ["widgets"] = new JsonArray("Fixture.Widget", "Fixture.Unlisted") });

        Refusal refusal = repository.Refuse("--event", "push", "--ref", "widgets/v2026.4.1022");

        Assert.Contains("The widgets family names Fixture.Unlisted, which packages.push does not.", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_github_env_file_receives_every_line()
    {
        using Repository repository = new(TwoFamilies());
        string env = Path.Combine(repository.Directory, "github-env");
        File.WriteAllText(env, "EARLIER=kept\n");

        repository.Resolve("--event", "push", "--ref", "widgets/v2026.4.1022", "--github-env", env);

        Assert.Equal(
            ["EARLIER=kept", "VERSION=2026.4.1022", "TAG=widgets/v2026.4.1022", "FAMILY=widgets", "IDS=Fixture.Widget", "PREVIOUS_TAG="],
            File.ReadAllLines(env));
    }

    /// <summary>
    /// A family's release packs that family's projects alone, found by the PackageId MSBuild evaluates:
    /// Gizmo's project is named nothing like its id, and the tests project packs nothing.
    /// </summary>
    [Fact]
    public void Projects_lists_the_project_that_packs_each_of_the_family_s_ids()
    {
        using Repository repository = new(TwoFamilies());
        repository.WriteSolution();
        string list = Path.Combine(repository.Directory, "family-projects.txt");

        (int exit, string output) = ShippedScript.ReleaseTarget.Run(repository.Directory, "projects", "--family", "gadgets", "--out", list);

        Assert.True(exit == 0, output);
        Assert.Equal(
            [Path.Combine("src", "Gadget", "Gadget.csproj"), Path.Combine("src", "Contraption", "Contraption.csproj")],
            File.ReadAllLines(list));
    }

    [Fact]
    public void Projects_refuses_a_family_id_that_no_project_packs()
    {
        using Repository repository = new(TwoFamilies());
        repository.WriteSolution(withGizmo: false);

        (int exit, string output) = ShippedScript.ReleaseTarget.Run(repository.Directory, "projects", "--family", "gadgets");

        Assert.Equal(1, exit);
        Assert.Contains("No packable project in the solution has the PackageId Fixture.Gizmo, which the gadgets family names.", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Projects_refuses_a_repository_without_families()
    {
        using Repository repository = new(families: null);

        (int exit, string output) = ShippedScript.ReleaseTarget.Run(repository.Directory, "projects", "--family", "widgets");

        Assert.Equal(1, exit);
        Assert.Contains("declares no families, so there is no family to pack", output, StringComparison.Ordinal);
    }

    [Fact]
    public void An_unknown_verb_is_a_usage_error()
    {
        using Repository repository = new(families: null);

        (int exit, string output) = ShippedScript.ReleaseTarget.Run(repository.Directory, "publish");

        Assert.Equal(2, exit);
        Assert.Contains("the first argument must be 'resolve' or 'projects'", output, StringComparison.Ordinal);
    }

    private sealed class Resolved(Dictionary<string, string> values)
    {
        public string this[string key] => Assert.Contains(key, (IReadOnlyDictionary<string, string>)values);
    }

    private sealed record Refusal(string Message);

    /// <summary>A repository with three published ids, its families if any, and the given tags, in a git repository.</summary>
    private sealed class Repository : IDisposable
    {
        public Repository(JsonObject? families, params string[] tags)
        {
            Directory = Path.Combine(Path.GetTempPath(), "release-target-" + Guid.NewGuid().ToString("N"));
            JsonObject config = new() { ["description"] = "Fixture." };
            if (families is not null)
            {
                config["families"] = families.DeepClone();
            }
            Write(".github/repository.json", config.ToJsonString());
            Write("packages.push", "# Published ids.\n\nFixture.Widget\nFixture.Gadget\nFixture.Gizmo\n");

            Git("init", "-q");
            Git("-c", "user.name=Fixture", "-c", "user.email=fixture@example.com", "commit", "-q", "--allow-empty", "-m", "Fixture");
            foreach (string tag in tags)
            {
                Git("tag", tag);
            }
        }

        public string Directory { get; }

        public Resolved Resolve(params string[] arguments)
        {
            (int exit, string output) = ShippedScript.ReleaseTarget.Run(Directory, ["resolve", .. arguments]);
            Assert.True(exit == 0, output);
            return new Resolved(output.Replace("\r\n", "\n", StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal));
        }

        public Refusal Refuse(params string[] arguments)
        {
            (int exit, string output) = ShippedScript.ReleaseTarget.Run(Directory, ["resolve", .. arguments]);
            Assert.True(exit == 1, $"Expected a refusal, exit 1; got {exit}:\n{output}");
            Assert.Contains("::error::", output, StringComparison.Ordinal);
            Assert.Contains("Nothing was published.", output, StringComparison.Ordinal);
            Assert.DoesNotContain("VERSION=", output, StringComparison.Ordinal);
            return new Refusal(output);
        }

        /// <summary>
        /// Widget, Gadget and Contraption (packing Fixture.Gizmo) as libraries, and a test project that
        /// packs nothing.
        /// </summary>
        public void WriteSolution(bool withGizmo = true)
        {
            string[] projects = withGizmo ? ["Widget", "Gadget", "Contraption", "Fixture.Tests"] : ["Widget", "Gadget", "Fixture.Tests"];
            Write("Fixture.slnx", "<Solution>\n" + string.Concat(projects.Select(p => $"  <Project Path=\"src/{p}/{p}.csproj\" />\n")) + "</Solution>\n");
            foreach (string project in projects)
            {
                string properties = project switch
                {
                    "Contraption" => "<PackageId>Fixture.Gizmo</PackageId>",
                    "Fixture.Tests" => "<IsPackable>false</IsPackable>",
                    _ => $"<PackageId>Fixture.{project}</PackageId>",
                };
                Write($"src/{project}/{project}.csproj",
                    $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net10.0</TargetFramework>{properties}</PropertyGroup>\n</Project>\n");
            }
        }

        public void Dispose()
        {
            try
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private void Write(string relative, string text)
        {
            string path = Path.Combine(Directory, relative.Replace('/', Path.DirectorySeparatorChar));
            System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }

        private void Git(params string[] arguments)
        {
            (int exit, string output) = ConventionsScript.Execute("git", Directory, arguments);
            Assert.True(exit == 0, output);
        }
    }
}
