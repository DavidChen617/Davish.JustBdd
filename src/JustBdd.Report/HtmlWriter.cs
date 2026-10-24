using System.Net;
using System.Text.Json.Nodes;

namespace Davish.JustBdd;

/// <summary>
/// Renders a self-contained HTML file using the MTP template documented in THIRD-PARTY-NOTICES.md of Davish.JustBdd.XunitV3.
/// System.Text.Json escapes HTML-sensitive characters in the embedded JSON payload,
/// preventing a test-provided <c>&lt;/script&gt;</c> from closing the script element.
/// </summary>
internal static class HtmlWriter
{
    public const string Title = "Davish.JustBdd scenarios";

    private const string DataPlaceholder = "/*__MTP_DATA__*/null";
    private const string VersionPlaceholder = "__MTP_GENERATOR_VERSION__";

    private const string ResourcePrefix = "Davish.JustBdd.Report.Templates.";

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

    /// <param name="view">Template data; see <see cref="ReportView.FromCtrf"/>.</param>
    /// <param name="generatorVersion">The version shown as the report generator.</param>
    public static string Render(JsonObject view, string generatorVersion)
        => Template.Value
            .Replace(DataPlaceholder, view.ToJsonString())
            .Replace(VersionPlaceholder, WebUtility.HtmlEncode(generatorVersion));
}
