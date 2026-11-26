using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Bennewitz.Ninja.ApiStem.Tests;

/// <summary>
/// The API's log, read where it goes: the built API is started as a process, so its two streams can
/// be seen, and asked for one greeting. <see cref="RunningApi"/> starts it once for the class.
/// </summary>
/// <remarks>
/// The log's text is the family's form ("Log lines", in the family's repository conventions): a tag
/// <c>[Area.Kind]</c>, then key=value pairs, free text quoted by the template.
/// </remarks>
public sealed partial class LoggingTests(LoggingTests.RunningApi api) : IClassFixture<LoggingTests.RunningApi>
{
    /// <summary>Every log line is a diagnostic: stdout stays free for whatever runs the API to read.</summary>
    [Fact]
    public void Nothing_is_written_to_stdout() => Assert.Equal(string.Empty, api.Stdout);

    /// <summary>
    /// The API's own line, logged through <c>ILogger</c>, reaches the console as written. The name is
    /// quoted once, by the template: a sink rendering <c>{Message}</c> instead of <c>{Message:lj}</c>
    /// would print <c>name=""Ada""</c>.
    /// </summary>
    [Fact]
    public void The_APIs_own_line_reaches_stderr_as_written() =>
        Assert.Contains("[Greeting.Answer] name=\"Ada\"" + Environment.NewLine, api.Stderr, StringComparison.Ordinal);

    /// <summary>
    /// <c>Microsoft.AspNetCore</c> logs at Warning, so a request writes nothing of the framework's own:
    /// at Information it writes a line as each request starts and as it finishes.
    /// </summary>
    [Fact]
    public void A_request_writes_no_line_of_ASP_NET_Cores() =>
        Assert.DoesNotContain("Request starting", api.Stderr, StringComparison.Ordinal);

    /// <summary><c>Microsoft.Hosting.Lifetime</c> stays at Information: the address is logged at start.</summary>
    [Fact]
    public void The_hosts_start_up_lines_are_logged() =>
        Assert.Contains("Now listening on: http://127.0.0.1:", api.Stderr, StringComparison.Ordinal);

    /// <summary>
    /// The built API, started on a free loopback port and asked for one greeting, then stopped; what
    /// it wrote to each stream is kept for the tests.
    /// </summary>
    /// <remarks>
    /// ⚠ Stopped by killing it: a console's Ctrl+C cannot be sent to a child process reliably on
    /// Windows. Every line the request logs is on stderr before its response is, so none is lost.
    /// </remarks>
    public sealed partial class RunningApi : IAsyncLifetime
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        public string Stdout { get; private set; } = string.Empty;

        public string Stderr { get; private set; } = string.Empty;

        public async ValueTask InitializeAsync()
        {
            string executable = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "ApiStem.exe" : "ApiStem");
            Assert.True(File.Exists(executable), $"The API was not built beside the tests: no {executable}.");

            ProcessStartInfo startInfo = new(executable)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = AppContext.BaseDirectory,
            };
            startInfo.ArgumentList.Add("--urls");
            startInfo.ArgumentList.Add("http://127.0.0.1:0");
            startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

            using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Could not start {executable}.");
            using CancellationTokenSource timeout = new(Timeout);
            Task<string> stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
            StringBuilder stderr = new();

            try
            {
                Uri address = await ReadAddressAsync(process.StandardError, stderr, timeout.Token);
                using HttpClient client = new() { BaseAddress = address };
                HttpResponseMessage response = await client.GetAsync(new Uri("/api/greetings/%20Ada%20", UriKind.Relative), timeout.Token);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            finally
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(CancellationToken.None);
                stderr.Append(await process.StandardError.ReadToEndAsync(CancellationToken.None));
                Stderr = stderr.ToString();
                Stdout = await stdout;
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        // The host logs the address it bound once it is listening; port 0 lets the system pick one.
        // The quote is optional so that a sink rendering {Message}, which quotes the address, still
        // starts, and fails the assertions about the text rather than this fixture.
        private static async Task<Uri> ReadAddressAsync(StreamReader reader, StringBuilder read, CancellationToken cancellationToken)
        {
            while (await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                read.AppendLine(line);
                Match match = ListeningOn().Match(line);

                if (match.Success)
                {
                    return new Uri(match.Groups[1].Value);
                }
            }

            throw new InvalidOperationException("The API exited before it was listening:" + Environment.NewLine + read);
        }

        [GeneratedRegex("Now listening on: \"?(http://[^\\s\"]+)")]
        private static partial Regex ListeningOn();
    }
}
