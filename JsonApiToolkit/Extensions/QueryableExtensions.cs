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

        // Apply filters
        if (parameters.Filter != null)
        {
            query = query.ApplyFilters(parameters.Filter);
        }

        // Apply sorting
        if (parameters.Sort?.Count > 0)
        {
            query = query.ApplySorting(parameters.Sort);
        }
        else
        {
            query = query.ApplySorting([new SortParameter { Field = "Id", IsDescending = false }]);
        }

        // Apply pagination (but don't execute the query yet)
        if (parameters.Pagination != null)
        {
            query = query.ApplyPagination(parameters.Pagination);
        }

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

        var parameter = Expression.Parameter(typeof(T), "x");
        var expression = BuildFilterExpression<T>(filterGroup, parameter);

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
        var entityType = typeof(T);
        bool isFirstSort = true;

        foreach (var sortParam in sortParameters)
        {
            var property = GetPropertyByJsonName(entityType, sortParam.Field);
            if (property == null)
                continue;

            var parameter = Expression.Parameter(entityType, "x");
            var propertyAccess = Expression.Property(parameter, property);
            var lambda = Expression.Lambda(propertyAccess, parameter);

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

            var orderByMethod = typeof(Queryable)
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
        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pagination.Size);

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
        // First try exact name match
        var property = entityType.GetProperty(jsonPropertyName);

        if (property != null)
            return property;

        // Then try pascal case version
        var pascalCase = ToPascalCase(jsonPropertyName);
        property = entityType.GetProperty(pascalCase);

        if (property != null)
            return property;

        // Finally try case-insensitive match
        return entityType
            .GetProperties()
            .FirstOrDefault(p =>
                string.Equals(p.Name, jsonPropertyName, StringComparison.OrdinalIgnoreCase)
            );
    }

    private static string ToPascalCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return char.ToUpperInvariant(str[0]) + str.Substring(1);
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

        // Add individual filter conditions
        foreach (var filter in group.Filters)
        {
            Expression? expr;
            if (filter.Field.Contains('.'))
            {
                expr = BuildSingleFilterExpression(parameter, filter);
            }
            else
            {
                var property = GetPropertyByJsonName(typeof(T), filter.Field);
                if (property == null)
                    continue;
                expr = BuildSingleFilterExpression(parameter, filter);
            }

            if (expr != null)
                expressions.Add(expr);
        }

        // Add nested groups
        foreach (var nestedGroup in group.Groups)
        {
            var nestedExpr = BuildFilterExpression<T>(nestedGroup, parameter);
            if (nestedExpr != null)
                expressions.Add(nestedExpr);
        }

        if (expressions.Count == 0)
            return null;

        if (expressions.Count == 1)
            return expressions[0];

        // Combine expressions based on logical operator
        Expression? combinedExpression = null;

        foreach (var expr in expressions)
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
            var property = GetPropertyByJsonName(parameter.Type, filter.Field);
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

        var targetType = (propertyAccess as MemberExpression)!.Type;

        // --- Handle In/NotIn for nullable types ---
        if (filter.Operator == FilterOperator.In)
        {
            var underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
            {
                // Build: x.Property != null && list.Contains(x.Property.Value)
                var notNullExpr = Expression.NotEqual(
                    propertyAccess,
                    Expression.Constant(null, propertyAccess.Type)
                );
                var containsExpr = BuildInExpression(
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
            var underlying = Nullable.GetUnderlyingType(targetType);
            if (underlying != null)
            {
                var isNullExpr = Expression.Equal(
                    propertyAccess,
                    Expression.Constant(null, propertyAccess.Type)
                );
                var containsExpr = BuildInExpression(
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

        // For other operators, convert the filter value and build comparisons.
        var filterValue = ConvertToPropertyType(filter.Value, targetType);
        if (
            filterValue == null
            && filter.Operator != FilterOperator.Eq
            && filter.Operator != FilterOperator.Ne
        )
        {
            return null;
        }
        var constant = Expression.Constant(filterValue, targetType);

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
        // Use Contains method for string properties
        if (property.Type == typeof(string))
        {
            var method = typeof(string).GetMethod("Contains", [typeof(string)]);
            return Expression.Call(property, method!, Expression.Constant(value));
        }

        // For non-string properties, use ToString().Contains()
        var toStringMethod = property.Type.GetMethod("ToString", Type.EmptyTypes);
        var toStringCall = Expression.Call(property, toStringMethod!);
        var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)]);
        return Expression.Call(toStringCall, containsMethod!, Expression.Constant(value));
    }

    private static Expression BuildInExpression(
        Expression property,
        string value,
        Type propertyType
    )
    {
        // Split the comma-separated values and convert them.
        // Note that ConvertToPropertyType returns object? so the resulting list is List<object>.
        var rawConvertedValues = value
            .Split(',')
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrEmpty(v))
            .Select(v => ConvertToPropertyType(v, propertyType))
            .Where(v => v != null)
            .Select(v => v!) // Now rawConvertedValues is a List<object> but its runtime values are, say, Guid.
            .ToList();

        // If there are no valid values, return a constant expression 'false'
        if (rawConvertedValues.Count == 0)
            return Expression.Constant(false);

        // Determine the underlying type if propertyType is nullable (e.g. Guid?)
        Type listElementType = propertyType;
        if (
            propertyType.IsGenericType
            && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
        )
        {
            listElementType = Nullable.GetUnderlyingType(propertyType)!;
        }

        // Create the proper generic list type.
        var listType = typeof(List<>).MakeGenericType(listElementType);

        // Instead of using rawConvertedValues directly (which is List<object>),
        // create an instance of List<T> and add the converted values.
        var typedList = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in rawConvertedValues)
        {
            // Each 'item' is already a Guid (or another target type) at runtime.
            typedList.Add(item);
        }

        // Create a constant expression for the typed list.
        var listConstant = Expression.Constant(typedList, listType);

        // Get the Contains method that expects a parameter of listElementType.
        var containsMethod =
            listType.GetMethod("Contains", [listElementType])
            ?? throw new InvalidOperationException("Cannot find 'Contains' method on list type.");

        // If the property is not exactly the element type, convert it.
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
        var parts = propertyPath.Split('.');
        Expression current = parameter;
        foreach (var part in parts)
        {
            var prop = current.Type.GetProperty(
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
