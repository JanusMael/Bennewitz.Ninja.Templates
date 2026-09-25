using Avalonia;
using Avalonia.Headless;
using Bennewitz.Ninja.AppStem.Tests.Headless;

[assembly: AvaloniaTestApplication(typeof(HeadlessTestApp))]

// Tests in this assembly run one at a time: one headless session drives one dispatcher, which two
// tests must not drive at once. HeadlessSessionTests guards that premise.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Bennewitz.Ninja.AppStem.Tests.Headless;

/// <summary>
/// Builds the app's REAL <see cref="App"/> on the headless platform, so every test runs under the
/// theme and resources a user gets. <see cref="App.OnFrameworkInitializationCompleted"/> opens no
/// window here: the headless platform has no desktop lifetime.
/// </summary>
/// <remarks>
/// ⛔ With no <c>[AvaloniaTestIsolation]</c> on the assembly, isolation is per test: every
/// <c>Dispatch</c> builds the application afresh (Avalonia 12.1.3). So nothing a test does leaks into
/// the next, and a warm-up dispatch would prove nothing. Sharing one application would take
/// <c>AvaloniaTestIsolationLevel.PerAssembly</c>, which needs its own evidence first.
/// </remarks>
public static class HeadlessTestApp
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
}
