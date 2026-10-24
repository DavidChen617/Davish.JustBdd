using System.Globalization;
using System.Text.Json.Nodes;

namespace Davish.JustBdd;

/// <summary>Converts CTRF documents into the data the HTML template renders.</summary>
internal static class ReportView
{
    public static JsonObject FromCtrf(JsonObject ctrf) => FromCtrf([new CtrfDocument("", ctrf)]);

    /// <summary>
    /// Merges documents in order: tests are concatenated, the first feature of a name wins, and the run spans the earliest
    /// start and the latest stop. xUnit's own CTRF for an assembly that also has a Davish.JustBdd CTRF is skipped,
    /// because the Davish.JustBdd document holds the same tests with their scenarios.
    /// </summary>
    public static JsonObject FromCtrf(IReadOnlyList<CtrfDocument> documents)
    {
        var covered = new HashSet<string>(documents.Where(d => d.IsJustBdd && d.AssemblyName is not null).Select(d => d.AssemblyName!));
        var included = documents.Where(d => d.IsJustBdd || d.AssemblyName is null || !covered.Contains(d.AssemblyName)).ToArray();

        var rows = new JsonArray();
        var features = new JsonObject();
        var projectFeatures = new JsonObject();
        var outcomes = new List<string>();
        var totalDuration = 0d;
        long? start = null, stop = null;
        foreach (var document in included)
        {
            var results = document.Root["results"]!.AsObject();
            var assembly = document.AssemblyName;
            foreach (var node in results["tests"]!.AsArray())
            {
                var row = Row(node!.AsObject(), rows.Count, assembly);
                outcomes.Add((string)row["outcome"]!);
                totalDuration += (double)row["durationMs"]!;
                rows.Add(row);
            }

            if (results["extra"]?[Ctrf.ExtensionKey]?["features"] is JsonObject documentFeatures)
            {
                foreach (var feature in documentFeatures)
                    if (!features.ContainsKey(feature.Key)) features[feature.Key] = feature.Value?.DeepClone();
                // Projects can define a feature of the same name differently; Project › Feature shows each project's own.
                if (assembly is not null && !projectFeatures.ContainsKey(assembly)) projectFeatures[assembly] = documentFeatures.DeepClone();
            }

            if (results["summary"]?["start"] is { } s) start = Math.Min(start ?? long.MaxValue, (long)Number(s));
            if (results["summary"]?["stop"] is { } e) stop = Math.Max(stop ?? long.MinValue, (long)Number(e));
        }

        var frameworks = included.Select(d => (string?)d.Root["results"]!["tool"]?["name"]).Where(n => n is not null).Distinct().ToArray();
        var projects = included.Select(d => d.AssemblyName).Where(a => a is not null).Distinct().Count();
        var view = new JsonObject
        {
            ["testApplication"] = HtmlWriter.Title,
            ["framework"] = frameworks.Length == 0 ? null : string.Join(", ", frameworks),
            ["startTime"] = Time(start),
            ["endTime"] = Time(stop),
            ["tests"] = rows,
            ["features"] = features,
            ["summary"] = new JsonObject
            {
                ["total"] = outcomes.Count,
                ["passed"] = outcomes.Count(o => o == "passed"),
                ["failed"] = outcomes.Count(o => o == "failed"),
                ["errored"] = outcomes.Count(o => o == "errored"),
                ["timedOut"] = outcomes.Count(o => o == "timedOut"),
                ["skipped"] = outcomes.Count(o => o == "skipped"),
                ["totalDurationMs"] = totalDuration,
            },
        };
        if (projects > 1) view["projectFeatures"] = projectFeatures;
        return view;
    }

    private static JsonObject Row(JsonObject test, int rowKey, string? assembly)
    {
        var extra = test["extra"]?[Ctrf.ExtensionKey];
        var name = (string)test["name"]!;
        var row = new JsonObject
        {
            ["rowKey"] = rowKey,
            ["uid"] = name,
            ["displayName"] = name,
            ["outcome"] = Outcome(test),
            ["durationMs"] = Number(test["duration"]),
            // xUnit's CTRF names the type and method in its un-namespaced extra.
            ["className"] = (string?)extra?["className"] ?? ShortName((string?)test["extra"]?["type"]) ?? Last(test["suite"]),
            ["methodName"] = (string?)extra?["methodName"] ?? (string?)test["extra"]?["method"],
            ["scenarios"] = extra?["scenarios"]?.DeepClone() ?? new JsonArray(),
            ["traits"] = Traits(test["labels"]),
        };
        if (extra?["feature"] is { } feature) row["featureId"] = feature.DeepClone();
        if (assembly is not null) row["assembly"] = assembly;
        // The template's own test-level detail fields.
        if ((string?)test["message"] is { } message) row["errorMessage"] = message;
        if ((string?)test["trace"] is { } trace) row["stackTrace"] = trace;
        if (test["stdout"] is JsonArray { Count: > 0 } stdout) row["standardOutput"] = string.Join("\n", stdout.Select(l => (string?)l));
        return row;
    }

    // rawStatus keeps the MTP outcome (errored, timedOut) that CTRF's status folds into failed. Without it, map CTRF's
    // status onto the outcomes the template counts.
    private static string Outcome(JsonObject test) => (string?)test["rawStatus"] ?? (string)test["status"]! switch
    {
        "pending" => "skipped",
        "other" => "errored",
        var status => status,
    };

    // CTRF labels map a key to one value or to an array of values; the template expects one key/value pair per trait.
    private static JsonArray Traits(JsonNode? labels)
    {
        var traits = new JsonArray();
        if (labels is not JsonObject map) return traits;
        foreach (var label in map)
        {
            var values = label.Value is JsonArray array ? array.Select(v => v!.ToString()) : [label.Value!.ToString()];
            foreach (var value in values) traits.Add(new JsonObject { ["key"] = label.Key, ["value"] = value });
        }
        return traits;
    }

    private static string? ShortName(string? typeName) => typeName?.Substring(typeName.LastIndexOf('.') + 1);

    private static string? Last(JsonNode? suite) => suite is JsonArray { Count: > 0 } array ? (string?)array[array.Count - 1] : null;

    private static string? Time(long? unixMilliseconds)
        => unixMilliseconds is { } ms ? DateTimeOffset.FromUnixTimeMilliseconds(ms).ToString("O") : null;

    // A number built in memory keeps its CLR type (long cannot be read as double); one parsed from a file can be read as either.
    private static double Number(JsonNode? node)
        => double.Parse(node?.ToJsonString() ?? "0", NumberStyles.Float, CultureInfo.InvariantCulture);
}
