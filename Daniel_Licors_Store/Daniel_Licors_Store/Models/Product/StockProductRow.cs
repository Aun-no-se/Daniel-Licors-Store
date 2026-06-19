namespace Daniel_Licors_Store.Models.Product
{

        public class StockProductRow
        {
            public int ProductId { get; set; }
            public string Name { get; set; } = "";
            public string? ImageUrl { get; set; }
            public string Category { get; set; } = "";
            public int CategoryId { get; set; }
            public string? Brand { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public bool IsAvailable { get; set; }
            public bool IsPremium { get; set; }
            public int? AgingYears { get; set; }
        }

        public class StockPromoRow
        {
            public int PromotionId { get; set; }
            public string Title { get; set; } = "";
            public string? Description { get; set; }
            public string PromotionType { get; set; } = "";
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public bool IsActive { get; set; }
            public List<StockPromoProductRow> Products { get; set; } = new();
        }

        public class StockPromoProductRow
        {
            public string Name { get; set; } = "";
            public decimal? SpecialPrice { get; set; }
            public string? ImageUrl { get; set; }
        }

        public class StockComboRow
        {
            public int ComboId { get; set; }
            public string Name { get; set; } = "";
            public string? Description { get; set; }
            public decimal ComboPrice { get; set; }
            public string? ImageUrl { get; set; }
            public DateTime? EndDate { get; set; }
            public bool IsActive { get; set; }
            public List<string> ProductNames { get; set; } = new();
        }

        public class StockSummary
        {
            public int TotalProductos { get; set; }
            public int TotalUnidades { get; set; }
            public int ProductosSinStock { get; set; }
            public int TotalCombos { get; set; }
            public int TotalPromociones { get; set; }
            public int CategoriasActivas { get; set; }
        }
}
