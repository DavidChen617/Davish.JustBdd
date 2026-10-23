namespace Davish.JustBdd;

/// <summary>
/// A feature that groups scenarios in reports. Define it as a class to share it or to add links and metadata,
/// and reference it with <see cref="FeatureAttribute{T}"/>; or declare it inline with <see cref="FeatureAttribute"/>.
/// <code>
/// public sealed class CheckoutFeature : IFeature
/// {
///     public string Name => "Create an order";
///     public string? Description => "As a customer, I want to check out.";
/// }
///
/// [Feature&lt;CheckoutFeature&gt;]
/// public class CheckoutTests { ... }
/// </code>
/// </summary>
public interface IFeature
{
    /// <summary>Identifies the feature: features with the same name are the same feature in reports.</summary>
    string Name { get; }

    /// <summary>Plain text; line breaks are preserved in reports.</summary>
    string? Description => null;

    IReadOnlyList<FeatureLink> Links => [];

    /// <summary>Free-form key and values, shown in declaration order.</summary>
    IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> Metadata => [];
}

/// <param name="Url">Reports render only http and https links.</param>
public sealed record FeatureLink(string Title, string Url);

/// <summary>
/// Base of the feature attributes, which lets readers find a feature without knowing how it is declared.
/// Apply <see cref="FeatureAttribute"/> or <see cref="FeatureAttribute{T}"/> instead.
/// </summary>
public abstract class FeatureAttributeBase : Attribute
{
    // Only the feature attributes in this assembly can derive from this class.
    private protected FeatureAttributeBase() { }

    /// <summary>Creates a new feature instance on every call.</summary>
    public abstract IFeature CreateFeature();
}

/// <summary>
/// Assigns a test class or method to a feature declared inline. A method attribute overrides the class attribute.
/// Add links and metadata with <see cref="FeatureLinksAttribute"/> and <see cref="FeatureMetadataAttribute"/>.
/// <code>
/// [Feature("Checkout", Description = "As a customer, I want to check out.")]
/// public class CheckoutTests { ... }
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class FeatureAttribute(string name) : FeatureAttributeBase
{
    public string Name { get; } = name;

    /// <summary>Plain text; line breaks are preserved in reports.</summary>
    public string? Description { get; set; }

    public override IFeature CreateFeature() => new DeclaredFeature(Name, Description);

    private sealed class DeclaredFeature(string name, string? description) : IFeature
    {
        public string Name { get; } = name;
        public string? Description { get; } = description;
    }
}

/// <summary>
/// Assigns a test class or method to feature <typeparamref name="T"/>. A method attribute overrides the class attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class FeatureAttribute<T> : FeatureAttributeBase where T : IFeature, new()
{
    public Type FeatureType => typeof(T);

    public override IFeature CreateFeature() => new T();
}

/// <summary>
/// Adds a link to the feature declared on the same class or method; links follow the feature's own links.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class FeatureLinksAttribute(string title, string url) : Attribute
{
    public string Title { get; } = title;

    /// <summary>Reports render only http and https links.</summary>
    public string Url { get; } = url;
}

/// <summary>
/// Adds metadata to the feature declared on the same class or method; it follows the feature's own metadata.
/// <code>
/// [FeatureMetadata("Tags", "checkout", "coupon")]
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class FeatureMetadataAttribute(string key, params string[] values) : Attribute
{
    public string Key { get; } = key;

    public IReadOnlyList<string> Values { get; } = values;
}
