using System;
using System.ComponentModel.DataAnnotations;

namespace aspnet_core_tutorial.Models
{
    public class Category
    {
        // Constructor
        public Category()
        {
            
        }

        // Properties goes here
        [Key]
        public int Id { get; set; }

        public string CategoryName { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}