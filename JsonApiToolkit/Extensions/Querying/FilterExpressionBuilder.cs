using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Builds LINQ expressions for applying JSON:API filter parameters to entity queries.
/// </summary>
/// <remarks>
/// This utility class converts JSON:API filter syntax into strongly-typed LINQ expressions
/// that can be used with Entity Framework or other LINQ providers.
/// </remarks>
public static class FilterExpressionBuilder
{
    /// <summary>
    /// Builds a composite filter expression from a group of filter conditions.
    /// </summary>
    /// <typeparam name="T">The entity type being filtered</typeparam>
    /// <param name="group">The filter group containing conditions and nested groups</param>
    /// <param name="parameter">The parameter expression representing the entity in the LINQ expression</param>
    /// <param name="logger">Optional logger for debugging and tracing</param>
    /// <returns>
    /// A composite Expression that can be used in a LINQ Where clause, or null if no valid filters exist
    /// </returns>
    /// <remarks>
    /// Handles both simple filters and complex nested filter groups with different logical operators.
    /// For nested properties, supports dot notation (e.g., "user.address.city").
    /// </remarks>
    public static Expression? BuildFilterExpression<T>(
        FilterGroup group,
        ParameterExpression parameter,
        ILogger? logger = null
    )
    {
        logger?.LogDebug(
            "Building filter expression for {FilterCount} filters and {GroupCount} nested groups with logical operator {LogicalOperator}",
            group.Filters.Count,
            group.Groups.Count,
            group.LogicalOperator
        );

        var expressions = new List<Expression>();

        foreach (FilterParameter filter in group.Filters)
        {
            logger?.LogDebug(
                "Processing filter: Field='{Field}', Operator={Operator}, Value='{Value}'",
                filter.Field,
                filter.Operator,
                filter.Value
            );

            Expression? expr;
            if (filter.Field.Contains('.'))
            {
                logger?.LogDebug(
                    "Building nested property filter expression for field '{Field}'",
                    filter.Field
                );
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
                        "Property '{Field}' not found on type {Type}, skipping filter",
                        filter.Field,
                        typeof(T).Name
                    );
                    continue;
                }
                logger?.LogDebug(
                    "Building simple property filter expression for field '{Field}' -> property '{PropertyName}'",
                    filter.Field,
                    property.Name
                );
                expr = BuildSingleFilterExpression(parameter, filter, logger);
            }

