using System.Collections;
using aspnet_core_tutorial.Controllers;
using aspnet_core_tutorial.Models;
using aspnet_core_tutorial.UnitTests.TestSupport;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Xunit;

namespace aspnet_core_tutorial.UnitTests.Controllers;

public class OrdersControllerTests
{
    private static Category NewCategory(string name)
    {
        var now = DateTime.UtcNow;
        return new Category { CategoryName = name, CreatedAt = now, UpdatedAt = now };
    }

    private static Customer NewCustomer(string firstName, string lastName)
    {
        var now = DateTime.UtcNow;
        return new Customer
        {
            FirstName = firstName,
            LastName = lastName,
            Telephone = "555-0000",
            Email = $"{firstName}.{lastName}@example.com".ToLowerInvariant(),
            Address = "1 Test Street",
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static Product NewProduct(string name, double unitPrice, int? categoryId = null)
    {
        var now = DateTime.UtcNow;
        return new Product
        {
            ProductName = name,
            CategoryId = categoryId,
            UnitPrice = unitPrice,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static Order NewOrder(int? customerId, int? productId, int quantity, double total = 0, DateTime? createdAt = null)
    {
        var timestamp = createdAt ?? DateTime.UtcNow;
        return new Order
        {
            CustomerId = customerId,
            ProductId = productId,
            Quantity = quantity,
            Total = total,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };
    }

    [Fact]
    public async Task Index_Returns_All_Orders_When_No_Search_String()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        var product = NewProduct("Citron", 800);
        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        context.Orders.AddRange(
            NewOrder(customer.Id, product.Id, 2, 1600),
            NewOrder(customer.Id, product.Id, 1, 800));
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);

        var result = await controller.Index(searchString: null);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<PaginatedList<Order>>(viewResult.Model);
        Assert.Equal(2, model.Count);
    }

    [Fact]
    public async Task Index_Filters_Orders_By_Customer_First_Or_Last_Name()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var edgar = NewCustomer("Edgar", "Eldy");
        var john = NewCustomer("John", "Travolta");
        var product = NewProduct("Citron", 800);
        context.Customers.AddRange(edgar, john);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        context.Orders.AddRange(
            NewOrder(edgar.Id, product.Id, 1, 800),
            NewOrder(john.Id, product.Id, 1, 800));
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);

        var result = await controller.Index(searchString: "Travolta");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<PaginatedList<Order>>(viewResult.Model);
        var order = Assert.Single(model);
        Assert.Equal(john.Id, order.CustomerId);
    }

