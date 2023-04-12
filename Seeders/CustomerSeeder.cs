using System;
using System.Linq;
using aspnet_core_tutorial.Data;
using aspnet_core_tutorial.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace aspnet_core_tutorial.Seeders;

public static class CustomerSeeder
{
    public static void Seed(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            context.Database.EnsureCreated();

            var customers = context.Customers.FirstOrDefault();
            if (customers == null)
            {
                context.Customers.AddRange(
                    new Customer
                    {
                        FirstName = "Edgar",
                        LastName = "Eldy",
                        Tel = "87568899",
                        Email = "edgareldy@mailnator.com",
                        Address = "New York",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    },
                    new Customer
                    {
                        FirstName = "John",
                        LastName = "Travolta",
                        Tel = "53446477",
                        Email = "johntravolta@mailnator.com",
                        Address = "Los Angeles",
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