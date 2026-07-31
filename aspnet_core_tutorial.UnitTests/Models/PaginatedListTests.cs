using aspnet_core_tutorial.Models;
using aspnet_core_tutorial.UnitTests.TestSupport;
using Xunit;

namespace aspnet_core_tutorial.UnitTests.Models;

public class PaginatedListTests
{
    private static async Task<aspnet_core_tutorial.Data.ApplicationDbContext> SeededContextAsync(int categoryCount)
    {
        var context = InMemoryDbContextFactory.Create();
        var now = DateTime.UtcNow;
        for (var i = 0; i < categoryCount; i++)
        {
            context.Categories.Add(new Category { CategoryName = $"Category {i:D2}", CreatedAt = now, UpdatedAt = now });
        }
        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task CreateAsync_Returns_First_Page_With_Correct_Item_Count()
    {
        await using var context = await SeededContextAsync(25);

        var page = await PaginatedList<Category>.CreateAsync(context.Categories.OrderBy(c => c.CategoryName), pageIndex: 1, pageSize: 10);

        Assert.Equal(10, page.Count);
        Assert.Equal(1, page.PageIndex);
        Assert.Equal(3, page.TotalPages);
    }

    [Fact]
    public async Task CreateAsync_Clamps_Zero_PageIndex_To_First_Page()
    {
        await using var context = await SeededContextAsync(5);

        var page = await PaginatedList<Category>.CreateAsync(context.Categories.OrderBy(c => c.CategoryName), pageIndex: 0, pageSize: 10);

        Assert.Equal(1, page.PageIndex);
        Assert.Equal(5, page.Count);
    }

    [Fact]
    public async Task CreateAsync_Clamps_Negative_PageIndex_To_First_Page()
    {
        await using var context = await SeededContextAsync(5);

        var page = await PaginatedList<Category>.CreateAsync(context.Categories.OrderBy(c => c.CategoryName), pageIndex: -3, pageSize: 10);

        Assert.Equal(1, page.PageIndex);
        Assert.Equal(5, page.Count);
    }

    [Fact]
    public async Task CreateAsync_Reports_HasPreviousPage_And_HasNextPage_Correctly_On_Middle_Page()
    {
        await using var context = await SeededContextAsync(25);

        var page = await PaginatedList<Category>.CreateAsync(context.Categories.OrderBy(c => c.CategoryName), pageIndex: 2, pageSize: 10);

        Assert.True(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public async Task CreateAsync_Reports_No_Previous_Page_On_First_Page()
    {
        await using var context = await SeededContextAsync(25);

        var page = await PaginatedList<Category>.CreateAsync(context.Categories.OrderBy(c => c.CategoryName), pageIndex: 1, pageSize: 10);

        Assert.False(page.HasPreviousPage);
    }

    [Fact]
    public async Task CreateAsync_Reports_No_Next_Page_On_Last_Page()
    {
        await using var context = await SeededContextAsync(25);

        var page = await PaginatedList<Category>.CreateAsync(context.Categories.OrderBy(c => c.CategoryName), pageIndex: 3, pageSize: 10);

        Assert.False(page.HasNextPage);
        Assert.Equal(5, page.Count);
    }

    [Fact]
    public async Task CreateAsync_Returns_Empty_Page_When_Source_Is_Empty()
    {
        await using var context = await SeededContextAsync(0);

        var page = await PaginatedList<Category>.CreateAsync(context.Categories.OrderBy(c => c.CategoryName), pageIndex: 1, pageSize: 10);

        Assert.Empty(page);
        Assert.Equal(0, page.TotalPages);
        Assert.False(page.HasNextPage);
        Assert.False(page.HasPreviousPage);
    }
}
