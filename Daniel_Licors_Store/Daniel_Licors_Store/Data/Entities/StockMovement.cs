namespace Daniel_Licors_Store.Data.Entities;

public class StockMovement
{
    public int StockMovementId { get; set; }
    public int ProductId { get; set; }
    public int StoreId { get; set; }
    public int Quantity { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Product Product { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public User? User { get; set; }
}