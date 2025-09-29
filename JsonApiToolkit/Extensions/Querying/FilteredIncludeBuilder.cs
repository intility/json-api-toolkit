using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.EntityFrameworkCore;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Builds filtered Include expressions for EF Core queries.
/// Uses EF Core's filtered Include() to apply filters on relationships.
/// </summary>
public static class FilteredIncludeBuilder
{
    /// <summary>
    /// Applies filtered includes using EF Core's Include().Where() pattern.
    /// </summary>
    public static IQueryable<T> ApplyFilteredIncludes<T>(
        this IQueryable<T> query,
        List<string>? includePaths,
        List<IncludeFilter> includeFilters
    )
        where T : class
    {
        if (includePaths == null || includePaths.Count == 0)
            return query;

        // Group filters by relationship path
        var filtersByRelationship = includeFilters
            .GroupBy(f => f.RelationshipPath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        // Process each include path
        foreach (var includePath in includePaths)
        {
            var segments = includePath.Split('.');
            query = ApplyFilteredInclude(query, segments, filtersByRelationship, typeof(T));
        }

        return query;
    }

    private static IQueryable<T> ApplyFilteredInclude<T>(
        IQueryable<T> query,
        string[] pathSegments,
        Dictionary<string, List<IncludeFilter>> filtersByRelationship,
        Type rootType
    )
        where T : class
    {
        if (pathSegments.Length == 0)
            return query;

        var currentPath = pathSegments[0];
        var fullPath = currentPath;

        // Build the Include expression
        var parameter = Expression.Parameter(rootType, "x");
        var includeProperty = GetPropertyExpression(parameter, currentPath, rootType);

        if (includeProperty == null)
            return query;

        // Check if we have filters for this relationship
        if (filtersByRelationship.TryGetValue(fullPath, out var filters) && filters.Count > 0)
        {
            // Apply filtered include
            query = ApplyFilteredIncludeWithFilters(query, currentPath, filters, rootType);
        }
        else
        {
            // Regular include without filters
            query = query.Include(currentPath);
        }

        // Handle nested includes recursively
        if (pathSegments.Length > 1)
        {
            var remainingPath = string.Join(".", pathSegments.Skip(1));

            // For nested includes, we need to check if there are filters at deeper levels
            var nestedPath = string.Join(".", pathSegments.Take(2));
            if (
                filtersByRelationship.TryGetValue(nestedPath, out var nestedFilters)
                && nestedFilters.Count > 0
            )
            {
                // We have filters at a deeper level - this requires special handling
                // For now, we'll include the nested path normally
                query = query.Include($"{currentPath}.{remainingPath}");
            }
            else
            {
                // Regular nested include
                query = query.Include($"{currentPath}.{remainingPath}");
            }
        }

        return query;
    }

    private static IQueryable<T> ApplyFilteredIncludeWithFilters<T>(
        IQueryable<T> query,
        string navigationPath,
        List<IncludeFilter> filters,
        Type entityType
    )
        where T : class
    {
        // Get the navigation property info
        var navigationProperty = QueryHelpers.GetPropertyByJsonName(entityType, navigationPath);
        if (navigationProperty == null)
            return query.Include(navigationPath); // Fallback to regular include

        // Determine if it's a collection or single navigation
        var propertyType = navigationProperty.PropertyType;
        var isCollection = IsCollectionType(propertyType);

        if (isCollection)
        {
            // Build filtered include for collection
            var elementType = GetCollectionElementType(propertyType);
            if (elementType != null)
            {
                var includeExpression = BuildFilteredIncludeExpression(
                    entityType,
                    navigationProperty,
                    elementType,
                    filters
                );

                // Apply the filtered include
                query = ApplyIncludeExpression(query, includeExpression);
            }
            else
            {
                // Fallback to regular include
                query = query.Include(navigationPath);
            }
        }
        else
        {
            // For single navigations, we can't filter - just include normally
            query = query.Include(navigationPath);
        }

        return query;
    }

    private static Expression? BuildFilteredIncludeExpression(
        Type entityType,
        PropertyInfo navigationProperty,
        Type elementType,
        List<IncludeFilter> filters
    )
    {
        // Create parameter for the main entity (e.g., Blog)
        var entityParameter = Expression.Parameter(entityType, "e");

        // Create the navigation property access (e.g., e.Posts)
        var navigationAccess = Expression.Property(entityParameter, navigationProperty);

        // Create parameter for the collection element (e.g., Post)
        var elementParameter = Expression.Parameter(elementType, "x");

        // Build filter expression for the collection elements
        Expression? filterExpression = null;

        foreach (var filter in filters)
        {
            var singleFilterExpr = BuildSingleFilterExpression(elementParameter, filter);

            if (singleFilterExpr != null)
            {
                filterExpression =
                    filterExpression == null
                        ? singleFilterExpr
                        : Expression.OrElse(filterExpression, singleFilterExpr);
            }
        }

        if (filterExpression == null)
            return null;

        // Create the Where lambda: x => [filter expression]
        var whereLambda = Expression.Lambda(filterExpression, elementParameter);

        // Get the Where method for IEnumerable<T>
        var whereMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

        // Create the filtered collection expression: navigation.Where(lambda)
        var filteredCollection = Expression.Call(whereMethod, navigationAccess, whereLambda);

        // Create the final lambda: e => e.Navigation.Where(filter)
        var includeLambda = Expression.Lambda(filteredCollection, entityParameter);

        return includeLambda;
    }

    private static Expression? BuildSingleFilterExpression(
        ParameterExpression parameter,
        IncludeFilter filter
    )
    {
        // Get the property path within the related entity
        var property = GetPropertyExpression(parameter, filter.FieldPath, parameter.Type);
        if (property == null)
            return null;

        // Use the existing FilterExpressionBuilder logic
        var filterParam = new FilterParameter
        {
            Field = filter.FieldPath,
            Operator = filter.Filter.Operator,
            Value = filter.Filter.Value,
        };

        return FilterExpressionBuilder.BuildSingleFilterExpression(parameter, filterParam);
    }

    private static IQueryable<T> ApplyIncludeExpression<T>(
        IQueryable<T> query,
        Expression? includeExpression
    )
        where T : class
    {
        if (includeExpression == null)
            return query;

        // Get the return type of the lambda expression
        var lambdaType = includeExpression.Type;
        if (lambdaType.IsGenericType && lambdaType.GetGenericTypeDefinition() == typeof(Func<,>))
        {
            var returnType = lambdaType.GetGenericArguments()[1];

            // Use reflection to call the Include method with the expression
            var includeMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .First(m =>
                    m.Name == "Include"
                    && m.GetParameters().Length == 2
                    && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition()
                        == typeof(Expression<>)
                )
                .MakeGenericMethod(typeof(T), returnType);

            return (IQueryable<T>)
                includeMethod.Invoke(null, new object[] { query, includeExpression })!;
        }

        return query;
    }

    private static MemberExpression? GetPropertyExpression(
        Expression parameter,
        string propertyPath,
        Type entityType
    )
    {
        if (string.IsNullOrEmpty(propertyPath))
            return null;

        var parts = propertyPath.Split('.');
        Expression current = parameter;
        Type currentType = entityType;

        foreach (var part in parts)
        {
            var property = QueryHelpers.GetPropertyByJsonName(currentType, part);
            if (property == null)
                return null;

            current = Expression.Property(current, property);
            currentType = property.PropertyType;
        }

        return current as MemberExpression;
    }

    private static bool IsCollectionType(Type type)
    {
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            return genericTypeDef == typeof(ICollection<>)
                || genericTypeDef == typeof(IList<>)
                || genericTypeDef == typeof(List<>)
                || genericTypeDef == typeof(IEnumerable<>)
                || genericTypeDef == typeof(HashSet<>)
                || genericTypeDef == typeof(ISet<>);
        }

        return type.IsArray
            || type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    private static Type? GetCollectionElementType(Type collectionType)
    {
        if (collectionType.IsArray)
            return collectionType.GetElementType();

        if (collectionType.IsGenericType)
        {
            return collectionType.GetGenericArguments().FirstOrDefault();
        }

        var enumerableInterface = collectionType
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            );

        return enumerableInterface?.GetGenericArguments().FirstOrDefault();
    }
}
