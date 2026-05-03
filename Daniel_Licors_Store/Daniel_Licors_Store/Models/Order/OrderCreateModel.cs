namespace Daniel_Licors_Store.Models.Order;

public class OrderCreateModel
{
    public int StoreId { get; set; }
    public string? UserId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}