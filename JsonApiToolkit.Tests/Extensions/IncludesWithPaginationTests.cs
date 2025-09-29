using JsonApiToolkit.Extensions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JsonApiToolkit.Tests.Extensions;

/// <summary>
/// Tests for EF Core Include behavior with pagination to catch split query issues.
/// </summary>
public class IncludesWithPaginationTests
{
    private class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<RelatedEntity> Related { get; set; } = new();
    }

    private class RelatedEntity
    {
        public int Id { get; set; }
        public string Value { get; set; } = string.Empty;
        public int TestEntityId { get; set; }
        public TestEntity? TestEntity { get; set; }
    }

    private class TestDbContext : DbContext
    {
        public DbSet<TestEntity> TestEntities { get; set; } = null!;
        public DbSet<RelatedEntity> RelatedEntities { get; set; } = null!;

        public TestDbContext(DbContextOptions<TestDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestEntity>().HasMany(e => e.Related).WithOne(r => r.TestEntity);
        }
    }

    private static TestDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new TestDbContext(options);

        // Create a large dataset similar to the user's scenario
        for (int i = 1; i <= 100; i++)
        {
            var entity = new TestEntity
            {
                Id = i,
                Name = $"Entity {i}",
                Related = new(),
            };

            // Add 2-5 related entities per main entity
            var relatedCount = (i % 4) + 2;
            for (int j = 1; j <= relatedCount; j++)
            {
                entity.Related.Add(
                    new RelatedEntity
                    {
                        Id = i * 100 + j,
                        Value = $"Related {i}-{j}",
                        TestEntityId = i,
                    }
                );
            }

            context.TestEntities.Add(entity);
        }

        context.SaveChanges();
        return context;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(50)]
    public async Task ApplyIncludesSingleQuery_WithPagination_LoadsAllRelatedEntitiesAsync(
        int pageSize
    )
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act - Apply includes with AsSingleQuery and pagination
        var query = context
            .TestEntities.OrderBy(e => e.Id)
            .ApplyIncludesSingleQuery(new List<string> { "Related" })
            .Skip(0)
            .Take(pageSize);

        var results = await query.ToListAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal(pageSize, results.Count);

        // Verify that ALL entities have their related entities loaded
        foreach (var entity in results)
        {
            Assert.NotNull(entity.Related);
            Assert.NotEmpty(entity.Related);
            Assert.InRange(entity.Related.Count, 2, 5); // We created 2-5 related entities per entity
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(9)]
    public async Task ApplyIncludes_WithPagination_MayFailOnSmallPageSizesAsync(int pageSize)
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act - Regular includes (may exhibit split query issues)
        var query = context
            .TestEntities.OrderBy(e => e.Id)
            .ApplyIncludes(new List<string> { "Related" })
            .Skip(0)
            .Take(pageSize);

        var results = await query.ToListAsync();

        // Assert - Just verify we got results, may or may not have includes loaded
        Assert.NotEmpty(results);
        Assert.Equal(pageSize, results.Count);

        // Note: This test documents the issue - with regular ApplyIncludes,
        // related entities may not be loaded consistently across page sizes
    }

    [Fact]
    public async Task ApplyIncludesSingleQuery_WithLargeDatasetAndPaginationAtFirstPage_LoadsCorrectEntitiesAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act - Get first page with page size 1 (the failing scenario from user's issue)
        var results = await context
            .TestEntities.OrderBy(e => e.Id)
            .ApplyIncludesSingleQuery(new List<string> { "Related" })
            .Skip(0)
            .Take(1)
            .ToListAsync();

        // Assert
        var entity = Assert.Single(results);
        Assert.Equal(1, entity.Id);
        Assert.NotEmpty(entity.Related);
        Assert.All(entity.Related, r => Assert.Equal(1, r.TestEntityId));
    }

    [Fact]
    public async Task ApplyIncludesSingleQuery_WithLargeDatasetAndPaginationAtMiddlePage_LoadsCorrectEntitiesAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act - Get middle page (page 50) with page size 1
        var results = await context
            .TestEntities.OrderBy(e => e.Id)
            .ApplyIncludesSingleQuery(new List<string> { "Related" })
            .Skip(49)
            .Take(1)
            .ToListAsync();

        // Assert
        var entity = Assert.Single(results);
        Assert.Equal(50, entity.Id);
        Assert.NotEmpty(entity.Related);
        Assert.All(entity.Related, r => Assert.Equal(50, r.TestEntityId));
    }

    [Fact]
    public async Task ApplyIncludesSingleQuery_WithMultiplePagesSmallPageSize_EachPageHasCorrectIncludesAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        const int pageSize = 3;
        const int totalPages = 5;

        // Act & Assert - Verify each page has correct includes
        for (int page = 0; page < totalPages; page++)
        {
            var results = await context
                .TestEntities.OrderBy(e => e.Id)
                .ApplyIncludesSingleQuery(new List<string> { "Related" })
                .Skip(page * pageSize)
                .Take(pageSize)
                .ToListAsync();

            Assert.Equal(pageSize, results.Count);

            foreach (var entity in results)
            {
                Assert.NotEmpty(entity.Related);
                // Verify the related entities belong to this entity
                Assert.All(entity.Related, r => Assert.Equal(entity.Id, r.TestEntityId));
            }
        }
    }

    [Fact]
    public async Task ApplyIncludesSingleQuery_WithNullIncludePaths_ReturnsQueryUnmodifiedAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act
        var query = context.TestEntities.ApplyIncludesSingleQuery(null);
        var results = await query.ToListAsync();

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task ApplyIncludesSingleQuery_WithEmptyIncludePaths_ReturnsQueryUnmodifiedAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act
        var query = context.TestEntities.ApplyIncludesSingleQuery(new List<string>());
        var results = await query.ToListAsync();

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task ApplyIncludesSingleQuery_WithoutPagination_WorksCorrectlyAsync()
    {
        // Arrange
        using var context = CreateInMemoryContext();

        // Act
        var results = await context
            .TestEntities.OrderBy(e => e.Id)
            .ApplyIncludesSingleQuery(new List<string> { "Related" })
            .Take(10)
            .ToListAsync();

        // Assert
        Assert.Equal(10, results.Count);
        Assert.All(results, e => Assert.NotEmpty(e.Related));
    }
}
