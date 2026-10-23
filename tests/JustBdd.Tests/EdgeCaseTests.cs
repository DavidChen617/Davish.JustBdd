namespace Davish.JustBdd.Tests;

public class EdgeCaseTests
{
    private static string? StepOf(Exception ex) => ex.Data[Scenario.StepDataKey] as string;

    private static ScenarioResult ResultOf(Exception ex) => Assert.IsType<ScenarioResult>(ex.Data[Scenario.ResultDataKey]);

    // Not async: throws synchronously at the call, before any Task exists
    private static Task ThrowNowTask() => throw new InvalidOperationException();
    private static ValueTask ThrowNowValueTask() => throw new InvalidOperationException();
    private static Task<int> ThrowNowTaskOfT() => throw new InvalidOperationException();
    private static ValueTask<int> ThrowNowValueTaskOfT() => throw new InvalidOperationException();

    private static int Make() => 1;

    private static async Task<int> MakeAsync()
    {
        await Task.Yield();
        return 2;
    }

    private static readonly List<int> Effects = [];
    private static void Effect(int x) => Effects.Add(x);

    // ---- ThenThrows

    [Fact]
    public async Task ThenThrows_does_not_catch_an_exception_thrown_by_Given()
    {
        var whenRan = false;

        var ex = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await new Scenario("S")
                .Given<int>("Broken Given", () => throw new ArgumentException())
                .When("w", x => whenRan = true)
                .ThenThrows<ArgumentException>("Expect When to throw").RunAsync());

