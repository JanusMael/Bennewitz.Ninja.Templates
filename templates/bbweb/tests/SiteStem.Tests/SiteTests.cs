using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bennewitz.Ninja.SiteStem.Tests;

/// <summary>
/// The site over HTTP, started in memory by <see cref="WebApplicationFactory{TEntryPoint}"/>: the
/// real <c>Program</c>, its routes, views and static assets.
/// </summary>
public sealed class SiteTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_home_page_renders_through_the_layout()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/", Cancel);
        string html = await response.Content.ReadAsStringAsync(Cancel);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<main id=\"main\">", html, StringComparison.Ordinal);
        Assert.Contains("<title>Home — SiteStem</title>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Healthz_answers_ok()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/healthz", Cancel);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", await response.Content.ReadAsStringAsync(Cancel));
    }

    /// <summary>
    /// <c>/version</c> answers with a version and a build stamp, each shaped like one. Shape, because
    /// the obvious attribute, <c>AssemblyInformationalVersion</c>, is AutoVersioning's
    /// <c>"Built with ♥ …"</c>: a check for "not empty" passed on it.
    /// </summary>
    [Fact]
    public async Task Version_names_the_release_and_the_build()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/version", Cancel);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancel));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Matches(@"^\d+\.\d+\.\d+", body.RootElement.GetProperty("version").GetString());
        Assert.Matches(@"^\d+\.\d+\.\d+\.\d+$", body.RootElement.GetProperty("build").GetString());
        Assert.True(body.RootElement.TryGetProperty("commit", out _), body.RootElement.ToString());
    }

    [Fact]
    public async Task The_stylesheet_is_served()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/css/site.css", Cancel);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task An_unknown_address_renders_the_not_found_page()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/no/such/page", Cancel);
        string html = await response.Content.ReadAsStringAsync(Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("<h1>Not found</h1>", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// In Production an unhandled exception renders the error page, with its status, and never the
    /// exception: a stack trace in a response tells an attacker what the site runs.
    /// </summary>
    [Fact]
    public async Task An_exception_in_production_renders_the_error_page_without_the_exception()
    {
        using WebApplicationFactory<Program> production = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Production);
            builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, ThrowingRoute>());
        });

        HttpResponseMessage response = await production.CreateClient().GetAsync("/tests/throw", Cancel);
        string html = await response.Content.ReadAsStringAsync(Cancel);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("<h1>Something went wrong</h1>", html, StringComparison.Ordinal);
        Assert.DoesNotContain(ThrowingRoute.Message, html, StringComparison.Ordinal);
    }

    /// <summary>
    /// Throws for <c>/tests/throw</c>, from a middleware placed after the site's own pipeline: no
    /// endpoint matches the path, so the request falls through to it, and the exception travels back
    /// through the site's exception handler exactly as one from a controller would.
    /// </summary>
    private sealed class ThrowingRoute : IStartupFilter
    {
        public const string Message = "a secret the error page must not show";

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Use(async (HttpContext context, RequestDelegate following) =>
            {
                if (context.Request.Path == "/tests/throw")
                {
                    throw new InvalidOperationException(Message);
                }

                await following(context);
            });
        };
    }
}
