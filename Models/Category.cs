using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace aspnet_core_tutorial.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }
        [Column(TypeName = "varchar(45)")]
        [Required]
        public string category_name { get; set; }

        public List<Product> Products { get; set; }
    }
}
