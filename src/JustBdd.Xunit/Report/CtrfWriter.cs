using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using Xunit;

namespace Davish.JustBdd;

/// <summary>Run-level facts for a CTRF document.</summary>
/// <param name="AssemblyName">The test assembly; one CTRF document covers one test application.</param>
internal sealed record CtrfRun(DateTimeOffset Start, DateTimeOffset Stop, string? AssemblyName, string? TargetFramework);

/// <summary>
/// Writes a Common Test Report Format document (https://github.com/ctrf-io/ctrf). CTRF rejects unknown properties
/// outside <c>extra</c> objects, so Davish.JustBdd data lives under <c>extra["davish.justbdd"]</c>.
/// </summary>
internal static class CtrfWriter
{

    public static JsonObject Build(IReadOnlyList<ReportTest> tests, CtrfRun run)
    {
        var nodes = new JsonArray();
        var features = new JsonObject();
        foreach (var t in tests)
        {
            nodes.Add(Test(t));
            if (t.Feature is { } f && !features.ContainsKey(f.Id)) features[f.Id] = ReportJson.Feature(f);
        }

        var statuses = tests.Select(t => Status(t.Outcome)).ToArray();
        var start = run.Start.ToUnixTimeMilliseconds();
        var stop = run.Stop.ToUnixTimeMilliseconds();
        var results = new JsonObject
        {
            ["tool"] = new JsonObject { ["name"] = "xUnit.net v3", ["version"] = Ctrf.Version(typeof(TestContext).Assembly) },
            ["summary"] = new JsonObject
            {
                ["tests"] = tests.Count,
                ["passed"] = statuses.Count(s => s == "passed"),
                ["failed"] = statuses.Count(s => s == "failed"),
                ["skipped"] = statuses.Count(s => s == "skipped"),
                ["pending"] = 0,
                ["other"] = statuses.Count(s => s == "other"),
                ["start"] = start,
                ["stop"] = stop,
                ["duration"] = stop - start,
            },
            ["tests"] = nodes,
            ["environment"] = new JsonObject
            {
                ["osPlatform"] = OsPlatform(),
                ["osRelease"] = Environment.OSVersion.Version.ToString(),
                ["osVersion"] = RuntimeInformation.OSDescription,
                ["extra"] = Extension(WithoutNulls(new JsonObject { ["assembly"] = run.AssemblyName, ["targetFramework"] = run.TargetFramework })),
            },
        };
        if (features.Count > 0) results["extra"] = Extension(new JsonObject { ["features"] = features });

        return new JsonObject
        {
            ["reportFormat"] = "CTRF",
            ["specVersion"] = Ctrf.SpecVersion,
            ["timestamp"] = run.Stop.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
            ["generatedBy"] = "Davish.JustBdd.XunitV3 " + Ctrf.Version(typeof(CtrfWriter).Assembly),
            ["results"] = results,
        };
    }

    /// <summary>CTRF has five statuses; errored and timed-out tests are failures, and rawStatus keeps the MTP outcome.</summary>
    internal static string Status(string outcome) => outcome switch
    {
        "passed" => "passed",
        "failed" or "errored" or "timedOut" => "failed",
        "skipped" => "skipped",
        _ => "other",
    };

    private static JsonObject Test(ReportTest t)
    {
        var n = t.Node;
        var test = new JsonObject
        {
            ["name"] = t.DisplayName,
            ["status"] = Status(t.Outcome),
            ["rawStatus"] = t.Outcome,
            ["duration"] = (long)Math.Round(t.DurationMs),
        };
        if (n?.Uid is { } uid) test["testId"] = uid;
        if (n?.Start is { } s) test["start"] = s.ToUnixTimeMilliseconds();
        if (n?.Stop is { } e) test["stop"] = e.ToUnixTimeMilliseconds();
        var suite = new[] { n?.AssemblyName, n?.Namespace, t.ClassName }.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        if (suite.Length > 0) test["suite"] = Strings(suite!);
        if (n?.Message is { } message) test["message"] = message;
        if (n?.Trace is { } trace) test["trace"] = trace;
        if (n?.FilePath is { } file) test["filePath"] = file;
        if (n?.Line is { } line) test["line"] = line;
        if (t.Traits.Count > 0) test["labels"] = Labels(t.Traits);
        if (n?.Stdout.Count > 0) test["stdout"] = Strings(n.Stdout);
        if (n?.Attachments.Count > 0)
            test["attachments"] = new JsonArray(n.Attachments.Select(a => (JsonNode)new JsonObject
            {
                ["name"] = a.Name,
                ["contentType"] = ContentType(a.Path),
                ["path"] = a.Path,
            }).ToArray());

        var steps = t.Scenarios.SelectMany(Flatten).ToArray();
        if (steps.Length > 0) test["steps"] = new JsonArray(steps);

        // CTRF has no class or method fields; the report shows both.
        var extra = WithoutNulls(new JsonObject { ["className"] = t.ClassName, ["methodName"] = t.MethodName });
        if (t.Feature is { } f) extra["feature"] = f.Id;
        if (t.Scenarios.Count > 0) extra["scenarios"] = new JsonArray(t.Scenarios.Select(ScenarioJson.ToNode).ToArray());
        if (extra.Count > 0) test["extra"] = Extension(extra);
        return test;
    }

    // CTRF steps are a flat list of name and status; a nested scenario's steps follow the step that ran it.
    private static IEnumerable<JsonNode> Flatten(ScenarioResult scenario)
    {
        foreach (var step in scenario.Steps)
        {
            yield return new JsonObject
            {
                ["name"] = $"{step.Step.Kind} {step.Step.Description ?? step.Step.Code}",
                ["status"] = step.Status switch
                {
                    StepStatus.Passed or StepStatus.ThrewAsExpected => "passed",
                    StepStatus.Failed => "failed",
                    _ => "skipped",
                },
            };
            foreach (var child in step.Children.SelectMany(Flatten)) yield return child;
        }
    }

    // A trait key with one value becomes a string; repeated keys become an array.
    private static JsonObject Labels(IReadOnlyList<KeyValuePair<string, string>> traits)
    {
        var labels = new JsonObject();
        foreach (var group in traits.GroupBy(t => t.Key, StringComparer.Ordinal))
        {
            var values = group.Select(t => t.Value).ToArray();
            labels[group.Key] = values.Length == 1 ? JsonValue.Create(values[0]) : Strings(values);
        }
        return labels;
    }

    private static JsonObject Extension(JsonObject value) => new() { [Ctrf.ExtensionKey] = value };

    private static JsonObject WithoutNulls(JsonObject value)
    {
        foreach (var key in value.Where(p => p.Value is null).Select(p => p.Key).ToArray()) value.Remove(key);
        return value;
    }

    private static JsonArray Strings(IEnumerable<string> values) => new(values.Select(v => (JsonNode)JsonValue.Create(v)!).ToArray());

    private static string OsPlatform() =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows"
        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS"
        : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux"
        : RuntimeInformation.OSDescription;

    private static string ContentType(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".txt" or ".log" => "text/plain",
        ".json" => "application/json",
        ".xml" => "application/xml",
        ".html" or ".htm" => "text/html",
        ".csv" => "text/csv",
        ".md" => "text/markdown",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".svg" => "image/svg+xml",
        ".pdf" => "application/pdf",
        ".zip" => "application/zip",
        _ => "application/octet-stream",
    };
}
