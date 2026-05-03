namespace Daniel_Licors_Store.Models.Product;

public class ProductCreateModel
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public bool IsPremium { get; set; }
    public string? ImageUrl { get; set; }
}