namespace Daniel_Licors_Store.Models.Order;

public class OrderResponseModel
{
    public int OrderId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}