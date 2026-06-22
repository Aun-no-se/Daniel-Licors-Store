using System.ComponentModel.DataAnnotations;

namespace Daniel_Licors_Store.Models.Promotion;

// ══════════════════════════════════════════════════════════════
// PROMOTION — Modelos agrupados por dominio
// ══════════════════════════════════════════════════════════════

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

    public List<PromotionProductLine> PromotionProducts { get; set; } = new();
}

/// <summary>Modelo de creación/edición de promoción (formulario admin).</summary>
public class PromotionCreateModel
{
    [Required(ErrorMessage = "El título es obligatorio")]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Required]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

    // Discount | SpecialPrice | PayXGetY
    public string PromotionType { get; set; } = "Discount";

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public List<PromotionProductLine> Products { get; set; } = new();
}

/// <summary>Línea de producto dentro de una promoción (formulario admin).</summary>
public class PromotionProductLine
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }

    // Solo uno de los tres se usa según PromotionType
    public decimal? DiscountPercentage { get; set; }
    public decimal? SpecialPrice { get; set; }
    public int? RequiredQuantity { get; set; }
    public int? PayQuantity { get; set; }
}
