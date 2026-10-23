using static Davish.JustBdd.Tests.Assertions.Probe;

namespace Davish.JustBdd.Tests.Assertions;

public class CollectionTests
{
    private static readonly User David = new("David")
    {
        Numbers = [1, 2, 3],
        Tags = ["admin"],
        Scores = new() { ["math"] = 90 },
        Set = [1, 2, 3],
    };

    [Fact]
    public async Task Contains_item_and_predicate()
    {
        await Passes(On(David).Contains("Has 2", 2, u => u.Numbers), "Has 2", "Contains(2, u => u.Numbers)");
        await Passes(On(David).Contains("ADMIN", u => u.Tags, StringComparer.OrdinalIgnoreCase), null, "Contains(\"ADMIN\", u => u.Tags)");
        await Passes(On(David).Contains("Greater than 2", u => u.Numbers, n => n > 2), "Greater than 2", "Contains(u => u.Numbers, n => n > 2)");
        await Passes(On(David).Contains(u => u.Numbers, n => n > 2), null, "Contains(u => u.Numbers, n => n > 2)");
        await Fails(On(David).Contains(9, u => u.Numbers));
        await Fails(On(David).Contains(u => u.Numbers, n => n > 9));
    }

    [Fact]
    public async Task DoesNotContain_item_and_predicate()
    {
        await Passes(On(David).DoesNotContain("No 9", 9, u => u.Numbers), "No 9", "DoesNotContain(9, u => u.Numbers)");
        await Passes(On(David).DoesNotContain(u => u.Numbers, n => n > 9), null, "DoesNotContain(u => u.Numbers, n => n > 9)");
        await Fails(On(David).DoesNotContain(2, u => u.Numbers));
    }

    [Fact]
    public async Task Contains_and_DoesNotContain_dictionary_key()
    {
        await Passes(On(David).Contains("Has math", "math", u => u.Scores), "Has math", "Contains(\"math\", u => u.Scores)");
        await Passes(On(David).DoesNotContain("art", u => u.Scores), null, "DoesNotContain(\"art\", u => u.Scores)");
        await Fails(On(David).Contains("art", u => u.Scores));
        await Fails(On(David).DoesNotContain("math", u => u.Scores));
        // A KeyValuePair argument picks the collection overload and checks the whole pair
        await Passes(On(David).Contains(new KeyValuePair<string, int>("math", 90), u => u.Scores), null, "Contains(new KeyValuePair<string, int>(\"math\", 90), u => u.Scores)");
        await Fails(On(David).Contains(new KeyValuePair<string, int>("math", 0), u => u.Scores));
    }

    [Fact]
    public async Task Empty_and_NotEmpty()
    {
        await Passes(On(new User("x")).Empty("No numbers", u => u.Numbers), "No numbers", "Empty(u => u.Numbers)");
        await Passes(On(David).NotEmpty(u => u.Numbers), null, "NotEmpty(u => u.Numbers)");
        await Fails(On(David).Empty(u => u.Numbers));
        await Fails(On(new User("x")).NotEmpty(u => u.Numbers));
    }

    [Fact]
    public async Task Single()
    {
        await Passes(On(David).Single("Exactly one tag", u => u.Tags), "Exactly one tag", "Single(u => u.Tags)");
        await Passes(On(David).Single(u => u.Numbers, n => n > 2), null, "Single(u => u.Numbers, n => n > 2)");
        await Passes(On(David).Single(u => u.Tags, "admin"), null, "Single(u => u.Tags, \"admin\")");   // The second argument is not a Predicate, so the non-generic overload is chosen
        await Fails(On(David).Single(u => u.Tags, "user"));
        await Fails(On(David).Single(u => u.Numbers));
    }

    [Fact]
    public async Task Distinct()
    {
        await Passes(On(David).Distinct("Distinct", u => u.Numbers), "Distinct", "Distinct(u => u.Numbers)");
        await Fails(On(new User("x") { Tags = ["a", "A"] }).Distinct(u => u.Tags, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task All_and_AllAsync()
    {
        await Passes(On(David).All("All positive", u => u.Numbers, n => Assert.True(n > 0)), "All positive", "All(u => u.Numbers, n => Assert.True(n > 0))");
        await Passes(On(David).All(u => u.Numbers, (n, i) => Assert.Equal(i + 1, n)), null, "All(u => u.Numbers, (n, i) => Assert.Equal(i + 1, n))");
        await Fails(On(David).All(u => u.Numbers, n => Assert.True(n > 1)));

        await Passes(On(David).AllAsync(u => u.Numbers, async n => { await Task.Yield(); Assert.True(n > 0); }), null,
            "AllAsync(u => u.Numbers, async n => { await Task.Yield(); Assert.True(n > 0); })");
        await Fails(On(David).AllAsync(u => u.Numbers, async (n, i) => { await Task.Yield(); Assert.Equal(0, i); }));
    }

    [Fact]
    public async Task Collection_and_CollectionAsync()
    {
        await Passes(On(David).Collection("Inspect in order", u => u.Numbers, [n => Assert.Equal(1, n), n => Assert.Equal(2, n), n => Assert.Equal(3, n)]), "Inspect in order",
            "Collection(u => u.Numbers, [n => Assert.Equal(1, n), n => Assert.Equal(2, n), n => Assert.Equal(3, n)])");
        await Fails(On(David).Collection(u => u.Numbers, [n => Assert.Equal(1, n)]));
        await Passes(On(David).CollectionAsync(u => u.Tags, [async t => { await Task.Yield(); Assert.Equal("admin", t); }]), null,
            "CollectionAsync(u => u.Tags, [async t => { await Task.Yield(); Assert.Equal(\"admin\", t); }])");
    }

    [Fact]
    public async Task Set_relations()
    {
        await Passes(On(David).Subset("Is a subset", new HashSet<int> { 1, 2, 3, 4 }, u => u.Set), "Is a subset", "Subset(new HashSet<int> { 1, 2, 3, 4 }, u => u.Set)");
        await Passes(On(David).Superset(new HashSet<int> { 1 }, u => u.Set), null, "Superset(new HashSet<int> { 1 }, u => u.Set)");
        await Passes(On(David).ProperSubset(new HashSet<int> { 1, 2, 3, 4 }, u => u.Set), null, "ProperSubset(new HashSet<int> { 1, 2, 3, 4 }, u => u.Set)");
        await Passes(On(David).ProperSuperset(new HashSet<int> { 1, 2 }, u => u.Set), null, "ProperSuperset(new HashSet<int> { 1, 2 }, u => u.Set)");
        await Fails(On(David).ProperSubset(new HashSet<int> { 1, 2, 3 }, u => u.Set));
        await Fails(On(David).Subset(new HashSet<int> { 1 }, u => u.Set));
    }

    [Fact]
    public async Task Chain_continues_with_then_and_records_every_assertion()
    {
        var result = await new Scenario()
            .Given(() => David)
            .Then("Name")
            .Equal("David", u => u.Name)
            .NotEmpty(u => u.Numbers)
            .Then("Numbers")
            .Contains(2, u => u.Numbers)
            .RunAsync();

        Assert.Equal(
            ["Equal(\"David\", u => u.Name)", "NotEmpty(u => u.Numbers)", "Contains(2, u => u.Numbers)"],
            result.Steps.Where(s => s.Step.Kind == StepKind.Assert).Select(s => s.Step.Code));
    }
}
