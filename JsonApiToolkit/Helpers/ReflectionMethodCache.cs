using System.Collections.Concurrent;
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
    private static MethodInfo? s_thenIncludeCollection;
    private static MethodInfo? s_thenIncludeReference;
    private static MethodInfo? s_queryableSelect;
    private static readonly ConcurrentDictionary<
        (Type Source, Type Projection),
        MethodInfo
    > s_queryableSelectInstances = new();
    private static MethodInfo? s_efCoreToListAsync;
    private static readonly ConcurrentDictionary<Type, MethodInfo> s_efCoreToListAsyncInstances =
        new();
    private static readonly ConcurrentDictionary<Type, PropertyInfo> s_taskResultProperties = new();
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
    /// Gets Queryable.Select&lt;TSource, TResult&gt;(IQueryable&lt;TSource&gt;, Expression&lt;Func&lt;TSource, TResult&gt;&gt;) method.
    /// The open generic method is cached once; the closed instantiation is cached per (sourceType, projectionType) pair.
    /// </summary>
    internal static MethodInfo GetQueryableSelectMethod(Type sourceType, Type projectionType)
    {
        return s_queryableSelectInstances.GetOrAdd(
            (sourceType, projectionType),
            key =>
            {
                if (s_queryableSelect == null)
                {
                    lock (s_lock)
                    {
                        s_queryableSelect ??=
                            typeof(Queryable)
                                .GetMethods()
                                .FirstOrDefault(m =>
                                    m.Name == "Select"
                                    && m.GetParameters().Length == 2
                                    && m.GetParameters()[1].ParameterType.IsGenericType
                                    && m.GetParameters()[1].ParameterType.GetGenericTypeDefinition()
                                        == typeof(Expression<>)
                                    // Distinguish Select<T,R>(expr) from Select<T,R>(indexExpr):
                                    // the non-indexed overload has Func<T,R> (2 type args), the other has Func<T,int,R> (3)
                                    && m.GetParameters()[1]
                                        .ParameterType.GetGenericArguments()[0]
                                        .GetGenericArguments()
                                        .Length == 2
                                )
                            ?? throw new InvalidOperationException(
                                "Could not find Queryable.Select<TSource, TResult>(IQueryable<TSource>, Expression<Func<TSource, TResult>>) method. "
                                    + "This is a core .NET method that should always exist. "
                                    + "Please report this issue at https://github.com/Intility/Intility.JsonApiToolkit/issues"
                            );
                    }
                }

                return s_queryableSelect.MakeGenericMethod(key.Source, key.Projection);
            }
        );
    }

    /// <summary>
    /// Gets EntityFrameworkQueryableExtensions.ToListAsync&lt;T&gt;(IQueryable&lt;T&gt;, CancellationToken) method.
    /// The open generic method is cached once; the closed instantiation is cached per element type.
    /// </summary>
    internal static MethodInfo GetEfCoreToListAsyncMethod(Type elementType)
    {
        return s_efCoreToListAsyncInstances.GetOrAdd(
            elementType,
            type =>
            {
                if (s_efCoreToListAsync == null)
                {
                    lock (s_lock)
                    {
                        s_efCoreToListAsync ??=
                            typeof(EntityFrameworkQueryableExtensions)
                                .GetMethods()
                                .FirstOrDefault(m =>
                                    m.Name == "ToListAsync"
                                    && m.GetParameters().Length == 2
                                    && m.GetParameters()[0].ParameterType.IsGenericType
                                    && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition()
                                        == typeof(IQueryable<>)
                                )
                            ?? throw new InvalidOperationException(
                                "Could not find EntityFrameworkQueryableExtensions.ToListAsync<T>(IQueryable<T>, CancellationToken) method. "
                                    + "Ensure Microsoft.EntityFrameworkCore is properly referenced. "
                                    + "Please report this issue at https://github.com/Intility/Intility.JsonApiToolkit/issues"
                            );
                    }
                }

                return s_efCoreToListAsync.MakeGenericMethod(type);
            }
        );
    }

    /// <summary>
    /// Gets the Result property of Task&lt;List&lt;T&gt;&gt; for the given projection type.
    /// Cached per projection type to avoid per-request reflection overhead.
    /// </summary>
    internal static PropertyInfo GetTaskResultProperty(Type projectionType)
    {
        return s_taskResultProperties.GetOrAdd(
            projectionType,
            type =>
            {
                var taskListType = typeof(Task<>).MakeGenericType(
                    typeof(List<>).MakeGenericType(type)
                );
                return taskListType.GetProperty("Result")
                    ?? throw new InvalidOperationException(
                        $"Could not access Result property on Task<List<{type.Name}>>. "
                            + "This is unexpected and may indicate a runtime compatibility issue."
                    );
            }
        );
    }

    /// <summary>
    /// Gets EntityFrameworkQueryableExtensions.ThenInclude method for either collection or reference navigation.
    /// The two overloads (collection and reference) are each cached once in static fields.
    /// </summary>
    internal static MethodInfo GetEfCoreThenIncludeMethod(
        bool isPreviousCollection,
        Type entityType,
        Type previousPropertyType,
        Type newPropertyType
    )
    {
        if (s_thenIncludeCollection == null || s_thenIncludeReference == null)
        {
            lock (s_lock)
            {
                if (s_thenIncludeCollection == null || s_thenIncludeReference == null)
                {
                    var candidates = typeof(EntityFrameworkQueryableExtensions)
                        .GetMethods()
                        .Where(m =>
                            m.Name == "ThenInclude"
                            && m.GetGenericArguments().Length == 3
                            && m.GetParameters().Length == 2
                            && m.GetParameters()[0].ParameterType.IsGenericType
                            && m.GetParameters()[0].ParameterType.GetGenericTypeDefinition().Name
                                == "IIncludableQueryable`2"
                        )
                        .ToList();

                    if (candidates.Count == 0)
                        throw new InvalidOperationException(
                            "Could not find EntityFrameworkQueryableExtensions.ThenInclude method. "
                                + "Ensure Microsoft.EntityFrameworkCore is properly referenced. "
                                + "Please report this issue at https://github.com/Intility/Intility.JsonApiToolkit/issues"
                        );

                    foreach (var candidate in candidates)
                    {
                        var secondGenericArg = candidate
                            .GetParameters()[0]
                            .ParameterType.GetGenericArguments()[1];

                        bool isCollection =
                            secondGenericArg.IsGenericType
                            && secondGenericArg.GetGenericTypeDefinition() == typeof(IEnumerable<>);

                        if (isCollection)
                            s_thenIncludeCollection = candidate;
                        else
                            s_thenIncludeReference = candidate;
                    }

                    // Fallback: if only one overload was found, use it for both
                    s_thenIncludeCollection ??=
                        s_thenIncludeReference
                        ?? throw new InvalidOperationException(
                            "Could not find EntityFrameworkQueryableExtensions.ThenInclude method. "
                                + "Ensure Microsoft.EntityFrameworkCore is properly referenced. "
                                + "Please report this issue at https://github.com/Intility/Intility.JsonApiToolkit/issues"
                        );
                    s_thenIncludeReference ??= s_thenIncludeCollection;
                }
            }
        }

        MethodInfo openMethod = isPreviousCollection
            ? s_thenIncludeCollection!
            : s_thenIncludeReference!;

        return openMethod.MakeGenericMethod(entityType, previousPropertyType, newPropertyType);
    }
}
