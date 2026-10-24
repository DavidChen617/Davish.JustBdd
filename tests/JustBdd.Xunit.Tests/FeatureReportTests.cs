using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.TestHost;

namespace Davish.JustBdd.Tests;

internal sealed class CheckoutFeature : IFeature
{
    public string Name => "Create an order";
    public string? Description => "As a customer,\nI want to check out.";
    public IReadOnlyList<FeatureLink> Links => [new("Spec", "https://example.com/spec")];
}

internal sealed class CouponFeature : IFeature
{
    public string Name => "Coupons";
}

[Feature<CheckoutFeature>]
internal class FeatureOnClass
{
    public void Plain() { }

    [Feature<CouponFeature>]
    public void Overridden() { }
}

internal sealed class InheritsFeature : FeatureOnClass;

internal sealed class NoFeature
{
    public void Plain() { }
}

internal sealed class TaggedCheckoutFeature : IFeature
{
    public string Name => "Create an order";
    public IReadOnlyList<FeatureLink> Links => [new("Spec", "https://example.com/spec")];
    public IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> Metadata => [new("Owner", ["team-checkout"])];
}

[Feature("Create an order", Description = "Declared inline")]
internal sealed class DeclaredFeature
{
    public void Plain() { }

    [Feature("Refund an order")]
    public void Overridden() { }
}

[Feature<TaggedCheckoutFeature>]
[FeatureLinks("Ticket", "https://example.com/SHOP-1")]
[FeatureMetadata("Tags", "checkout", "coupon")]
internal sealed class AnnotatedFeature
{
    public void Plain() { }
}

public class FeatureResolverTests
{
    private static MethodInfo Method<T>(string name) => typeof(T).GetMethod(name)!;

    [Fact]
    public void Class_feature_applies_to_its_methods()
    {
        var feature = FeatureResolver.Resolve(Method<FeatureOnClass>(nameof(FeatureOnClass.Plain)));

        Assert.NotNull(feature);
        Assert.Equal("Create an order", feature.Id);
        Assert.Equal("Create an order", feature.Name);
        Assert.Equal("As a customer,\nI want to check out.", feature.Description);
        Assert.Equal([new FeatureLink("Spec", "https://example.com/spec")], feature.Links);
    }

    [Fact]
    public void Method_feature_overrides_class_feature()
    {
        Assert.Equal("Coupons", FeatureResolver.Resolve(Method<FeatureOnClass>(nameof(FeatureOnClass.Overridden)))?.Name);
        Assert.Equal("Refund an order", FeatureResolver.Resolve(Method<DeclaredFeature>(nameof(DeclaredFeature.Overridden)))?.Name);
    }

    [Fact]
    public void Feature_is_inherited_from_a_base_test_class()
        => Assert.Equal("Create an order", FeatureResolver.Resolve(Method<InheritsFeature>(nameof(FeatureOnClass.Plain)))?.Name);

    [Fact]
    public void Missing_feature_resolves_to_null()
        => Assert.Null(FeatureResolver.Resolve(Method<NoFeature>(nameof(NoFeature.Plain))));

    [Fact]
    public void Optional_members_default_to_empty()
    {
        var feature = FeatureResolver.Resolve(Method<FeatureOnClass>(nameof(FeatureOnClass.Overridden)))!;

        Assert.Null(feature.Description);
        Assert.Empty(feature.Links);
        Assert.Empty(feature.Metadata);
    }

    [Fact]
    public void String_attribute_feature_is_resolved()
    {
        var feature = FeatureResolver.Resolve(Method<DeclaredFeature>(nameof(DeclaredFeature.Plain)))!;

        Assert.Equal(("Create an order", "Create an order", "Declared inline"), (feature.Id, feature.Name, feature.Description));
    }

    [Fact]
    public void Link_and_metadata_attributes_follow_the_feature_definition()
    {
        var feature = FeatureResolver.Resolve(Method<AnnotatedFeature>(nameof(AnnotatedFeature.Plain)))!;

        Assert.Equal([new FeatureLink("Spec", "https://example.com/spec"), new FeatureLink("Ticket", "https://example.com/SHOP-1")], feature.Links);
        Assert.Equal(["Owner", "Tags"], feature.Metadata.Select(m => m.Key));
        Assert.Equal(["checkout", "coupon"], feature.Metadata[1].Value);
    }

