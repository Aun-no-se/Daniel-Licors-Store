namespace Daniel_Licors_Store.Data.Entities;

public class Combo
{
    public int ComboId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal ComboPrice { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }

    public ICollection<ComboProduct> ComboProducts { get; set; } = new List<ComboProduct>();
}