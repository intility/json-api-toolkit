using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Helpers;
using Microsoft.EntityFrameworkCore;

namespace JsonApiToolkit.Extensions.Querying;

internal static class EfCoreIncludeExpressions
{
    internal static MethodInfo GetThenIncludeMethod(
        bool isPreviousCollection,
        Type entityType,
        Type previousPropertyType,
        Type newPropertyType
    )
    {
        return ReflectionMethodCache.GetEfCoreThenIncludeMethod(
            isPreviousCollection,
            entityType,
            previousPropertyType,
            newPropertyType
        );
    }

    internal static IQueryable<T> ApplyIncludeExpression<T>(
        IQueryable<T> query,
        Expression? includeExpression
    )
        where T : class
    {
        if (includeExpression == null)
            return query;

        var lambdaType = includeExpression.Type;
        if (lambdaType.IsGenericType && lambdaType.GetGenericTypeDefinition() == typeof(Func<,>))
        {
            var returnType = lambdaType.GetGenericArguments()[1];

            var includeMethod = ReflectionMethodCache.GetEfCoreIncludeMethod(typeof(T), returnType);

            return (IQueryable<T>)
                includeMethod.Invoke(null, new object[] { query, includeExpression })!;
        }

        return query;
    }
}
