namespace Davish.JustBdd.Tests;

/// <summary>Scenarios run inside a step, passed or failed, are collected into that step's <see cref="StepResult.Children"/>.</summary>
public class NestedScenarioTests
{
    private static ValueTask<ScenarioResult> Inner(string name, bool fail = false)
        => new Scenario(name)
            .Given("Inner Given", () => 1)
            .Then("Inner Then", x => { if (fail) throw new InvalidOperationException(name); })
            .RunAsync();

    private static ScenarioResult ResultOf(Exception ex) => Assert.IsType<ScenarioResult>(ex.Data[Scenario.ResultDataKey]);

    [Fact]
    public async Task Passing_inner_scenario_is_collected()
    {
        var outer = await new Scenario("Outer")
            .Given("g", () => 1)
            .Then("Run inner", async x => { await Inner("Inner"); })
            .Then("No inner", x => { });

        var child = Assert.Single(outer.Steps[1].Children);
        Assert.Equal("Inner", child.Description);
        Assert.True(child.Passed);
        Assert.Equal(["Inner Given", "Inner Then"], child.Steps.Select(s => s.Step.Description));
        Assert.Empty(outer.Steps[0].Children);
        Assert.Empty(outer.Steps[2].Children);
    }

    [Fact]
    public async Task Failing_inner_scenario_is_collected_under_the_failed_outer_step()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario("Outer")
                .Given("g", () => 1)
                .Then("Run inner", async x => { await Inner("Inner", fail: true); })
                .Then("Afterwards", x => { }));

        var outer = ResultOf(ex);
        Assert.Equal("Outer", outer.Description);
        Assert.Equal([StepStatus.Passed, StepStatus.Failed, StepStatus.NotRun], outer.Steps.Select(s => s.Status));

        var child = Assert.Single(outer.Steps[1].Children);
        Assert.Equal([StepStatus.Passed, StepStatus.Failed], child.Steps.Select(s => s.Status));
        Assert.Same(ex, child.Steps[1].Error);
    }

    [Fact]
    public async Task Several_inner_scenarios_in_one_step_are_all_collected()
    {
        var outer = await new Scenario("Outer")
            .Given("g", () => 1)
            .Then("Run two at once", async x => { await Task.WhenAll(Inner("A").AsTask(), Inner("B").AsTask()); });

        Assert.Equal(["A", "B"], outer.Steps[1].Children.Select(c => c.Description).OrderBy(d => d));
    }

    [Fact]
    public async Task Three_levels_form_a_tree()
    {
        var outer = await new Scenario("L1")
            .Given("g", () => 1)
            .Then("L1 step", async x =>
            {
                await new Scenario("L2")
                    .Given("g", () => 1)
                    .Then("L2 step", async y => { await Inner("L3"); });
            });

        var l2 = Assert.Single(outer.Steps[1].Children);
        var l3 = Assert.Single(l2.Steps[1].Children);
        Assert.Equal(("L2", "L3"), (l2.Description, l3.Description));
        Assert.Empty(l3.Steps[1].Children);
    }

    [Fact]
    public async Task Inner_scenarios_in_given_and_when_are_collected()
    {
        var outer = await new Scenario("Outer")
            .Given("g", async () => { await Inner("FromGiven"); return 1; })
            .When("w", async x => { await Inner("FromWhen"); return x; })
            .Then("t", x => { });

        Assert.Equal(["FromGiven"], outer.Steps[0].Children.Select(c => c.Description));
        Assert.Equal(["FromWhen"], outer.Steps[1].Children.Select(c => c.Description));
    }

    [Fact]
    public async Task ThenThrows_keeps_the_inner_scenario_of_the_throwing_when()
    {
        var outer = await new Scenario("Outer")
            .Given("g", () => 1)
            .When("w", async x => { await Inner("Inner", fail: true); })
            .ThenThrows<InvalidOperationException>("Expect the inner to fail");

        var child = Assert.Single(outer.Steps[1].Children);
        Assert.False(child.Passed);
    }

    [Fact(Timeout = 5000)]
    public async Task Concurrent_outer_scenarios_do_not_share_children()
    {
        var gate = new TaskCompletionSource();
        var arrived = 0;

        ValueTask<ScenarioResult> Outer(string name) => new Scenario(name)
            .Given("g", () => 1)
            .Then("t", async x =>
            {
                if (Interlocked.Increment(ref arrived) == 2) gate.SetResult();
                await gate.Task.WaitAsync(TestContext.Current.CancellationToken);   // Release only after both outer scenarios reach this point, so they really interleave
                await Inner(name + "-inner");
            })
            .RunAsync();

        var results = await Task.WhenAll(Outer("A").AsTask(), Outer("B").AsTask());

        Assert.Equal(["A-inner"], results[0].Steps[1].Children.Select(c => c.Description));
        Assert.Equal(["B-inner"], results[1].Steps[1].Children.Select(c => c.Description));
    }

    [Fact]
    public async Task Top_level_scenario_is_not_collected_anywhere()
    {
        var result = await Inner("Top");

        Assert.True(result.Passed);
        Assert.All(result.Steps, s => Assert.Empty(s.Children));
    }
}
