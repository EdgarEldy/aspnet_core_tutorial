using aspnet_core_tutorial.Data;
using Microsoft.AspNetCore.Mvc;

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

        // GET
        public IActionResult Index()
        {
            return View();
        }
    }
}