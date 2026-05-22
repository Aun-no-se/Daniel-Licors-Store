namespace Daniel_Licors_Store.Models.Product
{
    public class ProductDetailDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string Category { get; set; } = "";
        public string? Brand { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsPremium { get; set; }

        public bool IsActive { get; set; }

        // 💰 precio y stock (vienen de ProductStores)
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }
}