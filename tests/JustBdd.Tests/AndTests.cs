namespace Davish.JustBdd.Tests;

/// <summary>And follows a Then: it checks the same state and is recorded as StepKind.And.</summary>
public class AndTests
{
    private static async Task FailLater()
    {
        await Task.Yield();
        throw new InvalidOperationException();
    }

    private static async ValueTask<int> FailLaterOfT()
    {
        await Task.Yield();
        throw new InvalidOperationException();
    }

    [Fact]
    public void And_checks_the_same_state_and_is_recorded_as_And()
    {
        var seen = new List<int>();

        var result = new Scenario()
            .Given(() => 1)
            .Then("Is one", x => seen.Add(x))
            .And("Still one", x => seen.Add(x))
            .And(x => seen.Add(x))
            .Run();

        Assert.Equal([1, 1, 1], seen);
        Assert.Equal(
            [(StepKind.Given, null, "() => 1"),
             (StepKind.Then, "Is one", "x => seen.Add(x)"),
             (StepKind.And, "Still one", "x => seen.Add(x)"),
             (StepKind.And, (string?)null, "x => seen.Add(x)")],
            result.Steps.Select(s => (s.Step.Kind, s.Step.Description, s.Step.Code)));
    }

    [Fact]
    public void Description_only_And_is_followed_by_assertions()
    {
        var result = new Scenario()
            .Given(() => 2)
            .Then("Is two", x => Assert.Equal(2, x))
            .And("Is positive")
            .Assert(x => Assert.True(x > 0))
            .Run();

        Assert.Equal([StepKind.Given, StepKind.Then, StepKind.And, StepKind.Assert], result.Steps.Select(s => s.Step.Kind));
        Assert.Equal(("Is positive", ""), (result.Steps[2].Step.Description, result.Steps[2].Step.Code));
    }

    [Fact]
    public void Failing_And_reports_an_And_step_path_and_stops_the_rest()
    {
        var ex = Assert.Throws<Xunit.Sdk.EqualException>(() =>
            new Scenario("S")
                .Given(() => 1)
                .Then(x => { })
                .And("Equals 2", x => Assert.Equal(2, x))
                .And(x => { })
                .Run());

        Assert.Equal("Scenario 'S' > And 'Equals 2'", ex.Data[Scenario.StepDataKey]);
        var result = Assert.IsType<ScenarioResult>(ex.Data[Scenario.ResultDataKey]);
        Assert.Equal([StepStatus.Passed, StepStatus.Passed, StepStatus.Failed, StepStatus.NotRun], result.Steps.Select(s => s.Status));
    }

    [Fact]
    public void And_after_ThenThrows_receives_Thrown()
    {
        string? message = null;

        new Scenario()
            .Given(() => 1)
            .When(x => { if (x == 1) throw new ArgumentException("bad"); })
            .ThenThrows<ArgumentException>()
            .And(t => message = t.Exception.Message)
            .Run();

        Assert.Equal("bad", message);
    }

    [Fact]
    public void And_can_be_followed_by_the_next_When()
    {
        var observed = 0;

        var result = new Scenario()
            .Given(() => 1)
            .Then(x => { })
            .And(x => { })
            .When(x => x + 1)
            .Then(x => observed = x)
            .Run();

        Assert.Equal(2, observed);
        Assert.Equal([StepKind.Given, StepKind.Then, StepKind.And, StepKind.When, StepKind.Then], result.Steps.Select(s => s.Step.Kind));
    }

    [Fact]
    public async Task Async_And_overloads_are_awaited()
    {
        var stage = new Scenario().Given(() => 1).Then(x => { });

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await stage.And(async x => await FailLater()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await stage.And(x => FailLater()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await stage.And(x => new ValueTask(FailLater())).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await stage.And(x => FailLaterOfT()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await stage.And("desc", async x => await FailLater()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await stage.And("desc", x => new ValueTask(FailLater())).RunAsync());
    }
}
