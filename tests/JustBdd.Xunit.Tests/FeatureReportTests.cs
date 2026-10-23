using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

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

public class FeatureResolverTests
{
    private static MethodInfo Method<T>(string name) => typeof(T).GetMethod(name)!;

    [Fact]
    public void Class_feature_applies_to_its_methods()
    {
        var feature = FeatureResolver.Resolve(Method<FeatureOnClass>(nameof(FeatureOnClass.Plain)));

        Assert.NotNull(feature);
        Assert.Equal(typeof(CheckoutFeature).FullName, feature.Id);
        Assert.Equal("Create an order", feature.Name);
        Assert.Equal("As a customer,\nI want to check out.", feature.Description);
        Assert.Equal([new FeatureLink("Spec", "https://example.com/spec")], feature.Links);
    }

    [Fact]
    public void Method_feature_overrides_class_feature()
        => Assert.Equal("Coupons", FeatureResolver.Resolve(Method<FeatureOnClass>(nameof(FeatureOnClass.Overridden)))?.Name);

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
    }

    [Fact]
    public void Each_feature_type_is_created_once()
        => Assert.Same(
            FeatureResolver.Resolve(Method<FeatureOnClass>(nameof(FeatureOnClass.Plain))),
            FeatureResolver.Resolve(Method<InheritsFeature>(nameof(FeatureOnClass.Plain))));
}

public class FeatureReportTests
{
    private static readonly ReportFeature Checkout = new("Shop.Checkout", "Create an order", "As a customer,\nI want to check out.", [new("Spec", "https://example.com/spec")]);

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

        var html = HtmlWriter.Render(c.Build(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "xunit");
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

        var html = HtmlWriter.Render(c.Build(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "xunit");
        var json = Regex.Match(html, """<script id="mtp-data" type="application/json">(.*?)</script>""", RegexOptions.Singleline).Groups[1].Value;

        Assert.Empty(JsonDocument.Parse(json).RootElement.GetProperty("features").EnumerateObject());
    }
}
