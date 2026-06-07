using System.ComponentModel.DataAnnotations;

namespace Daniel_Licors_Store.Models.Promotion;

public class PromotionCreateModel
{
    [Required(ErrorMessage = "El título es obligatorio")]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "La fecha de fin es obligatoria")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

    // Weekly | Highlight | Discount | PayXGetY
    public string? PromotionType { get; set; } = "Discount";

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    // Productos incluidos en la promo
    public List<PromotionProductLine> Products { get; set; } = new();
}

public class PromotionProductLine
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }

    // Llenar solo uno de los tres tipos:
    public decimal? SpecialPrice { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public int? RequiredQuantity { get; set; }
    public int? PayQuantity { get; set; }
}
