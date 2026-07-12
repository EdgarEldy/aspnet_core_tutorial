using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aspnet_core_tutorial.Models
{
    /// <summary>
    /// Represents a sellable product, optionally attached to a <see cref="Category"/> and
    /// referenced by zero or more <see cref="Order"/> entities.
    /// </summary>
    /// <remarks>
    /// Created by edgar.muhamyangabo on 7/12/26
    /// Author : edgar.muhamyangabo
    /// Date : 7/12/26
    /// Project : aspnet_core_tutorial
    /// </remarks>
    [Table("Products")]
    public class Product
    {
        [Key]
        public int Id { get; set; }

        public int? CategoryId { get; set; }

        [Column(TypeName = "varchar(100)")]
        public string ProductName { get; set; } = string.Empty;

        public double UnitPrice { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Optional relationship to the owning Category.
        public Category? Category { get; set; }

        // One-to-many relationship to Order.
        public List<Order> Orders { get; set; } = new();
    }
}
