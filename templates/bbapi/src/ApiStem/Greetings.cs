using Microsoft.AspNetCore.Http.HttpResults;

namespace Bennewitz.Ninja.ApiStem;

/// <summary>One greeting, as the API answers it.</summary>
public sealed record Greeting(string Name, string Message);

/// <summary>
/// The API's one resource, as an example of the shape every endpoint takes: typed results, so the
/// OpenAPI document names each response, and no reflection anywhere on the path.
/// </summary>
public static class GreetingEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{name}", Greet)
            .WithName("GetGreeting")
            .WithSummary("Greets someone by name.");
    }

    /// <summary>A name made only of whitespace, or longer than 100 characters, is a bad request.</summary>
    public static Results<Ok<Greeting>, ValidationProblem> Greet(string name)
    {
        string trimmed = name.Trim();

        if (trimmed.Length is 0 or > 100)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["A name is 1 to 100 characters."],
            });
        }

        return TypedResults.Ok(new Greeting(trimmed, $"Hello, {trimmed}."));
    }
}
