using System.Collections.Concurrent;
using System.Reflection;

namespace Davish.JustBdd;

/// <summary>Feature data captured for the report. <see cref="Id"/> is the feature name, which identifies a feature.</summary>
internal sealed record ReportFeature(
    string Id,
    string Name,
    string? Description,
    IReadOnlyList<FeatureLink> Links,
    IReadOnlyList<KeyValuePair<string, IReadOnlyList<string>>> Metadata);

/// <summary>
/// Finds the feature of a test method: a method feature attribute overrides the class one. Links and metadata attributes
/// on the same class or method follow the feature's own links and metadata.
/// </summary>
internal static class FeatureResolver
{
    private static readonly ConcurrentDictionary<MethodInfo, ReportFeature?> Cache = new();

    public static ReportFeature? Resolve(MethodInfo method) => Cache.GetOrAdd(method, Create);

    private static ReportFeature? Create(MethodInfo method)
    {
        // Both [Feature("...")] and [Feature<T>] on one target are allowed by the compiler; the first one wins.
        MemberInfo? target = method.GetCustomAttributes<FeatureAttributeBase>(inherit: true).Any() ? method : method.ReflectedType;
        var attribute = target?.GetCustomAttributes<FeatureAttributeBase>(inherit: true).FirstOrDefault();
        if (target is null || attribute is null) return null;

        var feature = attribute.CreateFeature();
        var links = feature.Links
            .Concat(target.GetCustomAttributes<FeatureLinksAttribute>(inherit: true).Select(a => new FeatureLink(a.Title, a.Url)))
            .ToArray();
        var metadata = feature.Metadata
            .Concat(target.GetCustomAttributes<FeatureMetadataAttribute>(inherit: true).Select(a => new KeyValuePair<string, IReadOnlyList<string>>(a.Key, a.Values)))
            .ToArray();
        return new ReportFeature(feature.Name, feature.Name, feature.Description, links, metadata);
    }
}
