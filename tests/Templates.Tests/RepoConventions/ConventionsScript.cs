using System.Diagnostics;

namespace Templates.Tests.RepoConventions;

/// <summary>
/// <c>scripts/repo-conventions.cs</c>, built once per test run and launched as a program. Every
/// conventions test runs the script through this.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>Why not <c>dotnet run --file</c> each time.</b> Each such launch first asks whether the script
/// is up to date, about 0.5 s against 0.06 s for the built program (measured 2026-10-22). It also
/// forced every conventions class into one serial collection, because parallel builds of one file
/// contend for its output. Built once, the classes run in parallel.
/// </para>
/// <para>
/// ⛔ <b>Built with <c>--no-incremental</c>, into a folder of its own.</b> The file-based app cache
/// judges freshness by timestamp, so a script copied in with an older timestamp, as a canary does,
/// ran the PREVIOUS build and passed every test. A forced build reads the file as it is now.
/// </para>
/// <para>
/// ⓘ One test still runs the script as a person does, with <c>dotnet run --file</c>:
/// <c>RepoConventionsTests.The_script_runs_as_a_person_runs_it</c>.
/// </para>
/// </remarks>
internal static class ConventionsScript
{
    private static readonly Lazy<string> Built = new(Build, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Runs the built script in <paramref name="workingDirectory"/>, as <c>dotnet run --file</c> would.</summary>
    public static (int ExitCode, string Output) Run(string workingDirectory, params string[] arguments) =>
        Execute("dotnet", workingDirectory, [Built.Value, .. arguments]);

    /// <summary>The script's output, a trimmed line each, as a person reads it.</summary>
    /// <remarks>
    /// ⛔ Under GitHub Actions the script prefixes a failing finding with <c>::error::</c>, so CI's tests
    /// see <c>::error::FAIL …</c>. Stripped here, or an assertion that a line starts with <c>FAIL</c>
    /// matches nothing in CI, and one that no such line exists passes whatever the script found.
    /// </remarks>
    public static IEnumerable<string> Lines(string output) =>
        output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n')
            .Select(l => l.Trim())
            .Select(l => l.StartsWith(ErrorPrefix, StringComparison.Ordinal) ? l[ErrorPrefix.Length..] : l);

    private const string ErrorPrefix = "::error::";

    public static string RepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Bennewitz.Ninja.Templates.csproj")))
        {
            directory = directory.Parent;
        }
        Assert.NotNull(directory);
        return directory.FullName;
    }

    private static string Build()
    {
        string output = Path.Combine(Path.GetTempPath(), "repo-conventions-built-" + Guid.NewGuid().ToString("N"));
        string root = RepoRoot();
        (int exit, string log) = Execute("dotnet", root,
            "build", Path.Combine(root, "scripts", "repo-conventions.cs"), "-c", "Release", "--no-incremental", "-o", output);
        if (exit != 0)
        {
            throw new InvalidOperationException("scripts/repo-conventions.cs did not build:\n" + log);
        }
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                Directory.Delete(output, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        };
        return Path.Combine(output, "repo-conventions.dll");
    }

    public static (int ExitCode, string Output) Execute(string file, string workingDirectory, params string[] arguments)
    {
        ProcessStartInfo start = new(file) { WorkingDirectory = workingDirectory, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }
        using Process process = Process.Start(start)!;
        Task<string> stdout = process.StandardOutput.ReadToEndAsync();
        Task<string> stderr = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        return (process.ExitCode, stdout.Result + stderr.Result);
    }
}
