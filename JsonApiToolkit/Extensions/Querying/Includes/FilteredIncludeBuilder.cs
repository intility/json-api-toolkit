using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Helpers;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Builds filtered Include expressions for EF Core queries.
/// </summary>
public static class FilteredIncludeBuilder
{
    /// <summary>
    /// Applies filtered includes using EF Core's Include().Where() pattern.
    /// </summary>
    public static IQueryable<T> ApplyFilteredIncludes<T>(
        this IQueryable<T> query,
        List<string>? includePaths,
        List<IncludeFilter> includeFilters,
        ILogger? logger = null
    )
        where T : class
    {
        if (includePaths == null || includePaths.Count == 0)
            return query;

        // Use single query mode to prevent EF Core split query correlation issues.
        // Without this, filtered includes on one relationship can break other includes
        // because EF Core's split query correlation logic fails with mixed filtered/unfiltered includes.
        query = query.AsSingleQuery();

        var filtersByRelationship = includeFilters.ToDictionary(
            f => f.RelationshipPath,
            f => f.FilterGroup,
            StringComparer.OrdinalIgnoreCase
        );

        var sortedPaths = includePaths.OrderBy(p => p.Count(c => c == '.')).ToList();

        foreach (var includePath in sortedPaths)
        {
            var segments = includePath.Split('.');

            if (
                filtersByRelationship.TryGetValue(includePath, out var filterGroup)
                && (filterGroup.Filters.Count > 0 || filterGroup.Groups.Count > 0)
            )
            {
                query = ApplyFilteredIncludeChain(query, segments, filterGroup, typeof(T), logger);
            }
            else
            {
                query = query.Include(includePath);
            }
        }

        return query;
    }

    private static IQueryable<T> ApplyFilteredIncludeChain<T>(
        IQueryable<T> query,
        string[] pathSegments,
        FilterGroup filterGroup,
        Type rootType,
        ILogger? logger
    )
        where T : class
    {
        if (pathSegments.Length == 0)
            return query;

        if (pathSegments.Length == 1)
            return ApplyFilteredIncludeWithFilters(
                query,
                pathSegments[0],
                filterGroup,
                rootType,
                logger
            );

        if (pathSegments.Length == 2)
            return ApplyTwoLevelFilteredInclude(query, pathSegments, filterGroup, rootType, logger);

        logger?.LogWarning(
            "Filtered includes beyond 2 levels are not supported. Include path '{Path}' will use unfiltered include. Filters will be ignored.",
            string.Join(".", pathSegments)
        );
        return query.Include(string.Join(".", pathSegments));
    }

