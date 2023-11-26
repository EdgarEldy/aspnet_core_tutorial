using System.Threading.Tasks;
using aspnet_core_tutorial.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnet_core_tutorial.Controllers
{
    public class ProductsController : Controller
    {
        // Initialize database context
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index()
        {
            // Get products with categories
            var products = _context.Products.Include(p => p.Category);

            return View(await products.ToListAsync());
        }
    }
}