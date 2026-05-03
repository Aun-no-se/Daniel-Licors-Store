namespace Daniel_Licors_Store.Data.Entities;

public class Cart
{
    public int CartId { get; set; }
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
    public int StoreId { get; set; }
    public string CartStatus { get; set; } = "Active";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
    public Store Store { get; set; } = null!;

    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<CartCombo> CartCombos { get; set; } = new List<CartCombo>();
}