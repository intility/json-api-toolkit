using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Helpers;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Composes LINQ predicates from JSON:API <see cref="FilterGroup"/> trees.
/// Owns the full pipeline in one place: group combination (And/Or/Not),
/// path walking (scalar, dot-path navigation, collection <c>Any()</c>,
/// nested collections), operator semantics, null-safety guards, and
/// recursion-depth limiting.
/// </summary>
public sealed class FilterExpressionComposer
{
    /// <summary>
    /// Maximum recursion depth for nested collection navigations.
    /// Prevents stack overflow from malicious deeply nested filter paths.
    /// </summary>
    internal const int MaxRecursionDepth = 5;

    private readonly ILogger? _logger;
    private readonly Func<Type, string, PropertyInfo?> _resolveProperty;
    private readonly bool _strictValidation;

    /// <summary>
    /// Creates a composer. The optional <paramref name="propertyResolver"/> maps a JSON
    /// field name to a CLR property; defaults to <see cref="QueryHelpers.GetPropertyByJsonName"/>.
    /// </summary>
    public FilterExpressionComposer(
        ILogger? logger = null,
        Func<Type, string, PropertyInfo?>? propertyResolver = null,
        bool strictValidation = false
    )
    {
        _logger = logger;
        _resolveProperty = propertyResolver ?? QueryHelpers.GetPropertyByJsonName;
        _strictValidation = strictValidation;
    }

    /// <summary>
    /// Composes a predicate for <typeparamref name="T"/>, or null when the group
    /// yields no usable filters.
    /// </summary>
    public Expression<Func<T, bool>>? Compose<T>(FilterGroup group)
    {
        return (Expression<Func<T, bool>>?)Compose(group, typeof(T));
    }

    /// <summary>
    /// Composes a predicate lambda for <paramref name="entityType"/> (non-generic
    /// overload for Type-erased callers), or null when the group yields no usable filters.
    /// </summary>
    public LambdaExpression? Compose(FilterGroup group, Type entityType)
    {
        ParameterExpression parameter = Expression.Parameter(entityType, "x");
        Expression? body = BuildGroup(group, parameter);
        if (body == null)
            return null;

        Type delegateType = typeof(Func<,>).MakeGenericType(entityType, typeof(bool));
        return Expression.Lambda(delegateType, body, parameter);
    }

    private Expression? BuildGroup(FilterGroup group, ParameterExpression parameter)
    {
        var expressions = new List<Expression>();

        foreach (FilterParameter filter in group.Filters)
        {
            Expression? expr = BuildFilter(parameter, filter);
            if (expr != null)
                expressions.Add(expr);
            else
                _logger?.LogWarning("Failed to build filter for '{Field}'", filter.Field);
        }

        expressions.AddRange(
            group.Groups.Select(g => BuildGroup(g, parameter)).OfType<Expression>()
        );

        if (expressions.Count == 0)
            return null;

        if (group.LogicalOperator == LogicalOperator.Not)
        {
            return expressions
                .Select(e => (Expression)Expression.Not(e))
                .Aggregate((acc, next) => Expression.OrElse(acc, next));
        }

        return expressions.Aggregate(
            (acc, next) =>
                group.LogicalOperator == LogicalOperator.Or
                    ? Expression.OrElse(acc, next)
                    : Expression.AndAlso(acc, next)
        );
    }

    private Expression? BuildFilter(ParameterExpression parameter, FilterParameter filter)
    {
        if (filter.Field.Contains('.'))
            return BuildNestedPath(parameter, filter, depth: 0);

        PropertyInfo? property = _resolveProperty(parameter.Type, filter.Field);
        if (property == null)
        {
            if (_strictValidation)
                throw JsonApiErrors.InvalidFilterField(filter.Field, parameter.Type);

            _logger?.LogWarning(
                "Property '{Field}' not found on {EntityType}",
                filter.Field,
                parameter.Type.Name
            );
            return null;
        }

        return BuildLeaf(Expression.Property(parameter, property), filter);
    }

