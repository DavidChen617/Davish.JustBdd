using static Davish.JustBdd.Tests.Assertions.Probe;

namespace Davish.JustBdd.Tests.Assertions;

public class EqualityTests
{
    private static readonly DateTime Created = new(2026, 10, 18, 12, 0, 0);

    private static readonly User David = new("David")
    {
        Age = 30,
        Score = 3.14159,
        Ratio = 0.5f,
        Balance = 10.126m,
        CreatedAt = Created,
        UpdatedAt = new DateTimeOffset(Created),
        Numbers = [1, 2, 3],
    };

    [Fact]
    public async Task Equal_generic()
    {
        await Passes(On(David).Equal("Age", 30, u => u.Age), "Age", "Equal(30, u => u.Age)");
        await Passes(On(David).Equal(30, u => u.Age), null, "Equal(30, u => u.Age)");
        await Fails(On(David).Equal(31, u => u.Age));
    }

    [Fact]
    public async Task Equal_with_comparers()
    {
        var ignoreCase = StringComparer.OrdinalIgnoreCase;
        await Passes(On(David).Equal("Ignore case", (object)"DAVID", u => (object)u.Name, new LooseComparer()), "Ignore case", "Equal((object)\"DAVID\", u => (object)u.Name)");
        await Passes(On(David).Equal("david", u => u.Name.ToLowerInvariant(), ignoreCase), null, "Equal(\"david\", u => u.Name.ToLowerInvariant())");
        await Passes(On(David).Equal(30, u => u.Age!.Value, (a, b) => a == b), null, "Equal(30, u => u.Age!.Value)");
        await Fails(On(David).Equal(31, u => u.Age!.Value, (a, b) => a == b));
    }

    [Fact]
    public async Task Equal_collections()
    {
        await Passes(On(David).Equal("Numbers", new[] { 1, 2, 3 }, u => u.Numbers), "Numbers", "Equal(new[] { 1, 2, 3 }, u => u.Numbers)");
        await Passes(On(David).Equal(new[] { 1, 2, 3 }, u => u.Numbers), null, "Equal(new[] { 1, 2, 3 }, u => u.Numbers)");
        await Passes(On(David).Equal(new[] { 1, 2, 3 }, u => u.Numbers, EqualityComparer<int>.Default), null, "Equal(new[] { 1, 2, 3 }, u => u.Numbers)");
        await Fails(On(David).Equal(new[] { 1, 2 }, u => u.Numbers));
    }

    [Fact]
    public async Task Equal_string_options_pick_the_string_overload()
    {
        await Passes(On(David).Equal("Name", "David", u => u.Name), "Name", "Equal(\"David\", u => u.Name)");
        await Passes(On(David).Equal("DAVID", u => u.Name, ignoreCase: true), null, "Equal(\"DAVID\", u => u.Name)");
        await Fails(On(David).Equal("DAVID", u => u.Name));
    }

    [Fact]
    public async Task Equal_floating_point_and_decimal()
    {
        await Passes(On(David).Equal("Score", 3.14, u => u.Score, 2), "Score", "Equal(3.14, u => u.Score)");
        await Passes(On(David).Equal(3.14, u => u.Score, 2, MidpointRounding.AwayFromZero), null, "Equal(3.14, u => u.Score)");
        await Passes(On(David).Equal(3.14, u => u.Score, 0.01), null, "Equal(3.14, u => u.Score)");
        await Fails(On(David).Equal(3.15, u => u.Score, 2));

        await Passes(On(David).Equal(0.5f, u => u.Ratio, 2), null, "Equal(0.5f, u => u.Ratio)");
        await Passes(On(David).Equal(0.5f, u => u.Ratio, 0.01f), null, "Equal(0.5f, u => u.Ratio)");
        await Passes(On(David).Equal(10.13m, u => u.Balance, 2), null, "Equal(10.13m, u => u.Balance)");
        await Fails(On(David).Equal(10.12m, u => u.Balance, 2));
    }

