using System.Globalization;
using Serilog;
using Serilog.Events;

namespace Bennewitz.Ninja.ApiStem;

/// <summary>
/// The API's log: Serilog's console, with every line on stderr, so stdout stays free for whatever runs
/// the API to read. Every line <c>ILogger</c> writes, the framework's among them, reaches it through
/// Serilog's bridge, which <c>Program</c> plugs in.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>The levels are set here, in code, not in <c>appsettings.json</c>.</b> Serilog's bridge
/// replaces Microsoft.Extensions.Logging's logger factory, so a <c>Logging:LogLevel</c> section there
/// would be read by nothing. <c>Microsoft.AspNetCore</c> logs at Warning, so a request writes no line
/// of its own; <c>Microsoft.Hosting.Lifetime</c>'s start-up lines, the address among them, stay at
/// Information.
/// </para>
/// <para>
/// ⛔ <b>The message renders as <c>{Message:lj}</c></b>, which writes a string as it is: a value the
/// template quotes, <c>name="{Name}"</c>, prints quoted once. <c>{Message}</c> would quote it again.
/// Every line this API writes is in the family's form, <c>[Area.Kind] key=value</c>.
/// </para>
/// </remarks>
internal static class AppLog
{
    /// <summary>Every console line: the time, the level, and the message.</summary>
    public const string OutputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

    public static Serilog.Core.Logger Create() =>
        new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .WriteTo.Console(
                outputTemplate: OutputTemplate,
                formatProvider: CultureInfo.InvariantCulture,
                standardErrorFromLevel: LogEventLevel.Verbose)
            .CreateLogger();
}
