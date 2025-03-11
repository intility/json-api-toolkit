using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Models.Querying;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Helper class for applying sorting.
/// </summary>
public static class SortingHandler
{
    /// <summary>
    /// Applies sorting to an IQueryable.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="query">The IQueryable to apply the sorting to.</param>
    /// <param name="sortParameters">The sort parameters to apply.</param>
    /// <returns>The IQueryable with the sorting applied.</returns>
    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> query,
        List<SortParameter> sortParameters
    )
    {
        if (sortParameters == null || sortParameters.Count == 0)
        {
            return query;
        }

        IOrderedQueryable<T>? orderedQuery = null;
        Type entityType = typeof(T);

        for (int i = 0; i < sortParameters.Count; i++)
        {
            SortParameter sortParam = sortParameters[i];
            PropertyInfo? property = QueryHelpers.GetPropertyByJsonName(
                entityType,
                sortParam.Field
            );

            if (property == null)
                continue;

            ParameterExpression parameter = Expression.Parameter(entityType, "x");
            MemberExpression propertyAccess = Expression.Property(parameter, property);
            LambdaExpression lambda = Expression.Lambda(propertyAccess, parameter);

            string methodName;

            if (i == 0)
            {
                methodName = sortParam.IsDescending ? "OrderByDescending" : "OrderBy";
                orderedQuery = (IOrderedQueryable<T>?)
                    typeof(Queryable)
                        .GetMethods()
                        .Single(method =>
                            method.Name == methodName
                            && method.IsGenericMethodDefinition
                            && method.GetParameters().Length == 2
                        )
                        .MakeGenericMethod(entityType, property.PropertyType)
                        .Invoke(null, [query, lambda]);
            }
            else
            {
                methodName = sortParam.IsDescending ? "ThenByDescending" : "ThenBy";

                orderedQuery = (IOrderedQueryable<T>?)
                    typeof(Queryable)
                        .GetMethods()
                        .Single(method =>
                            method.Name == methodName
                            && method.IsGenericMethodDefinition
                            && method.GetParameters().Length == 2
                        )
                        .MakeGenericMethod(entityType, property.PropertyType)
                        .Invoke(null, [orderedQuery, lambda]);
            }
        }

        return orderedQuery ?? query;
    }
}
