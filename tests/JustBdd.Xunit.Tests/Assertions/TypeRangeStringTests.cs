using System.Text.RegularExpressions;
using static Davish.JustBdd.Tests.Assertions.Probe;

namespace Davish.JustBdd.Tests.Assertions;

public class TypeTests
{
    private static readonly User Owner = new("David", new SuperAdmin());

    [Fact]
    public async Task IsType_and_IsNotType()
    {
        await Passes(On(Owner).IsType("Super admin", typeof(SuperAdmin), u => u.Account), "Super admin", "IsType(typeof(SuperAdmin), u => u.Account)");
        await Passes(On(Owner).IsType(typeof(Admin), u => u.Account, exactMatch: false), null, "IsType(typeof(Admin), u => u.Account)");
        await Fails(On(Owner).IsType(typeof(Admin), u => u.Account));
        await Passes(On(Owner).IsNotType(typeof(Admin), u => u.Account), null, "IsNotType(typeof(Admin), u => u.Account)");
        await Fails(On(Owner).IsNotType(typeof(Admin), u => u.Account, exactMatch: false));
    }

    [Fact]
    public async Task IsAssignableFrom_and_IsNotAssignableFrom()
    {
        await Passes(On(Owner).IsAssignableFrom("Is an admin", typeof(Admin), u => u.Account), "Is an admin", "IsAssignableFrom(typeof(Admin), u => u.Account)");
        await Passes(On(Owner).IsNotAssignableFrom(typeof(string), u => u.Account), null, "IsNotAssignableFrom(typeof(string), u => u.Account)");
        await Fails(On(Owner).IsAssignableFrom(typeof(string), u => u.Account));
        await Fails(On(Owner).IsNotAssignableFrom(typeof(Admin), u => u.Account));
    }
}

public class RangeTests
{
    private static readonly User David = new("David") { Age = 30 };

    [Fact]
    public async Task InRange_and_NotInRange()
    {
        await Passes(On(David).InRange("Adult", 18, 65, u => u.Age!.Value), "Adult", "InRange(18, 65, u => u.Age!.Value)");
        await Passes(On(David).InRange(18, 65, u => u.Age!.Value), null, "InRange(18, 65, u => u.Age!.Value)");
        await Passes(On(David).InRange(65, 18, u => u.Age!.Value, Comparer<int>.Create((a, b) => b.CompareTo(a))), null, "InRange(65, 18, u => u.Age!.Value)");
        await Fails(On(David).InRange(40, 65, u => u.Age!.Value));
        await Passes(On(David).NotInRange(40, 65, u => u.Age!.Value), null, "NotInRange(40, 65, u => u.Age!.Value)");
        await Fails(On(David).NotInRange(18, 65, u => u.Age!.Value));
    }
}

public class StringTests
{
    private static readonly User David = new("David");

    [Fact]
    public async Task StartsWith_and_EndsWith()
    {
        await Passes(On(David).StartsWith("Starts with", "Da", u => u.Name), "Starts with", "StartsWith(\"Da\", u => u.Name)");
        await Passes(On(David).StartsWith("da", u => u.Name, StringComparison.OrdinalIgnoreCase), null, "StartsWith(\"da\", u => u.Name)");
        await Fails(On(David).StartsWith("da", u => u.Name));
        await Passes(On(David).EndsWith("vid", u => u.Name), null, "EndsWith(\"vid\", u => u.Name)");
        await Fails(On(David).EndsWith("VID", u => u.Name));
    }

    [Fact]
    public async Task Contains_and_DoesNotContain_substring()
    {
        await Passes(On(David).Contains("Contains", "avi", u => u.Name), "Contains", "Contains(\"avi\", u => u.Name)");
        await Passes(On(David).Contains("AVI", u => u.Name, StringComparison.OrdinalIgnoreCase), null, "Contains(\"AVI\", u => u.Name)");
        await Fails(On(David).Contains("xyz", u => u.Name));
        await Passes(On(David).DoesNotContain("xyz", u => u.Name), null, "DoesNotContain(\"xyz\", u => u.Name)");
        await Fails(On(David).DoesNotContain("avi", u => u.Name));
    }

    [Fact]
    public async Task Matches_and_DoesNotMatch()
    {
        await Passes(On(David).Matches("Latin name", "^[A-Z][a-z]+$", u => u.Name), "Latin name", "Matches(\"^[A-Z][a-z]+$\", u => u.Name)");
        await Passes(On(David).Matches(new Regex("^D"), u => u.Name), null, "Matches(new Regex(\"^D\"), u => u.Name)");
        await Fails(On(David).Matches("^x", u => u.Name));
        await Passes(On(David).DoesNotMatch("^x", u => u.Name), null, "DoesNotMatch(\"^x\", u => u.Name)");
        await Passes(On(David).DoesNotMatch(new Regex("^x"), u => u.Name), null, "DoesNotMatch(new Regex(\"^x\"), u => u.Name)");
        await Fails(On(David).DoesNotMatch("^D", u => u.Name));
    }

    [Fact]
    public async Task Empty_string()
    {
        await Passes(On(new User("")).Empty("No name", u => u.Name), "No name", "Empty(u => u.Name)");
        await Fails(On(David).Empty(u => u.Name));
    }
}
