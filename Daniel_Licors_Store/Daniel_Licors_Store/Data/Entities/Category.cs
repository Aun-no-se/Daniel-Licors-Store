namespace Daniel_Licors_Store.Data.Entities;

public class Category
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    // Navigation
    public ICollection<Product> Products { get; set; } = new List<Product>();
}