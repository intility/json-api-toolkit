using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Builds LINQ expressions for JSON:API filter parameters.
/// Converts filter syntax to strongly-typed expressions for Entity Framework.
/// </summary>
public static class FilterExpressionBuilder
{
    /// <summary>
    /// Builds a composite filter expression from filter conditions and nested groups.
    /// Supports dot notation for nested properties (e.g., "user.address.city").
    /// </summary>
    /// <param name="group">Filter group with conditions and nested groups</param>
    /// <param name="parameter">Parameter expression for the entity</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>Expression for LINQ Where clause, or null if no valid filters</returns>
    public static Expression? BuildFilterExpression<T>(
        FilterGroup group,
        ParameterExpression parameter,
        ILogger? logger = null
    )
    {
        var expressions = new List<Expression>();

        foreach (FilterParameter filter in group.Filters)
        {
            Expression? expr;
            if (filter.Field.Contains('.'))
            {
                expr = BuildSingleFilterExpression(parameter, filter, logger);
            }
            else
            {
                PropertyInfo? property = QueryHelpers.GetPropertyByJsonName(
                    typeof(T),
                    filter.Field
                );
                if (property == null)
                {
                    logger?.LogWarning(
                        "Property '{Field}' not found on {Type}, skipping filter",
                        filter.Field,
                        typeof(T).Name
                    );
                    continue;
                }
                expr = BuildSingleFilterExpression(parameter, filter, logger);
            }

            if (expr != null)
            {
                expressions.Add(expr);
            }
            else
            {
                logger?.LogWarning("Failed to build filter for '{Field}'", filter.Field);
            }
        }

        foreach (FilterGroup nestedGroup in group.Groups)
        {
            Expression? nestedExpr = BuildFilterExpression<T>(nestedGroup, parameter, logger);
            if (nestedExpr != null)
            {
                expressions.Add(nestedExpr);
            }
        }

        if (expressions.Count == 0)
            return null;

        if (expressions.Count == 1)
        {
            Expression singleExpression = expressions[0];
            if (group.LogicalOperator == LogicalOperator.Not)
            {
                return Expression.Not(singleExpression);
            }
            return singleExpression;
        }

        Expression? combinedExpression = null;

        // For NOT: apply De Morgan's law
        // NOT(A AND B) = NOT(A) OR NOT(B)
        if (group.LogicalOperator == LogicalOperator.Not)
        {
            foreach (Expression expr in expressions)
            {
                var notExpr = Expression.Not(expr);
                combinedExpression =
                    combinedExpression == null
                        ? notExpr
                        : Expression.OrElse(combinedExpression, notExpr);
            }
        }
        else
        {
            foreach (Expression expr in expressions)
            {
                if (combinedExpression == null)
                {
                    combinedExpression = expr;
                }
                else
                {
                    combinedExpression = group.LogicalOperator switch
                    {
                        LogicalOperator.And => Expression.AndAlso(combinedExpression, expr),
                        LogicalOperator.Or => Expression.OrElse(combinedExpression, expr),
                        _ => Expression.AndAlso(combinedExpression, expr),
                    };
                }
            }
        }

        return combinedExpression;
    }

    /// <summary>
    /// Builds a filter expression for a single FilterParameter.
    /// </summary>
    /// <param name="parameter">Parameter expression for the entity</param>
    /// <param name="filter">Filter parameter to build</param>
    /// <param name="logger">Optional logger</param>
    /// <returns>Expression for the filter, or null if invalid</returns>
    public static Expression? BuildSingleFilterExpression(
        ParameterExpression parameter,
        FilterParameter filter,
        ILogger? logger = null
    )
    {
        if (filter.Field.Contains('.'))
        {
            return BuildSafeNestedFilterExpression(parameter, filter, logger);
        }

        PropertyInfo? property = QueryHelpers.GetPropertyByJsonName(parameter.Type, filter.Field);
        if (property == null)
        {
            logger?.LogWarning(
                "Property '{Field}' not found on {EntityType}",
                filter.Field,
                parameter.Type.Name
            );
            return null;
        }

        Expression propertyAccess = Expression.Property(parameter, property);
        return BuildPropertyFilterExpression(propertyAccess, filter, logger);
    }

