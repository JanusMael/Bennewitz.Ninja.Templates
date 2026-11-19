using Avalonia.Headless;
using Avalonia.Threading;
using Bennewitz.Ninja.AppStem.Headless.Tests.Session;

[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerAssembly)]
[assembly: AssemblyFixture(typeof(HeadlessSessionOwnership))]

namespace Bennewitz.Ninja.AppStem.Headless.Tests.Session;

/// <summary>
/// Makes the headless session's thread the owner of <see cref="Dispatcher.UIThread"/> before any
/// test runs, and fails the whole assembly if something else got there first.
/// </summary>
/// <remarks>
/// <para>
/// ⛔ <b>Avalonia gives <see cref="Dispatcher.UIThread"/> to whichever thread reads it first</b>
/// (Avalonia 12.1.6: the <c>Dispatcher</c> constructor does <c>s_uiThread ??= this</c>). Under the
/// default <c>PerTest</c> isolation the session clears that field at the start of every dispatch and
/// rebuilds the app, so a background thread that reads <c>Dispatcher.UIThread</c> during the rebuild
/// takes the UI thread, and the test fails with "the calling thread cannot access this object". App
/// code makes that read from pool threads whenever it marshals back to the UI, and a late
/// continuation from an earlier test is enough.
/// </para>
/// <para>
/// ⭐ <b>Measured in ClaudeForge</b> with one background thread reading
/// <c>Dispatcher.UIThread.CheckAccess()</c> in a loop over 2,000 empty dispatches: <c>PerTest</c>
/// failed 1,815; <c>PerAssembly</c> with this warm-up failed none, on Windows, Ubuntu and macOS.
/// </para>
/// <para>
/// ⛔ <b><c>PerAssembly</c> WITHOUT this warm-up is worse, not better.</b> It never resets the
/// dispatcher, so if anything reads <c>Dispatcher.UIThread</c> before the first dispatch, the wrong
/// thread owns it for the rest of the process: 2,000 of 2,000 failed in the same measurement. This
/// fixture runs before every test in the assembly, so the session's thread reads it first.
/// </para>
/// <para>
/// ⚠ <b>The cost:</b> every test shares one <see cref="Avalonia.Application"/>, and Avalonia
/// disposes nothing between tests. A test that changes application state, such as merged
/// dictionaries, <c>RequestedThemeVariant</c> or open windows, restores it in a <c>finally</c>.
/// </para>
/// </remarks>
public sealed class HeadlessSessionOwnership : IAsyncLifetime
{
    /// <summary>The thread the headless session dispatches on.</summary>
    public Thread SessionThread { get; private set; } = null!;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        HeadlessUnitTestSession session =
            HeadlessUnitTestSession.GetOrStartForAssembly(typeof(HeadlessSessionOwnership).Assembly);

        SessionThread = await session.Dispatch(() => Thread.CurrentThread, CancellationToken.None);

        Thread owner = UiThreadOwnerSeenFromAnotherThread();
        if (owner != SessionThread)
        {
            throw new InvalidOperationException(
                $"Dispatcher.UIThread belongs to thread {owner.ManagedThreadId} ('{owner.Name}'), not to "
                + $"the headless session's thread {SessionThread.ManagedThreadId}. Either this assembly "
                + "lost [assembly: AvaloniaTestIsolation(PerAssembly)], so every dispatch resets the UI "
                + "dispatcher, or something read Dispatcher.UIThread before the session's first "
                + "dispatch, which under PerAssembly cannot be undone.");
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>
    /// The owner of <see cref="Dispatcher.UIThread"/>, read on a NEW thread that is neither the
    /// session's nor the caller's.
    /// </summary>
    /// <remarks>
    /// ⛔ <b>Never read it right after <c>await session.Dispatch(…)</c>.</b> The session completes its
    /// task on the session's thread, so the continuation runs there too, and a read placed after the
    /// await returns the session's own dispatcher whatever the isolation. Such a check is true by
    /// construction: in ClaudeForge, with that read, removing <c>PerAssembly</c> left it green.
    /// </remarks>
    public static Thread UiThreadOwnerSeenFromAnotherThread()
    {
        Thread? owner = null;
        Exception? failure = null;
        Thread reader = new(() =>
        {
            // Caught here and thrown again on the caller's thread: a throw left on this thread would
            // end the test process instead of failing the test.
            try
            {
                owner = Dispatcher.UIThread.Thread;
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        })
        { IsBackground = true, Name = "UIThread owner probe" };
        reader.Start();
        reader.Join();

        return failure is null
            ? owner!
            : throw new InvalidOperationException("Reading Dispatcher.UIThread on a probe thread failed.", failure);
    }
}
