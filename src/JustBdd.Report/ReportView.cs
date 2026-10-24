using System.Globalization;
using System.Text.Json.Nodes;

namespace Davish.JustBdd;

/// <summary>Converts a CTRF document into the data the HTML template renders.</summary>
internal static class ReportView
{
    public static JsonObject FromCtrf(JsonObject ctrf)
    {
        var results = ctrf["results"]!.AsObject();
        var assembly = (string?)results["environment"]?["extra"]?[Ctrf.ExtensionKey]?["assembly"];

        var rows = new JsonArray();
        var outcomes = new List<string>();
        var totalDuration = 0d;
        foreach (var node in results["tests"]!.AsArray())
        {
            var test = node!.AsObject();
            var extra = test["extra"]?[Ctrf.ExtensionKey];
            var name = (string)test["name"]!;
            // rawStatus keeps the MTP outcome (errored, timedOut) that CTRF's status folds into failed.
            var outcome = (string?)test["rawStatus"] ?? (string)test["status"]!;
            var duration = Number(test["duration"]);
            outcomes.Add(outcome);
            totalDuration += duration;

            var row = new JsonObject
            {
                ["rowKey"] = rows.Count,
                ["uid"] = name,
                ["displayName"] = name,
                ["outcome"] = outcome,
                ["durationMs"] = duration,
                ["className"] = (string?)extra?["className"] ?? Last(test["suite"]),
                ["methodName"] = (string?)extra?["methodName"],
                ["scenarios"] = extra?["scenarios"]?.DeepClone() ?? new JsonArray(),
                ["traits"] = Traits(test["labels"]),
            };
            if (extra?["feature"] is { } feature) row["featureId"] = feature.DeepClone();
            if (assembly is not null) row["assembly"] = assembly;
            rows.Add(row);
        }

        var summary = results["summary"]!;
        return new JsonObject
        {
            ["testApplication"] = HtmlWriter.Title,
            ["framework"] = (string?)results["tool"]?["name"],
            ["startTime"] = Time(summary["start"]),
            ["endTime"] = Time(summary["stop"]),
            ["tests"] = rows,
            ["features"] = results["extra"]?[Ctrf.ExtensionKey]?["features"]?.DeepClone() ?? new JsonObject(),
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
    }

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

    private static string? Last(JsonNode? suite) => suite is JsonArray { Count: > 0 } array ? (string?)array[array.Count - 1] : null;

    private static string? Time(JsonNode? unixMilliseconds)
        => unixMilliseconds is null ? null : DateTimeOffset.FromUnixTimeMilliseconds((long)Number(unixMilliseconds)).ToString("O");

    // A number built in memory keeps its CLR type (long cannot be read as double); one parsed from a file can be read as either.
    private static double Number(JsonNode? node)
        => double.Parse(node?.ToJsonString() ?? "0", NumberStyles.Float, CultureInfo.InvariantCulture);
}
