namespace Daniel_Licors_Store.Models.Brand;

public class BrandUpdateModel
{
    public int BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}