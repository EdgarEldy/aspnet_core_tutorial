﻿using aspnet_core_tutorial.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace aspnet_core_tutorial.Data
{
    public class ApplicationDbContext: IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            :base(options)
        {

        }

        // Categories DbSet
        public DbSet<Category> Categories { get; set; }

        // Products DbSet
        public DbSet<Product> Products { get; set; }

        // Customers DbSet
        public DbSet<Customer> Customers { get; set; }
    }
}
