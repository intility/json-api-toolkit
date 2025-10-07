using System.Linq.Expressions;
using System.Reflection;
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
        var thenIncludeMethods = typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .Where(m => m.Name == "ThenInclude" && m.GetGenericArguments().Length == 3)
            .ToList();

        foreach (var method in thenIncludeMethods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length != 2)
                continue;

            var firstParamType = parameters[0].ParameterType;
            if (
                !firstParamType.IsGenericType
                || firstParamType.GetGenericTypeDefinition().Name != "IIncludableQueryable`2"
            )
                continue;

            var genericArgs = firstParamType.GetGenericArguments();
            if (genericArgs.Length != 2)
                continue;

            var secondGenericArg = genericArgs[1];

            bool isCollectionOverload =
                secondGenericArg.IsGenericType
                && secondGenericArg.GetGenericTypeDefinition() == typeof(IEnumerable<>);

            if (isCollectionOverload == isPreviousCollection)
                return method.MakeGenericMethod(entityType, previousPropertyType, newPropertyType);
        }

        return typeof(EntityFrameworkQueryableExtensions)
            .GetMethods()
            .First(m => m.Name == "ThenInclude" && m.GetGenericArguments().Length == 3)
            .MakeGenericMethod(entityType, previousPropertyType, newPropertyType);
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
}
