using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Bennewitz.Ninja.ApiStem;

/// <summary>One greeting, as the API answers it.</summary>
public sealed record Greeting(string Name, string Message);

/// <summary>
/// The API's one resource, as an example of the shape every endpoint takes: typed results, so the
/// OpenAPI document names each response, no reflection anywhere on the path, and one log line for what
/// the caller asked, in the family's form.
/// </summary>
public static partial class GreetingEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapGet("/{name}", Greet)
            .WithName("GetGreeting")
            .WithSummary("Greets someone by name.");
    }

    /// <summary>A name made only of whitespace, or longer than 100 characters, is a bad request.</summary>
    public static Results<Ok<Greeting>, ValidationProblem> Greet(string name, [FromServices] ILogger<Greeting> logger)
    {
        string trimmed = name.Trim();

        if (trimmed.Length is 0 or > 100)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["A name is 1 to 100 characters."],
            });
        }

        LogGreeted(logger, trimmed);
        return TypedResults.Ok(new Greeting(trimmed, $"Hello, {trimmed}."));
    }

    // [Area.Kind], then key=value pairs. The name is free text, so the template quotes it, and
    // Serilog's {Message:lj} writes it as it is, quoted once ("Log lines", in the family's repository
    // conventions).
    [LoggerMessage(Level = LogLevel.Information, Message = "[Greeting.Answer] name=\"{Name}\"")]
    private static partial void LogGreeted(ILogger logger, string name);
}
