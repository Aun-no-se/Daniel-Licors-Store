namespace Daniel_Licors_Store.Models.Promotion;

/// <summary>DTO para la tabla de administración de promociones.</summary>
public class PromotionAdminDto
{
    public int PromotionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PromotionType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public int ProductCount { get; set; }
    public string ProductsSummary { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public bool IsVigente => IsActive && StartDate <= DateTime.Now && EndDate >= DateTime.Now;

    // ── Nuevo ──
    public List<PromotionProductLine> PromotionProducts { get; set; } = new();
}
