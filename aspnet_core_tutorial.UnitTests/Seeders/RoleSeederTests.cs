using aspnet_core_tutorial.Data;
using aspnet_core_tutorial.Seeders;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace aspnet_core_tutorial.UnitTests.Seeders;

public class RoleSeederTests
{
    private static WebApplication BuildAppWithInMemoryDb(string databaseName, string? adminEmail = null, string? adminPassword = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        builder.Services.AddIdentity<IdentityUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        if (adminEmail != null)
        {
            builder.Configuration["Admin:Email"] = adminEmail;
        }
        if (adminPassword != null)
        {
            builder.Configuration["Admin:Password"] = adminPassword;
        }

        return builder.Build();
    }

    [Fact]
    public async Task SeedAsync_Creates_Admin_And_User_Roles_When_No_Admin_Account_Configured()
    {
        using var app = BuildAppWithInMemoryDb(nameof(SeedAsync_Creates_Admin_And_User_Roles_When_No_Admin_Account_Configured));

        await RoleSeeder.SeedAsync(app);

        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.True(await roleManager.RoleExistsAsync(RoleSeeder.Admin));
        Assert.True(await roleManager.RoleExistsAsync(RoleSeeder.User));
    }

    [Fact]
    public async Task SeedAsync_Does_Not_Create_An_Admin_Account_When_Credentials_Are_Blank()
    {
        using var app = BuildAppWithInMemoryDb(nameof(SeedAsync_Does_Not_Create_An_Admin_Account_When_Credentials_Are_Blank));

        await RoleSeeder.SeedAsync(app);

        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.Empty(userManager.Users);
    }

    [Fact]
    public async Task SeedAsync_Creates_And_Promotes_The_Configured_Admin_Account()
    {
        const string adminEmail = "admin@example.com";
        const string adminPassword = "Adm1n!Pass1";
        using var app = BuildAppWithInMemoryDb(
            nameof(SeedAsync_Creates_And_Promotes_The_Configured_Admin_Account), adminEmail, adminPassword);

        await RoleSeeder.SeedAsync(app);

        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var admin = await userManager.FindByEmailAsync(adminEmail);
        Assert.NotNull(admin);
        Assert.True(admin!.EmailConfirmed);
        Assert.True(await userManager.IsInRoleAsync(admin, RoleSeeder.Admin));
    }

    [Fact]
    public async Task SeedAsync_Is_Idempotent_When_Run_Twice_With_The_Same_Configured_Admin()
    {
        // Mirrors the idempotency coverage already established for the business-entity seeders
        // (CategorySeeder, ProductSeeder, etc. in SeederTests.cs): running the seeder a second
        // time, as would happen across repeated `docker compose up` runs or app restarts against
        // an already-seeded database, must not throw or create a duplicate role/account.
        const string adminEmail = "admin@example.com";
        const string adminPassword = "Adm1n!Pass1";
        using var app = BuildAppWithInMemoryDb(
            nameof(SeedAsync_Is_Idempotent_When_Run_Twice_With_The_Same_Configured_Admin), adminEmail, adminPassword);

        await RoleSeeder.SeedAsync(app);
        await RoleSeeder.SeedAsync(app);

        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.Single(await roleManager.Roles.ToListAsync(), r => r.Name == RoleSeeder.Admin);
        Assert.Single(await roleManager.Roles.ToListAsync(), r => r.Name == RoleSeeder.User);
        Assert.Single(userManager.Users, u => u.Email == adminEmail);
    }
}
