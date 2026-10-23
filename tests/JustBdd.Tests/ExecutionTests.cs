namespace Davish.JustBdd.Tests;

public class LazyTests
{
    [Fact]
    public void Building_the_chain_runs_nothing()
    {
        var calls = 0;

        _ = new Scenario("lazy")
            .Given("g", () => ++calls)
            .When("w", x => ++calls)
            .Then("t", _ => calls++);

        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task Awaiting_twice_reruns_from_given()
    {
        var givens = 0;
        var stage = new Scenario("rerun")
            .Given("g", () => ++givens)
            .Then("t", _ => { });

        await stage.RunAsync();
        await stage.RunAsync();

        Assert.Equal(2, givens);
    }

    [Fact]
    public async Task Branches_from_same_stage_do_not_affect_each_other()
    {
        var log = new List<string>();
        var given = new Scenario("branch").Given("g", () => "x");
        var left = given.Then("left", _ => log.Add("left"));
        var right = given.Then("right", _ => log.Add("right"));
        var leftMore = left.Then("left2", _ => log.Add("left2"));

        await right.RunAsync();
        await left.RunAsync();
        await leftMore.RunAsync();

        Assert.Equal(["right", "left", "left", "left2"], log);
    }
}

public class FailureTests
{
    private static string? StepOf(Exception ex) => ex.Data[Scenario.StepDataKey] as string;

    [Fact]
    public async Task Given_failure_throws_original_exception_with_step_path()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario("S")
                .Given("Broken Given", () => ThrowsInvalid<int>())
                .Then("t", _ => { }).RunAsync());

        Assert.Equal("Scenario 'S' > Given 'Broken Given'", StepOf(ex));
    }

    [Fact]
    public async Task When_failure_records_when_step()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario("S")
                .Given("g", () => 1)
                .When("Broken When", x => ThrowsInvalid<int>())
                .Then("t", _ => { }).RunAsync());

        Assert.Equal("Scenario 'S' > When 'Broken When'", StepOf(ex));
    }

    [Fact]
    public async Task Async_failure_records_step()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario("S")
                .When("w", async () => { await Task.Yield(); throw new InvalidOperationException(); })
                .Then("t", _ => { }).RunAsync());

        Assert.Equal("Scenario 'S' > When 'w'", StepOf(ex));
    }

    [Fact]
    public async Task Assertion_exception_is_not_wrapped()
    {
        var ex = await Assert.ThrowsAsync<Xunit.Sdk.EqualException>(async () =>
            await new Scenario("S")
                .Given("g", () => "David")
                .Then("The name is Davish", name => Assert.Equal("Davish", name)).RunAsync());

        Assert.Equal("Scenario 'S' > Then 'The name is Davish'", StepOf(ex));
    }

    [Fact]
    public async Task First_failing_Then_stops_the_rest()
    {
        var reached = false;

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario("S")
                .Given("g", () => 1)
                .Then("ok", _ => { })
                .Then("Breaks", _ => throw new InvalidOperationException())
                .Then("Should not run", _ => reached = true).RunAsync());

        Assert.False(reached);
        Assert.Equal("Scenario 'S' > Then 'Breaks'", StepOf(ex));
    }

    private static T ThrowsInvalid<T>() => throw new InvalidOperationException();
}
