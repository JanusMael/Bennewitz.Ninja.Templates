using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Templates.Tests.RepoConventions;

/// <summary>
/// The workflows a generated repository ships run <c>scripts/repo-conventions.cs</c> where
/// <c>docs/repository-conventions.md</c> says they do: CI on every push and pull request, and the
/// release before anything can publish.
/// </summary>
/// <remarks>
/// ⚠ Like <c>PackagingTests</c>, these read the SHIPPED workflows by path. Actions never runs them
/// in this repository, so nothing else would notice one rotting.
/// </remarks>
public sealed class ConventionsWorkflowTests
{
    private const string ShippedCi = "templates/bbpkg/.github/workflows/ci.yml";
    private const string ShippedRelease = "templates/bbpkg/.github/workflows/release.yml";
    private const string ShippedRepositoryJson = "templates/bbpkg/.github/repository.json";

    [Fact]
    public void The_shipped_ci_has_a_conventions_job_that_runs_check_with_a_read_only_token()
    {
        string job = Job(Workflow(ShippedCi), "conventions");

        Assert.Contains("dotnet run --file scripts/repo-conventions.cs -- check", job, StringComparison.Ordinal);
        Assert.DoesNotContain("--admin", job, StringComparison.Ordinal);
        Assert.Contains("GH_TOKEN: ${{ github.token }}", job, StringComparison.Ordinal);
        Assert.Contains("contents: read", job, StringComparison.Ordinal);
    }

    [Fact]
    public void The_shipped_release_checks_the_conventions_before_it_can_log_in_or_publish()
    {
        string workflow = Workflow(ShippedRelease);

        int check = workflow.IndexOf("repo-conventions.cs -- check --release", StringComparison.Ordinal);
        Assert.True(check >= 0, "The release no longer runs `repo-conventions check --release`.");

        foreach (string later in (string[])["NuGet/login", "dotnet nuget push", "gh release create"])
        {
            int position = workflow.IndexOf(later, StringComparison.Ordinal);
            Assert.True(position > check, $"`{later}` runs before the conventions check, so a release can publish from a repository the conventions reject.");
        }
    }

    [Fact]
    public void The_release_preflight_runs_in_both_modes_not_only_when_releasing()
    {
        string workflow = Workflow(ShippedRelease);
        string[] lines = workflow.Split('\n');
        int run = Array.FindIndex(lines, l => l.Contains("repo-conventions.cs -- check --release", StringComparison.Ordinal));
        int step = Array.FindLastIndex(lines, run, l => l.TrimStart().StartsWith("- name:", StringComparison.Ordinal));

        Assert.True(step >= 0 && run > step);
        Assert.DoesNotContain(lines[step..run], l => l.TrimStart().StartsWith("if:", StringComparison.Ordinal));
    }

    [Fact]
    public void Every_required_check_the_template_ships_is_a_job_in_its_ci()
    {
        JsonNode config = JsonNode.Parse(File.ReadAllText(Path.Combine(RepoRoot(), ShippedRepositoryJson)))!;
        string[] required = [.. config["requiredChecks"]!.AsArray().Select(c => c!.GetValue<string>())];
        string[] jobs = [.. Regex.Matches(Workflow(ShippedCi), @"^  ([A-Za-z0-9_-]+):\s*$", RegexOptions.Multiline).Select(m => m.Groups[1].Value)];

        Assert.Contains("conventions", required);
        Assert.All(required, check => Assert.Contains(check, jobs));
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
