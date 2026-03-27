using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace JsonApiToolkit.Extensions.Projection;

/// <summary>
/// Caches generated projection types and their Select() expressions per (source type, field set).
/// Ensures the same field combination reuses the same runtime-generated type across requests.
/// </summary>
internal static class ProjectionTypeCache
{
    private static readonly ConcurrentDictionary<
        string,
        (Type ProjectionType, LambdaExpression Expression)
    > s_cache = new();

    /// <summary>
    /// Returns a cached or newly generated (projectionType, expression) pair for the given
    /// source type and property set.
    /// </summary>
    internal static (Type ProjectionType, LambdaExpression Expression) GetOrCreate(
        Type sourceType,
        IReadOnlyList<PropertyInfo> sourceProperties
    )
    {
        string key = BuildCacheKey(sourceType, sourceProperties);

        return s_cache.GetOrAdd(
            key,
            _ =>
            {
                var properties = sourceProperties.Select(p => (p.Name, p.PropertyType)).ToList();

                Type projectionType = DynamicTypeBuilder.Build(properties);
                LambdaExpression expression = ProjectionExpressionBuilder.Build(
                    sourceType,
                    projectionType,
                    sourceProperties
                );

                return (projectionType, expression);
            }
        );
    }

    // Sort by name so that {Name, Email} and {Email, Name} share the same cache entry.
    private static string BuildCacheKey(
        Type sourceType,
        IReadOnlyList<PropertyInfo> sourceProperties
    )
    {
        var sorted = sourceProperties
            .OrderBy(p => p.Name)
            .Select(p => $"{p.Name}:{p.PropertyType.FullName}");

        return $"{sourceType.FullName}|{string.Join("|", sorted)}";
    }
}
