using Avalonia;
using Bennewitz.Ninja.AppServices.AvaloniaUI;
using Serilog;

namespace Bennewitz.Ninja.AppStem;

internal static class Program
{
    /// <summary>
    /// Starts the app. <c>--smoke</c> opens the main window, lets it render once and exits 0: the
    /// cheapest proof that a trimmed single-file build still starts, loads its theme and binds.
    /// </summary>
    [STAThread]
    public static int Main(string[] args)
    {
        // Before any UI: a failure during start-up must still reach the log and a dialog.
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            AvaloniaDiagnostics.ShowNativeFatalError(e.ExceptionObject.ToString() ?? "Unknown error");

        AvaloniaDiagnostics.ConfigureLogging(new AvaloniaDiagnosticsOptions
        {
            AppName = "AppStem",
            LogsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppStem", "logs"),
        });

        try
        {
            App.Smoke = args.Contains("--smoke", StringComparer.Ordinal);
            return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "AppStem stopped on an unhandled exception");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
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
