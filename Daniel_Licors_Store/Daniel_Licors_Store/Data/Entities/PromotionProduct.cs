namespace Daniel_Licors_Store.Data.Entities;

public class PromotionProduct
{
    public int PromotionProductId { get; set; }

    public int PromotionId { get; set; }
    public int ProductId { get; set; }

    // Precio especial directo (ej: $350 en vez de $400)
    public decimal? SpecialPrice { get; set; }

    // Descuento porcentual (ej: 5% off)
    public decimal? DiscountPercentage { get; set; }

    // Para promociones tipo "Lleva X paga Y"
    public int? RequiredQuantity { get; set; }
    public int? PayQuantity { get; set; }

    public Promotion Promotion { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
