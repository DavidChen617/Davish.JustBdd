namespace Davish.JustBdd.Tests;

public class ScenarioPathTests
{
    [Fact]
    public async Task Given_When_Then_passes_state_along()
    {
        string? observed = null;

        await new Scenario("Rename")
            .Given("A user exists", () => new User("David"))
            .When("Rename to Davish", user => user.Rename("Davish"))
            .Then("The name is updated", user => observed = user.Name)
            .RunAsync();

        Assert.Equal("Davish", observed);
    }

    [Fact]
    public async Task Given_Then()
    {
        string? observed = null;

        await new Scenario("Create")
            .Given("A user exists", () => new User("David"))
            .Then("The name is correct", user => observed = user.Name)
            .RunAsync();

        Assert.Equal("David", observed);
    }

    [Fact]
    public async Task When_Then_with_value()
    {
        var observed = 0;

        await new Scenario("Calculate")
            .When("Add", () => 1 + 2)
            .Then("The result is 3", result => observed = result)
            .RunAsync();

        Assert.Equal(3, observed);
    }

    [Fact]
    public async Task When_Then_without_value()
    {
        var calls = new List<string>();

        await new Scenario("Side effects")
            .When("Run", () => calls.Add("when"))
            .Then("It ran", _ => calls.Add("then"))
            .RunAsync();

        Assert.Equal(["when", "then"], calls);
    }

    [Fact]
    public async Task Then_only()
    {
        var called = false;

        await new Scenario("Then only")
            .Then("Run", () => called = true)
            .RunAsync();

        Assert.True(called);
    }

    [Fact]
    public async Task Multiple_Thens_run_in_order_with_same_state()
    {
        var seen = new List<string>();

        await new Scenario("Multiple Thens")
            .Given("A user exists", () => new User("David"))
            .Then("First", user => seen.Add("1:" + user.Name))
            .Then("Second", user => seen.Add("2:" + user.Name))
            .Then("Third", user => seen.Add("3:" + user.Name))
            .RunAsync();

        Assert.Equal(["1:David", "2:David", "3:David"], seen);
    }

    [Fact]
    public async Task Void_When_keeps_given_state()
    {
        string? observed = null;

        await new Scenario("Rename (void)")
            .Given("A user exists", () => new User("David"))
            .When("Rename", user => user.RenameInPlace("Davish"))
            .Then("The same instance is updated", user => observed = user.Name)
            .RunAsync();

        Assert.Equal("Davish", observed);
    }

    [Fact]
    public async Task Value_When_replaces_state()
    {
        var observed = 0;

        await new Scenario("Transform")
            .Given("A user exists", () => new User("David"))
            .When("Take the name length", user => user.Name.Length)
            .Then("The length is 5", length => observed = length)
            .RunAsync();

        Assert.Equal(5, observed);
    }
}

internal sealed class User(string name)
{
    public string Name { get; private set; } = name;

    public User Rename(string name)
    {
        Name = name;
        return this;
    }

    public void RenameInPlace(string name) => Name = name;
}