        Assert.False(whenRan);
        Assert.Equal("Scenario 'S' > Given 'Broken Given'", StepOf(ex));
        Assert.Equal([StepStatus.Failed, StepStatus.NotRun, StepStatus.NotRun], ResultOf(ex).Steps.Select(s => s.Status));
    }

    // ---- Synchronous throw: a lambda that returns an awaitable throws before creating the Task

    [Fact]
    public async Task Synchronous_throw_from_awaitable_returning_lambda_is_recorded_as_step_failure()
    {
        var given = new Scenario("S").Given("g", () => 1);

        var paths = new List<string?>
        {
            StepOf(await Assert.ThrowsAsync<InvalidOperationException>(async () => await given.Then("t", x => ThrowNowTask()).RunAsync())),
            StepOf(await Assert.ThrowsAsync<InvalidOperationException>(async () => await given.Then("t", x => ThrowNowValueTask()).RunAsync())),
            StepOf(await Assert.ThrowsAsync<InvalidOperationException>(async () => await given.Then("t", x => ThrowNowValueTaskOfT()).RunAsync())),
            StepOf(await Assert.ThrowsAsync<InvalidOperationException>(async () => await given.When("w", x => ThrowNowTaskOfT()).Then(_ => { }).RunAsync())),
            StepOf(await Assert.ThrowsAsync<InvalidOperationException>(async () => await new Scenario("S").Given("g", () => ThrowNowValueTaskOfT()).Then(_ => { }).RunAsync())),
        };

        Assert.Equal(
            ["Scenario 'S' > Then 't'", "Scenario 'S' > Then 't'", "Scenario 'S' > Then 't'", "Scenario 'S' > When 'w'", "Scenario 'S' > Given 'g'"],
            paths);
    }

    // ---- Concurrent execution

    // If the two runs do not interleave (for example, they are serialized), the gate never opens; Timeout turns that into a failure instead of a hang
    [Fact(Timeout = 5000)]
    public async Task Concurrent_runs_of_the_same_stage_record_independently()
    {
        var gate = new TaskCompletionSource();
        var started = 0;
        var stage = new Scenario("S")
            .Given(async () =>
            {
                if (Interlocked.Increment(ref started) == 2) gate.SetResult();
                await gate.Task.WaitAsync(TestContext.Current.CancellationToken);   // Release only after both runs reach this point, so they really interleave
                return started;
            })
            .Then("t", x => { });

        var results = await Task.WhenAll(stage.RunAsync().AsTask(), stage.RunAsync().AsTask());

        Assert.All(results, r =>
        {
            Assert.Equal([StepKind.Given, StepKind.Then], r.Steps.Select(s => s.Step.Kind));
            Assert.True(r.Passed);
        });
        Assert.NotSame(results[0].Steps, results[1].Steps);
    }

    // ---- State

    [Fact]
    public async Task Given_failure_marks_the_rest_not_run()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario("S")
                .Given<int>("g", () => throw new InvalidOperationException())
                .When("w", x => x + 1)
                .Then("t1", x => { })
                .Then("t2", x => { }).RunAsync());

        Assert.Equal(
            [StepStatus.Failed, StepStatus.NotRun, StepStatus.NotRun, StepStatus.NotRun],
            ResultOf(ex).Steps.Select(s => s.Status));
    }

    [Fact]
    public async Task Null_state_flows_to_later_steps()
    {
        var seen = new List<string?>();

        var result = await new Scenario()
            .Given(() => (string?)null)
            .When(s => seen.Add(s))
            .Then(s => seen.Add(s))
            .Assert(s => Assert.Null(s))
            .RunAsync();

        Assert.Equal([null, null], seen);
        Assert.True(result.Passed);
    }

    // ---- Overloads: method groups and typed delegate variables

    [Fact]
    public async Task Method_groups_pick_the_right_overload_and_record_the_method_name()
    {
        Effects.Clear();

        var sync = await new Scenario().Given(Make).Then(x => Assert.Equal(1, x)).RunAsync();
        var async = await new Scenario().Given(MakeAsync).When(x => x + 1).Then(x => Assert.Equal(3, x)).RunAsync();
        var voidWhen = await new Scenario().Given(Make).When(Effect).Then(x => Assert.Equal(1, x)).RunAsync();

        Assert.Equal(["Make", "MakeAsync", "Effect"], new[] { sync.Steps[0], async.Steps[0], voidWhen.Steps[1] }.Select(s => s.Step.Code));
        Assert.Equal([1], Effects);
    }

    [Fact]
    public async Task Typed_delegate_variables_are_awaited_and_not_misled_by_covariance()
    {
        // Func<int, Task<int>> also converts to Func<int, Task> through covariance; make sure the value-returning When is chosen
        Func<int, Task<int>> doubleAsync = async x => { await Task.Yield(); return x * 2; };
        Func<int, ValueTask> check = x => { Assert.Equal(2, x); return default; };

        var result = await new Scenario().Given(() => 1).When(doubleAsync).Then(check).RunAsync();

        Assert.True(result.Passed);
        Assert.Equal(["doubleAsync", "check"], result.Steps.Skip(1).Select(s => s.Step.Code));
    }

    // ---- An empty or whitespace description counts as missing; code is displayed instead

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_description_falls_back_to_code(string blank)
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario("S").Given(blank, () => 1).Then(blank, x => ThrowNowTask()).RunAsync());

        Assert.Equal("Scenario 'S' > Then 'x => ThrowNowTask()'", StepOf(ex));
        Assert.All(ResultOf(ex).Steps, s => Assert.Null(s.Step.Description));
    }

    // ---- Nested scenarios: Data keeps the outermost result and the innermost step path (where the failure happened)

    [Fact]
    public async Task Nested_scenario_failure_keeps_outer_result_and_inner_step_path()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario("Outer")
                .Given("g", () => 1)
                .Then("Run inner", async x =>
                {
                    await new Scenario("Inner").When<int>("Inner breaks", () => throw new InvalidOperationException()).Then(_ => { }).RunAsync();
                })
                .Then("After outer", x => { }).RunAsync());

        Assert.Equal("Scenario 'Inner' > When 'Inner breaks'", StepOf(ex));
        var outer = ResultOf(ex);
        Assert.Equal("Outer", outer.Description);
        Assert.Equal([StepStatus.Passed, StepStatus.Failed, StepStatus.NotRun], outer.Steps.Select(s => s.Status));
    }
}
