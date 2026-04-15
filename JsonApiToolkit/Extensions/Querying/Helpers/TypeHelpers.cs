namespace JsonApiToolkit.Extensions.Querying;

internal static class TypeHelpers
{
    internal static bool IsCollectionType(Type type)
    {
        if (type.IsGenericType)
        {
            var genericTypeDef = type.GetGenericTypeDefinition();
            return genericTypeDef == typeof(ICollection<>)
                || genericTypeDef == typeof(IList<>)
                || genericTypeDef == typeof(List<>)
                || genericTypeDef == typeof(IEnumerable<>)
                || genericTypeDef == typeof(HashSet<>)
                || genericTypeDef == typeof(ISet<>);
        }

        return type.IsArray
            || type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
    }

    internal static Type? GetCollectionElementType(Type collectionType)
    {
        if (collectionType == typeof(string))
            return null;

        if (collectionType.IsArray)
            return collectionType.GetElementType();

        if (
            collectionType.IsGenericType
            && collectionType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
        )
            return collectionType.GetGenericArguments()[0];

        var enumerableInterface = collectionType
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            );

        return enumerableInterface?.GetGenericArguments().FirstOrDefault();
    }

    internal static Type GetNavigationTargetType(Type navigationType) =>
        GetCollectionElementType(navigationType) ?? navigationType;
}
