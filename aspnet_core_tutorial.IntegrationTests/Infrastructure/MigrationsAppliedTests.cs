using aspnet_core_tutorial.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace aspnet_core_tutorial.IntegrationTests.Infrastructure;

/// <summary>
/// Confirms the Testcontainers-backed Postgres database was provisioned through real EF Core
/// migrations rather than <c>EnsureCreated()</c>: the migrations history table must exist and
/// record the app's single <c>InitialCreate</c> migration once the shared container has started.
/// </summary>
/// <remarks>
/// Created by edgar.muhamyangabo on 7/28/26
/// Author : edgar.muhamyangabo
/// Date : 7/28/26
/// Project : aspnet_core_tutorial
/// </remarks>
[Collection(IntegrationTestCollection.Name)]
public class MigrationsAppliedTests
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public MigrationsAppliedTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InitialCreate_Migration_Is_Recorded_In_The_Testcontainer_Database()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();

        Assert.Contains(appliedMigrations, migrationId => migrationId.EndsWith("_InitialCreate"));
    }
}