    /// <summary>
    /// Walks a dot-notation path (e.g. "author.address.city"), emitting a
    /// <c>!= null</c> guard per nullable navigation step and delegating to
    /// <see cref="BuildCollectionAny"/> when a step is a collection.
    /// </summary>
    private Expression? BuildNestedPath(
        ParameterExpression parameter,
        FilterParameter filter,
        int depth
    )
    {
        ThrowIfTooDeep(depth, filter);

        string[] parts = filter.Field.Split('.');
        Expression current = parameter;
        var nullChecks = new List<Expression>();

        for (int i = 0; i < parts.Length - 1; i++)
        {
            PropertyInfo? prop = _resolveProperty(current.Type, parts[i]);
            if (prop == null)
            {
                if (_strictValidation)
                    throw JsonApiErrors.InvalidFilterField(filter.Field, current.Type);

                _logger?.LogWarning(
                    "Property '{PropertyName}' not found on {Type} during navigation",
                    parts[i],
                    current.Type.Name
                );
                return null;
            }

            current = Expression.Property(current, prop);

            Type? elementType = TypeHelpers.GetCollectionElementType(prop.PropertyType);
            if (elementType != null)
            {
                string[] remainingParts = parts.Skip(i + 1).ToArray();
                Expression? collectionFilter = BuildCollectionAny(
                    current,
                    elementType,
                    remainingParts,
                    filter,
                    depth + 1
                );

                if (collectionFilter == null)
                    return null;

                // No null check for collection navigations:
                // 1. Collection navigations in EF Core are never truly null in SQL
                // 2. A null check forces MaterializeCollectionNavigation() which breaks many-to-many translation
                // 3. The Any() predicate handles empty collections correctly (returns false)
                Expression result = collectionFilter;
                for (int j = nullChecks.Count - 1; j >= 0; j--)
                    result = Expression.AndAlso(nullChecks[j], result);

                return result;
            }

            if (
                !prop.PropertyType.IsValueType
                || Nullable.GetUnderlyingType(prop.PropertyType) != null
            )
            {
                nullChecks.Add(Expression.NotEqual(current, Expression.Constant(null)));
            }
        }

        PropertyInfo? finalProp = _resolveProperty(current.Type, parts[^1]);
        if (finalProp == null)
        {
            if (_strictValidation)
                throw JsonApiErrors.InvalidFilterField(filter.Field, current.Type);

            _logger?.LogWarning(
                "Property '{PropertyName}' not found on {Type}",
                parts[^1],
                current.Type.Name
            );
            return null;
        }

        Expression? filterExpression = BuildLeaf(Expression.Property(current, finalProp), filter);
        if (filterExpression == null)
            return null;

        if (
            (filter.Operator == FilterOperator.Ne || filter.Operator == FilterOperator.Nin)
            && nullChecks.Count > 0
        )
        {
            // Ne/Nin semantics: a null anywhere in the chain counts as "not equal"
            Expression allNotNull = nullChecks.Aggregate(Expression.AndAlso);
            return Expression.OrElse(
                Expression.Not(allNotNull),
                Expression.AndAlso(allNotNull, filterExpression)
            );
        }

        // Outer null checks first: e.A != null && e.A.B != null && filterExpression
        Expression guarded = filterExpression;
        for (int i = nullChecks.Count - 1; i >= 0; i--)
            guarded = Expression.AndAlso(nullChecks[i], guarded);

        return guarded;
    }

    /// <summary>
    /// Builds <c>collection.Any(item =&gt; predicate)</c> for the remaining path
    /// segments of a collection navigation (e.g. <c>posts.title</c>).
    /// </summary>
    private Expression? BuildCollectionAny(
        Expression collectionAccess,
        Type elementType,
        string[] remainingParts,
        FilterParameter filter,
        int depth
    )
    {
        ThrowIfTooDeep(depth, filter);

        ParameterExpression itemParam = Expression.Parameter(elementType, "item");

        Expression? innerExpression;
        if (remainingParts.Length == 1)
        {
            PropertyInfo? prop = _resolveProperty(elementType, remainingParts[0]);
            if (prop == null)
            {
                if (_strictValidation)
                    throw JsonApiErrors.InvalidFilterField(filter.Field, elementType);

                _logger?.LogWarning(
                    "Property '{PropertyName}' not found on {Type}",
                    remainingParts[0],
                    elementType.Name
                );
                return null;
            }

            innerExpression = BuildLeaf(Expression.Property(itemParam, prop), filter);
        }
        else
        {
            var innerFilter = new FilterParameter
            {
                Field = string.Join(".", remainingParts),
                Value = filter.Value,
                Operator = filter.Operator,
                IsIncludeFilter = filter.IsIncludeFilter,
            };
            innerExpression = BuildNestedPath(itemParam, innerFilter, depth);
        }

        if (innerExpression == null)
            return null;

        LambdaExpression predicate = Expression.Lambda(innerExpression, itemParam);
        MethodInfo anyMethod = ReflectionMethodCache.GetEnumerableAnyWithPredicate(elementType);
        return Expression.Call(anyMethod, collectionAccess, predicate);
    }

