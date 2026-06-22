namespace Daniel_Licors_Store.Models.Combo;

// ══════════════════════════════════════════════════════════════
// COMBO — Modelos agrupados por dominio
// ══════════════════════════════════════════════════════════════

/// <summary>DTO para la tabla de administración de combos.</summary>
public class ComboAdminDto
{
    public int ComboId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal ComboPrice { get; set; }
    public string ImageUrl { get; set; } = "/images/combo-default.png";
    public string ProductsSummary { get; set; } = string.Empty;
}

/// <summary>DTO resumido para listas y catálogo.</summary>
public class ComboDto
{
    public int ComboId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal ComboPrice { get; set; }
    public string? ImageUrl { get; set; }
}

/// <summary>DTO completo para la página de detalle de un combo.</summary>
public class ComboDetailDto
{
    public int ComboId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal ComboPrice { get; set; }
    public string? ImageUrl { get; set; }
    public List<ComboProductDetailDto> Products { get; set; } = new();
}

/// <summary>Producto dentro del detalle de un combo (vista pública).</summary>
public class ComboProductDetailDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = "";
    public int Quantity { get; set; }
}

/// <summary>Producto dentro de un combo (DTO simplificado).</summary>
public class ComboProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
}

/// <summary>Producto temporal mientras se construye un combo en el formulario admin.</summary>
public class ComboProductTemp
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public int Quantity { get; set; }
}

/// <summary>Modelo de creación/edición de combo (formulario admin).</summary>
public class ComboCreateModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ComboPrice { get; set; }
    public string? ImageUrl { get; set; }
    public List<ComboProductTemp> Products { get; set; } = new();
}

/// <summary>
/// DTO de una promoción activa vinculada a un producto.
/// Se usa en la vista de detalle de producto para mostrar promos vigentes.
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

    // ── Helpers para la UI ──
    public bool HasDiscount => DiscountPercentage.HasValue && DiscountPercentage > 0;
    public bool HasSpecialPrice => SpecialPrice.HasValue && SpecialPrice > 0;
    public bool IsPayXGetY => RequiredQuantity.HasValue && PayQuantity.HasValue;

    public string BadgeLabel
    {
        get
        {
            if (IsPayXGetY) return $"Lleva {RequiredQuantity} Paga {PayQuantity}";
            if (HasDiscount) return $"{DiscountPercentage:0.##}% OFF";
            if (HasSpecialPrice) return "Precio Especial";
            return PromotionType ?? "Promo";
        }
    }
}
