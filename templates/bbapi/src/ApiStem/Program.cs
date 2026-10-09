using Bennewitz.Ninja.ApiStem;
using Bennewitz.Ninja.AppServices.EntryPoint;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Console;

// The entry point every family app shares, from Bennewitz.Ninja.AppServices.EntryPoint: an unhandled
// exception is reported on stderr and exits 1, and --version, alone, prints the release version before
// the host is built. Everything else on the command line is configuration (--urls, --environment), so
// nothing is rejected. The host owns Ctrl+C and SIGTERM, and shuts down gracefully on either.
return await AppMain.RunHostAsync(typeof(Program).Assembly, args, RunAsync);

static async Task RunAsync(string[] args)
{
    // The slim builder leaves out what a native API does not use: IIS integration, the EventLog and
    // EventSource providers, and HTTPS configuration from appsettings. TLS belongs to what is in front.
    var builder = WebApplication.CreateSlimBuilder(args);

    // Every log line is a diagnostic, so all of them go to stderr: stdout stays free for whatever
    // runs the API to read.
    builder.Services.Configure<ConsoleLoggerOptions>(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

    // Every type serialised to or from JSON is in ApiJson, so no serialisation needs reflection.
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.TypeInfoResolverChain.Insert(0, ApiJson.Default));

    builder.Services.AddOpenApi();

    // Errors answer as RFC 9457 problem details, application/problem+json, and never carry the exception.
    builder.Services.AddProblemDetails();

    // Behind a reverse proxy, the scheme and client address arrive in X-Forwarded-* headers. Only a
    // proxy on this machine (loopback) is trusted by default; add a KnownNetworks entry for one elsewhere,
    // such as a container network, or every request looks like plain HTTP from the proxy.
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

    // Disposed when the API stops, which flushes the log.
    await using var app = builder.Build();

    app.UseForwardedHeaders();
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    // The contract, at /openapi/v1.json: what a client generator or a reviewer reads.
    app.MapOpenApi();

    // For a load balancer, an orchestrator or a container healthcheck: the process is up and serving.
    app.MapGet("/healthz", () => Results.Text("ok", "text/plain"))
        .ExcludeFromDescription();

    // Which build is live, as AutoVersioning stamped it: the release version the tag gave the build (its
    // PublicVersion; 1.0.0 on a local build), the build's own stamp, and the commit.
    // ⚠ Not AssemblyInformationalVersion: AutoVersioning sets that to "Built with ♥ <commit>".
    app.MapGet("/version", () => VersionInfo.Of(typeof(Program).Assembly))
        .WithName("GetVersion");

    GreetingEndpoints.Map(app.MapGroup("/api/greetings"));

    await app.RunAsync();
}

/// <summary>Public so the tests' <c>WebApplicationFactory&lt;Program&gt;</c> can start this API.</summary>
public partial class Program;
