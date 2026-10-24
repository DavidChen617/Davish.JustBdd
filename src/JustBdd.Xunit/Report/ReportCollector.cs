namespace Davish.JustBdd;

/// <param name="Outcome">MTP outcome: passed, failed, errored, timedOut, or skipped.</param>
/// <param name="Feature">The test's feature, or null when the test has none.</param>
/// <param name="Traits">xUnit traits sorted by key, then value; empty when the test has none.</param>
internal sealed record ReportTest(
    string DisplayName,
    string Outcome,
    double DurationMs,
    string? ClassName,
    string? MethodName,
    IReadOnlyList<ScenarioResult> Scenarios,
    ReportFeature? Feature = null,
    IReadOnlyList<KeyValuePair<string, string>>? Traits = null)
{
    public IReadOnlyList<KeyValuePair<string, string>> Traits { get; init; } = Traits ?? [];

    /// <summary>What MTP reported for the test, or null when no MTP outcome arrived.</summary>
    public TestNodeResult? Node { get; init; }
}

/// <summary>What MTP reports about one finished test.</summary>
/// <param name="Outcome">MTP outcome: passed, failed, errored, timedOut, or skipped.</param>
internal sealed record TestNodeResult(string DisplayName, string Outcome, double DurationMs, string? ClassName, string? MethodName)
{
    public string? Uid { get; init; }
    public string? AssemblyName { get; init; }
    public string? Namespace { get; init; }
    public DateTimeOffset? Start { get; init; }
    public DateTimeOffset? Stop { get; init; }
    public string? FilePath { get; init; }
    public int? Line { get; init; }

    /// <summary>The failure message, or the skip reason.</summary>
    public string? Message { get; init; }

    public string? Trace { get; init; }

    /// <summary>xUnit traits sorted by key, then value.</summary>
    public IReadOnlyList<KeyValuePair<string, string>> Traits { get; init; } = [];

    /// <summary>ITestOutputHelper lines. Console output is not attributed to tests.</summary>
    public IReadOnlyList<string> Stdout { get; init; } = [];

    public IReadOnlyList<ReportAttachment> Attachments { get; init; } = [];
}

internal sealed record ReportAttachment(string Name, string Path);

/// <summary>
/// Collects scenarios by xUnit TestDisplayName and outcomes from MTP TestNodeUpdateMessage.
/// MTP node UIDs differ from xUnit UniqueIDs; this implementation correlates their display names.
/// </summary>
internal sealed class ReportCollector
{
    private readonly object _gate = new();
    private readonly List<string> _order = [];          // Preserve first-seen scenario order.
    private readonly List<string> _resultOrder = [];    // Preserve first-seen outcome order.
    private readonly Dictionary<string, List<ScenarioResult>> _scenarios = [];
    private readonly Dictionary<string, TestNodeResult> _results = [];
    private readonly Dictionary<string, ReportFeature> _features = [];

    public DateTimeOffset StartTime { get; } = DateTimeOffset.UtcNow;

    /// <summary>Excludes scenarios outside tests, which have no TestDisplayName.</summary>
    public void AddScenario(string? testDisplayName, ScenarioResult result, ReportFeature? feature = null)
    {
        if (testDisplayName is null) return;
        lock (_gate)
        {
            if (!_scenarios.TryGetValue(testDisplayName, out var list))
            {
                _scenarios[testDisplayName] = list = [];
                _order.Add(testDisplayName);
            }
            list.Add(result);
            if (feature is not null) _features[testDisplayName] = feature;
        }
    }

    public void SetOutcome(string displayName, string outcome, double durationMs, string? className, string? methodName,
        IReadOnlyList<KeyValuePair<string, string>>? traits = null)
        => SetResult(new TestNodeResult(displayName, outcome, durationMs, className, methodName) { Traits = traits ?? [] });

    public void SetResult(TestNodeResult result)
    {
        lock (_gate)
        {
            if (!_results.ContainsKey(result.DisplayName)) _resultOrder.Add(result.DisplayName);
            _results[result.DisplayName] = result;
        }
    }

    /// <summary>Returns tests with scenarios only.</summary>
    public IReadOnlyList<ReportTest> Build()
    {
        lock (_gate) return _order.Select(Create).ToArray();
    }

    /// <summary>Returns tests with scenarios, then tests without scenarios in the order MTP reported them.</summary>
    public IReadOnlyList<ReportTest> BuildAll()
    {
        lock (_gate) return _order.Concat(_resultOrder.Where(name => !_scenarios.ContainsKey(name))).Select(Create).ToArray();
    }

    private ReportTest Create(string name)
    {
        var scenarios = _scenarios.TryGetValue(name, out var list) ? list.ToArray() : [];
        var feature = _features.TryGetValue(name, out var f) ? f : null;
        return _results.TryGetValue(name, out var r)
            ? new ReportTest(name, r.Outcome, r.DurationMs, r.ClassName, r.MethodName, scenarios, feature, r.Traits) { Node = r }
            // Fall back to scenario outcomes if no MTP outcome arrived.
            : new ReportTest(name, scenarios.All(s => s.Passed) ? "passed" : "failed", 0, null, null, scenarios, feature);
    }
}