    [Fact]
    public void Feature_identity_is_its_name()
        => Assert.Equal(
            FeatureResolver.Resolve(Method<FeatureOnClass>(nameof(FeatureOnClass.Plain)))!.Id,
            FeatureResolver.Resolve(Method<DeclaredFeature>(nameof(DeclaredFeature.Plain)))!.Id);
}

public class FeatureReportTests
{
    private static readonly ReportFeature Checkout = new("Shop.Checkout", "Create an order", "As a customer,\nI want to check out.", [new("Spec", "https://example.com/spec")], []);

    private static Task<ScenarioResult> Scenario(string name) => new Scenario(name).Given(() => 1).Then(x => { }).RunAsync().AsTask();

    [Fact]
    public async Task Collector_keeps_the_feature_of_each_test()
    {
        var c = new ReportCollector();
        c.AddScenario("T.A", await Scenario("a"), Checkout);
        c.AddScenario("T.B", await Scenario("b"));

        var tests = c.Build();

        Assert.Same(Checkout, tests[0].Feature);
        Assert.Null(tests[1].Feature);
    }

    [Fact]
    [Feature<CheckoutFeature>]
    public async Task Reported_scenarios_carry_the_feature_of_the_running_test()
    {
        var reporter = new JustBddReporter(null!, null!);

        reporter.Report(await new Scenario("s").Then(() => { }).RunAsync());

        Assert.Equal("Create an order", Assert.Single(reporter.Collector.Build()).Feature?.Name);
    }

