namespace Daniel_Licors_Store.Models.Category;

public class CategoryResponseModel
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}