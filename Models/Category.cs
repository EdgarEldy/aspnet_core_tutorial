﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aspnet_core_tutorial.Models
{
    [Table("Categories")]
    public class Category
    {
        // Constructor
        public Category()
        {
        }

        // Properties goes here..
        [Key]
        public int Id { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string CategoryName { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Add one to many relationship to Product model
        public List<Product> Products { get; set; }
    }
}