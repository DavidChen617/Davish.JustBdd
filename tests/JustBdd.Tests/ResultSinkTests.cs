using System.Collections.Concurrent;

namespace Davish.JustBdd.Tests;

/// <summary>
/// Sinks are global and xUnit runs tests in parallel, so each test's sink only collects scenarios whose description contains its own id.
/// </summary>
public class ResultSinkTests
{
    private sealed class Collecting(string id) : IScenarioResultSink
    {
        public ConcurrentQueue<ScenarioResult> Results { get; } = new();

        public void Report(ScenarioResult result)
        {
            if (result.Description.StartsWith(id)) Results.Enqueue(result);
        }
    }

    private sealed class Throwing(string id, Exception error) : IScenarioResultSink
    {
        public void Report(ScenarioResult result)
        {
            if (result.Description.StartsWith(id)) throw error;
        }
    }

    private static string NewId() => Guid.NewGuid().ToString("N");

    [Fact]
    public async Task Top_level_passing_scenario_is_reported_once()
    {
        var id = NewId();
        var sink = new Collecting(id);
        using var _ = ScenarioResults.AddSink(sink);

        var result = await new Scenario(id).Given(() => 1).Then(x => { });

        Assert.Same(result, Assert.Single(sink.Results));
    }

    [Fact]
    public async Task Failing_scenario_is_reported_and_original_exception_still_thrown()
    {
        var id = NewId();
        var sink = new Collecting(id);
        using var _ = ScenarioResults.AddSink(sink);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario(id).Given(() => 1).Then(x => throw new InvalidOperationException()));

        Assert.False(Assert.Single(sink.Results).Passed);
    }

    [Fact]
    public async Task Nested_scenarios_are_not_reported_separately()
    {
        var id = NewId();
        var sink = new Collecting(id);
        using var _ = ScenarioResults.AddSink(sink);

        await new Scenario(id + "-outer")
            .Given(() => 1)
            .Then(async x => { await new Scenario(id + "-inner").Given(() => 2).Then(y => { }); });

        var outer = Assert.Single(sink.Results);
        Assert.Equal(id + "-outer", outer.Description);
        Assert.Equal(id + "-inner", Assert.Single(outer.Steps[1].Children).Description);
    }

    [Fact]
    public async Task Disposed_sink_no_longer_receives_results()
    {
        var id = NewId();
        var sink = new Collecting(id);
        var registration = ScenarioResults.AddSink(sink);

        await new Scenario(id).Given(() => 1).Then(x => { });
        registration.Dispose();
        registration.Dispose();   // Disposing twice is harmless
        await new Scenario(id).Given(() => 1).Then(x => { });

        Assert.Single(sink.Results);
    }

    [Fact]
    public async Task Every_sink_is_called()
    {
        var id = NewId();
        var first = new Collecting(id);
        var second = new Collecting(id);
        using var a = ScenarioResults.AddSink(first);
        using var b = ScenarioResults.AddSink(second);

        await new Scenario(id).Given(() => 1).Then(x => { });

        Assert.Single(first.Results);
        Assert.Single(second.Results);
    }

    [Fact(Timeout = 5000)]
    public async Task Concurrent_scenarios_are_each_reported_once()
    {
        var id = NewId();
        var sink = new Collecting(id);
        using var _ = ScenarioResults.AddSink(sink);

        await Task.WhenAll(Enumerable.Range(0, 20).Select(i =>
            new Scenario($"{id}-{i}").Given(async () => { await Task.Yield(); return i; }).Then(x => { }).RunAsync().AsTask()))
            .WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(20, sink.Results.Select(r => r.Description).Distinct().Count());
        Assert.Equal(20, sink.Results.Count);
    }

    // ---- A throwing sink: a failed scenario keeps its original exception; a passed scenario surfaces the sink's exception

    [Fact]
    public async Task Sink_failure_surfaces_when_scenario_passed()
    {
        var id = NewId();
        var error = new IOException("Cannot write the report");
        var after = new Collecting(id);
        using var a = ScenarioResults.AddSink(new Throwing(id, error));
        using var b = ScenarioResults.AddSink(after);

        var ex = await Assert.ThrowsAsync<IOException>(async () => await new Scenario(id).Given(() => 1).Then(x => { }));

        Assert.Same(error, ex);
        Assert.Single(after.Results);   // Later sinks still run after an earlier sink fails
    }

    [Fact]
    public async Task Sink_failure_is_ignored_when_scenario_failed()
    {
        var id = NewId();
        using var _ = ScenarioResults.AddSink(new Throwing(id, new IOException()));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario(id).Given(() => 1).Then(x => throw new InvalidOperationException()));
    }

    [Fact]
    public async Task Several_sink_failures_surface_as_aggregate()
    {
        var id = NewId();
        using var a = ScenarioResults.AddSink(new Throwing(id, new IOException("a")));
        using var b = ScenarioResults.AddSink(new Throwing(id, new IOException("b")));

        var ex = await Assert.ThrowsAsync<AggregateException>(async () => await new Scenario(id).Given(() => 1).Then(x => { }));

        Assert.Equal(["a", "b"], ex.InnerExceptions.Select(e => e.Message));
    }
}
