using System.Threading.Tasks;
using aspnet_core_tutorial.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        // GET: Products/Create
        public IActionResult Create()
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "CategoryName");
            return View();
        }
    }
}