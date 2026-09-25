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
