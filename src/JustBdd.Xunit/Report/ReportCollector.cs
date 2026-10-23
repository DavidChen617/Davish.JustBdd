namespace Davish.JustBdd;

/// <param name="Outcome">MTP outcome: passed, failed, errored, timedOut, or skipped.</param>
/// <param name="Feature">The test's feature, or null when the test has none.</param>
internal sealed record ReportTest(
    string DisplayName,
    string Outcome,
    double DurationMs,
    string? ClassName,
    string? MethodName,
    IReadOnlyList<ScenarioResult> Scenarios,
    ReportFeature? Feature = null);

/// <summary>
/// Collects scenarios by xUnit TestDisplayName and outcomes from MTP TestNodeUpdateMessage.
/// MTP node UIDs differ from xUnit UniqueIDs; this implementation correlates their display names.
/// </summary>
internal sealed class ReportCollector
{
    private readonly object _gate = new();
    private readonly List<string> _order = [];   // Preserve first-seen scenario order.
    private readonly Dictionary<string, List<ScenarioResult>> _scenarios = [];
    private readonly Dictionary<string, TestOutcome> _outcomes = [];
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

    public void SetOutcome(string displayName, string outcome, double durationMs, string? className, string? methodName)
    {
        lock (_gate) _outcomes[displayName] = new TestOutcome(outcome, durationMs, className, methodName);
    }

    /// <summary>Returns tests with scenarios. Use the native report for ordinary tests.</summary>
    public IReadOnlyList<ReportTest> Build()
    {
        lock (_gate)
        {
            return _order.Select(name =>
            {
                var scenarios = _scenarios[name].ToArray();
                var feature = _features.TryGetValue(name, out var f) ? f : null;
                return _outcomes.TryGetValue(name, out var o)
                    ? new ReportTest(name, o.Outcome, o.DurationMs, o.ClassName, o.MethodName, scenarios, feature)
                    // Fall back to scenario outcomes if no MTP outcome arrived.
                    : new ReportTest(name, scenarios.All(s => s.Passed) ? "passed" : "failed", 0, null, null, scenarios, feature);
            }).ToArray();
        }
    }

    private sealed record TestOutcome(string Outcome, double DurationMs, string? ClassName, string? MethodName);
}
