using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace aspnet_core_tutorial.Models
{
    public class Order
    {
        [Key]
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int ProductId { get; set; }
        public int Qty { get; set; }
        public float Total { get; set; }

        public Customer Customer { get; set; }
        public Product Product { get; set; }
    }
}
