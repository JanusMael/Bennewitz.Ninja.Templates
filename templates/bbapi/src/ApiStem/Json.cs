using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Bennewitz.Ninja.ApiStem;

/// <summary>What <c>/version</c> answers.</summary>
public sealed record VersionInfo(string Version, string Build, string Commit)
{
    public static VersionInfo Of(Assembly assembly)
    {
        string Metadata(string key) =>
            assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == key)?.Value ?? "";

        return new VersionInfo(Metadata("PublicVersion"), assembly.GetName().Version?.ToString() ?? "", Metadata("CommitSha"));
    }
}

/// <summary>
/// Every type the API reads or writes as JSON, serialised by generated code.
/// </summary>
/// <remarks>
/// ⛔ A type an endpoint reads or writes and missing here is a 500 from the native binary, and the
/// build does not warn: measured, a Greeting left out built clean with warnings as errors. The tests
/// catch it, because their project turns serialisation by reflection off as the native binary does;
/// CI's container job, which requests the native API, catches it too. Add each type with its endpoint.
/// </remarks>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(Greeting))]
[JsonSerializable(typeof(VersionInfo))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal sealed partial class ApiJson : JsonSerializerContext;
