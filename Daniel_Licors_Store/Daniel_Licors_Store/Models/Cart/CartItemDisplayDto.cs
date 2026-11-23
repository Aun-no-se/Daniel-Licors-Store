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

    /// <summary>Unidades que el negocio tiene hoy disponibles de este producto (informativo para la UI).</summary>
    public int AvailableStock { get; set; }
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

    /// <summary>Cuántos combos completos se pueden armar hoy con el stock real.</summary>
    public int AvailableStock { get; set; }
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

    /// <summary>Máximo de unidades de esta promoción que el stock real permite hoy.</summary>
    public int AvailableStock { get; set; }
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

    /// <summary>
    /// Avisos para el cliente cuando el carrito se corrigió solo (se agotó algo,
    /// bajó el stock, cambió un precio, etc.). La UI los muestra y listo.
    /// </summary>
    public List<string> Adjustments { get; set; } = new();

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

    /// <summary>Correo con el que el cliente se registró; ahí llega el recibo.</summary>
    public string CustomerEmail { get; set; } = string.Empty;

    public string StoreLocation { get; set; } = "";

    /// <summary>Total de unidades compradas (suma de las cantidades).</summary>
    public int TotalUnits => Items?.Sum(i => i.Quantity) ?? 0;
}

/// <summary>Lo que el cliente eligió en el recibo antes de confirmar.</summary>
public class ReceiptOptionsDto
{
    public bool SendWhatsApp { get; set; }
    public string Phone { get; set; } = string.Empty;
}

/// <summary>Resultado de confirmar la compra (lo muestra el recibo).</summary>
public class PurchaseOutcomeDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool EmailSent { get; set; }
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>Link de WhatsApp con el recibo listo (solo si el cliente lo pidió).</summary>
    public string? WhatsAppLink { get; set; }
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

/// <summary>
/// Qué líneas del carrito se van a comprar. Se identifican por el Id de cada línea
/// (CartItemId / CartComboId / CartPromotionId). Si se pasa null, se compra todo el carrito.
/// </summary>
public class CheckoutSelectionDto
{
    public HashSet<int> ProductItemIds { get; set; } = new();
    public HashSet<int> ComboItemIds { get; set; } = new();
    public HashSet<int> PromotionItemIds { get; set; } = new();

    public bool IsEmpty => ProductItemIds.Count == 0 && ComboItemIds.Count == 0 && PromotionItemIds.Count == 0;
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
