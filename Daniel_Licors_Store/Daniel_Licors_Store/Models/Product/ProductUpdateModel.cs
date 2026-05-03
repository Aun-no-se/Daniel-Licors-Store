namespace Daniel_Licors_Store.Models.Product;

public class ProductUpdateModel
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int? BrandId { get; set; }
    public bool IsPremium { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
}