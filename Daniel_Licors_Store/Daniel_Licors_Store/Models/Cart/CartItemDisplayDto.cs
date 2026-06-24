namespace Daniel_Licors_Store.Models.Cart;

/// <summary>
/// Representa UN ITEM de producto en el carrito
/// </summary>
public class CartItemDisplayDto
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public bool IsSelected { get; set; } = false;
}

/// <summary>
/// Representa UN COMBO en el carrito
/// </summary>
public class CartComboDisplayDto
{
    public int CartComboId { get; set; }
    public int ComboId { get; set; }
    public string ComboName { get; set; } = string.Empty;
    public string? ComboImage { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public bool IsSelected { get; set; } = false;
}

/// <summary>
/// Representa UNA PROMOCIÓN en el carrito
/// </summary>
public class CartPromotionDisplayDto
{
    public int CartPromotionId { get; set; }
    public int PromotionId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public string? PromotionImage { get; set; }
    public string Category { get; set; } = "Promoción";
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public bool IsSelected { get; set; } = false;
}

/// <summary>
/// DTO principal del carrito para la UI
/// </summary>
public class CartViewDto
{
    public int CartId { get; set; }
    public List<CartItemDisplayDto> Products { get; set; } = new();
    public List<CartComboDisplayDto> Combos { get; set; } = new();
    public List<CartPromotionDisplayDto> Promotions { get; set; } = new();

    public int TotalProductsCount =>
        Products.Sum(p => p.Quantity) + Combos.Sum(c => c.Quantity) + Promotions.Sum(pr => pr.Quantity);

    public decimal TotalPrice =>
        Products.Sum(p => p.SubTotal) + Combos.Sum(c => c.SubTotal) + Promotions.Sum(pr => pr.SubTotal);

    public int SelectedProductsCount =>
        Products.Where(p => p.IsSelected).Sum(p => p.Quantity) +
        Combos.Where(c => c.IsSelected).Sum(c => c.Quantity) +
        Promotions.Where(pr => pr.IsSelected).Sum(pr => pr.Quantity);

    public decimal SelectedPrice =>
        Products.Where(p => p.IsSelected).Sum(p => p.SubTotal) +
        Combos.Where(c => c.IsSelected).Sum(c => c.SubTotal) +
        Promotions.Where(pr => pr.IsSelected).Sum(pr => pr.SubTotal);
}

public class ReceiptDto
{
    public string ReceiptNumber { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<ReceiptItemDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string CustomerName { get; set; } = "Cliente";
    public string StoreLocation { get; set; } = "";
}

/// <summary>
/// FIX: Se agregan ItemId e ItemType para que Cart.razor pueda filtrar
/// los items seleccionados al generar el recibo de "Comprar Seleccionados"
/// </summary>
public class ReceiptItemDto
{
    /// <summary>ID del CartItem o CartCombo origen</summary>
    public int ItemId { get; set; }

    /// <summary>"product" o "combo"</summary>
    public string ItemType { get; set; } = string.Empty;

    // Propiedades originales — no se modificaron
    public string ItemName { get; set; } = null!;
    public string Category { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

// ── Modelos de creación — no se modificaron ──

public class CartComboCreateModel
{
    public int ComboId { get; set; }
    public int Quantity { get; set; }
}

public class CartItemCreateModel
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class CartResponseModel
{
    public int CartId { get; set; }
    public string CartStatus { get; set; } = string.Empty;
    public decimal Total { get; set; }
}
