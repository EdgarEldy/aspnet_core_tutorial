using System;
using System.Linq;
using System.Threading.Tasks;
using aspnet_core_tutorial.Data;
using aspnet_core_tutorial.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace aspnet_core_tutorial.Controllers
{
    /// <summary>
    /// Handles CRUD operations for <see cref="Customer"/> records: listing with search and
    /// pagination, creation, editing, and deletion.
    /// </summary>
    /// <remarks>
    /// Created by edgar.muhamyangabo on 7/31/26
    /// Author : edgar.muhamyangabo
    /// Date : 7/31/26
    /// Project : aspnet_core_tutorial
    /// </remarks>
    public class CustomersController : Controller
    {
        private const int PageSize = 10;

        // Initialize database context using dependency injection
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Customers
        public async Task<IActionResult> Index(string? searchString, int pageNumber = 1)
        {
            var customers = _context.Customers.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                customers = customers.Where(c =>
                    c.FirstName.Contains(searchString) || c.LastName.Contains(searchString));
            }

            ViewData["CurrentFilter"] = searchString;

            customers = customers.OrderBy(c => c.LastName).ThenBy(c => c.FirstName);
            return View(await PaginatedList<Customer>.CreateAsync(customers, pageNumber, PageSize));
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,FirstName,LastName,Telephone,Email,Address")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                var now = DateTime.UtcNow;
                customer.CreatedAt = now;
                customer.UpdatedAt = now;
                _context.Add(customer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }

        // GET: Customers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Customers == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // Check if customer exists
        private bool CustomerExists(int id)
        {
            return (_context.Customers?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        // POST: Customers/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FirstName,LastName,Telephone,Email,Address")] Customer customer)
        {
            if (id != customer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Fetch the tracked entity instead of attaching the partially-bound one directly:
                // the incoming model has no CreatedAt (excluded from [Bind] on purpose), and
                // _context.Update() on a detached entity would mark every property as modified,
                // overwriting CreatedAt with its CLR default.
                var customerToUpdate = await _context.Customers.FindAsync(id);
                if (customerToUpdate == null)
                {
                    return NotFound();
                }

                customerToUpdate.FirstName = customer.FirstName;
                customerToUpdate.LastName = customer.LastName;
                customerToUpdate.Telephone = customer.Telephone;
                customerToUpdate.Email = customer.Email;
                customerToUpdate.Address = customer.Address;
                customerToUpdate.UpdatedAt = DateTime.UtcNow;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.Id))
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

            return View(customer);
        }

        // GET: Customers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Customers == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Customers == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Customers'  is null.");
            }
            var customer = await _context.Customers.FindAsync(id);
            if (customer != null)
            {
                _context.Customers.Remove(customer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
