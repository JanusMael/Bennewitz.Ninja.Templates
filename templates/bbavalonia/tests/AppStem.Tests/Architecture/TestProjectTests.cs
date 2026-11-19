using System.Xml.Linq;
using Bennewitz.Ninja.AppStem.Tests.Support;

namespace Bennewitz.Ninja.AppStem.Tests.Architecture;

/// <summary>
/// Headless tests live in one project, isolated per assembly. See tests/AGENTS.md.
/// </summary>
/// <remarks>
/// ⛔ Under the default per-test isolation, a background thread that reads
/// <c>Dispatcher.UIThread</c> while a dispatch rebuilds the app takes the UI thread, and the test
/// fails at random. <c>AppStem.Headless.Tests</c> closes that, and its fixture fails every test in it
/// if the isolation is lost. A headless test in any other project would bring the race back
/// unguarded, so no other project may reference <c>Avalonia.Headless</c>.
/// </remarks>
public sealed class TestProjectTests
{
    private const string HeadlessProject = "AppStem.Headless.Tests.csproj";

    [Fact]
    public void Only_the_headless_test_project_references_Avalonia_Headless()
    {
        // One level down only: each test project is a folder of tests/, and bin/ and obj/ are never walked.
        string[] projects =
        [
            .. Directory.EnumerateDirectories(Path.Combine(RepoPaths.Root, "tests"))
                .SelectMany(folder => Directory.EnumerateFiles(folder, "*.csproj")),
        ];
        Assert.Contains(projects, project => Path.GetFileName(project) == HeadlessProject);

        string[] referencing =
        [
            .. projects
                .Where(project => XDocument.Load(project).Descendants("PackageReference")
                    .Any(reference => (string?)reference.Attribute("Include") == "Avalonia.Headless"))
                .Select(Path.GetFileName)
                .Order()
                .OfType<string>(),
        ];

        Assert.Equal([HeadlessProject], referencing);
    }
}
