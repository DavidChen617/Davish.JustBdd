namespace Davish.JustBdd.Tests;

/// <summary>Run() executes the chain synchronously; RunAsync() is the asynchronous counterpart.</summary>
public class RunTests
{
    private static async Task<int> Later(int x)
    {
        await Task.Yield();
        await Task.Delay(10);
        return x + 1;
    }

    [Fact]
    public void Run_completes_async_steps_before_returning()
    {
        var log = new List<string>();

        var result = new Scenario("S")
            .Given(async () => await Later(0))
            .When(async x => { log.Add("when"); return await Later(x); })
            .Then(async x => { await Task.Yield(); log.Add($"then {x}"); })
            .Run();

        Assert.Equal(["when", "then 2"], log);
        Assert.True(result.Passed);
        Assert.Equal(3, result.Steps.Count);
    }

    [Fact]
    public void Run_throws_the_original_exception_with_step_data()
    {
        var ex = Assert.Throws<Xunit.Sdk.EqualException>(() =>
            new Scenario("S").Given(async () => await Later(0)).Then("Equals 5", x => Assert.Equal(5, x)).Run());

        Assert.Equal("Scenario 'S' > Then 'Equals 5'", ex.Data[Scenario.StepDataKey]);
        Assert.IsType<ScenarioResult>(ex.Data[Scenario.ResultDataKey]);
    }

    [Fact]
    public void Run_inside_a_step_is_collected_as_a_child()
    {
        var outer = new Scenario("Outer")
            .Given(() => 1)
            .Then("Run inner", x => { new Scenario("Inner").Then(() => { }).Run(); })
            .Run();

        Assert.Equal("Inner", Assert.Single(outer.Steps[1].Children).Description);
    }

    /// <summary>Drops every posted continuation, so anything that captures it never resumes.</summary>
    private sealed class NeverPumpedContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) { }
    }

    [Fact]
    public void Run_does_not_deadlock_on_a_caller_synchronization_context()
    {
        ScenarioResult? result = null;
        SynchronizationContext? after = null;
        var context = new NeverPumpedContext();

        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(context);
            result = new Scenario().Given(async () => await Later(0)).Then(x => Assert.Equal(1, x)).Run();
            after = SynchronizationContext.Current;
        });
        thread.Start();

        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "Run() deadlocked on the caller's SynchronizationContext.");
        Assert.True(result?.Passed);
        Assert.Same(context, after);   // The caller's context is restored.
    }
}
