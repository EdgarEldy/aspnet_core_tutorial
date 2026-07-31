using aspnet_core_tutorial.Controllers;
using aspnet_core_tutorial.Models;
using aspnet_core_tutorial.UnitTests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace aspnet_core_tutorial.UnitTests.Controllers;

public class CategoriesControllerTests
{
    private static Category NewCategory(string name, DateTime? createdAt = null)
    {
        var timestamp = createdAt ?? DateTime.UtcNow;
        return new Category
        {
            CategoryName = name,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
    }

    [Fact]
    public async Task Index_Returns_All_Categories_When_No_Search_String()
    {
        await using var context = InMemoryDbContextFactory.Create();
        context.Categories.AddRange(NewCategory("Lemonades"), NewCategory("Alcohols"));
        await context.SaveChangesAsync();
        var controller = new CategoriesController(context);

        var result = await controller.Index(searchString: null);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<PaginatedList<Category>>(viewResult.Model);
        Assert.Equal(2, model.Count);
    }

    [Fact]
    public async Task Index_Filters_Categories_By_Search_String()
    {
        await using var context = InMemoryDbContextFactory.Create();
        context.Categories.AddRange(NewCategory("Lemonades"), NewCategory("Alcohols"));
        await context.SaveChangesAsync();
        var controller = new CategoriesController(context);

        var result = await controller.Index(searchString: "Lem");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<PaginatedList<Category>>(viewResult.Model);
        var category = Assert.Single(model);
        Assert.Equal("Lemonades", category.CategoryName);
    }

    [Fact]
    public async Task Index_Paginates_Results_According_To_Page_Number()
    {
        await using var context = InMemoryDbContextFactory.Create();
        for (var i = 0; i < 15; i++)
        {
            context.Categories.Add(NewCategory($"Category {i:D2}"));
        }
        await context.SaveChangesAsync();

        // A fresh controller instance per simulated request, matching how ASP.NET Core actually
        // instantiates controllers (one per HTTP request, never reused across requests).
        var firstPage = await new CategoriesController(context).Index(searchString: null, pageNumber: 1);
        var secondPage = await new CategoriesController(context).Index(searchString: null, pageNumber: 2);

        var firstModel = Assert.IsAssignableFrom<PaginatedList<Category>>(Assert.IsType<ViewResult>(firstPage).Model);
        var secondModel = Assert.IsAssignableFrom<PaginatedList<Category>>(Assert.IsType<ViewResult>(secondPage).Model);
        Assert.Equal(10, firstModel.Count);
        Assert.Equal(5, secondModel.Count);
        Assert.True(firstModel.HasNextPage);
        Assert.True(secondModel.HasPreviousPage);
    }

    [Fact]
    public async Task Create_Post_Adds_Category_And_Stamps_Timestamps_When_ModelState_Is_Valid()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CategoriesController(context);
        var category = new Category { CategoryName = "Sodas" };
        var before = DateTime.UtcNow;

        var result = await controller.Create(category);

        Assert.IsType<RedirectToActionResult>(result);
        var saved = Assert.Single(context.Categories);
        Assert.Equal("Sodas", saved.CategoryName);
        Assert.True(saved.CreatedAt >= before);
        Assert.Equal(saved.CreatedAt, saved.UpdatedAt);
    }

    [Fact]
    public async Task Create_Post_Returns_View_With_Same_Model_When_ModelState_Is_Invalid()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CategoriesController(context);
        controller.ModelState.AddModelError("CategoryName", "Required");
        var category = new Category { CategoryName = "" };

        var result = await controller.Create(category);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(category, viewResult.Model);
        Assert.Empty(context.Categories);
    }

    [Fact]
    public async Task Edit_Get_Returns_Category_When_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var category = NewCategory("Lemonades");
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        var controller = new CategoriesController(context);

        var result = await controller.Edit(category.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Category>(viewResult.Model);
        Assert.Equal(category.Id, model.Id);
    }

    [Fact]
    public async Task Edit_Get_Returns_404_When_Id_Is_Null()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CategoriesController(context);

        var result = await controller.Edit(id: null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_Returns_404_When_Category_Not_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CategoriesController(context);

        var result = await controller.Edit(id: 999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_Returns_404_When_Route_Id_Does_Not_Match_Model_Id()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CategoriesController(context);
        var category = new Category { Id = 2, CategoryName = "Sodas" };

        var result = await controller.Edit(1, category);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_Returns_404_When_Category_No_Longer_Exists()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CategoriesController(context);
        var category = new Category { Id = 42, CategoryName = "Sodas" };

        var result = await controller.Edit(42, category);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_Does_Not_Overwrite_CreatedAt_And_Updates_UpdatedAt()
    {
        // Locks in the CreatedAt-preservation fix: the POST action only binds Id and
        // CategoryName, so it must fetch the tracked entity and patch fields individually
        // rather than attaching the partially-bound model, which would otherwise reset
        // CreatedAt to default(DateTime).
        await using var context = InMemoryDbContextFactory.Create();
        var originalCreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var category = new Category
        {
            CategoryName = "Lemonades",
            CreatedAt = originalCreatedAt,
            UpdatedAt = originalCreatedAt
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        var controller = new CategoriesController(context);

        var incoming = new Category { Id = category.Id, CategoryName = "Lemonades Renamed" };
        var result = await controller.Edit(category.Id, incoming);

        Assert.IsType<RedirectToActionResult>(result);
        var updated = await context.Categories.FindAsync(category.Id);
        Assert.NotNull(updated);
        Assert.Equal(originalCreatedAt, updated!.CreatedAt);
        Assert.True(updated.UpdatedAt > originalCreatedAt);
        Assert.Equal("Lemonades Renamed", updated.CategoryName);
    }

    [Fact]
    public async Task Delete_Get_Returns_Category_When_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var category = NewCategory("Lemonades");
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        var controller = new CategoriesController(context);

        var result = await controller.Delete(category.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(category.Id, Assert.IsType<Category>(viewResult.Model).Id);
    }

    [Fact]
    public async Task Delete_Get_Returns_404_When_Category_Not_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CategoriesController(context);

        var result = await controller.Delete(id: 999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_Removes_Category_And_Redirects()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var category = NewCategory("Lemonades");
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        var controller = new CategoriesController(context);

        var result = await controller.DeleteConfirmed(category.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(context.Categories);
    }

    [Fact]
    public async Task DeleteConfirmed_Redirects_Without_Throwing_When_Category_Already_Gone()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CategoriesController(context);

        var result = await controller.DeleteConfirmed(id: 999);

        Assert.IsType<RedirectToActionResult>(result);
    }
}
