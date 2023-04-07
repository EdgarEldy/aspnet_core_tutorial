using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace aspnet_core_tutorial.Models
{
    [Table("Categories")]
    public class Category
    {
        // Constructor
        public Category()
        {
        }

        // Properties goes here..
        [Key] 
        public int Id { get; set; }

        public string CategoryName { get; set; }
    }
}