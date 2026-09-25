using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Bennewitz.Ninja.SiteStem.Tests;

/// <summary>
/// The interactive server component embedded in the home view, over HTTP: it prerenders into the
/// page, the page loads the script that starts its circuit, and the circuit's hub answers.
/// </summary>
/// <remarks>
/// A click needs a browser and a live circuit, which is past what a test host does; each piece a
/// click depends on is checked here instead.
/// </remarks>
public sealed class ComponentTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_counter_prerenders_into_the_home_page()
    {
        string html = await factory.CreateClient().GetStringAsync("/", Cancel);

        Assert.Contains("Clicked 0 times.", html, StringComparison.Ordinal);
        // The marker the circuit attaches to. Without it the component is static HTML and nothing
        // responds to a click.
        Assert.Contains("<!--Blazor:", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_page_loads_the_circuit_script_and_the_script_is_served()
    {
        HttpClient client = factory.CreateClient();
        string html = await client.GetStringAsync("/", Cancel);
        HttpResponseMessage script = await client.GetAsync("/_framework/blazor.server.js", Cancel);

        Assert.Contains("<script src=\"_framework/blazor.server.js\"></script>", html, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, script.StatusCode);
    }

    [Fact]
    public async Task The_circuit_hub_negotiates()
    {
        HttpResponseMessage response = await factory.CreateClient()
            .PostAsync("/_blazor/negotiate?negotiateVersion=1", content: null, Cancel);
        using JsonDocument body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Cancel));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.RootElement.TryGetProperty("connectionToken", out _), body.RootElement.ToString());
    }
}
