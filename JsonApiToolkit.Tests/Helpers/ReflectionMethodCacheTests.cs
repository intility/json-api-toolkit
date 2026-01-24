using JsonApiToolkit.Helpers;

namespace JsonApiToolkit.Tests.Helpers;

public class ReflectionMethodCacheTests
{
    [Fact]
    public void GetEnumerableAnyWithPredicate_ReturnsCorrectMethod()
    {
        // Act
        var method = ReflectionMethodCache.GetEnumerableAnyWithPredicate(typeof(int));

        // Assert
        Assert.NotNull(method);
        Assert.Equal("Any", method.Name);
        Assert.Equal(2, method.GetParameters().Length);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void GetEnumerableAnyWithPredicate_IsCached()
    {
        // Act
        var method1 = ReflectionMethodCache.GetEnumerableAnyWithPredicate(typeof(int));
        var method2 = ReflectionMethodCache.GetEnumerableAnyWithPredicate(typeof(int));

        // Assert - both should be the same instance (cached base method, different generic instantiation)
        Assert.Equal(method1, method2);
    }

    [Fact]
    public void GetEnumerableContains_ReturnsCorrectMethod()
    {
        // Act
        var method = ReflectionMethodCache.GetEnumerableContains(typeof(string));

        // Assert
        Assert.NotNull(method);
        Assert.Equal("Contains", method.Name);
        Assert.Equal(2, method.GetParameters().Length);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void GetEnumerableWhere_ReturnsCorrectMethod()
    {
        // Act
        var method = ReflectionMethodCache.GetEnumerableWhere(typeof(int));

        // Assert
        Assert.NotNull(method);
        Assert.Equal("Where", method.Name);
        Assert.Equal(2, method.GetParameters().Length);
        Assert.True(method.IsGenericMethod);
    }

    [Theory]
    [InlineData("OrderBy")]
    [InlineData("OrderByDescending")]
    [InlineData("ThenBy")]
    [InlineData("ThenByDescending")]
    public void GetQueryableOrderingMethod_ReturnsCorrectMethod(string methodName)
    {
        // Act
        var method = ReflectionMethodCache.GetQueryableOrderingMethod(
            methodName,
            typeof(TestEntity),
            typeof(string)
        );

        // Assert
        Assert.NotNull(method);
        Assert.Equal(methodName, method.Name);
        Assert.Equal(2, method.GetParameters().Length);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void GetQueryableOrderingMethod_WithInvalidMethodName_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ReflectionMethodCache.GetQueryableOrderingMethod(
                "NonExistentMethod",
                typeof(TestEntity),
                typeof(string)
            )
        );

        Assert.Contains("Could not find Queryable.NonExistentMethod", ex.Message);
        Assert.Contains("report this issue", ex.Message);
    }

    [Fact]
    public void GetEfCoreIncludeMethod_ReturnsCorrectMethod()
    {
        // Act
        var method = ReflectionMethodCache.GetEfCoreIncludeMethod(
            typeof(TestEntity),
            typeof(string)
        );

        // Assert
        Assert.NotNull(method);
        Assert.Equal("Include", method.Name);
        Assert.Equal(2, method.GetParameters().Length);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void GetEfCoreThenIncludeMethod_ForCollectionNavigation_ReturnsCorrectMethod()
    {
        // Act
        var method = ReflectionMethodCache.GetEfCoreThenIncludeMethod(
            isPreviousCollection: true,
            entityType: typeof(TestEntity),
            previousPropertyType: typeof(TestRelated),
            newPropertyType: typeof(string)
        );

        // Assert
        Assert.NotNull(method);
        Assert.Equal("ThenInclude", method.Name);
        Assert.True(method.IsGenericMethod);
    }

    [Fact]
    public void GetEfCoreThenIncludeMethod_ForReferenceNavigation_ReturnsCorrectMethod()
    {
        // Act
        var method = ReflectionMethodCache.GetEfCoreThenIncludeMethod(
            isPreviousCollection: false,
            entityType: typeof(TestEntity),
            previousPropertyType: typeof(TestRelated),
            newPropertyType: typeof(string)
        );

        // Assert
        Assert.NotNull(method);
        Assert.Equal("ThenInclude", method.Name);
        Assert.True(method.IsGenericMethod);
    }

    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private class TestRelated
    {
        public int Id { get; set; }
    }
}
