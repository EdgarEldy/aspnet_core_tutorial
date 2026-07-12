using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aspnet_core_tutorial.Models
{
    /// <summary>
    /// Represents an order line linking an optional <see cref="Customer"/> to an optional
    /// <see cref="Product"/>, with the ordered quantity and computed total.
    /// </summary>
    /// <remarks>
    /// Created by edgar.muhamyangabo on 7/12/26
    /// Author : edgar.muhamyangabo
    /// Date : 7/12/26
    /// Project : aspnet_core_tutorial
    /// </remarks>
    [Table("Orders")]
    public class Order
    {
        [Key]
        public int Id { get; set; }

        public int? CustomerId { get; set; }

        public int? ProductId { get; set; }

        public int Quantity { get; set; }

        public double Total { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Optional relationship to the ordering Customer.
        public Customer? Customer { get; set; }

        // Optional relationship to the ordered Product.
        public Product? Product { get; set; }
    }
}