    [Fact]
    public async Task Features_are_emitted_once_and_tests_reference_them()
    {
        var c = new ReportCollector();
        c.AddScenario("T.A", await Scenario("a"), Checkout);
        c.AddScenario("T.B", await Scenario("b"), Checkout);
        c.AddScenario("T.C", await Scenario("c"));

        var html = HtmlReport.Render(c.Build(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var json = Regex.Match(html, """<script id="mtp-data" type="application/json">(.*?)</script>""", RegexOptions.Singleline).Groups[1].Value;
        var data = JsonDocument.Parse(json).RootElement;

        var feature = Assert.Single(data.GetProperty("features").EnumerateObject());
        Assert.Equal("Shop.Checkout", feature.Name);
        Assert.Equal("Create an order", feature.Value.GetProperty("name").GetString());
        Assert.Equal("As a customer,\nI want to check out.", feature.Value.GetProperty("description").GetString());
        var link = Assert.Single(feature.Value.GetProperty("links").EnumerateArray());
        Assert.Equal(("Spec", "https://example.com/spec"), (link.GetProperty("title").GetString(), link.GetProperty("url").GetString()));

        var tests = data.GetProperty("tests");
        Assert.Equal("Shop.Checkout", tests[0].GetProperty("featureId").GetString());
        Assert.Equal("Shop.Checkout", tests[1].GetProperty("featureId").GetString());
        Assert.False(tests[2].TryGetProperty("featureId", out _));
    }

    [Fact]
    public async Task Without_features_the_features_object_is_empty()
    {
        var c = new ReportCollector();
        c.AddScenario("T.A", await Scenario("a"));

        var html = HtmlReport.Render(c.Build(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var json = Regex.Match(html, """<script id="mtp-data" type="application/json">(.*?)</script>""", RegexOptions.Singleline).Groups[1].Value;

        Assert.Empty(JsonDocument.Parse(json).RootElement.GetProperty("features").EnumerateObject());
    }

    private static JsonElement Data(IReadOnlyList<ReportTest> tests)
    {
        var html = HtmlReport.Render(tests, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var json = Regex.Match(html, """<script id="mtp-data" type="application/json">(.*?)</script>""", RegexOptions.Singleline).Groups[1].Value;
        return JsonDocument.Parse(json).RootElement;
    }

    private static ReportFeature Resolved<T>(string method) => FeatureResolver.Resolve(typeof(T).GetMethod(method)!)!;

    [Fact]
    public async Task Features_are_keyed_by_name()
    {
        var c = new ReportCollector();
        c.AddScenario("T.A", await Scenario("a"), Resolved<DeclaredFeature>(nameof(DeclaredFeature.Plain)));
        c.AddScenario("T.B", await Scenario("b"), Resolved<DeclaredFeature>(nameof(DeclaredFeature.Plain)));
        c.AddScenario("T.C", await Scenario("c"));

        var data = Data(c.Build());

        Assert.Equal("Create an order", Assert.Single(data.GetProperty("features").EnumerateObject()).Name);
        var tests = data.GetProperty("tests");
        Assert.Equal("Create an order", tests[0].GetProperty("featureId").GetString());
        Assert.Equal("Create an order", tests[1].GetProperty("featureId").GetString());
        Assert.False(tests[2].TryGetProperty("featureId", out _));
    }

    [Fact]
    public async Task Metadata_and_links_are_emitted()
    {
        var c = new ReportCollector();
        c.AddScenario("T.A", await Scenario("a"), Resolved<AnnotatedFeature>(nameof(AnnotatedFeature.Plain)));

        var feature = Data(c.Build()).GetProperty("features").GetProperty("Create an order");

        Assert.Equal(["Spec", "Ticket"], feature.GetProperty("links").EnumerateArray().Select(l => l.GetProperty("title").GetString()));
        Assert.Equal("https://example.com/SHOP-1", feature.GetProperty("links")[1].GetProperty("url").GetString());
        var metadata = feature.GetProperty("metadata");
        Assert.Equal(["Owner", "Tags"], metadata.EnumerateArray().Select(m => m.GetProperty("key").GetString()));
        Assert.Equal(["checkout", "coupon"], metadata[1].GetProperty("values").EnumerateArray().Select(v => v.GetString()));
    }

    [Fact]
    public async Task First_definition_wins_for_a_duplicate_name()
    {
        var c = new ReportCollector();
        c.AddScenario("T.A", await Scenario("a"), Resolved<DeclaredFeature>(nameof(DeclaredFeature.Plain)));       // "Declared inline"
        c.AddScenario("T.B", await Scenario("b"), Resolved<FeatureOnClass>(nameof(FeatureOnClass.Plain)));         // same name, other description

        var feature = Assert.Single(Data(c.Build()).GetProperty("features").EnumerateObject());

        Assert.Equal("Declared inline", feature.Value.GetProperty("description").GetString());
    }

    [Fact]
    public async Task Traits_from_test_node_metadata_are_collected_in_key_order()
    {
        var reporter = new JustBddReporter(null!, null!);
        reporter.Collector.AddScenario("T.A", await Scenario("a"));
        var node = new TestNode
        {
            Uid = new TestNodeUid("T.A"),
            DisplayName = "T.A",
            Properties = new PropertyBag(PassedTestNodeStateProperty.CachedInstance,
                new TestMetadataProperty("Owner", "team-a"), new TestMetadataProperty("Category", "Smoke")),
        };

        await reporter.ConsumeAsync(null!, new TestNodeUpdateMessage(new SessionUid("s"), node), TestContext.Current.CancellationToken);

        Assert.Equal(
            [new KeyValuePair<string, string>("Category", "Smoke"), new KeyValuePair<string, string>("Owner", "team-a")],
            Assert.Single(reporter.Collector.Build()).Traits);
    }

    [Fact]
    public async Task Tests_without_traits_have_an_empty_list()
    {
        var c = new ReportCollector();
        c.AddScenario("T.A", await Scenario("a"));
        c.SetOutcome("T.A", "passed", 1, "T", "A");

        Assert.Empty(Assert.Single(c.Build()).Traits);
    }

    [Fact]
    public async Task Traits_are_emitted_as_key_value_pairs()
    {
        var c = new ReportCollector();
        c.AddScenario("T.A", await Scenario("a"));
        c.SetOutcome("T.A", "passed", 1, "T", "A", [new("Category", "Smoke")]);

        var trait = Assert.Single(Data(c.Build()).GetProperty("tests")[0].GetProperty("traits").EnumerateArray());

        Assert.Equal(("Category", "Smoke"), (trait.GetProperty("key").GetString(), trait.GetProperty("value").GetString()));
    }
}
