namespace Davish.JustBdd.Tests;

/// <summary>Given(scenario) runs another scenario as the Given step and continues with its final state.</summary>
public class GivenScenarioTests
{
    private static int _prepareRuns;

    private static ThenStage<User> RenamedUser() =>
        new Scenario("Prepare a renamed user")
            .Given(() => { _prepareRuns++; return new User("David"); })
            .When(u => u.Rename("Davish"))
            .Then(u => Assert.Equal("Davish", u.Name));

    [Fact]
    public void The_inner_final_state_becomes_the_outer_state()
    {
        string? observed = null;

        var result = new Scenario("Outer")
            .Given("A renamed user", RenamedUser())
            .When(u => u.RenameInPlace(u.Name + "!"))
            .Then(u => observed = u.Name)
            .Run();

        Assert.Equal("Davish!", observed);
        Assert.Equal((StepKind.Given, "A renamed user", "RenamedUser()"),
            (result.Steps[0].Step.Kind, result.Steps[0].Step.Description, result.Steps[0].Step.Code));
    }

    [Fact]
    public void Inner_steps_are_collected_under_the_Given_step()
    {
        var result = new Scenario("Outer").Given(RenamedUser()).Then(u => { }).Run();

        var inner = Assert.Single(result.Steps[0].Children);
        Assert.Equal("Prepare a renamed user", inner.Description);
        Assert.Equal([StepKind.Given, StepKind.When, StepKind.Then], inner.Steps.Select(s => s.Step.Kind));
    }

    [Fact]
    public void The_inner_scenario_runs_lazily_on_every_outer_run()
    {
        var before = _prepareRuns;
        var outer = new Scenario().Given(RenamedUser()).Then(u => { });
        Assert.Equal(before, _prepareRuns);

        outer.Run();
        outer.Run();

        Assert.Equal(before + 2, _prepareRuns);
    }

    [Fact]
    public void An_inner_failure_fails_the_Given_step()
    {
        var failing = new Scenario("Inner").Given(() => 1).Then("Equals 2", x => Assert.Equal(2, x));

        var ex = Assert.Throws<Xunit.Sdk.EqualException>(() =>
            new Scenario("Outer").Given("Prepared", failing).When(x => x).Then(x => { }).Run());

        Assert.Equal("Scenario 'Inner' > Then 'Equals 2'", ex.Data[Scenario.StepDataKey]);
        var result = Assert.IsType<ScenarioResult>(ex.Data[Scenario.ResultDataKey]);
        Assert.Equal("Outer", result.Description);
        Assert.Equal([StepStatus.Failed, StepStatus.NotRun, StepStatus.NotRun], result.Steps.Select(s => s.Status));
    }

    [Fact]
    public async Task Async_inner_steps_run_with_RunAsync()
    {
        static async Task<int> Later(int x)
        {
            await Task.Yield();
            return x;
        }

        var inner = new Scenario().Given(async () => await Later(1)).When(async x => await Later(x + 1)).Then(x => { });
        var observed = 0;

        await new Scenario().Given(inner).Then(x => observed = x).RunAsync();

        Assert.Equal(2, observed);
    }

    [Fact]
    public void Inner_scenarios_can_end_with_ThenThrows_or_produce_no_state()
    {
        var thrown = new Scenario().Given(() => 1).When(x => { if (x == 1) throw new InvalidOperationException("bad"); }).ThenThrows<InvalidOperationException>();
        string? message = null;
        new Scenario().Given(thrown).Then(t => message = t.Exception.Message).Run();
        Assert.Equal("bad", message);

        var log = new List<string>();
        var stateless = new Scenario().Given(() => { log.Add("inner"); }).Then(() => { });
        new Scenario().Given(stateless).When(() => { log.Add("outer"); }).Then(() => { }).Run();
        Assert.Equal(["inner", "outer"], log);
    }
}
