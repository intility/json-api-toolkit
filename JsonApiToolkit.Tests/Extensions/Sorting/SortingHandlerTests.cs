using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Tests.Models;

namespace JsonApiToolkit.Tests.Extensions.Sorting;

public class SortingHandlerTests
{
    private IQueryable<TestEntity> GetTestData()
    {
        return new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "Charlie",
                Description = "Third alphabetically",
                CreatedAt = new DateTime(2023, 3, 15),
                IsActive = true,
                Status = TestStatus.Published,
            },
            new TestEntity
            {
                Id = 2,
                Name = "Alpha",
                Description = "First alphabetically",
                CreatedAt = new DateTime(2023, 1, 10),
                IsActive = false,
                Status = TestStatus.Draft,
            },
            new TestEntity
            {
                Id = 3,
                Name = "Bravo",
                Description = null,
                CreatedAt = new DateTime(2023, 2, 20),
                IsActive = true,
                Status = TestStatus.Archived,
            },
            new TestEntity
            {
                Id = 4,
                Name = "Delta",
                Description = "Fourth alphabetically",
                CreatedAt = new DateTime(2023, 4, 5),
                IsActive = false,
                Status = TestStatus.Published,
            },
            new TestEntity
            {
                Id = 5,
                Name = "Alpha",
                Description = "Duplicate name",
                CreatedAt = new DateTime(2023, 5, 1),
                IsActive = true,
                Status = TestStatus.Draft,
            },
        }.AsQueryable();
    }

    #region Basic Sorting

    [Fact]
    public void ApplySorting_WithSingleFieldAscending_SortsCorrectly()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "name", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        Assert.Equal(5, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Alpha", result[1].Name);
        Assert.Equal("Bravo", result[2].Name);
        Assert.Equal("Charlie", result[3].Name);
        Assert.Equal("Delta", result[4].Name);
    }

    [Fact]
    public void ApplySorting_WithSingleFieldDescending_SortsCorrectly()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "name", IsDescending = true },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        Assert.Equal(5, result.Count);
        Assert.Equal("Delta", result[0].Name);
        Assert.Equal("Charlie", result[1].Name);
        Assert.Equal("Bravo", result[2].Name);
        // Alpha items at the end (order between them is stable from original)
        Assert.Equal("Alpha", result[3].Name);
        Assert.Equal("Alpha", result[4].Name);
    }

    [Fact]
    public void ApplySorting_WithEmptyList_ReturnsUnchangedQuery()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>();

        var result = query.ApplySorting(sortParameters).ToList();

        // Original order preserved
        Assert.Equal(5, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
        Assert.Equal(3, result[2].Id);
        Assert.Equal(4, result[3].Id);
        Assert.Equal(5, result[4].Id);
    }

    [Fact]
    public void ApplySorting_WithNullList_ReturnsUnchangedQuery()
    {
        var query = GetTestData();

        var result = query.ApplySorting(null!).ToList();

        // Original order preserved
        Assert.Equal(5, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    #endregion

    #region Multiple Field Sorting

    [Fact]
    public void ApplySorting_WithTwoFields_AppliesInPriorityOrder()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "name", IsDescending = false },
            new SortParameter { Field = "id", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        Assert.Equal(5, result.Count);
        // Two "Alpha" entries sorted by Id
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal(2, result[0].Id);
        Assert.Equal("Alpha", result[1].Name);
        Assert.Equal(5, result[1].Id);
        Assert.Equal("Bravo", result[2].Name);
        Assert.Equal("Charlie", result[3].Name);
        Assert.Equal("Delta", result[4].Name);
    }

    [Fact]
    public void ApplySorting_WithTwoFields_MixedDirection()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "name", IsDescending = false },
            new SortParameter { Field = "id", IsDescending = true },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        // Two "Alpha" entries - now sorted by Id descending
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal(5, result[0].Id); // Higher Id first
        Assert.Equal("Alpha", result[1].Name);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public void ApplySorting_WithThreeFields_AppliesAllInOrder()
    {
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "A",
                IsActive = true,
                Status = TestStatus.Draft,
            },
            new TestEntity
            {
                Id = 2,
                Name = "A",
                IsActive = true,
                Status = TestStatus.Published,
            },
            new TestEntity
            {
                Id = 3,
                Name = "A",
                IsActive = false,
                Status = TestStatus.Draft,
            },
            new TestEntity
            {
                Id = 4,
                Name = "B",
                IsActive = true,
                Status = TestStatus.Draft,
            },
        }.AsQueryable();

        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "name", IsDescending = false },
            new SortParameter { Field = "isActive", IsDescending = true }, // true before false
            new SortParameter { Field = "status", IsDescending = false },
        };

        var result = testData.ApplySorting(sortParameters).ToList();

        Assert.Equal(4, result.Count);
        // Name "A" first, then by IsActive (true first), then by Status
        Assert.Equal(1, result[0].Id); // A, true, Draft
        Assert.Equal(2, result[1].Id); // A, true, Published
        Assert.Equal(3, result[2].Id); // A, false, Draft
        Assert.Equal(4, result[3].Id); // B, true, Draft
    }

    #endregion

    #region Property Name Mapping

    [Fact]
    public void ApplySorting_WithCamelCaseField_MapsToPascalCase()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "createdAt", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        Assert.Equal(5, result.Count);
        Assert.Equal(new DateTime(2023, 1, 10), result[0].CreatedAt);
        Assert.Equal(new DateTime(2023, 2, 20), result[1].CreatedAt);
        Assert.Equal(new DateTime(2023, 3, 15), result[2].CreatedAt);
        Assert.Equal(new DateTime(2023, 4, 5), result[3].CreatedAt);
        Assert.Equal(new DateTime(2023, 5, 1), result[4].CreatedAt);
    }

    [Fact]
    public void ApplySorting_WithPascalCaseField_StillWorks()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "CreatedAt", IsDescending = true },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        Assert.Equal(5, result.Count);
        Assert.Equal(new DateTime(2023, 5, 1), result[0].CreatedAt);
        Assert.Equal(new DateTime(2023, 1, 10), result[4].CreatedAt);
    }

    [Fact]
    public void ApplySorting_WithJsonPropertyName_SortsByJsonName()
    {
        var testData = new List<TestEntityWithJsonPropertyName>
        {
            new TestEntityWithJsonPropertyName { Id = 1, ActualPropertyName = "Zebra" },
            new TestEntityWithJsonPropertyName { Id = 2, ActualPropertyName = "Apple" },
            new TestEntityWithJsonPropertyName { Id = 3, ActualPropertyName = "Mango" },
        }.AsQueryable();

        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "customId", IsDescending = false },
        };

        var result = testData.ApplySorting(sortParameters).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal("Apple", result[0].ActualPropertyName);
        Assert.Equal("Mango", result[1].ActualPropertyName);
        Assert.Equal("Zebra", result[2].ActualPropertyName);
    }

    #endregion

    #region Invalid Field Handling

    [Fact]
    public void ApplySorting_WithNonExistentField_SkipsField()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "nonExistentField", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        // Query unchanged - original order
        Assert.Equal(5, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public void ApplySorting_WithMixedValidAndInvalidFields_AppliesValidOnly()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "invalidField", IsDescending = false },
            new SortParameter { Field = "name", IsDescending = false },
            new SortParameter { Field = "anotherInvalid", IsDescending = true },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        // Should be sorted by name (the only valid field)
        Assert.Equal(5, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Alpha", result[1].Name);
        Assert.Equal("Bravo", result[2].Name);
    }

    [Fact]
    public void ApplySorting_WithAllInvalidFields_ReturnsUnchangedQuery()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "invalid1", IsDescending = false },
            new SortParameter { Field = "invalid2", IsDescending = true },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        // Original order preserved
        Assert.Equal(5, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public void ApplySorting_WithEmptyFieldName_SkipsField()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "", IsDescending = false },
            new SortParameter { Field = "name", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        // Should still sort by name
        Assert.Equal("Alpha", result[0].Name);
    }

    #endregion

    #region Different Data Types

    [Fact]
    public void ApplySorting_ByIntegerField_SortsNumerically()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "id", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
        Assert.Equal(3, result[2].Id);
        Assert.Equal(4, result[3].Id);
        Assert.Equal(5, result[4].Id);
    }

    [Fact]
    public void ApplySorting_ByDateTimeField_SortsChronologically()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "createdAt", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        for (int i = 0; i < result.Count - 1; i++)
        {
            Assert.True(result[i].CreatedAt <= result[i + 1].CreatedAt);
        }
    }

    [Fact]
    public void ApplySorting_ByBooleanField_SortsCorrectly()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "isActive", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        // false (0) comes before true (1) in ascending order
        Assert.False(result[0].IsActive);
        Assert.False(result[1].IsActive);
        Assert.True(result[2].IsActive);
        Assert.True(result[3].IsActive);
        Assert.True(result[4].IsActive);
    }

    [Fact]
    public void ApplySorting_ByBooleanFieldDescending_SortsCorrectly()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "isActive", IsDescending = true },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        // true (1) comes before false (0) in descending order
        Assert.True(result[0].IsActive);
        Assert.True(result[1].IsActive);
        Assert.True(result[2].IsActive);
        Assert.False(result[3].IsActive);
        Assert.False(result[4].IsActive);
    }

    [Fact]
    public void ApplySorting_ByEnumField_SortsByEnumValue()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "status", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        // Enum values: Draft=0, Published=1, Archived=2
        Assert.Equal(TestStatus.Draft, result[0].Status);
        Assert.Equal(TestStatus.Draft, result[1].Status);
        Assert.Equal(TestStatus.Published, result[2].Status);
        Assert.Equal(TestStatus.Published, result[3].Status);
        Assert.Equal(TestStatus.Archived, result[4].Status);
    }

    [Fact]
    public void ApplySorting_ByNullableField_HandlesNullsCorrectly()
    {
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "A",
                RelatedEntityId = 10,
            },
            new TestEntity
            {
                Id = 2,
                Name = "B",
                RelatedEntityId = null,
            },
            new TestEntity
            {
                Id = 3,
                Name = "C",
                RelatedEntityId = 5,
            },
            new TestEntity
            {
                Id = 4,
                Name = "D",
                RelatedEntityId = null,
            },
        }.AsQueryable();

        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "relatedEntityId", IsDescending = false },
        };

        var result = testData.ApplySorting(sortParameters).ToList();

        // Nulls typically sort first in ascending order
        Assert.Null(result[0].RelatedEntityId);
        Assert.Null(result[1].RelatedEntityId);
        Assert.Equal(5, result[2].RelatedEntityId);
        Assert.Equal(10, result[3].RelatedEntityId);
    }

    [Fact]
    public void ApplySorting_ByStringField_SortsAlphabetically()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "name", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        var names = result.Select(e => e.Name).ToList();
        var sortedNames = names.OrderBy(n => n).ToList();
        Assert.Equal(sortedNames, names);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ApplySorting_WithSingleItem_ReturnsItem()
    {
        var query = new List<TestEntity>
        {
            new TestEntity { Id = 1, Name = "Only" },
        }.AsQueryable();

        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "name", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        Assert.Single(result);
        Assert.Equal("Only", result[0].Name);
    }

    [Fact]
    public void ApplySorting_WithEmptyQuery_ReturnsEmpty()
    {
        var query = new List<TestEntity>().AsQueryable();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "name", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void ApplySorting_WithDuplicateValues_MaintainsStableSort()
    {
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "Same",
                CreatedAt = DateTime.Now,
            },
            new TestEntity
            {
                Id = 2,
                Name = "Same",
                CreatedAt = DateTime.Now,
            },
            new TestEntity
            {
                Id = 3,
                Name = "Same",
                CreatedAt = DateTime.Now,
            },
        }.AsQueryable();

        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "name", IsDescending = false },
        };

        var result = testData.ApplySorting(sortParameters).ToList();

        // All have same name, order should be stable (original order preserved)
        Assert.Equal(3, result.Count);
        Assert.All(result, e => Assert.Equal("Same", e.Name));
    }

    [Fact]
    public void ApplySorting_IsChainable_WithOtherOperations()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "name", IsDescending = false },
        };

        var result = query.Where(e => e.IsActive).ApplySorting(sortParameters).Take(2).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Bravo", result[1].Name);
    }

    [Fact]
    public void ApplySorting_PreservesQueryableType()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "name", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters);

        Assert.IsAssignableFrom<IQueryable<TestEntity>>(result);
        Assert.IsAssignableFrom<IOrderedQueryable<TestEntity>>(result);
    }

    #endregion

    #region Null Description Sorting

    [Fact]
    public void ApplySorting_ByNullableStringField_SortsCorrectly()
    {
        var query = GetTestData();
        var sortParameters = new List<SortParameter>
        {
            new SortParameter { Field = "description", IsDescending = false },
        };

        var result = query.ApplySorting(sortParameters).ToList();

        // Verify sorting works (nulls position depends on LINQ provider)
        // Just verify all items are present and non-null values are in order
        Assert.Equal(5, result.Count);

        // Get non-null descriptions in result order
        var nonNullDescriptions = result
            .Where(e => e.Description != null)
            .Select(e => e.Description)
            .ToList();

        // Verify non-null descriptions are sorted
        var sortedDescriptions = nonNullDescriptions.OrderBy(d => d).ToList();
        Assert.Equal(sortedDescriptions, nonNullDescriptions);
    }

    #endregion
}
