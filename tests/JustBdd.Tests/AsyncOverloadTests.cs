namespace Davish.JustBdd.Tests;

/// <summary>
/// Verifies that every kind of async lambda picks an overload that awaits it.
/// Void steps are verified by throwing after a yield: if an Action overload swallows the lambda, the exception never reaches the await.
/// </summary>
public class AsyncOverloadTests
{
    private sealed class LateException : Exception;

    private static async Task FailLaterTask()
    {
        await Task.Yield();
        throw new LateException();
    }

    private static async ValueTask FailLaterValueTask()
    {
        await Task.Yield();
        throw new LateException();
    }

    private static async Task<User> LoadTask(string name)
    {
        await Task.Yield();
        return new User(name);
    }

    private static async ValueTask<User> LoadValueTask(string name)
    {
        await Task.Yield();
        return new User(name);
    }

    private static Task AssertAwaited<T>(ThenStage<T> stage)
        => Assert.ThrowsAsync<LateException>(async () => await stage.RunAsync());

    // ---- With a value: picking the wrong overload makes T a Task<User>, so user.Name below would not compile

    [Fact]
    public async Task Given_async_lambda()
    {
        string? observed = null;
        await new Scenario("s")
            .Given("g", async () => { await Task.Yield(); return new User("A"); })
            .Then("t", user => observed = user.Name)
            .RunAsync();
        Assert.Equal("A", observed);
    }

    [Fact]
    public async Task Given_returning_Task_without_async()
    {
        string? observed = null;
        await new Scenario("s").Given("g", () => LoadTask("A")).Then("t", user => observed = user.Name).RunAsync();
        Assert.Equal("A", observed);
    }

    [Fact]
    public async Task Given_returning_ValueTask_without_async()
    {
        string? observed = null;
        await new Scenario("s").Given("g", () => LoadValueTask("A")).Then("t", user => observed = user.Name).RunAsync();
        Assert.Equal("A", observed);
    }

    [Fact]
    public async Task Scenario_When_returning_Task_and_ValueTask_values()
    {
        string? a = null, b = null;
        await new Scenario("s").When("w", () => LoadTask("A")).Then("t", user => a = user.Name).RunAsync();
        await new Scenario("s").When("w", () => LoadValueTask("B")).Then("t", user => b = user.Name).RunAsync();
        Assert.Equal(("A", "B"), (a, b));
    }

    [Fact]
    public async Task GivenStage_When_async_values()
    {
        string? a = null, b = null, c = null;
        var given = new Scenario("s").Given("g", () => new User("x"));

        await given.When("w", async user => { await Task.Yield(); return user.Rename("A"); }).Then("t", u => a = u.Name).RunAsync();
        await given.When("w", user => LoadTask("B")).Then("t", u => b = u.Name).RunAsync();
        await given.When("w", user => LoadValueTask("C")).Then("t", u => c = u.Name).RunAsync();

        Assert.Equal(("A", "B", "C"), (a, b, c));
    }

    // ---- Without a value: the lambda must really be awaited

    [Fact]
    public async Task Scenario_When_void_async_is_awaited()
    {
        await AssertAwaited(new Scenario("s").When("w", async () => await FailLaterTask()).Then("t", _ => { }));
        await AssertAwaited(new Scenario("s").When("w", () => FailLaterTask()).Then("t", _ => { }));
        await AssertAwaited(new Scenario("s").When("w", () => FailLaterValueTask()).Then("t", _ => { }));
    }

    [Fact]
    public async Task GivenStage_When_void_async_is_awaited()
    {
        var given = new Scenario("s").Given("g", () => new User("x"));
        await AssertAwaited(given.When("w", async user => await FailLaterTask()).Then("t", _ => { }));
        await AssertAwaited(given.When("w", user => FailLaterTask()).Then("t", _ => { }));
        await AssertAwaited(given.When("w", user => FailLaterValueTask()).Then("t", _ => { }));
    }

    [Fact]
    public async Task Scenario_Then_async_is_awaited()
    {
        await AssertAwaited(new Scenario("s").Then("t", async () => await FailLaterTask()));
        await AssertAwaited(new Scenario("s").Then("t", () => FailLaterTask()));
        await AssertAwaited(new Scenario("s").Then("t", () => FailLaterValueTask()));
    }

    [Fact]
    public async Task GivenStage_Then_async_is_awaited()
    {
        var given = new Scenario("s").Given("g", () => new User("x"));
        await AssertAwaited(given.Then("t", async user => await FailLaterTask()));
        await AssertAwaited(given.Then("t", user => FailLaterTask()));
        await AssertAwaited(given.Then("t", user => FailLaterValueTask()));
    }

    [Fact]
    public async Task WhenStage_Then_async_is_awaited()
    {
        var when = new Scenario("s").When("w", () => new User("x"));
        await AssertAwaited(when.Then("t", async user => await FailLaterTask()));
        await AssertAwaited(when.Then("t", user => FailLaterTask()));
        await AssertAwaited(when.Then("t", user => FailLaterValueTask()));
    }

    [Fact]
    public async Task ThenStage_Then_async_is_awaited()
    {
        var then = new Scenario("s").Given("g", () => new User("x")).Then("t1", _ => { });
        await AssertAwaited(then.Then("t", async user => await FailLaterTask()));
        await AssertAwaited(then.Then("t", user => FailLaterTask()));
        await AssertAwaited(then.Then("t", user => FailLaterValueTask()));
    }
}
