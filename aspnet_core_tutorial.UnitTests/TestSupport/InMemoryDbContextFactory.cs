using aspnet_core_tutorial.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace aspnet_core_tutorial.UnitTests.TestSupport;

/// <summary>
/// Builds isolated, disposable <see cref="ApplicationDbContext"/> instances backed by the EF Core
/// InMemory provider, each on its own uniquely named database with its own <see cref="InMemoryDatabaseRoot"/>.
/// </summary>
/// <remarks>
/// Calls to <c>UseInMemoryDatabase</c> that omit the root parameter all share a single
/// process-wide static default root, which backs the provider's internal service provider. Giving
/// each context its own <see cref="InMemoryDatabaseRoot"/>, on top of a uniquely named database,
/// keeps every test's underlying store fully independent, as recommended by the EF Core testing
/// documentation, rather than relying on database-name uniqueness alone.
/// Created by edgar.muhamyangabo on 7/13/26
/// Author : edgar.muhamyangabo
/// Date : 7/13/26
/// Project : aspnet_core_tutorial
/// </remarks>
public static class InMemoryDbContextFactory
{
    public static ApplicationDbContext Create(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString(), new InMemoryDatabaseRoot())
            .Options;

        return new ApplicationDbContext(options);
    }
}
