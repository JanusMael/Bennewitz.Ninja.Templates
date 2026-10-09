using Bennewitz.Ninja.AppServices.EntryPoint;

namespace Bennewitz.Ninja.CliStem;

/// <summary>
/// The entry point. What every family app's entry point must get right is
/// <see cref="AppMain"/>'s, from Bennewitz.Ninja.AppServices.EntryPoint, so a fix there reaches this
/// app with a package update:
/// <list type="bullet">
/// <item>exit 0 on success, 1 on an unhandled exception, with the report on stderr, 2 on a
/// <see cref="UsageException"/>, with the usage on stderr, and 130 after Ctrl+C;</item>
/// <item>Ctrl+C cancels the token the work is handed;</item>
/// <item><c>--version</c>, alone, prints the release version.</item>
/// </list>
/// What is here is this app's own: its usage and its work.
/// </summary>
internal static class Program
{
    public static Task<int> Main(string[] args) =>
        AppMain.RunConsoleAsync(
            typeof(Program).Assembly,
            args,
            LineCount.RunAsync,
            new AppMainOptions { Usage = LineCount.Usage });
}
