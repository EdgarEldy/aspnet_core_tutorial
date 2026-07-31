using System.Net;
using System.Text.RegularExpressions;
using aspnet_core_tutorial.Data;
using aspnet_core_tutorial.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace aspnet_core_tutorial.IntegrationTests.Controllers;

/// <summary>
/// Full HTTP round-trip coverage of <c>ProductsController</c>: real GET/POST requests against a
/// real ASP.NET Core host and a real PostgreSQL Testcontainer, including the
/// `[ValidateAntiForgeryToken]`-protected POST actions (Create/Edit/Delete), search and
/// pagination.
/// </summary>
/// <remarks>
/// Created by edgar.muhamyangabo on 7/13/26
/// Author : edgar.muhamyangabo
/// Date : 7/13/26
/// Project : aspnet_core_tutorial
/// </remarks>
[Collection(IntegrationTestCollection.Name)]
public class ProductsControllerIntegrationTests
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public ProductsControllerIntegrationTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_Returns_200_And_Contains_Seeded_Product()
    {
        using var client = _factory.CreateTestClient();

        var response = await client.GetAsync("/Products");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Seeded by ProductSeeder in Program.cs, applied once against the Testcontainer at host
        // startup.
        Assert.Contains("Citron", body);
    }

    [Fact]
    public async Task Create_Get_Returns_Form_With_Antiforgery_Token_And_Category_Dropdown()
    {
        using var client = _factory.CreateTestClient();

        var response = await client.GetAsync("/Products/Create");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(AntiforgeryHtmlHelper.ExtractAntiforgeryToken(body));
        Assert.Contains("<select", body);
    }

    [Fact]
    public async Task Create_Post_With_Valid_Token_Creates_Product_And_Redirects_To_Index()
    {
        using var client = _factory.CreateTestClient();
        var categoryId = await CreateCategoryAsync(client);
        var productName = $"Product-{Guid.NewGuid():N}";

        var response = await CreateProductAsync(client, categoryId, productName, unitPrice: "1200");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Products", response.Headers.Location?.OriginalString);

        var indexResponse = await client.GetAsync($"/Products?searchString={productName}");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        Assert.Contains(productName, indexBody);
    }

    [Fact]
    public async Task Create_Post_Without_Antiforgery_Token_Returns_400()
    {
        using var client = _factory.CreateTestClient();
        var categoryId = await CreateCategoryAsync(client);

        var response = await client.PostAsync("/Products/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CategoryId"] = categoryId.ToString(),
            ["ProductName"] = $"NoToken-{Guid.NewGuid():N}",
            ["UnitPrice"] = "500"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Edit_Get_Returns_200_For_Existing_Product()
    {
        using var client = _factory.CreateTestClient();
        var categoryId = await CreateCategoryAsync(client);
        var productName = $"Product-{Guid.NewGuid():N}";
        await CreateProductAsync(client, categoryId, productName, unitPrice: "999");
        var id = await FindProductIdAsync(client, productName);

        var response = await client.GetAsync($"/Products/Edit/{id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(productName, body);
    }

    [Fact]
    public async Task Edit_Get_Returns_404_When_Product_Not_Found()
    {
        using var client = _factory.CreateTestClient();

        var response = await client.GetAsync("/Products/Edit/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Edit_Post_Updates_Product_And_Redirects_To_Index()
    {
        using var client = _factory.CreateTestClient();
        var categoryId = await CreateCategoryAsync(client);
        var originalName = $"Product-{Guid.NewGuid():N}";
        var updatedName = $"Product-{Guid.NewGuid():N}";
        await CreateProductAsync(client, categoryId, originalName, unitPrice: "100");
        var id = await FindProductIdAsync(client, originalName);

        var editGetResponse = await client.GetAsync($"/Products/Edit/{id}");
        var editGetBody = await editGetResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(editGetBody);

        var editPostResponse = await client.PostAsync($"/Products/Edit/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = id.ToString(),
            ["CategoryId"] = categoryId.ToString(),
            ["ProductName"] = updatedName,
            ["UnitPrice"] = "150",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, editPostResponse.StatusCode);

        var indexResponse = await client.GetAsync($"/Products?searchString={updatedName}");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        Assert.Contains(updatedName, indexBody);
    }

    [Fact]
    public async Task Edit_Post_Does_Not_Overwrite_CreatedAt_And_Refreshes_UpdatedAt()
    {
        // Regression test for commit 9abef79: Edit used to attach a partially-bound entity via
        // _context.Update(), marking every property (including the [Bind]-excluded CreatedAt) as
        // modified and overwriting it with the CLR default (0001-01-01). This asserts the real,
        // persisted row in the Testcontainer's Postgres database, not just the HTTP response, so
        // it exercises the actual round-trip through the fixed Edit action.
        using var client = _factory.CreateTestClient();
        var categoryId = await CreateCategoryAsync(client);
        var productName = $"Product-{Guid.NewGuid():N}";
        await CreateProductAsync(client, categoryId, productName, unitPrice: "100");
        var id = await FindProductIdAsync(client, productName);

        DateTime createdAtBeforeEdit;
        DateTime updatedAtBeforeEdit;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var productBeforeEdit = await db.Products.AsNoTracking().SingleAsync(p => p.Id == id);
            createdAtBeforeEdit = productBeforeEdit.CreatedAt;
            updatedAtBeforeEdit = productBeforeEdit.UpdatedAt;
        }
        Assert.NotEqual(default, createdAtBeforeEdit);

        var editGetResponse = await client.GetAsync($"/Products/Edit/{id}");
        var editGetBody = await editGetResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(editGetBody);

        var updatedName = $"Product-{Guid.NewGuid():N}";
        var editPostResponse = await client.PostAsync($"/Products/Edit/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = id.ToString(),
            ["CategoryId"] = categoryId.ToString(),
            ["ProductName"] = updatedName,
            ["UnitPrice"] = "150",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, editPostResponse.StatusCode);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var productAfterEdit = await verificationDb.Products.AsNoTracking().SingleAsync(p => p.Id == id);

        Assert.Equal(createdAtBeforeEdit, productAfterEdit.CreatedAt);
        Assert.NotEqual(updatedAtBeforeEdit, productAfterEdit.UpdatedAt);
        Assert.Equal(updatedName, productAfterEdit.ProductName);
    }

    [Fact]
    public async Task Delete_Get_Returns_Confirmation_Page_For_Existing_Product()
    {
        using var client = _factory.CreateTestClient();
        var categoryId = await CreateCategoryAsync(client);
        var productName = $"Product-{Guid.NewGuid():N}";
        await CreateProductAsync(client, categoryId, productName, unitPrice: "250");
        var id = await FindProductIdAsync(client, productName);

        var response = await client.GetAsync($"/Products/Delete/{id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(productName, body);
    }

    [Fact]
    public async Task Delete_Post_Removes_Product_And_Redirects_To_Index()
    {
        using var client = _factory.CreateTestClient();
        var categoryId = await CreateCategoryAsync(client);
        var productName = $"Product-{Guid.NewGuid():N}";
        await CreateProductAsync(client, categoryId, productName, unitPrice: "300");
        var id = await FindProductIdAsync(client, productName);

        var deleteGetResponse = await client.GetAsync($"/Products/Delete/{id}");
        var deleteGetBody = await deleteGetResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(deleteGetBody);

        var deletePostResponse = await client.PostAsync("/Products/Delete", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = id.ToString(),
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, deletePostResponse.StatusCode);

        // Not searching by productName here: the search box echoes ViewData["CurrentFilter"]
        // back into its own value attribute, so a search for the exact deleted name would always
        // "contain" that name in the page regardless of whether it's still in the results table.
        var indexResponse = await client.GetAsync("/Products");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(productName, indexBody);
    }

    [Fact]
    public async Task Index_SearchString_Filters_To_Matching_Products_Only()
    {
        using var client = _factory.CreateTestClient();
        var categoryId = await CreateCategoryAsync(client);
        var uniqueMarker = $"Findable-{Guid.NewGuid():N}";
        var matching = $"{uniqueMarker}-A";
        var nonMatching = $"Other-{Guid.NewGuid():N}";
        await CreateProductAsync(client, categoryId, matching, unitPrice: "10");
        await CreateProductAsync(client, categoryId, nonMatching, unitPrice: "10");

        var response = await client.GetAsync($"/Products?searchString={uniqueMarker}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains(matching, body);
        Assert.DoesNotContain(nonMatching, body);
    }

    [Fact]
    public async Task Index_Pagination_Splits_Results_Across_Pages()
    {
        using var client = _factory.CreateTestClient();
        var categoryId = await CreateCategoryAsync(client);
        var uniqueMarker = $"PgTest{Guid.NewGuid():N}";

        // PageSize is 10 on ProductsController; 12 uniquely-searchable products guarantee a
        // second page regardless of any other data already present in the shared test database.
        for (var i = 0; i < 12; i++)
        {
            await CreateProductAsync(client, categoryId, $"{uniqueMarker}-{i:D2}", unitPrice: "10");
        }

        var page1Response = await client.GetAsync($"/Products?searchString={uniqueMarker}&pageNumber=1");
        var page1Body = await page1Response.Content.ReadAsStringAsync();
        var page2Response = await client.GetAsync($"/Products?searchString={uniqueMarker}&pageNumber=2");
        var page2Body = await page2Response.Content.ReadAsStringAsync();

        Assert.Contains("Page 1 of 2", page1Body);
        Assert.Contains("Page 2 of 2", page2Body);
        Assert.Contains($"{uniqueMarker}-00", page1Body);
        Assert.DoesNotContain($"{uniqueMarker}-11", page1Body);
        Assert.Contains($"{uniqueMarker}-11", page2Body);
    }

    private static async Task<int> CreateCategoryAsync(HttpClient client)
    {
        var categoryName = $"ProductTestCategory-{Guid.NewGuid():N}";
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

    private static async Task<HttpResponseMessage> CreateProductAsync(HttpClient client, int categoryId, string productName, string unitPrice)
    {
        var getResponse = await client.GetAsync("/Products/Create");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(getBody);

        return await client.PostAsync("/Products/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CategoryId"] = categoryId.ToString(),
            ["ProductName"] = productName,
            ["UnitPrice"] = unitPrice,
            ["__RequestVerificationToken"] = token
        }));
    }

    private static async Task<int> FindProductIdAsync(HttpClient client, string productName)
    {
        var response = await client.GetAsync($"/Products?searchString={productName}");
        var body = await response.Content.ReadAsStringAsync();

        var match = Regex.Match(body, @"/Products/Edit/(\d+)");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not locate product '{productName}' in the Index page.");
        }

        return int.Parse(match.Groups[1].Value);
    }
}
