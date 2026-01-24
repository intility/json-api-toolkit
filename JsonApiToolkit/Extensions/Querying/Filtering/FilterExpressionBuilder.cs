using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Builds LINQ expressions for JSON:API filter parameters.
/// </summary>
public static class FilterExpressionBuilder
{
    /// <summary>
    /// Builds a composite filter expression from filter conditions and nested groups.
    /// </summary>
    public static Expression? BuildFilterExpression<T>(
        FilterGroup group,
        ParameterExpression parameter,
        ILogger? logger = null
    )
    {
        return BuildFilterExpression(group, parameter, typeof(T), logger);
    }

    /// <summary>
    /// Builds a composite filter expression from filter conditions and nested groups (non-generic overload).
    /// </summary>
    public static Expression? BuildFilterExpression(
        FilterGroup group,
        ParameterExpression parameter,
        Type entityType,
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
                    entityType,
                    filter.Field
                );
                if (property == null)
                {
                    logger?.LogWarning(
                        "Property '{Field}' not found on {Type}, skipping filter",
                        filter.Field,
                        entityType.Name
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
            Expression? nestedExpr = BuildFilterExpression(
                nestedGroup,
                parameter,
                entityType,
                logger
            );
            if (nestedExpr != null)
                expressions.Add(nestedExpr);
        }

        if (expressions.Count == 0)
            return null;

        if (expressions.Count == 1)
        {
            Expression singleExpression = expressions[0];
            if (group.LogicalOperator == LogicalOperator.Not)
                return Expression.Not(singleExpression);
            return singleExpression;
        }

        Expression? combinedExpression = null;

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
    public static Expression? BuildSingleFilterExpression(
        ParameterExpression parameter,
        FilterParameter filter,
        ILogger? logger = null
    )
    {
        if (filter.Field.Contains('.'))
            return NestedPropertyNavigator.BuildSafeNestedFilterExpression(
                parameter,
                filter,
                logger
            );

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
        return NestedPropertyNavigator.BuildPropertyFilterExpression(
            propertyAccess,
            filter,
            logger
        );
    }
}
