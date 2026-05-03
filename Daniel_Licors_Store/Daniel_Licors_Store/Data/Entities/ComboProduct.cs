namespace Daniel_Licors_Store.Data.Entities;

public class ComboProduct
{
    public int ComboProductId { get; set; }

    public int ComboId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }

    public Combo Combo { get; set; } = null!;
    public Product Product { get; set; } = null!;
}