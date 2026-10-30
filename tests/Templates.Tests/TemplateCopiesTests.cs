namespace Templates.Tests;

/// <summary>
/// Files that every template ships as an exact copy of one canonical file.
/// </summary>
/// <remarks>
/// ⚠ This repository's drift check compares only <c>scripts/repo-conventions.cs</c> with the
/// canonical copy in <c>templates/bbpkg</c>. A second template's copy is invisible to it, so without
/// this a repository generated from that template would ship an older script, and its first CI run
/// would fail on drift for a reason that is the template's, not its own.
/// </remarks>
public sealed class TemplateCopiesTests
{
    private static readonly string Canonical = Path.Combine("templates", "bbpkg", "scripts", "repo-conventions.cs");

    [Fact]
    public void Every_template_ships_the_canonical_repo_conventions_script()
    {
        string root = RepoRoot();
        string canonical = File.ReadAllText(Path.Combine(root, Canonical));

        string[] templates = [.. Directory.GetDirectories(Path.Combine(root, "templates"))
            .Where(directory => Directory.Exists(Path.Combine(directory, ".template.config")))];

        // bbpkg and at least one other: with bbpkg alone the comparison is the file against itself.
        Assert.True(templates.Length >= 2, "Expected bbpkg and at least one other template, found " + templates.Length);

        List<string> differing = [];

        foreach (string template in templates)
        {
            string copy = Path.Combine(template, "scripts", "repo-conventions.cs");
            string relative = Path.GetRelativePath(root, copy);

            if (!File.Exists(copy))
            {
                differing.Add(relative + " is missing");
            }
            else if (File.ReadAllText(copy) != canonical)
            {
                differing.Add(relative + " differs");
            }
        }

        Assert.True(
            differing.Count == 0,
            "Copy " + Canonical + " over each: " + string.Join("; ", differing));
    }

    /// <summary>
    /// Every template folder is shipped content in <c>.github/repository.json</c>. Otherwise this
    /// repository's own conventions check reads the template's placeholder markers as this
    /// repository's unfinished documents, and evaluates the template's projects as its own.
    /// </summary>
    [Fact]
    public void Every_template_is_declared_shipped_content()
    {
        string root = RepoRoot();
        System.Text.Json.Nodes.JsonNode config =
            System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(root, ".github", "repository.json")))!;
        HashSet<string> content =
        [
            .. config["content"]!.AsArray().Select(node => node!.GetValue<string>().TrimEnd('/')),
        ];

        string[] templates = [.. Directory.GetDirectories(Path.Combine(root, "templates"))
            .Where(directory => Directory.Exists(Path.Combine(directory, ".template.config")))
            .Select(directory => "templates/" + Path.GetFileName(directory))];

        Assert.NotEmpty(templates);
        string[] missing = [.. templates.Where(template => !content.Contains(template))];
        Assert.True(missing.Length == 0, "Add to \"content\" in .github/repository.json: " + string.Join(", ", missing));
    }

    /// <summary>
    /// The link reader is one rule in two scripts (plans/00012 decision 3): <c>assert-packages.cs</c>
    /// carries <c>repo-conventions.cs</c>'s <c>Links</c>, with its documentation, character for
    /// character. Each script is copied into a repository on its own, so neither can reference the other.
    /// </summary>
    [Fact]
    public void Assert_packages_carries_the_conventions_link_reader_unchanged()
    {
        string scripts = Path.Combine(RepoRoot(), "templates", "bbpkg", "scripts");

        string conventions = LinksClass(File.ReadAllText(Path.Combine(scripts, "repo-conventions.cs")));
        string packages = LinksClass(File.ReadAllText(Path.Combine(scripts, "assert-packages.cs")));

        Assert.True(conventions == packages, "Copy repo-conventions.cs's Links class, with its documentation, over assert-packages.cs's.");
    }

    // The Links class with the documentation above it, to the brace that closes it at the start of a line.
    private static string LinksClass(string source)
    {
        string[] lines = source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        int start = Array.IndexOf(lines, "static class Links");
        Assert.True(start >= 0, "No `static class Links` at the start of a line.");
        int first = start;
        while (first > 0 && lines[first - 1].StartsWith("///", StringComparison.Ordinal))
        {
            first--;
        }
        int end = Array.IndexOf(lines, "}", start);
        Assert.True(end > start, "`static class Links` never closes at the start of a line.");
        return string.Join('\n', lines[first..(end + 1)]);
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
