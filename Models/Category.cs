using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aspnet_core_tutorial.Models
{
    public class Category
    {
        // Constructor
        public Category()
        {
            this.Products = new HashSet<Product>();
        }

        // Properties goes here
        [Key]
        public int Id { get; set; }

        public string CategoryName { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Add relationship to Product Model
        public virtual ICollection<Product> Products { get; set; }
    }
}