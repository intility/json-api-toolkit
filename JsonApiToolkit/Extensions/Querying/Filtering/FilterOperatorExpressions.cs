using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace JsonApiToolkit.Extensions.Querying;

internal static class FilterOperatorExpressions
{
    internal static Expression BuildLikeExpression(Expression property, string value)
    {
        if (property.Type == typeof(string))
        {
            MethodInfo? method = typeof(string).GetMethod("Contains", [typeof(string)]);
            return Expression.Call(property, method!, Expression.Constant(value));
        }

        Type? underlyingType = Nullable.GetUnderlyingType(property.Type);
        if (underlyingType != null || !property.Type.IsValueType)
        {
            Expression notNullCheck = Expression.NotEqual(
                property,
                Expression.Constant(null, property.Type)
            );

            MethodInfo? toStringMethod = property.Type.GetMethod("ToString", Type.EmptyTypes);
            if (toStringMethod == null)
            {
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

            return Expression.AndAlso(notNullCheck, containsCall);
        }
        else
        {
            MethodInfo? toStringMethod = property.Type.GetMethod("ToString", Type.EmptyTypes);
            MethodCallExpression toStringCall = Expression.Call(property, toStringMethod!);
            MethodInfo? containsMethod = typeof(string).GetMethod("Contains", [typeof(string)]);
            return Expression.Call(toStringCall, containsMethod!, Expression.Constant(value));
        }
    }

    internal static Expression BuildInExpression(
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
                    convertedValues.Add(converted);
            }
            catch (Exception)
            {
                failedValues.Add(rawValue);
            }
        }

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
            property = Expression.Convert(property, listElementType);

        return Expression.Call(listConstant, containsMethod, property);
    }
}
