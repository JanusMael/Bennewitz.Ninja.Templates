using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Nodes;

namespace Templates.Tests.Release;

/// <summary>
/// A NuGet flat container served from this process over HTTP, which <c>release-target.cs</c> reads
/// its version floor from as it reads nuget.org's, so the tests never reach nuget.org.
/// </summary>
/// <remarks>
/// One listener for the test run; each <see cref="FakeFeed"/> is a path of its own beneath it, as the
/// tests run in parallel. An id is looked up by its exact path segment, so a script asking for
/// <c>Fixture.Widget</c> rather than <c>fixture.widget</c> finds nothing, as on nuget.org.
/// </remarks>
internal sealed class FakeFeed
{
    private static readonly ConcurrentDictionary<string, FakeFeed> Feeds = new(StringComparer.Ordinal);
    private static readonly Lazy<string> Root = new(Listen, LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly ConcurrentDictionary<string, string[]> versions = new(StringComparer.Ordinal);

    private FakeFeed(string url) => Url = url;

    /// <summary>The flat container's base URL, for <c>--feed</c>.</summary>
    public string Url { get; }

    /// <summary>A status every request is answered with instead of the versions, when set.</summary>
    public HttpStatusCode? Failure { get; set; }

    public static FakeFeed Create()
    {
        string name = Guid.NewGuid().ToString("N");
        FakeFeed feed = new(Root.Value + name + "/");
        Feeds[name] = feed;
        return feed;
    }

    /// <summary>Lists <paramref name="listed"/> for <paramref name="id"/>, under its lower-cased id as nuget.org does.</summary>
    public void List(string id, params string[] listed) => versions[id.ToLowerInvariant()] = listed;

    /// <summary>A URL on this machine that nothing listens on.</summary>
    public static string Unreachable() => $"http://localhost:{FreePort()}/";

    private static string Listen()
    {
        for (int attempt = 0; ; attempt++)
        {
            string prefix = $"http://localhost:{FreePort()}/";
            HttpListener listener = new();
            listener.Prefixes.Add(prefix);
            try
            {
                listener.Start();
            }
            catch (HttpListenerException) when (attempt < 10)
            {
                // Another process took the port between choosing it and listening on it.
                listener.Close();
                continue;
            }

            Thread serve = new(() =>
            {
                try
                {
                    Serve(listener);
                }
                catch (Exception error)
                {
                    // Every request after this one fails to connect, and its test says so.
                    Console.Error.WriteLine("FakeFeed stopped serving: " + error);
                }
            })
            { IsBackground = true, Name = "FakeFeed" };
            serve.Start();
            return prefix;
        }
    }

    private static int FreePort()
    {
        TcpListener probe = new(IPAddress.Loopback, 0);
        probe.Start();
        try
        {
            return ((IPEndPoint)probe.LocalEndpoint).Port;
        }
        finally
        {
            probe.Stop();
        }
    }

    private static void Serve(HttpListener listener)
    {
        while (true)
        {
            HttpListenerContext context;
            try
            {
                context = listener.GetContext();
            }
            catch (HttpListenerException)
            {
                return;
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            try
            {
                Answer(context);
            }
            catch (HttpListenerException)
            {
                // The script gave up on the request; the next one is answered.
            }
            catch (IOException)
            {
            }
        }
    }

    // GET /<feed>/<id>/index.json
    private static void Answer(HttpListenerContext context)
    {
        using HttpListenerResponse response = context.Response;
        string[] segments = context.Request.Url!.AbsolutePath.Trim('/').Split('/');
        if (segments.Length != 3 || segments[2] != "index.json" || !Feeds.TryGetValue(segments[0], out FakeFeed? feed))
        {
            response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }
        if (feed.Failure is { } failure)
        {
            response.StatusCode = (int)failure;
            return;
        }
        if (!feed.versions.TryGetValue(segments[1], out string[]? listed))
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            return;
        }

        byte[] body = Encoding.UTF8.GetBytes(new JsonObject { ["versions"] = new JsonArray([.. listed.Select(v => (JsonNode?)v)]) }.ToJsonString());
        response.ContentType = "application/json";
        response.ContentLength64 = body.Length;
        response.OutputStream.Write(body);
    }
}
