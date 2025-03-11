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
        Type entityType = typeof(T);
        bool isFirstSort = true;

        foreach (SortParameter sortParam in sortParameters)
        {
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
            if (isFirstSort)
            {
                methodName = sortParam.IsDescending ? "OrderByDescending" : "OrderBy";
                isFirstSort = false;
            }
            else
            {
                methodName = sortParam.IsDescending ? "ThenByDescending" : "ThenBy";
            }

            MethodInfo orderByMethod = typeof(Queryable)
                .GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(entityType, property.PropertyType);

            query = (IQueryable<T>)orderByMethod.Invoke(null, [query, lambda])!;
        }

        return query;
    }
}
