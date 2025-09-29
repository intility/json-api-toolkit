using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Tests.Models;

namespace JsonApiToolkit.Tests.Extensions;

public class QueryHelpersTests
{
    [Fact]
    public void ConvertToPropertyType_WithValidEnum_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("Published", typeof(TestStatus));

        Assert.Equal(TestStatus.Published, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithValidEnumIgnoreCase_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("PUBLISHED", typeof(TestStatus));

        Assert.Equal(TestStatus.Published, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithInvalidEnum_ThrowsArgumentException()
    {
        var exception = Assert.Throws<FormatException>(
            () => QueryHelpers.ConvertToPropertyType("InvalidStatus", typeof(TestStatus))
        );

        Assert.Contains(
            "Invalid enum value 'InvalidStatus' for type 'TestStatus'",
            exception.Message
        );
    }

    [Fact]
    public void ConvertToPropertyType_WithNullableEnum_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("Draft", typeof(TestStatus?));

        Assert.Equal(TestStatus.Draft, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithEmptyStringForEnum_ThrowsArgumentException()
    {
        var exception = Assert.Throws<FormatException>(
            () => QueryHelpers.ConvertToPropertyType("", typeof(TestStatus))
        );

        Assert.Contains("Invalid enum value '' for type 'TestStatus'", exception.Message);
    }

    [Fact]
    public void ConvertToPropertyType_WithString_ReturnsString()
    {
        var result = QueryHelpers.ConvertToPropertyType("test", typeof(string));

        Assert.Equal("test", result);
    }

    [Fact]
    public void ConvertToPropertyType_WithInt_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("42", typeof(int));

        Assert.Equal(42, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithInvalidInt_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(
            () => QueryHelpers.ConvertToPropertyType("not-a-number", typeof(int))
        );

        Assert.Contains(
            "Failed to convert filter value 'not-a-number' to type 'System.Int32'",
            exception.Message
        );
    }

    [Fact]
    public void ConvertToPropertyType_WithBool_ConvertsCorrectly()
    {
        var trueResult = QueryHelpers.ConvertToPropertyType("true", typeof(bool));
        var falseResult = QueryHelpers.ConvertToPropertyType("false", typeof(bool));

        Assert.Equal(true, trueResult);
        Assert.Equal(false, falseResult);
    }

    [Fact]
    public void ConvertToPropertyType_WithDateTime_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("2023-01-01T00:00:00Z", typeof(DateTime));

        Assert.Equal(new DateTime(2023, 1, 1, 0, 0, 0, DateTimeKind.Utc), result);
    }

    [Fact]
    public void ConvertToPropertyType_WithGuid_ConvertsCorrectly()
    {
        var guidString = "550e8400-e29b-41d4-a716-446655440000";
        var expectedGuid = new Guid(guidString);

        var result = QueryHelpers.ConvertToPropertyType(guidString, typeof(Guid));

        Assert.Equal(expectedGuid, result);
    }

    [Fact]
    public void GetPropertyByJsonName_WithExactMatch_ReturnsProperty()
    {
        var property = QueryHelpers.GetPropertyByJsonName(typeof(TestEntity), "Name");

        Assert.NotNull(property);
        Assert.Equal("Name", property.Name);
    }

    [Fact]
    public void GetPropertyByJsonName_WithCamelCase_ReturnsProperty()
    {
        var property = QueryHelpers.GetPropertyByJsonName(typeof(TestEntity), "isActive");

        Assert.NotNull(property);
        Assert.Equal("IsActive", property.Name);
    }

    [Fact]
    public void GetPropertyByJsonName_WithCaseInsensitive_ReturnsProperty()
    {
        var property = QueryHelpers.GetPropertyByJsonName(typeof(TestEntity), "DESCRIPTION");

        Assert.NotNull(property);
        Assert.Equal("Description", property.Name);
    }

    [Fact]
    public void GetPropertyByJsonName_WithNonExistentProperty_ReturnsNull()
    {
        var property = QueryHelpers.GetPropertyByJsonName(
            typeof(TestEntity),
            "NonExistentProperty"
        );

        Assert.Null(property);
    }
}
