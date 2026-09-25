using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bennewitz.Ninja.ApiStem.Tests;

/// <summary>
/// The API over HTTP, started in memory by <see cref="WebApplicationFactory{TEntryPoint}"/>: the real
/// <c>Program</c> and its endpoints, run by the JIT.
/// </summary>
/// <remarks>
/// ⚠ These tests do not run the native binary. Their project turns JSON serialisation by reflection
/// off, as the native binary has it, so a type missing from <c>ApiJson</c> fails here. What else only
/// native AOT breaks, CI's <c>container</c> job catches by requesting the native API.
/// </remarks>
public sealed class ApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

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
    /// <c>"Built with ♥ …"</c>.
    /// </summary>
    [Fact]
    public async Task Version_names_the_release_and_the_build()
    {
        using JsonDocument body = await GetJson("/version", HttpStatusCode.OK);

        Assert.Matches(@"^\d+\.\d+\.\d+", body.RootElement.GetProperty("version").GetString());
        Assert.Matches(@"^\d+\.\d+\.\d+\.\d+$", body.RootElement.GetProperty("build").GetString());
        Assert.True(body.RootElement.TryGetProperty("commit", out _), body.RootElement.ToString());
    }

    [Fact]
    public async Task A_greeting_answers_with_the_trimmed_name()
    {
        using JsonDocument body = await GetJson("/api/greetings/%20Ada%20", HttpStatusCode.OK);

        Assert.Equal("Ada", body.RootElement.GetProperty("name").GetString());
        Assert.Equal("Hello, Ada.", body.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task A_name_too_long_is_a_validation_problem()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/api/greetings/" + new string('a', 101), Cancel);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancel));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("name", out _), body.RootElement.ToString());
    }

    /// <summary>The OpenAPI document describes the API's endpoints, and leaves out /healthz.</summary>
    [Fact]
    public async Task The_OpenAPI_document_describes_the_endpoints()
    {
        using JsonDocument body = await GetJson("/openapi/v1.json", HttpStatusCode.OK);
        JsonElement paths = body.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/greetings/{name}", out _), paths.ToString());
        Assert.True(paths.TryGetProperty("/version", out _), paths.ToString());
        Assert.False(paths.TryGetProperty("/healthz", out _), paths.ToString());
    }

    [Fact]
    public async Task An_unknown_address_is_a_problem_not_found()
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync("/no/such/thing", Cancel);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// In Production an unhandled exception answers 500 as problem details, and never carries the
    /// exception: a stack trace in a response tells an attacker what the API runs.
    /// </summary>
    [Fact]
    public async Task An_exception_in_production_is_a_problem_without_the_exception()
    {
        using WebApplicationFactory<Program> production = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Production);
            builder.ConfigureServices(services => services.AddSingleton<IStartupFilter, ThrowingRoute>());
        });

        HttpResponseMessage response = await production.CreateClient().GetAsync("/tests/throw", Cancel);
        string body = await response.Content.ReadAsStringAsync(Cancel);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain(ThrowingRoute.Message, body, StringComparison.Ordinal);
    }

    private async Task<JsonDocument> GetJson(string path, HttpStatusCode expected)
    {
        HttpResponseMessage response = await factory.CreateClient().GetAsync(path, Cancel);
        string text = await response.Content.ReadAsStringAsync(Cancel);

        Assert.True(response.StatusCode == expected, $"{path} answered {(int)response.StatusCode}: {text}");
        return JsonDocument.Parse(text);
    }

    /// <summary>
    /// Throws for <c>/tests/throw</c>, from a middleware placed after the API's own pipeline: no
    /// endpoint matches the path, so the request falls through to it, and the exception travels back
    /// through the API's exception handler exactly as one from an endpoint would.
    /// </summary>
    private sealed class ThrowingRoute : IStartupFilter
    {
        public const string Message = "a secret the error response must not carry";

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
