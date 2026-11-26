using Bennewitz.Ninja.AppServices.Logging;
using Serilog;
using Serilog.Core;

namespace Bennewitz.Ninja.CliStem;

/// <summary>
/// The app's log: AppServices.Logging's rolling file, one file per 8 hours kept for 3 days, in the
/// user's local application data, where every family app keeps its log. Nothing is written to the
/// console, so stdout and stderr stay the tool's own: its results, and its usage and failure reports.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ <b>The file renders the message as <c>{Message:lj}</c></b>, AppServices.Logging's own template,
/// which writes a string as it is: a value the template quotes, <c>path="{Path}"</c>, prints quoted
/// once. Every line this app writes is in the family's form, <c>[Area.Kind] key=value</c>
/// ("Log lines", in the family's repository conventions).
/// </para>
/// <para>
/// ⚠ <b>Set up before the entry point runs</b>, so a failure here would escape the entry point's
/// report. A log that cannot be kept is off for the run instead, said once on stderr, and the tool
/// still runs.
/// </para>
/// </remarks>
internal static class AppLog
{
    /// <summary>Where the log is kept: <c>CliStem/logs</c> in the user's local application data.</summary>
    public static string LogsDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CliStem", "logs");

    public static Logger Create(string logsDirectory)
    {
        LoggerConfiguration configuration = new LoggerConfiguration().MinimumLevel.Information();

        // ⚠ With no local application data folder the path is relative, and the log would land in
        // whatever directory the tool was run from.
        if (!Path.IsPathRooted(logsDirectory))
        {
            Console.Error.WriteLine($"warning: no log this run: '{logsDirectory}' is not a full path.");
            return configuration.CreateLogger();
        }

        try
        {
            Directory.CreateDirectory(logsDirectory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"warning: no log this run: {exception.Message}");
            return configuration.CreateLogger();
        }

        return configuration.WriteTo.Sink(new BucketedRollingFileSink(logsDirectory)).CreateLogger();
    }
}
