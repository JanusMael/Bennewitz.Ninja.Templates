using Bennewitz.Ninja.AppServices.EntryPoint;
using Microsoft.Extensions.Logging;

namespace Bennewitz.Ninja.CliStem;

/// <summary>
/// The example command, in the shape every command takes: it reads its arguments, throws
/// <see cref="UsageException"/> for a bad command line, writes results to stdout and what it did to
/// the log, honours the cancellation token, and lets any other failure escape for the entry point to
/// report.
/// </summary>
internal static partial class LineCount
{
    /// <summary>Printed on stderr after a usage error's message.</summary>
    public const string Usage = """
        Usage: CliStem <file>...
               CliStem -
               CliStem --version

        Prints the number of lines in each file, then its name, one file per line. '-' reads
        standard input.
        """;

    /// <summary>Counts the lines of each file named in <paramref name="args"/>.</summary>
    /// <returns>0. A failure is an exception, which the entry point turns into an exit code.</returns>
    public static async Task<int> RunAsync(string[] args, ILogger logger, CancellationToken cancellationToken)
    {
        if (args.Length == 0)
        {
            throw new UsageException("No file given.");
        }

        string? option = args.FirstOrDefault(argument => argument.StartsWith('-') && argument != "-");

        if (option is not null)
        {
            throw new UsageException($"Unknown option '{option}'.");
        }

        foreach (string path in args)
        {
            long lines = path == "-"
                ? await CountStandardInputAsync(cancellationToken)
                : await CountFileAsync(path, cancellationToken);

            Console.Out.WriteLine($"{lines}\t{path}");
            LogCounted(logger, path, lines);
        }

        return 0;
    }

    // [Area.Kind], then key=value pairs. The path is free text, so the template quotes it, and the
    // log's {Message:lj} writes it as it is, quoted once ("Log lines", in the family's repository
    // conventions).
    [LoggerMessage(Level = LogLevel.Information, Message = "[LineCount.File] path=\"{Path}\" lines={Lines}")]
    private static partial void LogCounted(ILogger logger, string path, long lines);

    // ⚠ A file that does not exist is not caught: FileNotFoundException escapes, the entry point
    // reports it on stderr and exits 1. Catching it here to print a friendlier line is this app's
    // choice to make; exiting 0 after it would not be.
    private static async Task<long> CountFileAsync(string path, CancellationToken cancellationToken)
    {
        using StreamReader reader = new(path);
        long count = 0;

        while (await reader.ReadLineAsync(cancellationToken) is not null)
        {
            count++;
        }

        return count;
    }

    // ⛔ Standard input is read on a thread of its own, and the wait for it is what observes the token.
    // Console.In is a synchronised reader whose ReadLineAsync runs SYNCHRONOUSLY, blocking the caller
    // inside the read, so nothing after it could see Ctrl+C until a line arrived: measured on Linux, the
    // app kept waiting 5 s after SIGINT. Abandoning the blocked read is safe, since the process exits.
    private static Task<long> CountStandardInputAsync(CancellationToken cancellationToken) =>
        Task.Run(
            () =>
            {
                long count = 0;

                while (Console.In.ReadLine() is not null)
                {
                    count++;
                }

                return count;
            },
            CancellationToken.None).WaitAsync(cancellationToken);
}
