using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aspnet_core_tutorial.Models
{
    [Table("Products")]
    public class Product
    {
        // Constructor
        public Product()
        {
        }

        // Properties goes here
        [Key]
        public int Id { get; set; }

        public int? CategoryId { get; set; }

        public string ProductName { get; set; }

        public double UnitPrice { get; set; }
    }
}