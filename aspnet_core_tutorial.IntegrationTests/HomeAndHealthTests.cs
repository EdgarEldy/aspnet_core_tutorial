using System.Net;
using aspnet_core_tutorial.IntegrationTests.Infrastructure;

namespace aspnet_core_tutorial.IntegrationTests;

/// <summary>
/// End-to-end HTTP checks for the application's home page and `/health` endpoint, run against a
/// real ASP.NET Core host backed by a real, ephemeral PostgreSQL Testcontainer.
/// </summary>
/// <remarks>
/// Created by edgar.muhamyangabo on 7/13/26
/// Author : edgar.muhamyangabo
/// Date : 7/13/26
/// Project : aspnet_core_tutorial
/// </remarks>
[Collection(IntegrationTestCollection.Name)]
public class HomeAndHealthTests
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public HomeAndHealthTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Home_Page_Returns_200_Ok()
    {
        using var client = _factory.CreateTestClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_Endpoint_Returns_Healthy_Once_Postgres_Testcontainer_Is_Connected()
    {
        using var client = _factory.CreateTestClient();

        var response = await client.GetAsync("/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", body);
    }
}
