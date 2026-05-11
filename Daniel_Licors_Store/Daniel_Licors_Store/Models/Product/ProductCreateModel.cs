namespace Daniel_Licors_Store.Models.Product;

public class ProductCreateModel
{
    public string Name { get; set; }
    public string Description { get; set; }

    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int? AgingYears { get; set; }

    public decimal Price { get; set; }
    public int Stock { get; set; }

    public string? ImageFileName { get; set; }

    public bool IsPremium { get; set; }
    public bool IsActive { get; set; } = true;
}