using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Models.Querying.Filtering;
using Microsoft.EntityFrameworkCore;

namespace JsonApiToolkit.Tests.Extensions.Filtering;

/// <summary>
/// Applies composed filter expressions through the EF Core InMemory provider to catch
/// IQueryable translation issues that plain LINQ-to-Objects tests mask.
/// </summary>
public class FilterExpressionComposerEfCoreTests
{
    private static readonly FilterExpressionComposer Composer = new();

    private class Author
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public Address? Address { get; set; }
        public List<Post> Posts { get; set; } = [];
    }

    private class Address
    {
        public int Id { get; set; }
        public string City { get; set; } = "";
    }

    private class Post
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public List<Comment> Comments { get; set; } = [];
    }

    private class Comment
    {
        public int Id { get; set; }
        public string Text { get; set; } = "";
    }

    private class BlogContext(DbContextOptions<BlogContext> options) : DbContext(options)
    {
        public DbSet<Author> Authors => Set<Author>();
    }

    private static BlogContext CreateSeededContext()
    {
        var options = new DbContextOptionsBuilder<BlogContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new BlogContext(options);
        context.Authors.AddRange(
            new Author
            {
                Id = 1,
                Name = "Ada",
                Age = 30,
                Address = new Address { Id = 1, City = "Oslo" },
                Posts =
                [
                    new Post
                    {
                        Id = 1,
                        Title = "Hello World",
                        Comments = [new Comment { Id = 1, Text = "Nice" }],
                    },
                    new Post { Id = 2, Title = "Second Post" },
                ],
            },
            new Author
            {
                Id = 2,
                Name = "Bob",
                Age = 40,
                Address = new Address { Id = 2, City = "Bergen" },
                Posts = [new Post { Id = 3, Title = "Draft" }],
            },
            new Author
            {
                Id = 3,
                Name = "Cleo",
                Age = 50,
                Address = null,
                Posts = [],
            }
        );
        context.SaveChanges();
        return context;
    }

    private static List<Author> Run(FilterGroup group)
    {
        using var context = CreateSeededContext();
        var lambda = Composer.Compose<Author>(group);
        Assert.NotNull(lambda);
        return context.Authors.Where(lambda).OrderBy(a => a.Id).ToList();
    }

    private static FilterGroup Single(string field, FilterOperator op, string value)
    {
        return new FilterGroup
        {
            Filters =
            [
                new FilterParameter
                {
                    Field = field,
                    Operator = op,
                    Value = value,
                },
            ],
        };
    }

    [Theory]
    [InlineData(FilterOperator.Eq, "40", new[] { 2 })]
    [InlineData(FilterOperator.Ne, "40", new[] { 1, 3 })]
    [InlineData(FilterOperator.Gt, "30", new[] { 2, 3 })]
    [InlineData(FilterOperator.Ge, "40", new[] { 2, 3 })]
    [InlineData(FilterOperator.Lt, "50", new[] { 1, 2 })]
    [InlineData(FilterOperator.Le, "40", new[] { 1, 2 })]
    [InlineData(FilterOperator.In, "30,50", new[] { 1, 3 })]
    [InlineData(FilterOperator.Nin, "30,50", new[] { 2 })]
    public void ScalarOperators_TranslateAndFilter(FilterOperator op, string value, int[] expected)
    {
        var result = Run(Single("age", op, value));

        Assert.Equal(expected, result.Select(a => a.Id));
    }

    [Fact]
    public void LikeOperator_TranslatesToContains()
    {
        var result = Run(Single("name", FilterOperator.Like, "%o%"));

        Assert.Equal([2, 3], result.Select(a => a.Id));
    }

    [Fact]
    public void DotPath_NavigatesReferenceAndGuardsNull()
    {
        // Cleo has a null Address; the composed null guard must not throw
        var result = Run(Single("address.city", FilterOperator.Eq, "Oslo"));

        Assert.Equal([1], result.Select(a => a.Id));
    }

    [Fact]
    public void DotPath_NeTreatsNullChainAsNotEqual()
    {
        var result = Run(Single("address.city", FilterOperator.Ne, "Oslo"));

        Assert.Equal([2, 3], result.Select(a => a.Id));
    }

    [Fact]
    public void CollectionNavigation_TranslatesToAny()
    {
        var result = Run(Single("posts.title", FilterOperator.Eq, "Draft"));

        Assert.Equal([2], result.Select(a => a.Id));
    }

    [Fact]
    public void CollectionOfCollection_TranslatesToChainedAny()
    {
        var result = Run(Single("posts.comments.text", FilterOperator.Eq, "Nice"));

        Assert.Equal([1], result.Select(a => a.Id));
    }

    [Fact]
    public void OrGroup_CombinesConditions()
    {
        var group = new FilterGroup
        {
            LogicalOperator = LogicalOperator.Or,
            Filters =
            [
                new FilterParameter
                {
                    Field = "name",
                    Operator = FilterOperator.Eq,
                    Value = "Ada",
                },
                new FilterParameter
                {
                    Field = "age",
                    Operator = FilterOperator.Eq,
                    Value = "50",
                },
            ],
        };

        var result = Run(group);

        Assert.Equal([1, 3], result.Select(a => a.Id));
    }

    [Fact]
    public void NotGroup_NegatesCondition()
    {
        var group = new FilterGroup
        {
            LogicalOperator = LogicalOperator.Not,
            Filters =
            [
                new FilterParameter
                {
                    Field = "name",
                    Operator = FilterOperator.Eq,
                    Value = "Ada",
                },
            ],
        };

        var result = Run(group);

        Assert.Equal([2, 3], result.Select(a => a.Id));
    }

    [Fact]
    public void NestedGroups_ComposeWithParent()
    {
        // age > 25 AND (name == Ada OR name == Bob)
        var group = new FilterGroup
        {
            LogicalOperator = LogicalOperator.And,
            Filters =
            [
                new FilterParameter
                {
                    Field = "age",
                    Operator = FilterOperator.Gt,
                    Value = "25",
                },
            ],
            Groups =
            [
                new FilterGroup
                {
                    LogicalOperator = LogicalOperator.Or,
                    Filters =
                    [
                        new FilterParameter
                        {
                            Field = "name",
                            Operator = FilterOperator.Eq,
                            Value = "Ada",
                        },
                        new FilterParameter
                        {
                            Field = "name",
                            Operator = FilterOperator.Eq,
                            Value = "Bob",
                        },
                    ],
                },
            ],
        };

        var result = Run(group);

        Assert.Equal([1, 2], result.Select(a => a.Id));
    }
}
