using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using JsonApiToolkit.Models;
using JsonApiToolkit.Models.FilterParameters;
using Microsoft.EntityFrameworkCore;

namespace JsonApiToolkit.Extensions;

/// <summary>
/// Extensions for IQueryable to apply JSON:API query parameters.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Applies JSON:API query parameters to an IQueryable.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="query">The IQueryable to apply the query parameters to.</param>
    /// <param name="parameters">The JSON:API query parameters.</param>
    /// <returns>The IQueryable with the query parameters applied.</returns>
    public static IQueryable<T> ApplyJsonApiParameters<T>(
        this IQueryable<T> query,
        QueryParameters parameters
    )
    {
        if (parameters == null)
            return query;

        if (parameters.Filter != null)
            query = query.ApplyFilters(parameters.Filter);

        if (parameters.Sort?.Count > 0)
            query = query.ApplySorting(parameters.Sort);
        else
            query = query.ApplySorting([new SortParameter { Field = "Id", IsDescending = false }]);

        if (parameters.Pagination != null)
            query = query.ApplyPagination(parameters.Pagination);

        return query;
    }

    /// <summary>
    /// Applies filters to an IQueryable.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="query">The IQueryable to apply the filters to.</param>
    /// <param name="filterGroup">The filter group to apply.</param>
    /// <returns>The IQueryable with the filters applied.</returns>
    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, FilterGroup filterGroup)
    {
        if (
            filterGroup == null
            || (filterGroup.Filters.Count == 0 && filterGroup.Groups.Count == 0)
        )
        {
            return query;
        }

        ParameterExpression parameter = Expression.Parameter(typeof(T), "x");
        Expression? expression = BuildFilterExpression<T>(filterGroup, parameter);

        if (expression != null)
        {
            var lambda = Expression.Lambda<Func<T, bool>>(expression, parameter);
            query = query.Where(lambda);
        }

        return query;
    }

    /// <summary>
    /// Applies sorting to an IQueryable.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="query">The IQueryable to apply the sorting to.</param>
    /// <param name="sortParameters">The sort parameters to apply.</param>
    /// <returns>The IQueryable with the sorting applied.</returns>
    public static IQueryable<T> ApplySorting<T>(
        this IQueryable<T> query,
        List<SortParameter> sortParameters
    )
    {
        Type entityType = typeof(T);
        bool isFirstSort = true;

        foreach (SortParameter sortParam in sortParameters)
        {
            PropertyInfo? property = GetPropertyByJsonName(entityType, sortParam.Field);
            if (property == null)
                continue;

            ParameterExpression parameter = Expression.Parameter(entityType, "x");
            MemberExpression propertyAccess = Expression.Property(parameter, property);
            LambdaExpression lambda = Expression.Lambda(propertyAccess, parameter);

            string methodName;
            if (isFirstSort)
            {
                methodName = sortParam.IsDescending ? "OrderByDescending" : "OrderBy";
                isFirstSort = false;
            }
            else
            {
                methodName = sortParam.IsDescending ? "ThenByDescending" : "ThenBy";
            }

            MethodInfo orderByMethod = typeof(Queryable)
                .GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(entityType, property.PropertyType);

            query = (IQueryable<T>)orderByMethod.Invoke(null, [query, lambda])!;
        }

        return query;
    }

    /// <summary>
    /// Applies pagination to an IQueryable.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="query">The IQueryable to apply the pagination to.</param>
    /// <param name="pagination">The pagination parameters to apply.</param>
    /// <returns>The IQueryable with the pagination applied.</returns>
    public static IQueryable<T> ApplyPagination<T>(
        this IQueryable<T> query,
        PaginationParameters pagination
    )
    {
        int skip = (pagination.Number - 1) * pagination.Size;
        return query.Skip(skip).Take(pagination.Size);
    }

    /// <summary>
    /// Creates pagination metadata for an IQueryable.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="query">The IQueryable to create the pagination metadata for.</param>
    /// <param name="pagination">The pagination parameters.</param>
    /// <returns>The pagination metadata.</returns>
    public static async Task<PaginationMeta> CreatePaginationMetaAsync<T>(
        this IQueryable<T> query,
        PaginationParameters pagination
    )
    {
        int totalCount = await query.CountAsync();
        int totalPages = (int)Math.Ceiling(totalCount / (double)pagination.Size);

        return new PaginationMeta
        {
            TotalResources = totalCount,
            TotalPages = totalPages,
            CurrentPage = pagination.Number,
            PageSize = pagination.Size,
        };
    }

    private static PropertyInfo? GetPropertyByJsonName(Type entityType, string jsonPropertyName)
    {
        PropertyInfo? property = entityType.GetProperty(jsonPropertyName);

        if (property != null)
            return property;

        string pascalCase = StringExtensions.ToPascalCase(jsonPropertyName);
        property = entityType.GetProperty(pascalCase);

        return property
            ?? entityType
                .GetProperties()
                .FirstOrDefault(p =>
                    string.Equals(p.Name, jsonPropertyName, StringComparison.OrdinalIgnoreCase)
                );
    }

    private static object? ConvertToPropertyType(string value, Type targetType)
    {
        try
        {
            if (targetType == typeof(string))
                return value;

            if (targetType == typeof(int) || targetType == typeof(int?))
                return int.Parse(value);

            if (targetType == typeof(long) || targetType == typeof(long?))
                return long.Parse(value);

            if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                return decimal.Parse(value);

            if (targetType == typeof(double) || targetType == typeof(double?))
                return double.Parse(value);

            if (targetType == typeof(bool) || targetType == typeof(bool?))
                return bool.Parse(value);

            if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
            {
                return DateTime.Parse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal
                );
            }

            if (targetType == typeof(Guid) || targetType == typeof(Guid?))
                return Guid.Parse(value);

            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return null;
        }
    }

    private static Expression? BuildFilterExpression<T>(
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
                PropertyInfo? property = GetPropertyByJsonName(typeof(T), filter.Field);
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
            PropertyInfo? property = GetPropertyByJsonName(parameter.Type, filter.Field);
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

        object? filterValue = ConvertToPropertyType(filter.Value, targetType);
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
            .Select(v => ConvertToPropertyType(v, propertyType))
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
