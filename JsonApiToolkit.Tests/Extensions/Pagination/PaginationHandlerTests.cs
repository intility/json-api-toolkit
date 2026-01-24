using JsonApiToolkit.Extensions.Querying;
using JsonApiToolkit.Models.Querying;
using JsonApiToolkit.Tests.Models;

namespace JsonApiToolkit.Tests.Extensions.Pagination;

public class PaginationHandlerTests
{
    private IQueryable<TestEntity> GetTestData(int count = 10)
    {
        return Enumerable
            .Range(1, count)
            .Select(i => new TestEntity
            {
                Id = i,
                Name = $"Entity{i}",
                CreatedAt = DateTime.Now.AddDays(-i),
            })
            .AsQueryable();
    }

    #region Basic Pagination

    [Fact]
    public void ApplyPagination_FirstPage_ReturnsCorrectItems()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = 1, Size = 3 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(2, result[1].Id);
        Assert.Equal(3, result[2].Id);
    }

    [Fact]
    public void ApplyPagination_MiddlePage_ReturnsCorrectItems()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = 2, Size = 3 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(4, result[0].Id);
        Assert.Equal(5, result[1].Id);
        Assert.Equal(6, result[2].Id);
    }

    [Fact]
    public void ApplyPagination_LastPage_ReturnsRemainingItems()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = 4, Size = 3 };

        var result = query.ApplyPagination(pagination).ToList();

        // 10 items / 3 per page = 4 pages, last page has 1 item
        Assert.Single(result);
        Assert.Equal(10, result[0].Id);
    }

    [Fact]
    public void ApplyPagination_ExactlyFillsPages_LastPageIsFull()
    {
        var query = GetTestData(9);
        var pagination = new PaginationParameters { Number = 3, Size = 3 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(7, result[0].Id);
        Assert.Equal(8, result[1].Id);
        Assert.Equal(9, result[2].Id);
    }

    #endregion

    #region Page Number Edge Cases

    [Fact]
    public void ApplyPagination_PageZero_ClampsToFirstPage()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = 0, Size = 3 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void ApplyPagination_NegativePage_ClampsToFirstPage()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = -5, Size = 3 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void ApplyPagination_PageBeyondTotal_ClampsToLastPage()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = 100, Size = 3 };

        var result = query.ApplyPagination(pagination).ToList();

        // Should return last page (page 4 with 1 item)
        Assert.Single(result);
        Assert.Equal(10, result[0].Id);
    }

    [Fact]
    public void ApplyPagination_MaxIntPage_ClampsToLastPage()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = int.MaxValue, Size = 3 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Single(result);
        Assert.Equal(10, result[0].Id);
    }

    [Fact]
    public void ApplyPagination_MinIntPage_ClampsToFirstPage()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = int.MinValue, Size = 3 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Id);
    }

    #endregion

    #region Page Size Edge Cases

    [Fact]
    public void ApplyPagination_SizeZero_ClampsToSizeOne()
    {
        var query = GetTestData(5);
        var pagination = new PaginationParameters { Number = 1, Size = 0 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void ApplyPagination_NegativeSize_ClampsToSizeOne()
    {
        var query = GetTestData(5);
        var pagination = new PaginationParameters { Number = 1, Size = -10 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void ApplyPagination_SizeOne_ReturnsSingleItem()
    {
        var query = GetTestData(5);
        var pagination = new PaginationParameters { Number = 3, Size = 1 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Single(result);
        Assert.Equal(3, result[0].Id);
    }

    [Fact]
    public void ApplyPagination_SizeLargerThanTotal_ReturnsAllItems()
    {
        var query = GetTestData(5);
        var pagination = new PaginationParameters { Number = 1, Size = 100 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void ApplyPagination_SizeEqualsTotal_ReturnsAllOnOnePage()
    {
        var query = GetTestData(5);
        var pagination = new PaginationParameters { Number = 1, Size = 5 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void ApplyPagination_VeryLargeSize_HandlesGracefully()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = 1, Size = int.MaxValue };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Equal(10, result.Count);
    }

    #endregion

    #region Empty Dataset

    [Fact]
    public void ApplyPagination_EmptyDataset_ReturnsEmpty()
    {
        var query = GetTestData(0);
        var pagination = new PaginationParameters { Number = 1, Size = 10 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void ApplyPagination_EmptyDataset_PageTwo_ReturnsEmpty()
    {
        var query = GetTestData(0);
        var pagination = new PaginationParameters { Number = 2, Size = 10 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public async Task CreatePaginationMetaAsync_EmptyDataset_ReturnsZeroTotals()
    {
        var query = GetTestData(0);
        var pagination = new PaginationParameters { Number = 1, Size = 10 };

        var meta = await query.CreatePaginationMetaAsync(pagination);

        Assert.Equal(0, meta.TotalResources);
        Assert.Equal(0, meta.TotalPages);
        Assert.Equal(1, meta.CurrentPage); // Clamped to 1 even with no data
        Assert.Equal(10, meta.PageSize);
    }

    #endregion

    #region Single Item Dataset

    [Fact]
    public void ApplyPagination_SingleItem_ReturnsItem()
    {
        var query = GetTestData(1);
        var pagination = new PaginationParameters { Number = 1, Size = 10 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void ApplyPagination_SingleItem_PageTwo_ClampsToPageOne()
    {
        var query = GetTestData(1);
        var pagination = new PaginationParameters { Number = 2, Size = 10 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    #endregion

    #region Pagination Metadata

    [Fact]
    public async Task CreatePaginationMetaAsync_NormalCase_ReturnsCorrectMetadata()
    {
        var query = GetTestData(25);
        var pagination = new PaginationParameters { Number = 2, Size = 10 };

        var meta = await query.CreatePaginationMetaAsync(pagination);

        Assert.Equal(25, meta.TotalResources);
        Assert.Equal(3, meta.TotalPages);
        Assert.Equal(2, meta.CurrentPage);
        Assert.Equal(10, meta.PageSize);
    }

    [Fact]
    public async Task CreatePaginationMetaAsync_FirstPage_ReturnsCorrectMetadata()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = 1, Size = 3 };

        var meta = await query.CreatePaginationMetaAsync(pagination);

        Assert.Equal(10, meta.TotalResources);
        Assert.Equal(4, meta.TotalPages);
        Assert.Equal(1, meta.CurrentPage);
        Assert.Equal(3, meta.PageSize);
    }

    [Fact]
    public async Task CreatePaginationMetaAsync_LastPage_ReturnsCorrectMetadata()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = 4, Size = 3 };

        var meta = await query.CreatePaginationMetaAsync(pagination);

        Assert.Equal(10, meta.TotalResources);
        Assert.Equal(4, meta.TotalPages);
        Assert.Equal(4, meta.CurrentPage);
        Assert.Equal(3, meta.PageSize);
    }

    [Fact]
    public async Task CreatePaginationMetaAsync_PageBeyondTotal_ClampsCurrentPage()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = 100, Size = 3 };

        var meta = await query.CreatePaginationMetaAsync(pagination);

        Assert.Equal(10, meta.TotalResources);
        Assert.Equal(4, meta.TotalPages);
        Assert.Equal(4, meta.CurrentPage); // Clamped to last page
        Assert.Equal(3, meta.PageSize);
    }

    [Fact]
    public async Task CreatePaginationMetaAsync_PageZero_ClampsToPageOne()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = 0, Size = 3 };

        var meta = await query.CreatePaginationMetaAsync(pagination);

        Assert.Equal(1, meta.CurrentPage);
    }

    [Fact]
    public async Task CreatePaginationMetaAsync_SizeZero_ClampsToSizeOne()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = 1, Size = 0 };

        var meta = await query.CreatePaginationMetaAsync(pagination);

        Assert.Equal(10, meta.TotalResources);
        Assert.Equal(10, meta.TotalPages); // 10 items / size 1 = 10 pages
        Assert.Equal(1, meta.PageSize);
    }

    [Fact]
    public async Task CreatePaginationMetaAsync_SinglePage_TotalPagesIsOne()
    {
        var query = GetTestData(5);
        var pagination = new PaginationParameters { Number = 1, Size = 10 };

        var meta = await query.CreatePaginationMetaAsync(pagination);

        Assert.Equal(5, meta.TotalResources);
        Assert.Equal(1, meta.TotalPages);
        Assert.Equal(1, meta.CurrentPage);
    }

    #endregion

    #region Consistency Between Apply and Meta

    [Fact]
    public async Task ApplyAndMeta_ReturnConsistentResults()
    {
        var query = GetTestData(25);
        var pagination = new PaginationParameters { Number = 2, Size = 10 };

        var result = query.ApplyPagination(pagination).ToList();
        var meta = await query.CreatePaginationMetaAsync(pagination);

        // The actual page of data should match metadata
        Assert.Equal(10, result.Count);
        Assert.Equal(25, meta.TotalResources);
        Assert.Equal(3, meta.TotalPages);
        Assert.Equal(2, meta.CurrentPage);

        // First item should be 11th (page 2 starts at offset 10)
        Assert.Equal(11, result[0].Id);
    }

    [Fact]
    public async Task ApplyAndMeta_WithPageOverflow_BothClampToLastPage()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = 999, Size = 3 };

        var result = query.ApplyPagination(pagination).ToList();
        var meta = await query.CreatePaginationMetaAsync(pagination);

        // Both should clamp to last page (page 4)
        Assert.Single(result);
        Assert.Equal(10, result[0].Id);
        Assert.Equal(4, meta.CurrentPage);
        Assert.Equal(4, meta.TotalPages);
    }

    [Fact]
    public async Task ApplyAndMeta_WithPageUnderflow_BothClampToFirstPage()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = -5, Size = 3 };

        var result = query.ApplyPagination(pagination).ToList();
        var meta = await query.CreatePaginationMetaAsync(pagination);

        Assert.Equal(3, result.Count);
        Assert.Equal(1, result[0].Id);
        Assert.Equal(1, meta.CurrentPage);
    }

    #endregion

    #region Edge Case Combinations

    [Fact]
    public void ApplyPagination_SizeZeroAndPageZero_HandlesGracefully()
    {
        var query = GetTestData(5);
        var pagination = new PaginationParameters { Number = 0, Size = 0 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public void ApplyPagination_NegativeSizeAndNegativePage_HandlesGracefully()
    {
        var query = GetTestData(5);
        var pagination = new PaginationParameters { Number = -1, Size = -1 };

        var result = query.ApplyPagination(pagination).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task CreatePaginationMetaAsync_ExtremeValues_HandlesGracefully()
    {
        var query = GetTestData(5);
        var pagination = new PaginationParameters { Number = int.MaxValue, Size = int.MinValue };

        var meta = await query.CreatePaginationMetaAsync(pagination);

        // Size clamped to 1, page clamped to last page (5)
        Assert.Equal(5, meta.TotalResources);
        Assert.Equal(5, meta.TotalPages); // 5 items / size 1
        Assert.Equal(5, meta.CurrentPage);
        Assert.Equal(1, meta.PageSize);
    }

    #endregion

    #region Type Preservation

    [Fact]
    public void ApplyPagination_PreservesQueryableType()
    {
        var query = GetTestData(10);
        var pagination = new PaginationParameters { Number = 1, Size = 5 };

        var result = query.ApplyPagination(pagination);

        Assert.IsAssignableFrom<IQueryable<TestEntity>>(result);
    }

    [Fact]
    public void ApplyPagination_IsChainable()
    {
        var query = GetTestData(20);
        var pagination = new PaginationParameters { Number = 1, Size = 5 };

        var result = query
            .Where(e => e.Id > 5)
            .ApplyPagination(pagination)
            .Where(e => e.Id < 15)
            .ToList();

        // After filtering Id > 5, we have items 6-20 (15 items)
        // Page 1, size 5 gives us items 6-10
        // Then filter Id < 15 keeps all of them
        Assert.Equal(5, result.Count);
        Assert.Equal(6, result[0].Id);
        Assert.Equal(10, result[4].Id);
    }

    #endregion

    #region Real World Scenarios

    [Fact]
    public async Task Scenario_TypicalApiPagination_WorksCorrectly()
    {
        // Simulate typical API usage: 100 items, page 5 of 10 per page
        var query = GetTestData(100);
        var pagination = new PaginationParameters { Number = 5, Size = 10 };

        var result = query.ApplyPagination(pagination).ToList();
        var meta = await query.CreatePaginationMetaAsync(pagination);

        Assert.Equal(10, result.Count);
        Assert.Equal(41, result[0].Id); // Page 5 starts at offset 40
        Assert.Equal(50, result[9].Id);
        Assert.Equal(100, meta.TotalResources);
        Assert.Equal(10, meta.TotalPages);
        Assert.Equal(5, meta.CurrentPage);
    }

    [Fact]
    public async Task Scenario_LastPagePartialFill_WorksCorrectly()
    {
        // 23 items, page 3 of 10 per page = 3 items on last page
        var query = GetTestData(23);
        var pagination = new PaginationParameters { Number = 3, Size = 10 };

        var result = query.ApplyPagination(pagination).ToList();
        var meta = await query.CreatePaginationMetaAsync(pagination);

        Assert.Equal(3, result.Count);
        Assert.Equal(21, result[0].Id);
        Assert.Equal(23, result[2].Id);
        Assert.Equal(23, meta.TotalResources);
        Assert.Equal(3, meta.TotalPages);
    }

    [Fact]
    public async Task Scenario_RequestPageBeyondData_GracefulDegradation()
    {
        // User requests page 10 but only 3 pages exist
        var query = GetTestData(25);
        var pagination = new PaginationParameters { Number = 10, Size = 10 };

        var result = query.ApplyPagination(pagination).ToList();
        var meta = await query.CreatePaginationMetaAsync(pagination);

        // Should get last page (page 3) with 5 items
        Assert.Equal(5, result.Count);
        Assert.Equal(21, result[0].Id);
        Assert.Equal(3, meta.CurrentPage);
        Assert.Equal(3, meta.TotalPages);
    }

    #endregion
}
