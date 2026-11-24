using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Display;

namespace Templates.Tests.Logging;

/// <summary>
/// What a log line in the family's form prints as (plans/00014 decision 3 and step 1): free text
/// quoted by hand, an enum passed as its name, a boolean and a number bare, each rendered by a real
/// Serilog logger through the sinks the family's apps write to, which render <c>{Message:lj}</c>.
/// </summary>
/// <remarks>
/// <para>
/// ⭐ <b>These are measurements of Serilog, not of this repository's code.</b> The conventions'
/// <i>Log lines</i> section says what a template prints; these tests are where that sentence comes
/// from, and they fail when a Serilog release changes it.
/// </para>
/// <para>
/// ⓘ <b>The same template, five ways.</b> Serilog directly; Microsoft.Extensions.Logging bridged to
/// Serilog by Serilog.Extensions.Logging, as code written against <c>ILogger</c> runs in a family
/// app; Serilog's Trace sink, which <c>bbavalonia</c> writes to, given <c>{Message:lj}</c> and at its
/// own default; and Microsoft.Extensions.Logging's own formatter with no Serilog, as a library's own
/// tests may read it. The last two are the ones that differ.
/// </para>
/// </remarks>
public sealed class LogLineRenderingTests
{
    private const string Template =
        "[Logging.Probe] name=\"{Name}\" mode={Mode} level={Level} flag={Flag} count={Count} ratio={Ratio}";

    /// <summary>What the family's sinks print for <see cref="Template"/>, each value as <see cref="Values"/> passes it.</summary>
    private const string InTheFamilysSinks =
        "[Logging.Probe] name=\"a b\" mode=Fast level=\"Fast\" flag=true count=7 ratio=0.5";

    private enum Speed
    {
        Fast,
    }

    /// <summary>A name with a space, an enum's name as a string, the enum itself, a boolean, an int and a double.</summary>
    private static object?[] Values() => ["a b", nameof(Speed.Fast), Speed.Fast, true, 7, 0.5];

    [Fact]
    public void Through_Serilog_a_hand_quoted_string_is_quoted_once_an_enum_s_name_is_bare_and_the_enum_itself_is_quoted()
    {
        var sink = new MessageSink();
        using (Logger logger = new Serilog.LoggerConfiguration().WriteTo.Sink(sink).CreateLogger())
        {
            logger.Information(Template, Values());
        }

        Assert.Equal([InTheFamilysSinks], sink.Lines);
    }

    [Fact]
    public void Through_Microsoft_Extensions_Logging_bridged_to_Serilog_the_line_is_the_same()
    {
        var sink = new MessageSink();
        using (Logger serilog = new Serilog.LoggerConfiguration().WriteTo.Sink(sink).CreateLogger())
        using (var factory = new SerilogLoggerFactory(serilog))
        {
            factory.CreateLogger("Probe").LogInformation(Template, Values());
        }

        Assert.Equal([InTheFamilysSinks], sink.Lines);
    }

    [Fact]
    public void Through_Serilog_s_Trace_sink_given_Message_lj_the_line_is_the_same()
    {
        string written = WrittenToTrace(sinks => sinks.Trace(outputTemplate: "{Message:lj}"));

        Assert.Contains(InTheFamilysSinks, written, StringComparison.Ordinal);
    }

    /// <summary>
    /// ⛔ <b>Serilog.Sinks.Trace's default template renders <c>{Message}</c>, not <c>{Message:lj}</c></b>,
    /// so a sink left at it quotes every string itself: a hand-quoted value prints its quotes twice,
    /// and the enum's name is quoted while the enum is not. A family app's Trace sink names
    /// <c>{Message:lj}</c>.
    /// </summary>
    [Fact]
    public void Through_Serilog_s_Trace_sink_at_its_default_template_strings_are_quoted_twice()
    {
        string written = WrittenToTrace(sinks => sinks.Trace());

        Assert.Contains(
            "[Logging.Probe] name=\"\"a b\"\" mode=\"Fast\" level=Fast flag=True count=7 ratio=0.5",
            written,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Microsoft_Extensions_Logging_s_own_formatter_writes_the_enum_bare_and_the_boolean_capitalised()
    {
        var provider = new FormattingProvider();
        using (var factory = LoggerFactory.Create(builder => builder.AddProvider(provider)))
        {
            factory.CreateLogger("Probe").LogInformation(Template, Values());
        }

        Assert.Equal(
            ["[Logging.Probe] name=\"a b\" mode=Fast level=Fast flag=True count=7 ratio=0.5"],
            provider.Lines);
    }

    [Fact]
    public void Through_Serilog_an_enum_hole_with_l_loses_the_line()
    {
        var sink = new MessageSink();
        using (Logger logger = new Serilog.LoggerConfiguration().WriteTo.Sink(sink).CreateLogger())
        {
            logger.Information("[Logging.Probe] level={Level:l} count={Count}", Speed.Fast, 7);
        }

        Assert.Empty(sink.Lines);
    }

    /// <summary>What a logger whose only sink is Serilog's Trace sink, configured by <paramref name="trace"/>, writes for <see cref="Template"/>.</summary>
    private static string WrittenToTrace(Func<Serilog.Configuration.LoggerSinkConfiguration, LoggerConfiguration> trace)
    {
        var listener = new CapturingListener();
        Trace.Listeners.Add(listener);
        try
        {
            using Logger logger = trace(new LoggerConfiguration().WriteTo).CreateLogger();
            logger.Information(Template, Values());
        }
        finally
        {
            Trace.Listeners.Remove(listener);
        }

        return listener.Written;
    }

    /// <summary>Renders each event's message as the family's sinks do, <c>{Message:lj}</c>.</summary>
    private sealed class MessageSink : ILogEventSink
    {
        private static readonly MessageTemplateTextFormatter Formatter = new("{Message:lj}");
        private readonly List<string> _lines = [];

        public IReadOnlyList<string> Lines => _lines;

        public void Emit(LogEvent logEvent)
        {
            using var writer = new StringWriter();
            Formatter.Format(logEvent, writer);
            _lines.Add(writer.ToString());
        }
    }

    /// <summary>Collects what Serilog's Trace sink writes.</summary>
    private sealed class CapturingListener : TraceListener
    {
        private readonly StringWriter _written = new();

        public string Written => _written.ToString();

        public override void Write(string? message) => _written.Write(message);

        public override void WriteLine(string? message) => _written.WriteLine(message);
    }

    /// <summary>Microsoft.Extensions.Logging with no Serilog: each line as its own formatter writes it.</summary>
    private sealed class FormattingProvider : ILoggerProvider
    {
        private readonly List<string> _lines = [];

        public IReadOnlyList<string> Lines => _lines;

        public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new Formatting(_lines);

        public void Dispose()
        {
        }

        private sealed class Formatting(List<string> lines) : Microsoft.Extensions.Logging.ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                lines.Add(formatter(state, exception));
        }
    }
}
