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

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Tel { get; set; }

        public string Email { get; set; }

        public string Address { get; set; }
    }
}