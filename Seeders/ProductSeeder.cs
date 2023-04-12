using System;
using System.Linq;
using aspnet_core_tutorial.Data;
using aspnet_core_tutorial.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace aspnet_core_tutorial.Seeders;

public static class ProductSeeder
{
    public static void Seed(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            context.Database.EnsureCreated();

            var products = context.Products.FirstOrDefault();
            if (products == null)
            {
                context.Products.AddRange(
                    new Product
                    {
                        CategoryId = 1,
                        ProductName = "Citron",
                        UnitPrice = 800,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    },
                    new Product()
                    {
                        CategoryId = 2,
                        ProductName = "Amstel",
                        UnitPrice = 2500,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    });

                context.SaveChanges();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}