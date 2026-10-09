using Avalonia;
using Bennewitz.Ninja.AppServices.AvaloniaUI;
using Bennewitz.Ninja.AppServices.EntryPoint;

namespace Bennewitz.Ninja.AppStem;

internal static class Program
{
    /// <summary>
    /// Starts the app through the entry point every family app shares: an unhandled exception is
    /// logged as fatal, shown in the native fatal-error dialog and exits 1; the log is flushed on every
    /// exit; and <c>--version</c>, alone, prints the release version. <c>--smoke</c> opens the main
    /// window, lets it render once and exits 0: the cheapest proof that a trimmed single-file build
    /// still starts, loads its theme and binds.
    /// </summary>
    [STAThread]
    public static int Main(string[] args) =>
        AppMain.RunDesktop(typeof(Program).Assembly, args, Run, AvaloniaDiagnostics.EntryPointOptions());

    // Logging is configured inside the run, after the entry point has hooked the process-wide
    // exception events, so a failure configuring it is still reported, on stderr and in the dialog.
    private static int Run(string[] args)
    {
        AvaloniaDiagnostics.ConfigureLogging(new AvaloniaDiagnosticsOptions
        {
            AppName = "AppStem",
            LogsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppStem", "logs"),
        });

        App.Smoke = args.Contains("--smoke", StringComparer.Ordinal);
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// The app builder, also used by the visual designer. LogToTrace is not called: Avalonia's own
    /// log events already reach Trace through the Serilog bridge ConfigureLogging installs, and
    /// calling it too would write each one twice.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont();
}
