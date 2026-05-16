using Daniel_Licors_Store.Data;
using Daniel_Licors_Store.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace Daniel_Licors_Store.Data.Services
{
    public class AdminDashboardService
    {
        private readonly ApplicationDbContext _context;

        public AdminDashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardSummary> GetSummaryAsync()
        {
            return new DashboardSummary
            {
                TotalSales = await _context.Orders.SumAsync(o => o.TotalAmount),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalCustomers = await _context.Users.CountAsync(),
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive)
            };
        }

        public async Task<ChartData> GetSalesChartAsync()
        {
            var sales = await _context.Orders
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Total = g.Sum(x => x.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .Take(7)
                .ToListAsync();

            return new ChartData
            {
                Labels = sales.Select(x => x.Date.ToString("dd/MM")).ToArray(),
                Data = new[]
                {
                    sales.Select(x => (double)x.Total).ToArray()
                }
            };
        }

        public async Task<ChartData> GetCategoryChartAsync()
        {
            var data = await _context.OrderItems
                .Include(o => o.Product)
                .ThenInclude(p => p.Category)
                .GroupBy(x => x.Product.Category.Name)
                .Select(g => new
                {
                    Category = g.Key,

                    Total = g.Sum(x => x.UnitPrice * x.Quantity)
                })
                .ToListAsync();

            return new ChartData
            {
                Labels = data.Select(x => x.Category).ToArray(),
                Data = new[]
                {
                    data.Select(x => (double)x.Total).ToArray()
                }
            };
        }
    }
}