    [Fact]
    public async Task Index_Filters_Orders_By_Product_Name()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        var citron = NewProduct("Citron", 800);
        var amstel = NewProduct("Amstel", 2500);
        context.Customers.Add(customer);
        context.Products.AddRange(citron, amstel);
        await context.SaveChangesAsync();
        context.Orders.AddRange(
            NewOrder(customer.Id, citron.Id, 1, 800),
            NewOrder(customer.Id, amstel.Id, 1, 2500));
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);

        var result = await controller.Index(searchString: "Amstel");

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<PaginatedList<Order>>(viewResult.Model);
        var order = Assert.Single(model);
        Assert.Equal(amstel.Id, order.ProductId);
    }

    [Fact]
    public async Task Index_Paginates_Results_According_To_Page_Number()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        var product = NewProduct("Citron", 800);
        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        for (var i = 0; i < 15; i++)
        {
            context.Orders.Add(NewOrder(customer.Id, product.Id, 1, 800));
        }
        await context.SaveChangesAsync();

        // A fresh controller instance per simulated request, matching how ASP.NET Core actually
        // instantiates controllers (one per HTTP request, never reused across requests).
        var firstPage = await new OrdersController(context).Index(searchString: null, pageNumber: 1);
        var secondPage = await new OrdersController(context).Index(searchString: null, pageNumber: 2);

        var firstModel = Assert.IsAssignableFrom<PaginatedList<Order>>(Assert.IsType<ViewResult>(firstPage).Model);
        var secondModel = Assert.IsAssignableFrom<PaginatedList<Order>>(Assert.IsType<ViewResult>(secondPage).Model);
        Assert.Equal(10, firstModel.Count);
        Assert.Equal(5, secondModel.Count);
        Assert.True(firstModel.HasNextPage);
        Assert.True(secondModel.HasPreviousPage);
    }

    [Fact]
    public async Task GetProducts_Returns_All_Products_When_No_CategoryId_Given()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var lemonades = NewCategory("Lemonades");
        var beers = NewCategory("Beers");
        context.Categories.AddRange(lemonades, beers);
        await context.SaveChangesAsync();
        context.Products.AddRange(
            NewProduct("Citron", 800, lemonades.Id),
            NewProduct("Amstel", 2500, beers.Id));
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);

        var result = await controller.GetProducts(categoryId: null);

        var products = Assert.IsAssignableFrom<IEnumerable>(result.Value).Cast<object>().ToList();
        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task GetProducts_Filters_Products_By_CategoryId()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var lemonades = NewCategory("Lemonades");
        var beers = NewCategory("Beers");
        context.Categories.AddRange(lemonades, beers);
        await context.SaveChangesAsync();
        context.Products.AddRange(
            NewProduct("Citron", 800, lemonades.Id),
            NewProduct("Amstel", 2500, beers.Id));
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);

        var result = await controller.GetProducts(categoryId: beers.Id);

        var products = Assert.IsAssignableFrom<IEnumerable>(result.Value).Cast<object>().ToList();
        var product = Assert.Single(products);
        var productType = product.GetType();
        Assert.Equal("Amstel", productType.GetProperty("productName")!.GetValue(product));
    }

    [Fact]
    public async Task Create_Get_Populates_Customer_And_Product_Dropdowns()
    {
        await using var context = InMemoryDbContextFactory.Create();
        context.Customers.Add(NewCustomer("Edgar", "Eldy"));
        context.Products.Add(NewProduct("Citron", 800));
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);

        var result = await controller.Create();

        Assert.IsType<ViewResult>(result);
        var customerSelectList = Assert.IsAssignableFrom<SelectList>(controller.ViewData["CustomerId"]);
        var productSelectList = Assert.IsAssignableFrom<SelectList>(controller.ViewData["ProductId"]);
        Assert.Single(customerSelectList);
        Assert.Single(productSelectList);
    }

    [Fact]
    public async Task Create_Post_Adds_Order_Computes_Total_And_Stamps_Timestamps_When_Valid()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        var product = NewProduct("Citron", 800);
        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);
        var order = new Order { CustomerId = customer.Id, ProductId = product.Id, Quantity = 3 };
        var before = DateTime.UtcNow;

        var result = await controller.Create(order);

        Assert.IsType<RedirectToActionResult>(result);
        var saved = Assert.Single(context.Orders);
        Assert.Equal(3, saved.Quantity);
        Assert.Equal(2400, saved.Total);
        Assert.True(saved.CreatedAt >= before);
        Assert.Equal(saved.CreatedAt, saved.UpdatedAt);
    }

    [Fact]
    public async Task Create_Post_Adds_ModelState_Error_When_CustomerId_Is_Null()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var product = NewProduct("Citron", 800);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);
        var order = new Order { CustomerId = null, ProductId = product.Id, Quantity = 1 };

        var result = await controller.Create(order);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState[nameof(Order.CustomerId)]!.Errors,
            e => e.ErrorMessage == "Please select a customer.");
        Assert.Empty(context.Orders);
    }

    [Fact]
    public async Task Create_Post_Adds_ModelState_Error_When_CustomerId_Does_Not_Exist()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var product = NewProduct("Citron", 800);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);
        var order = new Order { CustomerId = 999, ProductId = product.Id, Quantity = 1 };

        var result = await controller.Create(order);

        Assert.IsType<ViewResult>(result);
        Assert.Contains(controller.ModelState[nameof(Order.CustomerId)]!.Errors,
            e => e.ErrorMessage == "Selected customer no longer exists.");
        Assert.Empty(context.Orders);
    }

    [Fact]
    public async Task Create_Post_Adds_ModelState_Error_When_ProductId_Is_Null()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);
        var order = new Order { CustomerId = customer.Id, ProductId = null, Quantity = 1 };

        var result = await controller.Create(order);

        Assert.IsType<ViewResult>(result);
        Assert.Contains(controller.ModelState[nameof(Order.ProductId)]!.Errors,
            e => e.ErrorMessage == "Please select a product.");
        Assert.Empty(context.Orders);
    }

    [Fact]
    public async Task Create_Post_Adds_ModelState_Error_When_ProductId_Does_Not_Exist()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);
        var order = new Order { CustomerId = customer.Id, ProductId = 999, Quantity = 1 };

        var result = await controller.Create(order);

        Assert.IsType<ViewResult>(result);
        Assert.Contains(controller.ModelState[nameof(Order.ProductId)]!.Errors,
            e => e.ErrorMessage == "Selected product no longer exists.");
        Assert.Empty(context.Orders);
    }

    [Fact]
    public async Task Create_Post_Adds_ModelState_Error_When_Quantity_Is_Not_Positive()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        var product = NewProduct("Citron", 800);
        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);
        var order = new Order { CustomerId = customer.Id, ProductId = product.Id, Quantity = 0 };

        var result = await controller.Create(order);

        Assert.IsType<ViewResult>(result);
        Assert.Contains(controller.ModelState[nameof(Order.Quantity)]!.Errors,
            e => e.ErrorMessage == "Quantity must be greater than zero.");
        Assert.Empty(context.Orders);
    }

    [Fact]
    public async Task Create_Post_Returns_View_With_Same_Model_And_Repopulates_Dropdowns_When_Invalid()
    {
        await using var context = InMemoryDbContextFactory.Create();
        context.Customers.Add(NewCustomer("Edgar", "Eldy"));
        context.Products.Add(NewProduct("Citron", 800));
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);
        var order = new Order { CustomerId = null, ProductId = null, Quantity = 0 };

        var result = await controller.Create(order);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(order, viewResult.Model);
        Assert.IsAssignableFrom<SelectList>(controller.ViewData["CustomerId"]);
        Assert.IsAssignableFrom<SelectList>(controller.ViewData["ProductId"]);
    }

    [Fact]
    public async Task Edit_Get_Returns_Order_When_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        var product = NewProduct("Citron", 800);
        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var order = NewOrder(customer.Id, product.Id, 2, 1600);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);

        var result = await controller.Edit(order.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Order>(viewResult.Model);
        Assert.Equal(order.Id, model.Id);
        Assert.IsAssignableFrom<SelectList>(controller.ViewData["CustomerId"]);
        Assert.IsAssignableFrom<SelectList>(controller.ViewData["ProductId"]);
    }

    [Fact]
    public async Task Edit_Get_Returns_404_When_Id_Is_Null()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new OrdersController(context);

        var result = await controller.Edit(id: null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_Returns_404_When_Order_Not_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new OrdersController(context);

        var result = await controller.Edit(id: 999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_Returns_404_When_Route_Id_Does_Not_Match_Model_Id()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new OrdersController(context);
        var order = new Order { Id = 2, CustomerId = 1, ProductId = 1, Quantity = 1 };

        var result = await controller.Edit(1, order);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_Returns_404_When_Order_No_Longer_Exists()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        var product = NewProduct("Citron", 800);
        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);
        var order = new Order { Id = 42, CustomerId = customer.Id, ProductId = product.Id, Quantity = 1 };

        var result = await controller.Edit(42, order);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_Does_Not_Overwrite_CreatedAt_And_Updates_UpdatedAt()
    {
        // Locks in the CreatedAt-preservation fix, mirrored from Customers/Products: the POST
        // action only binds a handful of fields, so it must fetch the tracked entity and patch
        // fields individually rather than attaching the partially-bound model, which would
        // otherwise reset CreatedAt to default(DateTime).
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        var product = NewProduct("Citron", 800);
        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var originalCreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var order = NewOrder(customer.Id, product.Id, 2, 1600, originalCreatedAt);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);

        var incoming = new Order { Id = order.Id, CustomerId = customer.Id, ProductId = product.Id, Quantity = 5 };
        var result = await controller.Edit(order.Id, incoming);

        Assert.IsType<RedirectToActionResult>(result);
        var updated = await context.Orders.FindAsync(order.Id);
        Assert.NotNull(updated);
        Assert.Equal(originalCreatedAt, updated!.CreatedAt);
        Assert.True(updated.UpdatedAt > originalCreatedAt);
        Assert.Equal(5, updated.Quantity);
        Assert.Equal(4000, updated.Total);
    }

    [Fact]
    public async Task Edit_Post_Recomputes_Total_From_Current_Product_Price_When_Product_Changes()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        var citron = NewProduct("Citron", 800);
        var amstel = NewProduct("Amstel", 2500);
        context.Customers.Add(customer);
        context.Products.AddRange(citron, amstel);
        await context.SaveChangesAsync();
        var order = NewOrder(customer.Id, citron.Id, 2, 1600);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);

        var incoming = new Order { Id = order.Id, CustomerId = customer.Id, ProductId = amstel.Id, Quantity = 2 };
        var result = await controller.Edit(order.Id, incoming);

        Assert.IsType<RedirectToActionResult>(result);
        var updated = await context.Orders.FindAsync(order.Id);
        Assert.NotNull(updated);
        Assert.Equal(amstel.Id, updated!.ProductId);
        Assert.Equal(5000, updated.Total);
    }

    [Fact]
    public async Task Edit_Post_Returns_View_With_Same_Model_When_Invalid()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        var product = NewProduct("Citron", 800);
        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var order = NewOrder(customer.Id, product.Id, 2, 1600);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);

        var incoming = new Order { Id = order.Id, CustomerId = customer.Id, ProductId = product.Id, Quantity = 0 };
        var result = await controller.Edit(order.Id, incoming);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Same(incoming, viewResult.Model);
        Assert.IsAssignableFrom<SelectList>(controller.ViewData["CustomerId"]);
        Assert.IsAssignableFrom<SelectList>(controller.ViewData["ProductId"]);
        var unchanged = await context.Orders.FindAsync(order.Id);
        Assert.Equal(2, unchanged!.Quantity);
    }

    [Fact]
    public async Task Delete_Get_Returns_Order_When_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        var product = NewProduct("Citron", 800);
        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var order = NewOrder(customer.Id, product.Id, 2, 1600);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);

        var result = await controller.Delete(order.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<Order>(viewResult.Model);
        Assert.Equal(order.Id, model.Id);
        Assert.NotNull(model.Customer);
        Assert.NotNull(model.Product);
    }

    [Fact]
    public async Task Delete_Get_Returns_404_When_Id_Is_Null()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new OrdersController(context);

        var result = await controller.Delete(id: null);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Get_Returns_404_When_Order_Not_Found()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new OrdersController(context);

        var result = await controller.Delete(id: 999);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_Removes_Order_And_Redirects()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var customer = NewCustomer("Edgar", "Eldy");
        var product = NewProduct("Citron", 800);
        context.Customers.Add(customer);
        context.Products.Add(product);
        await context.SaveChangesAsync();
        var order = NewOrder(customer.Id, product.Id, 2, 1600);
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        var controller = new OrdersController(context);

        var result = await controller.DeleteConfirmed(order.Id);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(context.Orders);
    }

    [Fact]
    public async Task DeleteConfirmed_Redirects_Without_Throwing_When_Order_Already_Gone()
    {
        await using var context = InMemoryDbContextFactory.Create();
        var controller = new OrdersController(context);

        var result = await controller.DeleteConfirmed(id: 999);

        Assert.IsType<RedirectToActionResult>(result);
    }
}
