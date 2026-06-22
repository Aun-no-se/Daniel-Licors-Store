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
            var now = DateTime.Now;
            var startOfThisMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfThisMonth.AddMonths(-1);

            var totalSales = await _context.Orders
                .Select(o => (decimal?)o.TotalAmount)
                .SumAsync() ?? 0m;

            var salesThisMonth = await _context.Orders
                .Where(o => o.CreatedAt >= startOfThisMonth)
                .Select(o => (decimal?)o.TotalAmount)
                .SumAsync() ?? 0m;

            var salesLastMonth = await _context.Orders
                .Where(o => o.CreatedAt >= startOfLastMonth && o.CreatedAt < startOfThisMonth)
                .Select(o => (decimal?)o.TotalAmount)
                .SumAsync() ?? 0m;

            var ordersThisMonth = await _context.Orders
                .CountAsync(o => o.CreatedAt >= startOfThisMonth);

            var ordersLastMonth = await _context.Orders
                .CountAsync(o => o.CreatedAt >= startOfLastMonth && o.CreatedAt < startOfThisMonth);

            decimal growthPercent = 0;
            if (salesLastMonth > 0)
                growthPercent = Math.Round(((salesThisMonth - salesLastMonth) / salesLastMonth) * 100, 1);

            var lowStock = await _context.ProductStores
                .CountAsync(ps => ps.Stock > 0 && ps.Stock <= 5 && ps.IsAvailable);

            var outOfStock = await _context.ProductStores
                .CountAsync(ps => ps.Stock == 0 && ps.IsAvailable);

            return new DashboardSummary
            {
                TotalSales = totalSales,
                TotalOrders = await _context.Orders.CountAsync(),
                TotalCustomers = await _context.Users.CountAsync(u => u.IsActive),
                TotalProducts = await _context.Products.CountAsync(p => p.IsActive),
                SalesThisMonth = salesThisMonth,
                SalesLastMonth = salesLastMonth,
                OrdersThisMonth = ordersThisMonth,
                OrdersLastMonth = ordersLastMonth,
                SalesGrowthPercent = growthPercent,
                OrdersGrowthCount = ordersThisMonth - ordersLastMonth,
                OrdersPending = await _context.Orders.CountAsync(o => o.OrderStatus == "Pending"),
                OrdersInProcess = await _context.Orders.CountAsync(o => o.OrderStatus == "InProcess"),
                OrdersCompleted = await _context.Orders.CountAsync(o => o.OrderStatus == "Completed"),
                LowStockProducts = lowStock,
                OutOfStockProducts = outOfStock
            };
        }

        public async Task<ChartData> GetSalesChartAsync(int days = 30)
        {
            var since = DateTime.Now.AddDays(-days);
            var sales = await _context.Orders
                .Where(o => o.CreatedAt >= since)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Total = g.Sum(x => x.TotalAmount),
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return new ChartData
            {
                Labels = sales.Select(x => x.Date.ToString("dd/MM")).ToArray(),
                Data = new[]
                {
                    sales.Select(x => (double)x.Total).ToArray()
                },
                ChartSeries = new[] { "Ventas (RD$)" }
            };
        }

        public async Task<ChartData> GetCategoryChartAsync()
        {
            var raw = await _context.OrderItems
                .Include(o => o.Product)
                    .ThenInclude(p => p.Category)
                .Where(x => x.Product != null && x.Product.Category != null)
                .Select(x => new
                {
                    CategoryName = x.Product.Category.Name,
                    Total = (double)(x.UnitPrice * x.Quantity)
                })
                .ToListAsync();

            var data = raw
                .GroupBy(x => x.CategoryName)
                .Select(g => new { Category = g.Key, Total = g.Sum(x => x.Total) })
                .OrderByDescending(x => x.Total)
                .Take(6)
                .ToList();

            return new ChartData
            {
                Labels = data.Select(x => x.Category).ToArray(),
                Data = new[]
                {
                    data.Select(x => x.Total).ToArray()
                }
            };
        }

        public async Task<List<TopProductDto>> GetTopProductsAsync(int top = 10)
        {
            // Proyectar primero a SQL, luego agrupar en memoria para evitar
            // el error de traducción de SubTotal en GroupBy complejo
            var raw = await _context.OrderItems
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                .Where(oi => oi.Product != null)
                .Select(oi => new
                {
                    oi.ProductId,
                    ProductName = oi.Product.Name,
                    CategoryName = oi.Product.Category != null ? oi.Product.Category.Name : "Sin categoría",
                    oi.Quantity,
                    Revenue = oi.UnitPrice * oi.Quantity
                })
                .ToListAsync();

            return raw
                .GroupBy(x => new { x.ProductId, x.ProductName, x.CategoryName })
                .Select(g => new TopProductDto
                {
                    ProductName = g.Key.ProductName,
                    CategoryName = g.Key.CategoryName,
                    TotalSold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Revenue)
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(top)
                .ToList();
        }

        public async Task<List<RecentOrderDto>> GetRecentOrdersAsync(int count = 10)
        {
            return await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(count)
                .Select(o => new RecentOrderDto
                {
                    OrderId = o.OrderId,
                    CustomerName = o.User != null ? o.User.FullName + " " + o.User.LastName : "Cliente anónimo",
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    PaymentMethod = o.PaymentMethod,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<List<EmployeeActivityDto>> GetEmployeeActivityAsync()
        {
            var employees = await _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.Name == "Empleado" || ur.Role.Name == "Admin")
                .Select(ur => new
                {
                    ur.User.UserId,
                    ur.User.FullName,
                    ur.User.Email,
                    ur.User.IsActive,
                    RoleName = ur.Role.Name
                })
                .ToListAsync();

            var result = new List<EmployeeActivityDto>();

            foreach (var emp in employees)
            {
                var lastLog = await _context.AuditLogs
                    .Where(a => a.UserId == emp.UserId)
                    .OrderByDescending(a => a.CreatedAt)
                    .FirstOrDefaultAsync();

                var totalActions = await _context.AuditLogs
                    .CountAsync(a => a.UserId == emp.UserId);

                result.Add(new EmployeeActivityDto
                {
                    UserId = emp.UserId,
                    FullName = emp.FullName,
                    Email = emp.Email,
                    RoleName = emp.RoleName,
                    IsActive = emp.IsActive,
                    TotalActions = totalActions,
                    LastAction = lastLog?.Action,
                    LastEntity = lastLog?.Entity,
                    LastActivityAt = lastLog?.CreatedAt
                });
            }

            return result.OrderByDescending(e => e.LastActivityAt ?? DateTime.MinValue).ToList();
        }

        public async Task<ChartData> GetWeeklySalesComparisonAsync()
        {
            var thisWeekStart = DateTime.Now.AddDays(-(int)DateTime.Now.DayOfWeek);
            var lastWeekStart = thisWeekStart.AddDays(-7);

            var days = new[] { "Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb" };

            var thisWeek = await _context.Orders
                .Where(o => o.CreatedAt >= thisWeekStart)
                .GroupBy(o => (int)o.CreatedAt.DayOfWeek)
                .Select(g => new { Day = g.Key, Total = g.Sum(x => (double)x.TotalAmount) })
                .ToListAsync();

            var lastWeek = await _context.Orders
                .Where(o => o.CreatedAt >= lastWeekStart && o.CreatedAt < thisWeekStart)
                .GroupBy(o => (int)o.CreatedAt.DayOfWeek)
                .Select(g => new { Day = g.Key, Total = g.Sum(x => (double)x.TotalAmount) })
                .ToListAsync();

            return new ChartData
            {
                Labels = days,
                Data = new[]
                {
                    Enumerable.Range(0, 7).Select(i => thisWeek.FirstOrDefault(x => x.Day == i)?.Total ?? 0).ToArray(),
                    Enumerable.Range(0, 7).Select(i => lastWeek.FirstOrDefault(x => x.Day == i)?.Total ?? 0).ToArray()
                },
                ChartSeries = new[] { "Esta semana", "Semana anterior" }
            };
        }
    }
}
