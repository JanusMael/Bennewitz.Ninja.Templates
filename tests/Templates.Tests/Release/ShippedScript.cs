using Templates.Tests.RepoConventions;

namespace Templates.Tests.Release;

/// <summary>
/// A script <c>bbpkg</c> ships in <c>templates/bbpkg/scripts/</c>, built once per test run and launched
/// as a program, as <see cref="ConventionsScript"/> launches <c>repo-conventions.cs</c>.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ <b>Built from a copy outside this repository.</b> In place, it would compile under
/// <c>templates/bbpkg</c>'s own <c>Directory.Build.props</c>, which is template content holding
/// placeholders. A generated repository compiles it under its real one, which <c>verify-release</c>
/// covers; here it is compiled with warnings as errors, as that props file sets.
/// </para>
/// <para>
/// ⛔ <b>With <c>--no-incremental</c></b>, for the reason <see cref="ConventionsScript"/> gives.
/// </para>
/// </remarks>
internal sealed class ShippedScript(string fileName)
{
    private readonly Lazy<string> _built = new(() => Build(fileName), LazyThreadSafetyMode.ExecutionAndPublication);

    public static ShippedScript ReleaseTarget { get; } = new("release-target.cs");

    public static ShippedScript AssertPackages { get; } = new("assert-packages.cs");

    /// <summary>Runs the built script in <paramref name="workingDirectory"/>, as <c>dotnet run</c> would.</summary>
    public (int ExitCode, string Output) Run(string workingDirectory, params string[] arguments) =>
        ConventionsScript.Execute("dotnet", workingDirectory, [_built.Value, .. arguments]);

    private static string Build(string fileName)
    {
        string directory = Path.Combine(Path.GetTempPath(), "shipped-script-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string source = Path.Combine(directory, fileName);
        File.Copy(Path.Combine(ConventionsScript.RepoRoot(), "templates", "bbpkg", "scripts", fileName), source);

        string output = Path.Combine(directory, "out");
        (int exit, string log) = ConventionsScript.Execute("dotnet", directory,
            "build", source, "-c", "Release", "--no-incremental", "-p:TreatWarningsAsErrors=true", "-o", output);
        if (exit != 0)
        {
            throw new InvalidOperationException($"templates/bbpkg/scripts/{fileName} did not build:\n" + log);
        }
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        };
        return Path.Combine(output, Path.ChangeExtension(fileName, ".dll"));
    }
}
