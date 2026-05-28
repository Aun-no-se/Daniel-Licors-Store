namespace Daniel_Licors_Store.Models.Combo;

public class ComboDetailDto
{
    public int ComboId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal ComboPrice { get; set; }

    public string? ImageUrl { get; set; }   

    public List<ComboProductDetailDto> Products { get; set; } = new();
}