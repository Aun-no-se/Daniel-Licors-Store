namespace Daniel_Licors_Store.Models.Product;

public class ProductResponseModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public bool IsPremium { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public decimal Price { get; set; }   // ← agregado para mostrar en el carrusel
}
