namespace Davish.JustBdd.Tests;

/// <summary>ContinueOnFailure() keeps running the consecutive checks of a segment after one fails.</summary>
public class ContinueOnFailureTests
{
    private static StepStatus[] Statuses(Exception ex)
        => Assert.IsType<ScenarioResult>(ex.Data[Scenario.ResultDataKey]).Steps.Select(s => s.Status).ToArray();

    [Fact]
    public void Without_the_option_the_first_failing_check_stops_the_rest()
    {
        var ex = Assert.Throws<EqualException>(() =>
            new Scenario("S").Given(() => 1)
                .Then("Equals 2", x => Assert.Equal(2, x))
                .And("Equals 3", x => Assert.Equal(3, x))
                .Run());

        Assert.Equal([StepStatus.Passed, StepStatus.Failed, StepStatus.NotRun], Statuses(ex));
    }

    [Fact]
    public void Several_failing_checks_throw_ScenarioFailuresException_in_step_order()
    {
        var ex = Assert.Throws<ScenarioFailuresException>(() =>
            new Scenario("S").ContinueOnFailure()
                .Given(() => 1)
                .Then("Equals 2", x => Assert.Equal(2, x))
                .And("Is positive", x => Assert.True(x > 0))
                .And("Equals 3", x => throw new InvalidOperationException("three"))
                .Assert(x => Assert.Equal(1, x))
                .Run());

        Assert.Collection(ex.InnerExceptions,
            e => Assert.Equal("Scenario 'S' > Then 'Equals 2'", Assert.IsType<Xunit.Sdk.EqualException>(e).Data[Scenario.StepDataKey]),
            e => Assert.Equal("Scenario 'S' > And 'Equals 3'", Assert.IsType<InvalidOperationException>(e).Data[Scenario.StepDataKey]));
        Assert.Contains("Then 'Equals 2'", ex.Message);
        Assert.Contains("And 'Equals 3'", ex.Message);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(ex.Message, "three"));   // AggregateException would append inner messages again.
        Assert.Equal([StepStatus.Passed, StepStatus.Failed, StepStatus.Passed, StepStatus.Failed, StepStatus.Passed], Statuses(ex));
    }

    [Fact]
    public void A_single_failure_throws_the_original_exception_after_the_other_checks_run()
    {
        var ran = false;

        var ex = Assert.Throws<EqualException>(() =>
            new Scenario("S").ContinueOnFailure()
                .Given(() => 1)
                .Then(x => Assert.Equal(2, x))
                .And(x => { ran = true; })
                .Run());

        Assert.True(ran);
        Assert.Equal([StepStatus.Passed, StepStatus.Failed, StepStatus.Passed], Statuses(ex));
    }

    [Fact]
    public void Given_and_When_failures_still_stop_the_scenario()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new Scenario().ContinueOnFailure()
                .Given(() => 1)
                .When(x => throw new InvalidOperationException())
                .Then(x => { })
                .And(x => { })
                .Run());

        Assert.Equal([StepStatus.Passed, StepStatus.Failed, StepStatus.NotRun, StepStatus.NotRun], Statuses(ex));
    }

    [Fact]
    public void Failures_stop_at_the_segment_boundary()
    {
        var secondWhenRan = false;

        var ex = Assert.Throws<ScenarioFailuresException>(() =>
            new Scenario().ContinueOnFailure()
                .Given(() => 1)
                .Then(x => Assert.Equal(2, x))
                .And(x => Assert.Equal(3, x))
                .When(x => { secondWhenRan = true; })
                .Then(x => { })
                .Run());

        Assert.False(secondWhenRan);
        Assert.Equal([StepStatus.Passed, StepStatus.Failed, StepStatus.Failed, StepStatus.NotRun, StepStatus.NotRun], Statuses(ex));
    }

    [Fact]
    public void A_failing_ThenThrows_stops_because_no_state_is_produced()
    {
        var ex = Assert.Throws<ScenarioExpectationException>(() =>
            new Scenario().ContinueOnFailure()
                .Given(() => 1)
                .When(x => x)
                .ThenThrows<InvalidOperationException>()
                .Then(t => { })
                .Run());

        Assert.Equal([StepStatus.Passed, StepStatus.Passed, StepStatus.Failed, StepStatus.NotRun], Statuses(ex));
    }

    [Fact]
    public async Task Async_checks_are_collected_by_RunAsync()
    {
        static async Task FailLater(string message)
        {
            await Task.Yield();
            throw new InvalidOperationException(message);
        }

        var ex = await Assert.ThrowsAsync<ScenarioFailuresException>(async () =>
            await new Scenario().ContinueOnFailure()
                .Given(() => 1)
                .Then(async x => await FailLater("a"))
                .And(async x => await FailLater("b"))
                .RunAsync());

        Assert.Equal(["a", "b"], ex.InnerExceptions.Select(e => e.Message));
    }

    [Fact]
    public void The_option_returns_a_new_scenario_and_keeps_its_metadata()
    {
        var plain = new Scenario("S");
        var continuing = plain.ContinueOnFailure();

        Assert.NotSame(plain, continuing);
        Assert.Equal((plain.Description, plain.FilePath, plain.LineNumber), (continuing.Description, continuing.FilePath, continuing.LineNumber));
        Assert.Throws<EqualException>(() =>
            plain.Given(() => 1).Then(x => Assert.Equal(2, x)).And(x => Assert.Equal(3, x)).Run());
    }

    [Fact]
    public void The_option_does_not_apply_to_nested_scenarios()
    {
        var innerSecondRan = false;

        var ex = Assert.Throws<EqualException>(() =>
            new Scenario("Outer").ContinueOnFailure()
                .Given(() => 1)
                .Then("Run inner", x =>
                {
                    new Scenario("Inner").Given(() => 1)
                        .Then(y => Assert.Equal(2, y))
                        .And(y => { innerSecondRan = true; })
                        .Run();
                })
                .Run());

        Assert.False(innerSecondRan);
        Assert.Equal("Scenario 'Inner' > Then 'y => Assert.Equal(2, y)'", ex.Data[Scenario.StepDataKey]);
    }
}
