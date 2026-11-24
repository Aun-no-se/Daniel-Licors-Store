namespace Daniel_Licors_Store.Models.Promotion;

/// <summary>Todo lo que la vista de detalle de una promoción necesita mostrar.</summary>
public class PromotionDetailDto
{
    public int PromotionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PromotionType { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }

    public List<PromotionDetailLine> Lines { get; set; } = new();

    // Mismas reglas que usa el resto del sistema (tabla de admin, carrito y catálogo).
    public bool IsVigente => IsActive && StartDate <= DateTime.Now && EndDate >= DateTime.Now;
    public bool IsUpcoming => IsActive && StartDate > DateTime.Now;
    public bool IsExpired => IsActive && EndDate < DateTime.Now;

    public string StatusLabel =>
        !IsActive ? "Inactiva" :
        IsUpcoming ? "Programada" :
        IsExpired ? "Vencida" :
        "Vigente";

    public string TypeLabel => PromotionType switch
    {
        "Discount" => "% Descuento",
        "SpecialPrice" => "Precio Especial",
        "PayXGetY" => "Lleva X Paga Y",
        _ => PromotionType ?? "Promoción"
    };

    /// <summary>La oferta en una frase corta: "Lleva 3 · Paga 2", "15% OFF"…</summary>
    public string OfferHeadline
    {
        get
        {
            switch (PromotionType)
            {
                case "PayXGetY":
                    var pay = Lines.FirstOrDefault(l => l.IsPayXGetY);
                    if (pay != null) return $"Lleva {pay.RequiredQuantity} · Paga {pay.PayQuantity}";
                    break;

                case "Discount":
                    var pcts = Lines.Where(l => l.DiscountPercentage.HasValue).Select(l => l.DiscountPercentage!.Value).ToList();
                    if (pcts.Count > 0)
                        return pcts.Distinct().Count() == 1
                            ? $"{pcts[0]:0.##}% OFF"
                            : $"Hasta {pcts.Max():0.##}% OFF";
                    break;

                case "SpecialPrice":
                    return Lines.Count == 1 ? "Precio especial" : "Precios especiales";
            }

            return TypeLabel;
        }
    }
}

/// <summary>Una bebida dentro de la promoción, con su precio normal y su precio en promoción.</summary>
public class PromotionDetailLine
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Size { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    public decimal OriginalPrice { get; set; }
    public int Stock { get; set; }
    public bool IsProductActive { get; set; } = true;

    // Solo uno de los tres se usa según el tipo de promoción.
    public decimal? DiscountPercentage { get; set; }
    public decimal? SpecialPrice { get; set; }
    public int? RequiredQuantity { get; set; }
    public int? PayQuantity { get; set; }

    public bool IsPayXGetY => RequiredQuantity is > 0 && PayQuantity is > 0 && PayQuantity <= RequiredQuantity;

    /// <summary>Precio por unidad dentro de la promoción (misma precedencia que el carrito:
    /// precio especial, luego descuento, luego "Lleva X Paga Y").</summary>
    public decimal PromoUnitPrice
    {
        get
        {
            if (SpecialPrice.HasValue) return SpecialPrice.Value;
            if (DiscountPercentage.HasValue) return OriginalPrice * (1 - DiscountPercentage.Value / 100m);
            if (IsPayXGetY) return OriginalPrice * PayQuantity!.Value / RequiredQuantity!.Value;
            return OriginalPrice;
        }
    }

    public decimal SavingPerUnit => Math.Max(OriginalPrice - PromoUnitPrice, 0);

    public decimal SavingPercent =>
        OriginalPrice > 0 ? Math.Round(SavingPerUnit / OriginalPrice * 100m, 1) : 0;

    // "Lleva X Paga Y": lo que cuesta el paquete completo de X unidades.
    public decimal PackNormalPrice => OriginalPrice * (RequiredQuantity ?? 0);
    public decimal PackPromoPrice => OriginalPrice * (PayQuantity ?? 0);
    public decimal PackSaving => PackNormalPrice - PackPromoPrice;

    public string DisplayName => string.IsNullOrWhiteSpace(Size) ? Name : $"{Name} ({Size.Trim()})";

    public string RuleLabel
    {
        get
        {
            if (SpecialPrice.HasValue) return "Precio especial";
            if (DiscountPercentage.HasValue) return $"{DiscountPercentage.Value:0.##}% de descuento";
            if (IsPayXGetY) return $"Lleva {RequiredQuantity} · Paga {PayQuantity}";
            return "Sin descuento";
        }
    }
}