    /// <summary>
    /// Builds the operator-specific expression for a resolved property access.
    /// Collection-typed properties (e.g. <c>List&lt;string&gt; Tags</c>) get
    /// Contains/Any semantics; everything else gets the scalar operator table.
    /// </summary>
    private Expression? BuildLeaf(Expression propertyAccess, FilterParameter filter)
    {
        Type targetType = propertyAccess.Type;

        Type? collectionElementType = TypeHelpers.GetCollectionElementType(targetType);
        if (collectionElementType != null)
            return BuildCollectionLeaf(propertyAccess, collectionElementType, filter);

        if (filter.Operator == FilterOperator.IsNull)
            return Expression.Equal(propertyAccess, Expression.Constant(null));

        if (filter.Operator == FilterOperator.IsNotNull)
            return Expression.NotEqual(propertyAccess, Expression.Constant(null));

        if (filter.Operator == FilterOperator.In || filter.Operator == FilterOperator.Nin)
        {
            try
            {
                return BuildInOrNinLeaf(propertyAccess, filter, targetType);
            }
            catch (ArgumentException) when (_strictValidation)
            {
                throw JsonApiErrors.InvalidFilterValue(filter.Field, filter.Value, targetType);
            }
        }

        if (filter.Operator == FilterOperator.Like)
            return BuildLikeExpression(propertyAccess, filter.Value);

        object? filterValue;
        try
        {
            filterValue = QueryHelpers.ConvertToPropertyType(filter.Value, targetType);
        }
        catch (FormatException) when (_strictValidation)
        {
            throw JsonApiErrors.InvalidFilterValue(filter.Field, filter.Value, targetType);
        }

        if (
            filterValue == null
            && filter.Operator != FilterOperator.Eq
            && filter.Operator != FilterOperator.Ne
        )
        {
            _logger?.LogWarning(
                "Failed to convert '{Value}' to {PropertyType}",
                FilterLogSanitizer.SanitizeForLog(filter.Value),
                targetType.Name
            );
            return null;
        }

        ConstantExpression constant = Expression.Constant(filterValue, targetType);

        return filter.Operator switch
        {
            FilterOperator.Eq => Expression.Equal(propertyAccess, constant),
            FilterOperator.Ne => Expression.NotEqual(propertyAccess, constant),
            FilterOperator.Gt => Expression.GreaterThan(propertyAccess, constant),
            FilterOperator.Ge => Expression.GreaterThanOrEqual(propertyAccess, constant),
            FilterOperator.Lt => Expression.LessThan(propertyAccess, constant),
            FilterOperator.Le => Expression.LessThanOrEqual(propertyAccess, constant),
            _ => Expression.Equal(propertyAccess, constant),
        };
    }

    private Expression BuildInOrNinLeaf(
        Expression propertyAccess,
        FilterParameter filter,
        Type targetType
    )
    {
        Expression contains;
        Type? underlying = Nullable.GetUnderlyingType(targetType);
        if (underlying != null)
        {
            Expression notNull = Expression.NotEqual(
                propertyAccess,
                Expression.Constant(null, targetType)
            );
            contains = Expression.AndAlso(
                notNull,
                BuildInExpression(
                    Expression.Property(propertyAccess, "Value"),
                    filter.Value,
                    underlying
                )
            );
        }
        else
        {
            contains = BuildInExpression(propertyAccess, filter.Value, targetType);
        }

        // Nin: null values count as "not in"
        return filter.Operator == FilterOperator.In ? contains : Expression.Not(contains);
    }

    /// <summary>
    /// Builds a filter expression when the property itself is a collection,
    /// e.g. <c>entity.Tags.Contains("value")</c> for <c>filter[tags][in]=value</c>.
    /// </summary>
    private Expression? BuildCollectionLeaf(
        Expression collectionAccess,
        Type elementType,
        FilterParameter filter
    )
    {
        if (filter.Operator == FilterOperator.Like)
        {
            // collection.Any(item => item.Contains(value))
            ParameterExpression itemParam = Expression.Parameter(elementType, "item");
            Expression containsCall = Expression.Call(
                itemParam,
                StringContainsMethod,
                Expression.Constant(StripLikeWildcards(filter.Value))
            );
            LambdaExpression predicate = Expression.Lambda(containsCall, itemParam);
            MethodInfo anyMethod = ReflectionMethodCache.GetEnumerableAnyWithPredicate(elementType);
            return Expression.Call(anyMethod, collectionAccess, predicate);
        }

        if (
            filter.Operator
            is FilterOperator.In
                or FilterOperator.Eq
                or FilterOperator.Nin
                or FilterOperator.Ne
        )
        {
            object? filterValue;
            try
            {
                filterValue = QueryHelpers.ConvertToPropertyType(filter.Value, elementType);
            }
            catch (FormatException) when (_strictValidation)
            {
                throw JsonApiErrors.InvalidFilterValue(filter.Field, filter.Value, elementType);
            }

            if (filterValue == null)
            {
                _logger?.LogWarning(
                    "Failed to convert '{Value}' to {ElementType} for collection filter",
                    FilterLogSanitizer.SanitizeForLog(filter.Value),
                    elementType.Name
                );
                return null;
            }

            Expression contains = Expression.Call(
                ReflectionMethodCache.GetEnumerableContains(elementType),
                collectionAccess,
                Expression.Constant(filterValue, elementType)
            );

            return filter.Operator is FilterOperator.In or FilterOperator.Eq
                ? contains
                : Expression.Not(contains);
        }

        if (filter.Operator == FilterOperator.IsNull)
            return Expression.Equal(collectionAccess, Expression.Constant(null));

        if (filter.Operator == FilterOperator.IsNotNull)
            return Expression.NotEqual(collectionAccess, Expression.Constant(null));

        if (_strictValidation)
        {
            throw new JsonApiBadRequestException(
                $"Operator '{filter.Operator}' is not supported for collection property '{filter.Field}'.",
                JsonApiErrorCodes.InvalidFilterOperator,
                new ErrorSource { Parameter = $"filter[{filter.Field}]" }
            );
        }

        _logger?.LogWarning(
            "Operator '{Operator}' is not supported for collection properties",
            filter.Operator
        );
        return null;
    }

