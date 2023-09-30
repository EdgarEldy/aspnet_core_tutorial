using Microsoft.AspNetCore.Mvc;

namespace aspnet_core_tutorial.Controllers
{
    public class ProductsController : Controller
    {
        // GET
        public IActionResult Index()
        {
            return View();
        }
    }
}