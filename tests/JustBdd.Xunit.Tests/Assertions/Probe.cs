namespace Davish.JustBdd.Tests.Assertions;

internal static class Probe
{
    public static ThenStage<T> On<T>(T value) => new Scenario("S").Given(() => value).Then("t");

    /// <summary>The assertion passes, and the last step records the expected description and code.</summary>
    public static async Task Passes<T>(ThenStage<T> a, string? desc, string code)
    {
        var result = await a.RunAsync();
        var step = result.Steps[^1];
        Assert.Equal((StepKind.Assert, StepStatus.Passed, desc, code), (step.Step.Kind, step.Status, step.Step.Description, step.Step.Code));
    }

    /// <summary>The assertion fails with xUnit's own assertion exception, not wrapped by the core.</summary>
    public static Task<XunitException> Fails<T>(ThenStage<T> a)
        => Assert.ThrowsAnyAsync<XunitException>(async () => await a.RunAsync());
}

internal sealed class User(string name, object? account = null)
{
    public string Name { get; } = name;
    public object? Account { get; } = account;
    public int? Age { get; init; }
    public bool IsActive { get; init; }
    public double Score { get; init; }
    public float Ratio { get; init; }
    public decimal Balance { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public List<int> Numbers { get; init; } = [];
    public List<string> Tags { get; init; } = [];
    public Dictionary<string, int> Scores { get; init; } = [];
    public HashSet<int> Set { get; init; } = [];
}

internal class Admin;

internal sealed class SuperAdmin : Admin;
