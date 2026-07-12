using aspnet_core_tutorial.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace aspnet_core_tutorial.Data
{
    /// <summary>
    /// EF Core database context combining ASP.NET Core Identity's schema with the app's
    /// business entities (categories, products, customers, orders), targeting PostgreSQL
    /// via the Npgsql provider.
    /// </summary>
    /// <remarks>
    /// Created by edgar.muhamyangabo on 7/12/26
    /// Author : edgar.muhamyangabo
    /// Date : 7/12/26
    /// Project : aspnet_core_tutorial
    /// </remarks>
    public class ApplicationDbContext: IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            :base(options)
        {

        }

        public DbSet<Category> Categories { get; set; } = null!;

        public DbSet<Product> Products { get; set; } = null!;

        public DbSet<Customer> Customers { get; set; } = null!;

        public DbSet<Order> Orders { get; set; } = null!;
    }
}
