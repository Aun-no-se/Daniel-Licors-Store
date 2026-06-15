namespace Daniel_Licors_Store.Models.Cart;

/// <summary>
/// Representa UN ITEM de producto en el carrito
/// Si el usuario compra 3 unidades, puede ser:
/// - Un CartItem con Quantity = 3 (vista antigua)
/// - O 3 CartItems con Quantity = 1 cada uno (vista nueva)
/// </summary>
public class CartItemDisplayDto
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad de esta unidad específica
    /// En vista expandida: siempre es 1 por item individual
    /// En vista agrupada: puede ser > 1
    /// </summary>
    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    /// <summary>
    /// SubTotal = UnitPrice * Quantity
    /// </summary>
    public decimal SubTotal { get; set; }

    /// <summary>
    /// ¿Este item está seleccionado para compra?
    /// </summary>
    public bool IsSelected { get; set; } = false;
}

/// <summary>
/// Representa UN COMBO en el carrito (igual estructura que CartItemDisplayDto)
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
/// DTO para mostrar el carrito en la UI
/// los items se muestran individuales, no como contadores
/// </summary>
public class CartViewDto
{
    public int CartId { get; set; }

    /// <summary>
    /// Lista EXPANDIDA de productos individuales
    /// Ejemplo: Si hay 3 unidades del mismo producto, aquí hay 3 objetos CartItemDisplayDto diferentes
    /// cada uno con su propio CartItemId único (o agrupado por CartItemId pero quantity = 1)
    /// </summary>
    public List<CartItemDisplayDto> Products { get; set; } = new();

    /// <summary>
    /// Lista EXPANDIDA de combos individuales
    /// Igual que Products, cada combo se muestra separado
    /// </summary>
    public List<CartComboDisplayDto> Combos { get; set; } = new();

    /// <summary>
    /// Lista EXPANDIDA de promociones
    /// </summary>
    public List<CartPromotionDisplayDto> Promotions { get; set; } = new();

    /// <summary>
    /// TOTAL de unidades en el carrito (suma de quantities)
    /// </summary>
    public int TotalProductsCount => Products.Sum(p => p.Quantity) + Combos.Sum(c => c.Quantity);

    /// <summary>
    /// TOTAL de precio sin filtrar selección
    /// </summary>
    public decimal TotalPrice => Products.Sum(p => p.SubTotal) + Combos.Sum(c => c.SubTotal);

    /// <summary>
    /// Conteo de items SELECCIONADOS
    /// </summary>
    public int SelectedProductsCount => Products.Where(p => p.IsSelected).Sum(p => p.Quantity) +
                                        Combos.Where(c => c.IsSelected).Sum(c => c.Quantity);

    /// <summary>
    /// Precio total de items SELECCIONADOS
    /// </summary>
    public decimal SelectedPrice => Products.Where(p => p.IsSelected).Sum(p => p.SubTotal) +
                                    Combos.Where(c => c.IsSelected).Sum(c => c.SubTotal);
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

public class ReceiptItemDto
{
    public string ItemName { get; set; } = null!;
    public string Category { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}
