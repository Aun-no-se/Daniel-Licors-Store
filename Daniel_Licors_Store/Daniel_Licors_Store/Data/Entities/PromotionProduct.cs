namespace Daniel_Licors_Store.Data.Entities;

public class PromotionProduct
{
    public int PromotionProductId { get; set; }

    public int PromotionId { get; set; }
    public int ProductId { get; set; }

    public decimal? SpecialPrice { get; set; }

    public Promotion Promotion { get; set; } = null!;
    public Product Product { get; set; } = null!;
}