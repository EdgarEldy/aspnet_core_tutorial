using System;
using System.ComponentModel.DataAnnotations;

namespace aspnet_core_tutorial.Models
{
    public class Order
    {
        // Constructor
        public Order()
        {
            
        }
        
        // Order properties goes here
        [Key]
        public int Id { get; set; }

        public int? CustomerId { get; set; }

        public int? ProductId { get; set; }

        public double Qty { get; set; }

        public double Total { get; set; }
        
        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}