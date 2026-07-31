using System.Net;
using aspnet_core_tutorial.Data;
using aspnet_core_tutorial.IntegrationTests.Infrastructure;
using aspnet_core_tutorial.Seeders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace aspnet_core_tutorial.IntegrationTests.Controllers;

/// <summary>
/// Full HTTP round-trip coverage of the role-based authorization layer added on top of the
/// Categories/Products/Customers/Orders controllers: anonymous access, an authenticated but
/// wrong-role ("User") account, and an authenticated Admin account, plus the "User" role
/// assignment performed on self-registration. Exercises the real
/// `[Authorize(Roles = RoleSeeder.Admin)]`/`[AllowAnonymous]` attributes through ASP.NET Core's
/// actual authorization middleware, which the unit test project's direct controller-action calls
/// never go through.
/// </summary>
/// <remarks>
/// Created by edgar.muhamyangabo on 7/31/26
/// Author : edgar.muhamyangabo
/// Date : 7/31/26
/// Project : aspnet_core_tutorial
/// </remarks>
[Collection(IntegrationTestCollection.Name)]
public class AuthorizationTests
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public AuthorizationTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/Categories")]
    [InlineData("/Products")]
    public async Task Anonymous_Can_Reach_The_Public_Catalog_Index(string path)
    {
        using var client = _factory.CreateTestClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Customers")]
    [InlineData("/Orders")]
    public async Task Anonymous_Hitting_Customer_Or_Order_Data_Is_Redirected_To_Login(string path)
    {
        using var client = _factory.CreateTestClient();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/Identity/Account/Login", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Anonymous_Posting_To_A_Protected_Write_Action_Is_Redirected_To_Login_Before_Antiforgery_Runs()
    {
        // A missing antiforgery token alone would 400 (covered by
        // CategoriesControllerIntegrationTests.Create_Post_Without_Antiforgery_Token_Returns_400).
        // Here the request also omits the token, but since the caller is anonymous against an
        // Admin-only action, the authorization challenge must win and redirect to Login instead
        // of the pipeline ever reaching antiforgery validation.
        using var client = _factory.CreateTestClient();

        var response = await client.PostAsync("/Categories/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CategoryName"] = $"Anon-{Guid.NewGuid():N}"
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/Identity/Account/Login", response.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("/Categories")]
    [InlineData("/Products")]
    public async Task User_Role_Can_Still_Reach_The_Public_Catalog_Index(string path)
    {
        using var client = (await _factory.CreateUserAuthenticatedClientAsync()).Client;

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/Customers")]
    [InlineData("/Orders")]
    public async Task User_Role_Hitting_Customer_Or_Order_Data_Is_Redirected_To_AccessDenied(string path)
    {
        using var client = (await _factory.CreateUserAuthenticatedClientAsync()).Client;

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/Identity/Account/AccessDenied", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task AccessDenied_Page_Itself_Returns_200()
    {
        using var client = (await _factory.CreateUserAuthenticatedClientAsync()).Client;

        var response = await client.GetAsync("/Customers");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var accessDeniedResponse = await client.GetAsync(response.Headers.Location!);

        Assert.Equal(HttpStatusCode.OK, accessDeniedResponse.StatusCode);
    }

    [Fact]
    public async Task User_Role_Posting_To_A_Protected_Write_Action_With_A_Valid_Token_Is_Redirected_To_AccessDenied_And_Does_Not_Create_The_Category()
    {
        using var client = (await _factory.CreateUserAuthenticatedClientAsync()).Client;
        var categoryName = $"User-{Guid.NewGuid():N}";

        // A plain "User" account cannot reach Categories/Create's GET either (it is also under
        // the controller's [Authorize(Roles = Admin)]), so there is no legitimate way for this
        // role to obtain a real antiforgery token for this action. Reusing a token minted for the
        // (anonymous-reachable) Register page against a different action still proves the point:
        // even with a syntactically valid, correctly-paired antiforgery token attached, the role
        // check redirects to AccessDenied before the Create action, and therefore the antiforgery
        // check on it, ever runs.
        var registerGetResponse = await client.GetAsync("/Identity/Account/Register");
        var registerGetBody = await registerGetResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(registerGetBody);

        var response = await client.PostAsync("/Categories/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CategoryName"] = categoryName,
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Identity/Account/AccessDenied", response.Headers.Location!.OriginalString);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Categories.AsNoTracking().AnyAsync(c => c.CategoryName == categoryName));
    }

    [Theory]
    [InlineData("/Customers")]
    [InlineData("/Orders")]
    public async Task Admin_Can_Reach_Customer_And_Order_Data(string path)
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_Can_Complete_A_Full_Create_Round_Trip_On_Categories()
    {
        using var client = await _factory.CreateAdminAuthenticatedClientAsync();
        var categoryName = $"Admin-{Guid.NewGuid():N}";

        var getResponse = await client.GetAsync("/Categories/Create");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var getBody = await getResponse.Content.ReadAsStringAsync();
        var token = AntiforgeryHtmlHelper.ExtractAntiforgeryToken(getBody);

        var postResponse = await client.PostAsync("/Categories/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["CategoryName"] = categoryName,
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Equal("/Categories", postResponse.Headers.Location?.OriginalString);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.Categories.AsNoTracking().AnyAsync(c => c.CategoryName == categoryName));
    }

    [Fact]
    public async Task Register_Assigns_The_User_Role_Not_Admin()
    {
        var registration = await _factory.CreateUserAuthenticatedClientAsync();
        using var client = registration.Client;
        var email = registration.Email;

        await using var scope = _factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByEmailAsync(email);

        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user!, RoleSeeder.User));
        Assert.False(await userManager.IsInRoleAsync(user!, RoleSeeder.Admin));
    }
}
