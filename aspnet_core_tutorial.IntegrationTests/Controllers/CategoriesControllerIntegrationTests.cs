using System.Net;
using System.Text.RegularExpressions;
using aspnet_core_tutorial.Data;
using aspnet_core_tutorial.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace aspnet_core_tutorial.IntegrationTests.Controllers;

/// <summary>
/// Full HTTP round-trip coverage of <c>CategoriesController</c>: real GET/POST requests against
/// a real ASP.NET Core host and a real PostgreSQL Testcontainer, including the
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
public class CategoriesControllerIntegrationTests
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public CategoriesControllerIntegrationTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_Returns_200_And_Contains_Seeded_Category()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();

        var response = await client.GetAsync("/Categories");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Seeded by CategorySeeder in Program.cs, applied once against the Testcontainer at
        // host startup.
        Assert.Contains("Lemonades", body);
    }

    [Fact]
    public async Task Create_Get_Returns_Form_With_Antiforgery_Token()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();

        var response = await client.GetAsync("/Categories/Create");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(AntiforgeryHtmlHelper.ExtractAntiforgeryToken(body));
    }

    [Fact]
    public async Task Create_Post_With_Valid_Token_Creates_Category_And_Redirects_To_Index()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var categoryName = $"Category-{Guid.NewGuid():N}";

        var response = await CreateCategoryAsync(client, categoryName);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Categories", response.Headers.Location?.OriginalString);

        var indexResponse = await client.GetAsync($"/Categories?searchString={categoryName}");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        Assert.Contains(categoryName, indexBody);
    }

    [Fact]
    public async Task Create_Post_Without_Antiforgery_Token_Returns_400()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();

        var response = await client.PostAsync("/Categories/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CategoryName"] = $"NoToken-{Guid.NewGuid():N}"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Edit_Get_Returns_200_For_Existing_Category()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var categoryName = $"Category-{Guid.NewGuid():N}";
        await CreateCategoryAsync(client, categoryName);
        var id = await FindCategoryIdAsync(client, categoryName);

        var response = await client.GetAsync($"/Categories/Edit/{id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(categoryName, body);
    }

    [Fact]
    public async Task Edit_Get_Returns_404_When_Category_Not_Found()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();

        var response = await client.GetAsync("/Categories/Edit/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Edit_Post_Updates_Category_And_Redirects_To_Index()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var originalName = $"Category-{Guid.NewGuid():N}";
        var updatedName = $"Category-{Guid.NewGuid():N}";
        await CreateCategoryAsync(client, originalName);
        var id = await FindCategoryIdAsync(client, originalName);

        var editGetResponse = await client.GetAsync($"/Categories/Edit/{id}");
        var editGetBody = await editGetResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(editGetBody);

        var editPostResponse = await client.PostAsync($"/Categories/Edit/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = id.ToString(),
            ["CategoryName"] = updatedName,
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, editPostResponse.StatusCode);

        var indexResponse = await client.GetAsync($"/Categories?searchString={updatedName}");
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
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var originalName = $"Category-{Guid.NewGuid():N}";
        await CreateCategoryAsync(client, originalName);
        var id = await FindCategoryIdAsync(client, originalName);

        DateTime createdAtBeforeEdit;
        DateTime updatedAtBeforeEdit;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var categoryBeforeEdit = await db.Categories.AsNoTracking().SingleAsync(c => c.Id == id);
            createdAtBeforeEdit = categoryBeforeEdit.CreatedAt;
            updatedAtBeforeEdit = categoryBeforeEdit.UpdatedAt;
        }
        Assert.NotEqual(default, createdAtBeforeEdit);

        var editGetResponse = await client.GetAsync($"/Categories/Edit/{id}");
        var editGetBody = await editGetResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(editGetBody);

        var updatedName = $"Category-{Guid.NewGuid():N}";
        var editPostResponse = await client.PostAsync($"/Categories/Edit/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = id.ToString(),
            ["CategoryName"] = updatedName,
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, editPostResponse.StatusCode);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var categoryAfterEdit = await verificationDb.Categories.AsNoTracking().SingleAsync(c => c.Id == id);

        Assert.Equal(createdAtBeforeEdit, categoryAfterEdit.CreatedAt);
        Assert.NotEqual(updatedAtBeforeEdit, categoryAfterEdit.UpdatedAt);
        Assert.Equal(updatedName, categoryAfterEdit.CategoryName);
    }

    [Fact]
    public async Task Delete_Get_Returns_Confirmation_Page_For_Existing_Category()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var categoryName = $"Category-{Guid.NewGuid():N}";
        await CreateCategoryAsync(client, categoryName);
        var id = await FindCategoryIdAsync(client, categoryName);

        var response = await client.GetAsync($"/Categories/Delete/{id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(categoryName, body);
    }

    [Fact]
    public async Task Delete_Post_Removes_Category_And_Redirects_To_Index()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var categoryName = $"Category-{Guid.NewGuid():N}";
        await CreateCategoryAsync(client, categoryName);
        var id = await FindCategoryIdAsync(client, categoryName);

        var deleteGetResponse = await client.GetAsync($"/Categories/Delete/{id}");
        var deleteGetBody = await deleteGetResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(deleteGetBody);

        var deletePostResponse = await client.PostAsync("/Categories/Delete", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = id.ToString(),
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, deletePostResponse.StatusCode);

        // Not searching by categoryName here: the search box echoes ViewData["CurrentFilter"]
        // back into its own value attribute, so a search for the exact deleted name would always
        // "contain" that name in the page regardless of whether it's still in the results table.
        var indexResponse = await client.GetAsync("/Categories");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(categoryName, indexBody);
    }

    [Fact]
    public async Task Index_SearchString_Filters_To_Matching_Categories_Only()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var uniqueMarker = $"Findable-{Guid.NewGuid():N}";
        var matching = $"{uniqueMarker}-A";
        var nonMatching = $"Other-{Guid.NewGuid():N}";
        await CreateCategoryAsync(client, matching);
        await CreateCategoryAsync(client, nonMatching);

        var response = await client.GetAsync($"/Categories?searchString={uniqueMarker}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains(matching, body);
        Assert.DoesNotContain(nonMatching, body);
    }

    [Fact]
    public async Task Index_Pagination_Splits_Results_Across_Pages()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var uniqueMarker = $"PgTest{Guid.NewGuid():N}";

        // PageSize is 10 on CategoriesController; 12 uniquely-searchable categories guarantee a
        // second page regardless of any other data already present in the shared test database.
        for (var i = 0; i < 12; i++)
        {
            await CreateCategoryAsync(client, $"{uniqueMarker}-{i:D2}");
        }

        var page1Response = await client.GetAsync($"/Categories?searchString={uniqueMarker}&pageNumber=1");
        var page1Body = await page1Response.Content.ReadAsStringAsync();
        var page2Response = await client.GetAsync($"/Categories?searchString={uniqueMarker}&pageNumber=2");
        var page2Body = await page2Response.Content.ReadAsStringAsync();

        Assert.Contains("Page 1 of 2", page1Body);
        Assert.Contains("Page 2 of 2", page2Body);
        Assert.Contains($"{uniqueMarker}-00", page1Body);
        Assert.DoesNotContain($"{uniqueMarker}-11", page1Body);
        Assert.Contains($"{uniqueMarker}-11", page2Body);
    }

    private static async Task<HttpResponseMessage> CreateCategoryAsync(HttpClient client, string categoryName)
    {
        var getResponse = await client.GetAsync("/Categories/Create");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(getBody);

        return await client.PostAsync("/Categories/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CategoryName"] = categoryName,
            ["__RequestVerificationToken"] = token
        }));
    }

    private static async Task<int> FindCategoryIdAsync(HttpClient client, string categoryName)
    {
        var response = await client.GetAsync($"/Categories?searchString={categoryName}");
        var body = await response.Content.ReadAsStringAsync();

        var match = Regex.Match(body, @"/Categories/Edit/(\d+)");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not locate category '{categoryName}' in the Index page.");
        }

        return int.Parse(match.Groups[1].Value);
    }
}
