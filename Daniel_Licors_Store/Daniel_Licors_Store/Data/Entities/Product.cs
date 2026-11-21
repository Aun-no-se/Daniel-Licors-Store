using Daniel_Licors_Store.Data.Entities;
using Daniel_Licors_Store.Models.Product;
using System.ComponentModel.DataAnnotations.Schema;

public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AgingYears { get; set; }

    // Tamaño / presentación (ej. "355 ml", "750 ml", "1 L"). Cada tamaño es una bebida aparte,
    // con su propio precio y stock en ProductStores.
    public string? Size { get; set; }
    public int? SizeMl { get; set; }   // solo para ordenar de menor a mayor
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public bool IsPremium { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    // Nombre para mostrar en textos: "Corona (355 ml)". No es una columna.
    [NotMapped]
    public string DisplayName => ProductNaming.Compose(Name, Size);

    // Relaciones
    public Category Category { get; set; } = null!;
    public Brand? Brand { get; set; }

    public ICollection<ProductStore> ProductStores { get; set; } = new List<ProductStore>();
}