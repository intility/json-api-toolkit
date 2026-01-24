using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace JsonApiToolkit.Helpers;

/// <summary>
/// Caches reflection method lookups for commonly used LINQ and EF Core methods.
/// Provides clear error messages when method lookups fail.
/// </summary>
internal static class ReflectionMethodCache
{
    private static MethodInfo? s_enumerableAnyWithPredicate;
    private static MethodInfo? s_enumerableContains;
    private static MethodInfo? s_enumerableWhere;
    private static MethodInfo? s_efCoreIncludeExpression;
    private static readonly Lock s_lock = new();

    /// <summary>
    /// Gets Enumerable.Any&lt;T&gt;(IEnumerable&lt;T&gt;, Func&lt;T, bool&gt;) method.
    /// </summary>
    internal static MethodInfo GetEnumerableAnyWithPredicate(Type elementType)
    {
        if (s_enumerableAnyWithPredicate == null)
        {
            lock (s_lock)
            {
                s_enumerableAnyWithPredicate ??=
                    typeof(Enumerable)
                        .GetMethods()
                        .FirstOrDefault(m =>
                            m.Name == "Any"
                            && m.GetParameters().Length == 2
                            && m.GetParameters()[1].ParameterType.IsGenericType
                            && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition()
                                == typeof(Func<,>)
                        )
                    ?? throw new InvalidOperationException(
                        "Could not find Enumerable.Any<T>(IEnumerable<T>, Func<T, bool>) method. "
                            + "This is a core .NET method that should always exist. "
                            + "Please report this issue at https://github.com/Intility/Intility.JsonApiToolkit/issues"
                    );
            }
        }

        return s_enumerableAnyWithPredicate.MakeGenericMethod(elementType);
    }

    /// <summary>
    /// Gets Enumerable.Contains&lt;T&gt;(IEnumerable&lt;T&gt;, T) method.
    /// </summary>
    internal static MethodInfo GetEnumerableContains(Type elementType)
    {
        if (s_enumerableContains == null)
        {
            lock (s_lock)
            {
                s_enumerableContains ??=
                    typeof(Enumerable)
                        .GetMethods()
                        .FirstOrDefault(m => m.Name == "Contains" && m.GetParameters().Length == 2)
                    ?? throw new InvalidOperationException(
                        "Could not find Enumerable.Contains<T>(IEnumerable<T>, T) method. "
                            + "This is a core .NET method that should always exist. "
                            + "Please report this issue at https://github.com/Intility/Intility.JsonApiToolkit/issues"
                    );
            }
        }

        return s_enumerableContains.MakeGenericMethod(elementType);
    }

    /// <summary>
    /// Gets Enumerable.Where&lt;T&gt;(IEnumerable&lt;T&gt;, Func&lt;T, bool&gt;) method.
    /// </summary>
    internal static MethodInfo GetEnumerableWhere(Type elementType)
    {
        if (s_enumerableWhere == null)
        {
            lock (s_lock)
            {
                s_enumerableWhere ??=
                    typeof(Enumerable)
                        .GetMethods()
                        .FirstOrDefault(m => m.Name == "Where" && m.GetParameters().Length == 2)
                    ?? throw new InvalidOperationException(
                        "Could not find Enumerable.Where<T>(IEnumerable<T>, Func<T, bool>) method. "
                            + "This is a core .NET method that should always exist. "
                            + "Please report this issue at https://github.com/Intility/Intility.JsonApiToolkit/issues"
                    );
            }
        }

        return s_enumerableWhere.MakeGenericMethod(elementType);
    }

    /// <summary>
    /// Gets Queryable.OrderBy, OrderByDescending, ThenBy, or ThenByDescending method.
    /// </summary>
    internal static MethodInfo GetQueryableOrderingMethod(
        string methodName,
        Type entityType,
        Type propertyType
    )
    {
        var method =
            typeof(Queryable)
                .GetMethods()
                .FirstOrDefault(m =>
                    m.Name == methodName
                    && m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 2
                )
            ?? throw new InvalidOperationException(
                $"Could not find Queryable.{methodName} method. "
                    + "This is a core .NET method that should always exist. "
                    + "Please report this issue at https://github.com/Intility/Intility.JsonApiToolkit/issues"
            );

        return method.MakeGenericMethod(entityType, propertyType);
    }

    /// <summary>
    /// Gets EntityFrameworkQueryableExtensions.Include&lt;TEntity, TProperty&gt; method with expression parameter.
    /// </summary>
    internal static MethodInfo GetEfCoreIncludeMethod(Type entityType, Type propertyType)
    {
        if (s_efCoreIncludeExpression == null)
        {
            lock (s_lock)
            {
                s_efCoreIncludeExpression ??=
                    typeof(EntityFrameworkQueryableExtensions)
                        .GetMethods()
                        .FirstOrDefault(m =>
                            m.Name == "Include"
                            && m.GetParameters().Length == 2
                            && m.GetParameters()[1].ParameterType.IsGenericType
                            && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition()
                                == typeof(Expression<>)
                        )
                    ?? throw new InvalidOperationException(
                        "Could not find EntityFrameworkQueryableExtensions.Include<TEntity, TProperty> method. "
                            + "Ensure Microsoft.EntityFrameworkCore is properly referenced. "
                            + "Please report this issue at https://github.com/Intility/Intility.JsonApiToolkit/issues"
                    );
            }
        }

        return s_efCoreIncludeExpression.MakeGenericMethod(entityType, propertyType);
    }

    /// <summary>
    /// Gets EntityFrameworkQueryableExtensions.ThenInclude method for either collection or reference navigation.
    /// </summary>
    internal static MethodInfo GetEfCoreThenIncludeMethod(
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

        // Fallback with defensive check
        var fallbackMethod =
            thenIncludeMethods.FirstOrDefault()
            ?? throw new InvalidOperationException(
                "Could not find EntityFrameworkQueryableExtensions.ThenInclude method. "
                    + "Ensure Microsoft.EntityFrameworkCore is properly referenced. "
                    + "Please report this issue at https://github.com/Intility/Intility.JsonApiToolkit/issues"
            );

        return fallbackMethod.MakeGenericMethod(entityType, previousPropertyType, newPropertyType);
    }
}
