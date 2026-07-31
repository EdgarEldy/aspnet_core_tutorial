using aspnet_core_tutorial.Controllers;
using aspnet_core_tutorial.Models;
using aspnet_core_tutorial.UnitTests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Xunit;

namespace aspnet_core_tutorial.UnitTests.Controllers;

public class ProductsControllerTests
{
    private static Category NewCategory(string name)
    {
        var now = DateTime.UtcNow;
        return new Category { CategoryName = name, CreatedAt = now, UpdatedAt = now };
    }

    private static Product NewProduct(string name, int? categoryId, double unitPrice = 100, DateTime? createdAt = null)
    {
        var timestamp = createdAt ?? DateTime.UtcNow;
        return new Product
        {
            ProductName = name,
            CategoryId = categoryId,
            UnitPrice = unitPrice,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
    }

    [Fact]
    public async Task Index_Returns_All_Products_When_No_Search_String()
    {
        await using var context = InMemoryDbContextFactory.Create();
        context.Products.AddRange(NewProduct("Citron", null), NewProduct("Amstel", null));
        await context.SaveChangesAsync();
        var controller = new ProductsController(context);

        var result = await controller.Index(searchString: null);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<PaginatedList<Product>>(viewResult.Model);
        Assert.Equal(2, model.Count);
    }

    [Fact]
    public async Task Index_Filters_Products_By_Search_String()
    {
        await using var context = InMemoryDbContextFactory.Create();
        context.Products.AddRange(NewProduct("Citron", null), NewProduct("Amstel", null));
        await context.SaveChangesAsync();
        var controller = new ProductsController(context);

        var result = await controller.Index(searchString: "Cit");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<PaginatedList<Product>>(viewResult.Model);
        var product = Assert.Single(model);
        Assert.Equal("Citron", product.ProductName);
    }

    [Fact]
    public async Task Index_Paginates_Results_According_To_Page_Number()
    {
        await using var context = InMemoryDbContextFactory.Create();
        for (var i = 0; i < 15; i++)
        {
            context.Products.Add(NewProduct($"Product {i:D2}", null));
        }
        await context.SaveChangesAsync();

        // A fresh controller instance per simulated request, matching how ASP.NET Core actually
        // instantiates controllers (one per HTTP request, never reused across requests).
        var firstPage = await new ProductsController(context).Index(searchString: null, pageNumber: 1);
        var secondPage = await new ProductsController(context).Index(searchString: null, pageNumber: 2);

        var firstModel = Assert.IsAssignableFrom<PaginatedList<Product>>(Assert.IsType<ViewResult>(firstPage).Model);
        var secondModel = Assert.IsAssignableFrom<PaginatedList<Product>>(Assert.IsType<ViewResult>(secondPage).Model);
        Assert.Equal(10, firstModel.Count);
        Assert.Equal(5, secondModel.Count);
    }

    [Fact]
    public async Task Create_Get_Populates_Category_Dropdown()
    {
        await using var context = InMemoryDbContextFactory.Create();
        context.Categories.Add(NewCategory("Lemonades"));
        await context.SaveChangesAsync();
        var controller = new ProductsController(context);

        var result = await controller.Create();

        Assert.IsType<ViewResult>(result);
        var selectList = Assert.IsAssignableFrom<SelectList>(controller.ViewData["CategoryId"]);
        Assert.Single(selectList);
    }

    [Fact]
    public async Task Create_Post_Adds_Product_And_Stamps_Timestamps_When_ModelState_Is_Valid()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new ProductsController(context);
        var product = new Product { ProductName = "Citron", CategoryId = null, UnitPrice = 800 };
        var before = DateTime.UtcNow;

        var result = await controller.Create(product);

        Assert.IsType<RedirectToActionResult>(result);
        var saved = Assert.Single(context.Products);
        Assert.Equal("Citron", saved.ProductName);
        Assert.True(saved.CreatedAt >= before);
        Assert.Equal(saved.CreatedAt, saved.UpdatedAt);
    }

    [Fact]
    public async Task Create_Post_Returns_View_With_Category_Dropdown_When_ModelState_Is_Invalid()
    {
        await using var context = InMemoryDbContextFactory.Create();
        context.Categories.Add(NewCategory("Lemonades"));
        await context.SaveChangesAsync();
        var controller = new ProductsController(context);
        controller.ModelState.AddModelError("ProductName", "Required");
        var product = new Product { ProductName = "" };

        var result = await controller.Create(product);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(product, viewResult.Model);
        Assert.IsAssignableFrom<SelectList>(controller.ViewData["CategoryId"]);
        Assert.Empty(context.Products);
    }

    [Fact]
    public async Task Edit_Get_Returns_Product_When_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var product = NewProduct("Citron", null);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var controller = new ProductsController(context);

        var result = await controller.Edit(product.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(product.Id, Assert.IsType<Product>(viewResult.Model).Id);
    }

    [Fact]
    public async Task Edit_Get_Returns_404_When_Product_Not_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new ProductsController(context);

        var result = await controller.Edit(id: 999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_Returns_404_When_Route_Id_Does_Not_Match_Model_Id()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new ProductsController(context);
        var product = new Product { Id = 2, ProductName = "Citron" };

        var result = await controller.Edit(1, product);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_Does_Not_Overwrite_CreatedAt_And_Updates_UpdatedAt()
    {
        // Locks in the CreatedAt-preservation fix mirrored from CategoriesController.
        await using var context = InMemoryDbContextFactory.Create();
        var originalCreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var product = new Product
        {
            ProductName = "Citron",
            CategoryId = null,
            UnitPrice = 800,
            CreatedAt = originalCreatedAt,
            UpdatedAt = originalCreatedAt
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var controller = new ProductsController(context);

        var incoming = new Product { Id = product.Id, ProductName = "Citron Vert", UnitPrice = 900, CategoryId = null };
        var result = await controller.Edit(product.Id, incoming);

        Assert.IsType<RedirectToActionResult>(result);
        var updated = await context.Products.FindAsync(product.Id);
        Assert.NotNull(updated);
        Assert.Equal(originalCreatedAt, updated!.CreatedAt);
        Assert.True(updated.UpdatedAt > originalCreatedAt);
        Assert.Equal("Citron Vert", updated.ProductName);
        Assert.Equal(900, updated.UnitPrice);
    }

    [Fact]
    public async Task Delete_Get_Returns_Product_When_Found_And_Has_Category()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var category = NewCategory("Lemonades");
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        var product = NewProduct("Citron", category.Id);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var controller = new ProductsController(context);

        var result = await controller.Delete(product.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Product>(viewResult.Model);
        Assert.NotNull(model.Category);
    }

    [Fact]
    public async Task Delete_Get_Does_Not_Throw_When_Product_Has_No_Category()
    {
        // Locks in the nullable-FK fix: a Product with CategoryId == null must round-trip
        // through Delete (Include(p => p.Category) + the view's null-conditional access)
        // without throwing a NullReferenceException.
        await using var context = InMemoryDbContextFactory.Create();
        var product = NewProduct("Orphan Product", categoryId: null);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var controller = new ProductsController(context);

        var result = await controller.Delete(product.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Product>(viewResult.Model);
        Assert.Null(model.Category);
        Assert.Null(model.CategoryId);
    }

    [Fact]
    public async Task Delete_Get_Returns_404_When_Product_Not_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new ProductsController(context);

        var result = await controller.Delete(id: 999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_Removes_Product_And_Redirects()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var product = NewProduct("Citron", null);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var controller = new ProductsController(context);

        var result = await controller.DeleteConfirmed(product.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(context.Products);
    }
}
