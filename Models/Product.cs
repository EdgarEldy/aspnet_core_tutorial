﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aspnet_core_tutorial.Models
{
    public class Product
    {
        // Constructor
        public Product()
        {
            this.Orders = new HashSet<Order>();
        }
        
        // Product properties goes here..
        [Key]
        public int Id { get; set; }

        public int? CategoryId { get; set; }

        public string ProductName { get; set; }

        public string UnitPrice { get; set; }
        
        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Add relationship to Category Model
        public virtual Category Category { get; set; }

        // Add relationship to Order Model
        public virtual ICollection<Order> Orders { get; set; }
    }
}