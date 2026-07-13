using Microsoft.EntityFrameworkCore;

namespace aspnet_core_tutorial.Models
{
    /// <summary>
    /// A page of results from an <see cref="IQueryable{T}"/>, together with the paging metadata
    /// (current page index and total page count) needed to render pagination controls.
    /// </summary>
    /// <remarks>
    /// Created by edgar.muhamyangabo on 7/13/26
    /// Author : edgar.muhamyangabo
    /// Date : 7/13/26
    /// Project : aspnet_core_tutorial
    /// </remarks>
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; }
        public int TotalPages { get; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            // Clamp so a caller-supplied pageIndex <= 0 (e.g. straight from a query string) can
            // never turn into a negative Skip(), which Postgres rejects as an invalid OFFSET.
            pageIndex = Math.Max(pageIndex, 1);

            var count = await source.CountAsync();
            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}
