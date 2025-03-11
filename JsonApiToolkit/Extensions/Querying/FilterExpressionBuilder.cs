using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Models.Querying.Filtering;

namespace JsonApiToolkit.Extensions.Querying;

/// <summary>
/// Helper class for building filter expressions.
/// </summary>
public static class FilterExpressionBuilder
{
    /// <summary>
    /// Builds a filter expression for a group of filters.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="group">The filter group to build the expression for.</param>
    /// <param name="parameter">The parameter expression for the entity.</param>
    /// <returns>The filter expression.</returns>
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
            return expressions[0];

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
                    LogicalOperator.Not => Expression.Not(expr),
                    _ => Expression.AndAlso(combinedExpression, expr),
                };
            }
        }

        return combinedExpression;
    }

    private static Expression? BuildSingleFilterExpression(
        ParameterExpression parameter,
        FilterParameter filter
    )
    {
        Expression? propertyAccess;
        if (filter.Field.Contains('.'))
        {
            propertyAccess = GetNestedPropertyExpression(parameter, filter.Field);
        }
        else
        {
            PropertyInfo? property = QueryHelpers.GetPropertyByJsonName(
                parameter.Type,
                filter.Field
            );
            if (property == null)
                return null;
            propertyAccess = Expression.Property(parameter, property);
        }
        if (propertyAccess == null)
            return null;

        if (filter.Operator == FilterOperator.IsNull)
        {
            return Expression.Equal(propertyAccess, Expression.Constant(null));
        }
        if (filter.Operator == FilterOperator.IsNotNull)
        {
            return Expression.NotEqual(propertyAccess, Expression.Constant(null));
        }

        Type targetType = (propertyAccess as MemberExpression)!.Type;

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

    private static MemberExpression? GetNestedPropertyExpression(
        Expression parameter,
        string propertyPath
    )
    {
        string[] parts = propertyPath.Split('.');
        Expression current = parameter;
        foreach (string part in parts)
        {
            PropertyInfo? prop = current.Type.GetProperty(
                part,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance
            );
            if (prop == null)
                return null;
            current = Expression.Property(current, prop);
        }
        return current as MemberExpression;
    }
}
