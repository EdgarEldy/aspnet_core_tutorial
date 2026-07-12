using System;
using System.Linq;
using aspnet_core_tutorial.Data;
using aspnet_core_tutorial.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace aspnet_core_tutorial.Seeders;

public static class OrderSeeder
{
    public static void Seed(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            context.Database.EnsureCreated();

            var orders = context.Orders.FirstOrDefault();
            if (orders == null)
            {
                context.Orders.AddRange(
                    new Order
                    {
                        CustomerId = 1,
                        ProductId = 1,
                        Quantity = 2,
                        Total = 1600,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    },
                    new Order
                    {
                        CustomerId = 2,
                        ProductId = 2,
                        Quantity = 2,
                        Total = 5000,
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