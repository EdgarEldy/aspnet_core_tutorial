using aspnet_core_tutorial.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace aspnet_core_tutorial.IntegrationTests.Infrastructure;

/// <summary>
/// Boots a real, ephemeral PostgreSQL container via Testcontainers for the lifetime of the
/// integration test run, then wires the application under test (via
/// <see cref="WebApplicationFactory{Program}"/>) to that container instead of the
/// developer/production database configured in appsettings.json.
/// </summary>
/// <remarks>
/// Created by edgar.muhamyangabo on 7/13/26
/// Author : edgar.muhamyangabo
/// Date : 7/13/26
/// Project : aspnet_core_tutorial
/// </remarks>
public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("aspnet_core_tutorial_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Program.cs registers ApplicationDbContext and the "postgresql" health check
            // against the appsettings.json connection string as soon as those lines run, well
            // before this callback executes. Both registrations capture that connection string
            // directly (not lazily), so simply adding a new registration would leave the
            // original one active too; the existing ones must be removed first.
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));

            services.RemoveAll<IConfigureOptions<HealthCheckServiceOptions>>();
            services.AddHealthChecks()
                .AddNpgSql(_dbContainer.GetConnectionString(), name: "postgresql", tags: new[] { "database", "postgresql" });
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Apply the real EF Core migrations up front, using a throwaway context/connection,
        // before the host (and therefore Program.cs's own EnsureCreated()-based seeders) starts.
        // This guarantees the schema is created via migrations, matching production, rather than
        // via EnsureCreated: once the tables already exist, the seeders' EnsureCreated() calls
        // become safe no-ops and only their idempotent seed-data inserts run. Accessing Services
        // instead (to reuse the app's own DI-configured context) was tried and rejected: it
        // triggers host startup immediately, running the seeders' EnsureCreated() before this
        // method gets a chance to migrate, against an empty database.
        //
        // PendingModelChangesWarning is suppressed here: `dotnet ef database update` against this
        // same migration and a real PostgreSQL instance applies cleanly with no drift (verified
        // directly), so this is a false positive from comparing the model built by this
        // minimally-configured throwaway context against the migration snapshot, not a real
        // mismatch between the model and InitialCreate.
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_dbContainer.GetConnectionString())
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        await using var context = new ApplicationDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await base.DisposeAsync();
    }
}
