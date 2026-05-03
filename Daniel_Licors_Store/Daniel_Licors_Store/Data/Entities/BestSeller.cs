namespace Daniel_Licors_Store.Data.Entities;

public class BestSeller
{
    public int BestSellerId { get; set; }
    public int ProductId { get; set; }
    public int OrderPriority { get; set; }
    public bool IsActive { get; set; }

    public Product Product { get; set; } = null!;
}