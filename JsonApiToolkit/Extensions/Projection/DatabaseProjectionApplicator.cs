using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Helpers;

namespace JsonApiToolkit.Extensions.Projection;

/// <summary>
/// Applies a database-level Select() projection to an IQueryable and materializes the result.
/// Uses reflection to invoke the generic EF Core methods with a runtime-determined type.
/// </summary>
internal static class DatabaseProjectionApplicator
{
    /// <summary>
    /// Applies <paramref name="projectionExpression"/> as a <c>Select()</c> on <paramref name="query"/>.
    /// Returns a non-generic <see cref="IQueryable"/> whose runtime element type is <paramref name="projectionType"/>.
    /// EF Core translates this to SQL with only the projected columns.
    /// </summary>
    internal static IQueryable ApplySelect<T>(
        IQueryable<T> query,
        Type projectionType,
        LambdaExpression projectionExpression
    )
        where T : class
    {
        MethodInfo selectMethod = ReflectionMethodCache.GetQueryableSelectMethod(
            typeof(T),
            projectionType
        );

        return (IQueryable)selectMethod.Invoke(null, [query, projectionExpression])!;
    }

    /// <summary>
    /// Materializes <paramref name="projectedQuery"/> via EF Core's <c>ToListAsync</c>,
    /// using reflection to invoke the correct generic overload for <paramref name="projectionType"/>.
    /// Returns a <see cref="List{T}"/> of <see cref="object"/> containing the projected results.
    /// </summary>
    internal static async Task<List<object>> MaterializeAsync(
        IQueryable projectedQuery,
        Type projectionType,
        CancellationToken cancellationToken = default
    )
    {
        MethodInfo toListAsyncMethod = ReflectionMethodCache.GetEfCoreToListAsyncMethod(
            projectionType
        );

        // Invokes Task<List<ProjectionType>> ToListAsync<ProjectionType>(IQueryable<ProjectionType>, CancellationToken)
        var task = (Task)toListAsyncMethod.Invoke(null, [projectedQuery, cancellationToken])!;

        await task.ConfigureAwait(false);

        // Task<List<ProjectionType>>.Result returns List<ProjectionType> boxed as object
        PropertyInfo resultProperty = ReflectionMethodCache.GetTaskResultProperty(projectionType);

        var list = (IList)resultProperty.GetValue(task)!;

        var result = new List<object>(list.Count);
        foreach (object item in list)
            result.Add(item);

        return result;
    }
}
