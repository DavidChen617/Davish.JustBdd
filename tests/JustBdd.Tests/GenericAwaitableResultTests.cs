namespace Davish.JustBdd.Tests;

/// <summary>
/// Steps without a value (Then, Assert, and checks after ThenThrows) must await lambdas that return Task&lt;T&gt; or ValueTask&lt;T&gt;.
/// Task&lt;T&gt; converts to Task through covariance; ValueTask&lt;T&gt; is a struct and cannot convert to ValueTask,
/// so without a dedicated overload only Action&lt;T&gt; applies, the result is discarded, and the test passes falsely.
/// </summary>
public class GenericAwaitableResultTests
{
    private sealed class LateException : Exception;

    private static async Task<int> FailLaterTaskOfT()
    {
        await Task.Yield();
        throw new LateException();
    }

    private static async ValueTask<int> FailLaterValueTaskOfT()
    {
        await Task.Yield();
        throw new LateException();
    }

    private static Task AssertAwaited<T>(ThenStage<T> stage)
        => Assert.ThrowsAsync<LateException>(async () => await stage.RunAsync());

    [Fact]
    public async Task Scenario_Then()
    {
        await AssertAwaited(new Scenario().Then(() => FailLaterValueTaskOfT()));
        await AssertAwaited(new Scenario().Then("d", () => FailLaterValueTaskOfT()));
        await AssertAwaited(new Scenario().Then(() => FailLaterTaskOfT()));
        await AssertAwaited(new Scenario().Then("d", () => FailLaterTaskOfT()));
    }

    [Fact]
    public async Task GivenStage_Then()
    {
        var given = new Scenario().Given(() => 1);
        await AssertAwaited(given.Then(x => FailLaterValueTaskOfT()));
        await AssertAwaited(given.Then("d", x => FailLaterValueTaskOfT()));
        await AssertAwaited(given.Then(x => FailLaterTaskOfT()));
        await AssertAwaited(given.Then("d", x => FailLaterTaskOfT()));
    }

    [Fact]
    public async Task WhenStage_Then()
    {
        var when = new Scenario().When(() => 1);
        await AssertAwaited(when.Then(x => FailLaterValueTaskOfT()));
        await AssertAwaited(when.Then("d", x => FailLaterValueTaskOfT()));
        await AssertAwaited(when.Then(x => FailLaterTaskOfT()));
        await AssertAwaited(when.Then("d", x => FailLaterTaskOfT()));
    }

    [Fact]
    public async Task ThenStage_Then_and_Assert()
    {
        var then = new Scenario().Given(() => 1).Then("t");
        await AssertAwaited(then.Then(x => FailLaterValueTaskOfT()));
        await AssertAwaited(then.Then("d", x => FailLaterValueTaskOfT()));
        await AssertAwaited(then.Assert(x => FailLaterValueTaskOfT()));
        await AssertAwaited(then.Then(x => FailLaterTaskOfT()));
        await AssertAwaited(then.Assert(x => FailLaterTaskOfT()));
    }

    [Fact]
    public async Task Then_after_Then_desc()
    {
        var a = new Scenario().Given(() => 1).Then("t");
        await AssertAwaited(a.Then(x => FailLaterValueTaskOfT()));
        await AssertAwaited(a.Then("d", x => FailLaterValueTaskOfT()));
        await AssertAwaited(a.Then(x => FailLaterTaskOfT()));
        await AssertAwaited(a.Then("d", x => FailLaterTaskOfT()));
    }

    // ThenThrows has no check-fn overload (TEx is explicit, so TIgnored cannot be inferred); checks go in a following Then
    [Fact]
    public async Task Then_after_ThenThrows()
    {
        var thrown = new Scenario().Given(() => 1).When(x => throw new InvalidOperationException()).ThenThrows<InvalidOperationException>();
        await AssertAwaited(thrown.Then(t => FailLaterValueTaskOfT()));
        await AssertAwaited(thrown.Then("d", t => FailLaterValueTaskOfT()));
        await AssertAwaited(thrown.Then(t => FailLaterTaskOfT()));
        await AssertAwaited(thrown.Assert(t => FailLaterValueTaskOfT()));
    }

    [Fact]
    public async Task Sync_lambdas_still_pick_Action()
    {
        var log = new List<string>();

        await new Scenario().Given(() => 1)
            .Then(x => log.Add("void"))          // returns void
            .Then(x => log.Remove("nothing"))     // returns bool: Action applies and the result is discarded
            .Assert(x => log.Add("assert"))
            .RunAsync();

        Assert.Equal(["void", "assert"], log);
    }

    [Fact]
    public async Task Code_is_recorded_for_generic_awaitable_overloads()
    {
        var result = await new Scenario().Given(() => 1)
            .Then("d", x => new ValueTask<int>(x))
            .Assert(x => new ValueTask<int>(x))
            .RunAsync();

        Assert.Equal(["x => new ValueTask<int>(x)", "x => new ValueTask<int>(x)"], result.Steps.Skip(1).Select(s => s.Step.Code));
    }
}
