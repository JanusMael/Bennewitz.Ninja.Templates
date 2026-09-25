using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Bennewitz.Ninja.AppServices.AvaloniaUI;
using Bennewitz.Ninja.AppStem.ViewModels;
using Bennewitz.Ninja.AppStem.Views;

namespace Bennewitz.Ninja.AppStem;

/// <summary>
/// The application. Wiring is by hand, with no container: one window and its view model.
/// </summary>
public partial class App : Application
{
    /// <summary>Set by <c>--smoke</c>: open the main window, then exit once it has opened.</summary>
    internal static bool Smoke { get; set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            AvaloniaDiagnostics.InstallAvaloniaHooks();

            MainWindow window = new() { DataContext = new MainWindowViewModel() };
            desktop.MainWindow = window;
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;

            if (Smoke)
            {
                // After the first layout pass, so a theme or binding that fails only when the window
                // is realised fails the smoke run rather than a user's first launch.
                window.Opened += (_, _) => Dispatcher.UIThread.Post(() => desktop.Shutdown(0), DispatcherPriority.Background);
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
