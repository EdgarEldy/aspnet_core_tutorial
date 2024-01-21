using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aspnet_core_tutorial.Models
{
    [Table("Customers")]
    public class Customer
    {
        // Constructor
        public Customer()
        {
        }

        // Properties goes here..
        [Key]
        public int Id { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string FirstName { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string LastName { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Tel { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Email { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string Address { get; set; }
        
        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
        
        // Add one to many relationship to Order Model
        public List<Order> Orders { get; set; }
    }
}