using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;

namespace Davish.JustBdd;

/// <summary>
/// Renders a self-contained HTML file using the MTP template documented in THIRD-PARTY-NOTICES.md.
/// System.Text.Json escapes HTML-sensitive characters in the embedded JSON payload,
/// preventing a test-provided <c>&lt;/script&gt;</c> from closing the script element.
/// </summary>
internal static class HtmlWriter
{
    public const string Title = "Davish.JustBdd scenarios";

    private const string DataPlaceholder = "/*__MTP_DATA__*/null";
    private const string VersionPlaceholder = "__MTP_GENERATOR_VERSION__";

    private const string ResourcePrefix = "Davish.JustBdd.XunitV3.Report.";

    private static readonly Lazy<string> Template = new(() => Assemble(ReadPart));

    /// <summary>
    /// Inlines the template parts into one HTML file: justbdd.css into report.css, justbdd.js into report.js,
    /// then both into report.html. Each part keeps its bytes except one trailing newline.
    /// </summary>
    internal static string Assemble(Func<string, string> read)
    {
        var css = Fill(read("report.css"), ("/*__JUSTBDD_CSS__*/", read("justbdd.css")));
        var js = Fill(read("report.js"), ("/*__JUSTBDD_JS__*/", read("justbdd.js")));
        // An inlined closing tag would end the element early.
        RejectClosingTag(css, "</style");
        RejectClosingTag(js, "</script");

        var html = Fill(read("report.html"), ("/*__STYLE__*/", css), ("/*__SCRIPT__*/", js));
        if (html.Replace(DataPlaceholder, "").Contains("/*__"))
            throw new InvalidOperationException("The HTML template has an unfilled placeholder.");
        return html;
    }

    private static string Fill(string text, params (string Placeholder, string Content)[] parts)
    {
        foreach (var (placeholder, content) in parts)
        {
            var at = text.IndexOf(placeholder, StringComparison.Ordinal);
            if (at < 0 || text.IndexOf(placeholder, at + placeholder.Length, StringComparison.Ordinal) >= 0)
                throw new InvalidOperationException($"The HTML template must contain {placeholder} exactly once.");
            var body = content.EndsWith("\n", StringComparison.Ordinal) ? content.Substring(0, content.Length - 1) : content;
            text = text.Substring(0, at) + body + text.Substring(at + placeholder.Length);
        }
        return text;
    }

    private static void RejectClosingTag(string content, string tag)
    {
        if (content.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0)
            throw new InvalidOperationException($"An inlined template part contains {tag}>.");
    }

    private static string ReadPart(string name)
    {
        using var stream = typeof(HtmlWriter).Assembly.GetManifestResourceStream(ResourcePrefix + name)
            ?? throw new InvalidOperationException($"The embedded HTML template part {name} was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string Render(IReadOnlyList<ReportTest> tests, DateTimeOffset startTime, DateTimeOffset endTime, string? framework)
    {
        var rows = new JsonArray();
        var features = new JsonObject();
        for (var i = 0; i < tests.Count; i++)
        {
            var t = tests[i];
            var row = new JsonObject
            {
                ["rowKey"] = i,
                ["uid"] = t.DisplayName,
                ["displayName"] = t.DisplayName,
                ["outcome"] = t.Outcome,
                ["durationMs"] = t.DurationMs,
                ["className"] = t.ClassName,
                ["methodName"] = t.MethodName,
                ["scenarios"] = new JsonArray(t.Scenarios.Select(ScenarioJson.ToNode).ToArray()),
                ["traits"] = new JsonArray(t.Traits.Select(p => (JsonNode)new JsonObject { ["key"] = p.Key, ["value"] = p.Value }).ToArray()),
            };
            // Emit each feature once; tests reference it by id.
            if (t.Feature is { } f)
            {
                row["featureId"] = f.Id;
                if (!features.ContainsKey(f.Id))
                    features[f.Id] = new JsonObject
                    {
                        ["name"] = f.Name,
                        ["description"] = f.Description,
                        ["links"] = new JsonArray(f.Links.Select(l => (JsonNode)new JsonObject { ["title"] = l.Title, ["url"] = l.Url }).ToArray()),
                        ["metadata"] = new JsonArray(f.Metadata.Select(m => (JsonNode)new JsonObject
                        {
                            ["key"] = m.Key,
                            ["values"] = new JsonArray(m.Value.Select(v => (JsonNode)JsonValue.Create(v)!).ToArray()),
                        }).ToArray()),
                    };
            }
            rows.Add(row);
        }

        var data = new JsonObject
        {
            ["testApplication"] = Title,
            ["framework"] = framework,
            ["startTime"] = startTime.ToString("O"),
            ["endTime"] = endTime.ToString("O"),
            ["tests"] = rows,
            ["features"] = features,
            ["summary"] = new JsonObject
            {
                ["total"] = tests.Count,
                ["passed"] = tests.Count(t => t.Outcome == "passed"),
                ["failed"] = tests.Count(t => t.Outcome == "failed"),
                ["errored"] = tests.Count(t => t.Outcome == "errored"),
                ["timedOut"] = tests.Count(t => t.Outcome == "timedOut"),
                ["skipped"] = tests.Count(t => t.Outcome == "skipped"),
                ["totalDurationMs"] = tests.Sum(t => t.DurationMs),
            },
        };

        return Template.Value
            .Replace(DataPlaceholder, data.ToJsonString())
            .Replace(VersionPlaceholder, WebUtility.HtmlEncode(Version()));
    }

    private static string Version()
    {
        var version = typeof(HtmlWriter).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        var plus = version.IndexOf('+');
        return plus < 0 ? version : version.Substring(0, plus);
    }
}
