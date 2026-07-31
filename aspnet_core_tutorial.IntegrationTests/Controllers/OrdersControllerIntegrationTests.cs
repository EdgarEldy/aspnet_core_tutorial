using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using aspnet_core_tutorial.Data;
using aspnet_core_tutorial.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace aspnet_core_tutorial.IntegrationTests.Controllers;

/// <summary>
/// Full HTTP round-trip coverage of <c>OrdersController</c>: real GET/POST requests against a
/// real ASP.NET Core host and a real PostgreSQL Testcontainer, including the
/// `[ValidateAntiForgeryToken]`-protected POST actions (Create/Edit/Delete), the application-layer
/// validation performed by <c>ValidateOrderAsync</c>, server-side <c>Total</c> computation, search
/// and pagination.
/// </summary>
/// <remarks>
/// Created by edgar.muhamyangabo on 7/31/26
/// Author : edgar.muhamyangabo
/// Date : 7/31/26
/// Project : aspnet_core_tutorial
/// </remarks>
[Collection(IntegrationTestCollection.Name)]
public class OrdersControllerIntegrationTests
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public OrdersControllerIntegrationTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_Returns_200_And_Contains_Seeded_Order_Customer()
    {
        using var client = _factory.CreateTestClient();

        var response = await client.GetAsync("/Orders");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Seeded by OrderSeeder/CustomerSeeder in Program.cs, applied once against the
        // Testcontainer at host startup.
        Assert.Contains("Eldy", body);
    }

    [Fact]
    public async Task Create_Get_Returns_Form_With_Antiforgery_Token_And_Customer_And_Product_Dropdowns()
    {
        using var client = _factory.CreateTestClient();

        var response = await client.GetAsync("/Orders/Create");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(AntiforgeryHtmlHelper.ExtractAntiforgeryToken(body));
        Assert.Contains("<select", body);
    }

    [Fact]
    public async Task Create_Post_With_Valid_Token_Creates_Order_Computes_Total_And_Redirects_To_Index()
    {
        using var client = _factory.CreateTestClient();
        var customerId = await CreateCustomerAsync(client);
        var (productId, productName) = await CreateProductAsync(client, unitPrice: "100");

        var response = await CreateOrderAsync(client, customerId, productId, quantity: "4");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Orders", response.Headers.Location?.OriginalString);

        var indexResponse = await client.GetAsync($"/Orders?searchString={productName}");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        Assert.Contains(productName, indexBody);
        // Total = Quantity * UnitPrice = 4 * 100, computed server-side and never trusted from the
        // client since there is no Total field in the Create action's [Bind] list.
        Assert.Contains("400", indexBody);
    }

    [Fact]
    public async Task Create_Post_Without_Antiforgery_Token_Returns_400()
    {
        using var client = _factory.CreateTestClient();
        var customerId = await CreateCustomerAsync(client);
        var (productId, _) = await CreateProductAsync(client, unitPrice: "100");

        var response = await client.PostAsync("/Orders/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CustomerId"] = customerId.ToString(),
            ["ProductId"] = productId.ToString(),
            ["Quantity"] = "1"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Post_Without_Customer_Returns_200_With_Validation_Error_And_Does_Not_Create_Order()
    {
        using var client = _factory.CreateTestClient();
        var (productId, _) = await CreateProductAsync(client, unitPrice: "100");

        var getResponse = await client.GetAsync("/Orders/Create");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(getBody);

        var response = await client.PostAsync("/Orders/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["ProductId"] = productId.ToString(),
            ["Quantity"] = "1",
            ["__RequestVerificationToken"] = token
        }));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Please select a customer.", body);

        // Verifies against the real, persisted rows in the Testcontainer's Postgres database
        // rather than searching the Index page: the search box echoes ViewData["CurrentFilter"]
        // back into its own value attribute, so a search for the product name would always
        // "contain" it regardless of whether an order referencing it was actually created.
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Orders.AsNoTracking().AnyAsync(o => o.ProductId == productId));
    }

    [Fact]
    public async Task Create_Post_Without_Product_Returns_200_With_Validation_Error()
    {
        using var client = _factory.CreateTestClient();
        var customerId = await CreateCustomerAsync(client);

        var getResponse = await client.GetAsync("/Orders/Create");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(getBody);

        var response = await client.PostAsync("/Orders/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CustomerId"] = customerId.ToString(),
            ["Quantity"] = "1",
            ["__RequestVerificationToken"] = token
        }));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Please select a product.", body);
    }

    [Fact]
    public async Task Create_Post_With_Zero_Quantity_Returns_200_With_Validation_Error()
    {
        using var client = _factory.CreateTestClient();
        var customerId = await CreateCustomerAsync(client);
        var (productId, _) = await CreateProductAsync(client, unitPrice: "100");

        var getResponse = await client.GetAsync("/Orders/Create");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(getBody);

        var response = await client.PostAsync("/Orders/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CustomerId"] = customerId.ToString(),
            ["ProductId"] = productId.ToString(),
            ["Quantity"] = "0",
            ["__RequestVerificationToken"] = token
        }));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Quantity must be greater than zero.", body);
    }

    [Fact]
    public async Task Edit_Get_Returns_200_For_Existing_Order()
    {
        using var client = _factory.CreateTestClient();
        var customerId = await CreateCustomerAsync(client);
        var (productId, productName) = await CreateProductAsync(client, unitPrice: "100");
        await CreateOrderAsync(client, customerId, productId, quantity: "2");
        var id = await FindOrderIdAsync(client, productName);

        var response = await client.GetAsync($"/Orders/Edit/{id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(productName, body);
    }

    [Fact]
    public async Task Edit_Get_Returns_404_When_Order_Not_Found()
    {
        using var client = _factory.CreateTestClient();

        var response = await client.GetAsync("/Orders/Edit/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Edit_Post_Updates_Order_Recomputes_Total_And_Redirects_To_Index()
    {
        using var client = _factory.CreateTestClient();
        var customerId = await CreateCustomerAsync(client);
        var (productId, productName) = await CreateProductAsync(client, unitPrice: "100");
        await CreateOrderAsync(client, customerId, productId, quantity: "2");
        var id = await FindOrderIdAsync(client, productName);

        var editGetResponse = await client.GetAsync($"/Orders/Edit/{id}");
        var editGetBody = await editGetResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(editGetBody);

        var editPostResponse = await client.PostAsync($"/Orders/Edit/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = id.ToString(),
            ["CustomerId"] = customerId.ToString(),
            ["ProductId"] = productId.ToString(),
            ["Quantity"] = "5",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, editPostResponse.StatusCode);

        var indexResponse = await client.GetAsync($"/Orders?searchString={productName}");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        // Total = Quantity * UnitPrice = 5 * 100, recomputed server-side on edit.
        Assert.Contains("500", indexBody);
    }

    [Fact]
    public async Task Edit_Post_Does_Not_Overwrite_CreatedAt_And_Refreshes_UpdatedAt()
    {
        // Regression test mirroring the same fix already locked in for Categories/Products/
        // Customers: Edit fetches the tracked entity and patches fields individually instead of
        // attaching the partially-bound model, so CreatedAt survives an edit while UpdatedAt moves
        // forward. Asserts the real, persisted row in the Testcontainer's Postgres database.
        using var client = _factory.CreateTestClient();
        var customerId = await CreateCustomerAsync(client);
        var (productId, productName) = await CreateProductAsync(client, unitPrice: "100");
        await CreateOrderAsync(client, customerId, productId, quantity: "2");
        var id = await FindOrderIdAsync(client, productName);

        DateTime createdAtBeforeEdit;
        DateTime updatedAtBeforeEdit;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var orderBeforeEdit = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == id);
            createdAtBeforeEdit = orderBeforeEdit.CreatedAt;
            updatedAtBeforeEdit = orderBeforeEdit.UpdatedAt;
        }
        Assert.NotEqual(default, createdAtBeforeEdit);

        var editGetResponse = await client.GetAsync($"/Orders/Edit/{id}");
        var editGetBody = await editGetResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(editGetBody);

        var editPostResponse = await client.PostAsync($"/Orders/Edit/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = id.ToString(),
            ["CustomerId"] = customerId.ToString(),
            ["ProductId"] = productId.ToString(),
            ["Quantity"] = "9",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, editPostResponse.StatusCode);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var orderAfterEdit = await verificationDb.Orders.AsNoTracking().SingleAsync(o => o.Id == id);

        Assert.Equal(createdAtBeforeEdit, orderAfterEdit.CreatedAt);
        Assert.NotEqual(updatedAtBeforeEdit, orderAfterEdit.UpdatedAt);
        Assert.Equal(9, orderAfterEdit.Quantity);
        Assert.Equal(900, orderAfterEdit.Total);
    }

    [Fact]
    public async Task Delete_Get_Returns_Confirmation_Page_For_Existing_Order()
    {
        using var client = _factory.CreateTestClient();
        var customerId = await CreateCustomerAsync(client);
        var (productId, productName) = await CreateProductAsync(client, unitPrice: "100");
        await CreateOrderAsync(client, customerId, productId, quantity: "2");
        var id = await FindOrderIdAsync(client, productName);

        var response = await client.GetAsync($"/Orders/Delete/{id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(productName, body);
    }

    [Fact]
    public async Task Delete_Post_Removes_Order_And_Redirects_To_Index()
    {
        using var client = _factory.CreateTestClient();
        var customerId = await CreateCustomerAsync(client);
        var (productId, productName) = await CreateProductAsync(client, unitPrice: "100");
        await CreateOrderAsync(client, customerId, productId, quantity: "2");
        var id = await FindOrderIdAsync(client, productName);

        var deleteGetResponse = await client.GetAsync($"/Orders/Delete/{id}");
        var deleteGetBody = await deleteGetResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(deleteGetBody);

        var deletePostResponse = await client.PostAsync("/Orders/Delete", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = id.ToString(),
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, deletePostResponse.StatusCode);

        // Not searching by productName here: the search box echoes ViewData["CurrentFilter"]
        // back into its own value attribute, so a search for the exact deleted name would always
        // "contain" that name in the page regardless of whether it's still in the results table.
        var indexResponse = await client.GetAsync("/Orders");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(productName, indexBody);
    }

    [Fact]
    public async Task DeleteConfirmed_Redirects_Without_Throwing_When_Order_Already_Gone()
    {
        using var client = _factory.CreateTestClient();
        var deleteToken = await GetDeleteTokenForAnyOrderAsync(client);

        var response = await client.PostAsync("/Orders/Delete", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = "999999",
            ["__RequestVerificationToken"] = deleteToken
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task Index_SearchString_Filters_By_Customer_Last_Name()
    {
        using var client = _factory.CreateTestClient();
        var (productId, _) = await CreateProductAsync(client, unitPrice: "50");
        var matchingLastName = $"Findable-{Guid.NewGuid():N}";
        var matchingCustomerId = await CreateCustomerAsync(client, matchingLastName);
        var nonMatchingCustomerId = await CreateCustomerAsync(client, $"Other-{Guid.NewGuid():N}");
        await CreateOrderAsync(client, matchingCustomerId, productId, quantity: "1");
        await CreateOrderAsync(client, nonMatchingCustomerId, productId, quantity: "1");

        var response = await client.GetAsync($"/Orders?searchString={matchingLastName}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains(matchingLastName, body);
    }

    [Fact]
    public async Task Index_SearchString_Filters_By_Product_Name()
    {
        using var client = _factory.CreateTestClient();
        var customerId = await CreateCustomerAsync(client);
        var uniqueMarker = $"Findable-{Guid.NewGuid():N}";
        var (matchingProductId, matchingProductName) = await CreateProductAsync(client, unitPrice: "50", productNamePrefix: uniqueMarker);
        var (nonMatchingProductId, nonMatchingProductName) = await CreateProductAsync(client, unitPrice: "50");
        await CreateOrderAsync(client, customerId, matchingProductId, quantity: "1");
        await CreateOrderAsync(client, customerId, nonMatchingProductId, quantity: "1");

        var response = await client.GetAsync($"/Orders?searchString={uniqueMarker}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains(matchingProductName, body);
        Assert.DoesNotContain(nonMatchingProductName, body);
    }

    [Fact]
    public async Task Index_Pagination_Splits_Results_Across_Pages()
    {
        using var client = _factory.CreateTestClient();
        var customerId = await CreateCustomerAsync(client);
        var uniqueMarker = $"PgTest{Guid.NewGuid():N}";

        // PageSize is 10 on OrdersController; 12 orders against uniquely-searchable products
        // (sharing the same marker) guarantee a second page regardless of any other data already
        // present in the shared test database.
        for (var i = 0; i < 12; i++)
        {
            var (productId, _) = await CreateProductAsync(client, unitPrice: "10", productNamePrefix: $"{uniqueMarker}-{i:D2}");
            await CreateOrderAsync(client, customerId, productId, quantity: "1");
        }

        var page1Response = await client.GetAsync($"/Orders?searchString={uniqueMarker}&pageNumber=1");
        var page1Body = await page1Response.Content.ReadAsStringAsync();
        var page2Response = await client.GetAsync($"/Orders?searchString={uniqueMarker}&pageNumber=2");
        var page2Body = await page2Response.Content.ReadAsStringAsync();

        Assert.Contains("Page 1 of 2", page1Body);
        Assert.Contains("Page 2 of 2", page2Body);
    }

    [Fact]
    public async Task GetProducts_Returns_Only_Products_In_The_Given_Category()
    {
        using var client = _factory.CreateTestClient();
        var categoryAId = await CreateCategoryAsync(client);
        var categoryBId = await CreateCategoryAsync(client);
        var (_, matchingProductName) = await CreateProductAsync(client, unitPrice: "50", categoryId: categoryAId);
        var (_, otherProductName) = await CreateProductAsync(client, unitPrice: "50", categoryId: categoryBId);

        var response = await client.GetAsync($"/Orders/GetProducts?categoryId={categoryAId}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        var productNames = document.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("productName").GetString())
            .ToList();
        Assert.Contains(matchingProductName, productNames);
        Assert.DoesNotContain(otherProductName, productNames);
    }

    private static async Task<int> CreateCustomerAsync(HttpClient client, string? lastName = null)
    {
        var resolvedLastName = lastName ?? $"OrderTestCustomer-{Guid.NewGuid():N}";
        var getResponse = await client.GetAsync("/Customers/Create");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(getBody);

        await client.PostAsync("/Customers/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["FirstName"] = "Test",
            ["LastName"] = resolvedLastName,
            ["Telephone"] = "555-0000",
            ["Email"] = $"{Guid.NewGuid():N}@example.com",
            ["Address"] = "1 Test Street",
            ["__RequestVerificationToken"] = token
        }));

        var indexResponse = await client.GetAsync($"/Customers?searchString={resolvedLastName}");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        var match = Regex.Match(indexBody, @"/Customers/Edit/(\d+)");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not locate freshly created customer '{resolvedLastName}'.");
        }

        return int.Parse(match.Groups[1].Value);
    }

    private static async Task<(int Id, string ProductName)> CreateProductAsync(HttpClient client, string unitPrice, string? productNamePrefix = null, int? categoryId = null)
    {
        var resolvedCategoryId = categoryId ?? await CreateCategoryAsync(client);
        var productName = productNamePrefix ?? $"OrderTestProduct-{Guid.NewGuid():N}";

        var getResponse = await client.GetAsync("/Products/Create");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(getBody);

        await client.PostAsync("/Products/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CategoryId"] = resolvedCategoryId.ToString(),
            ["ProductName"] = productName,
            ["UnitPrice"] = unitPrice,
            ["__RequestVerificationToken"] = token
        }));

        var indexResponse = await client.GetAsync($"/Products?searchString={productName}");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        var match = Regex.Match(indexBody, @"/Products/Edit/(\d+)");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not locate freshly created product '{productName}'.");
        }

        return (int.Parse(match.Groups[1].Value), productName);
    }

    private static async Task<int> CreateCategoryAsync(HttpClient client)
    {
        var categoryName = $"OrderTestCategory-{Guid.NewGuid():N}";
        var getResponse = await client.GetAsync("/Categories/Create");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(getBody);

        await client.PostAsync("/Categories/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CategoryName"] = categoryName,
            ["__RequestVerificationToken"] = token
        }));

        var indexResponse = await client.GetAsync($"/Categories?searchString={categoryName}");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        var match = Regex.Match(indexBody, @"/Categories/Edit/(\d+)");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not locate freshly created category '{categoryName}'.");
        }

        return int.Parse(match.Groups[1].Value);
    }

    private static async Task<HttpResponseMessage> CreateOrderAsync(HttpClient client, int customerId, int productId, string quantity)
    {
        var getResponse = await client.GetAsync("/Orders/Create");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(getBody);

        return await client.PostAsync("/Orders/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CustomerId"] = customerId.ToString(),
            ["ProductId"] = productId.ToString(),
            ["Quantity"] = quantity,
            ["__RequestVerificationToken"] = token
        }));
    }

    private static async Task<int> FindOrderIdAsync(HttpClient client, string productName)
    {
        var response = await client.GetAsync($"/Orders?searchString={productName}");
        var body = await response.Content.ReadAsStringAsync();

        var match = Regex.Match(body, @"/Orders/Edit/(\d+)");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not locate an order for product '{productName}' in the Index page.");
        }

        return int.Parse(match.Groups[1].Value);
    }

    private static async Task<string> GetDeleteTokenForAnyOrderAsync(HttpClient client)
    {
        var customerId = await CreateCustomerAsync(client);
        var (productId, _) = await CreateProductAsync(client, unitPrice: "10");
        await CreateOrderAsync(client, customerId, productId, quantity: "1");
        var indexResponse = await client.GetAsync("/Orders");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        var match = Regex.Match(indexBody, @"/Orders/Delete/(\d+)");
        if (!match.Success)
        {
            throw new InvalidOperationException("Could not locate any order's Delete link on the Index page.");
        }

        var deleteResponse = await client.GetAsync($"/Orders/Delete/{match.Groups[1].Value}");
        var deleteBody = await deleteResponse.Content.ReadAsStringAsync();
        return AntiforgeryHtmlHelper.ExtractAntiforgeryToken(deleteBody);
    }
}
