namespace Daniel_Licors_Store.Models.Store;

public class StoreUpdateModel : StoreCreateModel
{
    public int StoreId { get; set; }
    public bool IsActive { get; set; }
}