    [Fact]
    public async Task Equal_dates_with_precision()
    {
        await Passes(On(David).Equal("Created at", Created.AddSeconds(1), u => u.CreatedAt, TimeSpan.FromSeconds(2)), "Created at", "Equal(Created.AddSeconds(1), u => u.CreatedAt)");
        await Passes(On(David).Equal(new DateTimeOffset(Created).AddSeconds(1), u => u.UpdatedAt, TimeSpan.FromSeconds(2)), null, "Equal(new DateTimeOffset(Created).AddSeconds(1), u => u.UpdatedAt)");
        await Fails(On(David).Equal(Created.AddSeconds(5), u => u.CreatedAt, TimeSpan.FromSeconds(2)));
        await Passes(On(David).Equal(Created, u => u.CreatedAt), null, "Equal(Created, u => u.CreatedAt)");   // Without precision: the generic overload
        await Fails(On(David).Equal(Created.AddTicks(1), u => u.CreatedAt));
    }

    [Fact]
    public async Task NotEqual()
    {
        await Passes(On(David).NotEqual("Not 31", 31, u => u.Age), "Not 31", "NotEqual(31, u => u.Age)");
        await Passes(On(David).NotEqual(31, u => u.Age), null, "NotEqual(31, u => u.Age)");
        await Passes(On(David).NotEqual(31, u => u.Age!.Value, (a, b) => a == b), null, "NotEqual(31, u => u.Age!.Value)");
        await Passes(On(David).NotEqual(new[] { 1 }, u => u.Numbers), null, "NotEqual(new[] { 1 }, u => u.Numbers)");
        await Passes(On(David).NotEqual(3.15, u => u.Score, 2), null, "NotEqual(3.15, u => u.Score)");
        await Passes(On(David).NotEqual(0.6f, u => u.Ratio, 0.01f), null, "NotEqual(0.6f, u => u.Ratio)");
        await Passes(On(David).NotEqual(10.12m, u => u.Balance, 2), null, "NotEqual(10.12m, u => u.Balance)");
        await Fails(On(David).NotEqual(30, u => u.Age));
    }

    [Fact]
    public async Task StrictEqual_and_NotStrictEqual()
    {
        await Passes(On(David).StrictEqual("Age", 30, u => u.Age), "Age", "StrictEqual(30, u => u.Age)");
        await Passes(On(David).NotStrictEqual(31, u => u.Age), null, "NotStrictEqual(31, u => u.Age)");
        await Fails(On(David).StrictEqual(31, u => u.Age));
        await Fails(On(David).NotStrictEqual(30, u => u.Age));
    }

    [Fact]
    public async Task Same_and_NotSame()
    {
        var account = new object();
        var user = new User("David", account);
        await Passes(On(user).Same("Same account", account, u => u.Account), "Same account", "Same(account, u => u.Account)");
        await Passes(On(user).NotSame(new object(), u => u.Account), null, "NotSame(new object(), u => u.Account)");
        await Fails(On(user).Same(new object(), u => u.Account));
        await Fails(On(user).NotSame(account, u => u.Account));
    }

    [Fact]
    public async Task Equivalent()
    {
        await Passes(On(David).Equivalent("Same name", new { Name = "David" }, u => u), "Same name", "Equivalent(new { Name = \"David\" }, u => u)");
        await Passes(On(David).Equivalent(new { Name = "David" }, u => u), null, "Equivalent(new { Name = \"David\" }, u => u)");
        await Fails(On(David).Equivalent(new { Name = "Davish" }, u => u));
        await Fails(On(David).Equivalent(new { Name = "David" }, u => u, strict: true));
    }

    [Fact]
    public async Task EquivalentWithExclusions()
    {
        var expected = new { Name = "David", Age = 99 };
        await Passes(On(David).EquivalentWithExclusions("Ignore age", expected, u => u, ["Age"]), "Ignore age", "EquivalentWithExclusions(expected, u => u, [\"Age\"])");
        await Passes(On(David).EquivalentWithExclusions(expected, u => u, [x => x.Age]), null, "EquivalentWithExclusions(expected, u => u, [x => x.Age])");
        await Fails(On(David).EquivalentWithExclusions(expected, u => u, ["Name"]));
    }

    private sealed class LooseComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y) => string.Equals(x?.ToString(), y?.ToString(), StringComparison.OrdinalIgnoreCase);
        public int GetHashCode(object obj) => 0;
    }
}
