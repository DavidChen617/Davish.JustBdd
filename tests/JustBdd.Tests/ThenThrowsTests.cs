namespace Davish.JustBdd.Tests;

public class ThenThrowsTests
{
    private sealed class Account(string name)
    {
        public string Name { get; private set; } = name;

        public void Rename(string? name) => Name = name ?? throw new ArgumentNullException(nameof(name));

        public Account Renamed(string? name) => new(name ?? throw new ArgumentNullException(nameof(name)));

        public async Task RenameAsync(string? name)
        {
            await Task.Yield();
            Rename(name);
        }

        public async ValueTask RenameValueTaskAsync(string? name)
        {
            await Task.Yield();
            Rename(name);
        }
    }

    private static string? StepOf(Exception ex) => ex.Data[Scenario.StepDataKey] as string;

    [Fact]
    public async Task Expected_exception_flows_with_state()
    {
        Account? given = null;
        Thrown<Account, ArgumentNullException>? seen = null;

        await new Scenario("S")
            .Given(() => given = new Account("David"))
            .When("Rename to null", a => a.Rename(null))
            .ThenThrows<ArgumentNullException>("Rejects an empty name")
            .Then("The name is unchanged", t => seen = t);

        Assert.NotNull(seen);
        Assert.Same(given, seen.State);
        Assert.Equal("David", seen.State.Name);
        Assert.Equal("name", seen.Exception.ParamName);
    }

    [Fact]
    public async Task Value_returning_When_passes_its_input_as_state()
    {
        Account? given = null;
        Account? state = null;

        await new Scenario()
            .Given(() => given = new Account("David"))
            .When(a => a.Renamed(null))
            .ThenThrows<ArgumentNullException>()
            .Then(t => state = t.State);

        Assert.Same(given, state);
    }

    [Fact]
    public async Task Scenario_When_state_is_Unit()
    {
        Unit? state = null;

        await new Scenario()
            .When(() => throw new InvalidOperationException())
            .ThenThrows<InvalidOperationException>("Throw")
            .Then(t => state = t.State);

        Assert.Equal(default(Unit), state);
    }

    [Fact]
    public async Task Subclass_of_expected_exception_is_caught()
    {
        Exception? caught = null;

        await new Scenario()
            .Given(() => new Account("David"))
            .When(a => a.Rename(null))
            .ThenThrows<ArgumentException>()
            .Then(t => caught = t.Exception);

        Assert.IsType<ArgumentNullException>(caught);
    }

    [Fact]
    public async Task Other_exception_type_is_rethrown_with_when_step_path()
    {
        var ex = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await new Scenario("S")
                .Given(() => new Account("David"))
                .When("Rename to null", a => a.Rename(null))
                .ThenThrows<InvalidOperationException>("Expect a different exception"));

        Assert.Equal("Scenario 'S' > When 'Rename to null'", StepOf(ex));
        var result = Assert.IsType<ScenarioResult>(ex.Data[Scenario.ResultDataKey]);
        Assert.Equal([StepStatus.Passed, StepStatus.Failed, StepStatus.NotRun], result.Steps.Select(s => s.Status));
    }

    [Fact]
    public async Task No_exception_fails_the_ThenThrows_step()
    {
        var ex = await Assert.ThrowsAsync<ScenarioExpectationException>(async () =>
            await new Scenario("S")
                .Given(() => new Account("David"))
                .When("Rename normally", a => a.Rename("Davish"))
                .ThenThrows<ArgumentNullException>("Rejects an empty name")
                .Then(_ => { }));

        Assert.Equal("Scenario 'S' > Then 'Rejects an empty name'", StepOf(ex));
        Assert.Contains(nameof(ArgumentNullException), ex.Message);
        var result = Assert.IsType<ScenarioResult>(ex.Data[Scenario.ResultDataKey]);
        Assert.Equal(
            [StepStatus.Passed, StepStatus.Passed, StepStatus.Failed, StepStatus.NotRun],
            result.Steps.Select(s => s.Status));
    }

    [Fact]
    public async Task Async_When_exceptions_are_caught()
    {
        var given = new Scenario().Given(() => new Account("David"));

        await given.When(a => a.RenameAsync(null)).ThenThrows<ArgumentNullException>();
        await given.When(a => a.RenameValueTaskAsync(null)).ThenThrows<ArgumentNullException>();
        await given.When(async a => { await Task.Yield(); return a.Renamed(null); }).ThenThrows<ArgumentNullException>();
    }

    [Fact]
    public async Task Async_checks_after_ThenThrows_are_awaited()
    {
        var thrown = new Scenario().Given(() => new Account("David")).When(a => a.Rename(null)).ThenThrows<ArgumentNullException>("t");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await thrown.Then(async t => { await Task.Yield(); throw new InvalidOperationException(); }));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await thrown.Then(t => FailLaterValueTask()));
    }

    [Fact]
    public async Task Result_records_when_passed_and_ThenThrows_code()
    {
        var result = await new Scenario()
            .Given(() => new Account("David"))
            .When("Rename to null", a => a.Rename(null))
            .ThenThrows<ArgumentNullException>("Rejects an empty name")
            .Then(t => { });

        Assert.Equal(
            [(StepKind.Given, StepStatus.Passed, (string?)null, "() => new Account(\"David\")"),
             (StepKind.When, StepStatus.ThrewAsExpected, "Rename to null", "a => a.Rename(null)"),
             (StepKind.Then, StepStatus.Passed, "Rejects an empty name", "ThenThrows<ArgumentNullException>()")],
            result.Steps.Take(3).Select(s => (s.Step.Kind, s.Status, s.Step.Description, s.Step.Code)));
    }

    [Fact]
    public async Task Non_generic_ThenThrows_catches_any_exception()
    {
        Exception? caught = null;

        await new Scenario()
            .Given(() => new Account("David"))
            .When(a => a.Rename(null))
            .ThenThrows("Any exception")
            .Then(t => caught = t.Exception);

        Assert.IsType<ArgumentNullException>(caught);
    }

    [Fact]
    public async Task ThenThrows_chains_into_assertions()
    {
        var result = await new Scenario()
            .Given(() => new Account("David"))
            .When(a => a.Rename(null))
            .ThenThrows<ArgumentNullException>()
            .Assert(t => Assert.Equal("David", t.State.Name))
            .True(t => t.Exception.ParamName == "name");

        Assert.Equal(
            ["() => new Account(\"David\")", "a => a.Rename(null)", "ThenThrows<ArgumentNullException>()",
             "t => Assert.Equal(\"David\", t.State.Name)", "True(t => t.Exception.ParamName == \"name\")"],
            result.Steps.Select(s => s.Step.Code));
    }

    [Fact]
    public async Task ThrewAsExpected_records_the_expected_exception_and_counts_as_passed()
    {
        Exception? caught = null;

        var result = await new Scenario()
            .Given(() => new Account("David"))
            .When(a => a.Rename(null))
            .ThenThrows<ArgumentNullException>()
            .Then(t => caught = t.Exception);

        var when = result.Steps[1];
        Assert.Equal(StepStatus.ThrewAsExpected, when.Status);
        Assert.Same(caught, when.Error);
        Assert.True(result.Passed);
    }

    private static async ValueTask FailLaterValueTask()
    {
        await Task.Yield();
        throw new InvalidOperationException();
    }
}
