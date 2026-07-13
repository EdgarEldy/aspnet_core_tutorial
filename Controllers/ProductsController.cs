using System;
using System.Linq;
using System.Threading.Tasks;
using aspnet_core_tutorial.Data;
using aspnet_core_tutorial.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace aspnet_core_tutorial.Controllers
{
    public class ProductsController : Controller
    {
        private const int PageSize = 10;

        // Initialize database context
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Products
        public async Task<IActionResult> Index(string? searchString, int pageNumber = 1)
        {
            // Get products with categories
            var products = _context.Products.AsNoTracking().Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                products = products.Where(p => p.ProductName.Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;

            products = products.OrderBy(p => p.ProductName);
            return View(await PaginatedList<Product>.CreateAsync(products, pageNumber, PageSize));
        }

        // GET: Products/Create
        public async Task<IActionResult> Create()
        {
            await PopulateCategoryDropDownAsync();
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CategoryId,ProductName,UnitPrice")] Product product)
        {
            if (ModelState.IsValid)
            {
                var now = DateTime.UtcNow;
                product.CreatedAt = now;
                product.UpdatedAt = now;
                _context.Add(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateCategoryDropDownAsync(product.CategoryId);
            return View(product);
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Products == null)
            {
                return NotFound();
            }

            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            await PopulateCategoryDropDownAsync(product.CategoryId);
            return View(product);
        }

        // Check if product exists
        private bool ProductExists(int id)
        {
            return (_context.Products?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        // Categories dropdown for Create/Edit views. Awaited explicitly (ToListAsync) rather than
        // handing SelectList a raw IQueryable, which would enumerate it synchronously and block
        // inside these async actions.
        private async Task PopulateCategoryDropDownAsync(int? selectedCategoryId = null)
        {
            var categories = await _context.Categories.AsNoTracking().ToListAsync();
            ViewData["CategoryId"] = new SelectList(categories, "Id", "CategoryName", selectedCategoryId);
        }

        // POST: Products/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CategoryId,ProductName,UnitPrice")] Product product)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Fetch the tracked entity instead of attaching the partially-bound one directly:
                // the incoming model has no CreatedAt (excluded from [Bind] on purpose), and
                // _context.Update() on a detached entity would mark every property as modified,
                // overwriting CreatedAt with its CLR default.
                var productToUpdate = await _context.Products.FindAsync(id);
                if (productToUpdate == null)
                {
                    return NotFound();
                }

                productToUpdate.CategoryId = product.CategoryId;
                productToUpdate.ProductName = product.ProductName;
                productToUpdate.UnitPrice = product.UnitPrice;
                productToUpdate.UpdatedAt = DateTime.UtcNow;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateCategoryDropDownAsync(product.CategoryId);
            return View(product);
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Products == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Products == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Products'  is null.");
            }
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}