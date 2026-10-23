using static Davish.JustBdd.Tests.Assertions.Probe;

namespace Davish.JustBdd.Tests.Assertions;

public class BooleanAndNullTests
{
    private static readonly User Active = new("David") { IsActive = true, Age = null };

    [Fact]
    public async Task True()
    {
        await Passes(On(Active).True("Active", u => u.IsActive), "Active", "True(u => u.IsActive)");
        await Passes(On(Active).True(u => u.IsActive), null, "True(u => u.IsActive)");
        var ex = await Fails(On(Active).True("The name is empty", u => u.Name == ""));
        Assert.Contains("The name is empty", ex.Message);   // desc is also passed as userMessage
    }

    [Fact]
    public async Task False()
    {
        await Passes(On(Active).False("Not disabled", u => !u.IsActive), "Not disabled", "False(u => !u.IsActive)");
        await Passes(On(Active).False(u => !u.IsActive), null, "False(u => !u.IsActive)");
        await Fails(On(Active).False(u => u.IsActive));
    }

    [Fact]
    public async Task Null()
    {
        await Passes(On(Active).Null("No age", u => u.Age), "No age", "Null(u => u.Age)");
        await Passes(On(Active).Null(u => u.Account), null, "Null(u => u.Account)");
        await Fails(On(Active).Null(u => u.Name));
    }

    [Fact]
    public async Task NotNull()
    {
        await Passes(On(Active).NotNull("Has a name", u => u.Name), "Has a name", "NotNull(u => u.Name)");
        await Passes(On(Active).NotNull(u => u.Name), null, "NotNull(u => u.Name)");
        await Fails(On(Active).NotNull(u => u.Age));
    }
}
