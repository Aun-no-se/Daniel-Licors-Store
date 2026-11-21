namespace Daniel_Licors_Store.Models.Product;

public class ProductResponseModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Size { get; set; }   // tamaño: "355 ml", "750 ml"…
    public bool IsPremium { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public decimal Price { get; set; }   // ← agregado para mostrar en el carrusel

    /// <summary>Nombre con tamaño, para textos: "Corona (355 ml)".</summary>
    public string DisplayName => ProductNaming.Compose(Name, Size);
}
