namespace Daniel_Licors_Store.Data.Entities;

public class CartPromotion
{
    public int CartPromotionId { get; set; }
    public int CartId { get; set; }
    public int PromotionId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }

    public Cart Cart { get; set; } = null!;
    public Promotion Promotion { get; set; } = null!;
}