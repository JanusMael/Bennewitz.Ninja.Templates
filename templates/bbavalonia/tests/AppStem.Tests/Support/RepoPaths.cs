namespace Bennewitz.Ninja.AppStem.Tests.Support;

/// <summary>Paths inside this repository, found from the test's own location.</summary>
internal static class RepoPaths
{
    /// <summary>
    /// The directory holding the solution, walked up to from the test assembly, so no test depends
    /// on the working directory a runner happens to choose.
    /// </summary>
    public static string Root { get; } = FindRoot();

    /// <summary>The app project's directory, whose markup the XamlQuality rules read.</summary>
    public static string App => Path.Combine(Root, "src", "AppStem");

    private static string FindRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null && !Directory.EnumerateFiles(directory.FullName, "*.slnx").Any())
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException($"No *.slnx above {AppContext.BaseDirectory}.");
    }
}
