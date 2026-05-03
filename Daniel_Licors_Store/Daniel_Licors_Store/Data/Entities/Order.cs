namespace Daniel_Licors_Store.Data.Entities;

public class Order
{
    public int OrderId { get; set; }
    public string? UserId { get; set; }
    public int StoreId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public Store Store { get; set; } = null!;

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}