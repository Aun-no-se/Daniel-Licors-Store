namespace Daniel_Licors_Store.Data.Entities;

public class Cart
{
    public int CartId { get; set; }
    public string UserId { get; set; } = string.Empty;  // 
    public string? SessionId { get; set; }
    public int StoreId { get; set; }
    public string CartStatus { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Store Store { get; set; } = null!;  // Solo Store, no User
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<CartCombo> CartCombos { get; set; } = new List<CartCombo>();

    //  Promociones en carrito
    public ICollection<CartPromotion> CartPromotions { get; set; } = new List<CartPromotion>();
}
