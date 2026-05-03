namespace Daniel_Licors_Store.Data.Entities;

public class Brand
{
    public int BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}