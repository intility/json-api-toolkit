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
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("InvalidStatus", typeof(TestStatus))
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
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("", typeof(TestStatus))
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
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("not-a-number", typeof(int))
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

    [Fact]
    public void GetPropertyByJsonName_WithJsonPropertyNameAttribute_ReturnsProperty()
    {
        var property = QueryHelpers.GetPropertyByJsonName(
            typeof(TestEntityWithJsonPropertyName),
            "customId"
        );

        Assert.NotNull(property);
        Assert.Equal("ActualPropertyName", property.Name);
    }

    [Fact]
    public void GetPropertyByJsonName_WithJsonPropertyNameAttribute_SnakeCase_ReturnsProperty()
    {
        var property = QueryHelpers.GetPropertyByJsonName(
            typeof(TestEntityWithJsonPropertyName),
            "display_name"
        );

        Assert.NotNull(property);
        Assert.Equal("InternalName", property.Name);
    }

    [Fact]
    public void GetPropertyByJsonName_PrefersPascalCaseOverJsonPropertyName()
    {
        // When a property name matches via PascalCase, it should be preferred over JsonPropertyName
        // This ensures backward compatibility
        var property = QueryHelpers.GetPropertyByJsonName(
            typeof(TestEntityWithJsonPropertyName),
            "id"
        );

        Assert.NotNull(property);
        Assert.Equal("Id", property.Name);
    }

    #region Comprehensive Type Conversion Tests

    // ─────────────────────────────────────────────────────────────────────────
    // Long
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToPropertyType_WithLong_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("9223372036854775807", typeof(long));

        Assert.Equal(long.MaxValue, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithNegativeLong_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("-9223372036854775808", typeof(long));

        Assert.Equal(long.MinValue, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithInvalidLong_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("not-a-long", typeof(long))
        );

        Assert.Contains("Failed to convert filter value", exception.Message);
    }

    [Fact]
    public void ConvertToPropertyType_WithNullableLong_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("12345", typeof(long?));

        Assert.Equal(12345L, result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Decimal
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToPropertyType_WithDecimal_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("123.456", typeof(decimal));

        Assert.Equal(123.456m, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithNegativeDecimal_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("-99999.99", typeof(decimal));

        Assert.Equal(-99999.99m, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithInvalidDecimal_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("12.34.56", typeof(decimal))
        );

        Assert.Contains("Failed to convert filter value", exception.Message);
    }

    [Fact]
    public void ConvertToPropertyType_WithNullableDecimal_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("0.001", typeof(decimal?));

        Assert.Equal(0.001m, result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Double
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToPropertyType_WithDouble_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("3.14159265359", typeof(double));

        Assert.Equal(3.14159265359, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithScientificNotationDouble_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("1.5E10", typeof(double));

        Assert.Equal(1.5E10, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithInvalidDouble_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("not-a-double", typeof(double))
        );

        Assert.Contains("Failed to convert filter value", exception.Message);
    }

    [Fact]
    public void ConvertToPropertyType_WithNullableDouble_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("-273.15", typeof(double?));

        Assert.Equal(-273.15, result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Float
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToPropertyType_WithFloat_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("3.14", typeof(float));

        Assert.Equal(3.14f, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithInvalidFloat_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("abc", typeof(float))
        );

        Assert.Contains("Failed to convert filter value", exception.Message);
    }

    [Fact]
    public void ConvertToPropertyType_WithNullableFloat_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("1.5", typeof(float?));

        Assert.Equal(1.5f, result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Bool - additional cases
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToPropertyType_WithBoolUpperCase_ConvertsCorrectly()
    {
        var trueResult = QueryHelpers.ConvertToPropertyType("True", typeof(bool));
        var falseResult = QueryHelpers.ConvertToPropertyType("False", typeof(bool));

        Assert.Equal(true, trueResult);
        Assert.Equal(false, falseResult);
    }

    [Fact]
    public void ConvertToPropertyType_WithInvalidBool_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("yes", typeof(bool))
        );

        Assert.Contains("Failed to convert filter value", exception.Message);
    }

    [Fact]
    public void ConvertToPropertyType_WithNullableBool_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("true", typeof(bool?));

        Assert.Equal(true, result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DateTime - additional cases
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToPropertyType_WithDateTimeNoTimezone_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("2023-06-15T14:30:00", typeof(DateTime));

        Assert.IsType<DateTime>(result);
    }

    [Fact]
    public void ConvertToPropertyType_WithInvalidDateTime_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("not-a-date", typeof(DateTime))
        );

        Assert.Contains("Failed to convert filter value", exception.Message);
    }

    [Fact]
    public void ConvertToPropertyType_WithNullableDateTime_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("2023-12-25T00:00:00Z", typeof(DateTime?));

        Assert.IsType<DateTime>(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DateOnly
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToPropertyType_WithDateOnly_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("2023-06-15", typeof(DateOnly));

        Assert.Equal(new DateOnly(2023, 6, 15), result);
    }

    [Fact]
    public void ConvertToPropertyType_WithInvalidDateOnly_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("2023-13-45", typeof(DateOnly))
        );

        Assert.Contains("Failed to convert filter value", exception.Message);
    }

    [Fact]
    public void ConvertToPropertyType_WithNullableDateOnly_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("2024-01-01", typeof(DateOnly?));

        Assert.Equal(new DateOnly(2024, 1, 1), result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TimeOnly
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToPropertyType_WithTimeOnly_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("14:30:00", typeof(TimeOnly));

        Assert.Equal(new TimeOnly(14, 30, 0), result);
    }

    [Fact]
    public void ConvertToPropertyType_WithTimeOnlyShortFormat_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("09:15", typeof(TimeOnly));

        Assert.Equal(new TimeOnly(9, 15, 0), result);
    }

    [Fact]
    public void ConvertToPropertyType_WithInvalidTimeOnly_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("25:00:00", typeof(TimeOnly))
        );

        Assert.Contains("Failed to convert filter value", exception.Message);
    }

    [Fact]
    public void ConvertToPropertyType_WithNullableTimeOnly_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("23:59:59", typeof(TimeOnly?));

        Assert.Equal(new TimeOnly(23, 59, 59), result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Guid - additional cases
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToPropertyType_WithGuidNoDashes_ConvertsCorrectly()
    {
        // Guid.Parse accepts GUIDs without dashes
        var result = QueryHelpers.ConvertToPropertyType(
            "550e8400e29b41d4a716446655440000",
            typeof(Guid)
        );

        Assert.Equal(new Guid("550e8400-e29b-41d4-a716-446655440000"), result);
    }

    [Fact]
    public void ConvertToPropertyType_WithInvalidGuid_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("not-a-guid", typeof(Guid))
        );

        Assert.Contains("Failed to convert filter value", exception.Message);
    }

    [Fact]
    public void ConvertToPropertyType_WithNullableGuid_ConvertsCorrectly()
    {
        var guidString = "12345678-1234-1234-1234-123456789012";
        var result = QueryHelpers.ConvertToPropertyType(guidString, typeof(Guid?));

        Assert.Equal(new Guid(guidString), result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TimeSpan
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToPropertyType_WithTimeSpan_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("1.02:03:04", typeof(TimeSpan));

        Assert.Equal(new TimeSpan(1, 2, 3, 4), result);
    }

    [Fact]
    public void ConvertToPropertyType_WithTimeSpanHoursMinutesSeconds_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("12:30:45", typeof(TimeSpan));

        Assert.Equal(new TimeSpan(12, 30, 45), result);
    }

    [Fact]
    public void ConvertToPropertyType_WithInvalidTimeSpan_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("not-a-timespan", typeof(TimeSpan))
        );

        Assert.Contains("Failed to convert filter value", exception.Message);
    }

    [Fact]
    public void ConvertToPropertyType_WithNullableTimeSpan_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("00:30:00", typeof(TimeSpan?));

        Assert.Equal(TimeSpan.FromMinutes(30), result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Uri
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToPropertyType_WithAbsoluteUri_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType(
            "https://example.com/path?query=value",
            typeof(Uri)
        );

        Assert.IsType<Uri>(result);
        Assert.Equal("https://example.com/path?query=value", ((Uri)result!).ToString());
    }

    [Fact]
    public void ConvertToPropertyType_WithRelativeUri_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("/api/items/123", typeof(Uri));

        Assert.IsType<Uri>(result);
        Assert.Equal("/api/items/123", ((Uri)result!).ToString());
    }

    [Fact]
    public void ConvertToPropertyType_WithFileUri_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("file:///home/user/file.txt", typeof(Uri));

        Assert.IsType<Uri>(result);
        Assert.True(((Uri)result!).IsFile);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Byte Array (Base64)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToPropertyType_WithBase64ByteArray_ConvertsCorrectly()
    {
        // "Hello" in base64
        var result = QueryHelpers.ConvertToPropertyType("SGVsbG8=", typeof(byte[]));

        Assert.IsType<byte[]>(result);
        Assert.Equal("Hello", System.Text.Encoding.UTF8.GetString((byte[])result!));
    }

    [Fact]
    public void ConvertToPropertyType_WithEmptyBase64_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("", typeof(byte[]));

        Assert.IsType<byte[]>(result);
        Assert.Empty((byte[])result!);
    }

    [Fact]
    public void ConvertToPropertyType_WithInvalidBase64_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("not-valid-base64!!!", typeof(byte[]))
        );

        Assert.Contains("Failed to convert filter value", exception.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Int - additional edge cases
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToPropertyType_WithNegativeInt_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("-42", typeof(int));

        Assert.Equal(-42, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithIntMaxValue_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("2147483647", typeof(int));

        Assert.Equal(int.MaxValue, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithIntMinValue_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("-2147483648", typeof(int));

        Assert.Equal(int.MinValue, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithIntOverflow_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("9999999999999", typeof(int))
        );

        Assert.Contains("Failed to convert filter value", exception.Message);
    }

    [Fact]
    public void ConvertToPropertyType_WithNullableInt_ConvertsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("100", typeof(int?));

        Assert.Equal(100, result);
    }

    [Fact]
    public void ConvertToPropertyType_WithIntDecimalValue_ThrowsFormatException()
    {
        var exception = Assert.Throws<FormatException>(() =>
            QueryHelpers.ConvertToPropertyType("42.5", typeof(int))
        );

        Assert.Contains("Failed to convert filter value", exception.Message);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // String - edge cases
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ConvertToPropertyType_WithEmptyString_ReturnsEmptyString()
    {
        var result = QueryHelpers.ConvertToPropertyType("", typeof(string));

        Assert.Equal("", result);
    }

    [Fact]
    public void ConvertToPropertyType_WithWhitespaceString_ReturnsWhitespace()
    {
        var result = QueryHelpers.ConvertToPropertyType("   ", typeof(string));

        Assert.Equal("   ", result);
    }

    [Fact]
    public void ConvertToPropertyType_WithSpecialCharacters_ReturnsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("Hello\nWorld\t!", typeof(string));

        Assert.Equal("Hello\nWorld\t!", result);
    }

    [Fact]
    public void ConvertToPropertyType_WithUnicodeString_ReturnsCorrectly()
    {
        var result = QueryHelpers.ConvertToPropertyType("こんにちは世界 🌍", typeof(string));

        Assert.Equal("こんにちは世界 🌍", result);
    }

    #endregion
}
