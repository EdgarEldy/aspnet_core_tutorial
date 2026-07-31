using System.Net;
using aspnet_core_tutorial.IntegrationTests.Infrastructure;

namespace aspnet_core_tutorial.IntegrationTests.Identity;

/// <summary>
/// Verifies the real authentication behavior of the scaffolded ASP.NET Core Identity UI: the
/// login page is reachable anonymously, while a protected account management page redirects an
/// anonymous visitor to it. Note that the business CRUD controllers (Categories/Products) carry
/// no `[Authorize]` attribute in this codebase, so no such redirect is exercised there; only the
/// Identity area's own protected pages are covered here.
/// </summary>
/// <remarks>
/// Created by edgar.muhamyangabo on 7/13/26
/// Author : edgar.muhamyangabo
/// Date : 7/13/26
/// Project : aspnet_core_tutorial
/// </remarks>
[Collection(IntegrationTestCollection.Name)]
public class IdentityAuthenticationTests
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public IdentityAuthenticationTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_Page_Is_Accessible_Anonymously()
    {
        using var client = _factory.CreateTestClient();

        var response = await client.GetAsync("/Identity/Account/Login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_Page_Is_Accessible_Anonymously()
    {
        using var client = _factory.CreateTestClient();

        var response = await client.GetAsync("/Identity/Account/Register");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Manage_Page_Redirects_Anonymous_User_To_Login()
    {
        using var client = _factory.CreateTestClient();

        var response = await client.GetAsync("/Identity/Account/Manage");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/Identity/Account/Login", response.Headers.Location!.OriginalString);
    }
}
