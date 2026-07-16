namespace Daniel_Licors_Store.Models.Admin;

// ── Producto oculto (IsActive = false) ─────────────────────────────
public class HiddenProductDto
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

// ── Combo oculto (IsActive = false) ────────────────────────────────
public class HiddenComboDto
{
    public int ComboId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal ComboPrice { get; set; }
    public int ProductCount { get; set; }
}

// ── Promoción oculta (IsActive = false) ────────────────────────────
public class HiddenPromotionDto
{
    public int PromotionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? PromotionType { get; set; }
    public DateTime EndDate { get; set; }
    public int ProductCount { get; set; }
}
