using System.Collections.Concurrent;
using System.Reflection;

namespace Davish.JustBdd;

/// <summary>Feature data captured for the report. <see cref="Id"/> is the feature type's full name.</summary>
internal sealed record ReportFeature(string Id, string Name, string? Description, IReadOnlyList<FeatureLink> Links);

/// <summary>Finds the feature of a test method: a method attribute overrides the class attribute.</summary>
internal static class FeatureResolver
{
    // Create each feature type once; tests that share a feature share one instance.
    private static readonly ConcurrentDictionary<Type, ReportFeature> Cache = new();

    public static ReportFeature? Resolve(MethodInfo method)
    {
        var attribute = method.GetCustomAttribute<FeatureAttribute>(inherit: true)
            ?? method.ReflectedType?.GetCustomAttribute<FeatureAttribute>(inherit: true);
        return attribute is null ? null : Cache.GetOrAdd(attribute.FeatureType, _ => Create(attribute));
    }

    private static ReportFeature Create(FeatureAttribute attribute)
    {
        var feature = attribute.CreateFeature();
        return new ReportFeature(attribute.FeatureType.FullName ?? attribute.FeatureType.Name, feature.Name, feature.Description, feature.Links);
    }
}
