using System.Reflection;
using Avalonia.Headless;

namespace Bennewitz.Ninja.AppStem.Tests.Headless;

/// <summary>
/// Guards the premise the serial test run rests on: one headless session, and so one dispatcher,
/// per assembly.
/// </summary>
public sealed class HeadlessSessionTests
{
    [Fact]
    public void GetOrStartForAssembly_returns_the_same_session_every_time()
    {
        HeadlessUnitTestSession a = HeadlessUnitTestSession.GetOrStartForAssembly(Assembly.GetExecutingAssembly());
        HeadlessUnitTestSession b = HeadlessUnitTestSession.GetOrStartForAssembly(Assembly.GetExecutingAssembly());

        // Cached per assembly. If it were not, each fixture would run on a dispatcher of its own, and
        // running the tests one at a time would no longer protect a single dispatcher.
        Assert.Same(a, b);
    }
}