    private static readonly MethodInfo StringContainsMethod = typeof(string).GetMethod(
        nameof(string.Contains),
        [typeof(string)]
    )!;

    /// <summary>
    /// Strips % only when the value has both leading AND trailing % (wildcard intent),
    /// preserving literal % in values like "100%" or "%discount".
    /// </summary>
    private static string StripLikeWildcards(string value)
    {
        return value.StartsWith('%') && value.EndsWith('%') && value.Length > 2
            ? value[1..^1]
            : value;
    }

    private static Expression BuildLikeExpression(Expression property, string value)
    {
        string cleanValue = StripLikeWildcards(value);

        if (property.Type == typeof(string))
            return Expression.Call(property, StringContainsMethod, Expression.Constant(cleanValue));

        Type? underlyingType = Nullable.GetUnderlyingType(property.Type);
        if (underlyingType != null || !property.Type.IsValueType)
        {
            Expression notNullCheck = Expression.NotEqual(
                property,
                Expression.Constant(null, property.Type)
            );

            MethodInfo? toStringMethod = property.Type.GetMethod("ToString", Type.EmptyTypes);
            if (toStringMethod == null)
            {
                toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes);
                property = Expression.Convert(property, typeof(object));
            }

            Expression containsCall = Expression.Call(
                Expression.Call(property, toStringMethod!),
                StringContainsMethod,
                Expression.Constant(cleanValue)
            );

            return Expression.AndAlso(notNullCheck, containsCall);
        }

        MethodInfo valueToString = property.Type.GetMethod("ToString", Type.EmptyTypes)!;
        return Expression.Call(
            Expression.Call(property, valueToString),
            StringContainsMethod,
            Expression.Constant(cleanValue)
        );
    }

    private static Expression BuildInExpression(
        Expression property,
        string value,
        Type propertyType
    )
    {
        var rawValues = value
            .Split(',')
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrEmpty(v))
            .ToList();

        var convertedValues = new List<object?>();
        var failedValues = new List<string>();

        foreach (var rawValue in rawValues)
        {
            try
            {
                var converted = QueryHelpers.ConvertToPropertyType(rawValue, propertyType);
                if (converted != null)
                    convertedValues.Add(converted);
            }
            catch (FormatException)
            {
                failedValues.Add(rawValue);
            }
        }

        if (failedValues.Count > 0)
        {
            throw new ArgumentException(
                $"Failed to convert the following values to type '{propertyType.Name}' for IN operator: {string.Join(", ", failedValues)}"
            );
        }

        if (convertedValues.Count == 0)
            return Expression.Constant(false);

        Type listElementType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        Type listType = typeof(List<>).MakeGenericType(listElementType);
        var typedList = (IList)Activator.CreateInstance(listType)!;
        foreach (object? item in convertedValues)
            typedList.Add(item);

        MethodInfo containsMethod =
            listType.GetMethod("Contains", [listElementType])
            ?? throw new InvalidOperationException("Cannot find 'Contains' method on list type.");

        if (property.Type != listElementType)
            property = Expression.Convert(property, listElementType);

        return Expression.Call(Expression.Constant(typedList, listType), containsMethod, property);
    }

    private static void ThrowIfTooDeep(int depth, FilterParameter filter)
    {
        if (depth <= MaxRecursionDepth)
            return;

        throw new JsonApiBadRequestException(
            $"Filter path recursion depth exceeds maximum of {MaxRecursionDepth}. "
                + "Simplify the filter expression or reduce collection nesting.",
            JsonApiErrorCodes.QueryTooComplex,
            new ErrorSource { Parameter = $"filter[{filter.Field}]" },
            new Dictionary<string, object>
            {
                ["field"] = filter.Field,
                ["maxDepth"] = MaxRecursionDepth,
                ["actualDepth"] = depth,
            }
        );
    }
}
