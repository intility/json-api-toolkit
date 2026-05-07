using System.Linq.Expressions;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.Extensions.Logging;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Builds the operator-specific LINQ expression for a single property access
/// (Eq, Ne, Gt, Lt, Like, In, Nin, IsNull, IsNotNull). Delegates to
/// <see cref="CollectionFilterBuilder"/> when the property itself is a collection.
/// </summary>
internal static class PropertyFilterBuilder
{
    internal static Expression? BuildPropertyFilterExpression(
        Expression propertyAccess,
        FilterParameter filter,
        ILogger? logger = null
    )
    {
        Type targetType = propertyAccess.Type;

        // Check if the property itself is a collection (e.g., List<string> for CVEs/Tags)
        Type? collectionElementType = TypeHelpers.GetCollectionElementType(targetType);
        if (collectionElementType != null)
        {
            return CollectionFilterBuilder.BuildCollectionPropertyFilterExpression(
                propertyAccess,
                collectionElementType,
                filter,
                logger
            );
        }

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
            FilterOperator.Like => FilterOperatorExpressions.BuildLikeExpression(
                propertyAccess,
                filter.Value
            ),
            _ => Expression.Equal(propertyAccess, constant),
        };
    }
}
