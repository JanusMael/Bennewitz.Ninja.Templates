using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Templates.Tests.RepoConventions;

/// <summary>
/// Both repositories' workflows run <c>scripts/repo-conventions.cs</c> where
/// <c>docs/repository-conventions.md</c> says they do: CI on every push and pull request, and the
/// release before anything can publish. "Both" means this repository's own and the ones a
/// generated repository ships.
/// </summary>
/// <remarks>
/// ⚠ Like <c>PackagingTests</c>, these read the SHIPPED workflows by path. Actions never runs them
/// in this repository, so nothing else would notice one rotting.
/// </remarks>
public sealed class ConventionsWorkflowTests
{
    private const string RootCi = ".github/workflows/ci.yml";
    private const string RootRelease = ".github/workflows/release.yml";
    private const string RootRepositoryJson = ".github/repository.json";
    private const string ShippedCi = "templates/bbpkg/.github/workflows/ci.yml";
    private const string ShippedRelease = "templates/bbpkg/.github/workflows/release.yml";
    private const string ShippedRepositoryJson = "templates/bbpkg/.github/repository.json";

    [Theory]
    [InlineData(RootCi)]
    [InlineData(ShippedCi)]
    public void The_ci_has_a_conventions_job_that_runs_check_with_a_read_only_token(string ci)
    {
        string job = Job(Workflow(ci), "conventions");

        Assert.Contains("dotnet run --file scripts/repo-conventions.cs -- check", job, StringComparison.Ordinal);
        Assert.DoesNotContain("--admin", job, StringComparison.Ordinal);
        Assert.Contains("GH_TOKEN: ${{ github.token }}", job, StringComparison.Ordinal);
        Assert.Contains("contents: read", job, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(RootRelease)]
    [InlineData(ShippedRelease)]
    public void The_release_checks_the_conventions_before_it_can_log_in_or_publish(string release)
    {
        string workflow = Workflow(release);

        int check = workflow.IndexOf("repo-conventions.cs -- check --release", StringComparison.Ordinal);
        Assert.True(check >= 0, "The release no longer runs `repo-conventions check --release`.");

        foreach (string later in (string[])["NuGet/login", "dotnet nuget push", "gh release create"])
        {
            int position = workflow.IndexOf(later, StringComparison.Ordinal);
            Assert.True(position > check, $"`{later}` runs before the conventions check, so a release can publish from a repository the conventions reject.");
        }
    }

    [Theory]
    [InlineData(RootRelease)]
    [InlineData(ShippedRelease)]
    public void The_release_preflight_runs_in_both_modes_not_only_when_releasing(string release)
    {
        string[] lines = Workflow(release).Split('\n');
        int run = Array.FindIndex(lines, l => l.Contains("repo-conventions.cs -- check --release", StringComparison.Ordinal));
        int step = Array.FindLastIndex(lines, run, l => l.TrimStart().StartsWith("- name:", StringComparison.Ordinal));

        Assert.True(step >= 0 && run > step);
        Assert.DoesNotContain(lines[step..run], l => l.TrimStart().StartsWith("if:", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(RootCi, RootRepositoryJson)]
    [InlineData(ShippedCi, ShippedRepositoryJson)]
    public void Every_required_check_is_a_job_in_the_ci(string ci, string repositoryJson)
    {
        JsonNode config = JsonNode.Parse(File.ReadAllText(Path.Combine(RepoRoot(), repositoryJson)))!;
        string[] required = [.. config["requiredChecks"]!.AsArray().Select(c => c!.GetValue<string>())];

        Assert.Contains("conventions", required);
        Assert.All(required, check => Assert.Contains(check, JobIds(Workflow(ci))));
    }

    /// <summary>
    /// A generated repository requires trimming from its first commit (plans/00004 step 4). It costs
    /// nothing there, because the template's src/Directory.Build.props already marks every shipped
    /// assembly trimmable; this keeps it from quietly ceasing to be required.
    /// </summary>
    [Fact]
    public void The_template_requires_trimming()
    {
        JsonNode config = JsonNode.Parse(File.ReadAllText(Path.Combine(RepoRoot(), ShippedRepositoryJson)))!;

        Assert.Equal("required", config["trimming"]?.GetValue<string>());
    }

    /// <summary>
    /// Job ids: keys two spaces in under <c>jobs:</c>, and only there. Keys under <c>on:</c>, such as
    /// <c>push</c>, sit at the same indent and are not jobs.
    /// </summary>
    private static string[] JobIds(string workflow)
    {
        string[] lines = workflow.Split('\n');
        int jobs = Array.FindIndex(lines, l => l.TrimEnd() == "jobs:");
        Assert.True(jobs >= 0, "No `jobs:` section.");

        return
        [
            .. lines[(jobs + 1)..]
                .TakeWhile(l => l.Length == 0 || char.IsWhiteSpace(l[0]))
                .Select(l => Regex.Match(l, @"^  ([A-Za-z0-9_-]+):\s*$"))
                .Where(m => m.Success)
                .Select(m => m.Groups[1].Value),
        ];
    }

    /// <summary>The lines of one job, from its id to the next job's id.</summary>
    private static string Job(string workflow, string id)
    {
        string[] lines = workflow.Split('\n');
        int start = Array.FindIndex(lines, l => l.TrimEnd() == $"  {id}:");
        Assert.True(start >= 0, $"No job `{id}`.");

        int end = Array.FindIndex(lines, start + 1, l => Regex.IsMatch(l, @"^  [A-Za-z0-9_-]+:\s*$"));
        return string.Join('\n', lines[start..(end < 0 ? lines.Length : end)]);
    }

    /// <summary>The workflow with comment lines dropped, so an explanation is never mistaken for a step.</summary>
    private static string Workflow(string relativePath)
    {
        string path = Path.Combine(RepoRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(path), relativePath + " does not exist.");
        return string.Join('\n', File.ReadLines(path).Where(line => !line.TrimStart().StartsWith('#')));
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
