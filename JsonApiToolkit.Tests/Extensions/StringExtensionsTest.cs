// JsonApiToolkit.Tests/Extensions/StringExtensionsTests.cs
using JsonApiToolkit.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("TestString", "testString")]
    [InlineData("testString", "testString")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void ToCamelCase_ConvertsProperly(string? input, string? expected)
    {
        // Act
        string result = input!.ToCamelCase();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("testString", "TestString")]
    [InlineData("TestString", "TestString")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void ToPascalCase_ConvertsProperly(string? input, string? expected)
    {
        // Act
        string result = StringExtensions.ToPascalCase(input!);

        // Assert
        Assert.Equal(expected, result);
    }
}
