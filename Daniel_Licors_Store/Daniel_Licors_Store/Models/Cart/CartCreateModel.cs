namespace Daniel_Licors_Store.Models.Cart;

public class CartCreateModel
{
    public int StoreId { get; set; }
    public string? UserId { get; set; }
    public string? SessionId { get; set; }
}