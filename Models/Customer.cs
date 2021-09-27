using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace aspnet_core_tutorial.Models
{
    public class Customer
    {
        [Key]
        public int id { get; set; }
        [Column(TypeName = "varchar(255)")]
        [Required]
        public string FirstName { get; set; }
        [Column(TypeName = "varchar(255)")]
        [Required]
        public string LastName { get; set; }
        [Column(TypeName = "varchar(255)")]
        [Required]
        public string Email { get; set; }
        [Column(TypeName = "varchar(255)")]
        [Required]
        public string Tel { get; set; }
        [Column(TypeName = "varchar(255)")]
        [Required]
        public string Address { get; set; }

        public string Pays { get; set; }
    }
}
