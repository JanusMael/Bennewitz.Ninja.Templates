using System.Xml.Linq;

namespace Bennewitz.Ninja.ApiStem.Tests;

/// <summary>
/// Guards what this repository publishes. An API releases binaries and a container image, and packs
/// nothing; a library added under <c>src/</c> later is held to the two package lists from its first
/// commit.
/// </summary>
/// <remarks>
/// ⛔ <b>A published NuGet version can never be replaced</b>, only unlisted, so a project that packs
/// by accident has to fail here, on the push that adds it.
/// </remarks>
public sealed class PackagingTests
{
    [Fact]
    public void The_API_is_never_packed()
    {
        XDocument api = XDocument.Load(Path.Combine(RepoRoot(), "src", "ApiStem", "ApiStem.csproj"));

        Assert.Contains(
            api.Descendants("IsPackable"),
            element => string.Equals(element.Value.Trim(), "false", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Every packable project appears in <c>packages.push</c> or <c>packages.local</c>, and every
    /// listed id has a project. Today both lists are empty and nothing is packable.
    /// </summary>
    [Fact]
    public void Every_packable_project_is_classified()
    {
        // ⛔ Without this, a src/ that moved would make every assertion below pass on nothing.
        Assert.NotEmpty(Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src"), "*.csproj", SearchOption.AllDirectories));

        HashSet<string> packable = PackableIds();
        HashSet<string> classified = [.. Ids("packages.push"), .. Ids("packages.local")];

        string[] unclassified = [.. packable.Except(classified).Order()];
        Assert.True(
            unclassified.Length == 0,
            "Packable but in neither packages.push nor packages.local: " + string.Join(", ", unclassified) +
            ". Classify it, and see AGENTS.md, \"Adding a library\": publishing it by accident cannot be undone.");

        string[] phantom = [.. classified.Except(packable).Order()];
        Assert.True(phantom.Length == 0, "Listed but nothing packs it: " + string.Join(", ", phantom) + ".");
    }

    [Fact]
    public void No_id_is_both_published_and_private()
    {
        string[] both = [.. Ids("packages.push").Intersect(Ids("packages.local")).Order()];

        Assert.True(both.Length == 0, "In both packages.push and packages.local: " + string.Join(", ", both));
    }

    /// <summary>
    /// The release publishes binaries to GitHub Releases and nothing to nuget.org. Comment lines are
    /// dropped first, because the workflow's comments name what it does not do.
    /// </summary>
    [Fact]
    public void The_release_publishes_binaries_and_never_pushes_a_package()
    {
        string path = Path.Combine(RepoRoot(), ".github", "workflows", "release.yml");
        Assert.True(File.Exists(path), "There is no release workflow.");

        string workflow = string.Join('\n', File.ReadLines(path).Where(line => !line.TrimStart().StartsWith('#')));

        Assert.Contains("gh release create", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet nuget push", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("*.nupkg", workflow, StringComparison.Ordinal);
    }

    private static HashSet<string> Ids(string fileName)
    {
        string path = Path.Combine(RepoRoot(), fileName);
        Assert.True(File.Exists(path), fileName + " is missing, so nothing declares what may be published.");

        return
        [
            .. File.ReadLines(path)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith('#'))
        ];
    }

    /// <summary>
    /// Package ids of every packable project, read from the project files, with the same fallback
    /// <c>PackageId</c> has: <c>AssemblyName</c>, then the file name.
    /// </summary>
    private static HashSet<string> PackableIds()
    {
        HashSet<string> ids = [];

        foreach (string project in Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src"), "*.csproj", SearchOption.AllDirectories))
        {
            XDocument document = XDocument.Load(project);

            // ⛔ Fails closed: a project is packable unless it says otherwise, because the SDK packs a
            // class library by default. Counting only an explicit IsPackable=true missed exactly the
            // project nobody classified. The last declaration wins, as it does in MSBuild.
            bool packable = !string.Equals(
                document.Descendants("IsPackable").LastOrDefault()?.Value.Trim(), "false", StringComparison.OrdinalIgnoreCase);

            if (!packable)
            {
                continue;
            }

            string? declared = document.Descendants("PackageId").LastOrDefault()?.Value.Trim();
            string? assembly = document.Descendants("AssemblyName").LastOrDefault()?.Value.Trim();

            ids.Add(!string.IsNullOrWhiteSpace(declared) ? declared
                : !string.IsNullOrWhiteSpace(assembly) ? assembly
                : Path.GetFileNameWithoutExtension(project));
        }

        return ids;
    }

    private static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !Directory.EnumerateFiles(directory.FullName, "*.slnx").Any())
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
