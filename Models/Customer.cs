using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aspnet_core_tutorial.Models
{
    /// <summary>
    /// Represents a customer placing orders. Holds contact details and the one-to-many
    /// relationship to their <see cref="Order"/> history.
    /// </summary>
    /// <remarks>
    /// Created by edgar.muhamyangabo on 7/12/26
    /// Author : edgar.muhamyangabo
    /// Date : 7/12/26
    /// Project : aspnet_core_tutorial
    /// </remarks>
    [Table("Customers")]
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Column(TypeName = "varchar(255)")]
        public string FirstName { get; set; } = string.Empty;

        [Column(TypeName = "varchar(255)")]
        public string LastName { get; set; } = string.Empty;

        [Column(TypeName = "varchar(50)")]
        public string Telephone { get; set; } = string.Empty;

        [Column(TypeName = "varchar(255)")]
        public string Email { get; set; } = string.Empty;

        [Column(TypeName = "varchar(255)")]
        public string Address { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // One-to-many relationship to Order.
        public List<Order> Orders { get; set; } = new();
    }
}
