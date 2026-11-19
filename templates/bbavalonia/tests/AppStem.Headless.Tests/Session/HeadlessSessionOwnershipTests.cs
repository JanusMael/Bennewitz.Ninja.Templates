using System.Reflection;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Threading;

namespace Bennewitz.Ninja.AppStem.Headless.Tests.Session;

/// <summary>
/// Guards <see cref="HeadlessSessionOwnership"/>: the headless session's thread owns
/// <see cref="Dispatcher.UIThread"/>, and background reads of it cannot take that away.
/// </summary>
/// <remarks>
/// ⚠ <b>Both read from a thread that is NOT the session's, on purpose.</b> A read inside a dispatch,
/// or right after awaiting one, runs on the session's thread and cannot fail: see
/// <see cref="HeadlessSessionOwnership.UiThreadOwnerSeenFromAnotherThread"/>. Under <c>PerTest</c>
/// isolation both fail: each dispatch ends by clearing <c>UIThread</c>, so the probe's read creates
/// a dispatcher of its own, and the stress test fails most of its dispatches.
/// </remarks>
public sealed class HeadlessSessionOwnershipTests(HeadlessSessionOwnership ownership)
{
    private static HeadlessUnitTestSession Session =>
        HeadlessUnitTestSession.GetOrStartForAssembly(Assembly.GetExecutingAssembly());

    [Fact]
    public async Task The_session_thread_still_owns_the_UI_dispatcher_between_dispatches()
    {
        Thread dispatchedOn = await Session.Dispatch(() => Thread.CurrentThread, CancellationToken.None);

        Assert.Same(ownership.SessionThread, dispatchedOn);
        Thread owner = HeadlessSessionOwnership.UiThreadOwnerSeenFromAnotherThread();
        Assert.True(owner == dispatchedOn,
            $"Dispatcher.UIThread belongs to thread {owner.ManagedThreadId} ('{owner.Name}'), read "
            + $"from a probe thread, but the session dispatches on {dispatchedOn.ManagedThreadId}. "
            + "Under PerTest isolation each dispatch clears the UI dispatcher, and the next thread to "
            + "read it takes it. Check that [assembly: AvaloniaTestIsolation(PerAssembly)] is still set.");
    }

    [Fact]
    public async Task Background_reads_of_UIThread_cannot_take_the_dispatcher_from_the_session()
    {
        using CancellationTokenSource stop = new();
        Exception? readerFailure = null;
        Thread noise = new(() =>
        {
            // Caught and asserted on below: a throw left on this thread would end the test process.
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    _ = Dispatcher.UIThread.CheckAccess();
                }
            }
            catch (Exception ex)
            {
                readerFailure = ex;
            }
        })
        { IsBackground = true, Name = "UIThread reader" };
        noise.Start();

        int failed = 0;
        string? first = null;
        try
        {
            for (int i = 0; i < 200; i++)
            {
                try
                {
                    await Session.Dispatch(() => Application.Current is not null, CancellationToken.None);
                }
                catch (InvalidOperationException ex)
                {
                    failed++;
                    first ??= ex.Message;
                }
            }
        }
        finally
        {
            await stop.CancelAsync();
            noise.Join();
        }

        // A reader that died early read nothing, and 200 clean dispatches would then prove nothing.
        Assert.Null(readerFailure);
        Assert.True(failed == 0,
            $"{failed} of 200 dispatches failed while another thread read Dispatcher.UIThread; first: "
            + $"{first}. Under PerTest isolation the reader claims the UI thread during app set-up.");
    }
}
