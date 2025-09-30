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

        // Sort include paths by depth (process shorter paths first)
        var sortedPaths = includePaths.OrderBy(p => p.Count(c => c == '.')).ToList();

        // Process each include path
        foreach (var includePath in sortedPaths)
        {
            var segments = includePath.Split('.');

            // Check if this path has filters
            if (
                filtersByRelationship.TryGetValue(includePath, out var filters)
                && filters.Count > 0
            )
            {
                // Apply filtered include chain
                query = ApplyFilteredIncludeChain(query, segments, filters, typeof(T));
            }
            else
            {
                // Regular include without filters - use string-based Include
                query = query.Include(includePath);
            }
        }

        return query;
    }

    /// <summary>
    /// Applies a filtered include chain for nested relationships.
    /// For example: Include(v => v.Cve).ThenInclude(c => c.CveComments.Where(...))
    /// </summary>
    private static IQueryable<T> ApplyFilteredIncludeChain<T>(
        IQueryable<T> query,
        string[] pathSegments,
        List<IncludeFilter> filters,
        Type rootType
    )
        where T : class
    {
        if (pathSegments.Length == 0)
            return query;

        if (pathSegments.Length == 1)
        {
            // Simple case - single level filtered include
            return ApplyFilteredIncludeWithFilters(query, pathSegments[0], filters, rootType);
        }

        // Multi-level case: need to build Include().ThenInclude().ThenInclude()...
        // with filtering on the last level
        return ApplyNestedFilteredInclude(query, pathSegments, filters, rootType);
    }

    /// <summary>
    /// Builds a nested Include/ThenInclude chain with filtering on the deepest level.
    /// Uses string-based Include with manual query construction.
    /// </summary>
    private static IQueryable<T> ApplyNestedFilteredInclude<T>(
        IQueryable<T> query,
        string[] pathSegments,
        List<IncludeFilter> filters,
        Type rootType
    )
        where T : class
    {
        // For 2-level nesting (e.g., cve.cvecomments), we can build the chain
        if (pathSegments.Length == 2)
        {
            return ApplyTwoLevelFilteredInclude(query, pathSegments, filters, rootType);
        }

        // For deeper nesting, use string-based include (no filtering)
        // TODO: Implement full depth support
        var fullPath = string.Join(".", pathSegments);
        return query.Include(fullPath);
    }

    /// <summary>
    /// Special case for two-level includes: Include(x => x.Nav1).ThenInclude(x => x.Nav2.Where(...))
    /// </summary>
    private static IQueryable<T> ApplyTwoLevelFilteredInclude<T>(
        IQueryable<T> query,
        string[] pathSegments,
        List<IncludeFilter> filters,
        Type rootType
    )
        where T : class
    {
        var firstProperty = QueryHelpers.GetPropertyByJsonName(rootType, pathSegments[0]);
        if (firstProperty == null)
            return query.Include(string.Join(".", pathSegments));

        var firstNavType = GetNavigationTargetType(firstProperty.PropertyType);
        var secondProperty = QueryHelpers.GetPropertyByJsonName(firstNavType, pathSegments[1]);
        if (secondProperty == null)
            return query.Include(string.Join(".", pathSegments));

        var isSecondCollection = IsCollectionType(secondProperty.PropertyType);
        if (!isSecondCollection)
        {
            // Can't filter non-collection, use string-based include
            return query.Include(string.Join(".", pathSegments));
        }

        var elementType = GetCollectionElementType(secondProperty.PropertyType);
        if (elementType == null)
            return query.Include(string.Join(".", pathSegments));

        // Build: Include(v => v.FirstNav).ThenInclude(x => x.SecondNav.Where(filter))
        try
        {
            // Build Include expression for first nav
            var rootParam = Expression.Parameter(rootType, "root");
            var firstNavAccess = Expression.Property(rootParam, firstProperty);
            var includeLambda = Expression.Lambda(firstNavAccess, rootParam);

            // Apply the Include
            var includeMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .First(m =>
                    m.Name == "Include"
                    && m.GetParameters().Length == 2
                    && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition()
                        == typeof(Expression<>)
                )
                .MakeGenericMethod(rootType, firstProperty.PropertyType);

            var includedQuery = includeMethod.Invoke(null, new object[] { query, includeLambda });

            // Build ThenInclude expression with filtering
            var navParam = Expression.Parameter(firstNavType, "nav");
            var secondNavAccess = Expression.Property(navParam, secondProperty);

            // Build filter: nav.SecondNav.Where(filter)
            var filterParam = Expression.Parameter(elementType, "item");
            Expression? filterExpr = null;

            foreach (var filter in filters)
            {
                var singleFilterExpr = BuildSingleFilterExpression(filterParam, filter);
                if (singleFilterExpr != null)
                {
                    filterExpr =
                        filterExpr == null
                            ? singleFilterExpr
                            : Expression.OrElse(filterExpr, singleFilterExpr);
                }
            }

            if (filterExpr != null)
            {
                // Create Where lambda
                var whereLambda = Expression.Lambda(filterExpr, filterParam);

                // Call Where on the collection
                var whereMethod = typeof(Enumerable)
                    .GetMethods()
                    .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                    .MakeGenericMethod(elementType);

                var filteredCollection = Expression.Call(whereMethod, secondNavAccess, whereLambda);
                var thenIncludeLambda = Expression.Lambda(filteredCollection, navParam);

                // Apply ThenInclude
                var thenIncludeMethod = typeof(EntityFrameworkQueryableExtensions)
                    .GetMethods()
                    .First(m => m.Name == "ThenInclude" && m.GetGenericArguments().Length == 3)
                    .MakeGenericMethod(rootType, firstNavType, filteredCollection.Type);

                var result = thenIncludeMethod.Invoke(
                    null,
                    new[] { includedQuery, thenIncludeLambda }
                );
                return (IQueryable<T>)result!;
            }
            else
            {
                // No valid filter, use unfiltered ThenInclude
                var thenIncludeLambda = Expression.Lambda(secondNavAccess, navParam);

                var thenIncludeMethod = typeof(EntityFrameworkQueryableExtensions)
                    .GetMethods()
                    .First(m => m.Name == "ThenInclude" && m.GetGenericArguments().Length == 3)
                    .MakeGenericMethod(rootType, firstNavType, secondProperty.PropertyType);

                var result = thenIncludeMethod.Invoke(
                    null,
                    new[] { includedQuery, thenIncludeLambda }
                );
                return (IQueryable<T>)result!;
            }
        }
        catch
        {
            // Fallback to string-based include if expression building fails
            return query.Include(string.Join(".", pathSegments));
        }
    }

    private static LambdaExpression? BuildIncludeExpression(Type entityType, PropertyInfo property)
    {
        var parameter = Expression.Parameter(entityType, "x");
        var propertyAccess = Expression.Property(parameter, property);
        return Expression.Lambda(propertyAccess, parameter);
    }

    private static LambdaExpression? BuildThenIncludeExpression(
        Type entityType,
        PropertyInfo property,
        Type previousResultType
    )
    {
        var parameter = Expression.Parameter(entityType, "x");
        var propertyAccess = Expression.Property(parameter, property);
        return Expression.Lambda(propertyAccess, parameter);
    }

    private static LambdaExpression? BuildFilteredThenIncludeExpression(
        Type entityType,
        PropertyInfo property,
        List<IncludeFilter> filters,
        Type previousResultType
    )
    {
        var isCollection = IsCollectionType(property.PropertyType);

        if (!isCollection)
        {
            // Can't filter non-collection navigations
            return BuildThenIncludeExpression(entityType, property, previousResultType);
        }

        var elementType = GetCollectionElementType(property.PropertyType);
        if (elementType == null)
            return null;

        // Build: x => x.Navigation.Where(filter)
        var parameter = Expression.Parameter(entityType, "x");
        var navigationAccess = Expression.Property(parameter, property);
        var elementParameter = Expression.Parameter(elementType, "e");

        // Build combined filter expression (OR logic for multiple filters)
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

        // Create Where lambda
        var whereLambda = Expression.Lambda(filterExpression, elementParameter);

        // Call Where method
        var whereMethod = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
            .MakeGenericMethod(elementType);

        var filteredCollection = Expression.Call(whereMethod, navigationAccess, whereLambda);

        return Expression.Lambda(filteredCollection, parameter);
    }

    private static object ApplyIncludeToQuery(object query, LambdaExpression includeExpression)
    {
        var queryType = query.GetType().GetGenericArguments()[0];
        var navigationPropertyType = includeExpression.ReturnType;

        var includeMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .First(m =>
                m.Name == "Include"
                && m.GetParameters().Length == 2
                && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition()
                    == typeof(Expression<>)
            )
            .MakeGenericMethod(queryType, navigationPropertyType);

        return includeMethod.Invoke(null, new[] { query, includeExpression })!;
    }

    private static object ApplyThenIncludeToQuery(
        object query,
        LambdaExpression thenIncludeExpression,
        Type previousEntityType,
        Type previousNavigationType
    )
    {
        var queryType = query.GetType();
        var navigationPropertyType = thenIncludeExpression.ReturnType;

        // Find the right ThenInclude overload
        var thenIncludeMethod = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .FirstOrDefault(m =>
                m.Name == "ThenInclude"
                && m.GetParameters().Length == 2
                && m.GetGenericArguments().Length == 3
            );

        if (thenIncludeMethod == null)
            return query;

        // Get the element type for collections
        var previousEntityGenericType =
            GetCollectionElementType(previousNavigationType) ?? previousNavigationType;

        var entityType = queryType.GetGenericArguments()[0]; // TEntity
        var propertyType = thenIncludeExpression.ReturnType; // TProperty

        thenIncludeMethod = thenIncludeMethod.MakeGenericMethod(
            entityType,
            previousEntityGenericType,
            propertyType
        );

        return thenIncludeMethod.Invoke(null, new[] { query, thenIncludeExpression })!;
    }

    private static Type GetNavigationTargetType(Type navigationType)
    {
        return GetCollectionElementType(navigationType) ?? navigationType;
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
