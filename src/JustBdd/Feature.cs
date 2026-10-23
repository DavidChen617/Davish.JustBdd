namespace Davish.JustBdd;

/// <summary>
/// A feature that groups scenarios in reports. Define it once and reference it with <see cref="FeatureAttribute{T}"/>.
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
    string Name { get; }

    /// <summary>Plain text; line breaks are preserved in reports.</summary>
    string? Description => null;

    IReadOnlyList<FeatureLink> Links => [];
}

/// <param name="Url">Reports render only http and https links.</param>
public sealed record FeatureLink(string Title, string Url);

/// <summary>
/// Non-generic base that lets readers find a feature without knowing its type.
/// Apply <see cref="FeatureAttribute{T}"/> instead.
/// </summary>
public abstract class FeatureAttribute : Attribute
{
    // Only FeatureAttribute<T> can derive from this class.
    private protected FeatureAttribute() { }

    public abstract Type FeatureType { get; }

    /// <summary>Creates a new feature instance on every call.</summary>
    public abstract IFeature CreateFeature();
}

/// <summary>
/// Assigns a test class or method to feature <typeparamref name="T"/>. A method attribute overrides the class attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class FeatureAttribute<T> : FeatureAttribute where T : IFeature, new()
{
    public override Type FeatureType => typeof(T);

    public override IFeature CreateFeature() => new T();
}
