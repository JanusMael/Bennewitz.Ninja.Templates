using Serilog.Core;
using Serilog.Extensions.Logging;

namespace Bennewitz.Ninja.CliStem.Tests;

/// <summary>
/// The app's log, read where it goes: the rolling file <see cref="AppLog"/> sets up, here in a
/// directory of the test's own. That the log writes nothing to the console is
/// <see cref="CommandLineTests"/>': a successful run's stdout is its results and its stderr is empty.
/// </summary>
/// <remarks>
/// The log's text is the family's form ("Log lines", in the family's repository conventions): a tag
/// <c>[Area.Kind]</c>, then key=value pairs, free text quoted by the template.
/// </remarks>
public sealed class LoggingTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    /// <summary>
    /// The command's line, logged through <c>ILogger</c>, reaches the file as written. The path is
    /// quoted once, by the template: a sink rendering <c>{Message}</c> instead of <c>{Message:lj}</c>
    /// would print <c>path=""…""</c>.
    /// </summary>
    [Fact]
    public async Task A_line_logged_through_ILogger_reaches_the_file_as_written()
    {
        string logs = Path.Combine(Path.GetTempPath(), $"clistem-logs-{Guid.NewGuid():n}");
        string file = Path.Combine(Path.GetTempPath(), $"clistem-{Guid.NewGuid():n}.txt");
        await File.WriteAllTextAsync(file, "one\ntwo\n", Cancel);

        try
        {
            using (Logger log = AppLog.Create(logs))
            using (SerilogLoggerFactory loggers = new(log))
            {
                await LineCount.RunAsync([file], loggers.CreateLogger(typeof(LineCount).FullName!), Cancel);
            }

            string written = await File.ReadAllTextAsync(Assert.Single(Directory.GetFiles(logs)), Cancel);

            Assert.Contains($"[LineCount.File] path=\"{file}\" lines=2" + Environment.NewLine, written, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(file);

            if (Directory.Exists(logs))
            {
                Directory.Delete(logs, recursive: true);
            }
        }
    }
}
