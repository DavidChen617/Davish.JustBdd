namespace Davish.JustBdd.Tests;

/// <summary>Stages whose state is Unit accept parameterless When, Then, and And.</summary>
public class UnitStageTests
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

    private static async Task<int> Later(int x)
    {
        await Task.Yield();
        return x;
    }

    [Fact]
    public void Form_basic_chain_runs_without_parameters()
    {
        var log = new List<string>();

        var result = new Scenario()
            .Given(() => { log.Add("given"); })
            .When(() => { log.Add("when"); })
            .Then(() => { log.Add("then"); })
            .Run();

        Assert.Equal(["given", "when", "then"], log);
        Assert.Equal(
            [(StepKind.Given, "() => { log.Add(\"given\"); }"),
             (StepKind.When, "() => { log.Add(\"when\"); }"),
             (StepKind.Then, "() => { log.Add(\"then\"); }")],
            result.Steps.Select(s => (s.Step.Kind, s.Step.Code)));
    }

    [Fact]
    public void Described_parameterless_steps_on_every_unit_stage()
    {
        var log = new List<string>();

        var result = new Scenario()
            .Given("g", () => { log.Add("g"); })
            .Then("t1", () => log.Add("t1"))                // GivenStage<Unit>.Then
            .And("a", () => log.Add("a"))                   // ThenStage<Unit>.And
            .Then("t2", () => log.Add("t2"))                // ThenStage<Unit>.Then
            .When("w", () => { log.Add("w"); })             // ThenStage<Unit>.When
            .Then("t3", () => log.Add("t3"))                // WhenStage<Unit, Unit>.Then
            .Run();

        Assert.Equal(["g", "t1", "a", "t2", "w", "t3"], log);
        Assert.Equal(
            [StepKind.Given, StepKind.Then, StepKind.And, StepKind.Then, StepKind.When, StepKind.Then],
            result.Steps.Select(s => s.Step.Kind));
        Assert.Equal(["g", "t1", "a", "t2", "w", "t3"], result.Steps.Select(s => s.Step.Description));
    }

    [Fact]
    public void Value_returning_parameterless_When_produces_state()
    {
        var observed = 0;

        new Scenario().Given(() => { }).When(() => 5).Then(x => observed = x).Run();
        Assert.Equal(5, observed);

        new Scenario().Then(() => { }).When("value", () => 7).Then(x => observed = x).Run();
        Assert.Equal(7, observed);
    }

    [Fact]
    public void Scenario_When_without_state_is_followed_by_a_parameterless_Then()
    {
        var log = new List<string>();

        new Scenario().When(() => log.Add("when")).Then(() => log.Add("then")).And(() => log.Add("and")).Run();

        Assert.Equal(["when", "then", "and"], log);
    }

    [Fact]
    public async Task Async_parameterless_Then_and_And_are_awaited()
    {
        var given = new Scenario().Given(() => { });
        var then = new Scenario().Then(() => { });
        var when = new Scenario().When(() => { });

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await given.Then(async () => await FailLater()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await given.Then(() => FailLater()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await given.Then(() => new ValueTask(FailLater())).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await given.Then(() => FailLaterOfT()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await when.Then("desc", async () => await FailLater()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await when.Then(() => FailLaterOfT()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await then.Then(async () => await FailLater()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await then.And(async () => await FailLater()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await then.And("desc", () => new ValueTask(FailLater())).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await then.And(() => FailLaterOfT()).RunAsync());
    }

    [Fact]
    public async Task Async_parameterless_When_is_awaited_and_produces_state()
    {
        var observed = new List<int>();

        await new Scenario().Given(() => { }).When(async () => await Later(1)).Then(x => observed.Add(x)).RunAsync();
        await new Scenario().Given(() => { }).When(() => new ValueTask<int>(Later(2))).Then(x => observed.Add(x)).RunAsync();
        await new Scenario().Then(() => { }).When("desc", async () => await Later(3)).Then(x => observed.Add(x)).RunAsync();
        Assert.Equal([1, 2, 3], observed);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario().Given(() => { }).When(async () => await FailLater()).Then(() => { }).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario().Then(() => { }).When(() => new ValueTask(FailLater())).Then(() => { }).RunAsync());
    }

    private static void Check() { }

    [Fact]
    public void Parameterless_method_groups_are_accepted()
    {
        var result = new Scenario().Given(Check).When(Check).Then(Check).And(Check).Run();

        Assert.Equal(["Check", "Check", "Check", "Check"], result.Steps.Select(s => s.Step.Code));
    }

    [Fact]
    public void Failing_parameterless_step_reports_its_path()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            new Scenario("S").Given(() => { }).Then("Breaks", () => throw new InvalidOperationException()).Run());

        Assert.Equal("Scenario 'S' > Then 'Breaks'", ex.Data[Scenario.StepDataKey]);
    }
}
