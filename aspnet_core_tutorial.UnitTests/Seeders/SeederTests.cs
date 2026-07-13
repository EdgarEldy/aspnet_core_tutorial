using aspnet_core_tutorial.Data;
using aspnet_core_tutorial.Seeders;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace aspnet_core_tutorial.UnitTests.Seeders;

public class SeederTests
{
    private static WebApplication BuildAppWithInMemoryDb(string databaseName)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        return builder.Build();
    }

    [Fact]
    public void CategorySeeder_Seed_Inserts_Categories_On_Empty_Database()
    {
        using var app = BuildAppWithInMemoryDb(nameof(CategorySeeder_Seed_Inserts_Categories_On_Empty_Database));

        CategorySeeder.Seed(app);

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, context.Categories.Count());
        Assert.All(context.Categories, c => Assert.NotEqual(default, c.CreatedAt));
    }

    [Fact]
    public void CategorySeeder_Seed_Is_Idempotent_When_Run_Twice()
    {
        using var app = BuildAppWithInMemoryDb(nameof(CategorySeeder_Seed_Is_Idempotent_When_Run_Twice));

        CategorySeeder.Seed(app);
        CategorySeeder.Seed(app);

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, context.Categories.Count());
    }

    [Fact]
    public void ProductSeeder_Seed_Inserts_Products_On_Empty_Database()
    {
        using var app = BuildAppWithInMemoryDb(nameof(ProductSeeder_Seed_Inserts_Products_On_Empty_Database));

        ProductSeeder.Seed(app);

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, context.Products.Count());
    }

    [Fact]
    public void ProductSeeder_Seed_Is_Idempotent_When_Run_Twice()
    {
        using var app = BuildAppWithInMemoryDb(nameof(ProductSeeder_Seed_Is_Idempotent_When_Run_Twice));

        ProductSeeder.Seed(app);
        ProductSeeder.Seed(app);

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, context.Products.Count());
    }

    [Fact]
    public void CustomerSeeder_Seed_Inserts_Customers_On_Empty_Database()
    {
        using var app = BuildAppWithInMemoryDb(nameof(CustomerSeeder_Seed_Inserts_Customers_On_Empty_Database));

        CustomerSeeder.Seed(app);

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, context.Customers.Count());
    }

    [Fact]
    public void CustomerSeeder_Seed_Is_Idempotent_When_Run_Twice()
    {
        using var app = BuildAppWithInMemoryDb(nameof(CustomerSeeder_Seed_Is_Idempotent_When_Run_Twice));

        CustomerSeeder.Seed(app);
        CustomerSeeder.Seed(app);

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, context.Customers.Count());
    }

    [Fact]
    public void OrderSeeder_Seed_Inserts_Orders_On_Empty_Database()
    {
        using var app = BuildAppWithInMemoryDb(nameof(OrderSeeder_Seed_Inserts_Orders_On_Empty_Database));

        OrderSeeder.Seed(app);

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, context.Orders.Count());
    }

    [Fact]
    public void OrderSeeder_Seed_Is_Idempotent_When_Run_Twice()
    {
        using var app = BuildAppWithInMemoryDb(nameof(OrderSeeder_Seed_Is_Idempotent_When_Run_Twice));

        OrderSeeder.Seed(app);
        OrderSeeder.Seed(app);

        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(2, context.Orders.Count());
    }
}
