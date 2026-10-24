using System.Text.Json;
using System.Text.Json.Nodes;

namespace Davish.JustBdd;

/// <summary>One CTRF document and the test assembly it covers.</summary>
/// <param name="Source">Where the document came from, used in error messages.</param>
internal sealed record CtrfDocument(string Source, JsonObject Root)
{
    private JsonNode? JustBddEnvironment => Root["results"]?["environment"]?["extra"]?[Ctrf.ExtensionKey];

    /// <summary>Written by Davish.JustBdd, so it carries scenarios and features.</summary>
    public bool IsJustBdd => JustBddEnvironment is not null;

    /// <summary>
    /// The test assembly: Davish.JustBdd records it in <c>environment.extra</c>; xUnit's CTRF names the assembly file in
    /// <c>results.extra.suites[].filePath</c>.
    /// </summary>
    public string? AssemblyName => (string?)JustBddEnvironment?["assembly"]
        ?? (Root["results"]?["extra"]?["suites"] is JsonArray { Count: > 0 } suites && (string?)suites[0]?["filePath"] is { } file
            ? Path.GetFileNameWithoutExtension(file)
            : null);
}

internal sealed class CtrfFormatException(string message) : Exception(message);

/// <summary>
/// Reads CTRF documents. Checks only what the report needs, so documents from other producers (such as xUnit's
/// <c>--report-xunit-ctrf</c>) are accepted.
/// </summary>
internal static class CtrfReader
{
    public static CtrfDocument Read(string path) => Parse(File.ReadAllText(path), path);

    public static CtrfDocument Parse(string json, string source)
    {
        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch (JsonException ex) { throw Invalid(source, $"is not valid JSON: {ex.Message}"); }

        if (root is not JsonObject document) throw Invalid(source, "must be a JSON object");
        if ((document["reportFormat"] as JsonValue)?.ToString() != "CTRF") throw Invalid(source, "reportFormat must be \"CTRF\"");
        if (document["results"] is not JsonObject results) throw Invalid(source, "results is missing");
        if (results["tests"] is not JsonArray tests) throw Invalid(source, "results.tests is missing");

        for (var i = 0; i < tests.Count; i++)
        {
            if (tests[i] is not JsonObject test) throw Invalid(source, $"results.tests[{i}] must be an object");
            foreach (var field in new[] { "name", "status", "duration" })
                if (test[field] is not JsonValue) throw Invalid(source, $"results.tests[{i}].{field} is missing");
        }
        return new CtrfDocument(source, document);
    }

    private static CtrfFormatException Invalid(string source, string reason) => new($"{source}: {reason}");
}
