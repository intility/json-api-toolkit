using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Models.Errors;
using JsonApiToolkit.Models.Querying.Filtering;
using JsonApiToolkit.Tests.Models;

namespace JsonApiToolkit.Tests.Extensions.Filtering;

public class FilterCompositionTests
{
    #region Test Data

    private IQueryable<TestEntity> GetTestDataWithRelations()
    {
        return new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "Entity1",
                RelatedEntity = new TestRelatedEntity
                {
                    Id = 10,
                    Name = "Related1",
                    NestedEntity = new TestNestedEntity { Id = 100, Value = "DeepValue1" },
                },
                Children = new List<TestChildEntity>
                {
                    new TestChildEntity
                    {
                        Id = 101,
                        Name = "Child1A",
                        Tags = new List<string> { "important", "urgent" },
                    },
                    new TestChildEntity
                    {
                        Id = 102,
                        Name = "Child1B",
                        Tags = new List<string> { "normal" },
                    },
                },
            },
            new TestEntity
            {
                Id = 2,
                Name = "Entity2",
                RelatedEntity = new TestRelatedEntity
                {
                    Id = 20,
                    Name = "Related2",
                    NestedEntity = new TestNestedEntity { Id = 200, Value = "DeepValue2" },
                },
                Children = new List<TestChildEntity>
                {
                    new TestChildEntity
                    {
                        Id = 201,
                        Name = "Child2A",
                        Tags = new List<string> { "low-priority" },
                    },
                },
            },
            new TestEntity
            {
                Id = 3,
                Name = "Entity3",
                RelatedEntity = null, // No related entity
                Children = new List<TestChildEntity>(), // Empty children
            },
            new TestEntity
            {
                Id = 4,
                Name = "Entity4",
                RelatedEntity = new TestRelatedEntity
                {
                    Id = 40,
                    Name = "Related4",
                    NestedEntity = null, // Related exists but nested is null
                },
                Children = new List<TestChildEntity>
                {
                    new TestChildEntity
                    {
                        Id = 401,
                        Name = "Child4A",
                        Tags = new List<string>(),
                    },
                },
            },
        }.AsQueryable();
    }

    #endregion

    #region Single-Level Navigation

    [Fact]
    public void Filter_SingleLevelNavigation_FiltersCorrectly()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.name",
                    Operator = FilterOperator.Eq,
                    Value = "Related1",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void Filter_SingleLevelNavigation_CamelCaseMapping()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.id",
                    Operator = FilterOperator.Gt,
                    Value = "15",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == 2);
        Assert.Contains(result, e => e.Id == 4);
    }

    [Fact]
    public void Filter_SingleLevelNavigation_InvalidProperty_SkipsFilter()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.nonExistent",
                    Operator = FilterOperator.Eq,
                    Value = "test",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        // Invalid filter is skipped, returns all items
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Filter_SingleLevelNavigation_InvalidNavigationProperty_SkipsFilter()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "nonExistent.name",
                    Operator = FilterOperator.Eq,
                    Value = "test",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void Filter_SingleLevelNavigation_LikeOperator()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.name",
                    Operator = FilterOperator.Like,
                    Value = "Related",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        // Entities 1, 2, 4 have related entities with names containing "Related"
        Assert.Equal(3, result.Count);
    }

    #endregion

    #region Multi-Level Navigation

    [Fact]
    public void Filter_TwoLevelNavigation_FiltersCorrectly()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.nestedEntity.value",
                    Operator = FilterOperator.Eq,
                    Value = "DeepValue1",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void Filter_TwoLevelNavigation_IntegerProperty()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.nestedEntity.id",
                    Operator = FilterOperator.Lt,
                    Value = "150",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void Filter_TwoLevelNavigation_LikeOperator()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.nestedEntity.value",
                    Operator = FilterOperator.Like,
                    Value = "DeepValue",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        // Entities 1 and 2 have nested entities with values containing "DeepValue"
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region Collection Navigation

    [Fact]
    public void Filter_CollectionNavigation_SingleLevel_Any()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "children.name",
                    Operator = FilterOperator.Eq,
                    Value = "Child1A",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void Filter_CollectionNavigation_LikeOperator()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "children.name",
                    Operator = FilterOperator.Like,
                    Value = "Child1",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void Filter_CollectionNavigation_MultipleMatches()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "children.name",
                    Operator = FilterOperator.Like,
                    Value = "Child",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        // Entities 1, 2, 4 have children with names containing "Child"
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Filter_CollectionNavigation_EmptyCollection_NoMatch()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "children.name",
                    Operator = FilterOperator.Eq,
                    Value = "AnyValue",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        // Entity 3 has empty children, should not match
        Assert.DoesNotContain(result, e => e.Id == 3);
    }

    [Fact]
    public void Filter_CollectionProperty_InOperator()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "children.tags",
                    Operator = FilterOperator.In,
                    Value = "important",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void Filter_CollectionProperty_LikeOperator()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "children.tags",
                    Operator = FilterOperator.Like,
                    Value = "urgent",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void Filter_CollectionNavigation_IntegerProperty()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "children.id",
                    Operator = FilterOperator.Gt,
                    Value = "200",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        // Entities 2 and 4 have children with id > 200
        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == 2);
        Assert.Contains(result, e => e.Id == 4);
    }

    #endregion

    #region Null Handling

    [Fact]
    public void Filter_NullNavigationProperty_ExcludedFromResults()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.name",
                    Operator = FilterOperator.Like,
                    Value = "Related",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        // Entity 3 has null RelatedEntity, should be excluded
        Assert.DoesNotContain(result, e => e.Id == 3);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void Filter_NullNestedProperty_ExcludedFromResults()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.nestedEntity.value",
                    Operator = FilterOperator.Like,
                    Value = "Deep",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        // Entity 3 has null RelatedEntity, Entity 4 has null NestedEntity
        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == 1);
        Assert.Contains(result, e => e.Id == 2);
    }

    [Fact]
    public void Filter_IsNull_OnDirectProperty()
    {
        // IsNull works on direct properties, not through navigation paths
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "E1",
                Description = "Has description",
            },
            new TestEntity
            {
                Id = 2,
                Name = "E2",
                Description = null,
            },
            new TestEntity
            {
                Id = 3,
                Name = "E3",
                Description = "Also has description",
            },
        }.AsQueryable();

        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "description",
                    Operator = FilterOperator.IsNull,
                    Value = "true",
                },
            },
        };

        var result = testData.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void Filter_IsNotNull_OnDirectProperty()
    {
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "E1",
                Description = "Has description",
            },
            new TestEntity
            {
                Id = 2,
                Name = "E2",
                Description = null,
            },
            new TestEntity
            {
                Id = 3,
                Name = "E3",
                Description = "Also has description",
            },
        }.AsQueryable();

        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "description",
                    Operator = FilterOperator.IsNotNull,
                    Value = "true",
                },
            },
        };

        var result = testData.ApplyFilters(filterGroup).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == 1);
        Assert.Contains(result, e => e.Id == 3);
    }

    [Fact]
    public void Filter_NeOperator_WithNullNavigation_IncludesNulls()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.name",
                    Operator = FilterOperator.Ne,
                    Value = "Related1",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        // Ne should include entities where RelatedEntity is null OR name != "Related1"
        Assert.Equal(3, result.Count);
        Assert.Contains(result, e => e.Id == 2);
        Assert.Contains(result, e => e.Id == 3); // null RelatedEntity
        Assert.Contains(result, e => e.Id == 4);
    }

    #endregion

    #region All Filter Operators on Nested Properties

    [Fact]
    public void Filter_Nested_EqOperator()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.id",
                    Operator = FilterOperator.Eq,
                    Value = "20",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void Filter_Nested_NeOperator()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.id",
                    Operator = FilterOperator.Ne,
                    Value = "10",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        // All except entity 1, plus entity 3 (null)
        Assert.Equal(3, result.Count);
        Assert.DoesNotContain(result, e => e.Id == 1);
    }

    [Fact]
    public void Filter_Nested_GtOperator()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.id",
                    Operator = FilterOperator.Gt,
                    Value = "20",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(4, result[0].Id);
    }

    [Fact]
    public void Filter_Nested_GeOperator()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.id",
                    Operator = FilterOperator.Ge,
                    Value = "20",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == 2);
        Assert.Contains(result, e => e.Id == 4);
    }

    [Fact]
    public void Filter_Nested_LtOperator()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.id",
                    Operator = FilterOperator.Lt,
                    Value = "20",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void Filter_Nested_LeOperator()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.id",
                    Operator = FilterOperator.Le,
                    Value = "20",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == 1);
        Assert.Contains(result, e => e.Id == 2);
    }

    [Fact]
    public void Filter_Nested_InOperator()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.name",
                    Operator = FilterOperator.In,
                    Value = "Related1,Related4",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == 1);
        Assert.Contains(result, e => e.Id == 4);
    }

    [Fact]
    public void Filter_Nested_NinOperator()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.name",
                    Operator = FilterOperator.Nin,
                    Value = "Related1,Related2",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        // Entity 3 (null) and Entity 4 should match
        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == 3);
        Assert.Contains(result, e => e.Id == 4);
    }

    #endregion

    #region Type Conversions

    [Fact]
    public void Filter_Nested_DateTimeProperty()
    {
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "Entity1",
                RelatedEntity = new TestRelatedEntity { Id = 1, Name = "R1" },
                CreatedAt = new DateTime(2023, 6, 15),
            },
            new TestEntity
            {
                Id = 2,
                Name = "Entity2",
                RelatedEntity = new TestRelatedEntity { Id = 2, Name = "R2" },
                CreatedAt = new DateTime(2023, 12, 1),
            },
        }.AsQueryable();

        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "createdAt",
                    Operator = FilterOperator.Gt,
                    Value = "2023-07-01",
                },
            },
        };

        var result = testData.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].Id);
    }

    [Fact]
    public void Filter_Nested_EnumProperty()
    {
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "E1",
                Status = TestStatus.Published,
            },
            new TestEntity
            {
                Id = 2,
                Name = "E2",
                Status = TestStatus.Draft,
            },
            new TestEntity
            {
                Id = 3,
                Name = "E3",
                Status = TestStatus.Archived,
            },
        }.AsQueryable();

        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "status",
                    Operator = FilterOperator.Eq,
                    Value = "Published",
                },
            },
        };

        var result = testData.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(TestStatus.Published, result[0].Status);
    }

    [Fact]
    public void Filter_Nested_NullableProperty()
    {
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "E1",
                RelatedEntityId = 10,
            },
            new TestEntity
            {
                Id = 2,
                Name = "E2",
                RelatedEntityId = null,
            },
            new TestEntity
            {
                Id = 3,
                Name = "E3",
                RelatedEntityId = 30,
            },
        }.AsQueryable();

        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntityId",
                    Operator = FilterOperator.Gt,
                    Value = "15",
                },
            },
        };

        var result = testData.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(3, result[0].Id);
    }

    [Fact]
    public void Filter_InvalidTypeConversion_ThrowsFormatException()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.id",
                    Operator = FilterOperator.Eq,
                    Value = "not-a-number",
                },
            },
        };

        // Invalid type conversion throws FormatException with helpful message
        var ex = Assert.Throws<FormatException>(() => query.ApplyFilters(filterGroup).ToList());
        Assert.Contains("not-a-number", ex.Message);
        Assert.Contains("Int32", ex.Message);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Filter_SingleSegment_NoNavigation()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "name",
                    Operator = FilterOperator.Eq,
                    Value = "Entity1",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void Filter_EmptyValue_HandledGracefully()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.name",
                    Operator = FilterOperator.Eq,
                    Value = "",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        // No related entities have empty names
        Assert.Empty(result);
    }

    [Fact]
    public void Filter_MultipleNestedFilters_Combined()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            LogicalOperator = LogicalOperator.And,
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.name",
                    Operator = FilterOperator.Like,
                    Value = "Related",
                },
                new FilterParameter
                {
                    Field = "relatedEntity.id",
                    Operator = FilterOperator.Lt,
                    Value = "30",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == 1);
        Assert.Contains(result, e => e.Id == 2);
    }

    [Fact]
    public void Filter_OrLogic_WithNestedFilters()
    {
        var query = GetTestDataWithRelations();
        var filterGroup = new FilterGroup
        {
            LogicalOperator = LogicalOperator.Or,
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.id",
                    Operator = FilterOperator.Eq,
                    Value = "10",
                },
                new FilterParameter
                {
                    Field = "relatedEntity.id",
                    Operator = FilterOperator.Eq,
                    Value = "40",
                },
            },
        };

        var result = query.ApplyFilters(filterGroup).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, e => e.Id == 1);
        Assert.Contains(result, e => e.Id == 4);
    }

    [Fact]
    public void Filter_SpecialCharactersInValue_HandledCorrectly()
    {
        var testData = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 1,
                Name = "Test",
                RelatedEntity = new TestRelatedEntity { Id = 1, Name = "Value with 'quotes'" },
            },
            new TestEntity
            {
                Id = 2,
                Name = "Test2",
                RelatedEntity = new TestRelatedEntity { Id = 2, Name = "Normal" },
            },
        }.AsQueryable();

        var filterGroup = new FilterGroup
        {
            Filters = new List<FilterParameter>
            {
                new FilterParameter
                {
                    Field = "relatedEntity.name",
                    Operator = FilterOperator.Like,
                    Value = "quotes",
                },
            },
        };

        var result = testData.ApplyFilters(filterGroup).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    #endregion
}
