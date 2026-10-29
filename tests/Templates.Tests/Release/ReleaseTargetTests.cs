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
            ["EARLIER=kept", "VERSION=2026.4.1022", "TAG=widgets/v2026.4.1022", "FAMILY=widgets", "IDS=Fixture.Widget", "PREVIOUS_TAG=", "LATEST=true"],
            File.ReadAllLines(env));
    }

    /// <summary>
    /// A grown repository's bare tags were its first family's releases (plans/00011 decision 7), so
    /// that family's notes start from the newest of them, and no other family's do.
    /// </summary>
    [Fact]
    public void The_family_bare_tags_names_starts_from_the_newest_bare_tag()
    {
        using Repository repository = new(TwoFamilies(), "v2026.3.920", "v2026.4.1001", "v2026.4.1101");
        repository.Configure("bareTags", "widgets");

        Assert.Equal("v2026.4.1001", repository.Resolve("--event", "push", "--ref", "widgets/v2026.4.1022")["PREVIOUS_TAG"]);
        Assert.Equal("", repository.Resolve("--event", "push", "--ref", "gadgets/v2026.4.1022")["PREVIOUS_TAG"]);
    }

    [Fact]
    public void A_family_s_own_tag_newer_than_the_bare_ones_is_its_previous()
    {
        using Repository repository = new(TwoFamilies(), "v2026.4.1001", "widgets/v2026.4.1015");
        repository.Configure("bareTags", "widgets");

        Assert.Equal("widgets/v2026.4.1015", repository.Resolve("--event", "push", "--ref", "widgets/v2026.4.1022")["PREVIOUS_TAG"]);
    }

    [Fact]
    public void Bare_tags_naming_no_declared_family_are_refused()
    {
        using Repository repository = new(TwoFamilies());
        repository.Configure("bareTags", "gizmos");

        Refusal refusal = repository.Refuse("--event", "push", "--ref", "widgets/v2026.4.1022");

        Assert.Contains("\"bareTags\" in .github/repository.json is \"gizmos\", which is not a declared family", refusal.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// "Latest" names the repository's newest release, whichever family's (plans/00011 decision 8):
    /// a release below another family's newer tag, or a bare one, leaves it where it is.
    /// </summary>
    [Theory]
    [InlineData("true", new[] { "widgets/v2026.4.1001", "gadgets/v2026.4.1015", "v2026.4.1020" })]
    [InlineData("true", new[] { "gadgets/v2026.4.1022" })]
    [InlineData("false", new[] { "gadgets/v2026.4.1101" })]
    [InlineData("false", new[] { "widgets/v2026.4.1101" })]
    [InlineData("false", new[] { "v2026.4.1101" })]
    public void A_family_release_is_latest_unless_a_tag_is_newer(string latest, string[] tags)
    {
        using Repository repository = new(TwoFamilies(), tags);

        Assert.Equal(latest, repository.Resolve("--event", "push", "--ref", "widgets/v2026.4.1022")["LATEST"]);
    }

    [Fact]
    public void A_repository_without_families_leaves_latest_to_github()
    {
        using Repository repository = new(families: null, "v2026.4.1101");

        Assert.Equal("", repository.Resolve("--event", "push", "--ref", "v2026.4.1022")["LATEST"]);
    }

    /// <summary>
    /// A family's notes are its own commits (plans/00011 decision 6): those touching its projects and
    /// its README, and under a heading of their own those touching the build every family shares. Not
    /// another family's, and not the repository's bookkeeping.
    /// </summary>
    [Fact]
    public void A_family_s_notes_hold_its_own_commits_and_the_shared_build_s()
    {
        using Repository repository = new(TwoFamilies());
        repository.WriteSolution();
        repository.Commit("chore: the solution");
        repository.Tag("widgets/v2026.4.1001");
        repository.Commit("feat(widget): a widget change", "src/Widget/Change.cs");
        repository.Commit("feat(gadget): a gadget change", "src/Gadget/Change.cs");
        repository.Commit("build: a shared pin", "Directory.Packages.props");
        repository.Commit("build: the generated grants", "AssemblyInfo.InternalsVisibleTo.cs");
        repository.Commit("docs: the widgets README", "docs/widgets.md");
        repository.Commit("docs: the gadgets README", "docs/gadgets.md");
        repository.Commit("docs(progress): bookkeeping", "PROGRESS.md");

        string notes = repository.Notes("--family", "widgets", "--previous", "widgets/v2026.4.1001");

        string[] lines = Lines(notes);
        int shared = Array.IndexOf(lines, "## The build every family shares");
        Assert.True(shared > 0, notes);
        Assert.Equal(["- docs: the widgets README", "- feat(widget): a widget change"], Entries(lines[..shared]));
        Assert.Equal(["- build: the generated grants", "- build: a shared pin"], Entries(lines[shared..]));
        Assert.DoesNotContain("gadget", notes, StringComparison.Ordinal);
        Assert.DoesNotContain("bookkeeping", notes, StringComparison.Ordinal);
    }

    /// <summary>
    /// An imported history shares no commit with the family's tag, so all of it is "after" that tag.
    /// Its own root build files are not this family's (they count only descending from the tag), and
    /// its projects are another family's; an imported family's own commits are still its own.
    /// </summary>
    [Fact]
    public void An_unrelated_history_merged_in_lists_only_its_own_family_s_projects()
    {
        using Repository repository = new(TwoFamilies());
        repository.WriteSolution();
        repository.Commit("chore: the solution");
        repository.Tag("widgets/v2026.4.1001");
        repository.Tag("gadgets/v2026.4.1001");
        repository.MergeUnrelated(
            ("build: the imported repository's props", "Directory.Build.props"),
            ("feat(gadget): imported work", "src/Gadget/Imported.cs"));

        string widgets = repository.Notes("--family", "widgets", "--previous", "widgets/v2026.4.1001");
        string gadgets = repository.Notes("--family", "gadgets", "--previous", "gadgets/v2026.4.1001");

        Assert.DoesNotContain("imported", widgets, StringComparison.Ordinal);
        Assert.Contains("- feat(gadget): imported work", gadgets, StringComparison.Ordinal);
        Assert.DoesNotContain("props", gadgets, StringComparison.Ordinal);
    }

    /// <summary>
    /// A branch cut before the family's tag and merged after it: its commit does not descend from the
    /// tag, but touched the family's project, so it is the family's.
    /// </summary>
    [Fact]
    public void A_commit_from_a_branch_cut_before_the_tag_is_listed()
    {
        using Repository repository = new(TwoFamilies());
        repository.WriteSolution();
        repository.Commit("chore: the solution");
        repository.Branch("topic");
        repository.Commit("feat(widget): from a branch", "src/Widget/Topic.cs");
        repository.Switch("-");
        repository.Commit("feat(widget): released", "src/Widget/Released.cs");
        repository.Tag("widgets/v2026.4.1001");
        repository.MergeBranch("topic");

        string notes = repository.Notes("--family", "widgets", "--previous", "widgets/v2026.4.1001");

        Assert.Contains("- feat(widget): from a branch", notes, StringComparison.Ordinal);
        Assert.DoesNotContain("released", notes, StringComparison.Ordinal);
    }

    /// <summary>A project a family's package references and that packs nothing ships inside it.</summary>
    [Fact]
    public void A_commit_to_an_unpacked_project_a_family_references_is_listed()
    {
        using Repository repository = new(TwoFamilies());
        repository.WriteSolution(widgetReferencesShared: true);
        repository.Commit("chore: the solution");
        repository.Tag("widgets/v2026.4.1001");
        repository.Commit("feat(shared): inside the widget", "src/Shared/Change.cs");

        string widgets = repository.Notes("--family", "widgets", "--previous", "widgets/v2026.4.1001");
        string gadgets = repository.Notes("--family", "gadgets", "--previous", "widgets/v2026.4.1001");

        Assert.Contains("- feat(shared): inside the widget", widgets, StringComparison.Ordinal);
        Assert.DoesNotContain("inside the widget", gadgets, StringComparison.Ordinal);
    }

    [Fact]
    public void A_family_s_first_release_says_so()
    {
        using Repository repository = new(TwoFamilies());

        Assert.Equal("The first release of the widgets family.\n", repository.Notes("--family", "widgets", "--previous", ""));
    }

    private static string[] Lines(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

    // A notes line without its short hash, which differs from run to run.
    private static string[] Entries(string[] lines) =>
        [.. lines.Where(l => l.StartsWith("- ", StringComparison.Ordinal)).Select(l => l[..l.LastIndexOf(" (", StringComparison.Ordinal)])];

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
        Assert.Contains("the first argument must be 'resolve', 'projects' or 'notes'", output, StringComparison.Ordinal);
    }

    private sealed class Resolved(Dictionary<string, string> values)
    {
        public string this[string key] => Assert.Contains(key, (IReadOnlyDictionary<string, string>)values);
    }

    private sealed record Refusal(string Message);

    /// <summary>A repository with three published ids, its families if any, and the given tags, in a git repository.</summary>
    private sealed class Repository : IDisposable
    {
        private readonly JsonObject config = new() { ["description"] = "Fixture." };
        private int written;

        public Repository(JsonObject? families, params string[] tags)
        {
            Directory = Path.Combine(Path.GetTempPath(), "release-target-" + Guid.NewGuid().ToString("N"));
            if (families is not null)
            {
                config["families"] = families.DeepClone();
            }
            Write(".github/repository.json", config.ToJsonString());
            Write("packages.push", "# Published ids.\n\nFixture.Widget\nFixture.Gadget\nFixture.Gizmo\n");

            Git(Directory, "init", "-q");
            Git(Directory, "add", "-A");
            Commit("Fixture");
            foreach (string tag in tags)
            {
                Tag(tag);
            }
        }

        public string Directory { get; }

        /// <summary>Sets a key of .github/repository.json; the script reads the working tree.</summary>
        public void Configure(string key, string value)
        {
            config[key] = value;
            Write(".github/repository.json", config.ToJsonString());
        }

        /// <summary>A commit touching each of <paramref name="paths"/> with new text, or of whatever is pending.</summary>
        public void Commit(string message, params string[] paths)
        {
            foreach (string path in paths)
            {
                Write(path, Content(path, $"{message} {++written}"));
            }
            Git(Directory, "add", "-A");
            Git(Directory, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.com", "commit", "-q", "--allow-empty", "-m", message);
        }

        public void Tag(string name) => Git(Directory, "tag", name);

        public void Branch(string name) => Git(Directory, "switch", "-q", "-c", name);

        public void Switch(string name) => Git(Directory, "switch", "-q", name);

        public void MergeBranch(string name) =>
            Git(Directory, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.com", "merge", "-q", "--no-ff", "--no-edit", name);

        /// <summary>
        /// Another repository's history, one commit per (message, path), merged in as an import is:
        /// it shares no commit with this one.
        /// </summary>
        public void MergeUnrelated(params (string Message, string Path)[] commits)
        {
            string other = Directory + "-imported";
            System.IO.Directory.CreateDirectory(other);
            try
            {
                Git(other, "init", "-q");
                foreach ((string message, string path) in commits)
                {
                    string file = Path.Combine(other, path.Replace('/', Path.DirectorySeparatorChar));
                    System.IO.Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                    File.WriteAllText(file, Content(path, message));
                    Git(other, "add", "-A");
                    Git(other, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.com", "commit", "-q", "-m", message);
                }
                Git(Directory, "fetch", "-q", other, "HEAD:imported");
                Git(Directory, "-c", "user.name=Fixture", "-c", "user.email=fixture@example.com", "merge", "-q", "--no-edit", "--allow-unrelated-histories", "imported");
            }
            finally
            {
                Delete(other);
            }
        }

        /// <summary>Runs <c>notes</c> with the given arguments and returns the file it wrote.</summary>
        public string Notes(params string[] arguments)
        {
            // Beside the repository rather than in it, so the next commit does not take it.
            string file = Directory + "-notes.md";
            try
            {
                (int exit, string output) = ShippedScript.ReleaseTarget.Run(Directory, ["notes", .. arguments, "--out", file]);
                Assert.True(exit == 0, output);
                return File.ReadAllText(file);
            }
            finally
            {
                File.Delete(file);
            }
        }

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
        /// packs nothing. With <paramref name="widgetReferencesShared"/>, Widget also references Shared,
        /// a project outside the solution that packs nothing.
        /// </summary>
        public void WriteSolution(bool withGizmo = true, bool widgetReferencesShared = false)
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
                string references = project == "Widget" && widgetReferencesShared
                    ? "  <ItemGroup><ProjectReference Include=\"../Shared/Shared.csproj\" /></ItemGroup>\n"
                    : "";
                Write($"src/{project}/{project}.csproj",
                    $"<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net10.0</TargetFramework>{properties}</PropertyGroup>\n{references}</Project>\n");
            }
            if (widgetReferencesShared)
            {
                Write("src/Shared/Shared.csproj",
                    "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net10.0</TargetFramework><IsPackable>false</IsPackable></PropertyGroup>\n</Project>\n");
            }
        }

        public void Dispose() => Delete(Directory);

        // MSBuild imports a Directory.*.props or .targets file into every evaluation, so it must stay
        // a project; anything else is changed by a comment.
        private static string Content(string path, string text) =>
            path.EndsWith(".props", StringComparison.Ordinal) || path.EndsWith(".targets", StringComparison.Ordinal)
                ? $"<Project><!-- {text} --></Project>\n"
                : $"// {text}\n";

        private static void Delete(string directory)
        {
            try
            {
                System.IO.Directory.Delete(directory, recursive: true);
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

        private static void Git(string directory, params string[] arguments)
        {
            (int exit, string output) = ConventionsScript.Execute("git", directory, arguments);
            Assert.True(exit == 0, output);
        }
    }
}
