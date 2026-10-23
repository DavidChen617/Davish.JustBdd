using System.Runtime.CompilerServices;

namespace Davish.JustBdd.Tests;

/// <summary>Simulates an integration package that extends only through ThenStage&lt;T&gt;.AddAssertion.</summary>
internal static class FakeAssertions
{
    public static ThenStage<T> True<T>(this ThenStage<T> a, string desc, Func<T, bool> actual,
        [CallerArgumentExpression(nameof(actual))] string actualCode = "")
        => a.AddAssertion(x => { Assert.True(actual(x), desc); return default; }, desc, $"True({actualCode})");

    public static ThenStage<T> True<T>(this ThenStage<T> a, Func<T, bool> actual,
        [CallerArgumentExpression(nameof(actual))] string actualCode = "")
        => a.AddAssertion(x => { Assert.True(actual(x)); return default; }, null, $"True({actualCode})");
}

public class AssertionExtensionPointTests
{
    private static async Task FailLaterTask()
    {
        await Task.Yield();
        throw new InvalidOperationException();
    }

    private static async ValueTask FailLaterValueTask()
    {
        await Task.Yield();
        throw new InvalidOperationException();
    }

    [Fact]
    public async Task Then_desc_and_Assert_receive_same_state()
    {
        string? observed = null;

        var result = await new Scenario("S")
            .Given(() => new User("David"))
            .When(u => u.Rename("Davish"))
            .Then("The name is updated")
            .Assert(u => observed = u.Name)
            .RunAsync();

        Assert.Equal("Davish", observed);
        Assert.Equal(
            [(StepKind.Given, null, "() => new User(\"David\")"),
             (StepKind.When, null, "u => u.Rename(\"Davish\")"),
             (StepKind.Then, "The name is updated", ""),
             (StepKind.Assert, (string?)null, "u => observed = u.Name")],
            result.Steps.Select(s => (s.Step.Kind, s.Step.Description, s.Step.Code)));
        Assert.All(result.Steps, s => Assert.Equal(StepStatus.Passed, s.Status));
    }

    [Fact]
    public async Task Then_desc_is_available_on_given_when_and_then_stages()
    {
        var log = new List<string>();

        await new Scenario().Given(() => 1).Then("G").Assert(x => log.Add("G")).RunAsync();
        await new Scenario().When(() => 2).Then("W").Assert(x => log.Add("W")).RunAsync();
        await new Scenario().Given(() => 3).Then(x => { }).Then("T").Assert(x => log.Add("T")).RunAsync();
        var onlyDesc = await new Scenario().Given(() => 4).Then("Description only").RunAsync();

        Assert.Equal(["G", "W", "T"], log);
        Assert.Equal(StepStatus.Passed, onlyDesc.Steps[^1].Status);
    }

    [Fact]
    public async Task Scenario_Then_desc_starts_assertion_chain()
    {
        var called = false;

        var result = await new Scenario("S")
            .Then("Then only")
            .Assert(_ => called = true)
            .True(_ => called)
            .RunAsync();

        Assert.True(called);
        Assert.Equal(
            [(StepKind.Then, "Then only", ""),
             (StepKind.Assert, (string?)null, "_ => called = true"),
             (StepKind.Assert, (string?)null, "True(_ => called)")],
            result.Steps.Select(s => (s.Step.Kind, s.Step.Description, s.Step.Code)));
        Assert.All(result.Steps, s => Assert.Equal(StepStatus.Passed, s.Status));
    }

    [Fact]
    public async Task Async_Assert_is_awaited()
    {
        var then = new Scenario().Given(() => 1).Then("t");

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await then.Assert(async x => await FailLaterTask()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await then.Assert(x => FailLaterTask()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await then.Assert(x => FailLaterValueTask()).RunAsync());
    }

    [Fact]
    public async Task Assert_failure_keeps_original_exception_and_marks_rest_not_run()
    {
        var ex = await Assert.ThrowsAsync<Xunit.Sdk.EqualException>(async () =>
            await new Scenario("S")
                .Given(() => "David")
                .Then("Name")
                .Assert(n => Assert.Equal("Davish", n))
                .Assert(n => { }).RunAsync());

        Assert.Equal("Scenario 'S' > Assert 'n => Assert.Equal(\"Davish\", n)'", ex.Data[Scenario.StepDataKey]);
        var result = Assert.IsType<ScenarioResult>(ex.Data[Scenario.ResultDataKey]);
        Assert.Equal(
            [StepStatus.Passed, StepStatus.Passed, StepStatus.Failed, StepStatus.NotRun],
            result.Steps.Select(s => s.Status));
    }

    [Fact]
    public async Task Fluent_assertions_via_AddAssertion_are_recorded_and_awaitable()
    {
        var result = await new Scenario()
            .Given(() => new User("David"))
            .Then("The name is correct")
            .True("Is David", u => u.Name == "David")
            .True(u => u.Name.Length == 5)
            .RunAsync();

        Assert.Equal(
            [(StepKind.Assert, "Is David", "True(u => u.Name == \"David\")"),
             (StepKind.Assert, (string?)null, "True(u => u.Name.Length == 5)")],
            result.Steps.Skip(2).Select(s => (s.Step.Kind, s.Step.Description, s.Step.Code)));
    }

    [Fact]
    public async Task Fluent_failure_uses_description_in_step_path()
    {
        var ex = await Assert.ThrowsAsync<Xunit.Sdk.TrueException>(async () =>
            await new Scenario("S").Given(() => 1).Then("t").True("Equals 2", x => x == 2).RunAsync());

        Assert.Equal("Scenario 'S' > Assert 'Equals 2'", ex.Data[Scenario.StepDataKey]);
    }

    [Fact]
    public async Task Assertions_can_continue_with_Then_and_Assert()
    {
        var log = new List<string>();

        var result = await new Scenario()
            .Given(() => 1)
            .Then("A").True(x => x == 1)
            .Then("B").Assert(x => log.Add("B"))
            .True(x => x > 0)
            .Then(x => log.Add("then"))
            .Then("C", x => log.Add("C"))
            .RunAsync();

        Assert.Equal(["B", "then", "C"], log);
        Assert.Equal(
            [StepKind.Given, StepKind.Then, StepKind.Assert, StepKind.Then, StepKind.Assert, StepKind.Assert, StepKind.Then, StepKind.Then],
            result.Steps.Select(s => s.Step.Kind));
    }

    [Fact]
    public async Task Then_after_fluent_assertion_async_overloads_are_awaited()
    {
        var a = new Scenario().Given(() => 1).Then("t").True(x => x == 1);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await a.Then("d", async x => await FailLaterTask()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await a.Then("d", x => FailLaterTask()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await a.Then("d", x => FailLaterValueTask()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await a.Then(async x => await FailLaterTask()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await a.Then(x => FailLaterTask()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await a.Then(x => FailLaterValueTask()).RunAsync());
    }

    [Fact]
    public async Task Assertions_are_immutable_when_branching()
    {
        var log = new List<string>();
        var a = new Scenario().Given(() => 1).Then("t");
        var left = a.AddAssertion(_ => { log.Add("left"); return default; }, null, "left");
        var right = a.AddAssertion(_ => { log.Add("right"); return default; }, null, "right");

        await left.RunAsync();
        await right.RunAsync();
        await a.RunAsync();

        Assert.Equal(["left", "right"], log);
    }
}
