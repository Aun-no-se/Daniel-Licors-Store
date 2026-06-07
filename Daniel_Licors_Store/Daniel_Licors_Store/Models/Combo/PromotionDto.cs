namespace Daniel_Licors_Store.Models.Combo;

/// <summary>
/// DTO que representa una promoción activa vinculada a un producto,
/// con toda la info necesaria para mostrarla en el detalle del producto.
/// </summary>
public class PromotionDto
{
    public int PromotionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PromotionType { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Precio especial para este producto en la promo
    public decimal? SpecialPrice { get; set; }

    // % de descuento (ej: 5.00 = 5%)
    public decimal? DiscountPercentage { get; set; }

    // Lleva X paga Y
    public int? RequiredQuantity { get; set; }
    public int? PayQuantity { get; set; }

    // Helpers para la UI
    public bool HasDiscount => DiscountPercentage.HasValue && DiscountPercentage > 0;
    public bool HasSpecialPrice => SpecialPrice.HasValue && SpecialPrice > 0;
    public bool IsPayXGetY => RequiredQuantity.HasValue && PayQuantity.HasValue;

    public string BadgeLabel
    {
        get
        {
            if (IsPayXGetY)
                return $"Lleva {RequiredQuantity} Paga {PayQuantity}";
            if (HasDiscount)
                return $"{DiscountPercentage:0.##}% OFF";
            if (HasSpecialPrice)
                return "Precio Especial";
            return PromotionType ?? "Promo";
        }
    }
}
