namespace Daniel_Licors_Store.Models.Cart;

public class CartItemDisplayDto
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? ProductImage { get; set; }
    public string Category { get; set; } = "Producto";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public bool IsSelected { get; set; }
    public int Stock { get; set; }
}

public class CartComboDisplayDto
{
    public int CartComboId { get; set; }
    public int ComboId { get; set; }
    public string ComboName { get; set; } = null!;
    public string? ComboImage { get; set; }
    public string Category { get; set; } = "Combo";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public bool IsSelected { get; set; }
}

public class CartPromotionDisplayDto
{
    public int CartPromotionId { get; set; }
    public int PromotionId { get; set; }
    public string PromotionTitle { get; set; } = null!;
    public string? PromotionImage { get; set; }
    public string Category { get; set; } = "Promoción";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
    public bool IsSelected { get; set; }
}

public class CartViewDto
{
    public int CartId { get; set; }
    public List<CartItemDisplayDto> Products { get; set; } = new();
    public List<CartComboDisplayDto> Combos { get; set; } = new();
    public List<CartPromotionDisplayDto> Promotions { get; set; } = new();

    // Contadores
    public int TotalProductsCount => Products.Sum(p => p.Quantity) + Combos.Sum(c => c.Quantity) + Promotions.Sum(p => p.Quantity);
    public int SelectedProductsCount => Products.Where(p => p.IsSelected).Sum(p => p.Quantity) + 
                                        Combos.Where(c => c.IsSelected).Sum(c => c.Quantity) + 
                                        Promotions.Where(p => p.IsSelected).Sum(p => p.Quantity);

    public decimal TotalPrice => Products.Sum(p => p.SubTotal) + Combos.Sum(c => c.SubTotal) + Promotions.Sum(p => p.SubTotal);
    public decimal SelectedPrice => Products.Where(p => p.IsSelected).Sum(p => p.SubTotal) + 
                                   Combos.Where(c => c.IsSelected).Sum(c => c.SubTotal) + 
                                   Promotions.Where(p => p.IsSelected).Sum(p => p.SubTotal);
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
