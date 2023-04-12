using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aspnet_core_tutorial.Models
{
    [Table("Orders")]
    public class Order
    {
        // Constructor
        public Order()
        {
        }
        
        // Properties goes here..
        [Key]
        public int Id { get; set; }

        public int? CustomerId { get; set; }

        public int? ProductId { get; set; }

        public int Quantity { get; set; }

        public double Total { get; set; }
        
        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
        
        // Add relationship to Cutomer model
        public Customer Customer { get; set; }

        // Add relationship to Product model
        public Product Product { get; set; }   
    }
}