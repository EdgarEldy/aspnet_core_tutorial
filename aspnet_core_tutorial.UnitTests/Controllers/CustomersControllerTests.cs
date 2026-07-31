using aspnet_core_tutorial.Controllers;
using aspnet_core_tutorial.Models;
using aspnet_core_tutorial.UnitTests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace aspnet_core_tutorial.UnitTests.Controllers;

public class CustomersControllerTests
{
    private static Customer NewCustomer(string firstName, string lastName, DateTime? createdAt = null)
    {
        var timestamp = createdAt ?? DateTime.UtcNow;
        return new Customer
        {
            FirstName = firstName,
            LastName = lastName,
            Telephone = "555-0000",
            Email = $"{firstName}.{lastName}@example.com".ToLowerInvariant(),
            Address = "1 Test Street",
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
    }

    [Fact]
    public async Task Index_Returns_All_Customers_When_No_Search_String()
    {
        await using var context = InMemoryDbContextFactory.Create();
        context.Customers.AddRange(NewCustomer("Edgar", "Eldy"), NewCustomer("John", "Travolta"));
        await context.SaveChangesAsync();
        var controller = new CustomersController(context);

        var result = await controller.Index(searchString: null);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<PaginatedList<Customer>>(viewResult.Model);
        Assert.Equal(2, model.Count);
    }

    [Fact]
    public async Task Index_Filters_Customers_By_First_Or_Last_Name()
    {
        await using var context = InMemoryDbContextFactory.Create();
        context.Customers.AddRange(NewCustomer("Edgar", "Eldy"), NewCustomer("John", "Travolta"));
        await context.SaveChangesAsync();
        var controller = new CustomersController(context);

        var result = await controller.Index(searchString: "Travolta");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<PaginatedList<Customer>>(viewResult.Model);
        var customer = Assert.Single(model);
        Assert.Equal("John", customer.FirstName);
    }

    [Fact]
    public async Task Index_Paginates_Results_According_To_Page_Number()
    {
        await using var context = InMemoryDbContextFactory.Create();
        for (var i = 0; i < 15; i++)
        {
            context.Customers.Add(NewCustomer("Customer", $"{i:D2}"));
        }
        await context.SaveChangesAsync();

        // A fresh controller instance per simulated request, matching how ASP.NET Core actually
        // instantiates controllers (one per HTTP request, never reused across requests).
        var firstPage = await new CustomersController(context).Index(searchString: null, pageNumber: 1);
        var secondPage = await new CustomersController(context).Index(searchString: null, pageNumber: 2);

        var firstModel = Assert.IsAssignableFrom<PaginatedList<Customer>>(Assert.IsType<ViewResult>(firstPage).Model);
        var secondModel = Assert.IsAssignableFrom<PaginatedList<Customer>>(Assert.IsType<ViewResult>(secondPage).Model);
        Assert.Equal(10, firstModel.Count);
        Assert.Equal(5, secondModel.Count);
        Assert.True(firstModel.HasNextPage);
        Assert.True(secondModel.HasPreviousPage);
    }

    [Fact]
    public async Task Create_Post_Adds_Customer_And_Stamps_Timestamps_When_ModelState_Is_Valid()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CustomersController(context);
        var customer = new Customer
        {
            FirstName = "Edgar",
            LastName = "Eldy",
            Telephone = "555-1234",
            Email = "edgar@example.com",
            Address = "1 Test Street"
        };
        var before = DateTime.UtcNow;

        var result = await controller.Create(customer);

        Assert.IsType<RedirectToActionResult>(result);
        var saved = Assert.Single(context.Customers);
        Assert.Equal("Edgar", saved.FirstName);
        Assert.True(saved.CreatedAt >= before);
        Assert.Equal(saved.CreatedAt, saved.UpdatedAt);
    }

    [Fact]
    public async Task Create_Post_Returns_View_With_Same_Model_When_ModelState_Is_Invalid()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CustomersController(context);
        controller.ModelState.AddModelError("FirstName", "Required");
        var customer = new Customer { FirstName = "" };

        var result = await controller.Create(customer);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(customer, viewResult.Model);
        Assert.Empty(context.Customers);
    }

    [Fact]
    public async Task Edit_Get_Returns_Customer_When_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        var controller = new CustomersController(context);

        var result = await controller.Edit(customer.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Customer>(viewResult.Model);
        Assert.Equal(customer.Id, model.Id);
    }

    [Fact]
    public async Task Edit_Get_Returns_404_When_Id_Is_Null()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CustomersController(context);

        var result = await controller.Edit(id: null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_Returns_404_When_Customer_Not_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CustomersController(context);

        var result = await controller.Edit(id: 999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_Returns_404_When_Route_Id_Does_Not_Match_Model_Id()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CustomersController(context);
        var customer = new Customer { Id = 2, FirstName = "Edgar" };

        var result = await controller.Edit(1, customer);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_Returns_404_When_Customer_No_Longer_Exists()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CustomersController(context);
        var customer = new Customer { Id = 42, FirstName = "Edgar" };

        var result = await controller.Edit(42, customer);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_Does_Not_Overwrite_CreatedAt_And_Updates_UpdatedAt()
    {
        // Locks in the CreatedAt-preservation fix: the POST action only binds a handful of
        // fields, so it must fetch the tracked entity and patch fields individually rather than
        // attaching the partially-bound model, which would otherwise reset CreatedAt to
        // default(DateTime).
        await using var context = InMemoryDbContextFactory.Create();
        var originalCreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var customer = NewCustomer("Edgar", "Eldy", originalCreatedAt);
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        var controller = new CustomersController(context);

        var incoming = new Customer
        {
            Id = customer.Id,
            FirstName = "Edgar",
            LastName = "Eldy Renamed",
            Telephone = "555-9999",
            Email = customer.Email,
            Address = customer.Address
        };
        var result = await controller.Edit(customer.Id, incoming);

        Assert.IsType<RedirectToActionResult>(result);
        var updated = await context.Customers.FindAsync(customer.Id);
        Assert.NotNull(updated);
        Assert.Equal(originalCreatedAt, updated!.CreatedAt);
        Assert.True(updated.UpdatedAt > originalCreatedAt);
        Assert.Equal("Eldy Renamed", updated.LastName);
        Assert.Equal("555-9999", updated.Telephone);
    }

    [Fact]
    public async Task Delete_Get_Returns_Customer_When_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        var controller = new CustomersController(context);

        var result = await controller.Delete(customer.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(customer.Id, Assert.IsType<Customer>(viewResult.Model).Id);
    }

    [Fact]
    public async Task Delete_Get_Returns_404_When_Customer_Not_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CustomersController(context);

        var result = await controller.Delete(id: 999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_Removes_Customer_And_Redirects()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        var controller = new CustomersController(context);

        var result = await controller.DeleteConfirmed(customer.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(context.Customers);
    }

    [Fact]
    public async Task DeleteConfirmed_Redirects_Without_Throwing_When_Customer_Already_Gone()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new CustomersController(context);

        var result = await controller.DeleteConfirmed(id: 999);

        Assert.IsType<RedirectToActionResult>(result);
    }
}
