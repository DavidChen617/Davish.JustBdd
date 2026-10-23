namespace Davish.JustBdd.Tests;

public class NoDescriptionTests
{
    private static string? StepOf(Exception ex) => ex.Data[Scenario.StepDataKey] as string;

    private static T ThrowsInvalid<T>() => throw new InvalidOperationException();

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
    public void Scenario_without_description_uses_caller_member_name()
    {
        Assert.Equal(nameof(Scenario_without_description_uses_caller_member_name), new Scenario().Description);
    }

    [Fact]
    public async Task Steps_without_description_pass_state_along()
    {
        string? observed = null;

        await new Scenario()
            .Given(() => new User("David"))
            .When(user => user.Rename("Davish"))
            .Then(user => observed = user.Name)
            .Then(user => { })
            .RunAsync();

        Assert.Equal("Davish", observed);
    }

    [Fact]
    public async Task Other_paths_without_description()
    {
        var log = new List<string>();

        await new Scenario().Given(() => 1).Then(x => log.Add("G>T")).RunAsync();
        await new Scenario().When(() => 2).Then(x => log.Add("W>T")).RunAsync();
        await new Scenario().When(() => log.Add("W")).Then(_ => log.Add("Wv>T")).RunAsync();
        await new Scenario().Then(() => log.Add("T")).RunAsync();
        await new Scenario().Given(() => new User("x")).When(u => u.RenameInPlace("y")).Then(u => log.Add(u.Name)).RunAsync();

        Assert.Equal(["G>T", "W>T", "W", "Wv>T", "T", "y"], log);
    }

    [Fact]
    public async Task Description_falls_back_to_lambda_source_text()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await new Scenario().Given(() => ThrowsInvalid<int>()).Then(_ => { }).RunAsync());

        Assert.Equal(
            $"Scenario '{nameof(Description_falls_back_to_lambda_source_text)}' > Given '() => ThrowsInvalid<int>()'",
            StepOf(ex));
    }

    [Fact]
    public async Task Async_lambdas_without_description_are_awaited()
    {
        var given = new Scenario().Given(async () => { await Task.Yield(); return new User("x"); });

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await given.When(async u => await FailLaterTask()).Then(_ => { }).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await given.When(u => FailLaterValueTask()).Then(_ => { }).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await given.Then(u => FailLaterTask()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await given.Then(u => FailLaterValueTask()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await given.Then(_ => { }).Then(async u => await FailLaterTask()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await new Scenario().When(() => FailLaterValueTask()).Then(_ => { }).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await new Scenario().Then(async () => await FailLaterTask()).RunAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await new Scenario().When(() => ThrowsInvalid<int>()).Then(_ => { }).RunAsync());
    }

    [Fact]
    public async Task Mixed_with_and_without_description()
    {
        string? observed = null;

        await new Scenario("Mixed")
            .Given(() => new User("David"))
            .When("Rename", user => user.Rename("Davish"))
            .Then(user => observed = user.Name)
            .RunAsync();

        Assert.Equal("Davish", observed);
    }
}
