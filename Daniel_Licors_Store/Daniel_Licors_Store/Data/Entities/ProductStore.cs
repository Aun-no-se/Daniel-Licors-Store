namespace Daniel_Licors_Store.Data.Entities;

public class ProductStore
{
    public int ProductStoreId { get; set; }

    public int ProductId { get; set; }
    public int StoreId { get; set; }

    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool IsAvailable { get; set; }

    public Product Product { get; set; } = null!;
    public Store Store { get; set; } = null!;
}