using System.Reflection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
#if (Blazor)

// Interactive server components, embedded in the MVC views with the Component Tag Helper
// (<component render-mode="ServerPrerendered">, Views/Home/Index.cshtml), prerendered with the page
// and made interactive by the SignalR circuit _framework/blazor.server.js opens. The layout's
// <base href> is what the circuit resolves its hub against; without it, a component on a nested
// page asks for the hub at the wrong path.
builder.Services.AddServerSideBlazor();
#endif

// Behind a reverse proxy, the scheme and client address arrive in X-Forwarded-* headers. Only a
// proxy on this machine (loopback) is trusted by default; add a KnownNetworks entry for one elsewhere,
// such as a container network, or every request looks like plain HTTP from the proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    // The error page never shows the exception; the log has it.
    app.UseExceptionHandler("/error/500");
}

app.UseStatusCodePagesWithReExecute("/error/{0}");
app.UseRouting();

app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();
#if (Blazor)
app.MapBlazorHub();
#endif

// For a load balancer, an orchestrator or a container healthcheck: the process is up and serving.
app.MapGet("/healthz", () => Results.Text("ok", "text/plain"));

// Which build is live, as AutoVersioning stamped it: the release version the tag gave the build (its
// PublicVersion; 1.0.0 on a local build), the build's own stamp, and the commit.
// ⚠ Not AssemblyInformationalVersion: AutoVersioning sets that to "Built with ♥ <commit>".
app.MapGet("/version", () => Results.Json(VersionInfo.Of(typeof(Program).Assembly)));

app.Run();

/// <summary>What <c>/version</c> answers.</summary>
internal sealed record VersionInfo(string Version, string Build, string Commit)
{
    public static VersionInfo Of(Assembly assembly)
    {
        string Metadata(string key) =>
            assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == key)?.Value ?? "";

        return new VersionInfo(Metadata("PublicVersion"), assembly.GetName().Version?.ToString() ?? "", Metadata("CommitSha"));
    }
}

/// <summary>Public so the tests' <c>WebApplicationFactory&lt;Program&gt;</c> can start this app.</summary>
public partial class Program;
