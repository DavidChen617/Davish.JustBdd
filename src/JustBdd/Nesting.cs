namespace Davish.JustBdd;

/// <summary>
/// Collects scenarios executed within a step, including failures, in <see cref="StepResult.Children"/>.
/// AsyncLocal flows through child execution contexts, including Task.Run, without leaking child changes outward.
/// Each step and execution therefore has an independent collector.
/// </summary>
internal static class Nesting
{
    private static readonly AsyncLocal<ChildCollector?> Slot = new();

    /// <summary>The current step collector, or null for a top-level scenario.</summary>
    public static ChildCollector? Current => Slot.Value;

    public static ChildCollector Begin() => Slot.Value = new ChildCollector(Slot.Value);

    public static void End(ChildCollector collector) => Slot.Value = collector.Parent;
}

internal sealed class ChildCollector(ChildCollector? parent)
{
    private readonly List<ScenarioResult> _items = [];

    public ChildCollector? Parent { get; } = parent;

    // A step may run several child scenarios concurrently through Task.WhenAll.
    public void Add(ScenarioResult result)
    {
        lock (_items) _items.Add(result);
    }

    public IReadOnlyList<ScenarioResult> Snapshot()
    {
        lock (_items) return _items.ToArray();
    }
}
