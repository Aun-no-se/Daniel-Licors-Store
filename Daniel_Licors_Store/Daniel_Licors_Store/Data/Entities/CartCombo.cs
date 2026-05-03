namespace Daniel_Licors_Store.Data.Entities;

public class CartCombo
{
    public int CartComboId { get; set; }
    public int CartId { get; set; }
    public int ComboId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }

    public Cart Cart { get; set; } = null!;
    public Combo Combo { get; set; } = null!;
}