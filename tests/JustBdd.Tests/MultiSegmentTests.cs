namespace Davish.JustBdd.Tests;

/// <summary>When after Then: G→W→T→W→T. Then does not transform state; after ThenThrows, When receives Thrown.</summary>
public class MultiSegmentTests
{
    [Fact]
    public async Task Second_When_receives_the_state_seen_by_the_first_Then()
    {
        User? first = null, second = null;

        var result = await new Scenario("Rename twice")
            .Given("A user exists", () => new User("David"))
            .When("Rename to Davish", u => u.Rename("Davish"))
            .Then("The name is Davish", u => { first = u; Assert.Equal("Davish", u.Name); })
            .When("Rename again to Dave", u => u.RenameInPlace("Dave"))
            .Then("The name is Dave", u => { second = u; Assert.Equal("Dave", u.Name); })
            .RunAsync();

        Assert.Same(first, second);
        Assert.Equal(
            [StepKind.Given, StepKind.When, StepKind.Then, StepKind.When, StepKind.Then],
            result.Steps.Select(s => s.Step.Kind));
        Assert.All(result.Steps, s => Assert.Equal(StepStatus.Passed, s.Status));
    }

    [Fact]
    public async Task Second_value_When_replaces_state()
    {
        var observed = 0;

        await new Scenario()
            .Given(() => new User("David"))
            .When(u => u.Rename("Davish"))
            .Then(u => Assert.Equal("Davish", u.Name))
            .When(u => u.Name.Length)
            .Then(length => observed = length)
            .RunAsync();

        Assert.Equal(6, observed);
    }

    [Fact]
    public async Task First_segment_failure_marks_later_segments_not_run()
    {
        var secondWhenRan = false;

        var ex = await Assert.ThrowsAsync<Xunit.Sdk.EqualException>(async () =>
            await new Scenario("S")
                .Given(() => 1)
                .When(x => x + 1)
                .Then("Equals 3", x => Assert.Equal(3, x))
                .When("Second segment", x => { secondWhenRan = true; })
                .Then(x => { }).RunAsync());

        Assert.False(secondWhenRan);
        Assert.Equal("Scenario 'S' > Then 'Equals 3'", ex.Data[Scenario.StepDataKey]);
        var result = Assert.IsType<ScenarioResult>(ex.Data[Scenario.ResultDataKey]);
        Assert.Equal(
            [StepStatus.Passed, StepStatus.Passed, StepStatus.Failed, StepStatus.NotRun, StepStatus.NotRun],
            result.Steps.Select(s => s.Status));
    }

    [Fact]
    public async Task When_after_ThenThrows_receives_Thrown()
    {
        Thrown<User, ArgumentException>? received = null;
        string? finalName = null;

        await new Scenario()
            .Given(() => new User("David"))
            .When(u => { if (u.Name == "David") throw new ArgumentException("bad"); })
            .ThenThrows<ArgumentException>()
            .When(t => { received = t; t.State.RenameInPlace("Davish"); })
            .Then(t => finalName = t.State.Name)
            .RunAsync();

        Assert.NotNull(received);
        Assert.Equal("bad", received.Exception.Message);
        Assert.Equal("Davish", finalName);
    }

    [Fact]
    public async Task Unit_state_flows_through_segments()
    {
        var log = new List<string>();

        var result = await new Scenario()
            .When(() => log.Add("w1"))
            .Then(_ => log.Add("t1"))
            .When(_ => log.Add("w2"))
            .Then(_ => log.Add("t2"))
            .RunAsync();

        Assert.Equal(["w1", "t1", "w2", "t2"], log);
        Assert.Equal(4, result.Steps.Count);
    }

    [Fact]
    public async Task Branching_after_Then_is_independent()
    {
        var log = new List<string>();
        var then = new Scenario().Given(() => 1).Then(x => log.Add($"then {x}"));

        var left = await then.When(x => x + 1).Then(x => log.Add($"left {x}")).RunAsync();
        var right = await then.When(x => x * 10).Then(x => log.Add($"right {x}")).RunAsync();

        Assert.Equal(["then 1", "left 2", "then 1", "right 10"], log);
        Assert.Equal(4, left.Steps.Count);
        Assert.Equal(4, right.Steps.Count);
    }

    private static async Task<int> DoubleLater(int x)
    {
        await Task.Yield();
        return x * 2;
    }

    private static async Task TouchLater(User u)
    {
        await Task.Yield();
        u.RenameInPlace("Async");
    }

    [Fact]
    public async Task Then_When_overloads_pick_pass_or_map()
    {
        var then = new Scenario().Given(() => 3).Then(x => { });
        var user = new Scenario().Given(() => new User("David")).Then(u => { });
        int observed = 0;
        string? name = null;

        await then.When("map", x => x + 1).Then(x => observed = x).RunAsync();
        Assert.Equal(4, observed);
        await then.When("map task", x => DoubleLater(x)).Then(x => observed = x).RunAsync();
        Assert.Equal(6, observed);
        await then.When(async x => await DoubleLater(x)).Then(x => observed = x).RunAsync();
        Assert.Equal(6, observed);
        await then.When(x => new ValueTask<int>(x + 10)).Then(x => observed = x).RunAsync();
        Assert.Equal(13, observed);

        await user.When("pass", u => u.RenameInPlace("Pass")).Then(u => name = u.Name).RunAsync();
        Assert.Equal("Pass", name);
        await user.When(u => TouchLater(u)).Then(u => name = u.Name).RunAsync();
        Assert.Equal("Async", name);
        await user.When(async u => await TouchLater(u)).Then(u => name = u.Name).RunAsync();
        Assert.Equal("Async", name);
        // Every await reruns from Given, so a no-op When sees a fresh User.
        await user.When(u => new ValueTask()).Then(u => name = u.Name).RunAsync();
        Assert.Equal("David", name);
    }

    [Fact]
    public async Task When_follows_fluent_assertions()
    {
        var observed = 0;

        var result = await new Scenario()
            .Given(() => 1)
            .Then("Is 1")
            .Assert(x => Assert.Equal(1, x))
            .When(x => x + 1)
            .Then(x => observed = x)
            .RunAsync();

        Assert.Equal(2, observed);
        Assert.Equal(
            [StepKind.Given, StepKind.Then, StepKind.Assert, StepKind.When, StepKind.Then],
            result.Steps.Select(s => s.Step.Kind));
    }
}
