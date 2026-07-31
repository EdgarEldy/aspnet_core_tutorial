using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace aspnet_core_tutorial.Seeders
{
    /// <summary>
    /// Ensures the "Admin" and "User" Identity roles exist, and seeds a default Admin account
    /// from configuration if one hasn't been created yet.
    /// </summary>
    /// <remarks>
    /// Created by edgar.muhamyangabo on 7/31/26
    /// Author : edgar.muhamyangabo
    /// Date : 7/31/26
    /// Project : aspnet_core_tutorial
    /// </remarks>
    public static class RoleSeeder
    {
        public const string Admin = "Admin";
        public const string User = "User";

        public static async Task SeedAsync(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            try
            {
                foreach (var role in new[] { Admin, User })
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                    }
                }

                var adminEmail = configuration["Admin:Email"];
                var adminPassword = configuration["Admin:Password"];
                if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                {
                    // No admin credentials configured (e.g. running without .env set up). Skip
                    // rather than hardcoding a fallback account; an operator can register
                    // normally and be promoted to Admin manually instead.
                    return;
                }

                var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
                if (existingAdmin == null)
                {
                    var admin = new IdentityUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(admin, adminPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, Admin);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
