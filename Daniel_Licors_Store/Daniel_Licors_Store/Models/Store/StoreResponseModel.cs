namespace Daniel_Licors_Store.Models.Store;

public class StoreResponseModel
{
    public int StoreId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public bool IsActive { get; set; }
}