namespace Daniel_Licors_Store.Models.Admin
{
    public class DashboardSummary
    {
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }

        // Comparativas con mes anterior
        public decimal SalesGrowthPercent { get; set; }
        public int OrdersGrowthCount { get; set; }

        // Desglose del mes actual
        public decimal SalesThisMonth { get; set; }
        public decimal SalesLastMonth { get; set; }
        public int OrdersThisMonth { get; set; }
        public int OrdersLastMonth { get; set; }

        // Pedidos por estado
        public int OrdersPending { get; set; }
        public int OrdersInProcess { get; set; }
        public int OrdersCompleted { get; set; }

        // Stock
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
    }

    public class ChartData
    {
        public string[] Labels { get; set; } = [];
        public double[][] Data { get; set; } = [];
        public string[] ChartSeries { get; set; } = [];
    }

    public class TopProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class RecentOrderDto
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string OrderStatus { get; set; } = string.Empty;
        public string? PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class EmployeeActivityDto
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int TotalActions { get; set; }
        public string? LastAction { get; set; }
        public string? LastEntity { get; set; }
        public DateTime? LastActivityAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class SalesByDayDto
    {
        public DateTime Date { get; set; }
        public decimal Total { get; set; }
        public int OrderCount { get; set; }
    }
}