    private static IQueryable<T> ApplyTwoLevelFilteredInclude<T>(
        IQueryable<T> query,
        string[] pathSegments,
        FilterGroup filterGroup,
        Type rootType,
        ILogger? logger
    )
        where T : class
    {
        var firstProperty = QueryHelpers.GetPropertyByJsonName(rootType, pathSegments[0]);
        if (firstProperty == null)
            return query.Include(string.Join(".", pathSegments));

        var firstNavType = TypeHelpers.GetNavigationTargetType(firstProperty.PropertyType);
        var secondProperty = QueryHelpers.GetPropertyByJsonName(firstNavType, pathSegments[1]);
        if (secondProperty == null)
            return query.Include(string.Join(".", pathSegments));

        var isSecondCollection = TypeHelpers.IsCollectionType(secondProperty.PropertyType);
        if (!isSecondCollection)
        {
            logger?.LogWarning(
                "Cannot apply filters to reference navigation '{Path}'. Filters on single-valued navigations are not supported. Using unfiltered include.",
                string.Join(".", pathSegments)
            );
            return query.Include(string.Join(".", pathSegments));
        }

        var elementType = TypeHelpers.GetCollectionElementType(secondProperty.PropertyType);
        if (elementType == null)
            return query.Include(string.Join(".", pathSegments));

        try
        {
            var rootParam = Expression.Parameter(rootType, "root");
            var firstNavAccess = Expression.Property(rootParam, firstProperty);
            var includeLambda = Expression.Lambda(firstNavAccess, rootParam);

            var includeMethod = ReflectionMethodCache.GetEfCoreIncludeMethod(
                rootType,
                firstProperty.PropertyType
            );

            var includedQuery = includeMethod.Invoke(null, new object[] { query, includeLambda });

            var navParam = Expression.Parameter(firstNavType, "nav");
            var secondNavAccess = Expression.Property(navParam, secondProperty);

            var filterParam = Expression.Parameter(elementType, "item");

            // Use FilterExpressionBuilder to build the filter expression with proper logical operators
            var filterExpr = FilterExpressionBuilder.BuildFilterExpression(
                filterGroup,
                filterParam,
                elementType,
                logger
            );

            if (filterExpr != null)
            {
                var whereLambda = Expression.Lambda(filterExpr, filterParam);

                var whereMethod = ReflectionMethodCache.GetEnumerableWhere(elementType);

                var filteredCollection = Expression.Call(whereMethod, secondNavAccess, whereLambda);
                var thenIncludeLambda = Expression.Lambda(filteredCollection, navParam);

                var isFirstCollection = TypeHelpers.IsCollectionType(firstProperty.PropertyType);
                var thenIncludeMethod = EfCoreIncludeExpressions.GetThenIncludeMethod(
                    isFirstCollection,
                    rootType,
                    firstNavType,
                    filteredCollection.Type
                );

                var result = thenIncludeMethod.Invoke(
                    null,
                    new[] { includedQuery, thenIncludeLambda }
                );
                return (IQueryable<T>)result!;
            }
            else
            {
                var thenIncludeLambda = Expression.Lambda(secondNavAccess, navParam);

                var isFirstCollection = TypeHelpers.IsCollectionType(firstProperty.PropertyType);
                var thenIncludeMethod = EfCoreIncludeExpressions.GetThenIncludeMethod(
                    isFirstCollection,
                    rootType,
                    firstNavType,
                    secondProperty.PropertyType
                );

                var result = thenIncludeMethod.Invoke(
                    null,
                    new[] { includedQuery, thenIncludeLambda }
                );
                return (IQueryable<T>)result!;
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(
                ex,
                "Failed to build filtered include expression for '{Path}'. Falling back to unfiltered include.",
                string.Join(".", pathSegments)
            );
            return query.Include(string.Join(".", pathSegments));
        }
    }

    private static IQueryable<T> ApplyFilteredIncludeWithFilters<T>(
        IQueryable<T> query,
        string navigationPath,
        FilterGroup filterGroup,
        Type entityType,
        ILogger? logger
    )
        where T : class
    {
        var navigationProperty = QueryHelpers.GetPropertyByJsonName(entityType, navigationPath);
        if (navigationProperty == null)
            return query.Include(navigationPath);

        var propertyType = navigationProperty.PropertyType;
        var isCollection = TypeHelpers.IsCollectionType(propertyType);

        if (isCollection)
        {
            var elementType = TypeHelpers.GetCollectionElementType(propertyType);
            if (elementType != null)
            {
                var includeExpression = BuildFilteredIncludeExpression(
                    entityType,
                    navigationProperty,
                    elementType,
                    filterGroup,
                    logger
                );

                query = EfCoreIncludeExpressions.ApplyIncludeExpression(query, includeExpression);
            }
            else
            {
                query = query.Include(navigationPath);
            }
        }
        else
        {
            logger?.LogWarning(
                "Cannot apply filters to reference navigation '{Path}'. Filters on single-valued navigations are not supported. Using unfiltered include.",
                navigationPath
            );
            query = query.Include(navigationPath);
        }

        return query;
    }

    private static Expression? BuildFilteredIncludeExpression(
        Type entityType,
        PropertyInfo navigationProperty,
        Type elementType,
        FilterGroup filterGroup,
        ILogger? logger
    )
    {
        var entityParameter = Expression.Parameter(entityType, "e");
        var navigationAccess = Expression.Property(entityParameter, navigationProperty);
        var elementParameter = Expression.Parameter(elementType, "x");

        // Use FilterExpressionBuilder to build the filter expression with proper logical operators
        var filterExpression = FilterExpressionBuilder.BuildFilterExpression(
            filterGroup,
            elementParameter,
            elementType,
            logger
        );

        if (filterExpression == null)
            return null;

        var whereLambda = Expression.Lambda(filterExpression, elementParameter);

        var whereMethod = ReflectionMethodCache.GetEnumerableWhere(elementType);

        var filteredCollection = Expression.Call(whereMethod, navigationAccess, whereLambda);

        var includeLambda = Expression.Lambda(filteredCollection, entityParameter);

        return includeLambda;
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
}
