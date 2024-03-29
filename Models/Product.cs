﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aspnet_core_tutorial.Models
{
    [Table("Products")]
    public class Product
    {
        // Constructor
        public Product()
        {
        }

        // Properties goes here
        [Key]
        public int Id { get; set; }

        public int? CategoryId { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string ProductName { get; set; }

        public double UnitPrice { get; set; }
        
        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
        
        // Add relationship to Category Model
        public Category Category { get; set; }
        
        // Add one to many relationship to Order Model
        public List<Order> Orders { get; set; }
    }
}