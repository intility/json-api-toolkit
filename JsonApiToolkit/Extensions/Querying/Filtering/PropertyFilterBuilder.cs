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

        // A date-only value (no time component, e.g. "2026-09-14") used with
        // Le against a DateTime property means "up to and including that
        // whole day" to callers — but it parses to that day's midnight, so
        // an unadjusted <= would only match the exact midnight instant and
        // silently exclude the rest of the day. Bump it to the last tick of
        // the day so Le behaves as "on or before this date".
        if (
            filter.Operator == FilterOperator.Le
            && filterValue is DateTime dateOnlyBoundary
            && IsDateOnlyValue(filter.Value)
        )
        {
            filterValue = dateOnlyBoundary.Date.AddDays(1).AddTicks(-1);
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

    /// <summary>
    /// True if the raw filter value string carries no time-of-day component
    /// (e.g. "2026-09-14"), as opposed to a full timestamp (e.g.
    /// "2026-09-14T10:00:00"). ISO 8601 date-times always separate the time
    /// with 'T' and represent time-of-day with ':', so the absence of both
    /// is a reliable signal the caller only specified a calendar date.
    /// </summary>
    private static bool IsDateOnlyValue(string value) =>
        !value.Contains('T') && !value.Contains(':');
}
