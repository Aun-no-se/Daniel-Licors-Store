namespace Daniel_Licors_Store.Models.Cart;

public class CartResponseModel
{
    public int CartId { get; set; }
    public string CartStatus { get; set; } = string.Empty;
    public decimal Total { get; set; }
}