using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Extensions.Querying;

internal static class NestedPropertyNavigator
{
    internal static Expression? BuildSafeNestedFilterExpression(
        ParameterExpression parameter,
        FilterParameter filter,
        ILogger? logger = null
    )
    {
        string[] parts = filter.Field.Split('.');
        Expression current = parameter;
        var nullChecks = new List<Expression>();

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

            if (
                !prop.PropertyType.IsValueType
                || Nullable.GetUnderlyingType(prop.PropertyType) != null
            )
            {
                nullChecks.Add(Expression.NotEqual(current, Expression.Constant(null)));
            }
        }

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

    internal static Expression? BuildPropertyFilterExpression(
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
                Expression containsExpr = FilterOperatorExpressions.BuildInExpression(
                    Expression.Property(propertyAccess, "Value"),
                    filter.Value,
                    underlying
                );
                return Expression.AndAlso(notNullExpr, containsExpr);
            }
            return FilterOperatorExpressions.BuildInExpression(
                propertyAccess,
                filter.Value,
                targetType
            );
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
                Expression containsExpr = FilterOperatorExpressions.BuildInExpression(
                    Expression.Property(propertyAccess, "Value"),
                    filter.Value,
                    underlying
                );
                return Expression.OrElse(isNullExpr, Expression.Not(containsExpr));
            }
            return Expression.Not(
                FilterOperatorExpressions.BuildInExpression(
                    propertyAccess,
                    filter.Value,
                    targetType
                )
            );
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
            FilterOperator.Like => FilterOperatorExpressions.BuildLikeExpression(
                propertyAccess,
                filter.Value
            ),
            _ => Expression.Equal(propertyAccess, constant),
        };
    }
}
