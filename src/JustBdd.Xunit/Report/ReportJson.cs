using System.Text.Json.Nodes;

namespace Davish.JustBdd;

/// <summary>Feature JSON, shared by the CTRF document and the HTML report built from it.</summary>
internal static class ReportJson
{
    public static JsonObject Feature(ReportFeature f) => new()
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
