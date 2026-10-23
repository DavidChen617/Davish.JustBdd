using static Davish.JustBdd.Tests.Assertions.Probe;

namespace Davish.JustBdd.Tests.Assertions;

public class EdgeCaseTests
{
    private sealed record Box(string? Name, List<int>? Numbers);

    /// <summary>The exception type xUnit throws when called directly, to confirm the wrapper rethrows it unchanged.</summary>
    private static Type ThrownByXunit(Action assert)
    {
        try { assert(); }
        catch (Exception ex) { return ex.GetType(); }
        throw new InvalidOperationException("Expected xUnit to throw");
    }

    [Fact]
    public async Task Null_actual_surfaces_whatever_xunit_throws()
    {
        var box = new Box(null, null);

        var expectedForCollection = ThrownByXunit(() => Assert.Contains(1, (List<int>)null!));
        var actualForCollection = await Record.ExceptionAsync(async () => await On(box).Contains(1, b => b.Numbers!));
        Assert.IsType(expectedForCollection, actualForCollection);

        var expectedForString = ThrownByXunit(() => Assert.StartsWith("a", (string?)null));
        var actualForString = await Record.ExceptionAsync(async () => await On(box).StartsWith("a", b => b.Name));
        Assert.IsType(expectedForString, actualForString);
    }

    [Fact]
    public async Task Null_expected_is_not_ambiguous()
    {
        // null has no type: the string, generic, and collection overloads could all match
        var box = new Box(null, null);

        await Passes(On(box).Equal(null, b => b.Name), null, "Equal(null, b => b.Name)");
        await Passes(On(box).Equal(null, b => b.Numbers), null, "Equal(null, b => b.Numbers)");
        await Fails(On(new Box("x", [1])).Equal(null, b => b.Name));
        await Fails(On(new Box("x", [1])).Equal(null, b => b.Numbers));
    }

    // Across assemblies: extension assertions return ThenStage, so the next When segment can follow directly.
    [Fact]
    public async Task When_follows_xunit_assertions()
    {
        var result = await new Scenario()
            .Given(() => 1)
            .Then("Is 1")
            .Equal(1, x => x)
            .When(x => x + 1)
            .Then("Is 2")
            .Equal(2, x => x);

        Assert.Equal(
            [StepKind.Given, StepKind.Then, StepKind.Assert, StepKind.When, StepKind.Then, StepKind.Assert],
            result.Steps.Select(s => s.Step.Kind));
        Assert.All(result.Steps, s => Assert.Equal(StepStatus.Passed, s.Status));
    }
}
