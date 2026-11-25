namespace Daniel_Licors_Store.Models.Product;

public class ProductEditModel
{
    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int? AgingYears { get; set; }

    public string? Size { get; set; }
    public int? SizeMl { get; set; }

    public decimal Price { get; set; }
    public int Stock { get; set; }

    // Stock que el empleado veía al abrir la edición. Sirve para no pisar las ventas
    // que ocurran mientras tiene el formulario abierto.
    public int? OriginalStock { get; set; }

    public bool IsPremium { get; set; }
    public bool IsActive { get; set; }

    public string? ImageUrl { get; set; }
}