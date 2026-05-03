namespace Daniel_Licors_Store.Data.Entities;

public class CartItem
{
    public int CartItemId { get; set; }
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }

    public Cart Cart { get; set; } = null!;
    public Product Product { get; set; } = null!;
}