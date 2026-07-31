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
    /// <summary>
    /// Handles CRUD operations for <see cref="Order"/> records: listing with search and
    /// pagination, creation, editing, and deletion. Enforces at the application layer that every
    /// order resolves to a real customer and product, even though those foreign keys stay
    /// nullable at the database level.
    /// </summary>
    /// <remarks>
    /// Created by edgar.muhamyangabo on 7/31/26
    /// Author : edgar.muhamyangabo
    /// Date : 7/31/26
    /// Project : aspnet_core_tutorial
    /// </remarks>
    public class OrdersController : Controller
    {
        private const int PageSize = 10;

        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Orders
        public async Task<IActionResult> Index(string? searchString, int pageNumber = 1)
        {
            var orders = _context.Orders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                orders = orders.Where(o =>
                    (o.Customer != null && (o.Customer.FirstName.Contains(searchString) || o.Customer.LastName.Contains(searchString))) ||
                    (o.Product != null && o.Product.ProductName.Contains(searchString)));
            }

            ViewData["CurrentFilter"] = searchString;

            orders = orders.OrderByDescending(o => o.CreatedAt);
            return View(await PaginatedList<Order>.CreateAsync(orders, pageNumber, PageSize));
        }

        // GET: Orders/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropDownsAsync();
            return View();
        }

        // GET: Orders/GetProducts?categoryId=5
        // Backs the Create/Edit views' cascading Category -> Product dropdown: the page calls
        // this via AJAX whenever the selected category changes, without a full page reload.
        [HttpGet]
        public async Task<JsonResult> GetProducts(int? categoryId)
        {
            var products = _context.Products.AsNoTracking().AsQueryable();
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId);
            }

            var result = await products
                .OrderBy(p => p.ProductName)
                .Select(p => new { id = p.Id, productName = p.ProductName, unitPrice = p.UnitPrice })
                .ToListAsync();
            return Json(result);
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,CustomerId,ProductId,Quantity")] Order order)
        {
            await ValidateOrderAsync(order);

            if (ModelState.IsValid)
            {
                var product = await _context.Products.FindAsync(order.ProductId);
                var now = DateTime.UtcNow;
                order.Total = order.Quantity * product!.UnitPrice;
                order.CreatedAt = now;
                order.UpdatedAt = now;
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDownsAsync(order.CustomerId, order.ProductId, await ResolveCategoryIdAsync(order.ProductId));
            return View(order);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.Include(o => o.Product).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            await PopulateDropDownsAsync(order.CustomerId, order.ProductId, order.Product?.CategoryId);
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerId,ProductId,Quantity")] Order order)
        {
            if (id != order.Id)
            {
                return NotFound();
            }

            await ValidateOrderAsync(order);

            if (ModelState.IsValid)
            {
                // Fetch the tracked entity instead of attaching the partially-bound one directly:
                // the incoming model has no CreatedAt (excluded from [Bind] on purpose), and
                // _context.Update() on a detached entity would mark every property as modified,
                // overwriting CreatedAt with its CLR default.
                var orderToUpdate = await _context.Orders.FindAsync(id);
                if (orderToUpdate == null)
                {
                    return NotFound();
                }

                var product = await _context.Products.FindAsync(order.ProductId);
                orderToUpdate.CustomerId = order.CustomerId;
                orderToUpdate.ProductId = order.ProductId;
                orderToUpdate.Quantity = order.Quantity;
                orderToUpdate.Total = order.Quantity * product!.UnitPrice;
                orderToUpdate.UpdatedAt = DateTime.UtcNow;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.Id))
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDownsAsync(order.CustomerId, order.ProductId, await ResolveCategoryIdAsync(order.ProductId));
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }

        // Enforces at the application layer that an order always resolves to a real customer and
        // product, since Order.CustomerId/ProductId stay nullable at the database level.
        private async Task ValidateOrderAsync(Order order)
        {
            if (order.CustomerId == null)
            {
                ModelState.AddModelError(nameof(Order.CustomerId), "Please select a customer.");
            }
            else if (!await _context.Customers.AnyAsync(c => c.Id == order.CustomerId))
            {
                ModelState.AddModelError(nameof(Order.CustomerId), "Selected customer no longer exists.");
            }

            if (order.ProductId == null)
            {
                ModelState.AddModelError(nameof(Order.ProductId), "Please select a product.");
            }
            else if (!await _context.Products.AnyAsync(p => p.Id == order.ProductId))
            {
                ModelState.AddModelError(nameof(Order.ProductId), "Selected product no longer exists.");
            }

            if (order.Quantity <= 0)
            {
                ModelState.AddModelError(nameof(Order.Quantity), "Quantity must be greater than zero.");
            }
        }

        // Customers/Categories/Products dropdowns for Create/Edit views. Awaited explicitly
        // (ToListAsync) rather than handing SelectList a raw IQueryable, which would enumerate it
        // synchronously and block inside these async actions.
        //
        // The Product dropdown is seeded here only for the initial server-rendered page (a
        // category already selected on Edit, or none yet on Create); once the page has loaded,
        // the Create/Edit views' own script repopulates it via GetProducts whenever the Category
        // selection changes, without a full page reload.
        private async Task PopulateDropDownsAsync(int? selectedCustomerId = null, int? selectedProductId = null, int? selectedCategoryId = null)
        {
            var customers = await _context.Customers
                .AsNoTracking()
                .OrderBy(c => c.LastName)
                .Select(c => new { c.Id, FullName = c.LastName + " " + c.FirstName })
                .ToListAsync();
            ViewData["CustomerId"] = new SelectList(customers, "Id", "FullName", selectedCustomerId);

            var categories = await _context.Categories.AsNoTracking().OrderBy(c => c.CategoryName).ToListAsync();
            ViewData["CategoryId"] = new SelectList(categories, "Id", "CategoryName", selectedCategoryId);

            var products = _context.Products.AsNoTracking().AsQueryable();
            if (selectedCategoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == selectedCategoryId);
            }
            var productList = await products.OrderBy(p => p.ProductName).ToListAsync();
            ViewData["ProductId"] = new SelectList(productList, "Id", "ProductName", selectedProductId);
        }

        // Infers the category to preselect when redisplaying an invalid form: the submitted
        // ProductId is the only signal available, since Category is a UI-only filter that never
        // gets bound onto the Order model itself.
        private async Task<int?> ResolveCategoryIdAsync(int? productId)
        {
            if (productId == null)
            {
                return null;
            }

            var product = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId);
            return product?.CategoryId;
        }
    }
}
