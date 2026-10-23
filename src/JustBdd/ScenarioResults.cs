using System.Runtime.ExceptionServices;

namespace Davish.JustBdd;

/// <summary>Receives top-level results on success or failure. Implementations must support concurrent tests.</summary>
public interface IScenarioResultSink
{
    void Report(ScenarioResult result);
}

/// <summary>
/// Registers <see cref="IScenarioResultSink"/> instances. Nested results belong to <see cref="StepResult.Children"/>.
/// Sink errors propagate only when the scenario succeeds; multiple errors become an AggregateException.
/// A failed scenario preserves its original exception, while report failures cannot silently pass a successful test.
/// </summary>
public static class ScenarioResults
{
    private static readonly object Gate = new();
    private static IScenarioResultSink[] _sinks = [];

    /// <summary>Disposing the returned registration unsubscribes the sink. Repeated disposal is safe.</summary>
    public static IDisposable AddSink(IScenarioResultSink sink)
    {
        if (sink is null) throw new ArgumentNullException(nameof(sink));
        lock (Gate) _sinks = [.. _sinks, sink];
        return new Registration(sink);
    }

    // Copy on registration changes so frequent publication needs no lock.
    internal static void Publish(ScenarioResult result, bool throwOnSinkFailure)
    {
        var sinks = Volatile.Read(ref _sinks);
        List<Exception>? errors = null;

        foreach (var sink in sinks)
        {
            try { sink.Report(result); }
            catch (Exception ex) { (errors ??= []).Add(ex); }
        }

        if (errors is null || !throwOnSinkFailure) return;
        if (errors.Count == 1) ExceptionDispatchInfo.Capture(errors[0]).Throw();
        throw new AggregateException(errors);
    }

    private static void Remove(IScenarioResultSink sink)
    {
        lock (Gate)
        {
            var index = Array.IndexOf(_sinks, sink);
            if (index < 0) return;
            _sinks = [.. _sinks.Take(index), .. _sinks.Skip(index + 1)];
        }
    }

    private sealed class Registration(IScenarioResultSink sink) : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) Remove(sink);
        }
    }
}
