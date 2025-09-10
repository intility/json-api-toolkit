using JsonApiToolkit.Models.Validation;

namespace JsonApiToolkit.Tests.Validation;

public class IncludePatternTests
{
    [Theory]
    [InlineData("author", "author", true)]
    [InlineData("author", "Author", true)]
    [InlineData("author", "AUTHOR", true)]
    [InlineData("author", "posts", false)]
    public void Matches_ExactPattern_CaseInsensitive(string pattern, string include, bool expected)
    {
        var includePattern = new IncludePattern(pattern);
        Assert.Equal(expected, includePattern.Matches(include));
    }

    [Theory]
    [InlineData("author.posts", "author", true)]
    [InlineData("author.posts", "author.posts", true)]
    [InlineData("author.posts", "author.posts.comments", false)]
    [InlineData("author.posts.comments", "author", true)]
    [InlineData("author.posts.comments", "author.posts", true)]
    public void Matches_PartialPath(string pattern, string include, bool expected)
    {
        var includePattern = new IncludePattern(pattern);
        Assert.Equal(expected, includePattern.Matches(include));
    }

    [Theory]
    [InlineData("*", "author", true)]
    [InlineData("*", "posts", true)]
    [InlineData("*", "author.posts", false)]
    [InlineData("*", "author.posts.comments", false)]
    public void Matches_TopLevelWildcard(string pattern, string include, bool expected)
    {
        var includePattern = new IncludePattern(pattern);
        Assert.Equal(expected, includePattern.Matches(include));
        Assert.Equal(PatternType.TopLevelWildcard, includePattern.Type);
    }

    [Theory]
    [InlineData("author.*", "author", true)]
    [InlineData("author.*", "author.posts", true)]
    [InlineData("author.*", "author.comments", true)]
    [InlineData("author.*", "author.posts.comments", false)]
    [InlineData("author.*", "posts", false)]
    [InlineData("author.*", "posts.author", false)]
    public void Matches_SingleLevelWildcard(string pattern, string include, bool expected)
    {
        var includePattern = new IncludePattern(pattern);
        Assert.Equal(expected, includePattern.Matches(include));
        Assert.Equal(PatternType.SingleLevelWildcard, includePattern.Type);
    }

    [Theory]
    [InlineData("author.*", "Author", true)]
    [InlineData("author.*", "AUTHOR.POSTS", true)]
    [InlineData("Author.*", "author.posts", true)]
    [InlineData("AUTHOR.*", "author.POSTS", true)]
    public void Matches_WildcardCaseInsensitive(string pattern, string include, bool expected)
    {
        var includePattern = new IncludePattern(pattern);
        Assert.Equal(expected, includePattern.Matches(include));
    }

    [Fact]
    public void Constructor_NullPattern_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() => new IncludePattern(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Matches_EmptyInclude_ReturnsFalse(string? include)
    {
        var pattern = new IncludePattern("author");
        Assert.False(pattern.Matches(include!));
    }

    [Theory]
    [InlineData("posts.*", "posts", true)]
    [InlineData("posts.*", "posts.author", true)]
    [InlineData("posts.*", "posts.author.name", false)]
    [InlineData("cve.*", "cve", true)]
    [InlineData("cve.*", "cve.epss", true)]
    [InlineData("cve.*", "CVE.EPSS", true)]
    public void Matches_RealWorldExamples(string pattern, string include, bool expected)
    {
        var includePattern = new IncludePattern(pattern);
        Assert.Equal(expected, includePattern.Matches(include));
    }
}
