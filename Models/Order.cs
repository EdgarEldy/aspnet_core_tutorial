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
        
        // Add relationship to Cutomer model
        public Customer Customer { get; set; }
    }
}