using Avalonia;
using Avalonia.Headless;
using Bennewitz.Ninja.AppStem.Headless.Tests.Session;

[assembly: AvaloniaTestApplication(typeof(HeadlessTestApp))]

// Tests in this assembly run one at a time: one headless session drives one dispatcher and, under
// PerAssembly isolation, one Application for the whole process. Two tests running at once would
// interleave on both. HeadlessSessionTests guards the one-session premise.
[assembly: Xunit.v3.Parallelization(Mode = Xunit.Sdk.ParallelMode.None)]

namespace Bennewitz.Ninja.AppStem.Headless.Tests.Session;

/// <summary>
/// Builds the app's REAL <see cref="App"/> on the headless platform, so every test runs under the
/// theme and resources a user gets. <see cref="App.OnFrameworkInitializationCompleted"/> opens no
/// window here: the headless platform has no desktop lifetime.
/// </summary>
/// <remarks>
/// Built once for the whole process, because the assembly is isolated per assembly: see
/// <see cref="HeadlessSessionOwnership"/>.
/// </remarks>
public static class HeadlessTestApp
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
}
