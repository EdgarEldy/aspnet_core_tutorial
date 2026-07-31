using System.Net;
using System.Text.RegularExpressions;
using aspnet_core_tutorial.Data;
using aspnet_core_tutorial.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace aspnet_core_tutorial.IntegrationTests.Controllers;

/// <summary>
/// Full HTTP round-trip coverage of <c>CustomersController</c>: real GET/POST requests against
/// a real ASP.NET Core host and a real PostgreSQL Testcontainer, including the
/// `[ValidateAntiForgeryToken]`-protected POST actions (Create/Edit/Delete), search and
/// pagination.
/// </summary>
/// <remarks>
/// Created by edgar.muhamyangabo on 7/31/26
/// Author : edgar.muhamyangabo
/// Date : 7/31/26
/// Project : aspnet_core_tutorial
/// </remarks>
[Collection(IntegrationTestCollection.Name)]
public class CustomersControllerIntegrationTests
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public CustomersControllerIntegrationTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Index_Returns_200_And_Contains_Seeded_Customer()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();

        var response = await client.GetAsync("/Customers");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // Seeded by CustomerSeeder in Program.cs, applied once against the Testcontainer at
        // host startup.
        Assert.Contains("Eldy", body);
    }

    [Fact]
    public async Task Create_Get_Returns_Form_With_Antiforgery_Token()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();

        var response = await client.GetAsync("/Customers/Create");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty(AntiforgeryHtmlHelper.ExtractAntiforgeryToken(body));
    }

    [Fact]
    public async Task Create_Post_With_Valid_Token_Creates_Customer_And_Redirects_To_Index()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var lastName = $"Customer-{Guid.NewGuid():N}";

        var response = await CreateCustomerAsync(client, lastName);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Customers", response.Headers.Location?.OriginalString);

        var indexResponse = await client.GetAsync($"/Customers?searchString={lastName}");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        Assert.Contains(lastName, indexBody);
    }

    [Fact]
    public async Task Create_Post_Without_Antiforgery_Token_Returns_400()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();

        var response = await client.PostAsync("/Customers/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["FirstName"] = "No",
            ["LastName"] = $"Token-{Guid.NewGuid():N}",
            ["Telephone"] = "555-0000",
            ["Email"] = "no-token@example.com",
            ["Address"] = "1 Test Street"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Edit_Get_Returns_200_For_Existing_Customer()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var lastName = $"Customer-{Guid.NewGuid():N}";
        await CreateCustomerAsync(client, lastName);
        var id = await FindCustomerIdAsync(client, lastName);

        var response = await client.GetAsync($"/Customers/Edit/{id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(lastName, body);
    }

    [Fact]
    public async Task Edit_Get_Returns_404_When_Customer_Not_Found()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();

        var response = await client.GetAsync("/Customers/Edit/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Edit_Post_Updates_Customer_And_Redirects_To_Index()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var originalLastName = $"Customer-{Guid.NewGuid():N}";
        var updatedLastName = $"Customer-{Guid.NewGuid():N}";
        await CreateCustomerAsync(client, originalLastName);
        var id = await FindCustomerIdAsync(client, originalLastName);

        var editGetResponse = await client.GetAsync($"/Customers/Edit/{id}");
        var editGetBody = await editGetResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(editGetBody);

        var editPostResponse = await client.PostAsync($"/Customers/Edit/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = id.ToString(),
            ["FirstName"] = "Edited",
            ["LastName"] = updatedLastName,
            ["Telephone"] = "555-9999",
            ["Email"] = "edited@example.com",
            ["Address"] = "2 Test Street",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, editPostResponse.StatusCode);

        var indexResponse = await client.GetAsync($"/Customers?searchString={updatedLastName}");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        Assert.Contains(updatedLastName, indexBody);
    }

    [Fact]
    public async Task Edit_Post_Does_Not_Overwrite_CreatedAt_And_Refreshes_UpdatedAt()
    {
        // Regression test mirroring the same fix already locked in for Categories/Products: Edit
        // fetches the tracked entity and patches fields individually instead of attaching the
        // partially-bound model, so CreatedAt survives an edit while UpdatedAt moves forward.
        // Asserts the real, persisted row in the Testcontainer's Postgres database.
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var originalLastName = $"Customer-{Guid.NewGuid():N}";
        await CreateCustomerAsync(client, originalLastName);
        var id = await FindCustomerIdAsync(client, originalLastName);

        DateTime createdAtBeforeEdit;
        DateTime updatedAtBeforeEdit;
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var customerBeforeEdit = await db.Customers.AsNoTracking().SingleAsync(c => c.Id == id);
            createdAtBeforeEdit = customerBeforeEdit.CreatedAt;
            updatedAtBeforeEdit = customerBeforeEdit.UpdatedAt;
        }
        Assert.NotEqual(default, createdAtBeforeEdit);

        var editGetResponse = await client.GetAsync($"/Customers/Edit/{id}");
        var editGetBody = await editGetResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(editGetBody);

        var updatedLastName = $"Customer-{Guid.NewGuid():N}";
        var editPostResponse = await client.PostAsync($"/Customers/Edit/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = id.ToString(),
            ["FirstName"] = "Edited",
            ["LastName"] = updatedLastName,
            ["Telephone"] = "555-9999",
            ["Email"] = "edited@example.com",
            ["Address"] = "2 Test Street",
            ["__RequestVerificationToken"] = token
        }));
        Assert.Equal(HttpStatusCode.Redirect, editPostResponse.StatusCode);

        await using var verificationScope = _factory.Services.CreateAsyncScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customerAfterEdit = await verificationDb.Customers.AsNoTracking().SingleAsync(c => c.Id == id);

        Assert.Equal(createdAtBeforeEdit, customerAfterEdit.CreatedAt);
        Assert.NotEqual(updatedAtBeforeEdit, customerAfterEdit.UpdatedAt);
        Assert.Equal(updatedLastName, customerAfterEdit.LastName);
    }

    [Fact]
    public async Task Delete_Get_Returns_Confirmation_Page_For_Existing_Customer()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var lastName = $"Customer-{Guid.NewGuid():N}";
        await CreateCustomerAsync(client, lastName);
        var id = await FindCustomerIdAsync(client, lastName);

        var response = await client.GetAsync($"/Customers/Delete/{id}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(lastName, body);
    }

    [Fact]
    public async Task Delete_Post_Removes_Customer_And_Redirects_To_Index()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var lastName = $"Customer-{Guid.NewGuid():N}";
        await CreateCustomerAsync(client, lastName);
        var id = await FindCustomerIdAsync(client, lastName);

        var deleteGetResponse = await client.GetAsync($"/Customers/Delete/{id}");
        var deleteGetBody = await deleteGetResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(deleteGetBody);

        var deletePostResponse = await client.PostAsync("/Customers/Delete", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = id.ToString(),
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, deletePostResponse.StatusCode);

        // Not searching by lastName here: the search box echoes ViewData["CurrentFilter"] back
        // into its own value attribute, so a search for the exact deleted name would always
        // "contain" that name in the page regardless of whether it's still in the results table.
        var indexResponse = await client.GetAsync("/Customers");
        var indexBody = await indexResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(lastName, indexBody);
    }

    [Fact]
    public async Task Index_SearchString_Filters_To_Matching_Customers_Only()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var uniqueMarker = $"Findable-{Guid.NewGuid():N}";
        var matching = $"{uniqueMarker}-A";
        var nonMatching = $"Other-{Guid.NewGuid():N}";
        await CreateCustomerAsync(client, matching);
        await CreateCustomerAsync(client, nonMatching);

        var response = await client.GetAsync($"/Customers?searchString={uniqueMarker}");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains(matching, body);
        Assert.DoesNotContain(nonMatching, body);
    }

    [Fact]
    public async Task Index_Pagination_Splits_Results_Across_Pages()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var uniqueMarker = $"PgTest{Guid.NewGuid():N}";

        // PageSize is 10 on CustomersController; 12 uniquely-searchable customers guarantee a
        // second page regardless of any other data already present in the shared test database.
        for (var i = 0; i < 12; i++)
        {
            await CreateCustomerAsync(client, $"{uniqueMarker}-{i:D2}");
        }

        var page1Response = await client.GetAsync($"/Customers?searchString={uniqueMarker}&pageNumber=1");
        var page1Body = await page1Response.Content.ReadAsStringAsync();
        var page2Response = await client.GetAsync($"/Customers?searchString={uniqueMarker}&pageNumber=2");
        var page2Body = await page2Response.Content.ReadAsStringAsync();

        Assert.Contains("Page 1 of 2", page1Body);
        Assert.Contains("Page 2 of 2", page2Body);
        Assert.Contains($"{uniqueMarker}-00", page1Body);
        Assert.DoesNotContain($"{uniqueMarker}-11", page1Body);
        Assert.Contains($"{uniqueMarker}-11", page2Body);
    }

    private static async Task<HttpResponseMessage> CreateCustomerAsync(HttpClient client, string lastName)
    {
        var getResponse = await client.GetAsync("/Customers/Create");
        var getBody = await getResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(getBody);

        return await client.PostAsync("/Customers/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["FirstName"] = "Test",
            ["LastName"] = lastName,
            ["Telephone"] = "555-0000",
            ["Email"] = $"{Guid.NewGuid():N}@example.com",
            ["Address"] = "1 Test Street",
            ["__RequestVerificationToken"] = token
        }));
    }

    private static async Task<int> FindCustomerIdAsync(HttpClient client, string lastName)
    {
        var response = await client.GetAsync($"/Customers?searchString={lastName}");
        var body = await response.Content.ReadAsStringAsync();

        var match = Regex.Match(body, @"/Customers/Edit/(\d+)");
        if (!match.Success)
        {
            throw new InvalidOperationException($"Could not locate customer '{lastName}' in the Index page.");
        }

        return int.Parse(match.Groups[1].Value);
    }
}
