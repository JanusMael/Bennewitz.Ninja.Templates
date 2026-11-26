using Bennewitz.Ninja.AppServices.EntryPoint;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;

namespace Bennewitz.Ninja.CliStem;

/// <summary>
/// The entry point. What every family app's entry point must get right is
/// <see cref="AppMain"/>'s, from Bennewitz.Ninja.AppServices.EntryPoint, so a fix there reaches this
/// app with a package update:
/// <list type="bullet">
/// <item>exit 0 on success, 1 on an unhandled exception, with the report on stderr, 2 on a
/// <see cref="UsageException"/>, with the usage on stderr, and 130 after Ctrl+C;</item>
/// <item>Ctrl+C cancels the token the work is handed;</item>
/// <item><c>--version</c>, alone, prints the release version;</item>
/// <item>the log is flushed once, however the run ends.</item>
/// </list>
/// What is here is this app's own: its log, its usage and its work.
/// </summary>
internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // The log is set up before the entry point runs, so that everything from the first line of the
        // run to the last has somewhere to go; AppLog says where.
        Serilog.Log.Logger = AppLog.Create(AppLog.LogsDirectory);

        // The work logs through ILogger, as a library would, and Serilog's bridge renders it into the
        // same log. Left undisposed by the factory (dispose: false, the default): the entry point's
        // flush is the one that ends the log.
        using SerilogLoggerFactory loggers = new(Serilog.Log.Logger);
        ILogger logger = loggers.CreateLogger(typeof(LineCount).FullName!);

        return await AppMain.RunConsoleAsync(
            typeof(Program).Assembly,
            args,
            (arguments, cancellationToken) => LineCount.RunAsync(arguments, logger, cancellationToken),
            new AppMainOptions { Usage = LineCount.Usage, FlushLog = Serilog.Log.CloseAndFlush });
    }
}
