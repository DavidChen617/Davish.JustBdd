using System.Reflection;

namespace Davish.JustBdd.Tests;

public class FeatureTests
{
    private sealed class Checkout : IFeature
    {
        public string Name => "Checkout";
        public string? Description => "As a customer, I want to check out.";
        public IReadOnlyList<FeatureLink> Links => [new("Spec", "https://example.com/spec")];
    }

    private sealed class Minimal : IFeature
    {
        public string Name => "Minimal";
    }

    [Feature<Checkout>]
    private sealed class Tagged;

    [Fact]
    public void Generic_attribute_exposes_its_feature_type_and_creates_the_feature()
    {
        FeatureAttribute attribute = new FeatureAttribute<Checkout>();

        Assert.Equal(typeof(Checkout), attribute.FeatureType);
        var feature = attribute.CreateFeature();
        Assert.IsType<Checkout>(feature);
        Assert.Equal("Checkout", feature.Name);
        Assert.Equal("As a customer, I want to check out.", feature.Description);
        Assert.Equal([new FeatureLink("Spec", "https://example.com/spec")], feature.Links);
    }

    [Fact]
    public void Description_and_links_are_optional()
    {
        IFeature feature = new Minimal();

        Assert.Null(feature.Description);
        Assert.Empty(feature.Links);
    }

    [Fact]
    public void Attribute_is_readable_through_the_non_generic_base()
    {
        var attribute = typeof(Tagged).GetCustomAttribute<FeatureAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(typeof(Checkout), attribute.FeatureType);
    }

    [Fact]
    public void Attribute_targets_classes_and_methods_once_and_is_inherited()
    {
        var usage = typeof(FeatureAttribute<>).GetCustomAttribute<AttributeUsageAttribute>()!;

        Assert.Equal(AttributeTargets.Class | AttributeTargets.Method, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
        Assert.True(usage.Inherited);
    }
}
