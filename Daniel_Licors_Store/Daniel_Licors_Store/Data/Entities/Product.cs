namespace Daniel_Licors_Store.Data.Entities;

public class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public int? AgingYears { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int? BrandId { get; set; }
    public Brand? Brand { get; set; }

    public bool IsPremium { get; set; }
    public bool IsActive { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<ProductStore> ProductStores { get; set; }
        = new List<ProductStore>();
}