            if (expr != null)
            {
                logger?.LogDebug(
                    "Successfully built filter expression for field '{Field}'",
                    filter.Field
                );
                expressions.Add(expr);
            }
            else
            {
                logger?.LogWarning(
                    "Failed to build filter expression for field '{Field}'",
                    filter.Field
                );
            }
        }

        foreach (FilterGroup nestedGroup in group.Groups)
        {
            logger?.LogDebug(
                "Processing nested filter group with {NestedFilterCount} filters and logical operator {NestedLogicalOperator}",
                nestedGroup.Filters.Count,
                nestedGroup.LogicalOperator
            );
            Expression? nestedExpr = BuildFilterExpression<T>(nestedGroup, parameter, logger);
            if (nestedExpr != null)
            {
                logger?.LogDebug("Successfully built nested group expression");
                expressions.Add(nestedExpr);
            }
            else
            {
                logger?.LogDebug("Nested group expression resulted in null");
            }
        }

        if (expressions.Count == 0)
        {
            logger?.LogDebug("No valid filter expressions found, returning null");
            return null;
        }

        if (expressions.Count == 1)
        {
            Expression singleExpression = expressions[0];
            if (group.LogicalOperator == LogicalOperator.Not)
            {
                logger?.LogDebug("Applying NOT operator to single expression");
                return Expression.Not(singleExpression);
            }
            logger?.LogDebug("Returning single filter expression without logical combination");
            return singleExpression;
        }

        logger?.LogDebug(
            "Combining {ExpressionCount} expressions with logical operator {LogicalOperator}",
            expressions.Count,
            group.LogicalOperator
        );

        Expression? combinedExpression = null;

        // For NOT operator, we need to apply De Morgan's law:
        // NOT(A AND B) = NOT(A) OR NOT(B)
        // NOT(A OR B) = NOT(A) AND NOT(B)
        // Since the filters in a group are combined with AND by default,
        // NOT group means NOT(A AND B AND C...) = NOT(A) OR NOT(B) OR NOT(C)...
        if (group.LogicalOperator == LogicalOperator.Not)
        {
            // Apply NOT to each expression individually and combine with OR
            foreach (Expression expr in expressions)
            {
                var notExpr = Expression.Not(expr);
                if (combinedExpression == null)
                {
                    combinedExpression = notExpr;
                }
                else
                {
                    // Use OR for NOT group (De Morgan's law)
                    combinedExpression = Expression.OrElse(combinedExpression, notExpr);
                }
            }
            logger?.LogDebug("Applied NOT operator using De Morgan's law (combined with OR)");
        }
        else
        {
            // Normal AND/OR combination
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

        logger?.LogDebug("Successfully built combined filter expression");
        return combinedExpression;
    }

    /// <summary>
    /// Builds a filter expression for a single FilterParameter.
    /// </summary>
    /// <param name="parameter">The parameter expression representing the entity</param>
    /// <param name="filter">The filter parameter to build an expression for</param>
    /// <param name="logger">Optional logger for debugging and tracing</param>
    /// <returns>An expression representing the filter condition, or null if the filter cannot be applied</returns>
    public static Expression? BuildSingleFilterExpression(
        ParameterExpression parameter,
        FilterParameter filter,
        ILogger? logger = null
    )
    {
        logger?.LogDebug(
            "Building single filter expression for field '{Field}' with operator {Operator}",
            filter.Field,
            filter.Operator
        );

        if (filter.Field.Contains('.'))
        {
            logger?.LogDebug("Field contains dot notation, building safe nested filter expression");
            return BuildSafeNestedFilterExpression(parameter, filter, logger);
        }
        else
        {
            PropertyInfo? property = QueryHelpers.GetPropertyByJsonName(
                parameter.Type,
                filter.Field
            );
            if (property == null)
            {
                logger?.LogWarning(
                    "Property '{Field}' not found on entity type '{EntityType}'. Available properties: {Properties}. Check your filter field names",
                    filter.Field,
                    parameter.Type.Name,
                    string.Join(", ", parameter.Type.GetProperties().Select(p => p.Name))
                );
                return null;
            }

            logger?.LogDebug(
                "Found property '{PropertyName}' of type {PropertyType} for field '{Field}'",
                property.Name,
                property.PropertyType.Name,
                filter.Field
            );

            Expression propertyAccess = Expression.Property(parameter, property);
            return BuildPropertyFilterExpression(propertyAccess, filter, logger);
        }
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
        logger?.LogDebug(
            "Building safe nested filter expression for field path '{Field}'",
            filter.Field
        );

        string[] parts = filter.Field.Split('.');
        Expression current = parameter;
        var nullChecks = new List<Expression>();

        logger?.LogDebug(
            "Navigating through {PartCount} property parts: {Parts}",
            parts.Length,
            string.Join(" -> ", parts)
        );

        // Build null-safe navigation for all but the last property
        for (int i = 0; i < parts.Length - 1; i++)
        {
            PropertyInfo? prop = QueryHelpers.GetPropertyByJsonName(current.Type, parts[i]);
            if (prop == null)
            {
                logger?.LogWarning(
                    "Property '{PropertyName}' not found on type {Type} during nested navigation",
                    parts[i],
                    current.Type.Name
                );
                return null;
            }

            logger?.LogDebug(
                "Navigating to property '{PropertyName}' of type {PropertyType}",
                prop.Name,
                prop.PropertyType.Name
            );

            current = Expression.Property(current, prop);

            // Add null check for reference types
            if (
                !prop.PropertyType.IsValueType
                || Nullable.GetUnderlyingType(prop.PropertyType) != null
            )
            {
                logger?.LogDebug(
                    "Adding null check for reference type property '{PropertyName}'",
                    prop.Name
                );
                nullChecks.Add(Expression.NotEqual(current, Expression.Constant(null)));
            }
        }

        // Get the final property
        PropertyInfo? finalProp = QueryHelpers.GetPropertyByJsonName(current.Type, parts[^1]);
        if (finalProp == null)
        {
            logger?.LogWarning(
                "Final property '{PropertyName}' not found on type {Type}",
                parts[^1],
                current.Type.Name
            );
            return null;
        }

        logger?.LogDebug(
            "Found final property '{PropertyName}' of type {PropertyType}",
            finalProp.Name,
            finalProp.PropertyType.Name
        );

        Expression finalProperty = Expression.Property(current, finalProp);

        // Build the actual filter expression
        Expression? filterExpression = BuildPropertyFilterExpression(finalProperty, filter, logger);
        if (filterExpression == null)
        {
            logger?.LogWarning("Failed to build property filter expression for final property");
            return null;
        }

        logger?.LogDebug(
            "Built filter expression, applying {NullCheckCount} null checks",
            nullChecks.Count
        );

        // For inequality operators (Ne, Nin), null values should be treated differently
        // null != value should be true, not filtered out
        Expression result;
        if (filter.Operator == FilterOperator.Ne || filter.Operator == FilterOperator.Nin)
        {
            // For inequality: if any property in the chain is null, return true (not equal)
            // Otherwise, apply the filter expression
            if (nullChecks.Count > 0)
            {
                // Create an OR condition: (any property is null) OR (all not null AND filter matches)
                Expression allNotNull = nullChecks[0];
                for (int i = 1; i < nullChecks.Count; i++)
                {
                    allNotNull = Expression.AndAlso(allNotNull, nullChecks[i]);
                }

                // Any property is null
                Expression anyNull = Expression.Not(allNotNull);

                // All not null AND filter matches
                Expression notNullAndFilter = Expression.AndAlso(allNotNull, filterExpression);

                // Return true if any null OR filter matches
                result = Expression.OrElse(anyNull, notNullAndFilter);
            }
            else
            {
                result = filterExpression;
            }
        }
        else
        {
            // For equality and other operators: all properties must be non-null AND filter matches
            result = filterExpression;
            foreach (Expression nullCheck in nullChecks)
            {
                result = Expression.AndAlso(nullCheck, result);
            }
        }

        logger?.LogDebug(
            "Successfully built safe nested filter expression with proper null handling for operator {Operator}",
            filter.Operator
        );
        return result;
    }

    private static Expression? BuildPropertyFilterExpression(
        Expression propertyAccess,
        FilterParameter filter,
        ILogger? logger = null
    )
    {
        Type targetType = propertyAccess.Type;

        logger?.LogDebug(
            "Building property filter expression for operator {Operator} on type {PropertyType} with value '{Value}'",
            filter.Operator,
            targetType.Name,
            filter.Value
        );

        if (filter.Operator == FilterOperator.IsNull)
        {
            logger?.LogDebug("Building IsNull expression");
            return Expression.Equal(propertyAccess, Expression.Constant(null));
        }
        if (filter.Operator == FilterOperator.IsNotNull)
        {
            logger?.LogDebug("Building IsNotNull expression");
            return Expression.NotEqual(propertyAccess, Expression.Constant(null));
        }

        if (filter.Operator == FilterOperator.In)
        {
            logger?.LogDebug("Building In expression for values: {Values}", filter.Value);
            Type? underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
            {
                logger?.LogDebug("Property is nullable, building null-safe In expression");
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
            else
            {
                logger?.LogDebug("Property is not nullable, building direct In expression");
                return BuildInExpression(propertyAccess, filter.Value, targetType);
            }
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
            else
            {
                return Expression.Not(BuildInExpression(propertyAccess, filter.Value, targetType));
            }
        }

        logger?.LogDebug(
            "Converting filter value '{Value}' to property type {PropertyType}",
            filter.Value,
            targetType.Name
        );

        object? filterValue = QueryHelpers.ConvertToPropertyType(filter.Value, targetType);
        if (
            filterValue == null
            && filter.Operator != FilterOperator.Eq
            && filter.Operator != FilterOperator.Ne
        )
        {
            logger?.LogWarning(
                "Failed to convert filter value '{Value}' to type {PropertyType} for operator {Operator}",
                filter.Value,
                targetType.Name,
                filter.Operator
            );
            return null;
        }

        logger?.LogDebug(
            "Successfully converted filter value, building {Operator} expression",
            filter.Operator
        );
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