    private static Expression BuildLikeExpression(Expression property, string value)
    {
        if (property.Type == typeof(string))
        {
            // For string types, use Contains directly
            MethodInfo? method = typeof(string).GetMethod("Contains", [typeof(string)]);
            return Expression.Call(property, method!, Expression.Constant(value));
        }

        // For non-string types, we need to handle nulls properly
        // Check if the property is nullable
        Type? underlyingType = Nullable.GetUnderlyingType(property.Type);
        if (underlyingType != null || !property.Type.IsValueType)
        {
            // Property is nullable or reference type - need null check
            // Create: property != null && property.ToString().Contains(value)

            // Null check
            Expression notNullCheck = Expression.NotEqual(
                property,
                Expression.Constant(null, property.Type)
            );

            // ToString call with null check
            MethodInfo? toStringMethod = property.Type.GetMethod("ToString", Type.EmptyTypes);
            if (toStringMethod == null)
            {
                // If no ToString method, use Object.ToString
                toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes);
                property = Expression.Convert(property, typeof(object));
            }

            MethodCallExpression toStringCall = Expression.Call(property, toStringMethod!);
            MethodInfo? containsMethod = typeof(string).GetMethod("Contains", [typeof(string)]);
            Expression containsCall = Expression.Call(
                toStringCall,
                containsMethod!,
                Expression.Constant(value)
            );

            // Combine: not null && contains
            return Expression.AndAlso(notNullCheck, containsCall);
        }
        else
        {
            // Non-nullable value type - can call ToString directly
            MethodInfo? toStringMethod = property.Type.GetMethod("ToString", Type.EmptyTypes);
            MethodCallExpression toStringCall = Expression.Call(property, toStringMethod!);
            MethodInfo? containsMethod = typeof(string).GetMethod("Contains", [typeof(string)]);
            return Expression.Call(toStringCall, containsMethod!, Expression.Constant(value));
        }
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
                {
                    convertedValues.Add(converted);
                }
            }
            catch (Exception)
            {
                // Track failed conversions
                failedValues.Add(rawValue);
            }
        }

        // If any values failed to convert, throw an exception with details
        if (failedValues.Count > 0)
        {
            throw new ArgumentException(
                $"Failed to convert the following values to type '{propertyType.Name}' for IN operator: {string.Join(", ", failedValues)}"
            );
        }

        if (convertedValues.Count == 0)
            return Expression.Constant(false);

        Type listElementType = propertyType;
        if (
            propertyType.IsGenericType
            && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
        )
        {
            listElementType = Nullable.GetUnderlyingType(propertyType)!;
        }

        Type listType = typeof(List<>).MakeGenericType(listElementType);

        var typedList = (IList)Activator.CreateInstance(listType)!;

        foreach (object? item in convertedValues)
            typedList.Add(item);

        ConstantExpression listConstant = Expression.Constant(typedList, listType);

        MethodInfo containsMethod =
            listType.GetMethod("Contains", [listElementType])
            ?? throw new InvalidOperationException("Cannot find 'Contains' method on list type.");

        if (property.Type != listElementType)
        {
            property = Expression.Convert(property, listElementType);
        }

        return Expression.Call(listConstant, containsMethod, property);
    }

    private static Expression? BuildSafeNestedFilterExpression(
        ParameterExpression parameter,
        FilterParameter filter,
        ILogger? logger = null
    )
    {
        string[] parts = filter.Field.Split('.');
        Expression current = parameter;
        var nullChecks = new List<Expression>();

        // Navigate through all but the last property
        for (int i = 0; i < parts.Length - 1; i++)
        {
            PropertyInfo? prop = QueryHelpers.GetPropertyByJsonName(current.Type, parts[i]);
            if (prop == null)
            {
                logger?.LogWarning(
                    "Property '{PropertyName}' not found on {Type} during navigation",
                    parts[i],
                    current.Type.Name
                );
                return null;
            }

            current = Expression.Property(current, prop);

            // Add null check for reference types
            if (
                !prop.PropertyType.IsValueType
                || Nullable.GetUnderlyingType(prop.PropertyType) != null
            )
            {
                nullChecks.Add(Expression.NotEqual(current, Expression.Constant(null)));
            }
        }

        // Get the final property
        PropertyInfo? finalProp = QueryHelpers.GetPropertyByJsonName(current.Type, parts[^1]);
        if (finalProp == null)
        {
            logger?.LogWarning(
                "Property '{PropertyName}' not found on {Type}",
                parts[^1],
                current.Type.Name
            );
            return null;
        }

        Expression finalProperty = Expression.Property(current, finalProp);
        Expression? filterExpression = BuildPropertyFilterExpression(finalProperty, filter, logger);
        if (filterExpression == null)
            return null;

        // For inequality: null != value is true
        // For equality: null == value needs all non-null checks
        Expression result;
        if (filter.Operator == FilterOperator.Ne || filter.Operator == FilterOperator.Nin)
        {
            if (nullChecks.Count > 0)
            {
                Expression allNotNull = nullChecks[0];
                for (int i = 1; i < nullChecks.Count; i++)
                    allNotNull = Expression.AndAlso(allNotNull, nullChecks[i]);

                Expression anyNull = Expression.Not(allNotNull);
                Expression notNullAndFilter = Expression.AndAlso(allNotNull, filterExpression);
                result = Expression.OrElse(anyNull, notNullAndFilter);
            }
            else
            {
                result = filterExpression;
            }
        }
        else
        {
            result = filterExpression;
            foreach (Expression nullCheck in nullChecks)
                result = Expression.AndAlso(nullCheck, result);
        }

        return result;
    }

    private static Expression? BuildPropertyFilterExpression(
        Expression propertyAccess,
        FilterParameter filter,
        ILogger? logger = null
    )
    {
        Type targetType = propertyAccess.Type;

        if (filter.Operator == FilterOperator.IsNull)
            return Expression.Equal(propertyAccess, Expression.Constant(null));

        if (filter.Operator == FilterOperator.IsNotNull)
            return Expression.NotEqual(propertyAccess, Expression.Constant(null));

        if (filter.Operator == FilterOperator.In)
        {
            Type? underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
            {
                BinaryExpression notNullExpr = Expression.NotEqual(
                    propertyAccess,
                    Expression.Constant(null, propertyAccess.Type)
                );
                Expression containsExpr = BuildInExpression(
                    Expression.Property(propertyAccess, "Value"),
                    filter.Value,
                    underlying
                );
                return Expression.AndAlso(notNullExpr, containsExpr);
            }
            return BuildInExpression(propertyAccess, filter.Value, targetType);
        }

        if (filter.Operator == FilterOperator.Nin)
        {
            Type? underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
            {
                BinaryExpression isNullExpr = Expression.Equal(
                    propertyAccess,
                    Expression.Constant(null, propertyAccess.Type)
                );
                Expression containsExpr = BuildInExpression(
                    Expression.Property(propertyAccess, "Value"),
                    filter.Value,
                    underlying
                );
                return Expression.OrElse(isNullExpr, Expression.Not(containsExpr));
            }
            return Expression.Not(BuildInExpression(propertyAccess, filter.Value, targetType));
        }

        object? filterValue = QueryHelpers.ConvertToPropertyType(filter.Value, targetType);
        if (
            filterValue == null
            && filter.Operator != FilterOperator.Eq
            && filter.Operator != FilterOperator.Ne
        )
        {
            logger?.LogWarning(
                "Failed to convert '{Value}' to {PropertyType}",
                filter.Value,
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
            FilterOperator.Like => BuildLikeExpression(propertyAccess, filter.Value),
            _ => Expression.Equal(propertyAccess, constant),
        };
    }
}
