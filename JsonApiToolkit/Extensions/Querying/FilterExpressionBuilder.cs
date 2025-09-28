using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Models.Querying.Filtering;

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
    /// <returns>
    /// A composite Expression that can be used in a LINQ Where clause, or null if no valid filters exist
    /// </returns>
    /// <remarks>
    /// Handles both simple filters and complex nested filter groups with different logical operators.
    /// For nested properties, supports dot notation (e.g., "user.address.city").
    /// </remarks>
    public static Expression? BuildFilterExpression<T>(
        FilterGroup group,
        ParameterExpression parameter
    )
    {
        var expressions = new List<Expression>();

        foreach (FilterParameter filter in group.Filters)
        {
            Expression? expr;
            if (filter.Field.Contains('.'))
            {
                expr = BuildSingleFilterExpression(parameter, filter);
            }
            else
            {
                PropertyInfo? property = QueryHelpers.GetPropertyByJsonName(
                    typeof(T),
                    filter.Field
                );
                if (property == null)
                    continue;
                expr = BuildSingleFilterExpression(parameter, filter);
            }

            if (expr != null)
                expressions.Add(expr);
        }

        foreach (FilterGroup nestedGroup in group.Groups)
        {
            Expression? nestedExpr = BuildFilterExpression<T>(nestedGroup, parameter);
            if (nestedExpr != null)
                expressions.Add(nestedExpr);
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
                    LogicalOperator.Not => Expression.AndAlso(combinedExpression, expr),
                    _ => Expression.AndAlso(combinedExpression, expr),
                };
            }
        }

        if (group.LogicalOperator == LogicalOperator.Not && combinedExpression != null)
        {
            combinedExpression = Expression.Not(combinedExpression);
        }

        return combinedExpression;
    }

    /// <summary>
    /// Builds a filter expression for a single FilterParameter.
    /// </summary>
    /// <param name="parameter">The parameter expression representing the entity</param>
    /// <param name="filter">The filter parameter to build an expression for</param>
    /// <returns>An expression representing the filter condition, or null if the filter cannot be applied</returns>
    public static Expression? BuildSingleFilterExpression(
        ParameterExpression parameter,
        FilterParameter filter
    )
    {
        if (filter.Field.Contains('.'))
        {
            return BuildSafeNestedFilterExpression(parameter, filter);
        }
        else
        {
            PropertyInfo? property = QueryHelpers.GetPropertyByJsonName(
                parameter.Type,
                filter.Field
            );
            if (property == null)
                return null;
            Expression propertyAccess = Expression.Property(parameter, property);
            return BuildPropertyFilterExpression(propertyAccess, filter);
        }
    }

    private static Expression BuildLikeExpression(Expression property, string value)
    {
        if (property.Type == typeof(string))
        {
            MethodInfo? method = typeof(string).GetMethod("Contains", [typeof(string)]);
            return Expression.Call(property, method!, Expression.Constant(value));
        }

        MethodInfo? toStringMethod = property.Type.GetMethod("ToString", Type.EmptyTypes);
        MethodCallExpression toStringCall = Expression.Call(property, toStringMethod!);
        MethodInfo? containsMethod = typeof(string).GetMethod("Contains", [typeof(string)]);
        return Expression.Call(toStringCall, containsMethod!, Expression.Constant(value));
    }

    private static Expression BuildInExpression(
        Expression property,
        string value,
        Type propertyType
    )
    {
        var rawConvertedValues = value
            .Split(',')
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrEmpty(v))
            .Select(v => QueryHelpers.ConvertToPropertyType(v, propertyType))
            .Where(v => v != null)
            .Select(v => v!)
            .ToList();

        if (rawConvertedValues.Count == 0)
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

        foreach (object? item in rawConvertedValues)
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
        FilterParameter filter
    )
    {
        string[] parts = filter.Field.Split('.');
        Expression current = parameter;
        var nullChecks = new List<Expression>();

        // Build null-safe navigation for all but the last property
        for (int i = 0; i < parts.Length - 1; i++)
        {
            PropertyInfo? prop = QueryHelpers.GetPropertyByJsonName(current.Type, parts[i]);
            if (prop == null)
                return null;

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
            return null;

        Expression finalProperty = Expression.Property(current, finalProp);

        // Build the actual filter expression
        Expression? filterExpression = BuildPropertyFilterExpression(finalProperty, filter);
        if (filterExpression == null)
            return null;

        // Combine null checks with the filter expression
        Expression result = filterExpression;
        foreach (Expression nullCheck in nullChecks)
        {
            result = Expression.AndAlso(nullCheck, result);
        }

        return result;
    }

    private static Expression? BuildPropertyFilterExpression(
        Expression propertyAccess,
        FilterParameter filter
    )
    {
        Type targetType = propertyAccess.Type;

        if (filter.Operator == FilterOperator.IsNull)
        {
            return Expression.Equal(propertyAccess, Expression.Constant(null));
        }
        if (filter.Operator == FilterOperator.IsNotNull)
        {
            return Expression.NotEqual(propertyAccess, Expression.Constant(null));
        }

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
            else
            {
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

        object? filterValue = QueryHelpers.ConvertToPropertyType(filter.Value, targetType);
        if (
            filterValue == null
            && filter.Operator != FilterOperator.Eq
            && filter.Operator != FilterOperator.Ne
        )
        {
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
