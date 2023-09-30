using System.Threading.Tasks;
using aspnet_core_tutorial.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnet_core_tutorial.Controllers
{
    public class CategoriesController : Controller
    {
        // Initialize database context using dependency injection
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories.ToListAsync();
            return View(categories);
        }
        
        // GET: Categories/Create
        public IActionResult Create()
        {
            return View();
        }
    }
}