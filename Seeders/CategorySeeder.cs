using System;
using System.Linq;
using aspnet_core_tutorial.Data;
using aspnet_core_tutorial.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace aspnet_core_tutorial.Seeders
{
    public static class CategorySeeder
    {
        public static void Seed(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                context.Database.EnsureCreated();

                var categories = context.Categories.FirstOrDefault();
                if (categories == null)
                {
                    context.Categories.AddRange(
                        new Category
                        {
                            CategoryName = "Lemonades",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        },
                        new Category()
                        {
                            CategoryName = "Alcohols",
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
}