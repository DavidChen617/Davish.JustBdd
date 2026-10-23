using System.Runtime.CompilerServices;

namespace Davish.JustBdd.Tests;

public class ResultTests
{
    private static int Line([CallerLineNumber] int line = 0) => line;

    [Fact]
    public async Task RunAsync_returns_all_steps_with_description_and_code()
    {
        var result = await new Scenario("Rename")
            .Given("A user exists", () => new User("David"))
            .When(user => user.Rename("Davish"))
            .Then("The name is updated", user => Assert.Equal("Davish", user.Name))
            .RunAsync();

        Assert.Equal("Rename", result.Description);
        Assert.True(result.Passed);
        Assert.Collection(result.Steps,
            s => Assert.Equal((StepKind.Given, "A user exists", "() => new User(\"David\")", StepStatus.Passed),
                              (s.Step.Kind, s.Step.Description, s.Step.Code, s.Status)),
            s => Assert.Equal((StepKind.When, (string?)null, "user => user.Rename(\"Davish\")", StepStatus.Passed),
                              (s.Step.Kind, s.Step.Description, s.Step.Code, s.Status)),
            s => Assert.Equal((StepKind.Then, "The name is updated", "user => Assert.Equal(\"Davish\", user.Name)", StepStatus.Passed),
                              (s.Step.Kind, s.Step.Description, s.Step.Code, s.Status)));
    }

    [Fact]
    public async Task Awaiting_the_stage_returns_the_result()
    {
        ScenarioResult result = await new Scenario().Then(() => { }).RunAsync();

        Assert.Equal(nameof(Awaiting_the_stage_returns_the_result), result.Description);
        Assert.Single(result.Steps);
    }

    [Fact]
    public async Task Scenario_records_file_and_line()
    {
        var (scenario, line) = (new Scenario(), Line());

        var result = await scenario.Then(() => { }).RunAsync();

        Assert.EndsWith("ResultTests.cs", result.FilePath);
        Assert.Equal(line, result.LineNumber);
    }

    [Fact]
    public async Task Multiline_lambda_code_is_captured()
    {
        var result = await new Scenario()
            .Given(() =>
            {
                var user = new User("David");
                return user;
            })
            .Then(_ => { })
            .RunAsync();

        Assert.Contains("var user = new User(\"David\");", result.Steps[0].Step.Code);
    }

    [Fact]
    public async Task Failure_attaches_result_to_exception()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario("S")
                .Given("g", () => 1)
                .When("Breaks", x => Fail())
                .Then("Will not run", _ => { })
                .Then(_ => { }).RunAsync());

        var result = Assert.IsType<ScenarioResult>(ex.Data[Scenario.ResultDataKey]);
        Assert.False(result.Passed);
        Assert.Equal(
            [StepStatus.Passed, StepStatus.Failed, StepStatus.NotRun, StepStatus.NotRun],
            result.Steps.Select(s => s.Status));
        Assert.Same(ex, result.Steps[1].Error);
        Assert.Equal("Scenario 'S' > When 'Breaks'", ex.Data[Scenario.StepDataKey]);
    }

    [Fact]
    public async Task Step_path_uses_code_when_description_missing()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario("S").Then(() => Fail()).RunAsync());

        Assert.Equal("Scenario 'S' > Then '() => Fail()'", ex.Data[Scenario.StepDataKey]);
    }

    private static int Fail() => throw new InvalidOperationException();
}
