namespace Daniel_Licors_Store.Models.Admin
{
    public class DashboardSummary
    {
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalProducts { get; set; }
    }

    public class ChartData
    {
        public string[] Labels { get; set; } = [];
        public double[][] Data { get; set; } = [];
    }
}
