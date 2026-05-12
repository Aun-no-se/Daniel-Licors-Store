using Daniel_Licors_Store.Data.Entities;

public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? AgingYears { get; set; }
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public bool IsPremium { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }

    // Relaciones
    public virtual Category Category { get; set; } = null!;
    public virtual Brand? Brand { get; set; }
    // Esta es la relación con la tabla que sí tiene el precio/stock
    public virtual ICollection<ProductStore> ProductStores { get; set; } = new List<ProductStore>();
}