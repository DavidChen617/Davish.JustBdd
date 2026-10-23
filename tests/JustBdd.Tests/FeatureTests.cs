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

    [Feature("Checkout", Description = "As a customer, I want to check out.")]
    private sealed class Described;

    [Feature("Checkout")]
    [FeatureLinks("Spec", "https://example.com/spec")]
    [FeatureLinks("Ticket", "https://example.com/SHOP-1")]
    [FeatureMetadata("Owner", "team-checkout")]
    [FeatureMetadata("Tags", "checkout", "coupon")]
    private sealed class Annotated;

    [Fact]
    public void Generic_attribute_exposes_its_feature_type_and_creates_the_feature()
    {
        var generic = new FeatureAttribute<Checkout>();
        FeatureAttributeBase attribute = generic;

        Assert.Equal(typeof(Checkout), generic.FeatureType);
        var feature = attribute.CreateFeature();
        Assert.IsType<Checkout>(feature);
        Assert.Equal("Checkout", feature.Name);
        Assert.Equal("As a customer, I want to check out.", feature.Description);
        Assert.Equal([new FeatureLink("Spec", "https://example.com/spec")], feature.Links);
    }

    [Fact]
    public void Description_links_and_metadata_are_optional()
    {
        IFeature feature = new Minimal();

        Assert.Null(feature.Description);
        Assert.Empty(feature.Links);
        Assert.Empty(feature.Metadata);
    }

    [Fact]
    public void Attribute_is_readable_through_the_non_generic_base()
    {
        var attribute = typeof(Tagged).GetCustomAttribute<FeatureAttributeBase>();

        Assert.IsType<Checkout>(attribute?.CreateFeature());
    }

    [Fact]
    public void Attribute_targets_classes_and_methods_once_and_is_inherited()
    {
        var usage = typeof(FeatureAttribute<>).GetCustomAttribute<AttributeUsageAttribute>()!;

        Assert.Equal(AttributeTargets.Class | AttributeTargets.Method, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
        Assert.True(usage.Inherited);
    }

    [Fact]
    public void String_attribute_defines_name_and_description()
    {
        var feature = typeof(Described).GetCustomAttribute<FeatureAttributeBase>()!.CreateFeature();

        Assert.Equal("Checkout", feature.Name);
        Assert.Equal("As a customer, I want to check out.", feature.Description);
        Assert.Empty(feature.Links);
        Assert.Empty(feature.Metadata);
    }

    [Fact]
    public void Links_and_metadata_attributes_can_repeat()
    {
        var links = typeof(Annotated).GetCustomAttributes<FeatureLinksAttribute>().ToArray();
        var metadata = typeof(Annotated).GetCustomAttributes<FeatureMetadataAttribute>().ToArray();

        Assert.Equal([("Spec", "https://example.com/spec"), ("Ticket", "https://example.com/SHOP-1")], links.Select(l => (l.Title, l.Url)));
        Assert.Equal(["Owner", "Tags"], metadata.Select(m => m.Key));
        Assert.Equal(["checkout", "coupon"], metadata[1].Values);
    }
}
