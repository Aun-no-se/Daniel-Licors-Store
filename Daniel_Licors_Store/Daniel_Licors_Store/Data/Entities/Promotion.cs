namespace Daniel_Licors_Store.Data.Entities;

public class Promotion
{
    public int PromotionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? PromotionType { get; set; } // Weekly, Combo, Highlight
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }

    public ICollection<PromotionProduct> PromotionProducts { get; set; } = new List<PromotionProduct>();
}
