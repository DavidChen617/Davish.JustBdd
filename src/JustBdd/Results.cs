namespace Davish.JustBdd;

public enum StepKind { Given, When, Then, Assert }

/// <summary>
/// <see cref="StepStatus.ThrewAsExpected"/> passes: When threw the expected exception, retained in <see cref="StepResult.Error"/>.
/// </summary>
public enum StepStatus { Passed, Failed, NotRun, ThrewAsExpected }

/// <param name="Description">Optional description. Empty or whitespace values become null; display <paramref name="Code"/> instead.</param>
/// <param name="Code">Step lambda source captured by CallerArgumentExpression, with its original indentation.</param>
public sealed record StepInfo(StepKind Kind, string? Description, string Code)
{
    public string? Description { get; init; } = string.IsNullOrWhiteSpace(Description) ? null : Description;
}

public sealed record StepResult(StepInfo Step, StepStatus Status, TimeSpan Duration, Exception? Error)
{
    /// <summary>Nested scenarios, including failures, in completion order.</summary>
    public IReadOnlyList<ScenarioResult> Children { get; init; } = [];
}

public sealed record ScenarioResult(string Description, string FilePath, int LineNumber, IReadOnlyList<StepResult> Steps)
{
    public bool Passed => Steps.All(s => s.Status is StepStatus.Passed or StepStatus.ThrewAsExpected);
}

internal sealed record ScenarioContext(string Description, string FilePath, int LineNumber);

/// <summary>Records one execution. Each RunAsync owns an independent recorder.</summary>
internal sealed class Recorder
{
    private readonly List<StepResult> _results = [];

    public void Add(StepInfo step, StepStatus status, TimeSpan duration, Exception? error, IReadOnlyList<ScenarioResult> children)
        => _results.Add(new StepResult(step, status, duration, error) { Children = children });

    // Execution follows declaration order; unrecorded steps have not run.
    public ScenarioResult ToResult(ScenarioContext ctx, StepInfo[] declared)
        => new(ctx.Description, ctx.FilePath, ctx.LineNumber,
            [.. _results
, .. declared.Skip(_results.Count).Select(s => new StepResult(s, StepStatus.NotRun, TimeSpan.Zero, null))]);
}
