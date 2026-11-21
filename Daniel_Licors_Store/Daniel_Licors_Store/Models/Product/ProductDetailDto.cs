namespace Daniel_Licors_Store.Models.Product
{
    public class ProductDetailDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string Category { get; set; } = "";
        public string? Brand { get; set; }
        public string? Size { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsPremium { get; set; }

        public bool IsActive { get; set; }

        // 💰 precio y stock (vienen de ProductStores)
        public decimal Price { get; set; }
        public int Stock { get; set; }

        /// <summary>Nombre con tamaño, para textos y listas: "Corona (355 ml)".</summary>
        public string DisplayName => ProductNaming.Compose(Name, Size);

        /// <summary>Los demás tamaños de esta misma bebida (incluye este). Vacío si solo hay uno.</summary>
        public List<ProductSizeVariantDto> Variants { get; set; } = new();
    }

    /// <summary>Un tamaño de la misma bebida, con su precio y stock.</summary>
    public class ProductSizeVariantDto
    {
        public int ProductId { get; set; }
        public string? Size { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsCurrent { get; set; }
    }
}