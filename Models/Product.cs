using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace aspnet_core_tutorial.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        [Column(TypeName ="varchar(255)")]
        [Required]
        public string ProductName { get; set; }
        [Required]
        public float UnitPrice { get; set; }
    }
}
