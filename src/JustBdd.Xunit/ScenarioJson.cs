using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Davish.JustBdd;

/// <summary>Converts results to report JSON through DTOs instead of serializing exceptions directly.</summary>
internal static class ScenarioJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public static JsonNode ToNode(ScenarioResult result) => JsonSerializer.SerializeToNode(ToDto(result), Options)!;

    private static ScenarioDto ToDto(ScenarioResult r) => new()
    {
        Description = r.Description,
        FilePath = r.FilePath,
        LineNumber = r.LineNumber,
        Passed = r.Passed,
        Steps = [.. r.Steps.Select(s => new StepDto
        {
            Kind = s.Step.Kind,
            Description = s.Step.Description,
            Code = s.Step.Code,
            Status = s.Status,
            DurationMs = s.Duration.TotalMilliseconds,
            // Exception.Data can contain ScenarioResult and form cycles. Retain only type, message, and stack trace.
            Error = s.Error is { } ex ? new ErrorDto { Type = ex.GetType().FullName, Message = ex.Message, StackTrace = ex.StackTrace } : null,
            Children = [.. s.Children.Select(ToDto)],
        })],
    };

    private sealed class ScenarioDto
    {
        public string Description { get; init; } = "";
        public string FilePath { get; init; } = "";
        public int LineNumber { get; init; }
        public bool Passed { get; init; }
        public StepDto[] Steps { get; init; } = [];
    }

    private sealed class StepDto
    {
        public StepKind Kind { get; init; }
        public string? Description { get; init; }
        public string Code { get; init; } = "";
        public StepStatus Status { get; init; }
        public double DurationMs { get; init; }
        public ErrorDto? Error { get; init; }
        public ScenarioDto[] Children { get; init; } = [];
    }

    private sealed class ErrorDto
    {
        public string? Type { get; init; }
        public string Message { get; init; } = "";
        public string? StackTrace { get; init; }
    }
}
