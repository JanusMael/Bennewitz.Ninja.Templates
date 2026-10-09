using System.Diagnostics;
using System.Reflection;

namespace Bennewitz.Ninja.CliStem.Tests;

/// <summary>
/// The app's contract with whatever calls it, a person or a script: its exit codes and what it
/// writes to each stream. Every test runs the built executable as a child process, because an exit
/// code and the two streams can only be seen from outside the process.
/// </summary>
/// <remarks>
/// <list type="table">
/// <item><term>0</term><description>success, results on stdout, nothing on stderr</description></item>
/// <item><term>1</term><description>an unhandled failure, reported on stderr</description></item>
/// <item><term>2</term><description>a usage error, with the usage on stderr</description></item>
/// <item><term>130</term><description>cancelled by Ctrl+C</description></item>
/// </list>
/// </remarks>
public sealed class CommandLineTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Counting_a_file_prints_its_lines_and_exits_0()
    {
        string file = Path.Combine(Path.GetTempPath(), $"clistem-{Guid.NewGuid():n}.txt");
        await File.WriteAllTextAsync(file, "one\ntwo\nthree\n", TestContext.Current.CancellationToken);

        try
        {
            Run run = await RunAsync([file]);

            Assert.Equal(0, run.ExitCode);
            Assert.Equal($"3\t{file}", run.Stdout.TrimEnd());
            Assert.Equal(string.Empty, run.Stderr);
        }
        finally
        {
            File.Delete(file);
        }
    }

    /// <summary>
    /// ⛔ The release version, never AutoVersioning's AssemblyInformationalVersion, which reads
    /// "Built with ♥ &lt;commit&gt;". The expected value is read from the app's own assembly.
    /// </summary>
    [Fact]
    public async Task The_version_flag_prints_the_release_version_and_exits_0()
    {
        string expected = typeof(LineCount).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "PublicVersion")
            .Value!;

        Run run = await RunAsync(["--version"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Equal(expected, run.Stdout.Trim());
        Assert.DoesNotContain("Built with", run.Stdout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unknown_option_is_a_usage_error_and_exits_2()
    {
        Run run = await RunAsync(["--no-such-option"]);

        Assert.Equal(2, run.ExitCode);
        Assert.Contains("Unknown option '--no-such-option'", run.Stderr, StringComparison.Ordinal);
        Assert.Contains("Usage:", run.Stderr, StringComparison.Ordinal);
        Assert.Equal(string.Empty, run.Stdout);
    }

    [Fact]
    public async Task No_file_is_a_usage_error_and_exits_2()
    {
        Run run = await RunAsync([]);

        Assert.Equal(2, run.ExitCode);
        Assert.Contains("Usage:", run.Stderr, StringComparison.Ordinal);
    }

    /// <summary>
    /// A failure the command does not handle, here a file that does not exist, is reported on stderr
    /// and exits 1, so a calling script can tell it from success and from a bad invocation.
    /// </summary>
    [Fact]
    public async Task A_failure_is_reported_on_stderr_and_exits_1()
    {
        string missing = Path.Combine(Path.GetTempPath(), $"clistem-{Guid.NewGuid():n}-missing.txt");

        Run run = await RunAsync([missing]);

        Assert.Equal(1, run.ExitCode);
        Assert.Contains("fatal:", run.Stderr, StringComparison.Ordinal);
        Assert.Contains(nameof(FileNotFoundException), run.Stderr, StringComparison.Ordinal);
        Assert.Equal(string.Empty, run.Stdout);
    }

    /// <summary>
    /// Ctrl+C is SIGINT, sent while the app waits on standard input. The first file's line on stdout
    /// is the sign the work has started, so the signal cannot arrive before the entry point is ready.
    /// </summary>
    /// <remarks>
    /// ⚠ Linux and macOS only. On Windows a console Ctrl+C cannot be sent to a child process
    /// reliably, so there it is not verified.
    /// </remarks>
    [Fact]
    public async Task Ctrl_C_cancels_the_work_and_exits_130()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "A console Ctrl+C cannot be sent to a child process reliably on Windows.");

        string file = Path.Combine(Path.GetTempPath(), $"clistem-{Guid.NewGuid():n}.txt");
        await File.WriteAllTextAsync(file, "one\n", TestContext.Current.CancellationToken);

        try
        {
            using Process process = Start([file, "-"], redirectStdin: true);
            using CancellationTokenSource timeout = new(Timeout);

            string? first = await process.StandardOutput.ReadLineAsync(timeout.Token);
            Assert.Equal($"1\t{file}", first);

            // Standard input stays open, so the app is waiting on it when the signal arrives.
            using (Process kill = Process.Start("kill", ["-INT", process.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)]))
            {
                await kill.WaitForExitAsync(timeout.Token);
                Assert.Equal(0, kill.ExitCode);
            }

            await process.WaitForExitAsync(timeout.Token);

            Assert.Equal(130, process.ExitCode);
        }
        finally
        {
            File.Delete(file);
        }
    }

    private static async Task<Run> RunAsync(string[] arguments)
    {
        using Process process = Start(arguments, redirectStdin: false);
        using CancellationTokenSource timeout = new(Timeout);

        Task<string> stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
        Task<string> stderr = process.StandardError.ReadToEndAsync(timeout.Token);

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return new Run(process.ExitCode, await stdout, await stderr);
    }

    /// <summary>
    /// The app's own executable, built beside these tests by the project reference: the apphost a
    /// person runs, not <c>dotnet CliStem.dll</c>.
    /// </summary>
    private static Process Start(string[] arguments, bool redirectStdin)
    {
        string executable = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "CliStem.exe" : "CliStem");
        Assert.True(File.Exists(executable), $"The app was not built beside the tests: no {executable}.");

        ProcessStartInfo startInfo = new(executable)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = redirectStdin,
            UseShellExecute = false,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {executable}.");
    }

    private sealed record Run(int ExitCode, string Stdout, string Stderr);
}
